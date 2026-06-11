using System;
using System.Collections.Generic;
using System.Linq;

namespace MetBench_BLL.SystemMT.Runtime;

public interface IRuntimeBackendConfigurationProvider
{
    RuntimeBackendConfiguration Resolve(string backendKey);
}

public sealed class RuntimeBackendConfigurationException : InvalidOperationException
{
    public RuntimeBackendConfigurationException(string message)
        : base(message)
    {
    }
}

public sealed record RuntimeSecretReference
{
    public RuntimeSecretReference(string referenceName)
    {
        if (string.IsNullOrWhiteSpace(referenceName))
            throw new ArgumentException("Secret reference name is required.", nameof(referenceName));

        var normalized = referenceName.Trim();
        if (!IsSupportedReference(normalized))
            throw new ArgumentException("Secret reference scheme is not supported.", nameof(referenceName));

        ReferenceName = normalized;
    }

    public string ReferenceName { get; }

    private static bool IsSupportedReference(string referenceName) =>
        referenceName.StartsWith("env:", StringComparison.OrdinalIgnoreCase) ||
        referenceName.StartsWith("ssh-key:", StringComparison.OrdinalIgnoreCase) ||
        referenceName.StartsWith("key-file:", StringComparison.OrdinalIgnoreCase) ||
        referenceName.StartsWith("password:", StringComparison.OrdinalIgnoreCase) ||
        referenceName.StartsWith("ssh-agent:", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(referenceName, "configured-by-operator", StringComparison.OrdinalIgnoreCase);
}

public sealed record RuntimePathMapping
{
    public RuntimePathMapping(string sourcePath, string targetPath)
    {
        SourcePath = RuntimeBackendPathGuard.RequireSafePath(sourcePath, nameof(sourcePath));
        TargetPath = RuntimeBackendPathGuard.RequireSafePath(targetPath, nameof(targetPath));
    }

    public string SourcePath { get; }
    public string TargetPath { get; }

    public static RuntimePathMapping Create(string sourcePath, string targetPath) =>
        new(sourcePath, targetPath);
}

public sealed record DockerBackendConfiguration
{
    public DockerBackendConfiguration(
        string image,
        string commandTemplate,
        string workDirectory,
        IReadOnlyList<RuntimePathMapping> inputMounts,
        IReadOnlyList<RuntimePathMapping> outputMounts,
        string pullPolicy = "if-missing",
        string entryPoint = "",
        IReadOnlyDictionary<string, string>? environment = null,
        IReadOnlyDictionary<string, RuntimeSecretReference>? secretReferences = null,
        string networkMode = "",
        string user = "",
        int? cpuCores = null,
        long? memoryMegabytes = null,
        bool requiresGpu = false,
        TimeSpan? timeout = null,
        TimeSpan? killTimeout = null,
        string platform = "")
    {
        if (string.IsNullOrWhiteSpace(image))
            throw new ArgumentException("Docker image is required.", nameof(image));
        if (string.IsNullOrWhiteSpace(commandTemplate))
            throw new ArgumentException("Docker command template is required.", nameof(commandTemplate));
        if (string.IsNullOrWhiteSpace(workDirectory))
            throw new ArgumentException("Docker work directory is required.", nameof(workDirectory));
        if (inputMounts is null || inputMounts.Count == 0)
            throw new ArgumentException("At least one Docker input mount is required.", nameof(inputMounts));
        if (outputMounts is null || outputMounts.Count == 0)
            throw new ArgumentException("At least one Docker output mount is required.", nameof(outputMounts));
        if (timeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeout), "Docker timeout must be positive.");
        if (killTimeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(killTimeout), "Docker kill timeout must be positive.");

        Image = image.Trim();
        CommandTemplate = commandTemplate.Trim();
        WorkDirectory = RuntimeBackendPathGuard.RequireSafePath(workDirectory, nameof(workDirectory));
        InputMounts = inputMounts.ToArray();
        OutputMounts = outputMounts.ToArray();
        PullPolicy = string.IsNullOrWhiteSpace(pullPolicy) ? "if-missing" : pullPolicy.Trim();
        EntryPoint = entryPoint?.Trim() ?? string.Empty;
        Environment = CopyStringDictionary(environment);
        SecretReferences = CopySecretDictionary(secretReferences);
        NetworkMode = networkMode?.Trim() ?? string.Empty;
        User = user?.Trim() ?? string.Empty;
        CpuCores = cpuCores;
        MemoryMegabytes = memoryMegabytes;
        RequiresGpu = requiresGpu;
        Timeout = timeout ?? TimeSpan.FromMinutes(30);
        KillTimeout = killTimeout ?? TimeSpan.FromSeconds(10);
        Platform = platform?.Trim() ?? string.Empty;
    }

    public string Image { get; }
    public string CommandTemplate { get; }
    public string WorkDirectory { get; }
    public IReadOnlyList<RuntimePathMapping> InputMounts { get; }
    public IReadOnlyList<RuntimePathMapping> OutputMounts { get; }
    public string PullPolicy { get; }
    public string EntryPoint { get; }
    public IReadOnlyDictionary<string, string> Environment { get; }
    public IReadOnlyDictionary<string, RuntimeSecretReference> SecretReferences { get; }
    public string NetworkMode { get; }
    public string User { get; }
    public int? CpuCores { get; }
    public long? MemoryMegabytes { get; }
    public bool RequiresGpu { get; }
    public TimeSpan Timeout { get; }
    public TimeSpan KillTimeout { get; }
    public string Platform { get; }

    private static IReadOnlyDictionary<string, string> CopyStringDictionary(IReadOnlyDictionary<string, string>? source) =>
        source is null
            ? new Dictionary<string, string>(StringComparer.Ordinal)
            : new Dictionary<string, string>(source, StringComparer.Ordinal);

    private static IReadOnlyDictionary<string, RuntimeSecretReference> CopySecretDictionary(
        IReadOnlyDictionary<string, RuntimeSecretReference>? source) =>
        source is null
            ? new Dictionary<string, RuntimeSecretReference>(StringComparer.Ordinal)
            : new Dictionary<string, RuntimeSecretReference>(source, StringComparer.Ordinal);
}

public sealed record SshBackendConfiguration
{
    public SshBackendConfiguration(
        string host,
        string user,
        RuntimeSecretReference authSecret,
        string remoteRoot,
        string remoteWorkDirectoryTemplate,
        string commandTemplate,
        IReadOnlyList<string> inputPaths,
        IReadOnlyList<string> outputPaths,
        int port = 22,
        string uploadStrategy = "sftp",
        string downloadStrategy = "sftp",
        IReadOnlyDictionary<string, string>? environment = null,
        IReadOnlyDictionary<string, RuntimeSecretReference>? secretReferences = null,
        bool keepRemoteWorkDirectory = false,
        TimeSpan? connectionTimeout = null,
        TimeSpan? commandTimeout = null,
        TimeSpan? statusPollTimeout = null,
        string proxyCommandReference = "")
    {
        if (string.IsNullOrWhiteSpace(host))
            throw new ArgumentException("SSH host is required.", nameof(host));
        if (string.IsNullOrWhiteSpace(user))
            throw new ArgumentException("SSH user is required.", nameof(user));
        if (authSecret is null)
            throw new ArgumentNullException(nameof(authSecret));
        if (string.IsNullOrWhiteSpace(remoteRoot))
            throw new ArgumentException("SSH remote root is required.", nameof(remoteRoot));
        if (string.IsNullOrWhiteSpace(remoteWorkDirectoryTemplate))
            throw new ArgumentException("SSH remote work directory template is required.", nameof(remoteWorkDirectoryTemplate));
        if (string.IsNullOrWhiteSpace(commandTemplate))
            throw new ArgumentException("SSH command template is required.", nameof(commandTemplate));
        if (inputPaths is null || inputPaths.Count == 0)
            throw new ArgumentException("At least one SSH input path is required.", nameof(inputPaths));
        if (outputPaths is null || outputPaths.Count == 0)
            throw new ArgumentException("At least one SSH output path is required.", nameof(outputPaths));
        if (port <= 0 || port > 65535)
            throw new ArgumentOutOfRangeException(nameof(port), "SSH port must be between 1 and 65535.");
        if (connectionTimeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(connectionTimeout), "SSH connection timeout must be positive.");
        if (commandTimeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(commandTimeout), "SSH command timeout must be positive.");
        if (statusPollTimeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(statusPollTimeout), "SSH status poll timeout must be positive.");

        Host = host.Trim();
        User = user.Trim();
        AuthSecret = authSecret;
        RemoteRoot = RuntimeBackendPathGuard.RequireSafePath(remoteRoot, nameof(remoteRoot));
        RemoteWorkDirectoryTemplate = RuntimeBackendPathGuard.RequireSafePath(remoteWorkDirectoryTemplate, nameof(remoteWorkDirectoryTemplate));
        CommandTemplate = commandTemplate.Trim();
        InputPaths = inputPaths.Select(path => RuntimeBackendPathGuard.RequireSafePath(path, nameof(inputPaths))).ToArray();
        OutputPaths = outputPaths.Select(path => RuntimeBackendPathGuard.RequireSafePath(path, nameof(outputPaths))).ToArray();
        Port = port;
        UploadStrategy = string.IsNullOrWhiteSpace(uploadStrategy) ? "sftp" : uploadStrategy.Trim();
        DownloadStrategy = string.IsNullOrWhiteSpace(downloadStrategy) ? "sftp" : downloadStrategy.Trim();
        Environment = CopyStringDictionary(environment);
        SecretReferences = CopySecretDictionary(secretReferences);
        KeepRemoteWorkDirectory = keepRemoteWorkDirectory;
        ConnectionTimeout = connectionTimeout ?? TimeSpan.FromSeconds(30);
        CommandTimeout = commandTimeout ?? TimeSpan.FromMinutes(30);
        StatusPollTimeout = statusPollTimeout ?? TimeSpan.FromSeconds(30);
        ProxyCommandReference = proxyCommandReference?.Trim() ?? string.Empty;
    }

    public string Host { get; }
    public string User { get; }
    public RuntimeSecretReference AuthSecret { get; }
    public string RemoteRoot { get; }
    public string RemoteWorkDirectoryTemplate { get; }
    public string CommandTemplate { get; }
    public IReadOnlyList<string> InputPaths { get; }
    public IReadOnlyList<string> OutputPaths { get; }
    public int Port { get; }
    public string UploadStrategy { get; }
    public string DownloadStrategy { get; }
    public IReadOnlyDictionary<string, string> Environment { get; }
    public IReadOnlyDictionary<string, RuntimeSecretReference> SecretReferences { get; }
    public bool KeepRemoteWorkDirectory { get; }
    public TimeSpan ConnectionTimeout { get; }
    public TimeSpan CommandTimeout { get; }
    public TimeSpan StatusPollTimeout { get; }
    public string ProxyCommandReference { get; }

    private static IReadOnlyDictionary<string, string> CopyStringDictionary(IReadOnlyDictionary<string, string>? source) =>
        source is null
            ? new Dictionary<string, string>(StringComparer.Ordinal)
            : new Dictionary<string, string>(source, StringComparer.Ordinal);

    private static IReadOnlyDictionary<string, RuntimeSecretReference> CopySecretDictionary(
        IReadOnlyDictionary<string, RuntimeSecretReference>? source) =>
        source is null
            ? new Dictionary<string, RuntimeSecretReference>(StringComparer.Ordinal)
            : new Dictionary<string, RuntimeSecretReference>(source, StringComparer.Ordinal);
}

public sealed record RuntimeBackendConfiguration
{
    private RuntimeBackendConfiguration(
        string backendKey,
        RuntimeBackendKind kind,
        DockerBackendConfiguration? docker,
        SshBackendConfiguration? ssh,
        RuntimeResourceHints? resourceHints,
        RuntimeArtifactPolicy? artifactPolicy)
    {
        if (string.IsNullOrWhiteSpace(backendKey))
            throw new ArgumentException("Backend key is required.", nameof(backendKey));
        if (kind == RuntimeBackendKind.Docker && docker is null)
            throw new ArgumentException("Docker backend configuration is required.", nameof(docker));
        if (kind == RuntimeBackendKind.SshRemote && ssh is null)
            throw new ArgumentException("SSH backend configuration is required.", nameof(ssh));

        BackendKey = RuntimeProfile.NormalizeRuntimeKey(backendKey);
        Kind = kind;
        DockerBackend = docker;
        SshBackend = ssh;
        ResourceHints = resourceHints ?? new RuntimeResourceHints();
        ArtifactPolicy = artifactPolicy ?? new RuntimeArtifactPolicy();
    }

    public string BackendKey { get; }
    public RuntimeBackendKind Kind { get; }
    public DockerBackendConfiguration? DockerBackend { get; }
    public SshBackendConfiguration? SshBackend { get; }
    public RuntimeResourceHints ResourceHints { get; }
    public RuntimeArtifactPolicy ArtifactPolicy { get; }

    public static RuntimeBackendConfiguration Docker(
        string backendKey,
        DockerBackendConfiguration docker,
        RuntimeResourceHints? resourceHints = null,
        RuntimeArtifactPolicy? artifactPolicy = null) =>
        new(backendKey, RuntimeBackendKind.Docker, docker ?? throw new ArgumentNullException(nameof(docker)), null, resourceHints, artifactPolicy);

    public static RuntimeBackendConfiguration Ssh(
        string backendKey,
        SshBackendConfiguration ssh,
        RuntimeResourceHints? resourceHints = null,
        RuntimeArtifactPolicy? artifactPolicy = null) =>
        new(backendKey, RuntimeBackendKind.SshRemote, null, ssh ?? throw new ArgumentNullException(nameof(ssh)), resourceHints, artifactPolicy);

    public IReadOnlyDictionary<string, string> ToSanitizedDiagnostic()
    {
        var diagnostic = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["backend_key"] = BackendKey,
            ["kind"] = Kind == RuntimeBackendKind.Docker ? "docker" : "ssh"
        };

        if (DockerBackend is not null)
        {
            diagnostic["docker_image"] = DockerBackend.Image;
            diagnostic["docker_workdir"] = DockerBackend.WorkDirectory;
            diagnostic["docker_pull_policy"] = DockerBackend.PullPolicy;
            foreach (var pair in DockerBackend.SecretReferences)
                diagnostic[$"secret_ref:{pair.Key}"] = pair.Value.ReferenceName;
        }

        if (SshBackend is not null)
        {
            diagnostic["ssh_host"] = SshBackend.Host;
            diagnostic["ssh_user"] = SshBackend.User;
            diagnostic["ssh_remote_root"] = SshBackend.RemoteRoot;
            diagnostic["ssh_auth_ref"] = SshBackend.AuthSecret.ReferenceName;
            foreach (var pair in SshBackend.SecretReferences)
                diagnostic[$"secret_ref:{pair.Key}"] = pair.Value.ReferenceName;
        }

        return diagnostic;
    }
}

public sealed class InMemoryRuntimeBackendConfigurationProvider : IRuntimeBackendConfigurationProvider
{
    private readonly IReadOnlyDictionary<string, RuntimeBackendConfiguration> _configurations;

    public InMemoryRuntimeBackendConfigurationProvider(IEnumerable<RuntimeBackendConfiguration> configurations)
    {
        if (configurations is null)
            throw new ArgumentNullException(nameof(configurations));

        _configurations = configurations.ToDictionary(
            configuration => configuration.BackendKey,
            configuration => configuration,
            StringComparer.OrdinalIgnoreCase);
    }

    public RuntimeBackendConfiguration Resolve(string backendKey)
    {
        var normalizedKey = RuntimeProfile.NormalizeRuntimeKey(backendKey);
        if (_configurations.TryGetValue(normalizedKey, out var configuration))
            return configuration;

        throw new RuntimeBackendConfigurationException(
            $"Runtime backend configuration '{normalizedKey}' was not found.");
    }
}

internal static class RuntimeBackendPathGuard
{
    public static string RequireSafePath(string path, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Runtime backend path is required.", parameterName);

        var value = path.Trim();
        if (value.IndexOfAny(new[] { '\0', '\r', '\n' }) >= 0)
            throw new ArgumentException("Runtime backend path contains control characters.", parameterName);
        if (HasTraversalSegment(value))
            throw new ArgumentException("Runtime backend path must not contain traversal segments.", parameterName);

        return value;
    }

    private static bool HasTraversalSegment(string path)
    {
        foreach (var segment in path.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries))
        {
            if (segment == "..")
                return true;
        }

        return false;
    }
}
