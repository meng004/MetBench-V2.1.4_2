using System.Xml.Linq;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Architecture;

public sealed class RepositoryQualityGuardTests
{
    [Fact]
    public void SystemMT_tests_project_has_warning_ratchet_with_explicit_existing_debt_whitelist()
    {
        var csproj = XDocument.Load(Path.Combine(
            SolutionRoot(),
            "MetBench_SystemMT.Tests",
            "MetBench_SystemMT.Tests.csproj"));
        var properties = csproj.Root!
            .Elements("PropertyGroup")
            .Elements()
            .ToDictionary(e => e.Name.LocalName, e => e.Value.Trim(), StringComparer.Ordinal);

        Assert.True(
            properties.TryGetValue("TreatWarningsAsErrors", out var twae) && twae == "true",
            "MetBench_SystemMT.Tests must fail on new warning codes; existing warning codes belong in WarningsNotAsErrors.");

        Assert.True(
            properties.TryGetValue("WarningsNotAsErrors", out var whitelist),
            "Existing test-project warning codes must be explicit so the whitelist can shrink over time.");

        var actual = whitelist
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Order(StringComparer.Ordinal)
            .ToArray();
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

        Assert.True(
            duplicates.Length == 0,
            "MetBench_Client.csproj must not duplicate Compile Update entries:\n  - " + string.Join("\n  - ", duplicates));
    }

    [Fact]
    public void Readme_does_not_hardcode_stale_ci_or_inventory_baselines()
    {
        var readme = File.ReadAllText(Path.Combine(SolutionRoot(), "README.md"));

        Assert.DoesNotContain("full 521 tests", readme, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("521/521", readme, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("4 SUT", readme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("docs/status/current.md", readme, StringComparison.Ordinal);
        Assert.Contains("current status ledger", readme, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Projection_docs_do_not_redeclare_static_live_main_or_current_521_baseline()
    {
        var docs = new[]
        {
            Path.Combine(SolutionRoot(), "docs", "PROJECT-STRUCTURE.md"),
            Path.Combine(SolutionRoot(), "docs", "requirements.md"),
        };

        foreach (var doc in docs)
        {
            var text = File.ReadAllText(doc);
            Assert.DoesNotContain("2026-06-04 live `origin/main`", text, StringComparison.Ordinal);
            Assert.DoesNotContain("521/521", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void SystemMtReportService_sync_api_does_not_block_on_async_evidence_repository()
    {
        var source = File.ReadAllText(Path.Combine(
            SolutionRoot(),
            "MetBench_BLL.Core",
            "Reporting",
            "SystemMtReportService.cs"));

        Assert.DoesNotContain(".GetAwaiter().GetResult()", source, StringComparison.Ordinal);
        Assert.DoesNotContain(".Result", source, StringComparison.Ordinal);
        Assert.DoesNotContain(".Wait()", source, StringComparison.Ordinal);
    }

    private static string SolutionRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (dir.GetFiles("*.sln").Length > 0)
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException($"Could not locate solution root from {AppContext.BaseDirectory}.");
    }
}
