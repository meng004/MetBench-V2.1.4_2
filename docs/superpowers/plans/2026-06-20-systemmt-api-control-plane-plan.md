# System MT API Control Plane Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a REST API and agent-facing Business MCP as adapters over the existing System MT workflow without creating a second MT engine.

**Architecture:** The truth path remains `SystemMtJobService -> SystemMtLauncher -> SystemMtPipeline -> Typed Verification -> Recorder / Evidence / Anomaly`. REST API and Business MCP share a control-plane service; Runtime MCP remains a lower-level execution backend and is not exposed as the agent business interface.

**Tech Stack:** .NET 8, ASP.NET Core Minimal API, xUnit, existing `MetBench_BLL.SystemMT.*` services, optional future MCP adapter.

---

## Architecture Rules

1. REST API and Business MCP are business control-plane adapters.
2. Runtime MCP is an execution-plane adapter and must not become the default agent entry point.
3. API/MCP DTOs must not expose raw host paths such as `PackageRoot`, `StagingRoot`, `ExportRoot`, or raw `ArtifactPath`.
4. API/MCP must not call `DockerMcpRuntimeClient` or `DockerMcpProcessExecutor` directly.
5. Every run must enter through `ISystemMtJobService` or `ISystemMtLauncher`; no direct runner invocation is allowed.

## Task 1: Shared System MT DI

**Files:**
- Create: `MetBench_BLL.Core/SystemMT/Hosting/SystemMtServiceCollectionExtensions.cs`
- Test: `MetBench_SystemMT.Tests/SystemMT/Hosting/SystemMtServiceCollectionExtensionsTests.cs`

- [ ] Write a failing test that builds a service provider and resolves `ISystemMtJobService`, `ISystemMtLauncher`, `IRuntimeProfileProvider`, `IRuntimePreflightService`, and `ISystemMtArtifactAccessService`.
- [ ] Implement `AddSystemMtServices(...)` with options for SUT root, job DB path, result DB path, artifact roots, and runtime python map.
- [ ] Update WPF registration only after the API path is green and with Windows verification planned separately.

## Task 2: Control-Plane Application Service

**Files:**
- Create: `MetBench_BLL.Core/SystemMT/ControlPlane/SystemMtControlPlaneModels.cs`
- Create: `MetBench_BLL.Core/SystemMT/ControlPlane/ISystemMtControlPlaneService.cs`
- Create: `MetBench_BLL.Core/SystemMT/ControlPlane/SystemMtControlPlaneService.cs`
- Test: `MetBench_SystemMT.Tests/SystemMT/ControlPlane/SystemMtControlPlaneServiceTests.cs`

- [ ] Tests cover `ListMrs`, `SubmitRun`, `SubmitBatch`, `GetJob`, `CancelJob`, `GetResult`, and artifact listing with fake launcher/job/artifact services.
- [ ] `SubmitRun` maps to `ISystemMtJobService.SubmitOperationAsync(new SystemMtOperationJobRequest(SystemMtJobKind.RunMr, ...))`.
- [ ] `SubmitBatch` maps to `RunBatch`.
- [ ] The service must never accept package/staging/export roots from caller-provided DTOs.

## Task 3: REST API Adapter

**Files:**
- Create: `MetBench_Api/MetBench_Api.csproj`
- Create: `MetBench_Api/Program.cs`
- Create: `MetBench_Api/SystemMtApiEndpoints.cs`
- Create: `MetBench_Api/SystemMtApiModels.cs`
- Test: `MetBench_SystemMT.Tests/SystemMT/Api/SystemMtApiEndpointTests.cs`

- [ ] Add endpoints:
  - `GET /api/v1/health`
  - `GET /api/v1/mrs`
  - `GET /api/v1/mrs/{mrId}`
  - `POST /api/v1/runs`
  - `POST /api/v1/batches`
  - `GET /api/v1/jobs/{jobId}`
  - `POST /api/v1/jobs/{jobId}/cancel`
  - `GET /api/v1/jobs/{jobId}/result`
  - `GET /api/v1/jobs/{jobId}/artifacts`
  - `GET /api/v1/jobs/{jobId}/artifacts/{artifactId}`
- [ ] Add bearer token authentication for non-health endpoints.
- [ ] Add request size limits and ProblemDetails error responses.
- [ ] Endpoint tests must prove raw path fields are rejected or absent from DTOs.

## Task 4: Business MCP Adapter

**Files:**
- Create: `infra/mcp/metbench-business/README.md`
- Create: `infra/mcp/metbench-business/server.py` or a .NET MCP host, depending on selected deployment target.
- Test: `infra/mcp/metbench-business/tests/test_server.py` or .NET equivalent.

- [ ] Expose agent-facing tools only:
  - `list_mrs`
  - `submit_run`
  - `submit_batch`
  - `get_job`
  - `cancel_job`
  - `get_result`
  - `list_artifacts`
  - `get_artifact`
- [ ] Business MCP should call the REST API or the same control-plane service.
- [ ] Business MCP must not expose `run_sut_command`, Docker image build/run, raw shell, or raw artifact paths.

## Task 5: Architecture Guards

**Files:**
- Modify: `MetBench_SystemMT.Tests/SystemMT/Architecture/SystemMtControlPlaneBoundaryTests.cs`

- [ ] Guard that `MetBench_Api` and Business MCP do not reference `DockerMcpRuntimeClient`, `DockerMcpProcessExecutor`, or `run_sut_command`.
- [ ] Guard that API DTOs do not contain raw path fields.
- [ ] Guard that Runtime MCP documentation points readers to Business MCP for agent-facing run submission.

## Verification

- [ ] `rtk dotnet build MetBench_BLL.Core/MetBench_BLL.Core.csproj --no-restore`
- [ ] `rtk dotnet test MetBench_SystemMT.Tests --no-restore --filter "FullyQualifiedName~ControlPlane|FullyQualifiedName~Api|FullyQualifiedName~Artifact|FullyQualifiedName~SystemMtControlPlaneBoundary"`
- [ ] `rtk git diff --check`

## Windows Classification

Initial API/control-plane work is cloud-safe if it stays in `MetBench_BLL.Core/`, `MetBench_SystemMT.Tests/`, `MetBench_Api/`, and `infra/mcp/`. WPF registration cleanup requires a separate Windows build and UI evidence pass.
