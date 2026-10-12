using System.Diagnostics;
using DialBar.Core;

namespace DialBar.Providers.Windows;

public sealed class DiskProvider : IMetricProvider
{
    private readonly PerformanceCounter _active = new("PhysicalDisk", "% Disk Time", "_Total", readOnly: true);
    private readonly PerformanceCounter _read = new("PhysicalDisk", "Disk Read Bytes/sec", "_Total", readOnly: true);
    private readonly PerformanceCounter _write = new("PhysicalDisk", "Disk Write Bytes/sec", "_Total", readOnly: true);

    public DiskProvider()
    {
        _active.NextValue();
        _read.NextValue();
        _write.NextValue();
    }

    public MetricKind Kind => MetricKind.Disk;

    public MetricReading Read()
    {
        var percent = Math.Clamp(_active.NextValue(), 0, 100);
        var detail = $"Read {Format.Rate(_read.NextValue())}, Write {Format.Rate(_write.NextValue())}";
        return new MetricReading(Kind, percent / 100, $"{percent:0}%", detail);
    }

    public void Dispose()
    {
        _active.Dispose();
        _read.Dispose();
        _write.Dispose();
    }
}
