using System.ComponentModel;
using System.Runtime.CompilerServices;
using PcMeter.Core;

namespace PcMeter.App;

public sealed class DialViewModel(MetricKind kind, string label) : INotifyPropertyChanged
{
    private double? _fraction;
    private string _valueText = "…";
    private string _detail = "";

    public MetricKind Kind { get; } = kind;
    public string Label { get; } = label;

    public double? Fraction { get => _fraction; private set => Set(ref _fraction, value); }
    public string ValueText { get => _valueText; private set => Set(ref _valueText, value); }
    public string Detail { get => _detail; private set => Set(ref _detail, value); }

    public void Apply(MetricReading reading)
    {
        Fraction = reading.Fraction;
        ValueText = reading.Text;
        Detail = $"{Label}\n{reading.Detail}";
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
