using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using WinWidget.Core;

namespace WinWidget;

public sealed class HistoryChart : FrameworkElement
{
    public IReadOnlyList<HistoryPoint> Points { get; init; } = Array.Empty<HistoryPoint>();
    public DateTimeOffset End { get; init; }
    public bool ShowDeepSeek { get; init; } = true;
    public bool ShowCodex { get; init; } = true;
    public HistoryChart() { Height = 92; Margin = new Thickness(0, 8, 0, 0); }
    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        double left = 31, top = 8, w = Math.Max(10, ActualWidth - 63), h = 62;
        var blue = WidgetWindow.Brush("#74B4FF"); var green = WidgetWindow.Brush("#86DFC0");
        double max = Math.Max(1, Points.MaxBy(p => p.DeepSeek ?? 0)?.DeepSeek ?? 1);
        for (int i = 0; i <= 2; i++)
        {
            double y = top + i * h / 2;
            dc.DrawLine(new Pen(WidgetWindow.Brush("#303743"), 1), new(left, y), new(left + w, y));
            if (ShowDeepSeek) Text(dc, (max * (2 - i) / 2).ToString("0.#"), 0, y - 5, blue);
            if (ShowCodex) Text(dc, (100 - i * 50) + "%", left + w + 4, y - 5, green);
        }
        if (ShowDeepSeek) Line(dc, p => p.DeepSeek, max, blue, left, top, w, h);
        if (ShowCodex) Line(dc, p => p.Weekly, 100, green, left, top, w, h);
        Text(dc, End.AddHours(-167).ToLocalTime().ToString("MMM d"), left, top + h + 10, WidgetWindow.Brush("#858B97"));
        Text(dc, End.ToLocalTime().ToString("MMM d"), left + w - 28, top + h + 10, WidgetWindow.Brush("#858B97"));
        if (!Points.Any(p => (ShowDeepSeek && p.DeepSeek != null) || (ShowCodex && p.Weekly != null)))
            Text(dc, "Waiting for hourly snapshots", left + 12, top + 24, WidgetWindow.Brush("#A4ACBA"));
    }
    private void Line(DrawingContext dc, Func<HistoryPoint, double?> value, double max, Brush color, double left, double top, double w, double h)
    {
        Point? previous = null; DateTimeOffset? previousHour = null;
        foreach (var sample in Points)
        {
            var v = value(sample);
            if (v == null || !double.IsFinite(v.Value)) { previous = null; previousHour = null; continue; }
            var p = new Point(left + (sample.Hour - End.AddHours(-167)).TotalHours / 167 * w, top + h - Math.Clamp(v.Value / max, 0, 1) * h);
            if (previous is Point prior && (sample.Hour - previousHour!.Value).TotalHours <= 1) dc.DrawLine(new Pen(color, 1.7), prior, p);
            dc.DrawEllipse(color, null, p, 1.7, 1.7); previous = p; previousHour = sample.Hour;
        }
    }
    private void Text(DrawingContext dc, string text, double x, double y, Brush color) => dc.DrawText(new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), 9, color, VisualTreeHelper.GetDpi(this).PixelsPerDip), new(x, y));
}
