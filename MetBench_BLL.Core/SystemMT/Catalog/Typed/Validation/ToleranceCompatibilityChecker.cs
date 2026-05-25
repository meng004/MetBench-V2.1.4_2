using MetBench_BLL.SystemMT.Catalog.Typed.Specs;

namespace MetBench_BLL.SystemMT.Catalog.Typed.Validation;

public sealed class ToleranceCompatibilityChecker
{
    public ValidationResult Validate(ToleranceSpec? tolerance, string path) =>
        tolerance switch
        {
            null => ValidationResult.Valid,
            DeterministicToleranceSpec => ValidationResult.Valid,
            _ => ValidationResult.Invalid(new ValidationError(path, $"Unsupported tolerance type '{tolerance.GetType().Name}'."))
        };
}
