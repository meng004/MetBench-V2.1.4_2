# MetBench Docker Runtime MCP Implementation Plan

Date: 2026-06-12

Spec: `docs/superpowers/specs/2026-06-12-metbench-docker-runtime-mcp-design.md`

## Goal

Create a Docker-backed runtime MCP server for LAN Codex callers and activate a
Docker runtime backend in the MetBench System MT launcher/pipeline path.

## Phase A: Docker Runtime MCP Server

1. Add configuration validation and LAN bind-host handling.
2. Add authenticated tool dispatch for `runtime_health`, image listing/building,
   SUT command execution, and stored run-result lookup.
3. Add unit tests covering config shape, auth, allowlisted images, generated
   Docker commands, service-generated run IDs, malformed HTTP payloads, and SUT
   `--input` / `--output` arguments.

## Phase B: MetBench Docker Runtime Backend

1. Add Docker runtime profile parsing for `docker-mcp://` entries in
   `LauncherOptions.RuntimePythons`.
2. Add Docker MCP runtime client and preflight integration.
3. Add Docker MCP process executor.
4. Carry resolved `RuntimeProfile` through `PipelineContext`.
5. Route only SUT runner commands through Docker MCP; keep parser/adapter
   subprocesses local.
6. Add launcher tests proving Docker profile metadata reaches preflight and
   pipeline, and that generated commands use container Python instead of the
   `docker-mcp://` URI.

## Verification

- `rtk python3 -m unittest discover infra/mcp/docker-runtime/tests`
- `rtk dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~DockerMcpRuntimeProfileTests|FullyQualifiedName~DockerMcpRuntimeClientTests|FullyQualifiedName~DockerMcpProcessExecutorTests|FullyQualifiedName~RuntimePreflightServiceTests|FullyQualifiedName~RuntimePreflightLauncherTests|FullyQualifiedName~SystemMtPipelineTests.Docker_runtime_profile_routes_only_sut_runner_through_mcp|FullyQualifiedName~RuntimeProfileProviderTests"`
- `rtk dotnet build MetBench_BLL.Core/MetBench_BLL.Core.csproj --no-restore`
- `rtk git diff --check`

## PR Gate Classification

- Scope: one primary purpose, Docker runtime MCP/backend.
- Windows classification: no Windows evidence required.
- Status projection: update `docs/status/current.md` and active plan index.
