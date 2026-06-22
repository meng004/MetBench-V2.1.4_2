# MetBench Docker Runtime MCP Design

> Date: 2026-06-12
> Status: Approved for implementation
> Scope: Phase A Docker Runtime MCP infrastructure, Phase B MetBench Docker runtime backend activation

## Problem

MetBench can run on macOS while a program under test needs a different runtime environment, such as Linux-native scientific dependencies or a Windows-only runner. Existing Docker assets (`docker/Dockerfile`, `docker/Dockerfile.runtime`, and `docker/wrappers/*.sh`) prove the Linux SUT path, but they are local wrapper scripts rather than a network-callable runtime service.

The goal is to provide a LAN-accessible MCP server that other Codex instances can call, and then activate Docker as a first-class System MT runtime backend without bypassing existing launcher, preflight, async job, typed verification, or evidence paths.

## Non-Goals

- Do not expose a general-purpose Docker administration MCP.
- Do not expose raw Docker socket access.
- Do not allow arbitrary Docker arguments from clients.
- Do not change MR semantics, typed catalog predicates, kernels, or assertion behavior.
- Do not replace the existing local Python runtime path.
- Do not implement remote/HPC backends in this change.
- Do not require WPF UI changes for the first activation.

## Phase A: Docker Runtime MCP Infrastructure

Phase A adds an infrastructure service under `infra/mcp/docker-runtime/`.

The service defaults to listening on the first private LAN IPv4 address. It fails closed if no private address is available unless `bind_host` is explicitly configured. It requires bearer-token authentication for every tool call.

The server exposes only MetBench runtime operations:

- `runtime_health`: reports Docker CLI availability, Docker daemon availability, configured bind address, and allowed image status.
- `list_runtime_images`: lists only configured allowlist images.
- `build_runtime_image`: builds only allowlisted MetBench images from repository-owned Dockerfiles.
- `run_sut_command`: runs a command inside an allowlisted image using server-generated Docker arguments.
- `get_run_result`: returns a bounded run record by run id.

Security rules:

- Images are allowlisted; initial values are `metbench-sut:latest` and `metbench-runtime:latest`.
- Mounts are allowlisted; initial values are the repository root and service-managed work directories under `/tmp`.
- The client supplies command arguments for the in-container executable, not raw Docker flags.
- Privileged mode, host networking, arbitrary volumes, arbitrary images, and Docker socket mounts are rejected.
- Every run has a timeout and stdout/stderr truncation limit.
- Every run gets a run id and an audit record.

The Phase A server intentionally uses Python standard library components so it can run on a development machine without adding npm or pip dependencies.

## Phase B: MetBench Docker Runtime Backend

Phase B activates Docker runtime support through existing System MT runtime governance.

Current source has `RuntimeKind.DockerPlaceholder`, and `RuntimePreflightService` blocks all non-executable placeholder kinds. Phase B promotes Docker to an executable backend by adding Docker-specific runtime metadata and an executor path while preserving placeholders for remote and HPC.

Integration boundary:

- Runtime profile resolution remains owned by `LauncherOptionsRuntimeProfileProvider`.
- Runtime preflight remains owned by `RuntimePreflightService`.
- MR execution remains `SystemMtLauncher -> SystemMtPipeline -> IProcessExecutor`.
- Async execution remains `SystemMtJobWorker -> SystemMtAsyncPipeline -> ISystemMtLauncher`.
- Evidence remains `RuntimeEvidence.FromPreflightResult(...)` and existing execution/evidence persistence.

The Docker backend is represented as a runtime profile whose key is configured through `LauncherOptions.RuntimePythons` using a URI-like value:

```text
docker-mcp://<runtime-key>?image=metbench-sut:latest&tool=openmoc-runner&local=openmoc-runner&python=/opt/openmoc-venv/bin/python&endpoint=http://LAN-IP:PORT
```

The value is not a local executable path. The provider parses it into Docker runtime metadata and returns a Docker runtime profile. Local Python keys keep their existing behavior.

Execution model:

- For local Python profiles, `DefaultProcessExecutor` remains unchanged.
- For Docker profiles, `DockerMcpProcessExecutor` calls the Phase A MCP server.
- Docker executor input is still the command string produced by the existing pipeline, but it must map the configured local Python executable to the configured in-container Python path before sending the command to MCP.
- The Docker MCP server runs the container with the repository mounted at the same logical path expected by the command.
- Cancellation and timeout propagate through the executor.

Preflight model:

- Docker preflight checks the MCP endpoint health.
- Docker preflight checks the configured image exists or reports explicit missing-image diagnostics.
- Docker preflight checks the configured in-container Python responds to `--version`.
- Dependency checks are executed through MCP with the configured in-container Python.

Failure model:

- Missing MCP endpoint or authentication failure maps to `RuntimeFailureKind.MiddlewareUnavailable`.
- Missing image maps to `RuntimeFailureKind.MiddlewareUnavailable`.
- Bad in-container executable maps to `RuntimeFailureKind.RuntimeExecutableMissing`.
- Dependency import failure maps to `RuntimeFailureKind.DependencyMissing`.
- MCP run timeout maps to `RuntimeFailureKind.Timeout`.
- Container non-zero exit during SUT execution maps through the existing pipeline failure path.

## Configuration

Phase A server config:

```json
{
  "bind_host": "auto-private-ipv4",
  "bind_port": 8765,
  "auth_token": "change-me",
  "repo_root": "/Users/limeng/Codes/MetBench-V2.1.4_2",
  "allowed_images": {
    "metbench-sut:latest": {
      "dockerfile": "docker/Dockerfile",
      "context": "docker"
    },
    "metbench-runtime:latest": {
      "dockerfile": "docker/Dockerfile.runtime",
      "context": "docker"
    }
  },
  "allowed_mount_roots": [
    "/Users/limeng/Codes/MetBench-V2.1.4_2",
    "/tmp"
  ],
  "default_timeout_seconds": 120,
  "max_output_bytes": 65536
}
```

Phase B runtime config example:

```csharp
RuntimePythons = new Dictionary<string, string>
{
    ["openmoc-docker"] = "docker-mcp://openmoc-docker?image=metbench-sut:latest&tool=openmoc-runner&local=openmoc-runner&python=/opt/openmoc-venv/bin/python&endpoint=http://192.168.1.20:8765",
    ["openmc-docker"] = "docker-mcp://openmc-docker?image=metbench-sut:latest&tool=openmc-runner&local=openmc-runner&python=/opt/openmc-venv/bin/python&endpoint=http://192.168.1.20:8765"
}
```

## Verification

Phase A verification:

```bash
rtk python3 -m unittest discover infra/mcp/docker-runtime/tests
rtk python3 infra/mcp/docker-runtime/server.py --config infra/mcp/docker-runtime/config.example.json --self-test
```

Phase B verification:

```bash
rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~RuntimeProfile|FullyQualifiedName~RuntimePreflight|FullyQualifiedName~DockerMcp|FullyQualifiedName~SystemMtAsyncPipeline"
```

Optional Docker smoke, only after `metbench-sut:latest` exists or is built:

```bash
rtk docker --version
rtk docker image inspect metbench-sut:latest
```

## Acceptance Criteria

- A LAN MCP endpoint can be started with explicit token authentication.
- The MCP endpoint defaults to a private LAN IPv4 address and refuses ambiguous public binding by default.
- Docker operations are constrained by image and mount allowlists.
- Unit tests prove command validation rejects raw Docker control flags and disallowed mounts/images.
- Runtime profile resolution can parse Docker MCP runtime values.
- Runtime preflight no longer treats Docker as a placeholder when Docker MCP metadata is configured.
- Local Python runtime behavior remains unchanged.
- Docker runtime execution goes through `IProcessExecutor`, preserving launcher, async job, and evidence paths.
- Focused tests pass with no WPF build requirement.
