using System.Text.Json.Serialization;

namespace MetBench_BLL.SystemMT.V12Catalog.Specs;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(DeterministicToleranceSpec), "DeterministicTolerance")]
public abstract record ToleranceSpec(string Kind);

public sealed record DeterministicToleranceSpec(
    string Kind,
    double Atol,
    double Rtol) : ToleranceSpec(Kind);
