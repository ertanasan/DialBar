# DialBar

A small, always-on-top bar of circular gauges that shows your PC's health at a glance: **CPU, heat, memory, disk, network and GPU**. No window to open, no graphs to read: just six dials that turn green, amber or red.

![DialBar horizontal bar](docs/images/bar-horizontal.png)

## Features

- **Six live dials**: CPU load, hottest temperature, memory, disk activity, network throughput and GPU load. Refreshes every second.
- **Color-coded**: green below 70 %, amber from 70 %, red from 90 %.
- **Hover for details**: per-source temperatures, memory in GB, read/write speeds, adapter peak and more.

  ![Heat tooltip](docs/images/tooltip.png)

- **Click a dial to focus it**: one click leaves only that dial, the next click brings them all back.

  ![Single dial](docs/images/bar-single.png)

- **Horizontal or vertical** layout, switchable from the tray menu.

  ![Vertical bar](docs/images/bar-vertical.png)

- **Drag anywhere** to move it. The position is remembered, and ignored if that spot is no longer on a screen.
- **Tray icon**: show or hide the bar, toggle individual gauges, always-on-top, layout, About, Exit.
- **Private**: no network access, no telemetry. Everything stays on your machine.

## Requirements

- Windows 10 or 11 (x64)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) to build from source
- **Administrator rights**: DialBar asks for them at launch (UAC) so it can read temperature sensors
- For **CPU temperature**: the [PawnIO](https://pawnio.eu) driver. Without it the Heat dial still works from the sensors that don't need it (typically disk and GPU), and the tooltip tells you what is missing.

## Build and run

```powershell
git clone <this repository>
cd DialBar
dotnet build
```

DialBar requests administrator rights, so start it from an **elevated** PowerShell, or run the built exe and accept the UAC prompt:

```powershell
src\DialBar.App\bin\Debug\net8.0-windows\DialBar.App.exe
```

> **Behind a corporate proxy or private NuGet feed?** If restore fails, make sure nuget.org is enabled (`dotnet nuget enable source nuget.org`). The repo's `nuget.config` uses nuget.org only.

## Using it

| Action | How |
| --- | --- |
| Move the bar | Drag it |
| Focus one gauge / show all | Click a dial / click again |
| Show or hide the bar | Left-click the tray icon |
| Hide a single gauge | Tray menu, untick the gauge |
| Vertical layout | Tray menu, **Vertical layout** |
| Always on top | Tray menu, **Always on top** |
| Hide to tray or exit | Right-click the bar |

Settings are saved to `%APPDATA%\DialBar\settings.json`. Delete the file to reset everything.

## How temperatures work

| Source | Needs |
| --- | --- |
| GPU, disk (SMART / NVMe) | Administrator rights |
| CPU | Administrator rights **and** the PawnIO driver |

The Heat dial shows the **hottest** of the sources it can read, and the tooltip lists each one.

### Optional: sensor helper service

`src/DialBar.Helper` is a small Windows service that reads the same sensors as SYSTEM and shares them with the app over a local-only named pipe (network access is denied). It is intended for running the UI **without** elevation; the app reads from it automatically when it is installed.

```powershell
# elevated PowerShell
.\scripts\install-helper.ps1             # install and start
.\scripts\install-helper.ps1 -Uninstall  # remove
```

## Project layout

```
src/
  DialBar.Core/                 gauge models, sampler, settings (no OS code)
  DialBar.Providers.Windows/    CPU, memory, disk, network, GPU, heat providers
  DialBar.Helper/               optional elevated sensor service
  DialBar.App/                  Avalonia UI: floating bar, dials, tray, About
scripts/                        helper install script
docs/images/                    screenshots
REQUIREMENTS.md                 full requirements and roadmap
```

The metric code sits behind an `IMetricProvider` interface and the UI is [Avalonia](https://avaloniaui.net), so Linux and macOS providers can be added without touching the UI.

## Roadmap

- Settings window (refresh interval, thresholds, size, opacity, °C / °F)
- Global show/hide hotkey, start with Windows, click-through mode
- MSI installer
- Linux and macOS support

See [REQUIREMENTS.md](REQUIREMENTS.md) for the full list.

## Credits and licenses

- [Avalonia UI](https://avaloniaui.net) (MIT)
- [LibreHardwareMonitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor) (MPL-2.0) for temperature sensors

## Author

© 2026 Ertan Asan ([@ertanasan](https://github.com/ertanasan))
