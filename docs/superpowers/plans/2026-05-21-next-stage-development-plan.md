# Plan — 下一阶段开发计划（按 T0–T6 功能分层）

> **日期**: 2026-05-21（依 CLAUDE.md §2 七层模型重修）
> **状态**: 待 user 审定
> **关联**: [`CLAUDE.md`](../../../CLAUDE.md) §2 功能分层 · [AGENTS.md Stage 8](../../../AGENTS.md) · [Stage 8 详细计划](2026-05-18-stage8-expanded-mr-library-plan.md) · [polish 批次](2026-05-19-polish-existing-work-plan.md)
> **总工时**: Cloud 关键路径 ~8–9 周；VM 并行 ~1 周

---

## 1. 实现状态评估（按 T0–T6 功能分层）

| 层 | 已实现 | 缺口 |
|---|---|---|
| **T0 核心 MT 流程** | 引擎 + 门面 + 持久化；561 测试 0 fail；v2.1.x 已发布 —— **「流程走通」验收已达标** | 无（覆盖已剥离至 T3，不属 T0） |
| **T1 直接支撑与操作入口** | SUT 运行环境适配、输入/输出文件适配、CRUD、WPF 客户端均✅；cross-program 差分测试✅（OpenMOC×OpenMC 4/4） | 差分测试未按 Num/MC/Surr/PINN 泛化；5 个 UAT UI 缺口 |
| **T2 可视化与报表** | 图表 + 4 端（PDF/Word/Excel/HTML）报表✅ | 基本无 |
| **T3 覆盖** | boltzmann（OpenMOC/OpenMC）✅ | **diffusion / bateman / fourier / NS 四方程未覆盖** —— 最大缺口 |
| **T4 MR 识别** | multi-LLM 共识✅（60/60、100% accuracy） | 基于元模式的 meta-prompt 识别引擎未起 |
| **T5 异常** | 异常调查工作流、severity/category 分级（PR #83 评审中）、R-Case 复现✅ | 缺陷封存的「程序版本 × MR × 测试输入」三元组未一级化 |
| **T6 变异** | 变异 campaign 矩阵 + 杀死率/存活率/覆盖率/误报率✅ | 语义/语法句法变异分型、等价变异体识别、最小 MR 完备子集未起 |

**一句话**：T0「流程走通」与 T2 已达标；下一阶段的价值前沿在 **T3 覆盖** 与 **T4 MR 识别** —— 即 MR 库的填充。

---

## 2. 下一阶段定位 & 优先级原则

- **定位**：下一阶段 = Stage 8 / v2.2 主线（5 方程 × 4 程序类型 MR 库）+ 配套调整。
- **排序原则**：
  - T0「流程走通」已达标、T2 已成熟 → 不在打磨核心机制。
  - 最大缺口 = **T3 覆盖**（5 方程只覆盖 1 个）与 **T4 MR 识别**（meta-prompt 引擎未起）。二者**互为依赖** —— 覆盖一个 cell 须先识别该 cell 的 MR —— 合并即 Stage 8 主线，最先做。
  - T6 变异增强 tier 虽靠后，且**操作对象是 Stage 8 产出的 84 候选 MR** —— 必须排在主线之后。
  - T1 的 UI 缺口为 VM-track，可并行，不占 Cloud 关键路径；T1 的差分测试泛化并入 Stage 8 的程序类型工作。
  - 模型对齐清理（删 mutmut / Trend）小且应在主线动工前完成 —— 作 Phase 0。

---

## 3. Phase 序列

| Phase | 内容 | 层 | Track | 工时 | 依赖 |
|---|---|---|---|---|---|
| **P0** 模型对齐清理 | 删 `AdversarialMutmutValidator`（+ DI/测试）；删 `MetBench_BLL.Trend` 子系统（+ IDAL/DAL/测试/DI/WPF 页） | 模型 | Cloud + VM | ~1 天 | — |
| **P1** Stage 8 MR 库主线 | 5D schema → meta-prompt 引擎 → 现有 SUT 5D 升级 → 4 home-grown cells → 差分测试按程序类型泛化 → 覆盖 dashboard | T3 / T4 / T1 | Cloud | ~5–6 周 | P0 |
| **P2** 变异模块增强 | 语义/语法句法变异分型生成；等价变异体识别；最小 MR 完备子集搜寻 | T6 | Cloud | ~1.5–2 周 | P1（需 MR 集存在） |
| **P3** 缺陷封存收口 | 「程序版本 × MR × 测试输入」三元组在缺陷记录上一级化；回放/定位/分类与之绑定 | T5 | Cloud | ~3–5 天 | 可在 P1 中后期并入 |
| **P4** UI 缺口 + DP-3 | 5 个 UAT UI 缺口（Dashboard 入口、HTML 内嵌查看等）+ severity 阈值 `appsettings` 绑定 | T1 / T2 | VM | ~3–4 天 | 与 P1 并行 |

**关键路径**：P0 → P1 → P2（P3 插入 P1 中后期；P4 VM 并行）。

---

## 4. 各 Phase 详情

### P0 — 模型对齐清理（~1 天）

把代码对齐到已决定的功能模型：

- 删 `AdversarialMutmutValidator`（+ `AdversarialCampaignSampler` 若仅其使用）+ DI 注册 + 相关测试。MR 识别（T4）收敛为：基于元模式的 meta-prompt 方法、multi-LLM 共识方法。
- 删 `MetBench_BLL.Trend` 子系统（`TrendAnalysisService` / `WeeklyReport`）+ IDAL/DAL 关联 + 测试 + DI + WPF Trends 页与导航项。
- 同步 `CLAUDE.md` 的「v2 BLL.Core namespaces」表移除 Trend 行。

> 先做的理由：在主线动工前让代码与功能模型一致，避免把已废弃概念带进 Stage 8 的新工作。

### P1 — Stage 8 MR 库主线（~5–6 周）

执行既有 [Stage 8 详细计划](2026-05-18-stage8-expanded-mr-library-plan.md)，其内部 Phase 8.0–8.5 不在此重复；本计划只追加两点配套：

- Phase 8.0 **5D tag schema** 是地基，须最先落地（`Equation / ProgramType / MetaPattern / SourceLevel / FailureCorrelation` 经 BDD tag + LiteDB sync）。
- Phase 8.1 = **T4 MR 识别**的 meta-prompt 引擎；Phase 8.3 填 cells = **T3 覆盖**；Phase 8.4「D₃/D₄ 程序类型横切」即**同源异构差分测试（T1）按 Num/MC/Surr/PINN 的泛化**。

验收（沿用 Stage 8 plan）：≥ 12 cells 不空白 + ≥ 15 MR 入库 + 全套 `dotnet test` 0 fail。

### P2 — 变异模块增强（T6，~1.5–2 周）

在 Stage 8 产出 84 候选 MR 后补齐 T6：

- **变异分型**：语义变异 vs 语法/句法变异的分型生成（现有变异体来自目录、未分型）。
- **等价变异体识别**：识别并排除恒杀不死的等价变异体。
- **最小 MR 完备子集**：在保持检错力前提下搜最小 MR 子集。

### P3 — 缺陷封存收口（T5，~3–5 天）

缺陷记录显式绑定（程序版本 × MR × 测试输入）三元组（现散落在 `Execution.SutVersionSnapshot` / `CatalogVersionSha` 等字段），使回放、缺陷定位、缺陷分类有稳定锚点。

### P4 — UI 缺口 + DP-3（T1/T2，VM，~3–4 天）

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

1. **为什么不再打磨核心机制**：T0 的验收标准是「流程端到端走通」，已达标（561 测试 0 fail、v2.1.x 发布）；覆盖已明确剥离为独立的 T3，不再是 T0 的负担。T2 可视化报表亦成熟。继续打磨 T0/T2 边际收益低。
2. **为什么 MR 库（P1）是主线**：最大缺口落在 **T3 覆盖**（5 个核心方程只覆盖 boltzmann 一个）与 **T4 MR 识别**（meta-prompt 引擎未起）。这两层互为依赖 —— 要覆盖一个 cell，先得识别该 cell 的 MR —— 合起来就是 Stage 8，也是论文的核心交付物。
3. **为什么 P0 清理排最前**：删 mutmut/Trend 是已决定的模型调整，成本极小（~1 天）；在主线动工前对齐，避免 Stage 8 新代码挂接到将被删除的子系统上。
4. **为什么变异增强（P2）排在主线之后**：T6 变异模块评估的对象是「MR 集」，而 84 条 MR 由 Stage 8（P1）产出；先做 P2 则无 MR 可评估 —— 这是依赖约束，不只是 tier 次序。
5. **为什么 UI 缺口（P4）并行而非串行**：T1 的 UI 缺口是 VM-track 且小，不依赖、也不被 Cloud 主线依赖；放并行既释放价值（已实现的后端能力露出 UI）又不占关键路径。
6. **P3 缺陷封存为何可插入而非独立阶段**：改动小且与 Stage 8 的 Execution/版本快照天然相关，随 P1 中后期接入比单列阶段更省上下文切换。

---

## 7. 不交付（scope 外）

- **BNCT 硼中子放疗** —— Stage 9+ 候，Stage 8 plan 已明确暂缓。
- **故障注入 V3** —— 独立模块挂起。
- **第 5 个 SUT、F11 m_adj 路径** —— 外部依赖未解，被动监控。
- **论文 writeup** —— user 指令：先做实验、发现 bug 再考虑，不在本计划绑定。
- **项目管理类功能** —— 平台定位科研场景，不自研；未来如需以对接成熟工具实现。
