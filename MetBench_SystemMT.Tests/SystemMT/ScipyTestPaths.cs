using System.Diagnostics;

namespace MetBench_SystemMT.Tests.SystemMT;

internal static class ScipyTestPaths
{
    public static string ScipyPython()
    {
        var configured = Environment.GetEnvironmentVariable("METBENCH_SCIPY_PYTHON");
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured;
        }

        return TestAssetPaths.PythonExecutable();
    }

    public static bool ScipyImportable()
    {
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = ScipyPython(),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            process.StartInfo.ArgumentList.Add("-c");
            process.StartInfo.ArgumentList.Add("import scipy.integrate");

            process.Start();

            // Drain both pipes asynchronously so a slow import banner cannot
            // fill the kernel pipe buffer and deadlock WaitForExit.
            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();

            if (!process.WaitForExit(10_000))
            {
                process.Kill(entireProcessTree: true);
                return false;
            }

            Task.WaitAll(new[] { stdoutTask, stderrTask }, 1_000);
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
