# Minimum-MR-SubSet A-Group Runtime Verification VM Report

Date: 2026-06-02
VM shell: PowerShell
Repository worktree: `C:\MetBench-V2.1.4_2\_worktrees\minimum-mr-subset-runtime`
Branch under test: `main`
HEAD: `1bbbeda4849f660edb0d945074ef08a7c0e5cf57`

`rtk` was unavailable on this Windows VM, so the verification used native
PowerShell commands.

## Scope

This verification follows
`docs/superpowers/vm-prompts/2026-06-02-minimum-mr-subset-a-group-runtime-verification-vm-prompt.md`.
It distinguishes:

- MetBench staging import/export validation.
- External `Minimum-MR-SubSet` source `run_canonical()` smoke.
- MetBench live launcher runtime execution.

It does not promote imported A-group MRs into live System-MT catalogs, does not
write LiteDB execution evidence, and does not install Python dependencies.

## Repository State

- `git fetch origin`: passed.
- `git checkout main`: passed after sandbox escalation.
- `git pull --ff-only origin main`: passed after sandbox escalation.
- `git status -sb`: clean, with only Git's user-level ignore warning.
- Latest commit: `1bbbeda Merge pull request #272 from meng004/codex/vm-a-group-runtime-verification-plan`.

Git warned that it could not access `C:\Users\codex/.config/git/ignore`; no
tracked or untracked repository changes were listed.

## Windows Build And Tests

Initial parallel `dotnet test` execution hit Windows file locks in shared
`obj/bin` outputs. Residual `dotnet` processes were stopped and the focused
tests were rerun serially with `/m:1`.

- `dotnet restore MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj`:
  passed after sandbox escalation. The first sandboxed attempt failed with
  `NU1301` because NuGet network access was blocked.
- `dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~AGroupPutImportExportTests" --logger "console;verbosity=minimal" /m:1`:
  passed, 25 passed / 0 failed / 0 skipped.
- `dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --no-restore --filter "SemanticCatalogBoundaryTests" --logger "console;verbosity=minimal" /m:1`:
  passed, 3 passed / 0 failed / 0 skipped.
- `dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --no-restore --filter "Catalog_MR_id_set_equals_governance_whitelist" --logger "console;verbosity=minimal" /m:1`:
  passed, 1 passed / 0 failed / 0 skipped.
- `dotnet build MetBench.sln`: passed, exit code 0, warnings only.

## Staging-Only Checks

The live System-MT `SUT/**/catalog.json` searches returned no matches for the
imported A-group MR IDs:

- `p5-power-response`
- `p4-energy-invariant`
- `p9-k-eff-noise-aware`

The live System-MT `SUT/**/catalog.json` searches returned no matches for the
imported A-group adapter IDs:

- `minimum-mr-subset-p5`
- `minimum-mr-subset-p4`
- `minimum-mr-subset-p9`

`AGroupPutFixtures.cs` still marks the import fixtures as `ImportedOnly`.
`CompatibilityProfileBuilder.cs` still contains the `RuntimeCandidate` gate and
the explicit P9 `sigma_k` / noise-aware guard.

## External Source Smoke

The external source repository was not present initially, so it was cloned to
`C:\tmp\Minimum-MR-SubSet` after sandbox escalation.

- Remote: `https://github.com/meng004/Minimum-MR-SubSet.git`
- Commit: `0ec59b82f6a60df2b011e18dd077c68ade4d08ea`
- `experiments\puts\p5_pke.py`: present.
- `experiments\puts\p4_pendulum.py`: present.
- `experiments\puts\p9_openmc.py`: present.
- `tests\puts\test_smoke.py`: present.

`pytest` was not available:

```text
C:\Users\codex\AppData\Local\Programs\Python\Python312-arm64\python.exe: No module named pytest
```

The direct smoke was rerun from the external repository root so that the
`experiments` package imports resolved. Results:

```text
P5: PASS observables=t,power,precursor,power_extrema
P4: PASS observables=q,p,energy
ModuleNotFoundError: No module named 'openmc'
```

P9 did not execute because the VM does not have the `openmc` Python package.
No dependency was installed.

## Runtime Readiness Classification

- `AGroupImportExportStaging`: `PASS`
- `ExternalSourceCanonicalRun`: `BLOCKED`
- `MetBenchLauncherRuntimeRun`: `NOT_PROMOTED`

Staging import/export is green on Windows. External source canonical execution
is only partially verified: P5 and P4 execute, while P9 is blocked by missing
`openmc`. The imported A-group MRs are not live MetBench launcher MRs yet
because they are absent from live `SUT/**/catalog.json` and remain
`ImportedOnly`.

## Scope Drift Review

The A-group implementation commit inspected was
`0f9db70 feat(systemmt): stage A-group PUT import export`.

Changed files in that commit were limited to:

- `MetBench_BLL.Core/SystemMT/ImportExport/Put/AGroupPutFixtures.cs`
- `MetBench_BLL.Core/SystemMT/ImportExport/Put/CompatibilityProfileBuilder.cs`
- `MetBench_BLL.Core/SystemMT/ImportExport/Put/PutImportModels.cs`
- `MetBench_BLL.Core/SystemMT/ImportExport/Put/README.md`
- `MetBench_BLL.Core/SystemMT/ImportExport/Put/SutImportPackageExporter.cs`
- `MetBench_BLL.Core/SystemMT/ImportExport/Put/SutImportValidationException.cs`
- `MetBench_BLL.Core/SystemMT/ImportExport/Put/SutImportValidator.cs`
- `MetBench_SystemMT.Tests/SystemMT/ImportExport/AGroupPutImportExportTests.cs`

No WPF/XAML files, live `SUT/**/catalog.json` files, LiteDB code, or
`ExecutionEvidence` runtime code were touched by the A-group implementation
commit.
