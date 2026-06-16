using System.Collections.Generic;
using System;
using System.Text.Json;
using MetBench_BLL.SystemMT.Launcher;
using MetBench_BLL.SystemMT.Runtime;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Runtime;

public sealed class RuntimeProfileProviderTests
{
    private static LauncherOptions Options() => new(
        SutRoot: "SUT",
        SystemPython: "python-system",
        OpenMocPython: "python-openmoc",
        OpenMcPython: "python-openmc",
        ScipyPython: "python-scipy",
        RuntimePythons: new Dictionary<string, string>
        {
            ["custom"] = "python-custom"
        });

    [Fact]
    public void Known_runtime_key_resolves_to_a_profile()
    {
        IRuntimeProfileProvider provider = new LauncherOptionsRuntimeProfileProvider(Options());

        var profile = provider.GetProfile("OpenMOC");

        Assert.Equal("openmoc", profile.RuntimeKey);
        Assert.Equal("OpenMOC Python", profile.DisplayName);
        Assert.Equal(RuntimeKind.PythonVirtualEnvironment, profile.Kind);
        Assert.Equal("python-openmoc", profile.ExecutablePath);
    }

    [Fact]
    public void Unknown_non_system_runtime_key_fails_closed()
    {
        IRuntimeProfileProvider provider = new LauncherOptionsRuntimeProfileProvider(Options());

        var ex = Assert.Throws<RuntimeEnvironmentResolutionException>(() => provider.GetProfile("fenics"));

        Assert.Contains("fenics", ex.Message);
    }

    [Fact]
    public void Blank_runtime_key_maps_to_system_profile()
    {
        IRuntimeProfileProvider provider = new LauncherOptionsRuntimeProfileProvider(Options());

        var profile = provider.GetProfile("   ");

        Assert.Equal("system", profile.RuntimeKey);
        Assert.Equal(RuntimeKind.LocalPython, profile.Kind);
        Assert.Equal("python-system", profile.ExecutablePath);
    }

    [Fact]
    public void Local_python_profile_carries_a_resolved_executable_path()
    {
        IRuntimeProfileProvider provider = new LauncherOptionsRuntimeProfileProvider(Options());

        var profile = provider.GetProfile("custom");

        Assert.Equal("custom", profile.RuntimeKey);
        Assert.Equal(RuntimeKind.LocalPython, profile.Kind);
        Assert.Equal("python-custom", profile.ExecutablePath);
    }

    [Fact]
    public void Docker_remote_and_HPC_kinds_are_modelable_but_not_executable_in_v1()
    {
        var docker = RuntimeProfile.Placeholder("docker-demo", "Docker demo", RuntimeKind.DockerPlaceholder);
        var remote = RuntimeProfile.Placeholder("remote-demo", "Remote demo", RuntimeKind.RemotePlaceholder);
        var hpc = RuntimeProfile.Placeholder("hpc-demo", "HPC demo", RuntimeKind.HpcPlaceholder);

        Assert.Equal(RuntimeKind.DockerPlaceholder, docker.Kind);
        Assert.Equal(RuntimeKind.RemotePlaceholder, remote.Kind);
        Assert.Equal(RuntimeKind.HpcPlaceholder, hpc.Kind);
        Assert.False(docker.IsExecutableInV1);
        Assert.False(remote.IsExecutableInV1);
        Assert.False(hpc.IsExecutableInV1);
        Assert.Null(docker.ExecutablePath);
        Assert.Null(remote.ExecutablePath);
        Assert.Null(hpc.ExecutablePath);
    }

    [Fact]
    public void Runtime_profile_defensively_copies_check_collections()
    {
        var dependencyChecks = new List<RuntimeDependencyCheck>
        {
            new("NumPy", "numpy")
        };
        var versionChecks = new List<RuntimeVersionCheck>
        {
            new("Python", "python")
        };
        var envVars = new List<string> { "METBENCH_RUNTIME" };

        var profile = new RuntimeProfile(
            "system",
            "System Python",
            RuntimeKind.LocalPython,
            "python",
            dependencyChecks,
            versionChecks,
            envVars);

        dependencyChecks.Add(new RuntimeDependencyCheck("SciPy", "scipy"));
        versionChecks.Add(new RuntimeVersionCheck("Pip", "pip"));
        envVars.Add("METBENCH_OTHER");

        Assert.Single(profile.DependencyChecks);
        Assert.Single(profile.VersionChecks);
        Assert.Single(profile.RequiredEnvironmentVariables);
    }

    [Fact]
    public void Runtime_version_check_keeps_legacy_command_alias()
    {
#pragma warning disable CS0618
        var check = new RuntimeVersionCheck("Python", "python");

        Assert.Equal("python", check.Executable);
        Assert.Equal("python", check.Command);
#pragma warning restore CS0618
    }

    [Fact]
    public void Runtime_version_check_deserializes_legacy_command_field()
    {
        const string json = """
            {
              "Name": "Python",
              "Command": "python",
              "Arguments": ["--version"]
            }
            """;

        var check = JsonSerializer.Deserialize<RuntimeVersionCheck>(json);

        Assert.NotNull(check);
        Assert.Equal("python", check!.Executable);
        Assert.Equal("--version", check.Arguments);
        Assert.Equal(new[] { "--version" }, check.ArgumentList);
    }

    [Fact]
    public void Runtime_version_check_deserializes_legacy_string_arguments_field()
    {
        const string json = """
            {
              "Name": "Python",
              "Command": "python",
              "Arguments": "--version"
            }
            """;

        var check = JsonSerializer.Deserialize<RuntimeVersionCheck>(json);

        Assert.NotNull(check);
        Assert.Equal("python", check!.Executable);
        Assert.Equal("--version", check.Arguments);
        Assert.Equal(new[] { "--version" }, check.ArgumentList);
    }

    [Fact]
    public void Runtime_version_check_keeps_legacy_record_deconstruction_shape()
    {
#pragma warning disable CS0618
        var check = new RuntimeVersionCheck("Python", "python", "--version");

        var (name, command, arguments, timeout) = check;

        Assert.Equal("Python", name);
        Assert.Equal("python", command);
        Assert.Equal("--version", arguments);
        Assert.Null(timeout);
#pragma warning restore CS0618
    }

    [Fact]
    public void Blocked_preflight_result_requires_failure_kind()
    {
        var profile = new RuntimeProfile("system", "System Python", RuntimeKind.LocalPython, "python");

        Assert.Throws<ArgumentException>(() =>
            RuntimePreflightResult.Blocked(profile, RuntimeFailureKind.None, "invalid"));
    }
}
