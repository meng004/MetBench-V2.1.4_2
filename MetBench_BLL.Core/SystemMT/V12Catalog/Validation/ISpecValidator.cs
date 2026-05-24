namespace MetBench_BLL.SystemMT.V12Catalog.Validation;

public interface ISpecValidator<in TSpec>
{
    ValidationResult Validate(TSpec spec);
}
