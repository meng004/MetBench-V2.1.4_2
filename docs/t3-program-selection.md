# T3 覆盖 — 程序选型矩阵

> **T3**（见 [`CLAUDE.md`](../CLAUDE.md) §2）目标：反应堆物理 5 个核心控制方程，
> 每个至少对应一个 SUT、可执行 MT。
> 本文给出「方程 × 4 类程序类型」的主流程序全景，并按**可获取性**推荐选型。
> 选型结论大体沿用 [AGENTS.md Stage 8](../AGENTS.md)，并对 fourier / NS 的数值格
> 作了调整（见 §4）。

可获取性标注：〔开源〕直接可得 ·〔申请〕学术申请制 ·〔商业〕付费 license ·
〔管制〕出口管制 ·〔研究〕无成熟通用程序 ·〔已接入〕项目已装。

---

## 1. 主流程序全景

| 控制方程 | 数值模拟（确定论） | 概率（蒙特卡洛） | ML 代理模型 | PINN |
|---|---|---|---|---|
| **Boltzmann** 中子输运 | OpenMOC〔开源〕· DRAGON〔开源〕· DENOVO/SCALE〔申请〕· PARTISN〔管制〕 | OpenMC〔开源〕· Serpent〔申请〕· MCNP〔管制〕· TRIPOLI〔申请〕 | k_eff/通量神经网络代理〔研究〕 | 输运方程 PINN〔研究〕 |
| **Diffusion** 中子扩散 | PARCS〔申请〕· DYN3D〔申请〕· NESTLE/CITATION〔申请〕· nodal home-grown | —（MC 解输运而非扩散，本质不适用） | 节块通量代理〔研究〕 | 扩散方程 PINN（经典范例）· DeepXDE〔开源〕 |
| **Bateman** 燃耗/嬗变 | ORIGEN/SCALE〔申请〕· FISPACT〔申请〕· CRAM 求解器 | OpenMC depletion〔开源〕· Serpent depletion〔申请〕 | 核素浓度代理〔研究〕 | ODE 系统 PINN〔研究〕 |
| **Fourier** 热传导 | OpenFOAM〔开源〕· FEniCS〔开源〕· COMSOL/ANSYS〔商业〕· FRAPCON〔申请〕· FD/FEM home-grown | —（仅学术随机行走法，非主流） | scikit-learn GP / NN 代理〔开源、可自建〕 | 热传导 PINN（经典基准）· DeepXDE / Modulus〔开源〕 |
| **Navier-Stokes** 热工水力 | OpenFOAM〔开源〕· Fluent/STAR-CCM+〔商业〕· CTF/COBRA〔申请〕· RELAP5〔申请〕 | —（非主流） | CFD 代理〔研究、增长中〕 | NS PINN（研究热点）· Modulus / DeepXDE〔开源〕 |

---

## 2. 推荐选型

每格给出按可获取性优选的程序。

| 控制方程 | 数值模拟 | 概率（MC） | ML 代理模型 | PINN |
|---|---|---|---|---|
| **Boltzmann** 中子输运 | OpenMOC〔开源·已接入〕 | OpenMC〔开源·已接入〕 | scikit-learn GP 自建 | DeepXDE 输运 PINN 自建 |
| **Diffusion** 中子扩散 | home-grown nodal 扩散 | —¹ | scikit-learn GP 自建 | DeepXDE 扩散 PINN 自建 |
| **Bateman** 燃耗 | home-grown Bateman ODE | OpenMC depletion〔开源〕 | scikit-learn GP 自建 | DeepXDE ODE-PINN 自建 |
| **Fourier** 热传导 | FEniCS〔开源〕 | —² | scikit-learn GP 自建 | DeepXDE 热传导 PINN 自建 |
| **Navier-Stokes** 热工水力 | OpenFOAM〔开源〕 | —² | scikit-learn GP 自建 | DeepXDE NS-PINN 自建 |

¹ 蒙特卡洛解的是**输运**方程，不存在「解扩散方程的 MC 程序」；扩散 cell 里
OpenMC 充当同源异构差分测试的高保真参考基准。
² 热传导 / NS 的蒙特卡洛仅有学术「随机行走法」，无主流程序 → 该类型不设。

---

## 3. 逐格选型与理由

每个 cell 一个推荐程序；N/A 格说明原因。

| 方程 | 程序类型 | 推荐程序 | 理由 |
|---|---|---|---|
| Boltzmann | 数值模拟 | OpenMOC | 唯一已接入的开源确定论（MOC）输运码，成熟稳定。 |
| Boltzmann | 概率 | OpenMC | 唯一无获取门槛的开源 MC 输运码（MCNP 出口管制、Serpent 申请制）；已接入。 |
| Boltzmann | ML 代理 | scikit-learn GP | 无现成反应堆代理程序；k_eff 为标量，GP 轻量够用，以 OpenMC 输出训练，不引 PyTorch。 |
| Boltzmann | PINN | DeepXDE | 输运是积分-微分方程、无 turnkey 程序；DeepXDE 是最主流开源 PINN 框架，但此格属研究阶段、留 Stage 9。 |
| Diffusion | 数值模拟 | home-grown nodal | PARCS/DYN3D 全申请制、无开源生产 nodal 码；节块法结构简单，自研 100% 可得、轻量可控。 |
| Diffusion | 概率 | N/A | 蒙特卡洛解输运方程、不解扩散方程；此格以 OpenMC 输运解作差分参考。 |
| Diffusion | ML 代理 | scikit-learn GP | 无现成代理；以 home-grown nodal 输出训练，GP 轻量。 |
| Diffusion | PINN | DeepXDE | 扩散方程是 PINN 头号经典基准，DeepXDE 自带 worked example，改造成本最低。 |
| Bateman | 数值模拟 | home-grown Bateman ODE | ORIGEN 申请制；Bateman 是线性 ODE 组，自研 CRAM/ODE 求解器简单可靠、100% 可得。 |
| Bateman | 概率 | OpenMC depletion | OpenMC 内置 `openmc.deplete`（CRAM），开源、已接入，唯一无门槛的 MC 耦合燃耗。 |
| Bateman | ML 代理 | scikit-learn GP | 无现成代理；核素浓度时间序列低维，GP 够用。 |
| Bateman | PINN | DeepXDE | Bateman 是 ODE 组，DeepXDE 原生支持 ODE-PINN。 |
| Fourier | 数值模拟 | FEniCS | 开源、生产级 FEM 库，Python 写轻量热传导 SUT，比 home-grown 更有 SUT 可信度。 |
| Fourier | 概率 | N/A | 热传导的蒙特卡洛仅学术随机行走法，无主流程序。 |
| Fourier | ML 代理 | scikit-learn GP | AGENTS.md Phase 8.4.1 明确 fourier×Surr 用 scikit-learn GP；温度低维，GP 够用。 |
| Fourier | PINN | DeepXDE | 热传导方程是 PINN 最经典的 benchmark，DeepXDE example 最成熟。 |
| Navier-Stokes | 数值模拟 | OpenFOAM | 开源、世界级 CFD 标准，生产级。 |
| Navier-Stokes | 概率 | N/A | NS 无主流蒙特卡洛求解。 |
| Navier-Stokes | ML 代理 | scikit-learn GP | 统一用 GP（压降/最大流速等标量量）；若需场级代理可换 FNO（neuraloperator，代价：引 PyTorch）。 |
| Navier-Stokes | PINN | DeepXDE | NS 是 PINN 研究热点，DeepXDE 有 NS worked example；重负载场景可换 NVIDIA Modulus。 |

**总原则**：现成程序优先选**完全开源的生产级**程序（OpenMOC / OpenMC / FEniCS /
OpenFOAM）；仅在确无开源生产码时（diffusion、bateman 的确定论格）以 **home-grown**
兜底；ML 代理与 PINN 全行业无现成反应堆物理程序，统一用开源框架（scikit-learn /
DeepXDE）自建。

---

## 4. 与 Stage 8 的关系

本矩阵是 Stage 8「17 cells 覆盖矩阵」的程序侧选型依据：

- 已接入：OpenMOC / OpenMC（boltzmann + bateman）。
- **选型变更**（vs AGENTS.md Stage 8 原计划）：fourier / NS 的数值格由原定的
  home-grown（1D Fourier / 1D subchannel）改为接入开源生产码 **FEniCS / OpenFOAM**，
  以提升 SUT 可信度。Stage 8 Phase 8.3 的 home-grown cells 相应缩为 **nodal diffusion
  + Bateman ODE** 两个；现有 `SUT/heat_equation` 可保留作 fourier 的差分测试对照。
- Stage 8 Phase 8.4 横切：D₃ Surr（scikit-learn GP）；D₄ PINN 留 Stage 9。

> 该变更需同步到 [Stage 8 详细计划](superpowers/plans/2026-05-18-stage8-expanded-mr-library-plan.md)
> 的 Phase 8.3.2 / 8.3.4 与 AGENTS.md Stage 8 §Goal 2。
