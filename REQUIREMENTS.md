# PcMeter – Requirements

Status: Draft v0.2 (assumptions marked **[A]**, open questions in section 10)

## 1. Purpose
PcMeter is a lightweight, cross-platform desktop utility that shows live system health as a single floating bar of circular gauges, so the user can spot load, heat, and bottlenecks without opening a system monitor.

## 2. Scope
In scope: one floating bar with circular dial gauges for CPU, heat, memory, disk, network, and GPU.
Out of scope (v1): process-count gauge (dropped), historical logging/export, remote monitoring, alerts via email/SMS.

## 3. Target Platforms
- Windows 10/11, Linux (X11/Wayland best effort), macOS 12+. **[A]**
- Windows ships first; Linux and macOS follow after the Windows release.
- Runs as a normal user; elevated access only where a sensor requires it (see 10).

## 4. Gauges (Functional Requirements)

| ID | Gauge | Primary value | Secondary info (hover) |
|----|-------|---------------|------------------------|
| G1 | CPU | Total utilization % | Per-core %, clock speed, top 3 processes by CPU |
| G2 | Heat | Hottest of CPU/GPU/Disk temp, °C | Separate CPU, GPU, and disk temps; min/max since start |
| G3 | Memory | Used % | Used/total GB, swap/commit, top consumer |
| G4 | Disk | Active time % | Read/write MB/s, free space per drive |
| G5 | Network | Throughput (down/up) | Adapter name, peak, total transferred |
| G6 | GPU | GPU load % | GPU temp, VRAM used/total, clock, GPU name (multi-GPU: selectable, default busiest) |

Requirements for all gauges:
- FR-1: Each gauge updates at a configurable interval (default 1 s, range 0.5–10 s).
- FR-2: Each gauge is a circular dial with an arc fill, numeric value in the center, and a short label.
- FR-3: Arc color thresholds: green → amber → red, configurable per gauge. Defaults: CPU/Mem/Disk 70/90 %, Heat 70/85 °C.
- FR-4: Hover tooltip shows the secondary info above.
- FR-5: Network dial auto-scales (adaptive max, or link speed); unit toggle MB/s vs Mbps.
- FR-6: Heat gauge sub-source selectable: Hottest (default), CPU, GPU, Disk.
- FR-7: If a sensor is unavailable (no temperature, no GPU), show "N/A" and never crash.

## 5. Floating Bar Behavior
- FW-1: A single borderless, semi-transparent window contains all dials in a row (horizontal) or column (vertical).
- FW-2: Always-on-top (toggleable).
- FW-3: Drag to reposition; position persists across restarts and is validated against current monitors on launch.
- FW-4: Snap to screen edges; option to lock position.
- FW-5: Optional click-through mode, with a hotkey/tray toggle to exit it.
- FW-6: Adjustable opacity (20–100 %) and size (small/medium/large or scale slider).
- FW-7: Each gauge can be shown or hidden; the bar resizes to fit. Dial order is configurable.
- FW-8: Optionally hidden/dimmed when a full-screen app is active.
- FW-9: Multi-monitor and per-monitor DPI aware.

## 6. Tray & Control
- TR-1: Tray icon with menu: show/hide bar, toggle each gauge, settings, lock, click-through, exit.
- TR-2: Global hotkey to show/hide the bar (default Ctrl+Alt+M, configurable).
- TR-3: Optional start with OS login.

## 7. Settings & Persistence
- ST-1: Settings UI: update interval, thresholds, theme (light/dark/accent), opacity, size, orientation, units (°C/°F), heat source, startup.
- ST-2: JSON settings in the per-OS app-data folder (`%APPDATA%\PcMeter`, `~/.config/PcMeter`, `~/Library/Application Support/PcMeter`); corrupt/missing file falls back to defaults.
- ST-3: Reset-to-defaults option.

## 8. Non-Functional Requirements
- NFR-1 Performance: < 1 % average CPU and < 120 MB RAM at 1 s refresh.
- NFR-2 Responsiveness: sampling on a background thread; UI never blocks.
- NFR-3 Accuracy: CPU/memory/disk within ±2 % of the OS's native monitor.
- NFR-4 Reliability: 7+ days without memory growth; survives sleep/resume, display and adapter changes.
- NFR-5 Startup: bar visible within 2 s.
- NFR-6 Privacy: no network calls, no telemetry.
- NFR-7 Accessibility: contrast ≥ 4.5:1; status not conveyed by color alone (numbers always shown).
- NFR-8 Packaging: signed Windows installer (includes optional sensor-helper service) plus portable exe; Linux AppImage or .deb and macOS .app/dmg later; self-contained publish.
- NFR-9 Distribution: for public users: signed binaries, clear uninstall, helper service removed on uninstall, no data leaves the machine.
- NFR-10 Compatibility: works on NVIDIA, AMD, and Intel GPUs; absent or unsupported GPU shows "N/A".

## 9. Technical Approach **[A]**
- Language/runtime: C# on .NET 8.
- UI: Avalonia UI (cross-platform, transparent borderless windows, custom-drawn arcs). WPF is Windows-only, so it is not used.
- Architecture: `PcMeter.Core` (metric interfaces, models, settings) + per-OS metric providers behind `IMetricProvider`, + `PcMeter.App` (Avalonia UI). This keeps the UI identical across OSes.
- Metric sources:
  - Windows: Performance counters (PDH) for CPU/disk/network, `GlobalMemoryStatusEx` for memory, LibreHardwareMonitorLib for temperatures.
  - Linux: `/proc/stat`, `/proc/meminfo`, `/proc/diskstats`, `/proc/net/dev`, `/sys/class/hwmon` for temperatures.
  - macOS: `sysctl`/`host_statistics`; temperatures need SMC access and are best effort.
- Tray/hotkey/click-through need small per-OS shims (Avalonia `TrayIcon`; global hotkeys and click-through via platform-specific code).

## 10. Decisions
- Avalonia UI on .NET 8 is accepted.
- Windows ships first; Linux and macOS follow.
- A GPU dial is in v1 (see G6).
- The app is distributed to other users, so it needs a proper installer, code signing, and an update path.
- Temperature access (decided):
  - The UI always runs as a normal user (no UAC prompt at launch).
  - GPU temp/load use vendor APIs (NVML, ADL, Intel) and need no elevation.
  - CPU and disk temps (kernel driver / SMART, which need admin) are read by an optional helper Windows service installed by the installer. Without it, those readings show "N/A".
  - The helper exposes read-only sensor data over a local named pipe, restricted to the interactive user.

## 10a. Open Questions
1. Installer technology: MSIX (needs signing, simple updates) or MSI/Inno Setup (supports the service install more easily)?
2. Code-signing certificate: do you have one, or should the plan include getting one?
3. Licensing of the sensor library (LibreHardwareMonitor is MPL-2.0) is acceptable for distribution?
4. Auto-update needed in v1?

## 11. Milestones
1. M1 – Core: `IMetricProvider` plus Windows providers for CPU, memory, disk, network, GPU, verified against Task Manager.
2. M2 – Floating bar with CPU dial: drag, opacity, always-on-top.
3. M3 – All six dials, thresholds, tooltips, horizontal/vertical layout.
4. M4 – Tray, hotkey, settings UI, persistence.
5. M5 – Heat: GPU temp (no admin), then sensor helper service for CPU/disk temp.
6. M6 – Windows polish and release: DPI/multi-monitor, full-screen handling, signed installer, soak test.
7. M7 – Linux and macOS providers, per-OS shims, packaging.

## 12. Acceptance Criteria
- All six dials show live values matching the OS monitor within tolerance.
- The bar can be dragged, toggled, and restored to the same position after restart.
- Meets the NFR-1 resource budget over a 1-hour run.
- Missing sensors degrade to "N/A" without errors.
