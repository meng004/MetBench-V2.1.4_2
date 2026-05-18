# Brainstorming — 反应堆物理 5 大方程 SUT 覆盖

> **Stage 8 / Goal 2 — Brainstorming 阶段**
> **日期**: 2026-05-18
> **状态**: 需求探索 + 候选 SUT 调研；待 writing-plan 阶段固化
> **关联**: [AGENTS.md Stage 8 Goal 2](../../../AGENTS.md#goal-2-反应堆物理-5-大方程-sut-覆盖)
> **依赖**: [Goal 1 meta-prompt 引擎](2026-05-18-meta-prompt-mr-discovery-brainstorming.md) — 5 方程 SUT 用其自动生成 MR candidate

---

## 1. 目标重申

让 MetBench 覆盖**反应堆物理工程实践中完整的 5 大方程**，从论文当前的"演示 metamorphic testing 适用于多种 numerical solver" 升级为"覆盖反应堆物理工程**完整**方程栈"，论据强度大幅提升。

---

## 2. 现状（Explore 2026-05-18 调研）

### 2.1 当前 4 SUT 方程覆盖

| SUT | 方程 | 覆盖度 |
|---|---|---|
| OpenMOC | Neutron transport (Boltzmann, MOC deterministic) | ✅ |
| OpenMC | Neutron transport (Boltzmann, MC stochastic) | ✅ |
| heat_equation | Heat conduction (1D Fourier, demo) | ⚠ 仅 1D Fourier，无热源项，不是反应堆燃料几何 |
| projectile | 经典弹道 | ❌ demo only，不属反应堆物理 |

### 2.2 学术界反应堆物理 5 大主要方程

| # | 方程 | 物理 | 标准形式 |
|---|------|---|---|
| **1** | Neutron transport (Boltzmann) | 中子角通量 / k_eff / 临界性 | `∇·Ω̂φ + Σ_t φ = χ ν Σ_f /(4π) ∫φ + S` |
| **2** | Burnup (Bateman) | 核燃料核素演化 | `dN_i/dt = λ_{i-1} N_{i-1} - (λ_i + σ_i φ) N_i + ...` |
| **3** | Heat conduction (Fourier) | 燃料棒径向温度 | `ρc ∂T/∂t = ∇·(k∇T) + q'''` |
| **4** | Thermal-hydraulics (Navier-Stokes + 传热) | 冷却剂流动 + 反馈 | `∂ρ/∂t + ∇·(ρv)=0; ρ(Dv/Dt) = -∇p + ∇·τ; Q = h(T_surf - T_bulk)` |
| **5** | Point-kinetics | 瞬态反应堆功率 + 6 群缓发中子 | `dn/dt = (ρ-β)/Λ n + Σ λ_i C_i + S; dC_i/dt = β_i/Λ n - λ_i C_i` |

### 2.3 覆盖矩阵

| 方程 | 当前 SUT | 完整度 | gap |
|---|---|---|---|
| 1. Transport | OpenMOC + OpenMC | ✅ full | — |
| 2. Burnup | — | ❌ | 需接 |
| 3. Heat | heat_equation (1D demo) | ⚠ 50% | 升级为 2D 径向燃料 |
| 4. Th-Hyd | — | ❌ | 需接 |
| 5. Point-kinetics | — | ❌ | 需接 |

---

## 3. 设计空间扫描

### 3.1 接 SUT 的总策略问题

#### Q1: 优先级排序？

| 优先级 | 方程 | 论文价值 | 接入难度 | LLM-MR 信号 |
|---|---|---|---|---|
| ⭐⭐⭐⭐⭐ | Point-kinetics | 中（瞬态 unique 物理） | 低（纯 Python ODE） | 强（守恒 + 单调） |
| ⭐⭐⭐⭐ | Burnup | 高（核燃料管理工业标杆） | 中（需核数据库 或 简化版） | 强（核素守恒） |
| ⭐⭐⭐ | Thermal-hydraulics | 高（多物理耦合） | 高（PDE + 经验关联） | 中（complex） |
| ⭐ | Heat 2D upgrade | 低（已有简版） | 低 | 弱 |

**倾向**：Point-kinetics → Burnup → Th-Hyd（按"工时/价值"比，从最高开始）

#### Q2: 真程序 vs home-grown？

| 选项 | 描述 | 优 | 劣 |
|---|---|---|---|
| **A. 真程序优先** | 接 ORIGEN / RKTM / MOOSE 等 | 工业可信度 / reviewer 友好 | 安装重 / cloud-sandbox 可能跑不动 |
| **B. home-grown** | 写简化 Python 求解器 | 100% cloud 友好 / 轻量 | reviewer 可能问"为什么不接真程序" |
| **C. 混合** | Point-kinetics + Heat 用 home-grown；Burnup + Th-Hyd 接真程序 | 平衡 | 工程混杂 |

**倾向 C**（混合）—— 简单方程用 home-grown（demo MetaPattern + 论文中作 controlled-experiment），复杂方程用真程序（论文 reviewer trust）。

#### Q3: 接 SUT 是否要走 Goal 1 的 meta-prompt 引擎？

| 选项 | 描述 |
|---|---|
| **A. 必走** | 接 SUT 前先用 meta-prompt 引擎自动产 candidate MR，再人工 review 选好的 |
| B. 可选 | meta-prompt 是 nice-to-have，手写 MR 也接受 |
| C. baseline 对比 | 同一 SUT 两种方法都跑（meta-prompt vs 手工），论文做 controlled experiment |

**倾向 C**（baseline 对比）—— 论文价值最大化。同 SUT，meta-prompt 自动产 N 个 candidate + 手工产 M 个，比较召回率 / 精度 / 时间成本。

#### Q4: 每方程接几个 MR？

| 选项 | MR 数 | 论文 footprint |
|---|---|---|
| **A. 最低**（1 MR / 方程） | 5 total | 弱 |
| **B. 标准**（2-3 MR / 方程） | 10-15 total | 中 — **推荐** |
| C. 重度（5+ MR / 方程） | 25+ total | 强但工时翻倍 |

**倾向 B**（每方程 2-3 MR）。每方程覆盖 ≥ 2 MetaPattern（如 m_inv + m_mono），最少给出对称 + 单调两类 MR 跑通。

### 3.2 候选 SUT 详评

#### #2 Burnup

| 候选 | 优 | 劣 | 推荐度 |
|---|---|---|---|
| **OpenMC depletion** | 已装 OpenMC binary + Python 模块；depletion 是 OpenMC built-in | 需要核数据库（~5 GB ENDF/B-VIII，外部下载）；运行慢 | ⭐⭐⭐ |
| Serpent 2 | 业界标杆 | 闭源 / 需下载核数据库 / cmake build | ⭐ |
| ORIGEN（claimed by Explore agent as pip-installable）| 轻量、REST API | **需验证**：`pip install origen-core` 是否真存在；可能 hallucination | ⭐⭐ (待验) |
| **Home-grown Python Bateman solver** | 100% cloud 友好；纯 ODE 易实现；典型链可手写（U-235 → Pu-239 → ...）| reviewer 可能问 "为什么不用真程序" | ⭐⭐⭐⭐ |

**倾向**：先 home-grown 简化 Bateman（U-235 / Pu-239 / Xe-135 三核素链）作快速接入；后续选 OpenMC depletion 作 controlled-comparison（需 ENDF/B-VIII 数据，CI 不跑、本地可跑）。

#### #3 Heat conduction 升级

| 候选 | 描述 |
|---|---|
| **现有 heat_equation 扩 2D 径向** | 改 SUT/heat_equation/heat_equation.py 加 2D 求解模式 + 燃料几何（半径 / 包壳） |
| BISON-class | INL 多物理框架，依赖 MOOSE，重 |
| Home-grown 1D 径向（U / clad / gap） | 简化燃料棒 1D 径向 → 中心温度 vs 表面温度 |

**倾向**：home-grown 1D 径向 SUT —— 名字叫 `fuel_pin_conduction`，跟 heat_equation 并列。简单 + 反应堆几何特征明显。

#### #4 Thermal-hydraulics

| 候选 | 优 | 劣 |
|---|---|---|
| MOOSE + porous_flow + heat_conduction | 工业级 / 多物理 | cmake + PETSc 依赖重 |
| OpenFOAM + 热模块 | 文献多 | 500+ MB / GUI |
| **Home-grown Python 1D channel** | 100% cloud 友好；典型 sub-channel ODE 可写 | 简化版，reviewer 可能质疑 |
| CTF/COBRA-TF | NRC 标准 | 闭源 / 编译复杂 |

**倾向**：home-grown 1D sub-channel（mass / momentum / energy ODE，单通道，给定 inlet T/v + 热源 → 出口 T/v）。20-50 行 Python。

#### #5 Point-kinetics

| 候选 | 优 | 劣 |
|---|---|---|
| **Home-grown Python RK4** | 极简（< 50 行）；IAEA benchmark 数据公开 | — |
| RKTM（claimed by Explore agent） | 现成 pip 包 | 需验证 PyPI 存在性 |
| DYNSOL | IAEA reference | FORTRAN wrapper 复杂 |

**倾向**：home-grown 1-group 6-delayed-group point-kinetics RK4。Python stdlib + numpy 即可。

### 3.3 home-grown SUT 公共模板

为 #3 (fuel_pin_conduction) / #4 (sub_channel) / #5 (point_kinetics) 提供同一开发模板：

```
SUT/<sut>/
├── <sut>.py                    # runner (CLI: --input <json> --output <json>)
├── <sut>_input_adapter.py      # MR transformation adapter (per MR)
├── <sut>_output_adapter.py     # output metrics extraction
├── sample/<scenario>.json      # 1+ sample input
├── scg.json                    # 因果图（喂 ScgHeuristicDiscoverer）
└── equation.md                 # 方程 + 守恒律 + 已知对称性（Goal 1 meta-prompt 输入）
```

复用 `SUT/heat_equation/` 的开发约定（已验过的简单 Python SUT 模板）。

---

## 4. 替选方案比较

| 方案 | 描述 | 工时 | 论文价值 |
|---|---|---|---|
| **A. 全部 home-grown** | #2-#5 全用 home-grown Python | 20-25h | 中（reviewer 可能质疑工业可信度） |
| **B. 全部真程序** | 接 OpenMC depletion / MOOSE / 真 point-kinetics 包 | 60-80h | 高（reviewer 友好） |
| **C. 混合**（推荐） | #5 RKTM + #3 home-grown + #4 home-grown + #2 home-grown → OpenMC depletion (followup) | 25-35h | 中-高 |

**倾向 C** — 先 home-grown 跑通全部 5 方程的 baseline；论文 v1 投出去后，reviewer 反馈再选 1-2 个升级到真程序作 controlled comparison。

---

## 5. 风险

| 风险 | 缓解 |
|---|---|
| home-grown SUT 太简化、缺工程可信度 | 论文明写 "minimal canonical solvers per equation, used as MR scaffolding"；reviewer 反馈后升级到真程序 |
| OpenMC depletion 核数据库 5 GB / CI 跑不起来 | depletion test 标 SKIP；本地手工跑产 baseline |
| Goal 2 时间盒拖长 | 优先级排序严格（点动力学 → 燃耗 → 热工水力 → 热传导 upgrade），每方程独立 PR + 独立 phase |
| meta-prompt 引擎产的 MR 全是 generic | Goal 1 已有 4 validator 链做 promote 准入，过滤 vacuous |
| 5 方程 → 论文方向被指责 "too neutronics-focused" | 论文 framework 写法强调 "MetaPattern + meta-prompt + adapter pattern 框架普适，本论文以反应堆物理 5 方程作 case study" |

---

## 6. 推荐方向（待 plan 阶段固化）

| 维度 | 选择 |
|---|---|
| **方程优先级** | 5 Point-kinetics → 2 Burnup (home-grown Bateman) → 4 Th-Hyd → 3 Heat upgrade |
| **每方程 SUT 路线** | home-grown 先行；OpenMC depletion 作 followup |
| **MR 数 / 方程** | 2-3 个 MR（覆盖 ≥ 2 MetaPattern） |
| **meta-prompt 用法** | 每 SUT 用 Goal 1 引擎自动产 N candidate → 人工 review 选 2-3 入正式 MR；同时手写 1 个对照 |
| **SUT 开发模板** | 沿用 `SUT/heat_equation/` 约定 + 新 `equation.md` 字段 |
| **入 BDD** | 每方程 1 `.feature` 文件 + 入 cross-program feature 跟 transport 比对（如可比的话） |
| **入 UAT** | acceptance-rubric Part C 加 UC-C12 ~ UC-C15 各方程 smoke test |
| **入 baseline** | 每方程 land 后刷 baseline-2026-XX-YY |

---

## 7. 5 阶段交付计划（待 plan 阶段细化）

| Phase | 方程 | SUT | 目标 |
|---|---|---|---|
| 8.2.0 | (prep) | — | Goal 1 meta-prompt 引擎跑通（依赖） |
| 8.2.1 | #5 Point-kinetics | home-grown RK4 (W14) | 1 个 MR + smoke test + BDD scenario |
| 8.2.2 | #2 Burnup | home-grown Bateman (W14-15) | 2 个 MR（核素守恒 + 链平衡） |
| 8.2.3 | #4 Thermal-hydraulics | home-grown 1D channel (W15) | 2 个 MR（mass/energy 守恒） |
| 8.2.4 | #3 Heat | 升级 heat_equation 2D fuel pin (W16) | 1 个 MR + 旧 1D 保留作对比 |
| 8.2.5 | follow-up | OpenMC depletion (optional, W16-17) | 燃耗与 home-grown 对比 |
| 8.2.6 | paper writeup | — | "5 equations coverage" 实证段 |

---

## 8. 待 plan 阶段决定的细节

- 每方程具体哪 2-3 个 MR？需要 brainstorm 每个 SUT 的 NOETHER 适用 pattern
- home-grown solver 的物理近似程度（哪些项简化、哪些保留）
- BDD scenario 命名约定（`PointKineticsRampReactivity.feature` 等）
- baseline trx 多大（5 个新 SUT × 各自 smoke + BDD ~10-20s）
- 是否影响 release-v2.1.0 时间盒（不影响，Stage 8 在 v2.1 发版**之后**）

---

## 9. Ready-to-plan checklist

- [x] 现状调研完整
- [x] 5 方程 + 标准命名 + 物理形式列出
- [x] 候选 SUT 评估（含 Explore agent 的待验声明）
- [x] 设计空间问题穷举 (Q1-Q4)
- [x] 替选方案比较 (A/B/C)
- [x] 风险识别 + 缓解
- [x] 推荐方向明确（混合策略 / home-grown 先行）
- [x] 5 阶段交付草案
- [ ] **下一步**：[`2026-05-18-reactor-physics-five-equations-plan.md`](2026-05-18-reactor-physics-five-equations-plan.md) — 落每 phase 具体 deliverable + 工时 + 入仓位置
