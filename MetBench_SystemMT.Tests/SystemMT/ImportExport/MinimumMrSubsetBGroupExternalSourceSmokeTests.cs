using Xunit;
using Xunit.Abstractions;

namespace MetBench_SystemMT.Tests.SystemMT.ImportExport;

public sealed class MinimumMrSubsetBGroupExternalSourceSmokeTests
{
    private readonly ITestOutputHelper _output;

    public MinimumMrSubsetBGroupExternalSourceSmokeTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [SkippableFact]
    public void External_source_prerequisites_for_P3_P8_are_available()
    {
        var prerequisites = RequirePrerequisites();

        _output.WriteLine($"External root: {prerequisites.Root}");
        _output.WriteLine($"External commit: {prerequisites.Commit}");
        _output.WriteLine($"Python: {prerequisites.Python.DisplayName}");
        _output.WriteLine(prerequisites.DependencyVersions);

        Assert.Contains("numpy=", prerequisites.DependencyVersions, StringComparison.Ordinal);
        Assert.Contains("scipy=", prerequisites.DependencyVersions, StringComparison.Ordinal);
        Assert.Contains("pytest=", prerequisites.DependencyVersions, StringComparison.Ordinal);
    }

    [SkippableFact]
    public void External_pytest_put_smoke_runs_after_prerequisites_are_available()
    {
        var prerequisites = RequirePrerequisites();

        var result = MinimumMrSubsetExternalTestPaths.RunPythonWithNumpyTrapzCompatibility(
            prerequisites.Root,
            prerequisites.Python,
            ["-m", "pytest", "tests/puts/test_smoke.py", "-q"],
            TimeSpan.FromSeconds(120));

        Assert.True(
            result.ExitCode == 0,
            $"Command failed: {result.CommandLine}{Environment.NewLine}STDOUT:{Environment.NewLine}{result.StandardOutput}{Environment.NewLine}STDERR:{Environment.NewLine}{result.StandardError}");
    }

    [SkippableFact]
    public void External_P3_P8_run_canonical_outputs_expected_observables()
    {
        var prerequisites = RequirePrerequisites();

        const string script = @"
import importlib
import numpy as np

expected = {
    'experiments.puts.p3_lorenz': ('P3', ('t', 'trajectory', 'centroid')),
    'experiments.puts.p8_schrodinger': ('P8', ('x', 'probability_density', 'norm')),
}

for module_name, (put_id, names) in expected.items():
    result = importlib.import_module(module_name).run_canonical()
    assert result['put_id'] == put_id, result
    observables = result['observables']
    for name in names:
        value = observables[name]
        arr = np.asarray(value)
        assert arr.size > 0, (put_id, name, arr.shape)
        assert np.all(np.isfinite(arr)), (put_id, name)

print('P3/P8 canonical observables verified')
";

        var result = MinimumMrSubsetExternalTestPaths.RunPythonWithNumpyTrapzCompatibility(
            prerequisites.Root,
            prerequisites.Python,
            ["-c", script],
            TimeSpan.FromSeconds(120));

        Assert.True(
            result.ExitCode == 0,
            $"Command failed: {result.CommandLine}{Environment.NewLine}STDOUT:{Environment.NewLine}{result.StandardOutput}{Environment.NewLine}STDERR:{Environment.NewLine}{result.StandardError}");
        Assert.Contains("P3/P8 canonical observables verified", result.StandardOutput, StringComparison.Ordinal);
    }

    private static MinimumMrSubsetExternalPrerequisites RequirePrerequisites()
    {
        var root = MinimumMrSubsetExternalTestPaths.ResolveRoot();
        Skip.If(
            root is null,
            "Minimum-MR-SubSet source root not found. Set MINIMUM_MR_SUBSET_ROOT or place checkout at C:\\tmp\\Minimum-MR-SubSet.");

        var missingFiles = MinimumMrSubsetExternalTestPaths.FindMissingRequiredFiles(root!);
        Skip.If(
            missingFiles.Count > 0,
            "Minimum-MR-SubSet external source file missing: " + string.Join(", ", missingFiles));

        var commit = MinimumMrSubsetExternalTestPaths.ReadGitCommit(root!);
        Skip.IfNot(
            commit.ExitCode == 0,
            $"Minimum-MR-SubSet external git commit unavailable. Command: {commit.CommandLine}. Output: {commit.CombinedOutput}");

        var python = MinimumMrSubsetExternalTestPaths.ResolvePython();
        Skip.If(
            python is null,
            "Minimum-MR-SubSet external Python interpreter not found. Set MINIMUM_MR_SUBSET_PYTHON or put python on PATH.");

        var dependencies = MinimumMrSubsetExternalTestPaths.CheckDependencies(root!, python!);
        Skip.IfNot(
            dependencies.ExitCode == 0,
            $"Minimum-MR-SubSet external Python dependency missing or not importable. Command: {dependencies.CommandLine}. Output: {dependencies.CombinedOutput}");

        return new MinimumMrSubsetExternalPrerequisites(
            root!,
            python!,
            commit.StandardOutput.Trim(),
            dependencies.StandardOutput.Trim());
    }
}
