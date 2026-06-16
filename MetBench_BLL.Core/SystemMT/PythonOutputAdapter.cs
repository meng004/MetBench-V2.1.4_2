using System.Diagnostics;
using System.Text.Json;

namespace MetBench_BLL.SystemMT;

public sealed class PythonOutputAdapter
{
    private readonly string _pythonExecutable;

    public PythonOutputAdapter(string pythonExecutable)
    {
        _pythonExecutable = string.IsNullOrWhiteSpace(pythonExecutable)
            ? throw new ArgumentException("Python executable cannot be empty", nameof(pythonExecutable))
            : pythonExecutable;
    }

    public async Task<ParsedOutput> ParseAsync(
        string adapterPath,
        string outputPath,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(outputPath))
        {
            throw new InvalidOperationException($"Output artifact failure: output file does not exist: {outputPath}");
        }

        if (!File.Exists(adapterPath))
        {
            throw new InvalidOperationException($"Configuration failure: adapter file does not exist: {adapterPath}");
        }

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
        process.StartInfo.ArgumentList.Add("parse-output");
        process.StartInfo.ArgumentList.Add("--output-file");
        process.StartInfo.ArgumentList.Add(outputPath);

        process.Start();
        var stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderr = await process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Adapter failure: adapter exited with code {process.ExitCode}. stderr: {stderr}");
        }

        return ParseJson(stdout);
    }

    private static ParsedOutput ParseJson(string stdout)
    {
        try
        {
            using var document = JsonDocument.Parse(stdout);
            var root = document.RootElement;
            var values = new Dictionary<string, double>();
            foreach (var item in root.GetProperty("values").EnumerateObject())
            {
                values[item.Name] = item.Value.GetDouble();
            }

            var metadata = new Dictionary<string, string>();
            if (root.TryGetProperty("metadata", out var metadataElement))
            {
                foreach (var item in metadataElement.EnumerateObject())
                {
                    metadata[item.Name] = item.Value.ToString();
                }
            }

            return new ParsedOutput(values, metadata);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Adapter failure: adapter returned invalid JSON. {ex.Message}", ex);
        }
    }

}
