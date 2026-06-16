# Quality Architecture Remediation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Land the first low-risk quality remediation slice from the 2026-06-16 architecture review by turning three review findings into mechanical guards.

**Architecture:** This plan intentionally avoids large launcher/pipeline behavior refactors. It adds repository-quality guard tests first, watches each fail, then applies minimal project/document edits to make the guard pass. The bigger structured command model and launcher/pipeline decomposition should be handled by a later design PR.

**Tech Stack:** .NET 8, xUnit, XML parsing via `System.Xml.Linq`, existing `MetBench_SystemMT.Tests/SystemMT/Architecture` guard-test style.

---

## Scope

Implement exactly these three items:

1. Add a warning-ratchet guard for `MetBench_SystemMT.Tests.csproj`, then enable `TreatWarningsAsErrors` with a whitelist for currently known warning codes.
2. Add a WPF project-file duplicate `Compile Update` guard, then remove the duplicate `Compile Update` item group from `MetBench_Client.csproj`.
3. Add a README stale-test-baseline guard, then remove hard-coded stale CI baseline wording from `README.md`.

Do not modify `SystemMtLauncher`, `SystemMtPipeline`, runtime command construction, SUT behavior, CI workflows, or status-ledger inventory in this PR.

## Task 1: Test Project Warning Ratchet

**Files:**
- Modify: `MetBench_SystemMT.Tests/SystemMT/Architecture/RepositoryQualityGuardTests.cs`
- Modify: `MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj`

- [ ] **Step 1: Write the failing test**

Create `MetBench_SystemMT.Tests/SystemMT/Architecture/RepositoryQualityGuardTests.cs` if it does not exist. Add this test:

```csharp
using System.Xml.Linq;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Architecture;

public sealed class RepositoryQualityGuardTests
{
    [Fact]
    public void SystemMT_tests_project_has_warning_ratchet_with_explicit_existing_debt_whitelist()
    {
        var csproj = XDocument.Load(Path.Combine(SolutionRoot(), "MetBench_SystemMT.Tests", "MetBench_SystemMT.Tests.csproj"));
        var properties = csproj.Root!.Elements("PropertyGroup").Elements().ToDictionary(e => e.Name.LocalName, e => e.Value.Trim(), StringComparer.Ordinal);

        Assert.True(properties.TryGetValue("TreatWarningsAsErrors", out var twae) && twae == "true",
            "MetBench_SystemMT.Tests must fail on new warning codes; existing warning codes belong in WarningsNotAsErrors.");

        Assert.True(properties.TryGetValue("WarningsNotAsErrors", out var whitelist),
            "Existing test-project warning codes must be explicit so the whitelist can shrink over time.");

        var actual = whitelist.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Order(StringComparer.Ordinal).ToArray();
        var expected = new[]
        {
            "CS0618",
            "CS0649",
            "CS8602",
            "CS8766",
            "xUnit2013",
            "xUnit2031",
        }.Order(StringComparer.Ordinal).ToArray();

        Assert.Equal(expected, actual);
    }

    private static string SolutionRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (dir.GetFiles("*.sln").Length > 0)
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException($"Could not locate solution root from {AppContext.BaseDirectory}.");
    }
}
```

- [ ] **Step 2: Run test to verify RED**

Run:

```powershell
dotnet test MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj --no-restore --no-build --nologo --filter "FullyQualifiedName~RepositoryQualityGuardTests.SystemMT_tests_project_has_warning_ratchet" -v minimal
```

Expected: FAIL because `TreatWarningsAsErrors` / `WarningsNotAsErrors` are missing from `MetBench_SystemMT.Tests.csproj`.

- [ ] **Step 3: Minimal implementation**

In `MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj`, add these properties inside the existing top `PropertyGroup`:

```xml
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <WarningsNotAsErrors>CS8766;CS0618;CS8602;CS0649;xUnit2031;xUnit2013</WarningsNotAsErrors>
```

- [ ] **Step 4: Run GREEN**

Run:

```powershell
dotnet test MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj --no-restore --nologo --filter "FullyQualifiedName~RepositoryQualityGuardTests.SystemMT_tests_project_has_warning_ratchet" -v minimal
```

Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add MetBench_SystemMT.Tests/SystemMT/Architecture/RepositoryQualityGuardTests.cs MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj
git commit -m "test(systemmt): ratchet test project warnings"
```

## Task 2: WPF Project Duplicate Compile Guard

**Files:**
- Modify: `MetBench_SystemMT.Tests/SystemMT/Architecture/RepositoryQualityGuardTests.cs`
- Modify: `MetBench_Client/MetBench_Client.csproj`

- [ ] **Step 1: Write the failing test**

Add this test to `RepositoryQualityGuardTests`:

```csharp
[Fact]
public void Client_project_has_no_duplicate_compile_update_entries()
{
    var csproj = XDocument.Load(Path.Combine(SolutionRoot(), "MetBench_Client", "MetBench_Client.csproj"));
    var duplicates = csproj.Root!
        .Descendants()
        .Where(e => e.Name.LocalName == "Compile")
        .Select(e => e.Attribute("Update")?.Value)
        .Where(v => !string.IsNullOrWhiteSpace(v))
        .GroupBy(v => v!, StringComparer.OrdinalIgnoreCase)
        .Where(g => g.Count() > 1)
        .Select(g => $"{g.Key} x{g.Count()}")
        .Order(StringComparer.OrdinalIgnoreCase)
        .ToArray();

    Assert.True(duplicates.Length == 0,
        "MetBench_Client.csproj must not duplicate Compile Update entries:\n  - " + string.Join("\n  - ", duplicates));
}
```

- [ ] **Step 2: Run test to verify RED**

Run:

```powershell
dotnet test MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj --no-restore --no-build --nologo --filter "FullyQualifiedName~RepositoryQualityGuardTests.Client_project_has_no_duplicate_compile_update_entries" -v minimal
```

Expected: FAIL listing duplicate `Views\Pages\AutoDetectMRPage.xaml.cs`, `Views\Pages\MRRecommendationPage.xaml.cs`, and `Views\Pages\MTReportGeneratorPage.xaml.cs`.

- [ ] **Step 3: Minimal implementation**

In `MetBench_Client/MetBench_Client.csproj`, remove the second duplicate `<ItemGroup>` containing only these three repeated `<Compile Update=...><SubType>Code</SubType></Compile>` entries. Keep the first item group.

- [ ] **Step 4: Run GREEN**

Run:

```powershell
dotnet test MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj --no-restore --nologo --filter "FullyQualifiedName~RepositoryQualityGuardTests.Client_project_has_no_duplicate_compile_update_entries" -v minimal
```

Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add MetBench_SystemMT.Tests/SystemMT/Architecture/RepositoryQualityGuardTests.cs MetBench_Client/MetBench_Client.csproj
git commit -m "test(wpf): guard client project compile entries"
```

## Task 3: README Stale Baseline Guard

**Files:**
- Modify: `MetBench_SystemMT.Tests/SystemMT/Architecture/RepositoryQualityGuardTests.cs`
- Modify: `README.md`

- [ ] **Step 1: Write the failing test**

Add this test to `RepositoryQualityGuardTests`:

```csharp
[Fact]
public void Readme_does_not_hardcode_stale_ci_test_baseline()
{
    var readme = File.ReadAllText(Path.Combine(SolutionRoot(), "README.md"));

    Assert.DoesNotContain("full 521 tests", readme, StringComparison.OrdinalIgnoreCase);
    Assert.DoesNotContain("521/521", readme, StringComparison.OrdinalIgnoreCase);
    Assert.Contains("docs/status/current.md", readme, StringComparison.Ordinal);
    Assert.Contains("current status ledger", readme, StringComparison.OrdinalIgnoreCase);
}
```

- [ ] **Step 2: Run test to verify RED**

Run:

```powershell
dotnet test MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj --no-restore --no-build --nologo --filter "FullyQualifiedName~RepositoryQualityGuardTests.Readme_does_not_hardcode_stale_ci_test_baseline" -v minimal
```

Expected: FAIL because `README.md` currently says "full 521 tests".

- [ ] **Step 3: Minimal implementation**

In `README.md`, update the Continuous integration paragraph from a hard-coded historical count to ledger-based wording:

```markdown
Every push to `main` and every pull request runs
`dotnet test MetBench_SystemMT.Tests` on `ubuntu-24.04` via
`.github/workflows/dotnet-test.yml`. OpenMOC, OpenMC, SciPy, and live MCP
acceptance paths are environment-gated and skip cleanly when the matching
runtime is not configured. The current pass / skip baseline is maintained in
the [current status ledger](docs/status/current.md), while CI also enforces a
120-second performance budget through `tools/ci_perf_baseline.py`.
```

Do not rewrite unrelated README sections.

- [ ] **Step 4: Run GREEN**

Run:

```powershell
dotnet test MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj --no-restore --nologo --filter "FullyQualifiedName~RepositoryQualityGuardTests.Readme_does_not_hardcode_stale_ci_test_baseline" -v minimal
```

Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add MetBench_SystemMT.Tests/SystemMT/Architecture/RepositoryQualityGuardTests.cs README.md
git commit -m "docs(readme): avoid stale ci baseline claims"
```

## Final Verification

- [ ] Run focused guard suite:

```powershell
dotnet test MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj --no-restore --nologo --filter "FullyQualifiedName~RepositoryQualityGuardTests" -v minimal
```

Expected: all `RepositoryQualityGuardTests` pass.

- [ ] Run full cross-platform test suite:

```powershell
dotnet test MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj --no-restore --nologo -v minimal
```

Expected: 0 failures. Environment-dependent tests may skip.

- [ ] Confirm only planned files changed:

```powershell
git diff --name-status origin/main...HEAD
```

Expected changed files:

```text
M README.md
M MetBench_Client/MetBench_Client.csproj
M MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj
A MetBench_SystemMT.Tests/SystemMT/Architecture/RepositoryQualityGuardTests.cs
A docs/superpowers/plans/2026-06-16-quality-architecture-remediation-plan.md
```

