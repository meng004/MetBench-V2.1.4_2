# Quality Follow-Up Remediation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Close the immediate quality-assessment findings without mixing broad refactors into environment fixes.

**Architecture:** Keep the executable change inside test infrastructure by improving `TestAssetPaths.PythonExecutable()` resolution. Track WPF package warning remediation and large-file decomposition as separate, evidence-producing work because both have wider blast radius than a Python resolver fix.

**Tech Stack:** .NET 8 xUnit tests, PowerShell verification on Windows, project docs under `docs/superpowers/plans/` and `docs/uat/`.

---

### Task 1: Test Python Executable Resolution

**Files:**
- Modify: `MetBench_SystemMT.Tests/SystemMT/TestAssetPaths.cs`
- Create: `MetBench_SystemMT.Tests/SystemMT/TestPythonExecutableResolver.cs`
- Test: `MetBench_SystemMT.Tests/SystemMT/TestPythonExecutableResolverTests.cs`
- Modify: `docs/uat/mcp-three-case-acceptance-runbook.md`

- [x] **Step 1: Write the failing test**

Add tests that pin these cases:

```csharp
Assert.Equal("python3", TestPythonExecutableResolver.Resolve(null, true, c => c == "python3"));
Assert.Equal("py", TestPythonExecutableResolver.Resolve(null, true, c => c == "py"));
Assert.Equal("python", TestPythonExecutableResolver.Resolve(null, false, c => c == "python"));
```

- [x] **Step 2: Run test to verify it fails**

Run:

```powershell
dotnet test MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj --filter "FullyQualifiedName~TestPythonExecutableResolverTests" -v minimal
```

Expected in this sandbox: restore is blocked before compile by `Access to the path 'C:\Users\lemon\AppData\Local\Microsoft SDKs' is denied`. On a normal Windows dev shell, expected pre-implementation failure is missing `TestPythonExecutableResolver`.

- [x] **Step 3: Write minimal implementation**

`TestAssetPaths.PythonExecutable()` now delegates to a resolver that keeps `METBENCH_TEST_PYTHON` first, probes Windows candidates in `python`, `python3`, `py` order, probes non-Windows candidates in `python3`, `python` order, and preserves the previous platform default when no candidate exists.

- [x] **Step 4: Run focused green verification**

Run:

```powershell
dotnet test MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj --filter "FullyQualifiedName~TestPythonExecutableResolverTests" -v minimal
```

Actual evidence on 2026-06-16 after adding `TestPythonExecutableResolverTests`:

```text
Passed: 6, Failed: 0, Skipped: 0
```

### Task 2: WPF Package Warning Debt

**Files:**
- Future modify: WPF package references in `MetBench_Client/MetBench_Client.csproj` and `MetBench_Client.Tests/MetBench_Client.Tests.csproj`
- Future test: `dotnet build MetBench.sln --no-restore --nologo -v quiet`

- [ ] **Step 1: Capture current warning baseline**

Run:

```powershell
dotnet build MetBench.sln --no-restore --nologo -v quiet
```

Expected current baseline from the quality assessment: build succeeds with existing NU1701 warnings for `OpenTK`, `OpenTK.GLWpfControl`, and `SkiaSharp.Views.WPF` against `net8.0-windows7.0`.

- [ ] **Step 2: Choose one package remediation path**

Open a separate package-upgrade PR only after checking whether compatible replacements exist for the WPF rendering path. Do not combine package upgrades with System MT runtime changes.

### Task 3: Large-File Decomposition Backlog

**Files:**
- Future candidates: `MetBench_BLL.Core/SystemMT/Metadata/SystemMtMetadataCatalog.cs`, `MetBench_BLL.Core/SystemMT/Launcher/LegacyCatalogFactory.cs`, `MetBench_Client/ViewModels/ApplicationManagementViewModel.cs`, `MetBench_Client/ViewModels/MRManagementViewModel.cs`, `MetBench_BLL.Core/SystemMT/Launcher/SystemMtLauncher.cs`, `MetBench_BLL.Core/SystemMT/Pipeline/SystemMtPipeline.cs`

- [ ] **Step 1: Split only when touching behavior**

For each future feature in these files, extract one responsibility with tests already covering the behavior being moved. Avoid a standalone mass refactor PR unless it has focused source guards and before/after behavior evidence.

- [ ] **Step 2: Keep System MT boundaries intact**

Any extraction from launcher or pipeline must preserve the launcher facade and typed semantic catalog path. Do not introduce dictionary predicates, legacy `IMrAssertion`, or method-level assertion classes into System MT runtime.

### Task 4: Verification Evidence

**Files:**
- Verify: code diff and docs diff

- [x] **Step 1: Run focused tests**

Run the resolver focused test command from Task 1 Step 4.

- [ ] **Step 2: Run build smoke**

Run:

```powershell
dotnet build MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj --no-restore --nologo -v minimal
```

Expected: 0 errors. If the local machine lacks Python, do not run the full runtime suite as completion evidence; it will still fail for real dependency absence.
