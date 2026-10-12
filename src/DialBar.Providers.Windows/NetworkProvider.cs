using System.Diagnostics;
using DialBar.Core;

namespace DialBar.Providers.Windows;

public sealed class NetworkProvider : IMetricProvider
{
    private const string Category = "Network Interface";

    // The arc is relative to the busiest rate seen so far, with a floor so idle noise doesn't fill the dial.
    private const double MinScale = 1024 * 1024;

    private readonly Dictionary<string, (PerformanceCounter Recv, PerformanceCounter Sent)> _counters = new();
    private double _peak = MinScale;

    public MetricKind Kind => MetricKind.Network;

    public MetricReading Read()
    {
        RefreshAdapters();

        double down = 0, up = 0;
        foreach (var (recv, sent) in _counters.Values)
        {
            down += recv.NextValue();
            up += sent.NextValue();
        }

        var total = down + up;
        _peak = Math.Max(_peak, total);
        var detail = $"Down {Format.Rate(down)}, Up {Format.Rate(up)}\nPeak {Format.Rate(_peak)}";
        return new MetricReading(Kind, total / _peak, Format.Rate(total), detail);
    }

    private void RefreshAdapters()
    {
        var names = new PerformanceCounterCategory(Category).GetInstanceNames();

        foreach (var gone in _counters.Keys.Except(names).ToList())
        {
            _counters[gone].Recv.Dispose();
            _counters[gone].Sent.Dispose();
            _counters.Remove(gone);
        }

        foreach (var name in names.Except(_counters.Keys))
        {
            if (name.Contains("Loopback", StringComparison.OrdinalIgnoreCase)) continue;
            var recv = new PerformanceCounter(Category, "Bytes Received/sec", name, readOnly: true);
            var sent = new PerformanceCounter(Category, "Bytes Sent/sec", name, readOnly: true);
            recv.NextValue();
            sent.NextValue();
            _counters[name] = (recv, sent);
        }
    }

    public void Dispose()
    {
        foreach (var (recv, sent) in _counters.Values)
        {
            recv.Dispose();
            sent.Dispose();
        }
        _counters.Clear();
    }
}
