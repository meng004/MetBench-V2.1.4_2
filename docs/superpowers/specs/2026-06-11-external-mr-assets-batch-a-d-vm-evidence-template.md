# External MR Assets Batch A/D VM Evidence Template (2026-06-11)

> Fill this file from the Windows VM after running
> `docs/superpowers/vm-prompts/2026-06-11-batch-a-d-external-mr-assets-ui-acceptance-vm-prompt.md`.
> Do not use this template as evidence until every placeholder below is replaced
> with concrete command output, screenshot paths, and pass/fail conclusions.

## VM Context

- Branch:
- Commit:
- Windows version:
- .NET SDK:
- Operator:
- Run timestamp:

## Command Evidence

```powershell
git status --short --branch
```

Result:

```powershell
dotnet build MetBench.sln --no-restore
```

Result:

```powershell
dotnet test MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj --no-restore --filter FullyQualifiedName~ExternalMrAcceptanceCompletionTests
```

Result:

```powershell
dotnet test MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj --no-restore --filter FullyQualifiedName~ExternalMrAcceptanceBatchImportTests
```

Result:

```powershell
dotnet test MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj --no-restore --filter FullyQualifiedName~SystemMT.ImportExport
```

Result:

## UI Evidence

| Required artifact | Captured path | Result |
|---|---|---|
| `01-import-assets-batch-a-toy-succeeded.png` |  |  |
| `02-import-assets-batch-a-p1-succeeded.png` |  |  |
| `03-import-assets-batch-d-sciml-succeeded.png` |  |  |
| `04-runbatch-batch-a-four-mrs-succeeded.png` |  |  |
| `05-export-assets-roundtrip-succeeded.png` |  |  |
| `06-report-batch-a-d-evidence.png` |  |  |
| `07-dashboard-batch-a-d-counts.png` |  |  |
| `08-anomaly-imported-evidence-limitations.png` |  |  |

## Pass/Fail Checklist

| Criterion | Evidence | Result |
|---|---|---|
| WPF build exits 0 errors |  |  |
| Focused Batch A/D tests pass |  |  |
| `SystemMT.ImportExport` passes with only existing explicit environment-gated skips |  |  |
| Three `ImportAssets` jobs reach `Succeeded` |  |  |
| Export jobs reach `Succeeded` with valid `sut-import-unit.json` output |  |  |
| Four Batch A acceptance MRs pass through System MT execution |  |  |
| Report/dashboard/anomaly views preserve imported-evidence limitations |  |  |
| `mgn-discrete-divergence-boundedness` is displayed as deferred/diagnostic, not an absolute pass/fail verdict |  |  |

## Conclusion

- Verdict:
- Remaining gaps:
- Notes:
