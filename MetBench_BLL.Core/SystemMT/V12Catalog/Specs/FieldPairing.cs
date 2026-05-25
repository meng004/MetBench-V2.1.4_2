using System.Text.Json.Serialization;

namespace MetBench_BLL.SystemMT.V12Catalog.Specs;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(IdentityFieldPairing), "Identity")]
public abstract record FieldPairing;

public sealed record IdentityFieldPairing() : FieldPairing;
