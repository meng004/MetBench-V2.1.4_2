# bug(SystemMT): UC-B7 anomaly auto-creation crashes in production — ObjectId vs Guid resultId mismatch

> Draft GitHub issue body. Filed against `meng004/MetBench-V2.1.4_2` from the Windows UAT round-2 VM.

## Symptom

Every failed System-MT run on the WPF client throws an unhandled `ArgumentException`. Reproduced on `main @ 178e694` (post-PR #75), Windows 11 Pro ARM (Parallels), with the round-1 UAT DB snapshot loaded.

Reproduction:

1. Launch `dotnet run --project MetBench_Client`.
2. Navigate to **System MT**.
3. Scenario = `1D heat equation — ScaleAmplitude (linearity)` (no OpenMOC venv required).
4. `Factor parameter` = `0.5` (inverts the GreaterThan assertion → guarantees `result.Passed=false`).
5. Click **Run scenario**.

Outcome: `System-MT run failed` modal with the following stack trace.

```text
System.ArgumentException: resultId must be a Guid string, got '6a0c5df903a05102cba3d4f1' (Parameter 'resultId')
   at MetBench_BLL.SystemMT.Anomaly.AnomalyService.RecordAnomalyAsync(String mrName, String resultId, ...)
       MetBench_BLL.Core/SystemMT/Anomaly/AnomalyService.cs:line 147
   at MetBench_BLL.SystemMT.Launcher.SystemMtMrLauncher.RecordAnomalyIfFailedAsync(...)
       MetBench_BLL.Core/SystemMT/Launcher/SystemMtMrLauncher.cs:line 157
   at MetBench_BLL.SystemMT.Launcher.SystemMtMrLauncher.RunAsync(...)
       MetBench_BLL.Core/SystemMT/Launcher/SystemMtMrLauncher.cs:line 130
   at MetBench_Client.ViewModels.SystemMtExecutionViewModel.RunAsync()
       MetBench_Client/ViewModels/SystemMtExecutionViewModel.cs:line 108
```

Screenshot: `docs/uat/reports/round-2-windows-2026-05-19-limeng/screenshots/UC-B7-FAIL-status-error.png`

## Root cause

Production `LiteDbSystemMtResultRepository.SaveAsync` emits a BSON `ObjectId.NewObjectId().ToString()` (24 hex chars, no dashes) for `record.Id`:

```csharp
// MetBench_DAL/LiteDbSystemMtResultRepository.cs:115-123
public Task<string> SaveAsync(string mrName, SystemMtResult result, CancellationToken cancellationToken = default)
{
    cancellationToken.ThrowIfCancellationRequested();
    var record = SystemMtResultRecord.FromResult(mrName, result);
    record.Id = ObjectId.NewObjectId().ToString();   // ← 24-hex BSON ObjectId, NOT a Guid
    record.RunAt = DateTimeOffset.UtcNow;
    _collection.Insert(record);
    return Task.FromResult(record.Id);
}
```

PR #75 added a Guid-only contract on the anomaly-creation side:

```csharp
// MetBench_BLL.Core/SystemMT/Anomaly/AnomalyService.cs:137-149
public Task<MetBench_Domain.Anomaly> RecordAnomalyAsync(
    string mrName, string resultId, string severity, string category, CancellationToken cancellationToken = default)
{
    cancellationToken.ThrowIfCancellationRequested();
    if (!Guid.TryParse(resultId, out var resultGuid))
    {
        throw new ArgumentException(
            $"resultId must be a Guid string, got '{resultId}'", nameof(resultId));
    }
    ...
}
```

The launcher pipes the repo's return value straight through:

```csharp
// MetBench_BLL.Core/SystemMT/Launcher/SystemMtMrLauncher.cs:128-130
var recordId = await _repository.SaveAsync(blueprint.Mr.DisplayName, result, cancellationToken);
await RecordAnomalyIfFailedAsync(blueprint.Mr.DisplayName, recordId, result, cancellationToken);
```

## Why CI did not catch this

PR #75's `AnomalyCreationOnFailureTests` substitute a `StubResultRepository` whose `SaveAsync` returns `Guid.NewGuid().ToString()`. The real `LiteDbSystemMtResultRepository.SaveAsync` is never exercised by any test that also wires `AnomalyService` + `SystemMtMrLauncher` end-to-end.

```csharp
// MetBench_SystemMT.Tests/V2Anomaly/AnomalyCreationOnFailureTests.cs:161-163
public Task<string> SaveAsync(string mrName, SystemMtResult result, CancellationToken cancellationToken = default)
    => Task.FromResult(Guid.NewGuid().ToString());   // ← masks the prod incompatibility
```

## Suggested fix

Smallest defensible patch (one line):

```diff
 // MetBench_DAL/LiteDbSystemMtResultRepository.cs:119
-record.Id = ObjectId.NewObjectId().ToString();
+record.Id = Guid.NewGuid().ToString();
```

`SystemMtResultRecord.Id` is typed `string`, so existing rows in production DBs that carry ObjectId-format Ids keep round-tripping correctly through reads — only the *format* of *new* Ids changes, and only new rows ever flow into anomaly creation.

## Regression test (add as part of the fix PR)

End-to-end coverage with the real LiteDB repos:

```csharp
// MetBench_SystemMT.Tests/V2Anomaly/AnomalyCreationOnFailureIntegrationTests.cs (new)

[Fact]
public async Task Failed_run_with_real_litedb_repos_writes_anomaly_row()
{
    using var resultDb = new TempLiteDbFile();   // disposable wrapper, deletes on exit
    using var anomalyDb = new TempLiteDbFile();
    var resultRepo  = new LiteDbSystemMtResultRepository($"Filename={resultDb.Path}");
    var anomalyRepo = new LiteDbAnomalyRepository($"Filename={anomalyDb.Path}");
    var auditRepo   = new LiteDbAuditLogRepository($"Filename={anomalyDb.Path}");
    var anomalySvc  = new AnomalyService(anomalyRepo, auditRepo);
    var launcher    = new SystemMtMrLauncher(opts, resultRepo, anomalySvc, fakeRunner);

    // arrange a forced-fail MR
    var result = await launcher.RunAsync("heat-equation-amplitude",
        new Dictionary<string,string>{["factor"]="0.5"}, default);

    Assert.False(result.Passed);
    Assert.Single(anomalyRepo.GetAll());           // ← would FAIL today with `ArgumentException`
    Assert.True(Guid.TryParse(result.RecordId, out _));
}
```

That test would have caught this before PR #75 merged.

## Impact

- Every UI-triggered failed System-MT run on production throws. Result row IS persisted (SaveAsync completes before the throw), but no Anomaly is recorded — DBs diverge silently from intent.
- Blocks Windows UAT round-2 UC-B7 / UC-B8 / UC-B9.
- Blocks `release-v2.1.0` tag.

## Workaround

None VM-side without touching cloud-owned code (`MetBench_BLL.Core/SystemMT/*` per `CLAUDE.md` cross-environment rules).

## References

- Round-2 evidence pack: `docs/uat/reports/round-2-windows-2026-05-19-limeng/`
- Round-1 baseline: `docs/uat/reports/round-1-limeng-2026-05-18/` (UC-B7 round-1 noted as ⚠️ Partial — Anomalies page rendered but empty; round-2 confirms the wiring is now wrong, not absent)
- PR #75 (`fix(SystemMT): UC-B7 — 失败 run 自动建 Anomaly 记录`): commit `178e694`
