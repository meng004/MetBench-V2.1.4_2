# MetBench Current Status Ledger

> **Status date**: 2026-05-25
> **Ledger role**: the single current-status source for monitoring, planning, and handoff.
> **Rule**: other documents may project this status for their own purpose, but they do not redefine it.

---

## 1. Current Main

| Field | Value |
|---|---|
| Repository | `meng004/MetBench-V2.1.4_2` |
| Main branch | `origin/main` |
| Current main head | Resolve live from git; do not copy a static commit from this file |
| Last audited governance baseline | `6218106`, PR #112, `docs(governance): add control rules and next-stage audit` |
| Root worktree status at ledger introduction | `main...origin/main`, clean |

## 2. Latest Auditable Code Baseline

| Field | Value |
|---|---|
| Latest code-test baseline commit | `778ca04` |
| Baseline source | PR #130, `feat(systemmt): annotate anomalies with typed verification summary` |
| Test command | `dotnet test MetBench_SystemMT.Tests --no-build` |
| Result | `1081 pass / 0 fail / 8 skip` |
| Reason skips are not zero | Skips are the OpenMOC / OpenMC integration tests that skip cleanly when the matching Python venv is missing (per `CLAUDE.md` §8). They are environment-dependent and not regression. |
| Prior baselines | `545f8ce` (PR #128, evidence-aware markdown report) was `1077 / 0 / 8`; `a0d1add` (PR #126, evidence-aware HTML reporting) was `1072 / 0 / 8`; `970c7f9` (PR #124, dormant legacy code mapping) was `1065 / 0 / 8`; `7798515` (PR #123, live typed verification wiring) was `1062 / 0 / 8`; `ad0bb4b` (PR #121, ExecutionEvidence v2 PR-C0) was `1061 / 0 / 8`; `5d4dcc7` (PR #119, PR-D verification-semantics convergence cleanup) was `1048 / 0 / 8`; `e839214` (PR #110) remains the v1.2 retrospective historical baseline. |

## 3. Stage 8 Completion Snapshot

| Area | Status | Evidence |
|---|---|---|
| Catalog convergence / metadata / evidence | Complete for current mainline, with evidence granularity still eligible for expansion | PR #91-#95 |
| MR verification v1.2 | Complete for current roadmap | PR #97-#110 |
| v1.2 inventory denominator | `44 MR + 4 Property` | PR #109 / PR #110 migration and coverage gates |
| Documentation truth-source alignment | Complete through PR #111 | PR #111 |
| Governance baseline | Complete through PR #112 | PR #112 |
| Verification semantics convergence | Controlled — convergence closed | PR #115 (PR-B typed catalog rename), PR #118 (PR-C typed predicate runtime), PR #119 (PR-D architecture guards + W1 cleanup). System MT pipeline assertion stage now runs only on Typed Semantic Catalog predicates; the W1 `IMrAssertion` interface and its `Approx/Greater/Less` implementations plus `SystemMtRunner` and `EqualityThresholds` are removed from production. `SemanticCatalogBoundaryTests` prevents re-introduction. |
| ExecutionEvidence v2 schema and recorder | Controlled — PR-C0 merged | PR #121 added the nullable `TypedVerification` block on `ExecutionEvidence` plus 3 supporting POCOs, a stateless mapper, and a recorder overload. LiteDB collection layout, `IdEvidence` BSON id, `ExecutionId` unique index, `SystemMtResultRecord`, `ISystemMtLauncher`, `MrRunResult`, and `HtmlSystemMtResultReportRenderer` signatures all unchanged. `MrRunResultShapeLockTests` pins the facade. |
| Live typed verification wiring | Controlled — PR-123 merged | PR #123 captures the typed `(MrSpec, PredicateSpec, VerificationResult)` triple inside `SystemMtPipeline.ExecuteAsync`, carries it through three nullable init-only properties on `PipelineOutcome`, and `SystemMtExecutionRecorder` now reads them via outcome-fallback precedence. Live evidence rows produced by the System MT pipeline now populate `ExecutionEvidence.TypedVerification`; `Live_pipeline_outcome_carries_typed_triple_into_evidence_without_explicit_typed_args` is the regression guard. |
| Dormant legacy assertion code mapping | Controlled — PR-124 merged for the 4 representable codes; 2 noise-aware codes remain intentionally fail-closed | PR #124 added `MapVarianceRatio`, `MapFluxPointwise`, `MapCrossProgramAgree`, and an `approx-invariant -> BinaryComparisonPredicate(Equal)` case to `LegacyAssertionPredicateMapper`. The two noise-aware scalar codes (`less-noise-aware`, `greater-noise-aware`) remain fail-closed because the Typed Semantic Catalog scalar predicates cannot yet carry `SourceStd` / `FollowupStd` / `NoiseMultiplier` inputs; `Noise_aware_scalar_codes_fail_closed_with_documented_reason` pins the documented exception message. |
| Evidence-aware HTML report rendering | Controlled — PR-126 merged | PR #126 extended `HtmlSystemMtResultReportRenderer` and `ISystemMtResultReportRenderer` with an evidence-aware overload `Render(records, IReadOnlyDictionary<Guid, ExecutionEvidence>?, ReportContext?)` that surfaces `ExecutionEvidence.TypedVerification` (SpecId / SpecKind / Predicate / Status / Diagnostic / SkipOrInvalidReason / ordered PropertyPredicates) in the per-row `<details>` block. The legacy two-arg overload delegates to the new one with `evidenceByExecutionId: null`, so callers without an evidence dictionary render byte-identical HTML. `Render_without_evidence_dictionary_matches_legacy_overload_byte_identical` pins that contract. |
| Evidence-aware execution markdown report | Controlled — PR-128 merged | PR #128 extended `SystemMtReportService` (ctor: optional `IExecutionEvidenceRepository? evidence = null`) so `GenerateExecution` appends a `## Typed verification` markdown section whenever an evidence row with `TypedVerification` exists for the execution. Markdown surface mirrors the HTML projection (Spec kind / Spec ID / Predicate / Status / Diagnostic / Skip reason / ordered Property predicates) with invariant-culture numeric formatting. The legacy 5-arg ctor and the single-arg `BuildExecutionMarkdown` are retained; all pre-existing `SystemMtReportServiceTests` still pass without source edits. |
| Anomaly typed-verification annotation | Controlled — PR-130 merged | PR #130 extended `IAnomalyService.RecordAnomalyAsync` with an optional `string? typedVerificationSummary` parameter; `SystemMtLauncher.RecordAnomalyIfFailedAsync` projects `PipelineOutcome.TypedSpec` / `TypedPredicate` / `TypedVerification` into a one-line summary (`typed=<Status> metric=<Metric> predicate=<id> (<Kind>) residual=<G> tolerance=<G>`, with `reason: <text>` appended for Skipped*/InvalidSpec) and passes it through. The summary lands in `Anomaly.Notes` and the `anomaly.created` audit `detailsJson`. When no typed verification is present (legacy / error / timeout paths), `Notes` stays empty and `detailsJson` byte-identical to PR-129. |
| System MT architecture re-review | Controlled | Recorded in `docs/superpowers/specs/2026-05-25-systemmt-architecture-review-post-evidence-v2.md` at `ad0bb4b`. Method MT isolation, Typed Catalog upward independence, launcher facade insulation, and W1 cleanup all verified by `grep` audit. |

## 4. Active Control Documents

| Document | Role |
|---|---|
| `docs/superpowers/specs/2026-05-25-metbench-project-control-rules.md` | Control charter: source hierarchy, gates, review, status refresh |
| `docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md` | Active plan registry |
| `docs/superpowers/plans/2026-05-25-metbench-governed-next-stage-plan.md` | Current governed next-stage master plan |
| `docs/superpowers/specs/2026-05-25-metbench-macro-assessment-and-risk-audit.md` | Macro map and risk audit |
| `docs/superpowers/specs/2026-05-25-systemmt-architecture-review-post-evidence-v2.md` | Post-PR-D / post-PR-C0 System MT dependency-boundary audit: Method MT isolation, Typed Catalog upward independence, launcher facade insulation, W1 cleanup verification |
| `docs/superpowers/templates/pr-gate-checklist.md` | Required PR checklist template |

## 5. Projection Documents

The following documents are projections of the ledger and their own domain. They must not introduce a competing current-status truth:

| Document | Projection responsibility |
|---|---|
| `docs/requirements.md` | Requirement -> implementation -> test traceability |
| `AGENTS.md` | Roadmap and staged delivery projection |
| `CLAUDE.md` | Agent collaboration and engineering constraints |
| `docs/PROJECT-STRUCTURE.md` | Repository structure and test matrix projection |

## 6. Current Open Risks

| Risk | Status | Required next step |
|---|---|---|
| Verification semantics convergence | Controlled | Convergence is closed via PR #115 (PR-B), PR #118 (PR-C), PR #119 (PR-D). Maintenance only: keep `SemanticCatalogBoundaryTests` and `SemanticCatalogNamingBoundaryTests` green; future typed-predicate extensions must add new typed predicates in `MetBench_BLL.Core/SystemMT/Catalog/Typed/` and extend `LegacyAssertionPredicateMapper` rather than re-introducing legacy assertion classes. |
| ExecutionEvidence final shape | Controlled — full lifecycle closed; both report sinks (HTML + markdown) parity'd | PR #121 implemented the typed-verification persistence block + recorder projection (PR-C0). PR #123 wired `SystemMtPipeline.ExecuteAsync` to carry the typed `(MrSpec, PredicateSpec, VerificationResult)` triple through `PipelineOutcome` so live evidence rows now populate `ExecutionEvidence.TypedVerification`. PR #126 surfaced the typed block in HTML reports via an evidence-aware `HtmlSystemMtResultReportRenderer.Render(...)` overload. PR #128 surfaced the same projection in the markdown execution report via an optional `IExecutionEvidenceRepository?` ctor parameter on `SystemMtReportService`. The ExecutionEvidence v2 design's full lifecycle (data → persistence → recorder → renderer (HTML + markdown)) is now end-to-end. `Live_pipeline_outcome_carries_typed_triple_into_evidence_without_explicit_typed_args`, `Render_without_evidence_dictionary_matches_legacy_overload_byte_identical`, and `GenerateExecution_appends_typed_mr_verification_section_when_evidence_present` together pin the regression contract. |
| Codegraph and architecture re-review | Controlled | Recorded in `docs/superpowers/specs/2026-05-25-systemmt-architecture-review-post-evidence-v2.md` against `ad0bb4b`. All four audit questions pass: Method MT isolation, Typed Catalog upward independence, launcher facade insulation, W1 cleanup. Refresh required only when the next System MT cross-cutting change lands. PR #123 / #124 / #126 / #128 / #130 did not introduce any new cross-module dependency: Anomaly module already depended on `MetBench_Domain.Anomaly` (Notes is a pre-existing free-text field); the launcher's new `BuildTypedVerificationSummary` helper consumes only `Catalog.Typed.Specs` types it already imported via `PipelineOutcome`. |
| Windows verification policy | Partially controlled | Use PR gate classification now; write the dedicated Windows policy before the next Windows-touching PR. PR-B/C/D and PR #121 / #122 / #123 / #124 / #125 / #126 / #127 / #128 / #129 / #130 were all Linux-only and did not require Windows validation. |
| Legacy assertion code mapping | Controlled — 4 mapped, 2 intentionally fail-closed | PR #124 mapped `approx-invariant` → `BinaryComparisonPredicate(Equal)`, `variance-ratio` → `VarianceRatioPredicate`, `flux-pointwise-approx` → `FieldEqualityPredicate` (Identity pairing), and `cross-program-agree` → `CrossMethodComparisonPredicate(Equal)`. **Intentionally fail-closed** with documented reason: `less-noise-aware` and `greater-noise-aware` — the legacy scalar form takes `SourceStd` / `FollowupStd` / `NoiseMultiplier`, which the Typed Semantic Catalog scalar predicates do not yet carry. Before adopting these two codes in any future MR catalog binding, a noise-aware typed predicate (e.g. `NoiseAwareBinaryComparisonPredicate(SourceStd, FollowupStd, NoiseMultiplier, Operator)`) must be added under `MetBench_BLL.Core/SystemMT/Catalog/Typed/Specs/`, validated, kernel-implemented, and then exposed via a new `LegacyAssertionPredicateMapper.MapNoiseAwareScalar(...)` overload. |
| Active vs historical plan drift | Controlled but must be maintained | Keep the active plan index current whenever a phase changes |

## 7. Current Execution Order

Verification-semantics convergence (PR-A → PR-D, #114 / #115 / #118 / #119), ExecutionEvidence v2 (PR-A0 #116, PR-C0 #121, live wiring #123, evidence-aware reporting #126), the post-convergence architecture audit (#122), the dormant legacy code mapping (#124), and the status ledger refreshes (#120 / #125 / #127) are all merged. The ExecutionEvidence v2 design's full lifecycle is now closed. The next stage proceeds in this order:

1. Maintain this ledger as the current status source.
2. Use the active plan index to select work.
3. When the first MR catalog binding actually adopts `less-noise-aware` or `greater-noise-aware`, add a noise-aware typed predicate to `MetBench_BLL.Core/SystemMT/Catalog/Typed/Specs/` (e.g. `NoiseAwareBinaryComparisonPredicate`), implement its kernel + validator, and extend `LegacyAssertionPredicateMapper` with a `MapNoiseAwareScalar(...)` overload. Until such a binding exists, the two noise-aware codes remain intentionally fail-closed.
4. Design Windows verification policy before any next Windows-touching PR. Until then, all PRs must stay inside the Linux-CI-validated cross-platform projects.
5. Only then proceed to implementation PRs for the next stage (T2 visualization extensions, T3 coverage extensions, T4 MR discovery, T5 anomaly investigation, T6 mutation testing). A new scoped plan must be registered in the active plan index §1 before such work begins.

## 8. Update Triggers

Update this ledger when any of the following changes:

- latest auditable code-test baseline
- active plan
- inventory denominator
- Windows verification status
- open risk state
- Stage completion judgment

Do not update this ledger just to copy the latest merge commit. Monitoring should resolve the live `origin/main` head directly from git and then use this ledger for status interpretation.
