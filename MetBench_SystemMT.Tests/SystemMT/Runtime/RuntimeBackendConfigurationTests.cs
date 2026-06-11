using MetBench_BLL.SystemMT.Runtime;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Runtime;

public sealed class RuntimeBackendConfigurationTests
{
    [Fact]
    public void Docker_configuration_requires_executable_surface_and_artifact_mounts()
    {
        var config = RuntimeBackendConfiguration.Docker(
            "sciml-mgn-docker",
            new DockerBackendConfiguration(
                image: "metbench/sciml-mgn:cpu",
                commandTemplate: "python tools/run_mgn.py --input {input} --output {output}",
                workDirectory: "/workspace",
                inputMounts: new[] { RuntimePathMapping.Create("/host/in", "/workspace/in") },
                outputMounts: new[] { RuntimePathMapping.Create("/host/out", "/workspace/out") }));

        Assert.Equal("sciml-mgn-docker", config.BackendKey);
        Assert.Equal(RuntimeBackendKind.Docker, config.Kind);
        Assert.Equal("metbench/sciml-mgn:cpu", config.DockerBackend!.Image);
        Assert.Null(config.SshBackend);
    }

    [Fact]
    public void Docker_configuration_rejects_missing_image()
    {
        Assert.Throws<ArgumentException>(() =>
            new DockerBackendConfiguration(
                image: " ",
                commandTemplate: "python run.py",
                workDirectory: "/workspace",
                inputMounts: new[] { RuntimePathMapping.Create("/host/in", "/workspace/in") },
                outputMounts: new[] { RuntimePathMapping.Create("/host/out", "/workspace/out") }));
    }

    [Fact]
    public void Docker_configuration_rejects_missing_artifact_mount()
    {
        Assert.Throws<ArgumentException>(() =>
            new DockerBackendConfiguration(
                image: "metbench/sciml-mgn:cpu",
                commandTemplate: "python run.py",
                workDirectory: "/workspace",
                inputMounts: new[] { RuntimePathMapping.Create("/host/in", "/workspace/in") },
                outputMounts: Array.Empty<RuntimePathMapping>()));
    }

    [Fact]
    public void Ssh_configuration_requires_connection_auth_staging_and_artifacts()
    {
        var config = RuntimeBackendConfiguration.Ssh(
            "sciml-mgn-ssh",
            new SshBackendConfiguration(
                host: "gpu01.example.org",
                user: "metbench",
                authSecret: new RuntimeSecretReference("ssh-key:metbench-mgn"),
                remoteRoot: "/data/metbench/mgn",
                remoteWorkDirectoryTemplate: "{remote_root}/jobs/{job_id}",
                commandTemplate: "python tools/run_mgn.py --input {input} --output {output}",
                inputPaths: new[] { "inputs/source.json" },
                outputPaths: new[] { "outputs/result.json" }));

        Assert.Equal("sciml-mgn-ssh", config.BackendKey);
        Assert.Equal(RuntimeBackendKind.SshRemote, config.Kind);
        Assert.Equal("gpu01.example.org", config.SshBackend!.Host);
        Assert.Null(config.DockerBackend);
    }

    [Theory]
    [InlineData("", "metbench", "/data/metbench/mgn")]
    [InlineData("gpu01.example.org", "", "/data/metbench/mgn")]
    [InlineData("gpu01.example.org", "metbench", "")]
    public void Ssh_configuration_rejects_missing_required_fields(string host, string user, string remoteRoot)
    {
        Assert.Throws<ArgumentException>(() =>
            new SshBackendConfiguration(
                host: host,
                user: user,
                authSecret: new RuntimeSecretReference("ssh-key:metbench-mgn"),
                remoteRoot: remoteRoot,
                remoteWorkDirectoryTemplate: "{remote_root}/jobs/{job_id}",
                commandTemplate: "python run.py",
                inputPaths: new[] { "inputs/source.json" },
                outputPaths: new[] { "outputs/result.json" }));
    }

    [Fact]
    public void Ssh_configuration_rejects_artifact_path_traversal()
    {
        Assert.Throws<ArgumentException>(() =>
            new SshBackendConfiguration(
                host: "gpu01.example.org",
                user: "metbench",
                authSecret: new RuntimeSecretReference("ssh-key:metbench-mgn"),
                remoteRoot: "/data/metbench/mgn",
                remoteWorkDirectoryTemplate: "{remote_root}/jobs/{job_id}",
                commandTemplate: "python run.py",
                inputPaths: new[] { "../secrets/source.json" },
                outputPaths: new[] { "outputs/result.json" }));
    }

    [Fact]
    public void Secret_reference_rejects_unsupported_reference_scheme()
    {
        Assert.Throws<ArgumentException>(() => new RuntimeSecretReference("raw-secret-value"));
    }

    [Fact]
    public void Sanitized_diagnostics_preserve_secret_reference_names_without_secret_values()
    {
        var config = RuntimeBackendConfiguration.Ssh(
            "sciml-mgn-ssh",
            new SshBackendConfiguration(
                host: "gpu01.example.org",
                user: "metbench",
                authSecret: new RuntimeSecretReference("ssh-key:metbench-mgn"),
                remoteRoot: "/data/metbench/mgn",
                remoteWorkDirectoryTemplate: "{remote_root}/jobs/{job_id}",
                commandTemplate: "python run.py",
                environment: new Dictionary<string, string>
                {
                    ["TOKEN"] = "raw-secret-value"
                },
                secretReferences: new Dictionary<string, RuntimeSecretReference>
                {
                    ["TOKEN"] = new("env:MGN_TOKEN")
                },
                inputPaths: new[] { "inputs/source.json" },
                outputPaths: new[] { "outputs/result.json" }));

        var diagnostic = config.ToSanitizedDiagnostic();

        Assert.Equal("sciml-mgn-ssh", diagnostic["backend_key"]);
        Assert.Equal("ssh", diagnostic["kind"]);
        Assert.Equal("gpu01.example.org", diagnostic["ssh_host"]);
        Assert.Equal("ssh-key:metbench-mgn", diagnostic["ssh_auth_ref"]);
        Assert.Equal("env:MGN_TOKEN", diagnostic["secret_ref:TOKEN"]);
        Assert.DoesNotContain(diagnostic, pair => pair.Value == "raw-secret-value");
    }

    [Fact]
    public void In_memory_provider_resolves_backend_key_case_insensitively()
    {
        var expected = RuntimeBackendConfiguration.Docker(
            "sciml-mgn-docker",
            new DockerBackendConfiguration(
                image: "metbench/sciml-mgn:cpu",
                commandTemplate: "python run.py",
                workDirectory: "/workspace",
                inputMounts: new[] { RuntimePathMapping.Create("/host/in", "/workspace/in") },
                outputMounts: new[] { RuntimePathMapping.Create("/host/out", "/workspace/out") }));
        IRuntimeBackendConfigurationProvider provider =
            new InMemoryRuntimeBackendConfigurationProvider(new[] { expected });

        var actual = provider.Resolve("SCIML-MGN-DOCKER");

        Assert.Same(expected, actual);
    }

    [Fact]
    public void Provider_fails_closed_for_unknown_backend_key()
    {
        IRuntimeBackendConfigurationProvider provider =
            new InMemoryRuntimeBackendConfigurationProvider(Array.Empty<RuntimeBackendConfiguration>());

        Assert.Throws<RuntimeBackendConfigurationException>(() =>
            provider.Resolve("missing-backend"));
    }
}
