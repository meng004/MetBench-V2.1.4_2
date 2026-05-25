# Verification Semantics Convergence Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Converge System MT verification semantics onto the typed semantic catalog while keeping Method MT isolated and removing legacy System MT assertion runtime from production execution.

**Architecture:** The convergence runs as four small PRs. PR-A locks design and control documents only. PR-B renames `SystemMT/V12Catalog` to `SystemMT/Catalog/Typed` without behavior changes. PR-C routes System MT runtime assertions through typed predicates/kernels and treats legacy assertion codes only as migration input. PR-D adds architecture guards and removes obsolete production assertion classes once no production caller remains.

**Tech Stack:** C#/.NET, xUnit, YamlDotNet, MetBench System MT launcher/pipeline, typed semantic catalog validators and kernels.

---

## Preconditions

- Start every PR from latest `origin/main`.
- Verify a clean worktree before editing: `rtk git status --short --branch`.
- Do not change Method MT behavior in this plan.
- Do not change WPF or Windows-only code in PR-B, PR-C, or PR-D unless tests prove the launcher constructor or DI wiring requires it.
- PR-B may proceed after PR-A because it is a behavior-preserving naming migration.
- PR-C must not start until the status ledger explicitly allows assertion-runtime implementation. If ExecutionEvidence v2 is still open, PR-C must either wait for that design or prove in its PR checklist that result/evidence/reporting schema is unchanged.
- Run `rtk dotnet test MetBench_SystemMT.Tests --no-restore` before review for each PR.
- Use two-layer review before push: implementation self-review plus independent code-review pass.

## File Structure

### PR-A: Design Lock

- Modify: `docs/superpowers/specs/2026-05-25-verification-semantics-convergence-design.md`
- Modify: `docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md`
- Modify: `docs/status/current.md`
- Create: `docs/superpowers/plans/2026-05-25-verification-semantics-convergence-implementation-plan.md`

### PR-B: Naming Migration

- Move: `MetBench_BLL.Core/SystemMT/V12Catalog/` -> `MetBench_BLL.Core/SystemMT/Catalog/Typed/`
- Move: `MetBench_SystemMT.Tests/SystemMT/V12Catalog/` -> `MetBench_SystemMT.Tests/SystemMT/Catalog/Typed/`
- Move: `MetBench_SystemMT.Tests/TestAssets/V12Catalog/` -> `MetBench_SystemMT.Tests/TestAssets/SystemMT/Catalog/Typed/`
- Modify namespaces from `MetBench_BLL.SystemMT.V12Catalog.*` to `MetBench_BLL.SystemMT.Catalog.Typed.*`.
- Modify test namespaces from `MetBench_SystemMT.Tests.SystemMT.V12Catalog` to `MetBench_SystemMT.Tests.SystemMT.Catalog.Typed`.
- Modify: `MetBench_SystemMT.Tests/SystemMT/TestAssetPaths.cs`
- Create: `MetBench_SystemMT.Tests/Architecture/SemanticCatalogNamingBoundaryTests.cs`

### PR-C: Runtime Convergence

- Modify: `MetBench_BLL.Core/SystemMT/Pipeline/PipelineContext.cs`
- Modify: `MetBench_BLL.Core/SystemMT/Pipeline/SystemMtPipeline.cs`
- Modify: `MetBench_BLL.Core/SystemMT/Launcher/SystemMtLauncher.cs`
- Modify: `MetBench_BLL.Core/SystemMT/Catalog/MrBindingDefinition.cs`
- Create: `MetBench_BLL.Core/SystemMT/Catalog/Typed/Migration/LegacyAssertionPredicateMapper.cs`
- Create: `MetBench_BLL.Core/SystemMT/Catalog/Typed/Migration/TypedVerificationContextFactory.cs`
- Create: `MetBench_SystemMT.Tests/SystemMT/Catalog/Typed/LegacyAssertionPredicateMapperTests.cs`
- Create: `MetBench_SystemMT.Tests/SystemMT/Catalog/Typed/SystemMtPipelineTypedRuntimeTests.cs`

### PR-D: Guard And Cleanup

- Modify: `MetBench_SystemMT.Tests/Architecture/SemanticCatalogBoundaryTests.cs`
- Delete when production references are gone: `MetBench_BLL.Core/SystemMT/IMrAssertion.cs`
- Delete when production references are gone: `MetBench_BLL.Core/SystemMT/ApproxEqualAssertion.cs`
- Delete when production references are gone: `MetBench_BLL.Core/SystemMT/GreaterThanAssertion.cs`
- Delete when production references are gone: `MetBench_BLL.Core/SystemMT/LessThanAssertion.cs`
- Delete when production references are gone: `MetBench_BLL.Core/SystemMT/Assertions/AssertionEvaluator.cs`
- Keep only if recorder/reporting still needs the DTO shape: `MetBench_BLL.Core/SystemMT/Assertions/SystemMtAssertionResultV2.cs`
- Keep Method MT files untouched: `MetBench_BLL/MethodMT/MethodMtPipeline.cs`, `MetBench_BLL/MethodMT/Assertions/MethodAssertionEvaluator.cs`

---

## PR-A: Design Lock And Control Document Wiring

### Task A1: Lock The Design In Governance

**Files:**
- Create: `docs/superpowers/specs/2026-05-25-verification-semantics-convergence-design.md`
- Modify: `docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md`
- Modify: `docs/status/current.md`
- Create: `docs/superpowers/plans/2026-05-25-verification-semantics-convergence-implementation-plan.md`

- [ ] **Step 1: Verify design document states the accepted boundary**

Run:

```bash
rtk rg -n "Method MT stays isolated|System MT converges to Typed Semantic Catalog|Legacy System MT assertion runtime is migrated and removed|SystemMT/Catalog/Typed" docs/superpowers/specs/2026-05-25-verification-semantics-convergence-design.md
```

Expected: four matching lines.

- [ ] **Step 2: Verify the design is linked from current controls**

Run:

```bash
rtk rg -n "2026-05-25-verification-semantics-convergence-design.md|Verification semantics convergence \\| Design locked" docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md docs/status/current.md
```

Expected: matches in both files.

- [ ] **Step 3: Run document hygiene checks**

Run:

```bash
rtk rg -n "TB[D]|TO[D]O|implement[ ]later|fill[ ]in|待[定]|稍[后]|占[位]" docs/superpowers/specs/2026-05-25-verification-semantics-convergence-design.md docs/superpowers/plans/2026-05-25-verification-semantics-convergence-implementation-plan.md docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md docs/status/current.md
```

Expected: no matches.

- [ ] **Step 4: Verify no runtime files changed in PR-A**

Run:

```bash
rtk git diff --name-only
```

Expected: only files under `docs/`.

- [ ] **Step 5: Commit PR-A**

Run:

```bash
rtk git add docs/superpowers/specs/2026-05-25-verification-semantics-convergence-design.md docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md docs/status/current.md docs/superpowers/plans/2026-05-25-verification-semantics-convergence-implementation-plan.md
rtk git commit -m "docs(governance): lock verification semantics convergence"
```

Expected: one docs-only commit.

---

## PR-B: Rename V12Catalog To SystemMT/Catalog/Typed

### Task B1: Add Failing Naming Boundary Test

**Files:**
- Create: `MetBench_SystemMT.Tests/Architecture/SemanticCatalogNamingBoundaryTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
using System.IO;
using Xunit;

namespace MetBench_SystemMT.Tests.Architecture;

public sealed class SemanticCatalogNamingBoundaryTests
{
    private static string FindSolutionRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (dir.GetFiles("*.sln").Length > 0)
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        throw new InvalidOperationException("Cannot find solution root from " + AppContext.BaseDirectory);
    }

    [Fact]
    public void Typed_semantic_catalog_uses_permanent_catalog_typed_path()
    {
        var root = FindSolutionRoot();
        Assert.True(
            Directory.Exists(Path.Combine(root, "MetBench_BLL.Core", "SystemMT", "Catalog", "Typed")),
            "Typed semantic catalog must live under SystemMT/Catalog/Typed.");
        Assert.False(
            Directory.Exists(Path.Combine(root, "MetBench_BLL.Core", "SystemMT", "V12Catalog")),
            "SystemMT/V12Catalog is a phase name and must not remain a production path.");
    }

    [Fact]
    public void Production_and_tests_do_not_use_v12_catalog_namespace()
    {
        var root = FindSolutionRoot();
        var searchRoots = new[]
        {
            Path.Combine(root, "MetBench_BLL.Core", "SystemMT"),
            Path.Combine(root, "MetBench_SystemMT.Tests", "SystemMT"),
        };
        var violators = searchRoots
            .Where(Directory.Exists)
            .SelectMany(path => Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories))
            .Where(file => File.ReadAllText(file).Contains("V12Catalog", StringComparison.Ordinal))
            .Select(file => Path.GetRelativePath(root, file))
            .ToList();

        Assert.True(
            violators.Count == 0,
            "V12Catalog references remain in production/test semantic catalog code:\n" + string.Join("\n", violators));
    }
}
```

- [ ] **Step 2: Run focused test and confirm red**

Run:

```bash
rtk dotnet test MetBench_SystemMT.Tests --no-restore --filter FullyQualifiedName~SemanticCatalogNamingBoundaryTests
```

Expected: FAIL because `SystemMT/V12Catalog` still exists and namespaces still contain `V12Catalog`.

### Task B2: Move Production Typed Catalog

**Files:**
- Move: `MetBench_BLL.Core/SystemMT/V12Catalog/` -> `MetBench_BLL.Core/SystemMT/Catalog/Typed/`

- [ ] **Step 1: Move the directory**

Run:

```bash
rtk mkdir -p MetBench_BLL.Core/SystemMT/Catalog
rtk git mv MetBench_BLL.Core/SystemMT/V12Catalog MetBench_BLL.Core/SystemMT/Catalog/Typed
```

Expected: `MetBench_BLL.Core/SystemMT/Catalog/Typed/Specs/MrSpec.cs` exists.

- [ ] **Step 2: Replace production namespaces and using directives**

Run:

```bash
rtk perl -0pi -e 's/MetBench_BLL\\.SystemMT\\.V12Catalog/MetBench_BLL.SystemMT.Catalog.Typed/g' $(rtk rg -l "MetBench_BLL\\.SystemMT\\.V12Catalog" MetBench_BLL.Core MetBench_SystemMT.Tests)
```

Expected: no production C# reference to `MetBench_BLL.SystemMT.V12Catalog`.

- [ ] **Step 3: Rename phase-named serializer and schema classes only if public naming guard requires it**

Target file names after this step:

```text
MetBench_BLL.Core/SystemMT/Catalog/Typed/Serialization/TypedCatalogSerializer.cs
MetBench_BLL.Core/SystemMT/Catalog/Typed/Schema/TypedCatalogSchemaException.cs
MetBench_BLL.Core/SystemMT/Catalog/Typed/Schema/TypedCatalogStructuralSchema.cs
MetBench_BLL.Core/SystemMT/Catalog/Typed/Lint/TypedCatalogLintException.cs
MetBench_BLL.Core/SystemMT/Catalog/Typed/Lint/TypedCatalogAntiLegacyLinter.cs
```

Replace type names:

```text
V12CatalogSerializer -> TypedCatalogSerializer
V12CatalogSchemaException -> TypedCatalogSchemaException
V12CatalogStructuralSchema -> TypedCatalogStructuralSchema
V12CatalogLintException -> TypedCatalogLintException
V12CatalogAntiLegacyLinter -> TypedCatalogAntiLegacyLinter
```

Run focused compile after renaming:

```bash
rtk dotnet test MetBench_SystemMT.Tests --no-restore --filter FullyQualifiedName~V12CatalogSerializationTests
```

Expected: compile errors disappear after all references use the new names.

### Task B3: Move Tests And Assets

**Files:**
- Move: `MetBench_SystemMT.Tests/SystemMT/V12Catalog/` -> `MetBench_SystemMT.Tests/SystemMT/Catalog/Typed/`
- Move: `MetBench_SystemMT.Tests/TestAssets/V12Catalog/` -> `MetBench_SystemMT.Tests/TestAssets/SystemMT/Catalog/Typed/`
- Modify: `MetBench_SystemMT.Tests/SystemMT/TestAssetPaths.cs`

- [ ] **Step 1: Move test directories**

Run:

```bash
rtk mkdir -p MetBench_SystemMT.Tests/SystemMT/Catalog
rtk mkdir -p MetBench_SystemMT.Tests/TestAssets/SystemMT/Catalog
rtk git mv MetBench_SystemMT.Tests/SystemMT/V12Catalog MetBench_SystemMT.Tests/SystemMT/Catalog/Typed
rtk git mv MetBench_SystemMT.Tests/TestAssets/V12Catalog MetBench_SystemMT.Tests/TestAssets/SystemMT/Catalog/Typed
```

Expected: both moved directories exist at the new paths.

- [ ] **Step 2: Update test namespace and asset helper**

Replace test namespace:

```text
MetBench_SystemMT.Tests.SystemMT.V12Catalog
```

with:

```text
MetBench_SystemMT.Tests.SystemMT.Catalog.Typed
```

Update `MetBench_SystemMT.Tests/SystemMT/TestAssetPaths.cs` and replace all callers of the old phase-named asset helpers with the new typed names in the same PR:

```csharp
namespace MetBench_SystemMT.Tests.SystemMT;

internal static class TestAssetPaths
{
    public static string TypedCatalogRoot() => Path.Combine(AssetRoot(), "SystemMT", "Catalog", "Typed");
    public static string TypedMrSample => Path.Combine(TypedCatalogRoot(), "samples", "mr-sample.yaml");
    public static string TypedPropertySample => Path.Combine(TypedCatalogRoot(), "samples", "property-sample.yaml");

    public static string AssetRoot()
    {
        return Path.Combine(AppContext.BaseDirectory, "TestAssets");
    }

    public static string PythonExecutable()
    {
        var configured = Environment.GetEnvironmentVariable("METBENCH_TEST_PYTHON");
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured;
        }

        return OperatingSystem.IsWindows() ? "python" : "python3";
    }
}
```

- [ ] **Step 3: Run naming boundary test and full System MT tests**

Run:

```bash
rtk dotnet test MetBench_SystemMT.Tests --no-restore --filter FullyQualifiedName~SemanticCatalogNamingBoundaryTests
rtk dotnet test MetBench_SystemMT.Tests --no-restore
```

Expected: both pass.

- [ ] **Step 4: Commit PR-B**

Run:

```bash
rtk git add MetBench_BLL.Core/SystemMT/Catalog/Typed MetBench_SystemMT.Tests/SystemMT/Catalog/Typed MetBench_SystemMT.Tests/TestAssets/SystemMT/Catalog/Typed MetBench_SystemMT.Tests/SystemMT/TestAssetPaths.cs MetBench_SystemMT.Tests/Architecture/SemanticCatalogNamingBoundaryTests.cs
rtk git commit -m "refactor(systemmt): rename typed semantic catalog"
```

Expected: behavior-preserving rename commit.

---

## PR-C: Route System MT Runtime Through Typed Predicates

### Task C1: Add Legacy-Code To Typed-Predicate Mapper Tests

**Files:**
- Create: `MetBench_SystemMT.Tests/SystemMT/Catalog/Typed/LegacyAssertionPredicateMapperTests.cs`
- Create: `MetBench_BLL.Core/SystemMT/Catalog/Typed/Migration/LegacyAssertionPredicateMapper.cs`

- [ ] **Step 1: Write failing mapper tests**

```csharp
using MetBench_BLL.SystemMT.Catalog.Typed.Migration;
using MetBench_BLL.SystemMT.Catalog.Typed.Specs;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Catalog.Typed;

public sealed class LegacyAssertionPredicateMapperTests
{
    [Theory]
    [InlineData("less", "Less")]
    [InlineData("greater", "Greater")]
    [InlineData("approx", "Equal")]
    public void Scalar_assertion_codes_map_to_binary_comparison(string code, string expectedOperator)
    {
        var predicate = LegacyAssertionPredicateMapper.MapScalar(code, actualRole: "followup", expectedRole: "source", metric: "k_eff");

        var binary = Assert.IsType<BinaryComparisonPredicate>(predicate);
        Assert.Equal(expectedOperator, binary.Operator);
        Assert.Equal("followup", binary.LeftRole);
        Assert.Equal("source", binary.RightRole);
        Assert.Equal("k_eff", binary.Metric);
    }

    [Fact]
    public void Scaling_relation_maps_to_scaled_equality()
    {
        var predicate = LegacyAssertionPredicateMapper.MapScaling(
            actualRole: "followup",
            expectedRole: "source",
            metric: "delta_T",
            factor: new MrParameterRefExpression("factor"),
            exponent: 1.0);

        var scaled = Assert.IsType<ScaledEqualityPredicate>(predicate);
        Assert.Equal("followup", scaled.ActualRole);
        Assert.Equal("source", scaled.ReferenceRole);
        Assert.Equal("delta_T", scaled.Metric);
        Assert.Equal(1.0, scaled.Exponent);
    }

    [Fact]
    public void Unknown_legacy_code_is_rejected_fail_closed()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            LegacyAssertionPredicateMapper.MapScalar("string-switch-new-code", "followup", "source", "k_eff"));

        Assert.Contains("Unsupported legacy assertion code", ex.Message, StringComparison.Ordinal);
    }
}
```

- [ ] **Step 2: Run focused tests and confirm red**

Run:

```bash
rtk dotnet test MetBench_SystemMT.Tests --no-restore --filter FullyQualifiedName~LegacyAssertionPredicateMapperTests
```

Expected: FAIL because `LegacyAssertionPredicateMapper` does not exist.

- [ ] **Step 3: Implement minimal mapper**

```csharp
using MetBench_BLL.SystemMT.Catalog.Typed.Specs;

namespace MetBench_BLL.SystemMT.Catalog.Typed.Migration;

public static class LegacyAssertionPredicateMapper
{
    public static PredicateSpec MapScalar(
        string assertionTypeCode,
        string actualRole,
        string expectedRole,
        string metric)
    {
        var op = assertionTypeCode switch
        {
            "less" => "Less",
            "greater" => "Greater",
            "approx" => "Equal",
            _ => throw new ArgumentException(
                $"Unsupported legacy assertion code '{assertionTypeCode}'. Use Typed Semantic Catalog predicates.",
                nameof(assertionTypeCode)),
        };

        return new BinaryComparisonPredicate(
            PredicateId: $"{metric}-{op.ToLowerInvariant()}",
            LeftRole: actualRole,
            RightRole: expectedRole,
            Metric: metric,
            Operator: op);
    }

    public static PredicateSpec MapScaling(
        string actualRole,
        string expectedRole,
        string metric,
        ParameterExpression factor,
        double exponent)
    {
        return new ScaledEqualityPredicate(
            PredicateId: $"{metric}-scaled-equality",
            ActualRole: actualRole,
            ReferenceRole: expectedRole,
            Metric: metric,
            Factor: factor,
            Exponent: exponent);
    }
}
```

- [ ] **Step 4: Run mapper tests**

Run:

```bash
rtk dotnet test MetBench_SystemMT.Tests --no-restore --filter FullyQualifiedName~LegacyAssertionPredicateMapperTests
```

Expected: PASS.

### Task C2: Add Typed Runtime Pipeline Contract Test

**Files:**
- Create: `MetBench_SystemMT.Tests/SystemMT/Catalog/Typed/SystemMtPipelineTypedRuntimeTests.cs`
- Modify: `MetBench_BLL.Core/SystemMT/Pipeline/PipelineContext.cs`
- Modify: `MetBench_BLL.Core/SystemMT/Pipeline/SystemMtPipeline.cs`
- Create: `MetBench_BLL.Core/SystemMT/Catalog/Typed/Migration/TypedVerificationContextFactory.cs`

- [ ] **Step 1: Write failing runtime test**

```csharp
using MetBench_BLL.SystemMT.Catalog.Typed.Migration;
using MetBench_BLL.SystemMT.Catalog.Typed.Runtime;
using MetBench_BLL.SystemMT.Catalog.Typed.Specs;
using MetBench_BLL.SystemMT.Pipeline;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Catalog.Typed;

public sealed class SystemMtPipelineTypedRuntimeTests
{
    [Fact]
    public void Typed_runtime_factory_builds_context_from_scalar_outputs()
    {
        var spec = new MrSpec(
            "MrSpec",
            "diffusion-source-linearity",
            "Diffusion source linearity",
            null,
            new Dictionary<string, string>(),
            new Dictionary<string, ParameterExpression>
            {
                ["factor"] = new ConstantParameterExpression(2.0)
            },
            new Dictionary<string, RunRoleSpec>
            {
                ["source"] = new("Baseline"),
                ["followup"] = new("Followup")
            },
            new Dictionary<string, ProjectionSpec>
            {
                ["phi_max"] = new ScalarProjectionSpec("/results/phi_max")
            },
            new PredicateSpec[]
            {
                new ScaledEqualityPredicate("phi-scaled", "followup", "source", "phi_max", new MrParameterRefExpression("factor"), 1.0)
            },
            new DeterministicToleranceSpec(1e-6, 1e-4));

        var context = TypedVerificationContextFactory.FromScalarOutputs(
            spec,
            sourceScalars: new Dictionary<string, double> { ["phi_max"] = 3.0 },
            followupScalars: new Dictionary<string, double> { ["phi_max"] = 6.0 },
            parameterValues: new Dictionary<string, string> { ["factor"] = "2" });

        var result = new PredicateDispatcher().Dispatch(spec.Predicates.Single(), context);

        Assert.Equal(VerifyStatus.Passed, result.Status);
        Assert.True(result.Passed);
    }
}
```

- [ ] **Step 2: Run focused test and confirm red**

Run:

```bash
rtk dotnet test MetBench_SystemMT.Tests --no-restore --filter FullyQualifiedName~SystemMtPipelineTypedRuntimeTests
```

Expected: FAIL because `TypedVerificationContextFactory` does not exist.

- [ ] **Step 3: Implement typed context factory**

```csharp
using System.Globalization;
using MetBench_BLL.SystemMT.Catalog.Typed.Runtime;
using MetBench_BLL.SystemMT.Catalog.Typed.Specs;
using MetBench_BLL.SystemMT.Catalog.Typed.Validation;

namespace MetBench_BLL.SystemMT.Catalog.Typed.Migration;

public static class TypedVerificationContextFactory
{
    public static VerificationContext FromScalarOutputs(
        MrSpec spec,
        IReadOnlyDictionary<string, double> sourceScalars,
        IReadOnlyDictionary<string, double> followupScalars,
        IReadOnlyDictionary<string, string> parameterValues)
    {
        var validation = spec.Validate();
        if (!validation.IsValid)
        {
            throw new InvalidOperationException(
                "Typed semantic spec must validate before runtime execution: " +
                string.Join("; ", validation.Errors.Select(e => $"{e.Path}: {e.Message}")));
        }

        var roles = new Dictionary<string, RoleOutput>(StringComparer.Ordinal)
        {
            ["source"] = new RoleOutput("source", sourceScalars),
            ["followup"] = new RoleOutput("followup", followupScalars),
        };

        var inputs = parameterValues.ToDictionary(
            pair => pair.Key,
            pair => double.Parse(pair.Value, CultureInfo.InvariantCulture),
            StringComparer.Ordinal);

        return new VerificationContext(spec, roles, inputs);
    }
}
```

- [ ] **Step 4: Run focused tests**

Run:

```bash
rtk dotnet test MetBench_SystemMT.Tests --no-restore --filter FullyQualifiedName~SystemMtPipelineTypedRuntimeTests
```

Expected: PASS.

### Task C3: Replace Production AssertionEvaluator Dependency

**Files:**
- Modify: `MetBench_BLL.Core/SystemMT/Pipeline/SystemMtPipeline.cs`
- Modify: `MetBench_BLL.Core/SystemMT/Pipeline/PipelineContext.cs`
- Modify: `MetBench_BLL.Core/SystemMT/Launcher/SystemMtLauncher.cs`
- Modify tests under `MetBench_SystemMT.Tests/V2Pipeline/` and `MetBench_SystemMT.Tests/SystemMT/Launcher/`

- [ ] **Step 1: Add failing architecture assertion for production runtime**

Add to `MetBench_SystemMT.Tests/Architecture/SemanticCatalogBoundaryTests.cs`:

```csharp
[Fact]
public void System_mt_production_runtime_does_not_call_legacy_assertion_evaluator()
{
    var root = FindSolutionRoot();
    var productionRoot = Path.Combine(root, "MetBench_BLL.Core", "SystemMT");
    var allowed = new[]
    {
        Path.Combine("MetBench_BLL.Core", "SystemMT", "Catalog", "Typed", "Migration"),
    };

    var violators = Directory.GetFiles(productionRoot, "*.cs", SearchOption.AllDirectories)
        .Where(file => !allowed.Any(a => Path.GetRelativePath(root, file).StartsWith(a, StringComparison.OrdinalIgnoreCase)))
        .Where(file =>
        {
            var text = File.ReadAllText(file);
            return text.Contains("new AssertionEvaluator", StringComparison.Ordinal)
                || text.Contains("AssertionEvaluator.", StringComparison.Ordinal);
        })
        .Select(file => Path.GetRelativePath(root, file))
        .ToList();

    Assert.True(
        violators.Count == 0,
        "Production System MT runtime must use typed predicate dispatch, not AssertionEvaluator:\n" + string.Join("\n", violators));
}
```

Run:

```bash
rtk dotnet test MetBench_SystemMT.Tests --no-restore --filter FullyQualifiedName~System_mt_production_runtime_does_not_call_legacy_assertion_evaluator
```

Expected: FAIL because `SystemMtPipeline` constructs or receives `AssertionEvaluator`.

- [ ] **Step 2: Make `PipelineContext` carry a typed predicate path**

Extend `PipelineContext` with nullable typed fields without removing existing constructor parameters in this PR:

```csharp
public MrSpec? TypedSpec { get; init; }
public PredicateSpec? TypedPredicate { get; init; }
```

Expected: existing tests compile after adding `using MetBench_BLL.SystemMT.Catalog.Typed.Specs;`.

- [ ] **Step 3: Route assertion evaluation through `PredicateDispatcher`**

In `SystemMtPipeline.ExecuteAsync`, after source/followup parsed outputs are available:

```csharp
var typedSpec = ctx.TypedSpec;
var typedPredicate = ctx.TypedPredicate;
if (typedSpec is null || typedPredicate is null)
{
    typedPredicate = LegacyAssertionPredicateMapper.MapScalar(
        ctx.AssertionTypeCode,
        actualRole: "followup",
        expectedRole: "source",
        metric: ctx.ValueName);

    typedSpec = TypedSpecFactory.FromPipelineContext(ctx, typedPredicate);
}

var verificationContext = TypedVerificationContextFactory.FromScalarOutputs(
    typedSpec,
    sourceOutput.Values,
    followupOutput.Values,
    ctx.Parameters);

var verification = new PredicateDispatcher().Dispatch(typedPredicate, verificationContext);
var assertionResult = verification.Assertion;
```

If `TypedSpecFactory` is required, create `MetBench_BLL.Core/SystemMT/Catalog/Typed/Migration/TypedSpecFactory.cs` with a minimal scalar `MrSpec` containing roles `source` and `followup`, one scalar projection for `ctx.ValueName`, the passed predicate, and `ctx.Parameters` converted into `ConstantParameterExpression` entries where values parse as invariant-culture doubles.

- [ ] **Step 4: Preserve launcher results while changing assertion source**

Keep these existing output mappings in `SystemMtLauncher.RunAsync`:

```csharp
Passed: outcome.FinalStatus == PipelineStatus.Ok,
FailureReason: outcome.ErrorMessage ?? outcome.AssertionResult?.FailureReason ?? string.Empty,
ValueName: blueprint.Mr.ValueName,
SourceValue: outcome.AssertionResult?.SourceValue ?? 0.0,
FollowUpValue: outcome.AssertionResult?.FollowupValue ?? 0.0,
```

Expected: UI/API-facing `MrRunResult` shape remains unchanged.

- [ ] **Step 5: Run focused runtime and launcher tests**

Run:

```bash
rtk dotnet test MetBench_SystemMT.Tests --no-restore --filter FullyQualifiedName~SystemMtPipelineTests
rtk dotnet test MetBench_SystemMT.Tests --no-restore --filter FullyQualifiedName~SystemMtLauncherTests
rtk dotnet test MetBench_SystemMT.Tests --no-restore --filter FullyQualifiedName~SystemMtPipelineTypedRuntimeTests
```

Expected: all pass.

### Task C4: Run Full Tests And Commit PR-C

**Files:**
- All files modified in PR-C

- [ ] **Step 1: Verify no Method MT files changed**

Run:

```bash
rtk git diff --name-only | rtk rg "MetBench_BLL/MethodMT|MetBench_SystemMT.Tests/MethodMT"
```

Expected: no matches.

- [ ] **Step 2: Run full System MT tests**

Run:

```bash
rtk dotnet test MetBench_SystemMT.Tests --no-restore
```

Expected: all tests pass.

- [ ] **Step 3: Commit PR-C**

Run:

```bash
rtk git add MetBench_BLL.Core/SystemMT MetBench_SystemMT.Tests/SystemMT MetBench_SystemMT.Tests/V2Pipeline MetBench_SystemMT.Tests/Architecture
rtk git commit -m "feat(systemmt): route runtime assertions through typed predicates"
```

Expected: one implementation commit with tests.

---

## PR-D: Guard And Remove Legacy Production Assertion Runtime

### Task D1: Strengthen Architecture Guards

**Files:**
- Modify: `MetBench_SystemMT.Tests/Architecture/SemanticCatalogBoundaryTests.cs`

- [ ] **Step 1: Add guard for `IMrAssertion` production references**

```csharp
[Fact]
public void System_mt_production_runtime_does_not_depend_on_imrassertion()
{
    var root = FindSolutionRoot();
    var productionRoot = Path.Combine(root, "MetBench_BLL.Core", "SystemMT");
    var violators = Directory.GetFiles(productionRoot, "*.cs", SearchOption.AllDirectories)
        .Where(file =>
        {
            var rel = Path.GetRelativePath(root, file);
            if (rel.Contains(Path.Combine("Catalog", "Typed", "Migration"), StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            var text = File.ReadAllText(file);
            return text.Contains("IMrAssertion", StringComparison.Ordinal);
        })
        .Select(file => Path.GetRelativePath(root, file))
        .ToList();

    Assert.True(
        violators.Count == 0,
        "System MT production code must not depend on IMrAssertion:\n" + string.Join("\n", violators));
}
```

Run:

```bash
rtk dotnet test MetBench_SystemMT.Tests --no-restore --filter FullyQualifiedName~System_mt_production_runtime_does_not_depend_on_imrassertion
```

Expected: FAIL until old W1 assertion classes and callers are removed or explicitly moved to non-production historical fixtures.

- [ ] **Step 2: Add guard for new string-code dispatch**

```csharp
[Fact]
public void System_mt_production_runtime_does_not_add_string_assertion_dispatch()
{
    var root = FindSolutionRoot();
    var productionRoot = Path.Combine(root, "MetBench_BLL.Core", "SystemMT");
    var allowed = new[]
    {
        Path.Combine("MetBench_BLL.Core", "SystemMT", "Catalog", "Typed", "Migration"),
        Path.Combine("MetBench_BLL.Core", "SystemMT", "Catalog", "MrBindingDefinition.cs"),
    };
    var violators = Directory.GetFiles(productionRoot, "*.cs", SearchOption.AllDirectories)
        .Where(file => !allowed.Any(a => Path.GetRelativePath(root, file).EndsWith(a, StringComparison.OrdinalIgnoreCase)
            || Path.GetRelativePath(root, file).StartsWith(a, StringComparison.OrdinalIgnoreCase)))
        .Where(file => File.ReadAllText(file).Contains("AssertionTypeCodes.", StringComparison.Ordinal))
        .Select(file => Path.GetRelativePath(root, file))
        .ToList();

    Assert.True(
        violators.Count == 0,
        "String assertion-code dispatch must stay out of production runtime:\n" + string.Join("\n", violators));
}
```

Run:

```bash
rtk dotnet test MetBench_SystemMT.Tests --no-restore --filter FullyQualifiedName~System_mt_production_runtime_does_not_add_string_assertion_dispatch
```

Expected: FAIL until non-migration production dispatch references are removed.

### Task D2: Remove Obsolete Production Assertion Classes

**Files:**
- Delete: `MetBench_BLL.Core/SystemMT/IMrAssertion.cs`
- Delete: `MetBench_BLL.Core/SystemMT/ApproxEqualAssertion.cs`
- Delete: `MetBench_BLL.Core/SystemMT/GreaterThanAssertion.cs`
- Delete: `MetBench_BLL.Core/SystemMT/LessThanAssertion.cs`
- Delete or move to tests if needed: `MetBench_BLL.Core/SystemMT/SystemMtRunner.cs`
- Delete or confine to migration tests if no longer used: `MetBench_BLL.Core/SystemMT/Assertions/AssertionEvaluator.cs`
- Modify tests that explicitly validate obsolete W1 classes.

- [ ] **Step 1: List current production references**

Run:

```bash
rtk rg -n "IMrAssertion|ApproxEqualAssertion|GreaterThanAssertion|LessThanAssertion|new AssertionEvaluator|AssertionEvaluator\\(" MetBench_BLL.Core MetBench_SystemMT.Tests
```

Expected: references are limited to obsolete tests, old W1 runner, or migration tests before deletion.

- [ ] **Step 2: Delete obsolete W1 production classes once no production runtime depends on them**

Run:

```bash
rtk git rm MetBench_BLL.Core/SystemMT/IMrAssertion.cs
rtk git rm MetBench_BLL.Core/SystemMT/ApproxEqualAssertion.cs
rtk git rm MetBench_BLL.Core/SystemMT/GreaterThanAssertion.cs
rtk git rm MetBench_BLL.Core/SystemMT/LessThanAssertion.cs
```

Expected: compile errors identify obsolete tests to delete or rewrite against typed kernels.

- [ ] **Step 3: Replace obsolete assertion tests with typed kernel tests**

Delete these tests if their semantics are already covered by typed kernel tests:

```text
MetBench_SystemMT.Tests/SystemMT/ApproxEqualAssertionTests.cs
MetBench_SystemMT.Tests/SystemMT/GreaterThanAssertionTests.cs
MetBench_SystemMT.Tests/SystemMT/LessThanAssertionTests.cs
```

Coverage must remain in:

```text
MetBench_SystemMT.Tests/SystemMT/Catalog/Typed/BinaryComparisonKernelTests.cs
MetBench_SystemMT.Tests/SystemMT/Catalog/Typed/ScaledEqualityKernelTests.cs
MetBench_SystemMT.Tests/SystemMT/Catalog/Typed/V12GoldenFixtureTests.cs
```

- [ ] **Step 4: Run guard tests**

Run:

```bash
rtk dotnet test MetBench_SystemMT.Tests --no-restore --filter FullyQualifiedName~SemanticCatalogBoundaryTests
```

Expected: PASS.

### Task D3: Full Verification And Commit PR-D

**Files:**
- All files modified in PR-D

- [ ] **Step 1: Run full System MT test suite**

Run:

```bash
rtk dotnet test MetBench_SystemMT.Tests --no-restore
```

Expected: all tests pass.

- [ ] **Step 2: Confirm Method MT remains isolated**

Run:

```bash
rtk rg -n "IMrAssertion|MetBench_BLL\\.SystemMT\\.Catalog\\.Typed|PredicateDispatcher|IVerifierKernel" MetBench_BLL/MethodMT MetBench_SystemMT.Tests/MethodMT
```

Expected: no matches.

- [ ] **Step 3: Confirm legacy System MT assertion runtime is not production path**

Run:

```bash
rtk rg -n "IMrAssertion|new AssertionEvaluator|AssertionEvaluator\\(|AssertionTypeCodes\\." MetBench_BLL.Core/SystemMT
```

Expected: matches only in typed migration helpers, catalog validation constants that still parse imported legacy fields, or historical comments explicitly marked migration input.

- [ ] **Step 4: Commit PR-D**

Run:

```bash
rtk git add MetBench_BLL.Core/SystemMT MetBench_SystemMT.Tests/Architecture MetBench_SystemMT.Tests/SystemMT
rtk git commit -m "test(systemmt): guard typed semantic runtime boundary"
```

Expected: one cleanup/guard commit.

---

## Review Checklist For Every PR

- Scope stays inside the current PR.
- Method MT files are unchanged unless the PR explicitly says otherwise.
- No new production dependency on `IMrAssertion`.
- No new production call to `AssertionEvaluator`.
- No hidden string assertion dispatch outside typed migration helpers.
- Runtime validation remains fail-closed through typed spec validation.
- `ScaledEqualityPredicate` represents scaling relations such as `flw = k * src`.
- Equality semantics use explicit typed tolerance instead of implicit floating comparison.
- Test assets and docs use the permanent `SystemMT/Catalog/Typed` terminology after PR-B.
- Windows validation is not required unless the PR touches WPF, `MetBench_Client`, or Windows-only build wiring.

## Final Verification Gate

Run before each push:

```bash
rtk git diff --check
rtk dotnet test MetBench_SystemMT.Tests --no-restore
rtk rg -n "TB[D]|TO[D]O|implement[ ]later|fill[ ]in|待[定]|稍[后]|占[位]" docs/superpowers/specs/2026-05-25-verification-semantics-convergence-design.md docs/superpowers/plans/2026-05-25-verification-semantics-convergence-implementation-plan.md
```

Expected:

- `git diff --check` reports no whitespace errors.
- `dotnet test` passes.
- placeholder scan returns no matches.

## Execution Handoff

After PR-A is merged, PR-B may execute as the behavior-preserving naming migration. Do not start PR-C until PR-B is merged into `main` and `docs/status/current.md` allows assertion-runtime implementation under the current ExecutionEvidence v2 decision state. Do not start PR-D until PR-C is merged into `main`.
