import * as vscode from 'vscode';

export function activate(context: vscode.ExtensionContext) {
    const disposable = vscode.commands.registerCommand("wrapSelection.tryCatch", async () => {
        const editor = vscode.window.activeTextEditor;

        if(!editor) {
            vscode.window.showWarningMessage("No active editor found");
            return;
        }

        const selection = editor.selection;
        const selectedText = editor.document.getText(selection);

        if(!selectedText || selectedText.trim().length === 0) {
            vscode.window.showWarningMessage("No text selected");
            return;
        }

        const document = editor.document;
        const startLine = document.lineAt(selection.start);
        const indentMatch = startLine.text.match("/^(\s*)/");
        const baseIndent = indentMatch ? indentMatch[1] : '';

        const lines = selectedText.split('\n');

        let minIndent = Number.MAX_SAFE_INTEGER;
        const nonEmptyLines = lines.filter(line => line.trim().length > 0);
        nonEmptyLines.forEach(line => {
            const indentMatch = line.match("/^(\s*)/");
            if(indentMatch) {
                minIndent = Math.min(minIndent, indentMatch[1].length);
            }
        })

        const normalizedLine = lines.map(line => {
            if(line.trim().length === 0) {
                return line;
            }
            const indentMatch = line.match("/^(\s*)/");
            if (indentMatch && indentMatch[1].length >= minIndent) {
                return line.substring(minIndent);
            }
            return line;
        });

        const indentendLines = normalizedLine.map(line => {
            if (line.trim().length === 0) {
                return line;
            }
            return baseIndent + '   ' + line;
        });

        const wrappedCode = 
            baseIndent + 'try {\n' + 
            indentendLines.join('\n') + '\n' + 
            baseIndent + '} catch (error) {\n' + 
            baseIndent + ' console.error(error);\n' + 
            baseIndent + '}';

        const success = await editor.edit(editBuilder => {
            editBuilder.replace(selection, wrappedCode);
        });

        if(!success) {
            vscode.window.showErrorMessage('Failed to wrap selection in editor');
        }
    });

    context.subscriptions.push(disposable);
}