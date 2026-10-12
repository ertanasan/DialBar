using System.Runtime.InteropServices;
using PcMeter.Core;

namespace PcMeter.Providers.Windows;

public sealed class MemoryProvider : IMetricProvider
{
    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryStatusEx
    {
        public uint Length;
        public uint MemoryLoad;
        public ulong TotalPhys;
        public ulong AvailPhys;
        public ulong TotalPageFile;
        public ulong AvailPageFile;
        public ulong TotalVirtual;
        public ulong AvailVirtual;
        public ulong AvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx status);

    public MetricKind Kind => MetricKind.Memory;

    public MetricReading Read()
    {
        var status = new MemoryStatusEx { Length = (uint)Marshal.SizeOf<MemoryStatusEx>() };
        if (!GlobalMemoryStatusEx(ref status))
            return MetricReading.NotAvailable(Kind);

        const double gb = 1024.0 * 1024 * 1024;
        var used = status.TotalPhys - status.AvailPhys;
        var detail = $"{used / gb:0.0} / {status.TotalPhys / gb:0.0} GB";
        return new MetricReading(Kind, status.MemoryLoad / 100.0, $"{status.MemoryLoad}%", detail);
    }

    public void Dispose() { }
}
