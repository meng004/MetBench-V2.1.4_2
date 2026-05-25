using System.Collections.Generic;

namespace MetBench_BLL.SystemMT.Catalog.Typed.Specs;

public sealed record FiveDTags(
    string EquationKey,
    string ProgramType,
    string Pattern,
    string SourceLevel,
    string FailureCorrelation)
{
    public static FiveDTags FromTags(IReadOnlyDictionary<string, string>? tags)
    {
        static string Get(IReadOnlyDictionary<string, string>? values, string key) =>
            values is not null && values.TryGetValue(key, out var value) ? value : string.Empty;

        return new FiveDTags(
            Get(tags, "equation_key"),
            Get(tags, "program_type"),
            Get(tags, "pattern"),
            Get(tags, "source_level"),
            Get(tags, "failure_correlation"));
    }
}
