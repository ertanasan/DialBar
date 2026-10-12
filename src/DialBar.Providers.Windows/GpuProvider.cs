using System.Diagnostics;
using DialBar.Core;

namespace DialBar.Providers.Windows;

/// <summary>
/// GPU load from the Windows "GPU Engine" counters, which work for NVIDIA, AMD and Intel without admin rights.
/// Instances are per process/engine, so they are re-read each pass and summed per engine type.
/// </summary>
public sealed class GpuProvider : IMetricProvider
{
    private const string Category = "GPU Engine";
    private const string Counter = "Utilization Percentage";

    private Dictionary<string, CounterSample> _previous = new();

    public MetricKind Kind => MetricKind.Gpu;

    public MetricReading Read()
    {
        if (!PerformanceCounterCategory.Exists(Category))
            return MetricReading.NotAvailable(Kind, "No GPU counters on this system");

        var data = new PerformanceCounterCategory(Category).ReadCategory();
        if (!data.Contains(Counter))
            return MetricReading.NotAvailable(Kind);

        var current = new Dictionary<string, CounterSample>();
        var perEngine = new Dictionary<string, double>();

        foreach (System.Collections.DictionaryEntry entry in data[Counter])
        {
            var instance = (string)entry.Key;
            var sample = ((InstanceData)entry.Value!).Sample;
            current[instance] = sample;

            if (!_previous.TryGetValue(instance, out var before)) continue;

            // Instance names look like "pid_123_luid_0x..._phys_0_eng_0_engtype_3D".
            var engineType = instance[(instance.LastIndexOf("engtype_", StringComparison.Ordinal) + 8)..];
            perEngine[engineType] = perEngine.GetValueOrDefault(engineType) + CounterSample.Calculate(before, sample);
        }

        _previous = current;

        if (perEngine.Count == 0)
            return new MetricReading(Kind, 0, "0%", "Warming up");

        var busiest = perEngine.MaxBy(kv => kv.Value);
        var percent = Math.Clamp(busiest.Value, 0, 100);
        return new MetricReading(Kind, percent / 100, $"{percent:0}%", $"Busiest engine: {busiest.Key}");
    }

    public void Dispose() { }
}
