using System.Windows;
using System.Windows.Media;
using WpfBrush = System.Windows.Media.Brush;
using WpfPen = System.Windows.Media.Pen;
using WpfPoint = System.Windows.Point;

namespace LocalLlmConsole;

/// <summary>A small, dependency-free time-series plot intended for overview metric cards.</summary>
public sealed class MetricSparkline : FrameworkElement
{
    private const int MaximumSamples = 60;
    private readonly List<MetricSparklineSample> _samples = [];
    private string _seriesKey = "";

    public string PrimaryBrushKey { get; init; } = "AccentBlue";

    public string SecondaryBrushKey { get; init; } = "Accent";

    public double? FixedMaximum { get; init; }

    public int SampleCount => _samples.Count;

    public void Push(string seriesKey, double? primary, double? secondary = null)
    {
        if (string.IsNullOrWhiteSpace(seriesKey))
        {
            Clear();
            return;
        }

        if (!string.Equals(_seriesKey, seriesKey, StringComparison.Ordinal))
        {
            _seriesKey = seriesKey;
            _samples.Clear();
        }

        _samples.Add(new MetricSparklineSample(Normalize(primary), Normalize(secondary)));
        if (_samples.Count > MaximumSamples)
            _samples.RemoveRange(0, _samples.Count - MaximumSamples);
        InvalidateVisual();
    }

    public void Clear()
    {
        _seriesKey = "";
        _samples.Clear();
        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        if (ActualWidth <= 1 || ActualHeight <= 1) return;

        var gridBrush = ResourceBrush("PanelBorder").Clone();
        gridBrush.Opacity = 0.55;
        var gridPen = new WpfPen(gridBrush, 0.6) { DashStyle = DashStyles.Dot };
        for (var division = 1; division <= 2; division++)
        {
            var y = Math.Round(ActualHeight * division / 3.0) + 0.5;
            drawingContext.DrawLine(gridPen, new WpfPoint(0, y), new WpfPoint(ActualWidth, y));
        }

        if (_samples.Count == 0) return;
        var observedMaximum = _samples
            .SelectMany(sample => new[] { sample.Primary, sample.Secondary })
            .Where(value => value is not null)
            .Select(value => value!.Value)
            .DefaultIfEmpty(0)
            .Max();
        var maximum = FixedMaximum is > 0
            ? FixedMaximum.Value
            : Math.Max(1, observedMaximum * 1.08);

        DrawSeries(drawingContext, sample => sample.Primary, ResourceBrush(PrimaryBrushKey), maximum);
        DrawSeries(drawingContext, sample => sample.Secondary, ResourceBrush(SecondaryBrushKey), maximum);
    }

    private void DrawSeries(
        DrawingContext drawingContext,
        Func<MetricSparklineSample, double?> selector,
        WpfBrush brush,
        double maximum)
    {
        var pen = new WpfPen(brush, 1.5);
        WpfPoint? previous = null;
        WpfPoint? latest = null;
        for (var index = 0; index < _samples.Count; index++)
        {
            var value = selector(_samples[index]);
            if (value is null)
            {
                previous = null;
                continue;
            }

            var plotWidth = Math.Max(0, ActualWidth - 3);
            var x = _samples.Count == 1 ? plotWidth + 1 : 1 + index * plotWidth / (_samples.Count - 1.0);
            var fraction = Math.Clamp(value.Value / maximum, 0, 1);
            var point = new WpfPoint(x, Math.Max(1, (ActualHeight - 2) * (1 - fraction) + 1));
            if (previous is { } start)
                drawingContext.DrawLine(pen, start, point);
            previous = point;
            latest = point;
        }

        if (latest is { } marker)
            drawingContext.DrawEllipse(brush, null, marker, 2.2, 2.2);
    }

    private WpfBrush ResourceBrush(string key)
        => TryFindResource(key) as WpfBrush ?? System.Windows.Media.Brushes.SlateGray;

    private static double? Normalize(double? value)
        => value is { } number && double.IsFinite(number) && number >= 0 ? number : null;

    private sealed record MetricSparklineSample(double? Primary, double? Secondary);
}
