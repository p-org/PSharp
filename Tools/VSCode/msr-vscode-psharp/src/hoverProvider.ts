/*---------------------------------------------------------
 * Copyright (C) Microsoft Corporation. All rights reserved.
 *--------------------------------------------------------*/

'use strict';

import vscode = require('vscode');
import { HoverProvider, Hover, TextDocument, Position, CancellationToken } from 'vscode';
import { isPositionInQuotedString, tokenTips } from './utils';

export class PSharpHoverProvider implements HoverProvider {
	public provideHover(document: TextDocument, position: Position, token: CancellationToken): Thenable<Hover> {
		let wordRange = document.getWordRangeAtPosition(position);
		let lineText = document.lineAt(position.line).text;
		let word = wordRange ? document.getText(wordRange) : '';
	
		if (!wordRange || lineText.startsWith('//') || isPositionInQuotedString(lineText, position) || word.match(/^\d+.?\d+$/)) {
			return Promise.resolve(null);
		}
	
		let tipText = tokenTips.get(word);
		return tipText ? Promise.resolve(new Hover(tipText)) : Promise.resolve(null);
	}
}
