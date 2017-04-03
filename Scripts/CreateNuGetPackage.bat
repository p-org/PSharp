@echo off
if not exist .\NuGet\nuget.exe (
    echo . Downloading 'nuget.exe' tool
    powershell -command Invoke-WebRequest "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe" -OutFile ".\NuGet\nuget.exe"
    if not exist .\NuGet\nuget.exe (
        echo Error: Unable to download 'nuget.exe'. Please download from "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe" and place in 'PSharp\Scripts\NuGet\' directory.
        exit
    )
)

echo . Creating P# NuGet package
.\NuGet\nuget.exe pack .\NuGet\PSharp.nuspec