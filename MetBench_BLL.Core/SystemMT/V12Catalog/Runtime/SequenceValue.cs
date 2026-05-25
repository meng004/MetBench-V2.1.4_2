using System.Collections.Generic;
using System.Linq;

namespace MetBench_BLL.SystemMT.V12Catalog.Runtime;

public sealed record SequenceValue(
    IReadOnlyList<double> Samples,
    IReadOnlyList<double>? XValues = null)
{
    public IReadOnlyList<double> EffectiveXValues =>
        XValues ?? Enumerable.Range(0, Samples.Count).Select(static i => (double)i).ToArray();
}
