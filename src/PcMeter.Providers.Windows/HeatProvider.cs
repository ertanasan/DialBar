using PcMeter.Core;

namespace PcMeter.Providers.Windows;

/// <summary>
/// Hottest of CPU, GPU and disk temperature. Reads locally (GPU and some disks work unelevated) and fills the gaps
/// from the elevated helper service, which is what makes CPU temperature available to a normal-user app.
/// </summary>
public sealed class HeatProvider : IMetricProvider
{
    // The dial arc is temperature / ScaleMax, so the default 70 % / 90 % color thresholds land at 70 °C / 90 °C.
    private const double ScaleMax = 100;

    // LibreHardwareMonitor reads Intel/AMD CPU sensors through the PawnIO driver, which is installed separately.
    private static readonly bool PawnIoInstalled =
        Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\PawnIO") is not null;

    private readonly TemperatureReader _local = new();
    private readonly HelperClient _helper = new();

    public MetricKind Kind => MetricKind.Heat;

    public MetricReading Read()
    {
        var local = _local.Read();
        var remote = _helper.Latest;

        var cpu = local?.Cpu ?? remote?.Cpu;
        var gpu = local?.Gpu ?? remote?.Gpu;
        var disk = local?.Disk ?? remote?.Disk;

        var missingCpu = PawnIoInstalled ? "n/a" : "needs PawnIO driver";
        var detail = $"CPU {Show(cpu, missingCpu)}\nGPU {Show(gpu, "no sensor on this GPU")}\nDisk {Show(disk, "n/a")}";

        var temps = new[] { cpu, gpu, disk }.Where(t => t.HasValue).Select(t => t!.Value).ToList();
        if (temps.Count == 0)
            return MetricReading.NotAvailable(Kind, detail);

        var hottest = temps.Max();
        return new MetricReading(Kind, Math.Clamp(hottest / ScaleMax, 0, 1), $"{hottest:0}°C", detail);
    }

    private static string Show(double? temp, string missing) => temp is { } t ? $"{t:0}°C" : missing;

    public void Dispose()
    {
        _helper.Dispose();
        _local.Dispose();
    }
}
