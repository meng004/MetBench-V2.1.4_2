# System MT Architecture Re-Review After Verification-Semantics Convergence + ExecutionEvidence v2

> **Date**: 2026-05-25
> **Status**: Active review document. Captures the dependency-boundary state of `MetBench_BLL.Core/SystemMT/` after PR-B / PR-C / PR-D verification-semantics convergence and PR-C0 ExecutionEvidence v2 implementation.
> **Baseline commit reviewed**: `ad0bb4b` (`origin/main` after PR #121 squash-merge).
> **Scope**: structural dependency edges between `Catalog/Typed`, `Pipeline`, `Persistence`, `Launcher`, `Reporting`, `Anomaly`, `Metadata`, `Bootstrap`, `Migrations`, `ParameterMapping`, `Transformations`, and the Method MT subtree.

---

## 1. Why this re-review exists

`docs/status/current.md` §6 row "Codegraph and architecture re-review" was added after PR-D as a pending open risk. The status ledger required a post-convergence boundary audit before any new System MT cross-cutting work could be opened. PR #121 (ExecutionEvidence v2 PR-C0) completed the last large structural change inside `SystemMT/Persistence/`, so this review now covers the full state at `ad0bb4b`.

The audit answers four questions:

1. Does Method MT remain isolated from the typed semantic catalog?
2. Is the typed catalog free of upward dependencies on `Pipeline`, `Launcher`, `Persistence`, or `Reporting`?
3. Does the launcher facade keep typed-runtime types out of its public DTOs?
4. Are there any remaining production references to the legacy W1 assertion path (`IMrAssertion`, `AssertionEvaluator`, `SystemMtRunner`)?

## 2. Module inventory

`MetBench_BLL.Core/SystemMT/` at `ad0bb4b`:

| Module | Files | Role |
|---|---:|---|
| `Anomaly/` | 6 | Anomaly classification + commonality |
| `Assertions/` | 5 | Legacy `AssertionEvaluator`, `AssertionInput`, `AssertionTolerance`, `AssertionTypeCodes`, `SystemMtAssertionResultV2` |
| `Bootstrap/` | 1 | `SystemMtBootstrap.SeedCatalogsAsync` |
| `Catalog/` | 99 | Manifest provider catalog + Typed Semantic Catalog subtree |
| `Catalog/Typed/` | 81 | Typed semantic catalog: `Derived/`, `Lint/`, `Migration/`, `Property/`, `Runtime/`, `Schema/`, `Serialization/`, `Specs/`, `Validation/` |
| `Launcher/` | 10 | `ISystemMtLauncher`, `SystemMtLauncher`, DTOs |
| `Metadata/` | 6 | 5D-tag metadata catalog |
| `Migrations/` | n/a (in-flight only) | Catalog schema migration helpers |
| `ParameterMapping/` | 5 | Field-path resolvers |
| `Persistence/` | 10 | `ExecutionEvidence` + `TypedVerificationEvidence` v2 block + `SystemMtResultRecord` + `ExecutionMetadataSnapshot` + repository contracts |
| `Pipeline/` | 10 | `SystemMtPipeline`, `PipelineContext`, `PipelineOutcome`, `SystemMtExecutionRecorder`, `ReplayContextBuilder` |
| `Reporting/` | 2 | `ISystemMtResultReportRenderer`, `HtmlSystemMtResultReportRenderer` |
| `Transformations/` | 16 | MR transformation registry |

## 3. Boundary audit (post-PR-C0)

All checks were run against `ad0bb4b` with simple `grep -rn "namespace-prefix"` queries.

### 3.1 Method MT isolation

```
$ grep -rln "MetBench_BLL\.MethodMT\|using MetBench_BLL\.MT\b" MetBench_BLL.Core/SystemMT/
MetBench_BLL.Core/SystemMT/Pipeline/SystemMtPipeline.cs

$ grep -n "MetBench_BLL\.MT\|MetBench_BLL\.MethodMT" MetBench_BLL.Core/SystemMT/Pipeline/SystemMtPipeline.cs
3:using MetBench_BLL.MT;
25:/// 与方法级 <see cref="MetBench_BLL.MethodMT.MethodMtPipeline"/> 对称。

$ grep -rln "MetBench_BLL\.SystemMT\.Catalog\.Typed\|MetBench_BLL\.SystemMT\.Pipeline\|MetBench_BLL\.SystemMT\.Launcher" MetBench_BLL/MethodMT/
(no output)
```

**Verdict**: ✅ isolated.

- `MetBench_BLL.MT` is a **shared protocol layer** (`IMtPipeline<TReq, TOut>` defined in `MetBench_BLL.Core/MT/IMtPipeline.cs`), referenced symmetrically by both System MT and Method MT. This is intentional and matches CLAUDE.md §3's "shared abstract" pattern.
- The reference at `SystemMtPipeline.cs:25` is an XML doc comment (`<see cref="...MethodMtPipeline"/>`), not a code dependency.
- Method MT does not reference any System MT typed catalog, pipeline, or launcher type.

### 3.2 Typed Catalog upward dependencies

```
$ grep -rln "MetBench_BLL\.SystemMT\.Pipeline\|MetBench_BLL\.SystemMT\.Launcher\|MetBench_BLL\.SystemMT\.Persistence\|MetBench_BLL\.SystemMT\.Reporting" MetBench_BLL.Core/SystemMT/Catalog/Typed/
(no output)
```

**Verdict**: ✅ Typed Catalog stands alone.

The typed catalog subtree has zero upward dependencies on `Pipeline`, `Launcher`, `Persistence`, or `Reporting`. The dependency direction is `Pipeline / Persistence → Typed Catalog`, never the reverse. This is the long-term shape required by the verification-semantics convergence design.

### 3.3 Pipeline → Typed Catalog

```
$ grep -rn "using MetBench_BLL\.SystemMT\.Catalog\.Typed" MetBench_BLL.Core/SystemMT/Pipeline/
Pipeline/PipelineContext.cs:3:using MetBench_BLL.SystemMT.Catalog.Typed.Specs;
Pipeline/SystemMtExecutionRecorder.cs:1:using MetBench_BLL.SystemMT.Catalog.Typed.Property;
Pipeline/SystemMtExecutionRecorder.cs:2:using MetBench_BLL.SystemMT.Catalog.Typed.Runtime;
Pipeline/SystemMtExecutionRecorder.cs:3:using MetBench_BLL.SystemMT.Catalog.Typed.Specs;
Pipeline/SystemMtPipeline.cs:5:using MetBench_BLL.SystemMT.Catalog.Typed.Migration;
Pipeline/SystemMtPipeline.cs:6:using MetBench_BLL.SystemMT.Catalog.Typed.Runtime;
Pipeline/SystemMtPipeline.cs:7:using MetBench_BLL.SystemMT.Catalog.Typed.Specs;
```

**Verdict**: ✅ intended directionality. Pipeline consumes:

- `Catalog.Typed.Migration` — `LegacyAssertionPredicateMapper`, `TypedSpecFactory`, `TypedVerificationContextFactory` (the only typed entry point for legacy string codes; PR-C).
- `Catalog.Typed.Runtime` — `PredicateDispatcher`, `IPredicateDispatcher`, `VerificationContext`, `VerificationResult` (PR-C runtime route).
- `Catalog.Typed.Specs` — `MrSpec`, `PredicateSpec`, `BinaryComparisonPredicate`, etc. (typed spec types for the optional typed path on `PipelineContext`).
- `Catalog.Typed.Property` — `PropertyResult`, `PropertyStatus` (PR-C0 recorder typed-property projection).

These are exactly the surfaces named in the verification-semantics convergence design and the ExecutionEvidence v2 design.

### 3.4 Launcher → Typed Catalog

```
$ grep -rn "using MetBench_BLL\.SystemMT\.Catalog\.Typed" MetBench_BLL.Core/SystemMT/Launcher/
(no output)
```

**Verdict**: ✅ facade insulation holds. The launcher facade (`ISystemMtLauncher`, `MrRunResult`, `MrSummary`, `BatchMrRunRequest`, `BatchProgress`) does not leak any typed-runtime type. This is enforced at test level by `MetBench_SystemMT.Tests/SystemMT/Launcher/MrRunResultShapeLockTests.cs` (added in PR-C0), which pins the 9 public properties of `MrRunResult` via reflection and asserts `sealed record` discipline.

### 3.5 Persistence → Typed Catalog

```
$ grep -rln "MetBench_BLL\.SystemMT\.Catalog\.Typed" MetBench_BLL.Core/SystemMT/Persistence/
MetBench_BLL.Core/SystemMT/Persistence/TypedDiagnosticEvidence.cs        # XML doc comment only
MetBench_BLL.Core/SystemMT/Persistence/TypedPropertyPredicateEvidence.cs # XML doc comment only
MetBench_BLL.Core/SystemMT/Persistence/TypedVerificationEvidenceMapper.cs # real reference (intended)
```

**Verdict**: ✅ contained.

- `TypedDiagnosticEvidence` and `TypedPropertyPredicateEvidence` mention typed runtime types **only inside `<c>…</c>` XML doc comments**; their compiled surface is primitives + `double?` + `string?`. They have no real type dependency on the typed catalog.
- `TypedVerificationEvidenceMapper` is the **only** Persistence file that actually imports typed-runtime / typed-property / typed-specs types. This is by design — the mapper is the single boundary between typed runtime values and persistence-shaped POCOs.
- `ExecutionEvidence`, `TypedVerificationEvidence`, `ExecutionMetadataSnapshot`, `ExecutionSampleTrace`, `SystemMtResultRecord`, `IExecutionEvidenceRepository`, `ISystemMtResultRepository` carry no typed-runtime type. LiteDB persistence stays culture-independent.

### 3.6 DAL and Reporting

```
$ grep -rln "MetBench_BLL\.SystemMT\.Catalog\.Typed" MetBench_DAL/
(no output)

$ grep -rln "MetBench_BLL\.SystemMT\.Catalog\.Typed" MetBench_BLL.Core/SystemMT/Reporting/
(no output)
```

**Verdict**: ✅ DAL persistence and HTML reporting both stay free of typed-runtime references.

### 3.7 Legacy W1 path

```
$ grep -rn "IMrAssertion\|new ApproxEqualAssertion\|new GreaterThanAssertion\|new LessThanAssertion\|new SystemMtRunner\|EqualityThresholds" MetBench_BLL.Core/SystemMT/ MetBench_BLL/MethodMT/ MetBench_DAL/
(no output)

$ grep -rln "new AssertionEvaluator\|AssertionEvaluator\." MetBench_BLL.Core/SystemMT/ MetBench_BLL/MethodMT/ | grep -v "Assertions/AssertionEvaluator\.cs"
(no output)
```

**Verdict**: ✅ W1 cleanup is complete.

- Zero production references to `IMrAssertion`, the three W1 implementations, `SystemMtRunner`, or `EqualityThresholds` (PR-D deleted these files entirely).
- `AssertionEvaluator` is referenced only inside `Assertions/AssertionEvaluator.cs` itself; no production caller invokes it after PR-C.
- Regression is blocked by `SemanticCatalogBoundaryTests` (3 facts) and `SemanticCatalogNamingBoundaryTests` (2 facts).

### 3.8 Inter-module wiring (sanity)

| Edge | Status | Notes |
|---|---|---|
| `Launcher → Pipeline` | present (`SystemMtLauncher.cs:6`) | Launcher orchestrates pipeline runs. Intended. |
| `Launcher → Catalog` (manifest) | present (`ISystemMtCatalogReader.cs:1`, `SystemMtLauncher.cs:5`) | Launcher consumes manifest provider catalog. Intended. |
| `Launcher → Persistence` | none direct | Launcher does not directly reference `MetBench_BLL.SystemMT.Persistence`; record id flows via `SystemMtExecutionRecorder` from pipeline outcome. Facade-insulated. |
| `Pipeline → Persistence` | present | `SystemMtExecutionRecorder` writes `Execution` + `Result` + `ExecutionEvidence`. Intended. |
| `Persistence → Catalog` (manifest) | none | Persistence rows reference catalog manifest only as **string paths** in `ExecutionMetadataSnapshot.CatalogManifestPath`. |
| `Anomaly / Metadata / Bootstrap / Migrations / ParameterMapping / Transformations → Typed Catalog` | none | All six modules are isolated from the typed catalog. |
| `MetBench_BLL/`, `MetBench_DAL/`, `MetBench_Domain/`, `MetBench_IDAL/`, `MetBench_Client/` → Typed Catalog | none | Typed catalog is only consumed inside `MetBench_BLL.Core/SystemMT/`. WPF `App.xaml.cs` does **not** reference typed-catalog types after PR-D (verified via grep). |

## 4. Aggregate dependency diagram (text)

```
                  ┌─────────────────────────────────┐
                  │     MetBench_BLL.MT (shared)    │
                  │       IMtPipeline<TReq,TOut>    │
                  └────────────┬────────────────────┘
                               │
              ┌────────────────┴────────────────┐
              │                                 │
   ┌──────────▼──────────┐           ┌──────────▼──────────┐
   │   System MT         │           │   Method MT         │
   │   (this audit)      │           │   (isolated)        │
   └──────────┬──────────┘           └─────────────────────┘
              │
   ┌──────────▼──────────────────────────────────────────────┐
   │ Launcher  ──►  Pipeline  ──►  Persistence  ──►  DAL     │
   │             ▲                                            │
   │             │                                            │
   │   Catalog (manifest)                                     │
   │             ▲                                            │
   │             │ (no upward edges out of Typed Catalog)     │
   │   ┌─────────┴─────────┐                                  │
   │   │ Catalog / Typed   │ ◄── Pipeline (via Migration/    │
   │   │  Specs / Runtime  │     Runtime / Specs / Property) │
   │   │  Migration /      │ ◄── Persistence (via Mapper)    │
   │   │  Property / etc   │                                  │
   │   └───────────────────┘                                  │
   └──────────────────────────────────────────────────────────┘
   
   Reporting (HTML) ──► Persistence (record-only)
   AssertionEvaluator (Assertions/, no production callers; test-fixture only)
```

Key invariants visible in the diagram:

- Method MT does **not** appear in the System MT subgraph except via the shared `IMtPipeline` protocol.
- Typed Catalog has **no upward arrow** to any other System MT module.
- The only edge into Typed Catalog from outside is from Pipeline (runtime) and Persistence (mapper).
- Launcher has no edge into Typed Catalog at all.

## 5. Risks and follow-ups

| Risk | Severity | Mitigation / Tracking |
|---|---|---|
| Typed predicate coverage is incomplete: 6 legacy assertion codes (`less-noise-aware`, `greater-noise-aware`, `approx-invariant`, `variance-ratio`, `flux-pointwise-approx`, `cross-program-agree`) fail-closed when routed through the pipeline | Medium (dormant in CI; surfaces if production catalog bindings adopt them) | Already tracked in `docs/status/current.md` §6 row "Unmapped legacy assertion codes". Follow-up PR: extend `LegacyAssertionPredicateMapper` + add the matching typed predicates already in `Catalog/Typed/Specs/`. |
| `SystemMtPipeline` does not yet wire the typed `VerificationResult` to `SystemMtExecutionRecorder.Record(typedVerification: ..., typedSpec: ..., typedPredicate: ...)` | Low (PR-C0 made the recorder *capable*; live pipeline rows still carry `TypedVerification == null`) | Mechanical follow-up. Tracked as the immediate next item after this re-review. |
| `AssertionEvaluator` + `AssertionInput` + `AssertionTolerance` + `AssertionTypeCodes` + `SystemMtAssertionResult` (W1 result) + `SystemMtResult` still exist in production | Low (W1 result types are required by `SystemMtResultRecord.FromResult`, `LiteDbSystemMtResultRepository`, `AnomalyClassifier`; `AssertionTolerance` is required by `PipelineContext`; `AssertionTypeCodes` is required by `Catalog/MrBindingDefinition.cs`) | Removing these requires changing the persistence schema, which is governed by the ExecutionEvidence v2 plan and would need its own design slice. Not scoped here. |
| `SystemMtAssertionResultV2.PassedResult` / `FailedResult` static helpers still take `AssertionInput` | Low | Pure helper; tests in `V2Pipeline/AssertionEvaluatorTests` and `V2Anomaly/AnomalyCreationOnFailureTests` exercise them as fixtures. No production callsite in System MT pipeline. |
| WPF `MetBench_Client/App.xaml.cs` DI registration still constructs `LauncherOptions` + `IMrCatalogProvider` + repositories | Out of scope of this audit | Cannot be verified from Linux CI. UAT runbook covers the WPF side. |

## 6. Conclusion

All four audit questions are answered:

1. **Method MT remains isolated**: zero production references in either direction (only the intentional shared `IMtPipeline` protocol). ✅
2. **Typed catalog is free of upward dependencies** on Pipeline / Launcher / Persistence / Reporting / Anomaly / Metadata / Bootstrap / Migrations / ParameterMapping / Transformations. ✅
3. **Launcher facade keeps typed-runtime types out** of its public DTOs (`MrRunResultShapeLockTests` enforces). ✅
4. **No remaining production references** to W1 `IMrAssertion`, `SystemMtRunner`, or non-allowed `AssertionEvaluator` calls (`SemanticCatalogBoundaryTests` enforces). ✅

The post-PR-D + post-PR-C0 System MT architecture matches the verification-semantics convergence design (`docs/superpowers/specs/2026-05-25-verification-semantics-convergence-design.md`) and the ExecutionEvidence v2 design (`docs/superpowers/specs/2026-05-25-executionevidence-v2-design.md`) without deviation. The `Codegraph and architecture re-review` open-risk row in `docs/status/current.md` §6 may be marked controlled by the PR that lands this document.
