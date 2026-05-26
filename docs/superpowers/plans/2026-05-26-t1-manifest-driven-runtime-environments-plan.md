# T1 Manifest-Driven Runtime Environments Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the current hardcoded Python environment slots with manifest-driven runtime environment resolution so new SUT runtime dependencies do not require editing launcher option records, DI wiring, or a closed enum for every new venv.

**Architecture:** Keep `System MT` runtime execution inside `MetBench_BLL.Core` and keep WPF as a thin configuration caller. Each SUT manifest declares a stable runtime key such as `system`, `openmoc`, `openmc`, `scipy`, or a future key like `fenics`; `LauncherOptions` carries a dictionary of runtime key to executable path plus a `SystemPython` fallback for compatibility. `ManifestMrCatalogProvider` resolves the manifest key through a runtime resolver and fails closed when a non-system runtime is not configured.

**Tech Stack:** .NET 8, xUnit, System.Text.Json manifest loading, `MetBench_BLL.Core/SystemMT`, existing `SUT/<sut>/catalog.json` manifests.

---

## Scope And Non-Goals

This is a T1 cloud-side plan. It is suitable for Linux/cloud execution because it only touches `MetBench_BLL.Core`, `MetBench_SystemMT.Tests`, manifests, and docs.

This plan must not add a new SUT. It must not change Method MT. It must not change WPF screens or XAML. It must not add new verification predicates. It must preserve existing catalog semantics and existing skip-safe external dependency behavior.

The current T1 claim must be read narrowly: runner/adapter/catalog additivity has been demonstrated, but scalable multi-env management is not complete while `LauncherOptions` and `PythonExecutableKinds` grow one hardcoded field/value per dependency family.

## Preconditions

- [ ] Start from latest `origin/main`.
- [ ] Confirm `docs/status/current.md` records the current inventory as `15 SUT / 12 equations / 29 MRs`.
- [ ] Confirm `MetBench_BLL.Core/SystemMT/Launcher/LauncherOptions.cs` still contains hardcoded environment slots including `SystemPython`, `OpenMocPython`, `OpenMcPython`, and `ScipyPython`.
- [ ] Confirm `MetBench_BLL.Core/SystemMT/Catalog/PythonExecutableKinds.cs` still defines a closed runtime-key vocabulary.
- [ ] Confirm no active status-ledger gate forbids T1 runtime-environment work.
- [ ] Keep this PR cloud-side only unless a compile-time WPF registration issue is discovered; if WPF must change, stop and split a Windows/VM PR.

## Files

- Modify: `MetBench_BLL.Core/SystemMT/Launcher/LauncherOptions.cs`
- Create: `MetBench_BLL.Core/SystemMT/Launcher/RuntimeEnvironmentResolver.cs`
- Create: `MetBench_BLL.Core/SystemMT/Launcher/RuntimeEnvironmentResolutionException.cs`
- Modify: `MetBench_BLL.Core/SystemMT/Catalog/ManifestMrCatalogProvider.cs`
- Modify: `MetBench_BLL.Core/SystemMT/Catalog/PythonExecutableKinds.cs`
- Modify: `MetBench_Client/App.xaml.cs` only if the cloud build proves constructor compatibility cannot be preserved; otherwise do not touch WPF.
- Modify: `MetBench_SystemMT.Tests/SystemMT/Catalog/ManifestMrCatalogProviderTests.cs`
- Create: `MetBench_SystemMT.Tests/SystemMT/Launcher/RuntimeEnvironmentResolverTests.cs`
- Modify: `MetBench_SystemMT.Tests/SystemMT/Catalog/CatalogParityTests.cs`
- Modify: `CLAUDE.md`
- Modify: `docs/status/current.md`
- Modify: `docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md`

## Runtime Contract

`LauncherOptions` should keep source compatibility for existing callers while introducing a generic runtime map.

```csharp
public sealed record LauncherOptions(
    string SutRoot,
    string SystemPython,
    string OpenMocPython,
    string? OpenMcPython = null,
    string? ScipyPython = null,
    IReadOnlyDictionary<string, string>? RuntimePythons = null)
{
    public string EffectiveOpenMcPython =>
        ResolvePythonExecutable(PythonExecutableKinds.OpenMc, SystemPython);

    public string EffectiveScipyPython =>
        ResolvePythonExecutable(PythonExecutableKinds.Scipy, SystemPython);

    public string ResolvePythonExecutable(string? runtimeKey, string? fallback = null)
    {
        var key = string.IsNullOrWhiteSpace(runtimeKey)
            ? PythonExecutableKinds.System
            : runtimeKey.Trim();

        if (RuntimePythons is not null)
        {
            foreach (var pair in RuntimePythons)
            {
                if (string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrWhiteSpace(pair.Value))
                    return pair.Value;
            }
        }

        if (string.Equals(key, PythonExecutableKinds.System, StringComparison.OrdinalIgnoreCase))
            return SystemPython;
        if (string.Equals(key, PythonExecutableKinds.OpenMoc, StringComparison.OrdinalIgnoreCase))
            return string.IsNullOrWhiteSpace(OpenMocPython) ? SystemPython : OpenMocPython;
        if (string.Equals(key, PythonExecutableKinds.OpenMc, StringComparison.OrdinalIgnoreCase))
            return string.IsNullOrWhiteSpace(OpenMcPython) ? SystemPython : OpenMcPython!;
        if (string.Equals(key, PythonExecutableKinds.Scipy, StringComparison.OrdinalIgnoreCase))
            return string.IsNullOrWhiteSpace(ScipyPython) ? SystemPython : ScipyPython!;
        if (!string.IsNullOrWhiteSpace(fallback))
            return fallback;

        throw new RuntimeEnvironmentResolutionException(
            $"Runtime python for key '{key}' is not configured.");
    }
}
```

Resolution order must be deterministic:

1. `RuntimePythons[runtimeKey]` when present and non-blank.
2. Existing compatibility fields for `system`, `openmoc`, `openmc`, `scipy`.
3. `SystemPython` only for `system` or blank runtime key.
4. Fail closed for unknown non-system runtime keys with a diagnostic naming the missing key.

## Task 1: Pin Current Hardcoded Growth Problem

**Files:**
- Test: `MetBench_SystemMT.Tests/SystemMT/Launcher/RuntimeEnvironmentResolverTests.cs`
- Test: `MetBench_SystemMT.Tests/SystemMT/Catalog/ManifestMrCatalogProviderTests.cs`

- [ ] **Step 1: Add failing tests for generic runtime resolution**

Add tests that express the desired behavior before implementation:

```csharp
[Fact]
public void ResolvePythonExecutable_prefers_manifest_runtime_map_over_compat_fields()
{
    var options = new LauncherOptions(
        SutRoot: "/tmp/sut",
        SystemPython: "python3",
        OpenMocPython: "legacy-openmoc",
        RuntimePythons: new Dictionary<string, string>
        {
            ["openmoc"] = "/venv/openmoc/bin/python",
            ["fenics"] = "/venv/fenics/bin/python"
        });

    Assert.Equal("/venv/openmoc/bin/python", options.ResolvePythonExecutable("openmoc"));
    Assert.Equal("/venv/fenics/bin/python", options.ResolvePythonExecutable("fenics"));
}

[Fact]
public void ResolvePythonExecutable_fails_closed_for_unknown_non_system_runtime()
{
    var options = new LauncherOptions(
        SutRoot: "/tmp/sut",
        SystemPython: "python3",
        OpenMocPython: "python3");

    var ex = Assert.Throws<RuntimeEnvironmentResolutionException>(
        () => options.ResolvePythonExecutable("fenics"));

    Assert.Contains("fenics", ex.Message, StringComparison.Ordinal);
}
```

- [ ] **Step 2: Run focused tests and verify red**

Run:

```bash
dotnet test MetBench_SystemMT.Tests --no-restore --filter "FullyQualifiedName~RuntimeEnvironmentResolverTests|FullyQualifiedName~ManifestMrCatalogProviderTests"
```

Expected: fail because `ResolvePythonExecutable` and `RuntimeEnvironmentResolutionException` do not exist or unknown runtime keys still fall back silently.

## Task 2: Implement Runtime Resolver Without Breaking Existing Callers

**Files:**
- Modify: `MetBench_BLL.Core/SystemMT/Launcher/LauncherOptions.cs`
- Create: `MetBench_BLL.Core/SystemMT/Launcher/RuntimeEnvironmentResolutionException.cs`
- Modify: `MetBench_BLL.Core/SystemMT/Catalog/PythonExecutableKinds.cs`

- [ ] **Step 1: Add exception type**

Create:

```csharp
namespace MetBench_BLL.SystemMT.Launcher;

public sealed class RuntimeEnvironmentResolutionException : InvalidOperationException
{
    public RuntimeEnvironmentResolutionException(string message) : base(message)
    {
    }
}
```

- [ ] **Step 2: Add generic resolution while preserving constructor compatibility**

Update `LauncherOptions` to include `RuntimePythons` as an optional final constructor argument and implement `ResolvePythonExecutable`.

The implementation must:

- Treat runtime keys case-insensitively.
- Reject blank values in `RuntimePythons`.
- Keep `OpenMocPython`, `EffectiveOpenMcPython`, and `EffectiveScipyPython` working for existing tests.
- Fail closed for unknown non-system keys.

- [ ] **Step 3: Keep `PythonExecutableKinds` as known constants, not a closed gate**

Update the class comment to state that known constants are built-in runtime keys, not the complete set of allowed manifest keys. `All` may remain for compatibility tests but must not be used to reject future manifest keys.

- [ ] **Step 4: Run focused resolver tests**

Run:

```bash
dotnet test MetBench_SystemMT.Tests --no-restore --filter FullyQualifiedName~RuntimeEnvironmentResolverTests
```

Expected: pass.

## Task 3: Route Manifest Runtime Keys Through The Resolver

**Files:**
- Modify: `MetBench_BLL.Core/SystemMT/Catalog/ManifestMrCatalogProvider.cs`
- Modify: `MetBench_SystemMT.Tests/SystemMT/Catalog/ManifestMrCatalogProviderTests.cs`

- [ ] **Step 1: Add manifest-provider test for a future runtime key**

Write a temporary manifest in the test using:

```json
{
  "program": {
    "program_kind": "fenics-demo",
    "equation_key": "poisson",
    "program_type": "num",
    "runner_script": "runner.py",
    "input_adapter_script": "adapter.py",
    "output_adapter_script": "output.py",
    "input_parser_script": "parse_in.py",
    "output_parser_script": "parse_out.py",
    "python_executable_kind": "fenics"
  },
  "mrs": [
    {
      "mr_id": "fenics-demo-mr",
      "display_name": "FEniCS demo MR",
      "sut_name": "fenics-demo",
      "transformation_name": "ScaleSource",
      "assertion_name": "greater",
      "assertion_type_code": "greater",
      "value_name": "solution_norm",
      "sample_case": "sample/case.json",
      "work_root_name": "fenics-demo",
      "timeout_seconds": 30,
      "equation_key": "poisson"
    }
  ]
}
```

Assert that the loaded entry uses `/venv/fenics/bin/python` when `RuntimePythons["fenics"]` is configured.

- [ ] **Step 2: Replace switch-based runtime selection**

Replace the provider's `program.PythonExecutableKind switch` with:

```csharp
var pythonExecutable = _options.ResolvePythonExecutable(program.PythonExecutableKind);
```

- [ ] **Step 3: Run focused catalog tests**

Run:

```bash
dotnet test MetBench_SystemMT.Tests --no-restore --filter FullyQualifiedName~ManifestMrCatalogProviderTests
```

Expected: pass.

## Task 4: Prove Existing OpenMOC/OpenMC/SciPy Behavior Is Unchanged

**Files:**
- Test: existing catalog, launcher, and SciPy/OpenMC/OpenMOC path tests.

- [ ] **Step 1: Run existing focused tests**

Run:

```bash
dotnet test MetBench_SystemMT.Tests --no-restore --filter "FullyQualifiedName~CatalogParityTests|FullyQualifiedName~OpenMocTestPaths|FullyQualifiedName~OpenMcRunnerSmokeTests|FullyQualifiedName~Scipy"
```

Expected: pass or skip-safe for missing external runtimes. No test may fail because of resolution changes.

- [ ] **Step 2: Run full System MT suite**

Run:

```bash
dotnet test MetBench_SystemMT.Tests --no-restore
```

Expected: full suite green. Missing OpenMOC/OpenMC/SciPy runtime tests may skip with existing explicit reasons.

## Task 5: Update Docs And Monitoring Language

**Files:**
- Modify: `docs/status/current.md`
- Modify: `CLAUDE.md`
- Modify: `docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md`

- [ ] **Step 1: Correct T1 status wording**

Update the status ledger so it no longer says T1 is complete by demonstration. Use this distinction:

- Runner / adapter / catalog additivity: controlled by demonstration.
- Manifest-driven multi-env management: controlled only after this PR.
- UI MR CRUD: not part of this PR; remains Windows/VM scoped.

- [ ] **Step 2: Update collaboration docs**

Document that new runtime keys belong in `catalog.json` and are resolved through `LauncherOptions.RuntimePythons` or environment configuration, not by adding a new field to `LauncherOptions` for every dependency family.

- [ ] **Step 3: Move this plan to completed after merge**

After implementation merges, update active plan index to retire this plan and leave future SUT work free to use manifest runtime keys.

## Task 6: Two-Layer Review And PR

**Files:**
- All modified files.

- [ ] **Step 1: Layer 1 self-review**

Check:

- No Method MT changes.
- No WPF changes unless explicitly justified by compatibility.
- No new SUT added.
- No new typed predicate added.
- Runtime map supports future keys without changing production code.
- Existing `system`, `openmoc`, `openmc`, and `scipy` behavior is preserved.

- [ ] **Step 2: Layer 2 maintainer review**

Review as a maintainer trying to prevent future T1 drift:

- Would a future `fenics` or `fipy` SUT require only manifest/env configuration and no launcher record edit?
- Does unknown runtime configuration fail closed with a clear message?
- Are docs honest that UI MR CRUD remains separate?

- [ ] **Step 3: Commit and PR**

Run:

```bash
git status --short
git add MetBench_BLL.Core/SystemMT/Launcher/LauncherOptions.cs MetBench_BLL.Core/SystemMT/Launcher/RuntimeEnvironmentResolutionException.cs MetBench_BLL.Core/SystemMT/Catalog/ManifestMrCatalogProvider.cs MetBench_BLL.Core/SystemMT/Catalog/PythonExecutableKinds.cs MetBench_SystemMT.Tests/SystemMT/Launcher/RuntimeEnvironmentResolverTests.cs MetBench_SystemMT.Tests/SystemMT/Catalog/ManifestMrCatalogProviderTests.cs docs/status/current.md CLAUDE.md docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md
git commit -m "feat(t1): resolve SUT runtime environments from manifest keys"
```

Open a PR titled:

```text
feat(t1): resolve SUT runtime environments from manifest keys
```

PR body must include:

- Summary.
- Tests run.
- Explicit note that UI MR CRUD is not implemented here.
- Explicit note that no Method MT or WPF code changed unless the compatibility exception was triggered.

## Acceptance Criteria

- New runtime keys can be configured without adding a new `LauncherOptions` property.
- `ManifestMrCatalogProvider` resolves `program.python_executable_kind` through a generic runtime resolver.
- Unknown non-system runtime keys fail closed with clear diagnostics.
- Existing OpenMOC/OpenMC/SciPy/system behavior is preserved.
- Full `MetBench_SystemMT.Tests` is green or external dependency skips are explicit and unchanged.
- Status ledger no longer marks all of T1 complete merely because SUT additivity was demonstrated.

## Stop Conditions

Stop and report without coding if:

- `origin/main` is unreachable.
- The current branch is not based on latest `origin/main`.
- The status ledger contains a newer active plan that supersedes this one.
- The implementation would require redesigning WPF UI.
- Tests reveal that `LauncherOptions` constructor compatibility cannot be preserved without touching `MetBench_Client`; split a Windows PR instead.
