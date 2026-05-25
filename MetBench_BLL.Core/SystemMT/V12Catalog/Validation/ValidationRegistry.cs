namespace MetBench_BLL.SystemMT.V12Catalog.Validation;

public sealed class ValidationRegistry
{
    public static ValidationRegistry Default { get; } = new();

    public SharedReferenceResolver ReferenceResolver { get; } = new();
    public ParameterExpressionResolver ParameterResolver { get; } = new();
    public ToleranceCompatibilityChecker ToleranceChecker { get; } = new();

    public BinaryComparisonPredicateValidator BinaryComparisonValidator { get; }
    public ScaledEqualityPredicateValidator ScaledEqualityValidator { get; }
    public BoundPropertyPredicateValidator BoundPropertyValidator { get; }

    public ValidationRegistry()
    {
        BinaryComparisonValidator = new BinaryComparisonPredicateValidator(ReferenceResolver);
        ScaledEqualityValidator = new ScaledEqualityPredicateValidator(ReferenceResolver, ParameterResolver);
        BoundPropertyValidator = new BoundPropertyPredicateValidator(ReferenceResolver, ParameterResolver);
    }
}
