import * as vscode from 'vscode';

export async function selectDirectory(): Promise<vscode.Uri | undefined> {
    const folderUri = await vscode.window.showOpenDialog({
        canSelectFiles: false,
        canSelectFolders: true,
        canSelectMany: false,
        openLabel: 'Select Folder',
        title: 'Select a Folder',
    });

    return folderUri?.[0];
}

export async function getProgramFiles(
    dir: vscode.Uri
): Promise<vscode.Uri[]> {
    const programFiles: vscode.Uri[] = [];

    async function readDirRecursive(currentDir: vscode.Uri): Promise<void> {
        const entries = await vscode.workspace.fs.readDirectory(currentDir);

        for (const [name, type] of entries) {
            const fileUri = vscode.Uri.joinPath(currentDir, name);

            if (type === vscode.FileType.Directory) {
                await readDirRecursive(fileUri);
            } else if (
                type === vscode.FileType.File &&
                name.endsWith('.cs')
            ) {
                programFiles.push(fileUri);
            }
        }
    }

    await readDirRecursive(dir);

    return programFiles;
}

export function showFilesPanel(files: vscode.Uri[]): void {
    const panel = vscode.window.createWebviewPanel(
        'csharpFiles',
        'C# Files',
        vscode.ViewColumn.One,
        {}
    );

    const fileList = files
        .map(file => `<li>${file.fsPath}</li>`)
        .join('');

    panel.webview.html = `
        <!DOCTYPE html>
        <html>
        <body>
            <h1>C# Files</h1>
            <p>${files.length} files found</p>

            <ul>
                ${fileList || '<li>No C# files found</li>'}
            </ul>
        </body>
        </html>
    `;
}

export function activate(context: vscode.ExtensionContext) {
    const disposable = vscode.commands.registerCommand(
        'ddd-tactical-pattern-compliance.checkCompliance',
        async () => {
            const dir = await selectDirectory();

            if (!dir) {
                return;
            }

            try {
                const files = await getProgramFiles(dir);

                showFilesPanel(files);
            } catch (error) {
                vscode.window.showErrorMessage(
                    `Failed to scan directory: ${error}`
                );
            }
        }
    );

    context.subscriptions.push(disposable);
}

