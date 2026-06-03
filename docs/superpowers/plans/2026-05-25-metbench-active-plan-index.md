# MetBench 活跃计划索引

> **日期**: 2026-06-02
> **状态**: 生效
> **目的**: 明确哪些计划仍指导当前开发，哪些只是历史记录，防止执行和监控误取旧计划。
> **Current audited head**: local `origin/main` = `a58a72c` (PR #265 merged PR-5 WPF display; GitHub MCP confirmed merge commit `a58a72c6c7cb84cc4af10d44724887a8fa73bfe2`; VM `git fetch origin` was blocked by DNS resolution for `github.com`). Strict acceptance is on `claude/systemmt-explainability-pr5-strict-acceptance`.

---

## 1. 当前活跃主计划与范围计划

| Plan | Status | Scope | Expiry condition |
|---|---|---|---|
| `docs/superpowers/plans/2026-06-03-systemmt-async-execution-cloud-plan.md` | Implemented (PR-1 #278 Task 1-3 merged; PR-2 Task 4-10 in review) | System MT 异步执行抽象层 **Cloud 契约层**（v1）：`MetBench_BLL.SystemMT.Jobs` job 子系统（service/store/queue/worker/async-pipeline）+ DAL `LiteDbJobStore`，polling-only 状态、local backend = 委托既有 `ISystemMtLauncher.RunAsync`，TDD + fake backend，CI 可门禁。**45 新 facts**（PR-1 19 + PR-2 26），全量 0 fail / 1680 pass / 12 skip。实施偏差（真实 `MrRunResult` 无 SutName / 无 Passed-Failed 工厂 / Passed 是断言位非基础设施位）已按 §12.4 R3 写回 plan frontmatter。设计来源 `docs/superpowers/specs/2026-06-03-systemmt-async-execution-polling-design.md`；冷启动提示词 `docs/superpowers/specs/2026-06-03-async-execution-cloud-prompt.md`。 | Expires when PR-2 合入 main 且状态账本记录 Controlled；若整链 ≥3 PR 触发 §12.4 R2 链尾 review |
| `docs/superpowers/plans/2026-06-03-systemmt-async-execution-vm-plan.md` | In progress / blocked on AC-V5 failure-state evidence | System MT async execution VM consumer (WPF): `SystemMtAsyncJobPage`/`ViewModel` + in-process `SystemMtJobWorkerHostedService` + DI/navigation wiring. VM branch `claude/async-execution-vm` has real evidence for page load, submit/queued job id, polling success, manual refresh, result display, cancel, no dispatcher blocking waits, and zero `MetBench_BLL.Core`/`MetBench_DAL` diff. AC-V5 failure-state screenshot remains blocked because all attempted dependency-sensitive candidates succeeded on this VM; evidence is in `docs/superpowers/specs/2026-06-03-async-execution-vm-verification/`. | Expires when AC-V5 is resolved, VM evidence lands, and status ledger records Controlled |
| `docs/superpowers/plans/2026-05-25-metbench-governed-next-stage-plan.md` | Active | Governance-first next-stage planning; blocks further implementation until the named design ambiguities are resolved | Expires when semantic-convergence, Evidence v2, Windows verification policy, and transition plan are completed and replaced by a new implementation plan |
| `docs/superpowers/plans/2026-06-01-systemmt-explainability-pair-quality-plan.md` | Controlled (PR0-PR5 merged; strict VM acceptance landed via PR #268) | Improve System MT equation / SUT / MR explanations and add pair-level execution quality summaries. Merged chain: PR-0 docs plan/index (`3129ecb`), PR-1 equation/SUT profiles (`59b37cc`), PR-2 MR profiles (`95f674d`), PR-3 pair-quality evidence (`74a5292`), PR-4 HTML/Markdown report projection (`730723a` via PR #255), PR-5 WPF display (`a58a72c` via PR #265). Strict acceptance branch `claude/systemmt-explainability-pr5-strict-acceptance` fixed VM-capturability and stale-selection issues, generated 01-07 screenshots in `docs/superpowers/specs/2026-06-02-systemmt-explainability-pr5-vm-verification/`, and captured real non-empty PairQuality from a WPF-launched pure-stdlib `advection-amplitude-linearity` execution. Tests: build 0 errors; ClientI18n 16/16; SystemMT ClientI18n 18/18; focused explanation/pair-quality/localization 12/12. Strict-acceptance branch landed via PR #268 (merged 2026-06-02); row moved to Controlled. | Already expired (chain complete; strict acceptance landed via PR #268); future explainability or pair-quality changes need a new scoped plan |
| `docs/superpowers/plans/2026-06-02-minimum-mr-subset-a-group-import-export-plan.md` | Completed (staging import/export + live runtime promotion) | First controlled import/export version for `minimum-mr-subset` A group only (`P5`, `P4`, `P9`) landed via PR #273 as staging import/export with explicit provenance and external canonical-run evidence, then this runtime promotion added live pure-stdlib launcher SUTs and E2E facts for `p5-power-response`, `p4-energy-invariant`, and explicit surrogate `p9-k-eff-noise-aware`. Evidence: `docs/superpowers/specs/2026-06-02-minimum-mr-subset-a-group-runtime-verification-vm-report.md` and `docs/superpowers/specs/2026-06-03-minimum-mr-subset-a-group-live-runtime-promotion.md`. P9 remains a deterministic OpenMC surrogate; no real OpenMC execution is claimed. | Already expired when this PR lands and the status ledger records Controlled |
| `docs/superpowers/plans/2026-06-02-minimum-mr-subset-put-import-export-plan.md` | Superseded discussion history | Earlier P5-first/P8-second import/export plan. Superseded after design review changed the model to a single-SUT import unit and selected A group (`P5`, `P4`, `P9`) as the first version. | Do not execute; retained only as discussion history |
| `docs/superpowers/plans/2026-05-30-t0-t5-minimal-release-readiness-confirmation-plan.md` | Completed (release-readiness evidence merged) | Confirmed T0-T5 minimum engineered System-MT release readiness. Final report records 21/21 core checks PASS, 100.0% core-function confirmation coverage, 3 SUT / 3 MR release-smoke slices covering Mono/Inv/Conv, 22/22 VM filtered commands PASS, full suite 1558 / 0 / 12, WPF build 0 errors, and VM screenshot matrix 21/21 PASS. | Already expired (release-readiness confirmation complete); keep as release gate evidence |
| `docs/superpowers/plans/2026-05-30-metbench-client-multilingual-i18n-plan.md` | Completed (client i18n merged) | Added UI-neutral `MetBench_UI.Localization` (`net8.0`, .resx/ResourceManager, no WPF/Avalonia dependency) plus WPF bindings, Settings language switcher, Chinese/English resources, abnormal fallback coverage, and VM UIA screenshots. | Already expired (i18n foundation complete); future Avalonia UI can reuse the localization core |
| `docs/superpowers/plans/2026-05-30-systemmt-page-full-i18n-plan.md` | Completed (full-page i18n merged) | Extended localization across System-MT execution/catalog pages, legacy/function pages, and runtime status strings; VM evidence includes paired zh/en screenshots for the major pages and additional ClientI18n resource/status tests. | Already expired (full-page bilingual UI evidence complete) |
| `docs/usage/MetBench-T0-T5-操作指南.md` | Completed user guide | Chinese illustrated T0-T5 user guide covering startup/language switch, T0 execution, T1 CRUD/catalog/history, T2 reports, T3 coverage dashboard, T4 discovery/candidate review, and T5 anomaly/replay. | Keep current until UI workflows change; English guide is not yet present |
| `docs/superpowers/plans/2026-05-26-t1-t4-ui-sequenced-execution-plan.md` | Completed orchestration plan (PR-0 / PR-1 / PR-2 all merged) | Mandatory execution order for PR-0 docs-only gate → PR-1 T1 multi-env → PR-2 T4-to-T0 binder → separate Windows/VM UI MR CRUD plan. All three cloud PRs landed: PR-0 (#154 docs-only gate, commit `c776f1a`), PR-1 (#157 `feat(t1): resolve SUT runtime environments from manifest keys`, commit `008cc80`), PR-2 (this PR `feat(t4): bind discovery candidates into draft System MT catalog assets`). UI MR CRUD remains a separate Windows/VM plan and must not be folded into a cloud PR. | Already expired (orchestration complete); UI MR CRUD continues under its own Windows/VM plan row |
| `docs/superpowers/specs/2026-05-26-t3-coverage-assessment-and-next-sut-decision.md` | Active scoped reference | T3 representative-PDE-class coverage assessment + next-SUT gate. Inventory anchor pre-§5.2: **13 SUT / 12 equations / 25 MRs** after PR #134 (Poisson) / #136 (Advection) / #138 (Wave) / #140 (Burgers). Post-§5.2 IVP: **14 SUT / 12 equations / 27 MRs**. Post-§5.3 BVP: **15 SUT / 12 equations / 29 MRs** (real-physics inventory; subsequent PR-A added one synthetic non-physics test SUT `_test_csv` for I/O helper integration regression, bringing catalog total to **16 SUT / 13 equations / 30 MRs** without changing real-physics inventory). **Pure-stdlib expansion paused**; external-solver-pilot expansion proceeded one candidate at a time under §4 driver #1 through IVP and BVP. §5.1 backlog (MeshGraphNets cylinder-flow surrogate) is **anticipation only**. Any further T3 SUT PR must add a new §5.x decision plus a candidate-specific plan. | Expires only when a newer T3 decision record supersedes it |
| `docs/superpowers/plans/2026-05-26-t1-manifest-driven-runtime-environments-plan.md` | Completed (PR-1 merged) | T1 cloud-side scalability fix. `LauncherOptions.RuntimePythons` map + `ResolvePythonExecutable` resolver replaces the per-runtime hardcoded switch. Built-in `system` / `openmoc` / `openmc` / `scipy` behaviour preserved; unknown non-system keys fail closed at resolution time. Adding a new runtime family is now config-only. Status ledger row "T1 multi-env management" moved Open → Controlled. | Already expired (merged); referenced from §3 for historical context |
| `docs/superpowers/plans/2026-05-26-t4-to-t0-mr-discovery-binder-plan.md` | Completed (PR-2 merged) | T4 cloud-side binder. `DiscoveredMrCatalogBinder` is the controlled bridge from T4 discovery candidates to T0 catalog assets; pure / deterministic / IO-free; fails closed on schema and semantics violations; emits a manifest-compatible `MrBindingDefinition` + provenance; never mutates `SUT/<sut>/catalog.json`. Status ledger row "T4-to-T0 binder" moved Queued → Controlled. | Already expired (merged); referenced from §3 for historical context |
| `docs/superpowers/plans/2026-05-26-t1-ui-mr-crud-windows-vm-plan.md` | Completed (merged via `c7d9a6d`) | UI MR CRUD shipped: System MT manifest editor backend (`MetBench_BLL.Core/SystemMT/Catalog/Editing/`) + dedicated WPF page (`MetBench_Client/Views/Pages/SystemMtMrCatalogPage.xaml`) for manifest selection, MR listing, draft creation, validation, save, and reload. Pre-merge Windows VM verification: mandatory filtered set 28/28 green, WPF CRUD end-to-end usable, path-safety / SaveDraft re-validation / Method MT pollution-filter risks covered by code + tests. Full-suite Windows VM verification green at SHA `365df51` via PR-T1-CLOSURE (`claude/t1-closure-windows-vm-followup`). | Already expired (merged); referenced from §3 for historical context |
| `docs/superpowers/plans/2026-05-26-noise-aware-and-bol-alg-02-sequenced-execution-plan.md` | Retracted orchestration plan — historical blocker corrected by PR #168 | Original claim: PR-N2 is operationally sequenced after PR-N1 but type-wise independent because variance-ratio is typed since PR #124. **Correction (2026-05-26)**: that claim was incomplete. While `LegacyAssertionPredicateMapper.MapVarianceRatio(...)` existed, the launcher path was not wired. PR #168 later supplied the missing variance-ratio launcher pipeline wiring; this orchestration plan remains retracted and must not be reused as an active implementation plan. | Already retracted; referenced from §3 for context |
| `docs/superpowers/plans/2026-05-26-typed-noise-aware-scalar-predicate-plan.md` | Completed (PR-N1 merged) | Shipped `NoiseAwareBinaryComparisonPredicate` + `NoiseAwareScalarToleranceEvaluator` + `NoiseAwareBinaryComparisonKernel` + validator + `LegacyAssertionPredicateMapper.MapNoiseAwareScalar(...)` overload under `MetBench_BLL.Core/SystemMT/Catalog/Typed/`. Closed `less-noise-aware` / `greater-noise-aware` legacy mappings. 66 new facts; status ledger row moved Open → Controlled. | Already expired (merged); referenced from §3 for historical context |
| `docs/superpowers/plans/2026-05-26-bol-alg-02-mc-particle-count-convergence-plan.md` | Retracted scoped implementation plan — superseded by `2026-05-26-pr-n2-bol-alg-02-mc-particle-count-convergence-plan.md` (row below) | Original intent: add the MR `openmc-pincell-particle-count-convergence` using a "already-typed" `variance-ratio` predicate. **Correction (2026-05-26)**: the variance-ratio path was mapped at the `LegacyAssertionPredicateMapper` level but not reachable through the launcher / pipeline at the time. PR-VR (#168) supplied the missing wiring, then the successor scoped plan was registered and shipped via PR-N2 (#170). | Already retracted; referenced from §3 for historical context |
| `docs/superpowers/plans/2026-05-26-variance-ratio-launcher-pipeline-wiring-plan.md` | Completed (PR-VR merged) | **PR-VR** — wired `variance-ratio` assertion-type code end-to-end. Shipped: `TypedSpecFactory.ForLegacyAssertion` (single migration-side dispatch site for `variance-ratio` and the legacy scalar codes), `TypedSpecFactory.ForVarianceRatio` (mrCode/metric/factor/toleranceRel → MrSpec with `SigmaMultiplier = 1 + ToleranceRel`), and `TypedVerificationContextFactory.FromScalarOutputs` extension that promotes `scalars[StatisticalMetric]` into `RoleOutput.Statistics` only when the spec carries a `VarianceRatioPredicate` (additive — other specs see `Statistics = null`). `SystemMtPipeline.EvaluateAssertion` reduced to 2 branches: provided typed spec vs single `ForLegacyAssertion` call; string-code dispatch confined to `Catalog/Typed/Migration/` per the SemanticCatalogBoundaryTests architecture guard. 44 new always-passing facts across 3 test files. No new MR catalog row in PR-VR — PR-N2 is the first consumer. Defence-in-depth `ExtraAssertionValues["refinement_factor"]` deferred (typed kernel path does not consume it). | Already expired (merged at `befbe5f`, #168); referenced from §3 for historical context |
| `docs/superpowers/plans/2026-05-26-pr-n2-bol-alg-02-mc-particle-count-convergence-plan.md` | Completed (PR-N2 merged at `b931b3f`, #170) | **PR-N2** — restart of the retracted Bol-Alg-02 plan, unblocked by PR-VR. Shipped: one `MrBlueprint` row (`openmc-pincell-particle-count-convergence`: factor=4, ToleranceRel=0.30, NoiseMultiplier=1.0, AssertionTypeCode=variance-ratio, ValueName=k_eff_std, TransformSteps=ScaleField("/solver/particles")), one `MrMetadata` row (ComparisonType=Relative — fallback for missing `MrComparisonType.Statistical` enum value, semantically equivalent for variance-ratio's relative tolerance band), one `solver.particles: 5000` field added to `SUT/openmc/sample/pincell.json` (no behavior change — matches the runner's previously-implicit default), pinned-count bump 30 → 31 across 6 test files + 1 production comment, 2 SkippableFacts in `LauncherEndToEndOpenMcParticleCountConvergenceTests` gated on `OpenMcTestPaths.OpenMcImportable()`. No new SUT, no new Python script, no new transform C# class. | Already expired (merged); referenced from §3 for historical context |
| `docs/superpowers/plans/2026-05-26-t1-cross-method-and-io-sequenced-execution-plan.md` | Completed orchestration plan (PR-0 / PR-B / PR-A all merged) | Mandatory execution order for the cloud-side T1 PRs: PR-0 docs gate (`453e369`) → PR-B T1 same-equation cross-method differential runner (`2f997dd`) → PR-A T1 non-JSON I/O adapter (this PR). All three merged. Cloud-side only; no Method MT, no WPF, no UI MR CRUD entanglement. | Already expired (orchestration complete) |
| `docs/superpowers/plans/2026-05-26-t1-cross-method-differential-runner-plan.md` | Completed (PR-B merged) | T1 cloud-side same-equation cross-method differential. `IDifferentialTestRunner` + sealed `DifferentialTestRunner` shipped in `MetBench_BLL.Core/SystemMT/Differential/` with three deterministic criteria (BothPassed / DirectionConcordant / FollowUpRatioWithinTolerance), explicit fail-closed reasons, 28 facts. Status ledger row "T1 same-equation cross-method differential" moved Open → Controlled. | Already expired (merged); referenced from §3 for historical context |
| `docs/superpowers/plans/2026-05-26-t1-non-json-io-adapter-plan.md` | Completed (PR-A merged) | T1 cloud-side non-JSON SUT I/O. Shipped `SUT/_shared/metbench_io/` Python helper (CSV + plain-text round-trip via stdlib) plus synthetic `SUT/_test_csv/` test SUT (MR `csv-roundtrip-identity` in family `TestCsv.Scaling.Identity`). Pinned counts bumped 29 → 30 / 15 → 16 across the six descriptor-list files. Status ledger row "T1 non-JSON I/O adapter" moved Open → Controlled. | Already expired (merged); referenced from §3 for historical context |
| `docs/superpowers/plans/2026-05-26-pr-bol-2-error-monotonic-sequenced-execution-plan.md` | Completed orchestration plan (PR-Bol-2A merged at `8c267a1` #179; PR-Bol-2B merged at `a4244d9` #181) | **PR-Bol-2 sequenced plan** — split Bol-Alg-01 delivery into PR-Bol-2A (error-monotonic launcher pipeline wiring, zero MR catalog rows) + PR-Bol-2B (first MR catalog consumer `openmoc-pincell-ray-track-convergence`). Q1–Q4 design recommendations locked: run SUT a 3rd time at fine settings (Q1); new `ExecuteMultiPhaseAsync` keeps `ExecuteAsync` byte-identical for the 31 existing 2-side MRs (Q2); new top-level `refinement_phases` array in `catalog.json` (Q3); `NormKind.Relative` for scalar `k_eff` (Q4). Phase-role convention: last phase = `ReferenceRole`, earlier phases = `OrderedRoles` in declared order. | Already expired (orchestration complete); referenced from §3 for historical context |
| `docs/superpowers/plans/2026-05-26-pr-bol-2b-openmoc-ray-track-convergence-plan.md` | Completed (PR-Bol-2B merged at `a4244d9`, #181; scoped plan doc merged at `196c31c`, #180) | **PR-Bol-2B** — first catalog consumer of the error-monotonic launcher pipeline wired by PR-Bol-2A. Shipped: one `MrBlueprint` row (`openmoc-pincell-ray-track-convergence`: `AssertionTypeCode="error-monotonic"`, `RefinementPhases=[coarse(1), medium(2), reference(4)]`, `ValueName="k_eff"`, `TransformSteps=ScaleField("/tracking/num_azim")` informational), one `MrMetadata` row (ComparisonType=Relative), one new Python adapter `SUT/openmoc/openmoc_input_adapter_refine_ray_tracks.py` that atomically mutates `tracking.num_azim` (×factor) + `tracking.azim_spacing_cm` (/factor), one new manifest MR row in `SUT/openmoc/catalog.json` with `refinement_phases`, pinned-count bump 31 → 32 across 6 test files + 1 production comment, 2 SkippableFacts in `LauncherEndToEndOpenMocRayTrackConvergenceTests` gated on `OpenMocTestPaths.OpenMocImportable()`. `OpenMocCatalogParityTests` rewritten to classify Mono/Conv per MR (mirror of PR-N2's `OpenMcCatalogParityTests` update). `SystemMtLauncherTests.ListAvailableAsync_returns_known_scenarios_in_id_order` positional pin inserted at the alphabetical slot. Status ledger row "PR-Bol-2 / Bol-Alg-01" moved to Controlled. | Already expired (merged); referenced from §3 for historical context |
| PR-Bol-3 (Bol-Alg-02 MC particle count convergence on OpenMC) | Completed — Bol-Alg-02 MR shipped via PR #170 (`b931b3f`); `docs/status/current.md` §3 records Controlled | `Bol-Alg-02 → VarianceRatioPredicate` on OpenMC pin-cell. Earlier "blocked" framings (noise-aware mistake then "already typed since #124" mistake) corrected via PR-VR (#168). The successor scoped plan superseded the retracted v1; the execution PR delivered the catalog row + metadata + pinned-count bump + SkippableFact end-to-end test. | Already expired (merged); referenced from §3 for historical context |
| `docs/superpowers/plans/2026-05-27-t2-systemmt-visualization-and-t3-gap-fill-plan.md` | Completed (6-phase chain + 2 cleanup PRs + 1 governance PR merged) | **T2 SystemMT visualization 4-end + T3 gap-fill sequenced plan** — closed across PR #184 (Phase 1 ChartData DTO `7526407`), PR #185 (Phase 2 SkiaSharp PNG renderer `0184f63`), PR #186 (Phase 3a iTextSharp PDF renderer `9e898f1`), PR #188 (Phase 3b OpenXml Word renderer `b76f4d7`), PR #190 (Phase 3c ClosedXML Excel renderer `0fd1b93`), PR #191 (Phase 4 MetaPatternMatrixAuditor + spec doc `4c07c1f`), PR #192 (Phase 5 `subchannel-friction-invariance` gap-fill MR `c14ffd9`), PR #193 (Phase 6 ledger refresh `d2e1c5d`). Post-merge holistic review surfaced 11 findings (5 Cat-A single-PR visible + 6 Cat-B cross-PR/retrospective) closed by PR #195 `f10cfc2` (M1/M2/M3/M5) + PR #199 `227959f` (L1/C1/T3/D1/D2). Governance rules codified via PR #203 `8e4bf19` (`CLAUDE.md §12.4` R1-R4 + §12.5 + chain-end review checklist). Worked-example post-merge review doc: [`docs/superpowers/specs/2026-05-27-t2-t3-chain-post-merge-review.md`](../specs/2026-05-27-t2-t3-chain-post-merge-review.md). Cross-link to Phase 4 coverage-audit spec: `docs/superpowers/specs/2026-05-27-meta-pattern-coverage-audit.md` (matrix snapshot + 12 → 11 gap delta after Phase 5; A4 retrospective wording + SUT-precondition habit added by PR #199). Phase 5 substituted the spec's top-1 (`burgers-timestep-convergence`) for `subchannel-friction-invariance` after empirical validation surfaced Lax-Friedrichs dissipation growth under timestep refinement. Cumulative delta: +89 facts across 6 phases (26/12/12/13/14/10/2); +6 cleanup facts; +1 MR (32 → 33); no new SUT; no Method MT / WPF / SemanticCatalogBoundaryTests regression. Final cloud baseline 1463 / 0 / 16. Status ledger row "T2 SystemMT visualization 4-end stack + T3 gap-fill chain" moved Pending → Controlled. | Already expired (chain complete); referenced from §3 for historical context |
| `docs/superpowers/plans/2026-05-27-ci-governance-cat-b-hardening-plan.md` | Completed (5-phase chain + writing-plan + Phase 6 chain-end review all merged) | **CI Cat B hardening plan** — closed across PR #207 (writing-plan `272c51d`), PR #208 (Phase 1 G6-G9 grep `748f972`), PR #209 (Phase 2 G10 multi-projection registry `72b2823`), PR #210 (Phase 3 spec-freshness cron `dcf64f7`), PR #211 (Phase 4 `MetBench_Analyzers/` METBENCH001 `89596be`), PR #212 (Phase 5 Stryker pilot `4398a47`), PR-LEDGER Phase 6 (this PR). Fresh-session chain-end review per CLAUDE.md §12.4 R2: [`docs/superpowers/specs/2026-05-27-ci-cat-b-hardening-chain-post-merge-review.md`](../specs/2026-05-27-ci-cat-b-hardening-chain-post-merge-review.md) — 0 Cat A findings, 1 Cat B (Phase 6 itself, closed by this PR). Coverage delta achieved: Cat A grep ~75% → ~90%; Cat B ~5% → ~50%. Status ledger row "CI Cat B hardening chain — Controlled" moved Pending → Controlled. | Already expired (chain complete); referenced from §3 for historical context |
| `docs/superpowers/plans/2026-05-28-t1-non-mr-crud-windows-vm-plan.md` | Completed (5-PR sequence merged) | **T1 non-MR CRUD Windows VM plan** — registers + ships the 4 remaining CLAUDE.md §2.2 T1 CRUD entities (SUT / Equation / SampleCase / ExecutionHistory) not surfaced through System-MT UI after MR CRUD shipped via `c7d9a6d` + PR #214. Closed across **PR-0** ([#219](https://github.com/meng004/MetBench-V2.1.4_2/pull/219), `501f585` docs gate), **PR-1 SUT** ([#221](https://github.com/meng004/MetBench-V2.1.4_2/pull/221), `adc7245`, `ISystemMtSutEditor` + `SystemMtSutCatalogPage`, 8 facts, VM verified at `04ae0ab`), **PR-2 Equation** ([#223](https://github.com/meng004/MetBench-V2.1.4_2/pull/223), `e2cd142`, 13 Built-in seeds + LiteDB user rows + reference-guard, 11 facts, VM verified at `640f2f4`), **PR-3 SampleCase** ([#225](https://github.com/meng004/MetBench-V2.1.4_2/pull/225), `c3f087e`, filesystem editor + manifest-reference-guard, 9 facts, VM verified at `28d1e67`), **PR-4 ExecutionHistory R/D** ([#224](https://github.com/meng004/MetBench-V2.1.4_2/pull/224), `19ee12e`, paged R + tx-aware D + cloud-side recorder legacy-mirror wiring fix, 18 facts incl. 4 `LegacyResultMirrorTests`, VM verified at `3000892`). VM-side: 38 screenshots total at `docs/superpowers/specs/2026-05-28-pr-{1,2,3,4}-vm-verification/`; 3 VM-side `MessageBox` namespace disambiguation fixes recommended for codification as analyzer / grep rule in follow-up. Cross-table delete join verified for PR-4 across 4 state transitions. Status ledger row "T1 non-MR CRUD chain (SUT / Equation / SampleCase / ExecutionHistory)" moved Pending → Controlled. Final cloud baseline 1509 / 0 / 12. | Already expired (chain complete); referenced from §3 for historical context |
| `docs/superpowers/plans/2026-05-28-t5-anomaly-workflow-closure-plan.md` | Completed (3-PR sequence merged) | **T5 anomaly workflow closure plan** — Explore-agent audit confirmed T5 mostly Controlled (AnomalyService / Classifier / Filter / CommonalityReport / ReplayContextBuilder / ReplayService / AnomalyListPage / ReplayResultPage 全部 Controlled); only 2 real gaps remained: F4 orphan sweeper (deferred from PR-4 #224) + OpenMOC × OpenMC ScaleModeratorSigmaA suspected defect (|Δk|=49%) not in Anomaly DB. Closed across **PR-0** ([#232](https://github.com/meng004/MetBench-V2.1.4_2/pull/232), `66373f6` plan + VM prompts in `docs/superpowers/vm-prompts/`), **PR-1 orphan sweeper** ([#233](https://github.com/meng004/MetBench-V2.1.4_2/pull/233), `2259b5e`, `IAnomalyOrphanSweeper` cross-DB join + UI button, 7 facts, VM verified at `9bb491a` with 3 injected orphans + idempotent re-sweep), **PR-2 cross-program defect import** ([#234](https://github.com/meng004/MetBench-V2.1.4_2/pull/234), `698181f`, `tools/import_cross_program_anomalies.py` + 7 Python facts + `tools/SeedCrossProgramAnomalies/` .NET console app, +1 sweeper fact post-amend, VM verified at `38e2026` after in-flight architectural fix). VM verification surfaced PR-1 ↔ PR-2 mutual destruction (sweeper would delete report-only anomalies) — closed via in-flight amend `e63afcb` adding `AnomalyOrphanSweeper.ReportOnlyCategories` exemption + seed tool using fresh `Guid.NewGuid()` per row to avoid Anomalies unique-index collision. DB-target documentation corrected (Anomaly → `MR.Litedb`, sweeper does cross-DB join). VM screenshots: 7 + 7 at `docs/superpowers/specs/2026-05-28-t5-pr-{1,2}-vm-verification/`. Final cloud baseline 1517 / 0 / 12. Status ledger row "T5 anomaly workflow closure" added Controlled. | Already expired (chain complete); referenced from §3 for historical context |
| `docs/superpowers/plans/2026-05-28-t5-anomaly-cleanup-scoped-plan.md` | Implemented (Route B chosen + delivered via T5 workflow closure plan) | F4 anomaly orphan cleanup scoped plan from PR-FUP-1 (#230) registered 3 candidate Routes; **Route B (independent sweeper service)** was chosen + implemented via `IAnomalyOrphanSweeper` in PR #233 of the T5 workflow closure chain. ReportOnlyCategories exemption added later in PR #234 amend to protect cross-program rows. This scoped plan is referenced for historical decision context; concrete implementation lives in the T5 workflow closure plan + ledger row. | Already expired (Route B implemented); referenced from §3 for historical context |
| `docs/superpowers/plans/2026-05-28-p1-catalog-derived-counts-plan.md` | Completed — PR #215 (`a40f1f1`) merged 2026-05-28 | v2 charter P1：消除 catalog pinned-count 漂移 Cat B；引入 `expected-catalog-counts.txt` whitelist + `ExpectedCatalogCountsWhitelist` 辅助类 + `Catalog_MR_id_set_equals_governance_whitelist` 强化 fact；退役 G7 advisory grep。 | Expires on PR merge |
| `docs/superpowers/plans/2026-05-28-p2-spec-freshness-orphan-spec-plan.md` | Completed — PR #217 (`1cbcd86`) merged 2026-05-28 | v2 charter P2：orphan-spec 守卫扩展，落点模块 D + R3 | Expires on PR merge |
| `docs/superpowers/plans/2026-05-28-p3-metbench002-field-flow-plan.md` | Completed — PR #226 (`8873110`) merged 2026-05-28 | v2 charter P3：METBENCH002 通用 field-flow tracer Roslyn analyzer（threshold ≥ 5 跨文件 use site，扩 `RecursivePatternSyntax`），落点模块 B；含 v2 charter §4 retrospective 修订（R3 合规）。 | Expires on PR merge |
| `docs/superpowers/plans/2026-05-28-p7-g11-decision-record-plan.md` | Completed — PR #218 (`f6e60bf`) merged 2026-05-28 | v2 charter P7：G11 decision-record-or-die grep + `docs/superpowers/templates/decision-record-template.md`；落点模块 B + 元规则 Cat B 预防 | Expires on PR merge |
| `docs/superpowers/plans/2026-05-28-p6-code-review-advisory-plan.md` | Completed — PR #220 (`f2a9eaf`) merged 2026-05-28 | v2 charter P6：模块 F 作者侧 `/code-review` advisory 操作化（CLAUDE.md §12.2 加路径族建议 + PR Gate Checklist Review 节加 sub-check），docs-only | Expires on PR merge |
| `docs/superpowers/plans/2026-05-28-p5-ultra-chainend-automation-plan.md` | Completed — PR #222 (`c08d7df`) merged 2026-05-28 | v2 charter P5：`/code-review ultra` 链尾自动喂入 + checklist Step 0/N 接入，落点模块 E + R2 | Expires on PR merge |
| `docs/superpowers/plans/2026-05-29-debt5-anomaly-status-enum-plan.md` (+ companion VM plans `2026-05-29-debt5-wpf-vm-plan.md` / `2026-05-29-debt5-vm-screenshot-plan.md`) | Completed — follow-up debt batch merged via PR #236 (`827394b`) | **Follow-up debt batch #2–#5** — debt #2 governance Check-5 grep adapted to the PR-gate-checklist template; debt #3 inventory two-layer disambiguation (`44 MR + 4 Property` migration denominator vs `33 MR / 16 SUT / 13 eq` runtime catalog, authoritative `.github/governance/expected-catalog-counts.txt`); debt #4 removed the stale `e839214` perishable baseline snapshot from `CLAUDE.md`; debt #5 `Anomaly.Status` string → `AnomalyStatus` enum + code-enforced transition validation (`InvalidAnomalyStatusTransitionException`) + LiteDB int serialization + string→int migration + WPF wiring. WPF verified on a Windows host (compile + boot + UIA interactive; 7 screenshots at `docs/superpowers/specs/2026-05-29-debt5-vm-verification/`). Debt #1 (Stryker P4 delta gate) stays deferred. Cloud baseline 1556 / 0 / 12. Status ledger §6 row "Follow-up debt batch #2–#5" added Controlled. | Already expired (merged); referenced for historical context |
| `docs/superpowers/specs/2026-05-28-v2-charter-rollout-chain-post-merge-review.md` | Active — chain-end review doc; chain Controlled marker | v2 charter rollout chain (7 PRs: charter + P1+P2+P3+P5+P6+P7; P4 deferred) post-merge holistic review per §12.4 R2. Fresh-context Explore subagent; dogfooded P5 chain-end ultra invocation helper for Step 0. 0 blockers / 1 should-fix (F1: P3 reflection facts via Assembly.LoadFrom, P2 priority, non-blocking) / 0 nits. Cat A 90% → 95%; Cat B 50% → 75%. | Expires only when superseded by next chain-end review |
| **PR-Bol-3 / Bol-Alg-02 P3 reflection facts follow-up** (Anticipated) | Anticipated — F1 from v2 charter rollout chain-end review (`2026-05-28-v2-charter-rollout-chain-post-merge-review.md`) | Re-introduce 3 reflection facts for `METBENCH002` registration lock (DiagnosticId / DefaultSeverity / Category) using `Assembly.LoadFrom(<path>/MetBench_Analyzers/bin/.../netstandard2.0/MetBench_Analyzers.dll)`. Required because the original csproj ProjectReference approach failed under `IncludeBuildOutput=false` analyzer packaging (PR #226 fix commit `4688643`). P2 priority; non-blocking. | Expires on follow-up PR merge |
| (no active scoped plan beyond the above) | — | Verification-semantics convergence (PR-A → PR-D), ExecutionEvidence v2 (PR-A0 + PR-C0 + live wiring + evidence-aware reporting via PR #123 / #126 / #128), dormant legacy code mapping (PR #124), anomaly typed annotation + correctness (PR #130 / #132), T3 representative-PDE-class expansion (PR #134 / #136 / #138 / #140), T3C-IVP, T3C-BVP, PR-Bol-1, the T2/T3 visualization-and-gap-fill 6-phase plan (PR #184–#192), and the v2 governance charter rollout 7-PR chain (PR #215–#226, see above row) are merged. P4 (Stryker delta gate) deferred 3 weeks pending mutation-testing weekly cron baseline. Remaining follow-ups must enter through explicit scoped plans registered here. | A new scoped plan must be registered here before any new cross-cutting System MT work begins. |

Any new coding task must be derived from the active master plan or from a scoped successor plan registered here.

---

## 2. 当前活跃设计文档

| Document | Status | Scope | Expiry condition |
|---|---|---|---|
| `docs/status/current.md` | Active | Single current-status ledger for monitoring, handoff, baseline, active plan, and open risks | Expires only when replaced by a newer status ledger path |
| `docs/superpowers/specs/2026-05-25-metbench-project-control-rules.md` | Active | Control charter: source hierarchy, gates, review, status refresh | Expires only by explicit replacement PR |
| `docs/superpowers/specs/2026-05-25-metbench-macro-assessment-and-risk-audit.md` | Active | Macro assessment and risk audit for the Stage 8 to next-stage transition | Expires when the next macro assessment supersedes it |
| `docs/superpowers/specs/2026-05-25-mr-verification-v1.2-codex-ready.md` | Active reference | v1.2 typed semantic model and verifier design already implemented through the current roadmap | Expires when a v1.3 verification design supersedes it |
| `docs/superpowers/specs/2026-05-25-systemmt-architecture-review-post-evidence-v2.md` | Active | Post-PR-D / post-PR-C0 System MT dependency-boundary audit; baseline reference for future architecture-impact reviews. PR #123 / #124 / #126 did not invalidate it. | Refresh required (not necessarily expiring) when the next System MT cross-cutting change lands. |
| `docs/superpowers/templates/pr-gate-checklist.md` | Active | Required PR gate checklist for scope, facts, tests, Windows classification, review, merge, and dual AI advisory review | Expires only by explicit replacement PR |
| `docs/superpowers/templates/decision-record-template.md` | Active | New-module decision record template referenced by G11 (v2 charter §6 P7); copy to `docs/superpowers/specs/YYYY-MM-DD-<topic>-decision.md` whenever a PR adds a file under the four G11 watched path families | Expires only by explicit replacement PR |
| `docs/superpowers/specs/2026-05-26-pr-soft-review-via-claude-code-action.md` | Active design (implemented as dual AI advisory review) | Advisory LLM-based PR review layer running in GH Actions on `pull_request` events. `openai/codex-action@v1` performs Codex Governance Review (scope / status / traceability / Windows classification / boundary drift); `anthropics/claude-code-action@v1` performs Claude Semantic Review (C# logic / runtime boundaries / exception paths / test adequacy). | Expires when either action is deprecated, when GitHub-hosted AI review is replaced, or when the project adopts a different review pipeline |
| `docs/superpowers/specs/2026-05-28-code-governance-v2-charter.md` | Active | v2 治理章程（六模块 + 元规则集），P1-P7 真相层 | Replaced by future v3 charter |
| `docs/superpowers/specs/2026-06-03-systemmt-async-execution-polling-design.md` | Proposed design | Additive System MT async execution + polling status design for OpenMC-like long-running SUTs, Docker/remote/HPC backends, and durable MR-job status. This is not an implementation plan and does not change current runtime status. | Expires when replaced by an approved async execution implementation plan or a newer design |

### 条件性活跃

| Document | Status | Scope | Expiry condition |
|---|---|---|---|
| `docs/superpowers/specs/2026-05-24-systemmt-catalog-convergence-design.md` | Conditional reference | Catalog convergence design history and evidence-model context | Expires when Evidence v2 design supersedes the relevant sections |
| `docs/superpowers/plans/2026-05-24-systemmt-catalog-convergence-plan.md` | Conditional reference | Catalog convergence implementation history | Expires when the active transition plan no longer references it |

---

## 3. 已完成、可参考但不再活跃的计划

### v1.2 执行链

- `docs/superpowers/plans/2026-05-25-mr-verification-v12-master-roadmap.md`
- `docs/superpowers/plans/2026-05-25-mr-verification-v12-pr1-typed-model-validators.md`
- `docs/superpowers/plans/2026-05-25-mr-verification-v12-pr2-runtime-scalar-kernels.md`
- `docs/superpowers/plans/2026-05-25-mr-verification-v12-pr3-applicability-status.md`
- `docs/superpowers/plans/2026-05-25-mr-verification-v12-pr4-reference-convergence.md`
- `docs/superpowers/plans/2026-05-25-mr-verification-v12-pr5-sequence-shapes-subadditive.md`
- `docs/superpowers/plans/2026-05-25-mr-verification-v12-pr6-field-derived-invariant.md`
- `docs/superpowers/plans/2026-05-25-mr-verification-v12-pr7-statistical-cross-method.md`
- `docs/superpowers/plans/2026-05-25-mr-verification-v12-pr8-property-checker.md`
- `docs/superpowers/plans/2026-05-25-mr-verification-v12-pr9-exponential-growth.md`
- `docs/superpowers/plans/2026-05-25-mr-verification-v12-pr10-migration-fixtures-coverage.md`

**原因**:

- 它们已经对应完成并入主线
- 仍适合用作执行样板
- 但不再代表“下一步做什么”

### T3C 外部求解器试点（已合并）

- `docs/superpowers/plans/2026-05-26-t3c-scipy-ivp-lotka-volterra-plan.md`（T3C-IVP，已合并）
- `docs/superpowers/plans/2026-05-26-t3c-scipy-bvp-poisson-1d-plan.md`（T3C-BVP，已合并）

**原因**:

- §5.2 decision record 选定的 SciPy `solve_ivp` Lotka-Volterra SUT 已通过 T3C-IVP 合入主线（inventory 13→14 SUT / 25→27 MR）。
- §5.3 decision record 选定的 SciPy `solve_bvp` Poisson 1D SUT 已通过 T3C-BVP 合入主线（real-physics inventory 14→15 SUT / 27→29 MR）。
- PR-A（#162）追加了合成非物理 SUT `_test_csv` 以端到端回归 `metbench_io` helper —— catalog 总计为 **16 SUT / 13 equations / 30 MRs**；真实物理 inventory 维持 **15 SUT / 12 equations / 29 MRs** 不变。
- 仍适合用作未来外部求解器候选的实施样板。
- 但不再代表”下一步做什么”。

### T1 manifest-driven runtime environments（已合并）

- `docs/superpowers/plans/2026-05-26-t1-manifest-driven-runtime-environments-plan.md`（PR-1，已合并）

**原因**:

- `LauncherOptions.RuntimePythons` + `ResolvePythonExecutable` 已替换原先逐 venv 字段的硬编码槽位；`ManifestMrCatalogProvider` 改为单点 resolver 调用。
- 内置 `system`/`openmoc`/`openmc`/`scipy` 行为保留；未知非 system 键在 resolver 处 fail-closed 并附带可定位的诊断信息。
- 新增 runtime family（FEniCS/FiPy/torch-surrogate 等）从此为纯配置变更，无需改 `LauncherOptions` 字段或 `PythonExecutableKinds.All`。
- 状态账本”T1 multi-env management”行已由 Open 改为 Controlled。

### Typed noise-aware scalar predicate（已合并）

- `docs/superpowers/plans/2026-05-26-typed-noise-aware-scalar-predicate-plan.md`（PR-N1，已合并）

**原因**:

- `MetBench_BLL.Core/SystemMT/Catalog/Typed/Specs/PredicateSpec.cs` 加 `NoiseAwareBinaryComparisonPredicate` 记录；`Catalog/Typed/Runtime/NoiseAwareScalarToleranceEvaluator.cs` 计算 `NoiseMultiplier · √(σ_source² + σ_followup²)`；`Catalog/Typed/Runtime/NoiseAwareBinaryComparisonKernel.cs` 评估带噪方向不等式（Greater / Less；Equal 显式 `VerifyStatus.InvalidSpec`，因为带噪等式是不同二端测试）；`Catalog/Typed/Validation/NoiseAwareBinaryComparisonPredicateValidator.cs` 校验四个 metric 与 NoiseMultiplier；在 `ValidationRegistry` / `MrSpecValidator` / `PredicateDispatcher`（dispatch + `HasObservable` 两处）登记。
- `LegacyAssertionPredicateMapper.MapNoiseAwareScalar(...)` overload 把 `less-noise-aware` / `greater-noise-aware` 映射到新 predicate；bare `MapScalar` 仍 fail-closed 但 throw 信息指向新 overload。
- 66 新 facts（evaluator 16 / kernel 33 / validator 15 + 6 mapper + 1 dispatcher）。
- 状态账本 "Noise-aware typed scalar predicate" 行已由 Open 改为 Controlled。
- "Legacy assertion code mapping" 行从 "4 mapped / 2 intentionally fail-closed" 改为 "6 mapped (4 deterministic + 2 noise-aware via overload)"。
- 下一阶段 PR-N2 可用 `MapNoiseAwareScalar` 路径，但 PR-N2 (Bol-Alg-02) 实际用 `variance-ratio`，并不依赖 PR-N1 type-wise；PR-N1 解锁的是未来 MC m_mono direction MR。

### T1 non-JSON I/O adapter（已合并）

- `docs/superpowers/plans/2026-05-26-t1-non-json-io-adapter-plan.md`（PR-A，已合并）

**原因**:

- `SUT/_shared/metbench_io/` Python helper（纯 stdlib，支持 `csv-row` 与 `plain-text` wire format）已落地为 MetBench non-JSON SUT I/O 的唯一集成点。Launcher / pipeline / `ManifestMrCatalogProvider` / 参数映射 `IFieldPathResolver` 栈不感知 wire format。
- `SUT/_test_csv/` 合成测试 SUT（前缀下划线标记非物理性质）+ `csv-roundtrip-identity` MR + `_test_csv` 合成 EquationMetadata 端到端证明 helper 经未改动 launcher 跑通。type-coercion 在 SUT parser 层完成（与 JSON SUT 隐式通过 JSON 原生数字类型同一边界），helper 本身严格保留字符串。
- 11 个 `MetBenchIoHelperTests` facts 覆盖 CSV round-trip / 头部验证 / 空 params / 引号 / plain-text 字节同一 / 未知格式 fail-closed / JSON 直通；`LauncherEndToEndTestCsvTests` 1 个 fact 覆盖 launcher 全链路。
- Pinned descriptor 计数 29 → 30 / 15 → 16 across 6 个文件。
- 状态账本 "T1 non-JSON I/O adapter" 行已由 Open 改为 Controlled。
- 至此 `2026-05-26-t1-cross-method-and-io-sequenced-execution-plan.md` orchestration 中的 PR-0 / PR-B / PR-A 已全部合并。

### T1 same-equation cross-method differential runner（已合并）

- `docs/superpowers/plans/2026-05-26-t1-cross-method-differential-runner-plan.md`（PR-B，已合并）

**原因**:

- `MetBench_BLL.Core/SystemMT/Differential/` 命名空间已落地：`IDifferentialTestRunner` + 6 个支撑 type，作为 T1 §2.1 element 3「同源异构差分测试」的唯一 cloud-side API。
- 三种 agreement criteria（BothPassed / DirectionConcordant / FollowUpRatioWithinTolerance）全部确定性、total — 28 个 facts 覆盖 NaN / ±∞ / zero-source / negative-tolerance / metric-name-mismatch / launcher-throws / null-request / null-launcher / same-MR-both-sides / overrides-forwarded-per-side。
- 不复用 typed catalog 的 `CrossMethodComparisonKernel`（intra-MR 两 role 形态不同），但通过 XML doc 明确两者区别。
- 现有 BDD `Features/CrossProgramNeutronTransportMrs.feature` + `Steps/CrossProgramSteps.cs` 完全未动 — 这是 orthogonal cleanup 而非本 PR 范围。
- 状态账本 "T1 same-equation cross-method differential" 行已由 Open 改为 Controlled。

### T4-to-T0 MR discovery binder（已合并）

- `docs/superpowers/plans/2026-05-26-t4-to-t0-mr-discovery-binder-plan.md`（PR-2，已合并）

**原因**:

- `MetBench_BLL.Core/SystemMT/Catalog/Binding/DiscoveredMrCatalogBinder` 已落地为 T4 → T0 唯一被授权的桥接面：纯函数、无 IO、deterministic；输入 `DiscoveredMrBindingDraft`（16 字段含 transform steps / discovery method / run id / confidence），输出 manifest-compatible `MrBindingDefinition` + `DiscoveredMrBindingProvenance`，或 field-level errors。
- 校验严格 fail-closed：required-string blank、`AssertionTypeCode` 未识别 / noise-aware fail-closed、`Confidence ∉ [0,1]` / NaN / ±∞、`DefaultParameters` blank key、`SampleCaseRelativePath` 有 `..` 段或为根路径、`TimeoutSeconds ≤ 0`、空 `TransformSteps`、`MrBindingDefinition.Validate()` 自身违反等都在 errors 列表中一次性返回。
- 不读写任何 `SUT/<sut>/catalog.json`；测试用 sentinel 目录证明 5 次 binding 后磁盘零变更。
- 状态账本”T4-to-T0 binder”行已由 Planned/Queued 改为 Controlled。
- 至此 PR-0 / PR-1 / PR-2 orchestration plan 中的三个 PR 已全部合并；下一步需要新的 scoped plan 才能继续 cloud-side 工作。

### 阶段性修复计划

- `docs/superpowers/plans/2026-05-25-v12-doc-alignment-plan.md`
- `docs/superpowers/plans/2026-05-24-metbench-doc-runtime-alignment-plan.md`
  - 对应 design spec：`docs/superpowers/specs/2026-05-24-metbench-doc-runtime-alignment-design.md`

**原因**:

- 这些计划服务于一次性状态修正
- 对应问题已被 PR 收口

### 验证语义收敛链（已合并）

- `docs/superpowers/plans/2026-05-25-verification-semantics-convergence-implementation-plan.md`
- `docs/superpowers/specs/2026-05-25-verification-semantics-convergence-design.md`

**原因**:

- 全部四个 PR（PR-A 设计 lock #114 / PR-B 命名迁移 #115 / PR-C 运行时收敛 #118 / PR-D 守卫与清理 #119）已合并 `origin/main`。
- 设计目标“Method MT 隔离 + System MT 全部走 Typed Semantic Catalog + W1 IMrAssertion 路径下线”已闭环。
- `SemanticCatalogBoundaryTests` 与 `SemanticCatalogNamingBoundaryTests` 接管事后回潮守卫，参见 `docs/status/current.md` §6 Verification semantics convergence 行。
- 设计与计划保留为历史参考；新工作不得从这两个文件衍生执行排程。

### ExecutionEvidence v2 全生命周期（已合并）

- `docs/superpowers/plans/2026-05-25-executionevidence-v2-implementation-plan.md`
- `docs/superpowers/specs/2026-05-25-executionevidence-v2-design.md`

**原因**:

- PR-A0 设计 lock（#116）、PR-C0 schema/recorder（#121）、live wiring（#123）与 evidence-aware reporting（#126）均已合并 `origin/main`。
- 设计目标“nullable TypedVerification block + 三个支撑 POCO + 无生产端 schema break + live pipeline → recorder 写入 + 渲染器投影”已端到端闭环（数据→持久化→recorder→渲染器）。
- `MrRunResultShapeLockTests` 接管 launcher facade 防漂移守卫；`TypedVerificationEvidenceRoundtripTests` 接管 legacy / v2 双向兼容守卫；`Live_pipeline_outcome_carries_typed_triple_into_evidence_without_explicit_typed_args` 接管 live pipeline 写入守卫；`Render_without_evidence_dictionary_matches_legacy_overload_byte_identical` 接管渲染器无证据路径字节级守卫。
- 设计与实施计划均退役为历史参考；后续若需扩展 evidence 粒度或重大 schema 变更须另立 design + plan，再注册到本索引 §1 / §2。

### 验证语义收敛续作 + evidence-aware reporting + anomaly annotation/correctness + T3 expansion（已合并）

- 无独立文件（PR #123 / #124 / #126 / #128 / #130 / #132 / #134 / #136 / #138 / #140 直接在主线推进）

**原因**:

- PR #123（live typed verification wiring）、PR #124（dormant legacy code mapping）、PR #126（evidence-aware HTML report rendering）、PR #128（evidence-aware execution markdown）、PR #130（anomaly typed verification annotation）已合并 `origin/main`。
- PR #123 把 `SystemMtPipeline.ExecuteAsync` 产出的类型化三元组通过 `PipelineOutcome` 带回 recorder，live `ExecutionEvidence.TypedVerification` 不再 null。
- PR #124 把四个 dormant 旧 assertion code 映射到现有 typed predicates；剩余 `less-noise-aware` / `greater-noise-aware` 保留 fail-closed 并由 `Noise_aware_scalar_codes_fail_closed_with_documented_reason` 守卫。
- PR #126 给 `HtmlSystemMtResultReportRenderer` / `ISystemMtResultReportRenderer` 加 evidence-aware overload，渲染 `TypedVerification`（Spec / Predicate / Status / Diagnostic / Skip / PropertyPredicates）。Legacy 路径字节级不变。
- PR #128 给 `SystemMtReportService` 加 optional `IExecutionEvidenceRepository?` ctor 参数，markdown 执行报告同样投影 `TypedVerification`。Legacy 5-arg ctor 与 single-arg `BuildExecutionMarkdown` 仍存在；既有测试不需修改。
- PR #130 给 `IAnomalyService.RecordAnomalyAsync` 加 optional `string? typedVerificationSummary` 参数，`SystemMtLauncher.RecordAnomalyIfFailedAsync` 把 `PipelineOutcome.TypedVerification` 投影成 `typed=<Status> metric=<Metric> predicate=<id> (<Kind>) residual=… tolerance=…` 一行摘要，写入 `Anomaly.Notes` 与 `anomaly.created` 审计 `detailsJson`。无 typed verification 时 byte-identical to PR-129。
- PR #132 修正了一个 bug：`Status=SkippedMissingObservable / SkippedNotApplicable / InvalidSpec` 的 typed verification 之前会因 `SystemMtAssertionResultV2.Passed=false` 的 fallback 而被 launcher 错误归类为 Anomaly。`RecordAnomalyIfFailedAsync` 现在对这三种非-Failed 状态早返，避免误报。`Status=Failed` 与遗留非-typed 失败仍生成 Anomaly。
- PR #134 启动 T3 扩展，加首个椭圆 PDE SUT（`SUT/poisson_1d/`，pure-stdlib Thomas 三对角求解器，无需 venv，云端 CI 可跑），加 `poisson` `EquationMetadata` 和两条 MR（`poisson-source-superposition` m_mono 线性叠加 + `poisson-mesh-richardson` m_conv）。同时**通过演示验证 T1**：新 SUT 接入只改 SUT 文件 + catalog 接线 + 计数测试，未触 Pipeline / Persistence / Reporting / Launcher 类体 / DAL —— 证明 T1 的 CLI runner / Python adapter / catalog provider / launcher facade 已稳定可附加。Inventory 改为 10 SUT / 9 方程 / 19 MR；`EquationKind` enum 不变（poisson → `Other`；APPEND-ONLY 保持，5 反应堆方程仍为 canonical）。
- PR #136 续 T3 扩展，加首个一阶线性双曲 PDE SUT（`SUT/advection_1d/`，一阶迎风 FD + 周期边界 + 内部 Courant=0.5 dt 选择，pure-stdlib），加 `advection` `EquationMetadata` 和两条 MR（`advection-amplitude-linearity` m_mono + `advection-mesh-conservation` m_inv via 守恒迎风格式）。再次仅改 SUT 文件 + catalog 接线 + 计数测试，二次确认 T1 by-demonstration 验证。Inventory 改为 11 SUT / 10 方程 / 21 MR；APPEND-ONLY 保持（advection → `Other`）。
- PR #138 续 T3 扩展，加首个二阶线性双曲 PDE SUT（`SUT/wave_1d/`，规范 `u_tt = c²·u_xx`，二阶 leapfrog FD + Dirichlet 边界 + 零初始速度 + 内部 Courant=0.5 dt 选择，pure-stdlib），加 `wave` `EquationMetadata` 和两条 MR（`wave-amplitude-linearity` m_mono via 线性 leapfrog + `wave-mesh-energy-convergence` m_conv via L² 不变量 `0.5·∫u² dx`）。三次确认 T1 by-demonstration 验证。Inventory 改为 12 SUT / 11 方程 / 23 MR；APPEND-ONLY 保持（wave → `Other`）。
- PR #140 续 T3 扩展，加首个非线性 PDE SUT（`SUT/burgers_1d/`，无粘 Burgers `u_t + (u²/2)_x = 0`，守恒 Lax-Friedrichs 通量差分 + 周期边界 + 内部 Courant=0.5 dt 选择，pure-stdlib），加 `burgers` `EquationMetadata` 和两条 MR（`burgers-amplitude-peak-monotone` m_mono via 非线性幅值单调性（LxF 数值耗散使 peak_ratio < 2 但仍严格大）+ `burgers-mesh-conservation` m_inv via 守恒通量差分严格保 ∫u dx）。四次确认 T1 runner/adapter/catalog additivity by-demonstration 验证，且跨四类 PDE（椭圆 / 一阶线性双曲 / 二阶线性双曲 / 非线性双曲）；这不等同于 T1 多 env 管理或 UI MR CRUD 完成。Inventory 改为 13 SUT / 12 方程 / 25 MR；APPEND-ONLY 保持（burgers → `Other`）。T3 代表性 PDE-class 覆盖功能性完成。
- 后续触发条件（noise-aware 旧码被新增 catalog binding 采用 → 加噪声感知 typed predicate）记录在 `docs/status/current.md` §7。

---

## 4. 历史计划

以下计划默认视为历史记录，不可直接用于当前开发排程：

- `docs/superpowers/plans/2026-05-21-next-stage-development-plan.md`
- `docs/superpowers/plans/2026-05-18-stage8-expanded-mr-library-plan.md`
- `docs/superpowers/plans/2026-05-18-meta-prompt-mr-discovery-plan.md`
- 以及其他早于当前主线事实、且未被本索引列为活跃/条件性活跃的旧计划

**原因**:

- 它们形成于旧基线、旧阶段或旧分母之上
- 仍有参考价值，但不能直接驱动当前执行

### 历史 spec（pre-index 时期的 retrospective / review 类设计文档）

以下 spec 文档为 active-plan-index 创立（2026-05-25）前后的 retrospective / review 类设计稿，
已固化为只读历史参考；不再对应任何 active plan，但保留索引可追溯：

- `docs/superpowers/specs/2026-05-07-system-level-mt-bdd-design.md`
- `docs/superpowers/specs/2026-05-25-mr-verification-retrospective-review.md`
- `docs/superpowers/specs/2026-05-25-mr-verification-two-layer-review-policy.md`
- `docs/superpowers/specs/2026-05-25-v12-pwr-migration-map.md`

**原因**:

- pre-index 时期形成的 retrospective / review / migration-map 类 spec，对应工作已收口或纯粹作为历史快照
- 不会再有新的 active plan 衍生自这几份 spec
- 仍可作为相关主题查询的只读参考

---

## 5. 使用规则

1. 如果一份计划未出现在本索引的 Active 或 条件性活跃 中，则默认视为历史计划。
2. 监控输出不得直接从历史计划中提取“当前状态”。
3. 新阶段开启时，必须先更新本索引，再开启实现。
4. 如果活跃计划与 `docs/status/current.md` 冲突，以状态账本为准，并同步修正索引和投影文档。
5. 如果投影文档之间冲突，先回到状态账本裁决，再修正对应投影文档。
