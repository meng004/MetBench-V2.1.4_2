# ExecutionEvidence v2 Design

> **Date**: 2026-05-25
> **Status**: Locked design direction. Must be merged before verification semantics convergence PR-C starts runtime work.
> **Scope**: Persistence shape, recorder lifecycle, and reporting projection for System MT typed verification evidence. Method MT is out of scope.

---

## 1. Decision Summary

ExecutionEvidence v2 extends the existing v1 evidence row with **typed verification fields** that come from the System MT Typed Semantic Catalog runtime. It does **not** replace v1.

The accepted direction is:

```text
ExecutionEvidence keeps its v1 identity, FK, metadata snapshot, sample traces, transformation parameters, and recorded timestamp.
v2 adds an optional TypedVerification block that mirrors the typed VerificationResult / PropertyResult shape.
The LiteDB collection name, IdEvidence schema, and ExecutionId unique index are preserved.
The launcher facade DTO (MrRunResult) does not gain typed engine internals.
HtmlSystemMtResultReportRenderer keeps consuming SystemMtResultRecord and only learns to read the new typed fields when an evidence row carries them.
```

This is the minimum surface that lets PR-C route System MT runtime through `PredicateDispatcher` / `IVerifierKernel<TPredicate>` while keeping the §6 launcher type-leakage rule intact and keeping existing LiteDB rows readable.

## 2. Current Facts (v1)

The current evidence aggregate is `MetBench_BLL.SystemMT.Persistence.ExecutionEvidence`
(`MetBench_BLL.Core/SystemMT/Persistence/ExecutionEvidence.cs:16-31`):

```csharp
public sealed class ExecutionEvidence
{
    public Guid IdEvidence { get; set; }
    public Guid ExecutionId { get; set; }
    public ExecutionMetadataSnapshot Metadata { get; set; } = new();
    public List<ExecutionSampleTrace> SampleTraces { get; set; } = new();
    public Dictionary<string, string> TransformationParameters { get; set; } = new();
    public DateTime RecordedAtUtc { get; set; } = DateTime.UtcNow;
}
```

Surrounding state at `e839214`:

- `ExecutionMetadataSnapshot` carries `MrId`, `V3MrIdRef`, 5D tag strings, `CatalogManifestPath`, `MetbenchVersion`
  (`MetBench_BLL.Core/SystemMT/Persistence/ExecutionMetadataSnapshot.cs:10-32`).
- `ExecutionSampleTrace` carries `VariableName`, `Path`, `SourceValueJson`, `TransformedValueJson`, `OutputValueJson`
  (`MetBench_BLL.Core/SystemMT/Persistence/ExecutionSampleTrace.cs:9-25`).
- `LiteDbExecutionEvidenceRepository` upserts on `IdEvidence` and ensures a `unique` index on `ExecutionId`
  (`MetBench_DAL/LiteDbExecutionEvidenceRepository.cs:42-46`).
- `SystemMtExecutionRecorder.WriteEvidence` projects evidence from `PipelineContext` + `PipelineOutcome`, building at most one sample trace from `TargetFieldPath`, follow-up input path, and follow-up metrics
  (`MetBench_BLL.Core/SystemMT/Pipeline/SystemMtExecutionRecorder.cs:113-175`).
- The summary row `SystemMtResultRecord` is independent of `ExecutionEvidence` and serializes only scalar assertion fields (`SourceValue`, `FollowUpValue`, `Passed`, `FailureReason`, …)
  (`MetBench_BLL.Core/SystemMT/Persistence/SystemMtResultRecord.cs:16-119`).
- `HtmlSystemMtResultReportRenderer` consumes `SystemMtResultRecord` only; it does not currently load evidence rows
  (`MetBench_BLL.Core/SystemMT/Reporting/HtmlSystemMtResultReportRenderer.cs:8-67`).
- Typed verification today exposes `VerificationResult(Status, Assertion, Diagnostic, Context)`
  (`MetBench_BLL.Core/SystemMT/V12Catalog/Runtime/VerificationResult.cs:5-28`),
  with `VerifyStatus { Passed, Failed, SkippedNotApplicable, SkippedMissingObservable, InvalidSpec }`,
  `VerificationDiagnostic(Expected, Actual, Residual, Tolerance)`, and `DiagnosticContext(Reason)`.
- Typed property checking exposes `PropertyResult(PropertyId, Status, PredicateResults, Reason)`
  with `PropertyPredicateResult(PredicateId, PredicateKind, Status, Actual?, Expected?, Residual?, Tolerance?, Reason?)`
  (`MetBench_BLL.Core/SystemMT/V12Catalog/Property/PropertyResult.cs:5-22`).

These names move under `MetBench_BLL.SystemMT.Catalog.Typed.*` in PR-B; this design uses the post-rename names where it refers to long-term targets.

## 3. v2 Field Inventory and Lifecycle

ExecutionEvidence v2 keeps all v1 fields unchanged and adds one optional aggregate property:

```csharp
public sealed class ExecutionEvidence
{
    // v1 (unchanged identity, FK, metadata, traces, parameters, timestamp)
    public Guid IdEvidence { get; set; }
    public Guid ExecutionId { get; set; }
    public ExecutionMetadataSnapshot Metadata { get; set; } = new();
    public List<ExecutionSampleTrace> SampleTraces { get; set; } = new();
    public Dictionary<string, string> TransformationParameters { get; set; } = new();
    public DateTime RecordedAtUtc { get; set; } = DateTime.UtcNow;

    // v2 (additive, nullable; null when row was written by pre-v2 code or by a skipped/invalid path)
    public TypedVerificationEvidence? TypedVerification { get; set; }
}
```

`TypedVerificationEvidence` is the persistence projection of the typed runtime result and must be persistence-shaped (POCO with parameterless ctor, public setters, no engine-internal types):

```csharp
public sealed class TypedVerificationEvidence
{
    public string SpecId { get; set; } = string.Empty;
    public string SpecKind { get; set; } = string.Empty;          // "MrSpec" | "PropertySpec"
    public string PredicateId { get; set; } = string.Empty;
    public string PredicateKind { get; set; } = string.Empty;     // e.g. "BinaryComparison", "ScaledEquality", "FieldEquality"
    public string Status { get; set; } = string.Empty;            // VerifyStatus name; for properties the PropertyStatus name
    public bool? Passed { get; set; }                             // mirrors Status==Passed/Held; null when skipped/invalid

    public TypedDiagnosticEvidence? Diagnostic { get; set; }
    public string? SkipOrInvalidReason { get; set; }              // populated when Status is Skipped*/InvalidSpec

    public List<TypedPropertyPredicateEvidence> PropertyPredicates { get; set; } = new();
}

public sealed class TypedDiagnosticEvidence
{
    public double Expected { get; set; }
    public double Actual { get; set; }
    public double Residual { get; set; }
    public double Tolerance { get; set; }
}

public sealed class TypedPropertyPredicateEvidence
{
    public string PredicateId { get; set; } = string.Empty;
    public string PredicateKind { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public double? Residual { get; set; }
    public double? Tolerance { get; set; }
    public string? Reason { get; set; }
    public string? ExpectedJson { get; set; }                     // typed PropertyPredicateResult.Expected serialized invariant-culture JSON
    public string? ActualJson { get; set; }                       // typed PropertyPredicateResult.Actual serialized invariant-culture JSON
}
```

Lifecycle:

1. **Source**: `SystemMtExecutionRecorder.WriteEvidence` is invoked from the same call site as today
   (`MetBench_BLL.Core/SystemMT/Pipeline/SystemMtExecutionRecorder.cs:105-108`).
   PR-C feeds it the typed `VerificationResult` (or `PropertyResult`) produced by the dispatcher inside
   `SystemMtPipeline.ExecuteAsync` after source/follow-up parsing.
2. **Projection**: the recorder calls a new pure mapper
   `TypedVerificationEvidence.From(VerificationResult, MrSpec)` / `.From(PropertyResult, PropertySpec)` that
   - reads `VerifyStatus`, `VerificationDiagnostic`, `DiagnosticContext.Reason`,
   - reads `PredicateSpec.PredicateId` and the runtime-recorded `PredicateKind`,
   - serializes `PropertyPredicateResult.Expected` / `Actual` objects via `System.Text.Json` with `InvariantCulture` for numeric formatting.
3. **Write**: the same `IExecutionEvidenceRepository.SaveAsync` upserts the row. Unique-on-`ExecutionId` index is unchanged.
4. **Read**: `IExecutionEvidenceRepository.GetByExecutionAsync` returns the same aggregate; `TypedVerification` is `null` for legacy rows.
5. **Reporting**: `HtmlSystemMtResultReportRenderer` does **not** ingest evidence directly. A separate evidence-aware projection helper (added in a later report PR) augments the existing `Details` block when an evidence row exists for the result; this design only locks the data contract, not the new HTML block.

Recorder skip rules (preserve existing semantics):

- When `outcome.AssertionResult is null` (error / timeout / cancelled), evidence is **not** written. Same as today.
- When typed verification ran but returned `SkippedMissingObservable` / `SkippedNotApplicable` / `InvalidSpec`, evidence **is** written; `TypedVerification.Status` carries the skip code and `SkipOrInvalidReason` carries `DiagnosticContext.Reason`. `Passed` is `null`, `Diagnostic` is `null`.

## 4. Mapping from Typed VerificationResult to Persistence and Reporting

| Source (typed runtime) | Persistence (ExecutionEvidence v2) | Renderer projection |
|---|---|---|
| `VerificationResult.Status` | `TypedVerification.Status` (`VerifyStatus` name) | Status badge fallback when `SystemMtResultRecord.Passed` does not distinguish skip vs fail |
| `VerificationResult.Assertion.Passed` | `TypedVerification.Passed` | Mirrors existing pass/fail badge |
| `VerificationResult.Diagnostic.Expected/Actual/Residual/Tolerance` | `TypedVerification.Diagnostic.*` (4 doubles) | New `Details` rows: Expected, Actual, Residual, Tolerance |
| `VerificationResult.Context.Reason` | `TypedVerification.SkipOrInvalidReason` | Shown only when Status is Skipped*/InvalidSpec |
| `MrSpec.MrId` (post-PR-B `MetBench_BLL.SystemMT.Catalog.Typed.Specs.MrSpec`) | `TypedVerification.SpecId`, `SpecKind="MrSpec"` | New `Details` row: SpecId |
| `PredicateSpec.PredicateId` and concrete record type name | `TypedVerification.PredicateId`, `TypedVerification.PredicateKind` | New `Details` row: Predicate |
| `PropertyResult.PropertyId` | `TypedVerification.SpecId`, `SpecKind="PropertySpec"` | New `Details` row: PropertyId |
| `PropertyResult.Status` | `TypedVerification.Status` (`PropertyStatus` name) | Status badge |
| `PropertyResult.PredicateResults[i]` | `TypedVerification.PropertyPredicates[i]` | Optional table inside `Details` when present |

Numeric formatting:

- Persistence stores raw `double` values; serialization follows LiteDB defaults already in use for `SystemMtResultRecord.SourceMetrics`.
- JSON serialization for `ExpectedJson` / `ActualJson` uses `System.Text.Json` with `JsonSerializerOptions` set to invariant-culture handling (matches existing recorder code at `SystemMtExecutionRecorder.cs:159-162`).

The launcher facade (`ISystemMtLauncher`, `MrRunResult`) is **not** widened. Typed evidence lives in `ExecutionEvidence` only, consistent with the §6 type-leakage rule in `CLAUDE.md`.

## 5. Compatibility Contract

- **LiteDB collection**: name `ExecutionEvidence` is preserved
  (`MetBench_DAL/LiteDbExecutionEvidenceRepository.cs:19`).
- **Identity and FK**: `IdEvidence` (BSON `_id`, autoId), `ExecutionId` unique index, upsert-on-`IdEvidence`, all unchanged
  (`MetBench_DAL/LiteDbExecutionEvidenceRepository.cs:42-53`).
- **Existing rows**: rows written before PR-C deserialize with `TypedVerification == null`. No migration script runs. No defaults are filled retroactively.
- **`SystemMtResultRecord`**: shape unchanged. Public properties stay as on `main` so existing tests including `ExecutionEvidenceRoundtripTests` (`MetBench_SystemMT.Tests/SystemMT/Persistence/ExecutionEvidenceRoundtripTests.cs:59-162`), `ExecutionEvidenceWriteThroughTests` (`MetBench_SystemMT.Tests/SystemMT/Pipeline/ExecutionEvidenceWriteThroughTests.cs:74-220`), and `HtmlSystemMtResultReportRendererTests` (`MetBench_SystemMT.Tests/Reporting/HtmlSystemMtResultReportRendererTests.cs:50-80`) keep passing without source edits.
- **Launcher facade**: `ISystemMtLauncher`, `MrSummary`, `MrRunResult`, `BatchMrRunRequest`, `BatchProgress` signatures unchanged. Typed evidence is not leaked through the facade.
- **Report renderer signature**: `HtmlSystemMtResultReportRenderer.Render(IEnumerable<SystemMtResultRecord>, ReportContext?)` is unchanged. Adding evidence-aware detail rendering is a separate later PR that takes an additional optional parameter; PR-C must not touch the renderer.

## 6. What PR-C May Change

PR-C is allowed to:

- Add the new `TypedVerificationEvidence`, `TypedDiagnosticEvidence`, and `TypedPropertyPredicateEvidence` POCOs under `MetBench_BLL.Core/SystemMT/Persistence/`.
- Add the nullable `TypedVerification` property to `ExecutionEvidence`.
- Add the mapping helpers (`TypedVerificationEvidence.From(...)`) under `MetBench_BLL.Core/SystemMT/Persistence/` (mapper is a stateless static; no engine-internal types are exposed outside the assembly boundary that already references the typed catalog).
- Update `SystemMtExecutionRecorder` to accept and project the typed `VerificationResult` / `PropertyResult` produced by `PredicateDispatcher` / `PropertyChecker`. The recorder's constructor surface may gain optional dependencies provided they default to behavior-preserving values for the nine existing test call sites called out in `ExecutionEvidenceWriteThroughTests` (`MetBench_SystemMT.Tests/SystemMT/Pipeline/ExecutionEvidenceWriteThroughTests.cs:128-143`).
- Update `SystemMtPipeline.ExecuteAsync` to pass typed results to the recorder.
- Add focused tests under `MetBench_SystemMT.Tests/SystemMT/Persistence/` and `MetBench_SystemMT.Tests/SystemMT/Pipeline/` that prove the mapping for MR and Property paths and that legacy rows still round-trip with `TypedVerification == null`.

## 7. What PR-C Must Not Change

PR-C is **not** allowed to:

- Change or remove any existing v1 field on `ExecutionEvidence`, `ExecutionMetadataSnapshot`, or `ExecutionSampleTrace`.
- Change `LiteDbExecutionEvidenceRepository` collection name, BSON id mapping, or `ExecutionId` unique index.
- Change `SystemMtResultRecord` public properties or its `FromResult` projection signature.
- Change `ISystemMtLauncher`, `MrSummary`, `MrRunResult`, `BatchMrRunRequest`, or `BatchProgress`.
- Change `HtmlSystemMtResultReportRenderer.Render(...)` signature, generated HTML for legacy rows, or `ReportContext`.
- Change Method MT files (`MetBench_BLL/MethodMT/**`) or Method MT test files (`MetBench_SystemMT.Tests/MethodMT/**`).
- Touch `MetBench_Client/**` XAML or code-behind. PR-C is cross-platform `net8.0` only.
- Introduce non-deterministic clock or culture behavior in the mapper (timestamps remain `outcome.FinishedAt.ToUniversalTime()`; JSON serialization is invariant-culture).
- Retroactively fill `TypedVerification` for pre-existing rows; this is an additive forward-only schema change.

## 8. Test Matrix

PR-C must add the following tests. Each test maps to a concrete observable in this design.

### 8.1 Persistence round-trip

- **`TypedVerification_round_trips_for_MrSpec_with_diagnostic`**
  - Save evidence with `TypedVerification.SpecKind = "MrSpec"`, `Status = "Passed"`, `Passed = true`, populated `Diagnostic`, empty `PropertyPredicates`.
  - Reload and assert each field equals the input value (invariant-culture comparison for doubles).
- **`TypedVerification_round_trips_for_PropertySpec_with_predicate_results`**
  - Save evidence with `SpecKind = "PropertySpec"`, `Status = "Held"`, `PropertyPredicates` containing two entries with mixed `Status` values and JSON-serialized `Expected/Actual`.
  - Reload and assert ordering and field equality.
- **`Legacy_row_without_typed_verification_loads_with_null_typed_verification`**
  - Save evidence with `TypedVerification = null` (current v1 shape).
  - Reload and assert `TypedVerification` is `null`. Asserts forward compatibility for rows persisted by pre-v2 builds.

### 8.2 Recorder projection

- **`Record_projects_VerificationResult_into_TypedVerification`**
  - Feed recorder a passing `VerificationResult` with full diagnostic.
  - Assert evidence written by the in-memory repo has `TypedVerification.SpecKind = "MrSpec"`, `Status = "Passed"`, matching `Diagnostic` values, `SkipOrInvalidReason == null`.
- **`Record_projects_skipped_VerificationResult_with_reason`**
  - Feed recorder `VerificationResult.SkippedMissingObservable("missing source field")`.
  - Assert `TypedVerification.Status = "SkippedMissingObservable"`, `Passed = null`, `Diagnostic = null`, `SkipOrInvalidReason == "missing source field"`.
- **`Record_projects_PropertyResult_into_TypedVerification`**
  - Feed recorder a `PropertyResult.Held` with two `PropertyPredicateResult` entries.
  - Assert `TypedVerification.PropertyPredicates` has both entries with their `PredicateKind`, `Residual`, `Tolerance`, and serialized `ExpectedJson` / `ActualJson` set to invariant-culture strings.
- **`Record_without_typed_runtime_writes_evidence_without_TypedVerification`**
  - Use the existing nine-ctor backwards-compat path (`ExecutionEvidenceWriteThroughTests.Record_without_evidence_repo_preserves_pre_Task6_behavior`-style construction).
  - Assert evidence either is not written (when no evidence repo) or is written with `TypedVerification = null` when only legacy mapper is wired.

### 8.3 Launcher / facade invariants

- **`MrRunResult_shape_is_unchanged_after_typed_runtime_convergence`**
  - Reflection-based assertion or compile-time test that enumerates the public properties of `MrRunResult` and compares them to a golden list extracted from `main`.
  - Locked under PR-C to enforce §6 of `CLAUDE.md`.

### 8.4 Reporting invariants

- **`HtmlSystemMtResultReportRenderer_output_for_v1_input_is_byte_identical_to_main`**
  - Existing snapshot tests in `MetBench_SystemMT.Tests/Reporting/HtmlSystemMtResultReportRendererTests.cs` must keep passing without source edits.
  - PR-C does not need to add new renderer tests; it only needs to confirm none of the existing renderer tests changed.

## 9. Review Gates

PR-C cannot merge unless all gates below are green. Each gate is a hard requirement enforced by the PR checklist
(`docs/superpowers/templates/pr-gate-checklist.md`) and the implementation plan.

### 9.1 Scope gates

- [ ] No edits under `MetBench_BLL/MethodMT/**` or `MetBench_SystemMT.Tests/MethodMT/**`.
- [ ] No edits under `MetBench_Client/**`.
- [ ] No edits to `MetBench_BLL.Core/SystemMT/Reporting/HtmlSystemMtResultReportRenderer.cs` or to `ISystemMtResultReportRenderer.cs`.
- [ ] No edits to `MetBench_BLL.Core/SystemMT/Launcher/ISystemMtLauncher.cs`, `LauncherCatalog`, or DTO records under `MetBench_BLL.SystemMT.Launcher.*`.
- [ ] No edits to `MetBench_BLL.Core/SystemMT/Persistence/ExecutionMetadataSnapshot.cs` and `ExecutionSampleTrace.cs` other than adding XML doc cross-references if required.
- [ ] No edits to `MetBench_DAL/LiteDbExecutionEvidenceRepository.cs` other than mapper registrations strictly required for new POCOs (e.g. `Mapper.Entity<ExecutionEvidence>().Id(...)` is unchanged; new types only get default mapping).

### 9.2 Schema gates

- [ ] `ExecutionEvidence` keeps every v1 public property with the same name, type, and accessibility.
- [ ] `LiteDbExecutionEvidenceRepository` collection name is `ExecutionEvidence`.
- [ ] `ExecutionId` unique index is still asserted by `ExecutionId_unique_index_rejects_two_records_for_same_execution` in `ExecutionEvidenceRoundtripTests`.
- [ ] `TypedVerification` is nullable; the v1 round-trip tests do not set it and still pass.

### 9.3 Mapping gates

- [ ] Every value in `VerifyStatus` has a deterministic string projection (`enum.ToString()`).
- [ ] Every value in `PropertyStatus` has a deterministic string projection.
- [ ] Skipped / invalid runs persist `SkipOrInvalidReason` exactly equal to `DiagnosticContext.Reason`.
- [ ] Passed and failed runs persist `Diagnostic` with all four doubles round-tripping bit-exact through LiteDB.
- [ ] Property runs persist all `PropertyPredicateResult` entries in their original order.

### 9.4 Test gates

- [ ] Test matrix in §8 is implemented before any production code under `MetBench_BLL.Core/SystemMT/Persistence/` is added.
- [ ] `dotnet test MetBench_SystemMT.Tests --no-restore` is green locally before PR submission.
- [ ] CI green on `ubuntu-24.04` per `.github/workflows/dotnet-test.yml`.

### 9.5 Documentation gates

- [ ] `docs/status/current.md` open-risks row "ExecutionEvidence final shape" is moved to "Design locked" before PR-C opens, and to "Implemented" only after PR-C merges.
- [ ] Active plan index entry for verification semantics convergence is updated to reflect that PR-C is no longer ExecutionEvidence-blocked.
- [ ] PR-C description links this design and the implementation plan.

## 10. Non-Goals

ExecutionEvidence v2 does **not**:

- redesign `SystemMtResultRecord`
- introduce a new evidence-aware report renderer (a separate report PR after PR-C)
- change Method MT
- migrate any LiteDB data already on disk
- add a `TypedVerification` projection to `MrRunResult` or any launcher facade DTO
- introduce a new evidence collection or split `ExecutionEvidence` across multiple LiteDB collections
- add a sample-level capture loop beyond the existing single-target-field trace; broader sample capture remains a follow-up tracked separately

## 11. Risks and Controls

| Risk | Control |
|---|---|
| Adding a nullable property breaks LiteDB BSON mapping for legacy rows | Round-trip tests in §8.1 exercise both v1 and v2 shapes; nullable reference type is mapped as missing BSON field by default |
| Recorder constructor surface grows and breaks the nine existing call sites | `Record_without_evidence_repo_preserves_pre_Task6_behavior` style assertion stays green; new dependencies are optional with default `null` |
| Launcher facade silently widens through `MrRunResult` | `MrRunResult_shape_is_unchanged_after_typed_runtime_convergence` assertion in §8.3 |
| Numeric serialization drifts between cultures | Mapping helper uses `CultureInfo.InvariantCulture` and `System.Text.Json` defaults; tests assert byte-exact strings |
| Skipped / invalid runs lose their reason | `Record_projects_skipped_VerificationResult_with_reason` test |
| PR-C scope creep into renderer | Scope gate forbids edits in `MetBench_BLL.Core/SystemMT/Reporting/**`; renderer-side typed evidence projection is deferred to a later report PR |

## 12. Final Decision

The accepted direction is:

```text
ExecutionEvidence v2 keeps v1 fields, adds a nullable TypedVerification block, and locks the recorder mapping for typed VerificationResult / PropertyResult.
LiteDB collection, IdEvidence schema, and ExecutionId unique index are preserved.
SystemMtResultRecord and ISystemMtLauncher facade are not widened.
HtmlSystemMtResultReportRenderer signature is not changed in PR-C; an evidence-aware report block is deferred.
Method MT and WPF are out of scope.
```
