param(
    [Parameter(Mandatory=$true)]
    [string]$account_name="",
    [Parameter(Mandatory=$true)]
    [string]$account_key="",
    [Parameter(Mandatory=$true)]
    [string]$share_name=""
)

Import-Module $PSScriptRoot\powershell\common.psm1

$package_dir = "$PSScriptRoot\..\bin\nuget"

Write-Comment -prefix "." -text "Uploading the P# NuGet packages to Azure Storage" -color "yellow"

$packages = Get-ChildItem "$package_dir"
foreach ($p in $packages) {
    Write-Host "$p"
    $command = "az storage file upload --account-name $account_name --account-key $account_key --share-name $share_name --source $package_dir\$p"
    $error_msg = "Failed to upload the P# NuGet packages to Azure Storage"
    Invoke-ToolCommand -tool $nuget -command $command -error_msg $error_msg
}

Write-Comment -prefix "." -text "Successfully uploaded the P# NuGet packages to Azure Storage" -color "green"
