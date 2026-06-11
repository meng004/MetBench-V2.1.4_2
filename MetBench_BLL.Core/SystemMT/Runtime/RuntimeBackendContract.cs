using System;
using System.Collections.Generic;

namespace MetBench_BLL.SystemMT.Runtime;

public enum RuntimeBackendKind
{
    Docker,
    SshRemote
}

public sealed record RuntimeBackendContract
{
    public RuntimeBackendContract(
        string backendKey,
        RuntimeBackendKind kind,
        string runtimeKey,
        string displayName,
        IReadOnlyDictionary<string, string> settings,
        RuntimeResourceHints? resourceHints = null,
        RuntimeArtifactPolicy? artifactPolicy = null)
    {
        if (string.IsNullOrWhiteSpace(backendKey))
            throw new ArgumentException("Backend key is required.", nameof(backendKey));
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("Backend display name is required.", nameof(displayName));
        if (settings is null || settings.Count == 0)
            throw new ArgumentException("Backend settings are required.", nameof(settings));

        BackendKey = RuntimeProfile.NormalizeRuntimeKey(backendKey);
        Kind = kind;
        RuntimeKey = RuntimeProfile.NormalizeRuntimeKey(runtimeKey);
        DisplayName = displayName;
        Settings = new Dictionary<string, string>(settings, StringComparer.Ordinal);
        ResourceHints = resourceHints ?? new RuntimeResourceHints();
        ArtifactPolicy = artifactPolicy ?? new RuntimeArtifactPolicy();
    }

    public string BackendKey { get; }
    public RuntimeBackendKind Kind { get; }
    public string RuntimeKey { get; }
    public string DisplayName { get; }
    public IReadOnlyDictionary<string, string> Settings { get; }
    public RuntimeResourceHints ResourceHints { get; }
    public RuntimeArtifactPolicy ArtifactPolicy { get; }

    public RuntimeProfile ToRuntimeProfile()
    {
        var runtimeKind = Kind switch
        {
            RuntimeBackendKind.Docker => RuntimeKind.DockerPlaceholder,
            RuntimeBackendKind.SshRemote => RuntimeKind.RemotePlaceholder,
            _ => RuntimeKind.RemotePlaceholder
        };

        return RuntimeProfile.Placeholder(RuntimeKey, DisplayName, runtimeKind);
    }

    public static RuntimeBackendContract Docker(
        string backendKey,
        string image,
        string runtimeKey = "docker",
        string? displayName = null,
        RuntimeResourceHints? resourceHints = null,
        RuntimeArtifactPolicy? artifactPolicy = null)
    {
        if (string.IsNullOrWhiteSpace(image))
            throw new ArgumentException("Docker image is required.", nameof(image));

        return new RuntimeBackendContract(
            backendKey,
            RuntimeBackendKind.Docker,
            runtimeKey,
            displayName ?? $"Docker runtime {backendKey}",
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["image"] = image
            },
            resourceHints,
            artifactPolicy);
    }

    public static RuntimeBackendContract SshRemote(
        string backendKey,
        string host,
        string remoteRoot,
        string runtimeKey = "ssh",
        string? displayName = null,
        RuntimeResourceHints? resourceHints = null,
        RuntimeArtifactPolicy? artifactPolicy = null)
    {
        if (string.IsNullOrWhiteSpace(host))
            throw new ArgumentException("SSH host is required.", nameof(host));
        if (string.IsNullOrWhiteSpace(remoteRoot))
            throw new ArgumentException("SSH remote root is required.", nameof(remoteRoot));

        return new RuntimeBackendContract(
            backendKey,
            RuntimeBackendKind.SshRemote,
            runtimeKey,
            displayName ?? $"SSH runtime {backendKey}",
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["host"] = host,
                ["remote_root"] = remoteRoot
            },
            resourceHints,
            artifactPolicy);
    }
}
