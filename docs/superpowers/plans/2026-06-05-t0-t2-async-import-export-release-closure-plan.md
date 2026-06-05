# T0-T2 Async Import/Export Release Closure Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Close T0, T1, and T2 for release by making System-MT user-visible long-running operations asynchronous and by adding async import/export for assets plus export for results/evidence/reports.

**Architecture:** Extend the existing `SystemMtJobService -> SystemMtJobWorker -> SystemMtAsyncPipeline -> ISystemMtLauncher` path with operation-kind job requests and small operation handlers. Keep synchronous launcher/report/import helpers as internal compatibility paths; WPF becomes an async submit/poll/cancel/artifact viewer.

**Tech Stack:** .NET 8, WPF, LiteDB, xUnit, Reqnroll, existing `MetBench_BLL.Core/SystemMT/Jobs`, existing `MetBench_BLL.Core/SystemMT/ImportExport/Put`, existing reporting and `ExecutionEvidence` repositories.

---

## Scope, PR Chain, and Branching

This closure is intentionally split into a checkpointed PR chain. Do not ship it as one large implementation PR.

| PR | Scope | Controlled claim allowed? |
|---|---|---|
| PR-0 | Register corrected design/plan, active-index row, and initial status row as Planned. | No. |
| PR-1 | Async job request model, operation dispatch, and batch-run semantics. | No; mark as implemented evidence only. |
| PR-2 | Async asset import/export with real staged import artifacts. | No; mark as implemented evidence only. |
| PR-3 | Async execution/evidence/report export and no-import guard. | No; mark as implemented evidence only. |
| PR-4 | WPF async operations surface plus VM prompt/evidence. | No; mark as release-candidate evidence only. |
| PR-5 | Post-merge status/projection closure after PR-1..PR-4 are merged and fetched on `origin/main`. | Yes, if evidence is complete. |

After every implementation PR task, run a code-review checkpoint before starting the next PR. Critical and Important findings must be fixed or explicitly rejected with code/test evidence.

Use one release-closure branch per PR, branched from current `origin/main`, for example:

```bash
rtk git fetch origin
rtk git switch -c t0-t2-async-import-export-pr1-job-model origin/main
```

If local git cannot create a branch because `.git` refs are permission-blocked, stop and report the blocker. Do not implement on a dirty or stale worktree.

## File Structure

Expected cloud-side files:

- Modify: `MetBench_BLL.Core/SystemMT/Jobs/SystemMtJobRequest.cs` or add `MetBench_BLL.Core/SystemMT/Jobs/SystemMtOperationJobRequest.cs`
- Modify: `MetBench_BLL.Core/SystemMT/Jobs/SystemMtJobRecord.cs`
- Modify: `MetBench_BLL.Core/SystemMT/Jobs/SystemMtJobStatus.cs`
- Modify: `MetBench_BLL.Core/SystemMT/Jobs/SystemMtJobService.cs`
- Modify: `MetBench_BLL.Core/SystemMT/Jobs/SystemMtJobWorker.cs`
- Modify: `MetBench_BLL.Core/SystemMT/Jobs/SystemMtAsyncPipeline.cs`
- Create: `MetBench_BLL.Core/SystemMT/Jobs/Operations/SystemMtJobKind.cs`
- Create: `MetBench_BLL.Core/SystemMT/Jobs/Operations/ISystemMtJobOperationHandler.cs`
- Create: `MetBench_BLL.Core/SystemMT/Jobs/Operations/RunMrJobOperationHandler.cs`
- Create: `MetBench_BLL.Core/SystemMT/Jobs/Operations/RunBatchJobOperationHandler.cs`
- Create: `MetBench_BLL.Core/SystemMT/Jobs/Operations/ImportAssetsJobOperationHandler.cs`
- Create: `MetBench_BLL.Core/SystemMT/Jobs/Operations/ExportAssetsJobOperationHandler.cs`
- Create: `MetBench_BLL.Core/SystemMT/Jobs/Operations/ExportExecutionArtifactsJobOperationHandler.cs`
- Create: `MetBench_BLL.Core/SystemMT/ImportExport/Put/SutImportStagingService.cs`
- Create: `MetBench_BLL.Core/SystemMT/ImportExport/Put/SutImportStagingManifest.cs`
- Create: `MetBench_BLL.Core/SystemMT/ImportExport/ExecutionArtifacts/ExecutionArtifactExportRequest.cs`
- Create: `MetBench_BLL.Core/SystemMT/ImportExport/ExecutionArtifacts/ExecutionArtifactExportManifest.cs`
- Create: `MetBench_BLL.Core/SystemMT/ImportExport/ExecutionArtifacts/ExecutionArtifactExporter.cs`
- Modify or add tests under `MetBench_SystemMT.Tests/SystemMT/Jobs/`
- Modify or add tests under `MetBench_SystemMT.Tests/SystemMT/ImportExport/`
- Modify docs listed in the final docs task.

Expected VM/WPF files:

- Modify: `MetBench_Client/ViewModels/SystemMtAsyncJobViewModel.cs`
- Modify: `MetBench_Client/Views/Pages/SystemMtAsyncJobPage.xaml`
- Modify: `MetBench_Client/App.xaml.cs`
- Modify or add: `MetBench_Client.Tests/ClientI18n/*.cs` only if UI text/resources change and the VM can run them.
- Create: `docs/superpowers/vm-prompts/2026-06-05-t0-t2-async-import-export-release-closure-vm-prompt.md`

## Task 0: Code Discovery Before Any Implementation

**Files:**
- Modify only this plan if the discovery finds mismatched APIs.
- Do not edit production code in this task.

- [ ] **Step 1: Inventory actual async job APIs**

Run:

```bash
rtk rg -n "interface ISystemMtJobService|class SystemMtJobService|record SystemMtJobRequest|class SystemMtJobWorker|record SystemMtJobRecord|record SystemMtJobStatus|class SystemMtAsyncPipeline|record SystemMtJobProgress" MetBench_BLL.Core/SystemMT/Jobs MetBench_SystemMT.Tests/SystemMT/Jobs
```

Expected: output lists the current method signatures and record fields. Paste the relevant signatures into the PR notes.

- [ ] **Step 2: Inventory actual reporting and persistence APIs**

Run:

```bash
rtk rg -n "interface ISystemMtResultRepository|class SystemMtReportService|interface ISystemMtResultReportRenderer|interface IExecutionEvidenceRepository|class LiteDbExecutionEvidenceRepository|GenerateExecution|Render\\(" MetBench_BLL.Core MetBench_DAL MetBench_BLL MetBench_SystemMT.Tests
```

Expected: output identifies the exact methods to call for Task 4. If method names differ from snippets in this plan, edit this plan in PR-0 before implementation.

- [ ] **Step 3: Inventory actual import/export APIs**

Run:

```bash
rtk rg -n "record SutImportUnit|class SutImportValidator|class SutImportPackageExporter|AGroupPutImportExportTests|BGroupPutImportExportTests" MetBench_BLL.Core/SystemMT/ImportExport MetBench_SystemMT.Tests/SystemMT/ImportExport
```

Expected: output proves the existing PUT package model and test fixture locations.

- [ ] **Step 4: Record discovery evidence**

Add a short "Discovery evidence" subsection to the PR body or status note. Do not claim implementation completion from discovery.

## Task 1: Register the Plan and Guard the Scope

**Files:**
- Modify: `docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md`
- Modify: `docs/status/current.md`

- [ ] **Step 1: Add active plan row**

Add a row to active plan index §1:

```markdown
| `docs/superpowers/plans/2026-06-05-t0-t2-async-import-export-release-closure-plan.md` | Active | Release closure for T0/T1/T2: System-MT and WPF user-visible long-running operations move to async submit/poll/cancel; asset import/export runs as async jobs; execution/evidence/report export runs as async jobs; result/evidence import remains out of scope. | Expires when cloud code, VM evidence, docs projection, user guide screenshots, PR merge, and branch cleanup are complete |
```

- [ ] **Step 2: Add status-ledger planning row**

Add a Stage 8 row in `docs/status/current.md`. It must be `Planned`, not `Controlled`:

```markdown
| T0-T2 async import/export release closure | Planned | Scope locked by `docs/superpowers/specs/2026-06-05-t0-t2-async-import-export-release-closure-design.md`; implementation plan is `docs/superpowers/plans/2026-06-05-t0-t2-async-import-export-release-closure-plan.md`. Result/evidence import is out of scope until a trust model exists. This row must not move to Controlled until a post-merge closure PR records merge commits, fetched `origin/main`, required checks, VM evidence, docs projections, and branch cleanup. |
```

- [ ] **Step 3: Verify docs-only scope**

Run:

```bash
rtk git diff --check
rtk git diff --name-only
```

Expected:

- `git diff --check` exits 0.
- Diff contains only docs files for this task.

- [ ] **Step 4: Commit**

```bash
rtk git diff --name-only
rtk git add docs/status/current.md docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md docs/superpowers/specs/2026-06-05-t0-t2-async-import-export-release-closure-design.md docs/superpowers/plans/2026-06-05-t0-t2-async-import-export-release-closure-plan.md
rtk git commit -m "docs(plan): register T0-T2 async import export closure"
```

Expected: commit succeeds.

## Task 2: Generalize Job Requests Without Breaking MR Run Compatibility

**Files:**
- Create: `MetBench_BLL.Core/SystemMT/Jobs/Operations/SystemMtJobKind.cs`
- Create: `MetBench_BLL.Core/SystemMT/Jobs/SystemMtOperationJobRequest.cs`
- Modify: `MetBench_BLL.Core/SystemMT/Jobs/SystemMtJobRequest.cs`
- Modify: `MetBench_BLL.Core/SystemMT/Jobs/SystemMtJobRecord.cs`
- Modify: `MetBench_BLL.Core/SystemMT/Jobs/SystemMtJobStatus.cs`
- Modify: `MetBench_BLL.Core/SystemMT/Jobs/SystemMtJobService.cs`
- Test: `MetBench_SystemMT.Tests/SystemMT/Jobs/SystemMtJobServiceTests.cs`

- [ ] **Step 1: Write failing compatibility and operation-kind tests**

Add tests:

```csharp
[Fact]
public async Task SubmitAsync_old_mr_request_persists_run_mr_job_kind()
{
    var store = new InMemoryJobStore();
    var queue = new ChannelJobQueue();
    var service = new SystemMtJobService(store, queue, utcNow: () => new DateTime(2026, 6, 5, 0, 0, 0, DateTimeKind.Utc));

    var handle = await service.SubmitAsync(new SystemMtJobRequest("advection-amplitude-linearity"));

    var status = await service.GetStatusAsync(handle.JobId);
    Assert.NotNull(status);
    Assert.Equal(SystemMtJobKind.RunMr, status!.Kind);
    Assert.Equal("advection-amplitude-linearity", status.MrId);
}

[Fact]
public async Task SubmitOperationAsync_rejects_export_without_export_root()
{
    var service = new SystemMtJobService(new InMemoryJobStore(), new ChannelJobQueue());

    var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
        service.SubmitOperationAsync(new SystemMtOperationJobRequest(
            SystemMtJobKind.ExportAssets,
            MrId: null,
            MrIds: null,
            PackageRoot: "/tmp/pkg",
            ExportRoot: null,
            ExecutionId: null,
            ParameterOverrides: null)));

    Assert.Contains("ExportRoot", ex.Message, StringComparison.Ordinal);
}
```

- [ ] **Step 2: Run failing tests**

```bash
rtk dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~SystemMtJobServiceTests"
```

Expected: fails because `SystemMtJobKind`, `SystemMtOperationJobRequest`, `SubmitOperationAsync`, and `SystemMtJobStatus.Kind` do not exist yet.

- [ ] **Step 3: Implement minimal request model**

Create:

```csharp
namespace MetBench_BLL.SystemMT.Jobs;

public enum SystemMtJobKind
{
    RunMr,
    RunBatch,
    ImportAssets,
    ExportAssets,
    ExportExecutionArtifacts
}
```

Create:

```csharp
namespace MetBench_BLL.SystemMT.Jobs;

public sealed record SystemMtOperationJobRequest(
    SystemMtJobKind Kind,
    string? MrId = null,
    IReadOnlyList<string>? MrIds = null,
    string? PackageRoot = null,
    string? StagingRoot = null,
    string? ExportRoot = null,
    Guid? ExecutionId = null,
    IReadOnlyDictionary<string, string>? ParameterOverrides = null);
```

Add nullable/default fields to `SystemMtJobRecord` and `SystemMtJobStatus`:

```csharp
public SystemMtJobKind Kind { get; init; } = SystemMtJobKind.RunMr;
public string? PackageRoot { get; init; }
public string? StagingRoot { get; init; }
public string? ExportRoot { get; init; }
public Guid? ExecutionId { get; init; }
public string? ArtifactPath { get; init; }
```

Make `ToStatus()` project those fields.

- [ ] **Step 4: Implement `SubmitOperationAsync`**

In `SystemMtJobService`, add:

```csharp
public Task<SystemMtJobHandle> SubmitAsync(SystemMtJobRequest request, CancellationToken cancellationToken = default)
{
    if (request is null) throw new ArgumentNullException(nameof(request));
    return SubmitOperationAsync(new SystemMtOperationJobRequest(
        SystemMtJobKind.RunMr,
        MrId: request.MrId,
        ParameterOverrides: request.ParameterOverrides), cancellationToken);
}

public async Task<SystemMtJobHandle> SubmitOperationAsync(
    SystemMtOperationJobRequest request,
    CancellationToken cancellationToken = default)
{
    if (request is null) throw new ArgumentNullException(nameof(request));
    ValidateOperationRequest(request);

    var now = _utcNow();
    var id = Guid.NewGuid();
    var record = new SystemMtJobRecord
    {
        JobId = id,
        Kind = request.Kind,
        MrId = request.MrId ?? string.Empty,
        SutName = string.Empty,
        State = SystemMtJobState.Queued,
        CurrentPhase = "queued",
        ProgressPercent = 0,
        CreatedAtUtc = now,
        UpdatedAtUtc = now,
        PackageRoot = request.PackageRoot,
        StagingRoot = request.StagingRoot,
        ExportRoot = request.ExportRoot,
        ExecutionId = request.ExecutionId,
    };

    await _store.CreateAsync(record, cancellationToken);
    try
    {
        await _queue.EnqueueAsync(id, cancellationToken);
    }
    catch
    {
        var failedAt = _utcNow();
        await _store.UpdateStatusAsync(record with
        {
            State = SystemMtJobState.Failed,
            FailureReason = "failed to enqueue job for execution",
            CurrentPhase = "failed",
            UpdatedAtUtc = failedAt,
            FinishedAtUtc = failedAt,
        }, CancellationToken.None);
        throw;
    }

    return new SystemMtJobHandle(id, now);
}

private static void ValidateOperationRequest(SystemMtOperationJobRequest request)
{
    if (request.Kind == SystemMtJobKind.RunMr && string.IsNullOrWhiteSpace(request.MrId))
        throw new ArgumentException("MrId must be non-blank for RunMr jobs.", nameof(request));
    if (request.Kind == SystemMtJobKind.RunBatch && (request.MrIds is null || request.MrIds.Count == 0))
        throw new ArgumentException("MrIds must contain at least one MR id for RunBatch jobs.", nameof(request));
    if (request.Kind == SystemMtJobKind.ImportAssets && (string.IsNullOrWhiteSpace(request.PackageRoot) || string.IsNullOrWhiteSpace(request.StagingRoot)))
        throw new ArgumentException("PackageRoot and StagingRoot must be non-blank for ImportAssets jobs.", nameof(request));
    if (request.Kind == SystemMtJobKind.ExportAssets && (string.IsNullOrWhiteSpace(request.PackageRoot) || string.IsNullOrWhiteSpace(request.ExportRoot)))
        throw new ArgumentException("PackageRoot and ExportRoot must be non-blank for ExportAssets jobs.", nameof(request));
    if (request.Kind == SystemMtJobKind.ExportExecutionArtifacts && (request.ExecutionId is null || string.IsNullOrWhiteSpace(request.ExportRoot)))
        throw new ArgumentException("ExecutionId and ExportRoot must be provided for ExportExecutionArtifacts jobs.", nameof(request));
}
```

- [ ] **Step 5: Run tests**

```bash
rtk dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~SystemMtJobServiceTests"
```

Expected: `SystemMtJobServiceTests` pass.

- [ ] **Step 6: Commit**

```bash
rtk git diff --name-only
rtk git add MetBench_BLL.Core/SystemMT/Jobs/Operations/SystemMtJobKind.cs MetBench_BLL.Core/SystemMT/Jobs/SystemMtOperationJobRequest.cs MetBench_BLL.Core/SystemMT/Jobs/SystemMtJobRequest.cs MetBench_BLL.Core/SystemMT/Jobs/SystemMtJobRecord.cs MetBench_BLL.Core/SystemMT/Jobs/SystemMtJobStatus.cs MetBench_BLL.Core/SystemMT/Jobs/SystemMtJobService.cs MetBench_SystemMT.Tests/SystemMT/Jobs/SystemMtJobServiceTests.cs
rtk git commit -m "feat(systemmt): generalize async job requests"
```

## Task 2B: Add Batch Run Semantics

**Files:**
- Create: `MetBench_BLL.Core/SystemMT/Jobs/Operations/RunBatchJobOperationHandler.cs`
- Modify: `MetBench_BLL.Core/SystemMT/Jobs/SystemMtJobRecord.cs`
- Modify: `MetBench_BLL.Core/SystemMT/Jobs/SystemMtJobStatus.cs`
- Test: `MetBench_SystemMT.Tests/SystemMT/Jobs/RunBatchJobOperationHandlerTests.cs`

- [ ] **Step 1: Write failing batch tests**

Add tests for:

- two MR ids both pass -> batch job `Succeeded`;
- first MR pass, second MR fail -> batch job `Failed` with per-MR summary preserving both outcomes;
- cancellation before second MR starts -> batch job `Cancelled`;
- empty MR id list rejected by `SubmitOperationAsync`.

Run:

```bash
rtk dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~RunBatchJobOperationHandlerTests|FullyQualifiedName~SystemMtJobServiceTests"
```

Expected: fails because batch handler and per-MR summary fields do not exist yet.

- [ ] **Step 2: Implement minimal batch summary records**

Add records under `MetBench_BLL.Core/SystemMT/Jobs/Operations/`:

```csharp
public sealed record SystemMtBatchItemStatus(
    string MrId,
    SystemMtJobState State,
    Guid? ExecutionId,
    string? FailureReason);
```

Add `IReadOnlyList<SystemMtBatchItemStatus> BatchItems` to `SystemMtJobRecord` and `SystemMtJobStatus`, defaulting to an empty array.

- [ ] **Step 3: Implement batch handler**

`RunBatchJobOperationHandler` must execute MR ids sequentially through the same run-MR path used by single-run jobs. It must report progress after each MR, preserve per-MR outcomes, and return:

- `Succeeded` only when all required MR runs succeed;
- `Failed` when any MR fails and cancellation was not requested;
- `Cancelled` when cancellation is requested before or during the sequence.

- [ ] **Step 4: Run focused tests**

```bash
rtk dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~RunBatchJobOperationHandlerTests|FullyQualifiedName~SystemMtJobWorkerTests|FullyQualifiedName~SystemMtAsyncPipelineTests"
```

Expected: all focused tests pass.

- [ ] **Step 5: Review checkpoint**

Request code review for PR-1 scope before continuing. Critical/Important findings block Task 3.

- [ ] **Step 6: Commit**

```bash
rtk git diff --name-only
rtk git add MetBench_BLL.Core/SystemMT/Jobs/Operations/RunBatchJobOperationHandler.cs MetBench_BLL.Core/SystemMT/Jobs/SystemMtJobRecord.cs MetBench_BLL.Core/SystemMT/Jobs/SystemMtJobStatus.cs MetBench_SystemMT.Tests/SystemMT/Jobs/RunBatchJobOperationHandlerTests.cs
rtk git commit -m "feat(systemmt): run batch MR jobs asynchronously"
```

## Task 3: Add Operation Handlers for Asset Import/Export

**Files:**
- Create: `MetBench_BLL.Core/SystemMT/Jobs/Operations/ISystemMtJobOperationHandler.cs`
- Create: `MetBench_BLL.Core/SystemMT/Jobs/Operations/ImportAssetsJobOperationHandler.cs`
- Create: `MetBench_BLL.Core/SystemMT/Jobs/Operations/ExportAssetsJobOperationHandler.cs`
- Create: `MetBench_BLL.Core/SystemMT/ImportExport/Put/SutImportStagingService.cs`
- Create: `MetBench_BLL.Core/SystemMT/ImportExport/Put/SutImportStagingManifest.cs`
- Modify: `MetBench_BLL.Core/SystemMT/Jobs/SystemMtJobWorker.cs`
- Test: `MetBench_SystemMT.Tests/SystemMT/Jobs/AssetImportExportJobTests.cs`

- [ ] **Step 1: Write failing async import/export tests**

Add tests that:

- create an A-group fixture `SutImportUnit`;
- export it to a temp package root with `SutImportPackageExporter.Export`;
- submit `ImportAssets`;
- run one worker iteration;
- assert terminal `Succeeded`;
- assert a staged import artifact exists under a deterministic staging root;
- assert no live catalog/LiteDB writes;
- submit `ExportAssets`;
- assert `sut-import-unit.json` exists in export root and validates.

Use current A/B fixture helpers if present; if not, construct the smallest valid `SutImportUnit` from existing `AGroupPutImportExportTests` patterns.

Run:

```bash
rtk dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~AssetImportExportJobTests"
```

Expected: fails because operation handlers do not exist.

- [ ] **Step 2: Implement staging manifest**

Create `SutImportStagingManifest`:

```csharp
public sealed record SutImportStagingManifest(
    string SutId,
    string PackageId,
    string SourcePackageRoot,
    string StagedPackageRoot,
    DateTime StagedAtUtc);
```

Create `SutImportStagingService` with:

```csharp
public string Stage(SutImportUnit unit, string sourcePackageRoot, string stagingRoot)
```

The service must:

- validate `unit`;
- create `stagingRoot/<safe-sut-id>/<utc-timestamp-or-guid>/`;
- write `sut-import-unit.json` through `SutImportPackageExporter.Export`;
- write `staging-manifest.json`;
- reject rooted or traversal paths through existing package exporter guards.

- [ ] **Step 3: Implement handler interface**

```csharp
namespace MetBench_BLL.SystemMT.Jobs.Operations;

public interface ISystemMtJobOperationHandler
{
    SystemMtJobKind Kind { get; }

    Task<SystemMtJobOperationOutcome> ExecuteAsync(
        SystemMtJobRecord record,
        IProgress<SystemMtJobProgress> progress,
        CancellationToken cancellationToken);
}

public sealed record SystemMtJobOperationOutcome(
    SystemMtJobState FinalState,
    string? FailureReason = null,
    string? ArtifactPath = null,
    string? FailureKind = null);
```

- [ ] **Step 4: Implement import handler**

`ImportAssetsJobOperationHandler` must require both `PackageRoot` and `StagingRoot`. It must import, validate, stage, and return the staged manifest path as `ArtifactPath`. A job that only validates the source package is not sufficient.

Handlers must call `cancellationToken.ThrowIfCancellationRequested()` before file reads and before file writes.

- [ ] **Step 5: Implement export handler**

`ExportAssetsJobOperationHandler` may copy a validated package from `PackageRoot` to `ExportRoot`, but it must validate before writing and return the exported `sut-import-unit.json` path:

```csharp
public sealed class ExportAssetsJobOperationHandler : ISystemMtJobOperationHandler
{
    public SystemMtJobKind Kind => SystemMtJobKind.ExportAssets;

    public Task<SystemMtJobOperationOutcome> ExecuteAsync(
        SystemMtJobRecord record,
        IProgress<SystemMtJobProgress> progress,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(record.PackageRoot) || string.IsNullOrWhiteSpace(record.ExportRoot))
            return Task.FromResult(new SystemMtJobOperationOutcome(SystemMtJobState.Failed, "PackageRoot and ExportRoot are required."));

        progress.Report(new SystemMtJobProgress(SystemMtJobState.Running, "reading source package", 20));
        var unit = SutImportPackageExporter.Import(record.PackageRoot);
        progress.Report(new SystemMtJobProgress(SystemMtJobState.Running, "writing export package", 80));
        var artifact = SutImportPackageExporter.Export(unit, record.ExportRoot);
        progress.Report(new SystemMtJobProgress(SystemMtJobState.Succeeded, "asset export complete", 100));
        return Task.FromResult(new SystemMtJobOperationOutcome(SystemMtJobState.Succeeded, ArtifactPath: artifact));
    }
}
```

- [ ] **Step 6: Wire handlers into worker**

Add constructor parameter:

```csharp
IEnumerable<ISystemMtJobOperationHandler>? operationHandlers = null
```

Build a dictionary by `Kind`. Preserve existing `RunMr` behavior by wrapping the current async pipeline in `RunMrJobOperationHandler`. When a non-RunMr kind is seen, dispatch to the matching handler and persist `ArtifactPath`.

- [ ] **Step 7: Run focused tests**

```bash
rtk dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~AssetImportExportJobTests|FullyQualifiedName~AGroupPutImportExportTests|FullyQualifiedName~BGroupPutImportExportTests|FullyQualifiedName~SystemMtJobWorkerTests"
```

Expected: all focused tests pass.

- [ ] **Step 8: Review checkpoint**

Request code review for PR-2 scope before continuing. Critical/Important findings block Task 4.

- [ ] **Step 9: Commit**

```bash
rtk git diff --name-only
rtk git add MetBench_BLL.Core/SystemMT/Jobs/Operations/ISystemMtJobOperationHandler.cs MetBench_BLL.Core/SystemMT/Jobs/Operations/ImportAssetsJobOperationHandler.cs MetBench_BLL.Core/SystemMT/Jobs/Operations/ExportAssetsJobOperationHandler.cs MetBench_BLL.Core/SystemMT/ImportExport/Put/SutImportStagingService.cs MetBench_BLL.Core/SystemMT/ImportExport/Put/SutImportStagingManifest.cs MetBench_BLL.Core/SystemMT/Jobs/SystemMtJobWorker.cs MetBench_SystemMT.Tests/SystemMT/Jobs/AssetImportExportJobTests.cs
rtk git commit -m "feat(systemmt): stage asset import export jobs"
```

## Task 4: Add Async Execution Artifact Export

**Files:**
- Create: `MetBench_BLL.Core/SystemMT/ImportExport/ExecutionArtifacts/ExecutionArtifactExportRequest.cs`
- Create: `MetBench_BLL.Core/SystemMT/ImportExport/ExecutionArtifacts/ExecutionArtifactExportManifest.cs`
- Create: `MetBench_BLL.Core/SystemMT/ImportExport/ExecutionArtifacts/ExecutionArtifactExporter.cs`
- Create: `MetBench_BLL.Core/SystemMT/Jobs/Operations/ExportExecutionArtifactsJobOperationHandler.cs`
- Test: `MetBench_SystemMT.Tests/SystemMT/ImportExport/ExecutionArtifactExporterTests.cs`
- Test: `MetBench_SystemMT.Tests/SystemMT/Jobs/ExecutionArtifactExportJobTests.cs`
- Test: `MetBench_SystemMT.Tests/Architecture/ExecutionArtifactImportBoundaryTests.cs`

- [ ] **Step 1: Confirm actual API signatures from Task 0**

Before writing tests, compare the snippets below to Task 0 discovery output. If repository/report method names differ, update this task in the plan first and commit the plan correction. Do not leave "adapt if needed" implementation work to the coding step.

- [ ] **Step 2: Write failing export tests**

Tests must assert:

- missing execution result fails with clear diagnostic;
- export writes `manifest.json`;
- export writes `execution-result.json`;
- export writes `execution-evidence.json` only when evidence exists;
- export writes at least HTML and Markdown report files through existing render/report services;
- result/evidence import APIs are not introduced, guarded by `ExecutionArtifactImportBoundaryTests`.

Run:

```bash
rtk dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~ExecutionArtifactExporterTests|FullyQualifiedName~ExecutionArtifactExportJobTests"
```

Expected: fails because exporter, handler, and boundary guard do not exist.

- [ ] **Step 3: Implement request and manifest records**

```csharp
public sealed record ExecutionArtifactExportRequest(
    Guid ExecutionId,
    string ExportRoot,
    bool IncludeEvidence = true,
    bool IncludeHtml = true,
    bool IncludeMarkdown = true);

public sealed record ExecutionArtifactExportManifest(
    Guid ExecutionId,
    Guid JobId,
    DateTime ExportedAtUtc,
    string[] Files);
```

- [ ] **Step 4: Implement exporter**

Use structured JSON serialization and the exact interfaces discovered in Task 0. The code below reflects the current interface shape verified on 2026-06-05: `ISystemMtResultRepository.GetAsync(string, ...)`, `IExecutionEvidenceRepository.GetByExecutionAsync(Guid, ...)`, `ISystemMtResultReportRenderer.Render(records, evidenceMap, context)`, and `SystemMtReportService.GenerateExecution(Guid, string)`.

```csharp
public sealed class ExecutionArtifactExporter
{
    private readonly ISystemMtResultRepository _results;
    private readonly IExecutionEvidenceRepository? _evidence;
    private readonly ISystemMtResultReportRenderer _html;
    private readonly SystemMtReportService _markdown;

    public async Task<string> ExportAsync(
        ExecutionArtifactExportRequest request,
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(request.ExportRoot);
        var record = await _results.GetAsync(request.ExecutionId.ToString(), cancellationToken)
            ?? throw new InvalidOperationException($"Execution result '{request.ExecutionId}' was not found.");

        var files = new List<string>();
        var resultFile = Path.Combine(request.ExportRoot, "execution-result.json");
        await File.WriteAllTextAsync(resultFile, JsonSerializer.Serialize(record, JsonOptions), cancellationToken);
        files.Add("execution-result.json");

        ExecutionEvidence? evidence = null;
        if (request.IncludeEvidence && _evidence is not null)
        {
            evidence = await _evidence.GetByExecutionAsync(request.ExecutionId, cancellationToken);
            if (evidence is not null)
            {
                var evidenceFile = Path.Combine(request.ExportRoot, "execution-evidence.json");
                await File.WriteAllTextAsync(evidenceFile, JsonSerializer.Serialize(evidence, JsonOptions), cancellationToken);
                files.Add("execution-evidence.json");
            }
        }

        if (request.IncludeHtml)
        {
            var htmlFile = Path.Combine(request.ExportRoot, "report.html");
            var evidenceMap = evidence is null
                ? null
                : new Dictionary<Guid, ExecutionEvidence> { [request.ExecutionId] = evidence };
            await File.WriteAllTextAsync(htmlFile, _html.Render(new[] { record }, evidenceMap), cancellationToken);
            files.Add("report.html");
        }

        if (request.IncludeMarkdown)
        {
            var markdownFile = Path.Combine(request.ExportRoot, "report.md");
            _markdown.GenerateExecution(request.ExecutionId, markdownFile);
            files.Add("report.md");
        }

        var manifest = new ExecutionArtifactExportManifest(request.ExecutionId, jobId, DateTime.UtcNow, files.ToArray());
        var manifestFile = Path.Combine(request.ExportRoot, "manifest.json");
        await File.WriteAllTextAsync(manifestFile, JsonSerializer.Serialize(manifest, JsonOptions), cancellationToken);
        return manifestFile;
    }
}
```

- [ ] **Step 5: Add no-import boundary guard**

Add a source guard test that fails if code introduces any of these production symbols under `MetBench_BLL.Core/SystemMT/ImportExport/ExecutionArtifacts/`:

- `ImportExecutionArtifact`;
- `ExecutionArtifactImporter`;
- `ImportExecutionEvidence`;
- `ImportExecutionResult`.

The guard must read production `.cs` files and assert no forbidden symbol appears outside the guard test itself.

- [ ] **Step 6: Implement job handler**

`ExportExecutionArtifactsJobOperationHandler` reads `record.ExecutionId` and `record.ExportRoot`, calls `ExecutionArtifactExporter.ExportAsync`, reports 20/80/100 progress, and returns `ArtifactPath = manifestFile`.

- [ ] **Step 7: Run focused tests**

```bash
rtk dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~ExecutionArtifactExporterTests|FullyQualifiedName~ExecutionArtifactExportJobTests|FullyQualifiedName~SystemMtReportServiceTests|FullyQualifiedName~HtmlSystemMtResultReportRendererTests"
```

Expected: all focused tests pass.

- [ ] **Step 8: Review checkpoint**

Request code review for PR-3 scope before continuing. Critical/Important findings block Task 5.

- [ ] **Step 9: Commit**

```bash
rtk git diff --name-only
rtk git add MetBench_BLL.Core/SystemMT/ImportExport/ExecutionArtifacts/ExecutionArtifactExportRequest.cs MetBench_BLL.Core/SystemMT/ImportExport/ExecutionArtifacts/ExecutionArtifactExportManifest.cs MetBench_BLL.Core/SystemMT/ImportExport/ExecutionArtifacts/ExecutionArtifactExporter.cs MetBench_BLL.Core/SystemMT/Jobs/Operations/ExportExecutionArtifactsJobOperationHandler.cs MetBench_SystemMT.Tests/SystemMT/ImportExport/ExecutionArtifactExporterTests.cs MetBench_SystemMT.Tests/SystemMT/Jobs/ExecutionArtifactExportJobTests.cs MetBench_SystemMT.Tests/Architecture/ExecutionArtifactImportBoundaryTests.cs
rtk git commit -m "feat(systemmt): export execution artifacts asynchronously"
```

## Task 5: Extend WPF Async Operations Surface

**Files:**
- Modify: `MetBench_Client/ViewModels/SystemMtAsyncJobViewModel.cs`
- Modify: `MetBench_Client/Views/Pages/SystemMtAsyncJobPage.xaml`
- Modify: `MetBench_Client/App.xaml.cs`
- Modify or add: `MetBench_SystemMT.Tests/ClientI18n/*` source guards if text/resources change
- Create: `docs/superpowers/vm-prompts/2026-06-05-t0-t2-async-import-export-release-closure-vm-prompt.md`

- [ ] **Step 1: Add VM prompt before WPF execution**

Create a VM prompt containing these instructions:

```markdown
# T0-T2 Async Import/Export Release Closure VM Prompt

Switch to branch `t0-t2-async-import-export-pr4-wpf`.

Read:
- `docs/superpowers/specs/2026-06-05-t0-t2-async-import-export-release-closure-design.md`
- `docs/superpowers/plans/2026-06-05-t0-t2-async-import-export-release-closure-plan.md`

Execute only the VM/WPF task.

Preconditions:
- `git status --short --branch` must show the target branch and no unrelated dirty files.
- `dotnet --info` must succeed.

Core steps:
1. Build `MetBench.sln`.
2. Open the WPF app.
3. Navigate to System-MT async operations.
4. Submit a single MR async run and capture queued/running/succeeded polling evidence.
5. Use the package root `docs/superpowers/specs/2026-06-02-minimum-mr-subset-a-group-import-export-design.md` only as documentation; create or copy an actual valid package under `%TEMP%\metbench-async-ie\source-package\` from the test fixture output if the app does not ship one. Record exact source path.
6. Set staging root to `%TEMP%\metbench-async-ie\staging\` and export root to `%TEMP%\metbench-async-ie\exports\`.
7. Submit asset import/export async jobs and capture the staged `staging-manifest.json` and exported `sut-import-unit.json` paths.
8. Submit execution artifact export for the pure-stdlib MR execution created in step 4. Export to `%TEMP%\metbench-async-ie\execution-export\`.
9. Capture screenshots under `docs/superpowers/specs/2026-06-05-t0-t2-async-import-export-vm-evidence/` showing job id, terminal status, failure area when applicable, artifact path, and non-blocking UI.
10. Save a `vm-summary.md` with exact commands, exit codes, test counts, screenshot filenames, job ids, artifact paths, and blockers if any.
11. Run focused WPF/source guard tests.

Acceptance:
- WPF build has 0 errors.
- Async run reaches terminal Succeeded for a pure-stdlib MR.
- Asset import/export reaches terminal Succeeded and produces `staging-manifest.json` plus `sut-import-unit.json`.
- Execution artifact export reaches terminal Succeeded and produces `manifest.json`, `execution-result.json`, and report files.
- UI remains responsive; no dispatcher blocking waits are introduced.
- If any precondition is missing, stop and report blocker without claiming pass.
```

- [ ] **Step 2: Update ViewModel to submit operation kinds**

Add user-selectable operation modes:

```csharp
public ObservableCollection<SystemMtJobKind> AvailableJobKinds { get; } =
    new(new[]
    {
        SystemMtJobKind.RunMr,
        SystemMtJobKind.RunBatch,
        SystemMtJobKind.ImportAssets,
        SystemMtJobKind.ExportAssets,
        SystemMtJobKind.ExportExecutionArtifacts
    });
```

Add bindable `PackageRoot`, `ExportRoot`, and `ExecutionIdText` properties. `SubmitAsync` should build `SystemMtOperationJobRequest` from the selected kind and call `SubmitOperationAsync`.

- [ ] **Step 3: Update XAML**

Add tabs or a segmented operation selector for:

- Run;
- Import/Export assets;
- Export results/reports.

Keep existing polling log, progress bar, cancel button, refresh button, result summary, and failure reason. Add artifact path display. Do not add instructional text blocks beyond labels needed for fields.

- [ ] **Step 4: Run source guard / focused tests**

Cloud-safe guard:

```bash
rtk dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~WpfAsync|FullyQualifiedName~SystemMtJob"
```

Windows VM:

```powershell
dotnet build MetBench.sln --no-restore
dotnet test MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~SystemMtJob|FullyQualifiedName~ExecutionArtifact|FullyQualifiedName~PutImportExport"
```

Expected: cloud-safe tests pass; VM build has 0 errors.

- [ ] **Step 5: Commit**

```bash
rtk git diff --name-only
rtk git add MetBench_Client/ViewModels/SystemMtAsyncJobViewModel.cs MetBench_Client/Views/Pages/SystemMtAsyncJobPage.xaml MetBench_Client/App.xaml.cs docs/superpowers/vm-prompts/2026-06-05-t0-t2-async-import-export-release-closure-vm-prompt.md
rtk git commit -m "feat(client): expose async import export operations"
```

## Task 6: Update Release Documentation and User Guide

**Files:**
- Modify: `docs/status/current.md`
- Modify: `docs/requirements.md`
- Modify: `docs/PROJECT-STRUCTURE.md`
- Modify: `docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md`
- Modify: `docs/usage/MetBench-T0-T5-操作指南.md`
- Add screenshot files under `docs/usage/images/` only from real VM capture.

- [ ] **Step 1: Update status ledger**

Do not mark `T0-T2 async import/export release closure` as Controlled in this task. Mark it `Release-candidate evidence captured` after cloud and VM evidence exists. Include exact commands, pass counts, PR numbers, and VM screenshot directory.

- [ ] **Step 2: Update requirements traceability**

Add or update rows:

- T0 async single/batch run user path;
- T1 async asset import/export;
- T2 async execution artifact export.

Every completed row must reference implementation files and tests.

- [ ] **Step 3: Update PROJECT-STRUCTURE**

Document:

- job operation kinds;
- async operation handlers;
- export bundle format;
- WPF async operations page;
- result/evidence import remains out of scope.

- [ ] **Step 4: Update user guide**

Add Chinese usage sections:

- 异步运行单条 MR / 批量 MR;
- 导入/导出 SUT/MR/样例/变异体资产包;
- 导出执行结果、证据和报告;
- 查看 job 状态、失败原因、artifact path;
- 取消任务。

Use only screenshots captured by VM execution. Do not invent screenshot filenames.

- [ ] **Step 5: Run docs checks**

```bash
rtk git diff --check
rtk rg -n "TB[D]|TO-DO|待[补]|占[位]" docs/status/current.md docs/requirements.md docs/PROJECT-STRUCTURE.md docs/usage/MetBench-T0-T5-操作指南.md docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md
```

Expected:

- `git diff --check` exits 0.
- `rg` finds no placeholder text introduced by this PR.

- [ ] **Step 6: Review checkpoint**

Request code review for PR-4 docs/WPF evidence before creating the PR. Critical/Important findings block Task 7.

- [ ] **Step 7: Commit**

```bash
rtk git diff --name-only
rtk git add docs/status/current.md docs/requirements.md docs/PROJECT-STRUCTURE.md docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md docs/usage/MetBench-T0-T5-操作指南.md
# Then stage only the exact screenshot files listed in the VM `vm-summary.md`.
# Do not stage the whole docs/usage/images directory.
rtk git commit -m "docs(status): close T0-T2 async import export release"
```

## Task 7: Final Verification, PRs, Merge, and Cleanup

**Files:**
- No new implementation files unless verification reveals a bug.

- [ ] **Step 1: Run cloud verification**

```bash
rtk dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~SystemMtJob|FullyQualifiedName~PutImportExport|FullyQualifiedName~ExecutionArtifact|FullyQualifiedName~SystemMtReportServiceTests|FullyQualifiedName~HtmlSystemMtResultReportRendererTests"
rtk dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --no-restore --filter "SemanticCatalogBoundaryTests|Catalog_MR_id_set_equals_governance_whitelist"
rtk git diff --check
```

Expected: all tests pass; governance whitelist remains green; diff check exits 0.

- [ ] **Step 2: Run VM verification**

Give the VM operator this instruction:

```text
切换到分支 t0-t2-async-import-export-pr4-wpf，读取 docs/superpowers/vm-prompts/2026-06-05-t0-t2-async-import-export-release-closure-vm-prompt.md，执行任务。
```

Expected: VM returns exact build/test/screenshot evidence. If VM reports blocker, do not mark Controlled.

- [ ] **Step 3: Create PR**

Use the repository PR gate checklist for each PR and include:

- scope;
- cloud test commands and outputs;
- VM evidence path;
- Windows classification;
- explicit statement that result/evidence import is out of scope;
- docs projection updates.

- [ ] **Step 4: Watch required checks**

Required:

- `test` green;
- `governance` green.

Soft review:

- inspect review/advisory output;
- fix real findings or comment N/A with evidence.

- [ ] **Step 5: Merge and cleanup**

After checks are green for each implementation PR:

```bash
rtk git fetch origin
rtk git merge origin/main
```

If branch is behind, update and rerun focused checks. Merge PR through GitHub, fetch `origin/main`, verify `HEAD == origin/main`, delete merged branch if safe, and report merge commit.

## Task 8: Post-Merge Controlled Closure PR

**Files:**
- Modify: `docs/status/current.md`
- Modify: `docs/requirements.md`
- Modify: `docs/PROJECT-STRUCTURE.md`
- Modify: `docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md`

- [ ] **Step 1: Verify merged main**

```bash
rtk git fetch origin
rtk git status --short --branch
rtk git rev-parse HEAD origin/main
rtk git log --oneline -5 --decorate
```

Expected:

- local branch is `main...origin/main`;
- `HEAD` equals `origin/main`;
- log shows all PR-1..PR-4 merge commits.

- [ ] **Step 2: Update status to Controlled only with evidence**

Move `T0-T2 async import/export release closure` to Controlled only if all are true:

- PR-1..PR-4 are merged;
- required checks were green;
- cloud verification commands are recorded with pass/fail counts;
- VM `vm-summary.md` exists and records build/test/screenshot/artifact evidence;
- user guide and projections are merged;
- stale branches were deleted or explicitly retained with a reason.

- [ ] **Step 3: Run docs checks**

```bash
rtk git diff --check
rtk rg -n "TB[D]|TO-DO|待[补]|占[位]" docs/status/current.md docs/requirements.md docs/PROJECT-STRUCTURE.md docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md
```

Expected: no introduced placeholders; diff check exits 0.

- [ ] **Step 4: Commit, PR, merge**

```bash
rtk git add docs/status/current.md docs/requirements.md docs/PROJECT-STRUCTURE.md docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md
rtk git commit -m "docs(status): mark T0-T2 async import export controlled"
```

Create and merge the closure PR only after required checks pass. Report merge commit and branch cleanup state.

## Self-Review

- Spec coverage: T0 async run, T1 asset import/export with staged artifacts, T2 result/evidence/report export, WPF evidence, docs closure, PR/merge cleanup, and post-merge Controlled closure are covered by Tasks 0-8.
- Placeholder scan: no incomplete placeholder acceptance remains in this plan. Result/evidence import is explicitly out of scope, not deferred silently.
- Type consistency: `SystemMtJobKind`, `SystemMtOperationJobRequest`, operation handlers, `ArtifactPath`, and export records are named consistently across tasks.
- Scope check: Docker/remote/HPC, dependency installation, Method-MT async conversion, T6 mutation, and trusted evidence import are excluded and require future scoped plans.
