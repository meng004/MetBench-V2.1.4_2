# Plan — 代表性 SUT 接入计划（工作量从小到大）

> **日期**: 2026-05-21
> **状态**: 执行中 —— P1 已交付（2026-05-22）
> **关联**: [`docs/t3-program-selection.md`](../../t3-program-selection.md)（选型依据）·
> [`CLAUDE.md`](../../../CLAUDE.md) §2 T3 ·
> [下一阶段开发计划](2026-05-21-next-stage-development-plan.md)（本计划细化其 T3 部分）
> **总工时**: Cloud 关键路径 ~6–8 周

---

## 1. 背景 & 目标

平台定位放宽为「通用 MT 平台基线」—— 凡求解显式数学物理方程的程序皆可作 SUT。
本计划按 [`t3-program-selection.md`](../../t3-program-selection.md) 选定的代表性
ODE / PDE 方程，把对应**开源求解器程序与数据集接入为 SUT**。

**已接入（不在本计划）**：OpenMOC / OpenMC（boltzmann）、`SUT/projectile`（抛体运动 ODE）。

**Phase 排序原则**：按工作量**从小到大** —— 先做接入成本最低、复用现有 SUT 模式的，
再做需引入新框架 / ML 栈的，最后做最重的 OpenFOAM。

---

## 2. Phase 序列（工作量升序）

| Phase | 内容 | 新增 SUT | 工作量 | 关键成本 |
|---|---|---|---|---|
| **P1** ✅ | ODE SUTs（stdlib RK4） | decay_chain / damped_oscillator / lotka_volterra（已接入 catalog） | ~1.5–2 天 | 每个是小 Python 脚本，SUT 模式现成（仿 `heat_equation`） |
| **P2** | FEniCS PDE SUTs | 热传导 / 扩散-反应 / Poisson / 波动 | ~4–6 天 | 首个 FEniCS 接入（装 + SUT runner + 文件适配）；后续每方程便宜 |
| **P3** | Clawpack PDE SUTs | 对流 / Burgers | ~3–4 天 | Clawpack 接入 + 2 个脚本 |
| **P4** | DeepXDE PINN SUTs | 上述方程的 PINN 形态 | ~1 周 | 一个 DeepXDE runner + 逐方程 PINN 脚本 + 准入验证 |
| **P5** | PDEBench ML 代理 SUTs | diffusion / heat / NS 的 FNO 基线 | ~1 周 | PDEBench 装 + 数据集 + 基线模型包装为 SUT + 准入验证 |
| **P6** | OpenFOAM NS SUT | Navier-Stokes | ~1.5–2 周 | OpenFOAM 装 + case 目录 / dict 文件适配（最重） |

**关键路径**：P1 → P2 → P3 → P4 → P5 → P6，工作量升序；P4 / P5 的 ML 栈可与
P2 / P3 并行（若有第二人手）。

---

## 3. 各 Phase 详情

### P1 — ODE SUTs ✅ 已交付（2026-05-22）

衰变链（`SUT/decay_chain`）、阻尼振子（`SUT/damped_oscillator`）、Lotka-Volterra
（`SUT/lotka_volterra`）三个 ODE SUT，各含 runner + 输入/输出适配 + 样例算例，并接入
launcher catalog（各一条 `MrBlueprint`）：

- `decay_chain` — ScaleInitial（Bateman 链线性；N_C_final 翻倍验证 ✓）
- `damped_oscillator` — ScaleInitialState（线性；max_abs_displacement 翻倍验证 ✓）
- `lotka_volterra` — ScaleGamma（LV 恒等式 ⟨prey⟩=γ/δ；mean_prey 单调增验证 ✓）

**实现说明**：原计划用 `scipy.integrate.solve_ivp`，实际改用 **stdlib RK4**（零依赖，
与 `heat_equation` 一致，避免 SUT 运行 Python 缺 scipy 的部署风险）。验证：3 SUT 直接
调用 + MR 成立；catalog 5→8，全套 `dotnet test` 559 passed / 4 skipped / 0 failed。

### P2 — FEniCS PDE SUTs（~4–6 天）

接入 FEniCS（FEM 库）：装 FEniCS + 写一个 FEniCS SUT runner + 输入/输出文件适配。
之后热传导、扩散-反应、Poisson、波动各是一个 FEniCS 弱形式脚本（每个 ~0.5 天）。
扩散-反应可设入中子扩散系数 → 忠实切入反应堆 diffusion 格。

### P3 — Clawpack PDE SUTs（~3–4 天）

接入 Clawpack（双曲守恒律求解器）：对流方程、Burgers 方程各一个 SUT。

### P4 — DeepXDE PINN SUTs（~1 周）

接入 DeepXDE：一个 PINN SUT runner，逐方程写 PINN 脚本（ODE-PINN 用于 Bateman /
振子；PDE-PINN 用于热传导 / 扩散 / 波动 / NS）。**准入验证见 §4**。

### P5 — PDEBench ML 代理 SUTs（~1 周）

接入 PDEBench：取其 diffusion / heat / NS 的数据集 + FNO 基线模型，包装为 SUT
（「程序（PDEBench）+ 基线（FNO）」）。**准入验证见 §4**。

### P6 — OpenFOAM NS SUT（~1.5–2 周，最重）

接入 OpenFOAM：装 + 为其 case 目录 / dictionary 文件格式写输入/输出适配（OpenFOAM
输入是一组 dict 文件，比单文件适配重）。NS 格的数值求解 SUT。

---

## 4. 准入验证（自建 / ML / PINN SUT 通用）

P4 / P5 接入的 PINN 与 ML 代理 SUT，在用作 MT 主体前须先过「准入验证」：

- **A 逐点精度** —— 留出测试集上对照可信参考解（同方程的数值求解器），相对误差 <
  阈值（阈值绑定参考解自身不确定度）。
- **B 训练 / 测试无泄漏**。
- **C 物理不变量** —— 守恒律 / 非负性等硬约束满足。
- **★ D 验证准则不得与 MR 循环** —— 准入用**逐点精度**，**不得**用「是否满足 MR」，
  否则 MT 循环论证。
- **E 对标已发表基线** —— 有同型方程已发表精度数据时，自建模型精度应可比。

---

## 5. 与其他计划的关系

- 本计划是[下一阶段开发计划](2026-05-21-next-stage-development-plan.md) **P1（Stage 8
  主线）中「SUT 侧」的细化与放宽** —— MR 识别（T4）、异常（T5）、变异（T6）不在此。
- 与 [AGENTS.md](../../../AGENTS.md) Stage 8 §Goal 2 的「5 方程 × 4 程序类型 +
  4 home-grown」原描述已分歧（home-grown 取消、SUT 范围放宽）—— **AGENTS.md 待同步**。

## 6. 不交付（scope 外）

- MR 识别引擎、MR 有效性验证、变异、异常 —— 见下一阶段开发计划其余 Phase。
- 反应堆专用生产码（PARCS / ORIGEN / RELAP5 等）—— 申请制 / 出口管制，不接入。
- 论文 writeup。
