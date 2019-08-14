param(
    [string]$dotnet="dotnet",
    [ValidateSet("Debug","Release")]
    [string]$configuration="Release"
)

Import-Module $PSScriptRoot\powershell\common.psm1

# check that dotnet sdk is installed...
Function FindInPath() {
    param ([string]$name)
    $ENV:PATH.split(';') | ForEach-Object {
        If (Test-Path -Path $_\$name) {
            return $_
        }
    }
    return $null
}

$dotnet=$dotnet.Replace(".exe","")
$versions = $null
$dotnetpath=FindInPath "$dotnet.exe"
if ($dotnetpath -is [array]){
    $dotnetpath = $dotnetpath[0]
}
$sdkpath = Join-Path -Path $dotnetpath -ChildPath "sdk"
if (-not ("" -eq $dotnetpath))
{
    $versions = Get-ChildItem "$sdkpath"  -directory | Where-Object {$_ -like "2.2.4*"}
}

if ($null -eq $versions)
{
    Write-Comment -text "Please install dotnet sdk version 2.2.401 from https://www.microsoft.com/net/core." -color "yellow"
    exit
}
else
{
    if ($versions -is [array]){
        $versions = $versions[0]
    }
    Write-Comment -text "Using dotnet sdk version $versions at: $sdkpath" -color yellow
}


Write-Comment -prefix "." -text "Building P#" -color "yellow"
Write-Comment -prefix "..." -text "Configuration: $configuration" -color "white"
$solution = $PSScriptRoot + "\..\PSharp.sln"
$command = "build -c $configuration $solution"
$error_msg = "Failed to build P#"
Invoke-ToolCommand -tool $dotnet -command $command -error_msg $error_msg

Write-Comment -prefix "." -text "Successfully built P#" -color "green"
