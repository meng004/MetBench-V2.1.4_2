# T0-T5 Minimal Release Readiness Assessment

## Baseline

| Field | Value |
|---|---|
| Assessment date | 2026-05-30 |
| Coordinator worktree | /private/tmp/metbench-t0-t5-release-readiness |
| Production repository root | /Users/limeng/Codes/MetBench-V2.1.4_2 |
| Branch | codex/t0-t5-release-readiness |
| Target production base | b9e917c15683c37466f23e2c4927aecc6cdff8b2 |
| Coordinator package HEAD | Resolve live from `rtk git rev-parse HEAD` on `codex/t0-t5-release-readiness` |
| Production-code delta from target base | NO |
| origin/main at assessment start | b9e917c15683c37466f23e2c4927aecc6cdff8b2 |
| Runtime catalog denominator | 16 SUT / 13 equations / 33 MRs |
| Selected SUT/MR set | heat-equation/heat-equation-amplitude; advection-1d/advection-mesh-conservation; wave-1d/wave-mesh-energy-convergence |
| VM evidence branch | claude/vm-t0-t5-release-readiness |

## Evidence Log

| Check | Command | Result | Notes |
|---|---|---|---|
| Baseline git state | `rtk git status --short --branch`; `rtk git rev-parse HEAD`; `rtk git rev-parse origin/main` | PASS | Code base starts at `b9e917c15683c37466f23e2c4927aecc6cdff8b2`; only assessment docs/tools are untracked. |
| Status ledger input | `rtk sed -n '1,130p' docs/status/current.md` | PASS | Ledger states live main must be resolved from git; runtime denominator is 16 SUT / 13 equations / 33 MRs; latest recorded cloud baseline is 1556 / 0 / 12 at `827394b`. |
| Active plan input | `rtk sed -n '1,80p' docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md` | PASS | No newer scoped implementation plan supersedes this release-readiness confirmation; T0-T5 component tracks are controlled or completed except deferred non-T0-T5 debt. |
| T-layer definitions | `rtk sed -n '155,205p' CLAUDE.md` | PASS | T0-T6 definitions confirm this assessment is scoped to T0-T5 and excludes T6 mutation adequacy from the release decision. |
| Catalog row presence | `rtk rg -n "heat-equation-amplitude\|advection-mesh-conservation\|wave-mesh-energy-convergence" SUT MetBench_SystemMT.Tests/SystemMT/Launcher` | PASS | Selected MR ids exist in SUT catalogs and launcher tests. |
| Local sandbox limitation | `rtk dotnet test ... SystemMtLauncherTests.RunAsync_heat_equation_with_default_factor_passes_and_persists_execution_result` inside sandbox | BLOCKED | MSBuild failed before tests with `SocketException (13): Permission denied`; same command passed outside sandbox. |
| T0/T1/T3 S1 | `rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~SystemMtLauncherTests.RunAsync_heat_equation_with_default_factor_passes_and_persists_execution_result" --logger "console;verbosity=minimal"` | PASS | 1 test passed; heat-equation-amplitude; Mono; max_u. |
| T0/T1/T3 S2 | `rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~LauncherEndToEndAdvectionTests.RunAsync_advection_mesh_conservation_passes_end_to_end" --logger "console;verbosity=minimal"` | PASS | 1 test passed; advection-mesh-conservation; Inv; mass_integral. |
| T0/T1/T3 S3 | `rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~LauncherEndToEndWaveTests.RunAsync_wave_mesh_energy_convergence_passes_end_to_end" --logger "console;verbosity=minimal"` | PASS | 1 test passed; wave-mesh-energy-convergence; Conv; energy_proxy. |
| T1-4 MR CRUD | `rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~SystemMtManifestCatalogEditorTests" --logger "console;verbosity=minimal"` | PASS | 6 tests passed; MR manifest editor. |
| T1-4 non-MR CRUD | `rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~SystemMtSutEditorTests\|FullyQualifiedName~SystemMtEquationEditorTests\|FullyQualifiedName~SystemMtSampleCaseEditorTests\|FullyQualifiedName~ExecutionHistoryEditorTests" --logger "console;verbosity=minimal"` | PASS | 37 tests passed; SUT, Equation, SampleCase, ExecutionHistory editors. |
| T5-1 | `rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~SystemMtLauncherTests.RunAsync_persists_failure_when_assertion_fails" --logger "console;verbosity=minimal"` | PASS | 1 test passed; heat-equation-amplitude factor=0.5 creates anomaly. |
| T5-2 | `rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~V2Anomaly.AnomalyStatusTests\|FullyQualifiedName~AnomalyStatusPersistenceTests" --logger "console;verbosity=minimal"` | PASS | 37 tests passed; typed status and LiteDB persistence. |
| T5-3/T5-4 | `rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~AnomalyOrphanSweeperTests\|FullyQualifiedName~V2Pipeline.ReplayServiceTests\|FullyQualifiedName~V2Pipeline.ReplayContextBuilderTests" --logger "console;verbosity=minimal"` | PASS | 24 tests passed; replay/context and orphan cleanup. |
| T2-1 | `rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~SystemMtReportServiceTests\|FullyQualifiedName~HtmlSystemMtResultReportRendererTests" --logger "console;verbosity=minimal"` | PASS | 28 tests passed; markdown and HTML reporting. |
| T2-2 | `rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~PdfSystemMtResultReportRendererTests\|FullyQualifiedName~WordSystemMtResultReportRendererTests\|FullyQualifiedName~ExcelSystemMtResultReportRendererTests" --logger "console;verbosity=minimal"` | PASS | 45 tests passed; PDF, Word, Excel renderers. |
| T2-3 | `rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~SystemMT.Reporting.Charts" --logger "console;verbosity=minimal"` | PASS | 39 tests passed; chart projection/rendering. |
| T4-1/T4-2 | `rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~DiscoveredMrCatalogBinderTests" --logger "console;verbosity=minimal"` | PASS | 34 tests passed; valid discovery draft binds and invalid candidates fail closed. |
| T4-purity | `rtk git status --short SUT` | PASS | No modified files under `SUT/`. |
| Catalog denominator | `rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~Catalog_MR_id_set_equals_governance_whitelist" --logger "console;verbosity=minimal"` | PASS | 1 test passed; runtime catalog count guard green. |
| Full cloud gate | `rtk dotnet test MetBench_SystemMT.Tests --logger "console;verbosity=minimal"` | PASS | 1554 tests passed; 0 failures reported by the local test wrapper. |
| GitHub coordinator branch | `rtk git ls-remote --heads origin codex/t0-t5-release-readiness claude/vm-t0-t5-release-readiness` | PASS | `codex/t0-t5-release-readiness` exists at `d9f3bbf7119d884d4f67c57a801ffd9a76c8200a`; VM evidence branch is absent. |
| VM evidence polling | `rtk git fetch origin claude/vm-t0-t5-release-readiness` | NOT RUN | Remote returned `fatal: couldn't find remote ref claude/vm-t0-t5-release-readiness`; VM evidence has not been pushed. |
| T1-5 Windows UI | VM branch receipt and screenshot matrix | NOT RUN | VM evidence branch has not been collected in this coordinator run. |

## Selected Release-Smoke SUT/MR Set

| Slice | SUT | MR | Meta-pattern | Metric |
|---|---|---|---|---|
| S1 | heat-equation | heat-equation-amplitude | Mono | max_u |
| S2 | advection-1d | advection-mesh-conservation | Inv | mass_integral |
| S3 | wave-1d | wave-mesh-energy-convergence | Conv | energy_proxy |

## Core Function Confirmation

| Layer | Check ID | Result | Evidence |
|---|---|---|---|
| T0 | T0-1 launcher lists/runs selected MR by id | PASS | selected launcher test output for S1/S2/S3 |
| T0 | T0-2 source input becomes follow-up input | PASS | selected launcher test output for S1/S2/S3 |
| T0 | T0-3 SUT runner executes and parser returns metric | PASS | selected launcher test output for S1/S2/S3 |
| T0 | T0-4 MR assertion returns pass/fail | PASS | selected pass tests plus deliberate failure test |
| T1 | T1-1 system Python runtime works | PASS | selected pure-stdlib SUT tests for S1/S2/S3 |
| T1 | T1-2 input/output adapters work | PASS | selected pure-stdlib SUT tests for S1/S2/S3 |
| T1 | T1-3 execution/result persistence works | PASS | selected launcher test assertions for S1/S2/S3 |
| T1 | T1-4 CRUD/editor surfaces are covered by tests | PASS | 6 MR editor tests + 37 non-MR editor tests |
| T1 | T1-5 WPF user entry has Windows evidence | NOT RUN | VM evidence branch and screenshot matrix not collected yet |
| T2 | T2-1 markdown/HTML report path works | PASS | 28 markdown/HTML report tests |
| T2 | T2-2 PDF/Word/Excel report path works | PASS | 45 renderer tests |
| T2 | T2-3 chart projection/rendering works | PASS | 39 chart tests |
| T3 | T3-1 selected MRs cover Mono/Inv/Conv | PASS | selected MR table above |
| T3 | T3-2 selected SUTs cover multiple equation classes | PASS | selected SUT table above |
| T3 | T3-3 runtime catalog denominator is current | PASS | 1 catalog whitelist test |
| T4 | T4-1 discovered candidate can bind into catalog shape | PASS | 34 binder tests |
| T4 | T4-2 invalid discovery candidates fail closed | PASS | 34 binder tests |
| T5 | T5-1 deliberate MR failure creates anomaly | PASS | 1 failure launcher test |
| T5 | T5-2 anomaly status machine is typed and persisted | PASS | 37 anomaly status and persistence tests |
| T5 | T5-3 anomaly replay/context path works | PASS | replay service/context tests included in 24-test group |
| T5 | T5-4 orphan/cross-DB cleanup path works | PASS | anomaly orphan sweeper tests included in 24-test group |

## Scenario Coverage

| Metric | Confirmed | Denominator | Coverage |
|---|---:|---:|---:|
| Selected runtime SUTs | 3 | 16 | 18.8% |
| Selected runtime MRs | 3 | 33 | 9.1% |
| Selected runtime equations | 3 | 13 | 23.1% |
| Primary meta-pattern families | 3 | 3 | 100.0% |

Interpretation: this is release-smoke coverage for minimum engineering
readiness, not full scientific adequacy coverage.

## Coverage Calculation

| Metric | Value |
|---|---:|
| Passed core checks | 20 |
| Total core checks | 21 |
| Core-function confirmation coverage | 95.2% |
| Screenshot matrix completeness | incomplete |

## Release Decision

Decision: CONDITIONAL RELEASE

Reason: Full cloud suite and selected smoke have 0 failures, but Windows VM UI
evidence was not refreshed for `b9e917c15683c37466f23e2c4927aecc6cdff8b2`, the
screenshot matrix is incomplete, and core-function confirmation coverage is
20/21.

Limitation: user delivery is acceptable only as a cloud-validated conditional
release until the VM worker fills the screenshot matrix and pushes the VM
evidence branch.

## User-Facing Summary

| Question | Answer |
|---|---|
| Do T0-T5 form a minimum engineered System-MT system? | CONDITIONAL YES |
| Is it suitable for user delivery? | CONDITIONAL |
| Core-function confirmation coverage | 95.2% from baseline, selected T0/T1/T3 smoke, T1 CRUD/editor, T2 reporting, T3 denominator, T4 binder, and T5 anomaly workflow evidence |
| Scenario smoke coverage | 3 SUT / 3 MR; 100% primary meta-pattern family coverage |
| Main residual risk | Windows WPF UI entry evidence and screenshot matrix are not refreshed for the target production base. |
| Next recommended action | Push the VM execution package and collect VM screenshot evidence on `claude/vm-t0-t5-release-readiness`. |
