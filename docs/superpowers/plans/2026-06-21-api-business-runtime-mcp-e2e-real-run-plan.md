# API + Business MCP + Runtime MCP End-to-End Real Run Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Prove the real chain `Business MCP -> REST API -> SystemMtControlPlaneService -> SystemMtJobService -> Launcher -> Runtime MCP -> Evidence` works against a live local Runtime MCP server, with Docker or WSL accepted as the remote-environment simulator.

**Architecture:** Runtime MCP remains execution-plane only. Business MCP calls REST API only; REST API calls `ISystemMtControlPlaneService`; the core job/launcher/pipeline owns MR execution and evidence. The first executable target is a local Runtime MCP backend because it gives deterministic CI-style feedback without requiring Docker image build; a Docker/WSL run is a follow-up environment pass using the same test and config shape.

**Tech Stack:** .NET 8 xUnit, ASP.NET Core Minimal API, Python 3 stdlib `unittest` and HTTP servers, existing `infra/mcp/docker-runtime` and `infra/mcp/metbench-business`.

---

## Current Facts

- Branch: `codex/systemmt-api-mcp-control-plane`.
- Design source: `docs/superpowers/specs/2026-06-21-systemmt-api-mcp-control-plane-design.md`.
- Existing Runtime MCP acceptance stops at launcher/job level: `MetBench_SystemMT.Tests/SystemMT/Acceptance/McpThreeCaseAcceptanceTests.cs`.
- Existing API tests call endpoint methods directly: `MetBench_SystemMT.Tests/SystemMT/Api/SystemMtApiEndpointsTests.cs`.
- Existing Business MCP tests use a fake API client: `infra/mcp/metbench-business/tests/test_server.py`.
- Missing proof: one live run crossing Business MCP, REST API, job worker, launcher, Runtime MCP, and runtime evidence in the same test/runbook.

## File Structure

- Create: `MetBench_SystemMT.Tests/SystemMT/Acceptance/SystemMtApiBusinessRuntimeMcpEndToEndTests.cs`
  - Environment-gated xUnit acceptance test.
  - Starts or consumes live local services as configured.
  - Submits through Business MCP and verifies API job/result/evidence.
- Modify if needed: `MetBench_Api/Program.cs`
  - Only if required to make the API host configurable for test data dir, SUT root, runtime URI, and bind URLs.
- Modify if needed: `MetBench_Api/SystemMtApiEndpoints.cs`
  - Only if endpoint route or auth behavior blocks the real flow.
- Modify if needed: `infra/mcp/metbench-business/server.py`
  - Only if live tool dispatch cannot call the current REST routes.
- Modify if needed: `infra/mcp/docker-runtime/server.py`
  - Only if live local backend cannot execute allowlisted tool requests with safe absolute data paths.
- Create: `docs/uat/api-business-runtime-mcp-e2e-runbook.md`
  - Manual commands for local, Docker, and WSL-backed runs.
- Create evidence directory after a successful run:
  - `docs/superpowers/specs/2026-06-21-api-business-runtime-mcp-e2e-evidence/`

## Task 1: Add Real E2E Acceptance Test

**Files:**
- Create: `MetBench_SystemMT.Tests/SystemMT/Acceptance/SystemMtApiBusinessRuntimeMcpEndToEndTests.cs`

- [ ] **Step 1: Write failing test**

Add a `SkippableFact` gated by:

```text
METBENCH_E2E_BUSINESS_MCP_URL
METBENCH_E2E_BUSINESS_MCP_TOKEN
METBENCH_E2E_API_URL
METBENCH_E2E_MR_ID
```

The test must:

1. POST to `${METBENCH_E2E_BUSINESS_MCP_URL}/tool` with bearer token.
2. Use tool `submit_run` and argument `{ "mr_id": "<MR>" }`.
3. Poll `${METBENCH_E2E_API_URL}/api/v1/systemmt/jobs/{jobId}` until terminal.
4. Fetch `/result` and assert `passed == true`.
5. Fetch `/evidence` and assert runtime evidence contains at least one non-blank Runtime MCP run id.

Expected first RED result: fail because services are not started, or because evidence/API does not expose the needed run ids.

- [ ] **Step 2: Verify RED**

Run:

```powershell
dotnet test MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~SystemMtApiBusinessRuntimeMcpEndToEnd"
```

Expected: skipped when env is absent; fail with a clear connection/evidence assertion when env is present but the live chain is incomplete.

## Task 2: Bring Up Local Runtime MCP, API, and Business MCP

**Files:**
- Create temporary configs under `tmp/e2e-mcp/` during the run only.
- No committed code change expected unless Task 1 exposes a real defect.

- [ ] **Step 1: Start Runtime MCP local backend**

Create `tmp/e2e-mcp/runtime.local.json` with:

```json
{
  "backend": "local",
  "repo_root": "D:/Codes/MetBench-V2.1.4_2",
  "bind_host": "127.0.0.1",
  "bind_port": 8765,
  "auth_token": "runtime-secret",
  "allowed_images": {
    "local-metbench": { "dockerfile": "", "context": "" }
  },
  "allowed_tools": {
    "python": { "executable": "python" }
  },
  "allowed_mount_roots": [
    "D:/Codes/MetBench-V2.1.4_2",
    "C:/Users/lemon/AppData/Local/Temp"
  ],
  "default_timeout_seconds": 60,
  "max_output_bytes": 1048576
}
```

Start:

```powershell
python infra\mcp\docker-runtime\server.py tmp\e2e-mcp\runtime.local.json
```

Expected: server listens on `127.0.0.1:8765`.

- [ ] **Step 2: Start REST API**

Set runtime profile for a stdlib MR whose SUT runtime key is `system`:

```powershell
$env:METBENCH_DOCKER_MCP_TOKEN = "runtime-secret"
$env:LauncherOptions__RuntimePythons__system = "docker-mcp://system?image=local-metbench&tool=decay-chain-runner&local=decay-chain-runner&python=python&endpoint=http%3A%2F%2F127.0.0.1%3A8765&authTokenEnv=METBENCH_DOCKER_MCP_TOKEN"
dotnet run --project MetBench_Api --urls http://127.0.0.1:5080
```

Expected: API listens on `127.0.0.1:5080` and hosted job worker starts.

- [ ] **Step 3: Start Business MCP**

Create `tmp/e2e-mcp/business.local.json`:

```json
{
  "bind_host": "127.0.0.1",
  "bind_port": 8790,
  "auth_token": "business-secret",
  "api_base_url": "http://127.0.0.1:5080",
  "api_token": "unused",
  "default_timeout_seconds": 60
}
```

Start:

```powershell
python infra\mcp\metbench-business\server.py --config tmp\e2e-mcp\business.local.json
```

Expected: Business MCP listens on `127.0.0.1:8790`.

- [ ] **Step 4: Run the E2E test GREEN**

Set:

```powershell
$env:METBENCH_E2E_BUSINESS_MCP_URL = "http://127.0.0.1:8790"
$env:METBENCH_E2E_BUSINESS_MCP_TOKEN = "business-secret"
$env:METBENCH_E2E_API_URL = "http://127.0.0.1:5080"
$env:METBENCH_E2E_MR_ID = "decay-chain-scale-initial"
dotnet test MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~SystemMtApiBusinessRuntimeMcpEndToEnd"
```

Expected: pass; API job reaches `Succeeded`; result passes; evidence exposes Runtime MCP source/follow-up run ids.

## Task 3: TDD Fix Loop for Any Real Failure

**Files:** determined by the failing boundary.

- [ ] **Step 1: If Business MCP fails before reaching API**

Write or extend `infra/mcp/metbench-business/tests/test_server.py` first. The test must reproduce the exact payload/path/auth mismatch. Run:

```powershell
python -m unittest discover infra\mcp\metbench-business\tests
```

Expected RED before implementation, GREEN after minimal fix.

- [ ] **Step 2: If API accepts request but job does not complete**

Write or extend `MetBench_SystemMT.Tests/SystemMT/Api/SystemMtApiEndpointsTests.cs` or a new host-level test first. Run:

```powershell
dotnet test MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~SystemMtApi"
```

Expected RED before implementation, GREEN after minimal fix.

- [ ] **Step 3: If Runtime MCP runs but evidence lacks run ids**

Write or extend runtime/evidence tests first:

```powershell
dotnet test MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~DockerMcp|FullyQualifiedName~RuntimeEvidence|FullyQualifiedName~SystemMtPipeline"
```

Expected RED before implementation, GREEN after minimal fix.

## Task 4: Docker or WSL Remote Simulation Pass

**Files:**
- Create: `docs/uat/api-business-runtime-mcp-e2e-runbook.md`
- Evidence files under `docs/superpowers/specs/2026-06-21-api-business-runtime-mcp-e2e-evidence/`

- [x] **Step 1: Docker pass**

If Docker Desktop is available, run Runtime MCP with `backend: docker`, allowed image `metbench-runtime:latest` or `metbench-sut:latest`, and the same Business MCP/API test variables.

Expected: same E2E test passes and Runtime MCP log contains generated `run_id` lines.

- [x] **Step 2: WSL pass**

If WSL is available, run Runtime MCP server inside WSL on a LAN/WSL IP, point API runtime URI at that endpoint, and rerun the same E2E test.

Expected: same E2E test passes or a clear environment blocker is recorded.

- [x] **Step 3: Save evidence**

Record:

- commands used,
- process URLs/ports,
- MR id,
- job id,
- result status,
- runtime run ids,
- whether local/Docker/WSL was used,
- any blocker with exact stderr/log line.

2026-06-22 evidence:

- Docker backend PASS: `SystemMtApiBusinessRuntimeMcpEndToEnd` = 1 passed / 0 failed / 0 skipped; job `5955777a-0cf1-4f67-a368-3948fe079e4c`; Runtime MCP run ids `9d7208f51c174e6791cb991c2f9cb0ef` and `8215b48d5cc449089aa60565c47c432f`.
- Required Docker runtime profile options: `python=python` for local parser execution and `pathStyle=wsl` for container-visible path translation.
- WSL backend PASS after non-sandbox verification: distro `MetBenchUbuntu2404Run`; Runtime MCP local backend on `8976`; `SystemMtApiBusinessRuntimeMcpEndToEnd` = 1 passed / 0 failed / 0 skipped; job `1c44fd59-0bc1-4452-98e5-2e024aaf862d`; Runtime MCP run ids `bd18a635df7941ff8decd944fc3dacd0` and `94bacb6b6d8c4d60a7fac255064c89d1`.
- Evidence: `docs/superpowers/specs/2026-06-21-api-business-runtime-mcp-e2e-evidence/docker-wsl-debug-summary.md`.

## Final Verification

Run:

```powershell
python -m unittest discover infra\mcp\docker-runtime\tests
python -m unittest discover infra\mcp\metbench-business\tests
dotnet test MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~DockerMcp|FullyQualifiedName~RuntimeEvidence|FullyQualifiedName~ControlPlane|FullyQualifiedName~Api|FullyQualifiedName~SystemMtApiBusinessRuntimeMcpEndToEnd"
dotnet build MetBench_BLL.Core\MetBench_BLL.Core.csproj --no-restore
git diff --check
```

## Subagent Protocol

Use one worker subagent for each implementation/fix task:

1. Worker writes or updates the failing test and proves RED.
2. Worker implements the minimal fix and proves GREEN.
3. Main agent runs focused verification.
4. Spec-review subagent checks against this plan and `2026-06-21-systemmt-api-mcp-control-plane-design.md`.
5. Code-quality review subagent checks for boundary violations and overbuild.

No WPF changes are in scope. If WPF registration is required, stop and create a Windows VM plan.
