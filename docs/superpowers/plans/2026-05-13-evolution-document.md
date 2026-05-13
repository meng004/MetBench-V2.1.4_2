# MetBench 演化文档撰写计划

**目标**：把 MetBench 从最初版（commit `ec7f658`，2026-05-07）到当前 v2 设计（commit `b8401fd`，2026-05-13）的全过程**整理成单一权威文档**，便于交接、回顾、论文引用。**重点突出系统级 MT pipeline 在每个阶段的具体形态与演化**。

**日期**：2026-05-13
**状态**：active — `writing-plan` 阶段（本文件）→ `executing-plan` 阶段（产出 `docs/design/evolution.md`）

---

## Scope Guard

本计划**只**产出**回顾性**演化文档。

**不**包含：
- 重新设计 v2（已在 `docs/design/` 完成）
- 实施 P1 schema 扩展（暂停中）
- 续写未来路线图（已在 `migration-plan.md`）

---

## 信息来源 — 已确认

| 来源 | 提供什么 |
|------|--------|
| Git log（91 个 commit） | 时间线 + commit 消息 |
| `ec7f658` Initial commit | 最初版文件清单 + WPF + HandyControl Pagination 现状 |
| `MetBench_Client/Views/Pages/*.xaml` 6 个文件 | HandyControl `hc:Pagination` 使用情况 |
| `docs/superpowers/plans/*.md` | 每阶段当时的 plan + design |
| `docs/experiments/*.md` | Stage 5 实证产物 |
| `docs/design/*.md` | v2 设计基线 |
| `AGENTS.md` | 项目阶段定义（Stage 1-4） |
| `CLAUDE.md` | 当前项目约定 |

---

## 文档结构（产出物大纲）

```
docs/design/evolution.md
├── 0. 摘要：一张时间线图 + 6 段演化结论
├── 1. v1.0 最初版（ec7f658）
│    ├── 1.1 项目定位（方法级 MT 教学工具）
│    ├── 1.2 技术栈（WPF + HandyControl + LiveCharts + LiteDB）
│    ├── 1.3 分页功能（HandyControl Pagination 在 6 个页面）
│    ├── 1.4 系统 MT pipeline：⚠ 不存在
│    └── 1.5 设计意图（教学 + 演示）
├── 2. Stage 1（2026-05-07 至 2026-05-08）
│    ├── 2.1 演化目标：扩展到系统级 MT
│    ├── 2.2 重大变更
│    │    • 新增 MetBench_BLL.Core (net8.0 跨平台)
│    │    • 引入 Reqnroll BDD
│    │    • Python adapter 模式确立
│    ├── 2.3 系统 MT pipeline v0.1（5 个核心类）
│    ├── 2.4 设计说明
│    └── 2.5 关键 commit 引用
├── 3. Stage 2（2026-05-08）
│    ├── 3.1 演化目标：自动生成 followup
│    ├── 3.2 重大变更
│    │    • InputGenerator + PythonInputAdapter
│    │    • MrTransformation IR
│    ├── 3.3 系统 MT pipeline v0.2
│    └── 3.4 设计说明
├── 4. Stage 3 / 3+（2026-05-08 至 2026-05-09）
│    ├── 4.1 演化目标：OpenMOC 真实科学计算 SUT
│    ├── 4.2 重大变更
│    │    • OpenMOC venv + setup.sh
│    │    • ScaleNuSigmaF / ScaleFuelSigmaA 双向 MR
│    │    • IMrAssertion 接口 + LessThanAssertion
│    ├── 4.3 系统 MT pipeline v0.3
│    └── 4.4 设计说明
├── 5. Stage 4（2026-05-10）
│    ├── 5.1 演化目标：平台特性 + 第二 SUT
│    ├── 5.2 重大变更
│    │    • LiteDB SystemMtResultRecord 持久化
│    │    • HTML 单跑报告 renderer
│    │    • WPF SystemMtExecutionPage（launch UI）
│    │    • OpenMC 集成 + cross-program IR (MrFamily slug)
│    │    • Launcher facade (ISystemMtScenarioLauncher) + type-leakage rule
│    │    • Batch execution
│    │    • Heat-equation 第三 SUT
│    ├── 5.3 系统 MT pipeline v1.0（成熟）
│    ├── 5.4 设计说明
│    └── 5.5 ⚠ HandyControl 未移除（继续在 6 页面）
├── 6. Stage 5 — Phase 1, 2, 3（2026-05-12 至 2026-05-13）
│    ├── 6.1 演化目标：MR 实证有效性（mutation + NOETHER + 真实 bug）
│    ├── 6.2 重大变更（叙事化）
│    │    Phase 1 — 28 mutations × 4 MR 矩阵
│    │    Phase 2 — NOETHER MetaPattern (m_inv/m_mono/m_conv/m_cmp) +
│    │              25+ scenarios + Cohen's κ + LLM filter
│    │    Phase 3 — Tally / Temperature MR + Case 2/4/5/6 真实 bug live
│    │              + Plotly dashboard
│    ├── 6.3 系统 MT pipeline v2.0 vs v1.0 — 偏移诊断
│    │    • Python 矩阵 vs C# launcher 两套并行
│    │    • assertion 表达力差距（noise-aware / variance-ratio）
│    │    • 持久化分裂（LiteDB vs _data/*.json）
│    │    • BDD .feature 不再覆盖新 MR
│    └── 6.4 设计说明
├── 7. v2 设计（2026-05-13 至今）
│    ├── 7.1 演化目标：回归 C# 编排，统一两套系统
│    ├── 7.2 重大变更
│    │    • MR 4 级语义层次（MetaPattern / Schema / Binding / Instance）
│    │    • LiteDB 23 collection (3NF)
│    │    • Adapter 拆为 Input/Output Parser + ParameterMapping
│    │    • MR Transformation 移入 C# Pipeline
│    │    • FluentAssertions 扩展方法
│    │    • Discovery + Mutation 子系统首次显式
│    │    • BDD .feature 双向同步
│    ├── 7.3 系统 MT pipeline v2 设计图
│    ├── 7.4 关键设计决策表
│    └── 7.5 待执行任务（→ migration-plan.md）
├── 8. HandyControl 移除路线
│    ├── 8.1 现状：6 个 .xaml 文件仍依赖
│    ├── 8.2 替代方案（Wpf.Ui 原生组件）
│    └── 8.3 分阶段移除策略
├── 9. 系统 MT Pipeline 演化纵贯图
│    ├── v0.1 → v0.2 → v0.3 → v1.0 → v2.0 → v2.设计
│    └── 每代变化点 + 数据流图
└── 10. 经验教训（AI 编程影响 + 设计漂移）
```

---

## 实施步骤（executing-plan 阶段）

- [ ] Step 1 — 创建 `docs/design/evolution.md`
- [ ] Step 2 — 写第 0 节摘要（时间线图 + 6 段结论）
- [ ] Step 3 — 写第 1 节 v1.0 最初版（基于 `ec7f658` git show）
- [ ] Step 4 — 写第 2 节 Stage 1（基于 `git log` Stage 1 commit + `docs/superpowers/plans/2026-05-07-*`）
- [ ] Step 5 — 写第 3 节 Stage 2
- [ ] Step 6 — 写第 4 节 Stage 3 / 3+
- [ ] Step 7 — 写第 5 节 Stage 4
- [ ] Step 8 — 写第 6 节 Stage 5（三个 Phase）
- [ ] Step 9 — 写第 7 节 v2 设计
- [ ] Step 10 — 写第 8 节 HandyControl 移除路线
- [ ] Step 11 — 写第 9 节 系统 MT Pipeline 纵贯图（5 代对比）
- [ ] Step 12 — 写第 10 节 经验教训
- [ ] Step 13 — 在 `docs/design/README.md` 加入口链接
- [ ] Step 14 — commit + push

---

## Pipeline 演化重点（用户明确要求"尤其是"）

每个阶段必须包含：
1. **该阶段 Pipeline 的具体数据流图**（ASCII）
2. **该阶段 Pipeline 涉及的核心 C# 类清单**
3. **新增 / 删除 / 修改的 pipeline 节点**
4. **当时 Pipeline 的边界（什么进 Pipeline，什么不进）**

Pipeline 演化的关键节点：

| 版本 | 节点构成 | 数据格式 |
|------|--------|--------|
| v0（v1.0 初始） | 无系统级 pipeline — 只有方法级 in-proc MT | 内存对象 |
| v0.1（Stage 1） | SystemMtTask → CliProgramRunner → PythonOutputAdapter → IMrAssertion | 文件 JSON |
| v0.2（Stage 2） | 加 InputGenerator + PythonInputAdapter（生成 followup） | 文件 JSON |
| v0.3（Stage 3） | OpenMOC 实跑 + IMrAssertion 接口化（双向 MR） | 文件 JSON |
| v1.0（Stage 4） | + LiteDBSystemMtResultRepository + Launcher facade + Batch | LiteDB + 文件 JSON |
| v1.5（Stage 5） | **Python 旁路矩阵**完全脱离 C# pipeline | _data/*.json + matrix.csv |
| v2.0（v2 设计） | C# Pipeline 重新统一：Input Parser → MR Transformation (C#) → Output Parser → AssertionEvaluator → LiteDB Execution/Result/Anomaly | dict 内存 + LiteDB |

---

## 验收标准

- [ ] 文档放在 `docs/design/evolution.md`
- [ ] 长度约 1500-2500 行 markdown
- [ ] 每个 Stage 节都有：演化目标 + 重大变更 + 系统 MT pipeline 图 + 设计说明 + 关键 commit SHA 引用
- [ ] 第 9 节有 5 代 pipeline 纵贯对比图
- [ ] 第 10 节诚实评估 AI 编程对项目的结构性影响
- [ ] 中文为主，专业术语用英文（与 glossary.md 一致）
- [ ] `docs/design/README.md` 增加 `evolution.md` 入口
- [ ] commit 信息说明本文档目的

---

## 风险

| 风险 | 缓解 |
|------|------|
| Git 历史过密导致 commit 引用混乱 | 每节末尾固定 "关键 commit" 子节，列 3-5 个 SHA |
| Stage 划分与实际 git 不完全对齐 | 用 `git log --since/--until` 划界 |
| Pipeline 图过多失焦 | 每阶段一张主图 + 共用一个纵贯图，不画太多 |
| 与既有 `discussion-phase2.md`、`PHASE2.md` 重复 | 此文档是 **演化纵贯**，那两份是 **当时态横截**；明文交叉引用 |
| 用户对 HandyControl 移除态度可能变 | 第 8 节标注"建议路线"不"承诺路线" |

---

## P1 工作的暂停说明

P1 阶段（DB schema 扩展）已完成：
- ✅ P1.1：扩展 `MetamorphicRelation` + `Application` v2 字段
- ✅ P1.2：创建 5 个 value object record（ValueRange / ToleranceConfig / SutHyperparams / SamplingSpec / ParameterMapping）

P1 暂停内容：
- ⏸ P1.3-P1.10：18 个新 collection 实体 + DbConfig 扩展 + 编译验证

已实现的 P1.1-P1.2 改动**未 commit**，留在工作树中。等本演化文档完成后由用户决定：
- (a) 续 P1 → commit P1.1-P1.2 + 继续 P1.3-P1.10
- (b) 弃 P1 → `git checkout MetBench_Domain/` 还原 + 删除 V2/ 目录
- (c) 部分 → 单独 commit 演化文档，P1 改动保留为本地 WIP

本计划文件**不预设**用户偏好。
