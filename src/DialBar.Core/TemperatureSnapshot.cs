namespace DialBar.Core;

/// <summary>Hottest temperature (°C) per hardware group; null where no sensor reported.</summary>
public sealed record TemperatureSnapshot(double? Cpu, double? Gpu, double? Disk);

/// <summary>Contract between the elevated sensor helper service and the app: one JSON snapshot per line over a named pipe.</summary>
public static class HelperPipe
{
    public const string Name = "DialBar.Sensors";
    public const string ServiceName = "DialBarSensors";
}
