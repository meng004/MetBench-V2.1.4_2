namespace MetBench_BLL.SystemMT.Reporting.Charts;

/// <summary>
/// Knobs for the offscreen PNG renderer. Defaults target embedded-report
/// scale (1200 × 800 @ 150 dpi keeps a multi-record PDF under ~5 MB even
/// with 32 inline charts — risk R4 from the T2 plan).
/// </summary>
public sealed record ChartRenderOptions(
    int Width = 1200,
    int Height = 800,
    int Dpi = 150,
    ChartTheme Theme = ChartTheme.Light);
