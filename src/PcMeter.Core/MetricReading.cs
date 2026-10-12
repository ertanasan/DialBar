namespace PcMeter.Core;

public enum MetricKind
{
    Cpu,
    Heat,
    Memory,
    Disk,
    Network,
    Gpu,
}

/// <summary>
/// One sampled value for a gauge. <see cref="Fraction"/> drives the dial arc (0..1);
/// null means the sensor is unavailable and the dial shows N/A.
/// </summary>
public sealed record MetricReading(
    MetricKind Kind,
    double? Fraction,
    string Text,
    string Detail)
{
    public static MetricReading NotAvailable(MetricKind kind, string detail = "Sensor not available") =>
        new(kind, null, "N/A", detail);
}
