using System;
using System.Collections.Generic;
using System.Globalization;
using MetBench_BLL.SystemMT.Reporting.Charts;
using SkiaSharp;

namespace MetBench_BLL.Reporting.SystemMt.Charts.Rendering;

/// <summary>
/// Pure-SkiaSharp implementation of <see cref="ISystemMtChartRenderer"/>.
///
/// Deliberately bypasses LiveChartsCore for the canvas path — we only need
/// three simple figure kinds (BinaryScatter / PhaseLine / HistoricalTrend),
/// all reducible to axes + one polyline + dot markers. Going direct on Skia
/// keeps the PNG byte-deterministic and removes a layer of theming knobs
/// that would otherwise flake across LiveCharts releases.
///
/// Determinism contract (per <see cref="ISystemMtChartRenderer"/>): same
/// figure + same options yield byte-identical PNG output. Pinned by
/// <c>RenderPng_is_deterministic_for_same_input</c> in the test suite.
/// </summary>
public sealed class SkiaChartRenderer : ISystemMtChartRenderer
{
    public byte[] RenderPng(ChartFigure figure, ChartRenderOptions options)
    {
        if (figure is null) throw new ArgumentNullException(nameof(figure));
        if (options is null) throw new ArgumentNullException(nameof(options));
        if (options.Width <= 0) throw new ArgumentOutOfRangeException(nameof(options), $"Width must be > 0 (got {options.Width})");
        if (options.Height <= 0) throw new ArgumentOutOfRangeException(nameof(options), $"Height must be > 0 (got {options.Height})");
        if (options.Dpi <= 0) throw new ArgumentOutOfRangeException(nameof(options), $"Dpi must be > 0 (got {options.Dpi})");

        // SKAlphaType.Opaque tells the PNG encoder the image carries no alpha channel,
        // so downstream embedders (notably iTextSharp's Image.GetInstance) emit a single
        // /Subtype /Image XObject per chart instead of a (primary, /SMask soft-mask) pair.
        // The PDF test's smask-aware counter still works (becomes a no-op) but the
        // simpler 1:1 model is the principled state — see review item M4 from the
        // PR #183-#191 audit.
        var info = new SKImageInfo(options.Width, options.Height, SKColorType.Rgba8888, SKAlphaType.Opaque);
        using var surface = SKSurface.Create(info);
        if (surface is null)
        {
            throw new InvalidOperationException(
                $"SkiaSharp could not create a surface for {options.Width}x{options.Height}. Ensure the Linux native asset (SkiaSharp.NativeAssets.Linux.NoDependencies) is on the runtime path.");
        }

        var palette = ResolvePalette(options.Theme);
        var canvas = surface.Canvas;
        canvas.Clear(palette.Background);

        DrawTitleAndAxes(canvas, figure, options, palette, out var plotArea);
        DrawSeries(canvas, figure, plotArea, palette);
        DrawLegend(canvas, figure, plotArea, palette);

        using var image = surface.Snapshot();
        using var encoded = image.Encode(SKEncodedImageFormat.Png, quality: 100);
        return encoded.ToArray();
    }

    // ---- internal palette --------------------------------------------------

    private readonly struct Palette
    {
        public SKColor Background { get; init; }
        public SKColor Foreground { get; init; }
        public SKColor AxisColor { get; init; }
        public SKColor GridColor { get; init; }
        public IReadOnlyList<SKColor> Series { get; init; }
    }

    private static Palette ResolvePalette(ChartTheme theme) => theme switch
    {
        ChartTheme.Dark => new Palette
        {
            Background = new SKColor(0x1E, 0x1E, 0x1E),
            Foreground = new SKColor(0xE0, 0xE0, 0xE0),
            AxisColor = new SKColor(0xC0, 0xC0, 0xC0),
            GridColor = new SKColor(0x40, 0x40, 0x40),
            Series = new[]
            {
                new SKColor(0x4F, 0xC3, 0xF7),
                new SKColor(0xFF, 0x8A, 0x65),
                new SKColor(0xAE, 0xD5, 0x81),
                new SKColor(0xFF, 0xD5, 0x4F),
            },
        },
        _ => new Palette
        {
            Background = SKColors.White,
            Foreground = new SKColor(0x21, 0x21, 0x21),
            AxisColor = new SKColor(0x42, 0x42, 0x42),
            GridColor = new SKColor(0xE0, 0xE0, 0xE0),
            Series = new[]
            {
                new SKColor(0x19, 0x76, 0xD2),
                new SKColor(0xD3, 0x2F, 0x2F),
                new SKColor(0x38, 0x8E, 0x3C),
                new SKColor(0xFB, 0xC0, 0x2D),
            },
        },
    };

    // ---- title + axes ------------------------------------------------------

    private static void DrawTitleAndAxes(
        SKCanvas canvas,
        ChartFigure figure,
        ChartRenderOptions options,
        Palette palette,
        out SKRect plotArea)
    {
        var width = options.Width;
        var height = options.Height;

        const int marginTop = 60;
        const int marginBottom = 70;
        const int marginLeft = 80;
        const int marginRight = 40;

        plotArea = new SKRect(marginLeft, marginTop, width - marginRight, height - marginBottom);

        using var titlePaint = new SKPaint
        {
            Color = palette.Foreground,
            IsAntialias = true,
            TextSize = 22,
            Typeface = SKTypeface.Default,
        };
        var titleY = marginTop / 2f + titlePaint.TextSize / 3f;
        canvas.DrawText(figure.Title, marginLeft, titleY, titlePaint);

        using var axisLabelPaint = new SKPaint
        {
            Color = palette.Foreground,
            IsAntialias = true,
            TextSize = 14,
            Typeface = SKTypeface.Default,
        };
        // X axis label centered under plot area.
        var xLabelWidth = axisLabelPaint.MeasureText(figure.XAxisLabel);
        canvas.DrawText(
            figure.XAxisLabel,
            plotArea.MidX - xLabelWidth / 2f,
            height - 18,
            axisLabelPaint);

        // Y axis label rotated 90° on the left margin.
        canvas.Save();
        canvas.Translate(20, plotArea.MidY);
        canvas.RotateDegrees(-90);
        var yLabelWidth = axisLabelPaint.MeasureText(figure.YAxisLabel);
        canvas.DrawText(figure.YAxisLabel, -yLabelWidth / 2f, 0, axisLabelPaint);
        canvas.Restore();

        using var axisPaint = new SKPaint
        {
            Color = palette.AxisColor,
            IsAntialias = true,
            StrokeWidth = 1.5f,
            Style = SKPaintStyle.Stroke,
        };
        canvas.DrawLine(plotArea.Left, plotArea.Bottom, plotArea.Right, plotArea.Bottom, axisPaint);
        canvas.DrawLine(plotArea.Left, plotArea.Top, plotArea.Left, plotArea.Bottom, axisPaint);
    }

    // ---- series ------------------------------------------------------------

    private static void DrawSeries(SKCanvas canvas, ChartFigure figure, SKRect plotArea, Palette palette)
    {
        // Empty figure -> axes-only frame, valid PNG, no exception.
        if (figure.SeriesList.Count == 0)
        {
            return;
        }

        // Compute global min/max across all finite points.
        var (xMin, xMax, yMin, yMax) = ComputeBounds(figure);
        // Pad y range a bit so points aren't glued to the axis.
        if (yMax - yMin <= double.Epsilon)
        {
            yMax = yMin + 1.0;
        }
        if (xMax - xMin <= double.Epsilon)
        {
            xMax = xMin + 1.0;
        }
        DrawGridAndTicks(canvas, plotArea, palette, xMin, xMax, yMin, yMax);

        for (var si = 0; si < figure.SeriesList.Count; si++)
        {
            var series = figure.SeriesList[si];
            var color = palette.Series[si % palette.Series.Count];

            using var linePaint = new SKPaint
            {
                Color = color,
                IsAntialias = true,
                StrokeWidth = 2.5f,
                Style = SKPaintStyle.Stroke,
            };
            using var dotPaint = new SKPaint
            {
                Color = color,
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
            };

            SKPoint? previous = null;
            foreach (var point in series.Points)
            {
                if (!double.IsFinite(point.X) || !double.IsFinite(point.Y))
                {
                    previous = null;
                    continue;
                }
                var sp = Project(point, plotArea, xMin, xMax, yMin, yMax);
                // PhaseLine + HistoricalTrend draw a connecting line; BinaryScatter is dot-only.
                if (figure.Kind != ChartFigureKind.BinaryScatter && previous is SKPoint prev)
                {
                    canvas.DrawLine(prev, sp, linePaint);
                }
                canvas.DrawCircle(sp, 5f, dotPaint);
                previous = sp;
            }
        }
    }

    private static (double xMin, double xMax, double yMin, double yMax) ComputeBounds(ChartFigure figure)
    {
        double xMin = double.PositiveInfinity, xMax = double.NegativeInfinity;
        double yMin = double.PositiveInfinity, yMax = double.NegativeInfinity;
        var any = false;
        foreach (var s in figure.SeriesList)
        {
            foreach (var p in s.Points)
            {
                if (!double.IsFinite(p.X) || !double.IsFinite(p.Y)) continue;
                any = true;
                if (p.X < xMin) xMin = p.X;
                if (p.X > xMax) xMax = p.X;
                if (p.Y < yMin) yMin = p.Y;
                if (p.Y > yMax) yMax = p.Y;
            }
        }
        if (!any)
        {
            return (0.0, 1.0, 0.0, 1.0);
        }
        return (xMin, xMax, yMin, yMax);
    }

    private static SKPoint Project(
        ChartPoint p, SKRect plotArea, double xMin, double xMax, double yMin, double yMax)
    {
        var xT = (p.X - xMin) / (xMax - xMin);
        var yT = (p.Y - yMin) / (yMax - yMin);
        var px = plotArea.Left + (float)(xT * plotArea.Width);
        var py = plotArea.Bottom - (float)(yT * plotArea.Height);
        return new SKPoint(px, py);
    }

    private static void DrawGridAndTicks(
        SKCanvas canvas, SKRect plotArea, Palette palette,
        double xMin, double xMax, double yMin, double yMax)
    {
        using var gridPaint = new SKPaint
        {
            Color = palette.GridColor,
            IsAntialias = true,
            StrokeWidth = 0.75f,
            Style = SKPaintStyle.Stroke,
        };
        using var tickPaint = new SKPaint
        {
            Color = palette.Foreground,
            IsAntialias = true,
            TextSize = 11,
            Typeface = SKTypeface.Default,
        };

        // 4 horizontal grid lines + y-axis tick labels.
        const int yTicks = 5;
        for (var i = 0; i < yTicks; i++)
        {
            var t = i / (double)(yTicks - 1);
            var y = plotArea.Bottom - (float)(t * plotArea.Height);
            canvas.DrawLine(plotArea.Left, y, plotArea.Right, y, gridPaint);
            var value = yMin + t * (yMax - yMin);
            var label = value.ToString("G4", CultureInfo.InvariantCulture);
            canvas.DrawText(label, plotArea.Left - 8 - tickPaint.MeasureText(label), y + 4, tickPaint);
        }
    }

    // ---- legend ------------------------------------------------------------

    private static void DrawLegend(SKCanvas canvas, ChartFigure figure, SKRect plotArea, Palette palette)
    {
        if (figure.SeriesList.Count == 0) return;

        using var textPaint = new SKPaint
        {
            Color = palette.Foreground,
            IsAntialias = true,
            TextSize = 12,
            Typeface = SKTypeface.Default,
        };

        var x = plotArea.Right - 200;
        var y = plotArea.Top + 18;
        for (var si = 0; si < figure.SeriesList.Count; si++)
        {
            var series = figure.SeriesList[si];
            var color = palette.Series[si % palette.Series.Count];
            using var swatch = new SKPaint { Color = color, IsAntialias = true, Style = SKPaintStyle.Fill };
            canvas.DrawRect(new SKRect(x, y - 10, x + 12, y + 2), swatch);
            canvas.DrawText(series.Name, x + 18, y, textPaint);
            y += 18;
        }
    }
}
