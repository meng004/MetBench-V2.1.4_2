# System-level MT BDD Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the Stage 1 system-level metamorphic testing closed loop: Gherkin feature -> Reqnroll steps -> C# runner -> CLI execution -> Python output adapter -> C# MR assertion -> pass/fail result.

**Architecture:** Add a parallel system-level MT model under `MetBench_BLL/SystemMT` without changing the existing method-level `FunctionProgram` and `AutoRunMR_Await` flow. Reqnroll step definitions live in a new test project and call C# BLL services; Python is used only through an adapter process contract that returns normalized JSON.

**Tech Stack:** .NET 8 Windows-targeted class libraries, xUnit, Reqnroll.xUnit, C# `ProcessStartInfo`, Python adapter scripts, Gherkin `.feature` files.

---

## Scope Guard

This plan implements only Stage 1 from `docs/superpowers/specs/2026-05-07-system-level-mt-bdd-design.md`.

Do not implement these in this plan:

- automatic input generation;
- Randoop integration;
- production OpenMOC adapter;
- OpenMC/OpenMOC cross-program adapter reuse;
- WPF authoring screens;
- system-level MT reports.

## File Structure

Create:

- `MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj`  
  xUnit + Reqnroll test project that references `MetBench_BLL`.
- `MetBench_SystemMT.Tests/Features/SystemLevelCliMt.feature`  
  BDD scenario for the Stage 1 closed loop.
- `MetBench_SystemMT.Tests/Steps/SystemLevelCliMtSteps.cs`  
  Reqnroll step definitions that build a system MT task and call BLL.
- `MetBench_SystemMT.Tests/TestAssets/example_cli.py`  
  Tiny CLI program under test for deterministic source/follow-up runs.
- `MetBench_SystemMT.Tests/TestAssets/example_output_adapter.py`  
  Python adapter that parses output files into normalized JSON.
- `MetBench_BLL/SystemMT/SystemProgram.cs`  
  `TargetProgram` subclass for CLI-invoked system programs.
- `MetBench_BLL/SystemMT/SystemMtCase.cs`  
  One source or follow-up run case.
- `MetBench_BLL/SystemMT/SystemMtTask.cs`  
  Full source/follow-up MT task.
- `MetBench_BLL/SystemMT/CliRunResult.cs`  
  Process execution result.
- `MetBench_BLL/SystemMT/ParsedOutput.cs`  
  Adapter-normalized output values.
- `MetBench_BLL/SystemMT/SystemMtAssertionResult.cs`  
  MR assertion result.
- `MetBench_BLL/SystemMT/SystemMtResult.cs`  
  End-to-end system MT result.
- `MetBench_BLL/SystemMT/CliProgramRunner.cs`  
  C# process runner.
- `MetBench_BLL/SystemMT/PythonOutputAdapter.cs`  
  Adapter process invoker and JSON parser.
- `MetBench_BLL/SystemMT/GreaterThanAssertion.cs`  
  Minimal Stage 1 MR assertion.
- `MetBench_BLL/SystemMT/SystemMtRunner.cs`  
  End-to-end orchestrator.

Modify:

- `MetBench.sln`  
  Add `MetBench_SystemMT.Tests`.
- `MetBench_Client/App.xaml.cs`  
  Register Stage 1 services in dependency injection.
- `.gitignore`  
  Keep generated system MT test output ignored.

## Task 1: Create the Test Project and Reqnroll Harness

**Files:**

- Create: `MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj`
- Create: `MetBench_SystemMT.Tests/Features/SystemLevelCliMt.feature`
- Create: `MetBench_SystemMT.Tests/Steps/SystemLevelCliMtSteps.cs`
- Create: `MetBench_SystemMT.Tests/TestAssets/example_cli.py`
- Create: `MetBench_SystemMT.Tests/TestAssets/example_output_adapter.py`
- Modify: `MetBench.sln`

- [ ] **Step 1: Create xUnit test project**

Run:

```bash
rtk dotnet new xunit -n MetBench_SystemMT.Tests
```

Expected: command creates `MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj`.

- [ ] **Step 2: Add project reference and packages**

Run:

```bash
rtk dotnet sln MetBench.sln add MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj
rtk dotnet add MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj reference MetBench_BLL/MetBench_BLL.csproj
rtk dotnet add MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj package Reqnroll.xUnit
rtk dotnet add MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj package Reqnroll.Tools.MsBuild.Generation
```

Expected: solution contains `MetBench_SystemMT.Tests`, and package restore succeeds.

- [ ] **Step 3: Replace the test project file**

Replace `MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj` with:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <EnableWindowsTargeting>true</EnableWindowsTargeting>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.10.0" />
    <PackageReference Include="Reqnroll.xUnit" Version="3.3.4" />
    <PackageReference Include="Reqnroll.Tools.MsBuild.Generation" Version="3.3.4" />
    <PackageReference Include="xunit" Version="2.9.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\MetBench_BLL\MetBench_BLL.csproj" />
  </ItemGroup>

  <ItemGroup>
    <None Include="TestAssets\**\*">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
  </ItemGroup>
</Project>
```

- [ ] **Step 4: Add initial feature file**

Create `MetBench_SystemMT.Tests/Features/SystemLevelCliMt.feature`:

```gherkin
Feature: System-level metamorphic testing through CLI

  Scenario: Follow-up output value is greater than source output value
    Given a system MT case named "source" with input file "source-input.txt"
    And a system MT case named "follow-up" with input file "followup-input.txt"
    When I run both cases with program profile "example-cli"
    Then the parsed output value "result" of "follow-up" should be greater than "source"
```

- [ ] **Step 5: Add failing step definitions**

Create `MetBench_SystemMT.Tests/Steps/SystemLevelCliMtSteps.cs`:

```csharp
using Reqnroll;

namespace MetBench_SystemMT.Tests.Steps;

[Binding]
public sealed class SystemLevelCliMtSteps
{
    [Given("a system MT case named {string} with input file {string}")]
    public void GivenASystemMtCaseNamedWithInputFile(string caseName, string inputFile)
    {
        throw new NotImplementedException($"Case binding is not implemented: {caseName}, {inputFile}");
    }

    [When("I run both cases with program profile {string}")]
    public void WhenIRunBothCasesWithProgramProfile(string profileName)
    {
        throw new NotImplementedException($"Program profile is not implemented: {profileName}");
    }

    [Then("the parsed output value {string} of {string} should be greater than {string}")]
    public void ThenTheParsedOutputValueOfShouldBeGreaterThan(
        string valueName,
        string followUpCaseName,
        string sourceCaseName)
    {
        throw new NotImplementedException(
            $"Assertion is not implemented: {valueName}, {followUpCaseName}, {sourceCaseName}");
    }
}
```

- [ ] **Step 6: Add example CLI program**

Create `MetBench_SystemMT.Tests/TestAssets/example_cli.py`:

```python
import argparse
from pathlib import Path


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--input", required=True)
    parser.add_argument("--output", required=True)
    args = parser.parse_args()

    input_path = Path(args.input)
    output_path = Path(args.output)
    value = float(input_path.read_text(encoding="utf-8").strip())
    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(f"result={value}\n", encoding="utf-8")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
```

- [ ] **Step 7: Add example output adapter**

Create `MetBench_SystemMT.Tests/TestAssets/example_output_adapter.py`:

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


def main() -> int:
    parser = argparse.ArgumentParser()
    subparsers = parser.add_subparsers(dest="command", required=True)
    parse_parser = subparsers.add_parser("parse-output")
    parse_parser.add_argument("--output-file", required=True)
    args = parser.parse_args()

    if args.command == "parse-output":
        print(json.dumps(parse_output(args.output_file), ensure_ascii=False))
        return 0

    return 2


if __name__ == "__main__":
    raise SystemExit(main())
```

- [ ] **Step 8: Run the BDD test and verify it fails for the expected reason**

Run:

```bash
rtk dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --filter "FullyQualifiedName~SystemLevelCliMtFeature"
```

Expected: FAIL because step definitions throw `NotImplementedException`.

- [ ] **Step 9: Commit harness**

Run:

```bash
rtk git add MetBench.sln MetBench_SystemMT.Tests
rtk git commit -m "test: add system-level MT BDD harness"
```

## Task 2: Add System-level MT Models

**Files:**

- Create: `MetBench_BLL/SystemMT/SystemProgram.cs`
- Create: `MetBench_BLL/SystemMT/SystemMtCase.cs`
- Create: `MetBench_BLL/SystemMT/SystemMtTask.cs`
- Create: `MetBench_BLL/SystemMT/CliRunResult.cs`
- Create: `MetBench_BLL/SystemMT/ParsedOutput.cs`
- Create: `MetBench_BLL/SystemMT/SystemMtAssertionResult.cs`
- Create: `MetBench_BLL/SystemMT/SystemMtResult.cs`
- Create: `MetBench_SystemMT.Tests/SystemMT/SystemMtModelTests.cs`

- [ ] **Step 1: Write model tests**

Create `MetBench_SystemMT.Tests/SystemMT/SystemMtModelTests.cs`:

```csharp
using MetBench_BLL;
using MetBench_BLL.SystemMT;

namespace MetBench_SystemMT.Tests.SystemMT;

public sealed class SystemMtModelTests
{
    [Fact]
    public void SystemProgram_exposes_program_type_and_data()
    {
        var program = new SystemProgram(
            ProgramLanguage.Python,
            "example-cli",
            "python",
            "example_cli.py --input {input} --output {output}",
            "example_output_adapter.py");

        Assert.Equal("System", program.ProgramType);
        Assert.Equal("example-cli", program.ProfileName);
        Assert.Equal("python", program.ExecutablePath);
        Assert.Equal("example_cli.py --input {input} --output {output}", program.ArgumentTemplate);
        Assert.Equal("example_output_adapter.py", program.OutputAdapterPath);
    }

    [Fact]
    public void SystemMtCase_rejects_empty_case_name()
    {
        var error = Assert.Throws<ArgumentException>(() =>
            new SystemMtCase("", "input.txt", "work", "output.txt"));

        Assert.Contains("CaseName", error.Message);
    }

    [Fact]
    public void SystemMtTask_requires_different_source_and_followup_names()
    {
        var program = new SystemProgram(
            ProgramLanguage.Python,
            "example-cli",
            "python",
            "example_cli.py --input {input} --output {output}",
            "adapter.py");
        var source = new SystemMtCase("same", "source.txt", "work/source", "out.txt");
        var followUp = new SystemMtCase("same", "followup.txt", "work/followup", "out.txt");

        var error = Assert.Throws<ArgumentException>(() =>
            new SystemMtTask(program, source, followUp, "GreaterThan", TimeSpan.FromSeconds(5)));

        Assert.Contains("Source and follow-up case names must be different", error.Message);
    }
}
```

- [ ] **Step 2: Run tests and verify failure**

Run:

```bash
rtk dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --filter "FullyQualifiedName~SystemMtModelTests"
```

Expected: FAIL because `MetBench_BLL.SystemMT` types do not exist.

- [ ] **Step 3: Implement `SystemProgram`**

Create `MetBench_BLL/SystemMT/SystemProgram.cs`:

```csharp
namespace MetBench_BLL.SystemMT;

public sealed class SystemProgram : TargetProgram
{
    public SystemProgram(
        ProgramLanguage language,
        string profileName,
        string executablePath,
        string argumentTemplate,
        string outputAdapterPath,
        IReadOnlySet<int>? acceptableExitCodes = null)
        : base(language)
    {
        ProfileName = RequireText(profileName, nameof(ProfileName));
        ExecutablePath = RequireText(executablePath, nameof(ExecutablePath));
        ArgumentTemplate = RequireText(argumentTemplate, nameof(ArgumentTemplate));
        OutputAdapterPath = RequireText(outputAdapterPath, nameof(OutputAdapterPath));
        AcceptableExitCodes = acceptableExitCodes ?? new HashSet<int> { 0 };
    }

    public override string ProgramType => "System";

    public string ProfileName { get; }

    public string ExecutablePath { get; }

    public string ArgumentTemplate { get; }

    public string OutputAdapterPath { get; }

    public IReadOnlySet<int> AcceptableExitCodes { get; }

    public override object GetProgramData()
    {
        return new
        {
            Language = GetLanguageName(),
            ProfileName,
            ExecutablePath,
            ArgumentTemplate,
            OutputAdapterPath,
            AcceptableExitCodes
        };
    }

    private static string RequireText(string value, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{propertyName} cannot be empty", propertyName);
        }

        return value;
    }
}
```

- [ ] **Step 4: Implement `SystemMtCase`**

Create `MetBench_BLL/SystemMT/SystemMtCase.cs`:

```csharp
namespace MetBench_BLL.SystemMT;

public sealed class SystemMtCase
{
    public SystemMtCase(
        string caseName,
        string inputPath,
        string workingDirectory,
        string outputPath,
        IReadOnlyDictionary<string, string>? environmentVariables = null)
    {
        CaseName = RequireText(caseName, nameof(CaseName));
        InputPath = RequireText(inputPath, nameof(InputPath));
        WorkingDirectory = RequireText(workingDirectory, nameof(WorkingDirectory));
        OutputPath = RequireText(outputPath, nameof(OutputPath));
        EnvironmentVariables = environmentVariables ?? new Dictionary<string, string>();
    }

    public string CaseName { get; }

    public string InputPath { get; }

    public string WorkingDirectory { get; }

    public string OutputPath { get; }

    public IReadOnlyDictionary<string, string> EnvironmentVariables { get; }

    private static string RequireText(string value, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{propertyName} cannot be empty", propertyName);
        }

        return value;
    }
}
```

- [ ] **Step 5: Implement `SystemMtTask`**

Create `MetBench_BLL/SystemMT/SystemMtTask.cs`:

```csharp
namespace MetBench_BLL.SystemMT;

public sealed class SystemMtTask
{
    public SystemMtTask(
        SystemProgram program,
        SystemMtCase sourceCase,
        SystemMtCase followUpCase,
        string assertionName,
        TimeSpan timeout)
    {
        Program = program ?? throw new ArgumentNullException(nameof(program));
        SourceCase = sourceCase ?? throw new ArgumentNullException(nameof(sourceCase));
        FollowUpCase = followUpCase ?? throw new ArgumentNullException(nameof(followUpCase));
        AssertionName = RequireText(assertionName, nameof(AssertionName));
        Timeout = timeout;

        if (SourceCase.CaseName.Equals(FollowUpCase.CaseName, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Source and follow-up case names must be different");
        }

        if (timeout <= TimeSpan.Zero)
        {
            throw new ArgumentException("Timeout must be greater than zero", nameof(timeout));
        }
    }

    public SystemProgram Program { get; }

    public SystemMtCase SourceCase { get; }

    public SystemMtCase FollowUpCase { get; }

    public string AssertionName { get; }

    public TimeSpan Timeout { get; }

    private static string RequireText(string value, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{propertyName} cannot be empty", propertyName);
        }

        return value;
    }
}
```

- [ ] **Step 6: Implement result records**

Create `MetBench_BLL/SystemMT/CliRunResult.cs`:

```csharp
namespace MetBench_BLL.SystemMT;

public sealed record CliRunResult(
    string CaseName,
    int ExitCode,
    string StandardOutput,
    string StandardError,
    TimeSpan Elapsed,
    string OutputPath,
    bool Succeeded,
    string FailureReason);
```

Create `MetBench_BLL/SystemMT/ParsedOutput.cs`:

```csharp
namespace MetBench_BLL.SystemMT;

public sealed record ParsedOutput(
    IReadOnlyDictionary<string, double> Values,
    IReadOnlyDictionary<string, string> Metadata);
```

Create `MetBench_BLL/SystemMT/SystemMtAssertionResult.cs`:

```csharp
namespace MetBench_BLL.SystemMT;

public sealed record SystemMtAssertionResult(
    string AssertionName,
    string ValueName,
    double SourceValue,
    double FollowUpValue,
    bool Passed,
    string FailureReason);
```

Create `MetBench_BLL/SystemMT/SystemMtResult.cs`:

```csharp
namespace MetBench_BLL.SystemMT;

public sealed record SystemMtResult(
    CliRunResult SourceRun,
    CliRunResult FollowUpRun,
    ParsedOutput SourceOutput,
    ParsedOutput FollowUpOutput,
    SystemMtAssertionResult Assertion,
    bool Passed,
    string FailureReason);
```

- [ ] **Step 7: Run model tests**

Run:

```bash
rtk dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --filter "FullyQualifiedName~SystemMtModelTests"
```

Expected: PASS.

- [ ] **Step 8: Commit models**

Run:

```bash
rtk git add MetBench_BLL/SystemMT MetBench_SystemMT.Tests/SystemMT/SystemMtModelTests.cs
rtk git commit -m "feat: add system-level MT task models"
```

## Task 3: Implement CLI Program Runner

**Files:**

- Create: `MetBench_BLL/SystemMT/CliProgramRunner.cs`
- Create: `MetBench_SystemMT.Tests/SystemMT/CliProgramRunnerTests.cs`

- [ ] **Step 1: Write CLI runner tests**

Create `MetBench_SystemMT.Tests/SystemMT/CliProgramRunnerTests.cs`:

```csharp
using MetBench_BLL;
using MetBench_BLL.SystemMT;

namespace MetBench_SystemMT.Tests.SystemMT;

public sealed class CliProgramRunnerTests
{
    [Fact]
    public async Task RunAsync_starts_program_and_writes_output_file()
    {
        var assetRoot = TestAssetPaths.AssetRoot();
        var workDir = Path.Combine(Path.GetTempPath(), "MetBenchSystemMt", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workDir);
        var inputPath = Path.Combine(workDir, "input.txt");
        var outputPath = Path.Combine(workDir, "output.txt");
        await File.WriteAllTextAsync(inputPath, "7", CancellationToken.None);

        var program = new SystemProgram(
            ProgramLanguage.Python,
            "example-cli",
            TestAssetPaths.PythonExecutable(),
            $"{Path.Combine(assetRoot, "example_cli.py")} --input {{input}} --output {{output}}",
            Path.Combine(assetRoot, "example_output_adapter.py"));
        var testCase = new SystemMtCase("source", inputPath, workDir, outputPath);
        var runner = new CliProgramRunner();

        var result = await runner.RunAsync(program, testCase, TimeSpan.FromSeconds(10), CancellationToken.None);

        Assert.True(result.Succeeded, result.FailureReason);
        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(outputPath));
        Assert.Contains("result=7", await File.ReadAllTextAsync(outputPath, CancellationToken.None));
    }

    [Fact]
    public async Task RunAsync_reports_missing_input_file()
    {
        var assetRoot = TestAssetPaths.AssetRoot();
        var workDir = Path.Combine(Path.GetTempPath(), "MetBenchSystemMt", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workDir);

        var program = new SystemProgram(
            ProgramLanguage.Python,
            "example-cli",
            TestAssetPaths.PythonExecutable(),
            $"{Path.Combine(assetRoot, "example_cli.py")} --input {{input}} --output {{output}}",
            Path.Combine(assetRoot, "example_output_adapter.py"));
        var testCase = new SystemMtCase(
            "source",
            Path.Combine(workDir, "missing.txt"),
            workDir,
            Path.Combine(workDir, "output.txt"));
        var runner = new CliProgramRunner();

        var result = await runner.RunAsync(program, testCase, TimeSpan.FromSeconds(10), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains("Input file does not exist", result.FailureReason);
    }
}
```

- [ ] **Step 2: Add shared test asset helper**

Create `MetBench_SystemMT.Tests/SystemMT/TestAssetPaths.cs`:

```csharp
namespace MetBench_SystemMT.Tests.SystemMT;

internal static class TestAssetPaths
{
    public static string AssetRoot()
    {
        return Path.Combine(AppContext.BaseDirectory, "TestAssets");
    }

    public static string PythonExecutable()
    {
        var configured = Environment.GetEnvironmentVariable("METBENCH_TEST_PYTHON");
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured;
        }

        return OperatingSystem.IsWindows() ? "python" : "python3";
    }
}
```

- [ ] **Step 3: Run tests and verify failure**

Run:

```bash
rtk dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --filter "FullyQualifiedName~CliProgramRunnerTests"
```

Expected: FAIL because `CliProgramRunner` does not exist.

- [ ] **Step 4: Implement CLI runner**

Create `MetBench_BLL/SystemMT/CliProgramRunner.cs`:

```csharp
using System.Diagnostics;

namespace MetBench_BLL.SystemMT;

public sealed class CliProgramRunner
{
    public async Task<CliRunResult> RunAsync(
        SystemProgram program,
        SystemMtCase testCase,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(testCase.InputPath))
        {
            return Failed(testCase, -1, string.Empty, string.Empty, TimeSpan.Zero,
                $"Configuration failure: Input file does not exist for case '{testCase.CaseName}': {testCase.InputPath}");
        }

        Directory.CreateDirectory(testCase.WorkingDirectory);
        var arguments = BuildArguments(program.ArgumentTemplate, testCase);

        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = program.ExecutablePath,
            Arguments = arguments,
            WorkingDirectory = testCase.WorkingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        foreach (var item in testCase.EnvironmentVariables)
        {
            process.StartInfo.Environment[item.Key] = item.Value;
        }

        var stopwatch = Stopwatch.StartNew();
        try
        {
            process.Start();
            var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

            var completed = await WaitForExitAsync(process, timeout, cancellationToken);
            stopwatch.Stop();
            if (!completed)
            {
                TryKill(process);
                return Failed(testCase, -1, await stdoutTask, await stderrTask, stopwatch.Elapsed,
                    $"CLI execution failure: case '{testCase.CaseName}' timed out after {timeout.TotalSeconds:0.###} seconds");
            }

            var stdout = await stdoutTask;
            var stderr = await stderrTask;
            var exitCodeAccepted = program.AcceptableExitCodes.Contains(process.ExitCode);
            if (!exitCodeAccepted)
            {
                return Failed(testCase, process.ExitCode, stdout, stderr, stopwatch.Elapsed,
                    $"CLI execution failure: case '{testCase.CaseName}' exited with code {process.ExitCode}");
            }

            if (!File.Exists(testCase.OutputPath))
            {
                return Failed(testCase, process.ExitCode, stdout, stderr, stopwatch.Elapsed,
                    $"Output artifact failure: expected output file is missing for case '{testCase.CaseName}': {testCase.OutputPath}");
            }

            return new CliRunResult(
                testCase.CaseName,
                process.ExitCode,
                stdout,
                stderr,
                stopwatch.Elapsed,
                testCase.OutputPath,
                true,
                string.Empty);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return Failed(testCase, -1, string.Empty, ex.ToString(), stopwatch.Elapsed,
                $"CLI execution failure: case '{testCase.CaseName}' could not start: {ex.Message}");
        }
    }

    private static string BuildArguments(string argumentTemplate, SystemMtCase testCase)
    {
        return argumentTemplate
            .Replace("{input}", Quote(testCase.InputPath), StringComparison.Ordinal)
            .Replace("{output}", Quote(testCase.OutputPath), StringComparison.Ordinal);
    }

    private static string Quote(string value)
    {
        return value.Contains(' ') ? $"\"{value}\"" : value;
    }

    private static async Task<bool> WaitForExitAsync(Process process, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var exitTask = process.WaitForExitAsync(cancellationToken);
        var timeoutTask = Task.Delay(timeout, cancellationToken);
        return await Task.WhenAny(exitTask, timeoutTask) == exitTask;
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // Failure to kill is already represented as a timeout failure.
        }
    }

    private static CliRunResult Failed(
        SystemMtCase testCase,
        int exitCode,
        string stdout,
        string stderr,
        TimeSpan elapsed,
        string reason)
    {
        return new CliRunResult(testCase.CaseName, exitCode, stdout, stderr, elapsed, testCase.OutputPath, false, reason);
    }
}
```

- [ ] **Step 5: Run CLI runner tests**

Run:

```bash
rtk dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --filter "FullyQualifiedName~CliProgramRunnerTests"
```

Expected: PASS.

- [ ] **Step 6: Commit CLI runner**

Run:

```bash
rtk git add MetBench_BLL/SystemMT/CliProgramRunner.cs MetBench_SystemMT.Tests/SystemMT/CliProgramRunnerTests.cs MetBench_SystemMT.Tests/SystemMT/TestAssetPaths.cs
rtk git commit -m "feat: add CLI runner for system-level MT"
```

## Task 4: Implement Python Output Adapter Invoker

**Files:**

- Create: `MetBench_BLL/SystemMT/PythonOutputAdapter.cs`
- Create: `MetBench_SystemMT.Tests/SystemMT/PythonOutputAdapterTests.cs`

- [ ] **Step 1: Write adapter tests**

Create `MetBench_SystemMT.Tests/SystemMT/PythonOutputAdapterTests.cs`:

```csharp
using MetBench_BLL.SystemMT;

namespace MetBench_SystemMT.Tests.SystemMT;

public sealed class PythonOutputAdapterTests
{
    [Fact]
    public async Task ParseAsync_returns_normalized_values()
    {
        var workDir = Path.Combine(Path.GetTempPath(), "MetBenchSystemMt", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workDir);
        var outputPath = Path.Combine(workDir, "output.txt");
        await File.WriteAllTextAsync(outputPath, "result=12.5\n", CancellationToken.None);

        var adapter = new PythonOutputAdapter(TestAssetPaths.PythonExecutable());
        var parsed = await adapter.ParseAsync(
            Path.Combine(TestAssetPaths.AssetRoot(), "example_output_adapter.py"),
            outputPath,
            CancellationToken.None);

        Assert.Equal(12.5, parsed.Values["result"]);
        Assert.Equal("example", parsed.Metadata["adapter"]);
    }

    [Fact]
    public async Task ParseAsync_reports_missing_output_file()
    {
        var adapter = new PythonOutputAdapter(TestAssetPaths.PythonExecutable());

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            adapter.ParseAsync(
                Path.Combine(TestAssetPaths.AssetRoot(), "example_output_adapter.py"),
                Path.Combine(Path.GetTempPath(), "missing-output.txt"),
                CancellationToken.None));

        Assert.Contains("Output artifact failure", error.Message);
    }
}
```

- [ ] **Step 2: Run tests and verify failure**

Run:

```bash
rtk dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --filter "FullyQualifiedName~PythonOutputAdapterTests"
```

Expected: FAIL because `PythonOutputAdapter` does not exist.

- [ ] **Step 3: Implement adapter invoker**

Create `MetBench_BLL/SystemMT/PythonOutputAdapter.cs`:

```csharp
using System.Diagnostics;
using System.Text.Json;

namespace MetBench_BLL.SystemMT;

public sealed class PythonOutputAdapter
{
    private readonly string _pythonExecutable;

    public PythonOutputAdapter(string pythonExecutable)
    {
        _pythonExecutable = string.IsNullOrWhiteSpace(pythonExecutable)
            ? throw new ArgumentException("Python executable cannot be empty", nameof(pythonExecutable))
            : pythonExecutable;
    }

    public async Task<ParsedOutput> ParseAsync(
        string adapterPath,
        string outputPath,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(outputPath))
        {
            throw new InvalidOperationException($"Output artifact failure: output file does not exist: {outputPath}");
        }

        if (!File.Exists(adapterPath))
        {
            throw new InvalidOperationException($"Configuration failure: adapter file does not exist: {adapterPath}");
        }

        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = _pythonExecutable,
            Arguments = $"{Quote(adapterPath)} parse-output --output-file {Quote(outputPath)}",
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
                $"Adapter failure: adapter exited with code {process.ExitCode}. stderr: {stderr}");
        }

        return ParseJson(stdout);
    }

    private static ParsedOutput ParseJson(string stdout)
    {
        try
        {
            using var document = JsonDocument.Parse(stdout);
            var root = document.RootElement;
            var values = new Dictionary<string, double>();
            foreach (var item in root.GetProperty("values").EnumerateObject())
            {
                values[item.Name] = item.Value.GetDouble();
            }

            var metadata = new Dictionary<string, string>();
            if (root.TryGetProperty("metadata", out var metadataElement))
            {
                foreach (var item in metadataElement.EnumerateObject())
                {
                    metadata[item.Name] = item.Value.ToString();
                }
            }

            return new ParsedOutput(values, metadata);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Adapter failure: adapter returned invalid JSON. {ex.Message}", ex);
        }
    }

    private static string Quote(string value)
    {
        return value.Contains(' ') ? $"\"{value}\"" : value;
    }
}
```

- [ ] **Step 4: Run adapter tests**

Run:

```bash
rtk dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --filter "FullyQualifiedName~PythonOutputAdapterTests"
```

Expected: PASS.

- [ ] **Step 5: Commit adapter invoker**

Run:

```bash
rtk git add MetBench_BLL/SystemMT/PythonOutputAdapter.cs MetBench_SystemMT.Tests/SystemMT/PythonOutputAdapterTests.cs
rtk git commit -m "feat: add Python output adapter invoker"
```

## Task 5: Implement Greater-than MR Assertion

**Files:**

- Create: `MetBench_BLL/SystemMT/GreaterThanAssertion.cs`
- Create: `MetBench_SystemMT.Tests/SystemMT/GreaterThanAssertionTests.cs`

- [ ] **Step 1: Write assertion tests**

Create `MetBench_SystemMT.Tests/SystemMT/GreaterThanAssertionTests.cs`:

```csharp
using MetBench_BLL.SystemMT;

namespace MetBench_SystemMT.Tests.SystemMT;

public sealed class GreaterThanAssertionTests
{
    [Fact]
    public void Evaluate_passes_when_followup_value_is_greater()
    {
        var assertion = new GreaterThanAssertion();
        var source = new ParsedOutput(new Dictionary<string, double> { ["result"] = 2 }, new Dictionary<string, string>());
        var followUp = new ParsedOutput(new Dictionary<string, double> { ["result"] = 5 }, new Dictionary<string, string>());

        var result = assertion.Evaluate("result", source, followUp);

        Assert.True(result.Passed, result.FailureReason);
        Assert.Equal(2, result.SourceValue);
        Assert.Equal(5, result.FollowUpValue);
    }

    [Fact]
    public void Evaluate_fails_when_followup_value_is_not_greater()
    {
        var assertion = new GreaterThanAssertion();
        var source = new ParsedOutput(new Dictionary<string, double> { ["result"] = 5 }, new Dictionary<string, string>());
        var followUp = new ParsedOutput(new Dictionary<string, double> { ["result"] = 5 }, new Dictionary<string, string>());

        var result = assertion.Evaluate("result", source, followUp);

        Assert.False(result.Passed);
        Assert.Contains("Expected follow-up value", result.FailureReason);
    }

    [Fact]
    public void Evaluate_fails_when_value_is_missing()
    {
        var assertion = new GreaterThanAssertion();
        var source = new ParsedOutput(new Dictionary<string, double>(), new Dictionary<string, string>());
        var followUp = new ParsedOutput(new Dictionary<string, double> { ["result"] = 5 }, new Dictionary<string, string>());

        var result = assertion.Evaluate("result", source, followUp);

        Assert.False(result.Passed);
        Assert.Contains("Missing parsed value", result.FailureReason);
    }
}
```

- [ ] **Step 2: Run tests and verify failure**

Run:

```bash
rtk dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --filter "FullyQualifiedName~GreaterThanAssertionTests"
```

Expected: FAIL because `GreaterThanAssertion` does not exist.

- [ ] **Step 3: Implement assertion**

Create `MetBench_BLL/SystemMT/GreaterThanAssertion.cs`:

```csharp
namespace MetBench_BLL.SystemMT;

public sealed class GreaterThanAssertion
{
    public SystemMtAssertionResult Evaluate(string valueName, ParsedOutput source, ParsedOutput followUp)
    {
        if (!source.Values.TryGetValue(valueName, out var sourceValue))
        {
            return new SystemMtAssertionResult(
                "GreaterThan",
                valueName,
                double.NaN,
                double.NaN,
                false,
                $"Assertion failure: Missing parsed value '{valueName}' in source output");
        }

        if (!followUp.Values.TryGetValue(valueName, out var followUpValue))
        {
            return new SystemMtAssertionResult(
                "GreaterThan",
                valueName,
                sourceValue,
                double.NaN,
                false,
                $"Assertion failure: Missing parsed value '{valueName}' in follow-up output");
        }

        var passed = followUpValue > sourceValue;
        return new SystemMtAssertionResult(
            "GreaterThan",
            valueName,
            sourceValue,
            followUpValue,
            passed,
            passed
                ? string.Empty
                : $"Assertion failure: Expected follow-up value {followUpValue} to be greater than source value {sourceValue} for '{valueName}'");
    }
}
```

- [ ] **Step 4: Run assertion tests**

Run:

```bash
rtk dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --filter "FullyQualifiedName~GreaterThanAssertionTests"
```

Expected: PASS.

- [ ] **Step 5: Commit assertion**

Run:

```bash
rtk git add MetBench_BLL/SystemMT/GreaterThanAssertion.cs MetBench_SystemMT.Tests/SystemMT/GreaterThanAssertionTests.cs
rtk git commit -m "feat: add greater-than system MT assertion"
```

## Task 6: Implement End-to-end System MT Runner

**Files:**

- Create: `MetBench_BLL/SystemMT/SystemMtRunner.cs`
- Create: `MetBench_SystemMT.Tests/SystemMT/SystemMtRunnerTests.cs`

- [ ] **Step 1: Write runner integration test**

Create `MetBench_SystemMT.Tests/SystemMT/SystemMtRunnerTests.cs`:

```csharp
using MetBench_BLL;
using MetBench_BLL.SystemMT;

namespace MetBench_SystemMT.Tests.SystemMT;

public sealed class SystemMtRunnerTests
{
    [Fact]
    public async Task RunAsync_executes_source_and_followup_and_asserts_mr()
    {
        var assetRoot = TestAssetPaths.AssetRoot();
        var root = Path.Combine(Path.GetTempPath(), "MetBenchSystemMt", Guid.NewGuid().ToString("N"));
        var sourceDir = Path.Combine(root, "source");
        var followUpDir = Path.Combine(root, "followup");
        Directory.CreateDirectory(sourceDir);
        Directory.CreateDirectory(followUpDir);

        var sourceInput = Path.Combine(sourceDir, "input.txt");
        var followUpInput = Path.Combine(followUpDir, "input.txt");
        await File.WriteAllTextAsync(sourceInput, "3", CancellationToken.None);
        await File.WriteAllTextAsync(followUpInput, "9", CancellationToken.None);

        var program = new SystemProgram(
            ProgramLanguage.Python,
            "example-cli",
            TestAssetPaths.PythonExecutable(),
            $"{Path.Combine(assetRoot, "example_cli.py")} --input {{input}} --output {{output}}",
            Path.Combine(assetRoot, "example_output_adapter.py"));
        var task = new SystemMtTask(
            program,
            new SystemMtCase("source", sourceInput, sourceDir, Path.Combine(sourceDir, "output.txt")),
            new SystemMtCase("follow-up", followUpInput, followUpDir, Path.Combine(followUpDir, "output.txt")),
            "GreaterThan",
            TimeSpan.FromSeconds(10));
        var runner = new SystemMtRunner(
            new CliProgramRunner(),
            new PythonOutputAdapter(TestAssetPaths.PythonExecutable()),
            new GreaterThanAssertion());

        var result = await runner.RunAsync(task, "result", CancellationToken.None);

        Assert.True(result.Passed, result.FailureReason);
        Assert.Equal(3, result.Assertion.SourceValue);
        Assert.Equal(9, result.Assertion.FollowUpValue);
    }
}
```

- [ ] **Step 2: Run test and verify failure**

Run:

```bash
rtk dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --filter "FullyQualifiedName~SystemMtRunnerTests"
```

Expected: FAIL because `SystemMtRunner` does not exist.

- [ ] **Step 3: Implement runner**

Create `MetBench_BLL/SystemMT/SystemMtRunner.cs`:

```csharp
namespace MetBench_BLL.SystemMT;

public sealed class SystemMtRunner
{
    private readonly CliProgramRunner _cliRunner;
    private readonly PythonOutputAdapter _outputAdapter;
    private readonly GreaterThanAssertion _greaterThanAssertion;

    public SystemMtRunner(
        CliProgramRunner cliRunner,
        PythonOutputAdapter outputAdapter,
        GreaterThanAssertion greaterThanAssertion)
    {
        _cliRunner = cliRunner;
        _outputAdapter = outputAdapter;
        _greaterThanAssertion = greaterThanAssertion;
    }

    public async Task<SystemMtResult> RunAsync(
        SystemMtTask task,
        string valueName,
        CancellationToken cancellationToken)
    {
        var sourceRun = await _cliRunner.RunAsync(task.Program, task.SourceCase, task.Timeout, cancellationToken);
        if (!sourceRun.Succeeded)
        {
            return FailedBeforeParsing(sourceRun, sourceRun, valueName, sourceRun.FailureReason);
        }

        var followUpRun = await _cliRunner.RunAsync(task.Program, task.FollowUpCase, task.Timeout, cancellationToken);
        if (!followUpRun.Succeeded)
        {
            return FailedBeforeParsing(sourceRun, followUpRun, valueName, followUpRun.FailureReason);
        }

        var sourceOutput = await _outputAdapter.ParseAsync(task.Program.OutputAdapterPath, sourceRun.OutputPath, cancellationToken);
        var followUpOutput = await _outputAdapter.ParseAsync(task.Program.OutputAdapterPath, followUpRun.OutputPath, cancellationToken);
        var assertion = task.AssertionName switch
        {
            "GreaterThan" => _greaterThanAssertion.Evaluate(valueName, sourceOutput, followUpOutput),
            _ => new SystemMtAssertionResult(
                task.AssertionName,
                valueName,
                double.NaN,
                double.NaN,
                false,
                $"Configuration failure: unsupported assertion '{task.AssertionName}'")
        };

        return new SystemMtResult(
            sourceRun,
            followUpRun,
            sourceOutput,
            followUpOutput,
            assertion,
            assertion.Passed,
            assertion.FailureReason);
    }

    private static SystemMtResult FailedBeforeParsing(
        CliRunResult sourceRun,
        CliRunResult followUpRun,
        string valueName,
        string failureReason)
    {
        var emptyOutput = new ParsedOutput(new Dictionary<string, double>(), new Dictionary<string, string>());
        var assertion = new SystemMtAssertionResult(
            "GreaterThan",
            valueName,
            double.NaN,
            double.NaN,
            false,
            failureReason);

        return new SystemMtResult(
            sourceRun,
            followUpRun,
            emptyOutput,
            emptyOutput,
            assertion,
            false,
            failureReason);
    }
}
```

- [ ] **Step 4: Run runner tests**

Run:

```bash
rtk dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --filter "FullyQualifiedName~SystemMtRunnerTests"
```

Expected: PASS.

- [ ] **Step 5: Commit runner**

Run:

```bash
rtk git add MetBench_BLL/SystemMT/SystemMtRunner.cs MetBench_SystemMT.Tests/SystemMT/SystemMtRunnerTests.cs
rtk git commit -m "feat: add system-level MT runner"
```

## Task 7: Wire Reqnroll Steps to the Runner

**Files:**

- Modify: `MetBench_SystemMT.Tests/Steps/SystemLevelCliMtSteps.cs`

- [ ] **Step 1: Replace step definitions with real BDD workflow**

Replace `MetBench_SystemMT.Tests/Steps/SystemLevelCliMtSteps.cs` with:

```csharp
using MetBench_BLL;
using MetBench_BLL.SystemMT;
using MetBench_SystemMT.Tests.SystemMT;
using Reqnroll;

namespace MetBench_SystemMT.Tests.Steps;

[Binding]
public sealed class SystemLevelCliMtSteps
{
    private readonly Dictionary<string, SystemMtCase> _cases = new(StringComparer.OrdinalIgnoreCase);
    private SystemMtResult? _result;

    [Given("a system MT case named {string} with input file {string}")]
    public async Task GivenASystemMtCaseNamedWithInputFile(string caseName, string inputFile)
    {
        var root = Path.Combine(Path.GetTempPath(), "MetBenchSystemMtBdd", Guid.NewGuid().ToString("N"));
        var caseDir = Path.Combine(root, caseName);
        Directory.CreateDirectory(caseDir);

        var inputPath = Path.Combine(caseDir, inputFile);
        var outputPath = Path.Combine(caseDir, "output.txt");
        var value = caseName.Equals("source", StringComparison.OrdinalIgnoreCase) ? "3" : "9";
        await File.WriteAllTextAsync(inputPath, value, CancellationToken.None);

        _cases[caseName] = new SystemMtCase(caseName, inputPath, caseDir, outputPath);
    }

    [When("I run both cases with program profile {string}")]
    public async Task WhenIRunBothCasesWithProgramProfile(string profileName)
    {
        Assert.Equal("example-cli", profileName);

        var assetRoot = TestAssetPaths.AssetRoot();
        var program = new SystemProgram(
            ProgramLanguage.Python,
            "example-cli",
            TestAssetPaths.PythonExecutable(),
            $"{Path.Combine(assetRoot, "example_cli.py")} --input {{input}} --output {{output}}",
            Path.Combine(assetRoot, "example_output_adapter.py"));
        var task = new SystemMtTask(
            program,
            _cases["source"],
            _cases["follow-up"],
            "GreaterThan",
            TimeSpan.FromSeconds(10));
        var runner = new SystemMtRunner(
            new CliProgramRunner(),
            new PythonOutputAdapter(TestAssetPaths.PythonExecutable()),
            new GreaterThanAssertion());

        _result = await runner.RunAsync(task, "result", CancellationToken.None);
    }

    [Then("the parsed output value {string} of {string} should be greater than {string}")]
    public void ThenTheParsedOutputValueOfShouldBeGreaterThan(
        string valueName,
        string followUpCaseName,
        string sourceCaseName)
    {
        Assert.NotNull(_result);
        Assert.Equal("result", valueName);
        Assert.Equal("follow-up", followUpCaseName);
        Assert.Equal("source", sourceCaseName);
        Assert.True(_result.Passed, _result.FailureReason);
    }
}
```

- [ ] **Step 2: Run the Reqnroll feature**

Run:

```bash
rtk dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --filter "FullyQualifiedName~SystemLevelCliMtFeature"
```

Expected: PASS.

- [ ] **Step 3: Commit BDD steps**

Run:

```bash
rtk git add MetBench_SystemMT.Tests/Steps/SystemLevelCliMtSteps.cs
rtk git commit -m "test: wire system-level MT Reqnroll steps"
```

## Task 8: Register System MT Services and Ignore Test Output

**Files:**

- Modify: `MetBench_Client/App.xaml.cs`
- Modify: `.gitignore`

- [ ] **Step 1: Register services in WPF DI**

In `MetBench_Client/App.xaml.cs`, add this using near the existing BLL using statements:

```csharp
using MetBench_BLL.SystemMT;
```

Inside `.ConfigureServices`, after the existing method-level MT registration:

```csharp
// System-level metamorphic testing
services.AddScoped<CliProgramRunner>();
services.AddScoped(provider => new PythonOutputAdapter(
    OperatingSystem.IsWindows() ? "python" : "python3"));
services.AddScoped<GreaterThanAssertion>();
services.AddScoped<SystemMtRunner>();
```

- [ ] **Step 2: Ignore generated system MT temp artifacts**

Add to `.gitignore`:

```gitignore
# System-level MT local artifacts
MetBench_SystemMT.Tests/TestResults/
MetBenchSystemMt/
MetBenchSystemMtBdd/
```

- [ ] **Step 3: Build solution**

Run:

```bash
rtk dotnet build MetBench.sln
```

Expected: build succeeds. If the WPF project fails on macOS because Windows targeting is not enabled for the app project, run the focused test project verification in Step 4 and record the WPF build limitation in the final implementation note.

- [ ] **Step 4: Run full Stage 1 test project**

Run:

```bash
rtk dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj
```

Expected: PASS for all system-level MT tests.

- [ ] **Step 5: Commit DI and ignore updates**

Run:

```bash
rtk git add MetBench_Client/App.xaml.cs .gitignore
rtk git commit -m "chore: register system-level MT services"
```

## Task 9: Final Verification and Push

**Files:**

- Verify: all files changed by Tasks 1-8.

- [ ] **Step 1: Check status**

Run:

```bash
rtk git status --short --branch
```

Expected: only known user-owned unrelated files may be modified. Do not include automatic `AGENTS.md` memory timestamp changes in implementation commits.

- [ ] **Step 2: Run final focused verification**

Run:

```bash
rtk dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj
```

Expected: PASS.

- [ ] **Step 3: Run solution build when environment allows**

Run:

```bash
rtk dotnet build MetBench.sln
```

Expected on Windows or a correctly configured non-Windows .NET SDK: PASS. If it fails because existing WPF Windows targeting cannot build on the current host, include the exact error in the final implementation summary and keep the focused test project result as the Stage 1 verification.

- [ ] **Step 4: Push branch**

Run:

```bash
rtk git push
```

Expected: `main` pushes to `origin/main`.

## Plan Self-review

Spec coverage:

- Gherkin feature representation: Task 1 and Task 7.
- Reqnroll execution: Task 1 and Task 7.
- C# business orchestration: Task 2, Task 6, Task 8.
- CLI invocation: Task 3.
- Python output adapter: Task 4.
- Greater-than MR assertion: Task 5.
- End-to-end pass/fail result: Task 6 and Task 7.
- Existing method-level MT compatibility: Task 2 adds parallel models, Task 8 only registers new services.

Red-flag scan:

- No task uses unresolved filler language or unspecified error-handling instructions.
- Each code-changing step includes concrete code or exact insertion text.

Type consistency:

- `SystemProgram`, `SystemMtCase`, `SystemMtTask`, `CliRunResult`, `ParsedOutput`, `SystemMtAssertionResult`, `SystemMtResult`, `CliProgramRunner`, `PythonOutputAdapter`, `GreaterThanAssertion`, and `SystemMtRunner` names are consistent across tests and implementation steps.
