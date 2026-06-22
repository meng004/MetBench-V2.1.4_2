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

The control plane is a BLL.Core application service. It depends on existing business services: `ISystemMtJobService`, `ISystemMtCatalogReader` or `ISystemMtLauncher`, and `IJobStore`. It exposes operations such as `SubmitRun`, `GetJob`, `CancelJob`, `GetResult`, and `GetEvidence`.

REST API and Business MCP use the same control-plane contract. REST handles HTTP auth, DTO binding, request size limits, and `ProblemDetails`. Business MCP exposes the current agent-facing tools only: `business_health`, `submit_run`, `get_job`, `cancel_job`, `get_result`, and `get_evidence`.

## Control Semantics Vocabulary

The API and MCP surfaces use one shared semantic vocabulary. A term belongs to exactly one plane unless this section explicitly says it is a cross-plane propagation.

### Resource Terms

These terms are intentionally not interchangeable:

| Term | Plane | Identifier | Definition | Must not mean |
|---|---|---|---|---|
| `workflow` | Internal architecture description | None | The ordered System MT orchestration path inside MetBench core, such as `SystemMtJobService -> SystemMtLauncher -> SystemMtPipeline -> Typed Verification -> Recorder / Evidence / Anomaly`. It is not a public REST or MCP resource. | A submit target, durable record, queue item, or Runtime MCP command. |
| `job` | Business control plane | `jobId` | The durable, pollable async business execution record stored by `SystemMtJobService`. A job has one `SystemMtJobKind`, one state machine, progress, phase, failure reason, optional `ExecutionId`, and optional artifact pointer. | A runtime process, Docker container, Runtime MCP `run_id`, or MR assertion result. |
| `operation` / `job kind` | Business control plane | `SystemMtJobKind` | The kind of work carried by one job, for example `RunMr`, `RunBatch`, `ImportAssets`, `ExportAssets`, `ExportExecutionArtifacts`, or `ExportReport`. | A second resource hierarchy above job. |
| `run` / `submit_run` | Business control plane command | Returns `jobId` | A user-facing command that creates a `RunMr` job. The command name is a verb phrase, not a separate resource type. | Runtime MCP `run_sut_command` or `run_id`. |
| `execution` | Core result/evidence layer | `ExecutionId` | The persisted System MT execution result/evidence created by the core launcher/recorder path after a job runs far enough to produce result evidence. | The job record or runtime backend process. |
| `runtime run` | Runtime execution plane | Runtime `run_id` | One backend command invocation owned by Runtime MCP, such as parser, SUT runner, or output parser execution. Multiple runtime runs may support one MetBench job. | A MetBench job, workflow, or execution result. |

Public API and Business MCP use `job` as the resource. They may expose verbs such as `submit_run` for usability, but they must not add `workflow` as another resource name or accept runtime `run_id` values where a `jobId` is required.

The creation chain is intentionally one-way:

- Submit creates a `jobId`; it does not create an `ExecutionId`.
- Runtime MCP creates runtime `run_id` values; it does not create jobs or executions.
- The core recorder creates `ExecutionId` after the job runs far enough to produce persisted result/evidence.

| Semantic | Chinese label | Plane | Public surface | Target | Definition |
|---|---|---|---|---|---|
| `submit` | 提交 | Business control plane | REST API / Business MCP | MR id, batch MR ids, or operation request | Creates a durable System MT job and returns a job id. It does not directly start a process from the adapter. |
| `poll` / `get_job` | 查询 | Business control plane | REST API / Business MCP | Job id | Reads durable job state, phase, progress, failure reason, artifact pointer, and execution id when available. |
| `get_result` / `get_evidence` | 取结果 / 取证据 | Business control plane | REST API / Business MCP | Job id | Reads the MetBench result/evidence produced by the core workflow. It does not read host paths supplied by the caller. |
| `cancel` | 取消 | Business control plane | REST API / Business MCP | Job id | Requests that a queued or running business job stop and transition to the `Cancelled` terminal state. REST expresses this as `POST /jobs/{jobId}/cancel`; Business MCP expresses it as `cancel_job`. This is the user-facing stop operation. |
| `kill` | 终止 | Runtime execution plane | Runtime MCP only | Runtime `run_id` or backend execution handle | Forcibly stops an already-started backend execution unit such as a process, process tree, container, or remote command. This is an execution-plane operation, not a business workflow operation. |

`cancel` and `kill` are related but not interchangeable:

- `cancel` is job-oriented, durable, idempotent, and owns the business terminal state. A cancelled job must not later be rewritten as `Succeeded` by an orphaned worker result.
- `kill` is backend-oriented, best-effort, and owns only runtime interruption evidence. It must not decide MR pass/fail, job success/failure, anomaly classification, or artifact visibility.
- A running job `cancel` may propagate to a runtime `kill` when the active backend exposes a killable runtime handle. That propagation is an implementation detail recorded in runtime evidence; callers still invoke `cancel_job`.
- A direct Runtime MCP `kill` is reserved for runtime diagnostics or internal cancellation propagation. Business MCP must not expose `kill_run`, Docker controls, raw process ids, or container ids.
- If a backend cannot kill an in-flight run, `cancel` still marks the business job as cancelled, but evidence must not claim true runtime termination unless process/container disappearance is observed.

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
- `cancel_job` is the only public Business MCP stop operation. It targets a job id and never accepts runtime run ids, process ids, container ids, or host paths.
- Runtime MCP `kill` semantics require a runtime handle that exists before the command completes. The current synchronous `run_sut_command` shape can return a `run_id` only after completion, so `kill_run` can honestly report `not_found` or `not_running` but cannot claim true in-flight remote kill by itself.

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
