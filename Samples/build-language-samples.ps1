param(
    [string]$dotnet="dotnet",
    [string]$msbuild="msbuild",
    [ValidateSet("Debug","Release")]
    [string]$configuration="Release"
)

Import-Module $PSScriptRoot\..\Scripts\powershell\common.psm1

Write-Comment -prefix "." -text "Building P# language samples" -color "yellow"
Write-Comment -prefix "..." -text "Configuration: $configuration" -color "white"

$solution = $PSScriptRoot + "\Samples.Language.sln"

$command = "restore $solution"
$error_msg = "Failed to restore packages for the P# language samples"

Invoke-ToolCommand -tool $dotnet -command $command -error_msg $error_msg

$command = "$solution /p:Configuration=$configuration"
$error_msg = "Failed to build the P# language samples"

Invoke-ToolCommand -tool $msbuild -command $command -error_msg $error_msg

Write-Comment -prefix "." -text "Successfully built P# language samples" -color "green"
