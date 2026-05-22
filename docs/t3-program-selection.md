# T3 覆盖 — 程序选型矩阵

> **T3**（见 [`CLAUDE.md`](../CLAUDE.md) §2）目标：反应堆物理 5 个核心控制方程，
> 每个至少对应一个 SUT、可执行 MT。
> 本文给出「方程 × 4 类程序类型」的主流程序全景，并按**可获取性**推荐选型。
> 选型结论大体沿用 [AGENTS.md Stage 8](../AGENTS.md)，并对 fourier / NS 的数值格、
> 以及 ML 代理 / PINN 格作了调整（见 §3 注、§4）。

可获取性标注：〔开源〕直接可得 ·〔申请〕学术申请制 ·〔商业〕付费 license ·
〔管制〕出口管制 ·〔套件〕开源 benchmark 套件 ·〔研究〕无成熟通用程序 ·〔已接入〕项目已装。

---

## 1. 主流程序全景

| 控制方程 | 数值模拟（确定论） | 概率（蒙特卡洛） | ML 代理模型 | PINN |
|---|---|---|---|---|
| **Boltzmann** 中子输运 | OpenMOC〔开源〕· DRAGON〔开源〕· DENOVO/SCALE〔申请〕· PARTISN〔管制〕 | OpenMC〔开源〕· Serpent〔申请〕· MCNP〔管制〕· TRIPOLI〔申请〕 | 无 benchmark 套件覆盖；k_eff/通量代理〔研究〕 | 输运方程 PINN〔研究〕 |
| **Diffusion** 中子扩散 | PARCS〔申请〕· DYN3D〔申请〕· NESTLE/CITATION〔申请〕· nodal home-grown | —（MC 解输运而非扩散，本质不适用） | PDEBench / PDEArena〔套件〕· scikit-learn GP | DeepXDE〔开源〕· PDEBench〔套件〕 |
| **Bateman** 燃耗/嬗变 | ORIGEN/SCALE〔申请〕· FISPACT〔申请〕· CRAM 求解器 | OpenMC depletion〔开源〕· Serpent depletion〔申请〕 | 无 benchmark 套件覆盖（ODE 非 PDE）；浓度代理〔研究〕 | ODE 系统 PINN〔研究〕 |
| **Fourier** 热传导 | OpenFOAM〔开源〕· FEniCS〔开源〕· COMSOL/ANSYS〔商业〕· FRAPCON〔申请〕· FD/FEM home-grown | —（仅学术随机行走法，非主流） | PDEBench / PDEArena〔套件〕· scikit-learn GP | DeepXDE / Modulus〔开源〕· PDEBench〔套件〕 |
| **Navier-Stokes** 热工水力 | OpenFOAM〔开源〕· Fluent/STAR-CCM+〔商业〕· CTF/COBRA〔申请〕· RELAP5〔申请〕 | —（非主流） | PDEBench / PDEArena〔套件〕· FNO/neuraloperator | Modulus / DeepXDE〔开源〕· PDEBench〔套件〕 |

---

## 2. 推荐选型

数值模拟 / 概率列推荐**求解器程序**；ML 代理 / PINN 列优先推荐 **benchmark 套件**，
以「**程序（套件）+ 基线（模型）**」描述 —— 套件提供显式 PDE、参考解数据与基线
实现，基线模型即作 SUT。套件未覆盖的方程退回自建。

| 控制方程 | 数值模拟 | 概率（MC） | ML 代理模型 | PINN |
|---|---|---|---|---|
| **Boltzmann** 中子输运 | OpenMOC〔开源·已接入〕 | OpenMC〔开源·已接入〕 | scikit-learn GP 自建³ | DeepXDE 自建³ |
| **Diffusion** 中子扩散 | home-grown nodal 扩散 | —¹ | PDEBench〔套件〕+ FNO 基线 | PDEBench〔套件〕+ PINN 基线 |
| **Bateman** 燃耗 | home-grown Bateman ODE | OpenMC depletion〔开源〕 | scikit-learn GP 自建³ | DeepXDE ODE-PINN 自建³ |
| **Fourier** 热传导 | FEniCS〔开源〕 | —² | PDEBench〔套件〕+ FNO 基线 | PDEBench〔套件〕+ PINN 基线 |
| **Navier-Stokes** 热工水力 | OpenFOAM〔开源〕 | —² | PDEBench〔套件〕+ FNO 基线 | PDEBench〔套件〕+ PINN 基线 |

¹ 蒙特卡洛解的是**输运**方程，不存在「解扩散方程的 MC 程序」；扩散 cell 里
OpenMC 充当同源异构差分测试的高保真参考基准。
² 热传导 / NS 的蒙特卡洛仅有学术「随机行走法」，无主流程序 → 该类型不设。
³ boltzmann（中子输运，积分-微分方程）与 bateman（燃耗 ODE）**无 benchmark 套件
覆盖** —— PDEBench / PDEArena 等仅含 PDE，无中子输运、无燃耗 ODE，故退回自建。

> **「程序 + 基线」约定**：「程序」= 开源 benchmark 套件（**PDEBench**，提供显式
> PDE 定义、参考解数据集、基线模型实现）；「基线」= 取作 SUT 的具体基线模型 ——
> 代理列取 **FNO**（神经算子），PINN 列取 **PINN** 基线。套件的 PDE 为通用物理
> 形式，与反应堆方程同型不同参；MR 由方程族的数学性质导出，可迁移。

---

## 3. 逐格选型与理由

每个 cell 一个推荐；ML 代理 / PINN 用「程序 + 基线」；N/A 格说明原因。

| 方程 | 程序类型 | 推荐 | 理由 |
|---|---|---|---|
| Boltzmann | 数值模拟 | OpenMOC | 唯一已接入的开源确定论（MOC）输运码，成熟稳定。 |
| Boltzmann | 概率 | OpenMC | 唯一无获取门槛的开源 MC 输运码（MCNP 出口管制、Serpent 申请制）；已接入。 |
| Boltzmann | ML 代理 | scikit-learn GP（自建） | 中子输运无 benchmark 套件覆盖；k_eff 标量，GP 轻量，以 OpenMC 输出训练。 |
| Boltzmann | PINN | DeepXDE（自建） | 输运是积分-微分方程、无套件无 turnkey；DeepXDE 自建，属研究阶段、留 Stage 9。 |
| Diffusion | 数值模拟 | home-grown nodal | PARCS/DYN3D 全申请制、无开源生产 nodal 码；节块法结构简单，自研 100% 可得。 |
| Diffusion | 概率 | N/A | 蒙特卡洛解输运方程、不解扩散方程；此格以 OpenMC 输运解作差分参考。 |
| Diffusion | ML 代理 | PDEBench + FNO 基线 | 扩散-反应型 PDE PDEBench 已含；FNO 基线现成、经验证、可引用，优于手搓 GP。 |
| Diffusion | PINN | PDEBench + PINN 基线 | PDEBench 自带扩散类 PDE 的 PINN 基线 + 参考数据，直接复用（DeepXDE 为框架备选）。 |
| Bateman | 数值模拟 | home-grown Bateman ODE | ORIGEN 申请制；Bateman 是线性 ODE 组，自研 CRAM/ODE 求解器简单可靠。 |
| Bateman | 概率 | OpenMC depletion | OpenMC 内置 `openmc.deplete`（CRAM），开源、已接入。 |
| Bateman | ML 代理 | scikit-learn GP（自建） | 燃耗是 ODE 非 PDE，benchmark 套件不覆盖；浓度时间序列低维，GP 够用。 |
| Bateman | PINN | DeepXDE ODE-PINN（自建） | ODE 无套件覆盖；DeepXDE 原生支持 ODE-PINN。 |
| Fourier | 数值模拟 | FEniCS | 开源、生产级 FEM 库，Python 写轻量热传导 SUT，比 home-grown 更可信。 |
| Fourier | 概率 | N/A | 热传导的蒙特卡洛仅学术随机行走法，无主流程序。 |
| Fourier | ML 代理 | PDEBench + FNO 基线 | 热传导属扩散型 PDE，PDEBench 覆盖；FNO 基线现成、可引用。 |
| Fourier | PINN | PDEBench + PINN 基线 | 热传导是 PINN 头号经典基准，PDEBench 有现成 PINN 基线 + 参考数据。 |
| Navier-Stokes | 数值模拟 | OpenFOAM | 开源、世界级 CFD 标准，生产级。 |
| Navier-Stokes | 概率 | N/A | NS 无主流蒙特卡洛求解。 |
| Navier-Stokes | ML 代理 | PDEBench + FNO 基线 | PDEBench 含可压/不可压 NS 数据与 FNO 基线，直接复用。 |
| Navier-Stokes | PINN | PDEBench + PINN 基线 | PDEBench 含 NS 的 PINN 基线；重负载场景可换 NVIDIA Modulus。 |

**总原则**：① 数值/概率列选**完全开源的生产级求解器**（OpenMOC/OpenMC/FEniCS/
OpenFOAM），无开源生产码的（diffusion/bateman 确定论格）以 home-grown 兜底；
② ML 代理 / PINN 列优先用**开源 benchmark 套件**（PDEBench）的现成基线 —— 比手搓
模型更现成、经验证、可引用；③ 套件未覆盖的方程（boltzmann 输运、bateman ODE）
退回 DeepXDE / scikit-learn 自建。

---

## 4. 与 Stage 8 的关系

本矩阵是 Stage 8「17 cells 覆盖矩阵」的程序侧选型依据：

- 已接入：OpenMOC / OpenMC（boltzmann + bateman）。
- **选型变更**（vs AGENTS.md Stage 8 原计划）：
  - fourier / NS 的数值格由 home-grown（1D Fourier / 1D subchannel）改为接入开源
    生产码 **FEniCS / OpenFOAM**；现有 `SUT/heat_equation` 可保留作 fourier 差分对照。
  - diffusion / fourier / NS 的 ML 代理 / PINN 格由「自建」改为 **PDEBench 套件 + 基线**；
    Stage 8 Phase 8.4 的 D₃ Surr 不再限定 scikit-learn GP。
- Stage 8 Phase 8.3 home-grown cells 缩为 **nodal diffusion + Bateman ODE** 两个。

> 上述变更需同步到 [Stage 8 详细计划](superpowers/plans/2026-05-18-stage8-expanded-mr-library-plan.md)
> 的 Phase 8.3 / 8.4 与 AGENTS.md Stage 8 §Goal 2。
