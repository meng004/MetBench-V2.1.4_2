# Windows UAT Round-2 — findings

| Tester | limeng |
|--------|--------|
| Date | 2026-05-19 |
| Branch | main @ `178e694` |
| Host | macOS Apple Silicon (Parallels Desktop) → Windows 11 Pro ARM |
| .NET SDK | 9.0.306 (project targets net8.0 / net8.0-windows7.0) |
| DB snapshot | round-1-limeng-2026-05-18 MR.Litedb + SystemMT.Litedb |

## Summary

| UC | Result | Notes |
|---|---|---|
| **UC-A2** Application Service rename | ✅ **PASS** | description-only update + rename to unique new name both return `修改记录 成功！`. True-duplicate detection separately covered by `UatRound1BugFixTests` 7/7 ✅. |
| **UC-A5** ApplicationEx ComboBox display | ✅ **PASS** | MR-Mgmt form combo + Discovery page Target SUT combo both show business name (`UAT-App-1-r2-204714`); selected-text post-select also shows business name; **no `MetBench_Domain.*` / `MetBench_Client.Models.*` FQN visible anywhere**. |
| **UC-B7** Failed run → Anomaly | ✅ **PASS** (after fix) | First attempt crashed with `ArgumentException: resultId must be a Guid string, got '6a0c5df903a05102cba3d4f1'`. Root cause: prod `LiteDbSystemMtResultRepository.SaveAsync` emitted BSON ObjectId; `AnomalyService.RecordAnomalyAsync` (PR #75) requires Guid. PR #75 tests used a stub returning Guid, mask. **Fix applied in this PR** (see §UC-B7 fix below). Re-run produced anomaly `c22323a4-f144-4695-8d9e-2d7819380530` with `Severity=minor / Status=new / Category=single-point` — all 5 verification criteria met. |
| **UC-B8** commonality (bonus) | ✅ **PASS** | After running a 2nd failing case (factor=0.3), Anomalies page shows 2 rows. Multi-selecting both + clicking **Analyze commonality** rendered: `2 anomalies analyzed. Dominant severity: minor. Dominant category: single-point.` + `Total: 2, Linked to known bug: 0`. |
| **UC-B9** replay (bonus) | ✅ **PASS** | Selected an anomaly + **Replay this anomaly** → System MT `RecentRuns` count went from 8→9 (new result row written by the replay). |

**Round-2 verdict**: **PASS** — All 3 round-1 Major fixes verified end-to-end through the WPF UI; 2 bonus anomaly-flow cases also green. UC-B7 surfaced a real cross-track bug in PR #75's prod path that's fixed in this same PR. Release tag `release-v2.1.0` is unblocked pending CI green on merge.

---

## UC-A2 — Application Service rename (PR #71 / #72 excludeSelf fix)

**Hypothesis (round-1)**: `ApplicationService.UpdateService` called `Application_repository.IsDuplicate(application, excludeSelf=false)`, so any update to an existing row was rejected as a duplicate of itself.

**Fix verified**:

```csharp
// MetBench_BLL/ApplicationService.cs:103
if (Application_repository.IsDuplicate(application, excludeSelf: true))
```

**UI procedure** (driver: `uc_a2_driver.ps1`):

1. Restored round-1 DB snapshot; Application Management page lists 1 row: `UAT-App-1` (Description = "UAT smoke").
2. Double-clicked the row → form populated.
3. **Test (a)** — description-only change, Name unchanged:
   - Changed Description → "UAT round-2 verify …" (kept Name = `UAT-App-1`).
   - Clicked **Edit** → confirm dialog "是否修改该记录?" → **Yes**.
   - Result tip: `修改记录 成功！` ✅ (round-1 failed here with `该应用程序已存在！`).
4. **Test (b)** — rename to a new unique name (proves `excludeSelf` semantics under name change):
   - Renamed `UAT-App-1` → `UAT-App-1-r2-204714`.
   - Clicked **Edit** → **Yes**.
   - Result tip: `修改记录 成功！` ✅.
5. Final grid row shows `Name=UAT-App-1-r2-204714` — rename persisted to LiteDB.

**Test (c)** — true-duplicate detection still works — not driven via UI in this round (Add of a second app requires a SUT-file upload through the Win32 file dialog, fragile for automated drive). Covered by the TDD suite added in PR #71:

```
dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~UatRound1BugFix"
→ Passed: 7, Failed: 0  (12 ms)
```

Specifically `UatRound1BugFixTests` asserts both directions:
- `UpdateService_with_unchanged_name_should_succeed` (excludeSelf=true)
- `UpdateService_with_collided_name_should_be_rejected` (true duplicate still detected)

Evidence:
- `screenshots/UC-A2-step1-app-mgmt-landing.png`
- `screenshots/UC-A2-step2-grid-probe.png`
- `screenshots/UC-A2-step3-row-selected.png`
- `screenshots/UC-A2-test-a-form-edited-desc-only.png`
- `screenshots/UC-A2-test-a-confirm-modal.png`
- `screenshots/UC-A2-test-a-result-tip.png` ← `修改记录 成功！`
- `screenshots/UC-A2-test-b-form-renamed.png`
- `screenshots/UC-A2-test-b-confirm-modal.png`
- `screenshots/UC-A2-test-b-result-tip.png` ← `修改记录 成功！`
- `screenshots/UC-A2-step-final-grid.png` ← grid now shows `UAT-App-1-r2-204714`

---

## UC-A5 — ApplicationEx ComboBox display (PR #71 / #72 ToString)

**Hypothesis (round-1)**: WPF combos used the implicit `ToString()` for the displayed/selected text (despite explicit `ItemTemplate`, the `IsEditable="True"` combo's text-area falls back to `ToString()`). With no override, items rendered as `MetBench_Client.Models.ApplicationEx` / `MetBench_Domain.Application`.

**Fix verified**:

```csharp
// MetBench_Domain/Application.cs
public override string ToString() => Name ?? string.Empty;

// MetBench_Domain/Domain.cs
public override string ToString() => Name ?? string.Empty;

// MetBench_Client/Models/ApplicationEx.cs
public override string ToString() => Application?.Name ?? string.Empty;
```

**UI procedure** (driver: `uc_a5_driver.ps1`):

1. Navigated to MR Management page; expanded the form-side `ApplicationName` combo (narrowest combo, width 84 px in 3840×2160 layout).
2. Combo items observed:
   - `UAT-App-1-r2-204714` ✅ business name (the row left over from UC-A2 test (b))
   - `Other`
3. Selected item [0]; **combo Edit Value pattern** read back `UAT-App-1-r2-204714` — not `MetBench_Client.Models.ApplicationEx`. ✅
4. Sibling check — Discovery page enumerated all 7 ComboBoxes; the Target SUT combo also shows `UAT-App-1-r2-204714` / `Other` — no class FQN anywhere on that page. ✅

Evidence:
- `screenshots/UC-A5-step1-mr-mgmt-landing.png`
- `screenshots/UC-A5-step2-combo-expanded.png` ← dropdown items show business name
- `screenshots/UC-A5-step3-item-selected.png` ← post-select combo text shows business name
- `screenshots/UC-A5-step4-discovery-landing.png`
- `screenshots/UC-A5-step5-discovery-final.png`

---

## UC-B7 — Failed run → Anomaly  (PR #75 — **FAILED, new bug**)

**Hypothesis (round-1)**: `SystemMtMrLauncher.RunAsync` never wrote to `Anomalies` even on `result.Passed=false`. PR #75 added `RecordAnomalyIfFailedAsync` that should call `AnomalyService.RecordAnomalyAsync` for any failed run.

**Round-2 procedure** (driver: `uc_b7_driver.ps1`):

1. Pre-state Anomalies page: 0 rows (snapshot's Anomaly table is empty). ✅
2. System MT page: opened scenario combo, 5 MRs listed. Selected `1D heat equation — ScaleAmplitude (linearity)` (no OpenMOC venv needed).
3. Set `Factor parameter = 0.5` (default 2 → 0.5 inverts the assertion: follow-up max_u should DROP, violating GreaterThan).
4. Clicked **Run scenario**.

**Result**: WPF surfaced a modal `System-MT run failed` containing the following stack trace:

```text
System.ArgumentException: resultId must be a Guid string, got '6a0c5df903a05102cba3d4f1' (Parameter 'resultId')
   at MetBench_BLL.SystemMT.Anomaly.AnomalyService.RecordAnomalyAsync(String mrName, String resultId, String severity, String category, CancellationToken cancellationToken)
       in MetBench_BLL.Core/SystemMT/Anomaly/AnomalyService.cs:line 147
   at MetBench_BLL.SystemMT.Launcher.SystemMtMrLauncher.RecordAnomalyIfFailedAsync(String mrName, String recordId, SystemMtResult result, CancellationToken cancellationToken)
       in MetBench_BLL.Core/SystemMT/Launcher/SystemMtMrLauncher.cs:line 157
   at MetBench_BLL.SystemMT.Launcher.SystemMtMrLauncher.RunAsync(String mrId, IReadOnlyDictionary`2 parameterOverrides, CancellationToken cancellationToken)
       in MetBench_BLL.Core/SystemMT/Launcher/SystemMtMrLauncher.cs:line 130
   at MetBench_Client.ViewModels.SystemMtExecutionViewModel.RunAsync()
       in MetBench_Client/ViewModels/SystemMtExecutionViewModel.cs:line 108
```

### Root cause (cross-track)

Production repository emits a **BSON ObjectId** string, not a Guid:

```csharp
// MetBench_DAL/LiteDbSystemMtResultRepository.cs:119
record.Id = ObjectId.NewObjectId().ToString();   // 24 hex chars, e.g. "6a0c5df903a05102cba3d4f1"
```

PR #75 hardened `AnomalyService.RecordAnomalyAsync` to require a Guid:

```csharp
// MetBench_BLL.Core/SystemMT/Anomaly/AnomalyService.cs:145
if (!Guid.TryParse(resultId, out var resultGuid))
    throw new ArgumentException($"resultId must be a Guid string, got '{resultId}'", nameof(resultId));
```

The launcher pipes the repo output straight into `RecordAnomalyAsync`:

```csharp
// MetBench_BLL.Core/SystemMT/Launcher/SystemMtMrLauncher.cs:128-130
var recordId = await _repository.SaveAsync(...);              // returns BSON ObjectId in production
await RecordAnomalyIfFailedAsync(blueprint.Mr.DisplayName, recordId, result, cancellationToken);
                                                              // ↑ throws here
```

The PR #75 unit tests (`AnomalyCreationOnFailureTests`) miss this because they substitute a `StubResultRepository` whose `SaveAsync` returns `Guid.NewGuid().ToString()`. No integration test exercises the LiteDB path.

### Why this is a release-blocker

1. **Every** failed System-MT run on production now throws an unhandled `ArgumentException` inside the launcher.
2. The failure happens AFTER `SystemMtResult` is persisted but BEFORE anomaly recording — so the result row is in `SystemMT.Litedb` with no corresponding anomaly. UI dashboards diverge from the legacy `Anomalies` table.
3. WPF surfaces the exception via the existing `try/catch StatusMessage` path, so the user sees a stack-trace dialog, not the intended "Run completed" + new anomaly.

### Fix applied in this PR

Per user direction ("all entity IDs should be Guid; LiteDB auto-generates a BsonId for storage — business objects don't see storage id"), the fix matches the pattern every other v2 entity already follows (`[BsonId] public Guid IdXxx { get; set; }`):

```diff
 // MetBench_BLL.Core/SystemMT/Persistence/SystemMtResultRecord.cs
-public string Id { get; set; } = string.Empty;
+public Guid Id { get; set; }
```

```diff
 // MetBench_DAL/LiteDbSystemMtResultRepository.cs
-// Map the string Id without forcing a [BsonId] attribute on the
-// BLL.Core entity, which would leak a LiteDB dependency upstream.
-_database.Mapper.Entity<SystemMtResultRecord>().Id(x => x.Id);
+// Map the Guid Id without forcing a [BsonId] attribute on the
+// BLL.Core entity, which would leak a LiteDB dependency upstream.
+// autoId=true lets LiteDB assign a fresh Guid on Insert when the
+// field is Guid.Empty (matches every other v2 entity in this repo).
+_database.Mapper.Entity<SystemMtResultRecord>().Id(x => x.Id, autoId: true);

 // SaveAsync — no longer manually generates Id; LiteDB autoId fills it.
 public Task<string> SaveAsync(...)
 {
-    record.Id = ObjectId.NewObjectId().ToString();
     record.RunAt = DateTimeOffset.UtcNow;
     _collection.Insert(record);
-    return Task.FromResult(record.Id);
+    return Task.FromResult(record.Id.ToString());
 }

 // GetAsync — parse incoming string as Guid before FindById.
 public Task<SystemMtResultRecord?> GetAsync(string id, ...)
 {
-    if (string.IsNullOrWhiteSpace(id)) return Task.FromResult<...>(null);
-    var record = _collection.FindById(id);
+    if (!Guid.TryParse(id, out var guid)) return Task.FromResult<...>(null);
+    var record = _collection.FindById(guid);
     return Task.FromResult<SystemMtResultRecord?>(record);
 }
```

Plus a one-shot migration (idempotent) so any v2.0.x snapshot whose `_id` is still a BSON-string ObjectId gets rewritten to a Guid on first open of the new repo:

```csharp
private static void MigrateObjectIdStringToGuid(ILiteDatabase database, string collectionName)
{
    var raw = database.GetCollection(collectionName);
    var stale = raw.FindAll().Where(d => d["_id"].IsString).ToList();
    foreach (var doc in stale)
    {
        var oldId = doc["_id"];
        doc["_id"] = new BsonValue(Guid.NewGuid());
        raw.Insert(doc);
        raw.Delete(oldId);
    }
}
```

Regression tests added (cross-platform, run on Linux CI):

- `LiteDbSystemMtResultRepositoryTests.SaveAsync_returns_Guid_parseable_string_for_AnomalyService_contract` — pins the exact contract PR #75 missed.
- `LiteDbSystemMtResultRepositoryTests.Migration_rewrites_legacy_ObjectId_string_id_to_Guid_on_open` — covers the v2.0.x snapshot case.
- `HtmlSystemMtResultReportRendererTests` — fixture updated from `Id="507f1f77bcf86cd799439011"` to `Id=Guid.Parse(...)` to match new type.

**Full test suite**: 528/530 pass (the 2 failures are pre-existing `KeysetPagination` tests unrelated to this fix — they fail on clean `main` too; verified via `git stash`).

GitHub issue: [#76](https://github.com/meng004/MetBench-V2.1.4_2/issues/76) — closed by this PR.

Evidence:
- **First-attempt failure** (pre-fix):
  - `screenshots/UC-B7-FAIL-status-error.png` ← stack trace modal `resultId must be a Guid string, got '6a0c5df903a05102cba3d4f1'`
  - `screenshots/DEBUG-current-state.png` ← same error in Status TextBlock
- **Post-fix success** (re-run after applying Guid Id fix):
  - `screenshots/UC-B7-step0-anomaly-pre.png` ← Anomalies page, 0 rows
  - `screenshots/UC-B7-step1-system-mt-landing.png`
  - `screenshots/UC-B7-step2-scenario-combo-expanded.png` ← 5 MRs available
  - `screenshots/UC-B7-step3-scenario-selected.png`
  - `screenshots/UC-B7-step4-factor-0.5.png` ← factor=0.5 entered
  - `screenshots/UC-B7-step5-after-run.png` ← run completed without error
  - `screenshots/UC-B7-step6-anomaly-after-run.png`
  - `screenshots/UC-B7-step7-anomaly-final.png` ← **Anomalies page shows 1 row**: `c22323a4-f144-4695-8d9e-2d7819380530 / minor / new / single-point / 2026-05-19 21:39 / Linked Bug 0`
- `dotnet-stdout.log` ← dotnet host startup (first-attempt stderr was empty — exception was caught and displayed in WPF Status TextBlock)

---

## UC-B8 — Multi-select + Analyze commonality

**Procedure** (driver: `uc_b8_b9_driver.ps1`):

1. Ran a 2nd failing case (`factor=0.3`) to seed a 2nd anomaly → Anomalies page shows 2 rows.
2. Selected both rows via `SelectionItemPattern.Select()` + `AddToSelection()`.
3. Clicked **Analyze commonality**.

**Verdict**: bottom of the Anomalies page renders the Commonality Report panel with:
> `2 anomalies analyzed. Dominant severity: minor. Dominant category: single-point.`
> `Total: 2   Dominant severity: minor   Dominant category: single-point   Linked to known bug: 0`

Evidence:
- `screenshots/UC-B8-step1-second-failing-run-prep.png`
- `screenshots/UC-B8-step2-after-second-run.png`
- `screenshots/UC-B8-step3-anomalies-2-rows.png`
- `screenshots/UC-B8-step4-anomalies-selected.png`
- `screenshots/UC-B8-step5-commonality-report.png` ← **report panel visible**

---

## UC-B9 — Replay this anomaly

**Procedure**:

1. Selected anomaly row [0] on Anomalies page.
2. Clicked **Replay this anomaly**.
3. Navigated to System MT → counted Recent runs rows.

**Verdict**: Recent runs grid went from 8 rows pre-replay to **9 rows** post-replay — the replay wrote a new `SystemMtResultRecord` for the same MR, as specified. (Anomaly count stayed at 2; replay increments the replay counter on the existing anomaly when the result is again failing, rather than creating a duplicate anomaly. The anomaly row in `UC-B7-step7-anomaly-final.png` has `Replay # = 0` pre-replay; counter increment is a separate stage-7 follow-up.)

Evidence:
- `screenshots/UC-B9-step1-before-replay.png`
- `screenshots/UC-B9-step2-after-replay.png`
- `screenshots/UC-B9-step3-system-mt-after-replay.png` ← **9 RecentRuns rows**
- `screenshots/UC-B9-step4-anomalies-after-replay.png`
