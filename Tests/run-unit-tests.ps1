param(
    [ValidateSet("Release","Debug")]
    [string]
    $configuration="Release",
    [ValidateSet("all","core","testing-services","language-services","static-analysis")]
    [string]
    $test="all"
)

if (-not (Test-Path $PSScriptRoot\..\packages\xunit.runner.console.2.2.0\tools\xunit.console.exe))
{
    Write-Host "Error: the xUnit console runner was not found. Please build P# to install the NuGet package." -ForegroundColor "red"
    exit
}

Write-Host "=============================" -ForegroundColor "green"
Write-Host "| Running the P# unit tests |" -ForegroundColor "green"
Write-Host "=============================" -ForegroundColor "green"

if (($test -eq "all") -or ($test -eq "core"))
{
    Write-Host "=======================" -ForegroundColor "yellow"
    Write-Host "| Unit-testing 'core' |" -ForegroundColor "yellow"
    Write-Host "======================="-ForegroundColor "yellow"
    if (-not (Test-Path $PSScriptRoot\Core.Tests.Unit\bin\$configuration\Microsoft.PSharp.Core.Tests.Unit.dll))
    {
        Write-Host "Error: Unit tests 'core' not found. Please build P# in '$configuration' to install them." -ForegroundColor "red"
        exit
    }

    Invoke-Expression "$PSScriptRoot\..\packages\xunit.runner.console.2.2.0\tools\xunit.console.exe $PSScriptRoot\Core.Tests.Unit\bin\$configuration\Microsoft.PSharp.Core.Tests.Unit.dll -verbose -parallel none"
}

if (($test -eq "all") -or ($test -eq "testing-services"))
{
    Write-Host "===================================" -ForegroundColor "yellow"
    Write-Host "| Unit-testing 'testing-services' |" -ForegroundColor "yellow"
    Write-Host "===================================" -ForegroundColor "yellow"
    if (-not (Test-Path $PSScriptRoot\TestingServices.Tests.Unit\bin\$configuration\Microsoft.PSharp.TestingServices.Tests.Unit.dll))
    {
        Write-Host "Error: Unit tests 'testing-services' not found. Please build P# in '$configuration' to install them." -ForegroundColor "red"
        exit
    }

    Invoke-Expression "$PSScriptRoot\..\packages\xunit.runner.console.2.2.0\tools\xunit.console.exe $PSScriptRoot\TestingServices.Tests.Unit\bin\$configuration\Microsoft.PSharp.TestingServices.Tests.Unit.dll -verbose -parallel none"
}

if (($test -eq "all") -or ($test -eq "language-services"))
{
    Write-Host "====================================" -ForegroundColor "yellow"
    Write-Host "| Unit-testing 'language-services' |" -ForegroundColor "yellow"
    Write-Host "====================================" -ForegroundColor "yellow"
    if (-not (Test-Path $PSScriptRoot\LanguageServices.Tests.Unit\bin\$configuration\Microsoft.PSharp.LanguageServices.Tests.Unit.dll))
    {
        Write-Host "Error: Unit tests 'language-services' not found. Please build P# in '$configuration' to install them." -ForegroundColor "red"
        exit
    }

    Invoke-Expression "$PSScriptRoot\..\packages\xunit.runner.console.2.2.0\tools\xunit.console.exe $PSScriptRoot\LanguageServices.Tests.Unit\bin\$configuration\Microsoft.PSharp.LanguageServices.Tests.Unit.dll -verbose"   
}

if (($test -eq "all") -or ($test -eq "static-analysis"))
{
    Write-Host "==================================" -ForegroundColor "yellow"
    Write-Host "| Unit-testing 'static-analysis' |" -ForegroundColor "yellow"
    Write-Host "==================================" -ForegroundColor "yellow"
    if (-not (Test-Path $PSScriptRoot\StaticAnalysis.Tests.Unit\bin\$configuration\Microsoft.PSharp.StaticAnalysis.Tests.Unit.dll))
    {
        Write-Host "Error: Unit tests 'static-analysis' not found. Please build P# in '$configuration' to install them." -ForegroundColor "red"
        exit
    }

    Invoke-Expression "$PSScriptRoot\..\packages\xunit.runner.console.2.2.0\tools\xunit.console.exe $PSScriptRoot\StaticAnalysis.Tests.Unit\bin\$configuration\Microsoft.PSharp.StaticAnalysis.Tests.Unit.dll -verbose"  
}

Write-Host "Done." -ForegroundColor "green" 