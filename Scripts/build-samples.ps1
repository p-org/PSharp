param(
    [string]$dotnet="dotnet",
    [ValidateSet("Debug","Release")]
    [string]$configuration="Release"
)

Import-Module $PSScriptRoot\powershell\common.psm1

$samples_dir = "$PSScriptRoot\..\Samples"
$projects = "BoundedAsync\BoundedAsync.PSharpLibrary\BoundedAsync.PSharpLibrary.csproj",
    "CacheCoherence\CacheCoherence.PSharpLibrary\CacheCoherence.PSharpLibrary.csproj",
    "ChainReplication\ChainReplication.PSharpLibrary\ChainReplication.PSharpLibrary.csproj",
    "Chord\Chord.PSharpLibrary\Chord.PSharpLibrary.csproj",
    "FailureDetector\FailureDetector.PSharpLibrary\FailureDetector.PSharpLibrary.csproj",
    "MultiPaxos\MultiPaxos.PSharpLibrary\MultiPaxos.PSharpLibrary.csproj",
    "PingPong\PingPong.PSharpLibrary\PingPong.PSharpLibrary.csproj",
    "PingPong\PingPong.PSharpLibrary.AsyncAwait\PingPong.PSharpLibrary.AsyncAwait.csproj",
    "Raft\Raft.PSharpLibrary\Raft.PSharpLibrary.csproj",
    "ReplicatingStorage\ReplicatingStorage.PSharpLibrary\ReplicatingStorage.PSharpLibrary.csproj",
    "TwoPhaseCommit\TwoPhaseCommit.PSharpLibrary\TwoPhaseCommit.PSharpLibrary.csproj",
    "Timers\TimerSample\TimerSample.csproj"

Write-Comment -prefix "." -text "Building P# samples" -color "yellow"
Write-Comment -prefix "..." -text "Configuration: $configuration" -color "white"
foreach ($project in $projects) {
    $command = "build -c $configuration $samples_dir\$project"
    $error_msg = "Failed to build '$samples_dir\$project'"
    Invoke-ToolCommand -tool $dotnet -command $command -error_msg $error_msg
}

Write-Comment -prefix "." -text "Successfully built P# samples" -color "green"
