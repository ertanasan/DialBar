using PcMeter.Core;

namespace PcMeter.Providers.Windows;

public static class WindowsProviders
{
    /// <summary>Creates all providers; any that fail to initialise degrade to N/A instead of failing startup.</summary>
    public static IReadOnlyList<IMetricProvider> CreateAll() =>
    [
        Create(MetricKind.Cpu, () => new CpuProvider()),
        Create(MetricKind.Heat, () => new HeatProvider()),
        Create(MetricKind.Memory, () => new MemoryProvider()),
        Create(MetricKind.Disk, () => new DiskProvider()),
        Create(MetricKind.Network, () => new NetworkProvider()),
        Create(MetricKind.Gpu, () => new GpuProvider()),
    ];

    private static IMetricProvider Create(MetricKind kind, Func<IMetricProvider> factory)
    {
        try { return factory(); }
        catch (Exception ex) { return new UnavailableProvider(kind, ex.Message); }
    }

    private sealed class UnavailableProvider(MetricKind kind, string reason) : IMetricProvider
    {
        public MetricKind Kind => kind;
        public MetricReading Read() => MetricReading.NotAvailable(kind, reason);
        public void Dispose() { }
    }
}
