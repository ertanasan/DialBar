namespace PcMeter.Core;

public interface IMetricProvider : IDisposable
{
    MetricKind Kind { get; }

    /// <summary>Samples the metric. Must not throw; return <see cref="MetricReading.NotAvailable"/> instead.</summary>
    MetricReading Read();
}
