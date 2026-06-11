# External MR Assets Batch A/D Cloud Evidence (2026-06-11)

## Scope

This evidence note records the cloud-side completion evidence for Batch A and
Batch D in
`docs/superpowers/plans/2026-06-11-external-mr-assets-metbench-acceptance-import-plan.md`.

It does not claim Windows/WPF UI completion. WPF build, UI screenshots, and
visible report/dashboard/anomaly confirmation remain VM evidence and are
covered by
`docs/superpowers/vm-prompts/2026-06-11-batch-a-d-external-mr-assets-ui-acceptance-vm-prompt.md`.

## Branch and Commits

- Branch: `codex/external-mr-asset-acceptance-plan`
- Cloud acceptance commit:
  `5f6c7f7` `test: complete external MR batch A D acceptance`
- Report/chart coverage commit:
  `6f4a38f` `test: cover external MR batch reports and charts`
- Local and remote branch heads matched at
  `6f4a38f2431af6bd93438e352da18871769c7220` after push.

## Implemented Import Packages

The import packages are deterministic `SutImportUnit` fixtures in
`MetBench_BLL.Core/SystemMT/ImportExport/Put/ExternalMrAcceptancePutFixtures.cs`.

| Package | Batch | Runtime status | Evidence payload |
|---|---|---|---|
| `metbench-import-minmr-toy-classic-v1` | A | Imported package plus selected local acceptance runtime | 7 toy MRs over sorting, matrix multiplication, and quadratic roots |
| `metbench-import-minmr-p1-heat-v1` | A | Imported package plus selected local acceptance runtime | 10 P1 heat MRs, 5 mutation classes, 50 detection rows |
| `metbench-import-sciml-domain-validity-fixture-v1` | D | Imported-only evidence | 3 SciML MR cards, 10 seeded-fault mutants, 30 detection rows |

## Runtime Test Data

Batch A local acceptance runtime data lives under
`SUT/external_acceptance_minmr/`.

- `acceptance-catalog.json`: explicit test-only catalog. It is intentionally
  not named `catalog.json`, so the production manifest catalog provider does
  not discover it as part of the global catalog.
- `external_acceptance_minmr.py`: pure-stdlib local runner for one toy sorting
  MR and three P1 heat MRs.
- `external_acceptance_minmr_input_parser.py`: input parser.
- `external_acceptance_minmr_output_parser.py`: output parser.
- `sample/toy_sort.json`: toy sorting source input.
- `sample/p1_heat.json`: P1 heat source input.

The test project copies these assets into
`TestAssets/external_acceptance_minmr` through
`MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj`.

## Acceptance Coverage

| Plan item | Cloud evidence | Status |
|---|---|---|
| AT-01 Import Batch A package through `ImportAssets` | `ExternalMrAcceptanceCompletionTests.ImportAssets_job_stages_external_acceptance_package` covers toy and P1 packages | Complete |
| AT-02 Run local toy MR | `Batch_A_acceptance_catalog_runs_one_toy_and_three_p1_mrs_through_launcher` runs `minmr-toy-sort-permutation` | Complete |
| AT-03 Run local P1 heat MRs | Same launcher test runs alpha monotonic, timestep convergence, and mesh convergence | Complete |
| AT-04 Import Batch D SciML evidence | `ExternalMrAcceptanceBatchImportTests.Batch_D_sciml_fixture_preserves_domain_validity_cards_and_seeded_fault_matrix` | Complete |
| AT-05 Preserve deferred/diagnostic MR boundary | `Batch_D_sciml_report_renders_limitations_and_deferred_diagnostics` asserts `mgn-discrete-divergence-boundedness` stays diagnostic/deferred | Complete |
| AT-06 Project anomaly candidates from imported evidence | `Batch_A_p1_detection_matrix_projects_detected_records_to_anomaly_candidates` | Complete |
| AT-07 Export Batch A package through `ExportAssets` | `ExportAssets_job_round_trips_staged_external_acceptance_package` covers staged toy/P1/SciML package export | Complete |
| AT-08 Report export for Batch A execution set | `Batch_A_report_and_visualization_modules_render_execution_outputs` renders HTML, Word, Excel, and PDF | Complete |
| AT-09 Visualization DTOs for Batch A executions | Same test projects four execution records through `BinaryRunPointProjector` | Complete |
| AT-10 Local preflight for Batch A | Covered indirectly by successful launcher execution through the configured local Python runtime | Complete for cloud local runtime |

## Verification Commands

The following commands were run from the repository root on branch
`codex/external-mr-asset-acceptance-plan`.

```powershell
dotnet test MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj --no-restore --filter FullyQualifiedName~ExternalMrAcceptance
```

Result: 14 passed / 0 failed / 0 skipped.

```powershell
dotnet test MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj --no-restore --filter FullyQualifiedName~SystemMT.ImportExport
```

Result: 59 passed / 0 failed / 3 skipped. The skipped tests are existing
external-source prerequisite gates.

```powershell
dotnet test MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj --no-restore --filter FullyQualifiedName~CatalogParityTests
```

Result: 8 passed / 0 failed / 0 skipped.

```powershell
git diff --check
```

Result: exit 0. Git reported only line-ending warnings for touched files.

## Test Conclusion

Cloud-side Batch A is complete for import, selected local runtime execution,
export, anomaly-candidate projection, report rendering, and chart DTO
projection.

Cloud-side Batch D is complete for imported SciML domain-validity evidence,
seeded-fault matrix preservation, imported-evidence reporting, and explicit
diagnostic/deferred treatment of `mgn-discrete-divergence-boundedness`.

The remaining acceptance gap is Windows/WPF UI evidence. Completion of that
gap requires executing the registered VM prompt and collecting the listed build
output, focused test output, screenshots, and PowerShell transcript.
