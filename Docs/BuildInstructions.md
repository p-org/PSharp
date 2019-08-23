Building P# from source
=======================

# Prerequisites
Install [Visual Studio 2019](https://www.visualstudio.com/downloads/) and if necessary a version of [.NET Core](https://dotnet.microsoft.com/download/dotnet-core) that matches the version specified in the [global.json](../global.json) file. See [version matching rules](https://docs.microsoft.com/en-us/dotnet/core/tools/global-json).  Also install all the SDK versions of the .NET Framework that P# currently supports (4.5 and 4.6) from [here](https://www.microsoft.com/net/download/archives).

Optional: Get the [Visual Studio 2019 SDK](https://docs.microsoft.com/en-us/visualstudio/extensibility/installing-the-visual-studio-sdk?view=vs-2019
) to be able to compile the P# visual studio extension (syntax highlighting). Only for the high-level P# language.

# Building the P# project
To build P#, either open `PSharp.sln` and build from inside Visual Studio (you may need to run `dotnet restore` from the command line prior to opening the solution in order to successfully compile), or run the following powershell script (available in the root directory) from the Visual Studio developer command prompt:
```
powershell -c .\Scripts\build.ps1
```

# Building the samples
To build the samples, first run the following to package up PSharp as
a local nuget package:
```
powershell -c .\Scripts\create-nuget-packages.ps1
```

Then run the above script with the `samples` option:
```
powershell -c .\Samples\build-framework-samples.ps1
powershell -c .\Samples\build-language-samples.ps1
```

# Running the tests
To run all available tests, execute the following powershell script (available in the `Scripts` directory):
```
powershell -c .\Scripts\run-tests.ps1
```

To run only a specific category of tests, use the `-test` option to specify the category name, for example:
```
powershell -c .\Scripts\run-tests.ps1 -test core
```
