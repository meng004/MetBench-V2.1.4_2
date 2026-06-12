using System.Collections.Generic;
using System;
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
        var remote = RuntimeProfile.Placeholder("remote-demo", "Remote demo", RuntimeKind.RemotePlaceholder);
        var hpc = RuntimeProfile.Placeholder("hpc-demo", "HPC demo", RuntimeKind.HpcPlaceholder);

        Assert.Equal(RuntimeKind.RemotePlaceholder, remote.Kind);
        Assert.Equal(RuntimeKind.HpcPlaceholder, hpc.Kind);
        Assert.False(remote.IsExecutableInV1);
        Assert.False(hpc.IsExecutableInV1);
        Assert.Null(remote.ExecutablePath);
        Assert.Null(hpc.ExecutablePath);
    }

    [Fact]
    public void Docker_runtime_profile_is_executable_and_pins_image_probe()
    {
        var profile = RuntimeProfile.DockerContainer(
            "docker-sciml",
            "SciML Docker",
            "metbench-runtime:latest",
            "python --version",
            TimeSpan.FromSeconds(60));

        Assert.Equal("docker-sciml", profile.RuntimeKey);
        Assert.Equal(RuntimeKind.DockerContainer, profile.Kind);
        Assert.True(profile.IsExecutableInV1);
        Assert.Equal("docker", profile.ExecutablePath);
        Assert.Contains(profile.VersionChecks, check =>
            check.Name == "Docker image" &&
            check.Command == "docker" &&
            check.Arguments == "image inspect metbench-runtime:latest");
        Assert.Contains(profile.VersionChecks, check =>
            check.Name == "Docker container probe" &&
            check.Command == "docker" &&
            check.Arguments == "run --rm metbench-runtime:latest python --version");
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
    public void Blocked_preflight_result_requires_failure_kind()
    {
        var profile = new RuntimeProfile("system", "System Python", RuntimeKind.LocalPython, "python");

        Assert.Throws<ArgumentException>(() =>
            RuntimePreflightResult.Blocked(profile, RuntimeFailureKind.None, "invalid"));
    }
}
