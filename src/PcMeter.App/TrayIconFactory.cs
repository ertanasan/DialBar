using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace PcMeter.App;

internal static class TrayIconFactory
{
    /// <summary>Draws a small dial glyph so no binary icon asset is needed.</summary>
    public static WindowIcon Create()
    {
        const int size = 32;
        var bitmap = new RenderTargetBitmap(new PixelSize(size, size));
        using (var ctx = bitmap.CreateDrawingContext())
        {
            var green = new SolidColorBrush(Color.Parse("#4CAF50"));
            var track = new Pen(new SolidColorBrush(Color.Parse("#808080")), 4, lineCap: PenLineCap.Round);
            var arc = new Pen(green, 4, lineCap: PenLineCap.Round);

            ctx.DrawRectangle(new SolidColorBrush(Color.Parse("#1E1E1E")), null, new Rect(0, 0, size, size), 6, 6);
            ctx.DrawGeometry(null, track, Arc(16, 17, 9, 270));
            ctx.DrawGeometry(null, arc, Arc(16, 17, 9, 170));
        }
        return new WindowIcon(bitmap);
    }

    private static StreamGeometry Arc(double cx, double cy, double r, double sweep)
    {
        Point At(double deg) => new(cx + r * Math.Cos(deg * Math.PI / 180), cy + r * Math.Sin(deg * Math.PI / 180));
        var g = new StreamGeometry();
        using var c = g.Open();
        c.BeginFigure(At(135), false);
        c.ArcTo(At(135 + sweep), new Size(r, r), 0, sweep > 180, SweepDirection.Clockwise);
        c.EndFigure(false);
        return g;
    }
}
