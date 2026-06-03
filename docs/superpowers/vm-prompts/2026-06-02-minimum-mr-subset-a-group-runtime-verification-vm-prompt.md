# Windows VM Prompt: Minimum-MR-SubSet A-Group Runtime Verification

Use this file as the complete Windows VM task instruction.

User instruction to run this task:

```text
Read docs/superpowers/vm-prompts/2026-06-02-minimum-mr-subset-a-group-runtime-verification-vm-prompt.md and execute the task.
```

## Purpose

Verify what can and cannot run after the first A-group import/export implementation for `minimum-mr-subset` P5, P4, and P9.

This prompt must distinguish three different meanings of "run":

1. **MetBench staging run**: the imported A-group packages validate, export, and re-import on Windows.
2. **External source SUT run**: the original `minimum-mr-subset` P5/P4/P9 `run_canonical()` functions execute and return finite observables.
3. **MetBench launcher MR run**: the imported MRs execute through the live System-MT launcher.

The expected current design is:

- Items 1 and 2 may pass if dependencies are present.
- Item 3 is **not expected to pass yet** because PR #271 intentionally imports A-group assets as staging-only `ImportedOnly` packages and does not promote them into live `SUT/<sut>/catalog.json`, LiteDB, or the System-MT launcher catalog.

Do not report launcher runtime success unless there is actual evidence that the live launcher executed P5/P4/P9 imported MRs.

## Scope

In scope:

- P5 point kinetics import package.
- P4 Hamiltonian pendulum import package.
- P9 OpenMC criticality surrogate import package.
- Windows build/test verification.
- External source smoke for P5/P4/P9 only.
- A clear runtime-readiness conclusion.

Out of scope:

- P8, P3, P10, P1, P2, P6, P7.
- WPF UI changes.
- XAML changes.
- Promoting A-group MRs into the live System-MT catalog.
- Editing `SUT/`, live manifest catalogs, LiteDB repositories, or execution evidence repositories.
- Installing Python dependencies without explicit user approval.

## Required Reading

Read these files first and use them as the truth sources for this task:

1. `AGENTS.md`
2. `docs/status/current.md`
3. `docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md`
4. `docs/superpowers/specs/2026-06-02-minimum-mr-subset-a-group-import-export-design.md`
5. `docs/superpowers/plans/2026-06-02-minimum-mr-subset-a-group-import-export-plan.md`
6. `docs/superpowers/tasks/2026-06-02-minimum-mr-subset-a-group-cloud-linux-task.md`
7. `MetBench_BLL.Core/SystemMT/ImportExport/Put/README.md`
8. `MetBench_SystemMT.Tests/SystemMT/ImportExport/AGroupPutImportExportTests.cs`

## Preconditions

- Work on latest `origin/main` after PR #271 is merged.
- Do not work from a dirty tree.
- If the VM has an `rtk` wrapper and project policy requires it, prefix commands with `rtk`. Otherwise run the PowerShell commands directly and record that `rtk` is unavailable.
- Use PowerShell.

## Task V0: Repository State

**Core steps**

Run:

```powershell
git fetch origin
git checkout main
git pull --ff-only origin main
git status -sb
git rev-parse HEAD
git log -1 --oneline
```

**Acceptance standard**

- Report the exact `HEAD`.
- Report whether the worktree is clean.
- If the worktree is dirty, stop and ask the user whether to stash, switch worktrees, or continue read-only.

## Task V1: Confirm A-Group Implementation Is Present

**Core steps**

Run:

```powershell
Test-Path MetBench_BLL.Core/SystemMT/ImportExport/Put/AGroupPutFixtures.cs
Test-Path MetBench_BLL.Core/SystemMT/ImportExport/Put/SutImportValidator.cs
Test-Path MetBench_BLL.Core/SystemMT/ImportExport/Put/SutImportPackageExporter.cs
Test-Path MetBench_BLL.Core/SystemMT/ImportExport/Put/CompatibilityProfileBuilder.cs
Test-Path MetBench_SystemMT.Tests/SystemMT/ImportExport/AGroupPutImportExportTests.cs
```

Run:

```powershell
Select-String -Path MetBench_BLL.Core/SystemMT/ImportExport/Put/AGroupPutFixtures.cs -Pattern "P5","P4","P9","experiments/puts/p5_pke.py","experiments/puts/p4_pendulum.py","experiments/puts/p9_openmc.py"
```

**Acceptance standard**

- All five `Test-Path` commands return `True`.
- The fixture file references P5, P4, P9, and the three `experiments/puts/*.py` source paths.

## Task V2: Windows Build And Focused Import/Export Tests

**Core steps**

Run:

```powershell
dotnet restore MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj
dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~AGroupPutImportExportTests"
dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --no-restore --filter "SemanticCatalogBoundaryTests"
dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --no-restore --filter "Catalog_MR_id_set_equals_governance_whitelist"
```

Then run the Windows solution build:

```powershell
dotnet build MetBench.sln
```

**Acceptance standard**

- `AGroupPutImportExportTests` passes. Expected current count from PR #271: 25 tests.
- `SemanticCatalogBoundaryTests` passes. Expected current count from PR #271 evidence: 3 tests.
- `Catalog_MR_id_set_equals_governance_whitelist` passes. Expected current count from PR #271 evidence: 1 test.
- `dotnet build MetBench.sln` completes with 0 errors.
- Record exact pass/fail counts and warnings. Do not summarize a failed build as usable.

## Task V3: Verify Imported Packages Are Staging-Only

**Core steps**

Run:

```powershell
Select-String -Path "SUT/**/catalog.json" -Pattern "p5-power-response","p4-energy-invariant","p9-k-eff-noise-aware" -ErrorAction SilentlyContinue
Select-String -Path "SUT/**/catalog.json" -Pattern "minimum-mr-subset-p5","minimum-mr-subset-p4","minimum-mr-subset-p9" -ErrorAction SilentlyContinue
```

Run:

```powershell
Select-String -Path MetBench_BLL.Core/SystemMT/ImportExport/Put/AGroupPutFixtures.cs -Pattern "ImportedOnly"
Select-String -Path MetBench_BLL.Core/SystemMT/ImportExport/Put/CompatibilityProfileBuilder.cs -Pattern "RuntimeCandidate","sigma_k","noise-aware"
```

**Acceptance standard**

- The `SUT/**/catalog.json` searches return no live catalog rows for the imported A-group MR ids or adapter ids.
- The import fixtures show default `ImportedOnly` bindings.
- `CompatibilityProfileBuilder` contains the explicit RuntimeCandidate gate and P9 `sigma_k` / noise-aware guard.

If live catalog rows are found, stop and report this as an unexpected scope drift from PR #271.

## Task V4: External Minimum-MR-SubSet Source Smoke

**Core steps**

Locate or create a read-only external source clone.

If a clone already exists, set:

```powershell
$SubsetRepo = "C:\tmp\Minimum-MR-SubSet"
```

If it does not exist, and network access is available, run:

```powershell
git clone https://github.com/meng004/Minimum-MR-SubSet.git C:\tmp\Minimum-MR-SubSet
$SubsetRepo = "C:\tmp\Minimum-MR-SubSet"
```

Then run:

```powershell
git -C $SubsetRepo remote -v
git -C $SubsetRepo rev-parse HEAD
Test-Path "$SubsetRepo\experiments\puts\p5_pke.py"
Test-Path "$SubsetRepo\experiments\puts\p4_pendulum.py"
Test-Path "$SubsetRepo\experiments\puts\p9_openmc.py"
Test-Path "$SubsetRepo\tests\puts\test_smoke.py"
```

Attempt pytest only if it is already available:

```powershell
python -m pytest "$SubsetRepo\tests\puts\test_smoke.py" -q
```

If `pytest` is missing, do not install it. Instead run this direct P5/P4/P9 smoke:

```powershell
$Smoke = @'
import importlib.util
import math
import pathlib
import sys

repo = pathlib.Path(sys.argv[1])
cases = {
    "P5": ("p5_pke", ["t", "power", "precursor", "power_extrema"]),
    "P4": ("p4_pendulum", ["q", "p", "energy"]),
    "P9": ("p9_openmc", ["k_eff", "sigma_k", "reaction_balance"]),
}

def finite_value(value):
    if isinstance(value, (int, float)):
        return math.isfinite(value)
    if isinstance(value, (list, tuple)):
        return len(value) > 0 and all(finite_value(item) for item in value)
    if isinstance(value, dict):
        return len(value) > 0 and all(finite_value(item) for item in value.values())
    return value is not None

for put_id, (module_name, expected_observables) in cases.items():
    path = repo / "experiments" / "puts" / f"{module_name}.py"
    if not path.exists():
        raise SystemExit(f"missing source file for {put_id}: {path}")
    spec = importlib.util.spec_from_file_location(module_name, path)
    if spec is None or spec.loader is None:
        raise SystemExit(f"cannot load module spec for {put_id}: {path}")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    result = module.run_canonical()
    if result.get("put_id") != put_id:
        raise SystemExit(f"{put_id}: wrong put_id {result.get('put_id')!r}")
    observables = result.get("observables")
    if not isinstance(observables, dict) or not observables:
        raise SystemExit(f"{put_id}: missing or empty observables")
    missing = [name for name in expected_observables if name not in observables]
    if missing:
        raise SystemExit(f"{put_id}: missing observables {missing}")
    bad = [name for name, value in observables.items() if not finite_value(value)]
    if bad:
        raise SystemExit(f"{put_id}: non-finite observables {bad}")
    print(f"{put_id}: PASS observables={','.join(observables.keys())}")
'@
$Smoke | python - $SubsetRepo
```

**Acceptance standard**

- Report whether pytest smoke ran.
- If pytest did not run, report the exact blocker, for example `No module named pytest`.
- The direct smoke must print one PASS line each for P5, P4, and P9, or report the exact missing dependency/error.
- If Python is missing, report that external source SUT execution could not be verified on this VM.
- Do not install `pytest`, `numpy`, or any other dependency unless the user explicitly approves.

## Task V5: Runtime-Readiness Classification

**Core steps**

Use the results from V2, V3, and V4 to write a classification with exactly these fields:

```text
AGroupImportExportStaging: PASS | FAIL | BLOCKED
ExternalSourceCanonicalRun: PASS | FAIL | BLOCKED
MetBenchLauncherRuntimeRun: PASS | FAIL | NOT_PROMOTED | BLOCKED
```

Apply these rules:

- `AGroupImportExportStaging = PASS` only if focused import/export tests pass.
- `ExternalSourceCanonicalRun = PASS` only if pytest or direct P5/P4/P9 smoke executes and validates all three PUTs.
- `MetBenchLauncherRuntimeRun = NOT_PROMOTED` if the imported MR ids are absent from live `SUT/**/catalog.json` and compatibility is still `ImportedOnly`.
- `MetBenchLauncherRuntimeRun = PASS` only if a real live launcher run executes the imported P5/P4/P9 MRs and records pass/fail evidence. This is not expected in the current PR #271 design.

**Acceptance standard**

- The final classification does not blur staging import success with live runtime execution.
- If the user asks "can the three imported SUT/MR run normally?", answer in two layers:
  - staging import/export and external source SUT run status;
  - MetBench launcher runtime status.

## Task V6: Report Format

Return a concise VM report in this format:

```text
Branch / Commit
- branch:
- HEAD:
- worktree clean:

Windows Build / Tests
- dotnet restore:
- AGroupPutImportExportTests:
- SemanticCatalogBoundaryTests:
- Catalog_MR_id_set_equals_governance_whitelist:
- dotnet build MetBench.sln:

External Minimum-MR-SubSet Evidence
- repo:
- remote:
- commit:
- P5 source:
- P4 source:
- P9 source:
- pytest smoke:
- direct P5/P4/P9 smoke:

Runtime Readiness
- AGroupImportExportStaging:
- ExternalSourceCanonicalRun:
- MetBenchLauncherRuntimeRun:
- explanation:

Changed Files / Scope Drift
- WPF/XAML/MetBench_Client touched:
- live SUT catalog touched:
- LiteDB/execution evidence touched:

Conclusion
- one paragraph, no more than five sentences
```

## Stop Conditions

Stop and ask for guidance if any of these occurs:

- The VM worktree is dirty before verification.
- `dotnet build MetBench.sln` fails.
- A-group fixture source paths are missing from the MetBench repo.
- The external `Minimum-MR-SubSet` source files are missing.
- Live `SUT/**/catalog.json` already contains imported A-group MR ids, because that contradicts the staging-only design.
- You are tempted to install dependencies; ask the user first.

## What Not To Claim

- Do not claim P5/P4/P9 imported MRs are live MetBench runtime MRs unless the launcher actually runs them.
- Do not claim P9 is real OpenMC execution; it is an OpenMC surrogate in this A-group import.
- Do not convert `ImportedResearchEvidence` into MetBench `ExecutionEvidence`.
- Do not mark the A-group plan Controlled from VM evidence alone; status ledger updates require a separate repo PR.
