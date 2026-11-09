import * as fs from 'fs';
import * as path from 'path';
import { spawn } from 'child_process';
import * as vscode from 'vscode';

/** Published C# analyzer, relative to the extension root (`dotnet publish -c Release -o <extension>/server`). */
const SERVER_DLL = path.join('server', 'GettingStartedCS.dll');

/** Folders that never contain hand-written source: build output, VCS and tooling. */
const SKIPPED_DIRS = new Set(['bin', 'obj', '.git', '.vs', '.vscode', 'node_modules']);

/** One violation as the C# analyzer writes it to stdout. Positions are 0-based, as Roslyn reports them. */
interface Violation {
    file: string;
    line: number;
    column: number;
    constraint: string;
    message: string;
}

let diagnostics: vscode.DiagnosticCollection;
let output: vscode.OutputChannel;
let isRunning = false;


export function activate(context: vscode.ExtensionContext) {
    diagnostics = vscode.languages.createDiagnosticCollection('ddd-compliance');
    output = vscode.window.createOutputChannel('DDD Compliance');

    context.subscriptions.push(
        diagnostics,
        output,
        vscode.commands.registerCommand(
            'ddd-tactical-pattern-compliance.checkCompliance',
            () => checkCompliance(context)
        )
    );
}

export function deactivate() {}


async function checkCompliance(context: vscode.ExtensionContext): Promise<void> {
    if (isRunning) {
        vscode.window.showInformationMessage('A DDD compliance check is already running.');
        return;
    }

    const dir = await selectDirectory();
    if (!dir) {
        return;
    }

    const serverDll = context.asAbsolutePath(SERVER_DLL);
    if (!fs.existsSync(serverDll)) {
        vscode.window.showErrorMessage(
            `Analyzer not found at ${serverDll}. Publish the C# project into the extension's "server" folder first.`
        );
        return;
    }

    isRunning = true;
    try {
        // The analyzer reads the files from disk, so save open editors first.
        await vscode.workspace.saveAll(false);

        const result = await vscode.window.withProgress(
            { location: vscode.ProgressLocation.Notification, title: 'DDD compliance', cancellable: true },
            async (progress, token) => {
                progress.report({ message: 'Scanning for C# files…' });
                const files = await getProgramFiles(dir);

                if (token.isCancellationRequested) {
                    throw new vscode.CancellationError();
                }
                if (files.length === 0) {
                    return { files, violations: [] as Violation[] };
                }

                progress.report({ message: `Analyzing ${files.length} files…` });
                const violations = await runAnalyzer(serverDll, files, token);
                return { files, violations };
            }
        );

        if (result.files.length === 0) {
            diagnostics.clear();
            vscode.window.showInformationMessage(`No C# files found in ${dir.fsPath}.`);
            return;
        }

        showViolations(result.violations);
        reportSummary(result.files.length, result.violations);
    } catch (error) {
        if (error instanceof vscode.CancellationError) {
            return;
        }
        const message = error instanceof Error ? error.message : String(error);
        // Not awaited, so the next check can start while this message is still open.
        vscode.window
            .showErrorMessage(`DDD compliance check failed: ${message}`, 'Show Log')
            .then(choice => {
                if (choice === 'Show Log') {
                    output.show(true);
                }
            });
    } finally {
        isRunning = false;
    }
}

export async function selectDirectory(): Promise<vscode.Uri | undefined> {
    const folderUri = await vscode.window.showOpenDialog({
        canSelectFiles: false,
        canSelectFolders: true,
        canSelectMany: false,
        defaultUri: vscode.workspace.workspaceFolders?.[0]?.uri,
        openLabel: 'Select Folder',
        title: 'Select a folder to check for DDD compliance',
    });

    return folderUri?.[0];
}

export async function getProgramFiles(dir: vscode.Uri): Promise<vscode.Uri[]> {
    const programFiles: vscode.Uri[] = [];

    async function readDirRecursive(currentDir: vscode.Uri): Promise<void> {
        const entries = await vscode.workspace.fs.readDirectory(currentDir);

        for (const [name, type] of entries) {
            const fileUri = vscode.Uri.joinPath(currentDir, name);

            if (type === vscode.FileType.Directory) {
                if (!SKIPPED_DIRS.has(name)) {
                    await readDirRecursive(fileUri);
                }
            } else if (type === vscode.FileType.File && name.endsWith('.cs')) {
                programFiles.push(fileUri);
            }
        }
    }

    await readDirRecursive(dir);
    return programFiles;
}


function runAnalyzer(
    serverDll: string,
    files: vscode.Uri[],
    token: vscode.CancellationToken
): Promise<Violation[]> {
    return new Promise((resolve, reject) => {
        const proc = spawn('dotnet', [serverDll], { windowsHide: true });
        let stdout = '';
        let stderr = '';

        const cancellation = token.onCancellationRequested(() => {
            proc.kill();
            reject(new vscode.CancellationError());
        });

        proc.stdout.setEncoding('utf8');
        proc.stderr.setEncoding('utf8');
        proc.stdout.on('data', chunk => (stdout += chunk));
        proc.stderr.on('data', chunk => (stderr += chunk));

        proc.on('error', (err: NodeJS.ErrnoException) => {
            cancellation.dispose();
            reject(
                err.code === 'ENOENT'
                    ? new Error('"dotnet" was not found on PATH. Install the .NET runtime.')
                    : err
            );
        });

        proc.on('close', code => {
            cancellation.dispose();
            logRun(files.length, code, stderr);

            if (code !== 0) {
                reject(new Error(firstLine(stderr) ?? `Analyzer exited with code ${code}.`));
                return;
            }
            try {
                resolve(parseViolations(stdout));
            } catch (e) {
                const reason = e instanceof Error ? e.message : String(e);
                reject(new Error(`Could not read analyzer output (${reason}). Is something besides the JSON result writing to stdout?`));
            }
        });

        // If the analyzer exits before reading stdin, writing fails with EPIPE. The 'close'
        // handler above reports the real cause from stderr, so this error is ignored.
        proc.stdin.on('error', () => {});

        // Paths go over stdin, not as arguments (Windows limits command lines to ~32K characters).
        proc.stdin.end(JSON.stringify({ files: files.map(f => f.fsPath) }));
    });
}

function parseViolations(stdout: string): Violation[] {
    const data: unknown = JSON.parse(stdout);
    if (!Array.isArray(data)) {
        throw new Error('expected a JSON array');
    }
    return data as Violation[];
}

function logRun(fileCount: number, code: number | null, stderr: string): void {
    output.appendLine(`[${new Date().toLocaleTimeString()}] Analyzed ${fileCount} files, exit code ${code}`);
    if (stderr.trim()) {
        output.appendLine(stderr.trimEnd());
    }
}

function firstLine(text: string): string | undefined {
    return text.split(/\r?\n/).find(line => line.trim())?.trim();
}

// ─── Showing results ────────────────────────────────────────────────────────────

function showViolations(violations: Violation[]): void {
    diagnostics.clear();

    const byFile = new Map<string, vscode.Diagnostic[]>();
    for (const v of violations) {
        // A zero-width range: VS Code underlines the word at this position (the class name).
        const pos = new vscode.Position(Math.max(0, v.line), Math.max(0, v.column));
        const d = new vscode.Diagnostic(new vscode.Range(pos, pos), v.message, vscode.DiagnosticSeverity.Warning);
        d.source = 'DDD';
        d.code = v.constraint;

        const list = byFile.get(v.file) ?? [];
        list.push(d);
        byFile.set(v.file, list);
    }

    for (const [file, list] of byFile) {
        diagnostics.set(vscode.Uri.file(file), list);
    }
}

function reportSummary(fileCount: number, violations: Violation[]): void {
    if (violations.length === 0) {
        vscode.window.showInformationMessage(`No DDD violations found in ${fileCount} C# files.`);
        return;
    }

    const affectedFiles = new Set(violations.map(v => v.file)).size;
    vscode.window
        .showWarningMessage(
            `Found ${violations.length} DDD violation(s) in ${affectedFiles} file(s).`,
            'Show Problems'
        )
        .then(choice => {
            if (choice === 'Show Problems') {
                vscode.commands.executeCommand('workbench.panel.markers.view.focus');
            }
        });
}