# System MT Control Plane Architecture Guards Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Enforce the System MT architecture philosophy by preventing control-plane/runtime-plane drift and preparing a safe API implementation path.

**Architecture:** Keep `SystemMtPipeline` as the MT workflow owner while moving runtime backend selection behind an executor registry. Add artifact access as a service so future API/MCP adapters never expose raw host paths. Write an API implementation plan that builds REST/Business MCP as adapters over the same control-plane service.

**Tech Stack:** .NET 8, xUnit, existing `MetBench_BLL.Core/SystemMT/*`, `MetBench_SystemMT.Tests`.

---

### Task 1: Runtime Executor Registry

**Files:**
- Create: `MetBench_BLL.Core/SystemMT/Runtime/IRuntimeProcessExecutor.cs`
- Create: `MetBench_BLL.Core/SystemMT/Runtime/LocalRuntimeProcessExecutor.cs`
- Create: `MetBench_BLL.Core/SystemMT/Runtime/DockerRuntimeProcessExecutor.cs`
- Create: `MetBench_BLL.Core/SystemMT/Runtime/RuntimeProcessExecutorRegistry.cs`
- Modify: `MetBench_BLL.Core/SystemMT/Pipeline/SystemMtPipeline.cs`
- Test: `MetBench_SystemMT.Tests/SystemMT/Runtime/RuntimeProcessExecutorRegistryTests.cs`

- [x] **Step 1: Write failing tests**
  - Test registry dispatches local profiles to an `IProcessExecutor`.
  - Test registry dispatches Docker profiles to Docker MCP executor.
  - Test registry fails closed for unsupported executable runtime kinds.
- [x] **Step 2: Run focused tests and confirm RED**
  - `rtk dotnet test MetBench_SystemMT.Tests --no-restore --filter "FullyQualifiedName~RuntimeProcessExecutorRegistry"`
- [x] **Step 3: Implement minimal registry and pipeline integration**
  - `SystemMtPipeline` must depend on `IRuntimeProcessExecutor` and no longer hold `DockerMcpProcessExecutor` directly.
- [x] **Step 4: Run focused tests and existing Docker/pipeline tests**
  - `rtk dotnet test MetBench_SystemMT.Tests --no-restore --filter "FullyQualifiedName~RuntimeProcessExecutorRegistry|FullyQualifiedName~DockerMcpProcessExecutor|FullyQualifiedName~SystemMtPipelineTests|FullyQualifiedName~MultiPhasePipelineTests"`

### Task 2: Artifact Access Service

**Files:**
- Create: `MetBench_BLL.Core/SystemMT/Artifacts/ArtifactAccessModels.cs`
- Create: `MetBench_BLL.Core/SystemMT/Artifacts/ISystemMtArtifactAccessService.cs`
- Create: `MetBench_BLL.Core/SystemMT/Artifacts/SystemMtArtifactAccessService.cs`
- Test: `MetBench_SystemMT.Tests/SystemMT/Artifacts/SystemMtArtifactAccessServiceTests.cs`

- [x] **Step 1: Write failing tests**
  - Reject paths outside configured root.
  - Reject path traversal and missing manifest/file.
  - Return safe artifact id/name/length/content type without exposing absolute paths.
- [x] **Step 2: Run focused tests and confirm RED**
  - `rtk dotnet test MetBench_SystemMT.Tests --no-restore --filter "FullyQualifiedName~SystemMtArtifactAccessService"`
- [x] **Step 3: Implement minimal service**
  - Service accepts an artifact root allowlist and resolves only manifest-listed files.
- [x] **Step 4: Run focused tests**
  - `rtk dotnet test MetBench_SystemMT.Tests --no-restore --filter "FullyQualifiedName~SystemMtArtifactAccessService"`

### Task 3: Architecture Guard Tests

**Files:**
- Create or modify: `MetBench_SystemMT.Tests/SystemMT/Architecture/SystemMtControlPlaneBoundaryTests.cs`

- [x] **Step 1: Write failing/guard tests**
  - Guard that future `MetBench_Api` and Business MCP adapter paths do not directly reference `DockerMcpRuntimeClient`, `DockerMcpProcessExecutor`, or `run_sut_command`.
  - Guard that `SystemMtPipeline` does not directly reference `DockerMcpProcessExecutor`.
  - Guard that API DTO paths, when introduced, must not expose `PackageRoot`, `StagingRoot`, `ExportRoot`, or raw `ArtifactPath`.
- [x] **Step 2: Run guard tests**
  - `rtk dotnet test MetBench_SystemMT.Tests --no-restore --filter "FullyQualifiedName~SystemMtControlPlaneBoundaryTests"`

### Task 4: API Development Plan

**Files:**
- Create: `docs/superpowers/plans/2026-06-20-systemmt-api-control-plane-plan.md`

- [x] **Step 1: Write the API implementation plan**
  - Include shared DI, control-plane service, REST API adapter, Business MCP adapter, artifact service integration, auth, DTO validation, tests, and docs gates.
- [x] **Step 2: Verify plan references existing paths and does not prescribe bypassing workflow**
  - `rtk grep -n "SystemMtJobService\|SystemMtLauncher\|SystemMtPipeline\|Business MCP\|Runtime MCP" docs/superpowers/plans/2026-06-20-systemmt-api-control-plane-plan.md`

### Final Verification

- [x] `rtk dotnet build MetBench_BLL.Core/MetBench_BLL.Core.csproj --no-restore`
- [x] `rtk dotnet test MetBench_SystemMT.Tests --no-restore --filter "FullyQualifiedName~RuntimeProcessExecutorRegistry|FullyQualifiedName~SystemMtArtifactAccessService|FullyQualifiedName~SystemMtControlPlaneBoundaryTests"`
- [x] `rtk git diff --check`
