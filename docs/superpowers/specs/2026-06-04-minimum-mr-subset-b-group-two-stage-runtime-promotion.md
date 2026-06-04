# Minimum-MR-SubSet B-Group Two-Stage Runtime Promotion Evidence

> Date: 2026-06-04
> Branch: `codex/minimum-mr-subset-b-group-two-stage-plan`
> Scope: P3 Lorenz and P8 Schrodinger only.

## Classification

| Category | Result |
|---|---|
| AGroupImportExportStaging | Unchanged. Existing P4/P5/P9 staging and runtime promotion remain the prior A-group evidence. |
| ExternalSourceCanonicalRun | Not claimed for P3/P8 in this branch. The task's `/private/tmp/minimum-mr-subset` path was not present on this Windows VM; equivalent local checkout `C:\tmp\Minimum-MR-SubSet` was read at commit `b931b5f74d0f3f3cb704cd6fedbcb4f523cccd7f`. P3/P8 source files and observables were inspected. External smoke attempt `python -m pytest tests/puts/test_smoke.py -q` was blocked by `No module named pytest`; no external NumPy/SciPy smoke success is claimed. |
| MetBenchLauncherRuntimeRun | Controlled for the MetBench-owned runtime slices: `p3-trajectory-sensitivity` and `p8-norm-conservation` pass direct launcher E2E tests. |
| MetBenchAsyncJobRuntimeRun | Controlled: both promoted MR IDs pass through `SystemMtJobService -> ChannelJobQueue/InMemoryJobStore -> SystemMtJobWorker -> SystemMtAsyncPipeline -> ISystemMtLauncher`, ending in `Succeeded` with persisted `MrRunResult`. |
| PromotedLiveMrs | `p3-trajectory-sensitivity`, `p8-norm-conservation`. |
| NotPromotedMrs | None from B-group scope. P1/P2/P6/P7/P10 remain out of scope. |
| Core typed runtime changes | None. No `MetBench_BLL.Core/SystemMT/Catalog/Typed/*` public predicate/runtime semantics were changed. |

## Promotion Shape

| PUT | Live SUT | MR ID | Transform target | Assertion | Value | Relation |
|---|---|---|---|---|---|---|
| P3 | `minimum-mr-subset-p3` | `p3-trajectory-sensitivity` | `/initial/perturbation` | `greater` / `GreaterThan` | `separation` | Doubling the initial perturbation increases final trajectory separation in the deterministic Lorenz runtime slice. |
| P8 | `minimum-mr-subset-p8` | `p8-norm-conservation` | `/solver/time_steps` | `less` / `LessThan` | `norm_drift` | Doubling propagation steps reduces norm drift in the deterministic Schrodinger runtime slice. |

## Stage 1 Import-Only Evidence

Stage 1 commit: `61afaac feat(systemmt): stage minimum MR subset B group imports`.

- Adds `BGroupPutFixtures.Create("P3")` and `Create("P8")`.
- P3 observables: `t`, `trajectory`, `centroid`.
- P8 observables: `x`, `probability_density`, `norm`.
- Mutation entries are `OperatorClassOnly`.
- Detection entries are `DetectionResult.Inconclusive`; no real P3/P8 detection matrix was observed.
- Compatibility remains `ImportedOnly`.
- Stage 1 did not add live `SUT/minimum_mr_subset_p3*` or `SUT/minimum_mr_subset_p8*` assets and did not change the runtime count whitelist.

## Stage 2 Runtime Evidence

Stage 2 adds pure-stdlib MetBench runtime slices under:

- `SUT/minimum_mr_subset_p3/`
- `SUT/minimum_mr_subset_p8/`

These runners are MetBench-owned runtime slices derived from the imported PUT semantics. They are not evidence that the external NumPy/SciPy adapters executed locally.

## Verification

Commands run with native PowerShell/dotnet/git because `rtk` was unavailable in this VM and the operator instructed to ignore `rtk`.

| Command | Result |
|---|---|
| `dotnet test MetBench_SystemMT.Tests --no-restore --filter "FullyQualifiedName~BGroupPutImportExportTests"` before fixture implementation | Expected RED: compile failure because `BGroupPutFixtures` did not exist. |
| `dotnet test MetBench_SystemMT.Tests --no-restore --filter "FullyQualifiedName~AGroupPutImportExportTests\|FullyQualifiedName~BGroupPutImportExportTests"` | 37 passed, 0 failed, 0 skipped. |
| `dotnet test MetBench_SystemMT.Tests --no-restore --filter "FullyQualifiedName~LauncherEndToEndMinimumMrSubsetBGroupTests"` before runtime assets | Expected RED: unknown MR IDs, then missing copied TestAssets before csproj asset wiring. |
| `dotnet test MetBench_SystemMT.Tests --no-restore --filter "FullyQualifiedName~LauncherEndToEndMinimumMrSubsetBGroupTests"` after runtime assets | 4 passed, 0 failed, 0 skipped. |
| `dotnet test MetBench_SystemMT.Tests --no-restore --filter "FullyQualifiedName~MinimumMrSubsetBGroupAsyncJobTests"` | 2 passed, 0 failed, 0 skipped. |
| `dotnet test MetBench_SystemMT.Tests --no-restore --filter "FullyQualifiedName~MinimumMrSubset"` | 9 passed, 0 failed, 0 skipped. |
| `dotnet test MetBench_SystemMT.Tests --no-restore --filter "SemanticCatalogBoundaryTests"` | 3 passed, 0 failed, 0 skipped. |
| `dotnet test MetBench_SystemMT.Tests --no-restore --filter "Catalog_MR_id_set_equals_governance_whitelist"` | 1 passed, 0 failed, 0 skipped. |
| `python -m pytest tests/puts/test_smoke.py -q` from `C:\tmp\Minimum-MR-SubSet` | Blocked: `No module named pytest`. External P3/P8 smoke is not claimed. |
| `dotnet restore MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj` | Initial sandbox run failed with NU1301 due blocked `api.nuget.org`; rerun with approved network escalation succeeded. |
| `dotnet test MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~AGroupPutImportExportTests\|FullyQualifiedName~BGroupPutImportExportTests"` | 37 passed, 0 failed, 0 skipped. |
| `dotnet test MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~MinimumMrSubset"` | 9 passed, 0 failed, 0 skipped. |
| `dotnet test MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~SystemMtJob\|FullyQualifiedName~MinimumMrSubsetBGroupAsync"` | 35 passed, 0 failed, 0 skipped. |
| `dotnet test MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj --no-restore --filter "SemanticCatalogBoundaryTests\|Catalog_MR_id_set_equals_governance_whitelist"` | 4 passed, 0 failed, 0 skipped. |
| `dotnet build MetBench.sln` | 0 errors; existing warnings only. |
| `git diff --check` | Exit 0; LF-to-CRLF warnings only. |

Note: Four focused `dotnet test` commands were first attempted in parallel and failed with CS2012 file locks on Windows `obj/bin` outputs. The residual test processes from that attempt were stopped, and the same commands were rerun sequentially with the passing results above.
