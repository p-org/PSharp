param(
    [string]$dotnet="dotnet",
    [ValidateSet("all,netcoreapp2.1,net46")]
    [string]$framework="all",
    [ValidateSet("all","testing-services")]
    [string]$test="all"
)

Import-Module $PSScriptRoot\powershell\common.psm1

$frameworks = "netcoreapp2.1", "net46"

$targets = [ordered]@{
    "testing-services" = "TestingServices.Tests.Integration"
}

Write-Comment -prefix "." -text "Running the P# integration tests" -color "yellow"
foreach ($kvp in $targets.GetEnumerator()) {
    if (($test -ne "all") -and ($test -ne $($kvp.Name))) {
        continue
    }

    foreach ($f in $frameworks) {
        if (($framework -ne "all") -and ($f -ne $framework)) {
            continue
        }

        $target = "$PSScriptRoot\..\Tests\$($kvp.Value)\$($kvp.Value).csproj"
        Invoke-DotnetTest -dotnet $dotnet -project $($kvp.Name) -target $target -framework $f
    }
}

Write-Comment -prefix "." -text "Done testing." -color "green"
