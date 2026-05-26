# T1 Same-Equation Cross-Method Differential Runner — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the "same-equation, cross-method differential" semantic (T1 §2.1 element 3 in `CLAUDE.md`) a first-class, code-driven, cloud-side capability with a single sanctioned API. Today the OpenMOC × OpenMC pair only runs through a BDD feature file (`Features/CrossProgramNeutronTransportMrs.feature` + `Steps/CrossProgramSteps.cs`) and an ad-hoc subset of pipeline glue — there is no `IDifferentialTestRunner` abstraction in `MetBench_BLL.Core`, so non-BDD callers (future binder consumers, future UI, future mutation-campaign analytics) cannot trigger or interpret cross-method runs without copy-pasting the BDD step logic.

**Architecture:** A pure-orchestration service in `MetBench_BLL.Core/SystemMT/Differential/` that runs two `ISystemMtLauncher` invocations and applies a small, deterministic agreement criterion over the two `MrRunResult` objects. No new typed predicate; no Method MT change; no SUT execution change; no WPF change. The existing `Catalog/Typed/Runtime/CrossMethodComparisonKernel` is **not** consumed — it operates on a single MR with two intra-MR roles, which is a different shape from the cross-MR pair we're orchestrating. We document that distinction in the runner XML doc so a future reviewer doesn't conflate the two.

**Tech Stack:** .NET 8, xUnit, existing `ISystemMtLauncher` / `MrRunResult`, no new external deps.

---

## Scope and Non-Goals

This is a cloud-side T1 plan. It is suitable for Linux/cloud execution because it only touches `MetBench_BLL.Core`, `MetBench_SystemMT.Tests`, and docs.

This plan must **not**:

- Add a new SUT, MR, or `EquationMetadata` row.
- Add a new typed predicate, kernel, or validator under `MetBench_BLL.Core/SystemMT/Catalog/Typed/`.
- Modify Method MT.
- Modify WPF / `MetBench_Client` / `App.xaml.cs`.
- Re-route the existing BDD `CrossProgramSteps.cs` through the new abstraction (orthogonal cleanup; out of scope here).
- Change `ISystemMtLauncher` / `MrRunResult` / persistence schemas.
- Touch the Python adapters or any SUT runner script.

It must:

- Add a single `IDifferentialTestRunner` interface + sealed default implementation.
- Add deterministic request / result / agreement DTOs.
- Reuse `ISystemMtLauncher.RunAsync(mrId, overrides, ct)` exactly as today — no new launcher overload.
- Fail closed on insufficient data (metric name mismatch, missing values, NaN/∞ values, one-side timeout).
- Carry an explicit `DifferentialDisagreementReason` enum so the caller (and audit trail) can tell "both failed", "directions disagreed", "ratio outside tolerance", "metric names mismatched", "one side errored" apart.

## Files

- Create: `MetBench_BLL.Core/SystemMT/Differential/IDifferentialTestRunner.cs`
- Create: `MetBench_BLL.Core/SystemMT/Differential/DifferentialRunRequest.cs`
- Create: `MetBench_BLL.Core/SystemMT/Differential/DifferentialRunResult.cs`
- Create: `MetBench_BLL.Core/SystemMT/Differential/DifferentialAgreementCriterion.cs`
- Create: `MetBench_BLL.Core/SystemMT/Differential/DifferentialAgreementStatus.cs`
- Create: `MetBench_BLL.Core/SystemMT/Differential/DifferentialDisagreementReason.cs`
- Create: `MetBench_BLL.Core/SystemMT/Differential/DifferentialTestRunner.cs`
- Create: `MetBench_SystemMT.Tests/SystemMT/Differential/DifferentialTestRunnerTests.cs`
- Modify: `docs/status/current.md`
- Modify: `docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md`

## Runner Contract

```csharp
namespace MetBench_BLL.SystemMT.Differential;

public interface IDifferentialTestRunner
{
    Task<DifferentialRunResult> RunPairAsync(
        DifferentialRunRequest request,
        CancellationToken ct = default);
}

public sealed record DifferentialRunRequest(
    string LeftMrId,
    string RightMrId,
    DifferentialAgreementCriterion Criterion,
    double Tolerance = 0.0,
    IReadOnlyDictionary<string, string>? LeftParameterOverrides = null,
    IReadOnlyDictionary<string, string>? RightParameterOverrides = null);

public enum DifferentialAgreementCriterion
{
    /// <summary>Both MR assertions pass independently. No numeric comparison.</summary>
    BothPassed,
    /// <summary>Both runs move the metric in the same direction (sign(FollowUp - Source) matches).</summary>
    DirectionConcordant,
    /// <summary>|left.FollowUpRatio - right.FollowUpRatio| ≤ Tolerance, where FollowUpRatio = FollowUp / Source.</summary>
    FollowUpRatioWithinTolerance,
}

public enum DifferentialAgreementStatus { Agree, Disagree, Inconclusive }

public enum DifferentialDisagreementReason
{
    None,
    BothFailed,
    LeftFailed,
    RightFailed,
    DirectionsDisagreed,
    RatioOutsideTolerance,
    MetricNameMismatch,
    NonFiniteValue,
    ToleranceNegative,
}

public sealed record DifferentialRunResult(
    MrRunResult Left,
    MrRunResult Right,
    DifferentialAgreementStatus Status,
    DifferentialDisagreementReason Reason,
    string? Diagnostic);
```

### Resolution rules (deterministic, total)

Apply in order. The first rule that fires determines `(Status, Reason, Diagnostic)`:

1. `Tolerance < 0` → `(Inconclusive, ToleranceNegative, "Tolerance must be >= 0, got <T>.")`.
2. Either `RunAsync` throws → propagate to the caller (do not swallow).
3. `left.ValueName != right.ValueName` → `(Inconclusive, MetricNameMismatch, "Left metric '<L>' != right metric '<R>'.")`.
4. Any of `SourceValue` / `FollowUpValue` is `NaN`, `±∞`, or `0` for the source (division by zero in ratio criterion) → `(Inconclusive, NonFiniteValue, "...")`.
   For criteria that do not need the ratio, the zero-source check is skipped.
5. Criterion-specific:
   - `BothPassed` → `Agree` iff `left.Passed && right.Passed`; otherwise `Disagree` with `BothFailed` / `LeftFailed` / `RightFailed`.
   - `DirectionConcordant` → `Agree` iff `sign(left.FollowUpValue - left.SourceValue) == sign(right.FollowUpValue - right.SourceValue)`; ties (both diffs == 0) are `Agree`; otherwise `Disagree` with `DirectionsDisagreed`.
   - `FollowUpRatioWithinTolerance` → `Agree` iff `|left.FU/SR - right.FU/SR| <= Tolerance`; otherwise `Disagree` with `RatioOutsideTolerance`.

The runner does **not** consult `MrFamily` or any catalog metadata. The caller is responsible for asking for a pairing that makes physical sense; the runner only enforces the mechanical agreement criterion.

## Task 1: Pin Boundary With Failing Tests

**Files:**

- Test: `MetBench_SystemMT.Tests/SystemMT/Differential/DifferentialTestRunnerTests.cs`

- [ ] **Step 1:** Add a fake `ISystemMtLauncher` that returns canned `MrRunResult` per MR id (mirrors the in-memory `FakeExecRepo` pattern already used by `LauncherEndToEnd*Tests.cs`). The fake exposes a `Returns` configuration so each test sets `(mrId → MrRunResult)`.
- [ ] **Step 2:** Write **at least 14 failing facts** covering:
  - happy path BothPassed (Agree)
  - happy path DirectionConcordant
  - happy path FollowUpRatioWithinTolerance
  - both failed → Disagree, `BothFailed`
  - left failed → Disagree, `LeftFailed`
  - right failed → Disagree, `RightFailed`
  - directions disagreed (one increase, one decrease) → Disagree, `DirectionsDisagreed`
  - ratio outside tolerance → Disagree, `RatioOutsideTolerance`
  - metric name mismatch → Inconclusive, `MetricNameMismatch`
  - NaN in any of the four values (Theory) → Inconclusive, `NonFiniteValue`
  - ±∞ in any value (Theory) → Inconclusive, `NonFiniteValue`
  - zero source value under `FollowUpRatioWithinTolerance` → Inconclusive, `NonFiniteValue`
  - zero source value under `BothPassed` / `DirectionConcordant` is **allowed** (no division)
  - negative tolerance → Inconclusive, `ToleranceNegative`
  - launcher throws → propagated (no swallow)
  - request null → `ArgumentNullException`
  - both MR ids equal (degenerate) → still permitted; runs the launcher twice with the same MR id and applies the criterion
  - parameter overrides forwarded to the launcher per side (verified by the fake)
- [ ] **Step 3:** Run focused tests and verify red:
  ```bash
  dotnet test MetBench_SystemMT.Tests --no-restore --filter FullyQualifiedName~DifferentialTestRunnerTests
  ```
  Expected: compile errors (types do not exist) followed by fail.

## Task 2: Implement DTOs + Runner

**Files:**

- Create the seven new types listed under "Files".

- [ ] **Step 1:** Add the DTOs, enums, and interface verbatim from the contract above.
- [ ] **Step 2:** Implement `DifferentialTestRunner`:
  - Constructor injects `ISystemMtLauncher` (and only that — keep the runner stateless).
  - `RunPairAsync` runs the two launcher calls sequentially (no `Task.WhenAll` — keep ordering predictable for diagnostics).
  - Apply resolution rules above in order; build the diagnostic string with the offending values for readability.
- [ ] **Step 3:** Run focused tests:
  ```bash
  dotnet test MetBench_SystemMT.Tests --no-restore --filter FullyQualifiedName~DifferentialTestRunnerTests
  ```
  Expected: all pass.

## Task 3: Full Suite + Architecture Guard

- [ ] **Step 1:**
  ```bash
  dotnet test MetBench_SystemMT.Tests --no-restore
  ```
  Expected: full suite green (~1186 / 0 / 8 with SciPy locally; ~1182 / 0 / 12 cloud-shape).
- [ ] **Step 2:** Confirm `SemanticCatalogBoundaryTests` still pass without an allow-list edit. The runner does not reference `AssertionTypeCodes.*` (it only consumes `MrRunResult`'s value fields), so no boundary edit should be needed.

## Task 4: Docs

**Files:**

- Modify: `docs/status/current.md`
- Modify: `docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md`

- [ ] **Step 1:** Move `T1 same-equation cross-method differential` row from Open → Controlled in `docs/status/current.md` §3 with the runner contract + test surface.
- [ ] **Step 2:** Bump the baseline section to this PR's head + the new pass count.
- [ ] **Step 3:** Retire this plan to §3 of the active plan index (already-merged section), like PR-1 / PR-2 did.

## Task 5: Two-Layer Review and PR

- [ ] **Layer 1 self-review:**
  - No Method MT, no WPF, no SUT execution change.
  - No new typed predicate.
  - Runner is stateless and IO-free except for the two launcher calls it explicitly orchestrates.
  - Resolution rules cover every input the runner can see (proven by Theory tests on NaN / ±∞ / zero source).
- [ ] **Layer 2 maintainer review:**
  - Could the BDD `CrossProgramSteps.cs` be silently broken by this PR? Expected answer: no — the runner is additive; BDD steps are unchanged.
  - Could a future caller bypass `ISystemMtLauncher` and feed hand-crafted `MrRunResult` instances into the runner? Yes — the runner is intentionally agnostic to where the results came from, but the launcher pair is the only sanctioned production source.
  - Could the runner be reused for same-MR / same-method but different parameter overrides? Yes — passing the same `MrId` on both sides with different `*ParameterOverrides` works by design; pinned by `runs_the_launcher_twice_with_the_same_mr_id` fact.
- [ ] Commit, push, open PR titled `feat(t1): add same-equation cross-method differential runner`.

## Acceptance Criteria

- `IDifferentialTestRunner` is the only sanctioned API for "run two MRs and compare the metric" — the BDD step file remains unchanged but is now functionally a special case of this runner.
- All three agreement criteria (BothPassed / DirectionConcordant / FollowUpRatioWithinTolerance) are implemented and pinned.
- Every input the runner can see is total (no `default` fall-through) — proven by Theory tests.
- Full `MetBench_SystemMT.Tests` is green.
- Status ledger row moved Open → Controlled.

## Stop Conditions

Stop and report without coding if:

- `origin/main` is unreachable.
- The existing `ISystemMtLauncher.RunAsync` signature has changed such that fake substitution requires re-architecting the test surface.
- Implementation cannot avoid touching Method MT, WPF, or the Typed Semantic Catalog runtime.
