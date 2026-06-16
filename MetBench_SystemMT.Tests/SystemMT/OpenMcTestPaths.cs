using System.Diagnostics;

namespace MetBench_SystemMT.Tests.SystemMT;

internal static class OpenMcTestPaths
{
    private const string DefaultVenvPython = "/opt/openmc-venv/bin/python";

    public static string OpenMcPython()
    {
        var configured = Environment.GetEnvironmentVariable("METBENCH_OPENMC_PYTHON");
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured;
        }

        if (File.Exists(DefaultVenvPython))
        {
            return DefaultVenvPython;
        }

        return TestAssetPaths.PythonExecutable();
    }

    public static bool OpenMcImportable()
    {
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = OpenMcPython(),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            process.StartInfo.ArgumentList.Add("-c");
            process.StartInfo.ArgumentList.Add("import openmc");

            process.Start();

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
