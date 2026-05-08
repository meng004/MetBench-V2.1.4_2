using System.Diagnostics;

namespace MetBench_SystemMT.Tests.SystemMT;

internal static class OpenMocTestPaths
{
    private const string DefaultVenvPython = "/opt/openmoc-venv/bin/python";

    public static string OpenMocPython()
    {
        var configured = Environment.GetEnvironmentVariable("METBENCH_OPENMOC_PYTHON");
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

    public static bool OpenMocImportable()
    {
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = OpenMocPython(),
                Arguments = "-c \"import openmoc\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            process.Start();
            if (!process.WaitForExit(10_000))
            {
                process.Kill(entireProcessTree: true);
                return false;
            }

            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
