using System.Text.Json.Serialization;

namespace MetBench_BLL.SystemMT.Catalog.Typed.Specs;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(DeterministicToleranceSpec), "DeterministicTolerance")]
[JsonDerivedType(typeof(FieldNormToleranceSpec), "FieldNormTolerance")]
[JsonDerivedType(typeof(StatisticalToleranceSpec), "StatisticalTolerance")]
public abstract record ToleranceSpec;

public sealed record DeterministicToleranceSpec(
    double Atol,
    double Rtol) : ToleranceSpec;

public sealed record FieldNormToleranceSpec(
    string Norm,
    double Atol,
    double Rtol) : ToleranceSpec;

public sealed record StatisticalToleranceSpec(
    double SigmaMultiplier) : ToleranceSpec;
