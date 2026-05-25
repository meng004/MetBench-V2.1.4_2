namespace MetBench_BLL.SystemMT.V12Catalog.Validation;

public sealed class ValidationRegistry
{
    public static ValidationRegistry Default { get; } = new();

    public SharedReferenceResolver ReferenceResolver { get; } = new();
    public ParameterExpressionResolver ParameterResolver { get; } = new();
    public ToleranceCompatibilityChecker ToleranceChecker { get; } = new();

    public BinaryComparisonPredicateValidator BinaryComparisonValidator { get; }
    public ScaledEqualityPredicateValidator ScaledEqualityValidator { get; }
    public CrossMethodPredicateValidator CrossMethodValidator { get; }
    public ErrorMonotonicPredicateValidator ErrorMonotonicValidator { get; }
    public VarianceRatioPredicateValidator VarianceRatioValidator { get; }
    public FieldEqualityPredicateValidator FieldEqualityValidator { get; }
    public FieldProportionalityPredicateValidator FieldProportionalityValidator { get; }
    public DerivedInvariantPredicateValidator DerivedInvariantValidator { get; }
    public BoundPropertyPredicateValidator BoundPropertyValidator { get; }

    public ValidationRegistry()
    {
        BinaryComparisonValidator = new BinaryComparisonPredicateValidator(ReferenceResolver);
        ScaledEqualityValidator = new ScaledEqualityPredicateValidator(ReferenceResolver, ParameterResolver);
        CrossMethodValidator = new CrossMethodPredicateValidator(ReferenceResolver);
        ErrorMonotonicValidator = new ErrorMonotonicPredicateValidator(ReferenceResolver);
        VarianceRatioValidator = new VarianceRatioPredicateValidator(ReferenceResolver, ParameterResolver);
        FieldEqualityValidator = new FieldEqualityPredicateValidator();
        FieldProportionalityValidator = new FieldProportionalityPredicateValidator();
        DerivedInvariantValidator = new DerivedInvariantPredicateValidator();
        BoundPropertyValidator = new BoundPropertyPredicateValidator(ReferenceResolver, ParameterResolver);
    }
}
