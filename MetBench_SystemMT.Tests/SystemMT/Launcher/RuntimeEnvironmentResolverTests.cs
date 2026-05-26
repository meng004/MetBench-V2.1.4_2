using System.Collections.Generic;
using MetBench_BLL.SystemMT.Catalog;
using MetBench_BLL.SystemMT.Launcher;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Launcher;

/// <summary>
/// PR-1 T1 manifest-driven runtime environments — pins the generic resolver contract for
/// <see cref="LauncherOptions.ResolvePythonExecutable"/>. New runtime keys must be configurable
/// through <see cref="LauncherOptions.RuntimePythons"/> without growing per-runtime fields,
/// existing system/openmoc/openmc/scipy behaviour must be preserved, and unknown non-system
/// keys must fail closed with a diagnostic naming the missing key.
/// </summary>
public sealed class RuntimeEnvironmentResolverTests
{
    [Fact]
    public void ResolvePythonExecutable_prefers_manifest_runtime_map_over_compat_fields()
    {
        var options = new LauncherOptions(
            SutRoot: "/tmp/sut",
            SystemPython: "python3",
            OpenMocPython: "legacy-openmoc",
            RuntimePythons: new Dictionary<string, string>
            {
                ["openmoc"] = "/venv/openmoc/bin/python",
                ["fenics"] = "/venv/fenics/bin/python",
            });

        Assert.Equal("/venv/openmoc/bin/python", options.ResolvePythonExecutable("openmoc"));
        Assert.Equal("/venv/fenics/bin/python", options.ResolvePythonExecutable("fenics"));
    }

    [Fact]
    public void ResolvePythonExecutable_fails_closed_for_unknown_non_system_runtime()
    {
        var options = new LauncherOptions(
            SutRoot: "/tmp/sut",
            SystemPython: "python3",
            OpenMocPython: "python3");

        var ex = Assert.Throws<RuntimeEnvironmentResolutionException>(
            () => options.ResolvePythonExecutable("fenics"));

        Assert.Contains("fenics", ex.Message, System.StringComparison.Ordinal);
    }

    [Fact]
    public void ResolvePythonExecutable_blank_or_null_key_resolves_to_SystemPython()
    {
        var options = new LauncherOptions(
            SutRoot: "/tmp/sut",
            SystemPython: "python3",
            OpenMocPython: "python3");

        Assert.Equal("python3", options.ResolvePythonExecutable(null));
        Assert.Equal("python3", options.ResolvePythonExecutable(""));
        Assert.Equal("python3", options.ResolvePythonExecutable("   "));
    }

    [Fact]
    public void ResolvePythonExecutable_is_case_insensitive_on_runtime_key()
    {
        var options = new LauncherOptions(
            SutRoot: "/tmp/sut",
            SystemPython: "python3",
            OpenMocPython: "python3",
            RuntimePythons: new Dictionary<string, string>
            {
                ["fenics"] = "/venv/fenics/bin/python",
            });

        Assert.Equal("/venv/fenics/bin/python", options.ResolvePythonExecutable("Fenics"));
        Assert.Equal("/venv/fenics/bin/python", options.ResolvePythonExecutable("FENICS"));
    }

    [Fact]
    public void ResolvePythonExecutable_ignores_blank_RuntimePythons_value_and_falls_back_to_compat_field()
    {
        var options = new LauncherOptions(
            SutRoot: "/tmp/sut",
            SystemPython: "python3",
            OpenMocPython: "legacy-openmoc",
            RuntimePythons: new Dictionary<string, string>
            {
                ["openmoc"] = "   ",
            });

        Assert.Equal("legacy-openmoc", options.ResolvePythonExecutable("openmoc"));
    }

    [Fact]
    public void ResolvePythonExecutable_preserves_openmoc_compat_field_when_no_map_entry()
    {
        var options = new LauncherOptions(
            SutRoot: "/tmp/sut",
            SystemPython: "python3",
            OpenMocPython: "/legacy/openmoc/bin/python");

        Assert.Equal("/legacy/openmoc/bin/python", options.ResolvePythonExecutable("openmoc"));
    }

    [Fact]
    public void ResolvePythonExecutable_preserves_openmc_effective_fallback_when_field_blank()
    {
        var options = new LauncherOptions(
            SutRoot: "/tmp/sut",
            SystemPython: "python3",
            OpenMocPython: "python3",
            OpenMcPython: null);

        Assert.Equal("python3", options.ResolvePythonExecutable("openmc"));
    }

    [Fact]
    public void ResolvePythonExecutable_preserves_scipy_effective_fallback_when_field_blank()
    {
        var options = new LauncherOptions(
            SutRoot: "/tmp/sut",
            SystemPython: "python3",
            OpenMocPython: "python3",
            ScipyPython: null);

        Assert.Equal("python3", options.ResolvePythonExecutable("scipy"));
    }

    [Fact]
    public void EffectiveOpenMcPython_uses_resolver_route_for_backwards_compat()
    {
        var options = new LauncherOptions(
            SutRoot: "/tmp/sut",
            SystemPython: "python3",
            OpenMocPython: "python3",
            OpenMcPython: "/venv/openmc/bin/python");

        Assert.Equal("/venv/openmc/bin/python", options.EffectiveOpenMcPython);
    }

    [Fact]
    public void EffectiveScipyPython_uses_resolver_route_for_backwards_compat()
    {
        var options = new LauncherOptions(
            SutRoot: "/tmp/sut",
            SystemPython: "python3",
            OpenMocPython: "python3",
            ScipyPython: "/venv/scipy/bin/python");

        Assert.Equal("/venv/scipy/bin/python", options.EffectiveScipyPython);
    }

    [Fact]
    public void RuntimePythons_map_supports_future_keys_without_new_LauncherOptions_field()
    {
        // The point of this PR: adding fenics / fipy / surrogate-model venvs in the future must
        // be a config-only change, not a LauncherOptions edit. This test pins that contract.
        var options = new LauncherOptions(
            SutRoot: "/tmp/sut",
            SystemPython: "python3",
            OpenMocPython: "python3",
            RuntimePythons: new Dictionary<string, string>
            {
                ["fenics"] = "/venv/fenics/bin/python",
                ["fipy"]   = "/venv/fipy/bin/python",
                ["torch-surrogate"] = "/venv/torch/bin/python",
            });

        Assert.Equal("/venv/fenics/bin/python",         options.ResolvePythonExecutable("fenics"));
        Assert.Equal("/venv/fipy/bin/python",           options.ResolvePythonExecutable("fipy"));
        Assert.Equal("/venv/torch/bin/python",          options.ResolvePythonExecutable("torch-surrogate"));
    }
}
