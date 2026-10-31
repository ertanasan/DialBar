using System.Diagnostics;
using DialBar.Core;
using LibreHardwareMonitor.Hardware;

namespace DialBar.Providers.Windows;

/// <summary>
/// GPU load: the busier of the driver-reported core load (NVIDIA/AMD via LibreHardwareMonitor) and the Windows
/// "GPU Engine" counters. The counters work for every vendor without admin rights but miss CUDA/compute work,
/// which is why the driver reading is needed for things like AI image generation.
/// </summary>
public sealed class GpuProvider : IMetricProvider
{
    private const string Category = "GPU Engine";
    private const string Counter = "Utilization Percentage";

    private Dictionary<string, CounterSample> _previous = new();
    private Computer? _computer;
    private bool _openFailed;

    public MetricKind Kind => MetricKind.Gpu;

    public MetricReading Read()
    {
        var driver = ReadDriverLoad();
        var engine = ReadEngineCounters();

        if (driver is null && engine is null)
            return PerformanceCounterCategory.Exists(Category)
                ? new MetricReading(Kind, 0, "0%", "Warming up")
                : MetricReading.NotAvailable(Kind, "No GPU counters on this system");

        var (percent, detail) = engine is not { } e || driver is { } d && d.Percent >= e.Percent
            ? driver!.Value
            : e;
        percent = Math.Clamp(percent, 0, 100);
        return new MetricReading(Kind, percent / 100, $"{percent:0}%", detail);
    }

    /// <summary>Highest "GPU Core" load across discrete GPUs; null if the sensor library is unavailable.</summary>
    private (double Percent, string Detail)? ReadDriverLoad()
    {
        if (_computer is null && !_openFailed)
        {
            try
            {
                _computer = new Computer { IsGpuEnabled = true };
                _computer.Open();
            }
            catch
            {
                _openFailed = true;
                _computer = null;
            }
        }

        if (_computer is null) return null;

        (double Percent, string Detail)? best = null;
        foreach (var hardware in _computer.Hardware)
        {
            if (hardware.HardwareType is not (HardwareType.GpuNvidia or HardwareType.GpuAmd)) continue;
            hardware.Update();

            var core = hardware.Sensors.FirstOrDefault(s =>
                s.SensorType == SensorType.Load && s.Name == "GPU Core" && s.Value is not null);
            if (core?.Value is { } value && (best is null || value > best.Value.Percent))
                best = (value, $"{hardware.Name}: core load");
        }
        return best;
    }

    /// <summary>Busiest single engine, summed over processes; null until two samples exist.</summary>
    private (double Percent, string Detail)? ReadEngineCounters()
    {
        if (!PerformanceCounterCategory.Exists(Category)) return null;

        var data = new PerformanceCounterCategory(Category).ReadCategory();
        if (!data.Contains(Counter)) return null;

        var current = new Dictionary<string, CounterSample>();
        var perEngine = new Dictionary<string, double>();

        foreach (System.Collections.DictionaryEntry entry in data[Counter])
        {
            var instance = (string)entry.Key;
            var sample = ((InstanceData)entry.Value!).Sample;
            current[instance] = sample;

            if (!_previous.TryGetValue(instance, out var before)) continue;

            // Instance names look like "pid_123_luid_0x..._phys_0_eng_0_engtype_3D". Group by everything after the
            // pid so each physical engine on each adapter is summed over processes, as Task Manager does.
            var luid = instance.IndexOf("luid_", StringComparison.Ordinal);
            var engineKey = luid >= 0 ? instance[luid..] : instance;
            perEngine[engineKey] = perEngine.GetValueOrDefault(engineKey) + CounterSample.Calculate(before, sample);
        }

        _previous = current;

        if (perEngine.Count == 0) return null;

        var busiest = perEngine.MaxBy(kv => kv.Value);
        var type = busiest.Key.IndexOf("engtype_", StringComparison.Ordinal) is var t and >= 0 ? busiest.Key[(t + 8)..] : "?";
        return (busiest.Value, $"Busiest engine: {type}");
    }

    public void Dispose() => _computer?.Close();
}
