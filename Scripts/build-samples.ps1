param(
    [string]$dotnet="dotnet",
    [ValidateSet("Debug","Release")]
    [string]$configuration="Release"
)

Import-Module $PSScriptRoot\powershell\common.psm1

Write-Comment -prefix "." -text "Building the P# samples" -color "yellow"
Write-Comment -prefix "..." -text "Configuration: $configuration" -color "white"
$solution = $PSScriptRoot + "\..\Samples\Samples.sln"
$command = "build -c $configuration $solution"
$error_msg = "Failed to build the P# samples"
Invoke-ToolCommand -tool $dotnet -command $command -error_msg $error_msg

Write-Comment -prefix "." -text "Successfully built the P# samples" -color "green"
