using System.Collections.Generic;

namespace MetBench_BLL.SystemMT.Catalog;

public sealed class MrTransformStepDefinition
{
    public string TransformationName { get; set; } = string.Empty;
    public string TargetFieldPath { get; set; } = string.Empty;
    public Dictionary<string, string>? StepParameters { get; set; }

    /// <summary>
    /// Validate the step in the context of its owning binding.
    /// When <paramref name="allowEmptyPath"/> is true (single-step recipe-based MR — see
    /// <c>MrBindingDefinition.Validate</c>), an empty <see cref="TargetFieldPath"/> is
    /// permitted because the recipe in <c>EquationFunctionRegistry</c> manages paths internally
    /// (e.g. decay-chain-scale-initial uses TransformationName="ScaleInitial" + path="").
    /// </summary>
    public void Validate(string ownerMrId, int stepIndex, bool allowEmptyPath = false)
    {
        if (string.IsNullOrWhiteSpace(TransformationName))
            throw new CatalogValidationException(
                $"MrTransformStepDefinition (mrId='{ownerMrId}', stepIndex={stepIndex}) requires non-empty TransformationName");
        if (!allowEmptyPath && string.IsNullOrWhiteSpace(TargetFieldPath))
            throw new CatalogValidationException(
                $"MrTransformStepDefinition (mrId='{ownerMrId}', stepIndex={stepIndex}) requires non-empty TargetFieldPath");
    }
}
