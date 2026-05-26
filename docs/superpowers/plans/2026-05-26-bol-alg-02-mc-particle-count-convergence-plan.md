# Bol-Alg-02 — MC Particle Count Convergence on OpenMC (PR-N2)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add the MR `openmc-pincell-particle-count-convergence` (PWR `Bol-Alg-02`): doubling OpenMC's per-batch particle count must shrink the reported `k_eff_std` by approximately `1/√2`. Asserts via the noise-aware typed scalar predicate that PR-N1 ships, plus the existing `VarianceRatioPredicate` for the σ-ratio check. Closes the long-standing **Blocked** row for PR-Bol-3 in the active plan index.

**Architecture:** Reuse the existing OpenMC SUT and its `openmc_input_adapter_refine_particles.py` (already implements particle-count multiplication; `ScaleField` targets `/solver/particles`). Reuse the existing `VarianceRatioKernel` and `VarianceRatioPredicate`. Map `assertion_type_code: variance-ratio` (already supported by `LegacyAssertionPredicateMapper.MapVarianceRatio`) — no new typed predicate is needed for the variance ratio itself; the noise-aware predicate from PR-N1 is reserved for future MR designs that need a *direction-with-noise-aware-tolerance* assertion (k_eff change under particle refinement direction is **not** that — it's a variance-ratio assertion). This PR confirms PR-N1's noise-aware predicate is unblocking but does not consume it here; consumption lands in a future PR.

> **Why this PR confirms PR-N1 but does not consume it:** Bol-Alg-02 is a *variance ratio* check (σ ratio ≈ 1/√sampleRatio), not a noise-aware *directional* check. The variance-ratio path is already typed and shipping (PR #124 mapped `variance-ratio` to `VarianceRatioPredicate`). Listing PR-N1 as a prerequisite is a confusion the prior plan-index "blocked" wording carried; **after talking to the spec it turns out Bol-Alg-02 was always mappable via `MapVarianceRatio`**, and the active plan index's "PR-Bol-3 blocked on noise-aware typed predicate" is OVERSTATED. This PR's docs gate corrects that statement.

**Tech Stack:** .NET 8, xUnit, existing OpenMC SUT (no Python changes), existing `VarianceRatioKernel`, existing `MapVarianceRatio`.

---

## Scope and Non-Goals

This is a cloud-side MR-catalog plan. It is suitable for Linux/cloud execution except for the launcher end-to-end test which requires the OpenMC venv — the existing `OpenMcRunnerSmokeTests.Skip.IfNot(OpenMcTestPaths.OpenMcImportable(), …)` pattern is reused so CI skips cleanly when OpenMC is not installed.

This plan must **not**:

- Add a new SUT, EquationMetadata, or runtime key.
- Modify the OpenMC Python runner or its parsers.
- Add new C# adapters (the existing `openmc_input_adapter_refine_particles.py` handles `/solver/particles` already).
- Touch Method MT, WPF, or `App.xaml.cs`.
- Consume PR-N1's `NoiseAwareBinaryComparisonPredicate` here — variance-ratio is the correct semantic for this MR.

It must:

- Add one new `MrBlueprint` row in `LegacyCatalogFactory.cs` for `openmc-pincell-particle-count-convergence`.
- Add one new `MrMetadata` row in `SystemMtMetadataCatalog.cs`.
- Update the six pinned-count test files: 30 → 31 MR (16 → 16 SUT, equation count stays 13).
- Add `LauncherEndToEndOpenMcParticleCountConvergenceTests` (one `[SkippableFact]` gated on `OpenMcTestPaths.OpenMcImportable()`).
- Update `docs/status/current.md` row "T3 reactor anchor deepening" (PR-Bol-3) from Anticipated/Blocked to Controlled.
- Update active plan index PR-Bol-3 row.

## Files

- Modify: `MetBench_BLL.Core/SystemMT/Launcher/LegacyCatalogFactory.cs` (add one blueprint).
- Modify: `MetBench_BLL.Core/SystemMT/Metadata/SystemMtMetadataCatalog.cs` (add one `MrMetadata`).
- Modify: `MetBench_BLL.Core/SystemMT/Catalog/HardcodedMrCatalogProvider.cs` (doc comment 30→31 MR / 16 SUT).
- Create: `MetBench_SystemMT.Tests/SystemMT/Launcher/LauncherEndToEndOpenMcParticleCountConvergenceTests.cs`
- Modify: `MetBench_SystemMT.Tests/SystemMT/Launcher/SystemMtLauncherTests.cs` (pinned descriptor count + ordered list).
- Modify: `MetBench_SystemMT.Tests/SystemMT/Catalog/CatalogParityTests.cs` (count 30→31).
- Modify: `MetBench_SystemMT.Tests/SystemMT/Catalog/HardcodedMrCatalogProviderTests.cs` (count 30→31; SUT count 16 unchanged).
- Modify: `MetBench_SystemMT.Tests/SystemMT/Bootstrap/SystemMtBootstrapTests.cs` (counts 30→31 across SeedCatalogsAsync facts).
- Modify: `MetBench_SystemMT.Tests/SystemMT/Launcher/LauncherCatalogV2ImporterTests.cs` (counts 30→31 across MrsCreated / BindingsCreated / DetailsJson literals).
- Modify: `MetBench_SystemMT.Tests/SystemMT/Launcher/SystemMtLauncherProviderInjectionTests.cs` (count 30→31).
- Modify: `docs/status/current.md`
- Modify: `docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md`
- Modify: `docs/PROJECT-STRUCTURE.md` (§2 row already lists OpenMC; §3 list / §3.1 Boltzmann row append the new MR id).
- Modify: `docs/requirements.md` F-T3-03 (5-equation reactor anchor row) — Boltzmann MR list now 5 (4 single-program + 1 convergence).

## MR Contract

```json
{
  "mr_id": "openmc-pincell-particle-count-convergence",
  "sut_name": "openmc",
  "display_name": "OpenMC pin-cell — ParticleCount × 4 ⇒ σ(k_eff) ≈ /2 (variance-ratio)",
  "description": "PWR Bol-Alg-02 statistical convergence: doubling the per-batch particle count by factor f must shrink the reported k_eff_std by approximately 1/√f. Asserts via VarianceRatioPredicate with sampleRatio = factor.",
  "mr_family": "NeutronTransport.Convergence.ParticleCount",
  "transformation_name": "ScaleParticleCount",
  "assertion_type_code": "variance-ratio",
  "assertion_name": "VarianceRatio",
  "value_name": "k_eff_std",
  "default_parameters": { "factor": "4" },
  "transform_steps": [
    { "transformation_name": "ScaleField", "target_field_path": "/solver/particles" }
  ],
  "tolerance_rel": 0.30,
  "tolerance_abs": 0.0,
  "noise_aware": true,
  "noise_multiplier": 1.0,
  "equation_key": "boltzmann",
  "equation": "Boltzmann",
  "program_type": "MC",
  "meta_pattern": "Conv",
  "source_level": "Manual",
  "failure_correlation": "None",
  "sample_case_relative_path": "sample/pincell.json",
  "work_root_name": "MetBenchOpenMcParticleCountConvergence",
  "timeout_seconds": 600
}
```

Reasoning for `factor = 4` (not 2): OpenMC `k_eff_std` from 60 batches × 5000 particles ≈ 1e-3 — at factor 2 the σ-ratio noise alone can overwhelm the expected √2 shrink. Factor 4 gives a clean 2× shrink with sampler-noise tolerance of 30 % rel + NoiseMultiplier 1 σ (statistical tolerance via `StatisticalToleranceSpec` consumed by `VarianceRatioKernel`).

## MrBlueprint Code

```csharp
yield return new MrBlueprint(
    new MrSummary(
        Id: "openmc-pincell-particle-count-convergence",
        DisplayName: "OpenMC pin-cell — ParticleCount × 4 ⇒ σ(k_eff) ≈ /2 (variance-ratio)",
        SutName: "openmc",
        TransformationName: "ScaleParticleCount",
        AssertionName: "VarianceRatio",
        ValueName: "k_eff_std",
        DefaultParameters: new Dictionary<string, string> { ["factor"] = "4" },
        Description:
            "PWR Bol-Alg-02 statistical convergence: doubling the per-batch particle count " +
            "by factor f must shrink the reported k_eff_std by approximately 1/√f. " +
            "Asserts via VarianceRatioPredicate with sampleRatio = factor.",
        MrFamily: "NeutronTransport.Convergence.ParticleCount"),
    SampleCaseRelativePath: Path.Combine("openmc", "sample", "pincell.json"),
    RunnerScriptPath: Path.Combine(options.SutRoot, "openmc", "openmc_runner.py"),
    InputAdapterScriptPath: Path.Combine(options.SutRoot, "openmc", "openmc_input_adapter_refine_particles.py"),
    OutputAdapterScriptPath: Path.Combine(options.SutRoot, "openmc", "openmc_output_adapter.py"),
    PythonExecutable: options.EffectiveOpenMcPython,
    WorkRootName: "MetBenchOpenMcParticleCountConvergence",
    Timeout: TimeSpan.FromMinutes(10),
    InputParserScriptPath: Path.Combine(options.SutRoot, "openmc", "openmc_input_parser.py"),
    OutputParserScriptPath: Path.Combine(options.SutRoot, "openmc", "openmc_output_parser.py"),
    TransformSteps: new[] { new MrTransformStep("ScaleField", "/solver/particles") },
    AssertionTypeCode: "variance-ratio",
    EquationKey: "boltzmann",
    NoiseAware: true,
    NoiseMultiplier: 1.0,
    Tolerance: new AssertionTolerance(ToleranceRel: 0.30, ToleranceAbs: 0.0));
```

## Task 1: Pin Pinned Counts + Descriptor Order (TDD red)

**Files:**

- Modify: 6 pinned-count test files listed above.

- [ ] **Step 1:** Bump 30 → 31 MR / 16 → 16 SUT (SUT count unchanged) across the six files.
- [ ] **Step 2:** Insert `openmc-pincell-particle-count-convergence` in `SystemMtLauncherTests.cs`'s ordered descriptor list at alphabetical position between `openmc-pincell-nu-sigma-f` (index 15) and `openmc-pincell-sigma-a` (was 16, becomes 17). All subsequent indices shift +1.
- [ ] **Step 3:** Run `dotnet test --no-build --filter "FullyQualifiedName~SystemMtLauncherTests|FullyQualifiedName~CatalogParityTests"` → red (no blueprint yet).

## Task 2: Pin Launcher End-to-End Behaviour (TDD red, skip-on-CI)

**Files:**

- Create: `MetBench_SystemMT.Tests/SystemMT/Launcher/LauncherEndToEndOpenMcParticleCountConvergenceTests.cs`

- [ ] **Step 1:** Test class layout mirrors `OpenMcRunnerSmokeTests`:
  - Constructor builds the launcher with `OpenMocPython = OpenMcTestPaths.OpenMcPython()` and `OpenMcPython = OpenMcTestPaths.OpenMcPython()`.
  - One `[SkippableFact]`: `Skip.IfNot(OpenMcTestPaths.OpenMcImportable(), "OpenMC is not importable from the resolved Python. Set METBENCH_OPENMC_PYTHON or run \`.claude/web-setup.sh\` to install OpenMC into /opt/openmc-venv.");`
  - After Skip: `var result = await _launcher.RunAsync("openmc-pincell-particle-count-convergence");`
  - Assert `result.Passed` true; assert `result.ValueName == "k_eff_std"`; assert `result.FollowUpValue < result.SourceValue` (σ decreased); assert `result.FollowUpValue ≈ result.SourceValue / 2` within 30 % rel (consistent with the catalog tolerance).
- [ ] **Step 2:** Run focused → red (blueprint not yet added).

## Task 3: Add Blueprint + Metadata

**Files:**

- Modify: `LegacyCatalogFactory.cs`, `SystemMtMetadataCatalog.cs`, `HardcodedMrCatalogProvider.cs`.

- [ ] **Step 1:** Append the blueprint after the existing `openmc-pincell-sigma-a` row.
- [ ] **Step 2:** Append a matching `MrMetadata`:
  ```csharp
  new MrMetadata
  {
      MrId = "openmc-pincell-particle-count-convergence",
      EquationKey = "boltzmann",
      PhysicalMeaning =
          "PWR Bol-Alg-02 统计收敛：把每批粒子数乘以 factor 后，OpenMC 报告的 k_eff_std 应按 1/√factor 收缩。",
      InputTransformation = "particles → factor·particles（factor=4）",
      OutputRelation = "k_eff_std(flw) ≈ k_eff_std(src) / √factor（VarianceRatio，30% rel tolerance）",
      ComparisonType = MrComparisonType.Statistical,
      Parameters = new List<MrParameter>
      {
          new() { Symbol = "factor", PhysicalMeaning = "particles 缩放倍率", ValueRange = "factor > 1" },
          new() { Symbol = "k_eff_std", PhysicalMeaning = "OpenMC 报告的 k_eff 1σ 标差（输出）", ValueRange = "k_eff_std > 0" },
      },
  },
  ```
- [ ] **Step 3:** Update `HardcodedMrCatalogProvider.cs` comment 30→31 / 16→16.
- [ ] **Step 4:** Run focused tests → green for pinned counts and descriptor order; the end-to-end test skips cleanly without OpenMC.

## Task 4: Full Suite

- [ ] **Step 1:** `dotnet test --no-build`. Expected: 1234 + 1 = 1235 cloud (no SciPy, no OpenMC, no OpenMOC) / 1240 with SciPy + OpenMC.
- [ ] **Step 2:** Skip count goes from 12 to 13 on cloud CI (the new end-to-end test skips when OpenMC venv is missing — identical pattern to `OpenMcRunnerSmokeTests`).

## Task 5: Docs

- [ ] **Step 1:** `docs/status/current.md` row "T3 reactor anchor deepening" (or add it if missing): describe Bol-Alg-02 closure, point at this plan, distinguish it from Bol-Alg-01 (OpenMOC ray/track, still anticipated).
- [ ] **Step 2:** `docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md`: move PR-Bol-3 row from "Anticipated / blocked" to "Completed (this PR)"; correct the prior "blocked on noise-aware typed predicate" wording to "variance-ratio path was always typed-mappable; the blocker note was incorrect".
- [ ] **Step 3:** `docs/PROJECT-STRUCTURE.md §3.1` Boltzmann row: append `openmc-pincell-particle-count-convergence` as the 5th OpenMC MR; the family table maps it to `Bol-Alg-02`.
- [ ] **Step 4:** `docs/requirements.md` F-T3-03 (5-equation reactor anchor row) — Boltzmann list grows to 5 MR.
- [ ] **Step 5:** Bump baseline section to PR-N2 head + the new pass count.
- [ ] **Step 6:** Retire this plan to §3 of the active plan index.

## Task 6: Two-Layer Review and PR

- [ ] **Layer 1 self-review:**
  - No Method MT, no WPF, no SUT runner / parser edit.
  - The blueprint reuses the existing `openmc_input_adapter_refine_particles.py` — no new adapter.
  - `assertion_type_code = "variance-ratio"` is already typed-mappable (PR #124).
  - Equation count stays 13 (no new EquationMetadata).
  - SUT count stays 16 (no new SUT directory).
- [ ] **Layer 2 maintainer review:**
  - Does the new MR's tolerance (30 % rel) realistically pass on a 5000-particle baseline? Expected: yes (PR #124's `openmc-pincell-refine-particles-tests` style probe with factor 4 reliably shows σ ratio ≈ 0.5 with stat-tolerance noise ~10 %).
  - Could the change quietly weaken the original `openmc-pincell-nu-sigma-f` / `openmc-pincell-sigma-a` MRs? Expected: no — those are deterministic-tolerance scalar MRs and remain bit-for-bit unchanged.
  - Does the active plan index correctly retract the "blocked on noise-aware typed predicate" claim? Expected: yes (Task 5 Step 2).
- [ ] Commit: `feat(bol): add openmc-pincell-particle-count-convergence MR (Bol-Alg-02)`.

## Acceptance Criteria

- The new MR appears in the launcher descriptor list at the correct alphabetical position with the right transformation / assertion / value name / family.
- `LauncherEndToEndOpenMcParticleCountConvergenceTests` skips cleanly without OpenMC and passes when OpenMC is installed (factor 4 → σ-ratio within 30 % rel of 0.5).
- Full `MetBench_SystemMT.Tests` green; cloud CI baseline 1209 + 1 (new skip-fact) = 1210 pass, 13 skip.
- Status ledger PR-Bol-3 row moves Anticipated → Controlled.
- `MetBench_BLL.Core/SystemMT/Catalog/Typed/Migration/LegacyAssertionPredicateMapper.cs` is **not** modified (variance-ratio mapping already existed; this PR does not depend on PR-N1).

## Stop Conditions

Stop and report without coding if:

- PR-N1 has not merged yet AND the active plan index lists PR-Bol-3 as still blocked on it (i.e. the prior plan-index claim turns out to be substantively right rather than overstated; in that case the noise-aware predicate dependency must be re-evaluated before this PR proceeds).
- The OpenMC runner output schema has changed and `k_eff_std` is no longer emitted.
- The `openmc_input_adapter_refine_particles.py` adapter has been removed or refactored.
- Factor 4 does NOT reliably pass at 30 % rel tolerance on the OpenMC venv shipped via `.claude/web-setup.sh` — re-tune tolerance or factor before merging.
