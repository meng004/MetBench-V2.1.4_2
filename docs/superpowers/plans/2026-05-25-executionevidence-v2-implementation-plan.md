# ExecutionEvidence v2 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Extend `ExecutionEvidence` with a forward-only `TypedVerification` block that projects the typed `VerificationResult` / `PropertyResult` from the System MT Typed Semantic Catalog runtime, without changing the v1 schema, the LiteDB collection layout, the `SystemMtResultRecord` summary shape, the `ISystemMtLauncher` facade, or the `HtmlSystemMtResultReportRenderer` signature.

**Architecture:** Implementation runs as one design lock PR (this PR-A0, docs only) followed by one implementation PR (PR-C0 of evidence) that PR-C of verification semantics convergence consumes. PR-C0 only edits `MetBench_BLL.Core/SystemMT/Persistence/**`, `MetBench_BLL.Core/SystemMT/Pipeline/SystemMtExecutionRecorder.cs`, and tests under `MetBench_SystemMT.Tests/SystemMT/Persistence/**` + `MetBench_SystemMT.Tests/SystemMT/Pipeline/**`.

**Tech Stack:** C#/.NET 8, LiteDB, xUnit. Cross-platform `net8.0` only; no WPF, no Method MT.

---

## Preconditions

- Start every PR from latest `origin/main`.
- Confirm `docs/status/current.md` open risk "ExecutionEvidence final shape" is in the "Design locked" state before any code work begins.
- Confirm `docs/superpowers/specs/2026-05-25-executionevidence-v2-design.md` is merged before PR-C of verification semantics convergence references it.
- Do not touch Method MT files or WPF / `MetBench_Client/**` in any PR of this plan.
- Use two-layer review before push: implementation self-review plus independent code-review pass.

## File Structure

### PR-A0: Design Lock (this PR)

- Create: `docs/superpowers/specs/2026-05-25-executionevidence-v2-design.md`
- Create: `docs/superpowers/plans/2026-05-25-executionevidence-v2-implementation-plan.md`
- Modify: `docs/status/current.md`
- Modify: `docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md`

### PR-C0: Evidence v2 Schema and Recorder

- Modify: `MetBench_BLL.Core/SystemMT/Persistence/ExecutionEvidence.cs`
- Create: `MetBench_BLL.Core/SystemMT/Persistence/TypedVerificationEvidence.cs`
- Create: `MetBench_BLL.Core/SystemMT/Persistence/TypedDiagnosticEvidence.cs`
- Create: `MetBench_BLL.Core/SystemMT/Persistence/TypedPropertyPredicateEvidence.cs`
- Create: `MetBench_BLL.Core/SystemMT/Persistence/TypedVerificationEvidenceMapper.cs`
- Modify: `MetBench_BLL.Core/SystemMT/Pipeline/SystemMtExecutionRecorder.cs`
- Create: `MetBench_SystemMT.Tests/SystemMT/Persistence/TypedVerificationEvidenceRoundtripTests.cs`
- Create: `MetBench_SystemMT.Tests/SystemMT/Persistence/TypedVerificationEvidenceMapperTests.cs`
- Modify: `MetBench_SystemMT.Tests/SystemMT/Pipeline/ExecutionEvidenceWriteThroughTests.cs` (additions only; existing assertions must keep passing)
- Add: `MetBench_SystemMT.Tests/SystemMT/Launcher/MrRunResultShapeLockTests.cs`

PR-C of verification semantics convergence is the runtime convergence PR. It depends on PR-C0 being merged. PR-C0 must not depend on PR-C runtime changes; it must be self-sufficient and not destabilize existing tests.

---

## PR-A0: Design Lock

### Task A0-1: Create Design Document

**Files:**
- Create: `docs/superpowers/specs/2026-05-25-executionevidence-v2-design.md`

- [x] **Step 1: Write the design document**

The design must answer the six required questions:

- Final ExecutionEvidence v2 fields and lifecycle.
- Mapping from typed VerificationResult to persistence/reporting.
- Compatibility contract.
- What PR-C may change.
- What PR-C must not change.
- Test matrix and review gates.

### Task A0-2: Create Implementation Plan

**Files:**
- Create: `docs/superpowers/plans/2026-05-25-executionevidence-v2-implementation-plan.md`

- [x] **Step 1: Write this implementation plan with PR-A0 design-lock tasks and PR-C0 schema tasks.**

### Task A0-3: Update Status Ledger

**Files:**
- Modify: `docs/status/current.md`

- [ ] **Step 1: Move "ExecutionEvidence final shape" risk to "Design locked"**

The open-risk row in §6 of the ledger must change from `Open` to `Design locked` and link this design document, and the §4 active-control-documents table must add a row for the new design document and plan.

The §7 execution-order step that says "Design ExecutionEvidence v2" must be edited in place to point to the locked design and the implementation plan, not deleted.

### Task A0-4: Update Active Plan Index

**Files:**
- Modify: `docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md`

- [ ] **Step 1: Register the implementation plan in §1 (active plans) and the design document in §2 (active designs).**

Expiry conditions:

- Plan expires when PR-C0 merges and the status ledger marks ExecutionEvidence v2 implemented.
- Design expires only by explicit replacement PR (next major evidence redesign).

### Task A0-5: Validate Docs-Only PR-A0

- [ ] **Step 1: Confirm only docs files changed**

Run:

```bash
git diff --name-only origin/main...HEAD
```

Expected: only paths under `docs/`.

- [ ] **Step 2: Run whitespace check**

Run:

```bash
git diff --check origin/main...HEAD
```

Expected: no whitespace errors.

- [ ] **Step 3: Run placeholder scan**

Run:

```bash
rg -n "TB[D]|TO[D]O|implement[ ]later|fill[ ]in|待[定]|稍[后]|占[位]" docs/superpowers/specs/2026-05-25-executionevidence-v2-design.md docs/superpowers/plans/2026-05-25-executionevidence-v2-implementation-plan.md docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md docs/status/current.md
```

Expected: no matches.

- [ ] **Step 4: Commit and push PR-A0**

```bash
git add docs/superpowers/specs/2026-05-25-executionevidence-v2-design.md docs/superpowers/plans/2026-05-25-executionevidence-v2-implementation-plan.md docs/status/current.md docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md
git commit -m "docs(governance): lock executionevidence v2 design"
git push -u origin claude/tender-heisenberg-C0e7I
```

---

## PR-C0: Evidence v2 Schema and Recorder Projection

PR-C0 is the implementation PR. It is gated by PR-A0 merging.

### Task C0-1: Add Persistence POCOs

**Files:**
- Modify: `MetBench_BLL.Core/SystemMT/Persistence/ExecutionEvidence.cs`
- Create: `MetBench_BLL.Core/SystemMT/Persistence/TypedVerificationEvidence.cs`
- Create: `MetBench_BLL.Core/SystemMT/Persistence/TypedDiagnosticEvidence.cs`
- Create: `MetBench_BLL.Core/SystemMT/Persistence/TypedPropertyPredicateEvidence.cs`

- [ ] **Step 1: Add `TypedVerificationEvidence` POCO**

The POCO must match the design (§3 of `docs/superpowers/specs/2026-05-25-executionevidence-v2-design.md`) exactly, with parameterless constructor, public setters, default initial values, and no engine-internal types.

- [ ] **Step 2: Add `TypedDiagnosticEvidence` and `TypedPropertyPredicateEvidence` POCOs**

Both POCOs use only primitive types and `string?` plus `double?` for nullable numeric fields. No `JsonElement`, no `object?` typed at field level in persistence shape.

- [ ] **Step 3: Add nullable `TypedVerification` property to `ExecutionEvidence`**

Add `public TypedVerificationEvidence? TypedVerification { get; set; }` immediately after `RecordedAtUtc`. Do not change any existing v1 property.

- [ ] **Step 4: Verify LiteDB mapping is unchanged**

`LiteDbExecutionEvidenceRepository` must not need any new `BsonMapper` registration. The default mapping handles nullable reference type properties by omitting them from the BSON document when null.

Run focused round-trip test from §C0-3 and confirm legacy rows still load with `TypedVerification == null`.

### Task C0-2: Add Mapping Helper

**Files:**
- Create: `MetBench_BLL.Core/SystemMT/Persistence/TypedVerificationEvidenceMapper.cs`

- [ ] **Step 1: Add stateless mapper**

```csharp
public static class TypedVerificationEvidenceMapper
{
    public static TypedVerificationEvidence FromVerificationResult(
        MetBench_BLL.SystemMT.Catalog.Typed.Specs.MrSpec spec,
        MetBench_BLL.SystemMT.Catalog.Typed.Specs.PredicateSpec predicate,
        MetBench_BLL.SystemMT.Catalog.Typed.Runtime.VerificationResult result);

    public static TypedVerificationEvidence FromPropertyResult(
        MetBench_BLL.SystemMT.Catalog.Typed.Property.PropertySpec spec,
        MetBench_BLL.SystemMT.Catalog.Typed.Property.PropertyResult result);
}
```

Notes:

- The mapper uses `System.Text.Json` with default `JsonSerializerOptions` for `ExpectedJson` / `ActualJson`, using `CultureInfo.InvariantCulture` for any `double.ToString` call.
- Skipped / invalid runs persist `SkipOrInvalidReason` exactly equal to `VerificationResult.Context.Reason` and leave `Diagnostic` `null`.
- Property runs persist every `PropertyPredicateResult` in original order.

### Task C0-3: Add Persistence Round-Trip Tests

**Files:**
- Create: `MetBench_SystemMT.Tests/SystemMT/Persistence/TypedVerificationEvidenceRoundtripTests.cs`

- [ ] **Step 1: Write the three tests from §8.1 of the design**

- `TypedVerification_round_trips_for_MrSpec_with_diagnostic`
- `TypedVerification_round_trips_for_PropertySpec_with_predicate_results`
- `Legacy_row_without_typed_verification_loads_with_null_typed_verification`

Each test constructs an `ExecutionEvidence`, calls `SaveAsync`, calls `GetByExecutionAsync`, and asserts field equality. The disk LiteDB is a temp file per test, following the pattern in `MetBench_SystemMT.Tests/SystemMT/Persistence/ExecutionEvidenceRoundtripTests.cs:11-25`.

### Task C0-4: Add Mapper Tests

**Files:**
- Create: `MetBench_SystemMT.Tests/SystemMT/Persistence/TypedVerificationEvidenceMapperTests.cs`

- [ ] **Step 1: Write the four tests from §8.2 of the design**

- `Record_projects_VerificationResult_into_TypedVerification`
- `Record_projects_skipped_VerificationResult_with_reason`
- `Record_projects_PropertyResult_into_TypedVerification`
- `Record_without_typed_runtime_writes_evidence_without_TypedVerification`

Each test exercises the mapper directly (no LiteDB) and asserts the projected `TypedVerificationEvidence` matches the expected mapping table in §4 of the design.

### Task C0-5: Wire Recorder

**Files:**
- Modify: `MetBench_BLL.Core/SystemMT/Pipeline/SystemMtExecutionRecorder.cs`
- Modify: `MetBench_SystemMT.Tests/SystemMT/Pipeline/ExecutionEvidenceWriteThroughTests.cs` (add tests only; do not change existing tests)

- [ ] **Step 1: Allow the recorder to accept a typed `VerificationResult` or `PropertyResult` via an optional `Record(...)` overload**

The new overload signature is:

```csharp
public RecordedExecution Record(
    PipelineContext context,
    PipelineOutcome outcome,
    int mrInstanceId,
    Guid? batchId = null,
    MetBench_BLL.SystemMT.Catalog.Typed.Runtime.VerificationResult? typedVerification = null,
    MetBench_BLL.SystemMT.Catalog.Typed.Property.PropertyResult? typedProperty = null,
    MetBench_BLL.SystemMT.Catalog.Typed.Specs.MrSpec? typedSpec = null,
    MetBench_BLL.SystemMT.Catalog.Typed.Specs.PredicateSpec? typedPredicate = null,
    MetBench_BLL.SystemMT.Catalog.Typed.Property.PropertySpec? typedPropertySpec = null);
```

All typed parameters default to `null` so the existing nine call sites (asserted by `Record_without_evidence_repo_preserves_pre_Task6_behavior` in `MetBench_SystemMT.Tests/SystemMT/Pipeline/ExecutionEvidenceWriteThroughTests.cs:128-143`) continue compiling unchanged.

- [ ] **Step 2: Project typed verification into `ExecutionEvidence.TypedVerification` only when an evidence repo is wired AND a typed input is provided**

Pseudocode:

```
if (_evidence is not null) {
    var evidence = BuildV1Evidence(executionId, context, outcome);
    if (typedVerification is not null && typedSpec is not null && typedPredicate is not null) {
        evidence.TypedVerification = TypedVerificationEvidenceMapper.FromVerificationResult(typedSpec, typedPredicate, typedVerification);
    } else if (typedProperty is not null && typedPropertySpec is not null) {
        evidence.TypedVerification = TypedVerificationEvidenceMapper.FromPropertyResult(typedPropertySpec, typedProperty);
    }
    _evidence.SaveAsync(evidence).GetAwaiter().GetResult();
}
```

The recorder does not invent typed inputs from the legacy `SystemMtAssertionResultV2`. If the typed path is not wired (PR-C runtime convergence has not landed yet), the recorder writes evidence with `TypedVerification == null`. This is identical to today's behavior.

- [ ] **Step 3: Add the two new behavior tests**

Append to `ExecutionEvidenceWriteThroughTests`:

- `Record_writes_TypedVerification_when_typed_inputs_are_provided`
- `Record_writes_evidence_with_null_TypedVerification_when_typed_inputs_are_absent`

The existing `Record_writes_evidence_when_evidence_and_V3_repos_are_injected`, `Record_writes_evidence_with_empty_V3_ref_when_V3_lookup_misses`, `Record_without_evidence_repo_preserves_pre_Task6_behavior`, `Record_does_not_write_evidence_when_outcome_has_no_AssertionResult`, `Record_evidence_ExecutionId_matches_Execution_row`, and `Record_evidence_writes_sample_trace_for_target_field` must keep passing without source edits.

### Task C0-6: Lock MrRunResult Facade Shape

**Files:**
- Add: `MetBench_SystemMT.Tests/SystemMT/Launcher/MrRunResultShapeLockTests.cs`

- [ ] **Step 1: Write `MrRunResult_shape_is_unchanged_after_typed_runtime_convergence`**

Use reflection to enumerate `MrRunResult` public properties (and the public properties of every record listed in §4 of `docs/PROJECT-STRUCTURE.md` under the launcher facade) and compare to a golden literal string list inside the test.

This test is the structural guard required by §6 of `CLAUDE.md` and §9.1 of the design. If a future change is intentional, the golden list must be updated in the same PR that changes the shape.

### Task C0-7: Validate PR-C0

- [ ] **Step 1: Confirm scope gates**

```bash
git diff --name-only origin/main...HEAD | rg -v '^(MetBench_BLL\.Core/SystemMT/Persistence/|MetBench_BLL\.Core/SystemMT/Pipeline/SystemMtExecutionRecorder\.cs|MetBench_SystemMT\.Tests/SystemMT/Persistence/|MetBench_SystemMT\.Tests/SystemMT/Pipeline/ExecutionEvidenceWriteThroughTests\.cs|MetBench_SystemMT\.Tests/SystemMT/Launcher/MrRunResultShapeLockTests\.cs|docs/)'
```

Expected: empty output.

- [ ] **Step 2: Confirm Method MT and WPF are untouched**

```bash
git diff --name-only origin/main...HEAD | rg 'MetBench_BLL/MethodMT|MetBench_SystemMT.Tests/MethodMT|MetBench_Client'
```

Expected: no matches.

- [ ] **Step 3: Confirm `SystemMtResultRecord` and renderer are unchanged**

```bash
git diff origin/main...HEAD -- MetBench_BLL.Core/SystemMT/Persistence/SystemMtResultRecord.cs MetBench_BLL.Core/SystemMT/Reporting/HtmlSystemMtResultReportRenderer.cs MetBench_BLL.Core/SystemMT/Reporting/ISystemMtResultReportRenderer.cs MetBench_BLL.Core/SystemMT/Launcher/ISystemMtLauncher.cs
```

Expected: no diff.

- [ ] **Step 4: Run focused tests**

```bash
dotnet test MetBench_SystemMT.Tests --no-restore --filter "FullyQualifiedName~TypedVerificationEvidenceRoundtripTests|FullyQualifiedName~TypedVerificationEvidenceMapperTests|FullyQualifiedName~ExecutionEvidenceWriteThroughTests|FullyQualifiedName~ExecutionEvidenceRoundtripTests|FullyQualifiedName~HtmlSystemMtResultReportRendererTests|FullyQualifiedName~MrRunResultShapeLockTests"
```

Expected: all green.

- [ ] **Step 5: Run full System MT test suite**

```bash
dotnet test MetBench_SystemMT.Tests --no-restore
```

Expected: all green.

- [ ] **Step 6: Run whitespace and placeholder scans**

```bash
git diff --check origin/main...HEAD
rg -n "TB[D]|TO[D]O|implement[ ]later|fill[ ]in|待[定]|稍[后]|占[位]" MetBench_BLL.Core/SystemMT/Persistence MetBench_SystemMT.Tests/SystemMT/Persistence
```

Expected: clean output.

- [ ] **Step 7: Commit and push PR-C0**

```bash
git add MetBench_BLL.Core/SystemMT/Persistence MetBench_BLL.Core/SystemMT/Pipeline/SystemMtExecutionRecorder.cs MetBench_SystemMT.Tests/SystemMT/Persistence MetBench_SystemMT.Tests/SystemMT/Pipeline/ExecutionEvidenceWriteThroughTests.cs MetBench_SystemMT.Tests/SystemMT/Launcher/MrRunResultShapeLockTests.cs
git commit -m "feat(systemmt): add executionevidence v2 typed verification block"
git push -u origin <feature-branch>
```

---

## Review Checklist For Every PR

- Scope stays inside the listed file structure for that PR.
- Method MT files are unchanged.
- WPF / `MetBench_Client/**` files are unchanged.
- `SystemMtResultRecord`, `HtmlSystemMtResultReportRenderer`, and `ISystemMtLauncher` are unchanged.
- `LiteDbExecutionEvidenceRepository` collection name and `ExecutionId` unique index are unchanged.
- Existing evidence and renderer tests keep passing without source edits.
- New typed projection uses `CultureInfo.InvariantCulture` for numeric serialization.
- Skipped / invalid runs persist `SkipOrInvalidReason` from `DiagnosticContext.Reason`.

## Final Verification Gate

Run before each push:

```bash
git diff --check
dotnet test MetBench_SystemMT.Tests --no-restore
rg -n "TB[D]|TO[D]O|implement[ ]later|fill[ ]in|待[定]|稍[后]|占[位]" docs/superpowers/specs/2026-05-25-executionevidence-v2-design.md docs/superpowers/plans/2026-05-25-executionevidence-v2-implementation-plan.md
```

Expected:

- `git diff --check` reports no whitespace errors.
- `dotnet test` passes (PR-C0 only; PR-A0 is docs-only and does not run dotnet tests as part of its validation).
- placeholder scan returns no matches.

## Execution Handoff

After PR-A0 merges, the status ledger marks ExecutionEvidence v2 as "Design locked". PR-C of verification semantics convergence (`docs/superpowers/plans/2026-05-25-verification-semantics-convergence-implementation-plan.md`) is no longer blocked by an open evidence design state, but PR-C is allowed to run only after PR-C0 of this plan has merged or after a Window in which PR-C explicitly proves in its checklist that no evidence-schema change is required. PR-C0 must not be merged before PR-A0.
