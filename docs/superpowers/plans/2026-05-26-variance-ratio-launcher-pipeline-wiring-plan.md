# PR-VR — Wire `variance-ratio` Assertion Through the Launcher Pipeline

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make `AssertionTypeCode = "variance-ratio"` reachable end-to-end from `SystemMtLauncher.RunAsync(...)` → `SystemMtPipeline.EvaluateAssertion` → `VarianceRatioKernel.Evaluate`. After this PR, an `MrBlueprint` with `AssertionTypeCode: "variance-ratio"` and `DefaultParameters["factor"]` populated will assert correctly against the typed `VarianceRatioPredicate` via the typed dispatcher — no new MR catalog row in this PR; PR-N2 will be the first consumer.

**Why this exists.** PR #124 added `LegacyAssertionPredicateMapper.MapVarianceRatio(...)` to the typed migration namespace, but the production runtime never reaches it: `SystemMtPipeline.EvaluateAssertion` (lines 251–325) only calls `MapScalar`, and the launcher never populates `RoleOutput.Statistical(StdError)` or `ExtraAssertionValues["refinement_factor"]`. PR-N2 (Bol-Alg-02 MC particle count convergence) was retracted (see `2026-05-26-bol-alg-02-mc-particle-count-convergence-plan.md` § "Discovered blocker (2026-05-26)") because of these gaps. This plan ships the wiring; PR-N2 then restarts as a one-blueprint addition.

**Tech Stack:** .NET 8, xUnit. No Python changes, no new SUT, no new EquationMetadata.

---

## Scope and Non-Goals

This is a **wiring-only** cloud-side plan. Cloud-CI safe.

This plan must **not**:

- Add any new `MrBlueprint` row, `MrMetadata` row, or SUT directory.
- Change the OpenMC / OpenMOC Python runners or parsers (output parsers already emit `k_eff_std`; this PR just makes the launcher *use* it as a stderr metric).
- Modify Method MT, WPF, `App.xaml.cs`, or any UI binding.
- Add new external Python venvs.
- Touch the existing `MapScalar` legacy fallbacks (no behaviour change for `less` / `greater` / `approx` / `equal`).
- Re-tune any existing MR's tolerance.
- Introduce a new `AssertionTypeCode` constant beyond what `AssertionTypeCodes.VarianceRatio` already declares.

It must:

- Route `variance-ratio` through `LegacyAssertionPredicateMapper.MapVarianceRatio` from the pipeline.
- Populate `RoleOutput.Statistical(StdError)` for the metric the predicate names (e.g. `k_eff_std → StdError` keyed against `k_eff`).
- Resolve `SampleRatio` from blueprint `DefaultParameters` / per-run overrides into a `ConstantParameterExpression`.
- Keep the typed dispatcher as the assertion authority (typed path, not legacy `AssertionEvaluator` path).
- (Defence-in-depth) Also populate `ExtraAssertionValues["refinement_factor"]` so any legacy fallback that does take the old path still works.
- Add ≥18 new test facts pinning each wiring surface.

## Files (planned)

**Modify:**

- `MetBench_BLL.Core/SystemMT/Pipeline/SystemMtPipeline.cs` — `EvaluateAssertion` gains a `variance-ratio` switch arm.
- `MetBench_BLL.Core/SystemMT/Pipeline/PipelineContext.cs` — clarifying comment on `ExtraAssertionValues` to name `refinement_factor` as the canonical key.
- `MetBench_BLL.Core/SystemMT/Launcher/SystemMtLauncher.cs` — populate `ExtraAssertionValues` with `refinement_factor` from `DefaultParameters["factor"]` when `AssertionTypeCode == "variance-ratio"`, plus pass per-role stderr metrics into the new statistical-role surface.
- `MetBench_BLL.Core/SystemMT/Pipeline/TypedVerificationContextFactory.cs` (or successor) — extend the `FromScalarOutputs` shape so a metric / `metric_std` pair gets promoted to `RoleOutput.Statistical(StdError)` for the named metric.

**Create:**

- `MetBench_BLL.Core/SystemMT/Pipeline/VarianceRatioContextBuilder.cs` (or inline helper inside `SystemMtPipeline`) — single-responsibility builder that takes `(blueprint, sourceMetrics, followupMetrics)` and emits the typed `(MrSpec, VarianceRatioPredicate, VerificationContext)` triple. Pure / deterministic / no IO.
- `MetBench_SystemMT.Tests/SystemMT/Pipeline/VarianceRatioPipelineWiringTests.cs` — ≥12 facts (see § Test Surface).
- `MetBench_SystemMT.Tests/SystemMT/Launcher/LauncherVarianceRatioFakeRoleOutputsTests.cs` — ≥6 facts using a deterministic stub output parser (no OpenMC venv) to drive the launcher end-to-end through a `variance-ratio` blueprint, asserting the typed assertion result is reachable with synthetic σ values.

**Updated tests:** none of the existing pinned-count files (this PR adds no MR row).

## Resolution Rules (proposed for the new pipeline arm)

When `ctx.AssertionTypeCode == "variance-ratio"` and `ctx.TypedSpec` / `ctx.TypedPredicate` are not pre-provided:

1. **InvalidSpec** if `ctx.ValueName` is blank.
2. **InvalidSpec** if `DefaultParameters` / `parameterOverrides` do not contain a numeric `factor` (or `refinement_factor`) > 1; `double.TryParse` with `CultureInfo.InvariantCulture` is the only accepted form.
3. **InvalidSpec** if `ctx.Tolerance.ToleranceRel <= 0` (variance-ratio needs a positive σ-multiplier; `StatisticalToleranceSpec` is built from `Tolerance.ToleranceRel` or `Tolerance.NoiseMultiplier` — see open question #1).
4. **SkippedMissingObservable** if either role lacks the metric or lacks the `<metric>_std` companion key from the output parser.
5. **SkippedMissingObservable** if any σ is non-finite or negative.
6. **Pass** if `followup.StdError <= source.StdError / √factor · sigmaMultiplier`.
7. **Fail** otherwise; diagnostic carries `source.StdError`, `followup.StdError`, expected threshold, and the resolved `sampleRatio`.

This mirrors `VarianceRatioKernel.Evaluate` exactly — the new pipeline arm just constructs the inputs and calls the existing kernel via the dispatcher. **No duplicate evaluation logic.**

## Statistical Role Output Surface

`VarianceRatioKernel.Evaluate` calls `context.GetStatistical(role, metric).StdError`. The launcher currently builds `RoleOutput` from a flat scalar `Outputs[metric]` map. Two viable shapes for the new surface:

- **Shape A (recommended): naming convention.** Output parser emits a scalar `metric_std` (or `metric_sigma` / `metric_stderr`) alongside the existing `metric`. The pipeline / context-builder promotes the pair `(metric, metric_std)` into a single `RoleOutput.Statistical(metric, value, StdError)` entry. The OpenMC output parser already emits `k_eff` and `k_eff_std`; this requires zero Python change. Other SUTs (deterministic) won't have `_std` keys and would be unaffected (variance-ratio is only used by stochastic SUTs).
- **Shape B (alternative): explicit declaration.** Blueprint declares a list of statistical metrics; the pipeline reads them from the same flat dict and packages them. More code, but no naming-convention coupling.

Pick **Shape A** for this PR. Document the convention in `PipelineContext.cs` XML doc and add a guard test asserting OpenMC's output parser still emits the matching `_std` companion (smoke-test grade, not a runtime contract).

## Task 1 — Failing tests (TDD red)

**Files:**

- Create: `MetBench_SystemMT.Tests/SystemMT/Pipeline/VarianceRatioPipelineWiringTests.cs`
- Create: `MetBench_SystemMT.Tests/SystemMT/Launcher/LauncherVarianceRatioFakeRoleOutputsTests.cs`

- [ ] **Step 1:** Write `VarianceRatioPipelineWiringTests` with ≥12 red facts:
  - Happy path: σ_source = 1e-3, σ_followup = 5e-4, factor = 4 → typed assertion Pass.
  - Edge: σ_followup slightly above threshold → Fail with non-null diagnostic.
  - Missing `_std` companion on source → SkippedMissingObservable.
  - Missing `_std` companion on followup → SkippedMissingObservable.
  - Non-finite σ on either side → SkippedMissingObservable.
  - Negative σ → SkippedMissingObservable.
  - Blank `ctx.ValueName` → InvalidSpec.
  - `factor` missing from `Parameters` → InvalidSpec.
  - `factor = 1` (no refinement) → InvalidSpec.
  - `factor = "abc"` (non-numeric) → InvalidSpec.
  - `Tolerance.ToleranceRel = 0` → InvalidSpec.
  - Pre-provided `TypedSpec` / `TypedPredicate` bypass the mapper (transparent wiring).
- [ ] **Step 2:** Write `LauncherVarianceRatioFakeRoleOutputsTests` with ≥6 red facts using a stubbed `IMrCatalogProvider` that emits a fake `MrBlueprint` with `AssertionTypeCode: "variance-ratio"` and a fake runner / parser that returns pre-baked metric / metric_std pairs without invoking Python:
  - Happy path end-to-end through `SystemMtLauncher.RunAsync(...)` → `MrRunResult.Passed == true`.
  - σ-shrink below threshold → `Passed == true`.
  - σ-shrink stalls → `Passed == false` with non-null `FailureReason`.
  - Output parser drops `_std` companion → `Passed == false` with diagnostic naming the missing observable.
  - `factor` override via `parameterOverrides` is respected.
  - Recorded `ExecutionEvidence.TypedVerification` carries `Status = Passed` and a non-null diagnostic on the happy path.
- [ ] **Step 3:** Run focused: `dotnet test MetBench_SystemMT.Tests --no-restore --filter "FullyQualifiedName~VarianceRatioPipelineWiringTests|FullyQualifiedName~LauncherVarianceRatioFakeRoleOutputsTests"` → all red (the wiring code does not exist yet).

## Task 2 — Minimal Implementation

**Files:**

- Modify: `SystemMtPipeline.cs`, `SystemMtLauncher.cs`, `PipelineContext.cs`, `TypedVerificationContextFactory.cs` (or `RoleOutputBuilder.cs`).
- Create: `VarianceRatioContextBuilder.cs` (or inline helper).

- [ ] **Step 1:** Implement `(MrSpec, VarianceRatioPredicate, VerificationContext)` builder. Pure / stateless. Validates the inputs from § Resolution Rules; throws structured `ArgumentException` for InvalidSpec cases so the pipeline can convert to `SystemMtAssertionResultV2.UnknownType`.
- [ ] **Step 2:** Extend the typed context factory: when a metric `m` exists and a companion key `m + "_std"` exists, promote the pair to a `RoleOutput.Statistical(name: m, value, StdError)`. Existing scalar-only outputs unaffected (no `_std` companion means no statistical entry — variance-ratio paths skip cleanly).
- [ ] **Step 3:** Add the `variance-ratio` arm to `SystemMtPipeline.EvaluateAssertion`. Same outcome shape as the existing scalar arm (typed assertion result, optional triple for evidence).
- [ ] **Step 4:** Launcher: when `blueprint.AssertionTypeCode == "variance-ratio"`, copy `DefaultParameters["factor"]` (with override priority) into `ExtraAssertionValues["refinement_factor"]` as defence-in-depth for the legacy `AssertionEvaluator` path.
- [ ] **Step 5:** Run focused → green.

## Task 3 — Full Suite

- [ ] **Step 1:** `dotnet test MetBench_SystemMT.Tests --no-restore` → green. Expected delta from current cloud-CI 1275 / 0 / 12 baseline: +18 facts → 1293 / 0 / 12 (no new SkippableFact added; everything in this PR runs deterministically without a venv).
- [ ] **Step 2:** Architecture guard: `SemanticCatalogBoundaryTests` stays green without an allow-list edit — the new wiring runs entirely inside `MetBench_BLL.Core/SystemMT/Pipeline/` + `Launcher/`, calling only already-allowed dispatch sites.

## Task 4 — Docs Projection

- [ ] **Step 1:** `docs/status/current.md`:
  - "PR-Bol-3 / Bol-Alg-02 MC particle count convergence" row updated: blocker removed, dependency moves to "Open — successor PR-N2 plan to be drafted to add the MR catalog row".
  - Add a new ledger row "Variance-ratio launcher pipeline wiring": Controlled — PR-VR merged. Cite the new test counts.
  - Bump baseline section to PR-VR head + new pass count.
- [ ] **Step 2:** `docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md`:
  - Move this plan (`2026-05-26-variance-ratio-launcher-pipeline-wiring-plan.md`) from §1 Queued/Active to §3 historical with summary block.
  - Update the PR-N2 row: PR-N2 successor plan needs drafting before PR-N2 restarts.
- [ ] **Step 3:** `docs/PROJECT-STRUCTURE.md`: no row changes (no MR / SUT / equation delta); add a one-line note in §3 if there is an architecture-surface table that mentions variance-ratio.

## Task 5 — Two-Layer Review and PR

- [ ] **Layer 1 self-review:**
  - No Method MT, no WPF, no SUT runner / parser edit, no new MR catalog row, no new EquationMetadata.
  - The new pipeline arm parallels the existing scalar arm; it does not replace it.
  - `MapScalar` semantics for `less` / `greater` / `approx` / `equal` are bit-for-bit unchanged (no edit to that switch).
  - `VarianceRatioKernel.Evaluate` is bit-for-bit unchanged; this PR only feeds it.
  - Stat-role promotion is purely additive — outputs without `_std` companions are untouched.
- [ ] **Layer 2 maintainer questions:**
  - Does the `_std` naming convention conflict with any existing output-parser key? Sweep the four output parsers (`openmoc_output_parser.py`, `openmc_output_parser.py`, the deterministic ones) and report.
  - Could a future MR accidentally promote a non-statistical pair (e.g. `peak_amplitude` + `peak_amplitude_std` if some parser invented one) and break a non-variance-ratio assertion? Answer: no — `Statistical` promotion is keyed on `AssertionTypeCode == "variance-ratio"`; other assertion arms read the scalar path.
  - Does the defence-in-depth `ExtraAssertionValues["refinement_factor"]` population create surprises for non-variance-ratio MRs? Answer: no — it only fires inside the `variance-ratio` arm.
  - Should the convention also support `_sigma` / `_stderr` aliases? Answer: not in this PR; `_std` only. Aliases can land as a follow-up if a real SUT requires them.
- [ ] **Step 1:** Commit: `feat(verif): wire variance-ratio assertion through SystemMtLauncher pipeline`.
- [ ] **Step 2:** Open PR; wait CI green; squash-merge.
- [ ] **Step 3:** Sync local main; draft successor PR-N2 plan (`2026-05-26-bol-alg-02-mc-particle-count-convergence-plan-v2.md` or equivalent) that revives only the catalog-row additions from the retracted plan.

## Acceptance Criteria

1. `dotnet test MetBench_SystemMT.Tests --no-restore` → green (cloud-CI ~1293 / 0 / 12).
2. Two new test files (`VarianceRatioPipelineWiringTests` ≥ 12 facts, `LauncherVarianceRatioFakeRoleOutputsTests` ≥ 6 facts) all green.
3. `LegacyCatalogFactory.cs` byte-diff vs `origin/main` is `+0 / −0` (no MR row consumes the wiring in this PR).
4. `MapScalar` behaviour unchanged for `less` / `greater` / `approx` / `equal` (pinned by existing tests).
5. `VarianceRatioKernel.Evaluate` source-byte-identical (the wiring composes upstream of the kernel).
6. Status ledger row "Variance-ratio launcher pipeline wiring" reads Controlled with the test surface.
7. `git diff --check origin/main...HEAD` clean.
8. `git diff --name-only origin/main...HEAD | grep -vE '^(MetBench_BLL\.Core/|MetBench_SystemMT\.Tests/|docs/)'` empty (no SUT / WPF / Method MT).
9. `SemanticCatalogBoundaryTests` still green without an allow-list edit.

## Stop Conditions

Stop and report (without coding further) if:

- The `_std` naming convention conflicts with a real key emitted by any existing output parser → escalate; choose a less ambiguous companion suffix and update the plan before resuming.
- `RoleOutput.Statistical` does not exist or has a fundamentally different shape than `(name, value, StdError)` → re-read `VarianceRatioKernel` and adjust the builder API; do not invent a new statistical type without an updated plan.
- `MapVarianceRatio` signature changed since 2026-05-26 (current: `(lowSampleRole, highSampleRole, statisticalMetric, sampleRatio, sigmaMultiplier)`) → adjust mapping accordingly.
- The pipeline cannot reach the new arm without a wider refactor of `EvaluateAssertion` (e.g. the method is locked behind a sealed switch with no extension point) → propose the refactor as a separate review-only commit before proceeding.

## Open questions (resolve before red tests)

1. **σ-multiplier source.** `StatisticalToleranceSpec(SigmaMultiplier)` is the kernel's tolerance input. The blueprint's `AssertionTolerance` record carries both `ToleranceRel` and `NoiseMultiplier`. Which one feeds `SigmaMultiplier`? Two candidates: (a) `Tolerance.NoiseMultiplier` (semantically: "how many σ"), (b) `Tolerance.ToleranceRel` (semantically: "relative slack"). The current `VarianceRatioKernel` test fixtures use `SigmaMultiplier` as the multiplier of `expectedStdError`, i.e. the **noise** scale, so (a) `NoiseMultiplier` is the natural mapping. Confirm before red tests.
2. **`factor` vs `refinement_factor`.** Existing legacy code in `AssertionEvaluator` reads `ExtraValues["refinement_factor"]`. Blueprint `DefaultParameters` use `"factor"` for the user-facing scaling knob. Pick one canonical key (`refinement_factor` matches the legacy code path, `factor` matches blueprint convention). Recommendation: blueprint stays `"factor"`, pipeline copies into `ExtraValues["refinement_factor"]`; the typed `SampleRatio` parameter expression also reads from `"factor"`. Document the mapping explicitly.

## Notes for the eventual PR-N2 successor plan

After this PR-VR merges, the PR-N2 successor plan reduces to:

- One `MrBlueprint` row in `LegacyCatalogFactory.cs` for `openmc-pincell-particle-count-convergence` (full code listed in the retracted PR-N2 plan).
- One `MrMetadata` row in `SystemMtMetadataCatalog.cs`.
- Pinned-count bumps 30 → 31 across the six descriptor files.
- One new `LauncherEndToEndOpenMcParticleCountConvergenceTests` `[SkippableFact]` gated on `OpenMcTestPaths.OpenMcImportable()`.
- Doc projections for status / project structure / requirements.

No further wiring needed.
