# SUT 程序与数据集选型（T3 覆盖）

> **T3**（见 [`CLAUDE.md`](../CLAUDE.md) §2）目标：覆盖代表性的数学物理方程，
> 每个方程至少对应一个可执行 MT 的 SUT。
>
> **选型原则**：凡求解**显式数学物理方程**的程序皆可作 SUT；方程从数学上分
> ODE / PDE 两类，从中选**代表性强、流传广、使用多**的方程与程序；反应堆物理
> 5 方程为优先锚定。本表所列程序与数据集**均开源、可直接获取**。

---

## 1. ODE 类

| 方程 | 代表性 | 数值求解程序 | ML 代理 / PINN | 切入反应堆 |
|---|---|---|---|---|
| 线性 ODE 系统 / 衰变链（Bateman） | 燃耗、化学动力学、药代动力学的通用形态 | scipy `solve_ivp` / SUNDIALS CVODE | DeepXDE（ODE-PINN）；数据自生成 | ✓ bateman |
| 简谐 / 阻尼振子（2 阶线性） | 物理头号经典 ODE | scipy `solve_ivp` | DeepXDE | — |
| 抛体运动 | 最简经典；项目已有 `SUT/projectile` | 自带 / scipy | DeepXDE | — |
| Lotka-Volterra（非线性 ODE 系统） | 非线性动力学头号范例 | scipy `solve_ivp` | DeepXDE | — |

> ODE 求解便宜，无 PDEBench 式大型数据集；ML / PINN 训练数据按需自生成。

## 2. PDE 类

| 方程 | 代表性 | 数值求解程序 | ML 代理 / PINN 数据集 | 切入反应堆 |
|---|---|---|---|---|
| 热传导 / 扩散（抛物型） | PDE 头号经典 | FEniCS / OpenFOAM | PDEBench（diffusion） | ✓ fourier |
| 扩散-反应 | 反应-扩散系统通用形态 | FEniCS | PDEBench（diffusion-reaction） | ✓ diffusion（中子扩散） |
| Poisson / Laplace（椭圆型） | 椭圆 PDE 范式、稳态场 | FEniCS | DeepXDE | — |
| 波动方程（双曲型） | 双曲 PDE 范式 | FEniCS / Clawpack | DeepXDE | — |
| 对流 / 输运（线性双曲） | 输运范式 | Clawpack | PDEBench（advection） | △ boltzmann（输运的简化形态） |
| Burgers（非线性、激波） | 非线性 PDE、ML-PDE 圈头号 benchmark | Clawpack / FEniCS | PDEBench（Burgers） | — |
| Navier-Stokes | CFD 核心方程 | OpenFOAM | PDEBench（NS）/ PDEArena | ✓ NS |

---

## 3. 反应堆 5 方程的切入情况

| 反应堆方程 | 切入 | 对应代表方程 / 程序 |
|---|---|---|
| bateman 燃耗 | ✓ 干净 | 衰变链 ODE → scipy；MC 耦合燃耗 → OpenMC depletion（已接入） |
| fourier 热传导 | ✓ 干净 | 热传导 PDE → FEniCS |
| diffusion 中子扩散 | ✓ 干净 | 扩散-反应 PDE → FEniCS（可设入中子扩散系数） |
| NS 热工水力 | ✓ 干净 | Navier-Stokes → OpenFOAM |
| boltzmann 中子输运 | △ 松散 | 输运是积分-微分方程，无等同的「代表性通用方程」；advection 仅其简化形态。boltzmann 的数值 / 概率格仍由 OpenMOC / OpenMC（已接入）兜，不靠本套选型 |

4 个干净切入 + boltzmann 松散 —— 满足「能切入五个方程最好，不切入也行」。

## 4. 说明

- **不再用 home-grown**：广义看，每个代表性 ODE / PDE 都有成熟开源通用求解器
  （scipy / FEniCS / OpenFOAM / Clawpack）。先前 diffusion / bateman 退回 home-grown，
  是因为坚持用反应堆**专用**生产码（PARCS / ORIGEN，申请制）；改用通用求解器解同一
  方程后，home-grown 不再需要。FEniCS 解中子扩散方程是忠实求解，非近似。
- **概率（蒙特卡洛）**：对通用 ODE / PDE 基本 N/A（MC 是输运 / 随机问题专属），
  仅在 boltzmann / bateman 的反应堆语境下用 OpenMC。
- **「程序 + 基线」**：ML 代理 / PINN 列优先用开源 benchmark 套件 **PDEBench**（提供
  显式 PDE 定义、参考解数据集、基线模型 FNO / PINN）；套件未覆盖的方程（ODE、
  boltzmann 输运）退回 DeepXDE / scikit-learn 自建。
- **PDEBench 局限**：其 PDE 为通用规范形式，与反应堆方程「同型不同参」；MR 由方程族
  的数学性质导出、可迁移，但严格说套件支撑的 cell 测的是通用物理模型。
- **可获取性**：scipy / FEniCS / OpenFOAM / Clawpack / DeepXDE 全开源；PDEBench /
  PDEArena 开源套件。反应堆专用生产码（MCNP 出口管制、PARCS / ORIGEN / RELAP5
  申请制）一律不进选型 —— 这正是平台选「通用开源求解器」的原因。

---

## 5. 与 Stage 8 / AGENTS.md 的关系

本选型把 SUT 范围从「反应堆 5 方程 + home-grown」放宽为「代表性 ODE / PDE 方程 +
通用开源求解器」。**这与 AGENTS.md Stage 8 §Goal 2（5 方程 × 4 程序类型 + 4 home-grown）
的原描述已分歧**，需同步订正。SUT 接入的实施排期见
[representative-sut-onboarding-plan](superpowers/plans/2026-05-21-representative-sut-onboarding-plan.md)。
