using System.Collections.Generic;
using MetBench_BLL.SystemMT.Catalog;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Catalog;

public sealed class CatalogDefinitionValidationTests
{
    private static MrTransformStepDefinition Step(string name = "ScaleField", string path = "/x") =>
        new() { TransformationName = name, TargetFieldPath = path };

    private static MrBindingDefinition GoodBinding(string mrId = "test-mr", string sutName = "test-sut") => new()
    {
        MrId = mrId,
        SutName = sutName,
        TransformationName = "ScaleField",
        AssertionTypeCode = "greater",
        WorkRootName = "MetBenchTest",
        TimeoutSeconds = 60,
        TransformSteps = new() { Step() },
    };

    // === M1: tolerance-required covers the full approx-class family ===

    [Theory]
    [InlineData("approx")]
    [InlineData("approx-invariant")]
    [InlineData("flux-pointwise-approx")]
    [InlineData("variance-ratio")]
    [InlineData("less-noise-aware")]
    [InlineData("greater-noise-aware")]
    [InlineData("cross-program-agree")]
    public void MrBindingDefinition_rejects_zero_tolerance_for_full_approx_class_family(string code)
    {
        var b = GoodBinding();
        b.AssertionTypeCode = code;

        var ex = Assert.Throws<CatalogValidationException>(() => b.Validate());
        Assert.Contains("tolerance", ex.Message, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MrBindingDefinition_accepts_approx_when_static_tolerance_set()
    {
        var b = GoodBinding();
        b.AssertionTypeCode = "approx";
        b.ToleranceRel = 1e-3;
        b.ToleranceAbs = 1e-6;

        b.Validate();
    }

    // === M4: NoiseAware approx with derived tolerance is accepted ===

    [Fact]
    public void MrBindingDefinition_accepts_approx_when_NoiseAware_with_positive_NoiseMultiplier()
    {
        var b = GoodBinding();
        b.AssertionTypeCode = "approx-invariant";
        b.NoiseAware = true;
        b.NoiseMultiplier = 3.0;
        b.ToleranceRel = 0;
        b.ToleranceAbs = 0;

        b.Validate();
    }

    [Fact]
    public void MrBindingDefinition_rejects_NoiseAware_when_NoiseMultiplier_not_positive()
    {
        var b = GoodBinding();
        b.AssertionTypeCode = "approx";
        b.NoiseAware = true;
        b.NoiseMultiplier = 0;

        var ex = Assert.Throws<CatalogValidationException>(() => b.Validate());
        Assert.Contains("NoiseMultiplier", ex.Message);
    }

    // === M2: NaN / negative / infinity tolerances rejected ===

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(-1e-3)]
    public void MrBindingDefinition_rejects_pathological_ToleranceRel(double value)
    {
        var b = GoodBinding();
        b.ToleranceRel = value;
        Assert.Throws<CatalogValidationException>(() => b.Validate());
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(-1e-9)]
    public void MrBindingDefinition_rejects_pathological_ToleranceAbs(double value)
    {
        var b = GoodBinding();
        b.ToleranceAbs = value;
        Assert.Throws<CatalogValidationException>(() => b.Validate());
    }

    // === M3: null-set mutable collections produce CatalogValidationException not NRE ===

    [Fact]
    public void MrBindingDefinition_rejects_null_TransformSteps()
    {
        var b = GoodBinding();
        b.TransformSteps = null!;

        var ex = Assert.Throws<CatalogValidationException>(() => b.Validate());
        Assert.Contains("TransformSteps", ex.Message);
    }

    [Fact]
    public void MrBindingDefinition_rejects_null_DefaultParameters()
    {
        var b = GoodBinding();
        b.DefaultParameters = null!;

        var ex = Assert.Throws<CatalogValidationException>(() => b.Validate());
        Assert.Contains("DefaultParameters", ex.Message);
    }

    [Fact]
    public void SystemMtCatalogDocument_rejects_null_Mrs()
    {
        var doc = new SystemMtCatalogDocument { SutName = "test", Mrs = null! };

        var ex = Assert.Throws<CatalogValidationException>(() => doc.Validate());
        Assert.Contains("Mrs", ex.Message);
    }

    [Fact]
    public void SystemMtCatalogDocument_rejects_null_entry_inside_Mrs()
    {
        var doc = new SystemMtCatalogDocument
        {
            SutName = "test",
            Mrs = new() { null! },
        };

        Assert.Throws<CatalogValidationException>(() => doc.Validate());
    }

    // === M8: aggregate enforces child SutName matches document SutName ===

    [Fact]
    public void SystemMtCatalogDocument_rejects_child_SutName_mismatch()
    {
        var doc = new SystemMtCatalogDocument
        {
            SutName = "decay-chain",
            Mrs = new() { GoodBinding("mr1", sutName: "openmoc") },
        };

        var ex = Assert.Throws<CatalogValidationException>(() => doc.Validate());
        Assert.Contains("does not match document SutName", ex.Message);
    }

    // === M9: TransformStep validation surfaces empty TransformationName / TargetFieldPath ===

    [Fact]
    public void MrTransformStepDefinition_rejects_empty_TransformationName()
    {
        var b = GoodBinding();
        b.TransformSteps[0] = new MrTransformStepDefinition { TransformationName = "", TargetFieldPath = "/x" };

        var ex = Assert.Throws<CatalogValidationException>(() => b.Validate());
        Assert.Contains("TransformationName", ex.Message);
    }

    [Fact]
    public void MrTransformStepDefinition_rejects_empty_TargetFieldPath()
    {
        var b = GoodBinding();
        b.TransformSteps[0] = new MrTransformStepDefinition { TransformationName = "ScaleField", TargetFieldPath = "" };

        var ex = Assert.Throws<CatalogValidationException>(() => b.Validate());
        Assert.Contains("TargetFieldPath", ex.Message);
    }

    [Fact]
    public void MrTransformStepDefinition_allows_empty_TargetFieldPath_for_single_step_recipe_MR()
    {
        // Mirrors the existing decay-chain-scale-initial pattern in LegacyCatalogFactory:
        // single step "ScaleInitial" + empty path + EquationKey="bateman" → recipe-based.
        var b = GoodBinding(mrId: "decay-chain-scale-initial", sutName: "decay-chain");
        b.EquationKey = "bateman";
        b.TransformSteps = new() { new MrTransformStepDefinition { TransformationName = "ScaleInitial", TargetFieldPath = "" } };

        b.Validate();
    }

    [Fact]
    public void MrTransformStepDefinition_still_rejects_empty_path_when_multi_step_with_EquationKey()
    {
        // Recipe escape applies ONLY to single-step bindings. Multi-step with recipe-shaped
        // empty path is ambiguous — fall back to strict path requirement.
        var b = GoodBinding();
        b.EquationKey = "bateman";
        b.TransformSteps = new()
        {
            Step("ScaleField", "/x"),
            new MrTransformStepDefinition { TransformationName = "ScaleField", TargetFieldPath = "" },
        };

        Assert.Throws<CatalogValidationException>(() => b.Validate());
    }

    [Fact]
    public void MrTransformStepDefinition_error_message_carries_owner_MrId_and_step_index()
    {
        var b = GoodBinding();
        b.TransformSteps = new()
        {
            Step(),
            new MrTransformStepDefinition { TransformationName = "", TargetFieldPath = "/y" },
        };

        var ex = Assert.Throws<CatalogValidationException>(() => b.Validate());
        Assert.Contains("test-mr", ex.Message);
        Assert.Contains("stepIndex=1", ex.Message);
    }

    // === M11: empty Mrs list rejected ===

    [Fact]
    public void SystemMtCatalogDocument_rejects_zero_MR_bindings()
    {
        var doc = new SystemMtCatalogDocument { SutName = "test" };

        var ex = Assert.Throws<CatalogValidationException>(() => doc.Validate());
        Assert.Contains("at least one MR binding", ex.Message);
    }

    // === M14: TransformationName required ===

    [Fact]
    public void MrBindingDefinition_requires_TransformationName()
    {
        var b = GoodBinding();
        b.TransformationName = "";

        var ex = Assert.Throws<CatalogValidationException>(() => b.Validate());
        Assert.Contains("TransformationName", ex.Message);
    }

    // === M17: TimeoutSeconds must be > 0 ===

    [Theory]
    [InlineData(0)]
    [InlineData(-30)]
    public void MrBindingDefinition_requires_positive_TimeoutSeconds(int value)
    {
        var b = GoodBinding();
        b.TimeoutSeconds = value;

        var ex = Assert.Throws<CatalogValidationException>(() => b.Validate());
        Assert.Contains("TimeoutSeconds", ex.Message);
    }

    // === M18: WorkRootName required ===

    [Fact]
    public void MrBindingDefinition_requires_WorkRootName()
    {
        var b = GoodBinding();
        b.WorkRootName = "";

        var ex = Assert.Throws<CatalogValidationException>(() => b.Validate());
        Assert.Contains("WorkRootName", ex.Message);
    }

    // === M16: PythonExecutableKind open vocabulary at validate-time (PR-1 T1 manifest-driven runtime envs) ===
    // The original M16 closed-vocabulary gate has been retired so manifest authors can declare
    // future runtime keys (e.g. "fenics", "fipy") without source edits. Fail-closed for unknown
    // non-system keys is now enforced at *resolution* time by
    // LauncherOptions.ResolvePythonExecutable, not at ProgramDefinition.Validate.

    [Fact]
    public void ProgramDefinition_rejects_blank_PythonExecutableKind()
    {
        var p = new ProgramDefinition
        {
            ProgramName = "openmoc",
            RunnerScriptRelativePath = "openmoc/openmoc_runner.py",
            PythonExecutableKind = "",
        };

        var ex = Assert.Throws<CatalogValidationException>(() => p.Validate());
        Assert.Contains("PythonExecutableKind", ex.Message);
    }

    [Fact]
    public void ProgramDefinition_accepts_unknown_PythonExecutableKind_for_future_runtime_keys()
    {
        // A future SUT family (e.g. fenics, fipy) declares its own runtime key in the manifest.
        // Validation must allow it; the launcher fails closed only at resolution time when
        // LauncherOptions.RuntimePythons does not configure the key.
        var p = new ProgramDefinition
        {
            ProgramName = "fenics-demo",
            RunnerScriptRelativePath = "fenics_demo/runner.py",
            PythonExecutableKind = "fenics",
        };

        var ex = Record.Exception(() => p.Validate());
        Assert.Null(ex);
    }

    [Theory]
    [InlineData("system")]
    [InlineData("openmoc")]
    [InlineData("openmc")]
    public void ProgramDefinition_accepts_all_canonical_PythonExecutableKinds(string kind)
    {
        var p = new ProgramDefinition
        {
            ProgramName = "test-prog",
            RunnerScriptRelativePath = "test/runner.py",
            PythonExecutableKind = kind,
        };

        p.Validate();
    }

    [Fact]
    public void ProgramDefinition_requires_RunnerScriptRelativePath()
    {
        var p = new ProgramDefinition { ProgramName = "test-prog", RunnerScriptRelativePath = "" };

        var ex = Assert.Throws<CatalogValidationException>(() => p.Validate());
        Assert.Contains("RunnerScriptRelativePath", ex.Message);
    }

    // === M26: AssertionTypeCode validated against the known closed set ===

    [Fact]
    public void MrBindingDefinition_rejects_unknown_AssertionTypeCode()
    {
        var b = GoodBinding();
        b.AssertionTypeCode = "approximate";  // typo of "approx"

        var ex = Assert.Throws<CatalogValidationException>(() => b.Validate());
        Assert.Contains("not a recognized code", ex.Message);
    }

    // === Existing pre-amendment invariants (kept) ===

    [Fact]
    public void SystemMtCatalogDocument_rejects_duplicate_mr_ids()
    {
        var doc = new SystemMtCatalogDocument
        {
            SutName = "test-sut",
            Mrs = new()
            {
                GoodBinding("dup"),
                new MrBindingDefinition
                {
                    MrId = "dup",
                    SutName = "test-sut",
                    TransformationName = "ScaleField",
                    AssertionTypeCode = "less",
                    WorkRootName = "MetBenchTest",
                    TimeoutSeconds = 60,
                    TransformSteps = new() { Step() },
                },
            },
        };

        var ex = Assert.Throws<CatalogValidationException>(() => doc.Validate());
        Assert.Contains("duplicate", ex.Message, System.StringComparison.OrdinalIgnoreCase);
        Assert.Contains("dup", ex.Message);
    }

    [Fact]
    public void MrBindingDefinition_requires_at_least_one_transform_step()
    {
        var b = GoodBinding();
        b.TransformSteps = new();

        Assert.Throws<CatalogValidationException>(() => b.Validate());
    }

    [Fact]
    public void MrBindingDefinition_requires_MrId()
    {
        var b = GoodBinding();
        b.MrId = "";

        var ex = Assert.Throws<CatalogValidationException>(() => b.Validate());
        Assert.Contains("MrId", ex.Message, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SystemMtCatalogDocument_requires_SutName()
    {
        var doc = new SystemMtCatalogDocument { SutName = "" };
        var ex = Assert.Throws<CatalogValidationException>(() => doc.Validate());
        Assert.Contains("SutName", ex.Message, System.StringComparison.OrdinalIgnoreCase);
    }

    // === Smoke: a well-formed multi-MR document validates clean ===

    [Fact]
    public void Well_formed_document_with_multiple_distinct_MRs_validates_clean()
    {
        var doc = new SystemMtCatalogDocument
        {
            SutName = "diffusion-1d",
            Program = new ProgramDefinition
            {
                ProgramName = "diffusion-1d",
                RunnerScriptRelativePath = "diffusion_1d/diffusion_1d_runner.py",
                PythonExecutableKind = "system",
            },
            Mrs = new()
            {
                new MrBindingDefinition
                {
                    MrId = "diffusion-source-linearity",
                    SutName = "diffusion-1d",
                    TransformationName = "ScaleSource",
                    AssertionTypeCode = "approx",
                    ToleranceRel = 1e-3,
                    ToleranceAbs = 1e-6,
                    WorkRootName = "MetBenchDiffusionSourceLin",
                    TimeoutSeconds = 30,
                    TransformSteps = new() { Step("ScaleField", "/source/strength") },
                },
                new MrBindingDefinition
                {
                    MrId = "diffusion-mesh-richardson",
                    SutName = "diffusion-1d",
                    TransformationName = "RefineMesh",
                    AssertionTypeCode = "variance-ratio",
                    ToleranceRel = 1e-2,
                    WorkRootName = "MetBenchDiffusionMeshRich",
                    TimeoutSeconds = 30,
                    TransformSteps = new() { Step("ScaleField", "/geometry/num_points") },
                },
            },
        };

        doc.Validate();
    }
}
