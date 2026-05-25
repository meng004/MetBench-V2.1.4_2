using System.Collections.Generic;

namespace MetBench_BLL.SystemMT.V12Catalog.Runtime;

public sealed record SequenceValue(
    IReadOnlyList<double> Samples);
