using DialBar.Core;

namespace DialBar.Providers.Windows;

/// <summary>
/// CPU and GPU fan speeds (RPM). Reads locally (GPU fans work unelevated) and fills the gaps from the elevated
/// helper service, which can see the motherboard's CPU fan header.
/// </summary>
public sealed class FanProvider : IMetricProvider
{
    // The dial arc is RPM / ScaleMax.
    private const double ScaleMax = 3000;

    private readonly TemperatureReader _local = new();
    private readonly HelperClient _helper = new();

    public MetricKind Kind => MetricKind.Fan;

    public MetricReading Read()
    {
        var local = _local.Read();
        var remote = _helper.Latest;

        var cpu = local?.CpuFan ?? remote?.CpuFan;
        var gpu = local?.GpuFan ?? remote?.GpuFan;

        var detail = $"CPU fan {Show(cpu)}\nGPU fan {Show(gpu)}";

        var speeds = new[] { cpu, gpu }.Where(r => r.HasValue).Select(r => r!.Value).ToList();
        if (speeds.Count == 0)
            return MetricReading.NotAvailable(Kind, detail);

        var fastest = speeds.Max();
        return new MetricReading(Kind, Math.Clamp(fastest / ScaleMax, 0, 1), $"{fastest:0}", detail);
    }

    private static string Show(double? rpm) => rpm is { } r ? $"{r:0} RPM" : "n/a";

    public void Dispose()
    {
        _helper.Dispose();
        _local.Dispose();
    }
}
