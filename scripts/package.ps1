<#
.SYNOPSIS
  Builds the DialBar setup: publishes the app and the sensor helper (self-contained, win-x64) and compiles
  installer\DialBar.iss with Inno Setup into dist\DialBar-Setup-<version>.exe.
  Needs Inno Setup 6.3+ (winget install JRSoftware.InnoSetup).
.EXAMPLE
  .\package.ps1
  .\package.ps1 -Version 0.2.0
#>
param([string]$Version = '0.1.0')

$ErrorActionPreference = 'Stop'

$scriptDir = if ($PSScriptRoot) { $PSScriptRoot } elseif ($MyInvocation.MyCommand.Path) { Split-Path $MyInvocation.MyCommand.Path } else { $null }
if (-not $scriptDir) { throw 'Cannot locate the script folder. Run it from a file, not pasted into the console.' }
$root = Resolve-Path (Join-Path $scriptDir '..')
$publish = Join-Path $root 'publish'

$iscc = @(
    (Get-Command ISCC.exe -ErrorAction SilentlyContinue).Source
    (Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 6\ISCC.exe')
    (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe')
    (Join-Path $env:ProgramFiles 'Inno Setup 6\ISCC.exe')
) | Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1
if (-not $iscc) { throw 'Inno Setup not found. Install it with: winget install JRSoftware.InnoSetup' }

if (Test-Path $publish) { Remove-Item $publish -Recurse -Force }

foreach ($name in 'App', 'Helper') {
    Write-Host "Publishing DialBar.$name..." -ForegroundColor Cyan
    dotnet publish (Join-Path $root "src\DialBar.$name\DialBar.$name.csproj") -c Release -r win-x64 --self-contained `
        -p:Version=$Version -o (Join-Path $publish $name) --nologo -v q
    if ($LASTEXITCODE -ne 0) { throw "Publish failed: DialBar.$name" }
}

Write-Host 'Compiling setup...' -ForegroundColor Cyan
& $iscc "/DAppVersion=$Version" /Q (Join-Path $root 'installer\DialBar.iss')
if ($LASTEXITCODE -ne 0) { throw 'Inno Setup compile failed' }

Write-Host "Setup ready: $(Join-Path $root "dist\DialBar-Setup-$Version.exe")" -ForegroundColor Green
