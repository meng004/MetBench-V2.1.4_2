# API/MCP Semantic Vocabulary Cleanup Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Save and enforce the API / Business MCP / Runtime MCP vocabulary so `submit`, `job`, `execution`, `runtime run`, `cancel`, and `kill` have one clear meaning each.

**Architecture:** Public API and Business MCP expose durable `job` resources and user-facing commands. Runtime MCP owns backend command invocations and runtime `run_id` values. Documentation records the vocabulary, while source guards prevent public control-plane surfaces from adding a redundant `workflow` resource.

**Tech Stack:** .NET 8 xUnit architecture tests, Python stdlib `unittest`, Markdown control-plane documentation.

---

### Task 1: Save Vocabulary Definitions

**Files:**
- Modify: `docs/superpowers/specs/2026-06-21-systemmt-api-mcp-control-plane-design.md`
- Modify: `infra/mcp/metbench-business/README.md`
- Modify: `infra/mcp/docker-runtime/README.md`
- Modify: `docs/uat/api-business-runtime-mcp-e2e-runbook.md`

- [x] **Step 1: Record resource terms in the control-plane design**

Add a `Resource Terms` table that defines:

```text
workflow: internal architecture description, no public id
job: durable business control-plane resource, jobId
operation / job kind: SystemMtJobKind carried by one job
run / submit_run: command that creates a RunMr job, returns jobId
execution: persisted result/evidence, ExecutionId
runtime run: Runtime MCP backend command invocation, run_id
```

- [x] **Step 2: Project the vocabulary to Business MCP**

Add README text stating that Business MCP exposes `job` as the public resource, `submit_run` creates a `RunMr` job, and `workflow` is not an MCP object.

- [x] **Step 3: Project the vocabulary to Runtime MCP**

Add README text stating that Runtime MCP `run_id` identifies one backend command invocation and is not a MetBench `job_id`, not a System MT `ExecutionId`, and not a workflow id.

- [x] **Step 4: Project the vocabulary to the E2E runbook**

Add the same id mapping to the API / Business MCP / Runtime MCP E2E runbook so acceptance reports do not mix `job_id`, `ExecutionId`, and runtime `run_id`.

### Task 2: Enforce Public Surface Boundary

**Files:**
- Modify: `MetBench_SystemMT.Tests/SystemMT/Architecture/SystemMtControlPlaneBoundaryTests.cs`

- [x] **Step 1: Add an architecture guard**

Add an xUnit fact named `Public_api_and_business_mcp_do_not_expose_workflow_as_a_resource`.

- [x] **Step 2: Scope the guard to executable public surface**

Scan API/control-plane C# files and Business MCP Python files. Do not scan README files, because documentation must be able to explain why `workflow` is not a resource.

- [x] **Step 3: Verify the guard**

Run:

```powershell
dotnet test MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~SystemMtControlPlaneBoundaryTests"
```

Expected: `5 passed / 0 failed`.

### Task 3: Verify Semantic Refactor

**Files:**
- Existing changed API, Business MCP, Runtime MCP, docs, and tests.

- [x] **Step 1: Run Business MCP tests**

```powershell
python -m unittest discover infra/mcp/metbench-business/tests
```

Expected: `9 tests OK`.

- [x] **Step 2: Run Runtime MCP tests**

```powershell
python -m unittest discover infra/mcp/docker-runtime/tests
```

Expected: `48 tests OK`.

- [x] **Step 3: Run API/control-plane tests**

```powershell
dotnet test MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~SystemMtApi|FullyQualifiedName~ControlPlane"
```

Expected: `0 failed`; environment-gated E2E may skip when live MCP endpoints are not configured.

- [x] **Step 4: Run whitespace checks**

```powershell
git diff --check
```

Expected: no whitespace errors. LF/CRLF warnings are acceptable in this workspace.

### Task 4: Review, Fix, Commit, Push

**Files:**
- All tracked semantic refactor files.

- [x] **Step 1: Review the diff**

Check that public API/Biz MCP expose `job`, `cancel_job`, and `submit_run` only as business control-plane terms, while Runtime MCP owns `kill_run` and runtime `run_id`.

- [x] **Step 2: Fix any Critical or Important review findings**

Apply small patches and rerun the focused tests that cover the changed files.

- [x] **Step 3: Commit**

```powershell
git add <semantic-refactor-files>
git commit -m "docs(api): clarify api and mcp control semantics"
```

- [x] **Step 4: Push**

```powershell
git push -u origin HEAD
```

Expected: branch pushes to the configured `origin` remote. If network/auth is blocked, report the exact push failure.
