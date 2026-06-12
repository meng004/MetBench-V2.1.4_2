using System;
using System.Collections.Generic;
using System.Linq;

namespace MetBench_BLL.SystemMT.Runtime;

public enum RuntimeKind
{
    LocalPython,
    PythonVirtualEnvironment,
    DockerContainer,
    DockerPlaceholder,
    RemotePlaceholder,
    HpcPlaceholder
}

public enum RuntimeFailureKind
{
    None,
    RuntimeProfileMissing,
    RuntimeExecutableMissing,
    DependencyMissing,
    MiddlewareUnavailable,
    PreflightFailed,
    SutStartupFailure,
    SutRuntimeFailure,
    AdapterFailure,
    MetBenchPipelineFailure,
    AssertionFailure,
    Timeout,
    Cancelled
}

public sealed record RuntimeDependencyCheck(
    string Name,
    string ImportName,
    bool Required = true);

public sealed record RuntimeVersionCheck(
    string Name,
    string Command,
    string Arguments = "--version",
    TimeSpan? Timeout = null);

public sealed record RuntimePreflightDiagnostic(
    string CheckKind,
    string Name,
    bool Passed,
    RuntimeFailureKind FailureKind,
    string Detail,
    bool Blocking = true,
    int? ExitCode = null,
    bool TimedOut = false,
    string Stdout = "",
    string Stderr = "");

public sealed record RuntimeResourceHints(
    int? CpuCores = null,
    long? MemoryMegabytes = null,
    bool RequiresGpu = false);

public sealed record RuntimeArtifactPolicy(
    bool PreserveInputs = true,
    bool PreserveOutputs = true,
    bool PreserveLogs = true,
    string ArtifactRoot = "");

public sealed record RuntimeProfile
{
    public RuntimeProfile(
        string runtimeKey,
        string displayName,
        RuntimeKind kind,
        string? executablePath,
        IReadOnlyList<RuntimeDependencyCheck>? dependencyChecks = null,
        IReadOnlyList<RuntimeVersionCheck>? versionChecks = null,
        IReadOnlyList<string>? requiredEnvironmentVariables = null,
        TimeSpan? timeout = null,
        RuntimeResourceHints? resourceHints = null,
        RuntimeArtifactPolicy? artifactPolicy = null)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("Runtime display name is required.", nameof(displayName));
        if (timeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeout), "Runtime timeout must be positive.");

        RuntimeKey = NormalizeRuntimeKey(runtimeKey);
        DisplayName = displayName;
        Kind = kind;
        ExecutablePath = executablePath;
        DependencyChecks = dependencyChecks?.ToArray() ?? Array.Empty<RuntimeDependencyCheck>();
        VersionChecks = versionChecks?.ToArray() ?? Array.Empty<RuntimeVersionCheck>();
        RequiredEnvironmentVariables = requiredEnvironmentVariables?.ToArray() ?? Array.Empty<string>();
        Timeout = timeout ?? TimeSpan.FromSeconds(30);
        ResourceHints = resourceHints ?? new RuntimeResourceHints();
        ArtifactPolicy = artifactPolicy ?? new RuntimeArtifactPolicy();
    }

    public string RuntimeKey { get; }
    public string DisplayName { get; }
    public RuntimeKind Kind { get; }
    public string? ExecutablePath { get; }
    public IReadOnlyList<RuntimeDependencyCheck> DependencyChecks { get; }

    public IReadOnlyList<RuntimeVersionCheck> VersionChecks { get; }

    public IReadOnlyList<string> RequiredEnvironmentVariables { get; }

    public TimeSpan Timeout { get; }

    public RuntimeResourceHints ResourceHints { get; }

    public RuntimeArtifactPolicy ArtifactPolicy { get; }

    public bool IsExecutableInV1 =>
        Kind is RuntimeKind.LocalPython
            or RuntimeKind.PythonVirtualEnvironment
            or RuntimeKind.DockerContainer;

    public static RuntimeProfile Placeholder(string runtimeKey, string displayName, RuntimeKind kind)
    {
        if (kind is RuntimeKind.LocalPython or RuntimeKind.PythonVirtualEnvironment)
            throw new ArgumentException("Executable runtime kinds require an executable path.", nameof(kind));

        return new RuntimeProfile(NormalizeRuntimeKey(runtimeKey), displayName, kind, executablePath: null);
    }

    public static RuntimeProfile DockerContainer(
        string runtimeKey,
        string displayName,
        string image,
        string probeCommand,
        TimeSpan? timeout = null)
    {
        if (!IsSafeDockerImage(image))
            throw new ArgumentException("Docker image name contains unsupported characters.", nameof(image));
        if (!IsSafeDockerProbeCommand(probeCommand))
            throw new ArgumentException("Docker probe command contains unsupported characters.", nameof(probeCommand));

        return new RuntimeProfile(
            runtimeKey,
            displayName,
            RuntimeKind.DockerContainer,
            "docker",
            versionChecks: new[]
            {
                new RuntimeVersionCheck("Docker image", "docker", $"image inspect {image}", timeout),
                new RuntimeVersionCheck("Docker container probe", "docker", $"run --rm {image} {probeCommand}", timeout)
            },
            timeout: timeout ?? TimeSpan.FromSeconds(60));
    }

    internal static string NormalizeRuntimeKey(string? runtimeKey) =>
        string.IsNullOrWhiteSpace(runtimeKey)
            ? "system"
            : runtimeKey.Trim().ToLowerInvariant();

    private static bool IsSafeDockerImage(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        foreach (var ch in value)
        {
            if (char.IsLetterOrDigit(ch)
                || ch is '-' or '_' or '.' or ':' or '/' or '@')
            {
                continue;
            }

            return false;
        }

        return true;
    }

    private static bool IsSafeDockerProbeCommand(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        foreach (var ch in value)
        {
            if (char.IsLetterOrDigit(ch)
                || ch is '-' or '_' or '.' or ':' or '/' or '=' or '+' or ' ')
            {
                continue;
            }

            return false;
        }

        return true;
    }
}

public sealed record RuntimePreflightResult
{
    public RuntimePreflightResult(
        RuntimeProfile profile,
        bool passed,
        RuntimeFailureKind failureKind,
        string detail,
        IReadOnlyList<RuntimePreflightDiagnostic>? diagnostics = null)
    {
        if (passed && failureKind != RuntimeFailureKind.None)
            throw new ArgumentException("Passing preflight results must use failure kind None.", nameof(failureKind));
        if (!passed && failureKind == RuntimeFailureKind.None)
            throw new ArgumentException("Failed preflight results require a failure kind.", nameof(failureKind));

        Profile = profile ?? throw new ArgumentNullException(nameof(profile));
        Passed = passed;
        FailureKind = failureKind;
        Detail = detail ?? string.Empty;
        Diagnostics = diagnostics?.ToArray() ?? Array.Empty<RuntimePreflightDiagnostic>();

        foreach (var diagnostic in Diagnostics)
        {
            if (diagnostic.Passed && diagnostic.FailureKind != RuntimeFailureKind.None)
                throw new ArgumentException("Passing diagnostics must use failure kind None.", nameof(diagnostics));
            if (!diagnostic.Passed && diagnostic.FailureKind == RuntimeFailureKind.None)
                throw new ArgumentException("Failed diagnostics require a failure kind.", nameof(diagnostics));
            if (!diagnostic.Passed && string.IsNullOrWhiteSpace(diagnostic.Detail))
                throw new ArgumentException("Failed diagnostics require detail.", nameof(diagnostics));
        }
        if (passed && Diagnostics.Any(d => !d.Passed && d.Blocking))
            throw new ArgumentException("Passing preflight results cannot contain blocking failed diagnostics.", nameof(diagnostics));
        if (!passed && Diagnostics.Count > 0
            && !Diagnostics.Any(d => !d.Passed && d.Blocking && d.FailureKind == failureKind))
        {
            throw new ArgumentException("Failed preflight results require a matching blocking diagnostic.", nameof(diagnostics));
        }
    }

    public RuntimeProfile Profile { get; }
    public bool Passed { get; }
    public RuntimeFailureKind FailureKind { get; }
    public string Detail { get; }
    public IReadOnlyList<RuntimePreflightDiagnostic> Diagnostics { get; }

    public static RuntimePreflightResult Pass(
        RuntimeProfile profile,
        string detail = "",
        IReadOnlyList<RuntimePreflightDiagnostic>? diagnostics = null) =>
        new(profile, true, RuntimeFailureKind.None, detail, diagnostics);

    public static RuntimePreflightResult Blocked(
        RuntimeProfile profile,
        RuntimeFailureKind failureKind,
        string detail,
        IReadOnlyList<RuntimePreflightDiagnostic>? diagnostics = null) =>
        new(profile, false, failureKind, detail, diagnostics);
}
