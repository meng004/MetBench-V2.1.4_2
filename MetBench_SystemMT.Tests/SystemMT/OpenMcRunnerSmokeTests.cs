using System.Diagnostics;
using System.Text.Json;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT;

public sealed class OpenMcRunnerSmokeTests
{
    private static string RunnerPath() =>
        Path.Combine(TestAssetPaths.AssetRoot(), "openmc", "openmc_runner.py");

    private static string SamplePinCellPath() =>
        Path.Combine(TestAssetPaths.AssetRoot(), "openmc", "sample", "pincell.json");

    [SkippableFact]
    public async Task Runner_solves_sample_pincell_and_writes_keff_json()
    {
        Skip.IfNot(
            OpenMcTestPaths.OpenMcImportable(),
            "OpenMC is not importable from the resolved Python. Set METBENCH_OPENMC_PYTHON " +
            "or run `.claude/web-setup.sh` to install OpenMC into /opt/openmc-venv.");

        var workDir = Path.Combine(Path.GetTempPath(), "MetBenchOpenMcSmoke", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workDir);
        var outputPath = Path.Combine(workDir, "output.json");

        var psi = new ProcessStartInfo
        {
            FileName = OpenMcTestPaths.OpenMcPython(),
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

        // k_eff comes from a 60-batch / 5000-particle Monte Carlo run, so allow
        // a generous range — the sample pin-cell is criticality-balanced and
        // statistically should land in [0.5, 2.0]. Tightening this would couple
        // the smoke test to MC convergence noise, defeating its smoke purpose.
        var keff = doc.RootElement.GetProperty("k_eff").GetDouble();
        Assert.InRange(keff, 0.5, 2.0);

        // OpenMC reports per-batch standard deviation; just sanity-check it is
        // finite and small enough to indicate the run produced statistics.
        var keffStd = doc.RootElement.GetProperty("k_eff_std").GetDouble();
        Assert.True(double.IsFinite(keffStd) && keffStd >= 0.0 && keffStd < 0.1,
            $"k_eff_std out of plausible range for a smoke run: {keffStd}");

        var batches = doc.RootElement.GetProperty("batches").GetInt32();
        Assert.True(batches > 0);

        Assert.True(doc.RootElement.GetProperty("converged").GetBoolean(),
            $"sample case did not complete cleanly after {batches} batches");

        Assert.Equal("openmc", doc.RootElement.GetProperty("metadata").GetProperty("runner").GetString());
    }
}
