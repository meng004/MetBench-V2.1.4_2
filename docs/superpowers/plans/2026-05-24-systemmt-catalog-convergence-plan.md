# SystemMT Catalog Convergence Implementation Plan

> **Spec**: [`docs/superpowers/specs/2026-05-24-systemmt-catalog-convergence-design.md`](../specs/2026-05-24-systemmt-catalog-convergence-design.md)
> **Branch base**: `main @ 8d2703f` (post PR #88 + PR #89 merge)
> **Baseline test count**: 878 pass / 0 fail (PR #88 + PR #89; PR #89 added G-X2-LatexGuard 2 tests)
> **For agentic workers**: use TDD per task (red → impl → green → commit).

**Goal:** Move runnable System-MT catalog definitions out of `SystemMtLauncher` into provider-backed manifest data, then extend persistence for Stage 8 metadata/evidence without breaking current launcher behavior.

**Architecture:** Keep `ISystemMtLauncher` as the stable façade. First introduce catalog definition models plus provider indirection and migrate the current runnable catalog by equivalence; only after parity is locked, extend persistence with metadata snapshots and sample-level execution evidence. Keep summary-facing result records separate from evidence-heavy models.

**Tech Stack:** .NET 8, C#, LiteDB, xUnit, Reqnroll, `System.Text.Json`

---

## Plan Revision Notes (v2)

Plan v1 had 5 critical errors fixed here:

1. **Class name**: `SystemMtMrLauncher` → `SystemMtLauncher` (PR #58 W12 rename).
2. **SUT/MR count**: 6 manifests/8 MRs → **9 manifests / 17 MRs** (PR #88 S8-P1..P4 added subchannel_1d + diffusion_1d; G-09 added projectile; S8-P1..P2 added 4 MRs on existing SUTs).
3. **Launcher constructor signature**: actual is `(LauncherOptions, ISystemMtPipeline, SystemMtExecutionRecorder, IAnomalyService, AnomalySeverityThresholds?)` — not the simplified `(LauncherOptions, ISystemMtResultRepository, IAnomalyService, ...)` plan v1 showed.
4. **`LegacyCatalogFactory.Build()`** referenced in plan v1 does not exist. Task 2 now includes the refactoring step: extract `SystemMtLauncher.BuildBlueprints()` (private) into an internal static method consumable by `HardcodedMrCatalogProvider`.
5. **V3 schema integration**: Task 5/6 now explicitly references `MetamorphicRelationV3.IdV3` (Guid) on `ExecutionMetadataSnapshot.V3MrIdRef`, and Task 6 wires the V3 repo into the pipeline write path (avoiding a second migration in a later PR).

Medium improvements:

6. catalog.json sample shows multi-MR layout (openmoc has 2 MRs, decay-chain has 3).
7. Manifest entries carry 5D-tag fields (`equation_key`, `equation`, `program_type`, `meta_pattern`, `source_level`, `failure_correlation`).
8. CompositeTransform multi-step MRs (damped-oscillator 2-step, decay-chain `ScaleInitial` Recipe) representable via `transform_steps` array.
9. Shell commands use plain `dotnet test` / `git` (no `rtk` wrapper — that is foreign to this repo).

---

## File Structure

### Existing files to modify

- `MetBench_BLL.Core/SystemMT/Launcher/SystemMtLauncher.cs`
  Responsibility: launcher façade and runtime orchestration. Extract `BuildBlueprints()` for provider consumption; replace direct ownership with `IMrCatalogProvider` dependency.
- `MetBench_Client/App.xaml.cs`
  Responsibility: DI registration for launcher + new provider + manifest path resolution.
- `MetBench_BLL.Core/SystemMT/Persistence/SystemMtResultRecord.cs`
  Responsibility: persisted summary projection of System-MT runs (kept summary-only; evidence moved to new models).
- `MetBench_DAL/LiteDbSystemMtResultRepository.cs`
  Responsibility: LiteDB collection/index/migration; add `execution_evidence` collection.
- `MetBench_BLL.Core/SystemMT/Pipeline/SystemMtPipeline.cs` and `SystemMtExecutionRecorder.cs`
  Responsibility: write `ExecutionEvidence` alongside summary on each run.
- `MetBench_SystemMT.Tests/SystemMT/Launcher/SystemMtLauncherTests.cs`
- `MetBench_SystemMT.Tests/SystemMT/Launcher/LauncherCatalogV2ImporterTests.cs`
- `MetBench_SystemMT.Tests/SystemMT/Launcher/LauncherEndToEndOdeTests.cs`
- `docs/PROJECT-STRUCTURE.md`
- `README.md`
- `AGENTS.md`
- `CLAUDE.md` (§10 BLL.Core namespaces table — add `Catalog` subnamespace)

### New files to create

**Catalog layer**:

- `MetBench_BLL.Core/SystemMT/Catalog/ProgramDefinition.cs`
- `MetBench_BLL.Core/SystemMT/Catalog/MrDefinition.cs`
- `MetBench_BLL.Core/SystemMT/Catalog/MrBindingDefinition.cs`
- `MetBench_BLL.Core/SystemMT/Catalog/MrTransformStepDefinition.cs`
- `MetBench_BLL.Core/SystemMT/Catalog/SystemMtCatalogDocument.cs`
- `MetBench_BLL.Core/SystemMT/Catalog/IMrCatalogProvider.cs`
- `MetBench_BLL.Core/SystemMT/Catalog/HardcodedMrCatalogProvider.cs`
- `MetBench_BLL.Core/SystemMT/Catalog/ManifestMrCatalogProvider.cs`
- `MetBench_BLL.Core/SystemMT/Catalog/CatalogValidationException.cs`

**Evidence layer**:

- `MetBench_BLL.Core/SystemMT/Persistence/ExecutionEvidence.cs`
- `MetBench_BLL.Core/SystemMT/Persistence/ExecutionSampleTrace.cs`
- `MetBench_BLL.Core/SystemMT/Persistence/ExecutionMetadataSnapshot.cs`
- `MetBench_BLL.Core/SystemMT/Persistence/IExecutionEvidenceRepository.cs`
- `MetBench_DAL/LiteDbExecutionEvidenceRepository.cs`

**Manifests** (9 SUT × catalog.json):

- `SUT/openmoc/catalog.json` — 2 MRs (nu-sigma-f / sigma-a)
- `SUT/openmc/catalog.json` — 2 MRs (nu-sigma-f / sigma-a)
- `SUT/heat_equation/catalog.json` — 3 MRs (amplitude / timestep-convergence / alpha-monotonic)
- `SUT/decay_chain/catalog.json` — 3 MRs (scale-initial / mass-conservation / timestep-cauchy)
- `SUT/damped_oscillator/catalog.json` — 1 MR (scale-state, 2-step composite)
- `SUT/lotka_volterra/catalog.json` — 1 MR (scale-gamma)
- `SUT/projectile/catalog.json` — 1 MR (scale-v0)
- `SUT/subchannel_1d/catalog.json` — 2 MRs (flow-temp-monotone / heat-flux-linearity)
- `SUT/diffusion_1d/catalog.json` — 2 MRs (source-linearity / mesh-richardson)

Total: 17 MRs across 9 SUTs (matches launcher BuildBlueprints output).

**Schema**:

- `docs/design/mr-catalog-manifest.schema.json`

**Tests**:

- `MetBench_SystemMT.Tests/SystemMT/Catalog/CatalogDefinitionValidationTests.cs`
- `MetBench_SystemMT.Tests/SystemMT/Catalog/HardcodedMrCatalogProviderTests.cs`
- `MetBench_SystemMT.Tests/SystemMT/Catalog/ManifestMrCatalogProviderTests.cs`
- `MetBench_SystemMT.Tests/SystemMT/Catalog/CatalogParityTests.cs`
- `MetBench_SystemMT.Tests/SystemMT/Catalog/HardcodedProviderObsoleteGuardTests.cs`
- `MetBench_SystemMT.Tests/SystemMT/Persistence/ExecutionMetadataSnapshotTests.cs`
- `MetBench_SystemMT.Tests/SystemMT/Persistence/ExecutionEvidenceRoundtripTests.cs`
- `MetBench_SystemMT.Tests/SystemMT/Pipeline/ExecutionEvidenceWriteThroughTests.cs`

---

## Phase Mapping (spec §6 → tasks)

| Spec Phase | Plan Tasks | PR slice |
|---|---|---|
| Phase A: provider boundary | Task 1 | PR-A |
| Phase B: externalize bindings | Task 2 | PR-A |
| Phase C: switch launcher | Task 3 + Task 4 | PR-A |
| Phase D: evidence persistence + V3 wiring | Task 5 + Task 6 | PR-B |
| Phase E: remove obsolete hardcoded | Task 7 (E removal) + Task 8 (docs) | PR-C (after PR-B parity stable) |

PR colocate: Task 1..4 → PR-A (~30 files); Task 5..6 → PR-B (~25 files); Task 7..8 → PR-C (~15 files).

---

### Task 1: Catalog Definition Models + Provider Boundary

Spec §5.1 + §5.2 (interface only). No launcher behavior change yet.

**Files (create)**:

- `MetBench_BLL.Core/SystemMT/Catalog/ProgramDefinition.cs`
- `MetBench_BLL.Core/SystemMT/Catalog/MrDefinition.cs`
- `MetBench_BLL.Core/SystemMT/Catalog/MrBindingDefinition.cs`
- `MetBench_BLL.Core/SystemMT/Catalog/MrTransformStepDefinition.cs`
- `MetBench_BLL.Core/SystemMT/Catalog/SystemMtCatalogDocument.cs`
- `MetBench_BLL.Core/SystemMT/Catalog/IMrCatalogProvider.cs`
- `MetBench_BLL.Core/SystemMT/Catalog/CatalogValidationException.cs`
- `MetBench_SystemMT.Tests/SystemMT/Catalog/CatalogDefinitionValidationTests.cs`

- [ ] **Step 1: Write failing definition validation tests**

```csharp
using MetBench_BLL.SystemMT.Catalog;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Catalog;

public sealed class CatalogDefinitionValidationTests
{
    [Fact]
    public void MrBindingDefinition_requires_tolerance_for_approx_assertion()
    {
        var binding = new MrBindingDefinition
        {
            MrId = "diffusion-mesh-richardson",
            SutName = "diffusion-1d",
            AssertionTypeCode = "approx",
            TransformSteps = new()
            {
                new MrTransformStepDefinition
                {
                    TransformationName = "ScaleField",
                    TargetFieldPath = "/geometry/num_points",
                },
            },
            DefaultParameters = new() { ["factor"] = "2" },
        };

        var ex = Assert.Throws<CatalogValidationException>(() => binding.Validate());
        Assert.Contains("tolerance", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MrBindingDefinition_accepts_approx_when_tolerance_set()
    {
        var binding = new MrBindingDefinition
        {
            MrId = "diffusion-mesh-richardson",
            SutName = "diffusion-1d",
            AssertionTypeCode = "approx",
            ToleranceRel = 1e-3,
            ToleranceAbs = 1e-6,
            TransformSteps = new()
            {
                new MrTransformStepDefinition
                {
                    TransformationName = "ScaleField",
                    TargetFieldPath = "/geometry/num_points",
                },
            },
            DefaultParameters = new() { ["factor"] = "2" },
        };

        binding.Validate(); // does not throw
    }

    [Fact]
    public void SystemMtCatalogDocument_rejects_duplicate_mr_ids()
    {
        var doc = new SystemMtCatalogDocument
        {
            SutName = "test",
            Mrs = new()
            {
                new MrBindingDefinition { MrId = "dup", SutName = "test", AssertionTypeCode = "greater" },
                new MrBindingDefinition { MrId = "dup", SutName = "test", AssertionTypeCode = "less" },
            },
        };

        var ex = Assert.Throws<CatalogValidationException>(() => doc.Validate());
        Assert.Contains("duplicate", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MrBindingDefinition_requires_at_least_one_transform_step()
    {
        var binding = new MrBindingDefinition
        {
            MrId = "no-steps", SutName = "test", AssertionTypeCode = "greater",
        };

        Assert.Throws<CatalogValidationException>(() => binding.Validate());
    }
}
```

- [ ] **Step 2: Run to verify red**

```bash
dotnet test MetBench_SystemMT.Tests --filter CatalogDefinitionValidationTests
```

Expected: compile error / fail (types do not exist).

- [ ] **Step 3: Implement minimal definition models + validation**

Key types:

```csharp
namespace MetBench_BLL.SystemMT.Catalog;

public sealed class MrBindingDefinition
{
    public string MrId { get; set; } = string.Empty;
    public string SutName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string MrFamily { get; set; } = string.Empty;

    // Execution contract
    public string AssertionTypeCode { get; set; } = string.Empty;  // "greater" / "less" / "approx" / ...
    public string AssertionName { get; set; } = string.Empty;       // "GreaterThan" / "LessThan" / "ApproxEqual" (display)
    public string ValueName { get; set; } = string.Empty;
    public Dictionary<string, string> DefaultParameters { get; set; } = new();
    public List<MrTransformStepDefinition> TransformSteps { get; set; } = new();

    // Tolerance / noise (Spec §5.1 — PR #88 review-fix-1 lesson: approx needs explicit tolerance)
    public double ToleranceRel { get; set; }
    public double ToleranceAbs { get; set; }
    public bool NoiseAware { get; set; }
    public double NoiseMultiplier { get; set; } = 3.0;

    // 5D tags (Spec §5.1 — Stage 8 metadata)
    public string EquationKey { get; set; } = string.Empty;
    public string Equation { get; set; } = string.Empty;            // "Boltzmann" / "Bateman" / ...
    public string ProgramType { get; set; } = string.Empty;         // "Num" / "MC" / "Analytic" / ...
    public string MetaPattern { get; set; } = string.Empty;         // "Mono" / "Inv" / "Conv" / ...
    public string SourceLevel { get; set; } = string.Empty;         // "Manual" / "MetaPrompt" / ...
    public string FailureCorrelation { get; set; } = string.Empty;  // "None" / "RealBug" / ...

    // SUT integration
    public string SampleCaseRelativePath { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 60;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(MrId))
            throw new CatalogValidationException("MrBindingDefinition.MrId is required");
        if (string.IsNullOrWhiteSpace(SutName))
            throw new CatalogValidationException("MrBindingDefinition.SutName is required");
        if (string.IsNullOrWhiteSpace(AssertionTypeCode))
            throw new CatalogValidationException("MrBindingDefinition.AssertionTypeCode is required");
        if (TransformSteps.Count == 0)
            throw new CatalogValidationException(
                $"MrBindingDefinition '{MrId}' must declare at least one transform step");

        // PR #88 review-fix-1 invariant: approx-class assertions require non-zero tolerance,
        // otherwise BeApproximately(src, 0) degrades to bit-exact equality.
        if (string.Equals(AssertionTypeCode, "approx", StringComparison.Ordinal)
            && ToleranceRel == 0 && ToleranceAbs == 0)
        {
            throw new CatalogValidationException(
                $"MrBindingDefinition '{MrId}' uses 'approx' assertion but has zero tolerance " +
                "(ToleranceRel == 0 && ToleranceAbs == 0); approx assertions require explicit tolerance.");
        }
    }
}

public sealed class MrTransformStepDefinition
{
    public string TransformationName { get; set; } = string.Empty;
    public string TargetFieldPath { get; set; } = string.Empty;
    public Dictionary<string, string>? StepParameters { get; set; }
}

public sealed class SystemMtCatalogDocument
{
    public string SutName { get; set; } = string.Empty;
    public ProgramDefinition? Program { get; set; }
    public List<MrBindingDefinition> Mrs { get; set; } = new();

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(SutName))
            throw new CatalogValidationException("SystemMtCatalogDocument.SutName is required");
        Program?.Validate();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var mr in Mrs)
        {
            mr.Validate();
            if (!ids.Add(mr.MrId))
                throw new CatalogValidationException(
                    $"SystemMtCatalogDocument has duplicate MR id: '{mr.MrId}'");
        }
    }
}

public interface IMrCatalogProvider
{
    /// <summary>Source description for diagnostics (e.g. "Hardcoded" or "Manifest:SUT/.../catalog.json").</summary>
    string SourceDescription { get; }
    /// <summary>Load all runnable catalog entries.</summary>
    IReadOnlyList<MrCatalogEntry> Load();
}

public sealed class CatalogValidationException : Exception
{
    public CatalogValidationException(string message) : base(message) { }
    public CatalogValidationException(string message, Exception inner) : base(message, inner) { }
}
```

`ProgramDefinition`, `MrDefinition`: minimal stubs at this stage; full responsibility table in Spec §5.1.

- [ ] **Step 4: Run to verify green**

```bash
dotnet test MetBench_SystemMT.Tests --filter CatalogDefinitionValidationTests
```

Expected: 4 pass / 0 fail.

- [ ] **Step 5: Commit**

```bash
git add MetBench_BLL.Core/SystemMT/Catalog/ \
        MetBench_SystemMT.Tests/SystemMT/Catalog/CatalogDefinitionValidationTests.cs
git commit -m "feat(catalog): definition models + IMrCatalogProvider boundary (Phase A)"
```

---

### Task 2: HardcodedMrCatalogProvider + ManifestMrCatalogProvider + 9 catalog.json + Parity Tests

Spec §5.2 + §5.3 + §6 Phase B + §7.1 (Equivalence Tests). Extract `SystemMtLauncher.BuildBlueprints()` to internal-static `LegacyCatalogFactory.Build` so the hardcoded provider can call it without launcher self-reference.

**Files**:

- Create: `MetBench_BLL.Core/SystemMT/Catalog/HardcodedMrCatalogProvider.cs`
- Create: `MetBench_BLL.Core/SystemMT/Catalog/ManifestMrCatalogProvider.cs`
- Modify: `MetBench_BLL.Core/SystemMT/Launcher/SystemMtLauncher.cs` (extract `BuildBlueprints` → `internal static LegacyCatalogFactory.Build(LauncherOptions)`; keep launcher behavior identical)
- Create: `docs/design/mr-catalog-manifest.schema.json`
- Create: 9 × `SUT/<sut_name>/catalog.json`
- Create: `MetBench_SystemMT.Tests/SystemMT/Catalog/HardcodedMrCatalogProviderTests.cs`
- Create: `MetBench_SystemMT.Tests/SystemMT/Catalog/ManifestMrCatalogProviderTests.cs`
- Create: `MetBench_SystemMT.Tests/SystemMT/Catalog/CatalogParityTests.cs`

- [ ] **Step 1: Write failing tests**

```csharp
// CatalogParityTests.cs
[Fact]
public void Hardcoded_and_manifest_providers_yield_identical_catalog()
{
    var options = new LauncherOptions(SutRoot: TestAssetPaths.AssetRoot(),
        SystemPython: "python3", OpenMocPython: "python3");
    IMrCatalogProvider hardcoded = new HardcodedMrCatalogProvider(options);
    IMrCatalogProvider manifest  = new ManifestMrCatalogProvider(options);

    var h = hardcoded.Load().OrderBy(e => e.Mr.Id).ToList();
    var m = manifest.Load().OrderBy(e => e.Mr.Id).ToList();

    Assert.Equal(h.Count, m.Count);
    Assert.Equal(17, h.Count); // pinning current branch state (post PR #88)
    for (var i = 0; i < h.Count; i++)
    {
        Assert.Equal(h[i].Mr.Id, m[i].Mr.Id);
        Assert.Equal(h[i].Mr.SutName, m[i].Mr.SutName);
        Assert.Equal(h[i].Mr.TransformationName, m[i].Mr.TransformationName);
        Assert.Equal(h[i].Mr.AssertionName, m[i].Mr.AssertionName);
        Assert.Equal(h[i].Mr.ValueName, m[i].Mr.ValueName);
        Assert.Equal(h[i].Mr.MrFamily, m[i].Mr.MrFamily);
        Assert.Equal(h[i].AssertionTypeCode, m[i].AssertionTypeCode);
        Assert.Equal(h[i].EquationKey, m[i].EquationKey);
        Assert.Equal(h[i].SampleCaseRelativePath, m[i].SampleCaseRelativePath);
        Assert.Equal(
            h[i].Mr.DefaultParameters.OrderBy(p => p.Key),
            m[i].Mr.DefaultParameters.OrderBy(p => p.Key));
    }
}

[Fact]
public void Hardcoded_provider_emits_17_entries_across_9_SUTs()
{
    var options = new LauncherOptions("/tmp", "python3", "python3");
    var hardcoded = new HardcodedMrCatalogProvider(options);

    var entries = hardcoded.Load();

    Assert.Equal(17, entries.Count);
    Assert.Equal(9, entries.Select(e => e.Mr.SutName).Distinct().Count());
}

// ManifestMrCatalogProviderTests.cs
[Fact]
public void Load_throws_CatalogValidationException_on_missing_required_field()
{
    var badJsonPath = WriteTempCatalog("""{"sut_name":""}""");
    var provider = new ManifestMrCatalogProvider(new LauncherOptions("/tmp", "python3", "python3"),
        manifestRoots: new[] { Path.GetDirectoryName(badJsonPath)! });

    var ex = Assert.Throws<CatalogValidationException>(() => provider.Load());
    Assert.Contains("SutName", ex.Message);
}

[Fact]
public void Load_throws_when_approx_assertion_lacks_tolerance()
{
    var json = """
        {"sut_name":"x", "mrs": [{
            "mr_id":"x-approx-no-tol", "sut_name":"x",
            "assertion_type_code":"approx", "assertion_name":"ApproxEqual",
            "value_name":"v", "default_parameters":{"factor":"2"},
            "transform_steps":[{"transformation_name":"ScaleField","target_field_path":"/x"}]
        }]}
        """;
    var path = WriteTempCatalog(json);
    var provider = new ManifestMrCatalogProvider(new LauncherOptions("/tmp", "python3", "python3"),
        manifestRoots: new[] { Path.GetDirectoryName(path)! });

    var ex = Assert.Throws<CatalogValidationException>(() => provider.Load());
    Assert.Contains("tolerance", ex.Message, StringComparison.OrdinalIgnoreCase);
}
```

- [ ] **Step 2: Run to verify red**

```bash
dotnet test MetBench_SystemMT.Tests --filter "CatalogParityTests|ManifestMrCatalogProviderTests|HardcodedMrCatalogProviderTests"
```

Expected: all fail (providers + manifests do not exist yet).

- [ ] **Step 3: Extract `LegacyCatalogFactory` from launcher**

In `SystemMtLauncher.cs`:

```csharp
// BEFORE: private static IEnumerable<MrBlueprint> BuildBlueprints(LauncherOptions options) { ... }
// AFTER:  internal static class LegacyCatalogFactory
//         { public static IEnumerable<MrBlueprint> Build(LauncherOptions options) => ... }
```

Move the 17 `yield return new MrBlueprint(...)` to `LegacyCatalogFactory.Build`. Launcher constructor still calls it directly for now (Task 3 swaps to provider).

- [ ] **Step 4: Implement HardcodedMrCatalogProvider**

```csharp
namespace MetBench_BLL.SystemMT.Catalog;

public sealed class HardcodedMrCatalogProvider : IMrCatalogProvider
{
    private readonly LauncherOptions _options;
    public HardcodedMrCatalogProvider(LauncherOptions options) => _options = options;
    public string SourceDescription => "Hardcoded(LegacyCatalogFactory)";
    public IReadOnlyList<MrCatalogEntry> Load() =>
        SystemMtLauncher.LegacyCatalogFactory.Build(_options)
            .Select(bp => MrCatalogEntry.FromBlueprint(bp))
            .ToList();
}
```

Define `MrCatalogEntry.FromBlueprint(MrBlueprint)` mirroring the existing `internal record MrCatalogEntry` used by `LauncherCatalogV2Importer`.

- [ ] **Step 5: Implement ManifestMrCatalogProvider**

Reads `SUT/<sut>/catalog.json` files (default root: `LauncherOptions.SutRoot`; injectable for tests). Maps `MrBindingDefinition` → `MrBlueprint`. Validates each document. Surfaces clear errors on bad JSON / missing fields / unknown assertion / duplicate MR ids.

- [ ] **Step 6: Write 9 catalog.json manifests**

Example (`SUT/heat_equation/catalog.json` — 3 MRs):

```json
{
  "sut_name": "heat-equation",
  "program": {
    "runner_script_path": "heat_equation.py",
    "input_parser_script_path": "heat_equation_input_parser.py",
    "output_parser_script_path": "heat_equation_output_parser.py",
    "input_adapter_script_path": "heat_equation_input_adapter.py",
    "output_adapter_script_path": "heat_equation_output_adapter.py",
    "equation_key": "heat-equation-1d",
    "work_root_name": "MetBenchHeatEq",
    "default_timeout_seconds": 60
  },
  "mrs": [
    {
      "mr_id": "heat-equation-amplitude",
      "display_name": "1D heat equation — ScaleAmplitude (linearity)",
      "description": "1D heat equation with homogeneous Dirichlet BCs is linear in the initial profile. Scaling the initial amplitude by factor > 1 must scale max_u at t_final by the same factor.",
      "mr_family": "Diffusion.Scaling.Amplitude",
      "assertion_type_code": "greater",
      "assertion_name": "GreaterThan",
      "value_name": "max_u",
      "default_parameters": { "factor": "2" },
      "transform_steps": [
        { "transformation_name": "ScaleField", "target_field_path": "/initial/amplitude" }
      ],
      "tolerance_rel": 0,
      "tolerance_abs": 0,
      "noise_aware": false,
      "equation_key": "heat-equation-1d",
      "equation": "Fourier",
      "program_type": "Num",
      "meta_pattern": "Mono",
      "source_level": "Manual",
      "failure_correlation": "None",
      "sample_case_relative_path": "sample/gaussian.json",
      "timeout_seconds": 60
    },
    {
      "mr_id": "fourier-timestep-convergence",
      "display_name": "1D heat equation — TimestepConvergence (forward-Euler refinement)",
      "description": "Time-step convergence MP_conv: doubling num_steps must leave max_u(t_final) within Euler truncation tolerance.",
      "mr_family": "Fourier.Convergence.Timestep",
      "assertion_type_code": "approx",
      "assertion_name": "ApproxEqual",
      "value_name": "max_u",
      "default_parameters": { "factor": "2" },
      "transform_steps": [
        { "transformation_name": "ScaleField", "target_field_path": "/params/num_steps" }
      ],
      "tolerance_rel": 0.01,
      "tolerance_abs": 1e-6,
      "noise_aware": false,
      "equation_key": "heat-equation-1d",
      "equation": "Fourier",
      "program_type": "Num",
      "meta_pattern": "Conv",
      "source_level": "Manual",
      "failure_correlation": "None",
      "sample_case_relative_path": "sample/gaussian.json",
      "timeout_seconds": 60
    },
    {
      "mr_id": "fourier-alpha-monotonic",
      "display_name": "1D heat equation — ScaleAlpha (diffusion monotonicity)",
      "description": "At fixed t_final, larger alpha causes faster diffusive smoothing, so scaling alpha by factor > 1 must strictly decrease max_u.",
      "mr_family": "Fourier.Scaling.Alpha",
      "assertion_type_code": "less",
      "assertion_name": "LessThan",
      "value_name": "max_u",
      "default_parameters": { "factor": "2" },
      "transform_steps": [
        { "transformation_name": "ScaleField", "target_field_path": "/params/alpha" }
      ],
      "tolerance_rel": 0,
      "tolerance_abs": 0,
      "noise_aware": false,
      "equation_key": "heat-equation-1d",
      "equation": "Fourier",
      "program_type": "Num",
      "meta_pattern": "Mono",
      "source_level": "Manual",
      "failure_correlation": "None",
      "sample_case_relative_path": "sample/gaussian.json",
      "timeout_seconds": 60
    }
  ]
}
```

Repeat for remaining 8 SUT manifests with their actual MRs (must exactly mirror launcher's 17 blueprints for parity test to pass). Multi-step example (`damped_oscillator`):

```json
"transform_steps": [
  { "transformation_name": "ScaleField", "target_field_path": "/initial/x0" },
  { "transformation_name": "ScaleField", "target_field_path": "/initial/v0" }
]
```

Recipe-based example (`decay_chain/scale-initial`):

```json
"transform_steps": [
  { "transformation_name": "ScaleInitial", "target_field_path": "" }
]
```

- [ ] **Step 7: Write JSON schema** (`docs/design/mr-catalog-manifest.schema.json`)

JSON Schema Draft 2020-12 describing the document shape; references all enums for 5D fields.

- [ ] **Step 8: Run all 3 test classes to verify green**

```bash
dotnet test MetBench_SystemMT.Tests \
  --filter "CatalogParityTests|ManifestMrCatalogProviderTests|HardcodedMrCatalogProviderTests"
```

Expected: pass. Parity test forces equivalence; deviation in any manifest fails CI loudly.

- [ ] **Step 9: Full regression**

```bash
dotnet test MetBench_SystemMT.Tests
```

Expected: 878 baseline + new catalog tests still green. Launcher behavior unchanged (still uses `LegacyCatalogFactory` directly).

- [ ] **Step 10: Commit**

```bash
git add MetBench_BLL.Core/SystemMT/Catalog/ \
        MetBench_BLL.Core/SystemMT/Launcher/SystemMtLauncher.cs \
        SUT/*/catalog.json \
        docs/design/mr-catalog-manifest.schema.json \
        MetBench_SystemMT.Tests/SystemMT/Catalog/
git commit -m "feat(catalog): manifest provider + 9 SUT catalogs + parity guard (Phase B)"
```

---

### Task 3: Switch `SystemMtLauncher` to Provider-Backed Loading

Spec §6 Phase C. `IMrCatalogProvider` becomes a constructor dependency; launcher no longer calls `LegacyCatalogFactory` directly.

**Files**:

- Modify: `MetBench_BLL.Core/SystemMT/Launcher/SystemMtLauncher.cs`
- Modify: `MetBench_Client/App.xaml.cs` (DI registration of provider; default to `ManifestMrCatalogProvider`)
- Modify: `MetBench_SystemMT.Tests/SystemMT/Launcher/SystemMtLauncherTests.cs`
- Modify: `MetBench_SystemMT.Tests/SystemMT/Launcher/LauncherCatalogV2ImporterTests.cs`
- Modify: `MetBench_SystemMT.Tests/SystemMT/Launcher/LauncherEndToEndOdeTests.cs`

- [ ] **Step 1: Write failing test**

```csharp
[Fact]
public async Task ListAvailableAsync_uses_injected_catalog_provider()
{
    var fakeProvider = new FakeMrCatalogProvider(new[]
    {
        TestBlueprints.MakeMrCatalogEntry("provider-only-mr", "test-sut"),
    });
    var launcher = new SystemMtLauncher(
        options: new LauncherOptions("/tmp", "python3", "python3"),
        pipeline: new SystemMtPipeline(),
        recorder: new SystemMtExecutionRecorder(_fakeExecRepo, _fakeResultRepo),
        anomalyService: new RecordingAnomalyService(),
        catalogProvider: fakeProvider);

    var items = await launcher.ListAvailableAsync();

    Assert.Single(items);
    Assert.Equal("provider-only-mr", items[0].Id);
}
```

- [ ] **Step 2: Run to verify red**

```bash
dotnet test MetBench_SystemMT.Tests --filter ListAvailableAsync_uses_injected_catalog_provider
```

Expected: fail (constructor does not accept provider).

- [ ] **Step 3: Modify launcher constructor**

Add `IMrCatalogProvider catalogProvider` parameter (positional, before `severityThresholds`). Default-not-allowed (must be injected). Replace internal `_mrCatalog` initialization:

```csharp
public sealed class SystemMtLauncher : ISystemMtLauncher
{
    private readonly LauncherOptions _options;
    private readonly ISystemMtPipeline _pipeline;
    private readonly SystemMtExecutionRecorder _recorder;
    private readonly IAnomalyService _anomalyService;
    private readonly AnomalySeverityThresholds _severityThresholds;
    private readonly IReadOnlyDictionary<string, MrBlueprint> _mrCatalog;
    private readonly EquationFunctionRegistry _equationFunctions;

    public SystemMtLauncher(
        LauncherOptions options,
        ISystemMtPipeline pipeline,
        SystemMtExecutionRecorder recorder,
        IAnomalyService anomalyService,
        IMrCatalogProvider catalogProvider,
        AnomalySeverityThresholds? severityThresholds = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
        _recorder = recorder ?? throw new ArgumentNullException(nameof(recorder));
        _anomalyService = anomalyService ?? throw new ArgumentNullException(nameof(anomalyService));
        if (catalogProvider is null) throw new ArgumentNullException(nameof(catalogProvider));
        _severityThresholds = severityThresholds ?? AnomalySeverityThresholds.Default;
        _mrCatalog = catalogProvider.Load()
            .Select(entry => entry.ToBlueprint())
            .ToDictionary(b => b.Mr.Id, StringComparer.Ordinal);
        _equationFunctions = BuildEquationFunctionRegistry();
        RegisterCompositeTransformsIfNeeded(); // unchanged
    }
    // ... rest of file unchanged
}
```

Define `MrCatalogEntry.ToBlueprint()` (inverse of `FromBlueprint`).

- [ ] **Step 4: Update DI registration in `App.xaml.cs`**

```csharp
services.AddSingleton<IMrCatalogProvider>(provider =>
    new ManifestMrCatalogProvider(provider.GetRequiredService<LauncherOptions>()));
// Existing AddScoped<ISystemMtLauncher, SystemMtLauncher> picks up provider via ctor injection automatically.
```

- [ ] **Step 5: Update existing test constructor sites**

`SystemMtLauncherTests.cs`, `LauncherCatalogV2ImporterTests.cs`, `LauncherEndToEndOdeTests.cs`, `SystemMtMetadataCatalogTests.cs`, `SystemMtBootstrapTests.cs` all instantiate `SystemMtLauncher` — add `new HardcodedMrCatalogProvider(options)` arg (or fake provider where appropriate). Hardcoded provider keeps existing test behavior intact during migration.

- [ ] **Step 6: Run full regression**

```bash
dotnet test MetBench_SystemMT.Tests
```

Expected: 878 + new catalog tests still green.

- [ ] **Step 7: Commit**

```bash
git add MetBench_BLL.Core/SystemMT/Launcher/SystemMtLauncher.cs \
        MetBench_Client/App.xaml.cs \
        MetBench_SystemMT.Tests/SystemMT/Launcher/
git commit -m "refactor(launcher): inject IMrCatalogProvider (Phase C)"
```

---

### Task 4: Mark Hardcoded Provider Transitional + Sunset Guard Test

Spec §5.2 + §8 Risk 5. `[Obsolete]` on the transition adapter; guard test pins it.

**Files**:

- Modify: `MetBench_BLL.Core/SystemMT/Catalog/HardcodedMrCatalogProvider.cs`
- Create: `MetBench_SystemMT.Tests/SystemMT/Catalog/HardcodedProviderObsoleteGuardTests.cs`

- [ ] **Step 1: Write failing guard test**

```csharp
[Fact]
public void HardcodedMrCatalogProvider_carries_Obsolete_attribute()
{
    var attr = typeof(HardcodedMrCatalogProvider).GetCustomAttribute<ObsoleteAttribute>();
    Assert.NotNull(attr);
    Assert.False(string.IsNullOrEmpty(attr!.Message));
    Assert.Contains("Stage 9", attr.Message);
    Assert.Contains("manifest", attr.Message, StringComparison.OrdinalIgnoreCase);
}
```

- [ ] **Step 2: Run to verify red**

```bash
dotnet test MetBench_SystemMT.Tests --filter HardcodedMrCatalogProvider_carries_Obsolete_attribute
```

- [ ] **Step 3: Apply [Obsolete]**

```csharp
[Obsolete(
    "Transition adapter for manifest migration. " +
    "Manifest-backed loading achieves parity; this provider must be removed before Stage 9. " +
    "While present, CatalogParityTests guards manifest ≡ hardcoded equivalence.")]
public sealed class HardcodedMrCatalogProvider : IMrCatalogProvider
{
    // ...
}
```

Suppress `CS0618` in tests that legitimately need the hardcoded provider (parity test + obsolete-guard test + launcher test sites pre-Phase-E removal) via file-level `#pragma warning disable CS0618`.

- [ ] **Step 4: Re-run guard + parity tests**

```bash
dotnet test MetBench_SystemMT.Tests \
  --filter "HardcodedProviderObsoleteGuardTests|CatalogParityTests"
```

Expected: green.

- [ ] **Step 5: Commit**

```bash
git add MetBench_BLL.Core/SystemMT/Catalog/HardcodedMrCatalogProvider.cs \
        MetBench_SystemMT.Tests/SystemMT/Catalog/HardcodedProviderObsoleteGuardTests.cs \
        MetBench_SystemMT.Tests/SystemMT/Catalog/CatalogParityTests.cs \
        MetBench_SystemMT.Tests/SystemMT/Launcher/SystemMtLauncherTests.cs
git commit -m "chore(catalog): mark HardcodedMrCatalogProvider transitional, lock sunset gate"
```

---

### Task 5: Execution Evidence Models + Persistence Contracts

Spec §5.4. Evidence-bearing models (no LiteDB wiring yet — Task 6).

**Files**:

- Create: `MetBench_BLL.Core/SystemMT/Persistence/ExecutionEvidence.cs`
- Create: `MetBench_BLL.Core/SystemMT/Persistence/ExecutionSampleTrace.cs`
- Create: `MetBench_BLL.Core/SystemMT/Persistence/ExecutionMetadataSnapshot.cs`
- Create: `MetBench_BLL.Core/SystemMT/Persistence/IExecutionEvidenceRepository.cs`
- Create: `MetBench_SystemMT.Tests/SystemMT/Persistence/ExecutionMetadataSnapshotTests.cs`

- [ ] **Step 1: Write failing test**

```csharp
[Fact]
public void ExecutionMetadataSnapshot_captures_5d_tags_and_V3_ref()
{
    var v3Id = Guid.NewGuid();
    var snap = new ExecutionMetadataSnapshot
    {
        MrId = "openmoc-pincell-nu-sigma-f",
        V3MrIdRef = v3Id,                // links to MetamorphicRelationV3.IdV3 (Spec §5.4)
        SutName = "openmoc",
        Equation = "Boltzmann",
        ProgramType = "Num",
        MetaPattern = "Mono",
        SourceLevel = "Manual",
        FailureCorrelation = "None",
        CatalogManifestPath = "SUT/openmoc/catalog.json",
        MetbenchVersion = "v2.2-dev",
    };

    Assert.Equal("Boltzmann", snap.Equation);
    Assert.Equal(v3Id, snap.V3MrIdRef);
}
```

- [ ] **Step 2: Run to verify red**

```bash
dotnet test MetBench_SystemMT.Tests --filter ExecutionMetadataSnapshotTests
```

- [ ] **Step 3: Implement models**

```csharp
namespace MetBench_BLL.SystemMT.Persistence;

public sealed class ExecutionMetadataSnapshot
{
    public string MrId { get; set; } = string.Empty;
    /// <summary>Reference to MetamorphicRelationV3.IdV3 (Spec §5.4, PR #88 V3 schema).</summary>
    public Guid V3MrIdRef { get; set; }
    public string SutName { get; set; } = string.Empty;
    public string Equation { get; set; } = string.Empty;
    public string ProgramType { get; set; } = string.Empty;
    public string MetaPattern { get; set; } = string.Empty;
    public string SourceLevel { get; set; } = string.Empty;
    public string FailureCorrelation { get; set; } = string.Empty;
    public string CatalogManifestPath { get; set; } = string.Empty;
    public string MetbenchVersion { get; set; } = string.Empty;
}

public sealed class ExecutionSampleTrace
{
    public string VariableName { get; set; } = string.Empty;
    public string SourceValueJson { get; set; } = string.Empty;       // serialized for arbitrary shape
    public string TransformedValueJson { get; set; } = string.Empty;
    public string OutputValueJson { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;                  // e.g. "/initial/amplitude"
}

public sealed class ExecutionEvidence
{
    public Guid IdEvidence { get; set; }
    public Guid ExecutionId { get; set; }                              // FK to Execution
    public ExecutionMetadataSnapshot Metadata { get; set; } = new();
    public List<ExecutionSampleTrace> SampleTraces { get; set; } = new();
    public Dictionary<string, string> TransformationParameters { get; set; } = new();
    public DateTime RecordedAtUtc { get; set; } = DateTime.UtcNow;
}

public interface IExecutionEvidenceRepository
{
    Task SaveAsync(ExecutionEvidence evidence, CancellationToken cancellationToken = default);
    Task<ExecutionEvidence?> GetByExecutionAsync(Guid executionId, CancellationToken cancellationToken = default);
}
```

- [ ] **Step 4: Run to verify green + commit**

```bash
dotnet test MetBench_SystemMT.Tests --filter ExecutionMetadataSnapshotTests
git add MetBench_BLL.Core/SystemMT/Persistence/ \
        MetBench_SystemMT.Tests/SystemMT/Persistence/ExecutionMetadataSnapshotTests.cs
git commit -m "feat(persistence): execution evidence models + V3 MR ref (Phase D part 1)"
```

---

### Task 6: LiteDB Evidence Repository + Pipeline Write-Through + V3 Wiring

Spec §5.4 wiring. Pipeline writes evidence alongside summary; `ExecutionMetadataSnapshot.V3MrIdRef` resolved from V3 repo.

**Files**:

- Create: `MetBench_DAL/LiteDbExecutionEvidenceRepository.cs`
- Modify: `MetBench_DAL/LiteDbSystemMtResultRepository.cs` (add evidence collection registration; no schema change to existing `SystemMtResultRecord`)
- Modify: `MetBench_BLL.Core/SystemMT/Pipeline/SystemMtExecutionRecorder.cs` (write evidence + summary)
- Modify: `MetBench_BLL.Core/SystemMT/Pipeline/SystemMtPipeline.cs` (capture sample traces during parse phase)
- Modify: `MetBench_Client/App.xaml.cs` (DI: register `IExecutionEvidenceRepository` + `IMetamorphicRelationV3Repository`)
- Create: `MetBench_SystemMT.Tests/SystemMT/Persistence/ExecutionEvidenceRoundtripTests.cs`
- Create: `MetBench_SystemMT.Tests/SystemMT/Pipeline/ExecutionEvidenceWriteThroughTests.cs`

- [ ] **Step 1: Write failing roundtrip + write-through tests**

```csharp
// ExecutionEvidenceRoundtripTests.cs
[Fact]
public async Task SaveAsync_then_GetByExecution_returns_full_evidence()
{
    using var repo = new LiteDbExecutionEvidenceRepository(_conn);
    var executionId = Guid.NewGuid();
    var evidence = new ExecutionEvidence
    {
        IdEvidence = Guid.NewGuid(),
        ExecutionId = executionId,
        Metadata = new() { MrId = "heat-equation-amplitude", V3MrIdRef = Guid.NewGuid() },
        SampleTraces =
        {
            new() { VariableName = "amplitude", Path = "/initial/amplitude",
                    SourceValueJson = "1.0", TransformedValueJson = "2.0",
                    OutputValueJson = "2.0" },
        },
        TransformationParameters = { ["factor"] = "2" },
    };

    await repo.SaveAsync(evidence);
    var loaded = await repo.GetByExecutionAsync(executionId);

    Assert.NotNull(loaded);
    Assert.Single(loaded!.SampleTraces);
    Assert.Equal("amplitude", loaded.SampleTraces[0].VariableName);
}

// ExecutionEvidenceWriteThroughTests.cs
[Fact]
public async Task SystemMtPipeline_writes_evidence_with_V3_ref_on_successful_run()
{
    // launcher.RunAsync end-to-end; assert evidence repo has 1 record matching execution
    var v3Repo = new InMemoryV3Repo();
    v3Repo.Add(new MetamorphicRelationV3 { MrCode = "heat-equation-amplitude", IdV3 = Guid.NewGuid() });
    var evidenceRepo = new InMemoryEvidenceRepo();
    var launcher = TestLaunchers.MakeWithEvidence(v3Repo, evidenceRepo);

    var result = await launcher.RunAsync("heat-equation-amplitude");
    Assert.True(result.Passed);

    var evidence = await evidenceRepo.GetByExecutionAsync(result.ExecutionId);
    Assert.NotNull(evidence);
    Assert.NotEqual(Guid.Empty, evidence!.Metadata.V3MrIdRef);
    Assert.NotEmpty(evidence.SampleTraces);
}
```

- [ ] **Step 2: Run to verify red**

- [ ] **Step 3: Implement LiteDb repository**

```csharp
public sealed class LiteDbExecutionEvidenceRepository : IExecutionEvidenceRepository, IDisposable
{
    private readonly LiteDatabase _db;
    private readonly ILiteCollection<ExecutionEvidence> _col;
    public LiteDbExecutionEvidenceRepository(string connectionString)
    {
        _db = new LiteDatabase(connectionString);
        _col = _db.GetCollection<ExecutionEvidence>("execution_evidence");
        _col.EnsureIndex(e => e.ExecutionId);
    }
    public Task SaveAsync(ExecutionEvidence evidence, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        _col.Upsert(evidence);
        return Task.CompletedTask;
    }
    public Task<ExecutionEvidence?> GetByExecutionAsync(Guid executionId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult<ExecutionEvidence?>(_col.FindOne(e => e.ExecutionId == executionId));
    }
    public void Dispose() => _db.Dispose();
}
```

- [ ] **Step 4: Wire V3 lookup + evidence write in `SystemMtExecutionRecorder`**

Recorder gains constructor params `IExecutionEvidenceRepository` + `IMetamorphicRelationV3Repository`. On each pipeline outcome, build `ExecutionMetadataSnapshot` (resolve `V3MrIdRef` via `v3Repo.GetByCode(mrId)`), assemble `ExecutionSampleTrace[]` from pipeline state (source/transformed/output dicts), save evidence in same scope as summary.

- [ ] **Step 5: Pipeline captures sample traces**

`SystemMtPipeline` now also yields per-variable triples during the parse phase. Recorder receives them via an extended `PipelineOutcome` (add `IReadOnlyList<SampleTriple>? SampleTriples` field, default empty).

- [ ] **Step 6: DI registration in `App.xaml.cs`**

```csharp
services.AddSingleton<IExecutionEvidenceRepository>(provider => {
    var dataDir = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!;
    return new LiteDbExecutionEvidenceRepository($"Filename={Path.Combine(dataDir, "SystemMT.Litedb")}");
});
// IMetamorphicRelationV3Repository already exists from PR #88
services.AddSingleton<IMetamorphicRelationV3Repository, LiteDbMetamorphicRelationV3Repository>();
```

- [ ] **Step 7: Run full regression**

```bash
dotnet test MetBench_SystemMT.Tests
```

Expected: 878 base + new evidence/V3-wiring tests still green. End-to-end MR runs now produce non-empty evidence records.

- [ ] **Step 8: Commit**

```bash
git add MetBench_DAL/ MetBench_BLL.Core/SystemMT/Pipeline/ \
        MetBench_BLL.Core/SystemMT/Persistence/ \
        MetBench_Client/App.xaml.cs \
        MetBench_SystemMT.Tests/SystemMT/Persistence/ \
        MetBench_SystemMT.Tests/SystemMT/Pipeline/
git commit -m "feat(persistence): LiteDb evidence repo + pipeline write-through + V3 wiring (Phase D)"
```

---

### Task 7: Remove `LegacyCatalogFactory` and `HardcodedMrCatalogProvider`

Spec §6 Phase E. Only after Phase D parity is stable.

**Files**:

- Delete: `MetBench_BLL.Core/SystemMT/Catalog/HardcodedMrCatalogProvider.cs`
- Delete: `LegacyCatalogFactory` from `SystemMtLauncher.cs`
- Modify: `App.xaml.cs` (remove HardcodedMrCatalogProvider DI fallback path)
- Modify: tests that imported `HardcodedMrCatalogProvider` (replace with `ManifestMrCatalogProvider` or `FakeMrCatalogProvider`)
- Delete: `MetBench_SystemMT.Tests/SystemMT/Catalog/HardcodedMrCatalogProviderTests.cs`
- Delete: `MetBench_SystemMT.Tests/SystemMT/Catalog/HardcodedProviderObsoleteGuardTests.cs`
- Modify: `MetBench_SystemMT.Tests/SystemMT/Catalog/CatalogParityTests.cs` (replace parity test with single-source manifest contract test)

- [ ] **Step 1: Write failing test asserting hardcoded provider absence**

```csharp
[Fact]
public void HardcodedMrCatalogProvider_type_no_longer_exists()
{
    var asm = typeof(IMrCatalogProvider).Assembly;
    var type = asm.GetType("MetBench_BLL.SystemMT.Catalog.HardcodedMrCatalogProvider");
    Assert.Null(type);
}
```

- [ ] **Step 2: Run to verify red**

- [ ] **Step 3: Delete files + clean references**

- [ ] **Step 4: Full regression**

```bash
dotnet test MetBench_SystemMT.Tests
```

Expected: 878 baseline + Task 1..6 added tests; manifest is now the sole catalog source.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "refactor(catalog): remove hardcoded provider + LegacyCatalogFactory (Phase E)"
```

---

### Task 8: Update Documentation and Baseline References

Spec §7.5 (Documentation and Baseline Validation).

**Files**:

- Modify: `README.md`
- Modify: `docs/PROJECT-STRUCTURE.md` (refresh 4 SUT → 9 / 5 MR → 17 / 521 → current baseline; describe provider-backed catalog)
- Modify: `AGENTS.md` (Stage 8 progress: catalog convergence delivered)
- Modify: `CLAUDE.md` §10 (add `MetBench_BLL.SystemMT.Catalog` row to BLL.Core namespaces table)
- Modify: `docs/requirements.md` §10 (close G-X3-CatalogConvergence)

- [ ] **Step 1: Identify stale references**

```bash
grep -rn "BuildBlueprints\|BuildMrCatalog\|hard-coded catalog\|4 SUT\|5 MR\|521 facts" \
  README.md docs/PROJECT-STRUCTURE.md AGENTS.md CLAUDE.md docs/requirements.md
```

- [ ] **Step 2: Update each doc**

- README §architecture: add "Runnable catalog loaded via `IMrCatalogProvider` from `SUT/<sut>/catalog.json` manifests."
- PROJECT-STRUCTURE: refresh SUT/MR counts; new test baseline; new files structure.
- AGENTS Stage 8: add bullet "Catalog convergence delivered (Spec/Plan 2026-05-24); single source-of-truth via JSON manifests; hardcoded provider sunset before Stage 9."
- CLAUDE.md §10 table: insert row `MetBench_BLL.SystemMT.Catalog | manifest definition models + IMrCatalogProvider | ManifestMrCatalogProvider, MrBindingDefinition`.
- docs/requirements.md §10: strike G-X3-CatalogConvergence; new G-X4-LegacyHardcodedSunset for Phase E gate.

- [ ] **Step 3: Re-grep to confirm zero stale references**

- [ ] **Step 4: Commit**

```bash
git add README.md docs/ AGENTS.md CLAUDE.md
git commit -m "docs(catalog): sync architecture and baseline to provider-backed catalog"
```

---

## Self-Review

- Spec coverage: Tasks 1–4 = Phase A+B+C (PR-A); Tasks 5–6 = Phase D (PR-B); Tasks 7–8 = Phase E + doc (PR-C).
- Class-name consistency: all `SystemMtLauncher` (no stale `SystemMtMrLauncher`).
- Branch-state: SUT=9 / MR=17 (post PR #88) pinned in Task 2 Step 1 parity test (`Assert.Equal(17, h.Count)`).
- Tolerance invariant: enforced in `MrBindingDefinition.Validate()` for approx assertions (Task 1).
- V3 schema linkage: `ExecutionMetadataSnapshot.V3MrIdRef : Guid` references `MetamorphicRelationV3.IdV3` (Task 5/6).
- LegacyCatalogFactory: explicit creation step in Task 2 Step 3 (not assumed pre-existing).
- Sunset: `[Obsolete]` (Task 4) + removal (Task 7) gated by parity-stable Phase D.
- Multi-step transforms: `transform_steps` array supports damped-oscillator 2-step and decay-chain Recipe (Task 2).
- 5D tags: full set in `MrBindingDefinition` + `ExecutionMetadataSnapshot` (Tasks 1, 5).
- Shell commands: plain `dotnet test` / `git` (no foreign `rtk` wrapper).

## PR Slicing

| PR | Tasks | Estimated files | Notes |
|---|---|---|---|
| **PR-A** | 1, 2, 3, 4 | ~35 (catalog/, 9 manifests, schema, launcher, 5 tests, DI) | Provider boundary + manifest migration + sunset marker |
| **PR-B** | 5, 6 | ~25 (3 evidence models, IDAL, DAL, recorder/pipeline edits, 2 tests, V3 DI) | Evidence persistence + V3 wiring |
| **PR-C** | 7, 8 | ~15 (deletions + doc sync) | Hardcoded sunset + doc convergence |

## Execution Handoff

Plan v2 saved to `docs/superpowers/plans/2026-05-24-systemmt-catalog-convergence-plan.md`.

Recommended next action: PR-A (Tasks 1–4) on dedicated branch `claude/s8-catalog-convergence-pr-a` once this docs-only PR (spec v3 + plan v2 + §10 G-X3 registration + AGENTS pointer) is merged.
