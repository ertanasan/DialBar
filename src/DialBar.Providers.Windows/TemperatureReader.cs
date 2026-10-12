using LibreHardwareMonitor.Hardware;
using DialBar.Core;

namespace DialBar.Providers.Windows;

/// <summary>
/// Reads CPU/GPU/disk temperatures through LibreHardwareMonitor. Used in-process by the app (GPU and some disks work
/// unelevated) and by the elevated helper service (CPU and the rest).
/// </summary>
public sealed class TemperatureReader : IDisposable
{
    private Computer? _computer;
    private bool _openFailed;

    /// <summary>Returns null if the sensor library could not start. Opens lazily because probing hardware is slow.</summary>
    public TemperatureSnapshot? Read()
    {
        if (_computer is null && !_openFailed)
        {
            try
            {
                _computer = new Computer { IsCpuEnabled = true, IsGpuEnabled = true, IsStorageEnabled = true };
                _computer.Open();
            }
            catch
            {
                _openFailed = true;
                _computer = null;
            }
        }

        if (_computer is null) return null;

        double? cpu = null, gpu = null, disk = null;
        foreach (var hardware in _computer.Hardware)
        {
            hardware.Update();
            var hottest = HottestTemperature(hardware);
            if (hottest is null) continue;

            switch (hardware.HardwareType)
            {
                case HardwareType.Cpu: cpu = Max(cpu, hottest); break;
                case HardwareType.GpuNvidia or HardwareType.GpuAmd or HardwareType.GpuIntel: gpu = Max(gpu, hottest); break;
                case HardwareType.Storage: disk = Max(disk, hottest); break;
            }
        }
        return new TemperatureSnapshot(cpu, gpu, disk);
    }

    private static double? HottestTemperature(IHardware hardware)
    {
        double? best = null;
        foreach (var sensor in hardware.Sensors)
        {
            // "Distance to TjMax" is reported as a temperature but is headroom, not heat.
            if (sensor.SensorType != SensorType.Temperature || sensor.Value is not { } value) continue;
            if (sensor.Name.Contains("Distance", StringComparison.OrdinalIgnoreCase)) continue;
            best = Max(best, value);
        }

        foreach (var sub in hardware.SubHardware)
        {
            sub.Update();
            best = Max(best, HottestTemperature(sub));
        }
        return best;
    }

    private static double? Max(double? a, double? b) =>
        a is null ? b : b is null ? a : Math.Max(a.Value, b.Value);

    public void Dispose() => _computer?.Close();
}
