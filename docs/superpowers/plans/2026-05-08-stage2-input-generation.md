# Stage 2: Input Data Generation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Generate follow-up input files from a source input file plus an MR transformation configuration, and let the Stage 1 `SystemMtRunner` consume those generated inputs end-to-end.

**Architecture:** Add an `InputGenerator` to `MetBench_BLL.Core/SystemMT` that delegates the actual numeric transformation to a Python adapter via a new `PythonInputAdapter`, mirroring the existing `PythonOutputAdapter` shape. `SystemMtTask` gains an optional `MrTransformation` so a task can either supply a pre-existing follow-up `SystemMtCase` (Stage 1 mode) or a transformation that produces one (Stage 2 mode). The `SystemMtRunner` resolves the follow-up case before invoking the CLI runner. One numeric transformation (`ScalarMultiply`) ships in this stage; new transformations can be added by extending the adapter without touching C#.

**Tech Stack:** .NET 8 (`MetBench_BLL.Core`), xUnit, Reqnroll.xUnit, Python 3 adapter scripts, Newtonsoft-free `System.Text.Json` for adapter payloads.

---

## Scope Guard

This plan implements Stage 2 from `docs/superpowers/specs/2026-05-07-system-level-mt-bdd-design.md`.

Do not implement these here:

- Randoop integration;
- OpenMOC adapter;
- WPF authoring screens;
- database/disk persistence of `InputGenerationResult` (Stage 4);
- a transformation registry / DSL (only `ScalarMultiply` is required by the spec for "at least one numeric transformation").

## File Structure

Create:

- `MetBench_BLL.Core/SystemMT/MrTransformation.cs`  
  Configuration value object (transformation name + string-keyed parameters).
- `MetBench_BLL.Core/SystemMT/InputGenerationResult.cs`  
  Record of one generation pass (source path, follow-up path, transformation, log message).
- `MetBench_BLL.Core/SystemMT/PythonInputAdapter.cs`  
  Process invoker for `python adapter.py transform-input ...`.
- `MetBench_BLL.Core/SystemMT/InputGenerator.cs`  
  Business-layer orchestrator: validates inputs, calls the adapter, returns the result.
- `MetBench_SystemMT.Tests/SystemMT/MrTransformationTests.cs`
- `MetBench_SystemMT.Tests/SystemMT/PythonInputAdapterTests.cs`
- `MetBench_SystemMT.Tests/SystemMT/InputGeneratorTests.cs`
- `MetBench_SystemMT.Tests/Features/SystemLevelGeneratedFollowup.feature`  
  New BDD scenario covering source-only + transformation -> auto-generated follow-up.
- `MetBench_SystemMT.Tests/Steps/SystemLevelGeneratedFollowupSteps.cs`  
  Step definitions for the new scenario.

Modify:

- `MetBench_SystemMT.Tests/TestAssets/example_output_adapter.py`  
  Add a `transform-input` subcommand (so the adapter handles both output parsing and input transformation, per the spec's "Adapter Layer" note).
- `MetBench_BLL.Core/SystemMT/SystemMtTask.cs`  
  Allow an optional follow-up case **or** an optional `MrTransformation`; require exactly one of the two.
- `MetBench_BLL.Core/SystemMT/SystemMtRunner.cs`  
  Resolve the follow-up case (generate if needed) before running the CLI for it; fold `InputGenerationResult` into `SystemMtResult`.
- `MetBench_BLL.Core/SystemMT/SystemMtResult.cs`  
  Add a nullable `InputGenerationResult` so callers can see the transformation that produced the follow-up.
- `MetBench_Client/App.xaml.cs`  
  Register `PythonInputAdapter` and `InputGenerator` in DI.

## Task 1: Add `MrTransformation` Configuration Type

**Files:**

- Create: `MetBench_BLL.Core/SystemMT/MrTransformation.cs`
- Create: `MetBench_SystemMT.Tests/SystemMT/MrTransformationTests.cs`

- [ ] **Step 1: Write the failing test**

Create `MetBench_SystemMT.Tests/SystemMT/MrTransformationTests.cs`:

```csharp
using MetBench_BLL.SystemMT;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT;

public sealed class MrTransformationTests
{
    [Fact]
    public void Constructor_rejects_empty_name()
    {
        var error = Assert.Throws<ArgumentException>(() =>
            new MrTransformation("", new Dictionary<string, string>()));

        Assert.Contains("Name", error.Message);
    }

    [Fact]
    public void Constructor_copies_parameters_defensively()
    {
        var source = new Dictionary<string, string> { ["multiplier"] = "2" };
        var transformation = new MrTransformation("ScalarMultiply", source);
        source["multiplier"] = "9";

        Assert.Equal("2", transformation.Parameters["multiplier"]);
    }

    [Fact]
    public void Parameters_are_read_only()
    {
        var transformation = new MrTransformation(
            "ScalarMultiply",
            new Dictionary<string, string> { ["multiplier"] = "2" });

        Assert.IsAssignableFrom<IReadOnlyDictionary<string, string>>(transformation.Parameters);
    }
}
```

- [ ] **Step 2: Run tests to verify failure**

Run:

```bash
dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --filter "FullyQualifiedName~MrTransformationTests"
```

Expected: FAIL because `MetBench_BLL.SystemMT.MrTransformation` does not exist.

- [ ] **Step 3: Implement `MrTransformation`**

Create `MetBench_BLL.Core/SystemMT/MrTransformation.cs`:

```csharp
namespace MetBench_BLL.SystemMT;

public sealed class MrTransformation
{
    public MrTransformation(string name, IReadOnlyDictionary<string, string> parameters)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name cannot be empty", nameof(name));
        }

        if (parameters is null)
        {
            throw new ArgumentNullException(nameof(parameters));
        }

        Name = name;
        Parameters = new Dictionary<string, string>(parameters);
    }

    public string Name { get; }

    public IReadOnlyDictionary<string, string> Parameters { get; }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run:

```bash
dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --filter "FullyQualifiedName~MrTransformationTests"
```

Expected: PASS (3/3).

- [ ] **Step 5: Commit**

```bash
git add MetBench_BLL.Core/SystemMT/MrTransformation.cs MetBench_SystemMT.Tests/SystemMT/MrTransformationTests.cs
git commit -m "feat(stage2): add MrTransformation configuration type"
```

## Task 2: Add `InputGenerationResult` Record

**Files:**

- Create: `MetBench_BLL.Core/SystemMT/InputGenerationResult.cs`

- [ ] **Step 1: Implement the record**

Create `MetBench_BLL.Core/SystemMT/InputGenerationResult.cs`:

```csharp
namespace MetBench_BLL.SystemMT;

public sealed record InputGenerationResult(
    string SourceInputPath,
    string FollowUpInputPath,
    MrTransformation Transformation,
    bool Succeeded,
    string Log,
    string FailureReason);
```

(No dedicated test; this record is exercised end-to-end through `InputGeneratorTests` in Task 4.)

- [ ] **Step 2: Build to verify it compiles**

Run:

```bash
dotnet build MetBench_BLL.Core/MetBench_BLL.Core.csproj
```

Expected: PASS, 0 errors.

- [ ] **Step 3: Commit**

```bash
git add MetBench_BLL.Core/SystemMT/InputGenerationResult.cs
git commit -m "feat(stage2): add InputGenerationResult record"
```

## Task 3: Extend Example Python Adapter with `transform-input`

**Files:**

- Modify: `MetBench_SystemMT.Tests/TestAssets/example_output_adapter.py`

- [ ] **Step 1: Add the `transform-input` subcommand**

Replace the entire contents of `MetBench_SystemMT.Tests/TestAssets/example_output_adapter.py` with:

```python
import argparse
import json
from pathlib import Path


def parse_output(output_file: str) -> dict:
    output_path = Path(output_file)
    values = {}
    for line in output_path.read_text(encoding="utf-8").splitlines():
        if "=" not in line:
            continue
        key, raw_value = line.split("=", 1)
        values[key.strip()] = float(raw_value.strip())
    return {
        "values": values,
        "metadata": {
            "adapter": "example",
            "outputFile": str(output_path.resolve()),
        },
    }


def transform_input(source_file: str, output_file: str, params_json: str) -> dict:
    params = json.loads(params_json)
    multiplier = float(params["multiplier"])
    source_path = Path(source_file)
    output_path = Path(output_file)
    raw = source_path.read_text(encoding="utf-8").strip()
    if not raw:
        raise ValueError(f"Source input file is empty: {source_file}")
    value = float(raw)
    transformed = value * multiplier
    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(f"{transformed}\n", encoding="utf-8")
    return {
        "transformation": "ScalarMultiply",
        "source": str(source_path.resolve()),
        "output": str(output_path.resolve()),
        "params": {"multiplier": multiplier},
        "log": f"Multiplied {value} by {multiplier} -> {transformed}",
    }


def main() -> int:
    parser = argparse.ArgumentParser()
    subparsers = parser.add_subparsers(dest="command", required=True)

    parse_parser = subparsers.add_parser("parse-output")
    parse_parser.add_argument("--output-file", required=True)

    transform_parser = subparsers.add_parser("transform-input")
    transform_parser.add_argument("--source-file", required=True)
    transform_parser.add_argument("--output-file", required=True)
    transform_parser.add_argument("--params", required=True)

    args = parser.parse_args()

    if args.command == "parse-output":
        print(json.dumps(parse_output(args.output_file), ensure_ascii=False))
        return 0

    if args.command == "transform-input":
        print(json.dumps(transform_input(args.source_file, args.output_file, args.params), ensure_ascii=False))
        return 0

    return 2


if __name__ == "__main__":
    raise SystemExit(main())
```

- [ ] **Step 2: Smoke-test from the shell**

Run:

```bash
TMP=$(mktemp -d) && echo "3" > "$TMP/source.txt" && \
    python3 MetBench_SystemMT.Tests/TestAssets/example_output_adapter.py transform-input \
        --source-file "$TMP/source.txt" \
        --output-file "$TMP/followup.txt" \
        --params '{"multiplier": 2.5}' && \
    cat "$TMP/followup.txt"
```

Expected stdout: a JSON line whose `"log"` contains `Multiplied 3.0 by 2.5 -> 7.5`, then `7.5` printed by `cat`.

- [ ] **Step 3: Re-run the existing Stage 1 adapter tests to confirm no regression**

Run:

```bash
dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --filter "FullyQualifiedName~PythonOutputAdapterTests"
```

Expected: PASS (2/2; the parse-output behavior must still work).

- [ ] **Step 4: Commit**

```bash
git add MetBench_SystemMT.Tests/TestAssets/example_output_adapter.py
git commit -m "feat(stage2): add transform-input subcommand to example adapter"
```

## Task 4: Implement `PythonInputAdapter`

**Files:**

- Create: `MetBench_BLL.Core/SystemMT/PythonInputAdapter.cs`
- Create: `MetBench_SystemMT.Tests/SystemMT/PythonInputAdapterTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `MetBench_SystemMT.Tests/SystemMT/PythonInputAdapterTests.cs`:

```csharp
using MetBench_BLL.SystemMT;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT;

public sealed class PythonInputAdapterTests
{
    [Fact]
    public async Task TransformAsync_writes_followup_file_and_returns_log()
    {
        var workDir = Path.Combine(Path.GetTempPath(), "MetBenchSystemMt", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workDir);
        var sourcePath = Path.Combine(workDir, "source.txt");
        var followUpPath = Path.Combine(workDir, "followup.txt");
        await File.WriteAllTextAsync(sourcePath, "3", CancellationToken.None);

        var adapter = new PythonInputAdapter(TestAssetPaths.PythonExecutable());
        var transformation = new MrTransformation(
            "ScalarMultiply",
            new Dictionary<string, string> { ["multiplier"] = "2.5" });

        var log = await adapter.TransformAsync(
            Path.Combine(TestAssetPaths.AssetRoot(), "example_output_adapter.py"),
            sourcePath,
            followUpPath,
            transformation,
            CancellationToken.None);

        Assert.True(File.Exists(followUpPath));
        Assert.Contains("Multiplied 3.0 by 2.5", log);
        Assert.Equal("7.5", (await File.ReadAllTextAsync(followUpPath, CancellationToken.None)).Trim());
    }

    [Fact]
    public async Task TransformAsync_reports_missing_source_file()
    {
        var adapter = new PythonInputAdapter(TestAssetPaths.PythonExecutable());
        var transformation = new MrTransformation(
            "ScalarMultiply",
            new Dictionary<string, string> { ["multiplier"] = "2" });

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            adapter.TransformAsync(
                Path.Combine(TestAssetPaths.AssetRoot(), "example_output_adapter.py"),
                Path.Combine(Path.GetTempPath(), "missing-source.txt"),
                Path.Combine(Path.GetTempPath(), "followup.txt"),
                transformation,
                CancellationToken.None));

        Assert.Contains("source input file does not exist", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TransformAsync_propagates_adapter_failures()
    {
        var workDir = Path.Combine(Path.GetTempPath(), "MetBenchSystemMt", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workDir);
        var sourcePath = Path.Combine(workDir, "empty.txt");
        await File.WriteAllTextAsync(sourcePath, "", CancellationToken.None);

        var adapter = new PythonInputAdapter(TestAssetPaths.PythonExecutable());
        var transformation = new MrTransformation(
            "ScalarMultiply",
            new Dictionary<string, string> { ["multiplier"] = "2" });

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            adapter.TransformAsync(
                Path.Combine(TestAssetPaths.AssetRoot(), "example_output_adapter.py"),
                sourcePath,
                Path.Combine(workDir, "followup.txt"),
                transformation,
                CancellationToken.None));

        Assert.Contains("Adapter failure", error.Message);
    }
}
```

- [ ] **Step 2: Run tests to verify failure**

Run:

```bash
dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --filter "FullyQualifiedName~PythonInputAdapterTests"
```

Expected: FAIL because `PythonInputAdapter` does not exist.

- [ ] **Step 3: Implement `PythonInputAdapter`**

Create `MetBench_BLL.Core/SystemMT/PythonInputAdapter.cs`:

```csharp
using System.Diagnostics;
using System.Text.Json;

namespace MetBench_BLL.SystemMT;

public sealed class PythonInputAdapter
{
    private readonly string _pythonExecutable;

    public PythonInputAdapter(string pythonExecutable)
    {
        _pythonExecutable = string.IsNullOrWhiteSpace(pythonExecutable)
            ? throw new ArgumentException("Python executable cannot be empty", nameof(pythonExecutable))
            : pythonExecutable;
    }

    public async Task<string> TransformAsync(
        string adapterPath,
        string sourceInputPath,
        string followUpInputPath,
        MrTransformation transformation,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(adapterPath))
        {
            throw new InvalidOperationException($"Configuration failure: adapter file does not exist: {adapterPath}");
        }

        if (!File.Exists(sourceInputPath))
        {
            throw new InvalidOperationException($"Configuration failure: source input file does not exist: {sourceInputPath}");
        }

        var paramsJson = JsonSerializer.Serialize(transformation.Parameters);

        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = _pythonExecutable,
            Arguments = $"{Quote(adapterPath)} transform-input --source-file {Quote(sourceInputPath)} --output-file {Quote(followUpInputPath)} --params {Quote(paramsJson)}",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        process.Start();
        var stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderr = await process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Adapter failure: input adapter exited with code {process.ExitCode}. stderr: {stderr}");
        }

        return ParseLog(stdout);
    }

    private static string ParseLog(string stdout)
    {
        try
        {
            using var document = JsonDocument.Parse(stdout);
            return document.RootElement.TryGetProperty("log", out var log)
                ? log.GetString() ?? string.Empty
                : string.Empty;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Adapter failure: input adapter returned invalid JSON. {ex.Message}", ex);
        }
    }

    private static string Quote(string value)
    {
        return value.Contains(' ') ? $"\"{value.Replace("\"", "\\\"")}\"" : value;
    }
}
```

- [ ] **Step 4: Run tests**

Run:

```bash
dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --filter "FullyQualifiedName~PythonInputAdapterTests"
```

Expected: PASS (3/3).

- [ ] **Step 5: Commit**

```bash
git add MetBench_BLL.Core/SystemMT/PythonInputAdapter.cs MetBench_SystemMT.Tests/SystemMT/PythonInputAdapterTests.cs
git commit -m "feat(stage2): add PythonInputAdapter for transform-input invocations"
```

## Task 5: Implement `InputGenerator`

**Files:**

- Create: `MetBench_BLL.Core/SystemMT/InputGenerator.cs`
- Create: `MetBench_SystemMT.Tests/SystemMT/InputGeneratorTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `MetBench_SystemMT.Tests/SystemMT/InputGeneratorTests.cs`:

```csharp
using MetBench_BLL.SystemMT;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT;

public sealed class InputGeneratorTests
{
    [Fact]
    public async Task GenerateAsync_returns_success_result_with_followup_path_and_log()
    {
        var workDir = Path.Combine(Path.GetTempPath(), "MetBenchSystemMt", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workDir);
        var sourcePath = Path.Combine(workDir, "source.txt");
        var followUpPath = Path.Combine(workDir, "followup.txt");
        await File.WriteAllTextAsync(sourcePath, "4", CancellationToken.None);

        var transformation = new MrTransformation(
            "ScalarMultiply",
            new Dictionary<string, string> { ["multiplier"] = "3" });
        var generator = new InputGenerator(
            new PythonInputAdapter(TestAssetPaths.PythonExecutable()),
            Path.Combine(TestAssetPaths.AssetRoot(), "example_output_adapter.py"));

        var result = await generator.GenerateAsync(
            sourcePath, followUpPath, transformation, CancellationToken.None);

        Assert.True(result.Succeeded, result.FailureReason);
        Assert.Equal(sourcePath, result.SourceInputPath);
        Assert.Equal(followUpPath, result.FollowUpInputPath);
        Assert.Same(transformation, result.Transformation);
        Assert.Contains("Multiplied 4.0 by 3", result.Log);
        Assert.Equal("12.0", (await File.ReadAllTextAsync(followUpPath, CancellationToken.None)).Trim());
    }

    [Fact]
    public async Task GenerateAsync_returns_failure_result_when_source_missing()
    {
        var transformation = new MrTransformation(
            "ScalarMultiply",
            new Dictionary<string, string> { ["multiplier"] = "2" });
        var generator = new InputGenerator(
            new PythonInputAdapter(TestAssetPaths.PythonExecutable()),
            Path.Combine(TestAssetPaths.AssetRoot(), "example_output_adapter.py"));

        var result = await generator.GenerateAsync(
            Path.Combine(Path.GetTempPath(), "does-not-exist.txt"),
            Path.Combine(Path.GetTempPath(), "followup.txt"),
            transformation,
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains("source input file does not exist", result.FailureReason, StringComparison.OrdinalIgnoreCase);
    }
}
```

- [ ] **Step 2: Run tests to verify failure**

Run:

```bash
dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --filter "FullyQualifiedName~InputGeneratorTests"
```

Expected: FAIL because `InputGenerator` does not exist.

- [ ] **Step 3: Implement `InputGenerator`**

Create `MetBench_BLL.Core/SystemMT/InputGenerator.cs`:

```csharp
namespace MetBench_BLL.SystemMT;

public sealed class InputGenerator
{
    private readonly PythonInputAdapter _inputAdapter;
    private readonly string _adapterPath;

    public InputGenerator(PythonInputAdapter inputAdapter, string adapterPath)
    {
        _inputAdapter = inputAdapter ?? throw new ArgumentNullException(nameof(inputAdapter));
        if (string.IsNullOrWhiteSpace(adapterPath))
        {
            throw new ArgumentException("Adapter path cannot be empty", nameof(adapterPath));
        }

        _adapterPath = adapterPath;
    }

    public async Task<InputGenerationResult> GenerateAsync(
        string sourceInputPath,
        string followUpInputPath,
        MrTransformation transformation,
        CancellationToken cancellationToken)
    {
        try
        {
            var log = await _inputAdapter.TransformAsync(
                _adapterPath, sourceInputPath, followUpInputPath, transformation, cancellationToken);

            return new InputGenerationResult(
                sourceInputPath,
                followUpInputPath,
                transformation,
                Succeeded: true,
                Log: log,
                FailureReason: string.Empty);
        }
        catch (InvalidOperationException ex)
        {
            return new InputGenerationResult(
                sourceInputPath,
                followUpInputPath,
                transformation,
                Succeeded: false,
                Log: string.Empty,
                FailureReason: ex.Message);
        }
    }
}
```

- [ ] **Step 4: Run tests**

Run:

```bash
dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --filter "FullyQualifiedName~InputGeneratorTests"
```

Expected: PASS (2/2).

- [ ] **Step 5: Commit**

```bash
git add MetBench_BLL.Core/SystemMT/InputGenerator.cs MetBench_SystemMT.Tests/SystemMT/InputGeneratorTests.cs
git commit -m "feat(stage2): add InputGenerator orchestrator"
```

## Task 6: Extend `SystemMtTask` and `SystemMtResult` for Generated Follow-ups

**Files:**

- Modify: `MetBench_BLL.Core/SystemMT/SystemMtTask.cs`
- Modify: `MetBench_BLL.Core/SystemMT/SystemMtResult.cs`
- Modify: `MetBench_SystemMT.Tests/SystemMT/SystemMtModelTests.cs`

- [ ] **Step 1: Add failing model tests for the new constructor invariants**

Append to `MetBench_SystemMT.Tests/SystemMT/SystemMtModelTests.cs` (inside the existing class):

```csharp
    [Fact]
    public void SystemMtTask_accepts_a_followup_case()
    {
        var program = new SystemProgram(
            ProgramLanguage.Python,
            "example-cli",
            "python",
            "example_cli.py --input {input} --output {output}",
            "adapter.py");
        var source = new SystemMtCase("source", "source.txt", "work/source", "out.txt");
        var followUp = new SystemMtCase("follow-up", "followup.txt", "work/followup", "out.txt");

        var task = SystemMtTask.WithFollowUpCase(
            program, source, followUp, "GreaterThan", TimeSpan.FromSeconds(5));

        Assert.Same(followUp, task.FollowUpCase);
        Assert.Null(task.InputTransformation);
    }

    [Fact]
    public void SystemMtTask_accepts_a_transformation_in_place_of_a_followup_case()
    {
        var program = new SystemProgram(
            ProgramLanguage.Python,
            "example-cli",
            "python",
            "example_cli.py --input {input} --output {output}",
            "adapter.py");
        var source = new SystemMtCase("source", "source.txt", "work/source", "out.txt");
        var transformation = new MrTransformation(
            "ScalarMultiply",
            new Dictionary<string, string> { ["multiplier"] = "2" });

        var task = SystemMtTask.WithGeneratedFollowUp(
            program,
            source,
            followUpCaseName: "follow-up",
            followUpInputPath: "work/followup/input.txt",
            followUpWorkingDirectory: "work/followup",
            followUpOutputPath: "work/followup/output.txt",
            transformation,
            "GreaterThan",
            TimeSpan.FromSeconds(5));

        Assert.Null(task.FollowUpCase);
        Assert.NotNull(task.InputTransformation);
        Assert.Equal("ScalarMultiply", task.InputTransformation!.Name);
        Assert.Equal("follow-up", task.GeneratedFollowUpCaseName);
    }
```

- [ ] **Step 2: Run tests to verify failure**

Run:

```bash
dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --filter "FullyQualifiedName~SystemMtModelTests"
```

Expected: FAIL because `WithFollowUpCase` / `WithGeneratedFollowUp` / `InputTransformation` do not exist.

- [ ] **Step 3: Replace `SystemMtTask` with the dual-mode version**

Replace the entire contents of `MetBench_BLL.Core/SystemMT/SystemMtTask.cs` with:

```csharp
namespace MetBench_BLL.SystemMT;

public sealed class SystemMtTask
{
    private SystemMtTask(
        SystemProgram program,
        SystemMtCase sourceCase,
        SystemMtCase? followUpCase,
        string? generatedFollowUpCaseName,
        string? generatedFollowUpInputPath,
        string? generatedFollowUpWorkingDirectory,
        string? generatedFollowUpOutputPath,
        MrTransformation? inputTransformation,
        string assertionName,
        TimeSpan timeout)
    {
        Program = program ?? throw new ArgumentNullException(nameof(program));
        SourceCase = sourceCase ?? throw new ArgumentNullException(nameof(sourceCase));
        FollowUpCase = followUpCase;
        GeneratedFollowUpCaseName = generatedFollowUpCaseName;
        GeneratedFollowUpInputPath = generatedFollowUpInputPath;
        GeneratedFollowUpWorkingDirectory = generatedFollowUpWorkingDirectory;
        GeneratedFollowUpOutputPath = generatedFollowUpOutputPath;
        InputTransformation = inputTransformation;
        AssertionName = string.IsNullOrWhiteSpace(assertionName)
            ? throw new ArgumentException("AssertionName cannot be empty", nameof(assertionName))
            : assertionName;
        Timeout = timeout > TimeSpan.Zero
            ? timeout
            : throw new ArgumentException("Timeout must be greater than zero", nameof(timeout));
    }

    public SystemProgram Program { get; }
    public SystemMtCase SourceCase { get; }
    public SystemMtCase? FollowUpCase { get; }
    public string? GeneratedFollowUpCaseName { get; }
    public string? GeneratedFollowUpInputPath { get; }
    public string? GeneratedFollowUpWorkingDirectory { get; }
    public string? GeneratedFollowUpOutputPath { get; }
    public MrTransformation? InputTransformation { get; }
    public string AssertionName { get; }
    public TimeSpan Timeout { get; }

    public static SystemMtTask WithFollowUpCase(
        SystemProgram program,
        SystemMtCase sourceCase,
        SystemMtCase followUpCase,
        string assertionName,
        TimeSpan timeout)
    {
        if (followUpCase is null)
        {
            throw new ArgumentNullException(nameof(followUpCase));
        }

        if (sourceCase is not null
            && sourceCase.CaseName.Equals(followUpCase.CaseName, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Source and follow-up case names must be different");
        }

        return new SystemMtTask(
            program, sourceCase!, followUpCase,
            generatedFollowUpCaseName: null,
            generatedFollowUpInputPath: null,
            generatedFollowUpWorkingDirectory: null,
            generatedFollowUpOutputPath: null,
            inputTransformation: null,
            assertionName,
            timeout);
    }

    public static SystemMtTask WithGeneratedFollowUp(
        SystemProgram program,
        SystemMtCase sourceCase,
        string followUpCaseName,
        string followUpInputPath,
        string followUpWorkingDirectory,
        string followUpOutputPath,
        MrTransformation transformation,
        string assertionName,
        TimeSpan timeout)
    {
        if (transformation is null)
        {
            throw new ArgumentNullException(nameof(transformation));
        }

        if (string.IsNullOrWhiteSpace(followUpCaseName))
        {
            throw new ArgumentException("followUpCaseName cannot be empty", nameof(followUpCaseName));
        }

        if (sourceCase is not null
            && sourceCase.CaseName.Equals(followUpCaseName, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Source and follow-up case names must be different");
        }

        return new SystemMtTask(
            program, sourceCase!,
            followUpCase: null,
            generatedFollowUpCaseName: followUpCaseName,
            generatedFollowUpInputPath: followUpInputPath,
            generatedFollowUpWorkingDirectory: followUpWorkingDirectory,
            generatedFollowUpOutputPath: followUpOutputPath,
            inputTransformation: transformation,
            assertionName,
            timeout);
    }
}
```

- [ ] **Step 4: Update `SystemMtResult` to carry the optional generation result**

Replace the entire contents of `MetBench_BLL.Core/SystemMT/SystemMtResult.cs` with:

```csharp
namespace MetBench_BLL.SystemMT;

public sealed record SystemMtResult(
    CliRunResult SourceRun,
    CliRunResult FollowUpRun,
    ParsedOutput SourceOutput,
    ParsedOutput FollowUpOutput,
    SystemMtAssertionResult Assertion,
    bool Passed,
    string FailureReason,
    InputGenerationResult? InputGeneration = null);
```

- [ ] **Step 5: Run model tests**

Run:

```bash
dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --filter "FullyQualifiedName~SystemMtModelTests"
```

Expected: PASS (5/5 — three pre-existing tests still pass; two new tests pass).

- [ ] **Step 6: Update existing call sites that used the removed public constructor**

These previous tests used `new SystemMtTask(...)` directly:

- `MetBench_SystemMT.Tests/SystemMT/SystemMtRunnerTests.cs` (RunAsync_executes_source_and_followup_and_asserts_mr)
- `MetBench_SystemMT.Tests/Steps/SystemLevelCliMtSteps.cs` (the `When` step)

In both files, replace:

```csharp
var task = new SystemMtTask(
    program,
    <sourceCase>,
    <followUpCase>,
    "GreaterThan",
    TimeSpan.FromSeconds(10));
```

with:

```csharp
var task = SystemMtTask.WithFollowUpCase(
    program,
    <sourceCase>,
    <followUpCase>,
    "GreaterThan",
    TimeSpan.FromSeconds(10));
```

- [ ] **Step 7: Run the full Stage 1 test set to confirm no regression**

Run:

```bash
dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj
```

Expected: PASS (14/14 = 12 pre-existing + 2 new model tests).

- [ ] **Step 8: Commit**

```bash
git add MetBench_BLL.Core/SystemMT/SystemMtTask.cs \
        MetBench_BLL.Core/SystemMT/SystemMtResult.cs \
        MetBench_SystemMT.Tests/SystemMT/SystemMtModelTests.cs \
        MetBench_SystemMT.Tests/SystemMT/SystemMtRunnerTests.cs \
        MetBench_SystemMT.Tests/Steps/SystemLevelCliMtSteps.cs
git commit -m "feat(stage2): allow SystemMtTask to carry an MR transformation"
```

## Task 7: Wire `InputGenerator` into `SystemMtRunner`

**Files:**

- Modify: `MetBench_BLL.Core/SystemMT/SystemMtRunner.cs`
- Create: `MetBench_SystemMT.Tests/SystemMT/SystemMtRunnerGeneratedFollowupTests.cs`

- [ ] **Step 1: Write the failing integration test**

Create `MetBench_SystemMT.Tests/SystemMT/SystemMtRunnerGeneratedFollowupTests.cs`:

```csharp
using MetBench_BLL;
using MetBench_BLL.SystemMT;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT;

public sealed class SystemMtRunnerGeneratedFollowupTests
{
    [Fact]
    public async Task RunAsync_generates_followup_input_when_only_transformation_is_provided()
    {
        var assetRoot = TestAssetPaths.AssetRoot();
        var root = Path.Combine(Path.GetTempPath(), "MetBenchSystemMt", Guid.NewGuid().ToString("N"));
        var sourceDir = Path.Combine(root, "source");
        var followUpDir = Path.Combine(root, "followup");
        Directory.CreateDirectory(sourceDir);
        Directory.CreateDirectory(followUpDir);

        var sourceInput = Path.Combine(sourceDir, "input.txt");
        await File.WriteAllTextAsync(sourceInput, "4", CancellationToken.None);

        var program = new SystemProgram(
            ProgramLanguage.Python,
            "example-cli",
            TestAssetPaths.PythonExecutable(),
            $"{Path.Combine(assetRoot, "example_cli.py")} --input {{input}} --output {{output}}",
            Path.Combine(assetRoot, "example_output_adapter.py"));
        var task = SystemMtTask.WithGeneratedFollowUp(
            program,
            new SystemMtCase("source", sourceInput, sourceDir, Path.Combine(sourceDir, "output.txt")),
            followUpCaseName: "follow-up",
            followUpInputPath: Path.Combine(followUpDir, "input.txt"),
            followUpWorkingDirectory: followUpDir,
            followUpOutputPath: Path.Combine(followUpDir, "output.txt"),
            new MrTransformation("ScalarMultiply", new Dictionary<string, string> { ["multiplier"] = "3" }),
            "GreaterThan",
            TimeSpan.FromSeconds(10));
        var runner = new SystemMtRunner(
            new CliProgramRunner(),
            new PythonOutputAdapter(TestAssetPaths.PythonExecutable()),
            new GreaterThanAssertion(),
            new InputGenerator(
                new PythonInputAdapter(TestAssetPaths.PythonExecutable()),
                Path.Combine(assetRoot, "example_output_adapter.py")));

        var result = await runner.RunAsync(task, "result", CancellationToken.None);

        Assert.True(result.Passed, result.FailureReason);
        Assert.NotNull(result.InputGeneration);
        Assert.True(result.InputGeneration!.Succeeded);
        Assert.Equal(4, result.Assertion.SourceValue);
        Assert.Equal(12, result.Assertion.FollowUpValue);
    }

    [Fact]
    public async Task RunAsync_returns_failure_when_input_generation_fails()
    {
        var assetRoot = TestAssetPaths.AssetRoot();
        var root = Path.Combine(Path.GetTempPath(), "MetBenchSystemMt", Guid.NewGuid().ToString("N"));
        var sourceDir = Path.Combine(root, "source");
        var followUpDir = Path.Combine(root, "followup");
        Directory.CreateDirectory(sourceDir);
        Directory.CreateDirectory(followUpDir);

        var program = new SystemProgram(
            ProgramLanguage.Python,
            "example-cli",
            TestAssetPaths.PythonExecutable(),
            $"{Path.Combine(assetRoot, "example_cli.py")} --input {{input}} --output {{output}}",
            Path.Combine(assetRoot, "example_output_adapter.py"));
        var task = SystemMtTask.WithGeneratedFollowUp(
            program,
            new SystemMtCase(
                "source",
                Path.Combine(sourceDir, "missing-source.txt"),
                sourceDir,
                Path.Combine(sourceDir, "output.txt")),
            followUpCaseName: "follow-up",
            followUpInputPath: Path.Combine(followUpDir, "input.txt"),
            followUpWorkingDirectory: followUpDir,
            followUpOutputPath: Path.Combine(followUpDir, "output.txt"),
            new MrTransformation("ScalarMultiply", new Dictionary<string, string> { ["multiplier"] = "3" }),
            "GreaterThan",
            TimeSpan.FromSeconds(10));
        var runner = new SystemMtRunner(
            new CliProgramRunner(),
            new PythonOutputAdapter(TestAssetPaths.PythonExecutable()),
            new GreaterThanAssertion(),
            new InputGenerator(
                new PythonInputAdapter(TestAssetPaths.PythonExecutable()),
                Path.Combine(assetRoot, "example_output_adapter.py")));

        var result = await runner.RunAsync(task, "result", CancellationToken.None);

        Assert.False(result.Passed);
        Assert.NotNull(result.InputGeneration);
        Assert.False(result.InputGeneration!.Succeeded);
        Assert.Contains("source input file does not exist", result.FailureReason, StringComparison.OrdinalIgnoreCase);
    }
}
```

- [ ] **Step 2: Run tests to verify failure**

Run:

```bash
dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --filter "FullyQualifiedName~SystemMtRunnerGeneratedFollowupTests"
```

Expected: FAIL because `SystemMtRunner` has no `InputGenerator` constructor parameter and cannot resolve generated follow-ups.

- [ ] **Step 3: Replace `SystemMtRunner` with the Stage 2 version**

Replace the entire contents of `MetBench_BLL.Core/SystemMT/SystemMtRunner.cs` with:

```csharp
namespace MetBench_BLL.SystemMT;

public sealed class SystemMtRunner
{
    private readonly CliProgramRunner _cliRunner;
    private readonly PythonOutputAdapter _outputAdapter;
    private readonly GreaterThanAssertion _greaterThanAssertion;
    private readonly InputGenerator? _inputGenerator;

    public SystemMtRunner(
        CliProgramRunner cliRunner,
        PythonOutputAdapter outputAdapter,
        GreaterThanAssertion greaterThanAssertion,
        InputGenerator? inputGenerator = null)
    {
        _cliRunner = cliRunner ?? throw new ArgumentNullException(nameof(cliRunner));
        _outputAdapter = outputAdapter ?? throw new ArgumentNullException(nameof(outputAdapter));
        _greaterThanAssertion = greaterThanAssertion ?? throw new ArgumentNullException(nameof(greaterThanAssertion));
        _inputGenerator = inputGenerator;
    }

    public async Task<SystemMtResult> RunAsync(
        SystemMtTask task,
        string valueName,
        CancellationToken cancellationToken)
    {
        InputGenerationResult? generation = null;
        SystemMtCase followUpCase;

        if (task.FollowUpCase is not null)
        {
            followUpCase = task.FollowUpCase;
        }
        else if (task.InputTransformation is not null)
        {
            if (_inputGenerator is null)
            {
                return FailedBeforeRun(valueName,
                    "Configuration failure: task requires input generation but no InputGenerator is registered.");
            }

            generation = await _inputGenerator.GenerateAsync(
                task.SourceCase.InputPath,
                task.GeneratedFollowUpInputPath!,
                task.InputTransformation,
                cancellationToken);

            if (!generation.Succeeded)
            {
                return FailedBeforeRun(valueName, generation.FailureReason, generation);
            }

            followUpCase = new SystemMtCase(
                task.GeneratedFollowUpCaseName!,
                generation.FollowUpInputPath,
                task.GeneratedFollowUpWorkingDirectory!,
                task.GeneratedFollowUpOutputPath!);
        }
        else
        {
            return FailedBeforeRun(valueName,
                "Configuration failure: task has neither a follow-up case nor an input transformation.");
        }

        var sourceRun = await _cliRunner.RunAsync(task.Program, task.SourceCase, task.Timeout, cancellationToken);
        if (!sourceRun.Succeeded)
        {
            return FailedAfterRun(sourceRun, sourceRun, valueName, sourceRun.FailureReason, generation);
        }

        var followUpRun = await _cliRunner.RunAsync(task.Program, followUpCase, task.Timeout, cancellationToken);
        if (!followUpRun.Succeeded)
        {
            return FailedAfterRun(sourceRun, followUpRun, valueName, followUpRun.FailureReason, generation);
        }

        var sourceOutput = await _outputAdapter.ParseAsync(task.Program.OutputAdapterPath, sourceRun.OutputPath, cancellationToken);
        var followUpOutput = await _outputAdapter.ParseAsync(task.Program.OutputAdapterPath, followUpRun.OutputPath, cancellationToken);
        var assertion = task.AssertionName switch
        {
            "GreaterThan" => _greaterThanAssertion.Evaluate(valueName, sourceOutput, followUpOutput),
            _ => new SystemMtAssertionResult(
                task.AssertionName, valueName, double.NaN, double.NaN, false,
                $"Configuration failure: unsupported assertion '{task.AssertionName}'")
        };

        return new SystemMtResult(
            sourceRun, followUpRun, sourceOutput, followUpOutput, assertion,
            assertion.Passed, assertion.FailureReason, generation);
    }

    private static SystemMtResult FailedBeforeRun(string valueName, string reason, InputGenerationResult? generation = null)
    {
        var emptyRun = new CliRunResult(string.Empty, -1, string.Empty, string.Empty, TimeSpan.Zero, string.Empty, false, reason);
        var emptyOutput = new ParsedOutput(new Dictionary<string, double>(), new Dictionary<string, string>());
        var assertion = new SystemMtAssertionResult("GreaterThan", valueName, double.NaN, double.NaN, false, reason);
        return new SystemMtResult(emptyRun, emptyRun, emptyOutput, emptyOutput, assertion, false, reason, generation);
    }

    private static SystemMtResult FailedAfterRun(
        CliRunResult sourceRun, CliRunResult followUpRun, string valueName, string reason, InputGenerationResult? generation)
    {
        var emptyOutput = new ParsedOutput(new Dictionary<string, double>(), new Dictionary<string, string>());
        var assertion = new SystemMtAssertionResult("GreaterThan", valueName, double.NaN, double.NaN, false, reason);
        return new SystemMtResult(sourceRun, followUpRun, emptyOutput, emptyOutput, assertion, false, reason, generation);
    }
}
```

- [ ] **Step 4: Run the new and existing runner tests**

Run:

```bash
dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --filter "FullyQualifiedName~SystemMtRunner"
```

Expected: PASS (1 Stage 1 runner test + 2 Stage 2 generated-followup tests = 3/3).

- [ ] **Step 5: Commit**

```bash
git add MetBench_BLL.Core/SystemMT/SystemMtRunner.cs MetBench_SystemMT.Tests/SystemMT/SystemMtRunnerGeneratedFollowupTests.cs
git commit -m "feat(stage2): generate follow-up inputs from MrTransformation in SystemMtRunner"
```

## Task 8: Add a BDD Scenario for Generated Follow-ups

**Files:**

- Create: `MetBench_SystemMT.Tests/Features/SystemLevelGeneratedFollowup.feature`
- Create: `MetBench_SystemMT.Tests/Steps/SystemLevelGeneratedFollowupSteps.cs`

- [ ] **Step 1: Add the feature file**

Create `MetBench_SystemMT.Tests/Features/SystemLevelGeneratedFollowup.feature`:

```gherkin
Feature: System-level metamorphic testing with generated follow-up input

  Scenario: Follow-up input generated by ScalarMultiply produces a greater output
    Given a source MT case with input value "5"
    And the MR transformation "ScalarMultiply" with parameter "multiplier" set to "3"
    When I run source and the generated follow-up with program profile "example-cli"
    Then the parsed output value "result" of the generated follow-up should be greater than the source
```

- [ ] **Step 2: Add the step definitions**

Create `MetBench_SystemMT.Tests/Steps/SystemLevelGeneratedFollowupSteps.cs`:

```csharp
using MetBench_BLL;
using MetBench_BLL.SystemMT;
using MetBench_SystemMT.Tests.SystemMT;
using Reqnroll;
using Xunit;

namespace MetBench_SystemMT.Tests.Steps;

[Binding]
public sealed class SystemLevelGeneratedFollowupSteps
{
    private string? _sourceInputPath;
    private string? _followUpInputPath;
    private string _sourceDir = string.Empty;
    private string _followUpDir = string.Empty;
    private MrTransformation? _transformation;
    private SystemMtResult? _result;

    [Given("a source MT case with input value {string}")]
    public async Task GivenASourceMtCaseWithInputValue(string sourceValue)
    {
        var root = Path.Combine(Path.GetTempPath(), "MetBenchSystemMtBdd", Guid.NewGuid().ToString("N"));
        _sourceDir = Path.Combine(root, "source");
        _followUpDir = Path.Combine(root, "followup");
        Directory.CreateDirectory(_sourceDir);
        Directory.CreateDirectory(_followUpDir);

        _sourceInputPath = Path.Combine(_sourceDir, "input.txt");
        _followUpInputPath = Path.Combine(_followUpDir, "input.txt");
        await File.WriteAllTextAsync(_sourceInputPath, sourceValue, CancellationToken.None);
    }

    [Given("the MR transformation {string} with parameter {string} set to {string}")]
    public void GivenTheMrTransformationWithParameter(string name, string parameterName, string parameterValue)
    {
        _transformation = new MrTransformation(
            name,
            new Dictionary<string, string> { [parameterName] = parameterValue });
    }

    [When("I run source and the generated follow-up with program profile {string}")]
    public async Task WhenIRunSourceAndTheGeneratedFollowUp(string profileName)
    {
        Assert.Equal("example-cli", profileName);
        Assert.NotNull(_sourceInputPath);
        Assert.NotNull(_followUpInputPath);
        Assert.NotNull(_transformation);

        var assetRoot = TestAssetPaths.AssetRoot();
        var program = new SystemProgram(
            ProgramLanguage.Python,
            "example-cli",
            TestAssetPaths.PythonExecutable(),
            $"{Path.Combine(assetRoot, "example_cli.py")} --input {{input}} --output {{output}}",
            Path.Combine(assetRoot, "example_output_adapter.py"));
        var task = SystemMtTask.WithGeneratedFollowUp(
            program,
            new SystemMtCase("source", _sourceInputPath!, _sourceDir, Path.Combine(_sourceDir, "output.txt")),
            followUpCaseName: "follow-up",
            followUpInputPath: _followUpInputPath!,
            followUpWorkingDirectory: _followUpDir,
            followUpOutputPath: Path.Combine(_followUpDir, "output.txt"),
            _transformation!,
            "GreaterThan",
            TimeSpan.FromSeconds(10));
        var runner = new SystemMtRunner(
            new CliProgramRunner(),
            new PythonOutputAdapter(TestAssetPaths.PythonExecutable()),
            new GreaterThanAssertion(),
            new InputGenerator(
                new PythonInputAdapter(TestAssetPaths.PythonExecutable()),
                Path.Combine(assetRoot, "example_output_adapter.py")));

        _result = await runner.RunAsync(task, "result", CancellationToken.None);
    }

    [Then("the parsed output value {string} of the generated follow-up should be greater than the source")]
    public void ThenTheParsedOutputValueShouldBeGreater(string valueName)
    {
        Assert.NotNull(_result);
        Assert.Equal("result", valueName);
        Assert.True(_result!.Passed, _result.FailureReason);
        Assert.NotNull(_result.InputGeneration);
        Assert.True(_result.InputGeneration!.Succeeded);
    }
}
```

- [ ] **Step 3: Run the new BDD scenario**

Reqnroll generates the test class name from the feature title; run by feature class substring:

```bash
dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --filter "FullyQualifiedName~SystemLevelMetamorphicTestingWithGeneratedFollowUpInputFeature"
```

Expected: PASS (1/1). If the filter does not match, list discovered tests with `dotnet test ... --list-tests | grep Feature` and adjust the filter to the actual class name.

- [ ] **Step 4: Run the full test suite to confirm no regression**

Run:

```bash
dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj
```

Expected: PASS (Stage 1 12 + Stage 2 model 2 + adapter 3 + generator 2 + runner 2 + BDD 1 = 22/22).

- [ ] **Step 5: Commit**

```bash
git add MetBench_SystemMT.Tests/Features/SystemLevelGeneratedFollowup.feature \
        MetBench_SystemMT.Tests/Steps/SystemLevelGeneratedFollowupSteps.cs
git commit -m "test(stage2): add BDD scenario for generated follow-up input"
```

## Task 9: Register Stage 2 Services in WPF DI

**Files:**

- Modify: `MetBench_Client/App.xaml.cs`

- [ ] **Step 1: Register the new services**

In `MetBench_Client/App.xaml.cs`, find the existing block:

```csharp
// System-level metamorphic testing
services.AddScoped<CliProgramRunner>();
services.AddScoped(provider => new PythonOutputAdapter(
    OperatingSystem.IsWindows() ? "python" : "python3"));
services.AddScoped<GreaterThanAssertion>();
services.AddScoped<SystemMtRunner>();
```

Replace it with:

```csharp
// System-level metamorphic testing
services.AddScoped<CliProgramRunner>();
services.AddScoped(provider => new PythonOutputAdapter(
    OperatingSystem.IsWindows() ? "python" : "python3"));
services.AddScoped(provider => new PythonInputAdapter(
    OperatingSystem.IsWindows() ? "python" : "python3"));
services.AddScoped<GreaterThanAssertion>();
// InputGenerator needs an adapter script path; register a factory the
// caller can override per-task in Stage 4. Stage 2 leaves the path
// resolution to whoever resolves InputGenerator at the call site.
services.AddScoped<InputGenerator>(provider =>
    throw new InvalidOperationException(
        "InputGenerator must be constructed with a per-task adapter path; resolve PythonInputAdapter and the adapter path from the task instead."));
services.AddScoped<SystemMtRunner>();
```

- [ ] **Step 2: Build the solution to verify DI compiles**

Run:

```bash
dotnet build MetBench.sln
```

Expected: PASS, 0 errors.

- [ ] **Step 3: Commit**

```bash
git add MetBench_Client/App.xaml.cs
git commit -m "chore(stage2): register PythonInputAdapter and InputGenerator factory"
```

## Task 10: Final Verification

**Files:**

- Verify: all changes from Tasks 1-9.

- [ ] **Step 1: Run the focused Stage 1+2 test project**

Run:

```bash
dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj
```

Expected: PASS (22/22 — Stage 1 BDD + 11 Stage 1 unit + Stage 2 model 2 + adapter 3 + generator 2 + runner 2 + BDD 1).

- [ ] **Step 2: Build the full solution**

Run:

```bash
dotnet build MetBench.sln
```

Expected: PASS, 0 errors.

- [ ] **Step 3: Check status and confirm no untracked files were left behind**

Run:

```bash
git status --short --branch
```

Expected: only the user's pre-existing `AGENTS.md` modification (memory timestamp) appears as modified; no untracked files.

- [ ] **Step 4: Push the feature branch**

Run:

```bash
git push -u origin <feature-branch-name>
```

Expected: branch is pushed; PR can be opened against `main`.

## Plan Self-review

Spec coverage (acceptance criteria from `docs/superpowers/specs/2026-05-07-system-level-mt-bdd-design.md` Stage 2):

| Criterion | Where addressed |
|---|---|
| Source input + MR transformation config produces a follow-up input file | Tasks 3 (adapter), 4 (Python invoker), 5 (orchestrator), 7 (runner) |
| Records source input, follow-up input, transformation parameters, and logs | Task 2 (`InputGenerationResult`), Task 5 (`InputGenerator` populates fields), Task 7 (runner attaches it to `SystemMtResult`) |
| Stage 1 execution can consume generated follow-up inputs | Task 7 (`SystemMtRunner` resolves `SystemMtCase` from generation), Task 8 (BDD scenario verifies end-to-end) |
| At least one numeric transformation supported | Task 3 (`ScalarMultiply` in adapter), Task 8 (BDD scenario uses it) |
| Generation failures return explicit errors | Task 4 (`PythonInputAdapter` throws with classified messages), Task 5 (`InputGenerator` returns failure result), Task 7 (runner surfaces `InputGenerationResult` and `FailureReason`) |
| Existing method-level MT behavior unaffected | No method-level files touched. `SystemMtTask` constructor change is the only Stage 1 ripple; Task 6 Step 6 updates the two affected call sites and Step 7 reruns full suite. |

Red-flag scan:

- No "TBD" / "implement later" / "similar to Task N" placeholders.
- Every code-changing step shows the full code or full replacement.
- BDD scenario filter has a fallback instruction (Task 8 Step 3) in case the Reqnroll-generated class name does not match the predicted substring.

Type consistency:

- `MrTransformation`, `InputGenerationResult`, `PythonInputAdapter`, `InputGenerator`, `SystemMtTask.WithFollowUpCase`, `SystemMtTask.WithGeneratedFollowUp`, `SystemMtResult.InputGeneration`, and `SystemMtRunner(InputGenerator?)` are spelled identically across plan, tests, and implementation.
- `SystemMtTask` removes the public constructor; the only Stage 1 call sites (`SystemMtRunnerTests.cs`, `SystemLevelCliMtSteps.cs`) are updated in Task 6 Step 6 to use the new factory method.
