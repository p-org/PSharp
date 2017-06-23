param(
    [Parameter(Position=0,mandatory=$true)]
    [ValidateSet("core")]
    [string]$test
)

Write-Host "====================================" -ForegroundColor "green"
Write-Host "| Running the P# performance tests |" -ForegroundColor "green"
Write-Host "====================================" -ForegroundColor "green"

if ($test -eq "core")
{
    Write-Host "==============================" -ForegroundColor "yellow"
    Write-Host "| Performance-testing 'core' |" -ForegroundColor "yellow"
    Write-Host "==============================" -ForegroundColor "yellow"
    if (-not (Test-Path $PSScriptRoot\Core.Tests.Performance\bin\Release\Microsoft.PSharp.Core.Tests.Performance.exe))
    {
        Write-Host "Error: Performance tests 'core' not found. Please build P# in 'Release' to install them." -ForegroundColor "red"
        exit
    }

    Invoke-Expression "$PSScriptRoot\Core.Tests.Performance\bin\Release\Microsoft.PSharp.Core.Tests.Performance.exe"
}

Write-Host "Done." -ForegroundColor "green" 