# msr-vscode-psharp README

This extension provides syntax highlighting, snippets, and tooltips for P# code in VSCode.

## Features

This contains extension features that do not rely on P# parsing.

## Dependencies
With location being either "-g" or "--save-dev", run:
- npm install location typescript
- npm install location vscode
- npm install (this appears to be necessary in order to get node_modules\vscode\vscode.d.ts to be installed; it is not part of DefinitelyTyped as it is version-dependent)

## Building and Testing
- Select Tasks->Run Build Task.
    - If you see "error TS5001: The current host does not support the '--watch' option.", then run the Dependencies above, then rerun "Run Build Task".
    - copy the msr-vscode-psharp directory tree to <user directory>\.vscode\extensions\msr-vscode-psharp

## Release Notes

### 05/08/2018

* Initial merge to master

-----------------------------------------------------------------------------------------------------------

## Working with Markdown

**Note:** You can author your README using Visual Studio Code.  Here are some useful editor keyboard shortcuts:

* Split the editor (`Cmd+\` on OSX or `Ctrl+\` on Windows and Linux)
* Toggle preview (`Shift+CMD+V` on OSX or `Shift+Ctrl+V` on Windows and Linux)
* Press `Ctrl+Space` (Windows, Linux) or `Cmd+Space` (OSX) to see a list of Markdown snippets
* Back up in the history of this file for examples of other sections, image links, and indented comments with a left border bar.

### For more information

* [Visual Studio Code's Markdown Support](http://code.visualstudio.com/docs/languages/markdown)
* [Markdown Syntax Reference](https://help.github.com/articles/markdown-basics/)

**Enjoy!**