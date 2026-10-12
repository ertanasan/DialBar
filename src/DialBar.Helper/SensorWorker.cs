using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.Json;
using DialBar.Core;
using DialBar.Providers.Windows;

namespace DialBar.Helper;

/// <summary>
/// Runs elevated, samples temperatures once a second and streams the latest snapshot to every connected
/// app instance over a local-only named pipe.
/// </summary>
public sealed class SensorWorker(ILogger<SensorWorker> logger) : BackgroundService
{
    private readonly TemperatureReader _reader = new();
    private volatile TemperatureSnapshot _latest = new(null, null, null);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var sampling = Task.Run(() => SampleAsync(stoppingToken), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var pipe = CreatePipe();
            try
            {
                await pipe.WaitForConnectionAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                pipe.Dispose();
                break;
            }
            _ = ServeAsync(pipe, stoppingToken);
        }

        await sampling;
    }

    private async Task SampleAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        do
        {
            try { _latest = _reader.Read() ?? _latest; }
            catch (Exception ex) { logger.LogWarning(ex, "Temperature sampling failed"); }
        }
        while (await WaitAsync(timer, ct));
    }

    private static async Task<bool> WaitAsync(PeriodicTimer timer, CancellationToken ct)
    {
        try { return await timer.WaitForNextTickAsync(ct); }
        catch (OperationCanceledException) { return false; }
    }

    private async Task ServeAsync(NamedPipeServerStream pipe, CancellationToken ct)
    {
        await using (pipe)
        {
            try
            {
                await using var writer = new StreamWriter(pipe) { AutoFlush = true };
                while (!ct.IsCancellationRequested)
                {
                    await writer.WriteLineAsync(JsonSerializer.Serialize(_latest));
                    await Task.Delay(1000, ct);
                }
            }
            catch (Exception ex) when (ex is IOException or OperationCanceledException)
            {
                // Client went away or the service is stopping.
            }
        }
    }

    private static NamedPipeServerStream CreatePipe()
    {
        // Any signed-in local user may read; remote (network) access is explicitly denied.
        var security = new PipeSecurity();
        security.AddAccessRule(new PipeAccessRule(
            new SecurityIdentifier(WellKnownSidType.NetworkSid, null), PipeAccessRights.FullControl, AccessControlType.Deny));
        security.AddAccessRule(new PipeAccessRule(
            new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null), PipeAccessRights.ReadWrite, AccessControlType.Allow));
        // The creator needs CreateNewInstance to open further pipe instances for additional clients.
        security.AddAccessRule(new PipeAccessRule(
            WindowsIdentity.GetCurrent().User!, PipeAccessRights.FullControl, AccessControlType.Allow));
        security.AddAccessRule(new PipeAccessRule(
            new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null), PipeAccessRights.FullControl, AccessControlType.Allow));
        security.AddAccessRule(new PipeAccessRule(
            new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null), PipeAccessRights.FullControl, AccessControlType.Allow));

        return NamedPipeServerStreamAcl.Create(
            HelperPipe.Name, PipeDirection.Out, NamedPipeServerStream.MaxAllowedServerInstances,
            PipeTransmissionMode.Byte, PipeOptions.Asynchronous, 0, 0, security);
    }

    public override void Dispose()
    {
        _reader.Dispose();
        base.Dispose();
    }
}
