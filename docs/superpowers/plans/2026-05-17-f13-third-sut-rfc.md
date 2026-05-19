# RFC — F13 第 3 SUT 选型

> **Status**: 待决策（默认推荐 OpenMC）
> **Author**: cloud-side
> **Date**: 2026-05-17
> **Related**: [v2.1 followup pipeline](2026-05-15-v2.1-followup-pipeline.md) F13 · [W11 plan](2026-05-16-w11-plan.md) W11.3

---

## 1. 背景

v2.1 已稳定支持 **OpenMOC**（deterministic neutron transport, MOC 算法）作主 SUT，加 **heat_equation** 和 **projectile** 两个简单 SUT 做跨域演示。论文里"我们演示了 metamorphic testing 适用于多种 numerical solver" 的论据强度，取决于 **第 3 个真实科学计算 SUT** 的选型。

F13 即解决：**接哪一个**、**怎么接**、**接进来能强化什么**。

## 2. 选型矩阵

按域 / 接入难度 / 论文价值 / 顺带解锁 4 维评估：

| 候选 | 域 | 已有 adapter | 接入工时 | 论文价值 | 顺带解锁 |
|------|----|--------------|---------|----------|---------|
| **OpenMC** | Neutron transport (Monte Carlo) | 部分（仓库 `SUT/openmc/` 存在但未启用） | **low (~1 day)** | ⭐⭐⭐ 与 OpenMOC 同域不同算法 → **强化 m_cmp** 跨实现一致性 | F11 不解锁 |
| Serpent | Neutron transport (MC, commercial) | 无 | high (~5 day) | ⭐⭐ 商业 SUT 演示 vendor-agnostic，但 license 麻烦 | F11 不解锁 |
| MCNP | Neutron transport (MC, NNSA) | 无 | high (~7 day) | ⭐⭐ 权威工具，但 license 严格，CI 不可重现 | F11 不解锁 |
| **OpenFOAM** | CFD | 无 | medium (~3 day) | ⭐⭐⭐⭐ **跨域**演示 MetaPattern 普适（流体 vs 中子） | F11 不解锁 |
| **FEniCS / DOLFIN** | FEM PDE solver | 无 | medium (~3 day) | ⭐⭐⭐⭐ **跨域** + 高数学严谨度 + Python 友好 | F11 不解锁 |
| **SU2** | CFD + 优化 | 无 | high (~4 day) | ⭐⭐⭐ **跨域** + adjoint flux 原生支持 → **顺带解锁 F11 m_adj** | **F11 ✅** |

## 3. 评分细则

### 3.1 OpenMC（推荐）

**优点**：
- 仓库已有 `SUT/openmc/` 目录占位 + 关联 BDD feature 文件 `CrossProgramNeutronTransportMrs.feature` 已写好两组 MR scenario
- **与 OpenMOC 同域不同算法** —— MOC（deterministic）vs MC（stochastic），论文中可写"deterministic 和 stochastic 算法在同一 MR 上的一致性是强 reproducibility 信号"
- Apache 2.0 license，CI 友好
- Python API 成熟，adapter 写法与 OpenMOC 复用度 80%+

**缺点**：
- 不跨域，论文"普适性"论据较弱
- MC 收敛慢，单次跑 30-90 s，CI 性能预算压力大
- 不解锁 F11 m_adj（OpenMC 标准 build 也没有 adjoint flux export）

**接入步骤**（估 1 day）：
1. `.claude/web-setup.sh` 加 OpenMC venv 安装段
2. 启用 `SUT/openmc/` 下的 adapter（部分代码已在仓）
3. 写 `OpenMcRunnerSmokeTests.cs`
4. 解除 `CrossProgramNeutronTransportMrs.feature` 的 OpenMC skip 条件
5. 加 `LauncherOptions.OpenMcPython` + DI 注册
6. UC-A1 加 OpenMC scenario 进 UAT

### 3.2 OpenFOAM / FEniCS（高论文价值替选）

**优点**：
- **跨域** —— CFD / 通用 PDE，对论文最有冲击力
- FEniCS Python 接入容易；OpenFOAM 通过 PyFoam / 直接 case file
- 加之后 MetaPattern 库可以覆盖：流体守恒律 / 边界条件不变性 / 网格独立性等典型 PDE MR

**缺点**：
- OpenFOAM 编译 30+ min，CI 装一次 ~1 GB
- FEniCS 在某些 Python 版本上 wheel 不稳定（需要 conda）
- 工时 ~3 day，要写新的 adapter / MR 模板 / SCG / domain-specific assertion

### 3.3 SU2（解锁 F11 的最强候选）

**优点**：
- **顺带解锁 F11 m_adj** —— SU2 原生 adjoint flux export，m_adj MR 族可上线
- 跨域（CFD）
- Open source

**缺点**：
- 编译复杂（MPI + 多语言）
- 输入文件 schema 大，adapter 工时高
- ~4 day 全程接入

## 4. 推荐路径

### 短期（W11-W12）：**OpenMC**

理由：
- 最低风险接入（已有部分代码）
- 强化 m_cmp 跨算法一致性（论文卖点）
- 不分散精力 —— 写论文的同时即可接入

### 中期（W13-W14，如果论文需要）：**FEniCS 或 OpenFOAM**

视论文 reviewer 反馈决定，若被问"是否适用 CFD"则补 FEniCS / OpenFOAM。

### F11 解锁不与 F13 绑定

F11 m_adj 单独走 [`2026-05-17-f11-unlock-rfc.md`](2026-05-17-f11-unlock-rfc.md)。

## 5. 决策点

| 待你 confirm | 选项 | 默认 |
|--------------|------|------|
| F13 SUT | OpenMC / OpenFOAM / FEniCS / SU2 / 跳过 | **OpenMC** |
| 启动时间 | W12 / W13 | W12 |
| 工时预算 | 1 day / 3 day / 7 day | 1 day (OpenMC) |
| 接入后 UAT | 加 UC-C11 OpenMC BDD smoke / 不加 | 加 UC-C11 |

## 6. 接入 OpenMC 详细 checklist（决策后启动）

- [ ] `.claude/web-setup.sh` 加 OpenMC apt + Python venv 安装
- [ ] `LauncherOptions.OpenMcPython` 字段 + 默认 `$METBENCH_OPENMC_PYTHON`
- [ ] `MetBench_BLL.Core/SystemMT/Launcher/SystemMtScenarioLauncher` 接入 openmc binding
- [ ] `SUT/openmc/` 启用 adapter（部分代码已存在）
- [ ] `OpenMcRunnerSmokeTests.cs` 写 5 facts
- [ ] `CrossProgramNeutronTransportMrs.feature` 中 OpenMC 行 unskip
- [ ] UAT UC-C11 + acceptance-rubric 加行
- [ ] `SUT/openmc/scg.json`（OpenMC SCG，给 SCG-Heuristic 用）
- [ ] DiscoveryMethodSeed 不动（方法通用，不绑 SUT）
- [ ] CI Linux 跑通 OpenMC scenario

预估工期 1 day cloud-only。
