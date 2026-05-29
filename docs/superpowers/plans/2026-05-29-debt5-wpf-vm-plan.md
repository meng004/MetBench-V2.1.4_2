# Debt #5 — WPF VM Execution Plan (Task 4E)

> **For the VM worker (Windows + VS 2022):** This plan finishes debt #5 — the cloud side already shipped
> `Anomaly.Status` string→`AnomalyStatus` enum (commit `6c1484c`). WPF (`MetBench_Client`, net8.0-windows7.0)
> cannot be compiled on the cloud/mac host, so the 3 call sites below are yours. Execute task-by-task; each
> task has 前置条件检查 / 核心步骤 / 验收条件. The companion screenshot checklist is in
> [`2026-05-29-debt5-vm-prompt.md`](../specs/2026-05-29-debt5-vm-prompt.md).

**Goal:** Make `MetBench_Client` compile and run against the enum-ized Anomaly status API.

**Branch:** `followup/debts-2026-05-29` (already on origin). `git switch followup/debts-2026-05-29 && git pull --ff-only` first.

**Authoritative API (do NOT redefine — it lives in `MetBench_Domain/V2/AnomalyStatus.cs`):**
- `enum AnomalyStatus { Unspecified=0, New=1, Investigating=2, Known=3, ConfirmedBug=4, FalsePositive=5, FixedUpstream=6 }`
- `AnomalyStatuses.TryParseKebab(string?, out AnomalyStatus) → bool` (kebab→enum, no throw)
- `AnomalyStatuses.ToKebab(this AnomalyStatus) → string` (enum→kebab; `Unspecified`→`"unspecified"`, never throws)
- `IAnomalyService.TransitionStatus(Guid, AnomalyStatus, string?, string)` — **illegal transitions throw `InvalidAnomalyStatusTransitionException`** (legal: `new→investigating`; `investigating→{known,confirmed-bug,false-positive,fixed-upstream}`)
- `AnomalyFilter.Status` is now `AnomalyStatus?`

**Why WPF currently fails to compile:** `AnomalyListViewModel` still passes `string` where the enum API is now required (2 sites), and the DataGrid binds `Anomaly.Status` (now an enum) as raw text.

---

## Task 1: ViewModel List filter — string→enum

**Files:** Modify `MetBench_Client/ViewModels/AnomalyListViewModel.cs:114-116`.

**前置条件检查:**
- [ ] On branch `followup/debts-2026-05-29`, pulled.
- [ ] `using MetBench_Domain;` present (it is, line 11) — `AnomalyStatuses` lives there.
- [ ] `dotnet build MetBench_Client/MetBench_Client.csproj` currently FAILS (string→AnomalyStatus? mismatch).

**核心步骤:**
- [ ] **Step 1:** Replace the `AnomalyFilter` construction. Old (L114-116):
  ```csharp
  var filter = new AnomalyFilter(
      Severity: string.IsNullOrEmpty(SeverityFilter) ? null : SeverityFilter,
      Status: string.IsNullOrEmpty(StatusFilter) ? null : StatusFilter);
  ```
  New:
  ```csharp
  var filter = new AnomalyFilter(
      Severity: string.IsNullOrEmpty(SeverityFilter) ? null : SeverityFilter,
      Status: AnomalyStatuses.TryParseKebab(StatusFilter, out var statusFilter) ? statusFilter : (AnomalyStatus?)null);
  ```
  (`StatusFilter` stays `string?` — the ComboBox binds kebab strings; only the BLL call converts.)

**验收条件:** this file's filter site compiles; empty/blank `StatusFilter` → no status filter (TryParseKebab false → null).

---

## Task 2: ViewModel TransitionAsync — string→enum + illegal-transition handling

**Files:** Modify `MetBench_Client/ViewModels/AnomalyListViewModel.cs:155-167`.

**前置条件检查:** Task 1 applied. Note `TransitionStatus` now THROWS `InvalidAnomalyStatusTransitionException` on illegal edges; the existing `catch (Exception ex)` (L172-175) already surfaces it to `ErrorMessage`.

**核心步骤:**
- [ ] **Step 1:** Replace the call site. Old (inside the `try`, L157-167):
  ```csharp
  var ok = _service.TransitionStatus(
      SelectedAnomaly.IdAnomaly,
      TransitionTarget!,
      notes: null,
      actor: "wpf-user");

  if (!ok)
  {
      ErrorMessage = $"TransitionStatus returned false for anomaly {SelectedAnomaly.IdAnomaly}.";
      return;
  }
  ```
  New:
  ```csharp
  if (!AnomalyStatuses.TryParseKebab(TransitionTarget, out var target))
  {
      ErrorMessage = $"Unknown status '{TransitionTarget}'.";
      return;
  }

  var ok = _service.TransitionStatus(
      SelectedAnomaly.IdAnomaly,
      target,
      notes: null,
      actor: "wpf-user");

  if (!ok)
  {
      ErrorMessage = $"TransitionStatus returned false for anomaly {SelectedAnomaly.IdAnomaly}.";
      return;
  }
  ```

**验收条件:** compiles; legal transition (e.g. `new`→`investigating`) succeeds + reloads; illegal (e.g. `new`→`confirmed-bug`) is caught → `ErrorMessage` set, app does NOT crash, status unchanged.

---

## Task 3: DataGrid Status column — enum→kebab display converter

**Files:** Create `MetBench_Client/Converters/AnomalyStatusKebabConverter.cs`; Modify `MetBench_Client/Views/Pages/AnomalyListPage.xaml` (xmlns + Page.Resources + L80 binding).

**前置条件检查:** DataGrid Status column (L80) currently `Binding="{Binding Status}"` renders the enum's default ToString (`Investigating`), not kebab.

**核心步骤:**
- [ ] **Step 1 — create converter** `MetBench_Client/Converters/AnomalyStatusKebabConverter.cs`:
  ```csharp
  using System;
  using System.Globalization;
  using System.Windows.Data;
  using MetBench_Domain;

  namespace MetBench_Client.Converters;

  /// <summary>DataGrid 显示 AnomalyStatus 为 kebab 字符串（enum→kebab）。</summary>
  public sealed class AnomalyStatusKebabConverter : IValueConverter
  {
      public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
          => value is AnomalyStatus s ? s.ToKebab() : string.Empty;

      public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
          => value is string k && AnomalyStatuses.TryParseKebab(k, out var s) ? s : AnomalyStatus.Unspecified;
  }
  ```
- [ ] **Step 2 — XAML namespace:** in `AnomalyListPage.xaml` root `<Page ...>` (after line 5 `xmlns:controls=...`), add:
  ```xml
  xmlns:conv="clr-namespace:MetBench_Client.Converters"
  ```
- [ ] **Step 3 — XAML resources:** immediately after the `<Grid Margin="12">` open tag is wrong — put it at Page level. Insert BEFORE `<Grid Margin="12">` (i.e. right after the `<Page ...>` closing `>` on line 12):
  ```xml
  <Page.Resources>
      <conv:AnomalyStatusKebabConverter x:Key="AnomalyStatusKebabConverter" />
  </Page.Resources>
  ```
- [ ] **Step 4 — DataGrid binding (L80):** Old:
  ```xml
  <DataGridTextColumn Header="Status"       Binding="{Binding Status}"         Width="120" />
  ```
  New:
  ```xml
  <DataGridTextColumn Header="Status"       Binding="{Binding Status, Converter={StaticResource AnomalyStatusKebabConverter}}" Width="120" />
  ```
  (Severity / Category columns stay `string`, unchanged. Filter + transition ComboBoxes keep their kebab `ComboBoxItem.Content` — unchanged.)

**验收条件:** DataGrid Status column shows kebab text (`investigating`, `confirmed-bug`), not `2`/`Investigating`.

---

## Final Verification (VM operator)

- [ ] `dotnet build MetBench_Client/MetBench_Client.csproj` → **0 errors**. (If a BLL.Core/Domain enum-contract error appears, do NOT edit those — report to cloud; CI owns the contract.)
- [ ] `dotnet run --project MetBench_Client`, open Anomaly list page.
- [ ] Capture the 7 screenshots per [`2026-05-29-debt5-vm-prompt.md`](../specs/2026-05-29-debt5-vm-prompt.md) into `docs/superpowers/specs/2026-05-29-debt5-vm-verification/`.
- [ ] Commit: `git add MetBench_Client/ docs/superpowers/specs/2026-05-29-debt5-vm-verification/ && git commit -m "feat(t5): wire WPF AnomalyList to AnomalyStatus enum + VM verification (debt #5 4E)"`
- [ ] `git push origin followup/debts-2026-05-29`.
- [ ] Report build result + screenshot status back to cloud so the §12 PR can proceed.
