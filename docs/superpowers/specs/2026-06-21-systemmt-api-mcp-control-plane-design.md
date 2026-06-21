# System MT API / MCP Control Plane Design

> Date: 2026-06-21
> Status: Approved for implementation
> Scope: Runtime MCP boundary hardening, Runtime MCP evidence correlation, job-oriented control plane, REST API adapter, Business MCP adapter

## Problem

MetBench's design philosophy says System MT semantics belong to the core workflow:

```text
SystemMtJobService -> SystemMtLauncher -> SystemMtPipeline -> Typed Verification -> Recorder / Evidence / Anomaly
```

API, Business MCP, Runtime MCP, skills, and runtime agents are adapters. They must not create a second MR engine, second status machine, second assertion path, or unaudited runtime path.

The current `origin/main` direction is mostly aligned: Docker Runtime MCP is behind runtime profiles, preflight, launcher, pipeline, and async jobs; architecture guards already prevent future API adapters from directly naming Runtime MCP implementations. Two important gaps remain before REST API and Business MCP should be implemented:

- Docker Runtime MCP still accepts raw `argv` requests on `origin/main`.
- Docker Runtime MCP server produces a `run_id`, but C# runtime/evidence paths do not preserve it.

## Goals

1. Runtime MCP accepts only allowlisted structured tool requests, not raw process argv.
2. Runtime MCP run identifiers flow back into MetBench evidence so remote execution is auditable from the core result.
3. A shared System MT control-plane service exposes business operations by job id, never by host paths.
4. REST API and Business MCP are thin adapters over that control-plane service.
5. Tests and source guards prevent future control-plane/runtime-plane drift.

## Non-Goals

- Do not change MR semantics, typed catalog predicates, or assertion kernels.
- Do not add result/evidence import.
- Do not expose Docker build/run controls through Business MCP.
- Do not make Runtime MCP the default agent-facing business interface.
- Do not add WPF UI changes in this chain.
- Do not implement remote/HPC backends.

## Architecture

Runtime MCP remains an execution-plane adapter. The server owns image/tool/mount allowlists and produces bounded run records. The C# runtime client sends structured `{ image, tool, args, timeout_seconds }` requests, receives `run_id`, and makes that id available to the core evidence path.

The control plane is a BLL.Core application service. It depends on existing business services: `ISystemMtJobService`, `ISystemMtCatalogReader` or `ISystemMtLauncher`, `IJobStore`, and `ISystemMtArtifactAccessService`. It exposes operations such as `SubmitRun`, `SubmitBatch`, `GetJob`, `CancelJob`, `GetResult`, `ListArtifacts(jobId)`, and `GetArtifact(jobId, artifactId)`.

REST API and Business MCP use the same control-plane contract. REST handles HTTP auth, DTO binding, request size limits, and `ProblemDetails`. Business MCP exposes agent-facing tools only: `list_mrs`, `submit_run`, `submit_batch`, `get_job`, `cancel_job`, `get_result`, `list_artifacts`, and `get_artifact`.

## Data Flow

```text
REST / Business MCP
  -> SystemMtControlPlaneService
  -> SystemMtJobService.SubmitOperationAsync / GetStatusAsync / CancelAsync / GetResultAsync
  -> SystemMtJobWorker
  -> SystemMtLauncher
  -> SystemMtPipeline
  -> IRuntimeProcessExecutor
  -> DockerMcpProcessExecutor
  -> Docker Runtime MCP structured tool request
  -> ProcessResult + Runtime MCP run id
  -> ExecutionEvidence / RuntimeEvidence
```

Artifact access is job-oriented at the adapter boundary:

```text
GET /api/v1/jobs/{jobId}/artifacts
  -> control plane loads job
  -> control plane resolves job.ArtifactPath internally
  -> ISystemMtArtifactAccessService.ListAsync(manifestPath)
  -> safe descriptor list with opaque artifact ids
```

No public API or Business MCP request accepts `PackageRoot`, `StagingRoot`, `ExportRoot`, `ArtifactPath`, or `manifestPath`.

## Business Rules

- Blank MR ids are rejected before job submission.
- Batch submissions reject empty lists, blank ids, and duplicate MR ids.
- Parameter overrides reject blank keys and blank values at the control-plane boundary.
- Runtime MCP rejects raw `argv`; it accepts only configured tool names and bounded args.
- Runtime MCP rejects shell evaluation flags, module execution flags, script path args, path traversal, shell operators, and absolute host-control paths in tool args.
- A failed MR assertion remains a successful job carrying a failed MR verdict; infrastructure/runtime failures may fail the job.
- Runtime preflight failures remain runtime failures, not MR assertion anomalies.
- Artifact reads require a terminal job with an artifact manifest path recorded by the worker/export operation.

## Testing Strategy

- Python unit tests cover Runtime MCP config loading, auth, tool allowlist validation, command construction, run record storage, and `run_id` response shape.
- C# runtime tests cover profile parsing, Docker MCP request construction, tool/arg validation, `run_id` parsing, and evidence projection.
- Control-plane tests use fakes for job/catalog/artifact dependencies and verify job-oriented artifact access plus validation.
- REST API tests use an in-memory host and fake control-plane service.
- Business MCP tests use a fake REST/control-plane boundary and assert only business tools are exposed.
- Architecture source guards scan API, Business MCP, and ControlPlane paths for Runtime MCP implementation terms and raw path DTO fields.

## Execution Order

1. Harden Runtime MCP raw argv boundary.
2. Preserve Runtime MCP `run_id` in MetBench runtime/evidence paths.
3. Add job-oriented control-plane service.
4. Add REST API and Business MCP adapters.

Each step must use TDD: write the failing test, run it red, implement the minimal code, run focused tests green, then review before moving to the next task.
