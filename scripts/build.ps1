param(
    [string]$dotnet="dotnet",
    [string]$msbuild="msbuild",
    [ValidateSet("Debug","Release")]
    [string]$configuration="Release"
)

Import-Module $PSScriptRoot\powershell\common.psm1

Write-Comment -prefix "." -text "Building P#" -color "yellow"
Write-Comment -prefix "..." -text "Configuration: $configuration" -color "white"
$solution = $PSScriptRoot + "\..\PSharp.sln"
$command = "build -c $configuration $solution"
$error_msg = "Failed to build P#"
Invoke-ToolCommand -tool $dotnet -command $command -error_msg $error_msg

Write-Comment -prefix "." -text "Successfully built P#" -color "green"
