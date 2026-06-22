# Unified Runtime Tool Invocation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Treat parser, writer, SUT runner, and output parser calls as Runtime backend tool invocations; local execution becomes a Runtime backend variant instead of a separate pipeline path.

**Architecture:** This is the first vertical slice of the remote-async model. A `docker-mcp://` profile without legacy `tool/local/python/pathStyle` enters remote tool mode: launcher emits stable tool ids (`input-parser`, `sut-runner`, `output-parser`), pipeline routes every external tool call through `IRuntimeProcessExecutor`, and Runtime MCP owns host-path translation before invoking allowlisted backend tools. Existing `tool/local/python/pathStyle` profiles remain a legacy compatibility path.

**Tech Stack:** .NET 8, xUnit, Python 3 stdlib `unittest`, existing Runtime MCP JSON-over-HTTP server.

---

## File Structure

- Modify `infra/mcp/docker-runtime/server.py`: translate allowed absolute Windows data-path arguments into backend-visible paths before building local/docker commands.
- Modify `infra/mcp/docker-runtime/tests/test_server.py`: add tests proving path translation is owned by Runtime MCP.
- Modify `MetBench_BLL.Core/SystemMT/Runtime/LauncherOptionsRuntimeProfileProvider.cs`: allow remote tool mode profiles that omit `tool`, `local`, and `python`.
- Modify `MetBench_BLL.Core/SystemMT/Runtime/DockerMcpProcessExecutor.cs`: when no legacy `ToolName` is configured, use `ProcessInvocation.FileName` as the Runtime MCP tool id.
- Modify `MetBench_BLL.Core/SystemMT/Launcher/SystemMtLauncher.cs`: for remote tool mode, emit tool-id invocations for parser, runner, and output parser.
- Modify `MetBench_BLL.Core/SystemMT/Pipeline/SystemMtPipeline.cs`: route all external invocations through runtime only in remote tool mode.
- Add/modify tests under `MetBench_SystemMT.Tests/SystemMT/Runtime`, `SystemMT/Launcher`, and `V2Pipeline`.
- Update `docs/uat/api-business-runtime-mcp-e2e-runbook.md` after validation.

## Task 1: Runtime MCP Owns Path Translation

- [x] Write failing Python tests in `infra/mcp/docker-runtime/tests/test_server.py`:
  - local backend with Linux/WSL `repo_root` converts `C:\Users\...\source.in.json` to `/mnt/c/Users/.../source.in.json`.
  - relative flags such as `--input` and relative files stay unchanged.
- [x] Run focused Python test and confirm RED.
- [x] Update `validate_run_request` so it returns translated allowed absolute data-path args.
- [x] Re-run Python tests and confirm GREEN.

## Task 2: C# Runtime MCP Executor Supports Tool Id Mode

- [x] Write failing test in `DockerMcpProcessExecutorTests`:
  - `DockerMcpRuntimeOptions.ToolName == ""`.
  - invocation `FileName == "input-parser"`.
  - fake client receives `DockerMcpRunRequest.Tool == "input-parser"`.
  - no `LocalExecutable` validation is required in this mode.
- [x] Run Docker MCP executor focused tests and confirm RED.
- [x] Update `DockerMcpProcessExecutor.RunAsync`: legacy mode still validates `LocalExecutable`; remote tool mode uses invocation file name as tool id.
- [x] Re-run focused tests and confirm GREEN.

## Task 3: Launcher Emits Remote Tool Id Invocations

- [x] Write failing launcher/profile tests:
  - runtime profile value: `docker-mcp://system?image=wsl-metbench&endpoint=http%3A%2F%2F127.0.0.1%3A8976&authTokenEnv=METBENCH_DOCKER_MCP_TOKEN`
  - created runtime profile has Docker options with blank `ToolName`, blank `LocalExecutable`, blank `PythonExecutable`.
  - launcher context for a Docker profile uses `input-parser`, `sut-runner`, and `output-parser` as invocation file names.
- [x] Run focused launcher/profile tests and confirm RED.
- [x] Relax `LauncherOptionsRuntimeProfileProvider` required query fields for remote tool mode.
- [x] Update `SystemMtLauncher` remote profile branch to emit tool ids.
- [x] Re-run focused tests and confirm GREEN.

## Task 4: Pipeline Routes Every External Tool Through Runtime

- [x] Write failing pipeline test using Runtime MCP fake client and a failing `IProcessExecutor`.
  - The test executes one two-sided MR.
  - It asserts the runtime executor saw `input-parser parse`, `input-parser write`, two `sut-runner` calls, and two `output-parser parse` calls.
  - It asserts the outcome is `Ok` and source/followup runtime run ids are recorded.
- [x] Run focused pipeline tests and confirm RED.
- [x] Add `UseRemoteToolRuntime(ctx)` helper and route parser/writer/output parser through runtime only for remote tool mode.
- [x] Re-run focused tests and confirm GREEN.

## Task 5: Docker/WSL E2E Acceptance

- [x] Update temporary Docker/WSL Runtime MCP configs to expose `input-parser`, `sut-runner`, and `output-parser` allowlisted tools.
- [x] Run Docker E2E without `python`, `local`, or `pathStyle` in the API runtime profile.
- [x] Run WSL E2E without `python`, `local`, or `pathStyle` in the API runtime profile.
- [x] Save job ids, result payloads, evidence payloads, and runtime run ids in `docs/superpowers/specs/2026-06-21-api-business-runtime-mcp-e2e-evidence/`.

## Final Verification

- [x] `python -m unittest discover infra\mcp\docker-runtime\tests` — 46 pass.
- [x] `python -m unittest discover infra\mcp\metbench-business\tests` — 8 pass.
- [x] Focused .NET tests covering DockerMcp, launcher, pipeline, API, JobWorker, DbConfig, and E2E gate — 47 pass; 32 pass / 1 env-gated E2E skip when env vars are absent. Real Docker/WSL E2E ran separately and passed.
- [x] `dotnet build MetBench_BLL.Core\MetBench_BLL.Core.csproj --no-restore` — 0 warnings / 0 errors.
- [x] `git diff --check` — exit 0; only line-ending warnings from Git on Windows.
