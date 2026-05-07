namespace MetBench_BLL.SystemMT;

public sealed record ParsedOutput(
    IReadOnlyDictionary<string, double> Values,
    IReadOnlyDictionary<string, string> Metadata);
