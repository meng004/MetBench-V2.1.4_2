using System.Diagnostics;
using System.Text.Json;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT;

public sealed class OpenMocRunnerSmokeTests
{
    private static string RunnerPath() =>
        Path.Combine(TestAssetPaths.AssetRoot(), "openmoc", "openmoc_runner.py");

    private static string SamplePinCellPath() =>
        Path.Combine(TestAssetPaths.AssetRoot(), "openmoc", "sample", "pincell.json");

    [SkippableFact]
    public async Task Runner_solves_sample_pincell_and_writes_keff_json()
    {
        Skip.IfNot(
            OpenMocTestPaths.OpenMocImportable(),
            "OpenMOC is not importable from the resolved Python. Set METBENCH_OPENMOC_PYTHON " +
            "or run `.claude/web-setup.sh` to install OpenMOC into /opt/openmoc-venv.");

        var workDir = Path.Combine(Path.GetTempPath(), "MetBenchOpenMocSmoke", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workDir);
        var outputPath = Path.Combine(workDir, "output.json");

        var psi = new ProcessStartInfo
        {
            FileName = OpenMocTestPaths.OpenMocPython(),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        psi.ArgumentList.Add(RunnerPath());
        psi.ArgumentList.Add("--input");
        psi.ArgumentList.Add(SamplePinCellPath());
        psi.ArgumentList.Add("--output");
        psi.ArgumentList.Add(outputPath);

        using var process = Process.Start(psi)!;
        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        Assert.True(process.ExitCode == 0, $"runner failed (exit={process.ExitCode}). stderr: {stderr}");

        var text = await File.ReadAllTextAsync(outputPath);
        using var doc = JsonDocument.Parse(text);

        var keff = doc.RootElement.GetProperty("k_eff").GetDouble();
        Assert.InRange(keff, 0.5, 2.0);

        var iterations = doc.RootElement.GetProperty("iterations").GetInt32();
        Assert.True(iterations > 0);

        Assert.True(doc.RootElement.GetProperty("converged").GetBoolean(),
            $"sample case did not converge in {iterations} iterations");

        Assert.Equal("openmoc", doc.RootElement.GetProperty("metadata").GetProperty("runner").GetString());
    }
}
