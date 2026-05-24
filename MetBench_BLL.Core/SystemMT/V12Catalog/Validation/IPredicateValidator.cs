namespace MetBench_BLL.SystemMT.V12Catalog.Validation;

public interface IPredicateValidator<in TPredicate, in TSpec>
{
    ValidationResult Validate(TPredicate predicate, TSpec spec);
}
