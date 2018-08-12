param(
    [string]$dotnet="dotnet",
    [ValidateSet("all","netcoreapp2.1","net46")]
    [string]$framework="all",
    [ValidateSet("all","core","testing-services","shared-objects","language-services","static-analysis")]
    [string]$test="all",
    [string]$filter="",
    [ValidateSet("quiet","minimal","normal","detailed","diagnostic")]
    [string]$v="normal"
)

Import-Module $PSScriptRoot\powershell\common.psm1

$frameworks = "netcoreapp2.1", "net46"

$targets = [ordered]@{
    "core" = "Core.Tests.Unit"
    "testing-services" = "TestingServices.Tests.Unit"
    "language-services" = "LanguageServices.Tests.Unit"
    "static-analysis" = "StaticAnalysis.Tests.Unit"
    "shared-objects" = "SharedObjects.Tests.Unit"
}

Write-Comment -prefix "." -text "Running the P# unit tests" -color "yellow"
foreach ($kvp in $targets.GetEnumerator()) {
    if (($test -ne "all") -and ($test -ne $($kvp.Name))) {
        continue
    }

    foreach ($f in $frameworks) {
        if (($framework -ne "all") -and ($f -ne $framework)) {
            continue
        }

        if ((($($kvp.Name) -eq "language-services") -and ($f -eq "netcoreapp2.1")) -or
            (($($kvp.Name) -eq "language-services") -and ($f -eq "net45")) -or
            (($($kvp.Name) -eq "static-analysis") -and ($f -eq "netcoreapp2.1")) -or
            (($($kvp.Name) -eq "static-analysis") -and ($f -eq "net45"))) {
            # Not supported
            continue
        }

        $target = "$PSScriptRoot\..\Tests\$($kvp.Value)\$($kvp.Value).csproj"
        Invoke-DotnetTest -dotnet $dotnet -project $($kvp.Name) -target $target -filter $filter -framework $f -verbosity $v
    }
}

Write-Comment -prefix "." -text "Done testing." -color "green"
