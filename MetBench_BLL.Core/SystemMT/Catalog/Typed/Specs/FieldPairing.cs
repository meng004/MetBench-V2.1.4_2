using System.Text.Json.Serialization;

namespace MetBench_BLL.SystemMT.Catalog.Typed.Specs;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(IdentityFieldPairing), "Identity")]
[JsonDerivedType(typeof(TransposeFieldPairing), "Transpose")]
public abstract record FieldPairing;

public sealed record IdentityFieldPairing() : FieldPairing;

public sealed record TransposeFieldPairing() : FieldPairing;
