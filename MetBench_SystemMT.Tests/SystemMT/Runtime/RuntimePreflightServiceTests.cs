using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MetBench_BLL.SystemMT.Pipeline;
using MetBench_BLL.SystemMT.Runtime;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Runtime;

public sealed class RuntimePreflightServiceTests
{
    [Fact]
    public async Task Missing_executable_path_blocks_as_runtime_executable_missing()
    {
        var executor = new RecordingProcessExecutor(_ =>
            new ProcessResult(0, "", "", TimeSpan.Zero, false));
        IRuntimePreflightService service = new RuntimePreflightService(executor);
        var profile = new RuntimeProfile("system", "System Python", RuntimeKind.LocalPython, executablePath: null);

        var result = await service.CheckAsync(profile);

        Assert.False(result.Passed);
        Assert.Equal(RuntimeFailureKind.RuntimeExecutableMissing, result.FailureKind);
        Assert.Empty(executor.Commands);
        Assert.Contains("executable", result.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Placeholder_runtime_kind_blocks_as_middleware_unavailable_without_execution()
    {
        var executor = new RecordingProcessExecutor(_ =>
            new ProcessResult(0, "", "", TimeSpan.Zero, false));
        IRuntimePreflightService service = new RuntimePreflightService(executor);
        var profile = RuntimeProfile.Placeholder("docker-demo", "Docker demo", RuntimeKind.DockerPlaceholder);

        var result = await service.CheckAsync(profile);

        Assert.False(result.Passed);
        Assert.Equal(RuntimeFailureKind.MiddlewareUnavailable, result.FailureKind);
        Assert.Empty(executor.Commands);
        Assert.Contains("DockerPlaceholder", result.Detail);
    }

    [Fact]
    public async Task Docker_runtime_preflight_calls_mcp_health_without_local_startup_probe()
    {
        var executor = new RecordingProcessExecutor(_ =>
            new ProcessResult(0, "local python should not run", "", TimeSpan.Zero, false));
        var dockerClient = new RecordingDockerMcpRuntimeClient(
            new DockerMcpHealthResult(
                Available: true,
                Status: "ok",
                Detail: "MCP health ok",
                BindHost: "192.168.1.20",
                BindPort: 8765,
                RepoRoot: "/repo"));
        IRuntimePreflightService service = new RuntimePreflightService(executor, dockerClient);
        var profile = CreateDockerProfile();

        var result = await service.CheckAsync(profile);

        Assert.True(result.Passed);
        Assert.Empty(executor.Commands);
        Assert.Same(profile.DockerMcp, dockerClient.OptionsSeen.Single());
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("docker-mcp", diagnostic.CheckKind);
        Assert.Equal("openmoc-docker", diagnostic.Name);
        Assert.True(diagnostic.Passed);
        Assert.Contains("192.168.1.20:8765", diagnostic.Detail);
    }

    [Fact]
    public async Task Docker_runtime_preflight_blocks_when_mcp_health_unavailable()
    {
        var executor = new RecordingProcessExecutor(_ =>
            new ProcessResult(0, "local python should not run", "", TimeSpan.Zero, false));
        var dockerClient = new RecordingDockerMcpRuntimeClient(
            new DockerMcpHealthResult(
                Available: false,
                Status: "http_error",
                Detail: "401 Unauthorized"));
        IRuntimePreflightService service = new RuntimePreflightService(executor, dockerClient);
        var profile = CreateDockerProfile();

        var result = await service.CheckAsync(profile);

        Assert.False(result.Passed);
        Assert.Equal(RuntimeFailureKind.MiddlewareUnavailable, result.FailureKind);
        Assert.Empty(executor.Commands);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("docker-mcp", diagnostic.CheckKind);
        Assert.False(diagnostic.Passed);
        Assert.Equal(RuntimeFailureKind.MiddlewareUnavailable, diagnostic.FailureKind);
        Assert.Contains("401 Unauthorized", result.Detail);
    }

    [Fact]
    public async Task Missing_import_blocks_as_dependency_missing()
    {
        var executor = new RecordingProcessExecutor(command =>
            command.Contains("import numpy", StringComparison.Ordinal)
                ? new ProcessResult(1, "", "ModuleNotFoundError: No module named 'numpy'", TimeSpan.FromMilliseconds(2), false)
                : new ProcessResult(0, "Python 3.12", "", TimeSpan.FromMilliseconds(1), false));
        IRuntimePreflightService service = new RuntimePreflightService(executor);
        var profile = new RuntimeProfile(
            "system",
            "System Python",
            RuntimeKind.LocalPython,
            "python",
            dependencyChecks: new[] { new RuntimeDependencyCheck("NumPy", "numpy") });

        var result = await service.CheckAsync(profile);

        Assert.False(result.Passed);
        Assert.Equal(RuntimeFailureKind.DependencyMissing, result.FailureKind);
        Assert.Contains("NumPy", result.Detail);
        Assert.Contains("ModuleNotFoundError", result.Detail);
    }

    [Fact]
    public async Task Failed_version_command_blocks_as_preflight_failed()
    {
        var executor = new RecordingProcessExecutor(command =>
            command.Contains("bad-version-tool", StringComparison.Ordinal)
                ? new ProcessResult(2, "", "bad version command", TimeSpan.FromMilliseconds(1), false)
                : new ProcessResult(0, "Python 3.12", "", TimeSpan.FromMilliseconds(1), false));
        IRuntimePreflightService service = new RuntimePreflightService(executor);
        var profile = new RuntimeProfile(
            "system",
            "System Python",
            RuntimeKind.LocalPython,
            "python",
            versionChecks: new[] { new RuntimeVersionCheck("Python", "bad-version-tool", "--version") });

        var result = await service.CheckAsync(profile);

        Assert.False(result.Passed);
        Assert.Equal(RuntimeFailureKind.PreflightFailed, result.FailureKind);
        Assert.Contains("Python", result.Detail);
        Assert.Contains("bad version command", result.Detail);
    }

    [Fact]
    public async Task Missing_required_environment_variable_blocks_as_preflight_failed()
    {
        var variableName = "METBENCH_PREFLIGHT_TEST_MISSING_ENV";
        Environment.SetEnvironmentVariable(variableName, null);
        var executor = new RecordingProcessExecutor(_ =>
            new ProcessResult(0, "Python 3.12", "", TimeSpan.FromMilliseconds(1), false));
        IRuntimePreflightService service = new RuntimePreflightService(executor);
        var profile = new RuntimeProfile(
            "system",
            "System Python",
            RuntimeKind.LocalPython,
            "python",
            requiredEnvironmentVariables: new[] { variableName });

        var result = await service.CheckAsync(profile);

        Assert.False(result.Passed);
        Assert.Equal(RuntimeFailureKind.PreflightFailed, result.FailureKind);
        Assert.Contains(variableName, result.Detail);
        Assert.Empty(executor.Commands);
    }

    [Fact]
    public async Task Timeout_blocks_as_timeout()
    {
        var executor = new RecordingProcessExecutor(command =>
            command.Contains("slow-version-tool", StringComparison.Ordinal)
                ? new ProcessResult(-1, "", "timed out", TimeSpan.FromSeconds(5), true)
                : new ProcessResult(0, "Python 3.12", "", TimeSpan.FromMilliseconds(1), false));
        IRuntimePreflightService service = new RuntimePreflightService(executor);
        var profile = new RuntimeProfile(
            "system",
            "System Python",
            RuntimeKind.LocalPython,
            "python",
            versionChecks: new[] { new RuntimeVersionCheck("Python", "slow-version-tool", "--version") });

        var result = await service.CheckAsync(profile);

        Assert.False(result.Passed);
        Assert.Equal(RuntimeFailureKind.Timeout, result.FailureKind);
        Assert.Contains("timed out", result.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Successful_checks_return_pass_with_diagnostics()
    {
        var variableName = "METBENCH_PREFLIGHT_TEST_PRESENT_ENV";
        Environment.SetEnvironmentVariable(variableName, "present");
        try
        {
            var executor = new RecordingProcessExecutor(command =>
                command.Contains("import scipy", StringComparison.Ordinal)
                    ? new ProcessResult(0, "", "", TimeSpan.FromMilliseconds(2), false)
                    : new ProcessResult(0, "Python 3.12.4", "", TimeSpan.FromMilliseconds(1), false));
            IRuntimePreflightService service = new RuntimePreflightService(executor);
            var profile = new RuntimeProfile(
                "scipy",
                "SciPy Python",
                RuntimeKind.PythonVirtualEnvironment,
                "python",
                dependencyChecks: new[] { new RuntimeDependencyCheck("SciPy", "scipy") },
                versionChecks: new[] { new RuntimeVersionCheck("Python", "python", "--version") },
                requiredEnvironmentVariables: new[] { variableName });

            var result = await service.CheckAsync(profile);

            Assert.True(result.Passed);
            Assert.Equal(RuntimeFailureKind.None, result.FailureKind);
            Assert.Contains("passed", result.Detail, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(result.Diagnostics, d => d.Name == variableName && d.Passed);
            Assert.Contains(result.Diagnostics, d => d.Name == "Python" && d.Passed && d.Stdout.Contains("Python 3.12.4"));
            Assert.Contains(result.Diagnostics, d => d.Name == "SciPy" && d.Passed);
            Assert.Equal(3, executor.Commands.Count);
        }
        finally
        {
            Environment.SetEnvironmentVariable(variableName, null);
        }
    }

    [Fact]
    public async Task Preflight_never_installs_or_repairs_dependencies()
    {
        var executor = new RecordingProcessExecutor(command =>
            command.Contains("import scipy", StringComparison.Ordinal)
                ? new ProcessResult(1, "", "ModuleNotFoundError", TimeSpan.FromMilliseconds(1), false)
                : new ProcessResult(0, "Python 3.12", "", TimeSpan.FromMilliseconds(1), false));
        IRuntimePreflightService service = new RuntimePreflightService(executor);
        var profile = new RuntimeProfile(
            "system",
            "System Python",
            RuntimeKind.LocalPython,
            "python",
            dependencyChecks: new[] { new RuntimeDependencyCheck("SciPy", "scipy") });

        _ = await service.CheckAsync(profile);

        Assert.DoesNotContain(executor.Commands, command =>
            command.Contains("pip install", StringComparison.OrdinalIgnoreCase)
            || command.Contains("conda install", StringComparison.OrdinalIgnoreCase)
            || command.Contains("repair", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Bare_executable_without_configured_checks_still_runs_startup_probe()
    {
        var executor = new RecordingProcessExecutor(_ =>
            new ProcessResult(0, "Python 3.12", "", TimeSpan.FromMilliseconds(1), false));
        IRuntimePreflightService service = new RuntimePreflightService(executor);
        var profile = new RuntimeProfile("system", "System Python", RuntimeKind.LocalPython, "python");

        var result = await service.CheckAsync(profile);

        Assert.True(result.Passed);
        Assert.Single(executor.Commands);
        Assert.Contains("--version", executor.Commands[0]);
        Assert.Contains(result.Diagnostics, d => d.CheckKind == "startup" && d.Passed);
    }

    [Fact]
    public async Task Bare_executable_startup_failure_blocks_as_runtime_executable_missing()
    {
        var executor = new RecordingProcessExecutor(_ =>
            new ProcessResult(1, "", "command not found", TimeSpan.FromMilliseconds(1), false));
        IRuntimePreflightService service = new RuntimePreflightService(executor);
        var profile = new RuntimeProfile("system", "System Python", RuntimeKind.LocalPython, "python-does-not-exist");

        var result = await service.CheckAsync(profile);

        Assert.False(result.Passed);
        Assert.Equal(RuntimeFailureKind.RuntimeExecutableMissing, result.FailureKind);
        Assert.Single(executor.Commands);
        Assert.Contains("command not found", result.Detail);
    }

    [Fact]
    public async Task Unsafe_version_arguments_block_without_shell_execution()
    {
        var executor = new RecordingProcessExecutor(_ =>
            new ProcessResult(0, "Python 3.12", "", TimeSpan.FromMilliseconds(1), false));
        IRuntimePreflightService service = new RuntimePreflightService(executor);
        var profile = new RuntimeProfile(
            "system",
            "System Python",
            RuntimeKind.LocalPython,
            "python",
            versionChecks: new[] { new RuntimeVersionCheck("Python", "python", "--version & pip install scipy") });

        var result = await service.CheckAsync(profile);

        Assert.False(result.Passed);
        Assert.Equal(RuntimeFailureKind.PreflightFailed, result.FailureKind);
        Assert.Single(executor.Commands);
        Assert.Contains("unsafe", result.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Invalid_import_name_blocks_without_import_shell_execution()
    {
        var executor = new RecordingProcessExecutor(_ =>
            new ProcessResult(0, "Python 3.12", "", TimeSpan.FromMilliseconds(1), false));
        IRuntimePreflightService service = new RuntimePreflightService(executor);
        var profile = new RuntimeProfile(
            "system",
            "System Python",
            RuntimeKind.LocalPython,
            "python",
            dependencyChecks: new[] { new RuntimeDependencyCheck("Bad", "os;import subprocess") });

        var result = await service.CheckAsync(profile);

        Assert.False(result.Passed);
        Assert.Equal(RuntimeFailureKind.PreflightFailed, result.FailureKind);
        Assert.Single(executor.Commands);
        Assert.DoesNotContain(executor.Commands, command => command.Contains("os;import subprocess", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Optional_dependency_failure_is_recorded_as_non_blocking_diagnostic()
    {
        var executor = new RecordingProcessExecutor(command =>
            command.Contains("import optional_missing", StringComparison.Ordinal)
                ? new ProcessResult(1, "", "ModuleNotFoundError", TimeSpan.FromMilliseconds(1), false)
                : new ProcessResult(0, "Python 3.12", "", TimeSpan.FromMilliseconds(1), false));
        IRuntimePreflightService service = new RuntimePreflightService(executor);
        var profile = new RuntimeProfile(
            "system",
            "System Python",
            RuntimeKind.LocalPython,
            "python",
            dependencyChecks: new[] { new RuntimeDependencyCheck("Optional", "optional_missing", Required: false) });

        var result = await service.CheckAsync(profile);

        Assert.True(result.Passed);
        var diagnostic = Assert.Single(result.Diagnostics, d => d.Name == "Optional");
        Assert.False(diagnostic.Passed);
        Assert.False(diagnostic.Blocking);
        Assert.Equal(RuntimeFailureKind.DependencyMissing, diagnostic.FailureKind);
    }

    private sealed class RecordingProcessExecutor : IProcessExecutor
    {
        private readonly Func<string, ProcessResult> _handler;

        public RecordingProcessExecutor(Func<string, ProcessResult> handler)
        {
            _handler = handler;
        }

        public List<string> Commands { get; } = new();

        public Task<ProcessResult> RunAsync(
            ProcessInvocation invocation,
            string workingDirectory,
            int timeoutSeconds,
            CancellationToken cancellationToken)
        {
            var command = ToDisplayString(invocation);
            Commands.Add(command);
            return Task.FromResult(_handler(command));
        }

        private static string ToDisplayString(ProcessInvocation invocation) =>
            string.Join(" ", new[] { invocation.FileName }.Concat(invocation.Arguments.Select(FormatArgument)));

        private static string FormatArgument(string argument)
        {
            if (argument.StartsWith("--", StringComparison.Ordinal)
                || argument.All(ch => char.IsLetterOrDigit(ch) || ch is '-' or '_' or '.' or ':' or '/' or '\\' or '='))
            {
                return argument;
            }

            return "\"" + argument.Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";
        }
    }

    private static RuntimeProfile CreateDockerProfile() =>
        new(
            "openmoc-docker",
            "openmoc-docker Docker MCP",
            RuntimeKind.Docker,
            "/host/openmoc-runner",
            dockerMcp: new DockerMcpRuntimeOptions(
                "http://192.168.1.20:8765",
                "metbench-sut:latest",
                "/opt/openmoc-venv/bin/python",
                ToolName: "openmoc-runner",
                LocalExecutable: "/host/openmoc-runner"));

    private sealed class RecordingDockerMcpRuntimeClient : IDockerMcpRuntimeClient
    {
        private readonly DockerMcpHealthResult _result;

        public RecordingDockerMcpRuntimeClient(DockerMcpHealthResult result)
        {
            _result = result;
        }

        public List<DockerMcpRuntimeOptions> OptionsSeen { get; } = new();

        public Task<DockerMcpHealthResult> HealthAsync(
            DockerMcpRuntimeOptions options,
            CancellationToken cancellationToken = default)
        {
            OptionsSeen.Add(options);
            return Task.FromResult(_result);
        }

        public Task<DockerMcpRunResult> RunSutCommandAsync(
            DockerMcpRuntimeOptions options,
            DockerMcpRunRequest request,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Preflight tests must not execute SUT commands.");
    }
}
