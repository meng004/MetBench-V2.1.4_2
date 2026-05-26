# PR-N2 — Bol-Alg-02 MC Particle Count Convergence MR (OpenMC pincell)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a single metamorphic relation that exercises the variance-ratio launcher pipeline (just landed via PR-VR / #168) against the existing OpenMC pincell SUT. Increasing OpenMC's `particles` count by factor `f = 4` must shrink the reported `k_eff_std` by approximately `1/√f = 0.5`, with 30 % relative slack to absorb Monte-Carlo noise. The new MR is `openmc-pincell-particle-count-convergence`. After this PR the System-MT catalog goes from **30 MR → 31 MR**, still 16 SUT.

**Why this exists.** Bol-Alg-02 is the first MR consumer of the typed variance-ratio path. PR #166 retracted the original PR-N2 because the wiring didn't exist; PR-VR (#168) closed that gap on `befbe5f`. This PR restarts PR-N2 with the minimum delta required — no new SUT, no new Python script, no new transform — just one blueprint row + one metadata row + one end-to-end skip-safe launcher test.

**Tech Stack:** .NET 8, xUnit (`SkippableFact` via existing `OpenMcTestPaths.OpenMcImportable()` gate). No new Python, no WPF.

**Status:** queued — to land directly after PR-VR (#168 merged at `befbe5f`).

---

## Scope and Non-Goals

This is a **catalog + test** cloud-side plan. Cloud-CI safe (the OpenMC end-to-end test will skip cleanly under CI without OpenMC installed).

This plan must **not**:

- Modify any file under `MetBench_BLL.Core/SystemMT/Catalog/Typed/` (the typed pipeline is now correct).
- Modify any file under `MetBench_BLL.Core/SystemMT/Pipeline/`.
- Modify any file under `MetBench_BLL.Core/SystemMT/Catalog/Typed/Migration/`.
- Add a new SUT directory, new Python runner, new input adapter, or new output parser.
- Add a new `Transformation` C# type — `ScaleField` already handles integer `particles` multiplication (verified at plan time in `MetBench_BLL.Core/SystemMT/Transformations/ScaleField.cs:63–67`).
- Add a new `AssertionTypeCode` — uses the existing `"variance-ratio"` constant from `AssertionTypeCodes`.
- Touch the WPF client, App.xaml.cs, or any Windows-only file.
- Modify the legacy `AssertionEvaluator` path or its `ExtraAssertionValues["refinement_factor"]` defence-in-depth wiring — this PR intentionally relies on the typed-kernel path that PR-VR established.

It **may** (and will):

- Add **exactly one** `MrBlueprint` row in `LegacyCatalogFactory.cs`.
- Add **exactly one** `MrMetadata` row in `SystemMtMetadataCatalog.cs`.
- Reuse the existing `SUT/openmc/openmc_input_adapter_refine_particles.py` (already implements `ScaleField` on `/solver/particles`).
- Reuse the existing `SUT/openmc/openmc_output_parser.py` (already emits `k_eff` + `k_eff_std`).
- Reuse `SUT/openmc/sample/pincell.json` as the baseline case.
- Bump the **6 pinned-count assertions** + **1 production-side comment** from `30` → `31` (single grep-able delta).
- Update `docs/status/current.md` §2 baseline note and §3 PR-Bol-3 row.
- Update `docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md` to move the retracted PR-N2 plan to historical and add this plan as active.

---

## Open Questions Resolved Before Coding

**Q1 — Default parameter values.** factor = `4`, ToleranceRel = `0.30` (per user confirmation 2026-05-26). Maps via PR-VR convention to `SigmaMultiplier = 1 + ToleranceRel = 1.30`, so the kernel passes iff `high.StdError ≤ (low.StdError / √4) × 1.30 = low.StdError × 0.65`.

**Q2 — NoiseMultiplier field.** Set to `1.0` for explicitness, even though the variance-ratio kernel does not consume it (kernel reads `SigmaMultiplier` only). Documents intent on the blueprint side.

**Q3 — AssertionName display string.** `"VarianceRatio"` (PascalCase, parallels existing `"LessThan"` / `"Approximately"` display names on sibling rows). This is a UI label, not a dispatch key — the dispatch key is `AssertionTypeCode: "variance-ratio"`.

**Q4 — MrFamily tag.** `"NeutronTransport.Sampling.ParticleCount"` — sits alongside the existing `"NeutronTransport.Scaling.SigmaA"` / `"NeutronTransport.Scaling.NuSigmaF"` family naming convention. Distinct family axis (sampling vs scaling), so report aggregation groups it correctly.

**Q5 — Skip behaviour on CI.** Uses `SkippableFact` gated on `OpenMcTestPaths.OpenMcImportable()`. Ubuntu CI ships without OpenMC → the test skips, contributing `+0 pass / +0 fail / +1 skip` to the suite baseline. Local + Windows + Parallels VM runs with `METBENCH_OPENMC_PYTHON` set → the test executes and runs the launcher end-to-end.

**Q6 — Statistical noise tolerance for the end-to-end test.** With 5000 particles the OpenMC `k_eff_std` is in the 0.001–0.002 range; with 20 000 particles it drops to roughly 0.0005–0.001. Variance-ratio kernel's 30 % rel slack is wide enough that one local run will not be flaky. The test does **not** also assert on absolute `k_eff` change (the MR's contract is only about `k_eff_std`).

**Q7 — Why no Bol-Alg-02 transform-step block?** The existing `ScaleField` works directly on `/solver/particles` as an `int` field (verified). `MrTransformStep("ScaleField", "/solver/particles")` is all that's needed; no per-MR transformation C# class.

---

## Architecture (post-PR-N2)

The data flow exercises the wiring PR-VR added; this PR only contributes the leftmost box.

```
                                Catalog (NEW)
                                ┌─────────────────────────────────┐
                                │ openmc-pincell-particle-count-  │
                                │ convergence MrBlueprint:        │
                                │   factor=4, ToleranceRel=0.30,  │
                                │   AssertionTypeCode=variance-   │
                                │   ratio, ValueName=k_eff_std    │
                                └──────────────┬──────────────────┘
                                               │
                                               ▼
LauncherFacade.RunAsync ─▶ SystemMtPipeline.EvaluateAssertion
                              │  (PR-VR wired)
                              ▼
                          TypedSpecFactory.ForLegacyAssertion
                              │  dispatches on AssertionTypeCode
                              ▼
                          TypedSpecFactory.ForVarianceRatio
                              │  (factor → SampleRatio,
                              │   ToleranceRel → SigmaMultiplier)
                              ▼
                          VarianceRatioKernel.Evaluate
                              │  threshold = (low.σ / √f) × (1+rel)
                              ▼
                          VerifyStatus { Passed | Failed }
```

---

## Implementation Tasks

### Task 1 — Add the `MrBlueprint` row in `LegacyCatalogFactory.cs`

**File:** `MetBench_BLL.Core/SystemMT/Launcher/LegacyCatalogFactory.cs`

**Where:** Insert immediately after the existing `openmc-pincell-sigma-a` block (so OpenMC blueprints stay grouped). Indent + brace style must match the surrounding rows byte-for-byte.

**Content shape (illustrative, exact strings will be finalized at code time):**

```csharp
yield return new MrBlueprint(
    new MrSummary(
        Id: "openmc-pincell-particle-count-convergence",
        DisplayName: "OpenMC pin-cell — RefineParticles (k_eff_std ~ 1/√f)",
        SutName: "openmc",
        TransformationName: "ScaleField",
        AssertionName: "VarianceRatio",
        ValueName: "k_eff_std",
        DefaultParameters: new Dictionary<string, string> { ["factor"] = "4" },
        Description:
            "源算例与衍生算例使用相同物理输入；衍生算例将 OpenMC 的 particles 计数放大 factor 倍。" +
            "依 1/√N 抽样定律，期望 k_eff 的报告 std error 收缩到 1/√factor。" +
            "采用 variance-ratio 断言：" +
            "k_eff_std(flw) ≤ k_eff_std(src) / √factor × (1 + ToleranceRel)。",
        MrFamily: "NeutronTransport.Sampling.ParticleCount"),
    SampleCaseRelativePath: Path.Combine("openmc", "sample", "pincell.json"),
    RunnerScriptPath: Path.Combine(options.SutRoot, "openmc", "openmc_runner.py"),
    InputAdapterScriptPath: Path.Combine(options.SutRoot, "openmc", "openmc_input_adapter_refine_particles.py"),
    OutputAdapterScriptPath: Path.Combine(options.SutRoot, "openmc", "openmc_output_adapter.py"),
    PythonExecutable: options.EffectiveOpenMcPython,
    WorkRootName: "MetBenchOpenMcRefineParticles",
    Timeout: TimeSpan.FromMinutes(5),
    InputParserScriptPath: Path.Combine(options.SutRoot, "openmc", "openmc_input_parser.py"),
    OutputParserScriptPath: Path.Combine(options.SutRoot, "openmc", "openmc_output_parser.py"),
    TransformSteps: new[] { new MrTransformStep("ScaleField", "/solver/particles") },
    AssertionTypeCode: "variance-ratio",
    Tolerance: new AssertionTolerance(
        NoiseAware: true,
        ToleranceRel: 0.30,
        NoiseMultiplier: 1.0));
```

**Verification at code time:**
- [ ] Confirm `AssertionTolerance(NoiseAware, ToleranceRel, NoiseMultiplier)` constructor signature still matches (read first; do not assume).
- [ ] Confirm `MrBlueprint` accepts a `Tolerance` parameter at construction (read first; the existing sigma-a row may or may not pass it explicitly).
- [ ] Confirm `WorkRootName` collision-free vs the four other openmc work-roots.

### Task 2 — Add the `MrMetadata` row in `SystemMtMetadataCatalog.cs`

**File:** `MetBench_BLL.Core/SystemMT/Metadata/SystemMtMetadataCatalog.cs`

**Where:** Insert next to the existing `openmc-pincell-sigma-a` metadata row (keep OpenMC entries grouped, same as the blueprint side).

**Content shape:**

```csharp
new MrMetadata
{
    MrId = "openmc-pincell-particle-count-convergence",
    EquationKey = "neutron-transport",
    PhysicalMeaning =
        "对同一 OpenMC pin-cell 算例放大 particles 计数。" +
        "依据 Monte-Carlo 抽样的 1/√N 定律，更大的 N 应使 k_eff 的报告 std error 单调收缩。",
    InputTransformation = "particles → factor·particles（factor > 1）",
    OutputRelation = "k_eff_std(flw) ≤ k_eff_std(src) / √factor × (1 + ToleranceRel)",
    ComparisonType = MrComparisonType.Statistical,
    Parameters = new List<MrParameter>
    {
        new() { Symbol = "factor",     PhysicalMeaning = "particles 放大倍率",                ValueRange = "factor > 1" },
        new() { Symbol = "k_eff",      PhysicalMeaning = "OpenMC 报告的有效增殖因子（输出）", ValueRange = "k_eff > 0" },
        new() { Symbol = "k_eff_std",  PhysicalMeaning = "k_eff 的标准误（输出，本 MR 校验目标）", ValueRange = "k_eff_std > 0" },
    },
},
```

**Verification at code time:**
- [ ] Confirm `MrComparisonType.Statistical` exists. If only `Ordinal` / `Approximate` / `Exact` exist, add `Statistical` in a tightly-scoped second commit OR fall back to `Approximate` and note in the plan retrospective.
- [ ] Confirm `MrParameter` field names match the existing rows (Symbol / PhysicalMeaning / ValueRange).

### Task 3 — Bump pinned counts 30 → 31

**Files to update** (verified at plan time via `Assert.Equal(30,` and `30 MR` grep):

| File | Type | Line region |
|------|------|-------------|
| `MetBench_BLL.Core/SystemMT/Catalog/HardcodedMrCatalogProvider.cs` | Production comment | "30 MR × 16 SUT" → "31 MR × 16 SUT" |
| `MetBench_SystemMT.Tests/SystemMT/Catalog/CatalogParityTests.cs` | Test assertion | `Assert.Equal(30, ...)` → `31` |
| `MetBench_SystemMT.Tests/SystemMT/Catalog/HardcodedMrCatalogProviderTests.cs` | Test assertion | `Assert.Equal(30, ...)` → `31` |
| `MetBench_SystemMT.Tests/SystemMT/Bootstrap/SystemMtBootstrapTests.cs` | Test assertion | `Assert.Equal(30, ...)` → `31` |
| `MetBench_SystemMT.Tests/SystemMT/Launcher/LauncherCatalogV2ImporterTests.cs` | Test assertion | `Assert.Equal(30, ...)` → `31` |
| `MetBench_SystemMT.Tests/SystemMT/Launcher/SystemMtLauncherTests.cs` | Test assertion | `Assert.Equal(30, ...)` → `31` |
| `MetBench_SystemMT.Tests/SystemMT/Launcher/SystemMtLauncherProviderInjectionTests.cs` | Test assertion | `Assert.Equal(30, ...)` → `31` |

**Verification at code time:**
- [ ] Final grep `git grep -nE 'Assert\.Equal\(30,'` returns no SystemMT-relevant matches after the bump.
- [ ] No file under `MetBench_BLL/` (Method MT) is accidentally touched.

### Task 4 — Add the end-to-end SkippableFact

**File:** `MetBench_SystemMT.Tests/SystemMT/Launcher/OpenMcParticleCountConvergenceLauncherTests.cs` (new file)

**Structure:**

```csharp
public sealed class OpenMcParticleCountConvergenceLauncherTests
{
    [SkippableFact]
    public async Task Particle_count_convergence_mr_passes_under_default_parameters()
    {
        Skip.IfNot(OpenMcTestPaths.OpenMcImportable(),
            "OpenMC is not importable from the resolved Python. Set METBENCH_OPENMC_PYTHON.");

        // Resolve the launcher via the same DI pattern other SkippableFact suites use,
        // then call RunAsync("openmc-pincell-particle-count-convergence") with default parameters.
        // Assert: result.AssertionResult.Passed is true, result.MrCode is "openmc-pincell-particle-count-convergence",
        //         and the typed-verification path was taken (TypedVerification ≠ null).
    }

    [SkippableFact]
    public async Task Particle_count_convergence_mr_fails_when_factor_is_one()
    {
        Skip.IfNot(OpenMcTestPaths.OpenMcImportable(),
            "OpenMC is not importable from the resolved Python. Set METBENCH_OPENMC_PYTHON.");

        // Override factor → "1" via parameterOverrides. Source and followup use identical
        // particle counts; expected ratio is 1.0, but the kernel demands high.StdError ≤
        // low.StdError × 1.30 / √1 ≈ low.StdError × 1.30. With identical sample counts
        // this should pass too, so instead pass factor="0.5" (allowed by transform but
        // not by ForVarianceRatio's factor>1 check) — and assert the assertion fails
        // closed with an UnknownType FailureReason mentioning "factor".
        //
        // Decision at code time: if a "factor < 1" injection is not naturally testable
        // through the launcher (the spec factory throws before any process is spawned),
        // collapse this test into a unit-level assertion in PR-VR-style tests under
        // MetBench_SystemMT.Tests/SystemMT/Catalog/Typed/ — keep this file with only the
        // first SkippableFact.
    }
}
```

**Acceptance criteria:**
- First SkippableFact must run green on a host with OpenMC installed; must skip cleanly on CI without OpenMC.
- Second SkippableFact's value is debatable; if it complicates the file (e.g. requires a non-trivial launcher parameter override fixture), drop it before commit and document the drop in the PR description.

**Verification at code time:**
- [ ] Run `dotnet test --filter "FullyQualifiedName~OpenMcParticleCountConvergence"` on Linux with `METBENCH_OPENMC_PYTHON` set and verify the first test passes.
- [ ] Run the same command without `METBENCH_OPENMC_PYTHON` and verify the test skips (not fails).

### Task 5 — Update status ledger

**File:** `docs/status/current.md`

Update §2 baseline note: `1275 / 0 / 12` → `1275 / 0 / 13` (one more skip from the new SkippableFact; no new passing facts on CI) — **or** `1276 / 0 / 12` if a second non-skip unit test is added in Task 4. Final number TBD by what Task 4 lands as.

Update §3 PR-Bol-3 row: change from "Open — actual blocker: variance-ratio assertion is not wired" to "Controlled — variance-ratio wiring shipped via PR-VR (#168); MR row shipped via PR-N2 (#NNN)".

Add a new §3 row "Particle-count convergence MR (Bol-Alg-02)": "Controlled — `openmc-pincell-particle-count-convergence` ships; SkippableFact under `OpenMcParticleCountConvergenceLauncherTests` skips on CI / runs locally."

### Task 6 — Update active plan index

**File:** `docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md`

- Add a row in §1 (Active): `2026-05-26-pr-n2-bol-alg-02-mc-particle-count-convergence-plan.md` — "Active scoped implementation plan — Bol-Alg-02 MC particle count convergence MR".
- Move the old `2026-05-26-bol-alg-02-mc-particle-count-convergence-plan.md` retracted row from §1 to §3 (Historical) since its successor (this plan) is now active.

---

## Acceptance Criteria (verifiable on `origin/main` after merge)

- [ ] `git grep -nE 'AssertionTypeCode:\s*"variance-ratio"' MetBench_BLL.Core/` returns **exactly one** match — the new blueprint.
- [ ] `git grep -nE 'openmc-pincell-particle-count-convergence' MetBench_BLL.Core/ MetBench_SystemMT.Tests/` returns matches in both the blueprint and the metadata file, and in the new launcher test file.
- [ ] `git grep -nE 'Assert\.Equal\(30,' MetBench_SystemMT.Tests/SystemMT/(Catalog|Bootstrap|Launcher)/` returns **zero** matches. `Assert.Equal(31,` is present in the six bumped files.
- [ ] `dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~OpenMcParticleCountConvergence"` skips cleanly without `METBENCH_OPENMC_PYTHON` (xUnit "skipped" outcome, not "failed").
- [ ] `dotnet test MetBench_SystemMT.Tests --no-restore` full suite is green: baseline `1275 / 0 / 12` → `1275 / 0 / 13` (or `1276 / 0 / 12` per Task 4 final shape).
- [ ] `docs/status/current.md` baseline note + PR-Bol-3 row + new Particle-count row reflect the post-PR state.
- [ ] `docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md` lists this plan as active, retracted PR-N2 v1 as historical.
- [ ] PR description's "Soft Review" checklist passes (Scope / Facts / Tests / Windows / Review / Merge / Soft Review).

---

## Out of Scope (explicit deferrals)

- **Defence-in-depth `ExtraAssertionValues["refinement_factor"]`** at the launcher layer — same deferral as PR-VR; typed kernel path does not consume it.
- **A second variance-ratio MR for OpenMOC** (e.g. `openmoc-pincell-rays-convergence`) — out of scope; that needs its own analysis since OpenMOC is deterministic and the "convergence" semantics are different (ray count, not particle count).
- **Variance-ratio coverage scoring in the report** — handled by existing Coverage service via the MrFamily tag; no change here.

---

## Notes for Future Successors

- If the OpenMC `k_eff_std` ever proves noisier than expected and the test flakes, **do not widen ToleranceRel silently**. Reproduce locally, examine the actual ratio, and either (a) increase `particles` baseline in `pincell.json` (improves SNR symmetrically), or (b) raise ToleranceRel with a documented rationale and commit message explaining the Monte-Carlo noise floor for this specific case.
- If a second MC SUT (e.g. an OpenMC criticality benchmark) joins the catalog later and reuses variance-ratio, the `MrFamily` tag should be reused (`NeutronTransport.Sampling.ParticleCount`) so the Coverage report groups them.
