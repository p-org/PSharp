'use strict';

import vscode = require('vscode');
import { PSharpHoverProvider } from './hoverProvider';

export let errorDiagnosticCollection: vscode.DiagnosticCollection;
export let warningDiagnosticCollection: vscode.DiagnosticCollection;

const PSHARP_MODE: vscode.DocumentFilter = { language: 'psharp', scheme: 'file' };

export function activate(ctx: vscode.ExtensionContext): void {
	ctx.subscriptions.push(vscode.languages.registerHoverProvider(PSHARP_MODE, new PSharpHoverProvider()));
}

function deactivate() {
}

