using System.Diagnostics;

namespace MetBench_SystemMT.Tests.SystemMT;

/// <summary>
/// 通用 Python 子进程辅助：跑一个脚本带 args，返回 stdout；非零退出 / 超时抛错。
/// 给 v2 SUT parser 的 contract 测试用（无需建生产侧的 C# wrapper）。
/// </summary>
internal static class PythonScriptRunner
{
    public static string Run(string scriptPath, params string[] args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = TestAssetPaths.PythonExecutable(),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        psi.ArgumentList.Add(scriptPath);
        foreach (var a in args) psi.ArgumentList.Add(a);

        using var proc = Process.Start(psi)
            ?? throw new InvalidOperationException($"Failed to start python for {scriptPath}");
        var stdout = proc.StandardOutput.ReadToEnd();
        var stderr = proc.StandardError.ReadToEnd();
        if (!proc.WaitForExit(15_000))
        {
            try { proc.Kill(); } catch { /* best effort */ }
            throw new TimeoutException($"Python script {Path.GetFileName(scriptPath)} timed out");
        }
        if (proc.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Python {Path.GetFileName(scriptPath)} exited {proc.ExitCode}: stderr=[{stderr}]");
        }
        return stdout;
    }
}
