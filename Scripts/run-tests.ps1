param(
    [string]$dotnet="dotnet",
    [ValidateSet("all","netcoreapp3.1")]
    [string]$framework="all",
    [ValidateSet("all","core","testing-services","shared-objects","language-services","static-analysis")]
    [string]$test="all",
    [string]$filter="",
    [ValidateSet("quiet","minimal","normal","detailed","diagnostic")]
    [string]$v="normal"
)

Import-Module $PSScriptRoot\powershell\common.psm1

$frameworks = "netcoreapp3.1"

$targets = [ordered]@{
    "core" = "Core.Tests"
    "testing-services" = "TestingServices.Tests"
    "language-services" = "LanguageServices.Tests"
    "static-analysis" = "StaticAnalysis.Tests"
    "shared-objects" = "SharedObjects.Tests"
}

Write-Comment -prefix "." -text "Running the P# tests" -color "yellow"
foreach ($kvp in $targets.GetEnumerator()) {
    if (($test -ne "all") -and ($test -ne $($kvp.Name))) {
        continue
    }

    foreach ($f in $frameworks) {
        if (($framework -ne "all") -and ($f -ne $framework)) {
            continue
        }

        if ((($($kvp.Name) -eq "language-services") -and ($f -eq "netcoreapp3.1")) -or
            (($($kvp.Name) -eq "static-analysis") -and ($f -eq "netcoreapp3.1"))) {
            # Not supported
            continue
        }

        $target = "$PSScriptRoot\..\Tests\$($kvp.Value)\$($kvp.Value).csproj"
        Invoke-DotnetTest -dotnet $dotnet -project $($kvp.Name) -target $target -filter $filter -framework $f -verbosity $v
    }
}

Write-Comment -prefix "." -text "Done" -color "green"
