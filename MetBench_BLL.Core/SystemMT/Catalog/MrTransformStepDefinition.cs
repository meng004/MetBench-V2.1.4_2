using System.Collections.Generic;

namespace MetBench_BLL.SystemMT.Catalog;

public sealed class MrTransformStepDefinition
{
    public string TransformationName { get; set; } = string.Empty;
    public string TargetFieldPath { get; set; } = string.Empty;
    public Dictionary<string, string>? StepParameters { get; set; }

    public void Validate(string ownerMrId, int stepIndex)
    {
        if (string.IsNullOrWhiteSpace(TransformationName))
            throw new CatalogValidationException(
                $"MrTransformStepDefinition (mrId='{ownerMrId}', stepIndex={stepIndex}) requires non-empty TransformationName");
        if (string.IsNullOrWhiteSpace(TargetFieldPath))
            throw new CatalogValidationException(
                $"MrTransformStepDefinition (mrId='{ownerMrId}', stepIndex={stepIndex}) requires non-empty TargetFieldPath");
    }
}
