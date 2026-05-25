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
| Latest code-test baseline commit | `ad0bb4b` |
| Baseline source | PR #121, `feat(systemmt): add executionevidence v2 typed verification block` |
| Test command | `dotnet test MetBench_SystemMT.Tests --no-build` |
| Result | `1061 pass / 0 fail / 8 skip` |
| Reason skips are not zero | Skips are the OpenMOC / OpenMC integration tests that skip cleanly when the matching Python venv is missing (per `CLAUDE.md` §8). They are environment-dependent and not regression. |
| Prior baseline | `5d4dcc7` (PR #119) was the PR-D verification-semantics convergence cleanup baseline (`1048 / 0 / 8`); superseded by PR #121 ExecutionEvidence v2 PR-C0. `e839214` (PR #110) remains the v1.2 retrospective historical baseline. |

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
| System MT architecture re-review | Controlled | PR (this) lands `docs/superpowers/specs/2026-05-25-systemmt-architecture-review-post-evidence-v2.md` at `ad0bb4b`. Method MT isolation, Typed Catalog upward independence, launcher facade insulation, and W1 cleanup all verified by `grep` audit. |

## 4. Active Control Documents

| Document | Role |
|---|---|
| `docs/superpowers/specs/2026-05-25-metbench-project-control-rules.md` | Control charter: source hierarchy, gates, review, status refresh |
| `docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md` | Active plan registry |
| `docs/superpowers/plans/2026-05-25-metbench-governed-next-stage-plan.md` | Current governed next-stage master plan |
| `docs/superpowers/plans/2026-05-25-executionevidence-v2-implementation-plan.md` | Scoped implementation plan for ExecutionEvidence v2 (PR-A0 design lock and PR-C0 schema/recorder) |
| `docs/superpowers/specs/2026-05-25-metbench-macro-assessment-and-risk-audit.md` | Macro map and risk audit |
| `docs/superpowers/specs/2026-05-25-executionevidence-v2-design.md` | ExecutionEvidence v2 design: typed verification persistence block, recorder mapping, compatibility contract, PR-C scope boundaries |
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
| Verification semantics convergence | Controlled | Convergence is closed via PR #115 (PR-B), PR #118 (PR-C), PR #119 (PR-D). Maintenance only: keep `SemanticCatalogBoundaryTests` and `SemanticCatalogNamingBoundaryTests` green; if a future PR needs to extend typed predicate semantics (e.g. for the unmapped legacy codes `less-noise-aware`, `greater-noise-aware`, `approx-invariant`, `variance-ratio`, `flux-pointwise-approx`, `cross-program-agree`), add new typed predicates in `MetBench_BLL.Core/SystemMT/Catalog/Typed/` and extend `LegacyAssertionPredicateMapper` rather than re-introducing legacy assertion classes. |
| ExecutionEvidence final shape | Controlled — PR-C0 merged | PR #121 implemented the typed-verification persistence block + recorder projection. Recorder is now *capable* of writing `TypedVerification` when callers supply typed inputs; live `SystemMtPipeline.ExecuteAsync` does not yet plumb the typed dispatcher output into the recorder, so live rows still carry `TypedVerification == null`. Follow-up: wire pipeline → recorder typed-input flow (mechanical, no schema change). |
| Codegraph and architecture re-review | Controlled | Recorded in `docs/superpowers/specs/2026-05-25-systemmt-architecture-review-post-evidence-v2.md` against `ad0bb4b`. All four audit questions pass: Method MT isolation, Typed Catalog upward independence, launcher facade insulation, W1 cleanup. Refresh required only when the next System MT cross-cutting change lands. |
| Windows verification policy | Partially controlled | Use PR gate classification now; write the dedicated Windows policy before the next Windows-touching PR. PR-B/C/D were Linux-only and did not require Windows validation. |
| Unmapped legacy assertion codes | Dormant in CI; tracked | The 6 codes outside PR-C scope (`less-noise-aware`, `greater-noise-aware`, `approx-invariant`, `variance-ratio`, `flux-pointwise-approx`, `cross-program-agree`) now fail-closed when used through the System MT pipeline. No active test or catalog binding uses them through the pipeline today; surface check required before any MR catalog entry adopts them. Follow-up PR: extend `LegacyAssertionPredicateMapper` with the matching typed predicates already in `Catalog/Typed/` (`VarianceRatioPredicate`, `CrossMethodComparisonPredicate`, `FieldEqualityPredicate`, `FieldProportionalityPredicate`). |
| Active vs historical plan drift | Controlled but must be maintained | Keep the active plan index current whenever a phase changes |

## 7. Current Execution Order

Verification-semantics convergence (PR-A → PR-D, #114 / #115 / #118 / #119), ExecutionEvidence v2 PR-C0 (#121), and the post-convergence System MT architecture re-review are all merged. The next stage proceeds in this order:

1. Maintain this ledger as the current status source.
2. Use the active plan index to select work.
3. Wire the typed `VerificationResult` / `PropertyResult` from `SystemMtPipeline.ExecuteAsync` into `SystemMtExecutionRecorder.Record(typedVerification: ..., typedSpec: ..., typedPredicate: ...)`. PR-C0 made the recorder *capable*; live rows still carry `TypedVerification == null` until this wiring lands. This is a mechanical follow-up, no schema change.
4. Extend `LegacyAssertionPredicateMapper` to cover the 6 unmapped legacy assertion codes (`less-noise-aware`, `greater-noise-aware`, `approx-invariant`, `variance-ratio`, `flux-pointwise-approx`, `cross-program-agree`) by adding the matching typed predicates already in `Catalog/Typed/Specs/`. Required before any MR catalog binding adopts those codes.
5. Design Windows verification policy before any next Windows-touching PR. Until then, all PRs must stay inside the Linux-CI-validated cross-platform projects.
6. Only then proceed to implementation PRs for configuration, UI-facing work, or evidence-aware report rendering.

## 8. Update Triggers

Update this ledger when any of the following changes:

- latest auditable code-test baseline
- active plan
- inventory denominator
- Windows verification status
- open risk state
- Stage completion judgment

Do not update this ledger just to copy the latest merge commit. Monitoring should resolve the live `origin/main` head directly from git and then use this ledger for status interpretation.
