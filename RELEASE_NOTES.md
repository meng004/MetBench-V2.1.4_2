# MetBench v2.1.0-rc1 — Release Notes

> **Status**: Release Candidate（用于 UAT round-1）
> **Tag**: `v2.1.0-rc1`
> **Commit**: `eccb051e4fbb9a277475d25dce8040385594159e`
> **Date**: 2026-05-16
> **Baseline data**: [`docs/uat/reports/baseline-2026-05-16/`](docs/uat/reports/baseline-2026-05-16/)

## Highlights

### 论文核心
- **F9 R-Case 自动复现**：给定 `KnownBugCode + PipelineContext`，service 自动跑 pipeline → 判定 → 落 Anomaly + 关联 KnownBug + audit log。支持闭环复现 R-Case-4 (OpenMOC narrow basin)。
- **F12 Multi-LLM Consensus**：并发 fan-out 到 N 家 LLM provider，strict-majority consensus + pair-wise Cohen's κ。对应 reviewer 一定会问的 "为什么相信 LLM 判断" 问题。

### 主线功能
- **F5 / F6 / F7 / F14**：soft-delete + schema migration + MetaPattern 集成 + MRPairing m_cmp partner-binding
- **F10 LiteDB Keyset 分页**：高频实体（Executions / Anomalies）深翻页从 O(N) → O(pageSize)，UI infinite scroll 友好
- **F15 / F19**：9 个 feature 文件统一重命名 + `MRBinding.Status` 单一软删机制
- **F16 / F18**：CI 性能基线（< 120 s budget）+ DbConfig 3-tier override（Linux 兼容）
- **F3a / F3b**：Serive → Service 拼写修正（含 [Obsolete] 兼容别名）

### 数据模型
- **24 个 LiteDB collections**（NOETHER MetaPattern 是第 24 个）
- **4 级 MR 语义层级** MetaPattern → MRSchema → MRBinding → MRInstance → Execution
- **8 个 NOETHER MetaPatterns**：4 active (`m_inv` / `m_mono` / `m_conv` / `m_cmp`) + 4 out-of-scope

### UAT 包
- **45 个用例** 分 7 类（CRUD / 主流程 / 发现 & 验证 / R-Case / 可视化 & 报表 / 持久化 / 运营）
- **评价表** + 任务书 + 治理流程 + Issue 模板
- **Baseline trx** 给测试员对照

## 测试态

| 项 | 值 |
|----|----|
| 全套 facts | **458 pass** / 2 skip / 0 fail |
| Cumulative wall | **22.35 s** / 120 s budget |
| Slow tests (>2 s) | 0 |
| BDD smoke (OpenMOC + 3 SUT) | 22 pass / 0 fail |
| CI | ✅ Linux Ubuntu 24.04 全绿 |

## 已知遗留 / 未完成

| 编号 | 范围 | 状态 |
|------|------|------|
| **F11 m_adj MR 族** | 需 OpenMOC 升级支持 adjoint flux | 🚫 blocked，下版本（v2.2）排 |
| **F13 第 3 SUT 接入** | Serpent / MCNP / 其他选型未定 | 🟡 待产品决策 |
| **WPF UI 整改** | UAT 反馈驱动 | 🟡 与 UAT round-1 并行 |

## 升级 / 兼容性

- **新增**：API / 实体 / DB collection 全部为新增，**无破坏式变更**
- **拼写修正**：`MTReportGeneratorSerive` 等 6 个类已重命名为 `*Service`，旧名作 [Obsolete] 别名保留一版（v2.2 删除）
- **DB 自动迁移**：首次启动 v2.1 自动创建新 collection + 索引；不需要手动迁移
- **`.env`**：LLM API key 配置（参考 `docs/uat/setup-guide.md` §3）

## 验收门槛

**v2.1.0 正式版** 发版准入：

1. UAT round-1 由测试员独立跑通，**PASS** 或 **CONDITIONAL PASS** + 修复后 round-2 重验
2. Linux + Windows 双端覆盖率达 ≥ 80% 用例 ✅
3. 性能：cumulative test wall < 120 s（当前 22.35 s ✅）
4. 0 个 🔴 Blocker bug

## 谁该读

| 角色 | 怎么用 |
|------|--------|
| 测试工程师 | 拿这个 rc1 commit 跑 UAT round-1（[任务书](docs/uat/任务书.md)） |
| 项目负责人 | review UAT 结果，决定是否升 v2.1.0 正式版 |
| 开发者 | 看 `git log v2.1.0-rc1` 了解本版改了什么 |
| 论文 reviewer | 看 R-Case 复现 (F9) + Multi-LLM (F12) 两段论文支撑 |

## 链接

- [UAT 包入口](docs/uat/README.md)
- [UAT 任务书](docs/uat/任务书.md)
- [Baseline 数据](docs/uat/reports/baseline-2026-05-16/)
- [W11 计划](docs/superpowers/plans/2026-05-16-w11-plan.md)
- [v2.1 followup pipeline](docs/superpowers/plans/2026-05-15-v2.1-followup-pipeline.md)

---

## 完整 PR 列表（W9-W10）

| PR | 标题 | 类别 |
|----|------|------|
| #27 | v2 P1-P8 ship | 主线 |
| #28-#32 | followup 计划 + T-A/B/C/D/E | 主线 |
| #34 | F7+F5+F6 MetaPattern + soft-delete + burst | 主线 |
| #35 | F19+F15 Status + feature rename | 主线 |
| #37 | F18 DbConfig 3-tier | 主线 |
| #38 | F14+F16 CI baseline + MRPairing | 主线 |
| #40 / #44 | F3a/F3b Serive→Service | 主线 |
| #43 | F9 R-Case 自动复现 | **论文核心** |
| #45 | F12 Multi-LLM consensus | **论文加分** |
| #46 | F10 keyset 分页 | 性能 |
| #47 | UAT 包 45 用例 | UAT |
| #48 | UAT dry-run fixes | UAT |
| #49 | UAT 任务书 | UAT |
| #50 | UAT 治理 + baseline | UAT |

共 **14 个主线 + 4 个 UAT PR** merged 进 v2.1.0-rc1。
