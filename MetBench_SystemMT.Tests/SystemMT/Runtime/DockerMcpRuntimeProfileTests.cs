using System;
using System.Collections.Generic;
using MetBench_BLL.SystemMT.Launcher;
using MetBench_BLL.SystemMT.Runtime;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Runtime;

public sealed class DockerMcpRuntimeProfileTests
{
    [Fact]
    public void Provider_parses_docker_mcp_runtime_value()
    {
        var options = Options(new Dictionary<string, string>
        {
            ["openmoc-docker"] =
                "docker-mcp://openmoc-docker?image=metbench/openmoc:latest&python=/usr/local/bin/python&endpoint=http%3A%2F%2F127.0.0.1%3A8765&authTokenEnv=METBENCH_DOCKER_MCP_TOKEN",
        });
        IRuntimeProfileProvider provider = new LauncherOptionsRuntimeProfileProvider(options);

        var profile = provider.GetProfile("OpenMOC-Docker");

        Assert.Equal("openmoc-docker", profile.RuntimeKey);
        Assert.Equal(RuntimeKind.Docker, profile.Kind);
        Assert.True(profile.IsExecutableInV1);
        Assert.Equal("/usr/local/bin/python", profile.ExecutablePath);
        Assert.NotNull(profile.DockerMcp);
        Assert.Equal("http://127.0.0.1:8765", profile.DockerMcp.Endpoint);
        Assert.Equal("metbench/openmoc:latest", profile.DockerMcp.Image);
        Assert.Equal("/usr/local/bin/python", profile.DockerMcp.PythonExecutable);
        Assert.Equal("METBENCH_DOCKER_MCP_TOKEN", profile.DockerMcp.AuthTokenEnvironmentVariable);
    }

    [Fact]
    public void Local_python_profile_behavior_is_unchanged()
    {
        var options = Options(new Dictionary<string, string>
        {
            ["custom"] = "/venv/custom/bin/python",
        });
        IRuntimeProfileProvider provider = new LauncherOptionsRuntimeProfileProvider(options);

        var profile = provider.GetProfile("custom");

        Assert.Equal("custom", profile.RuntimeKey);
        Assert.Equal(RuntimeKind.LocalPython, profile.Kind);
        Assert.Equal("/venv/custom/bin/python", profile.ExecutablePath);
        Assert.True(profile.IsExecutableInV1);
        Assert.Null(profile.DockerMcp);
    }

    [Fact]
    public void Docker_placeholder_remains_non_executable()
    {
        var placeholder = RuntimeProfile.Placeholder(
            "docker-placeholder",
            "Docker placeholder",
            RuntimeKind.DockerPlaceholder);

        Assert.Equal(RuntimeKind.DockerPlaceholder, placeholder.Kind);
        Assert.False(placeholder.IsExecutableInV1);
        Assert.Null(placeholder.ExecutablePath);
        Assert.Null(placeholder.DockerMcp);
    }

    [Theory]
    [InlineData(
        "docker-mcp://openmoc-docker?python=/usr/local/bin/python&endpoint=http%3A%2F%2F127.0.0.1%3A8765",
        "image")]
    [InlineData(
        "docker-mcp://openmoc-docker?image=metbench/openmoc:latest&endpoint=http%3A%2F%2F127.0.0.1%3A8765",
        "python")]
    [InlineData(
        "docker-mcp://openmoc-docker?image=metbench/openmoc:latest&python=/usr/local/bin/python",
        "endpoint")]
    [InlineData(
        "docker-mcp://openmoc-docker?image=metbench/openmoc:latest&python=/usr/local/bin/python&endpoint=tcp%3A%2F%2F127.0.0.1%3A8765",
        "endpoint")]
    [InlineData(
        "docker-mcp://openmoc-docker?image=metbench/openmoc:latest&python=/usr/local/bin/python&endpoint=http%ZZ",
        "endpoint")]
    [InlineData(
        "docker-mcp://other-runtime?image=metbench/openmoc:latest&python=/usr/local/bin/python&endpoint=http%3A%2F%2F127.0.0.1%3A8765",
        "runtime key")]
    public void Malformed_docker_mcp_runtime_values_fail_closed(string runtimeValue, string expectedField)
    {
        var options = Options(new Dictionary<string, string>
        {
            ["openmoc-docker"] = runtimeValue,
        });
        IRuntimeProfileProvider provider = new LauncherOptionsRuntimeProfileProvider(options);

        var ex = Assert.Throws<RuntimeEnvironmentResolutionException>(() => provider.GetProfile("openmoc-docker"));

        Assert.Contains("openmoc-docker", ex.Message, StringComparison.Ordinal);
        Assert.Contains(expectedField, ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static LauncherOptions Options(IReadOnlyDictionary<string, string> runtimePythons) => new(
        SutRoot: "SUT",
        SystemPython: "python-system",
        OpenMocPython: "python-openmoc",
        OpenMcPython: "python-openmc",
        ScipyPython: "python-scipy",
        RuntimePythons: runtimePythons);
}
