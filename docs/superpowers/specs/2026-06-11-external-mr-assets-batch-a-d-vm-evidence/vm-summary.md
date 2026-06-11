# Batch A/D External MR Assets WPF VM Summary

branch=codex/external-mr-asset-acceptance-plan
head=39ebb4efd1ad824fa02e23e4e817cf467bb4c47c
origin_main=c0d5a0e5e17512a4d5e31d28e768421f06158a52

## Commands

- `dotnet build MetBench_Client\MetBench_Client.csproj --no-restore -v:minimal`: exit 0; errors 0
- `dotnet run --project package-generator\BatchPackageGenerator.csproj -- <packageRoot>`: exit 0
- `dotnet run --project package-generator\BatchPackageGenerator.csproj -- latest-minmr-result <db>`: exit 0

## Environment

- `METBENCH_EXTRA_MR_MANIFESTS=C:\MetBench-V2.1.4_2\MetBench_Client\bin\Debug\net8.0-windows7.0\SUT\external_acceptance_minmr\acceptance-catalog.json`

## WPF Jobs

| Operation | Scope | JobId | State | ArtifactPath |
|---|---|---|---|---|
| ImportAssets | Batch A toy | 13c5b6df-d6ab-4f42-a781-e16245eacfed | Succeeded | C:\MetBench-V2.1.4_2\docs\superpowers\specs\2026-06-11-external-mr-assets-batch-a-d-vm-evidence\operation-artifacts\staging\toy\minimum-mr-subset-toy-classic\202606110921093193033Z\staging-manifest.json |
| ImportAssets | Batch A P1 heat | 6e8725d2-af8f-4b30-bd48-d1469b974be6 | Succeeded | C:\MetBench-V2.1.4_2\docs\superpowers\specs\2026-06-11-external-mr-assets-batch-a-d-vm-evidence\operation-artifacts\staging\p1\minimum-mr-subset-p1-heat\202606110921167398228Z\staging-manifest.json |
| ImportAssets | Batch D SciML | ccc1fb85-0427-4371-a860-b17453ed7a55 | Succeeded | C:\MetBench-V2.1.4_2\docs\superpowers\specs\2026-06-11-external-mr-assets-batch-a-d-vm-evidence\operation-artifacts\staging\sciml\sciml-domain-validity-mgn\202606110921246913548Z\staging-manifest.json |
| RunBatch | Batch A 4 MRs | 9614d6ab-29b3-48bd-8ba4-20eec8cb654e | Succeeded | - |
| ExportAssets | Batch A toy staged package | 560b5d14-b286-44eb-9802-fcda15ea4431 | Succeeded | C:\MetBench-V2.1.4_2\docs\superpowers\specs\2026-06-11-external-mr-assets-batch-a-d-vm-evidence\operation-artifacts\export\toy\sut-import-unit.json |
| ExportAssets | Batch A P1 staged package | 7671ed3d-beed-4e94-8843-6d397caf9786 | Succeeded | C:\MetBench-V2.1.4_2\docs\superpowers\specs\2026-06-11-external-mr-assets-batch-a-d-vm-evidence\operation-artifacts\export\p1\sut-import-unit.json |
| ExportAssets | Batch D SciML staged package | 8c3a9201-7614-4794-aa16-a64e15aaaf56 | Succeeded | C:\MetBench-V2.1.4_2\docs\superpowers\specs\2026-06-11-external-mr-assets-batch-a-d-vm-evidence\operation-artifacts\export\sciml\sut-import-unit.json |
| ExportReport | Batch A latest minmr execution | 0e6b4f6c-4987-4275-ae14-d389c50b56ce | Succeeded | C:\MetBench-V2.1.4_2\docs\superpowers\specs\2026-06-11-external-mr-assets-batch-a-d-vm-evidence\operation-artifacts\report-export\manifest.json |

report_execution_id=e3a0c137-83df-4032-8ba8-bc5db357446b
report_export_root=C:\MetBench-V2.1.4_2\docs\superpowers\specs\2026-06-11-external-mr-assets-batch-a-d-vm-evidence\operation-artifacts\report-export
report_html=C:\MetBench-V2.1.4_2\docs\superpowers\specs\2026-06-11-external-mr-assets-batch-a-d-vm-evidence\operation-artifacts\report-export\report.html

## RunBatch Result

```text
RunBatch completed
Job: 9614d6ab-29b3-48bd-8ba4-20eec8cb654e
Batch MR assertions: total=4; passed=4; failed=0; cancelled=0; pending=0
All completed MR assertions passed.
minmr-toy-sort-permutation: Succeeded
minmr-p1-heat-alpha-monotonic: Succeeded
minmr-p1-heat-timestep-convergence: Succeeded
minmr-p1-heat-mesh-convergence: Succeeded
```

## Screenshots

- `01-async-page-ready.png`
- `02-import-batch-a-toy-succeeded.png`
- `03-import-batch-a-p1-succeeded.png`
- `04-import-batch-d-sciml-succeeded.png`
- `05-runbatch-batch-a-four-mrs-succeeded.png`
- `06-export-batch-a-toy-roundtrip-succeeded.png`
- `07-export-batch-a-p1-roundtrip-succeeded.png`
- `08-export-batch-d-sciml-roundtrip-succeeded.png`
- `09-result-dashboard-visible.png`
- `10-coverage-dashboard-visible.png`
- `11-anomaly-page-visible.png`
- `12-export-report-succeeded.png`

## Notes

- Result, Coverage, and Anomalies pages were opened and captured after Batch A/D async jobs.
- ExportReport generated `report.html` for the latest Batch A minmr execution.
- Batch D remains imported-only by design; the VM check verifies import/export visibility and artifact preservation, not live MGN replay.
