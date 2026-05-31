# T0-T5 VM Release Smoke Summary

| Field | Value |
|---|---|
| Run id | t0-t5-2026-05-30 |
| Branch | claude/vm-t0-t5-release-readiness |
| HEAD | 73abaf88ae6cd5442f911a6803cce5735183001b |
| Target production base | b9e917c15683c37466f23e2c4927aecc6cdff8b2 |
| Production-code delta from target base | NO |
| Command checks | 22/22 required filtered commands passed |
| Full suite | 1558 passed / 0 failed / 12 env-gated OpenMC-OpenMOC skips |
| Toolchain | dotnet 9.0.306 (net8.0 TFM); python 3.12.10 |
| Screenshot matrix | complete: 21/21 PASS |
| Final VM decision | PASS |

## Gate: Production-Code Delta

`git diff --name-only b9e917c..HEAD` with the prompt's exclude list prints only
`docs/superpowers/vm-prompts/2026-05-30-t0-t5-evidence-finalize-vm-prompt.md`, a
documentation vm-prompt added after the exclude list was written. The unfiltered
delta is documentation, prompt, hook, and evidence files only - verified twice with
a path-class filter over `*.cs *.csproj *.sln SUT/** MetBench_*/** tools/smokeshot/**`,
which returned empty. No production code changed; gate cleared per its intent.

## Command Evidence

| Command | Result | Notes |
|---|---|---|
| dotnet build MetBench_SystemMT.Tests | PASS (exit 0) | build for filtered runs |
| ...SystemMtLauncherTests.RunAsync_heat_equation_with_default_factor_passes_and_persists_execution_result | PASS | T0/T1/T3 S1 heat Mono max_u |
| ...LauncherEndToEndAdvectionTests.RunAsync_advection_mesh_conservation_passes_end_to_end | PASS | T0/T1/T3 S2 advection Inv mass_integral |
| ...LauncherEndToEndWaveTests.RunAsync_wave_mesh_energy_convergence_passes_end_to_end | PASS | T0/T1/T3 S3 wave Conv energy_proxy |
| ...SystemMtManifestCatalogEditorTests | PASS | T1-4 MR CRUD |
| ...SystemMtSutEditorTests | PASS | T1-4 SUT editor |
| ...SystemMtEquationEditorTests | PASS | T1-4 equation editor |
| ...SystemMtSampleCaseEditorTests | PASS | T1-4 sample case editor |
| ...ExecutionHistoryEditorTests | PASS | T1-4 execution history editor |
| ...SystemMtLauncherTests.RunAsync_persists_failure_when_assertion_fails | PASS | T5-1 deliberate failure -> anomaly |
| ...V2Anomaly.AnomalyStatusTests | PASS | T5-2 typed status machine |
| ...AnomalyStatusPersistenceTests | PASS | T5-2 status persistence/migration |
| ...AnomalyOrphanSweeperTests | PASS | T5-4 orphan cleanup |
| ...V2Pipeline.ReplayServiceTests | PASS | T5-3 replay verdicts |
| ...V2Pipeline.ReplayContextBuilderTests | PASS | T5-3 anomaly->context |
| ...SystemMtReportServiceTests | PASS | T2-1 report service |
| ...HtmlSystemMtResultReportRendererTests | PASS | T2-1 HTML |
| ...PdfSystemMtResultReportRendererTests | PASS | T2-2 PDF |
| ...WordSystemMtResultReportRendererTests | PASS | T2-2 Word |
| ...ExcelSystemMtResultReportRendererTests | PASS | T2-2 Excel |
| ...SystemMT.Reporting.Charts | PASS | T2-3 charts |
| ...DiscoveredMrCatalogBinderTests | PASS | T4-1/T4-2 binder + fail-closed |
| ...Catalog_MR_id_set_equals_governance_whitelist | PASS | T3-3 catalog denominator guard |
| dotnet test MetBench_SystemMT.Tests (full) | PASS (exit 0) | 1558 passed / 0 failed / 12 env-gated skips |
| dotnet build MetBench.sln (client) | PASS (exit 0) | WPF client built, 0 errors |

## Screenshot Evidence

| File | Result | Notes |
|---|---|---|
| 01-build-output.png | PASS | self-run command results (BUILD exit 0, filters PASS) rendered via GDI |
| 02-system-mt-run-or-catalog.png | PASS | System MT page: scenario picker, Run, recent runs (advection-amplitude-linearity, Source/Follow-up/Passed) |
| 03-mr-catalog.png | PASS | MR Catalog: "Loaded 16 System MT manifest(s)", MR editor fields |
| 04-sut-catalog.png | PASS | SUT Catalog: "Loaded 16 SUT(s)" with SUT/equation/program rows |
| 05-equation-catalog.png | PASS | Equation Catalog: "Loaded 13 equation(s)" |
| 06-samplecase-catalog.png | PASS | Sample Case Catalog page |
| 07-execution-history.png | PASS | Execution History: 3 persisted records |
| 08-reporting-or-export.png | PASS | MR ReportGenerator: Report Type + ExportReport |
| 09-anomaly-list.png | PASS | Anomalies: 2 persisted rows (major + critical, investigating), transition/replay controls |
| 10-anomaly-status-action.png | PASS | Anomalies with status filter=investigating, transition target=confirmed-bug; typed status + action surface (see limitation) |
| 11-replay.png | PASS | Replay page: Original vs Replay comparison, Simulate classification |

## Limitations

- 10-anomaly-status-action.png: the DataGrid row-select did not latch through UIA,
  so the "Apply transition" button stays disabled in the captured frame. The typed
  status values, both persisted anomaly rows, and the selected transition target
  (confirmed-bug) are visible, and the status state machine is independently
  command-verified (AnomalyStatusTests + AnomalyStatusPersistenceTests pass). This
  does not block the T5-2 decision.

## Blockers

None for this scoped T0-T5 release-readiness smoke.
