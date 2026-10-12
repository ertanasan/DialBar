using System.Reflection;
using Avalonia.Interactivity;

namespace DialBar.App;

public sealed partial class AboutWindow : Avalonia.Controls.Window
{
    public AboutWindow()
    {
        InitializeComponent();

        var version = Assembly.GetExecutingAssembly().GetName().Version;
        VersionText.Text = $"Version {version?.ToString(3) ?? "1.0.0"}";
    }

    private void OnClose(object? sender, RoutedEventArgs e) => Close();
}
