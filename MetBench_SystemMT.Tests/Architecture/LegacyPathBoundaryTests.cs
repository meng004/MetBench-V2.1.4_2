using System.IO;
using Xunit;

namespace MetBench_SystemMT.Tests.Architecture;

/// <summary>
/// G-X2-LatexGuard: v1 LaTeX 展示衍生字段路径的调用边界守护测试。
///
/// G-11 裁决（2026-05-23）：4 处 Latextosympy* 调用保留为 v1 兼容路径直至 Stage 9。
/// 本测试确保：
///   1. 不出现新增调用（新 MR 必须走 MethodTransformationRegistry，见 mr-architecture §5）。
///   2. 已知 4 处调用仍存在（防止清理后守卫静默失效）。
/// </summary>
public class LegacyPathBoundaryTests
{
    private static readonly HashSet<string> AllowedCallerFiles = new(StringComparer.OrdinalIgnoreCase)
    {
        Path.Combine("MetBench_BLL", "MetamorphicRelationService.cs"),
        Path.Combine("MetBench_BLL", "AutoMRParser.cs"),
        Path.Combine("MetBench_Client", "ViewModels", "MRRecommendationViewModel.cs"),
        Path.Combine("MetBench_Client", "ViewModels", "MRManagementViewModel.cs"),
    };

    private static readonly HashSet<string> ExcludedFiles = new(StringComparer.OrdinalIgnoreCase)
    {
        Path.Combine("MetBench_BLL", "Latextosympy.cs"),
        Path.Combine("MetBench_BLL", "Latextosympy_Await.cs"),
        Path.Combine("MetBench_SystemMT.Tests", "Architecture", "ObsoleteAttributeGuardTests.cs"),
        Path.Combine("MetBench_SystemMT.Tests", "Architecture", "LegacyPathBoundaryTests.cs"),
    };

    private static string FindSolutionRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (dir.GetFiles("*.sln").Length > 0)
                return dir.FullName;
            dir = dir.Parent;
        }
        throw new InvalidOperationException("Cannot find solution root from " + AppContext.BaseDirectory);
    }

    [Fact]
    public void Latextosympy_callsites_are_confined_to_v1_compat_files()
    {
        var root = FindSolutionRoot();
        var violators = new List<string>();

        foreach (var file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(root, file);

            if (ExcludedFiles.Any(e => rel.EndsWith(e, StringComparison.OrdinalIgnoreCase)))
                continue;

            if (!File.ReadAllText(file).Contains("Latextosympy", StringComparison.Ordinal))
                continue;

            if (!AllowedCallerFiles.Any(a => rel.EndsWith(a, StringComparison.OrdinalIgnoreCase)))
                violators.Add(rel);
        }

        Assert.True(violators.Count == 0,
            "New Latextosympy* call sites found outside v1-compat boundary. " +
            "New MRs must use MethodTransformationRegistry (mr-architecture §5). " +
            "Violating files:\n" + string.Join("\n", violators));
    }

    [Fact]
    public void Latextosympy_v1_callsites_all_still_present()
    {
        var root = FindSolutionRoot();

        foreach (var allowed in AllowedCallerFiles)
        {
            var fullPath = Path.Combine(root, allowed);
            Assert.True(File.Exists(fullPath),
                $"Expected v1-compat file not found: {allowed}");

            Assert.True(
                File.ReadAllText(fullPath).Contains("Latextosympy", StringComparison.Ordinal),
                $"Latextosympy reference disappeared from v1-compat file: {allowed}. " +
                "If this call site was cleaned up, update AllowedCallerFiles in G-X2-LatexGuard.");
        }
    }
}
