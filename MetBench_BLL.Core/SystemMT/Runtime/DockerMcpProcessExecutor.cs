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
        if (string.IsNullOrWhiteSpace(options.ToolName))
            throw new ArgumentException("Docker MCP tool name is required.", nameof(options));
        if (string.IsNullOrWhiteSpace(options.LocalExecutable))
            throw new ArgumentException("Docker MCP local executable is required.", nameof(options));
        if (!string.Equals(invocation.FileName, options.LocalExecutable, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "Docker MCP invocation executable must match the configured local executable.",
                nameof(invocation));
        }

        var args = invocation.Arguments?.ToArray() ?? Array.Empty<string>();
        foreach (var arg in args)
        {
            ValidateToolArgument(arg);
        }
        if (options.PathStyle == DockerMcpPathStyle.Wsl)
        {
            args = args.Select(TranslateWindowsPathToWsl).ToArray();
        }
        var sw = Stopwatch.StartNew();
        var result = await _client
            .RunSutCommandAsync(
                options,
                new DockerMcpRunRequest(
                    options.Image,
                    options.ToolName,
                    args,
                    timeoutSeconds),
                cancellationToken)
            .ConfigureAwait(false);
        sw.Stop();

        return new ProcessResult(
            result.ExitCode,
            result.Stdout,
            result.Stderr,
            sw.Elapsed,
            result.TimedOut);
    }

    private static void ValidateToolArgument(string arg)
    {
        if (string.IsNullOrWhiteSpace(arg))
            throw new ArgumentException("Docker MCP tool arguments must be non-blank strings.");
        if (arg is "-c" or "/c" or "-m" or "/m")
            throw new ArgumentException("Docker MCP tool arguments must not request shell or module execution.");
        if (arg.EndsWith(".py", StringComparison.OrdinalIgnoreCase)
            || arg.EndsWith(".pyc", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Docker MCP tool arguments must not contain script path values.");
        }
        if (arg.StartsWith("/", StringComparison.Ordinal) || IsWindowsAbsolutePath(arg))
            throw new ArgumentException("Docker MCP tool arguments must not contain absolute host paths.");
        if (arg.Split(new[] { '/', '\\' }, StringSplitOptions.None).Any(part => part == ".."))
            throw new ArgumentException("Docker MCP tool arguments must not contain path traversal.");
        if (arg.Contains(';', StringComparison.Ordinal)
            || arg.Contains("&&", StringComparison.Ordinal)
            || arg.Contains("||", StringComparison.Ordinal)
            || arg.Contains('|', StringComparison.Ordinal)
            || arg.Contains("$(", StringComparison.Ordinal)
            || arg.Contains('`', StringComparison.Ordinal))
        {
            throw new ArgumentException("Docker MCP tool arguments must not contain shell operators.");
        }
    }

    private static bool IsWindowsAbsolutePath(string token) =>
        token.Length >= 3
        && char.IsAsciiLetter(token[0])
        && token[1] == ':'
        && (token[2] == '\\' || token[2] == '/');

    internal static string TranslateWindowsPathToWsl(string token)
    {
        if (!IsWindowsAbsolutePath(token))
        {
            return token;
        }

        var drive = char.ToLowerInvariant(token[0]);
        var rest = token[3..].Replace('\\', '/');
        return $"/mnt/{drive}/{rest}";
    }
}
