using System.Diagnostics;
using System.Text.Json;

namespace MetBench_BLL.SystemMT;

public sealed class PythonInputAdapter
{
    private readonly string _pythonExecutable;

    public PythonInputAdapter(string pythonExecutable)
    {
        _pythonExecutable = string.IsNullOrWhiteSpace(pythonExecutable)
            ? throw new ArgumentException("Python executable cannot be empty", nameof(pythonExecutable))
            : pythonExecutable;
    }

    public async Task<string> TransformAsync(
        string adapterPath,
        string sourceInputPath,
        string followUpInputPath,
        MrTransformation transformation,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(adapterPath))
        {
            throw new InvalidOperationException($"Configuration failure: adapter file does not exist: {adapterPath}");
        }

        if (!File.Exists(sourceInputPath))
        {
            throw new InvalidOperationException($"Configuration failure: source input file does not exist: {sourceInputPath}");
        }

        var paramsJson = JsonSerializer.Serialize(transformation.Parameters);

        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = _pythonExecutable,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        process.StartInfo.ArgumentList.Add(adapterPath);
        process.StartInfo.ArgumentList.Add("transform-input");
        process.StartInfo.ArgumentList.Add("--source-file");
        process.StartInfo.ArgumentList.Add(sourceInputPath);
        process.StartInfo.ArgumentList.Add("--output-file");
        process.StartInfo.ArgumentList.Add(followUpInputPath);
        process.StartInfo.ArgumentList.Add("--params");
        process.StartInfo.ArgumentList.Add(paramsJson);

        process.Start();
        var stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderr = await process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Adapter failure: input adapter exited with code {process.ExitCode}. stderr: {stderr}");
        }

        return ParseLog(stdout);
    }

    private static string ParseLog(string stdout)
    {
        try
        {
            using var document = JsonDocument.Parse(stdout);
            return document.RootElement.TryGetProperty("log", out var log)
                ? log.GetString() ?? string.Empty
                : string.Empty;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Adapter failure: input adapter returned invalid JSON. {ex.Message}", ex);
        }
    }

}
