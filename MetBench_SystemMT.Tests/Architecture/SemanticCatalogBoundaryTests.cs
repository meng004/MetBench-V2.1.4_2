using System;
using System.IO;
using System.Linq;
using Xunit;

namespace MetBench_SystemMT.Tests.Architecture;

/// <summary>
/// PR-D guard: lock the verification-semantics convergence boundary on the
/// production System MT runtime side. After PR-C the typed semantic catalog
/// is the only assertion path; these tests prevent regressions that would
/// reintroduce <c>IMrAssertion</c>, <c>AssertionEvaluator</c>, or new
/// string-code assertion dispatch into <c>MetBench_BLL.Core/SystemMT/</c>.
/// </summary>
/// <remarks>
/// Allowed exceptions are kept explicit and minimal:
/// <list type="bullet">
///   <item>The Typed Semantic Catalog migration namespace
///         <c>MetBench_BLL.Core/SystemMT/Catalog/Typed/Migration/</c> is the
///         single typed entry point that *consumes* legacy assertion-type-code
///         strings to project them into typed predicates.</item>
///   <item><c>MetBench_BLL.Core/SystemMT/Catalog/MrBindingDefinition.cs</c>
///         reads the legacy code list as catalog-binding validation input —
///         the codes are still the canonical naming for catalog entries.</item>
///   <item><c>MetBench_BLL.Core/SystemMT/Assertions/</c> retains the class
///         shells <c>AssertionEvaluator</c>, <c>AssertionInput</c>,
///         <c>AssertionTolerance</c>, <c>AssertionTypeCodes</c>, and
///         <c>SystemMtAssertionResultV2</c>; the guard ensures they are not
///         called from elsewhere in production System MT.</item>
/// </list>
/// </remarks>
public sealed class SemanticCatalogBoundaryTests
{
    private static string FindSolutionRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (dir.GetFiles("*.sln").Length > 0)
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        throw new InvalidOperationException("Cannot find solution root from " + AppContext.BaseDirectory);
    }

    private static string ProductionRoot() =>
        Path.Combine(FindSolutionRoot(), "MetBench_BLL.Core", "SystemMT");

    private static bool IsAllowedAssertionEvaluatorSite(string relativePath) =>
        relativePath.Replace('\\', '/').StartsWith("MetBench_BLL.Core/SystemMT/Assertions/", StringComparison.Ordinal);

    private static bool IsAllowedStringDispatchSite(string relativePath)
    {
        var normalized = relativePath.Replace('\\', '/');
        return normalized.StartsWith("MetBench_BLL.Core/SystemMT/Assertions/", StringComparison.Ordinal)
            || normalized.StartsWith("MetBench_BLL.Core/SystemMT/Catalog/Typed/Migration/", StringComparison.Ordinal)
            || normalized.Equals("MetBench_BLL.Core/SystemMT/Catalog/MrBindingDefinition.cs", StringComparison.Ordinal);
    }

    [Fact]
    public void System_mt_production_runtime_does_not_depend_on_imrassertion()
    {
        var root = FindSolutionRoot();
        var productionRoot = ProductionRoot();
        var violators = Directory.GetFiles(productionRoot, "*.cs", SearchOption.AllDirectories)
            .Where(file =>
            {
                var rel = Path.GetRelativePath(root, file);
                if (rel.Replace('\\', '/').StartsWith("MetBench_BLL.Core/SystemMT/Catalog/Typed/Migration/", StringComparison.Ordinal))
                {
                    return false;
                }
                var text = File.ReadAllText(file);
                return text.Contains("IMrAssertion", StringComparison.Ordinal);
            })
            .Select(file => Path.GetRelativePath(root, file))
            .ToList();

        Assert.True(
            violators.Count == 0,
            "System MT production code must not depend on IMrAssertion:\n" + string.Join("\n", violators));
    }

    [Fact]
    public void System_mt_production_runtime_does_not_call_legacy_assertion_evaluator()
    {
        var root = FindSolutionRoot();
        var productionRoot = ProductionRoot();
        var violators = Directory.GetFiles(productionRoot, "*.cs", SearchOption.AllDirectories)
            .Where(file =>
            {
                var rel = Path.GetRelativePath(root, file);
                if (IsAllowedAssertionEvaluatorSite(rel))
                {
                    return false;
                }
                var text = File.ReadAllText(file);
                return text.Contains("new AssertionEvaluator", StringComparison.Ordinal)
                    || text.Contains("AssertionEvaluator.", StringComparison.Ordinal);
            })
            .Select(file => Path.GetRelativePath(root, file))
            .ToList();

        Assert.True(
            violators.Count == 0,
            "Production System MT runtime must use typed predicate dispatch, not AssertionEvaluator:\n"
            + string.Join("\n", violators));
    }

    [Fact]
    public void System_mt_production_runtime_does_not_add_string_assertion_dispatch()
    {
        var root = FindSolutionRoot();
        var productionRoot = ProductionRoot();
        var violators = Directory.GetFiles(productionRoot, "*.cs", SearchOption.AllDirectories)
            .Where(file =>
            {
                var rel = Path.GetRelativePath(root, file);
                if (IsAllowedStringDispatchSite(rel))
                {
                    return false;
                }
                var text = File.ReadAllText(file);
                return text.Contains("AssertionTypeCodes.", StringComparison.Ordinal);
            })
            .Select(file => Path.GetRelativePath(root, file))
            .ToList();

        Assert.True(
            violators.Count == 0,
            "String assertion-code dispatch must stay out of production runtime; new dispatch sites must go through the Typed Semantic Catalog migration helpers:\n"
            + string.Join("\n", violators));
    }
}
