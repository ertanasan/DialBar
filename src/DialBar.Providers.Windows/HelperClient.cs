using System.IO.Pipes;
using System.Text.Json;
using DialBar.Core;

namespace DialBar.Providers.Windows;

/// <summary>Keeps a connection to the sensor helper service and exposes its latest snapshot. Silent if the helper isn't installed.</summary>
public sealed class HelperClient : IDisposable
{
    private static readonly TimeSpan MaxAge = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(3);

    private readonly CancellationTokenSource _cts = new();
    private readonly Task _loop;
    private (TemperatureSnapshot Snapshot, DateTime At)? _latest;

    public HelperClient() => _loop = Task.Run(RunAsync);

    /// <summary>The newest snapshot, or null if the helper is absent or has gone quiet.</summary>
    public TemperatureSnapshot? Latest =>
        _latest is { } l && DateTime.UtcNow - l.At < MaxAge ? l.Snapshot : null;

    private async Task RunAsync()
    {
        var ct = _cts.Token;
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await using var pipe = new NamedPipeClientStream(".", HelperPipe.Name, PipeDirection.In, PipeOptions.Asynchronous);
                await pipe.ConnectAsync(2000, ct);
                using var reader = new StreamReader(pipe);
                while (await reader.ReadLineAsync(ct) is { } line)
                {
                    var snapshot = JsonSerializer.Deserialize<TemperatureSnapshot>(line);
                    if (snapshot is not null) _latest = (snapshot, DateTime.UtcNow);
                }
            }
            catch (OperationCanceledException) { return; }
            catch (Exception ex) when (ex is IOException or TimeoutException or JsonException or UnauthorizedAccessException)
            {
                // Helper not installed, not running, or restarting: retry quietly.
            }

            try { await Task.Delay(RetryDelay, ct); }
            catch (OperationCanceledException) { return; }
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        try { _loop.Wait(TimeSpan.FromSeconds(2)); } catch { /* shutting down */ }
        _cts.Dispose();
    }
}
