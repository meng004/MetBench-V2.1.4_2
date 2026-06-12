using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MetBench_BLL.SystemMT.Pipeline;

namespace MetBench_BLL.SystemMT.Runtime;

public sealed class DockerMcpProcessExecutor
{
    private readonly IDockerMcpRuntimeClient _client;

    public DockerMcpProcessExecutor(IDockerMcpRuntimeClient? client = null)
    {
        _client = client ?? new DockerMcpRuntimeClient();
    }

    public async Task<ProcessResult> RunAsync(
        DockerMcpRuntimeOptions options,
        string command,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        if (options is null)
            throw new ArgumentNullException(nameof(options));

        var argv = SplitCommand(command);
        var sw = Stopwatch.StartNew();
        var result = await _client
            .RunSutCommandAsync(options, argv, timeoutSeconds, cancellationToken)
            .ConfigureAwait(false);
        sw.Stop();

        return new ProcessResult(
            result.ExitCode,
            result.Stdout,
            result.Stderr,
            sw.Elapsed,
            result.TimedOut);
    }

    internal static IReadOnlyList<string> SplitCommand(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
            throw new ArgumentException("Command must be non-empty.", nameof(command));

        var result = new List<string>();
        var current = new StringBuilder();
        var inDoubleQuotes = false;
        var escaping = false;

        foreach (var ch in command)
        {
            if (escaping)
            {
                current.Append(ch);
                escaping = false;
                continue;
            }

            if (ch == '\\')
            {
                escaping = true;
                continue;
            }

            if (ch == '"')
            {
                inDoubleQuotes = !inDoubleQuotes;
                continue;
            }

            if (char.IsWhiteSpace(ch) && !inDoubleQuotes)
            {
                FlushToken(result, current);
                continue;
            }

            current.Append(ch);
        }

        if (escaping)
            current.Append('\\');
        if (inDoubleQuotes)
            throw new ArgumentException("Command contains an unterminated quoted string.", nameof(command));

        FlushToken(result, current);
        if (result.Count == 0)
            throw new ArgumentException("Command must contain at least one argument.", nameof(command));
        return result;
    }

    private static void FlushToken(List<string> result, StringBuilder current)
    {
        if (current.Length == 0)
            return;
        result.Add(current.ToString());
        current.Clear();
    }
}
