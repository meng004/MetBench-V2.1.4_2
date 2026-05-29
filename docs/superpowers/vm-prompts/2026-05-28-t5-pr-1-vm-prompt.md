# VM 提示词 — T5 PR-1: Orphan sweeper backend + UI button

> **使用方式**：在 Windows VM 中启动 Claude Code 会话后，粘贴本文件全部内容作为 prompt。VM agent 按 Step 1-4 执行。
> **PR**: meng004/MetBench-V2.1.4_2#TBD (待 cloud agent 开 PR 后填入)
> **分支**: `claude/t5-pr-1-orphan-sweeper`
> **计划**: [`docs/superpowers/plans/2026-05-28-t5-anomaly-workflow-closure-plan.md`](../plans/2026-05-28-t5-anomaly-workflow-closure-plan.md) §3

---

## 项目背景与硬约束

你在 Windows VM 中，working dir 是 MetBench V2.1.4_2 的本地 clone。
项目：.NET 8 + WPF System-MT 平台。

### 硬约束
- CLAUDE.md §0.5 ANTI-UNREQUESTED-EDIT：只动被指定文件的被指定位置
- CLAUDE.md §9：VM-track 只能动 `MetBench_Client/`，禁动 `MetBench_BLL.Core/` / `MetBench_DAL/` public 类型
- CLAUDE.md §0：声称"已验证 / 已通过"必须配真实工具输出（dotnet 输出、文件存在、截图）
- 截图必须真实跑得来，不允许伪造路径

### 禁止
- 改 `MetBench_BLL.Core/SystemMT/Anomaly/*` 产品代码
- 跳过 hook、`--no-verify`、`--amend` 已 push 的 commit
- 在没有 dotnet output 证据时声称 build/test 成功

---

## Step 1 拉最新代码并核对 HEAD

```powershell
git fetch origin
git checkout claude/t5-pr-1-orphan-sweeper
git pull --ff-only
git log --oneline -3   # 报告当前 HEAD SHA
```

---

## Step 2 编译

```powershell
dotnet build MetBench_Client/MetBench_Client.csproj -c Debug --no-restore
# 期望 0 errors（warnings 是 pre-existing 噪声）
```

**如有 MessageBox 类 CS0104 二义性**（G12 grep 应已挡，但如出现）：
- 仅 fully-qualify 为 `System.Windows.MessageBox` 等；不动其他代码

---

## Step 3 全量测试

```powershell
dotnet test MetBench_SystemMT.Tests --no-build
# 期望：1509 baseline + 新增 6 facts (AnomalyOrphanSweeperTests) = 1515 / 0 / 12
# 0 fail 是硬指标
```

---

## Step 4 数据准备（制造 orphan）

启动 WPF 客户端，按下列流程制造一个 orphan Anomaly：

```powershell
dotnet run --project MetBench_Client
```

UI 流程（用 UIA 或手动）：
1. 进 "System MT" 页，跑一个 MR 使其 fail（推荐：`openmc-pincell-particle-count-convergence` 之类有 noise 的 MR；如无 fail，可手动改 sample case 制造 fail）。
2. 记下生成的 Anomaly id（在 "Anomalies" 页可见）
3. 进 "System MT Execution History"，多选刚才生成的 Execution row，Delete
4. **关键**：此时该 Anomaly 的 ResultId 已悬空（PR-4 删 Execution 不级联删 Anomaly，这是 F4 deferral 的 by design）

---

## Step 5 UI 验证（截图存 `docs/superpowers/specs/2026-05-28-t5-pr-1-vm-verification/`）

```powershell
dotnet run --project MetBench_Client
```

按顺序截图：

1. **`01-orphan-before-sweep.png`**：nav 进 "Anomalies"，DataGrid 显示包含刚才制造的 orphan Anomaly 行。状态栏显示总数（如 "Loaded N anomalies"）。

2. **`02-litedb-before.png`**：用 LiteDB Studio 打开
   `MetBench_Client/bin/Debug/net8.0-windows7.0/SystemMT.Litedb`
   截图 `Anomaly` collection 行数（如 N 行）+ 对照 `SystemMtResults` collection（少了被删 Execution 的 row）。

3. **`03-sweep-button-visible.png`**：返回 AnomalyListPage，filter 工具栏旁可见 "Sweep orphans" 按钮 + status message 区。

4. **`04-sweep-confirm-or-direct.png`**：点击 "Sweep orphans" 按钮。
   - 如弹出 MessageBox 二次确认 → 截图，点确认
   - 如直接执行 → 截图执行后状态

5. **`05-sweep-status.png`**：sweep 完成后状态栏显示形如
   `Swept N, retained M, failed 0.`（具体数字以实际为准）。
   DataGrid 自动 refresh，orphan 行消失。

6. **`06-litedb-after.png`**：再开 LiteDB Studio，截图 `Anomaly` collection 行数减少了 N（对应 swept count）。证明跨表清理生效。

7. **`07-second-sweep-idempotent.png`**：再点一次 "Sweep orphans"，状态栏应显示 `Swept 0, retained M, failed 0.` 证明幂等。

---

## Step 6 VM SHA 回写

```powershell
git status   # 确认无 stray changes（除截图 + 可能的 MessageBox 修复）
git add docs/superpowers/specs/2026-05-28-t5-pr-1-vm-verification/
git -c commit.gpgsign=false commit -m "docs(t5-pr-1-vm): 7 screenshots from Windows VM verification"
git push
```

在 PR body §4 Windows 节追加：

````markdown
✅ VM verification complete at SHA `<git rev-parse HEAD>`:

| Check | Result |
|---|---|
| `dotnet build MetBench_Client/MetBench_Client.csproj` | 0 errors |
| `dotnet test MetBench_SystemMT.Tests` | <P> / 0 / 12 |
| UI smoke (sweep flow) | 7 screenshots at `docs/superpowers/specs/2026-05-28-t5-pr-1-vm-verification/` |
| Idempotency check | 二次 sweep → `Swept 0, retained M, failed 0` ✓ |
| Cross-table cleanup | LiteDB Anomaly collection N → N - SweptCount ✓ |

VM environment: Windows 11 + .NET 8.0.x + Parallels VM
````

---

## 重点验证 — 这是 F4 orphan sweeper 的 acceptance gate

| Gate | 标准 |
|---|---|
| ✓ Step 5.1 | orphan Anomaly 在 sweep 前可见 |
| ✓ Step 5.5 | sweep 后状态栏报告 N swept，N > 0 |
| ✓ Step 5.6 | LiteDB Anomaly collection 行数减少 |
| ✓ Step 5.7 | 二次 sweep 幂等（SweptCount=0）|

如 Step 5.6 LiteDB 计数未减少：cloud-side wiring fix 没生效，dump VM 控制台日志 + 在 PR comment 报告，**不自行修 BLL.Core**。

---

## 异常处理

- 编译报错且根因在 `MetBench_Client/` → 修；其他项目报错 → 在 PR comment 报告原因并暂停，请云端处理
- 全量测试 fail → 列出失败 test 名 + stderr，在 PR comment 报告，不自行 fix BLL.Core
- UI 上 "Sweep orphans" 按钮不出现 → 检查 `App.xaml.cs` DI + AnomalyListPage XAML 是否合入；如确实缺失，云端漏 push 了

---

## 完成后通知 cloud agent

回复一条 summary，至少含：
- VM HEAD SHA
- build / test 结果数字
- screenshot 路径 + 计数 (7 张)
- LiteDB Anomaly collection 前后行数对比
- Sweep status 实测数字（SweptCount / RetainedCount / FailedCount）
- 任何 VM-side fix（disambiguation 等）的文件 + 行号 + 1-3 行 rationale

cloud 收到后会立即 merge PR-1。
