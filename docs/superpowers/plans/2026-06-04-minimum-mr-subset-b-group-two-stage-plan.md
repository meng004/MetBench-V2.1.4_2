# Minimum-MR-SubSet B-Group Two-Stage Import/Runtime Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Import the second `minimum-mr-subset` batch (P8 Schrodinger and P3 Lorenz) in two controlled stages: import/export staging first, then live runtime promotion with current async job pipeline validation.

**Architecture:** Stage 1 extends the existing PUT import/export staging model without changing live runtime inventory. Stage 2 adds explicitly compatible live runtime slices and verifies both `ISystemMtLauncher` and `SystemMtJobService -> SystemMtJobWorker -> SystemMtAsyncPipeline -> ISystemMtLauncher` execution. Imported research evidence remains separate from MetBench execution evidence.

**Tech Stack:** .NET 8, xUnit, System.Text.Json, existing System-MT launcher/catalog assets, existing `MetBench_BLL.SystemMT.Jobs` async pipeline, Python SUT assets.

---

## Evidence Preconditions

- MetBench base: current `origin/main` at plan creation was `6293455df3fcbe7692032d46ea05b97fdfb6035f`.
- External source tree: `/private/tmp/minimum-mr-subset`.
- External source commit: `b931b5f74d0f3f3cb704cd6fedbcb4f523cccd7f`.
- P3 source: `experiments/puts/p3_lorenz.py`.
- P8 source: `experiments/puts/p8_schrodinger.py`.
- Shared smoke contract: `tests/puts/test_smoke.py`.
- Local limitation observed: P3/P8 smoke attempts fail with `ModuleNotFoundError: No module named 'numpy'`; P3 also imports SciPy.
- Data limitation observed: only `data/raw/p1_heat/` has `mrs.json` and `detection_matrix.csv`; no P3/P8 raw detection-matrix directory was observed.

## Task 0: Branch And Scope Gate

**Preconditions**

- Work starts from clean `main...origin/main`.
- Read `docs/status/current.md`.
- Read `docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md`.
- Read `docs/superpowers/specs/2026-06-04-minimum-mr-subset-b-group-two-stage-design.md`.

**Core steps**

- [ ] Run `rtk git status --short --branch`.
- [ ] Run `rtk git rev-parse origin/main`.
- [ ] Confirm the implementation branch is not `main`.
- [ ] Confirm scope is B group only: P8 and P3.
- [ ] Confirm Stage 1 and Stage 2 will be separate commits or PRs.
- [ ] Confirm no WPF or `MetBench_Client/` work is included unless a separate Windows plan is written.

**Acceptance standards**

- Evidence note records branch, base commit, clean/dirty state, and external commit.
- If the worktree is dirty, unrelated files are not staged.

## Task 1: Stage 1 RED - B-Group Fixture Validation Tests

**Preconditions**

- Task 0 is complete.
- Existing A-group tests are readable at `MetBench_SystemMT.Tests/SystemMT/ImportExport/AGroupPutImportExportTests.cs`.

**Core steps**

- [ ] Create `MetBench_SystemMT.Tests/SystemMT/ImportExport/BGroupPutImportExportTests.cs`.
- [ ] Add failing facts that require:
  - `BGroupPutFixtures.Create("P3")` validates.
  - `BGroupPutFixtures.Create("P8")` validates.
  - P3 observables equal `t`, `trajectory`, `centroid`.
  - P8 observables equal `x`, `probability_density`, `norm`.
  - P3/P8 detections are `DetectionResult.Inconclusive` when no real detection matrix is present.
  - P3/P8 mutation assets are `MutationRepresentationKind.OperatorClassOnly`.
  - P3/P8 compatibility defaults to `ImportedOnly`.
- [ ] Run `rtk dotnet test MetBench_SystemMT.Tests --no-restore --filter "FullyQualifiedName~BGroupPutImportExportTests"`.

**Acceptance standards**

- Tests fail because `BGroupPutFixtures` does not exist yet.
- Failure is not due to syntax errors in the test file.

## Task 2: Stage 1 GREEN - B-Group Import Fixtures

**Preconditions**

- Task 1 has the expected RED result.
- Existing model records in `MetBench_BLL.Core/SystemMT/ImportExport/Put/PutImportModels.cs` are unchanged unless tests require an additive field.

**Core steps**

- [ ] Create `MetBench_BLL.Core/SystemMT/ImportExport/Put/BGroupPutFixtures.cs`.
- [ ] Add `Create("P3")` and `Create("P8")`.
- [ ] P3 fixture:
  - `SutId = "P3"`.
  - `EquationFamily = "chaotic ODE / RK"`.
  - `ProgramKind = ProgramKind.NumericalSolver`.
  - `Adapter.SourcePath = "experiments/puts/p3_lorenz.py"`.
  - observables: `t` (`Series`), `trajectory` (`Vector`), `centroid` (`Vector`).
  - MR asset: `p3-trajectory-sensitivity`.
  - mutation: operator class only, such as `initial-condition-perturbation`.
  - detection: `Inconclusive`, `ImportedResearchEvidence`, note says no P3 detection matrix observed.
- [ ] P8 fixture:
  - `SutId = "P8"`.
  - `EquationFamily = "complex PDE / spectral"`.
  - `ProgramKind = ProgramKind.NumericalSolver`.
  - `Adapter.SourcePath = "experiments/puts/p8_schrodinger.py"`.
  - observables: `x` (`Vector`), `probability_density` (`Vector`), `norm` (`Series`).
  - MR asset: `p8-norm-conservation`.
  - mutation: operator class only, such as `time-step-perturbation`.
  - detection: `Inconclusive`, `ImportedResearchEvidence`, note says no P8 detection matrix observed.
- [ ] Use the same provenance commit and repository URL as A group.
- [ ] Run the focused B-group import/export tests.

**Acceptance standards**

- B-group fixture tests pass.
- A-group fixture tests still pass.
- No live `SUT/` or catalog files change in this task.

## Task 3: Stage 1 Round Trip And README

**Preconditions**

- Task 2 tests pass.

**Core steps**

- [ ] Extend B-group tests to export/import P3 and P8 using `SutImportPackageExporter`.
- [ ] Assert provenance commit and observable names survive round trip.
- [ ] Assert `CompatibilityProfileBuilder.Build(unit)` returns `ImportedOnly`.
- [ ] Update `MetBench_BLL.Core/SystemMT/ImportExport/Put/README.md`:
  - included staging SUTs: A group plus B group.
  - live runtime status: A group promoted; B group import-only until Stage 2.
  - local external smoke limitation: missing NumPy/SciPy.
  - no P3/P8 detection matrix observed.
- [ ] Run focused import/export tests.
- [ ] Run `rtk git diff --check`.

**Acceptance standards**

- P3/P8 round trips pass.
- README does not claim external P3/P8 execution.
- Runtime catalog counts do not change.

## Task 4: Stage 1 Commit And PR Gate

**Preconditions**

- Tasks 1-3 pass.

**Core steps**

- [ ] Run `rtk dotnet test MetBench_SystemMT.Tests --no-restore --filter "FullyQualifiedName~AGroupPutImportExportTests|FullyQualifiedName~BGroupPutImportExportTests"`.
- [ ] Run `rtk git diff --check`.
- [ ] Inspect `rtk git diff --name-only` and confirm no live runtime files changed.
- [ ] Commit Stage 1 as `feat(systemmt): stage minimum MR subset B group imports`.

**Acceptance standards**

- Stage 1 commit contains only import/export staging, tests, and docs.
- No `.github/governance/expected-catalog-counts.txt` change.
- No `SUT/minimum_mr_subset_p3*` or `SUT/minimum_mr_subset_p8*` live runtime files yet.

## Task 5: Stage 2 RED - Live Launcher Tests

**Preconditions**

- Stage 1 commit exists.
- Read A-group live tests:
  - `MetBench_SystemMT.Tests/SystemMT/Launcher/LauncherEndToEndMinimumMrSubsetP4Tests.cs`
  - `MetBench_SystemMT.Tests/SystemMT/Launcher/LauncherEndToEndMinimumMrSubsetP5Tests.cs`
  - `MetBench_SystemMT.Tests/SystemMT/Launcher/LauncherEndToEndMinimumMrSubsetP9SurrogateTests.cs`

**Core steps**

- [ ] Add launcher tests for:
  - `p8-norm-conservation` appears in `ListAvailableAsync()`.
  - `p3-trajectory-sensitivity` appears in `ListAvailableAsync()`.
  - Each MR runs through `ISystemMtLauncher.RunAsync`.
  - Each result has non-empty source/follow-up metrics required by its assertion.
- [ ] Run focused launcher tests.

**Acceptance standards**

- Tests fail because live catalog assets do not exist yet.
- Failure identifies missing MR IDs, not unrelated infrastructure errors.

## Task 6: Stage 2 GREEN - Live Runtime Assets And Catalog Rows

**Preconditions**

- Task 5 has the expected RED result.
- Stage 2 may change runtime inventory and count whitelist.

**Core steps**

- [ ] Add `SUT/minimum_mr_subset_p8/` with:
  - Python runner.
  - input parser.
  - output parser.
  - `sample/standard.json`.
  - `catalog.json`.
- [ ] Add `SUT/minimum_mr_subset_p3/` with the same shape.
- [ ] Keep runtime scripts cloud-safe. If using pure-stdlib deterministic implementations, document that they are MetBench runtime slices derived from imported PUT semantics, not proof of executing external NumPy/SciPy adapters.
- [ ] Add matching catalog blueprints / metadata rows following A-group runtime promotion patterns.
- [ ] Update `.github/governance/expected-catalog-counts.txt` and related count facts.
- [ ] Run focused launcher tests.

**Acceptance standards**

- P3/P8 live MR IDs are listed by launcher.
- P3/P8 focused launcher tests pass.
- Runtime inventory count updates match manifest output.
- No WPF files are changed.

## Task 7: Stage 2 RED - Async Job Pipeline Tests

**Preconditions**

- Task 6 launcher tests pass.
- Read current async tests under `MetBench_SystemMT.Tests/SystemMT/Jobs/`.

**Core steps**

- [ ] Add focused async integration tests, for example `MinimumMrSubsetBGroupAsyncJobTests`.
- [ ] Tests must construct the real `SystemMtAsyncPipeline` over a real test launcher.
- [ ] Tests must submit P3/P8 MR IDs as `SystemMtJobRequest` through `SystemMtJobService.SubmitAsync`.
- [ ] Tests must execute `SystemMtJobWorker.RunJobAsync`.
- [ ] Tests must poll `GetStatusAsync(jobId)` and read `GetResultAsync(jobId)`.
- [ ] Run focused async tests.

**Acceptance standards**

- If no async glue is missing, tests may already pass; if they do, record this as evidence that current async abstraction is already compatible.
- If tests fail, the failure must identify the missing async integration point.

## Task 8: Stage 2 GREEN - Async Compatibility Fixes Only If Needed

**Preconditions**

- Task 7 result is known.

**Core steps**

- [ ] If Task 7 passes without production changes, do not add code.
- [ ] If Task 7 fails because `SystemMtJobRequest` cannot carry needed parameter overrides, add the smallest compatible extension and preserve existing constructor semantics.
- [ ] If Task 7 fails because runtime MR IDs are not discoverable by `SystemMtAsyncPipeline.ResolveSutNameAsync`, fix catalog/listing only; do not special-case P3/P8 inside async code.
- [ ] Re-run focused async tests.

**Acceptance standards**

- Async tests pass through the current service/queue/store/worker/pipeline path.
- No P3/P8-specific branch is added to `SystemMtAsyncPipeline`.
- Cancellation behavior from PR #288 remains untouched unless a new long-running P3/P8 test is deliberately added.

## Task 9: Stage 2 Verification, Docs, And Commit

**Preconditions**

- Tasks 5-8 pass.

**Core steps**

- [ ] Run focused import/export tests.
- [ ] Run focused launcher tests for P3/P8.
- [ ] Run focused async job tests for P3/P8.
- [ ] Run `rtk dotnet test MetBench_SystemMT.Tests --no-restore --filter "SemanticCatalogBoundaryTests|Catalog_MR_id_set_equals_governance_whitelist"`.
- [ ] Run `rtk git diff --check`.
- [ ] Update `docs/status/current.md` only with evidence actually produced.
- [ ] Update `docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md` with Stage 1 / Stage 2 completion state.
- [ ] Commit Stage 2 as `feat(systemmt): promote minimum MR subset B group runtime`.

**Acceptance standards**

- Final report distinguishes:
  - imported research evidence,
  - MetBench live execution evidence,
  - async job pipeline evidence,
  - external smoke limitations.
- If local environment still cannot run external NumPy/SciPy adapters, that remains a stated limitation.
- No Windows evidence is claimed unless a separate VM prompt was executed.

## Task 10: Optional VM Prompt

**Preconditions**

- Stage 2 cloud-side implementation is complete.
- User requests VM validation or WPF evidence.

**Core steps**

- [ ] Write `docs/superpowers/vm-prompts/2026-06-04-minimum-mr-subset-b-group-runtime-vm-prompt.md`.
- [ ] Prompt must ask the VM agent to run only Windows build/optional UI smoke relevant to live runtime visibility.
- [ ] Prompt must not ask VM to redesign import/export or edit cloud-side catalog semantics.

**Acceptance standards**

- VM prompt gives exact branch, files, commands, expected outputs, logs to collect, and pass/fail criteria.
- VM prompt clearly states whether WPF UI evidence is required or optional.
