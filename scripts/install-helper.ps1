<#
.SYNOPSIS
  Installs (or removes) the DialBar sensor helper as a Windows service. Run from an elevated PowerShell.
  Dev convenience only; the MSI installer replaces this in the packaging milestone.
.EXAMPLE
  .\install-helper.ps1            # publish, install and start
  .\install-helper.ps1 -Uninstall # stop and remove
#>
param([switch]$Uninstall)

$ErrorActionPreference = 'Stop'
$service = 'DialBarSensors'
$target = Join-Path $env:ProgramFiles 'DialBar\Helper'

$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) { throw 'Run this script from an elevated (Administrator) PowerShell.' }

# Remove the helper installed under the app's previous name (PcMeter), if it is still there.
$legacyService = 'PcMeterSensors'
$legacyTarget = Join-Path $env:ProgramFiles 'PcMeter\Helper'
if (Get-Service $legacyService -ErrorAction SilentlyContinue) {
    if ((Get-Service $legacyService).Status -ne 'Stopped') { Stop-Service $legacyService -Force }
    & "$env:SystemRoot\System32\sc.exe" delete $legacyService | Out-Null
    Start-Sleep 1
}
if (Test-Path $legacyTarget) { Remove-Item (Split-Path $legacyTarget) -Recurse -Force }

if (Get-Service $service -ErrorAction SilentlyContinue) {
    if ((Get-Service $service).Status -ne 'Stopped') { Stop-Service $service }
    & "$env:SystemRoot\System32\sc.exe" delete $service | Out-Null
    Start-Sleep 1
}

if ($Uninstall) {
    if (Test-Path $target) { Remove-Item $target -Recurse -Force }
    'DialBar sensor helper removed.'
    return
}

$scriptDir = if ($PSScriptRoot) { $PSScriptRoot } elseif ($MyInvocation.MyCommand.Path) { Split-Path $MyInvocation.MyCommand.Path } else { $null }
if (-not $scriptDir) { throw 'Cannot locate the script folder. Run it from a file (.\scripts\install-helper.ps1), not pasted into the console.' }
$project = Join-Path $scriptDir '..\src\DialBar.Helper\DialBar.Helper.csproj'
dotnet publish $project -c Release -r win-x64 --self-contained -o $target
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed' }

$exe = Join-Path $target 'DialBar.Helper.exe'
New-Service -Name $service -BinaryPathName "`"$exe`"" -DisplayName 'DialBar Sensor Helper' `
    -Description 'Reads CPU and disk temperatures for DialBar and shares them with the app over a local pipe.' `
    -StartupType Automatic | Out-Null
Start-Service $service
"DialBar sensor helper installed and running from $target"
