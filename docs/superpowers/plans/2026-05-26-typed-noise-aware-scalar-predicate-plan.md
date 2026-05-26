# Typed Noise-Aware Scalar Predicate — Implementation Plan (PR-N1)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a `NoiseAwareBinaryComparisonPredicate` typed spec + kernel + validator + `LegacyAssertionPredicateMapper.MapNoiseAwareScalar` overload so the two currently fail-closed legacy assertion codes (`less-noise-aware`, `greater-noise-aware`) can be mapped to a typed predicate. Unblocks PR-N2 (Bol-Alg-02 MC particle count convergence on OpenMC) and any future MC / probabilistic SUT m_mono MR.

**Architecture:** Mirror the existing `BinaryComparisonPredicate` / `BinaryComparisonKernel` / `BinaryComparisonPredicateValidator` triple. Add a parallel `NoiseAwareScalarToleranceEvaluator` because the existing `DeterministicScalarToleranceEvaluator` only computes deterministic tolerance (`max(Atol, Rtol·|expected|)`) — noise-aware tolerance is `NoiseMultiplier · √(SourceStd² + FollowupStd²)`. No Method MT, no WPF, no new SUT, no new MR; this PR ships pure type-system + kernel + validator + mapper. The two legacy codes become mappable but no MR catalog row consumes them yet (PR-N2 is the first consumer).

**Tech Stack:** .NET 8, xUnit, existing `MetBench_BLL.Core/SystemMT/Catalog/Typed/`, no new external deps.

---

## Scope and Non-Goals

This is a cloud-side typed-catalog plan. It is suitable for Linux/cloud execution because it only touches `MetBench_BLL.Core/SystemMT/Catalog/Typed/` and `MetBench_SystemMT.Tests/`.

This plan must **not**:

- Add a new MR id, SUT, or `EquationMetadata` row.
- Modify any existing MR catalog binding's `AssertionTypeCode` (no behaviour change for shipped MRs).
- Modify `BinaryComparisonPredicate` / `BinaryComparisonKernel` / `DeterministicScalarToleranceEvaluator` (parallel, not replace).
- Modify `SystemMtPipeline` / `ISystemMtLauncher` / `MrRunResult` / persistence schemas.
- Touch Method MT.
- Touch WPF / `MetBench_Client` / `App.xaml.cs`.
- Touch any Python adapter or SUT runner script.

It must:

- Ship a new sealed record `NoiseAwareBinaryComparisonPredicate(PredicateId, LeftRole, RightRole, Metric, Operator, SourceStdMetric, FollowupStdMetric, NoiseMultiplier)` deriving from `PredicateSpec`.
- Ship a new `NoiseAwareScalarToleranceEvaluator` exposing `ComputeTolerance(double sourceStd, double followupStd, double noiseMultiplier)` → `noiseMultiplier · √(sourceStd² + followupStd²)`.
- Ship a new `NoiseAwareBinaryComparisonKernel : IVerifierKernel<NoiseAwareBinaryComparisonPredicate>` that evaluates `actual ⟂ expected ± noiseTolerance` for `Operator ∈ {"Greater", "Less"}` (Equal is intentionally NOT supported here — noise-aware equality is a separate semantics that the legacy codes do not declare).
- Ship a new `NoiseAwareBinaryComparisonPredicateValidator` mirroring `BinaryComparisonPredicateValidator`, additionally validating `SourceStdMetric` / `FollowupStdMetric` metrics exist and `NoiseMultiplier > 0`.
- Register the new validator in `ValidationRegistry`.
- Register the new kernel + spec in `PredicateDispatcher`.
- Replace `LegacyAssertionPredicateMapper`'s noise-aware fail-closed `throw` with a new `MapNoiseAwareScalar(actualRole, expectedRole, metric, sourceStdMetric, followupStdMetric, noiseMultiplier, "Greater"|"Less")` overload that emits a `NoiseAwareBinaryComparisonPredicate`. The original `MapScalar` switch still throws for noise-aware codes UNLESS callers explicitly route through `MapNoiseAwareScalar` (the noise-aware codes need extra inputs that the legacy scalar signature does not carry — fail-closed remains the default).

## Files

- Create: `MetBench_BLL.Core/SystemMT/Catalog/Typed/Specs/PredicateSpec.cs` (add the new record next to existing predicate records).
- Create: `MetBench_BLL.Core/SystemMT/Catalog/Typed/Runtime/NoiseAwareScalarToleranceEvaluator.cs`
- Create: `MetBench_BLL.Core/SystemMT/Catalog/Typed/Runtime/NoiseAwareBinaryComparisonKernel.cs`
- Create: `MetBench_BLL.Core/SystemMT/Catalog/Typed/Validation/NoiseAwareBinaryComparisonPredicateValidator.cs`
- Modify: `MetBench_BLL.Core/SystemMT/Catalog/Typed/Validation/ValidationRegistry.cs` (register new validator).
- Modify: `MetBench_BLL.Core/SystemMT/Catalog/Typed/Runtime/PredicateDispatcher.cs` (add new switch arm).
- Modify: `MetBench_BLL.Core/SystemMT/Catalog/Typed/Migration/LegacyAssertionPredicateMapper.cs` (add `MapNoiseAwareScalar` overload; `MapScalar` fail-closed message UPDATED to point at the new overload rather than say "Add a noise-aware typed predicate before mapping").
- Create: `MetBench_SystemMT.Tests/SystemMT/Catalog/Typed/NoiseAwareScalarToleranceEvaluatorTests.cs`
- Create: `MetBench_SystemMT.Tests/SystemMT/Catalog/Typed/NoiseAwareBinaryComparisonKernelTests.cs`
- Create: `MetBench_SystemMT.Tests/SystemMT/Catalog/Typed/NoiseAwareBinaryComparisonPredicateValidatorTests.cs`
- Modify: `MetBench_SystemMT.Tests/SystemMT/Catalog/Typed/LegacyAssertionPredicateMapperTests.cs` (add 2 new facts pinning `MapNoiseAwareScalar`).
- Modify: `MetBench_SystemMT.Tests/SystemMT/Catalog/Typed/PredicateDispatcherTests.cs` (1 new fact pinning the dispatch arm).
- Modify: `docs/status/current.md` (close the noise-aware fail-closed row).
- Modify: `docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md` (retire this plan to §3 after merge).

## Type Contract

```csharp
namespace MetBench_BLL.SystemMT.Catalog.Typed.Specs;

public sealed record NoiseAwareBinaryComparisonPredicate(
    string PredicateId,
    string LeftRole,
    string RightRole,
    string Metric,
    string Operator,          // "Greater" | "Less"   (Equal NOT supported — out of scope)
    string SourceStdMetric,   // metric name resolving to σ_source
    string FollowupStdMetric, // metric name resolving to σ_followup
    double NoiseMultiplier)   // > 0, typically 3.0 for 3σ band
    : PredicateSpec(PredicateId);
```

```csharp
namespace MetBench_BLL.SystemMT.Catalog.Typed.Runtime;

public sealed class NoiseAwareScalarToleranceEvaluator
{
    public double ComputeTolerance(double sourceStd, double followupStd, double noiseMultiplier)
    {
        if (!double.IsFinite(sourceStd) || sourceStd < 0)
            throw new ArgumentException($"sourceStd must be finite, non-negative; got {sourceStd}");
        if (!double.IsFinite(followupStd) || followupStd < 0)
            throw new ArgumentException($"followupStd must be finite, non-negative; got {followupStd}");
        if (!double.IsFinite(noiseMultiplier) || noiseMultiplier <= 0)
            throw new ArgumentException($"noiseMultiplier must be finite, > 0; got {noiseMultiplier}");
        return noiseMultiplier * Math.Sqrt(sourceStd * sourceStd + followupStd * followupStd);
    }
}
```

## Resolution Rules

`NoiseAwareBinaryComparisonKernel.Evaluate(predicate, context)` returns `VerificationResult` with status:

1. **InvalidSpec** if any of `LeftRole` / `RightRole` / `Metric` / `SourceStdMetric` / `FollowupStdMetric` are blank, or `NoiseMultiplier ≤ 0` / NaN / ±∞. (Validator should have caught these before runtime; runtime check is belt-and-suspenders.)
2. **Skipped** if any of the four metric lookups (`left[Metric]`, `right[Metric]`, `left[SourceStdMetric]`, `right[FollowupStdMetric]`) is missing on the role output; diagnostic names the missing role+metric. (Mirrors `BinaryComparisonKernel`'s skip rule for missing metrics.)
3. **Pass** if `Operator == "Greater"` and `right.Metric > left.Metric - tolerance`. Pass if `Operator == "Less"` and `right.Metric < left.Metric + tolerance`. (Tolerance widens the "no-significant-change" zone; passing means the directional move is statistically distinguishable from noise.)
4. **Fail** otherwise. Diagnostic carries `left.Metric`, `right.Metric`, computed tolerance, and `Operator`.
5. Unknown `Operator` (anything besides "Greater" / "Less") → **InvalidSpec**.

The kernel does **not** evaluate "Equal" — by design. Equal-with-noise is a different statistical test (`|left - right| ≤ tolerance`, two-sided) and would need its own predicate.

## Task 1: Pin Tolerance Evaluator With Failing Tests

**Files:**

- Test: `MetBench_SystemMT.Tests/SystemMT/Catalog/Typed/NoiseAwareScalarToleranceEvaluatorTests.cs`

- [ ] **Step 1:** Add facts for `ComputeTolerance`:
  - Happy path: `(σ_s=1, σ_f=1, k=3)` → `3·√2 ≈ 4.2426`.
  - Equal σ both zero: → `0`.
  - Asymmetric: `(σ_s=2, σ_f=1, k=1)` → `√5 ≈ 2.2361`.
  - Negative σ_s rejected with `ArgumentException`.
  - NaN σ_f rejected.
  - `NoiseMultiplier ≤ 0` rejected.
- [ ] **Step 2:** Run focused tests → red (evaluator does not exist).

## Task 2: Pin Kernel Contract With Failing Tests

**Files:**

- Test: `MetBench_SystemMT.Tests/SystemMT/Catalog/Typed/NoiseAwareBinaryComparisonKernelTests.cs`

- [ ] **Step 1:** Build a helper that constructs `VerificationContext` with two `RoleOutput`s carrying `Metric` + `SourceStdMetric` + `FollowupStdMetric` values (mirror `BinaryComparisonKernelTests.cs` setup).
- [ ] **Step 2:** ≥ 12 facts after Theory expansion:
  - Happy `Greater`: `left.k=1.0, right.k=2.0, σ_s=0.1, σ_f=0.1, k=3 → tol=0.424; 2.0 > 1.0 - 0.424 → Pass`.
  - Happy `Less`: `left.k=2.0, right.k=1.0, σ_s=0.1, σ_f=0.1, k=3 → 1.0 < 2.0 + 0.424 → Pass`.
  - Direction passes but inside noise band → still Pass (noise-aware permits tiny moves in either direction).
  - Direction reversed beyond noise band → Fail.
  - Missing `Metric` on right → Skipped + diagnostic names role + metric.
  - Missing `SourceStdMetric` on left → Skipped.
  - Missing `FollowupStdMetric` on right → Skipped.
  - Blank `Operator` → InvalidSpec.
  - Operator `"Equal"` → InvalidSpec with message naming Equal as out of scope.
  - Operator `"NotEqual"` → InvalidSpec.
  - NaN `σ_source` → InvalidSpec.
  - Theory: `(NaN, ±∞)` on any of the four metric lookups → Skipped (the metric existed but the value is non-finite — treat as missing data).
- [ ] **Step 3:** Run focused tests → red.

## Task 3: Pin Validator With Failing Tests

**Files:**

- Test: `MetBench_SystemMT.Tests/SystemMT/Catalog/Typed/NoiseAwareBinaryComparisonPredicateValidatorTests.cs`

- [ ] **Step 1:** Build a fake `SharedReferenceResolver` with two roles, each declaring `k_eff`, `k_eff_std_source`, `k_eff_std_followup`.
- [ ] **Step 2:** ≥ 8 facts:
  - Happy path: all fields valid → no error.
  - Blank `LeftRole` / `RightRole` / `Metric` / `SourceStdMetric` / `FollowupStdMetric` → error naming the offending field.
  - `LeftRole` not in resolver → error names the unknown role.
  - `Metric` not declared on either role → error names the missing metric.
  - `NoiseMultiplier = 0` → error.
  - `NoiseMultiplier = -1` → error.
  - `NoiseMultiplier = NaN` → error.
  - `Operator` not in `{"Greater", "Less"}` → error naming the rejected operator.
- [ ] **Step 3:** Run focused tests → red.

## Task 4: Implement Spec + Evaluator + Kernel + Validator

**Files:**

- See Files list. Each `Create:` file follows the test surface above. Implement minimum code to flip all three test suites green.

- [ ] **Step 1:** Add the spec record to `PredicateSpec.cs`.
- [ ] **Step 2:** Implement `NoiseAwareScalarToleranceEvaluator`.
- [ ] **Step 3:** Implement `NoiseAwareBinaryComparisonKernel`.
- [ ] **Step 4:** Implement `NoiseAwareBinaryComparisonPredicateValidator` + register in `ValidationRegistry`.
- [ ] **Step 5:** Run all three new test files → green.

## Task 5: Wire PredicateDispatcher

**Files:**

- Modify: `MetBench_BLL.Core/SystemMT/Catalog/Typed/Runtime/PredicateDispatcher.cs`
- Modify: `MetBench_SystemMT.Tests/SystemMT/Catalog/Typed/PredicateDispatcherTests.cs`

- [ ] **Step 1:** Add a new switch arm: `NoiseAwareBinaryComparisonPredicate noiseAware => _noiseAware.Evaluate(noiseAware, context),`. Field initialised in ctor.
- [ ] **Step 2:** Add 1 new fact pinning the dispatch (a `NoiseAwareBinaryComparisonPredicate` reaches the kernel and returns its `VerificationResult`).
- [ ] **Step 3:** Run dispatcher tests → green.

## Task 6: Open the LegacyAssertionPredicateMapper Path

**Files:**

- Modify: `MetBench_BLL.Core/SystemMT/Catalog/Typed/Migration/LegacyAssertionPredicateMapper.cs`
- Modify: `MetBench_SystemMT.Tests/SystemMT/Catalog/Typed/LegacyAssertionPredicateMapperTests.cs`

- [ ] **Step 1:** Add a new static method:
  ```csharp
  public static PredicateSpec MapNoiseAwareScalar(
      string actualRole, string expectedRole,
      string metric, string sourceStdMetric, string followupStdMetric,
      double noiseMultiplier, string operatorName) { ... }
  ```
  Returns a `NoiseAwareBinaryComparisonPredicate` with `LeftRole = expectedRole` (source), `RightRole = actualRole` (followup).
- [ ] **Step 2:** Update `MapScalar`'s noise-aware fail-closed message to say:

  > Legacy assertion code 'less-noise-aware' / 'greater-noise-aware' requires noise-aware inputs (SourceStdMetric / FollowupStdMetric / NoiseMultiplier) that the scalar signature does not carry. Route this MR's typed mapping through `LegacyAssertionPredicateMapper.MapNoiseAwareScalar(...)` instead.

  (The fail-closed throw stays — the bare `MapScalar` cannot route to noise-aware because it does not have the extra inputs. The message now points callers at the correct entry.)
- [ ] **Step 3:** Add 2 facts:
  - `MapNoiseAwareScalar` happy path emits a `NoiseAwareBinaryComparisonPredicate` with the four metric names and operator wired correctly.
  - Blank `sourceStdMetric` rejected with `ArgumentException` (mirrors `MapScalar` blank-input checks).
- [ ] **Step 4:** Run mapper tests → green.

## Task 7: Full Suite + Architecture Guard

- [ ] **Step 1:** `dotnet test MetBench_SystemMT.Tests --no-restore`. Expected: green; new tests add ~25 facts; full count moves from 1209 (cloud CI) to ~1234.
- [ ] **Step 2:** Confirm `SemanticCatalogBoundaryTests` still pass — no new file outside the existing allow-list references `AssertionTypeCodes.*` (the new noise-aware path is typed-catalog-native and does not reach for the legacy codes string list).

## Task 8: Docs

**Files:**

- Modify: `docs/status/current.md`
- Modify: `docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md`

- [ ] **Step 1:** Update `docs/status/current.md` §3 "Legacy assertion code mapping" row: move the noise-aware codes from "intentionally fail-closed" to "mappable via `LegacyAssertionPredicateMapper.MapNoiseAwareScalar`"; keep the documentation that bare `MapScalar` still rejects them so noise-aware codes cannot be silently mapped without the extra inputs.
- [ ] **Step 2:** Add a §3 row "Noise-aware typed scalar predicate" → Controlled.
- [ ] **Step 3:** Bump baseline section to PR-N1 head + the new pass count.
- [ ] **Step 4:** Retire this plan to §3 of the active plan index after merge.

## Task 9: Two-Layer Review and PR

- [ ] **Layer 1 self-review:**
  - No new MR, no new SUT, no new EquationKey.
  - No Method MT, no WPF.
  - `NoiseAwareBinaryComparisonKernel` is the parallel of `BinaryComparisonKernel`, not a replacement.
  - Existing MRs unchanged — pin by full-suite passes without modifying any catalog binding.
- [ ] **Layer 2 maintainer review:**
  - Does the message in the still-rejecting `MapScalar` path lead callers to the new overload? Expected: yes (Task 6 Step 2).
  - Could the new predicate accidentally collapse Equal-with-noise into the Greater/Less branch? Expected: no — `Operator = "Equal"` is explicitly InvalidSpec, pinned by test.
  - Is there a risk the noise-aware tolerance allows a real direction flip to silently pass? Expected: no, the kernel still checks the directional inequality; tolerance only widens the threshold.
- [ ] Commit: `feat(verif): add noise-aware typed scalar predicate (NoiseAwareBinaryComparisonPredicate)`.

## Acceptance Criteria

- The new predicate / evaluator / kernel / validator ship together with a registered dispatcher case and a `LegacyAssertionPredicateMapper.MapNoiseAwareScalar` overload.
- Full `MetBench_SystemMT.Tests` is green (~1234 facts after this PR).
- Status ledger row "Legacy assertion code mapping" no longer lists noise-aware codes as intentionally-fail-closed; a new row marks the noise-aware typed predicate as Controlled.
- No existing MR catalog binding's assertion code changes (binary diff on `LegacyCatalogFactory.cs` is `+0 / −0` for `AssertionTypeCode:` literals).

## Stop Conditions

Stop and report without coding if:

- `origin/main` is unreachable.
- The status ledger contains a newer plan that supersedes this one.
- The implementation would require redesigning `PredicateSpec` / `IVerifierKernel` / `VerificationContext`.
- A test reveals that `NoiseAwareBinaryComparisonKernel` cannot reuse `RoleOutput` / `VerificationContext` and would need a new context shape.
