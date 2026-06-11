# External MR Assets Batch A-D VM Evidence

Date: 2026-06-11
Worktree: `C:\tmp\metbench-origin-work`
Branch context: detached `origin/work` (`f193ef8` base) with local verification fixes.

## Batch Status

| Batch | VM task | Result | Evidence |
|---|---|---|---|
| A | Import toy + P1 heat packages, run 4 acceptance MRs, export packages | PASS | `01-*`, `02-*`, `06-runbatch-batch-a-four-mrs-succeeded.png`, `08-export-assets-batch-a-*` |
| B | Import existing-runtime reconciliation package, verify export path | PASS | `03-import-assets-batch-b-succeeded.png`, `08-export-assets-batch-b-existing-runtime-succeeded.png` |
| C | Import local remaining package, run 4 acceptance MRs, export package | PASS | `04-import-assets-batch-c-succeeded.png`, `07-runbatch-batch-c-four-mrs-succeeded.png`, `08-export-assets-batch-c-local-remaining-succeeded.png` |
| D | Import SciML seeded-fault package, verify export path and limitation visibility page reachability | PASS | `05-import-assets-batch-d-sciml-succeeded.png`, `08-export-assets-batch-d-sciml-succeeded.png`, `11-anomaly-imported-evidence-limitations.png` |

## WPF UIA Flow

Command:

```powershell
.\tools\smokeshot\bin\Debug\net8.0-windows\smokeshot.exe external-a-d --out docs\superpowers\specs\2026-06-11-external-mr-assets-batch-a-d-vm-evidence
```

Result: exit code 0.

Precondition for WPF runtime visibility:

```powershell
$env:METBENCH_EXTRA_MR_MANIFESTS="external_acceptance_minmr\acceptance-catalog.json"
```

Observed terminal states from `batch-a-d-vm-transcript.txt`:

- `ImportAssets`: 5/5 `Succeeded`
- Batch A `RunBatch`: 4/4 MR items `Succeeded`
- Batch C `RunBatch`: 4/4 MR items `Succeeded`
- `ExportAssets`: 5/5 `Succeeded`
- Report, Coverage, and Anomalies pages were reachable and captured.

## Fixes Applied During VM Verification

- WPF DI now supports explicit `METBENCH_EXTRA_MR_MANIFESTS` injection for acceptance-only MR manifests, so `SUT/external_acceptance_minmr/acceptance-catalog.json` can be visible to the WPF launcher path without changing the default runtime catalog inventory.
- Added `smokeshot external-a-d` to make the VM acceptance flow repeatable.

## Verification

```powershell
dotnet build MetBench.sln --no-restore
```

Result: pass, 0 errors, 6 existing NU1701 warnings.

```powershell
dotnet test MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~ExternalMrAcceptanceCompletionTests|FullyQualifiedName~ExternalMrAcceptanceBatchImportTests" --logger "console;verbosity=minimal"
```

Result: 22 passed / 0 failed / 0 skipped.

```powershell
dotnet test MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~Catalog_MR_id_set_equals_governance_whitelist|FullyQualifiedName~CatalogParityTests|FullyQualifiedName~ManifestMrCatalogProviderTests" --logger "console;verbosity=minimal"
```

Result: 31 passed / 0 failed / 0 skipped.

```powershell
dotnet test MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~SystemMT.ImportExport" --logger "console;verbosity=minimal"
```

Result: 67 passed / 0 failed / 3 environment-gated skips.

```powershell
git diff --check
```

Result: pass; only CRLF conversion warnings.
