using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace DialBar.App;

/// <summary>A 270° circular gauge: track, colored arc, value in the center, label underneath.</summary>
public sealed class DialControl : Control
{
    public static readonly StyledProperty<double?> FractionProperty =
        AvaloniaProperty.Register<DialControl, double?>(nameof(Fraction));

    public static readonly StyledProperty<string> ValueTextProperty =
        AvaloniaProperty.Register<DialControl, string>(nameof(ValueText), "");

    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<DialControl, string>(nameof(Label), "");

    private const double StartAngle = 135;
    private const double SweepAngle = 270;
    private const double WarnAt = 0.70;
    private const double CriticalAt = 0.90;

    private static readonly IBrush TrackBrush = new SolidColorBrush(Color.Parse("#33FFFFFF"));
    private static readonly IBrush TextBrush = new SolidColorBrush(Color.Parse("#F2F2F2"));
    private static readonly IBrush MutedBrush = new SolidColorBrush(Color.Parse("#B0B0B0"));
    private static readonly IBrush PlateBrush = new SolidColorBrush(Color.Parse("#CC1E1E1E"));
    private static readonly Pen OuterContour = new(new SolidColorBrush(Color.Parse("#E6000000")), 1);
    private static readonly Pen InnerContour = new(new SolidColorBrush(Color.Parse("#B3FFFFFF")), 1);
    private static readonly IBrush Green = new SolidColorBrush(Color.Parse("#4CAF50"));
    private static readonly IBrush Amber = new SolidColorBrush(Color.Parse("#FFB300"));
    private static readonly IBrush Red = new SolidColorBrush(Color.Parse("#E53935"));

    static DialControl()
    {
        AffectsRender<DialControl>(FractionProperty, ValueTextProperty, LabelProperty);
    }

    public double? Fraction { get => GetValue(FractionProperty); set => SetValue(FractionProperty, value); }
    public string ValueText { get => GetValue(ValueTextProperty); set => SetValue(ValueTextProperty, value); }
    public string Label { get => GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    protected override Size MeasureOverride(Size availableSize) => new(84, 96);

    public override void Render(DrawingContext context)
    {
        var plate = new Rect(0, 0, Bounds.Width, Bounds.Height);
        context.DrawRectangle(PlateBrush, null, plate, 10, 10);

        // Two-tone contour (dark outside, light inside) so the plate reads on any desktop background.
        context.DrawRectangle(null, OuterContour, plate.Deflate(0.5), 9.5, 9.5);
        context.DrawRectangle(null, InnerContour, plate.Deflate(1.5), 8.5, 8.5);

        var center = new Point(Bounds.Width / 2, 40);
        const double radius = 30;
        const double thickness = 7;

        var trackPen = new Pen(TrackBrush, thickness, lineCap: PenLineCap.Round);
        context.DrawGeometry(null, trackPen, Arc(center, radius, SweepAngle));

        if (Fraction is { } f)
        {
            f = Math.Clamp(f, 0, 1);
            if (f > 0.005)
            {
                var brush = f >= CriticalAt ? Red : f >= WarnAt ? Amber : Green;
                var pen = new Pen(brush, thickness, lineCap: PenLineCap.Round);
                context.DrawGeometry(null, pen, Arc(center, radius, SweepAngle * f));
            }
        }

        DrawCentered(context, ValueText, 13, FontWeight.SemiBold, TextBrush, center);
        DrawCentered(context, Label, 11, FontWeight.Normal, MutedBrush, new Point(center.X, 82));
    }

    private static void DrawCentered(DrawingContext context, string text, double size, FontWeight weight, IBrush brush, Point at)
    {
        var layout = new FormattedText(text, System.Globalization.CultureInfo.CurrentUICulture,
            FlowDirection.LeftToRight, new Typeface(FontFamily.Default, FontStyle.Normal, weight), size, brush);
        context.DrawText(layout, new Point(at.X - layout.Width / 2, at.Y - layout.Height / 2));
    }

    private static StreamGeometry Arc(Point center, double radius, double sweep)
    {
        Point At(double degrees)
        {
            var rad = degrees * Math.PI / 180;
            return new Point(center.X + radius * Math.Cos(rad), center.Y + radius * Math.Sin(rad));
        }

        var geometry = new StreamGeometry();
        using var g = geometry.Open();
        g.BeginFigure(At(StartAngle), false);
        g.ArcTo(At(StartAngle + sweep), new Size(radius, radius), 0, sweep > 180,
            SweepDirection.Clockwise);
        g.EndFigure(false);
        return geometry;
    }
}
