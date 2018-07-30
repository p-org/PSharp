# Builds the specified .NET project using msbuild.
function New-MSBuildProject([String]$msbuild, [String]$project, [String]$configuration)
{
    Write-Comment -prefix "..." -text "Building '$project' ($configuration)" -color "white"
    $command = "$project /p:Configuration=$configuration"
    $error_msg = "Failed to build '$project'"
    Invoke-ToolCommand -tool $msbuild -command $command -error_msg $error_msg
}

# Builds the specified .NET project using dotnet.
function New-DotnetProject([String]$dotnet, [String]$project, [String]$configuration) {
    Write-Comment -prefix "..." -text "Building '$project' ($configuration)" -color "white"
    $command = "build $project -c $configuration"
    $error_msg = "Failed to build '$project'"
    Invoke-ToolCommand -tool $dotnet -command $command -error_msg $error_msg
}

# Restores the packages for the specified .NET project.
function Invoke-DotnetRestore([String]$dotnet, [String]$project)
{
    Write-Comment -prefix "..." -text "Restoring packages for '$project'" -color "white"
    $command = "restore $project"
    $error_msg = "Failed to restore packages for '$project'"
    Invoke-ToolCommand -tool $dotnet -command $command -error_msg $error_msg
}

# Runs the specified .NET test using the specified framework.
function Invoke-DotnetTest([String]$dotnet, [String]$project, [String]$target, [string]$framework) {
    Write-Comment -prefix "..." -text "Testing '$project' ($framework)" -color "white"
    if (-not (Test-Path $target)) {
        Write-Error "tests for '$project' ($framework) not found."
        exit
    }

    $command = "test $target -f $framework --no-build -v n"
    $error_msg = "Failed to test '$project'"
    Invoke-ToolCommand -tool $dotnet -command $command -error_msg $error_msg
}

# Runs the specified tool command.
function Invoke-ToolCommand([String]$tool, [String]$command, [String]$error_msg) {
    Invoke-Expression "$tool $command"
    if (-not ($LASTEXITCODE -eq 0)) {
        Write-Error $error_msg
        exit
    }
}

function Write-Comment([String]$prefix, [String]$text, [String]$color) {
    Write-Host "$prefix " -b "black" -nonewline; Write-Host $text -b "black" -f $color
}

function Write-Error([String]$text) {
    Write-Host "Error: $text" -b "black" -f "red"
}
