namespace PcMeter.Providers.Windows;

internal static class Format
{
    public static string Rate(double bytesPerSecond) => bytesPerSecond switch
    {
        >= 1024 * 1024 * 1024 => $"{bytesPerSecond / (1024 * 1024 * 1024):0.0} GB/s",
        >= 1024 * 1024 => $"{bytesPerSecond / (1024 * 1024):0.0} MB/s",
        >= 1024 => $"{bytesPerSecond / 1024:0} KB/s",
        _ => $"{bytesPerSecond:0} B/s",
    };
}
