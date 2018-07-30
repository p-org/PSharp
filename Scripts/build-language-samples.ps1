param(
    [string]$dotnet="dotnet",
    [string]$msbuild="msbuild",
    [ValidateSet("Debug","Release")]
    [string]$configuration="Release"
)

Import-Module $PSScriptRoot\powershell\common.psm1

$samples_dir = "$PSScriptRoot\..\Samples"
$projects = "BoundedAsync\BoundedAsync.PSharpLanguage\BoundedAsync.PSharpLanguage.csproj",
    "CacheCoherence\CacheCoherence.PSharpLanguage\CacheCoherence.PSharpLanguage.csproj",
    "FailureDetector\FailureDetector.PSharpLanguage\FailureDetector.PSharpLanguage.csproj",
    "MultiPaxos\MultiPaxos.PSharpLanguage\MultiPaxos.PSharpLanguage.csproj",
    "PingPong\PingPong.CustomLogging\PingPong.CustomLogging.csproj",
    "PingPong\PingPong.MixedMode\PingPong.MixedMode.csproj",
    "PingPong\PingPong.PSharpLanguage\PingPong.PSharpLanguage.csproj",
    "PingPong\PingPong.PSharpLanguage.AsyncAwait\PingPong.PSharpLanguage.AsyncAwait.csproj",
    "Raft\Raft.PSharpLanguage\Raft.PSharpLanguage.csproj",
    "ReplicatingStorage\ReplicatingStorage.PSharpLanguage\ReplicatingStorage.PSharpLanguage.csproj"

Write-Comment -prefix "." -text "Building P# samples" -color "yellow"
Write-Comment -prefix "..." -text "Configuration: $configuration" -color "white"
foreach ($project in $projects) {
    Invoke-DotnetRestore -dotnet $dotnet -project $samples_dir\$project
    New-MSBuildProject -msbuild $msbuild -project $samples_dir\$project -configuration $configuration
}

Write-Comment -prefix "." -text "Successfully built P# samples" -color "green"
