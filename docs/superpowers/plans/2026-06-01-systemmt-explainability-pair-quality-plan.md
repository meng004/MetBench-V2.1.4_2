# System MT Explainability and Pair Quality Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Upgrade System MT from simple equation/SUT/MR labels and single-run pass/fail reporting to auditable explanation profiles plus pair-level execution quality summaries.

**Architecture:** Add explanation fields beside existing metadata/catalog assets, then extend execution evidence with pair summaries, then project those summaries into reports and WPF. Keep the launcher facade, typed semantic catalog boundary, and existing result shapes backward-compatible.

**Tech Stack:** .NET 8, LiteDB, WPF, System MT catalog manifests, typed semantic catalog, xUnit, Markdown/HTML report renderers.

---

## Scope

This plan is a scoped successor plan for the current Stage 8 controlled baseline. It does not add new SUTs, new MR semantics, or Method MT behavior. It only improves explanation, observability, and reporting for existing System MT assets.

## Impact Range

| Area | Files / modules | Impact |
|---|---|---|
| Metadata explanation | `MetBench_BLL.Core/SystemMT/Metadata/`, `MetBench_SystemMT.Tests/SystemMT/Metadata/` | Add structured explanation fields for equations and MRs; seed current built-ins. |
| Catalog / SUT explanation | `SUT/*/catalog.json`, `MetBench_BLL.Core/SystemMT/Catalog/`, catalog editor tests | Add SUT profile fields without changing execution dispatch. |
| Execution evidence | `MetBench_BLL.Core/SystemMT/Pipeline/`, `MetBench_BLL.Core/SystemMT/Persistence/`, LiteDB evidence tests | Add pair-level planned/executed/valid/passed/failed/skipped/invalid counters and skip reasons. |
| Reports | `MetBench_BLL.Core/SystemMT/Reporting/`, `MetBench_BLL.Core/Reporting/`, reporting tests | Project pair counts and pass rates into HTML and Markdown execution reports. |
| WPF | `MetBench_Client/ViewModels/`, `MetBench_Client/Views/Pages/`, client i18n resources | Show explanation cards and pair quality indicators. Requires Windows VM validation. |
| Docs / governance | `docs/usage/`, `docs/status/current.md`, active plan index | Update user-facing guide and status projection after implementation lands. |

## Metrics and Formulas

| Metric | Formula / rule |
|---|---|
| `planned_pairs` | Count of source-follow-up verification pairs intended by the MR run. Two-role MR = 1; multi-phase MR = number of ordered phase comparisons declared by the typed spec. |
| `executed_pairs` | Pairs for which all required role outputs were produced. |
| `valid_pairs` | Executed pairs that reached an applicable typed verifier status of Passed or Failed. |
| `passed_pairs` | Valid pairs with verifier status Passed. |
| `failed_pairs` | Valid pairs with verifier status Failed. |
| `skipped_pairs` | Pairs skipped because observables, runtime, environment, or applicability were missing. |
| `invalid_spec_pairs` | Pairs rejected because the typed spec or predicate is invalid. |
| `pass_rate_valid` | `passed_pairs / valid_pairs`; returns 0.0 when `valid_pairs == 0`. |
| `pass_rate_all` | `passed_pairs / planned_pairs`; returns 0.0 when `planned_pairs == 0`. |

Skipped, not-applicable, and invalid-spec states must never be folded into Passed. They must be counted and displayed separately.

## Plan Table

| Task | Impact range | Status | Preconditions | Main steps | Acceptance standard | Quality monitoring |
|---|---|---:|---|---|---|---|
| P0 Design lock and registration | Docs only | Complete (`3129ecb`) | `origin/main` fetched; current ledger and active index read | Record scoped design, formulas, compatibility rule, Windows classification | Plan appears in active index; no code touched | PR checklist; no ambiguous pair formulas |
| P1 Equation explanation profile | Metadata and seed catalog | Complete (`59b37cc`) | P0 merged | Extend `EquationMetadata`; seed equation class, variables, canonical law, expected invariants, benchmark rationale | Built-in equations round-trip; editor validation covers required explanation for new user rows | Metadata tests; LiteDB round-trip tests |
| P2 SUT explanation profile | SUT manifests and SUT editor | Complete (`59b37cc`) | P1 profile names stable | Add program type, solver method, runtime key, input/output contract, adapter, dependency risk | All runtime SUTs expose a SUT profile; unknown runtime remains fail-closed | Manifest validation; catalog count whitelist |
| P3 MR explanation profile | MR metadata and typed projection | Complete (`95f674d`) | P1/P2 completed | Add meta-pattern rationale, transform semantics, observables, predicate, tolerance, applicability, failure meaning | All current runtime MRs can render a meaningful MR card | Typed catalog boundary tests; MR id parity |
| P4 Pair quality evidence | Pipeline, recorder, evidence schema | Complete (`74a5292`) | P3 complete; pair counting rule approved | Add pair summary DTO; populate from two-role and multi-phase outcomes; persist beside typed verification | Existing evidence rows still read; new runs persist pair summary; non-Failed typed statuses do not create false failures | Evidence round-trip tests; anomaly correctness tests |
| P5 Report projection | HTML / Markdown execution reports | Complete locally (`730723a`; local `main` ahead of `origin/main`) | P4 data available | Add pair summary block and skip reason distribution; keep legacy no-evidence rendering stable | Reports show pair counts and pass rates; legacy path remains byte-identical when evidence absent | Renderer contract tests; report snapshot tests |
| P6 WPF display | Catalog pages, execution history, i18n | Pending | P5 merged/pushed; Windows VM available | Add explanation cards and pair quality badges; update localized strings; capture screenshots | Windows build has 0 errors; UI shows explanation and pair metrics on key pages | VM build log; UIA/FlaUI or screenshot matrix |
| P7 Documentation and status sync | Usage guide, ledger, active index | Partially updated; final completion pending P6 evidence | P6 evidence collected | Update Chinese usage guide and status ledger with exact evidence | Docs cite concrete commits, commands, screenshots; active plan moved to Completed | Spec-freshness guard; status truth-source review |

## Detailed Implementation Tasks

### Task P0: Design lock and active-plan registration

**Files:**
- Modify: `docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md`
- Keep: `docs/status/current.md`
- Keep: `CLAUDE.md`

- [ ] **Step 1: Verify current truth sources**

Run:

```bash
rtk git fetch origin
rtk git status --short --branch
rtk sed -n '1,120p' docs/status/current.md
rtk sed -n '1,90p' docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md
rtk sed -n '70,150p' CLAUDE.md
```

Expected: fetch succeeds; worktree scope is known; current ledger still states the active runtime catalog inventory separately from the v1.2 migration denominator.

- [ ] **Step 2: Register this plan**

Add a row for `docs/superpowers/plans/2026-06-01-systemmt-explainability-pair-quality-plan.md` as an Active scoped plan.

- [ ] **Step 3: Validate docs-only diff**

Run:

```bash
rtk git diff -- docs/superpowers/plans/2026-06-01-systemmt-explainability-pair-quality-plan.md docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md
```

Expected: only the new plan and active index row changed.

### Task P1: Equation explanation profile

**Files:**
- Modify: `MetBench_BLL.Core/SystemMT/Metadata/EquationMetadata.cs`
- Modify: `MetBench_BLL.Core/SystemMT/Metadata/SystemMtMetadataCatalog.cs`
- Modify: `MetBench_BLL.Core/SystemMT/Metadata/Editing/EquationMetadataDraft.cs`
- Test: `MetBench_SystemMT.Tests/SystemMT/Metadata/MrArchitectureSchemaP0Tests.cs`
- Test: `MetBench_SystemMT.Tests/SystemMT/Metadata/Editing/SystemMtEquationEditorTests.cs`

- [ ] **Step 1: Write failing metadata round-trip tests**

Add tests that construct an `EquationMetadata` with:

```csharp
EquationClass = "PDE";
EquationFamily = "elliptic";
PrimaryVariables = new List<string> { "u(x)" };
PhysicalMeaning = "Steady scalar field governed by source balance.";
BenchmarkRationale = "Representative elliptic PDE used for mesh convergence and source linearity checks.";
ExpectedLaws = new List<string> { "linearity", "mesh-convergence" };
```

Expected before implementation: compile fails because fields are missing.

- [ ] **Step 2: Add minimal metadata fields**

Add nullable/default-empty fields so old LiteDB rows deserialize:

```csharp
public string EquationClass { get; set; } = string.Empty;
public string EquationFamily { get; set; } = string.Empty;
public List<string> PrimaryVariables { get; set; } = new();
public string PhysicalMeaning { get; set; } = string.Empty;
public string BenchmarkRationale { get; set; } = string.Empty;
public List<string> ExpectedLaws { get; set; } = new();
```

- [ ] **Step 3: Seed built-in explanations**

Populate all built-in equations in `SystemMtMetadataCatalog` with non-empty explanation values. Keep existing keys unchanged.

- [ ] **Step 4: Run focused tests**

Run:

```bash
rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~Metadata"
```

Expected: metadata and editor tests pass.

### Task P2: SUT explanation profile

**Files:**
- Modify: `SUT/*/catalog.json`
- Modify: `MetBench_BLL.Core/SystemMT/Catalog/Model/`
- Modify: `MetBench_BLL.Core/SystemMT/Catalog/Editing/`
- Test: `MetBench_SystemMT.Tests/SystemMT/Catalog/ManifestMrCatalogProviderTests.cs`
- Test: `MetBench_SystemMT.Tests/SystemMT/Metadata/Editing/SystemMtSutEditorTests.cs`

- [ ] **Step 1: Add manifest validation tests**

Add tests requiring a SUT profile with:

```json
{
  "program_type": "Num",
  "solver_method": "finite-difference",
  "runtime_key": "system",
  "input_contract": "JSON params with mesh and coefficient fields",
  "output_contract": "JSON metrics consumed by typed verifier",
  "adapter": "python runner under SUT/<sut>/",
  "dependency_risk": "pure-stdlib"
}
```

Expected before implementation: schema field missing or ignored.

- [ ] **Step 2: Add additive SUT profile model**

Introduce a profile model with default-empty strings. Validation for built-in manifests should warn in tests but not break old rows until all current manifests are updated in the same PR.

- [ ] **Step 3: Populate current runtime SUTs**

Update current `SUT/*/catalog.json` files with concise profile blocks. Do not change MR ids, transform steps, predicate codes, runtime keys, or sample paths.

- [ ] **Step 4: Run catalog tests**

Run:

```bash
rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~Catalog"
```

Expected: manifest provider tests pass and catalog counts remain unchanged.

### Task P3: MR explanation profile

**Files:**
- Modify: `MetBench_BLL.Core/SystemMT/Metadata/MrMetadata.cs`
- Modify: `MetBench_BLL.Core/SystemMT/Metadata/SystemMtMetadataCatalog.cs`
- Modify: `MetBench_BLL.Core/SystemMT/Catalog/Typed/Migration/`
- Test: `MetBench_SystemMT.Tests/SystemMT/Metadata/`
- Test: `MetBench_SystemMT.Tests/SystemMT/SemanticCatalogBoundaryTests.cs`

- [ ] **Step 1: Add MR profile tests**

Add a test MR with:

```csharp
MetaPatternRationale = "Linearity MR: scaling the source should scale the solution.";
TransformSemantics = "Scale source input field by factor.";
ObservableSemantics = "Compare selected scalar or field residual after source/follow-up runs.";
PredicateSemantics = "Binary comparison with configured tolerance.";
ApplicabilityCondition = "Only valid when the SUT exposes the named metric.";
FailureMeaning = "MR violation indicates inconsistent response to the declared transformation.";
```

Expected before implementation: compile fails because fields are missing.

- [ ] **Step 2: Add additive metadata fields**

Add string fields with empty defaults. Do not change enum values or existing metadata keys.

- [ ] **Step 3: Seed all current runtime MRs**

Populate explanations for the current runtime MR inventory. Keep v1.2 typed migration denominator separate from runtime catalog count in wording.

- [ ] **Step 4: Run boundary and metadata tests**

Run:

```bash
rtk dotnet test MetBench_SystemMT.Tests --filter "SemanticCatalogBoundaryTests|Metadata"
```

Expected: no legacy assertion path reintroduced; metadata tests pass.

### Task P4: Pair quality evidence

**Files:**
- Create: `MetBench_BLL.Core/SystemMT/Persistence/PairQualityEvidence.cs`
- Modify: `MetBench_BLL.Core/SystemMT/Persistence/ExecutionEvidence.cs`
- Modify: `MetBench_BLL.Core/SystemMT/Pipeline/PipelineOutcome.cs`
- Modify: `MetBench_BLL.Core/SystemMT/Pipeline/SystemMtExecutionRecorder.cs`
- Modify: `MetBench_BLL.Core/SystemMT/Pipeline/SystemMtPipeline.cs`
- Test: `MetBench_SystemMT.Tests/SystemMT/Persistence/`
- Test: `MetBench_SystemMT.Tests/SystemMT/Pipeline/`
- Test: `MetBench_SystemMT.Tests/V2Anomaly/AnomalyCreationOnFailureTests.cs`

- [ ] **Step 1: Add evidence shape tests**

Define expected evidence:

```csharp
new PairQualityEvidence
{
    PlannedPairs = 1,
    ExecutedPairs = 1,
    ValidPairs = 1,
    PassedPairs = 1,
    FailedPairs = 0,
    SkippedPairs = 0,
    InvalidSpecPairs = 0,
    PassRateValid = 1.0,
    PassRateAll = 1.0,
    SkipReasons = new Dictionary<string, int>()
};
```

Expected before implementation: compile fails because the type is missing.

- [ ] **Step 2: Implement additive POCO and mapper**

Create `PairQualityEvidence` with integer counters, double pass rates, and `Dictionary<string, int> SkipReasons`. Provide a factory that clamps division by zero to `0.0`.

- [ ] **Step 3: Populate from pipeline outcome**

For two-role MR runs, write exactly one planned pair. For multi-phase runs, use ordered phase comparisons declared by the multi-phase outcome. Map `Passed` and `Failed` typed statuses into valid pairs. Map `SkippedNotApplicable`, `SkippedMissingObservable`, and environment skips into skipped pairs. Map `InvalidSpec` into invalid spec pairs.

- [ ] **Step 4: Persist beside typed verification**

Add nullable `PairQuality` to `ExecutionEvidence`. Old evidence rows must deserialize with `PairQuality == null`.

- [ ] **Step 5: Run focused tests**

Run:

```bash
rtk dotnet test MetBench_SystemMT.Tests --filter "PairQuality|ExecutionEvidence|AnomalyCreationOnFailure"
```

Expected: old evidence compatibility, pair formulas, and anomaly non-failure semantics all pass.

### Task P5: Report projection

**Files:**
- Modify: `MetBench_BLL.Core/SystemMT/Reporting/HtmlSystemMtResultReportRenderer.cs`
- Modify: `MetBench_BLL.Core/Reporting/SystemMtReportService.cs`
- Test: `MetBench_SystemMT.Tests/Reporting/HtmlSystemMtResultReportRendererTests.cs`
- Test: `MetBench_SystemMT.Tests/V2Reporting/SystemMtReportServiceTests.cs`

- [ ] **Step 1: Add failing renderer tests**

Assert HTML contains:

```text
Pairs planned: 3
Pairs valid: 2
Pair pass rate (valid): 50.0%
Skipped pairs: 1
```

Expected before implementation: assertions fail.

- [ ] **Step 2: Render pair summary only when evidence exists**

If no evidence dictionary or no `PairQuality`, keep legacy report unchanged. If present, render counts and pass rates using invariant culture.

- [ ] **Step 3: Add Markdown execution section**

Append:

```markdown
## Pair quality
- Planned pairs: 3
- Executed pairs: 3
- Valid pairs: 2
- Passed pairs: 1
- Failed pairs: 1
- Skipped pairs: 1
- Invalid spec pairs: 0
- Pass rate (valid): 50.0%
- Pass rate (all): 33.3%
```

- [ ] **Step 4: Run report tests**

Run:

```bash
rtk dotnet test MetBench_SystemMT.Tests --filter "Reporting|SystemMtReportService"
```

Expected: pair summary renders; no-evidence legacy tests remain byte-identical.

### Task P6: WPF display

**Files:**
- Modify: `MetBench_Client/ViewModels/SystemMtEquationCatalogViewModel.cs`
- Modify: `MetBench_Client/ViewModels/SystemMtSutCatalogViewModel.cs`
- Modify: `MetBench_Client/ViewModels/SystemMtMrCatalogViewModel.cs`
- Modify: `MetBench_Client/Views/Pages/SystemMtEquationCatalogPage.xaml`
- Modify: `MetBench_Client/Views/Pages/SystemMtSutCatalogPage.xaml`
- Modify: `MetBench_Client/Views/Pages/SystemMtMrCatalogPage.xaml`
- Modify: `MetBench_UI.Localization/`
- Test: `MetBench_Client.Tests/`
- Test: `MetBench_SystemMT.Tests --filter ClientI18n`

- [ ] **Step 1: Add view-model tests**

Assert selected equation/SUT/MR exposes explanation strings and empty values render as a localized "Not specified" fallback.

- [ ] **Step 2: Add compact explanation cards**

Use existing WPF page layout patterns. Do not put cards inside cards. Keep text compact, scannable, and localized.

- [ ] **Step 3: Add execution-history pair indicators**

Show planned pairs, valid pass rate, and skipped count where execution evidence is available. If evidence is absent, show a localized unavailable state.

- [ ] **Step 4: Run cloud-safe tests**

Run:

```bash
rtk dotnet test MetBench_SystemMT.Tests --filter ClientI18n
rtk dotnet test MetBench_Client.Tests --filter ClientI18n
```

Expected: localization tests pass on supported environment. If WPF SDK is unavailable on Linux, record the exact blocker and move to VM validation.

- [ ] **Step 5: Windows VM validation**

Run on Windows:

```powershell
dotnet build MetBench.sln
dotnet test MetBench_SystemMT.Tests --filter ClientI18n
dotnet test MetBench_Client.Tests --filter ClientI18n
```

Expected: build has 0 errors; tests pass; screenshots show equation/SUT/MR explanation cards and pair quality indicators.

### Task P7: Documentation and status sync

**Files:**
- Modify: `docs/usage/MetBench-T0-T5-操作指南.md`
- Modify: `docs/status/current.md`
- Modify: `docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md`

- [ ] **Step 1: Update user guide**

Document where users see equation explanations, SUT explanations, MR explanations, pair counts, and pass rates.

- [ ] **Step 2: Update status ledger after implementation evidence exists**

Record exact commit, commands, test counts, Windows evidence, and whether this plan is Controlled.

- [ ] **Step 3: Move active index row to Completed**

Only after P1-P6 land and verification evidence exists, change this plan row from Active to Completed.

- [ ] **Step 4: Run docs checks**

Run:

```bash
rtk git diff --check
rtk rg -n "T[B]D|TO[D]O|待[补]" docs/superpowers/plans/2026-06-01-systemmt-explainability-pair-quality-plan.md docs/status/current.md docs/usage/MetBench-T0-T5-操作指南.md
```

Expected: no whitespace errors; no unresolved placeholders introduced by this work.

## PR Sequence

| PR | Scope | Merge condition |
|---|---|---|
| PR-0 | This plan and active index registration | Docs diff reviewed; no code changes. |
| PR-1 | Equation + SUT explanation profiles | Metadata and catalog focused tests pass. |
| PR-2 | MR explanation profile | Metadata and semantic boundary tests pass. |
| PR-3 | Pair quality evidence | Evidence, pipeline, anomaly tests pass. |
| PR-4 | Report projection | Implemented locally at `730723a`; clean `git archive HEAD` snapshot report tests exited 0, but RTK returned binlog-only summaries without counts. |
| PR-5 | WPF display + final docs/status sync | Pending; cloud-safe tests plus Windows VM build/screenshots must pass before this plan can move to Completed. |

## Validation Commands

Use focused commands per task, then before PR-5 completion run:

```bash
rtk dotnet test MetBench_SystemMT.Tests
```

Windows-only validation must be collected separately for WPF:

```powershell
dotnet build MetBench.sln
dotnet test MetBench_Client.Tests --filter ClientI18n
```

## Risks and Controls

| Risk | Control |
|---|---|
| Explanation fields drift from runtime catalog | Add manifest/profile validation and keep catalog count whitelist unchanged. |
| Pair pass rate hides skipped or invalid work | Display skipped and invalid-spec counts separately; define valid denominator explicitly. |
| Old evidence rows fail deserialization | Make `PairQuality` nullable and add old-row round-trip tests. |
| Multi-phase MR pair counts become ambiguous | Treat ordered phase comparisons as the only counted pairs; document the role convention in tests. |
| WPF claims made from Linux-only evidence | Require Windows build/log/screenshot evidence for P6/P7. |
