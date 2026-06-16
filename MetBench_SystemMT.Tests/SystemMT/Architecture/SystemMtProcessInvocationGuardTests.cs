using System.Text.RegularExpressions;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Architecture;

public sealed class SystemMtProcessInvocationGuardTests
{
    private static readonly Regex ProcessStartInfoArgumentsAssignment = new(
        @"new\s+ProcessStartInfo\b[\s\S]*?\bArguments\s*=",
        RegexOptions.Compiled);

    private static readonly Regex ProcessStartInfoArgumentsConstructor = new(
        @"new\s+ProcessStartInfo\s*\(\s*[^,\r\n]+,\s*",
        RegexOptions.Compiled);

    [Fact]
    public void SystemMt_production_code_does_not_build_shell_or_command_string_processes()
    {
        var root = SolutionRoot();
        var violations = new List<string>();

        foreach (var productionRoot in ProcessExecutionSourceRoots(root))
        {
            foreach (var file in Directory.GetFiles(productionRoot, "*.cs", SearchOption.AllDirectories))
            {
                var text = File.ReadAllText(file);
                var relative = Path.GetRelativePath(root, file);

                if (ProcessStartInfoArgumentsAssignment.IsMatch(text)
                    || ProcessStartInfoArgumentsConstructor.IsMatch(text))
                {
                    violations.Add($"{relative}: ProcessStartInfo string arguments");
                }

                foreach (var token in ForbiddenCommandTokens())
                {
                    if (text.Contains(token, StringComparison.Ordinal))
                    {
                        violations.Add($"{relative}: {token}");
                    }
                }
            }
        }

        Assert.True(
            violations.Count == 0,
            "System MT process execution must pass executable and argv separately; string command execution is forbidden:\n  - " +
            string.Join("\n  - ", violations.Order(StringComparer.Ordinal)));
    }

    private static string[] ForbiddenCommandTokens()
    {
        return
        [
            "cmd.exe",
            "/bin/sh",
            "PlatformShell",
            "SplitCommand",
            "InputParserCommand",
            "OutputParserCommand",
            "RunnerCommand",
            "RunAsync(string command",
            "string command"
        ];
    }

    private static string[] ProcessExecutionSourceRoots(string solutionRoot)
    {
        return
        [
            Path.Combine(solutionRoot, "MetBench_BLL.Core", "SystemMT"),
            Path.Combine(solutionRoot, "MetBench_BLL.Core", "Discovery"),
        ];
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
