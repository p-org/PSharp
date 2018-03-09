param(
    [string]$dotnet="dotnet",
    [string]$msbuild="msbuild",
    [ValidateSet("net46")]
    [string]$framework="net46",
    [ValidateSet("Debug","Release")]
    [string]$configuration="Release",
    [bool]$restore=$true,
    [bool]$build=$true,
    [bool]$publish=$true,
    [switch]$show
)

# Restores the packages for the specified sample.
function Restore-SamplePackages([String]$sample)
{
    Write-Comment -prefix "....." -text "Restoring packages for '$samples_dir\$sample'" -color "white"
    $command = "restore $samples_dir\$sample"
    $error_message = "Failed to restore packages for '$samples_dir\$sample'"
    Invoke-ToolCommand -tool $dotnet -command $command -error_message $error_message -show_output $show.IsPresent
}

# # Builds the specified sample using the specified configuration.
# function New-Sample([String]$sample, [String]$configuration)
# {
#     Write-Comment -prefix "..." -text "Building '$samples_dir\$sample' using the '$configuration' configuration" -color "white"
#     $command = "build $samples_dir\$sample -f $framework -c $configuration"
#     $error_message = "Failed to build '$samples_dir\$sample'"
#     Invoke-ToolCommand -tool $dotnet -command $command -error_message $error_message -show_output $show.IsPresent
# }

# Builds the specified sample using the specified configuration.
function New-Sample([String]$sample, [String]$configuration)
{
    Write-Comment -prefix "..." -text "Building '$samples_dir\$sample' using the '$configuration' configuration" -color "white"
    $command = "$samples_dir\$sample /p:Configuration=$configuration"
    $error_message = "Failed to build '$samples_dir\$sample'"
    Invoke-ToolCommand -tool $msbuild -command $command -error_message $error_message -show_output $show.IsPresent
}

# Paths to the various directories.
$psharp_binaries = $PSScriptRoot + '\..\bin\' + $framework
$samples_dir = $PSScriptRoot

# Available samples.
$samples = "BoundedAsync\BoundedAsync.PSharpLanguage\BoundedAsync.PSharpLanguage.csproj",
    "BoundedAsync\BoundedAsync.PSharpLibrary\BoundedAsync.PSharpLibrary.csproj",
    "CacheCoherence\CacheCoherence.PSharpLanguage\CacheCoherence.PSharpLanguage.csproj",
    "CacheCoherence\CacheCoherence.PSharpLibrary\CacheCoherence.PSharpLibrary.csproj",
    "ChainReplication\ChainReplication.PSharpLibrary\ChainReplication.PSharpLibrary.csproj",
    "Chord\Chord.PSharpLibrary\Chord.PSharpLibrary.csproj",
    "FailureDetector\FailureDetector.PSharpLanguage\FailureDetector.PSharpLanguage.csproj",
    "FailureDetector\FailureDetector.PSharpLibrary\FailureDetector.PSharpLibrary.csproj",
    "MultiPaxos\MultiPaxos.PSharpLanguage\MultiPaxos.PSharpLanguage.csproj",
    "MultiPaxos\MultiPaxos.PSharpLibrary\MultiPaxos.PSharpLibrary.csproj",
    "PingPong\PingPong.CustomLogging\PingPong.CustomLogging.csproj",
    "PingPong\PingPong.MixedMode\PingPong.MixedMode.csproj",
    "PingPong\PingPong.PSharpLanguage\PingPong.PSharpLanguage.csproj",
    "PingPong\PingPong.PSharpLanguage.AsyncAwait\PingPong.PSharpLanguage.AsyncAwait.csproj",
    "PingPong\PingPong.PSharpLibrary\PingPong.PSharpLibrary.csproj",
    "PingPong\PingPong.PSharpLibrary.AsyncAwait\PingPong.PSharpLibrary.AsyncAwait.csproj",
    "Raft\Raft.PSharpLanguage\Raft.PSharpLanguage.csproj",
    "Raft\Raft.PSharpLibrary\Raft.PSharpLibrary.csproj",
    "ReplicatingStorage\ReplicatingStorage.PSharpLanguage\ReplicatingStorage.PSharpLanguage.csproj",
    "ReplicatingStorage\ReplicatingStorage.PSharpLibrary\ReplicatingStorage.PSharpLibrary.csproj",
    "TwoPhaseCommit\TwoPhaseCommit.PSharpLibrary\TwoPhaseCommit.PSharpLibrary.csproj",
    "Timers\MultiTimer\MultiTimer\MultiTimer.csproj",
    "Timers\SingleTimerModel\SingleTimerModel\SingleTimerModel.csproj",
    "Timers\SingleTimerProduction\SingleTimerProduction\SingleTimerProduction.csproj"

Import-Module $PSScriptRoot\..\Scripts\common.psm1

Write-Comment -prefix "." -text "Building P# samples using the '$framework' framework" -color "yellow"

# Checks if P# is built for the specified framework.
Write-Comment -prefix "..." -text "Checking if P# is built using the '$framework' framework" -color "white"
if (-not (Test-Path $psharp_binaries))
{
    Write-Error "P# is not built using the '$framework' framework. Please build and try again."
    exit
}

if ($restore -eq $true)
{
    Write-Comment -prefix "..." -text "Restoring packages (might take several minutes)" -color "white"
    foreach ($project in $samples)
    {
        # Restores the packages for the sample.
        Restore-SamplePackages -sample $project
    }
}

if ($build -eq $true)
{
    foreach ($project in $samples)
    {
        # Builds the sample for the specified configuration.
        New-Sample -sample $project -configuration $configuration
    }
}

Write-Comment -prefix "." -text "Successfully built all samples" -color "green"
