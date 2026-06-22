# API + Business MCP + Runtime MCP E2E Runbook

This runbook verifies the live chain:

```text
Business MCP -> REST API -> SystemMtControlPlaneService -> SystemMtJobService -> SystemMtLauncher -> Runtime MCP -> ExecutionEvidence
```

Current default mode: all external calls are Runtime MCP tool invocations. The API `docker-mcp://` profile should not pass `python`, `local`, or `pathStyle`; local and WSL execution are Runtime MCP backend choices.

## Resource Vocabulary Under Test

- `job_id` is the durable Business MCP / REST API resource identifier.
- `submit_run` creates a `RunMr` job and returns `job_id`; it does not create a
  separate public `run` resource.
- `ExecutionId` identifies the persisted System MT result/evidence created by
  the core launcher/recorder path.
- Runtime MCP `run_id` identifies one backend command invocation. One job may
  produce multiple runtime `run_id` values.
- `workflow` is not a public API or MCP resource in this chain; it only
  describes the internal MetBench orchestration path.

## Control Semantics Under Test

The API / Business MCP / Runtime MCP chain uses these stop semantics:

- `cancel` means a business control-plane request against a MetBench job id.
  REST API and Business MCP expose this as `cancel_job`. It owns the durable
  job terminal state `Cancelled`.
- `kill` means a runtime execution-plane request against an already-started
  backend handle such as a runtime `run_id`, process id, or container id.
  Runtime MCP owns this semantic. Business MCP must not expose it.

This runbook verifies submit/poll/result/evidence through Docker and WSL
Runtime MCP backends. It does not prove true remote in-flight kill, because the
current Runtime MCP `run_sut_command` call is synchronous and returns `run_id`
after command completion. Do not report `cancel_job` evidence as "remote kill"
unless a future async Runtime MCP contract records that the backend process or
container was actually terminated.

## Docker Backend

Preconditions:

- Docker Desktop is running.
- `docker info` succeeds from the shell used to start Runtime MCP.
- Image `metbench-e2e-remote-decay:latest` exists or is buildable from `tmp/e2e-remote-docker/Dockerfile`.
- Runtime MCP config exposes `input-parser`, `output-parser`, and `sut-runner`.

Start Runtime MCP:

```powershell
python infra\mcp\docker-runtime\server.py tmp\e2e-remote-docker\runtime.docker.json
```

Required API runtime profile:

```powershell
$env:METBENCH_DB_PATH='D:\Codes\MetBench-V2.1.4_2\tmp\e2e-remote-docker\api-data\SystemMT.Litedb'
$env:METBENCH_DOCKER_MCP_TOKEN='runtime-secret'
$env:LauncherOptions__SutRoot='D:\Codes\MetBench-V2.1.4_2\SUT'
$env:LauncherOptions__RuntimePythons__system='docker-mcp://system?image=metbench-e2e-remote-decay%3Alatest&endpoint=http%3A%2F%2F127.0.0.1%3A8985&authTokenEnv=METBENCH_DOCKER_MCP_TOKEN'
dotnet run --project MetBench_Api --urls http://127.0.0.1:5290
```

Start Business MCP:

```powershell
python infra\mcp\metbench-business\server.py --config tmp\e2e-remote-docker\business.docker.json
```

Run E2E test:

```powershell
$env:METBENCH_E2E_BUSINESS_MCP_URL='http://127.0.0.1:8995'
$env:METBENCH_E2E_BUSINESS_MCP_TOKEN='business-secret'
$env:METBENCH_E2E_API_URL='http://127.0.0.1:5290'
$env:METBENCH_E2E_MR_ID='decay-chain-scale-initial'
dotnet test MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~SystemMtApiBusinessRuntimeMcpEndToEnd"
```

Expected result:

```text
Failed: 0, Passed: 1, Skipped: 0, Total: 1
```

Expected evidence fields:

```text
runtimeKind: Docker
runtimePassed: true
sourceRunId: non-blank
followupRunId: non-blank
```

Runtime MCP should show six successful `run_sut_command` calls: input parser parse, input parser write, source runner, follow-up runner, source output parser, and follow-up output parser.

## WSL Backend

Preconditions:

- `wsl.exe -l -v` lists an installed, running distribution.
- Runtime MCP is started inside that distribution and is reachable from the Windows host.
- The API `docker-mcp://` endpoint points to the WSL-hosted Runtime MCP URL.
- Runtime MCP config exposes `input-parser`, `output-parser`, and `sut-runner`.

Example target used for verification:

```powershell
wsl.exe -d MetBenchUbuntu2404Run -- bash -lc "python3 --version; test -d /mnt/d/Codes/MetBench-V2.1.4_2 && echo repo-ok; hostname -I"
```

WSL-side allowlisted wrappers live under `tmp/e2e-remote-wsl/`:

```bash
input-parser-wsl.sh
output-parser-wsl.sh
sut-runner-wsl.sh
```

Start Runtime MCP inside WSL:

```powershell
wsl.exe -d MetBenchUbuntu2404Run -- bash -lc "cd /mnt/d/Codes/MetBench-V2.1.4_2 && python3 infra/mcp/docker-runtime/server.py tmp/e2e-remote-wsl/runtime.local-wsl.json"
```

Required API runtime profile:

```powershell
$env:METBENCH_DB_PATH='D:\Codes\MetBench-V2.1.4_2\tmp\e2e-remote-wsl\api-data\SystemMT.Litedb'
$env:METBENCH_DOCKER_MCP_TOKEN='runtime-secret'
$env:LauncherOptions__SutRoot='D:\Codes\MetBench-V2.1.4_2\SUT'
$env:LauncherOptions__RuntimePythons__system='docker-mcp://system?image=wsl-metbench-remote&endpoint=http%3A%2F%2F127.0.0.1%3A8986&authTokenEnv=METBENCH_DOCKER_MCP_TOKEN'
dotnet run --project MetBench_Api --urls http://127.0.0.1:5291
```

Start Business MCP:

```powershell
python infra\mcp\metbench-business\server.py --config tmp\e2e-remote-wsl\business.wsl.json
```

Run E2E test:

```powershell
$env:METBENCH_E2E_BUSINESS_MCP_URL='http://127.0.0.1:8996'
$env:METBENCH_E2E_BUSINESS_MCP_TOKEN='business-secret'
$env:METBENCH_E2E_API_URL='http://127.0.0.1:5291'
$env:METBENCH_E2E_MR_ID='decay-chain-scale-initial'
dotnet test MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~SystemMtApiBusinessRuntimeMcpEndToEnd"
```

Expected result:

```text
Failed: 0, Passed: 1, Skipped: 0, Total: 1
```

If `wsl.exe -l -v` reports no installed distribution, record WSL as environment-blocked and do not claim a WSL E2E pass.
