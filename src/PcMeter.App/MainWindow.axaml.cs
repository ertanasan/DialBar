using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using PcMeter.Core;
using PcMeter.Providers.Windows;

namespace PcMeter.App;

public sealed partial class MainWindow : Avalonia.Controls.Window
{
    private static readonly (MetricKind Kind, string Label)[] Layout =
    [
        (MetricKind.Cpu, "CPU"),
        (MetricKind.Heat, "HEAT"),
        (MetricKind.Memory, "MEM"),
        (MetricKind.Disk, "DISK"),
        (MetricKind.Network, "NET"),
        (MetricKind.Gpu, "GPU"),
    ];

    private readonly AppSettings _settings = SettingsStore.Load();
    private readonly List<DialViewModel> _allDials;
    private readonly MetricSampler _sampler;
    private MetricKind? _solo;
    private readonly DispatcherTimer _saveTimer = new() { Interval = TimeSpan.FromMilliseconds(500) };

    public MainWindow()
    {
        _allDials = Layout.Select(l => new DialViewModel(l.Kind, l.Label)).ToList();
        Dials = new ObservableCollection<DialViewModel>(_allDials.Where(d => IsShown(d.Kind)));
        DataContext = this;
        InitializeComponent();

        Topmost = _settings.AlwaysOnTop;
        ApplyOrientation();

        _sampler = new MetricSampler(WindowsProviders.CreateAll(), TimeSpan.FromSeconds(_settings.IntervalSeconds));
        _sampler.Sampled += OnSampled;

        _saveTimer.Tick += (_, _) => { _saveTimer.Stop(); SaveSettings(); };
        PositionChanged += (_, _) => { _saveTimer.Stop(); _saveTimer.Start(); };
        Opened += (_, _) => { RestorePosition(); _sampler.Start(); };
        Closed += (_, _) => { SaveSettings(); _sampler.Dispose(); };
    }

    public ObservableCollection<DialViewModel> Dials { get; }

    public bool IsGaugeShown(MetricKind kind) => IsShown(kind);

    public void SetGaugeShown(MetricKind kind, bool shown)
    {
        _settings.HiddenGauges.Remove(kind);
        if (!shown) _settings.HiddenGauges.Add(kind);

        if (_solo == kind && !shown) _solo = null;
        RebuildDials();
        SaveSettings();
    }

    public void SetAlwaysOnTop(bool value)
    {
        Topmost = value;
        _settings.AlwaysOnTop = value;
        SaveSettings();
    }

    public bool IsVertical => _settings.Vertical;

    public void SetVertical(bool vertical)
    {
        _settings.Vertical = vertical;
        ApplyOrientation();
        SaveSettings();
    }

    private Avalonia.Controls.StackPanel? _panel;

    private Avalonia.Layout.Orientation CurrentOrientation =>
        _settings.Vertical ? Avalonia.Layout.Orientation.Vertical : Avalonia.Layout.Orientation.Horizontal;

    private void ApplyOrientation()
    {
        // Reuse the live panel when it exists: swapping the ItemsPanel template after the window is shown is not reliable.
        if (_panel is not null)
        {
            _panel.Orientation = CurrentOrientation;
            return;
        }

        DialList.ItemsPanel = new Avalonia.Controls.Templates.FuncTemplate<Avalonia.Controls.Panel?>(
            () => _panel = new Avalonia.Controls.StackPanel { Orientation = CurrentOrientation, Spacing = 6 });
    }

    public void ToggleBar()
    {
        if (IsVisible) Hide();
        else Show();
    }

    private bool IsShown(MetricKind kind) => !_settings.HiddenGauges.Contains(kind);

    private void RestorePosition()
    {
        if (_settings.Left is not { } left || _settings.Top is not { } top) return;

        // Ignore a saved position that is no longer on any screen (monitor unplugged, resolution changed).
        var point = new PixelPoint(left, top);
        if (Screens.All.Any(s => s.WorkingArea.Contains(point)))
            Position = point;
    }

    private void SaveSettings()
    {
        _settings.Left = Position.X;
        _settings.Top = Position.Y;
        SettingsStore.Save(_settings);
    }

    private void OnSampled(IReadOnlyList<MetricReading> readings) =>
        Dispatcher.UIThread.Post(() =>
        {
            foreach (var reading in readings)
                _allDials.FirstOrDefault(d => d.Kind == reading.Kind)?.Apply(reading);
        });

    // A left press becomes a drag once the pointer travels past DragThreshold, otherwise a click on release.
    private const double DragThreshold = 4;
    private PointerPressedEventArgs? _press;
    private Point _pressPoint;

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            _press = e;
            _pressPoint = e.GetPosition(this);
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_press is not { } press) return;

        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            _press = null;
            return;
        }

        var delta = e.GetPosition(this) - _pressPoint;
        if (Math.Abs(delta.X) > DragThreshold || Math.Abs(delta.Y) > DragThreshold)
        {
            _press = null;
            BeginMoveDrag(press);
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_press is null) return;
        _press = null;

        var dial = (e.Source as Visual)?.FindAncestorOfType<DialControl>(includeSelf: true)?.DataContext as DialViewModel;
        ToggleSolo(dial);
    }

    /// <summary>First click on a dial shows only that dial; the next click anywhere on the bar brings all back.</summary>
    private void ToggleSolo(DialViewModel? clicked)
    {
        if (_solo is not null) _solo = null;
        else if (clicked is not null) _solo = clicked.Kind;
        else return;

        RebuildDials();
    }

    private void RebuildDials()
    {
        // Canonical order, so re-shown dials return to their original slot.
        Dials.Clear();
        foreach (var dial in _allDials.Where(d => IsShown(d.Kind) && (_solo is null || d.Kind == _solo)))
            Dials.Add(dial);
    }

    private void OnHide(object? sender, RoutedEventArgs e) => Hide();

    private void OnExit(object? sender, RoutedEventArgs e) =>
        ((Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)
            Application.Current!.ApplicationLifetime!).Shutdown();
}
