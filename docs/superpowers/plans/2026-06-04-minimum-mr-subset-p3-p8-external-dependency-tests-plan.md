# Minimum-MR-SubSet P3/P8 External Dependency Tests Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add honest, environment-gated supplementary tests for the B-group P3/P8 external `minimum-mr-subset` source path, checking NumPy/SciPy/PyTest prerequisites before executing P3/P8 smoke tests.

**Architecture:** Keep existing MetBench-owned pure-stdlib P3/P8 runtime tests unchanged. Add a separate external-source smoke test surface that resolves an external `Minimum-MR-SubSet` checkout and Python interpreter, checks `numpy`, `scipy`, and `pytest`, and then runs external smoke commands. Missing dependencies must skip or block with explicit reasons; they must not be reported as passing evidence.

**Tech Stack:** .NET 8, xUnit + Xunit.SkippableFact, Python, NumPy, SciPy, PyTest, external `Minimum-MR-SubSet` checkout.

---

## Evidence Basis

- Current `origin/main` when this plan was written: `4d41b4e61805c79427e7a777b1bb7ee8d9d93b75` (PR #291 merge commit).
- B-group import/runtime promotion is already Controlled for MetBench-owned runtime slices.
- Existing evidence explicitly does **not** claim external P3/P8 NumPy/SciPy smoke success.
- External source expected by prior evidence: `Minimum-MR-SubSet` commit `b931b5f74d0f3f3cb704cd6fedbcb4f523cccd7f`.
- P3 source path: `experiments/puts/p3_lorenz.py`; requires NumPy and SciPy.
- P8 source path: `experiments/puts/p8_schrodinger.py`; requires NumPy.
- Shared external smoke command previously attempted: `python -m pytest tests/puts/test_smoke.py -q`; prior blocker was `No module named pytest`.

## File Structure

- Create `MetBench_SystemMT.Tests/SystemMT/ImportExport/MinimumMrSubsetExternalTestPaths.cs`
  - Resolves external source root and Python interpreter.
  - Checks `numpy`, `scipy`, and `pytest` importability.
  - Runs external commands with timeout and captured stdout/stderr.
- Create `MetBench_SystemMT.Tests/SystemMT/ImportExport/MinimumMrSubsetBGroupExternalSourceSmokeTests.cs`
  - Skippable external-source tests for P3/P8 prerequisites and smoke execution.
  - Must not depend on MetBench-owned `SUT/minimum_mr_subset_p3` or `SUT/minimum_mr_subset_p8` assets.
- Modify `docs/superpowers/specs/2026-06-04-minimum-mr-subset-b-group-two-stage-runtime-promotion.md`
  - Append the new external-source result only after commands actually run.
  - If prerequisites are missing, record `BLOCKED` with exact missing module/source path.
- Do not modify WPF, `MetBench_Client/`, async runtime code, typed catalog runtime semantics, or live P3/P8 runtime assets unless a failing test proves a generic issue.

## Task 0: Preflight And Branch Hygiene

**Files:** none.

- [ ] **Step 1: Confirm repository state**

Run on Linux/macOS:

```bash
rtk git status --short --branch
rtk git rev-parse HEAD origin/main
```

Run on Windows VM if `rtk` is unavailable:

```powershell
git status --short --branch
git rev-parse HEAD origin/main
```

Expected:

- Current branch is the assigned task branch.
- Worktree is clean before edits.
- `HEAD` is based on current `origin/main`.

- [ ] **Step 2: Read required context**

Read:

```text
AGENTS.md
CLAUDE.md
docs/status/current.md
docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md
docs/superpowers/specs/2026-06-04-minimum-mr-subset-b-group-two-stage-runtime-promotion.md
MetBench_SystemMT.Tests/SystemMT/ImportExport/BGroupPutImportExportTests.cs
MetBench_SystemMT.Tests/SystemMT/Launcher/LauncherEndToEndMinimumMrSubsetBGroupTests.cs
MetBench_SystemMT.Tests/SystemMT/Jobs/MinimumMrSubsetBGroupAsyncJobTests.cs
MetBench_SystemMT.Tests/SystemMT/Launcher/LauncherEndToEndScipyIvpLotkaVolterraTests.cs
MetBench_SystemMT.Tests/SystemMT/ScipyTestPaths.cs
```

Expected:

- Confirm existing B-group MetBench-owned tests already cover import/export, launcher, and async job pipeline.
- Confirm this plan only adds external-source dependency/smoke evidence.

## Task 1: Check External Source And Python Prerequisites

**Files:** none.

- [ ] **Step 1: Resolve external source root**

Linux/macOS candidates:

```bash
rtk test -d "$MINIMUM_MR_SUBSET_ROOT"
rtk test -d /private/tmp/minimum-mr-subset
rtk test -d /private/tmp/Minimum-MR-SubSet
rtk test -d /tmp/minimum-mr-subset
rtk test -d /tmp/Minimum-MR-SubSet
```

Windows PowerShell candidates:

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

Expected:

- If no root exists, stop external smoke execution and report `ExternalSourceCanonicalRun = BLOCKED: source root not found`.
- Do not invent source evidence from MetBench `SUT/` files.

- [ ] **Step 2: Verify external source facts**

Linux/macOS:

```bash
rtk git -C "$MINIMUM_MR_SUBSET_ROOT" rev-parse HEAD
rtk git -C "$MINIMUM_MR_SUBSET_ROOT" status --short --branch
rtk test -f "$MINIMUM_MR_SUBSET_ROOT/experiments/puts/p3_lorenz.py"
rtk test -f "$MINIMUM_MR_SUBSET_ROOT/experiments/puts/p8_schrodinger.py"
rtk test -f "$MINIMUM_MR_SUBSET_ROOT/tests/puts/test_smoke.py"
```

Windows PowerShell:

```powershell
git -C $MinimumMrSubsetRoot rev-parse HEAD
git -C $MinimumMrSubsetRoot status --short --branch
Test-Path (Join-Path $MinimumMrSubsetRoot 'experiments\puts\p3_lorenz.py')
Test-Path (Join-Path $MinimumMrSubsetRoot 'experiments\puts\p8_schrodinger.py')
Test-Path (Join-Path $MinimumMrSubsetRoot 'tests\puts\test_smoke.py')
```

Expected:

- Prefer commit `b931b5f74d0f3f3cb704cd6fedbcb4f523cccd7f`.
- If commit differs, continue only if the report records the actual commit and does not reuse old evidence.

- [ ] **Step 3: Resolve Python and check dependency imports**

Linux/macOS:

```bash
rtk python3 -c "import sys; print(sys.executable); print(sys.version)"
rtk python3 -c "import numpy, scipy, pytest; print('numpy=' + numpy.__version__); print('scipy=' + scipy.__version__); print('pytest=' + pytest.__version__)"
```

Windows PowerShell:

```powershell
$Py = $env:MINIMUM_MR_SUBSET_PYTHON
if (-not $Py) { $cmd = Get-Command python -ErrorAction SilentlyContinue; if ($cmd) { $Py = $cmd.Source } }
if (-not $Py) { $cmd = Get-Command py -ErrorAction SilentlyContinue; if ($cmd) { $Py = $cmd.Source } }
$Py
& $Py -c "import sys; print(sys.executable); print(sys.version)"
& $Py -c "import numpy, scipy, pytest; print('numpy=' + numpy.__version__); print('scipy=' + scipy.__version__); print('pytest=' + pytest.__version__)"
```

Expected:

- If any import fails, stop external smoke execution and report the exact blocker, for example `No module named pytest`.
- Do not install `numpy`, `scipy`, or `pytest` unless the user explicitly approves dependency installation.

## Task 2: Add Environment-Gated External Source Test Helper

**Files:**

- Create: `MetBench_SystemMT.Tests/SystemMT/ImportExport/MinimumMrSubsetExternalTestPaths.cs`

- [ ] **Step 1: Write the helper**

Create a helper that:

- Resolves root from `MINIMUM_MR_SUBSET_ROOT` first, then known local candidates.
- Resolves Python from `MINIMUM_MR_SUBSET_PYTHON` first, then `python3`, then `python`.
- Checks external source files and `numpy/scipy/pytest`.
- Runs commands with captured stdout/stderr and timeout.

Implementation constraints:

- Use `System.Diagnostics.Process`.
- Use `Xunit.Sdk` nowhere in the helper; keep skip decisions in test files.
- Do not mutate the external source tree.

- [ ] **Step 2: Run a compile-focused test command**

Run:

```bash
rtk dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~MinimumMrSubsetExternal" --logger "console;verbosity=minimal"
```

Windows PowerShell:

```powershell
dotnet test MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~MinimumMrSubsetExternal" --logger "console;verbosity=minimal"
```

Expected:

- Initially no tests may match until Task 3 creates the test class.
- If compilation fails, fix only the helper/test code.

## Task 3: Add P3/P8 External Smoke Tests

**Files:**

- Create: `MetBench_SystemMT.Tests/SystemMT/ImportExport/MinimumMrSubsetBGroupExternalSourceSmokeTests.cs`

- [ ] **Step 1: Write external prerequisite test**

Add a `[SkippableFact]` named:

```csharp
External_source_prerequisites_for_P3_P8_are_available
```

It must:

- Skip if external root is missing.
- Skip if `p3_lorenz.py`, `p8_schrodinger.py`, or `tests/puts/test_smoke.py` is missing.
- Skip if `numpy`, `scipy`, or `pytest` is not importable.
- Write the external commit and dependency versions to test output.

- [ ] **Step 2: Write PyTest smoke test**

Add a `[SkippableFact]` named:

```csharp
External_pytest_put_smoke_runs_after_prerequisites_are_available
```

It must run:

```text
python -m pytest tests/puts/test_smoke.py -q
```

from the external source root.

Expected:

- PASS only when PyTest exits `0`.
- Skip only for missing root/files/dependencies.
- Failure output must include stdout and stderr.

- [ ] **Step 3: Write direct P3/P8 smoke test**

Add a `[SkippableFact]` named:

```csharp
External_P3_P8_run_canonical_outputs_expected_observables
```

It must execute a temporary Python script from the external root that imports:

```text
experiments/puts/p3_lorenz.py
experiments/puts/p8_schrodinger.py
```

and verifies:

- P3 output contains `t`, `trajectory`, and `centroid`.
- P8 output contains `x`, `probability_density`, and `norm`.
- Observables are non-empty and finite.

If the external `run_canonical()` shape differs from the prior documented contract, stop and report the actual shape before changing assertions.

- [ ] **Step 4: Run the new tests**

Run:

```bash
rtk dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~MinimumMrSubsetBGroupExternalSourceSmokeTests" --logger "console;verbosity=minimal"
```

Windows PowerShell:

```powershell
dotnet test MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~MinimumMrSubsetBGroupExternalSourceSmokeTests" --logger "console;verbosity=minimal"
```

Expected:

- If prerequisites exist: all new facts pass.
- If prerequisites are missing: tests skip with exact skip reasons; final report must say external canonical run remains blocked.
- No failed assertions should be converted to skips.

## Task 4: Re-run Existing B-group And Async Tests

**Files:** no new files unless Task 3 proves a real issue.

- [ ] **Step 1: Run existing B-group staging tests**

Run:

```bash
rtk dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~BGroupPutImportExportTests" --logger "console;verbosity=minimal"
```

Windows PowerShell:

```powershell
dotnet test MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~BGroupPutImportExportTests" --logger "console;verbosity=minimal"
```

Expected:

- Existing B-group import/export tests remain green.

- [ ] **Step 2: Run existing B-group launcher and async tests**

Run:

```bash
rtk dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~LauncherEndToEndMinimumMrSubsetBGroupTests|FullyQualifiedName~MinimumMrSubsetBGroupAsyncJobTests" --logger "console;verbosity=minimal"
```

Windows PowerShell:

```powershell
dotnet test MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~LauncherEndToEndMinimumMrSubsetBGroupTests|FullyQualifiedName~MinimumMrSubsetBGroupAsyncJobTests" --logger "console;verbosity=minimal"
```

Expected:

- Existing MetBench-owned runtime and async tests remain green.

## Task 5: Update Evidence Honestly

**Files:**

- Modify: `docs/superpowers/specs/2026-06-04-minimum-mr-subset-b-group-two-stage-runtime-promotion.md`

- [ ] **Step 1: Record external prerequisite result**

If prerequisites pass, append:

```text
ExternalPrerequisites: PASS
Python: <path and version>
numpy: <version>
scipy: <version>
pytest: <version>
External commit: <actual commit>
```

If prerequisites fail, append:

```text
ExternalPrerequisites: BLOCKED
Blocker: <exact missing dependency/path>
ExternalSourceCanonicalRun remains not claimed.
```

- [ ] **Step 2: Record external smoke result**

Only if the external PyTest and direct P3/P8 smoke tests pass, append:

```text
ExternalSourceCanonicalRun: PASS for P3/P8 at <commit>
Commands:
- python -m pytest tests/puts/test_smoke.py -q
- dotnet test ... MinimumMrSubsetBGroupExternalSourceSmokeTests
```

If tests skip or fail, do not write `PASS`.

## Task 6: Final Verification And Commit

**Files:** all files modified by Tasks 2-5.

- [ ] **Step 1: Run mechanical checks**

Run:

```bash
rtk git diff --check
rtk git status --short
```

Windows PowerShell:

```powershell
git diff --check
git status --short
```

Expected:

- `git diff --check` exits `0`.
- Only files in this plan are modified.

- [ ] **Step 2: Commit**

Run:

```bash
rtk git add MetBench_SystemMT.Tests/SystemMT/ImportExport/MinimumMrSubsetExternalTestPaths.cs MetBench_SystemMT.Tests/SystemMT/ImportExport/MinimumMrSubsetBGroupExternalSourceSmokeTests.cs docs/superpowers/specs/2026-06-04-minimum-mr-subset-b-group-two-stage-runtime-promotion.md
rtk git commit -m "test(systemmt): add P3 P8 external source smoke gates"
```

Windows PowerShell:

```powershell
git add MetBench_SystemMT.Tests\SystemMT\ImportExport\MinimumMrSubsetExternalTestPaths.cs MetBench_SystemMT.Tests\SystemMT\ImportExport\MinimumMrSubsetBGroupExternalSourceSmokeTests.cs docs\superpowers\specs\2026-06-04-minimum-mr-subset-b-group-two-stage-runtime-promotion.md
git commit -m "test(systemmt): add P3 P8 external source smoke gates"
```

Expected:

- Commit exists.
- Final report separates:
  - external source prerequisites,
  - external source smoke,
  - MetBench-owned B-group runtime/async tests.

## Acceptance Summary

This task is complete only when one of these is true:

1. **PASS path:** P3/P8 external prerequisites are available, PyTest smoke passes, direct P3/P8 smoke passes, existing B-group MetBench tests still pass, evidence doc records actual versions and commit.
2. **BLOCKED path:** Missing dependency/source root is recorded exactly, no external PASS is claimed, and the environment-gated test files are committed so a prepared environment can run them later.

