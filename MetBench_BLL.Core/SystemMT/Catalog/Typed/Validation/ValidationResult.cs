using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MetBench_BLL.SystemMT.Catalog.Typed.Validation;

public sealed record ValidationResult(bool IsValid, IReadOnlyList<ValidationError> Errors)
{
    public static ValidationResult Valid { get; } =
        new(true, ReadOnlyCollection<ValidationError>.Empty);

    public static ValidationResult Invalid(params ValidationError[] errors) =>
        new(false, errors.ToList());

    public static ValidationResult Invalid(IEnumerable<ValidationError> errors)
    {
        var list = errors.ToList();
        return list.Count == 0 ? Valid : new ValidationResult(false, list);
    }

    public ValidationResult Merge(ValidationResult other)
    {
        if (IsValid && other.IsValid)
        {
            return Valid;
        }

        return Invalid(Errors.Concat(other.Errors));
    }
}
