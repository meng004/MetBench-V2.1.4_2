# Minimum-MR-SubSet B-Group Two-Stage Runtime Promotion Evidence

> Date: 2026-06-04
> Branch: `codex/minimum-mr-subset-b-group-two-stage-plan`
> Scope: P3 Lorenz and P8 Schrodinger only.

## Classification

| Category | Result |
|---|---|
| AGroupImportExportStaging | Unchanged. Existing P4/P5/P9 staging and runtime promotion remain the prior A-group evidence. |
| ExternalSourceCanonicalRun | BLOCKED / not claimed for P3/P8. The task's `/private/tmp/minimum-mr-subset` path was not present on this Windows VM; equivalent local checkout `C:\tmp\Minimum-MR-SubSet` was read at commit `b931b5f74d0f3f3cb704cd6fedbcb4f523cccd7f`. After installing `pytest`, external prerequisites are importable, but P8 fails in the external source because `p8_schrodinger.py` calls removed NumPy API `np.trapz`; the shared `python -m pytest tests/puts/test_smoke.py -q` command also fails/times out after printing `.F...F.F..`. No external P3/P8 smoke success is claimed. |
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

P3 remains a trajectory-sensitivity stress case at the SUT level, but its live catalog governance meta-pattern is `Mono` / `m_mono` because the promoted predicate is monotone: a larger perturbation must produce a larger `separation` observable. This keeps the runtime slice inside the existing `Mono` / `Inv` / `Conv` catalog matrix instead of adding a new meta-pattern category without a separate governance plan.

## Stage 1 Import-Only Evidence

Stage 1 commit: `8ff573a feat(systemmt): stage minimum MR subset B group imports`.

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

## External Dependency Gate Update

Branch: `plan-minimum-mr-subset-p3-p8-dependency-tests`.

ExternalPrerequisites: PASS after installing `pytest` into the task Python environment.

- External root: `C:\tmp\Minimum-MR-SubSet`.
- External commit: `b931b5f74d0f3f3cb704cd6fedbcb4f523cccd7f`.
- External git status: `## HEAD (no branch)`; no tracked working-tree changes were reported.
- Required files present: `experiments\puts\p3_lorenz.py`, `experiments\puts\p8_schrodinger.py`, `tests\puts\test_smoke.py`.
- Python: `C:\Users\codex\AppData\Local\Programs\Python\Python312-arm64\python.exe`; version `3.12.10`.
- Dependency import command `python -c "import numpy, scipy, pytest; print('numpy=' + numpy.__version__); print('scipy=' + scipy.__version__); print('pytest=' + pytest.__version__)"` now passes with `numpy=2.4.6`, `scipy=1.17.1`, `pytest=9.0.3`.
- Blocker: `experiments\puts\p8_schrodinger.py` calls `np.trapz`, but the current NumPy exposes no `trapz` attribute and raises `AttributeError: module 'numpy' has no attribute 'trapz'. Did you mean: 'trace'?`.
- ExternalSourceCanonicalRun remains not claimed. `pytest` was installed only after explicit operator approval.

New environment-gated tests added:

- `External_source_prerequisites_for_P3_P8_are_available`
- `External_pytest_put_smoke_runs_after_prerequisites_are_available`
- `External_P3_P8_run_canonical_outputs_expected_observables`

These tests are `[SkippableFact]` gates. Missing source root, missing source files, unavailable Python, unavailable git commit metadata, or missing `numpy` / `scipy` / `pytest` skip with explicit command/path evidence. They do not use MetBench-owned `SUT/minimum_mr_subset_p3` or `SUT/minimum_mr_subset_p8` assets as external-source evidence.

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
| `dotnet test MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~MinimumMrSubsetBGroupExternalSourceSmokeTests" --logger "console;verbosity=minimal"` | 0 failed, 0 passed, 3 skipped. Blocked by missing `pytest`; external canonical run remains not claimed. |
| `dotnet test MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~BGroupPutImportExportTests" --logger "console;verbosity=minimal"` | 12 passed, 0 failed, 0 skipped. |
| `dotnet test MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~LauncherEndToEndMinimumMrSubsetBGroupTests\|FullyQualifiedName~MinimumMrSubsetBGroupAsyncJobTests" --logger "console;verbosity=minimal"` | 6 passed, 0 failed, 0 skipped. |
| `C:\Users\codex\AppData\Local\Programs\Python\Python312-arm64\python.exe -m pip install pytest` | Installed `pytest==9.0.3` plus `colorama`, `iniconfig`, `packaging`, `pluggy`, and `pygments`. |
| `C:\Users\codex\AppData\Local\Programs\Python\Python312-arm64\python.exe -c "import numpy, scipy, pytest; ..."` | PASS: `numpy=2.4.6`, `scipy=1.17.1`, `pytest=9.0.3`. |
| `C:\Users\codex\AppData\Local\Programs\Python\Python312-arm64\python.exe -m pytest tests/puts/test_smoke.py -q --tb=short` from `C:\tmp\Minimum-MR-SubSet` | Timed out after 300 seconds after printing `.F...F.F..`; full external shared smoke is not claimed. |
| `C:\Users\codex\AppData\Local\Programs\Python\Python312-arm64\python.exe -m pytest "tests/puts/test_smoke.py::test_put_canonical_smoke[p3_lorenz]" -q --tb=short -s` | Timed out after 120 seconds after printing `.`; P3 appeared to reach the pytest pass marker but the pytest process did not exit cleanly, so no PASS is claimed. |
| `C:\Users\codex\AppData\Local\Programs\Python\Python312-arm64\python.exe -m pytest "tests/puts/test_smoke.py::test_put_canonical_smoke[p8_schrodinger]" -q --tb=short -s` | Timed out after 120 seconds after printing `F`; P8 external pytest is not claimed. |
| direct Python `run_canonical()` inspection for P3/P8 | P3 observables were finite and non-empty; P8 raised `AttributeError: module 'numpy' has no attribute 'trapz'` in `p8_schrodinger.py`. |
| `dotnet test MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~MinimumMrSubsetBGroupExternalSourceSmokeTests" --logger "console;verbosity=minimal"` after installing `pytest` | 1 passed, 2 failed, 0 skipped. Prerequisites pass; direct P3/P8 smoke fails on P8 `np.trapz`; shared pytest smoke times out after 120 seconds after printing `.F...F.F..`. |

Note: Four focused `dotnet test` commands were first attempted in parallel and failed with CS2012 file locks on Windows `obj/bin` outputs. The residual test processes from that attempt were stopped, and the same commands were rerun sequentially with the passing results above.
