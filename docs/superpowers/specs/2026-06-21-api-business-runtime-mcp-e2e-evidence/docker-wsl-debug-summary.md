# API + Business MCP + Runtime MCP E2E Evidence

Date: 2026-06-22
Branch: `codex/systemmt-api-mcp-control-plane`
Workspace: `D:\Codes\MetBench-V2.1.4_2`

## Docker Backend Result

Status: PASS.

Command:

```powershell
$env:METBENCH_E2E_BUSINESS_MCP_URL='http://127.0.0.1:8990'
$env:METBENCH_E2E_BUSINESS_MCP_TOKEN='business-secret'
$env:METBENCH_E2E_API_URL='http://127.0.0.1:5280'
$env:METBENCH_E2E_MR_ID='decay-chain-scale-initial'
dotnet test MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~SystemMtApiBusinessRuntimeMcpEndToEnd"
```

Result:

```text
Passed! - Failed: 0, Passed: 1, Skipped: 0, Total: 1, Duration: 3 s
```

Final job:

```text
jobId: 5955777a-0cf1-4f67-a368-3948fe079e4c
executionId: f51f2c29-e9ad-4f56-a608-c572697a013b
state: Succeeded
mrId: decay-chain-scale-initial
result: passed=true
sourceValue: 778.9331592717061
followUpValue: 1557.8663185434123
```

Runtime evidence:

```text
runtimeKind: Docker
runtimeKey: system
runtimePassed: true
failureKind: None
sourceRunId: 9d7208f51c174e6791cb991c2f9cb0ef
followupRunId: 8215b48d5cc449089aa60565c47c432f
```

Runtime MCP log:

```text
docker-runtime MCP server (docker backend) listening on http://127.0.0.1:8975
run_sut_command run_id=790d8e1719fe4a1e91199382f1bfa907 status=failed image=metbench-e2e-decay:latest returncode=1
run_sut_command run_id=9d7208f51c174e6791cb991c2f9cb0ef status=completed image=metbench-e2e-decay:latest returncode=0
run_sut_command run_id=8215b48d5cc449089aa60565c47c432f status=completed image=metbench-e2e-decay:latest returncode=0
```

The failed run id is the diagnostic attempt before `pathStyle=wsl` was added.

## Root Cause Found During Debugging

Two configuration issues prevented the first Docker E2E attempts from passing:

1. The API initially ran with the old profile because the child `MetBench_Api.exe` process still held port `5280` after stopping only the PowerShell wrapper.
2. The Docker runtime profile needed both local parser execution and container path translation:

```text
docker-mcp://system?image=metbench-e2e-decay%3Alatest&tool=decay-chain-runner&local=decay-chain-runner&python=python&endpoint=http%3A%2F%2F127.0.0.1%3A8975&authTokenEnv=METBENCH_DOCKER_MCP_TOKEN&pathStyle=wsl
```

Without `python=python`, `ParsingSource` tried to start `decay-chain-runner` locally.
Without `pathStyle=wsl`, the container received Windows paths such as `C:\Users\...` and could not open the input file.

## WSL Backend Result

Status: PASS.

Correction: the first WSL check was run from a restricted/sandboxed context and produced misleading output. A non-sandboxed check confirmed the host has WSL2 distributions installed:

```text
Ubuntu-24.04             Stopped 2
MetBenchUbuntu2404Run    Stopped 2
MetBenchUbuntu2404       Stopped 2
MetBenchUbuntu2404Web    Stopped 2
docker-desktop           Running 2
```

Target distro: `MetBenchUbuntu2404Run`.

WSL environment smoke:

```text
Python 3.12.3
repo-ok
hostname -I: 198.18.0.1 192.168.50.108
```

Runtime MCP:

```text
docker-runtime MCP server (local backend) listening on http://0.0.0.0:8976
run_sut_command run_id=bd18a635df7941ff8decd944fc3dacd0 status=completed image=wsl-metbench returncode=0
run_sut_command run_id=94bacb6b6d8c4d60a7fac255064c89d1 status=completed image=wsl-metbench returncode=0
```

Command:

```powershell
$env:METBENCH_E2E_BUSINESS_MCP_URL='http://127.0.0.1:8991'
$env:METBENCH_E2E_BUSINESS_MCP_TOKEN='business-secret'
$env:METBENCH_E2E_API_URL='http://127.0.0.1:5281'
$env:METBENCH_E2E_MR_ID='decay-chain-scale-initial'
dotnet test MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~SystemMtApiBusinessRuntimeMcpEndToEnd"
```

Result:

```text
Passed! - Failed: 0, Passed: 1, Skipped: 0, Total: 1, Duration: 1 s
```

Final job:

```text
jobId: 1c44fd59-0bc1-4452-98e5-2e024aaf862d
executionId: 0f71863f-0315-42ba-9694-d594985d7e1e
state: Succeeded
mrId: decay-chain-scale-initial
result: passed=true
sourceValue: 778.9331592717061
followUpValue: 1557.8663185434123
```

Runtime evidence:

```text
runtimeKind: Docker
runtimeKey: system
runtimePassed: true
failureKind: None
sourceRunId: bd18a635df7941ff8decd944fc3dacd0
followupRunId: 94bacb6b6d8c4d60a7fac255064c89d1
```

Note: `runtimeKind` remains `Docker` because the C# runtime profile uses the existing `docker-mcp://` adapter contract. The remote backend used by Runtime MCP in this WSL pass is `local`.

## Remote Tool Mode Re-run (2026-06-22)

Status: PASS for both Docker backend and WSL backend.

Purpose: verify the revised design where all external calls are Runtime MCP tool invocations and the API runtime profile does not pass `python`, `local`, or `pathStyle`. The only runtime profile fields used were `image`, `endpoint`, and `authTokenEnv`.

### Docker Remote Tool Backend

Profile:

```text
docker-mcp://system?image=metbench-e2e-remote-decay%3Alatest&endpoint=http%3A%2F%2F127.0.0.1%3A8985&authTokenEnv=METBENCH_DOCKER_MCP_TOKEN
```

Command:

```powershell
$env:METBENCH_E2E_BUSINESS_MCP_URL='http://127.0.0.1:8995'
$env:METBENCH_E2E_BUSINESS_MCP_TOKEN='business-secret'
$env:METBENCH_E2E_API_URL='http://127.0.0.1:5290'
$env:METBENCH_E2E_MR_ID='decay-chain-scale-initial'
dotnet test MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~SystemMtApiBusinessRuntimeMcpEndToEnd"
```

Result:

```text
Passed! - Failed: 0, Passed: 1, Skipped: 0, Total: 1, Duration: 6 s
```

Job/evidence:

```text
jobId: 5a8adaaa-eead-4ce0-8b9a-c7612d0f32a3
executionId: e99a2f44-24ac-49c5-a1a0-f2ea620f75f0
mrId: decay-chain-scale-initial
passed: true
sourceValue: 778.9331592717061
followUpValue: 1557.8663185434123
runtimeKind: Docker
runtimeKey: system
runtimePassed: true
failureKind: None
sourceRunId: 42879bc79baf4306bbe58a1a5f4faa57
followupRunId: 883cc197dbec4fe6af9dc9f4d483052c
```

Runtime MCP log showed six successful tool calls, proving parser/write/runner/output-parser all went through Runtime MCP:

```text
abcb62f1171a4152b29de291d7e8a192
fa3d9cad31ba4e29916f917fc0f9ce2d
42879bc79baf4306bbe58a1a5f4faa57
883cc197dbec4fe6af9dc9f4d483052c
cf710bc6051c4d80b22e5f3bfa8b74b8
cce29ca438b349368303704f10b7ea6a
```

### WSL Remote Tool Backend

Profile:

```text
docker-mcp://system?image=wsl-metbench-remote&endpoint=http%3A%2F%2F127.0.0.1%3A8986&authTokenEnv=METBENCH_DOCKER_MCP_TOKEN
```

Command:

```powershell
$env:METBENCH_E2E_BUSINESS_MCP_URL='http://127.0.0.1:8996'
$env:METBENCH_E2E_BUSINESS_MCP_TOKEN='business-secret'
$env:METBENCH_E2E_API_URL='http://127.0.0.1:5291'
$env:METBENCH_E2E_MR_ID='decay-chain-scale-initial'
dotnet test MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~SystemMtApiBusinessRuntimeMcpEndToEnd"
```

Result:

```text
Passed! - Failed: 0, Passed: 1, Skipped: 0, Total: 1, Duration: 1 s
```

Job/evidence:

```text
jobId: ba597dbd-a821-4185-a00d-2f3b664f8894
executionId: 266066e8-b3f8-4dc6-ac7a-7608ff67755e
mrId: decay-chain-scale-initial
passed: true
sourceValue: 778.9331592717061
followUpValue: 1557.8663185434123
runtimeKind: Docker
runtimeKey: system
runtimePassed: true
failureKind: None
sourceRunId: ad0c12539bc4491c81f95e4a2b635431
followupRunId: 2befb34cc3e8446a9848a7dd1da7bcad
```

Runtime MCP log showed six successful tool calls:

```text
12227054f7b843909c1fe35ce6edd536
72d5827b01f7428497eab9d724d7e04d
ad0c12539bc4491c81f95e4a2b635431
2befb34cc3e8446a9848a7dd1da7bcad
51cd57e903254e69b5e9b1b0bcbe3b59
db32ddfd9a494919b7468ab81690706b
```
