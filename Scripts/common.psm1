# Runs the specified tool command.
function Invoke-ToolCommand([String]$tool, [String]$command, [String]$error_message, [bool]$show_output)
{
    $output = $null
    $command = "$tool $command"
    if ($show_output -eq $true)
    {
        Invoke-Expression $command
    }
    else
    {
        $output = Invoke-Expression "$command 2>&1"
    }

    if (-not ($LASTEXITCODE -eq 0))
    {
        if (-not (($output -eq $null) -and ($output.Length -eq 0)))
        {
            Write-ErrorOutput -text $output -color "white"
        }
        
        Write-Error $error_message
        exit
    }
}

function Write-Comment([String]$prefix, [String]$text, [String]$color)
{
    Write-Host "$prefix " -b "black" -nonewline; Write-Host $text -b "black" -f $color
}

function Write-Error([String]$text)
{
    Write-Host "Error: $text" -b "black" -f "red"
}

function Write-ErrorOutput([String]$text, [String]$color)
{
    Write-Host "===== Error Output Start =====" -b "black" -f "red"
    Write-Host $text -b "black" -f $color
    Write-Host "====== Error Output End ======" -b "black" -f "red"
}
