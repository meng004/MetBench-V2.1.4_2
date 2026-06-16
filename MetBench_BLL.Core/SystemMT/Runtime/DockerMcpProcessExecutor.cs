using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
        ProcessInvocation invocation,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        if (options is null)
            throw new ArgumentNullException(nameof(options));
        ArgumentNullException.ThrowIfNull(invocation);
        if (string.IsNullOrWhiteSpace(invocation.FileName))
            throw new ArgumentException("Executable file name is required.", nameof(invocation));

        IReadOnlyList<string> argv = new[] { invocation.FileName }
            .Concat(invocation.Arguments)
            .ToArray();
        if (options.PathStyle == DockerMcpPathStyle.Wsl)
        {
            argv = argv.Select(TranslateWindowsPathToWsl).ToList();
        }
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

    internal static string TranslateWindowsPathToWsl(string token)
    {
        if (token.Length < 3
            || !char.IsAsciiLetter(token[0])
            || token[1] != ':'
            || (token[2] != '\\' && token[2] != '/'))
        {
            return token;
        }

        var drive = char.ToLowerInvariant(token[0]);
        var rest = token[3..].Replace('\\', '/');
        return $"/mnt/{drive}/{rest}";
    }
}
