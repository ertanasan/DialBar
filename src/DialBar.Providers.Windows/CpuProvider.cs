using System.Diagnostics;
using DialBar.Core;

namespace DialBar.Providers.Windows;

public sealed class CpuProvider : IMetricProvider
{
    private readonly PerformanceCounter _total;
    private readonly PerformanceCounter? _frequency;

    public CpuProvider()
    {
        // "Processor Utility" matches Task Manager (can exceed 100 with boost, so it is clamped).
        _total = new PerformanceCounter("Processor Information", "% Processor Utility", "_Total", readOnly: true);
        try { _frequency = new PerformanceCounter("Processor Information", "Processor Frequency", "_Total", readOnly: true); }
        catch { /* optional */ }
        _total.NextValue();
    }

    public MetricKind Kind => MetricKind.Cpu;

    public MetricReading Read()
    {
        var percent = Math.Clamp(_total.NextValue(), 0, 100);
        var detail = $"{Environment.ProcessorCount} logical cores";
        if (_frequency is not null)
            detail += $", {_frequency.NextValue() / 1000:0.0#} GHz";
        return new MetricReading(Kind, percent / 100, $"{percent:0}%", detail);
    }

    public void Dispose()
    {
        _total.Dispose();
        _frequency?.Dispose();
    }
}
