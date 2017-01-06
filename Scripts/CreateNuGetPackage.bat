@echo off
if not exist .\NuGet\nuget.exe (
    echo Error: Cannot detect 'nuget.exe' tool in '.\NuGet\' directory
    exit
)

echo . Copying 'PSharp.nuspec' to '..\Binaries' directory
copy .\NuGet\PSharp.nuspec ..\Binaries\PSharp.nuspec

echo . Creating P# NuGet package
.\NuGet\nuget.exe pack ..\Binaries\PSharp.nuspec