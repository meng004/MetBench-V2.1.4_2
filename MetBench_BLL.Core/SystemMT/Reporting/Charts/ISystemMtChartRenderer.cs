namespace MetBench_BLL.SystemMT.Reporting.Charts;

/// <summary>
/// Renderer contract for turning a Phase-1 <see cref="ChartFigure"/> into a
/// PNG byte buffer. Implementations live in <c>MetBench_BLL</c> (SkiaSharp
/// dependency lives there); this interface stays in <c>MetBench_BLL.Core</c>
/// so consumers in the Core layer can depend on it without a cycle.
///
/// Implementations MUST be deterministic: same <see cref="ChartFigure"/> +
/// same <see cref="ChartRenderOptions"/> produce a byte-identical buffer.
/// </summary>
public interface ISystemMtChartRenderer
{
    /// <summary>Render <paramref name="figure"/> to PNG. Throws if options are invalid.</summary>
    byte[] RenderPng(ChartFigure figure, ChartRenderOptions options);
}
