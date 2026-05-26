using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Shared;

/// <summary>
/// PR-A T1 non-JSON I/O adapter — helper-level contract tests for
/// <c>SUT/_shared/metbench_io/</c>. Drives the helper through <c>python3 -c</c>
/// inline so the test does not depend on a specific SUT runner. The integration
/// path (launcher → manifest provider → parser scripts → helper → runner) is
/// pinned separately by <c>LauncherEndToEndTestCsvTests</c>.
/// </summary>
public sealed class MetBenchIoHelperTests : IDisposable
{
    private readonly string _workDir;
    private readonly string _sharedDir;

    public MetBenchIoHelperTests()
    {
        _workDir = Path.Combine(Path.GetTempPath(), "metbench-io-helper-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_workDir);
        _sharedDir = Path.Combine(TestAssetPaths.AssetRoot(), "_shared");
        Assert.True(Directory.Exists(_sharedDir),
            $"metbench_io helper not copied to test bin: missing {_sharedDir}. Check csproj <None Include='..\\SUT\\_shared\\metbench_io\\*.py'>.");
    }

    public void Dispose()
    {
        try { Directory.Delete(_workDir, recursive: true); } catch { /* swallow */ }
    }

    /// <summary>
    /// Runs a Python snippet with <c>SUT/_shared/</c> on <c>sys.path</c> so the
    /// <c>metbench_io</c> package is importable. Returns stdout; throws on non-zero exit.
    /// </summary>
    private string RunPython(string code)
    {
        var psi = new ProcessStartInfo
        {
            FileName = TestAssetPaths.PythonExecutable(),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        psi.ArgumentList.Add("-c");
        // Prepend the shared dir to sys.path INSIDE the snippet so the test does not
        // need a wrapper file and PYTHONPATH does not leak between tests.
        psi.ArgumentList.Add(
            "import sys; sys.path.insert(0, " + JsonSerializer.Serialize(_sharedDir) + ")\n" + code);

        using var proc = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start python");
        var stdout = proc.StandardOutput.ReadToEnd();
        var stderr = proc.StandardError.ReadToEnd();
        if (!proc.WaitForExit(10_000))
        {
            try { proc.Kill(); } catch { /* swallow */ }
            throw new TimeoutException("python snippet timed out");
        }
        if (proc.ExitCode != 0)
        {
            throw new InvalidOperationException($"python snippet exit={proc.ExitCode} stderr=[{stderr}]");
        }
        return stdout;
    }

    private string WriteFile(string name, string contents)
    {
        var path = Path.Combine(_workDir, name);
        File.WriteAllText(path, contents);
        return path;
    }

    // ---- csv-row -----------------------------------------------------------

    [Fact]
    public void CsvRow_round_trip_preserves_params_dict()
    {
        var input = WriteFile("in.csv", "key,value\nfactor,1.0\nlabel,baseline\n");
        var output = Path.Combine(_workDir, "out.csv");

        RunPython(
            "from metbench_io import read_input, write_input\n" +
            $"d = read_input({JsonSerializer.Serialize(input)}, fmt='csv-row')\n" +
            $"write_input(d, {JsonSerializer.Serialize(output)}, fmt='csv-row')\n");

        var roundTripped = File.ReadAllText(output);
        Assert.Equal("key,value\r\nfactor,1.0\r\nlabel,baseline\r\n", roundTripped.Replace("\n", "\r\n").Replace("\r\r\n", "\r\n"));
    }

    [Fact]
    public void CsvRow_read_returns_values_as_strings_no_type_coercion()
    {
        var input = WriteFile("in.csv", "key,value\nfactor,2.5\nflag,true\n");

        var stdout = RunPython(
            "import json\nfrom metbench_io import read_input\n" +
            $"d = read_input({JsonSerializer.Serialize(input)}, fmt='csv-row')\n" +
            "print(json.dumps(d))\n");

        using var doc = JsonDocument.Parse(stdout);
        var p = doc.RootElement.GetProperty("params");
        Assert.Equal("2.5", p.GetProperty("factor").GetString());
        Assert.Equal("true", p.GetProperty("flag").GetString());
    }

    [Fact]
    public void CsvRow_read_quoted_strings_preserves_inner_commas()
    {
        // RFC 4180: a value containing comma must be quoted; the helper must read it back
        // as a single string, not split into separate cells.
        var input = WriteFile("in.csv", "key,value\nlabel,\"a,b,c\"\n");

        var stdout = RunPython(
            "import json\nfrom metbench_io import read_input\n" +
            $"d = read_input({JsonSerializer.Serialize(input)}, fmt='csv-row')\n" +
            "print(json.dumps(d))\n");

        using var doc = JsonDocument.Parse(stdout);
        Assert.Equal("a,b,c", doc.RootElement.GetProperty("params").GetProperty("label").GetString());
    }

    [Fact]
    public void CsvRow_empty_params_writes_header_only()
    {
        var output = Path.Combine(_workDir, "out.csv");

        RunPython(
            "from metbench_io import write_input\n" +
            $"write_input({{'params': {{}}}}, {JsonSerializer.Serialize(output)}, fmt='csv-row')\n");

        var content = File.ReadAllText(output);
        // Header line only (line ending normalised).
        Assert.Equal("key,value", content.TrimEnd('\r', '\n'));
    }

    [Fact]
    public void CsvRow_header_only_file_reads_to_empty_params()
    {
        var input = WriteFile("in.csv", "key,value\n");

        var stdout = RunPython(
            "import json\nfrom metbench_io import read_input\n" +
            $"d = read_input({JsonSerializer.Serialize(input)}, fmt='csv-row')\n" +
            "print(json.dumps(d))\n");

        using var doc = JsonDocument.Parse(stdout);
        Assert.Equal(0, doc.RootElement.GetProperty("params").EnumerateObject().Count());
    }

    [Fact]
    public void CsvRow_wrong_header_fails_loud()
    {
        var input = WriteFile("in.csv", "name,number\nfactor,1.0\n");

        var ex = Assert.Throws<InvalidOperationException>(() => RunPython(
            "from metbench_io import read_input\n" +
            $"read_input({JsonSerializer.Serialize(input)}, fmt='csv-row')\n"));
        Assert.Contains("expected header 'key,value'", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CsvRow_empty_key_fails_loud()
    {
        var input = WriteFile("in.csv", "key,value\n,1.0\n");

        var ex = Assert.Throws<InvalidOperationException>(() => RunPython(
            "from metbench_io import read_input\n" +
            $"read_input({JsonSerializer.Serialize(input)}, fmt='csv-row')\n"));
        Assert.Contains("empty key", ex.Message, StringComparison.Ordinal);
    }

    // ---- plain-text --------------------------------------------------------

    [Fact]
    public void PlainText_round_trip_byte_identical()
    {
        var content = "first line\nsecond line\n";
        var input = WriteFile("in.txt", content);
        var output = Path.Combine(_workDir, "out.txt");

        RunPython(
            "from metbench_io import read_input, write_input\n" +
            $"d = read_input({JsonSerializer.Serialize(input)}, fmt='plain-text')\n" +
            $"write_input(d, {JsonSerializer.Serialize(output)}, fmt='plain-text')\n");

        Assert.Equal(content, File.ReadAllText(output));
    }

    [Fact]
    public void PlainText_no_trailing_newline_preserved()
    {
        var content = "no newline at end";
        var input = WriteFile("in.txt", content);
        var output = Path.Combine(_workDir, "out.txt");

        RunPython(
            "from metbench_io import read_input, write_input\n" +
            $"d = read_input({JsonSerializer.Serialize(input)}, fmt='plain-text')\n" +
            $"write_input(d, {JsonSerializer.Serialize(output)}, fmt='plain-text')\n");

        Assert.Equal(content, File.ReadAllText(output));
    }

    // ---- unknown format ---------------------------------------------------

    [Fact]
    public void Unknown_fmt_fails_loud_on_read()
    {
        var input = WriteFile("in.bin", "x");
        var ex = Assert.Throws<InvalidOperationException>(() => RunPython(
            "from metbench_io import read_input\n" +
            $"read_input({JsonSerializer.Serialize(input)}, fmt='binary-blob')\n"));
        Assert.Contains("unknown fmt", ex.Message, StringComparison.Ordinal);
        Assert.Contains("binary-blob", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Unknown_fmt_fails_loud_on_write()
    {
        var output = Path.Combine(_workDir, "out.bin");
        var ex = Assert.Throws<InvalidOperationException>(() => RunPython(
            "from metbench_io import write_input\n" +
            $"write_input({{}}, {JsonSerializer.Serialize(output)}, fmt='binary-blob')\n"));
        Assert.Contains("unknown fmt", ex.Message, StringComparison.Ordinal);
        Assert.Contains("binary-blob", ex.Message, StringComparison.Ordinal);
    }

    // ---- json passthrough -------------------------------------------------

    [Fact]
    public void Json_fmt_is_passthrough_to_stdlib_json()
    {
        var input = WriteFile("in.json", "{\"params\": {\"factor\": \"1.5\"}}");

        var stdout = RunPython(
            "import json\nfrom metbench_io import read_input\n" +
            $"d = read_input({JsonSerializer.Serialize(input)}, fmt='json')\n" +
            "print(json.dumps(d))\n");

        using var doc = JsonDocument.Parse(stdout);
        Assert.Equal("1.5", doc.RootElement.GetProperty("params").GetProperty("factor").GetString());
    }
}
