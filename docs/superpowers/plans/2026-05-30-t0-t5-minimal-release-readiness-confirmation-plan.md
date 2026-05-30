# T0-T5 Minimal Release Readiness Confirmation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Confirm whether MetBench T0, T1, T2, T3, T4, and T5 together form a minimum engineered System-MT product that can be released and delivered to users.

**Architecture:** This is a verification-and-assessment plan, not a feature build. It uses three cloud-runnable SUT/MR slices to exercise the end-to-end System-MT path and adds focused confirmation for reporting, discovery binding, anomaly workflow, Windows UI evidence, and release gates. The final output is a release-readiness assessment report with core-function coverage, scenario coverage, evidence links, and a release decision.

**Tech Stack:** .NET 8, xUnit, Reqnroll, LiteDB, WPF UI verification by Windows VM, pure-stdlib Python SUT runners, MetBench System-MT launcher/catalog/pipeline/reporting/anomaly services.

---

## Scope And Release Question

This plan answers one question for the week:

> Do T0, T1, T2, T3, T4, and T5 constitute a minimum engineered System-MT system that satisfies release conditions for user delivery?

The plan deliberately excludes T6 mutation adequacy from the release decision because the user asked for T0-T5. T6 remains a post-release quality-growth track unless a blocking defect is found in T0-T5.

## Current Truth Inputs

Read before executing:

1. `docs/status/current.md`
2. live `origin/main` and `HEAD`
3. `docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md`
4. `CLAUDE.md`
5. this plan

Live baseline at plan authoring time:

| Field | Value |
|---|---|
| Worktree | `/Users/limeng/Codes/MetBench-V2.1.4_2` |
| Branch | `main` |
| Live head | `b9e917c15683c37466f23e2c4927aecc6cdff8b2` |
| Runtime catalog denominator | `16 SUT / 13 equations / 33 MRs` |
| Ledger code-test baseline | `827394b`, `1556 / 0 / 12` cloud baseline |

Execution worktree for this run:

| Field | Value |
|---|---|
| Execution worktree | `/private/tmp/metbench-t0-t5-release-readiness` |
| Execution branch | `codex/t0-t5-release-readiness` |
| VM evidence branch | `claude/vm-t0-t5-release-readiness` |

## Minimal SUT/MR Confirmation Set

Use exactly these three SUT/MR slices for the release-smoke confirmation:

| Slice | SUT | MR | Meta-pattern | Why selected |
|---|---|---|---|---|
| S1 | `heat-equation` | `heat-equation-amplitude` | Mono | Exercises a mature pure-stdlib SUT, normal pass path, persistence, and the deliberate failure path used to confirm T5 anomaly creation. |
| S2 | `advection-1d` | `advection-mesh-conservation` | Inv | Exercises a pure-stdlib PDE SUT and an invariant MR through catalog, transformation, runner, parser, typed assertion, persistence. |
| S3 | `wave-1d` | `wave-mesh-energy-convergence` | Conv | Exercises a pure-stdlib PDE SUT and a convergence MR without SciPy/OpenMOC/OpenMC environment gates. |

Rationale:

- Three MRs cover the three primary meta-patterns: Mono, Inv, Conv.
- Three SUTs are enough to show repeatable additivity without turning this into a full benchmark campaign.
- All three SUTs use `python_executable_kind = system`, so failure means product regression rather than missing external scientific-runtime setup.
- The selected set covers `3 / 33 = 9.1%` of runtime MRs, `3 / 16 = 18.8%` of runtime SUTs, `3 / 13 = 23.1%` of runtime equations, and `3 / 3 = 100%` of primary meta-pattern families. Treat those numbers as release-smoke coverage, not scientific adequacy.

## File Structure

No production code should change during this confirmation.

- Create after execution: `docs/superpowers/specs/2026-05-30-t0-t5-minimal-release-readiness-report.md`
  - Purpose: final evidence-backed release-readiness assessment.
- Create: `docs/superpowers/specs/2026-05-30-t0-t5-github-exchange-protocol.md`
  - Purpose: define GitHub branch/status/screenshot exchange between macOS and VM.
- Create: `docs/superpowers/vm-prompts/2026-05-30-t0-t5-release-readiness-vm-prompt.md`
  - Purpose: self-contained Claude Code CLI prompt for the Windows VM.
- Create: `docs/superpowers/vm-prompts/2026-05-30-t0-t5-vm-monitor-hook.md`
  - Purpose: VM hook/heartbeat operating instructions.
- Create: `tools/release-readiness/vm_status_hook.ps1`
  - Purpose: append VM progress JSONL events and optionally commit/push evidence.
- Create after Windows VM execution: `docs/superpowers/specs/2026-05-30-t0-t5-vm-release-smoke/README.md`
  - Purpose: required evidence manifest mapping each check id to screenshot/command evidence.
- Read only: `SUT/heat_equation/catalog.json`
- Read only: `SUT/advection_1d/catalog.json`
- Read only: `SUT/wave_1d/catalog.json`
- Read only: `MetBench_SystemMT.Tests/SystemMT/Launcher/SystemMtLauncherTests.cs`
- Read only: `MetBench_SystemMT.Tests/SystemMT/Launcher/LauncherEndToEndAdvectionTests.cs`
- Read only: `MetBench_SystemMT.Tests/SystemMT/Launcher/LauncherEndToEndWaveTests.cs`
- Read only: `MetBench_SystemMT.Tests/SystemMT/Catalog/Binding/DiscoveredMrCatalogBinderTests.cs`
- Read only: `MetBench_SystemMT.Tests/V2Anomaly/`
- Read only: `MetBench_SystemMT.Tests/SystemMT/Reporting/`
- Read only: `MetBench_SystemMT.Tests/V2Reporting/SystemMtReportServiceTests.cs`

## Confirmation Coverage Model

Use this fixed checklist to compute core-function confirmation coverage.

| Layer | Check ID | Core function | Pass evidence |
|---|---|---|---|
| T0 | T0-1 | Launcher lists/runs selected MR by id | selected launcher tests pass |
| T0 | T0-2 | Source input becomes follow-up input | selected launcher tests pass and assert source/follow-up values |
| T0 | T0-3 | SUT runner executes and parser returns metric | selected launcher tests pass and assert value name |
| T0 | T0-4 | MR assertion returns pass/fail | selected pass + deliberate fail tests pass |
| T1 | T1-1 | system Python runtime works | selected pure-stdlib SUT tests pass |
| T1 | T1-2 | input/output adapters work | selected pure-stdlib SUT tests pass |
| T1 | T1-3 | execution/result persistence works | selected launcher tests assert execution/result records |
| T1 | T1-4 | CRUD/editor surfaces are covered by tests | catalog/editor focused tests pass |
| T1 | T1-5 | WPF user entry has Windows evidence | VM smoke report confirms page launch and core navigation |
| T2 | T2-1 | markdown/HTML report path works | report tests pass |
| T2 | T2-2 | PDF/Word/Excel report path works | report renderer tests pass |
| T2 | T2-3 | chart projection/rendering works | chart tests pass |
| T3 | T3-1 | selected MRs cover Mono/Inv/Conv | report records all three selected MR ids |
| T3 | T3-2 | selected SUTs cover multiple equation classes | report records all three selected SUT ids |
| T3 | T3-3 | runtime catalog denominator is current | expected catalog count fact passes |
| T4 | T4-1 | discovered candidate can bind into catalog shape | binder tests pass |
| T4 | T4-2 | invalid discovery candidates fail closed | binder validation tests pass |
| T5 | T5-1 | deliberate MR failure creates anomaly | failure launcher test passes |
| T5 | T5-2 | anomaly status machine is typed and persisted | anomaly status tests pass |
| T5 | T5-3 | anomaly replay/context path works | replay tests pass |
| T5 | T5-4 | orphan/cross-DB cleanup path works | orphan sweeper tests pass |

Coverage formula:

```text
core_function_confirmation_coverage = passed_check_count / 21
```

Decision thresholds:

| Decision | Rule |
|---|---|
| Release-ready | full cloud suite has 0 failures, selected smoke has 0 failures, Windows VM UI smoke has pass evidence for the target head, screenshot matrix is complete, and core coverage is 21/21 |
| Conditional release | full cloud suite has 0 failures and selected smoke has 0 failures, but screenshot evidence is stale/missing for a non-blocking T2/T4 item or core coverage is 19/21 or 20/21 |
| Not release-ready | any T0 selected smoke fails, any T1 runtime/persistence check fails, any T5 anomaly check fails, or full suite has non-environment failures |

## Task 1: Establish Baseline And Inputs

**Files:**
- Read: `docs/status/current.md`
- Read: `docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md`
- Read: `CLAUDE.md`
- Create later: `docs/superpowers/specs/2026-05-30-t0-t5-minimal-release-readiness-report.md`

- [ ] **Step 1: Confirm live git state**

Run:

```bash
rtk git status --short --branch
rtk git rev-parse HEAD
rtk git rev-parse origin/main
```

Expected:

```text
## codex/t0-t5-release-readiness...origin/codex/t0-t5-release-readiness
origin/main equals target production base b9e917c15683c37466f23e2c4927aecc6cdff8b2.
Coordinator branch contains only release-assessment docs, prompt, hook, and evidence-scaffold changes on top of the target production base.
```

- [ ] **Step 2: Confirm ledger and active-plan inputs are readable**

Run:

```bash
rtk sed -n '1,130p' docs/status/current.md
rtk sed -n '1,80p' docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md
rtk sed -n '155,205p' CLAUDE.md
```

Expected:

```text
docs/status/current.md states the current runtime catalog denominator.
active-plan-index.md lists no newer active scoped plan that supersedes this confirmation.
CLAUDE.md shows T0-T6 definitions.
```

- [ ] **Step 3: Record baseline facts in the assessment report**

Create `docs/superpowers/specs/2026-05-30-t0-t5-minimal-release-readiness-report.md` with this initial content:

```markdown
# T0-T5 Minimal Release Readiness Assessment

## Baseline

| Field | Value |
|---|---|
| Assessment date | 2026-05-30 |
| Worktree | /Users/limeng/Codes/MetBench-V2.1.4_2 |
| Branch | main |
| HEAD | value returned by `rtk git rev-parse HEAD` in Task 1 Step 1 |
| origin/main | value returned by `rtk git rev-parse origin/main` in Task 1 Step 1 |
| Runtime catalog denominator | 16 SUT / 13 equations / 33 MRs |
| Selected SUT/MR set | heat-equation/heat-equation-amplitude; advection-1d/advection-mesh-conservation; wave-1d/wave-mesh-energy-convergence |
| Execution worktree | /private/tmp/metbench-t0-t5-release-readiness |
| VM evidence branch | claude/vm-t0-t5-release-readiness |

## Evidence Log

| Check | Command | Result | Notes |
|---|---|---|---|
```

- [ ] **Step 4: Commit the report scaffold only if this plan is being executed on a release-assessment branch**

Run:

```bash
rtk git status --short
```

Expected:

```text
One new report file is present if Task 1 Step 3 was executed.
No production files are modified.
```

Do not commit during this task unless the user explicitly asks for commits.

## Task 2: Confirm T0/T1/T3 With Three SUT/MR Slices

**Files:**
- Read: `SUT/heat_equation/catalog.json`
- Read: `SUT/advection_1d/catalog.json`
- Read: `SUT/wave_1d/catalog.json`
- Test: `MetBench_SystemMT.Tests/SystemMT/Launcher/SystemMtLauncherTests.cs`
- Test: `MetBench_SystemMT.Tests/SystemMT/Launcher/LauncherEndToEndAdvectionTests.cs`
- Test: `MetBench_SystemMT.Tests/SystemMT/Launcher/LauncherEndToEndWaveTests.cs`

- [ ] **Step 1: Confirm the selected catalog rows exist**

Run:

```bash
rtk rg -n "heat-equation-amplitude|advection-mesh-conservation|wave-mesh-energy-convergence" SUT MetBench_SystemMT.Tests/SystemMT/Launcher
```

Expected:

```text
SUT/heat_equation/catalog.json contains heat-equation-amplitude.
SUT/advection_1d/catalog.json contains advection-mesh-conservation.
SUT/wave_1d/catalog.json contains wave-mesh-energy-convergence.
Launcher tests reference all three ids.
```

- [ ] **Step 2: Run the heat-equation normal pass path**

Run:

```bash
rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~SystemMtLauncherTests.RunAsync_heat_equation_with_default_factor_passes_and_persists_execution_result" --logger "console;verbosity=minimal"
```

Expected:

```text
Exit code 0.
Test summary has Failed: 0.
The test asserts Passed=true, value_name=max_u, execution status=ok, result assertion passed, and no anomaly.
```

- [ ] **Step 3: Run the advection invariant path**

Run:

```bash
rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~LauncherEndToEndAdvectionTests.RunAsync_advection_mesh_conservation_passes_end_to_end" --logger "console;verbosity=minimal"
```

Expected:

```text
Exit code 0.
Test summary has Failed: 0.
The test asserts Passed=true, value_name=mass_integral, execution status=ok, result assertion passed, and no anomaly.
```

- [ ] **Step 4: Run the wave convergence path**

Run:

```bash
rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~LauncherEndToEndWaveTests.RunAsync_wave_mesh_energy_convergence_passes_end_to_end" --logger "console;verbosity=minimal"
```

Expected:

```text
Exit code 0.
Test summary has Failed: 0.
The test asserts Passed=true, value_name=energy_proxy, execution status=ok, result assertion passed, and no anomaly.
```

- [ ] **Step 5: Update the report evidence log**

Append rows to the report:

```markdown
| T0/T1/T3 S1 | `rtk dotnet test ... SystemMtLauncherTests.RunAsync_heat_equation_with_default_factor_passes_and_persists_execution_result` | PASS/FAIL | heat-equation-amplitude; Mono; max_u |
| T0/T1/T3 S2 | `rtk dotnet test ... LauncherEndToEndAdvectionTests.RunAsync_advection_mesh_conservation_passes_end_to_end` | PASS/FAIL | advection-mesh-conservation; Inv; mass_integral |
| T0/T1/T3 S3 | `rtk dotnet test ... LauncherEndToEndWaveTests.RunAsync_wave_mesh_energy_convergence_passes_end_to_end` | PASS/FAIL | wave-mesh-energy-convergence; Conv; energy_proxy |
```

Replace `PASS/FAIL` with the actual command result.

## Task 3: Confirm T1 CRUD And Editor Surfaces

**Files:**
- Test: `MetBench_SystemMT.Tests/SystemMT/Catalog/Editing/SystemMtManifestCatalogEditorTests.cs`
- Test: `MetBench_SystemMT.Tests/SystemMT/Catalog/Editing/SystemMtSutEditorTests.cs`
- Test: `MetBench_SystemMT.Tests/SystemMT/Metadata/Editing/SystemMtEquationEditorTests.cs`
- Test: `MetBench_SystemMT.Tests/SystemMT/Catalog/Editing/SystemMtSampleCaseEditorTests.cs`
- Test: `MetBench_SystemMT.Tests/SystemMT/Persistence/Editing/ExecutionHistoryEditorTests.cs`

- [ ] **Step 1: Run MR manifest editor tests**

Run:

```bash
rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~SystemMtManifestCatalogEditorTests" --logger "console;verbosity=minimal"
```

Expected:

```text
Exit code 0.
Test summary has Failed: 0.
The tests confirm MR catalog list/create/validate/save behavior.
```

- [ ] **Step 2: Run SUT, Equation, SampleCase, and ExecutionHistory editor tests**

Run:

```bash
rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~SystemMtSutEditorTests" --logger "console;verbosity=minimal"
rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~SystemMtEquationEditorTests" --logger "console;verbosity=minimal"
rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~SystemMtSampleCaseEditorTests" --logger "console;verbosity=minimal"
rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~ExecutionHistoryEditorTests" --logger "console;verbosity=minimal"
```

Expected:

```text
All commands exit 0.
All summaries have Failed: 0.
The tests confirm the remaining T1 CRUD/editor surfaces.
```

- [ ] **Step 3: Update the report evidence log**

Append rows:

```markdown
| T1-4 MR CRUD | `rtk dotnet test ... SystemMtManifestCatalogEditorTests` | PASS/FAIL | MR manifest editor |
| T1-4 non-MR CRUD | `rtk dotnet test ... SUT/Equation/SampleCase/ExecutionHistory editor tests` | PASS/FAIL | SUT, Equation, SampleCase, ExecutionHistory |
```

Replace `PASS/FAIL` with actual command results.

## Task 4: Confirm T5 Failure-To-Anomaly Path

**Files:**
- Test: `MetBench_SystemMT.Tests/SystemMT/Launcher/SystemMtLauncherTests.cs`
- Test: `MetBench_SystemMT.Tests/V2Anomaly/AnomalyStatusTests.cs`
- Test: `MetBench_SystemMT.Tests/V2Schema/AnomalyStatusPersistenceTests.cs`
- Test: `MetBench_SystemMT.Tests/SystemMT/Anomaly/AnomalyOrphanSweeperTests.cs`
- Test: `MetBench_SystemMT.Tests/V2Pipeline/ReplayServiceTests.cs`
- Test: `MetBench_SystemMT.Tests/V2Pipeline/ReplayContextBuilderTests.cs`

- [ ] **Step 1: Run deliberate MR failure that must create an anomaly**

Run:

```bash
rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~SystemMtLauncherTests.RunAsync_persists_failure_when_assertion_fails" --logger "console;verbosity=minimal"
```

Expected:

```text
Exit code 0.
Test summary has Failed: 0.
The test uses heat-equation-amplitude with factor=0.5.
The test asserts Passed=false, execution status=anomaly, result assertion failed, and one anomaly is recorded.
```

- [ ] **Step 2: Run anomaly status machine tests**

Run:

```bash
rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~V2Anomaly.AnomalyStatusTests" --logger "console;verbosity=minimal"
rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~AnomalyStatusPersistenceTests" --logger "console;verbosity=minimal"
```

Expected:

```text
Both commands exit 0.
Both summaries have Failed: 0.
The tests confirm typed status parsing, legal transitions, illegal transition rejection, LiteDB int serialization, string-to-int migration, and GetByStatus.
```

- [ ] **Step 3: Run anomaly cleanup and replay tests**

Run:

```bash
rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~AnomalyOrphanSweeperTests" --logger "console;verbosity=minimal"
rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~V2Pipeline.ReplayServiceTests" --logger "console;verbosity=minimal"
rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~V2Pipeline.ReplayContextBuilderTests" --logger "console;verbosity=minimal"
```

Expected:

```text
All commands exit 0.
All summaries have Failed: 0.
The tests confirm orphan sweep behavior, report-only anomaly protection, replay verdicts, and anomaly-to-context reconstruction.
```

- [ ] **Step 4: Update the report evidence log**

Append rows:

```markdown
| T5-1 | `rtk dotnet test ... SystemMtLauncherTests.RunAsync_persists_failure_when_assertion_fails` | PASS/FAIL | heat-equation-amplitude factor=0.5 creates anomaly |
| T5-2 | `rtk dotnet test ... V2Anomaly.AnomalyStatusTests` and `... AnomalyStatusPersistenceTests` | PASS/FAIL | typed status + persistence |
| T5-3 | `rtk dotnet test ... V2Pipeline.ReplayServiceTests` and `... ReplayContextBuilderTests` | PASS/FAIL | replay path |
| T5-4 | `rtk dotnet test ... AnomalyOrphanSweeperTests` | PASS/FAIL | cleanup path |
```

Replace `PASS/FAIL` with actual command results.

## Task 5: Confirm T2 Reporting And Visualization

**Files:**
- Test: `MetBench_SystemMT.Tests/V2Reporting/SystemMtReportServiceTests.cs`
- Test: `MetBench_SystemMT.Tests/Reporting/HtmlSystemMtResultReportRendererTests.cs`
- Test: `MetBench_SystemMT.Tests/SystemMT/Reporting/`
- Test: `MetBench_SystemMT.Tests/SystemMT/Reporting/Charts/`

- [ ] **Step 1: Run markdown and HTML report tests**

Run:

```bash
rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~SystemMtReportServiceTests" --logger "console;verbosity=minimal"
rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~HtmlSystemMtResultReportRendererTests" --logger "console;verbosity=minimal"
```

Expected:

```text
Both commands exit 0.
Both summaries have Failed: 0.
The tests confirm execution markdown, typed verification projection, anomaly markdown, and HTML rendering compatibility.
```

- [ ] **Step 2: Run PDF, Word, Excel report renderer tests**

Run:

```bash
rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~PdfSystemMtResultReportRendererTests" --logger "console;verbosity=minimal"
rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~WordSystemMtResultReportRendererTests" --logger "console;verbosity=minimal"
rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~ExcelSystemMtResultReportRendererTests" --logger "console;verbosity=minimal"
```

Expected:

```text
All commands exit 0.
All summaries have Failed: 0.
The tests confirm the three non-HTML export paths.
```

- [ ] **Step 3: Run chart projection and renderer tests**

Run:

```bash
rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~SystemMT.Reporting.Charts" --logger "console;verbosity=minimal"
```

Expected:

```text
Exit code 0.
Test summary has Failed: 0.
The tests confirm chart DTO projection and rendering paths used by T2.
```

- [ ] **Step 4: Update the report evidence log**

Append rows:

```markdown
| T2-1 | `rtk dotnet test ... SystemMtReportServiceTests` and `... HtmlSystemMtResultReportRendererTests` | PASS/FAIL | markdown + HTML |
| T2-2 | `rtk dotnet test ... Pdf/Word/Excel renderer tests` | PASS/FAIL | 4-end reporting minus HTML already covered |
| T2-3 | `rtk dotnet test ... SystemMT.Reporting.Charts` | PASS/FAIL | chart projection/rendering |
```

Replace `PASS/FAIL` with actual command results.

## Task 6: Confirm T4 Discovery-To-Catalog Bridge

**Files:**
- Test: `MetBench_SystemMT.Tests/SystemMT/Catalog/Binding/DiscoveredMrCatalogBinderTests.cs`
- Read: `MetBench_BLL.Core/SystemMT/Catalog/Binding/`

- [ ] **Step 1: Run binder tests**

Run:

```bash
rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~DiscoveredMrCatalogBinderTests" --logger "console;verbosity=minimal"
```

Expected:

```text
Exit code 0.
Test summary has Failed: 0.
The tests confirm valid drafts bind into manifest-compatible catalog rows and invalid drafts fail closed.
```

- [ ] **Step 2: Confirm binder does not mutate SUT catalog files**

Run:

```bash
rtk git status --short SUT
```

Expected:

```text
No modified files under SUT/.
```

- [ ] **Step 3: Update the report evidence log**

Append rows:

```markdown
| T4-1 | `rtk dotnet test ... DiscoveredMrCatalogBinderTests` | PASS/FAIL | valid discovery draft binds |
| T4-2 | `rtk dotnet test ... DiscoveredMrCatalogBinderTests` | PASS/FAIL | invalid discovery draft fails closed |
| T4-purity | `rtk git status --short SUT` | PASS/FAIL | binder leaves SUT files unchanged |
```

Replace `PASS/FAIL` with actual command results.

## Task 7: Prepare And Monitor Windows VM Release Evidence

**Files:**
- Read: `docs/superpowers/specs/2026-05-29-debt5-vm-verification/`
- Read: `docs/superpowers/specs/2026-05-28-pr-1-vm-verification/`
- Read: `docs/superpowers/specs/2026-05-28-pr-2-vm-verification/`
- Read: `docs/superpowers/specs/2026-05-28-pr-3-vm-verification/`
- Read: `docs/superpowers/specs/2026-05-28-pr-4-vm-verification/`
- Create: `docs/superpowers/specs/2026-05-30-t0-t5-vm-release-smoke/README.md`

- [ ] **Step 1: Create required VM evidence manifest scaffold**

Create `docs/superpowers/specs/2026-05-30-t0-t5-vm-release-smoke/README.md`:

```markdown
# T0-T5 VM Release Smoke Evidence

| Field | Value |
|---|---|
| Run id | t0-t5-2026-05-30 |
| VM branch | claude/vm-t0-t5-release-readiness |
| Target head | b9e917c15683c37466f23e2c4927aecc6cdff8b2 |
| VM summary | vm-summary.md |
| Status stream | vm-status.jsonl |

## Screenshot Evidence Matrix

| Check ID | Layer | Required evidence | Status | Artifact |
|---|---|---|---|---|
| T0-1 | T0 | MR catalog or run page showing selected MR id | PENDING | 02-system-mt-run-or-catalog.png |
| T0-2 | T0 | terminal/UI screenshot showing source/follow-up execution evidence | PENDING | 02-system-mt-run-or-catalog.png |
| T0-3 | T0 | command or UI evidence showing returned metric | PENDING | 02-system-mt-run-or-catalog.png; vm-summary.md |
| T0-4 | T0 | command or UI evidence showing pass/fail assertion | PENDING | 02-system-mt-run-or-catalog.png; 09-anomaly-list.png; vm-summary.md |
| T1-1 | T1 | Windows build and runtime command evidence | PENDING | 01-build-output.png |
| T1-2 | T1 | selected SUT execution evidence | PENDING | 02-system-mt-run-or-catalog.png; vm-summary.md |
| T1-3 | T1 | execution/result persistence evidence | PENDING | 07-execution-history.png |
| T1-4 | T1 | CRUD/editor pages | PENDING | 03-mr-catalog.png; 04-sut-catalog.png; 05-equation-catalog.png; 06-samplecase-catalog.png |
| T1-5 | T1 | WPF user entry pages | PENDING | 03-mr-catalog.png; 04-sut-catalog.png; 05-equation-catalog.png; 06-samplecase-catalog.png; 07-execution-history.png |
| T2-1 | T2 | markdown/HTML report evidence | PENDING | 08-reporting-or-export.png |
| T2-2 | T2 | PDF/Word/Excel report evidence | PENDING | 08-reporting-or-export.png; vm-summary.md |
| T2-3 | T2 | chart/report projection evidence | PENDING | 08-reporting-or-export.png; vm-summary.md |
| T3-1 | T3 | Mono/Inv/Conv selected MR evidence | PENDING | 02-system-mt-run-or-catalog.png |
| T3-2 | T3 | selected SUT/equation evidence | PENDING | 02-system-mt-run-or-catalog.png |
| T3-3 | T3 | catalog denominator command evidence | PENDING | 02-system-mt-run-or-catalog.png; vm-summary.md |
| T4-1 | T4 | binder command evidence or catalog editor surface | PENDING | 03-mr-catalog.png; vm-summary.md |
| T4-2 | T4 | invalid candidate fail-closed command evidence | PENDING | 03-mr-catalog.png; vm-summary.md |
| T5-1 | T5 | failure-to-anomaly evidence | PENDING | 09-anomaly-list.png; vm-summary.md |
| T5-2 | T5 | typed status evidence | PENDING | 10-anomaly-status-action.png |
| T5-3 | T5 | replay/context command evidence | PENDING | 09-anomaly-list.png; 10-anomaly-status-action.png; vm-summary.md |
| T5-4 | T5 | orphan cleanup command/UI evidence | PENDING | 09-anomaly-list.png; 10-anomaly-status-action.png; vm-summary.md |
```

- [ ] **Step 2: Check whether recent VM evidence can be referenced as historical context only**

Run:

```bash
rtk rg -n "PASS|compile|boot|UIA|screenshot|System MT|Anomaly|CRUD|green" docs/superpowers/specs/2026-05-29-debt5-vm-verification docs/superpowers/specs/2026-05-28-pr-1-vm-verification docs/superpowers/specs/2026-05-28-pr-2-vm-verification docs/superpowers/specs/2026-05-28-pr-3-vm-verification docs/superpowers/specs/2026-05-28-pr-4-vm-verification
```

Expected:

```text
Evidence files show Windows compile/boot/UI interaction or screenshot proof for the WPF System-MT pages and anomaly workflow.
```

- [ ] **Step 3: Send the checked-in VM prompt to Claude Code inside the Windows VM**

Use the prompt saved at:

```text
docs/superpowers/vm-prompts/2026-05-30-t0-t5-release-readiness-vm-prompt.md
```

Expected:

```text
VM worker returns a concrete pass/fail receipt tied to the target head.
```

- [ ] **Step 4: Poll the VM branch for hook status**

Run:

```bash
rtk git fetch origin claude/vm-t0-t5-release-readiness
rtk git show origin/claude/vm-t0-t5-release-readiness:docs/superpowers/specs/2026-05-30-t0-t5-vm-release-smoke/vm-status.jsonl
```

Expected:

```text
`vm-status.jsonl` exists and contains setup/build/T-layer/final status events.
If the file is missing, VM has not pushed evidence yet; do not claim Windows evidence.
```

- [ ] **Step 5: Update the report evidence log**

Append row:

```markdown
| T1-5 Windows UI | VM receipt or existing VM evidence paths | PASS/FAIL/NOT RUN | WPF user entry evidence |
```

Replace `PASS/FAIL/NOT RUN` with actual evidence state. If not run, the release decision cannot be full release-ready.

## Task 8: Run Full Cloud Release Gate

**Files:**
- Test: `MetBench_SystemMT.Tests`
- Read: `.github/workflows/dotnet-test.yml`
- Read: `.github/governance/expected-catalog-counts.txt`

- [ ] **Step 1: Run the full cloud-safe test suite**

Run:

```bash
rtk dotnet test MetBench_SystemMT.Tests --logger "console;verbosity=minimal"
```

Expected:

```text
Exit code 0.
Test summary has Failed: 0.
Any skips are environment-gated OpenMOC/OpenMC/SciPy-style skips with explicit skip reasons.
```

- [ ] **Step 2: Run catalog denominator guard directly**

Run:

```bash
rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~Catalog_MR_id_set_equals_governance_whitelist" --logger "console;verbosity=minimal"
```

Expected:

```text
Exit code 0.
Test summary has Failed: 0.
Runtime catalog denominator remains aligned with .github/governance/expected-catalog-counts.txt.
```

- [ ] **Step 3: Check worktree cleanliness after verification**

Run:

```bash
rtk git status --short
```

Expected:

```text
Only the release-readiness report file and optional VM evidence index are modified or new.
No generated build artifacts are staged or tracked.
```

- [ ] **Step 4: Update the report evidence log**

Append rows:

```markdown
| Full cloud gate | `rtk dotnet test MetBench_SystemMT.Tests --logger "console;verbosity=minimal"` | PASS/FAIL | full suite |
| Catalog denominator | `rtk dotnet test ... Catalog_MR_id_set_equals_governance_whitelist` | PASS/FAIL | runtime catalog count guard |
| Worktree cleanliness | `rtk git status --short` | PASS/FAIL | no accidental generated artifacts |
```

Replace `PASS/FAIL` with actual command results.

## Task 9: Compute Coverage And Write Release Decision

**Files:**
- Modify: `docs/superpowers/specs/2026-05-30-t0-t5-minimal-release-readiness-report.md`

- [ ] **Step 1: Add core-function confirmation table**

Append this table to the report and fill the Result column from Tasks 2-7:

```markdown
## Core Function Confirmation

| Layer | Check ID | Result | Evidence |
|---|---|---|---|
| T0 | T0-1 launcher lists/runs selected MR by id | PASS/FAIL | selected launcher test output |
| T0 | T0-2 source input becomes follow-up input | PASS/FAIL | selected launcher test output |
| T0 | T0-3 SUT runner executes and parser returns metric | PASS/FAIL | selected launcher test output |
| T0 | T0-4 MR assertion returns pass/fail | PASS/FAIL | pass + deliberate fail tests |
| T1 | T1-1 system Python runtime works | PASS/FAIL | selected pure-stdlib SUT tests |
| T1 | T1-2 input/output adapters work | PASS/FAIL | selected pure-stdlib SUT tests |
| T1 | T1-3 execution/result persistence works | PASS/FAIL | selected launcher test assertions |
| T1 | T1-4 CRUD/editor surfaces are covered by tests | PASS/FAIL | catalog/editor tests or current ledger evidence |
| T1 | T1-5 WPF user entry has Windows evidence | PASS/FAIL/NOT RUN | VM evidence paths |
| T2 | T2-1 markdown/HTML report path works | PASS/FAIL | report tests |
| T2 | T2-2 PDF/Word/Excel report path works | PASS/FAIL | renderer tests |
| T2 | T2-3 chart projection/rendering works | PASS/FAIL | chart tests |
| T3 | T3-1 selected MRs cover Mono/Inv/Conv | PASS/FAIL | selected MR table |
| T3 | T3-2 selected SUTs cover multiple equation classes | PASS/FAIL | selected SUT table |
| T3 | T3-3 runtime catalog denominator is current | PASS/FAIL | catalog whitelist test |
| T4 | T4-1 discovered candidate can bind into catalog shape | PASS/FAIL | binder tests |
| T4 | T4-2 invalid discovery candidates fail closed | PASS/FAIL | binder tests |
| T5 | T5-1 deliberate MR failure creates anomaly | PASS/FAIL | failure launcher test |
| T5 | T5-2 anomaly status machine is typed and persisted | PASS/FAIL | anomaly status tests |
| T5 | T5-3 anomaly replay/context path works | PASS/FAIL | replay tests |
| T5 | T5-4 orphan/cross-DB cleanup path works | PASS/FAIL | orphan sweeper tests |
```

- [ ] **Step 2: Add scenario coverage summary**

Append:

```markdown
## Scenario Coverage

| Metric | Confirmed | Denominator | Coverage |
|---|---:|---:|---:|
| Selected runtime SUTs | 3 | 16 | 18.8% |
| Selected runtime MRs | 3 | 33 | 9.1% |
| Selected runtime equations | 3 | 13 | 23.1% |
| Primary meta-pattern families | 3 | 3 | 100.0% |

Interpretation: this is release-smoke coverage for minimum engineering readiness, not full scientific adequacy coverage.
```

- [ ] **Step 3: Compute core-function confirmation coverage**

Use this rule:

```text
PASS = 1
FAIL = 0
NOT RUN = 0
core_function_confirmation_coverage = sum(PASS) / 21
```

Append:

```markdown
## Coverage Calculation

| Metric | Value |
|---|---:|
| Passed core checks | integer count of PASS rows in the Core Function Confirmation table |
| Total core checks | 21 |
| Core-function confirmation coverage | passed core checks divided by 21, formatted as a percent with one decimal place |
| Screenshot matrix completeness | complete/incomplete, based on the VM evidence manifest |
```

- [ ] **Step 4: Write release decision**

Append one of these exact decisions:

```markdown
## Release Decision

Decision: RELEASE-READY

Reason: Full cloud suite has 0 failures; selected T0/T1/T3 smoke has 0 failures; T2/T4/T5 focused checks have 0 failures; Windows VM evidence is present for the target head; screenshot matrix is complete; core-function confirmation coverage is 21/21; no T0/T1/T5 blocker remains.
```

or:

```markdown
## Release Decision

Decision: CONDITIONAL RELEASE

Reason: Full cloud suite and selected smoke have 0 failures, but one non-blocking release evidence item is missing or stale, or core-function confirmation coverage is 19/21 or 20/21. User delivery is acceptable only with the limitation stated below.

Limitation: write the exact missing or stale evidence in one sentence, such as "Windows VM UI evidence was not refreshed for b9e917c15683c37466f23e2c4927aecc6cdff8b2."
```

or:

```markdown
## Release Decision

Decision: NOT RELEASE-READY

Reason: write the exact failing command and the affected T-layer in one sentence.

Required unblock action: write the smallest fix or verification step needed in one sentence.
```

- [ ] **Step 5: Add final user-facing summary**

Append:

```markdown
## User-Facing Summary

| Question | Answer |
|---|---|
| Do T0-T5 form a minimum engineered System-MT system? | YES/NO/CONDITIONAL |
| Is it suitable for user delivery? | YES/NO/CONDITIONAL |
| Core-function confirmation coverage | measured percent from Coverage Calculation |
| Scenario smoke coverage | 3 SUT / 3 MR; 100% primary meta-pattern family coverage |
| Main residual risk | one measured residual risk sentence |
| Next recommended action | one concrete next-action sentence |
```

Replace each angle-bracket field with the measured value or exact statement from the evidence log.

## Task 10: Final Self-Review Before Reporting

**Files:**
- Read: `docs/superpowers/plans/2026-05-30-t0-t5-minimal-release-readiness-confirmation-plan.md`
- Read: `docs/superpowers/specs/2026-05-30-t0-t5-minimal-release-readiness-report.md`

- [ ] **Step 1: Check that every user requirement has an evidence row**

Verify:

```text
T0 has at least 4 checks.
T1 has at least 5 checks.
T2 has at least 3 checks.
T3 has at least 3 checks.
T4 has at least 2 checks.
T5 has at least 4 checks.
The selected SUT/MR table has exactly 3 SUTs and exactly 3 MRs.
The VM evidence manifest has one row for each of the 21 core checks.
Full RELEASE-READY is used only when core-function confirmation coverage is 21/21.
The release decision is one of RELEASE-READY, CONDITIONAL RELEASE, NOT RELEASE-READY.
```

- [ ] **Step 2: Check for forbidden uncertainty in the report**

Run:

```bash
rtk rg -n "T[B]D|T[O]DO|probably|seems|should be|maybe|待补|待写" docs/superpowers/specs/2026-05-30-t0-t5-minimal-release-readiness-report.md
```

Expected:

```text
No matches.
```

If there are matches, replace them with measured evidence, explicit NOT RUN status, or a concrete blocker.

- [ ] **Step 3: Check final git status**

Run:

```bash
rtk git status --short
```

Expected:

```text
Only the plan file, the release-readiness report, and optional VM evidence index are changed.
No production code is changed.
```

- [ ] **Step 4: Prepare final response**

The final response must include:

```text
1. Exact HEAD assessed.
2. Selected SUT/MR set.
3. Core-function confirmation coverage.
4. Release decision.
5. One-line residual risk.
6. Path to the assessment report.
```

Do not claim release-ready unless the report contains the matching evidence rows.
