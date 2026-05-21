# Plan — 下一阶段开发计划（按 T0–T4 功能优先级）

> **日期**: 2026-05-21
> **状态**: 待 user 审定
> **关联**: [`CLAUDE.md`](../../../CLAUDE.md) §2 功能分层 · [AGENTS.md Stage 8](../../../AGENTS.md) · [Stage 8 详细计划](2026-05-18-stage8-expanded-mr-library-plan.md) · [polish 批次](2026-05-19-polish-existing-work-plan.md)
> **总工时**: Cloud 关键路径 ~8–9 周；VM 并行 ~1 周

---

## 1. 实现状态评估（按功能优先级 T0–T4）

| 层 | 已实现 | 缺口 | 缺口性质 |
|---|---|---|---|
| **T0 核心 MT 流程** | 引擎 + 门面 + 适配器 + 持久化；561 测试 0 fail；v2.1.x 已发布 | **MR 库覆盖稀薄** —— 仅 boltzmann（OpenMOC/OpenMC）+ 少量 fourier | 机制成熟，**内容**是缺口 |
| **T1 直接支撑** | SUT 适配器（OpenMOC / OpenMC / heat_equation / projectile）；Discovery 框架；multi-LLM 共识（60/60、100%）；cross-program 差分测试（OpenMOC×OpenMC 4/4） | meta-prompt MR 识别引擎未起；4 个 home-grown cell SUT 未接；差分测试未按 Num/MC/Surr/PINN 泛化；mutmut validator 待移除 | 主要缺口，紧贴核心 |
| **T2 呈现与交互** | 可视化 + 4 端报表 + CRUD + WPF 客户端 | 5 个 UAT UI 缺口；DP-3 severity 阈值 `appsettings` 绑定未接 | 小、VM-track |
| **T3 消费核心产出** | 异常调查工作流；severity/category 分级（PR #83 评审中）；R-Case 复现 | 缺陷封存的「程序版本 × MR × 测试输入」三元组未一级化 | 中小 |
| **T4 评估 MR 集** | 变异 campaign 矩阵 + 杀死率/存活率/覆盖率/误报率 | 语义/语法句法变异分型、等价变异体识别、最小 MR 完备子集搜寻未起 | 中，**依赖 Stage 8 产出** |

**一句话**：T0 的**机制**已成熟，下一阶段的价值前沿是 T0 的**覆盖**与 T1 —— 即 MR 库的填充。

---

## 2. 下一阶段定位 & 优先级原则

- **定位**：下一阶段 = Stage 8 / v2.2 主线（5 方程 × 4 程序类型 MR 库）+ 配套调整。
- **排序原则**：按 T0→T4 tier 优先级，但实际 Phase 顺序取「最高 tier 的最大缺口」叠加**依赖约束**：
  - T0/T1 的最大缺口（MR 库）→ 主线、最先做。
  - T4 变异增强 tier 虽低，但**操作对象是 Stage 8 产出的 84 候选 MR** —— 必须排在主线之后。
  - T2 UI 缺口为 VM-track，可并行，不占 Cloud 关键路径。
  - 模型对齐清理（删 mutmut / Trend）小且应在主线动工前完成 —— 作 Phase 0。

---

## 3. Phase 序列

| Phase | 内容 | 层 | Track | 工时 | 依赖 |
|---|---|---|---|---|---|
| **P0** 模型对齐清理 | 删 `AdversarialMutmutValidator`（+ DI/测试）；删 `MetBench_BLL.Trend` 子系统（+ IDAL/DAL/测试/DI/WPF 页） | T1 / — | Cloud + VM | ~1 天 | — |
| **P1** Stage 8 MR 库主线 | 5D schema → meta-prompt 引擎 → 现有 SUT 5D 升级 → 4 home-grown cells → 差分测试按程序类型泛化 → 覆盖 dashboard | T0 / T1 | Cloud | ~5–6 周 | P0 |
| **P2** 变异模块增强 | 语义变异与语法/句法变异的分型生成；等价变异体识别；最小 MR 完备子集搜寻 | T4 | Cloud | ~1.5–2 周 | P1（需 MR 集存在） |
| **P3** 缺陷封存收口 | 「程序版本 × MR × 测试输入」三元组在缺陷记录上一级化；回放/定位/分类与之绑定 | T3 | Cloud | ~3–5 天 | 可在 P1 中后期并入 |
| **P4** UI 缺口 + DP-3 | 5 个 UAT UI 缺口（Dashboard 入口、HTML 内嵌查看等）+ severity 阈值 `appsettings` 绑定 | T2 | VM | ~3–4 天 | 与 P1 并行 |

**关键路径**：P0 → P1 → P2（P3 插入 P1 中后期；P4 VM 并行）。

---

## 4. 各 Phase 详情

### P0 — 模型对齐清理（~1 天）

把代码对齐到已决定的功能模型（见 `2026-05-19-polish-existing-work-plan.md` 之后的决策）：

- 删 `AdversarialMutmutValidator`（+ `AdversarialCampaignSampler` 若仅其使用）+ DI 注册 + 相关测试。验证手段收敛为：专家经验（系统外，无代码）/ 数据验证（`EmpiricalValidator`）/ 多 LLM 共识。
- 删 `MetBench_BLL.Trend` 子系统（`TrendAnalysisService` / `WeeklyReport`）+ IDAL/DAL 关联 + 测试 + DI + WPF Trends 页与导航项。
- 同步 `CLAUDE.md` 的「v2 BLL.Core namespaces」表移除 Trend 行。

> 先做的理由：在主线动工前让代码与功能模型一致，避免把已废弃概念带进 Stage 8 的新工作。

### P1 — Stage 8 MR 库主线（~5–6 周）

执行既有 [Stage 8 详细计划](2026-05-18-stage8-expanded-mr-library-plan.md)，其内部 Phase 8.0–8.5 不在此重复；本计划只追加两点配套：

- Phase 8.0 **5D tag schema** 是地基，须最先落地（`Equation / ProgramType / MetaPattern / SourceLevel / FailureCorrelation` 经 BDD tag + LiteDB sync）。
- Phase 8.4 的「D₃/D₄ 程序类型横切」即**同源异构差分测试按 Num/MC/Surr/PINN 的泛化** —— 把现有 cross-program MR 升为一级命名能力。

验收（沿用 Stage 8 plan）：≥ 12 cells 不空白 + ≥ 15 MR 入库 + 全套 `dotnet test` 0 fail。

### P2 — 变异模块增强（~1.5–2 周）

在 Stage 8 产出 84 候选 MR 后，补齐 T4：

- **变异分型**：语义变异 vs 语法/句法变异的分型生成（现有变异体来自目录、未分型）。
- **等价变异体识别**：识别并排除恒杀不死的等价变异体。
- **最小 MR 完备子集**：在保持检错力前提下搜最小 MR 子集。

### P3 — 缺陷封存收口（~3–5 天）

缺陷记录显式绑定（程序版本 × MR × 测试输入）三元组（现散落在 `Execution.SutVersionSnapshot` / `CatalogVersionSha` 等字段），使回放、缺陷定位、缺陷分类有稳定锚点。

### P4 — UI 缺口 + DP-3（VM，~3–4 天）

5 个 UAT UI 缺口（见 `2026-05-21-uat-ui-gaps-backlog.md`）+ DP-3 的 `appsettings` severity 阈值绑定。VM-track，与 P1 并行。

---

## 5. 排期（粗）

```
周次   W13   W14 ──────────────── W19   W20 ── W21   (W22)
P0     ■
P1          ■■■■■■■■■■■■■■■■■■■■■■■■
P2                                    ■■■■■■■
P3                              ■■■           (插入 P1 中后期)
P4          ■■■  (VM 并行)
```

Cloud 关键路径 P0+P1+P2 ≈ 8–9 周；P4 VM 并行。

---

## 6. 理由

1. **为什么 MR 库（P1）是下一阶段主线**：T0 的核心机制已成熟（561 测试 0 fail、v2.1.x 发布），继续打磨机制边际收益低；真正的价值缺口是**覆盖** —— 当前 5 个核心方程只覆盖 boltzmann 一个。MR 库是论文的核心交付物，且按 tier 优先级它落在 T0（覆盖）/ T1（识别与验证），是最高优先级的最大缺口。
2. **为什么 P0 清理排在最前**：删 mutmut/Trend 是已决定的模型调整，成本极小（~1 天）；在主线动工前对齐，避免 Stage 8 的新代码挂接到将被删除的子系统上。
3. **为什么变异增强（P2）排在主线之后**：T4 tier 本身优先级低，更关键的是**依赖约束** —— 变异模块评估的对象是「MR 集」，而 84 条 MR 由 Stage 8 产出；先做 P2 则无 MR 可评估。
4. **为什么 UI 缺口（P4）并行而非串行**：T2 缺口是 VM-track 且小，不依赖、也不被 Cloud 主线依赖；放并行既释放价值（已实现的后端能力露出 UI）又不占关键路径。
5. **P3 缺陷封存为何可插入而非独立阶段**：改动小且与 Stage 8 的 Execution/版本快照天然相关，随 P1 中后期接入比单列阶段更省上下文切换。

---

## 7. 不交付（scope 外）

- **BNCT 硼中子放疗** —— Stage 9+ 候，Stage 8 plan 已明确暂缓。
- **故障注入 V3** —— 独立模块挂起。
- **第 5 个 SUT、F11 m_adj 路径** —— 外部依赖未解，被动监控。
- **论文 writeup** —— user 指令：先做实验、发现 bug 再考虑，不在本计划绑定。
- **项目管理类功能** —— 平台定位科研场景，不自研；未来如需以对接成熟工具实现。
