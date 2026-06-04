# Task — Minimum-MR-SubSet P3/P8 External Dependency Tests

## Operator Instruction

切换到分支 `plan-minimum-mr-subset-p3-p8-dependency-tests`，读取 `docs/superpowers/tasks/2026-06-04-minimum-mr-subset-p3-p8-external-dependency-tests-task.md`，执行任务。

## Role

You are the implementation agent for P3/P8 external-source supplementary tests. Use superpowers when available:

- Use `superpowers:executing-plans` or `superpowers:subagent-driven-development`.
- Use `superpowers:test-driven-development` for test/code changes.
- Use `superpowers:verification-before-completion` before any completion claim.

If the local environment lacks superpowers tooling, follow the same workflow manually.

## Objective

Add and execute supplementary external-source tests for Minimum-MR-SubSet B-group P3/P8:

1. Check external source root.
2. Check Python prerequisites: `numpy`, `scipy`, and `pytest`.
3. Execute external P3/P8 smoke tests only after prerequisites pass.
4. Keep existing MetBench-owned P3/P8 runtime and async tests green.

Do not claim external P3/P8 source execution passed unless the commands actually pass in this task.

## Required Reading

Read these first:

```text
AGENTS.md
CLAUDE.md
docs/status/current.md
docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md
docs/superpowers/plans/2026-06-04-minimum-mr-subset-p3-p8-external-dependency-tests-plan.md
docs/superpowers/specs/2026-06-04-minimum-mr-subset-b-group-two-stage-runtime-promotion.md
MetBench_SystemMT.Tests/SystemMT/ImportExport/BGroupPutImportExportTests.cs
MetBench_SystemMT.Tests/SystemMT/Launcher/LauncherEndToEndMinimumMrSubsetBGroupTests.cs
MetBench_SystemMT.Tests/SystemMT/Jobs/MinimumMrSubsetBGroupAsyncJobTests.cs
MetBench_SystemMT.Tests/SystemMT/Launcher/LauncherEndToEndScipyIvpLotkaVolterraTests.cs
MetBench_SystemMT.Tests/SystemMT/ScipyTestPaths.cs
```

## Hard Scope

Allowed:

- `MetBench_SystemMT.Tests/SystemMT/ImportExport/`
- `docs/superpowers/specs/2026-06-04-minimum-mr-subset-b-group-two-stage-runtime-promotion.md`

Forbidden unless a failing test proves a generic issue:

- `MetBench_BLL.Core/SystemMT/Jobs/`
- `MetBench_BLL.Core/SystemMT/Launcher/`
- `MetBench_BLL.Core/SystemMT/Catalog/Typed/`
- `SUT/minimum_mr_subset_p3/`
- `SUT/minimum_mr_subset_p8/`
- `.github/governance/expected-catalog-counts.txt`
- `MetBench_Client/`
- WPF/XAML

Do not install `numpy`, `scipy`, or `pytest` unless the user explicitly approves dependency installation. Missing dependencies are a blocker or skip reason, not a failure to hide.

## Execute The Plan

Follow:

```text
docs/superpowers/plans/2026-06-04-minimum-mr-subset-p3-p8-external-dependency-tests-plan.md
```

Minimum command sequence:

### 1. Resolve External Source Root

Windows PowerShell:

```powershell
$RootCandidates = @(
  $env:MINIMUM_MR_SUBSET_ROOT,
  'C:\tmp\Minimum-MR-SubSet',
  'C:\tmp\minimum-mr-subset',
  'C:\Users\limeng\Codes\Minimum-MR-SubSet',
  'C:\Users\limeng\Codes\minimum-mr-subset',
  'D:\Codes\Minimum-MR-SubSet'
) | Where-Object { $_ -and (Test-Path $_) }
$MinimumMrSubsetRoot = $RootCandidates | Select-Object -First 1
$MinimumMrSubsetRoot
```

Linux/macOS with `rtk`:

```bash
rtk test -d "$MINIMUM_MR_SUBSET_ROOT"
rtk test -d /private/tmp/minimum-mr-subset
rtk test -d /private/tmp/Minimum-MR-SubSet
rtk test -d /tmp/minimum-mr-subset
rtk test -d /tmp/Minimum-MR-SubSet
```

If no root is found, stop external smoke and report:

```text
ExternalSourceCanonicalRun: BLOCKED
Blocker: Minimum-MR-SubSet source root not found.
```

### 2. Verify Source Files And Commit

Windows PowerShell:

```powershell
git -C $MinimumMrSubsetRoot rev-parse HEAD
git -C $MinimumMrSubsetRoot status --short --branch
Test-Path (Join-Path $MinimumMrSubsetRoot 'experiments\puts\p3_lorenz.py')
Test-Path (Join-Path $MinimumMrSubsetRoot 'experiments\puts\p8_schrodinger.py')
Test-Path (Join-Path $MinimumMrSubsetRoot 'tests\puts\test_smoke.py')
```

Expected preferred commit:

```text
b931b5f74d0f3f3cb704cd6fedbcb4f523cccd7f
```

If the commit differs, continue only with the actual commit recorded in the final report.

### 3. Check Python Dependencies

Windows PowerShell:

```powershell
$Py = $env:MINIMUM_MR_SUBSET_PYTHON
if (-not $Py) { $cmd = Get-Command python -ErrorAction SilentlyContinue; if ($cmd) { $Py = $cmd.Source } }
if (-not $Py) { $cmd = Get-Command py -ErrorAction SilentlyContinue; if ($cmd) { $Py = $cmd.Source } }
$Py
& $Py -c "import sys; print(sys.executable); print(sys.version)"
& $Py -c "import numpy, scipy, pytest; print('numpy=' + numpy.__version__); print('scipy=' + scipy.__version__); print('pytest=' + pytest.__version__)"
```

Linux/macOS with `rtk`:

```bash
rtk python3 -c "import sys; print(sys.executable); print(sys.version)"
rtk python3 -c "import numpy, scipy, pytest; print('numpy=' + numpy.__version__); print('scipy=' + scipy.__version__); print('pytest=' + pytest.__version__)"
```

If any dependency is missing, stop external smoke and report the exact missing module.

### 4. Implement Environment-Gated Tests

Create:

```text
MetBench_SystemMT.Tests/SystemMT/ImportExport/MinimumMrSubsetExternalTestPaths.cs
MetBench_SystemMT.Tests/SystemMT/ImportExport/MinimumMrSubsetBGroupExternalSourceSmokeTests.cs
```

Required tests:

```text
External_source_prerequisites_for_P3_P8_are_available
External_pytest_put_smoke_runs_after_prerequisites_are_available
External_P3_P8_run_canonical_outputs_expected_observables
```

Use `[SkippableFact]` and explicit skip reasons for missing source root, missing source files, or missing `numpy/scipy/pytest`.

### 5. Run Tests

Windows PowerShell:

```powershell
dotnet test MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~MinimumMrSubsetBGroupExternalSourceSmokeTests" --logger "console;verbosity=minimal"
dotnet test MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~BGroupPutImportExportTests" --logger "console;verbosity=minimal"
dotnet test MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~LauncherEndToEndMinimumMrSubsetBGroupTests|FullyQualifiedName~MinimumMrSubsetBGroupAsyncJobTests" --logger "console;verbosity=minimal"
git diff --check
```

Linux/macOS with `rtk`:

```bash
rtk dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~MinimumMrSubsetBGroupExternalSourceSmokeTests" --logger "console;verbosity=minimal"
rtk dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~BGroupPutImportExportTests" --logger "console;verbosity=minimal"
rtk dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~LauncherEndToEndMinimumMrSubsetBGroupTests|FullyQualifiedName~MinimumMrSubsetBGroupAsyncJobTests" --logger "console;verbosity=minimal"
rtk git diff --check
```

### 6. Update Evidence And Commit

Update:

```text
docs/superpowers/specs/2026-06-04-minimum-mr-subset-b-group-two-stage-runtime-promotion.md
```

Record either:

```text
ExternalSourceCanonicalRun: PASS for P3/P8
```

or:

```text
ExternalSourceCanonicalRun: BLOCKED
```

with exact command evidence.

Commit:

```powershell
git add MetBench_SystemMT.Tests\SystemMT\ImportExport\MinimumMrSubsetExternalTestPaths.cs MetBench_SystemMT.Tests\SystemMT\ImportExport\MinimumMrSubsetBGroupExternalSourceSmokeTests.cs docs\superpowers\specs\2026-06-04-minimum-mr-subset-b-group-two-stage-runtime-promotion.md
git commit -m "test(systemmt): add P3 P8 external source smoke gates"
```

If using `rtk`:

```bash
rtk git add MetBench_SystemMT.Tests/SystemMT/ImportExport/MinimumMrSubsetExternalTestPaths.cs MetBench_SystemMT.Tests/SystemMT/ImportExport/MinimumMrSubsetBGroupExternalSourceSmokeTests.cs docs/superpowers/specs/2026-06-04-minimum-mr-subset-b-group-two-stage-runtime-promotion.md
rtk git commit -m "test(systemmt): add P3 P8 external source smoke gates"
```

## Final Report Requirements

Report:

- branch and commit SHA;
- external source root and commit;
- Python executable and versions of NumPy/SciPy/PyTest;
- exact test commands and pass/fail/skip counts;
- whether `ExternalSourceCanonicalRun` is `PASS` or `BLOCKED`;
- whether existing B-group import/export, launcher, and async tests remained green;
- any files changed outside the allowed scope.

Never report missing dependencies as PASS.

