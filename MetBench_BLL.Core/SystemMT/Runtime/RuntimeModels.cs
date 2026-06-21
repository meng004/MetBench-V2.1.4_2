using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MetBench_BLL.SystemMT.Runtime;

public enum RuntimeKind
{
    LocalPython,
    PythonVirtualEnvironment,
    Docker,
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

[JsonConverter(typeof(RuntimeVersionCheckJsonConverter))]
public sealed record RuntimeVersionCheck
{
    public RuntimeVersionCheck(
        string Name,
        string Command,
        string Arguments = "--version",
        TimeSpan? Timeout = null)
    {
        this.Name = Name ?? string.Empty;
        Executable = Command ?? string.Empty;
        this.Arguments = Arguments ?? string.Empty;
        ArgumentList = SplitLegacyArguments(this.Arguments);
        this.Timeout = Timeout;
    }

    public RuntimeVersionCheck(
        string name,
        string executable,
        IReadOnlyList<string> argumentList,
        TimeSpan? timeout = null)
    {
        Name = name ?? string.Empty;
        Executable = executable ?? string.Empty;
        ArgumentList = argumentList?.ToArray() ?? new[] { "--version" };
        Arguments = JoinLegacyArguments(ArgumentList);
        Timeout = timeout;
    }

    public string Name { get; init; }

    public string Executable { get; init; }

    [Obsolete("Use Executable. Command is kept for source and JSON compatibility.")]
    public string Command
    {
        get => Executable;
        init
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                Executable = value;
            }
        }
    }

    public string Arguments { get; init; }

    public IReadOnlyList<string> ArgumentList { get; init; }

    public TimeSpan? Timeout { get; init; }

    [Obsolete("Use explicit properties. This preserves the old positional record shape.")]
    public void Deconstruct(out string name, out string executableCommand, out string arguments, out TimeSpan? timeout)
    {
        name = Name;
        executableCommand = Executable;
        arguments = Arguments;
        timeout = Timeout;
    }

    private static IReadOnlyList<string> SplitLegacyArguments(string arguments)
    {
        return string.IsNullOrWhiteSpace(arguments)
            ? Array.Empty<string>()
            : arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    }

    private static string JoinLegacyArguments(IReadOnlyList<string> arguments) =>
        string.Join(" ", arguments);
}

internal sealed class RuntimeVersionCheckJsonConverter : JsonConverter<RuntimeVersionCheck>
{
    public override RuntimeVersionCheck Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;

        var name = ReadString(root, nameof(RuntimeVersionCheck.Name)) ?? string.Empty;
        var executable = ReadString(root, nameof(RuntimeVersionCheck.Executable));
        var command = ReadString(root, "Command");
        var resolvedExecutable = !string.IsNullOrWhiteSpace(executable)
            ? executable!
            : command ?? string.Empty;
        var timeout = ReadTimeout(root, options);

        if (TryReadStringArray(root, nameof(RuntimeVersionCheck.ArgumentList), out var argumentList)
            || TryReadStringArray(root, nameof(RuntimeVersionCheck.Arguments), out argumentList))
        {
            return new RuntimeVersionCheck(name, resolvedExecutable, argumentList, timeout);
        }

        var legacyArguments = ReadString(root, nameof(RuntimeVersionCheck.Arguments));
        return new RuntimeVersionCheck(name, resolvedExecutable, legacyArguments ?? "--version", timeout);
    }

    public override void Write(
        Utf8JsonWriter writer,
        RuntimeVersionCheck value,
        JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString(nameof(RuntimeVersionCheck.Name), value.Name);
        writer.WriteString(nameof(RuntimeVersionCheck.Executable), value.Executable);
        writer.WriteString(nameof(RuntimeVersionCheck.Arguments), value.Arguments);
        writer.WritePropertyName(nameof(RuntimeVersionCheck.ArgumentList));
        JsonSerializer.Serialize(writer, value.ArgumentList, options);
        if (value.Timeout is not null)
        {
            writer.WritePropertyName(nameof(RuntimeVersionCheck.Timeout));
            JsonSerializer.Serialize(writer, value.Timeout, options);
        }
        writer.WriteEndObject();
    }

    private static string? ReadString(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var property)
            && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
    }

    private static bool TryReadStringArray(
        JsonElement root,
        string propertyName,
        out IReadOnlyList<string> values)
    {
        values = Array.Empty<string>();
        if (!root.TryGetProperty(propertyName, out var property)
            || property.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        values = property
            .EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item => item.GetString() ?? string.Empty)
            .ToArray();
        return true;
    }

    private static TimeSpan? ReadTimeout(JsonElement root, JsonSerializerOptions options)
    {
        if (!root.TryGetProperty(nameof(RuntimeVersionCheck.Timeout), out var property)
            || property.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return property.Deserialize<TimeSpan?>(options);
    }
}

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

public enum DockerMcpPathStyle
{
    None = 0,
    Wsl = 1,
}

public sealed record DockerMcpRuntimeOptions(
    string Endpoint,
    string Image,
    string PythonExecutable,
    string? AuthTokenEnvironmentVariable = null,
    string? LocalPythonExecutable = null,
    DockerMcpPathStyle PathStyle = DockerMcpPathStyle.None,
    string ToolName = "",
    string LocalExecutable = "");

public sealed record DockerMcpRunRequest(
    string Image,
    string Tool,
    IReadOnlyList<string> Args,
    string WorkingDirectory,
    int TimeoutSeconds);

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
        RuntimeArtifactPolicy? artifactPolicy = null,
        DockerMcpRuntimeOptions? dockerMcp = null)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("Runtime display name is required.", nameof(displayName));
        if (timeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeout), "Runtime timeout must be positive.");
        if (kind == RuntimeKind.Docker && dockerMcp is null)
            throw new ArgumentException("Docker runtime profiles require Docker MCP options.", nameof(dockerMcp));

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
        DockerMcp = dockerMcp;
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

    public DockerMcpRuntimeOptions? DockerMcp { get; }

    public bool IsExecutableInV1 =>
        Kind is RuntimeKind.LocalPython or RuntimeKind.PythonVirtualEnvironment or RuntimeKind.Docker;

    public static RuntimeProfile Placeholder(string runtimeKey, string displayName, RuntimeKind kind)
    {
        if (kind is RuntimeKind.LocalPython or RuntimeKind.PythonVirtualEnvironment or RuntimeKind.Docker)
            throw new ArgumentException("Executable runtime kinds require an executable path.", nameof(kind));

        return new RuntimeProfile(NormalizeRuntimeKey(runtimeKey), displayName, kind, executablePath: null);
    }

    internal static string NormalizeRuntimeKey(string? runtimeKey) =>
        string.IsNullOrWhiteSpace(runtimeKey)
            ? "system"
            : runtimeKey.Trim().ToLowerInvariant();
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
