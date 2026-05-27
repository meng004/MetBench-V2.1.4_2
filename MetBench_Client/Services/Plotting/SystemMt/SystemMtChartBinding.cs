using System.Collections.Generic;
using LiveChartsCore;
using LiveChartsCore.Kernel.Sketches;

namespace MetBench_Client.Services.Plotting.SystemMt;

/// <summary>
/// Plotter output bundle. XAML binds a single <see cref="SystemMtChartBinding"/>
/// object on a <c>CartesianChart</c> rather than four separate properties to
/// avoid multi-property update race conditions when the source figure swaps.
/// </summary>
public sealed record SystemMtChartBinding(
    string Title,
    IReadOnlyList<ISeries> Series,
    IReadOnlyList<ICartesianAxis> XAxes,
    IReadOnlyList<ICartesianAxis> YAxes);
