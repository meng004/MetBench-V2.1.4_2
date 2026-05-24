using MetBench_BLL.SystemMT.V12Catalog.Specs;

namespace MetBench_BLL.SystemMT.V12Catalog.Validation;

public sealed class ParameterExpressionResolver
{
    public ValidationResult Validate(ParameterExpression expression, string path, MrSpec? mrSpec = null) =>
        expression switch
        {
            ConstantParameterExpression => ValidationResult.Valid,
            MrParameterRefExpression parameterRef when mrSpec is not null && mrSpec.Parameters is not null && mrSpec.Parameters.ContainsKey(parameterRef.Name)
                => ValidationResult.Valid,
            MrParameterRefExpression parameterRef => ValidationResult.Invalid(
                new ValidationError(path, $"Unknown MR parameter '{parameterRef.Name}'.")),
            _ => ValidationResult.Invalid(new ValidationError(path, $"Unsupported parameter expression type '{expression.GetType().Name}'."))
        };
}
