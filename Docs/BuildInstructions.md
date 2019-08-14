Building P# from source
=======================

# Prerequisites
Install [Visual Studio 2019](https://www.visualstudio.com/downloads/) and [.NET Core](https://www.microsoft.com/net/core) version "2.2.401" from [here](https://www.microsoft.com/net/core). Also install all the SDK versions of the .NET Framework that P# currently supports (4.5 and 4.6) from [here](https://www.microsoft.com/net/download/archives).

Optional: Get the [Visual Studio 2019 SDK](https://docs.microsoft.com/en-us/visualstudio/extensibility/installing-the-visual-studio-sdk?view=vs-2019
) to be able to compile the P# visual studio extension (syntax highlighting). Only for the high-level P# language.

# Building the P# project
To build P#, either open `PSharp.sln` and build from inside Visual Studio (you may need to run `dotnet restore` from the command line prior to opening the solution in order to successfully compile), or run the following powershell script (available in the root directory) from the Visual Studio developer command prompt:
```
powershell -c .\Scripts\build.ps1
```

# Building the samples
To build the samples, run the above script with the `samples` option:
```
powershell -c .\build.ps1 -samples
```

# Running the tests
To run all available tests, execute the following powershell script (available in the `Scripts` directory):
```
.\Scripts\run-tests.ps1
```

To run only a specific category of tests, use the `-test` option to specify the category name, for example:
```
.\Scripts\run-tests.ps1 -test core
```
