<#
.SYNOPSIS
  Builds the DialBar app and the sensor helper. Closes a running DialBar.App first so its files aren't locked.
.EXAMPLE
  .\build.ps1                  # Debug build of both
  .\build.ps1 -Configuration Release
#>
param([ValidateSet('Debug', 'Release')][string]$Configuration = 'Debug')

$ErrorActionPreference = 'Stop'

$scriptDir = if ($PSScriptRoot) { $PSScriptRoot } elseif ($MyInvocation.MyCommand.Path) { Split-Path $MyInvocation.MyCommand.Path } else { $null }
if (-not $scriptDir) { throw 'Cannot locate the script folder. Run it from a file, not pasted into the console.' }
$src = Join-Path $scriptDir '..\src'

try { Get-Process DialBar.App -ErrorAction SilentlyContinue | Stop-Process -Force }
catch { throw 'DialBar.App is running elevated and cannot be closed from here. Close it (or run this script from an elevated PowerShell) and retry.' }

foreach ($name in 'DialBar.App', 'DialBar.Helper') {
    Write-Host "Building $name ($Configuration)..." -ForegroundColor Cyan
    dotnet build (Join-Path $src "$name\$name.csproj") -c $Configuration --nologo -v q
    if ($LASTEXITCODE -ne 0) { throw "Build failed: $name" }
}

Write-Host 'Both projects built.' -ForegroundColor Green
