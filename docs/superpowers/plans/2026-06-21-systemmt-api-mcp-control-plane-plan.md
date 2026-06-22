# System MT API / MCP Control Plane Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Harden Runtime MCP, carry Runtime MCP run evidence into MetBench, then add a job-oriented REST API and agent-facing Business MCP without creating a second System MT workflow.

**Architecture:** Runtime MCP remains an execution-plane adapter behind `IRuntimeProcessExecutor`. A new BLL.Core control-plane service is the only business API surface; REST API and Business MCP call that service and never call Docker Runtime MCP directly. Public API/MCP contracts use job ids and artifact ids, not host paths.

**Tech Stack:** .NET 8, ASP.NET Core Minimal API, xUnit, Python 3 stdlib `unittest`, existing MetBench System MT job/launcher/runtime/evidence services.

---

## File Structure

- Modify `infra/mcp/docker-runtime/server.py`: replace raw `argv` execution requests with allowlisted `{ image, tool, args }` requests and keep bounded run records.
- Modify `infra/mcp/docker-runtime/config*.example.json`: add `allowed_tools`.
- Modify `infra/mcp/docker-runtime/tests/test_server.py`: RED/GREEN tests for allowlisted tools, rejected raw argv, rejected unsafe args, and returned run ids.
- Modify `MetBench_BLL.Core/SystemMT/Runtime/RuntimeModels.cs`: extend `DockerMcpRuntimeOptions` with `ToolName` and `LocalExecutable`.
- Modify `MetBench_BLL.Core/SystemMT/Runtime/LauncherOptionsRuntimeProfileProvider.cs`: parse `tool` and `local` query parameters from `docker-mcp://` runtime values.
- Modify `MetBench_BLL.Core/SystemMT/Runtime/DockerMcpRuntimeClient.cs`: send structured run requests and parse `run_id`.
- Modify `MetBench_BLL.Core/SystemMT/Runtime/DockerMcpProcessExecutor.cs`: validate the configured local executable and safe args before calling MCP.
- Modify `MetBench_BLL.Core/SystemMT/Pipeline/IProcessExecutor.cs`: add optional runtime metadata to `ProcessResult` without breaking local executor callers.
- Modify `MetBench_BLL.Core/SystemMT/Persistence/ExecutionEvidence.cs`: add `RuntimeEvidence.ExecutionTraces`.
- Modify `MetBench_BLL.Core/SystemMT/Pipeline/SystemMtPipeline.cs` and `SystemMtExecutionRecorder.cs`: carry Runtime MCP run ids from source/follow-up process results into evidence.
- Create `MetBench_BLL.Core/SystemMT/ControlPlane/SystemMtControlPlaneModels.cs`.
- Create `MetBench_BLL.Core/SystemMT/ControlPlane/ISystemMtControlPlaneService.cs`.
- Create `MetBench_BLL.Core/SystemMT/ControlPlane/SystemMtControlPlaneService.cs`.
- Create `MetBench_BLL.Core/SystemMT/Hosting/SystemMtServiceCollectionExtensions.cs`.
- Create `MetBench_Api/MetBench_Api.csproj`, `MetBench_Api/Program.cs`, `MetBench_Api/SystemMtApiEndpoints.cs`, and `MetBench_Api/SystemMtApiModels.cs`.
- Create `infra/mcp/metbench-business/server.py`, `infra/mcp/metbench-business/README.md`, and `infra/mcp/metbench-business/tests/test_server.py`.
- Modify `MetBench_SystemMT.Tests/SystemMT/Architecture/SystemMtControlPlaneBoundaryTests.cs`: extend source guards to ControlPlane and Business MCP.
- Add focused tests under `MetBench_SystemMT.Tests/SystemMT/Runtime`, `SystemMT/ControlPlane`, `SystemMT/Api`, `SystemMT/Hosting`, and `SystemMT/Pipeline`.

## Task 1: Runtime MCP Structured Tool Boundary

**Files:**
- Modify: `infra/mcp/docker-runtime/server.py`
- Modify: `infra/mcp/docker-runtime/config.example.json`
- Modify: `infra/mcp/docker-runtime/config.local-win.example.json`
- Modify: `infra/mcp/docker-runtime/config.docker-win.example.json`
- Modify: `infra/mcp/docker-runtime/config.local-wsl.example.json`
- Test: `infra/mcp/docker-runtime/tests/test_server.py`
- Modify: `MetBench_BLL.Core/SystemMT/Runtime/RuntimeModels.cs`
- Modify: `MetBench_BLL.Core/SystemMT/Runtime/LauncherOptionsRuntimeProfileProvider.cs`
- Modify: `MetBench_BLL.Core/SystemMT/Runtime/DockerMcpRuntimeClient.cs`
- Modify: `MetBench_BLL.Core/SystemMT/Runtime/DockerMcpProcessExecutor.cs`
- Test: `MetBench_SystemMT.Tests/SystemMT/Runtime/DockerMcpRuntimeClientTests.cs`
- Test: `MetBench_SystemMT.Tests/SystemMT/Runtime/DockerMcpProcessExecutorTests.cs`
- Test: `MetBench_SystemMT.Tests/SystemMT/Runtime/DockerMcpRuntimeProfileTests.cs`

- [ ] **Step 1: Write failing Python tests for raw argv rejection and tool allowlist**

Add these tests to `infra/mcp/docker-runtime/tests/test_server.py`:

```python
def test_load_config_requires_allowed_tools(self):
    payload = self._valid_config_payload()
    payload.pop("allowed_tools")
    with self._config_file(payload) as path:
        with self.assertRaisesRegex(ValueError, "allowed_tools"):
            self.server.load_config(path)

def test_validate_run_request_rejects_raw_argv(self):
    cfg = self._config()
    with self.assertRaisesRegex(ValueError, "raw argv"):
        self.server.validate_run_request(
            cfg,
            {"image": "metbench-sut:latest", "argv": ["python", "sut.py"]},
        )

def test_validate_run_request_allows_allowlisted_tool_args(self):
    cfg = self._config()
    command = self.server.validate_run_request(
        cfg,
        {
            "image": "metbench-sut:latest",
            "tool": "openmoc-runner",
            "args": ["--input", "source.json", "--output", "out.json"],
        },
    )
    self.assertEqual(
        ["/opt/metbench-tools/openmoc-runner", "--input", "source.json", "--output", "out.json"],
        command,
    )

def test_validate_run_request_rejects_unsafe_tool_args(self):
    cfg = self._config()
    cases = [
        ["-c", "id"],
        ["-m", "module"],
        ["../secret.json"],
        ["/etc/passwd"],
        ["runner.py"],
        ["&&"],
        ["$(id)"],
    ]
    for args in cases:
        with self.subTest(args=args):
            with self.assertRaises(ValueError):
                self.server.validate_run_request(
                    cfg,
                    {"image": "metbench-sut:latest", "tool": "openmoc-runner", "args": args},
                )
```

If helper methods are missing, add them in the test class:

```python
def _valid_config_payload(self):
    return {
        "repo_root": "/repo",
        "bind_host": "192.168.1.20",
        "bind_port": 8765,
        "auth_token": "secret",
        "allowed_images": {
            "metbench-sut:latest": {"dockerfile": "docker/sut/Dockerfile", "context": "."}
        },
        "allowed_tools": {
            "openmoc-runner": {"executable": "/opt/metbench-tools/openmoc-runner"}
        },
        "allowed_mount_roots": ["/repo", "/tmp"],
        "default_timeout_seconds": 60,
        "max_output_bytes": 4096,
    }
```

- [ ] **Step 2: Run Python tests to verify RED**

Run:

```bash
rtk python3 -m unittest discover infra/mcp/docker-runtime/tests
```

Expected: FAIL because `allowed_tools` / raw argv rejection / structured tool handling is not implemented on `origin/main`.

- [ ] **Step 3: Implement Python Runtime MCP structured tools**

In `server.py`, add:

```python
@dataclass
class ToolConfig:
    executable: str
```

Extend `RuntimeConfig` with:

```python
allowed_tools: dict[str, ToolConfig]
```

Add loader:

```python
def _load_allowed_tools(payload: dict[str, Any]) -> dict[str, ToolConfig]:
    allowed_tools = payload.get("allowed_tools")
    if not isinstance(allowed_tools, dict) or not allowed_tools:
        raise ValueError("allowed_tools must be a non-empty object")
    result: dict[str, ToolConfig] = {}
    for name, tool in allowed_tools.items():
        if not isinstance(name, str) or not name.strip():
            raise ValueError("allowed_tools keys must be non-blank strings")
        if not isinstance(tool, dict):
            raise ValueError("allowed_tools entries must be objects")
        result[name] = ToolConfig(executable=_required_string(tool, "executable"))
    return result
```

Replace `validate_run_request` with:

```python
def validate_run_request(config: RuntimeConfig, request: dict[str, Any]) -> list[str]:
    image = str(request.get("image", ""))
    if image not in config.allowed_images:
        raise ValueError(f"Image {image!r} is not allowlisted")
    if "argv" in request:
        raise ValueError("raw argv is not accepted; use an allowlisted tool and args")

    tool = str(request.get("tool", ""))
    if tool not in config.allowed_tools:
        raise ValueError(f"Tool {tool!r} is not allowlisted")
    args = request.get("args", [])
    if not isinstance(args, list):
        raise ValueError("args must be a list of non-blank strings")
    for arg in args:
        if not isinstance(arg, str) or not arg.strip():
            raise ValueError("args must contain only non-blank strings")
        _validate_tool_argument(arg)
    return [config.allowed_tools[tool].executable, *args]

def _validate_tool_argument(arg: str) -> None:
    if arg in {"-c", "/c", "-m", "/m"}:
        raise ValueError("tool arguments must not request shell or module execution")
    if arg.endswith((".py", ".pyc")):
        raise ValueError("tool arguments must not contain script path values")
    if arg.startswith("/") or WINDOWS_PATH_PATTERN.match(arg):
        raise ValueError("tool arguments must not contain absolute host paths")
    if ".." in re.split(r"[\\/]", arg):
        raise ValueError("tool arguments must not contain path traversal")
    if arg in {";", "&&", "||", "|"} or "$(" in arg or "`" in arg:
        raise ValueError("tool arguments must not contain shell operators")
```

Update `run_sut_command` to read `tool` and `args`, and update `build_docker_run_command` / `build_local_run_command` to accept `(image, tool, args, timeout_seconds)` and call `validate_run_request`.

- [ ] **Step 4: Run Python tests to verify GREEN**

Run:

```bash
rtk python3 -m unittest discover infra/mcp/docker-runtime/tests
```

Expected: PASS.

- [ ] **Step 5: Write failing C# tests for structured Docker MCP requests**

In `DockerMcpRuntimeClientTests.cs`, add a test that calls:

```csharp
await client.RunSutCommandAsync(
    options,
    new DockerMcpRunRequest(
        Image: "metbench-sut:latest",
        Tool: "openmoc-runner",
        Args: new[] { "--input", "source.json" },
        WorkingDirectory: string.Empty,
        TimeoutSeconds: 60));
```

Assert `handler.LastRequestBody` contains `"tool":"run_sut_command"`, `"image":"metbench-sut:latest"`, `"tool":"openmoc-runner"`, and does not contain `"argv"`.

In `DockerMcpProcessExecutorTests.cs`, add:

```csharp
[Fact]
public async Task RunAsync_rejects_commands_that_do_not_start_with_configured_local_executable()
{
    var executor = new DockerMcpProcessExecutor(new FakeClient());
    var options = new DockerMcpRuntimeOptions(
        "http://127.0.0.1:8765",
        "metbench-sut:latest",
        PythonExecutable: "/opt/metbench-tools/openmoc-runner",
        ToolName: "openmoc-runner",
        LocalExecutable: "/host/openmoc-runner");

    await Assert.ThrowsAsync<ArgumentException>(() =>
        executor.RunAsync(
            options,
            new ProcessInvocation("/bin/sh", new[] { "-c", "id" }),
            timeoutSeconds: 60,
            CancellationToken.None));
}
```

- [ ] **Step 6: Run C# tests to verify RED**

Run:

```bash
rtk dotnet test MetBench_SystemMT.Tests --no-restore --filter "FullyQualifiedName~DockerMcpRuntimeClient|FullyQualifiedName~DockerMcpProcessExecutor|FullyQualifiedName~DockerMcpRuntimeProfile"
```

Expected: FAIL because `DockerMcpRunRequest`, `ToolName`, and `LocalExecutable` are not implemented.

- [ ] **Step 7: Implement C# structured request support**

Change `DockerMcpRuntimeOptions` to:

```csharp
public sealed record DockerMcpRuntimeOptions(
    string Endpoint,
    string Image,
    string PythonExecutable,
    string? AuthTokenEnvironmentVariable = null,
    string? LocalPythonExecutable = null,
    DockerMcpPathStyle PathStyle = DockerMcpPathStyle.None,
    string ToolName = "",
    string LocalExecutable = "");
```

Add:

```csharp
public sealed record DockerMcpRunRequest(
    string Image,
    string Tool,
    IReadOnlyList<string> Args,
    string WorkingDirectory,
    int TimeoutSeconds);
```

Change `DockerMcpRuntimeClient.RunSutCommandAsync` to accept `DockerMcpRunRequest` and serialize `image`, `tool`, `args`, and `timeout_seconds`.

Change `DockerMcpProcessExecutor.RunAsync` to validate:

```csharp
if (!string.Equals(invocation.FileName, options.LocalExecutable, StringComparison.Ordinal))
    throw new ArgumentException($"Docker MCP command must start with configured local executable '{options.LocalExecutable}'.");
foreach (var arg in invocation.Arguments)
    ValidateToolArgument(arg);
```

Build `DockerMcpRunRequest(options.Image, options.ToolName, translatedArgs, "", timeoutSeconds)`.

Parse `tool` and `local` in `LauncherOptionsRuntimeProfileProvider`; if omitted, use `ToolName = Path.GetFileNameWithoutExtension(PythonExecutable)` and `LocalExecutable = LocalPythonExecutable ?? PythonExecutable` only for compatibility tests. New docs/config must pass both explicitly.

- [ ] **Step 8: Run C# focused tests to verify GREEN**

Run:

```bash
rtk dotnet test MetBench_SystemMT.Tests --no-restore --filter "FullyQualifiedName~DockerMcpRuntimeClient|FullyQualifiedName~DockerMcpProcessExecutor|FullyQualifiedName~DockerMcpRuntimeProfile"
```

Expected: PASS.

- [ ] **Step 9: Commit Task 1**

Run:

```bash
rtk git add infra/mcp/docker-runtime MetBench_BLL.Core/SystemMT/Runtime MetBench_SystemMT.Tests/SystemMT/Runtime docs/superpowers/specs/2026-06-12-metbench-docker-runtime-mcp-design.md
rtk git commit -m "fix(t1): harden docker runtime mcp tool boundary"
```

## Task 2: Runtime MCP Run Id Evidence

**Files:**
- Modify: `infra/mcp/docker-runtime/server.py`
- Test: `infra/mcp/docker-runtime/tests/test_server.py`
- Modify: `MetBench_BLL.Core/SystemMT/Runtime/DockerMcpRuntimeClient.cs`
- Modify: `MetBench_BLL.Core/SystemMT/Pipeline/IProcessExecutor.cs`
- Modify: `MetBench_BLL.Core/SystemMT/Pipeline/SystemMtPipeline.cs`
- Modify: `MetBench_BLL.Core/SystemMT/Pipeline/PipelineOutcome.cs`
- Modify: `MetBench_BLL.Core/SystemMT/Persistence/ExecutionEvidence.cs`
- Modify: `MetBench_BLL.Core/SystemMT/Pipeline/SystemMtExecutionRecorder.cs`
- Test: `MetBench_SystemMT.Tests/SystemMT/Runtime/DockerMcpRuntimeClientTests.cs`
- Test: `MetBench_SystemMT.Tests/V2Pipeline/SystemMtPipelineTests.cs`
- Test: `MetBench_SystemMT.Tests/SystemMT/Evidence/ExecutionEvidenceTests.cs`

- [ ] **Step 1: Write failing tests for run id preservation**

In Python tests, assert `run_sut_command` returns `run_id`:

```python
def test_run_sut_command_returns_run_id(self):
    cfg = self._config()
    response = self.server.run_sut_command(
        cfg,
        {"image": "metbench-sut:latest", "tool": "openmoc-runner", "args": ["--version"]},
        runner=lambda command, timeout: self.server.CommandResult(0, "ok", ""),
        id_factory=lambda: "run-123",
    )
    self.assertEqual("run-123", response["run_id"])
```

In `DockerMcpRuntimeClientTests.cs`, return JSON with `"run_id":"run-123"` and assert `result.RunId == "run-123"`.

In pipeline/evidence tests, use a fake `IRuntimeProcessExecutor` returning source/follow-up `ProcessResult` objects with runtime metadata:

```csharp
new ProcessResult(0, "{}", "", TimeSpan.FromMilliseconds(1), false)
{
    RuntimeExecutionId = "source-run",
    RuntimeBackend = "docker-mcp"
}
```

Assert the recorded `ExecutionEvidence.RuntimeEvidence.ExecutionTraces` contains `source-run` and `followup-run`.

- [ ] **Step 2: Run focused tests to verify RED**

Run:

```bash
rtk python3 -m unittest discover infra/mcp/docker-runtime/tests
rtk dotnet test MetBench_SystemMT.Tests --no-restore --filter "FullyQualifiedName~DockerMcpRuntimeClient|FullyQualifiedName~SystemMtPipeline|FullyQualifiedName~ExecutionEvidence"
```

Expected: FAIL because C# drops `run_id` and evidence has no runtime execution id projection.

- [ ] **Step 3: Add runtime execution metadata models**

Extend `DockerMcpRunResult`:

```csharp
public sealed record DockerMcpRunResult(
    int ExitCode,
    string Stdout,
    string Stderr,
    bool TimedOut,
    string RunId = "");
```

Extend `ProcessResult`:

```csharp
public sealed record ProcessResult(
    int ExitCode,
    string Stdout,
    string Stderr,
    TimeSpan Elapsed,
    bool TimedOut)
{
    public string RuntimeBackend { get; init; } = "";
    public string RuntimeExecutionId { get; init; } = "";
}
```

When `DockerMcpProcessExecutor` maps `DockerMcpRunResult`, set `RuntimeBackend = "docker-mcp"` and `RuntimeExecutionId = result.RunId`.

- [ ] **Step 4: Add evidence projection**

Add a compact evidence type:

```csharp
public sealed class RuntimeExecutionTrace
{
    public string Role { get; set; } = string.Empty;
    public string Backend { get; set; } = string.Empty;
    public string ExecutionId { get; set; } = string.Empty;
}
```

Add to `RuntimeEvidence`:

```csharp
public List<RuntimeExecutionTrace> ExecutionTraces { get; set; } = new();
```

Extend `PipelineOutcome` with:

```csharp
public IReadOnlyList<RuntimeExecutionTrace> RuntimeExecutionTraces { get; init; } =
    Array.Empty<RuntimeExecutionTrace>();
```

In `SystemMtPipeline`, collect source/follow-up run metadata:

```csharp
var runtimeTraces = new List<RuntimeExecutionTrace>();
AddTrace(runtimeTraces, "source", rsResult);
AddTrace(runtimeTraces, "followup", rfResult);
```

Use `outcome.RuntimeExecutionTraces` in `SystemMtExecutionRecorder.RecordAsync` to merge traces into `runtimeEvidence.ExecutionTraces`.

- [ ] **Step 5: Run focused tests to verify GREEN**

Run:

```bash
rtk python3 -m unittest discover infra/mcp/docker-runtime/tests
rtk dotnet test MetBench_SystemMT.Tests --no-restore --filter "FullyQualifiedName~DockerMcpRuntimeClient|FullyQualifiedName~SystemMtPipeline|FullyQualifiedName~ExecutionEvidence|FullyQualifiedName~RuntimeEvidence"
```

Expected: PASS.

- [ ] **Step 6: Commit Task 2**

Run:

```bash
rtk git add infra/mcp/docker-runtime MetBench_BLL.Core/SystemMT/Runtime MetBench_BLL.Core/SystemMT/Pipeline MetBench_BLL.Core/SystemMT/Persistence MetBench_SystemMT.Tests
rtk git commit -m "feat(t1): record docker mcp run ids in runtime evidence"
```

## Task 3: Job-Oriented Control Plane

**Files:**
- Create: `MetBench_BLL.Core/SystemMT/ControlPlane/SystemMtControlPlaneModels.cs`
- Create: `MetBench_BLL.Core/SystemMT/ControlPlane/ISystemMtControlPlaneService.cs`
- Create: `MetBench_BLL.Core/SystemMT/ControlPlane/SystemMtControlPlaneService.cs`
- Create: `MetBench_BLL.Core/SystemMT/Hosting/SystemMtServiceCollectionExtensions.cs`
- Test: `MetBench_SystemMT.Tests/SystemMT/ControlPlane/SystemMtControlPlaneServiceTests.cs`
- Test: `MetBench_SystemMT.Tests/SystemMT/Hosting/SystemMtServiceCollectionExtensionsTests.cs`
- Modify: `MetBench_SystemMT.Tests/SystemMT/Architecture/SystemMtControlPlaneBoundaryTests.cs`

- [ ] **Step 1: Write failing control-plane tests**

Create tests for:

```csharp
[Fact]
public async Task SubmitRun_rejects_blank_mr_id_before_enqueue()

[Fact]
public async Task SubmitBatch_rejects_duplicate_mr_ids_before_enqueue()

[Fact]
public async Task SubmitRun_rejects_blank_parameter_override_key_or_value()

[Fact]
public async Task ListArtifacts_uses_job_artifact_path_internally()

[Fact]
public async Task ListArtifacts_rejects_non_terminal_jobs()
```

The fake job service should record received `SystemMtOperationJobRequest`. The fake artifact service should expose `LastManifestPath` and return one descriptor. The test must call `ListArtifactsAsync(jobId)`, not `ListArtifactsAsync(manifestPath)`.

- [ ] **Step 2: Run control-plane tests to verify RED**

Run:

```bash
rtk dotnet test MetBench_SystemMT.Tests --no-restore --filter "FullyQualifiedName~SystemMtControlPlaneService|FullyQualifiedName~SystemMtServiceCollectionExtensions|FullyQualifiedName~SystemMtControlPlaneBoundary"
```

Expected: FAIL because control-plane types do not exist.

- [ ] **Step 3: Implement control-plane models and service**

Create request/response records:

```csharp
public sealed record SubmitSystemMtRunRequest(
    string MrId,
    IReadOnlyDictionary<string, string>? ParameterOverrides = null);

public sealed record SubmitSystemMtBatchRequest(
    IReadOnlyList<string> MrIds,
    IReadOnlyDictionary<string, string>? ParameterOverrides = null);

public sealed record SystemMtJobArtifactList(Guid JobId, IReadOnlyList<SystemMtArtifactDescriptor> Artifacts);
```

Create interface:

```csharp
public interface ISystemMtControlPlaneService
{
    Task<IReadOnlyList<MrSummary>> ListMrsAsync(CancellationToken cancellationToken = default);
    Task<SystemMtJobHandle> SubmitRunAsync(SubmitSystemMtRunRequest request, CancellationToken cancellationToken = default);
    Task<SystemMtJobHandle> SubmitBatchAsync(SubmitSystemMtBatchRequest request, CancellationToken cancellationToken = default);
    Task<SystemMtJobStatus?> GetJobAsync(Guid jobId, CancellationToken cancellationToken = default);
    Task CancelJobAsync(Guid jobId, CancellationToken cancellationToken = default);
    Task<MrRunResult?> GetResultAsync(Guid jobId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SystemMtArtifactDescriptor>> ListArtifactsAsync(Guid jobId, CancellationToken cancellationToken = default);
    Task<SystemMtArtifactContent> GetArtifactAsync(Guid jobId, string artifactId, CancellationToken cancellationToken = default);
}
```

Implementation rules:

```csharp
private static void ValidateOverrides(IReadOnlyDictionary<string, string>? overrides)
{
    if (overrides is null) return;
    foreach (var (key, value) in overrides)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Parameter override keys must be non-blank.");
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"Parameter override '{key}' must be non-blank.");
    }
}
```

`SubmitRunAsync` calls:

```csharp
_jobService.SubmitOperationAsync(
    new SystemMtOperationJobRequest(SystemMtJobKind.RunMr) {
        MrId = request.MrId,
        ParameterOverrides = request.ParameterOverrides
    },
    cancellationToken);
```

`SubmitBatchAsync` calls `RunBatch` with `MrIds`. Artifact methods load job status, require terminal state, require non-blank `ArtifactPath`, and pass that manifest path to `ISystemMtArtifactAccessService` internally.

- [ ] **Step 4: Implement DI extension**

Add `SystemMtServiceCollectionExtensions.AddSystemMtControlPlane(...)` that registers:

```csharp
services.AddSingleton<ISystemMtArtifactAccessService>(
    _ => new SystemMtArtifactAccessService(options.ArtifactRoots));
services.AddSingleton<ISystemMtControlPlaneService, SystemMtControlPlaneService>();
```

Keep full production `AddSystemMtServices(...)` minimal in this task: it may throw a clear `InvalidOperationException` if required options are missing, but tests must prove the control-plane dependency contract can be built with fakes.

- [ ] **Step 5: Extend architecture guard**

Update `SystemMtControlPlaneBoundaryTests` so raw path terms are scanned in:

```csharp
Path.Combine(root, "MetBench_BLL.Core", "SystemMT", "ControlPlane")
```

Allow internal implementation terms only if the file name is `SystemMtControlPlaneService.cs` and the member is private artifact resolution; public DTO files must not contain `PackageRoot`, `StagingRoot`, `ExportRoot`, `ArtifactPath`, or `ManifestPath`.

- [ ] **Step 6: Run control-plane tests to verify GREEN**

Run:

```bash
rtk dotnet test MetBench_SystemMT.Tests --no-restore --filter "FullyQualifiedName~SystemMtControlPlaneService|FullyQualifiedName~SystemMtServiceCollectionExtensions|FullyQualifiedName~SystemMtControlPlaneBoundary"
```

Expected: PASS.

- [ ] **Step 7: Commit Task 3**

Run:

```bash
rtk git add MetBench_BLL.Core/SystemMT/ControlPlane MetBench_BLL.Core/SystemMT/Hosting MetBench_SystemMT.Tests/SystemMT/ControlPlane MetBench_SystemMT.Tests/SystemMT/Hosting MetBench_SystemMT.Tests/SystemMT/Architecture/SystemMtControlPlaneBoundaryTests.cs
rtk git commit -m "feat(t0): add job-oriented system mt control plane"
```

## Task 4: REST API and Business MCP Adapters

**Files:**
- Create: `MetBench_Api/MetBench_Api.csproj`
- Create: `MetBench_Api/Program.cs`
- Create: `MetBench_Api/SystemMtApiEndpoints.cs`
- Create: `MetBench_Api/SystemMtApiModels.cs`
- Test: `MetBench_SystemMT.Tests/SystemMT/Api/SystemMtApiEndpointTests.cs`
- Create: `infra/mcp/metbench-business/server.py`
- Create: `infra/mcp/metbench-business/README.md`
- Test: `infra/mcp/metbench-business/tests/test_server.py`
- Modify: `MetBench_SystemMT.Tests/SystemMT/Architecture/SystemMtControlPlaneBoundaryTests.cs`
- Modify: solution file if needed: `MetBench.sln`

- [ ] **Step 1: Write failing REST API tests**

Create endpoint tests using `Microsoft.AspNetCore.Mvc.Testing` or a minimal `WebApplicationFactory` style helper. Tests must prove:

```csharp
[Fact]
public async Task Health_is_public()

[Fact]
public async Task ListMrs_requires_bearer_token()

[Fact]
public async Task SubmitRun_calls_control_plane_and_returns_accepted_job()

[Fact]
public async Task Artifact_routes_use_job_id_and_artifact_id_not_manifest_path()
```

Test request DTOs must be:

```json
{ "mrId": "advection-amplitude-linearity", "parameterOverrides": { "factor": "2" } }
```

Do not include `PackageRoot`, `StagingRoot`, `ExportRoot`, `ArtifactPath`, or `manifestPath` in any public model.

- [ ] **Step 2: Run API tests to verify RED**

Run:

```bash
rtk dotnet test MetBench_SystemMT.Tests --no-restore --filter "FullyQualifiedName~SystemMtApiEndpoint"
```

Expected: FAIL because `MetBench_Api` does not exist.

- [ ] **Step 3: Implement Minimal API adapter**

Create `MetBench_Api.csproj` with:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\MetBench_BLL.Core\MetBench_BLL.Core.csproj" />
  </ItemGroup>
</Project>
```

Endpoints:

```csharp
group.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();
group.MapGet("/mrs", async (ISystemMtControlPlaneService svc, CancellationToken ct) => Results.Ok(await svc.ListMrsAsync(ct)));
group.MapPost("/runs", async (SystemMtRunRequestDto dto, ISystemMtControlPlaneService svc, CancellationToken ct) =>
{
    var handle = await svc.SubmitRunAsync(new SubmitSystemMtRunRequest(dto.MrId, dto.ParameterOverrides), ct);
    return Results.Accepted($"/api/v1/jobs/{handle.JobId}", handle);
});
```

Add bearer token middleware for non-health routes. Token source: config key `MetBenchApi:BearerToken` or env `METBENCH_API_TOKEN`. Missing token in production mode must fail closed.

- [ ] **Step 4: Write failing Business MCP tests**

Python tests must assert:

```python
def test_server_exposes_only_business_tools(self):
    self.assertEqual(
        {
            "list_mrs",
            "submit_run",
            "submit_batch",
            "get_job",
            "cancel_job",
            "get_result",
            "list_artifacts",
            "get_artifact",
        },
        set(server.TOOLS),
    )
    self.assertNotIn("run_sut_command", server.TOOLS)

def test_submit_run_posts_to_rest_api(self):
    fake = FakeHttp()
    response = server.dispatch_tool(
        server.BusinessMcpConfig("http://localhost:5000", "token"),
        "submit_run",
        {"mr_id": "advection-amplitude-linearity", "parameter_overrides": {"factor": "2"}},
        http=fake,
    )
    self.assertEqual("/api/v1/runs", fake.last_path)
```

- [ ] **Step 5: Run Business MCP tests to verify RED**

Run:

```bash
rtk python3 -m unittest discover infra/mcp/metbench-business/tests
```

Expected: FAIL because Business MCP server does not exist.

- [ ] **Step 6: Implement Business MCP adapter**

Implement Python stdlib server with:

```python
TOOLS = {
    "list_mrs",
    "submit_run",
    "submit_batch",
    "get_job",
    "cancel_job",
    "get_result",
    "list_artifacts",
    "get_artifact",
}
```

Dispatch maps tool names to REST paths:

```python
PATHS = {
    "list_mrs": ("GET", "/api/v1/mrs"),
    "submit_run": ("POST", "/api/v1/runs"),
    "submit_batch": ("POST", "/api/v1/batches"),
}
```

Reject any payload containing forbidden keys:

```python
FORBIDDEN_KEYS = {"packageRoot", "stagingRoot", "exportRoot", "artifactPath", "manifestPath", "argv"}
```

- [ ] **Step 7: Extend architecture guards for REST and Business MCP**

Guard roots:

```csharp
Path.Combine(root, "MetBench_Api"),
Path.Combine(root, "MetBench_BLL.Core", "SystemMT", "ControlPlane"),
Path.Combine(root, "infra", "mcp", "metbench-business")
```

Runtime MCP implementation terms remain forbidden in API/Business MCP:

```csharp
"DockerMcpRuntimeClient", "DockerMcpProcessExecutor", "DockerRuntimeProcessExecutor", "run_sut_command"
```

- [ ] **Step 8: Run focused adapter tests to verify GREEN**

Run:

```bash
rtk dotnet test MetBench_SystemMT.Tests --no-restore --filter "FullyQualifiedName~SystemMtApiEndpoint|FullyQualifiedName~SystemMtControlPlaneBoundary"
rtk python3 -m unittest discover infra/mcp/metbench-business/tests
```

Expected: PASS.

- [ ] **Step 9: Commit Task 4**

Run:

```bash
rtk git add MetBench_Api infra/mcp/metbench-business MetBench_SystemMT.Tests/SystemMT/Api MetBench_SystemMT.Tests/SystemMT/Architecture/SystemMtControlPlaneBoundaryTests.cs MetBench.sln
rtk git commit -m "feat(api): add system mt rest and business mcp adapters"
```

## Final Verification

- [ ] Run Runtime MCP Python tests:

```bash
rtk python3 -m unittest discover infra/mcp/docker-runtime/tests
```

- [ ] Run Business MCP Python tests:

```bash
rtk python3 -m unittest discover infra/mcp/metbench-business/tests
```

- [ ] Run focused .NET tests:

```bash
rtk dotnet test MetBench_SystemMT.Tests --no-restore --filter "FullyQualifiedName~DockerMcp|FullyQualifiedName~RuntimeEvidence|FullyQualifiedName~ControlPlane|FullyQualifiedName~Api|FullyQualifiedName~Artifact|FullyQualifiedName~SystemMtControlPlaneBoundary"
```

- [ ] Build core:

```bash
rtk dotnet build MetBench_BLL.Core/MetBench_BLL.Core.csproj --no-restore
```

- [ ] Check whitespace:

```bash
rtk git diff --check
```

## Windows Classification

This chain is cloud-safe if it stays in `MetBench_BLL.Core/`, `MetBench_SystemMT.Tests/`, `MetBench_Api/`, `infra/mcp/`, and docs. No WPF files are in scope. If WPF registration or navigation is touched, stop and create a separate Windows VM plan.

## Subagent Execution Protocol

Use one worker subagent per task. The controller gives the subagent the full text of one task, the worktree path, and the rule that production code must follow TDD. After each worker reports back:

1. Inspect `rtk git status --short`.
2. Run the task's focused verification command.
3. Dispatch a spec-compliance review subagent against that task's requirements.
4. Dispatch a code-quality review subagent only after spec compliance is accepted.
5. Fix Critical and Important review findings before the next task.

No task may begin until the previous task's focused tests and reviews are complete.
