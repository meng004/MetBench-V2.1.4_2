using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Jobs;

/// <summary>
/// Cloud-safe source guard for the Windows-only WPF app's LauncherOptions
/// registration. Linux CI cannot compile the WPF project, so this pins the
/// RuntimePythons wiring that lets docker-mcp:// runtime URIs reach profile
/// resolution from the WPF app (gap G6).
/// </summary>
public sealed class WpfRuntimePythonsWiringTests
{
    [Fact]
    public void App_feeds_runtime_pythons_from_environment_variables()
    {
        var app = ReadRepoFile("MetBench_Client", "App.xaml.cs");

        Assert.Contains("RuntimePythons", app);
        Assert.Contains("METBENCH_SYSTEM_PYTHON", app);
        Assert.Contains("METBENCH_OPENMC_PYTHON", app);
        Assert.Contains("StringComparer.OrdinalIgnoreCase", app);
    }

    private static string ReadRepoFile(params string[] parts)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(new[] { dir.FullName }.Concat(parts).ToArray());
            if (File.Exists(candidate))
                return File.ReadAllText(candidate);
            dir = dir.Parent;
        }

        throw new FileNotFoundException(
            $"Could not locate repository file {Path.Combine(parts)} from {AppContext.BaseDirectory}.");
    }
}
