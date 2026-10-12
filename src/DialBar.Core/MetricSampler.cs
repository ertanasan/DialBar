namespace DialBar.Core;

/// <summary>Polls all providers on a background thread and publishes the readings.</summary>
public sealed class MetricSampler : IDisposable
{
    private readonly IReadOnlyList<IMetricProvider> _providers;
    private readonly CancellationTokenSource _cts = new();
    private Task? _loop;

    public MetricSampler(IEnumerable<IMetricProvider> providers, TimeSpan interval)
    {
        _providers = providers.ToList();
        Interval = interval;
    }

    public TimeSpan Interval { get; set; }

    /// <summary>Raised on a background thread after each sampling pass.</summary>
    public event Action<IReadOnlyList<MetricReading>>? Sampled;

    public void Start() => _loop ??= Task.Run(RunAsync);

    private async Task RunAsync()
    {
        // The first read primes rate-based counters; the next pass has real values.
        Sample();
        using var timer = new PeriodicTimer(Interval);
        var current = Interval;
        while (await WaitAsync(timer))
        {
            if (current != Interval)
            {
                current = Interval;
                timer.Period = current;
            }
            Sample();
        }
    }

    private async Task<bool> WaitAsync(PeriodicTimer timer)
    {
        try { return await timer.WaitForNextTickAsync(_cts.Token); }
        catch (OperationCanceledException) { return false; }
    }

    private void Sample()
    {
        var readings = new List<MetricReading>(_providers.Count);
        foreach (var provider in _providers)
        {
            try { readings.Add(provider.Read()); }
            catch (Exception ex) { readings.Add(MetricReading.NotAvailable(provider.Kind, ex.Message)); }
        }
        Sampled?.Invoke(readings);
    }

    public void Dispose()
    {
        _cts.Cancel();
        try { _loop?.Wait(TimeSpan.FromSeconds(2)); } catch { /* shutting down */ }
        foreach (var provider in _providers) provider.Dispose();
        _cts.Dispose();
    }
}
