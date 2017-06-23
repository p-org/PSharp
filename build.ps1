param(
    [string]$dotnet="dotnet",
    [string]$msbuild="msbuild",
    [ValidateSet("net46")]
    [string]$framework="net46",
    [ValidateSet("Debug","Release")]
    [string]$configuration="Release",
    [bool]$restore=$true,
    [bool]$build=$true,
    [switch]$samples,
    [switch]$show
)

# Paths to the various directories.
$solution = $PSScriptRoot + "\PSharp.sln"
$samples_dir = $PSScriptRoot + "\Samples"

Import-Module $PSScriptRoot\Scripts\common.psm1

Write-Comment -prefix "." -text "Building P# using the '$framework' framework" -color "yellow"

Write-Comment -prefix "..." -text "Restoring packages (might take several minutes)" -color "white"
$command = "restore"
$error_message = "Failed to restore packages"
Invoke-ToolCommand -tool $dotnet -command $command -error_message $error_message -show_output $show.IsPresent

Write-Comment -prefix "..." -text "Building P# using the '$configuration' configuration" -color "white"
$command = "/p:Configuration=$configuration $solution"
$error_message = "Failed to build P#"
Invoke-ToolCommand -tool $msbuild -command $command -error_message $error_message -show_output $show.IsPresent

Write-Comment -prefix "." -text "Successfully built P#" -color "green"

# Checks if samples should be built.
if ($samples.IsPresent -eq $true)
{
    $command = "$samples_dir\build.ps1 -dotnet $dotnet -msbuild $msbuild -framework $framework -configuration $configuration"
    switch ($show)
    {
        $true { Invoke-Expression "$command -show"; break }
        default { Invoke-Expression $command; break }
    }
}
