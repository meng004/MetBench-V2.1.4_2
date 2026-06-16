using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT;

public sealed class TestPythonExecutableResolverTests
{
    [Fact]
    public void Resolve_prefers_configured_python()
    {
        var result = TestPythonExecutableResolver.Resolve(
            @"C:\Python312\python.exe",
            isWindows: true,
            _ => false);

        Assert.Equal(@"C:\Python312\python.exe", result);
    }

    [Fact]
    public void Resolve_probes_windows_candidates_in_order()
    {
        Assert.Equal("python", TestPythonExecutableResolver.Resolve(null, true, c => c == "python"));
        Assert.Equal("python3", TestPythonExecutableResolver.Resolve(null, true, c => c == "python3"));
        Assert.Equal("py", TestPythonExecutableResolver.Resolve(null, true, c => c == "py"));
    }

    [Fact]
    public void Resolve_probes_non_windows_candidates_in_order()
    {
        Assert.Equal("python3", TestPythonExecutableResolver.Resolve(null, false, c => c == "python3"));
        Assert.Equal("python", TestPythonExecutableResolver.Resolve(null, false, c => c == "python"));
    }

    [Theory]
    [InlineData(true, "python")]
    [InlineData(false, "python3")]
    public void Resolve_falls_back_to_previous_platform_default(bool isWindows, string expected)
    {
        var result = TestPythonExecutableResolver.Resolve(null, isWindows, _ => false);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void CommandExists_rejects_blank_command()
    {
        Assert.False(TestPythonExecutableResolver.CommandExists(""));
        Assert.False(TestPythonExecutableResolver.CommandExists("   "));
    }
}
