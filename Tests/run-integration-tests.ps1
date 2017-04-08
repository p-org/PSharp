param(
    [ValidateSet("Release","Debug")]
    [string]$configuration="Release",
    [ValidateSet("all","testing-services")]
    [string]$test="all"
)

if (-not (Test-Path $PSScriptRoot\..\packages\xunit.runner.console.2.2.0\tools\xunit.console.exe))
{
    Write-Host "Error: the xUnit console runner was not found. Please build P# to install the NuGet package." -ForegroundColor "red"
    exit
}

Write-Host "====================================" -ForegroundColor "green"
Write-Host "| Running the P# integration tests |" -ForegroundColor "green"
Write-Host "====================================" -ForegroundColor "green"

if (($test -eq "all") -or ($test -eq "testing-services"))
{
    Write-Host "==========================================" -ForegroundColor "yellow"
    Write-Host "| Integration-testing 'testing-services' |" -ForegroundColor "yellow"
    Write-Host "==========================================" -ForegroundColor "yellow"
    if (-not (Test-Path $PSScriptRoot\TestingServices.Tests.Integration\bin\$configuration\Microsoft.PSharp.TestingServices.Tests.Integration.dll))
    {
        Write-Host "Error: Integration tests 'testing-services' not found. Please build P# in '$configuration' to install them." -ForegroundColor "red"
        exit
    }

    Invoke-Expression "$PSScriptRoot\..\packages\xunit.runner.console.2.2.0\tools\xunit.console.exe $PSScriptRoot\TestingServices.Tests.Integration\bin\$configuration\Microsoft.PSharp.TestingServices.Tests.Integration.dll -verbose"
}

Write-Host "Done." -ForegroundColor "green" 