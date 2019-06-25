This directory contains the P# code snippets. For details, see the code in ..\Intellisense\CompletionCommandHandler.cs.

This comes from the "Walkthrough: Implement Code Snippets" at https://docs.microsoft.com/en-us/visualstudio/extensibility/walkthrough-implementing-code-snippets?view=vs-2019.

To run these on your own system at development time:
- Open VisualStudioTools.sln in VS
- Make any desired changes in these files.
- hit F5 (or ctrl-F5) to run. This assumes you have the usual VS-extension debugging setup in the VisualStudio project:
    1. Start External Program: C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\Common7\IDE\devenv.exe
    2. Command line arguments: /rootSuffix ToolsDev          (or whatever name you want)
    3. VisualStudio.csproj under the DEBUG Configuration PropertyGroup must have:
		<VSSDKTargetPlatformRegRootSuffix>ToolsDev</VSSDKTargetPlatformRegRootSuffix>
		(must match the name in step 2)

This will start another instance of VS in the "hive" named ToolsDev (or whatever name you used).
For a quick test, open a .psharp file and in an appropriate location type "onevgo" and then hit <tab>; the expansion of
"on event ... goto" should be inserted.

Another test of correctness is to open Tools->Code Snippets Manager, select PSharp from the dropdown, and ensure that 
there is an expandable PSharp entry in the lower area and that expanding it shows the expanded $PackageFolder$ path in
"Location" and the list of P# snippets appears in the expansion.

After building the VSIX is in .\Tools\VisualStudio\Binaries\Microsoft.PSharp.VisualStudio.vsix and this can be installed
in the normal (double-click on it) way.

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
