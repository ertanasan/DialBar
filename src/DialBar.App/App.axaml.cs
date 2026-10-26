using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using DialBar.Core;

namespace DialBar.App;

public sealed class App : Application
{
    private static readonly (MetricKind Kind, string Label)[] Gauges =
    [
        (MetricKind.Cpu, "CPU"),
        (MetricKind.Heat, "Heat"),
        (MetricKind.Memory, "Memory"),
        (MetricKind.Disk, "Disk"),
        (MetricKind.Network, "Network"),
        (MetricKind.Gpu, "GPU"),
        (MetricKind.Fan, "Fans"),
    ];

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = new MainWindow();
            desktop.MainWindow = window;

            var tray = new TrayIcon
            {
                Icon = new WindowIcon(AssetLoader.Open(new Uri("avares://DialBar.App/Assets/DialBar.ico"))),
                ToolTipText = "DialBar",
                Menu = BuildMenu(window, desktop),
            };
            tray.Clicked += (_, _) => window.ToggleBar();
            TrayIcon.SetIcons(this, [tray]);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static AboutWindow? _about;

    private static void ShowAbout()
    {
        // One About window at a time; bring the existing one forward instead of opening a second.
        if (_about is not null)
        {
            _about.Activate();
            return;
        }
        _about = new AboutWindow();
        _about.Closed += (_, _) => _about = null;
        _about.Show();
    }

    private static NativeMenu BuildMenu(MainWindow window, IClassicDesktopStyleApplicationLifetime desktop)
    {
        var menu = new NativeMenu();

        var show = new NativeMenuItem("Show bar") { ToggleType = NativeMenuItemToggleType.CheckBox, IsChecked = true };
        show.Click += (_, _) => window.ToggleBar();
        menu.Add(show);
        menu.Add(new NativeMenuItemSeparator());

        foreach (var (kind, label) in Gauges)
        {
            var item = new NativeMenuItem(label)
            {
                ToggleType = NativeMenuItemToggleType.CheckBox,
                IsChecked = window.IsGaugeShown(kind),
            };
            item.Click += (_, _) =>
            {
                var shown = !window.IsGaugeShown(kind);
                item.IsChecked = shown;
                window.SetGaugeShown(kind, shown);
            };
            menu.Add(item);
        }

        menu.Add(new NativeMenuItemSeparator());

        var topmost = new NativeMenuItem("Always on top") { ToggleType = NativeMenuItemToggleType.CheckBox, IsChecked = window.Topmost };
        topmost.Click += (_, _) =>
        {
            var value = !window.Topmost;
            topmost.IsChecked = value;
            window.SetAlwaysOnTop(value);
        };
        menu.Add(topmost);

        var vertical = new NativeMenuItem("Vertical layout") { ToggleType = NativeMenuItemToggleType.CheckBox, IsChecked = window.IsVertical };
        vertical.Click += (_, _) =>
        {
            var value = !window.IsVertical;
            vertical.IsChecked = value;
            window.SetVertical(value);
        };
        menu.Add(vertical);

        var startup = new NativeMenuItem("Start with Windows") { ToggleType = NativeMenuItemToggleType.CheckBox, IsChecked = StartupRegistration.IsEnabled };
        startup.Click += (_, _) =>
        {
            var value = !StartupRegistration.IsEnabled;
            StartupRegistration.SetEnabled(value);
            startup.IsChecked = value;
        };
        menu.Add(startup);

        menu.Add(new NativeMenuItemSeparator());

        var about = new NativeMenuItem("About DialBar");
        about.Click += (_, _) => ShowAbout();
        menu.Add(about);

        var exit = new NativeMenuItem("Exit");
        exit.Click += (_, _) => desktop.Shutdown();
        menu.Add(exit);

        // Keep the "Show bar" check in sync when the bar is hidden from elsewhere (click on tray, context menu).
        window.PropertyChanged += (_, e) =>
        {
            if (e.Property == Visual.IsVisibleProperty) show.IsChecked = window.IsVisible;
        };

        return menu;
    }
}
