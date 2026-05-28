# T1 闭环 — Windows VM 全量测试 2 项 failure triage 计划

> **Date**: 2026-05-28
> **Branch (cloud)**: `claude/t1-closure-windows-vm-followup`
> **Status**: Active — cloud-side 修复已就位，等 VM 全量验证
> **Driver**: `docs/status/current.md` §3 row "T1 UI MR CRUD" 与 `active-plan-index.md §1` 中悬挂的"Full-suite residual at Windows VM time was 2 failures unrelated to the T1 UI CRUD changes" 跟踪项。本计划是把 T1 从"几乎 Controlled" 推进到 100% Controlled 的收尾 PR。

---

## §1 目标 (Goal)

把 T1 直接支撑层（CLAUDE.md §2.2 T1）推到 **100% Controlled**：

1. 静态确认并修复 cloud-side 已知 Windows-only bug。
2. 在 Windows VM 上跑全量套件，**列名**那 2 个 pre-existing failures。
3. 根据 VM 输出二选一：
   - 若是本计划已修的 bug 影响 → VM run 已自动闭环。
   - 若是新 bug → 现场补 fix（应用同一分支），再次跑通后回云端 push。
4. 同分支落 `docs/status/current.md` Stage-8 行 "T1 UI MR CRUD → Controlled (100%)"，移除 "outside this row" 悬挂语。

**不在 scope**：T4 三条 discoverer / T5 anomaly 分析层 / T6 mutation 扩范围 / 任何新 SUT / 任何 v1.3 verification。

---

## §2 已确认 Windows bug #1（cloud-side 修复）

### 现象（推断）

`MetBenchIoHelperTests.PlainText_round_trip_byte_identical` 期望 LF 字节同一回写，但在 Windows 上 Python `pathlib.Path.write_text(text, encoding="utf-8")` **默认 text 模式 + `newline=None`** 会把 `\n` 翻译为 `\r\n`，导致 `Assert.Equal(content, File.ReadAllText(output))` 失败：

```
expected: "first line\nsecond line\n"
actual:   "first line\r\nsecond line\r\n"
```

CSV 路径不受影响，因为 `_csv.py` 已显式 `path.open("w", newline="", encoding="utf-8")` 关闭翻译。

### 根因

`SUT/_shared/metbench_io/_plain_text.py` 用 `path.read_text(encoding="utf-8")` / `path.write_text(text, encoding="utf-8")`，没显式传 `newline=""`，Linux 上 no-op，但 Windows 上 text-mode 会做 universal newline 翻译。

### 修复（已应用）

`SUT/_shared/metbench_io/_plain_text.py` 改用显式 `path.open("r"/"w", encoding="utf-8", newline="")`：

- `read_plain_text` 不再吃 `\r\n` → `\n`（实际上原状即 LF only 时无差别，但保证 input 含 CRLF 时不静默改写）。
- `write_plain_text` 不再把 `\n` 翻成 `\r\n`，使得 `PlainText_round_trip_byte_identical` 在 Windows 上从红转绿。

属于 b1 类（contract claimed in docstring "byte-identical" 但实现不兑现）；既有测试就是契约 fact，修代码即解。

### 受益面

- `PlainText_round_trip_byte_identical`：Windows 红 → 绿（强预测）。
- `PlainText_no_trailing_newline_preserved`：保持绿（输入无 `\n`，原本无翻译）。
- 所有依赖 plain-text wire 格式的下游 SUT（目前仅 helper 测试覆盖；任何后续 plain-text SUT runner 都会受益）。

---

## §3 待 VM triage 的 Windows bug #2（候选清单 + 触发条件）

VM 跑全量后必有第 2 个 failure（账本说"2 failures"），按风险概率排序候选：

| 候选 | 文件 / 测试 | 触发条件 | 怎么修 |
|---|---|---|---|
| **C1** | `MrArchitectureSchemaP0Tests`、`KeysetPaginationTests` 或其他 LiteDB 测试 | LiteDB engine WAL `*-log` 文件 flush 与 `Dispose()` 阶段 `File.Delete` 之间的 race；Windows 文件锁可能拒绝删除 | 复制 `928e85c` 同款防御：把 `using var db = new LiteDatabase(...)` 改为 `using (...) { ... }` 块，确保 `File.Delete` 前 db 已 Dispose；或在 cleanup 中容忍 `IOException`（参照 `MetBenchIoHelperTests.Dispose` 的 `try/catch swallow`） |
| **C2** | `SystemMtManifestCatalogEditorTests.SaveDraft_adds_new_binding_when_validation_passes` | `File.Replace(tmpPath, path, null)` 在 Windows 上偶发 antivirus 锁；或 `Directory.Delete(recursive: true)` 在 `Dispose()` 时遇到残留 lock | 给 SaveDraft 写入加重试 / 用 `File.Move(tmpPath, path, overwrite: true)` 替代 `File.Replace` |
| **C3** | OpenMC/OpenMOC runner smoke tests 中的 `[Skippable]` 在 Windows 上 importable 检测假阳性 | `OpenMocTestPaths.OpenMocImportable()` 在 Windows VM 上若有遗留 venv 但 import 路径不全，会触发非 skip 路径继而真跑 | 紧检测 `python -c "import openmoc"` 退出码 + stderr |
| **C4** | 任何含 `Environment.NewLine` 或 `\r\n` 字面量的报表渲染断言 | Word/Excel/PDF/HTML renderer 输出在 Windows 上有 `\r\n` 而期望 `\n`，或反向 | 用 `.Replace("\r\n", "\n")` 规范化后比较 |
| **C5** | Path 大小写敏感性 | Windows 文件系统不区分大小写，导致 `Assert.Equal(expectedPath, actualPath)` 在路径比较时大小写差异 | 用 `StringComparer.OrdinalIgnoreCase` 或 `Path.GetFullPath` 规范化 |

**最高优先级**：C1（LiteDB 文件锁），原因是 `928e85c` 已存在的同款 fix 先例 + 我盘点过的多个 V2Schema 测试都在 `Dispose()` 里直接 `File.Delete(_dbPath)` 而 repo 是无 Dispose 的（每次 method 内 `using var` 已 Dispose，但 LiteDB 的 WAL `*-log` 文件可能有微秒级 flush 窗口）。

---

## §4 Cloud-side 改动清单

| 文件 | 改动 |
|---|---|
| `SUT/_shared/metbench_io/_plain_text.py` | `open(..., newline="")` 显式禁用 universal newline；docstring 解释为什么。**已应用**。 |
| `docs/superpowers/plans/2026-05-28-t1-closure-windows-vm-followup-plan.md` | 本计划文件 |
| `docs/status/current.md` | **待 VM 结果回来再补**：Stage-8 行 "T1 UI MR CRUD" 改为 "Controlled (100%)"，加 VM full-suite SHA 引用；§7 step 6 删除 "tracked outside this row" 悬挂语 |
| `docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md` | **待 VM 结果回来再补**：行 18 同步修订 |

---

## §5 VM 执行步骤（手动操作 + Claude Code CLI）

### 5.1 准备

```powershell
# In Windows VM, in the MetBench repo clone root
git fetch origin claude/t1-closure-windows-vm-followup
git checkout claude/t1-closure-windows-vm-followup
git reset --hard origin/claude/t1-closure-windows-vm-followup
dotnet restore MetBench.sln
dotnet build MetBench.sln --no-restore
```

### 5.2 跑全量并捕获失败

```powershell
# 跑 cross-platform 测试项目（与云端 CI 等价）
dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj `
  --no-build `
  --logger "trx;LogFileName=t1-closure-vm-results.trx" `
  --logger "console;verbosity=normal" `
  | Tee-Object -FilePath t1-closure-vm-stdout.log
```

预期：通过 1462 或 1463（含本 PR 的 plain-text fix），失败 0–1，跳过 16。若仍有 1 failure：

```powershell
# 把失败测试名抽出来
Select-String -Path t1-closure-vm-stdout.log -Pattern "Failed " -Context 0,2 | Tee-Object failures.log
```

### 5.3 触发 Claude Code CLI（在 VM 仓库根）

把 §6 提供的 prompt 一段复制进 Claude Code CLI。Claude 会读 `failures.log` + `t1-closure-vm-results.trx`，对照本计划 §3 候选清单定位根因，应用最小 fix（沿用同分支），重跑测试验证，再 push。

### 5.4 完成判定

- ✅ `dotnet test MetBench_SystemMT.Tests` 全量绿（0 failed）。
- ✅ Windows VM 的 `dotnet build MetBench_Client/MetBench_Client.csproj` 0 errors。
- ✅ Cloud CI（GitHub Actions `test` job）该分支 PR 上保持绿。
- ✅ `docs/status/current.md` Stage-8 行 "T1 UI MR CRUD" 改 "Controlled (100%) — VM full-suite green at SHA `<vm-pass-sha>`"。

---

## §6 Claude Code CLI 任务提示词（在 VM 仓库根直接 paste）

```
你正在 MetBench Windows VM 仓库根目录，处于 git 分支
claude/t1-closure-windows-vm-followup。

任务：T1 收尾 — 让 MetBench_SystemMT.Tests 在 Windows VM 上全量绿。

背景：
- 仓库 docs/superpowers/plans/2026-05-28-t1-closure-windows-vm-followup-plan.md
  是本任务的主计划，含 §3 候选 bug 清单。
- 云端已修复确认 bug #1 (SUT/_shared/metbench_io/_plain_text.py 的
  newline 翻译问题)。
- 历史账本（docs/status/current.md §3 T1 UI MR CRUD 行）登记还有 2 个
  pre-existing Windows-VM full-suite failures。bug #1 是其一；剩 1 个 待定。

步骤：

1. 跑 dotnet test 并捕获结果：

       dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj `
         --no-build `
         --logger "trx;LogFileName=t1-closure-vm-results.trx" `
         --logger "console;verbosity=normal" 2>&1 |
         Tee-Object t1-closure-vm-stdout.log

2. 解析 t1-closure-vm-stdout.log，提取所有 Failed 测试名 + assertion
   失败消息 + stack frame 顶帧。

3. 如果 Failed = 0：跳到第 7 步（直接收尾）。

4. 如果 Failed >= 1：对每个失败逐一对照计划 §3 候选清单（C1–C5）匹配根
   因。匹配不到则做 root-cause analysis：读对应测试代码 + 被测代码，
   定位为何 Windows 行为不同于 Linux。

5. 应用最小 fix（沿用当前分支 claude/t1-closure-windows-vm-followup）：
   - 严格遵守 CLAUDE.md §0.5 ANTI-UNREQUESTED-EDIT：只改和该 failure 直接
     相关的位置；不顺手重构。
   - 不动 MetBench_Client/ 下 *.xaml*（CLAUDE.md §9）。
   - 不动 SemanticCatalogBoundaryTests 涉及的边界（CLAUDE.md §6）。
   - 修法首选：和 PR #176 commit 928e85c 同款 "把 `using var ...` 改为
     `using (...) { }` 块以保证 Dispose 早于 File.Delete"；或在 Dispose 用
     `try { File.Delete(...) } catch (IOException) { }` 容忍 Windows 文件
     锁短暂占用。

6. 重跑步骤 1，确认 Failed = 0。

7. 收尾：
   a. 用 git diff 把所有改动整理清楚。
   b. 编辑 docs/status/current.md：
      - §3 Stage-8 "T1 UI MR CRUD" 行尾追加 "VM full-suite green at SHA
        <current-sha> on Windows after PR-T1-CLOSURE (was 2 failures →
        0)"。把 "Follow-up to triage those 2 Windows-only failures is
        tracked outside this row." 整句删掉。
      - §2 表格中"Current SUT / equation / MR inventory" 行不变（本 PR
        不动 inventory）；"Latest auditable code-test baseline" 不变（VM
        是补充验证不是新基线，云端 CI 仍以最近的 cloud baseline 为准）。
   c. 编辑 docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md
      行 18（T1 UI MR CRUD 行）：把 "Full-suite residual ... 2 failures
      ..." 整句改为 "Full-suite Windows VM verification green at SHA
      <current-sha> via PR-T1-CLOSURE (claude/t1-closure-windows-vm-followup)。"
   d. 不要新增任何文件（plan/spec/checklist），不要写 MEMORY 笔记。
   e. git add 改动的具体文件（不要 git add -A 或 git add .），写 commit：

         git commit -m "fix(t1): close Windows VM full-suite — <one-line summary>" \
           --signoff?=false

      commit message 简洁列出 (1) plain-text newline fix（云端已就位）
      (2) 新发现的 fix 是哪个 + 文件名 + 单行解释 (3) VM dotnet test
      pass/fail/skip 数字。
   f. git push origin claude/t1-closure-windows-vm-followup
      （网络失败重试 4 次：2s, 4s, 8s, 16s 指数退避）。
   g. 不要建 PR — 由用户决定何时建。
   h. 输出最终 status：失败修了哪些（按文件名 + 行号引用）、新 VM 测试
      结果、push 的 SHA。

8. 异常停止条件：
   - 如果 fix 牵涉 MetBench_Client/*.xaml* 或 WPF dispatcher 行为，停下
     来汇报；不擅自改 UI。
   - 如果一个 failure 经分析是真实 cross-platform 缺陷而非简单文件锁 /
     newline / 编码问题（如需要重写 launcher 流程），停下来汇报。
   - 如果失败数 > 2，停下来汇报全量列表，不自动修。
   - 如果 fix 后 Windows 全量绿但破坏了云端 cross-platform 测试预期（断
     言变化），停下来汇报。

通常完成时间：30–60 分钟。完成后回到本 chat 把 VM 输出贴回。
```

---

## §7 验证 (Verification)

### 7.1 Cloud-side

- `_plain_text.py` 已修；docstring 已交代 Windows newline 翻译陷阱。
- 本 PR 不引入新测试（既有 `PlainText_round_trip_byte_identical` 即回归守卫）。
- 云端 GitHub Actions `test` job 必须保持绿（无新 fail / 无新 skip）。

### 7.2 VM-side（user-driven via §6 prompt）

- `dotnet test MetBench_SystemMT.Tests` 全量绿（Failed = 0）。
- `dotnet build MetBench_Client/MetBench_Client.csproj` 0 errors。
- 第 2 个 bug 的 root cause + fix 由 §6 prompt 跑出来后落进同一分支。

### 7.3 文档完整性

- `docs/status/current.md` 与 `active-plan-index.md` 同步刷新（在 VM 跑通后）。
- 本计划文件归入 Active → 跑通后改 Completed 并移入"已完成可参考"段。

---

## §8 风险 & Stop 条件

- **Risk A** — VM 上的 `pytest`-style 误报：如果 Windows VM 跑出 > 2 failures，可能是 VM 环境本身有问题（如 Python venv 缺包），不是 cloud 代码 bug。stop 并汇报。
- **Risk B** — 修第 2 个 bug 时若需改 `MetBench_BLL.Core/` 公共 API 形状，违反 §6 type-leakage rule。stop 并汇报。
- **Risk C** — fix 触发 `SemanticCatalogBoundaryTests` 或 `SemanticCatalogNamingBoundaryTests` 红。stop 并汇报，因为这意味着碰了不该碰的边界。
- **Stop** — 若云端 main 已被推进且本分支 merge-base 太旧：先 `git fetch + rebase` 再继续，不强推。

---

## §9 完成判定 (Done When)

1. Cloud-side `_plain_text.py` fix 已 push 在分支 `claude/t1-closure-windows-vm-followup`。
2. VM 上 §6 prompt 跑完，分支头新增 (a) fix 第 2 个 bug 的 commit + (b) 账本刷新 commit。
3. Cloud CI 该分支 PR 通过 hard `test` gate。
4. `docs/status/current.md` Stage-8 T1 UI MR CRUD 行 = "Controlled (100%) — VM full-suite green at SHA `<sha>`"。
5. Active plan index 中本计划行从 "Active" 改 "Completed"。
6. （可选）该分支被 squash merge 进 `origin/main`，PR 标题 `fix(t1): close Windows VM full-suite — T1 100% controlled`。

---

## §10 引用

- `docs/status/current.md` §3 row "T1 UI MR CRUD"（pre-fix 状态）
- `docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md` row 18
- Commit `928e85c` `fix(systemmt-tests): close LiteDB before deleting schema db`（同款 race fix 先例）
- `CLAUDE.md` §0 / §0.5 / §6 / §9 / §11 / §12
