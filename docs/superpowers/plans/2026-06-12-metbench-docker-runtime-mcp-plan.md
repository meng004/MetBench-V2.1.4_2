# MetBench Docker Runtime MCP Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a LAN-accessible Docker Runtime MCP server and activate MetBench's Docker runtime backend through existing System MT runtime governance.

**Architecture:** Phase A creates a Python stdlib MCP/HTTP service that exposes only allowlisted MetBench Docker runtime operations. Phase B adds Docker runtime profile parsing, Docker preflight, and a Docker MCP-backed `IProcessExecutor` while preserving the current launcher, async job, pipeline, and evidence flow.

**Tech Stack:** Python 3 stdlib (`http.server`, `subprocess`, `json`, `unittest`), .NET 8 C#, xUnit, existing MetBench System MT runtime and pipeline abstractions.

---

## File Structure

Phase A files:

- Create `infra/mcp/docker-runtime/server.py`: HTTP JSON-RPC-like MCP-compatible service with tool dispatch, auth, LAN bind resolution, validation, Docker command execution, and run result storage.
- Create `infra/mcp/docker-runtime/config.example.json`: safe default service configuration.
- Create `infra/mcp/docker-runtime/README.md`: startup, LAN client configuration, tools, and security model.
- Create `infra/mcp/docker-runtime/tests/test_server.py`: Python unit tests for config loading, auth, LAN bind resolution, command validation, and Docker command construction.

Phase B files:

- Modify `MetBench_BLL.Core/SystemMT/Runtime/RuntimeModels.cs`: add executable Docker runtime kind and Docker MCP metadata record.
- Modify `MetBench_BLL.Core/SystemMT/Runtime/LauncherOptionsRuntimeProfileProvider.cs`: parse `docker-mcp://...` values from `LauncherOptions.RuntimePythons`.
- Modify `MetBench_BLL.Core/SystemMT/Runtime/RuntimePreflightService.cs`: route Docker profiles through Docker MCP preflight instead of placeholder failure.
- Create `MetBench_BLL.Core/SystemMT/Runtime/DockerMcpRuntimeClient.cs`: small HTTP JSON client abstraction for MCP calls.
- Create `MetBench_BLL.Core/SystemMT/Pipeline/DockerMcpProcessExecutor.cs`: `IProcessExecutor` implementation that runs commands through Docker MCP.
- Modify `MetBench_BLL.Core/SystemMT/Pipeline/SystemMtPipeline.cs`: accept the selected process executor from launcher/runtime wiring if required.
- Modify `MetBench_BLL.Core/SystemMT/Launcher/SystemMtLauncher.cs`: select Docker MCP executor for Docker runtime profiles while keeping local executor unchanged.
- Add tests in `MetBench_SystemMT.Tests/SystemMT/Runtime/DockerMcpRuntimeProfileTests.cs`.
- Add tests in `MetBench_SystemMT.Tests/SystemMT/Runtime/DockerMcpPreflightTests.cs`.
- Add tests in `MetBench_SystemMT.Tests/V2Pipeline/DockerMcpProcessExecutorTests.cs`.
- Add/extend launcher tests in `MetBench_SystemMT.Tests/SystemMT/Launcher/RuntimePreflightLauncherTests.cs`.

## Task 1: Phase A MCP Server Validation Core

**Files:**
- Create: `infra/mcp/docker-runtime/server.py`
- Create: `infra/mcp/docker-runtime/config.example.json`
- Create: `infra/mcp/docker-runtime/tests/test_server.py`

- [ ] **Step 1: Write failing Python tests for LAN bind and validation**

Add `infra/mcp/docker-runtime/tests/test_server.py`:

```python
import json
import tempfile
import unittest
from pathlib import Path

import server


class DockerRuntimeServerTests(unittest.TestCase):
    def test_choose_bind_host_prefers_private_ipv4(self):
        host = server.choose_bind_host(["127.0.0.1", "8.8.8.8", "192.168.1.20"])
        self.assertEqual(host, "192.168.1.20")

    def test_choose_bind_host_fails_without_private_ipv4(self):
        with self.assertRaisesRegex(ValueError, "private LAN IPv4"):
            server.choose_bind_host(["127.0.0.1", "8.8.8.8"])

    def test_config_rejects_blank_token(self):
        payload = {
            "bind_host": "192.168.1.20",
            "bind_port": 8765,
            "auth_token": "",
            "repo_root": "/repo",
            "allowed_images": {},
            "allowed_mount_roots": ["/repo"],
        }
        with tempfile.TemporaryDirectory() as tmp:
            path = Path(tmp) / "config.json"
            path.write_text(json.dumps(payload), encoding="utf-8")
            with self.assertRaisesRegex(ValueError, "auth_token"):
                server.load_config(path)

    def test_run_request_rejects_disallowed_image(self):
        cfg = server.RuntimeConfig(
            bind_host="192.168.1.20",
            bind_port=8765,
            auth_token="secret",
            repo_root="/repo",
            allowed_images={"metbench-sut:latest": server.ImageConfig("docker/Dockerfile", "docker")},
            allowed_mount_roots=["/repo", "/tmp"],
            default_timeout_seconds=120,
            max_output_bytes=4096,
        )
        with self.assertRaisesRegex(ValueError, "not allowlisted"):
            server.validate_run_request(cfg, {"image": "ubuntu:latest", "argv": ["python", "--version"]})

    def test_run_request_rejects_raw_docker_flags(self):
        cfg = server.RuntimeConfig(
            bind_host="192.168.1.20",
            bind_port=8765,
            auth_token="secret",
            repo_root="/repo",
            allowed_images={"metbench-sut:latest": server.ImageConfig("docker/Dockerfile", "docker")},
            allowed_mount_roots=["/repo", "/tmp"],
            default_timeout_seconds=120,
            max_output_bytes=4096,
        )
        with self.assertRaisesRegex(ValueError, "raw Docker flag"):
            server.validate_run_request(cfg, {"image": "metbench-sut:latest", "argv": ["--privileged"]})


if __name__ == "__main__":
    unittest.main()
```

- [ ] **Step 2: Run test to verify RED**

Run:

```bash
rtk python3 -m unittest discover infra/mcp/docker-runtime/tests
```

Expected: FAIL because `server` module and validation functions do not exist.

- [ ] **Step 3: Implement minimal validation core**

Add `server.py` dataclasses and pure functions:

```python
from __future__ import annotations

from dataclasses import dataclass
import ipaddress
import json
import socket
from pathlib import Path
from typing import Iterable


@dataclass(frozen=True)
class ImageConfig:
    dockerfile: str
    context: str


@dataclass(frozen=True)
class RuntimeConfig:
    bind_host: str
    bind_port: int
    auth_token: str
    repo_root: str
    allowed_images: dict[str, ImageConfig]
    allowed_mount_roots: list[str]
    default_timeout_seconds: int
    max_output_bytes: int


def choose_bind_host(candidates: Iterable[str] | None = None) -> str:
    addresses = list(candidates) if candidates is not None else _local_ipv4_addresses()
    for address in addresses:
        ip = ipaddress.ip_address(address)
        if ip.version == 4 and ip.is_private and not ip.is_loopback:
            return address
    raise ValueError("No private LAN IPv4 address found; configure bind_host explicitly.")


def load_config(path: str | Path) -> RuntimeConfig:
    payload = json.loads(Path(path).read_text(encoding="utf-8"))
    token = str(payload.get("auth_token", "")).strip()
    if not token:
        raise ValueError("auth_token is required.")
    bind_host = str(payload.get("bind_host", "auto-private-ipv4"))
    if bind_host == "auto-private-ipv4":
        bind_host = choose_bind_host()
    images = {
        name: ImageConfig(str(value["dockerfile"]), str(value["context"]))
        for name, value in payload.get("allowed_images", {}).items()
    }
    return RuntimeConfig(
        bind_host=bind_host,
        bind_port=int(payload.get("bind_port", 8765)),
        auth_token=token,
        repo_root=str(payload["repo_root"]),
        allowed_images=images,
        allowed_mount_roots=[str(root) for root in payload.get("allowed_mount_roots", [])],
        default_timeout_seconds=int(payload.get("default_timeout_seconds", 120)),
        max_output_bytes=int(payload.get("max_output_bytes", 65536)),
    )


def validate_run_request(config: RuntimeConfig, request: dict) -> list[str]:
    image = str(request.get("image", "")).strip()
    if image not in config.allowed_images:
        raise ValueError(f"Image '{image}' is not allowlisted.")
    argv = request.get("argv")
    if not isinstance(argv, list) or not argv or not all(isinstance(item, str) and item for item in argv):
        raise ValueError("argv must be a non-empty list of strings.")
    for item in argv:
        if item.startswith("--"):
            raise ValueError("raw Docker flag arguments are not allowed.")
    return argv


def _local_ipv4_addresses() -> list[str]:
    addresses: set[str] = set()
    hostname = socket.gethostname()
    for info in socket.getaddrinfo(hostname, None, socket.AF_INET):
        addresses.add(info[4][0])
    return sorted(addresses)
```

Create `config.example.json` with the config from the design spec, using the worktree absolute repo root.

- [ ] **Step 4: Run Python tests to verify GREEN**

Run:

```bash
rtk python3 -m unittest discover infra/mcp/docker-runtime/tests
```

Expected: PASS.

## Task 2: Phase A MCP Tool Dispatch and Docker Command Builder

**Files:**
- Modify: `infra/mcp/docker-runtime/server.py`
- Modify: `infra/mcp/docker-runtime/tests/test_server.py`
- Create: `infra/mcp/docker-runtime/README.md`

- [ ] **Step 1: Write failing tests for auth, tool dispatch, and command construction**

Extend `test_server.py` with tests for:

```python
    def test_build_docker_run_command_uses_allowlisted_image_and_repo_mount(self):
        cfg = self._config()
        command = server.build_docker_run_command(
            cfg,
            image="metbench-sut:latest",
            argv=["/opt/openmoc-venv/bin/python", "--version"],
            timeout_seconds=30,
        )
        self.assertIn("docker", command)
        self.assertIn("run", command)
        self.assertIn("--rm", command)
        self.assertIn("metbench-sut:latest", command)
        self.assertIn("/repo:/repo", command)
        self.assertNotIn("--privileged", command)

    def test_authorize_rejects_wrong_bearer_token(self):
        with self.assertRaisesRegex(PermissionError, "Unauthorized"):
            server.authorize("Bearer wrong", "secret")

    def test_authorize_accepts_matching_bearer_token(self):
        server.authorize("Bearer secret", "secret")
```

Add a `_config()` helper in the test class returning the same `RuntimeConfig`.

- [ ] **Step 2: Run test to verify RED**

Run:

```bash
rtk python3 -m unittest discover infra/mcp/docker-runtime/tests
```

Expected: FAIL because `build_docker_run_command` and `authorize` do not exist.

- [ ] **Step 3: Implement command builder and auth**

Implement:

```python
import subprocess
import uuid


def authorize(header: str | None, expected_token: str) -> None:
    if header != f"Bearer {expected_token}":
        raise PermissionError("Unauthorized MCP request.")


def build_docker_run_command(
    config: RuntimeConfig,
    image: str,
    argv: list[str],
    timeout_seconds: int | None = None,
) -> list[str]:
    if image not in config.allowed_images:
        raise ValueError(f"Image '{image}' is not allowlisted.")
    timeout = timeout_seconds or config.default_timeout_seconds
    if timeout <= 0:
        raise ValueError("timeout_seconds must be positive.")
    return [
        "docker",
        "run",
        "--rm",
        "-v",
        f"{config.repo_root}:{config.repo_root}",
        "-v",
        "/tmp:/tmp",
        "-w",
        config.repo_root,
        image,
        *argv,
    ]
```

Then add minimal JSON HTTP handler with methods for `runtime_health`, `list_runtime_images`, `build_runtime_image`, `run_sut_command`, and `get_run_result`. Use `subprocess.run(..., timeout=...)`; store run records in memory keyed by `uuid.uuid4().hex`.

- [ ] **Step 4: Run Python tests to verify GREEN**

Run:

```bash
rtk python3 -m unittest discover infra/mcp/docker-runtime/tests
```

Expected: PASS.

- [ ] **Step 5: Add README**

Document:

```bash
rtk python3 infra/mcp/docker-runtime/server.py --config infra/mcp/docker-runtime/config.example.json
```

Include a LAN client example with `Authorization: Bearer <token>`.

## Task 3: Phase B Docker Runtime Profile Parsing

**Files:**
- Modify: `MetBench_BLL.Core/SystemMT/Runtime/RuntimeModels.cs`
- Modify: `MetBench_BLL.Core/SystemMT/Runtime/LauncherOptionsRuntimeProfileProvider.cs`
- Create: `MetBench_SystemMT.Tests/SystemMT/Runtime/DockerMcpRuntimeProfileTests.cs`

- [ ] **Step 1: Write failing xUnit tests**

Create `DockerMcpRuntimeProfileTests.cs` with tests:

```csharp
using MetBench_BLL.SystemMT.Launcher;
using MetBench_BLL.SystemMT.Runtime;

namespace MetBench_SystemMT.Tests.SystemMT.Runtime;

public sealed class DockerMcpRuntimeProfileTests
{
    [Fact]
    public void Provider_parses_docker_mcp_runtime_value()
    {
        var options = new LauncherOptions(
            SutRoot: "/repo/SUT",
            SystemPython: "python3",
            OpenMocPython: "python3",
            RuntimePythons: new Dictionary<string, string>
            {
                ["openmoc-docker"] = "docker-mcp://openmoc-docker?image=metbench-sut:latest&python=/opt/openmoc-venv/bin/python&endpoint=http://192.168.1.20:8765"
            });

        var profile = new LauncherOptionsRuntimeProfileProvider(options).GetProfile("openmoc-docker");

        Assert.Equal(RuntimeKind.Docker, profile.Kind);
        Assert.NotNull(profile.DockerMcp);
        Assert.Equal("metbench-sut:latest", profile.DockerMcp!.Image);
        Assert.Equal("/opt/openmoc-venv/bin/python", profile.DockerMcp.PythonExecutable);
        Assert.Equal("http://192.168.1.20:8765", profile.DockerMcp.Endpoint);
    }

    [Fact]
    public void Local_python_profile_behavior_is_unchanged()
    {
        var options = new LauncherOptions("/repo/SUT", "python3", "python3");
        var profile = new LauncherOptionsRuntimeProfileProvider(options).GetProfile("system");

        Assert.Equal(RuntimeKind.LocalPython, profile.Kind);
        Assert.Null(profile.DockerMcp);
        Assert.Equal("python3", profile.ExecutablePath);
    }
}
```

- [ ] **Step 2: Run test to verify RED**

Run:

```bash
rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~DockerMcpRuntimeProfileTests"
```

Expected: FAIL because `RuntimeKind.Docker` and `RuntimeProfile.DockerMcp` do not exist.

- [ ] **Step 3: Implement Docker MCP metadata**

Add `RuntimeKind.Docker` and record:

```csharp
public sealed record DockerMcpRuntimeOptions(
    string Endpoint,
    string Image,
    string PythonExecutable,
    string? AuthTokenEnvironmentVariable = null);
```

Add nullable `DockerMcpRuntimeOptions? dockerMcp = null` to `RuntimeProfile` constructor and property. Update `IsExecutableInV1` to include `RuntimeKind.Docker`.

Parse `docker-mcp://` values in `LauncherOptionsRuntimeProfileProvider.GetProfile`. Reject malformed URI with `RuntimeEnvironmentResolutionException`.

- [ ] **Step 4: Run tests to verify GREEN**

Run:

```bash
rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~DockerMcpRuntimeProfileTests"
```

Expected: PASS.

## Task 4: Phase B Docker MCP Client and Preflight

**Files:**
- Create: `MetBench_BLL.Core/SystemMT/Runtime/DockerMcpRuntimeClient.cs`
- Modify: `MetBench_BLL.Core/SystemMT/Runtime/RuntimePreflightService.cs`
- Create: `MetBench_SystemMT.Tests/SystemMT/Runtime/DockerMcpPreflightTests.cs`

- [ ] **Step 1: Write failing preflight tests**

Create tests using a fake `IDockerMcpRuntimeClient`:

```csharp
public sealed class DockerMcpPreflightTests
{
    [Fact]
    public async Task Docker_profile_preflight_calls_mcp_health_and_passes()
    {
        var client = FakeDockerMcpRuntimeClient.Healthy();
        var service = new RuntimePreflightService(new RecordingProcessExecutor(), client);
        var profile = DockerProfile();

        var result = await service.CheckAsync(profile);

        Assert.True(result.Passed);
        Assert.Contains(result.Diagnostics, d => d.CheckKind == "middleware" && d.Passed);
    }

    [Fact]
    public async Task Docker_profile_preflight_blocks_when_mcp_unavailable()
    {
        var client = FakeDockerMcpRuntimeClient.Unavailable("connection refused");
        var service = new RuntimePreflightService(new RecordingProcessExecutor(), client);
        var result = await service.CheckAsync(DockerProfile());

        Assert.False(result.Passed);
        Assert.Equal(RuntimeFailureKind.MiddlewareUnavailable, result.FailureKind);
        Assert.Contains("connection refused", result.Detail);
    }
}
```

- [ ] **Step 2: Run test to verify RED**

Run:

```bash
rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~DockerMcpPreflightTests"
```

Expected: FAIL because Docker MCP client abstraction and preflight overload do not exist.

- [ ] **Step 3: Implement client abstraction and preflight route**

Create `IDockerMcpRuntimeClient` with:

```csharp
Task<DockerMcpHealthResult> CheckHealthAsync(DockerMcpRuntimeOptions options, CancellationToken cancellationToken);
Task<ProcessResult> RunAsync(DockerMcpRuntimeOptions options, string command, string workingDirectory, int timeoutSeconds, CancellationToken cancellationToken);
```

Implement `DockerMcpRuntimeClient` using `HttpClient` and JSON payloads.

Modify `RuntimePreflightService` constructor to accept optional `IDockerMcpRuntimeClient`. For Docker profiles:

- Require `profile.DockerMcp`.
- Call health.
- Return `RuntimePreflightResult.Pass` if health is healthy.
- Return `MiddlewareUnavailable` if health fails.

Keep non-Docker behavior byte-for-byte equivalent where possible.

- [ ] **Step 4: Run focused tests**

Run:

```bash
rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~DockerMcpPreflightTests|FullyQualifiedName~RuntimePreflightServiceTests"
```

Expected: PASS.

## Task 5: Phase B Docker MCP Process Executor

**Files:**
- Create: `MetBench_BLL.Core/SystemMT/Pipeline/DockerMcpProcessExecutor.cs`
- Create: `MetBench_SystemMT.Tests/V2Pipeline/DockerMcpProcessExecutorTests.cs`

- [ ] **Step 1: Write failing executor tests**

Create tests proving:

- local command containing the configured local executable is mapped to in-container Python.
- timeout and exit code are returned from MCP unchanged.
- missing Docker metadata throws `InvalidOperationException`.

Example:

```csharp
[Fact]
public async Task RunAsync_maps_configured_python_to_container_python()
{
    var client = new RecordingDockerMcpRuntimeClient(new ProcessResult(0, "ok", "", false, TimeSpan.FromMilliseconds(1)));
    var profile = DockerProfile();
    var executor = new DockerMcpProcessExecutor(profile, client);

    var result = await executor.RunAsync("\"/host/openmoc-wrapper\" --version", "/repo", 10, CancellationToken.None);

    Assert.Equal(0, result.ExitCode);
    Assert.Contains("/opt/openmoc-venv/bin/python", client.LastCommand);
}
```

- [ ] **Step 2: Run test to verify RED**

Run:

```bash
rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~DockerMcpProcessExecutorTests"
```

Expected: FAIL because `DockerMcpProcessExecutor` does not exist.

- [ ] **Step 3: Implement executor**

Implement `IProcessExecutor`. Constructor takes `RuntimeProfile profile` and `IDockerMcpRuntimeClient client`. `RunAsync` delegates to `client.RunAsync(profile.DockerMcp, mappedCommand, workingDirectory, timeoutSeconds, cancellationToken)`.

Mapping rule: replace the first quoted executable segment in the command with `profile.DockerMcp.PythonExecutable`. Keep arguments unchanged.

- [ ] **Step 4: Run focused tests**

Run:

```bash
rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~DockerMcpProcessExecutorTests"
```

Expected: PASS.

## Task 6: Phase B Launcher Wiring Through Existing Path

**Files:**
- Modify: `MetBench_BLL.Core/SystemMT/Launcher/SystemMtLauncher.cs`
- Modify if necessary: `MetBench_BLL.Core/SystemMT/Pipeline/SystemMtPipeline.cs`
- Modify: `MetBench_SystemMT.Tests/SystemMT/Launcher/RuntimePreflightLauncherTests.cs`
- Modify: `MetBench_SystemMT.Tests/SystemMT/Jobs/RuntimePreflightAsyncJobTests.cs` if async coverage needs explicit Docker case.

- [ ] **Step 1: Write failing launcher-path test**

Add a test proving a Docker runtime MR reaches preflight and pipeline without `RuntimeKind.DockerPlaceholder` failure. Use fake catalog/provider and fake Docker MCP client. Assert runtime evidence kind is `Docker`.

- [ ] **Step 2: Run test to verify RED**

Run:

```bash
rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~RuntimePreflightLauncherTests"
```

Expected: FAIL because launcher still builds local-only pipeline/executor.

- [ ] **Step 3: Implement minimal launcher wiring**

Add optional `IDockerMcpRuntimeClient` constructor dependency to `SystemMtLauncher`. When `CreateRuntimeProfile(blueprint).Kind == RuntimeKind.Docker`, construct pipeline execution with `DockerMcpProcessExecutor`; otherwise keep `DefaultProcessExecutor`.

If `SystemMtPipeline` currently stores its executor per instance, prefer creating a per-run `SystemMtPipeline(new DockerMcpProcessExecutor(...))` only for Docker, leaving existing injected `_pipeline` for local profiles.

- [ ] **Step 4: Run launcher and async focused tests**

Run:

```bash
rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~RuntimePreflightLauncherTests|FullyQualifiedName~RuntimePreflightAsyncJobTests|FullyQualifiedName~SystemMtAsyncPipelineTests"
```

Expected: PASS.

## Task 7: Documentation and End-to-End Smoke

**Files:**
- Modify: `infra/mcp/docker-runtime/README.md`
- Modify: `docs/status/current.md` only if implementation changes current status meaning.
- Modify: `docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md` only if this plan must be registered as active/completed under project governance.

- [ ] **Step 1: Update README with exact LAN startup and Codex client configuration**

Include:

```bash
rtk python3 infra/mcp/docker-runtime/server.py --config infra/mcp/docker-runtime/config.example.json
```

Include curl-style JSON examples for each tool.

- [ ] **Step 2: Run final verification**

Run:

```bash
rtk python3 -m unittest discover infra/mcp/docker-runtime/tests
rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~RuntimeProfile|FullyQualifiedName~RuntimePreflight|FullyQualifiedName~DockerMcp|FullyQualifiedName~SystemMtAsyncPipeline"
rtk git diff --check
```

Expected: all commands exit 0.

- [ ] **Step 3: Optional Docker smoke**

If Docker image exists:

```bash
rtk docker image inspect metbench-sut:latest
```

If missing, report that real Docker smoke was not run and give the build command instead of claiming runtime execution evidence.

## Self-Review Checklist

- Phase A and Phase B are separate and independently testable.
- No raw Docker management surface is exposed.
- Docker backend uses existing runtime governance and evidence paths.
- Local Python runtime behavior is covered by regression tests.
- No WPF build or UI validation is required for this cloud-safe backend activation.
