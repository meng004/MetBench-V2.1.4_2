# MetBench v2.1.0 / v2.1.1 / v2.1.2 — Release Notes

> **Status**: **Released** — `release-v2.1.2` tagged on `ae4370f`（2026-05-21，polish 批次：HandyControl 移除 + UAT runbook 对齐）
> **Release tags**: [`release-v2.1.0`](https://github.com/meng004/MetBench-V2.1.4_2/releases/tag/release-v2.1.0) · [`release-v2.1.1`](https://github.com/meng004/MetBench-V2.1.4_2/releases/tag/release-v2.1.1) · [`release-v2.1.2`](https://github.com/meng004/MetBench-V2.1.4_2/releases/tag/release-v2.1.2)
> **Release commits**: v2.1.0 = `9b89f9b`（含 PR #75 anomaly wiring + PR #77 ObjectId→Guid 结构性修 + round-2 全 5 UC PASS）；v2.1.1 = `7a6e228`（PR #79）；v2.1.2 = `ae4370f`（PR #80 + #81）
> **Baseline data**: cloud [`docs/uat/reports/baseline-2026-05-17/`](docs/uat/reports/baseline-2026-05-17/) + Windows UAT round-2 [`docs/uat/reports/round-2-windows-2026-05-19-limeng/`](docs/uat/reports/round-2-windows-2026-05-19-limeng/)

---

## v2.1.2 — polish 批次（2026-05-21）

### 变更

**PR #80 — refactor: 移除 HandyControl 依赖**

- 删除 `HandyControl 3.5.0` NuGet 包（`MetBench_Client.csproj`）
- 删除 `App.xaml` 中 HC 主题 ResourceDictionary 合并（`SkinDefault.xaml` / `Theme.xaml`）；Wpf.Ui 主题已覆盖所有画刷，无 styling 回归
- 删除 `MainWindow.xaml` 死 `xmlns:hc` 声明（body 无使用）
- 新增 `Controls/SimplePagination.xaml` UserControl：`◀`/`▶` `ui:Button` + 页码 TextBlock，暴露 `PageIndex`（TwoWay DP）/ `MaxPageCount` DP / `PageUpdated` RoutedEvent
- 6 个翻页页面（AutoDetectMRPage / MRDisplayPage / DomainManagementPage / MRRecommendationPage / MRManagementPage / ApplicationManagementPage）从 `hc:Pagination` 换为 `controls:SimplePagination`；各 code-behind 加 `pagination_PageUpdated` → `ViewModel.reload_ItemsSource()` 事件处理
- 删除 `MRDisplayPage.xaml` 中死注释块（`hc:Interaction.Triggers` / `hc:EventToCommand`）
- 验证：`grep -rn 'HandyControl|hc:' MetBench_Client/` → 0 命中；`dotnet build MetBench_Client` → 0 errors

**PR #81 — docs: UAT runbook 对齐 v2.1 WPF UI**

基于 round-1（limeng 2026-05-18）实测结果，修正 `docs/uat/runbooks/windows-uat-round-1.md` 中 8 个 UC 的过时描述：

| UC | 修正内容 |
|---|---|
| A1 | 补 `SoftwareUnderTest` 必填字段 + "Upl"/"Unzip" 文件上传说明 |
| A3 | 修正"软删 `Status=deleted`" → "硬删，行直接从 DB 移除" |
| A4 | 标注 "Bound Applications" 多选框在 v2.1 UI 中不存在（backlog G-1）；标注 "Desciption" 拼写错误（backlog G-2） |
| A5 | 更新为实际 MR 表单字段（Context / Granularity / Hierarchy / InputPattern / OutputPattern / Dimensions / ApplicationName checkbox / ArityOfMR / Operator / Expression） |
| B2–B6 | §4.2 标题改为 "System MT 主链路"；B2 指向 System MT 页；B3 标 N/A（单步 Run 替代 Generate Follow-up）；B5 更新列名；B6 标 N/A（无图表区） |
| E3 | 更新为实际 UI（Report Type 下拉 + ExportReport，无 "Generate All"，无 scope 下拉） |
| E4 | 标 N/A（"View HTML in App" 按钮不存在，backlog G-3） |
| E5 | 标 N/A（"Dashboard 主页" nav 项不存在，backlog G-4） |

新增 [`docs/superpowers/plans/2026-05-21-uat-ui-gaps-backlog.md`](docs/superpowers/plans/2026-05-21-uat-ui-gaps-backlog.md)，记录 5 个 UI 功能缺口（G-1 至 G-5）供下个 sprint 决定是否补实现。

### 升级 / 兼容性

- 无 API 变更，无 LiteDB schema 变更
- WPF 项目移除 HandyControl 包引用后需重新 `dotnet restore`（首次 build 会下载新 lock file）
- 翻页功能行为与原 `hc:Pagination` 等价（PageIndex TwoWay、PageUpdated 触发 reload）

---

## v2.1.0 Highlights — Post W11-W12

### 论文核心（v2.1.0-rc1 base + W11-W12 强化）

- **F9 R-Case 自动复现**（v2.1.0-rc1 carryover）：给定 `KnownBugCode + PipelineContext`，service 自动跑 pipeline → 判定 → 落 Anomaly + 关联 KnownBug + audit log
- **F12 Multi-LLM Consensus (W11.2 实证跑通)**：DeepSeek + OpenAI + Claude **真实** 60/60 calls，consensus accuracy 100%，mean pair-wise Cohen's κ = 0.925。唯一非 unanimous 行（`MR-sin-full-period`）展示 LLM 间"数学 vs 浮点严格等"口径分歧的真实信号，strict majority 正确吸收。数据：[`docs/experiments/2026-05-w11-llm-consensus/`](docs/experiments/2026-05-w11-llm-consensus/)
- **F13 第 3 SUT OpenMC 接入 (W12)**：与 OpenMOC 同域不同算法（MOC deterministic vs MC stochastic），强化 `m_cmp` 跨实现一致性。cmake 源码 build + Python bindings + 4 cross-program BDD scenarios

### 主线功能（v2.1.0-rc1 carryover）

- **F5 / F6 / F7 / F14**：soft-delete + schema migration + MetaPattern + MRPairing m_cmp partner
- **F10 LiteDB Keyset 分页**：深翻页 O(pageSize)
- **F15 / F19**：9 个 feature 文件重命名 + `MRBinding.Status` 单一软删
- **F16 / F18**：CI 性能基线（< 120 s）+ DbConfig 3-tier override
- **F3a / F3b**：Serive → Service 拼写修正（含 [Obsolete] 兼容别名）

### W11-W12 新增

- **命名统一**：launcher 层 `scenario` → `MR` 彻底改名（65 处），消除与 BDD Gherkin Scenario 撞名混淆。persistence 层 `ScenarioName` → `MrName` + LiteDB **自动 schema migration**（兼容现有 .Litedb）。详见 [`docs/PROJECT-STRUCTURE.md`](docs/PROJECT-STRUCTURE.md) §8
- **F11 m_adj 路径 A 启动**：`tools/check_openmoc_adjoint.sh` + GitHub Actions 月度 cron（`17 3 1 * *` UTC）→ 上游有 adjoint commit 自动开 issue。`m_adj` 暂保留 `out-of-scope`，paper "future work" 段
- **flake 根治**：`DbConfig.Instance` 跨 class 竞态用 `[Collection("DbConfigGlobal")]` 注解 6 个类根治
- **UAT 双轨**：47 用例 markdown 三段式（初始条件 / 操作步骤 / 断言）+ 21 用例 BDD wrapper（机器可验，反射 + baseline trx 双重检查）+ Windows round-1 完整 runbook

### 数据模型

- **24 个 LiteDB collections**（NOETHER MetaPattern 是第 24 个）
- **4 级 MR 语义层级** MetaPattern → MRSchema → MRBinding → MRInstance → Execution
- **8 个 NOETHER MetaPatterns**：4 active (`m_inv` / `m_mono` / `m_conv` / `m_cmp`) + 4 out-of-scope

### UAT 包

- **47 用例 markdown** 分 7 类（A 管理 CRUD / B MT 主流程 / C 发现 & 验证 / D R-Case / E 可视化 & 报表 / F 持久化 / G 运营），全部三段式
- **21 用例 BDD wrapper**（Part F + G + C 共 21 个 .feature，自动反射 + baseline trx 检查）
- **Windows UAT runbook**：**21 个 WPF UI 用例**（A1-A7 + B1-B9 + E1-E5）2-2.5 小时 1 轮完整指导（[`docs/uat/runbooks/windows-uat-round-1.md`](docs/uat/runbooks/windows-uat-round-1.md)）；其余 5 个 CLI 用例（A8 / D1 / D2 / E6 / E7）已由 cloud baseline 完全覆盖，**不重跑**
- **Baseline 2026-05-17**：cloud-side reference，**521/521 Pass / 0 Skip / 0 Fail**

## 测试态（baseline-2026-05-17）

| 项 | 值 |
|---|---|
| 全套 facts | **521 Pass** / 0 Skip / 0 Fail |
| 整体 wall | **35 s** |
| Cumulative wall | **73.02 s** / 120 s budget |
| Slow tests (>2 s) | 6（全部 OpenMOC/OpenMC 物理跑，合理） |
| BDD smoke | **30 Pass** / 1 Skip |
| UAT BDD filter | **48 Pass** / 0 Skip |
| CI | ✅ Linux Ubuntu 24.04 全绿 |

## 已知遗留 / 未完成

| 编号 | 范围 | 状态 |
|---|---|---|
| **F11 m_adj MR 族** | OpenMOC 上游升级 adjoint flux | 🟢 路径 A 月度监控在线，v2.2 候 |
| **第 5 SUT** | SU2 / FEniCS / OpenFOAM | 🟡 等论文 reviewer 反馈 |
| **WPF UI 验收** | Windows round-1 | 🟡 跟 v2.1.0 同步推进 |

## 升级 / 兼容性

- **LiteDB schema migration**：v2.1.0 首次开 `MR.Litedb` 自动把 `ScenarioName` field 改名 `MrName`，幂等
- **API 改名**：launcher `ScenarioDescriptor` / `ScenarioRunResult` / `ISystemMtScenarioLauncher` 等改为 `Mr*` 对应名。**WPF + 第三方调用方需要更新引用**，无兼容别名（cloud + Windows agents 已同步）
- **`.env`**：LLM API key 配置（参考 `docs/uat/setup-guide.md` §3）

## 验收门槛

**v2.1.0 正式版**发版准入：

1. UAT round-1 由 Windows 测试员独立跑通，**PASS** 或 **CONDITIONAL PASS** + 修复后 round-2 重验
2. Linux baseline ≥ 90% Pass ✅（当前 100%）
3. 性能：cumulative test wall < 120 s ✅（73.02 s）
4. 0 个 🔴 Blocker bug

## 谁该读

| 角色 | 怎么用 |
|---|---|
| Windows 测试员 | 拿 baseline-2026-05-17 commit 按 [`windows-uat-round-1.md`](docs/uat/runbooks/windows-uat-round-1.md) 跑 round-1 |
| 项目负责人 | review round-1 + dashboard.md PASS → tag `release-v2.1.0` |
| 新加入开发者 | 先看 [`docs/PROJECT-STRUCTURE.md`](docs/PROJECT-STRUCTURE.md) 拿全图 |
| 论文 reviewer | 看 W11.2 LLM 数据 + R-Case 复现 + 4 SUT (含 OpenMC m_cmp) |

## 链接

- 📘 [项目结构 + 测试矩阵 `docs/PROJECT-STRUCTURE.md`](docs/PROJECT-STRUCTURE.md)
- 📋 [UAT 包入口](docs/uat/README.md)
- 📊 [Baseline 2026-05-17](docs/uat/reports/baseline-2026-05-17/)
- 🪟 [Windows UAT runbook](docs/uat/runbooks/windows-uat-round-1.md)
- 🧪 [W11.2 LLM consensus 实验](docs/experiments/2026-05-w11-llm-consensus/)
- 🗓 [W11 计划](docs/superpowers/plans/2026-05-16-w11-plan.md) · [F13 RFC](docs/superpowers/plans/2026-05-17-f13-third-sut-rfc.md) · [F11 RFC](docs/superpowers/plans/2026-05-17-f11-unlock-rfc.md) · [F11 status](docs/superpowers/plans/2026-05-17-f11-status.md)
- 📜 [v2.1.0-rc1 release notes (历史) — 见 git log `v2.1.0-rc1` tag](https://github.com/meng004/MetBench-V2.1.4_2/tags)

---

## v2.1.0 涉及 PR 一览（W9-W12 累计）

### W9-W10 (v2.1.0-rc1)

| PR | 标题 | 类别 |
|---|---|---|
| #27 | v2 P1-P8 ship | 主线 |
| #28-#32 | followup 计划 + T-A/B/C/D/E | 主线 |
| #34 | F7+F5+F6 MetaPattern + soft-delete + burst | 主线 |
| #35 | F19+F15 Status + feature rename | 主线 |
| #37 | F18 DbConfig 3-tier | 主线 |
| #38 | F14+F16 CI baseline + MRPairing | 主线 |
| #40 / #44 | F3a/F3b Serive→Service | 主线 |
| #43 | F9 R-Case 自动复现 | **论文核心** |
| #45 | F12 Multi-LLM consensus（架构） | **论文加分** |
| #46 | F10 keyset 分页 | 性能 |
| #47-#50 | UAT 包 45 用例 + 治理 + baseline | UAT |

### W11-W12 (Stage 7, post-rc1)

| PR | 标题 | 类别 |
|---|---|---|
| #54 | W11.3 RFC（F13 + F11 解锁） | 决策 |
| #57 | W11.2 Multi-LLM 真实跑通 + W12 F13 OpenMC 接入 | **论文加分** + 主线 |
| #58 | scenario → MR launcher 改名 | 命名清理 |
| #59 | UAT BDD 21 用例（Part F/G/C） | UAT 双轨 |
| #60 | UC-C11 unignore（PR #57 land 后） | UAT |
| #61 | W12 F11 m_adj 路径 A 被动监控 | v2.2 准备 |
| #62 | LiteDB ScenarioName → MrName + schema migration | 命名清理 |
| #63 | UAT 47 用例三段式重写 | UAT |
| #64 | baseline-2026-05-17 + DbConfig flake 根治 | UAT |
| #65 | Windows UAT runbook | UAT |
| #66 | PROJECT-STRUCTURE.md + 文档同步 | 文档 |
| #67 | UAT Windows scope 收缩 26 → 21 | UAT |
| #68 / #69 | Stage 8 brainstorming + rev3 plan (5 MP × 84 MR 母集) | Stage 8 启动 |

### v2.1.0 收尾（2026-05-18 → 05-19）

| PR | 标题 | 类别 |
|---|---|---|
| #70 | UAT round-1 Windows CONDITIONAL PASS 报告 | UAT |
| #71 / #72 | round-1 fix: UC-A2 excludeSelf + UC-A5 Entity.ToString (2 Major) | bugfix |
| #73 | Docker SUT 镜像（`metbench-sut` + `Dockerfile.runtime` all-in-container） + VM 任务书 | infra |
| #74 | Docker SUT 任务书 follow-up（Track C `--no-build` + 中国网络配置） | UAT 文档 |
| #75 | UC-B7 — 失败 run 自动建 Anomaly（`SystemMtMrLauncher` 接线） | bugfix |
| #76 | issue: UC-B7 round-2 跑出来的 ObjectId↔Guid cross-track bug | issue |
| #77 | UC-B7 ObjectId → Guid 结构性修 + round-2 5/5 PASS + closes #76 | bugfix + UAT |

### v2.1.1 hotfix（2026-05-19）

| PR | 标题 | 类别 |
|---|---|---|
| #79 | `LiteDbSystemMtResultRepository` 加 `UTC_DATE` pragma；`SystemMtResultRecord.RunAt` 保持 `Kind=Utc` 跨进程 | bugfix |

LiteDB v5 默认反序列化把 `DateTime` 还原为 `Kind=Local`，在非 UTC 主机（如 Windows VM CST=UTC+8）让 `Ticks` 偏移 8 小时，间接破坏两个 `KeysetPaginationTests`（`GetByStatusKeyset_*` 系列依赖 `QueuedAt` 排序）。Linux CI 跑 UTC 所以一直绿，Windows VM 每次复现。修法：连接串加 `UTC_DATE=true` pragma，让 LiteDB 反序列化时统一返回 UTC `DateTime`。

共 v2.1.0/v2.1.1 累计 **22+ 个主线 + 19 UAT/文档/infra PR**。

### Windows UAT 双轮总结

- **Round-1** (2026-05-18, commit `0c0cd24`, limeng on Win11+Parallels)：**CONDITIONAL PASS** — 26 UC 跑 21 个 WPF + 5 cloud-covered；6 ✅ / 10 ⚠️ / 3 ❌；找到 3 个 Major bug (UC-A2/A5/B7) 由 PR #71/#72/#75 修复。
- **Round-2** (2026-05-19, commit `9b89f9b`, limeng on Win11 ARM+Parallels)：**PASS 5/5** — UC-A2 / UC-A5 / UC-B7 全部 fix verified，加跑 UC-B8 + UC-B9 同步通过。Round-2 过程中又命中 UC-B7 cross-track ObjectId↔Guid bug（issue #76），由 PR #77 inline 做结构性修复（`SystemMtResultRecord.Id: string → Guid` + 一次性 idempotent migration + 3 个回归测试）。
- **Post-release Windows TZ bug**：v2.1.0 tag 后 Windows TZ 上 2 个 `KeysetPaginationTests` 一直挂（Linux CI 因 UTC 漏报），PR #79 加 LiteDB `UTC_DATE` pragma 修，tagged 为 `release-v2.1.1`。
- **Release 决策矩阵**：`1 轮 CONDITIONAL PASS + 全部 Major 已有 fix PR → 待 fix merge 后再验 1 轮` 满足，v2.1.0 已 tag；v2.1.1 是 post-release 兼容性修，不重走 UAT 流程。
