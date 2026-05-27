# PR-Bol-2B — `openmoc-pincell-ray-track-convergence` MR (Bol-Alg-01 catalog consumer)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add the first catalog consumer of the error-monotonic launcher pipeline wired by PR-Bol-2A (#179) — `openmoc-pincell-ray-track-convergence` (Bol-Alg-01). Refining OpenMOC's angular discretization across three phases (num_azim 16 → 32 → 64, azim_spacing 0.05 → 0.025 → 0.0125 cm) must drive `k_eff` monotonically toward the reference (most-refined) value via the typed `ErrorMonotonicPredicate`. After this PR the System-MT catalog goes from **31 MR → 32 MR**, still 16 SUT.

**Status:** queued — to land directly after PR-Bol-2A (#179 merged at `8c267a1`).

**Tech Stack:** .NET 8, xUnit (`SkippableFact` via existing `OpenMocTestPaths.OpenMocImportable()` gate). One new Python adapter file. No WPF.

---

## Why this exists

PR-Bol-2A shipped the wiring (`ExecuteMultiPhaseAsync`, `TypedSpecFactory.ForErrorMonotonic`, `RefinementPhase` records, manifest schema, launcher branching) with **zero MR catalog rows**. This PR is the first consumer — analogous to PR-N2 (#170) after PR-VR (#168).

Bol-Alg-01 (NOETHER MR1 / B5 limit, deterministic-MoC convergence rate) is the OpenMOC counterpart of Bol-Alg-02 (OpenMC variance-ratio). The physical claim: as OpenMOC's ray density and track spacing get finer, the angular flux solution converges; `k_eff` error toward the most-refined run drops monotonically.

---

## Scope and Non-Goals

This is a **catalog + adapter + test** cloud-side plan. Cloud-CI safe (the OpenMOC end-to-end test will skip cleanly under CI without OpenMOC installed).

This plan must **not**:

- Touch `MetBench_BLL.Core/SystemMT/Pipeline/`, `MetBench_BLL.Core/SystemMT/Catalog/Typed/`, or `MetBench_BLL.Core/SystemMT/Catalog/Typed/Migration/` (PR-Bol-2A finalized those surfaces).
- Add new C# `Transformation` types — the new Python adapter handles both `num_azim` and `azim_spacing_cm` mutation atomically per phase.
- Touch the WPF client, `App.xaml.cs`, or any Windows-only file.
- Add a new `AssertionTypeCode` — uses the existing `"error-monotonic"` constant from PR-Bol-2A.
- Add new SUT directory — reuses `SUT/openmoc/` end-to-end.

It **may** (and will):

- Add **exactly one** `MrBlueprint` row in `LegacyCatalogFactory.cs`.
- Add **exactly one** `MrMetadata` row in `SystemMtMetadataCatalog.cs`.
- Add **exactly one** Python adapter `SUT/openmoc/openmoc_input_adapter_refine_ray_tracks.py` that handles per-phase scaling of `/tracking/num_azim` AND `/tracking/azim_spacing_cm` atomically.
- Add **exactly one** manifest MR row in `SUT/openmoc/catalog.json` with the new `refinement_phases` field.
- Bump the **6 pinned-count test files** + **1 production-side comment** from `31` → `32` (single grep-able delta).
- Add `LauncherEndToEndOpenMocRayTrackConvergenceTests` with **2 SkippableFact**s gated on `OpenMocTestPaths.OpenMocImportable()`.
- Update `OpenMocCatalogParityTests` to accept the new 3rd OpenMOC MR (mirror what PR-N2 did for `OpenMcCatalogParityTests`).
- Update `SystemMtLauncherTests.ListAvailableAsync_returns_known_scenarios_in_id_order` positional pin (insert at alphabetical slot).
- Update `LauncherCatalogV2ImporterTests.Import_writes_one_audit_log_entry_with_counts` string-literal pins (`mrsCreated:31 → 32`, `bindingsCreated:31 → 32`).
- Update `docs/status/current.md` §2 baseline note + §3 PR-Bol-2 row + inventory (31 → 32).
- Update `docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md`: this plan + PR-Bol-2A row both move to Completed.

---

## Open Questions Resolved Before Coding

**Q5 — Default refinement schedule** (per #177 orchestration plan):

| Phase role | num_azim | azim_spacing_cm | Expected wall-clock | Expected k_eff |
|---|---|---|---|---|
| `coarse` (baseline) | 16 | 0.05 | ~10 s | ~ 1.4290 |
| `medium` | 32 | 0.025 | ~20 s | ~ 1.4310 |
| `reference` | 64 | 0.0125 | ~60 s | ~ 1.4317 |

Total per MR run: ~90 s; well within the 5 min OpenMOC timeout already configured for sibling MRs (`openmoc-pincell-sigma-a`, `openmoc-pincell-nu-sigma-f`).

**Q6 — Plateau risk on `medium`.** OpenMOC pin-cell with the sample material set converges visibly between 16 and 32 azimuthal angles. Error `|k_eff(medium) − k_eff(reference)|` is empirically ~5× smaller than `|k_eff(coarse) − k_eff(reference)|`, giving a comfortable monotonic-decrease margin without flakiness. If a future SUT change tightens this margin, the test still asserts via `ErrorMonotonicKernel` with `NormKind.Relative`, which scales the comparison appropriately.

**Q7 — Adapter scope.** Add **one** Python adapter `openmoc_input_adapter_refine_ray_tracks.py` that takes a `factor` parameter and applies:

```
new num_azim         = round(old num_azim * factor)
new azim_spacing_cm  = old azim_spacing_cm / factor
```

Both mutations atomically per phase. This mirrors PR-N2's precedent (`openmc_input_adapter_refine_particles.py`). Rejected alternative: two-step `ScaleField` chain (one multiply on num_azim, one inverse-multiply on azim_spacing) — would require a new `InverseScaleField` C# type, out of scope for this PR.

**Q8 — Refinement factor encoding.** Each phase carries its own factor in `refinement_phases[i].parameters["factor"]`:

```json
"refinement_phases": [
  { "role": "coarse",    "parameters": { "factor": "1" } },
  { "role": "medium",    "parameters": { "factor": "2" } },
  { "role": "reference", "parameters": { "factor": "4" } }
]
```

The C# pipeline's `ExecuteMultiPhaseAsync` (PR-Bol-2A) merges `ctx.Parameters` with `phase.Parameters` per-phase; the Python adapter sees one `factor` value at a time.

**Q9 — `MrFamily` tag.** `"NeutronTransport.Convergence.RayTracks"` — sits alongside Bol-Alg-02's `NeutronTransport.Convergence.ParticleCount`. Both share the `Convergence` axis so the Coverage report groups them as a convergence-MR family.

**Q10 — `ComparisonType` on `MrMetadata`.** `MrComparisonType.Relative` (same fallback as PR-N2 — no `Statistical` value in the enum; `Relative` semantically matches "k_eff error decay below a relative threshold").

**Q11 — `AssertionName` display string.** `"ErrorMonotonic"` (PascalCase, parallels PR-N2's `"VarianceRatio"`).

---

## Implementation Tasks

### Task 1 — Add the Python adapter `openmoc_input_adapter_refine_ray_tracks.py`

**File:** `SUT/openmoc/openmoc_input_adapter_refine_ray_tracks.py` (new)

Mirror the shape of `openmc_input_adapter_refine_particles.py`: CLI subcommand `transform-input`, args `--source-file`, `--output-file`, `--params '{"factor": "..."}'`. Body multiplies `/tracking/num_azim` by `factor` (round to nearest int, `max(1, ...)`) and divides `/tracking/azim_spacing_cm` by `factor`. Sets defaults if `tracking` section absent.

**Acceptance:**
- [ ] `python openmoc_input_adapter_refine_ray_tracks.py transform-input --source-file pincell.json --output-file out.json --params '{"factor":"2"}'` produces `tracking.num_azim=32, tracking.azim_spacing_cm=0.025` from the baseline `16 / 0.05`.

### Task 2 — Add the `MrBlueprint` row in `LegacyCatalogFactory.cs`

**File:** `MetBench_BLL.Core/SystemMT/Launcher/LegacyCatalogFactory.cs`

**Where:** Insert immediately after the `openmoc-pincell-sigma-a` block (keep OpenMOC rows grouped). Use the `RefinementPhases` trailing optional parameter added by PR-Bol-2A.

**Content shape:**

```csharp
yield return new MrBlueprint(
    new MrSummary(
        Id: "openmoc-pincell-ray-track-convergence",
        DisplayName: "OpenMOC pin-cell — RefineRayTracks (k_eff error ↘ toward reference)",
        SutName: "openmoc",
        TransformationName: "ScaleField",  // placeholder; ExecuteMultiPhaseAsync invokes the Python adapter per phase, not the C# transform
        AssertionName: "ErrorMonotonic",
        ValueName: "k_eff",
        DefaultParameters: new Dictionary<string, string> { ["factor"] = "1" },
        Description:
            "OpenMOC angular-discretization convergence: refining num_azim and " +
            "azim_spacing_cm via three phases (16/0.05 → 32/0.025 → 64/0.0125) drives " +
            "k_eff monotonically toward the reference. Error is computed under " +
            "NormKind.Relative; the assertion passes iff |k_eff(medium)−k_eff(ref)| ≤ " +
            "|k_eff(coarse)−k_eff(ref)|.",
        MrFamily: "NeutronTransport.Convergence.RayTracks"),
    SampleCaseRelativePath: Path.Combine("openmoc", "sample", "pincell.json"),
    RunnerScriptPath: Path.Combine(options.SutRoot, "openmoc", "openmoc_runner.py"),
    InputAdapterScriptPath: Path.Combine(options.SutRoot, "openmoc", "openmoc_input_adapter_refine_ray_tracks.py"),
    OutputAdapterScriptPath: Path.Combine(options.SutRoot, "openmoc", "openmoc_output_adapter.py"),
    PythonExecutable: options.OpenMocPython,
    WorkRootName: "MetBenchOpenMocRayTracks",
    Timeout: TimeSpan.FromMinutes(5),
    InputParserScriptPath: Path.Combine(options.SutRoot, "openmoc", "openmoc_input_parser.py"),
    OutputParserScriptPath: Path.Combine(options.SutRoot, "openmoc", "openmoc_output_parser.py"),
    TransformSteps: new[] { new MrTransformStep("ScaleField", "/tracking/num_azim") },
    AssertionTypeCode: AssertionTypeCodes.ErrorMonotonic,
    RefinementPhases: new[]
    {
        new RefinementPhase("coarse",    new Dictionary<string, string> { ["factor"] = "1" }),
        new RefinementPhase("medium",    new Dictionary<string, string> { ["factor"] = "2" }),
        new RefinementPhase("reference", new Dictionary<string, string> { ["factor"] = "4" }),
    });
```

**Verification at code time:**
- [ ] `LegacyCatalogFactory.cs` byte-diff vs origin/main shows exactly one new `yield return new MrBlueprint(...)` block.
- [ ] `AssertionTypeCodes.ErrorMonotonic` constant resolves (it's already in `using MetBench_BLL.SystemMT.Assertions;` from PR-Bol-2A).
- [ ] `RefinementPhase` resolves (it's already in `using MetBench_BLL.SystemMT.Pipeline;`).

**Note on `TransformationName`**: For multi-phase MRs the `MrBlueprint.TransformationName` field is informational only — `ExecuteMultiPhaseAsync` doesn't use it for C#-side transformation; the Python adapter handles the actual mutation atomically. `"ScaleField"` is a reasonable placeholder for UI display; if a future Coverage report classifies on this string we may revisit.

### Task 3 — Add the `MrMetadata` row in `SystemMtMetadataCatalog.cs`

**File:** `MetBench_BLL.Core/SystemMT/Metadata/SystemMtMetadataCatalog.cs`

**Where:** Insert after the `openmc-pincell-particle-count-convergence` row (alphabetical-ish grouping by SUT+MR id; OpenMOC follows OpenMC in current ordering).

**Content shape:**

```csharp
new MrMetadata
{
    MrId = "openmoc-pincell-ray-track-convergence",
    EquationKey = "neutron-transport",
    PhysicalMeaning =
        "OpenMOC 角度离散收敛性 (Bol-Alg-01)：细化 num_azim 与 azim_spacing_cm 三相位 " +
        "(16/0.05 → 32/0.025 → 64/0.0125) 使 k_eff 朝最细参考解单调收敛。",
    InputTransformation = "(num_azim, azim_spacing_cm) → (factor·num_azim, azim_spacing_cm/factor); factor > 1",
    OutputRelation = "|k_eff(medium) − k_eff(reference)| ≤ |k_eff(coarse) − k_eff(reference)| (NormKind.Relative)",
    ComparisonType = MrComparisonType.Relative,
    Parameters = new List<MrParameter>
    {
        new() { Symbol = "factor",    PhysicalMeaning = "角度细化倍率（per-phase）",                ValueRange = "factor ≥ 1" },
        new() { Symbol = "k_eff",     PhysicalMeaning = "OpenMOC 报告的有效增殖因子（输出）",       ValueRange = "k_eff > 0" },
    },
},
```

### Task 4 — Add the `SUT/openmoc/catalog.json` manifest entry

**File:** `SUT/openmoc/catalog.json`

**Where:** Append after the existing `openmoc-pincell-sigma-a` block in the `mrs` array.

**Content shape:**

```json
{
  "mr_id": "openmoc-pincell-ray-track-convergence",
  "sut_name": "openmoc",
  "display_name": "OpenMOC pin-cell — RefineRayTracks (k_eff error ↘ toward reference)",
  "description": "OpenMOC 角度离散收敛性 (Bol-Alg-01) ...",
  "mr_family": "NeutronTransport.Convergence.RayTracks",
  "transformation_name": "ScaleField",
  "assertion_type_code": "error-monotonic",
  "assertion_name": "ErrorMonotonic",
  "value_name": "k_eff",
  "default_parameters": { "factor": "1" },
  "transform_steps": [
    { "transformation_name": "ScaleField", "target_field_path": "/tracking/num_azim" }
  ],
  "tolerance_rel": 0,
  "tolerance_abs": 0,
  "noise_aware": false,
  "equation_key": "",
  "equation": "Boltzmann",
  "program_type": "Num",
  "meta_pattern": "Conv",
  "source_level": "Manual",
  "failure_correlation": "None",
  "input_adapter_script_relative_path": "openmoc_input_adapter_refine_ray_tracks.py",
  "sample_case_relative_path": "sample/pincell.json",
  "work_root_name": "MetBenchOpenMocRayTracks",
  "timeout_seconds": 300,
  "refinement_phases": [
    { "role": "coarse",    "parameters": { "factor": "1" } },
    { "role": "medium",    "parameters": { "factor": "2" } },
    { "role": "reference", "parameters": { "factor": "4" } }
  ]
}
```

### Task 5 — Bump pinned counts 31 → 32

**Files** (verified from PR-N2 ledger entry):

| File | Line | Change |
|------|------|--------|
| `MetBench_BLL.Core/SystemMT/Catalog/HardcodedMrCatalogProvider.cs` | comment | `31 MR × 16 SUT` → `32 MR × 16 SUT` |
| `MetBench_SystemMT.Tests/SystemMT/Catalog/CatalogParityTests.cs` | 32 | `Assert.Equal(31, ...)` → `32` |
| `MetBench_SystemMT.Tests/SystemMT/Catalog/HardcodedMrCatalogProviderTests.cs` | 22 | `Assert.Equal(31, ...)` → `32` |
| `MetBench_SystemMT.Tests/SystemMT/Bootstrap/SystemMtBootstrapTests.cs` | (9 occurrences) | All `31 → 32` |
| `MetBench_SystemMT.Tests/SystemMT/Launcher/LauncherCatalogV2ImporterTests.cs` | (9 occurrences incl. `"\"mrsCreated\":31"` + `"\"bindingsCreated\":31"`) | All `31 → 32` |
| `MetBench_SystemMT.Tests/SystemMT/Launcher/SystemMtLauncherTests.cs` | 75 | `Assert.Equal(31, ...)` → `32` |
| `MetBench_SystemMT.Tests/SystemMT/Launcher/SystemMtLauncherProviderInjectionTests.cs` | 110 | `Assert.Equal(31, ...)` → `32` |

### Task 6 — Update parity / ordering tests

- `MetBench_SystemMT.Tests/SystemMT/OpenMocCatalogParityTests.cs` — extend "exactly two single-program Boltzmann MRs" test to 3 IDs (Mono + Conv mix), mirroring how PR-N2 (#170) updated `OpenMcCatalogParityTests`.
- `MetBench_SystemMT.Tests/SystemMT/Launcher/SystemMtLauncherTests.ListAvailableAsync_returns_known_scenarios_in_id_order` — insert `openmoc-pincell-ray-track-convergence` at alphabetical slot. Alphabetical order: `openmoc-pincell-nu-sigma-f` < `openmoc-pincell-ray-track-convergence` < `openmoc-pincell-sigma-a`. So insert at the slot currently occupied by `openmoc-pincell-sigma-a` and shift subsequent indices by 1.

### Task 7 — End-to-end test

**File:** `MetBench_SystemMT.Tests/SystemMT/Launcher/LauncherEndToEndOpenMocRayTrackConvergenceTests.cs` (new)

Mirror `LauncherEndToEndOpenMcParticleCountConvergenceTests` (PR-N2). Two `SkippableFact`s gated on `OpenMocTestPaths.OpenMocImportable()`:

1. **`RunAsync_ray_track_convergence_passes_end_to_end_with_default_phases`**: launcher returns `Passed=true`; `MrRunResult.MrId` matches; exactly one execution + one result + `ok` status + zero anomalies.
2. **`RunAsync_ray_track_convergence_reference_k_eff_strictly_greater_than_coarse`**: sanity guard — `PhaseMetrics["reference"]["k_eff"] > PhaseMetrics["coarse"]["k_eff"]` (OpenMOC under-resolves at coarse, so refinement strictly increases k_eff toward truth on this geometry). Regression alarm for a SUT change that flattens convergence.

### Task 8 — Docs ledger

- `docs/status/current.md`:
  - §1 status date header bump.
  - §2 baseline commit + result narrative refresh (1349 → ~1351 / 0 / 16 with 2 new OpenMOC skips on CI).
  - §2 inventory bump 31 → 32 MRs.
  - §3 PR-Bol-2 row: Open → Controlled; reference PR-Bol-2A `8c267a1` (wiring) + this PR (catalog row).
- `docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md`:
  - PR-Bol-2A row: Active → Completed (`8c267a1`).
  - This plan row: register as Active scoped, then mark Completed.
  - PR-Bol-2 anticipated row: In flight → Completed.

---

## Acceptance Criteria (verifiable on `origin/main` after merge)

- [ ] `git grep -nE 'AssertionTypeCode:\s*AssertionTypeCodes\.ErrorMonotonic|"error-monotonic"' MetBench_BLL.Core/` returns exactly one match (the new blueprint).
- [ ] `git grep -nE 'openmoc-pincell-ray-track-convergence' MetBench_BLL.Core/ MetBench_SystemMT.Tests/ SUT/` returns matches in 4 files (blueprint, metadata, manifest, launcher test).
- [ ] `git grep -nE 'Assert\.Equal\(31,' MetBench_SystemMT.Tests/SystemMT/(Catalog|Bootstrap|Launcher)/` returns zero matches.
- [ ] `dotnet test --filter "FullyQualifiedName~OpenMocRayTrackConvergence"` skips cleanly without `METBENCH_OPENMOC_PYTHON`.
- [ ] Full suite green on CI: expected `~1351 / 0 / 16` (1349 prior + 2 new SkippableFacts).
- [ ] `docs/status/current.md` baseline + PR-Bol-2 row + inventory updated.

---

## Out of Scope

- OpenMC ray-track equivalent (not applicable — MC has no deterministic ray-tracking knob).
- Multi-phase pipeline retrofit for variance-ratio MRs.
- Adaptive refinement until plateau.
- Coverage-service classification by MrFamily (the `NeutronTransport.Convergence.*` family bucket auto-includes Bol-Alg-01 + Bol-Alg-02; no new service code needed).

---

## Notes for Future Successors

- If `OpenMOC` pin-cell convergence proves non-monotonic at the proposed schedule (e.g. due to material-set or geometry tweaks), revise the schedule (e.g. coarser baseline `num_azim=8`) — do NOT switch to `NormKind.Absolute` to mask a real convergence issue.
- If a third Bol-Alg MR is added later (e.g. multi-pin / lattice), this PR's pattern is reusable: new Python adapter + manifest `refinement_phases` + one blueprint row. No further wiring change.
