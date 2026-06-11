# VM Prompt: Batch A/D External MR Assets UI Acceptance

## Preconditions

- Worktree branch: `codex/external-mr-asset-acceptance-plan`.
- Confirm the plan file exists:
  `docs/superpowers/plans/2026-06-11-external-mr-assets-metbench-acceptance-import-plan.md`.
- Confirm the acceptance SUT files exist under:
  `SUT/external_acceptance_minmr/`, including `acceptance-catalog.json`.
- Use Windows PowerShell from the repository root.

## Commands

```powershell
git status --short --branch
dotnet build MetBench.sln --no-restore
dotnet test MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj --no-restore --filter FullyQualifiedName~ExternalMrAcceptanceCompletionTests
dotnet test MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj --no-restore --filter FullyQualifiedName~ExternalMrAcceptanceBatchImportTests
dotnet test MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj --no-restore --filter FullyQualifiedName~SystemMT.ImportExport
```

## UI Steps

1. Start the WPF client.
2. Open the System MT async/import-export surface.
3. Submit `ImportAssets` for:
   - `metbench-import-minmr-toy-classic-v1`
   - `metbench-import-minmr-p1-heat-v1`
   - `metbench-import-sciml-domain-validity-fixture-v1`
4. Confirm each job reaches `Succeeded` and exposes a staged artifact path
   containing `staging-manifest.json` and `sut-import-unit.json`.
5. Run the Batch A acceptance MRs through RunBatch or equivalent MR execution:
   - `minmr-toy-sort-permutation`
   - `minmr-p1-heat-alpha-monotonic`
   - `minmr-p1-heat-timestep-convergence`
   - `minmr-p1-heat-mesh-convergence`
6. Submit `ExportAssets` for the staged packages and confirm valid exported
   `sut-import-unit.json` files.
7. Open report/dashboard/anomaly views and verify:
   - Batch A shows four successful local acceptance executions.
   - Batch A P1 detection evidence is visible as imported anomaly candidates,
     not fresh runtime failures.
   - Batch D SciML evidence shows 30 detection records, 5 detected records, and
     the one-SUT / one-checkpoint limitation.
   - `mgn-discrete-divergence-boundedness` is shown as deferred/diagnostic, not
     an absolute pass/fail MR.

## Screenshots / Logs To Collect

- `01-import-assets-batch-a-toy-succeeded.png`
- `02-import-assets-batch-a-p1-succeeded.png`
- `03-import-assets-batch-d-sciml-succeeded.png`
- `04-runbatch-batch-a-four-mrs-succeeded.png`
- `05-export-assets-roundtrip-succeeded.png`
- `06-report-batch-a-d-evidence.png`
- `07-dashboard-batch-a-d-counts.png`
- `08-anomaly-imported-evidence-limitations.png`
- PowerShell transcript with build/test command outputs.

## Pass Criteria

- WPF build exits 0 errors.
- Both focused A/D test classes pass.
- `SystemMT.ImportExport` regression passes, allowing only explicit
  environment-gated skips already present in the suite.
- All three import jobs and export jobs reach `Succeeded`.
- The four Batch A acceptance MRs pass through the System MT execution path.
- Report/dashboard/anomaly views visibly preserve imported-evidence limitations.

## Fail Criteria

- Any WPF build error.
- Any Batch A/D focused test failure.
- Any import/export job reaches `Failed`.
- A Batch D diagnostic/deferred MR is displayed as an absolute pass/fail
  conservation verdict.
- Imported detection evidence is displayed as a fresh MetBench runtime failure.
