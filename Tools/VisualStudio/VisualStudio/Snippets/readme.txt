This directory contains the P# code snippets. For details, see the code in ..\Intellisense\CompletionCommandHandler.cs.

This comes from the "Walkthrough: Implement Code Snippets" at https://docs.microsoft.com/en-us/visualstudio/extensibility/walkthrough-implementing-code-snippets?view=vs-2019.

To run these on your own system:
- Open the VisualStudioTools.sln in VS
- Make any desired changes in these files.
- xcopy /s .\1033 %VSINSTALLDIR%\P#\Snippets\1033\
    - do not copy this readme file
- hit F5 (or ctrl-F5) to run. This assumes you have the usual VS-extension debugging setup in the VisualStudio project:
    - Start External Program: C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\Common7\IDE\devenv.exe
    - Command line arguments: /rootSuffix ToolsDev          (or whatever name you want)

This will start another instance of VS in the "hive" named ToolsDev (or whatever name you used).
For a quick test, open a .psharp file and in an appropriate location type "onevgo" and then hit <tab>; the expansion of
"on event ... goto" should be inserted.

The current list of expansions and their shortcuts is available via "findstr /is shortcut .\1033\*.snippet" from this
directory; as of this writing it is:
    declare event:              event
    if:                         if
    if .. else:                 ife
    declare machine:            machine
    on event .. do:             onevdo
    on event .. goto:           onevgo
    on event .. goto .. with:   onevgow
    on event .. push:           onevpu
    declare state:              state
