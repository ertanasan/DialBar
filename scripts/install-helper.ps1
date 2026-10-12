<#
.SYNOPSIS
  Installs (or removes) the PcMeter sensor helper as a Windows service. Run from an elevated PowerShell.
  Dev convenience only; the MSI installer replaces this in the packaging milestone.
.EXAMPLE
  .\install-helper.ps1            # publish, install and start
  .\install-helper.ps1 -Uninstall # stop and remove
#>
param([switch]$Uninstall)

$ErrorActionPreference = 'Stop'
$service = 'PcMeterSensors'
$target = Join-Path $env:ProgramFiles 'PcMeter\Helper'

$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) { throw 'Run this script from an elevated (Administrator) PowerShell.' }

if (Get-Service $service -ErrorAction SilentlyContinue) {
    if ((Get-Service $service).Status -ne 'Stopped') { Stop-Service $service }
    & "$env:SystemRoot\System32\sc.exe" delete $service | Out-Null
    Start-Sleep 1
}

if ($Uninstall) {
    if (Test-Path $target) { Remove-Item $target -Recurse -Force }
    'PcMeter sensor helper removed.'
    return
}

$project = Join-Path $PSScriptRoot '..\src\PcMeter.Helper\PcMeter.Helper.csproj'
dotnet publish $project -c Release -r win-x64 --self-contained -o $target
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed' }

$exe = Join-Path $target 'PcMeter.Helper.exe'
New-Service -Name $service -BinaryPathName "`"$exe`"" -DisplayName 'PcMeter Sensor Helper' `
    -Description 'Reads CPU and disk temperatures for PcMeter and shares them with the app over a local pipe.' `
    -StartupType Automatic | Out-Null
Start-Service $service
"PcMeter sensor helper installed and running from $target"
