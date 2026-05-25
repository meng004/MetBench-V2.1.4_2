namespace MetBench_BLL.SystemMT.Catalog.Typed.Validation;

public interface IPropertyPredicateValidator<in TPredicate, in TSpec>
{
    ValidationResult Validate(TPredicate predicate, TSpec spec);
}
