# Debt #5 — WPF VM 验证 Prompt（2026-05-29）

> 云端已在分支 `followup/debts-2026-05-29`（从 `origin/main` @ 84ae500 切出）完成 debt #5
> 的**跨平台部分**（Anomaly 状态机 string→`AnomalyStatus` enum + 转移校验 + LiteDB
> string→int 迁移）。BLL.Core / Domain / IDAL / DAL / Tests 在云端 `dotnet test` 跨平台
> 套件**零新增失败**（+39 新通过测试；与 `origin/main` 同样的 79 项 pre-existing
> 失败全部是本机缺 `python`（exit 9009）所致的环境失败，CI 在 Linux 上有 python3）。
> **唯独 WPF（`MetBench_Client`，net8.0-windows7.0）云端不编译验证，需在 Windows 上完成 + 验证。**
>
> 实施细节见 [`docs/superpowers/plans/2026-05-29-debt5-anomaly-status-enum-plan.md`](../plans/2026-05-29-debt5-anomaly-status-enum-plan.md)。
> 下面是可直接复制给 Windows VM 操作者的内容。

---

你在 Windows + VS 2022 环境，为 MetBench-V2 完成并验证 debt #5 的 WPF 侧。云端已把
`Anomaly.Status` 从 string 改为强类型 `AnomalyStatus` enum（转移校验 + LiteDB int 迁移），
跨平台项目（BLL.Core/Domain/IDAL/DAL/Tests）已在云端测试通过；WPF 侧因契约变更会编译失败，
**这几处由你修**。

**enum 契约（云端拥有，VM 不要改）**：`AnomalyStatus` = `Unspecified=0, New=1, Investigating=2,
Known=3, ConfirmedBug=4, FalsePositive=5, FixedUpstream=6`（LiteDB 存 int）。
kebab：`new / investigating / known / confirmed-bug / false-positive / fixed-upstream`。
合法转移：`new → investigating`；`investigating → {known, confirmed-bug, false-positive, fixed-upstream}`，
其余抛 `InvalidAnomalyStatusTransitionException`。

**kebab↔enum 辅助（在 `MetBench_Domain`，跨平台，WPF 直接用）**：
- `AnomalyStatuses.ToKebab(this AnomalyStatus)` → kebab 字符串
- `AnomalyStatuses.TryParseKebab(string?, out AnomalyStatus)` → bool
- `from.CanTransition(to)` / `from.AllowedNext()` → 状态机判定（可用来 gate UI 按钮）

**WPF 必改清单（`MetBench_Client`，纯 WPF 侧；不要碰 BLL.Core/Domain 契约）**：

1. `ViewModels/AnomalyListViewModel.cs` L114-116 —— `AnomalyFilter(Status:)` 现在收
   `AnomalyStatus?`。把 `StatusFilter`（string）转成 enum：
   ```csharp
   AnomalyStatus? statusFilter =
       AnomalyStatuses.TryParseKebab(StatusFilter, out var s) ? s : (AnomalyStatus?)null;
   var filter = new AnomalyFilter(
       Severity: string.IsNullOrEmpty(SeverityFilter) ? null : SeverityFilter,
       Status: statusFilter);
   ```
2. `ViewModels/AnomalyListViewModel.cs` L157-167 —— `TransitionStatus` 现在收 `AnomalyStatus`。
   先 `TryParseKebab(TransitionTarget, out var target)`，失败则报错返回；非法转移会抛
   `InvalidAnomalyStatusTransitionException`，现有 try/catch 会把消息塞进 `ErrorMessage`（即截图⑤）。
   可选：用 `SelectedAnomaly.Status.CanTransition(target)` 在 `CanTransition()` 里 gate 按钮。
3. `Views/Pages/AnomalyListPage.xaml` L80 —— DataGrid `Status` 列现在绑定的是 enum，
   默认会显示 `Investigating`（PascalCase）而非 kebab。加一个 `IValueConverter`
   （`AnomalyStatuses.ToKebab((AnomalyStatus)value)`）并在该列 `Binding` 上挂 `Converter=`，
   使其显示 kebab（截图②）。L36 / L96 两个 ComboBox 仍绑 string `StatusFilter` /
   `TransitionTarget`（items 用 kebab），无需改。

**步骤**：
1. `git fetch origin && git switch followup/debts-2026-05-29 && git pull --ff-only`
2. `dotnet build MetBench_Client/MetBench_Client.csproj` — 按上面清单修 WPF 侧编译错误；
   **不要改 BLL.Core/Domain 的 enum 契约**（云端拥有，VM 改会被 CI catch）。出现非上述清单内的
   编译错误，先回报云端再动。
3. `dotnet run --project MetBench_Client`，进 Anomaly 列表页。
4. 截图存 `docs/superpowers/specs/2026-05-29-debt5-vm-verification/`：①build 成功 ②Status 列显示 kebab ③按 investigating 过滤 ④new→investigating 成功 ⑤new→confirmed-bug 非法被拒（ErrorMessage 出现转移非法消息）⑥cross-program 行完好 ⑦LiteDB Status 已是 int。
5. commit（WPF 改动 + 截图）+ push。

**验收**：WPF 0 error；7 截图齐全；非法转移被拒；旧数据完好（迁移后 Status 为 int 且读取正确）；任何不符明确回报云端。
