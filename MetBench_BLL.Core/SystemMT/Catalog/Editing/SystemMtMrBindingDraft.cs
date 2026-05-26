using MetBench_BLL.SystemMT.Catalog;

namespace MetBench_BLL.SystemMT.Catalog.Editing;

public sealed record SystemMtMrBindingDraft
{
    public string MrId { get; set; } = string.Empty;
    public string SutName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string MrFamily { get; set; } = string.Empty;
    public string TransformationName { get; set; } = string.Empty;
    public string AssertionTypeCode { get; set; } = string.Empty;
    public string AssertionName { get; set; } = string.Empty;
    public string ValueName { get; set; } = string.Empty;
    public string EquationKey { get; set; } = string.Empty;
    public string Equation { get; set; } = string.Empty;
    public string ProgramType { get; set; } = string.Empty;
    public string MetaPattern { get; set; } = string.Empty;
    public string SourceLevel { get; set; } = string.Empty;
    public string FailureCorrelation { get; set; } = string.Empty;
    public string InputAdapterScriptRelativePath { get; set; } = string.Empty;
    public string OutputAdapterScriptRelativePath { get; set; } = string.Empty;
    public string SampleCaseRelativePath { get; set; } = string.Empty;
    public string WorkRootName { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
    public string Factor { get; set; } = string.Empty;
    public string TransformStepName { get; set; } = string.Empty;
    public string TransformTargetFieldPath { get; set; } = string.Empty;
    public double ToleranceRel { get; set; }
    public double ToleranceAbs { get; set; }
    public bool NoiseAware { get; set; }
    public double NoiseMultiplier { get; set; } = 3.0;

    public static SystemMtMrBindingDraft NewForSut(string sutId) => new()
    {
        SutName = sutId,
        TimeoutSeconds = 30,
        NoiseMultiplier = 3.0,
    };

    public static SystemMtMrBindingDraft FromBinding(MrBindingDefinition binding)
    {
        var firstStep = binding.TransformSteps.FirstOrDefault();
        binding.DefaultParameters.TryGetValue("factor", out var factor);

        return new SystemMtMrBindingDraft
        {
            MrId = binding.MrId,
            SutName = binding.SutName,
            DisplayName = binding.DisplayName,
            Description = binding.Description,
            MrFamily = binding.MrFamily,
            TransformationName = binding.TransformationName,
            AssertionTypeCode = binding.AssertionTypeCode,
            AssertionName = binding.AssertionName,
            ValueName = binding.ValueName,
            EquationKey = binding.EquationKey,
            Equation = binding.Equation,
            ProgramType = binding.ProgramType,
            MetaPattern = binding.MetaPattern,
            SourceLevel = binding.SourceLevel,
            FailureCorrelation = binding.FailureCorrelation,
            InputAdapterScriptRelativePath = binding.InputAdapterScriptRelativePath,
            OutputAdapterScriptRelativePath = binding.OutputAdapterScriptRelativePath,
            SampleCaseRelativePath = binding.SampleCaseRelativePath,
            WorkRootName = binding.WorkRootName,
            TimeoutSeconds = binding.TimeoutSeconds,
            Factor = factor ?? string.Empty,
            TransformStepName = firstStep?.TransformationName ?? binding.TransformationName,
            TransformTargetFieldPath = firstStep?.TargetFieldPath ?? string.Empty,
            ToleranceRel = binding.ToleranceRel,
            ToleranceAbs = binding.ToleranceAbs,
            NoiseAware = binding.NoiseAware,
            NoiseMultiplier = binding.NoiseMultiplier,
        };
    }

    public MrBindingDefinition ToBinding()
    {
        var parameters = new Dictionary<string, string>(StringComparer.Ordinal);
        if (!string.IsNullOrWhiteSpace(Factor))
            parameters["factor"] = Factor.Trim();

        return new MrBindingDefinition
        {
            MrId = MrId.Trim(),
            SutName = SutName.Trim(),
            DisplayName = DisplayName.Trim(),
            Description = Description.Trim(),
            MrFamily = MrFamily.Trim(),
            TransformationName = TransformationName.Trim(),
            AssertionTypeCode = AssertionTypeCode.Trim(),
            AssertionName = AssertionName.Trim(),
            ValueName = ValueName.Trim(),
            DefaultParameters = parameters,
            TransformSteps =
            [
                new MrTransformStepDefinition
                {
                    TransformationName = string.IsNullOrWhiteSpace(TransformStepName)
                        ? TransformationName.Trim()
                        : TransformStepName.Trim(),
                    TargetFieldPath = TransformTargetFieldPath.Trim(),
                }
            ],
            ToleranceRel = ToleranceRel,
            ToleranceAbs = ToleranceAbs,
            NoiseAware = NoiseAware,
            NoiseMultiplier = NoiseMultiplier,
            EquationKey = EquationKey.Trim(),
            Equation = Equation.Trim(),
            ProgramType = ProgramType.Trim(),
            MetaPattern = MetaPattern.Trim(),
            SourceLevel = SourceLevel.Trim(),
            FailureCorrelation = FailureCorrelation.Trim(),
            InputAdapterScriptRelativePath = InputAdapterScriptRelativePath.Trim(),
            OutputAdapterScriptRelativePath = OutputAdapterScriptRelativePath.Trim(),
            SampleCaseRelativePath = SampleCaseRelativePath.Trim(),
            WorkRootName = WorkRootName.Trim(),
            TimeoutSeconds = TimeoutSeconds,
        };
    }
}
