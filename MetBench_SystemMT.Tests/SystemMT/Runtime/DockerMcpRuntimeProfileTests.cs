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
                "docker-mcp://openmoc-docker?image=metbench/openmoc:latest&tool=openmoc-runner&local=/host/openmoc-runner&python=/usr/local/bin/python&endpoint=http%3A%2F%2F127.0.0.1%3A8765&authTokenEnv=METBENCH_DOCKER_MCP_TOKEN",
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
        Assert.Equal("openmoc-runner", profile.DockerMcp.ToolName);
        Assert.Equal("/host/openmoc-runner", profile.DockerMcp.LocalExecutable);
        Assert.Equal("METBENCH_DOCKER_MCP_TOKEN", profile.DockerMcp.AuthTokenEnvironmentVariable);
    }

    [Fact]
    public void Provider_parses_docker_mcp_structured_tool_boundary_fields()
    {
        var options = Options(new Dictionary<string, string>
        {
            ["openmoc"] =
                "docker-mcp://openmoc?image=metbench-sut%3Alatest&tool=openmoc-runner&local=/host/openmoc-runner&python=/opt/metbench-tools/openmoc-runner&endpoint=http%3A%2F%2F127.0.0.1%3A8765",
        });
        IRuntimeProfileProvider provider = new LauncherOptionsRuntimeProfileProvider(options);

        var profile = provider.GetProfile("openmoc");

        Assert.Equal(RuntimeKind.Docker, profile.Kind);
        Assert.Equal("/opt/metbench-tools/openmoc-runner", profile.ExecutablePath);
        Assert.NotNull(profile.DockerMcp);
        Assert.Equal("metbench-sut:latest", profile.DockerMcp!.Image);
        Assert.Equal("openmoc-runner", profile.DockerMcp.ToolName);
        Assert.Equal("/host/openmoc-runner", profile.DockerMcp.LocalExecutable);
        Assert.Equal("/opt/metbench-tools/openmoc-runner", profile.DockerMcp.PythonExecutable);
        Assert.Equal("http://127.0.0.1:8765", profile.DockerMcp.Endpoint);
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
        "docker-mcp://openmoc-docker?image=metbench/openmoc:latest&python=/usr/local/bin/python&local=/host/openmoc-runner&endpoint=http%3A%2F%2F127.0.0.1%3A8765",
        "tool")]
    [InlineData(
        "docker-mcp://openmoc-docker?image=metbench/openmoc:latest&python=/usr/local/bin/python&tool=openmoc-runner&endpoint=http%3A%2F%2F127.0.0.1%3A8765",
        "local")]
    [InlineData(
        "docker-mcp://openmoc-docker?image=metbench/openmoc:latest&tool=openmoc-runner&local=/host/openmoc-runner&python=/usr/local/bin/python",
        "endpoint")]
    [InlineData(
        "docker-mcp://openmoc-docker?image=metbench/openmoc:latest&tool=openmoc-runner&local=/host/openmoc-runner&python=/usr/local/bin/python&endpoint=tcp%3A%2F%2F127.0.0.1%3A8765",
        "endpoint")]
    [InlineData(
        "docker-mcp://openmoc-docker?image=metbench/openmoc:latest&tool=openmoc-runner&local=/host/openmoc-runner&python=/usr/local/bin/python&endpoint=http%ZZ",
        "endpoint")]
    [InlineData(
        "docker-mcp://other-runtime?image=metbench/openmoc:latest&tool=openmoc-runner&local=/host/openmoc-runner&python=/usr/local/bin/python&endpoint=http%3A%2F%2F127.0.0.1%3A8765",
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

    [Fact]
    public void Provider_parses_optional_local_python_and_wsl_path_style()
    {
        var options = Options(new Dictionary<string, string>
        {
            ["openmc"] =
                "docker-mcp://openmc?image=metbench-sut:latest&tool=openmc-runner&local=openmc-runner&python=/opt/openmc-venv/bin/python&endpoint=http%3A%2F%2F192.168.1.20%3A8765&authTokenEnv=METBENCH_DOCKER_MCP_TOKEN&localPython=python&pathStyle=wsl",
        });
        IRuntimeProfileProvider provider = new LauncherOptionsRuntimeProfileProvider(options);

        var profile = provider.GetProfile("openmc");

        Assert.NotNull(profile.DockerMcp);
        Assert.Equal("python", profile.DockerMcp!.LocalPythonExecutable);
        Assert.Equal(DockerMcpPathStyle.Wsl, profile.DockerMcp.PathStyle);
    }

    [Fact]
    public void Provider_defaults_local_python_and_path_style_when_absent()
    {
        var options = Options(new Dictionary<string, string>
        {
            ["openmc"] =
                "docker-mcp://openmc?image=metbench-sut:latest&tool=openmc-runner&local=openmc-runner&python=/opt/openmc-venv/bin/python&endpoint=http%3A%2F%2F192.168.1.20%3A8765",
        });
        IRuntimeProfileProvider provider = new LauncherOptionsRuntimeProfileProvider(options);

        var profile = provider.GetProfile("openmc");

        Assert.Null(profile.DockerMcp!.LocalPythonExecutable);
        Assert.Equal(DockerMcpPathStyle.None, profile.DockerMcp.PathStyle);
    }

    [Theory]
    [InlineData("docker-mcp://openmc?image=i&tool=t&local=l&python=p&endpoint=http%3A%2F%2F127.0.0.1%3A8765&pathStyle=windows")]
    [InlineData("docker-mcp://openmc?image=i&tool=t&local=l&python=p&endpoint=http%3A%2F%2F127.0.0.1%3A8765&pathStyle=")]
    public void Provider_fails_closed_on_invalid_path_style(string value)
    {
        var options = Options(new Dictionary<string, string> { ["openmc"] = value });
        IRuntimeProfileProvider provider = new LauncherOptionsRuntimeProfileProvider(options);

        var ex = Assert.Throws<RuntimeEnvironmentResolutionException>(() => provider.GetProfile("openmc"));

        Assert.Contains("pathStyle", ex.Message);
    }

    private static LauncherOptions Options(IReadOnlyDictionary<string, string> runtimePythons) => new(
        SutRoot: "SUT",
        SystemPython: "python-system",
        OpenMocPython: "python-openmoc",
        OpenMcPython: "python-openmc",
        ScipyPython: "python-scipy",
        RuntimePythons: runtimePythons);
}
