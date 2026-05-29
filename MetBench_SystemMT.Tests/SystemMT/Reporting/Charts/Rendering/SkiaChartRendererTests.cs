using System;
using System.Collections.Generic;
using MetBench_BLL.Reporting.SystemMt.Charts.Rendering;
using MetBench_BLL.SystemMT.Reporting.Charts;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Reporting.Charts.Rendering;

public sealed class SkiaChartRendererTests
{
    private static readonly byte[] PngMagic = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

    private static ChartFigure MakePhaseFigure(params (string Role, double Value)[] phases)
    {
        var points = new List<ChartPoint>();
        for (var i = 0; i < phases.Length; i++)
        {
            points.Add(new ChartPoint(i, phases[i].Value, Label: phases[i].Role));
        }
        return new ChartFigure(
            Title: "openmoc-pincell-ray-track-convergence — k_eff convergence across phases",
            XAxisLabel: "phase",
            YAxisLabel: "k_eff",
            SeriesList: new[] { new ChartSeries("k_eff", points) },
            Kind: ChartFigureKind.PhaseLine);
    }

    private static ChartFigure MakeBinaryFigure(double src, double fu) =>
        new(
            Title: "openmoc-pincell-nu-sigma-f — k_eff (source vs follow-up)",
            XAxisLabel: "side",
            YAxisLabel: "k_eff",
            SeriesList: new[]
            {
                new ChartSeries(
                    "k_eff",
                    new[]
                    {
                        new ChartPoint(0.0, src, Label: "source"),
                        new ChartPoint(1.0, fu, Label: "follow-up"),
                    }),
            },
            Kind: ChartFigureKind.BinaryScatter);

    private static (int Width, int Height) ReadPngDimensions(byte[] png)
    {
        // IHDR chunk follows the 8-byte magic + 4-byte length + "IHDR".
        var width = (png[16] << 24) | (png[17] << 16) | (png[18] << 8) | png[19];
        var height = (png[20] << 24) | (png[21] << 16) | (png[22] << 8) | png[23];
        return (width, height);
    }

    [Fact]
    public void RenderPng_returns_valid_png_magic_for_phase_line()
    {
        var renderer = new SkiaChartRenderer();
        var fig = MakePhaseFigure(("coarse", 1.42), ("medium", 1.43), ("reference", 1.4317));

        var bytes = renderer.RenderPng(fig, new ChartRenderOptions());

        Assert.True(bytes.Length >= 8);
        for (var i = 0; i < PngMagic.Length; i++)
        {
            Assert.Equal(PngMagic[i], bytes[i]);
        }
    }

    [Fact]
    public void RenderPng_ihdr_dimensions_match_options()
    {
        var renderer = new SkiaChartRenderer();
        var fig = MakePhaseFigure(("a", 1.0), ("b", 2.0));

        var bytes = renderer.RenderPng(fig, new ChartRenderOptions(Width: 800, Height: 600));

        var (w, h) = ReadPngDimensions(bytes);
        Assert.Equal(800, w);
        Assert.Equal(600, h);
    }

    [Fact]
    public void RenderPng_default_options_produces_size_in_sane_range()
    {
        var renderer = new SkiaChartRenderer();
        var fig = MakePhaseFigure(("coarse", 1.42), ("medium", 1.43), ("reference", 1.4317));

        var bytes = renderer.RenderPng(fig, new ChartRenderOptions());

        // 1200x800 PNG with axes + a few points compresses to well under 500 KB.
        Assert.InRange(bytes.Length, 2_000, 500_000);
    }

    [Fact]
    public void RenderPng_is_deterministic_for_same_input()
    {
        var renderer = new SkiaChartRenderer();
        var fig = MakePhaseFigure(("coarse", 1.42), ("medium", 1.43), ("reference", 1.4317));
        var opts = new ChartRenderOptions();

        var a = renderer.RenderPng(fig, opts);
        var b = renderer.RenderPng(fig, opts);

        Assert.Equal(a, b);
    }

    [Fact]
    public void RenderPng_handles_empty_series_list_without_throwing()
    {
        var renderer = new SkiaChartRenderer();
        var fig = new ChartFigure(
            Title: "empty",
            XAxisLabel: "x",
            YAxisLabel: "y",
            SeriesList: Array.Empty<ChartSeries>(),
            Kind: ChartFigureKind.PhaseLine);

        var bytes = renderer.RenderPng(fig, new ChartRenderOptions());

        Assert.True(bytes.Length > 8);
        for (var i = 0; i < PngMagic.Length; i++)
        {
            Assert.Equal(PngMagic[i], bytes[i]);
        }
    }

    [Fact]
    public void RenderPng_handles_binary_scatter_kind()
    {
        var renderer = new SkiaChartRenderer();
        var fig = MakeBinaryFigure(src: 1.0, fu: 1.5);

        var bytes = renderer.RenderPng(fig, new ChartRenderOptions());

        Assert.True(bytes.Length > 1000);
        var (w, h) = ReadPngDimensions(bytes);
        Assert.Equal(1200, w);
        Assert.Equal(800, h);
    }

    [Fact]
    public void RenderPng_handles_historical_trend_kind()
    {
        var renderer = new SkiaChartRenderer();
        var points = new List<ChartPoint>();
        for (var i = 0; i < 10; i++)
        {
            points.Add(new ChartPoint(i, 1.0 + 0.1 * i));
        }
        var fig = new ChartFigure(
            Title: "mr-x — historical trend",
            XAxisLabel: "run index",
            YAxisLabel: "k_eff",
            SeriesList: new[] { new ChartSeries("k_eff", points) },
            Kind: ChartFigureKind.HistoricalTrend);

        var bytes = renderer.RenderPng(fig, new ChartRenderOptions());

        Assert.True(bytes.Length > 1000);
    }

    [Fact]
    public void RenderPng_skips_non_finite_points_without_throwing()
    {
        var renderer = new SkiaChartRenderer();
        var fig = new ChartFigure(
            Title: "with NaNs",
            XAxisLabel: "x",
            YAxisLabel: "y",
            SeriesList: new[]
            {
                new ChartSeries("series-a", new[]
                {
                    new ChartPoint(0, 1.0),
                    new ChartPoint(1, double.NaN),
                    new ChartPoint(2, double.PositiveInfinity),
                    new ChartPoint(3, 2.0),
                }),
            },
            Kind: ChartFigureKind.PhaseLine);

        var bytes = renderer.RenderPng(fig, new ChartRenderOptions());

        Assert.True(bytes.Length > 1000);
    }

    [Fact]
    public void RenderPng_supports_dark_theme()
    {
        var renderer = new SkiaChartRenderer();
        var fig = MakePhaseFigure(("a", 1.0), ("b", 2.0));

        var darkBytes = renderer.RenderPng(fig, new ChartRenderOptions(Theme: ChartTheme.Dark));
        var lightBytes = renderer.RenderPng(fig, new ChartRenderOptions(Theme: ChartTheme.Light));

        Assert.NotEqual(darkBytes, lightBytes);
    }

    [Fact]
    public void RenderPng_throws_on_null_figure()
    {
        var renderer = new SkiaChartRenderer();
        Assert.Throws<ArgumentNullException>(() => renderer.RenderPng(null!, new ChartRenderOptions()));
    }

    [Fact]
    public void RenderPng_throws_on_null_options()
    {
        var renderer = new SkiaChartRenderer();
        var fig = MakePhaseFigure(("a", 1.0), ("b", 2.0));
        Assert.Throws<ArgumentNullException>(() => renderer.RenderPng(fig, null!));
    }

    [Fact]
    public void RenderPng_throws_on_non_positive_dimensions_or_dpi()
    {
        var renderer = new SkiaChartRenderer();
        var fig = MakePhaseFigure(("a", 1.0), ("b", 2.0));

        Assert.Throws<ArgumentOutOfRangeException>(() => renderer.RenderPng(fig, new ChartRenderOptions(Width: 0)));
        Assert.Throws<ArgumentOutOfRangeException>(() => renderer.RenderPng(fig, new ChartRenderOptions(Height: -1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => renderer.RenderPng(fig, new ChartRenderOptions(Dpi: 0)));
    }

    [Fact]
    public void RenderPng_output_is_opaque_no_alpha_channel_in_ihdr()
    {
        // M4 (PR #183-#191 review): the renderer now uses SKAlphaType.Opaque so the PNG
        // carries no alpha channel. Downstream embedders (iTextSharp) consequently emit
        // a single /Subtype /Image XObject per chart instead of (primary, /SMask) pair.
        // PNG IHDR byte 25 is the color type: 2 = truecolor RGB (opaque), 6 = RGBA.
        // We require 2 here so future regressions to Premul fail loudly.
        var renderer = new SkiaChartRenderer();
        var fig = MakePhaseFigure(("a", 1.0), ("b", 2.0), ("c", 3.0));

        var bytes = renderer.RenderPng(fig, new ChartRenderOptions());

        // IHDR layout: 8-byte PNG magic + 4-byte length + 4-byte "IHDR" + 4+4 dimensions
        // + 1-byte bit depth + 1-byte color type (index 25).
        Assert.Equal(2, bytes[25]);
    }
}
