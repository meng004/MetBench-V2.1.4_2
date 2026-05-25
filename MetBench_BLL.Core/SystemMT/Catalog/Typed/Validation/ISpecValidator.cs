namespace MetBench_BLL.SystemMT.Catalog.Typed.Validation;

public interface ISpecValidator<in TSpec>
{
    ValidationResult Validate(TSpec spec);
}
