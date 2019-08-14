param(
    [string]$dotnet="dotnet",
    [ValidateSet("Debug","Release")]
    [string]$configuration="Release"
)


foreach ($e in Get-ChildItem Env:)
{
    $key = $e.Key
    $value = $e.Value
    Write-Host "$key=$value"
}
exit
Import-Module $PSScriptRoot\powershell\common.psm1

Write-Comment -prefix "." -text "Building P#" -color "yellow"
Write-Comment -prefix "..." -text "Configuration: $configuration" -color "white"
$solution = $PSScriptRoot + "\..\PSharp.sln"
$command = "build -c $configuration $solution"
$error_msg = "Failed to build P#"
Invoke-ToolCommand -tool $dotnet -command $command -error_msg $error_msg

Write-Comment -prefix "." -text "Successfully built P#" -color "green"
