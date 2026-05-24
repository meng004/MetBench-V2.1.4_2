using System.Text.Json.Serialization;

namespace MetBench_BLL.SystemMT.V12Catalog.Specs;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(DeterministicToleranceSpec), "DeterministicTolerance")]
public abstract record ToleranceSpec;

public sealed record DeterministicToleranceSpec(
    double Atol,
    double Rtol) : ToleranceSpec;
