using System;
using System.IO;
using System.Text.Json;
using MetBench_BLL.SystemMT.Launcher;
using MetBench_BLL.SystemMT.Runtime;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Runtime;

public sealed class DockerMcpRuntimeProfileSettingsTests
{
    [Fact]
    public void Draft_builds_docker_mcp_runtime_value_with_encoded_query()
    {
        var draft = new DockerMcpRuntimeProfileDraft(
            RuntimeKey: "Docker-Linux",
            Endpoint: "http://192.168.1.42:8765",
            Image: "metbench/runtime-python:latest",
            ToolName: "openmoc-runner",
            LocalExecutable: "/host/openmoc-runner",
            PythonExecutable: "python3",
            AuthTokenEnvironmentVariable: "METBENCH_DOCKER_MCP_TOKEN");

        var value = draft.ToRuntimePythonValue();

        Assert.Equal(
            "docker-mcp://docker-linux?image=metbench%2Fruntime-python%3Alatest"
            + "&tool=openmoc-runner"
            + "&local=%2Fhost%2Fopenmoc-runner"
            + "&python=python3"
            + "&endpoint=http%3A%2F%2F192.168.1.42%3A8765"
            + "&authTokenEnv=METBENCH_DOCKER_MCP_TOKEN",
            value);
    }

    [Fact]
    public void Draft_runtime_value_round_trips_through_launcher_profile_provider()
    {
        var draft = new DockerMcpRuntimeProfileDraft(
            RuntimeKey: "openmoc",
            Endpoint: "http://127.0.0.1:8765",
            Image: "metbench-sut:latest",
            ToolName: "openmoc-runner",
            LocalExecutable: "/host/openmoc-runner",
            PythonExecutable: "/opt/metbench-tools/openmoc-runner");
        var options = new LauncherOptions(
            SutRoot: "SUT",
            SystemPython: "python",
            OpenMocPython: "python",
            RuntimePythons: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["openmoc"] = draft.ToRuntimePythonValue(),
            });

        var profile = new LauncherOptionsRuntimeProfileProvider(options).GetProfile("openmoc");

        Assert.Equal(RuntimeKind.Docker, profile.Kind);
        Assert.Equal("/host/openmoc-runner", profile.ExecutablePath);
        Assert.NotNull(profile.DockerMcp);
        Assert.Equal("metbench-sut:latest", profile.DockerMcp!.Image);
        Assert.Equal("openmoc-runner", profile.DockerMcp.ToolName);
        Assert.Equal("/host/openmoc-runner", profile.DockerMcp.LocalExecutable);
        Assert.Equal("/opt/metbench-tools/openmoc-runner", profile.DockerMcp.PythonExecutable);
        Assert.Equal("http://127.0.0.1:8765", profile.DockerMcp.Endpoint);
    }

    [Fact]
    public void Store_saves_profile_into_launcher_runtime_pythons_without_losing_existing_json()
    {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "appsettings.local.json");
        File.WriteAllText(
            path,
            """
            {
              "AppConfig": {
                "Theme": "Dark"
              },
              "LauncherOptions": {
                "RuntimePythons": {
                  "system": "python3"
                }
              }
            }
            """);
        var store = new LocalDockerMcpRuntimeProfileStore(path);

        store.Save(new DockerMcpRuntimeProfileDraft(
            RuntimeKey: "docker-linux",
            Endpoint: "http://192.168.1.42:8765",
            Image: "metbench/runtime-python:latest",
            ToolName: "openmoc-runner",
            LocalExecutable: "/host/openmoc-runner",
            PythonExecutable: "python3",
            AuthTokenEnvironmentVariable: "METBENCH_DOCKER_MCP_TOKEN"));

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var root = document.RootElement;
        Assert.Equal("Dark", root.GetProperty("AppConfig").GetProperty("Theme").GetString());
        var runtimePythons = root
            .GetProperty("LauncherOptions")
            .GetProperty("RuntimePythons");
        Assert.Equal("python3", runtimePythons.GetProperty("system").GetString());
        Assert.Equal(
            "docker-mcp://docker-linux?image=metbench%2Fruntime-python%3Alatest"
            + "&tool=openmoc-runner"
            + "&local=%2Fhost%2Fopenmoc-runner"
            + "&python=python3"
            + "&endpoint=http%3A%2F%2F192.168.1.42%3A8765"
            + "&authTokenEnv=METBENCH_DOCKER_MCP_TOKEN",
            runtimePythons.GetProperty("docker-linux").GetString());
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"metbench-docker-mcp-settings-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, recursive: true);
        }
    }
}
