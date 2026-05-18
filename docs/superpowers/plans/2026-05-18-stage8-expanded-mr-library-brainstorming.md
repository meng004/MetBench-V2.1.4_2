# Stage 8 expanded MR library — brainstorming

> **Date**: 2026-05-18（rev：5 MP + 17 cells + 84 MR 母集 + cloud-friendly 程序评估）
> **Status**: Brainstorming（→ writing-plan）
> **Supersedes**: [`2026-05-18-reactor-physics-five-equations-brainstorming.md`](2026-05-18-reactor-physics-five-equations-brainstorming.md)
> **术语规范**: [`docs/GLOSSARY.md`](../../GLOSSARY.md)（5 MP 定义 + BDD 术语 + NOETHER↔5 MP 映射 + 内部命名）
> **Upstream refs**（外部上传，未入仓）:
>   - P-series CLAUDE.md（论文协作硬约束）
>   - Cmrlibrary.md（5D MR schema + 12 网格 + 57 条种子 MR + 6 类来源）
>   - PWR_MR_Analysis.md（PWR 5 层方程 + 27 条新增 MR + PARCS/Serpent 适用性）
>   - MT____MR___1998_2025_.md（MT 文献 MR 数据源清单 + NUIT/HTGR 私有程序）

---

## §1 需求重述

User 指令链（合并多轮）:

1. 反应堆物理 5 个核心方程：boltzmann / diffusion / bateman / fourier / NS
2. 4 程序类型：Num / MC / Surr / PINN
3. 5 MetaPattern：MP_inv / MP_mono / MP_conv / MP_traj / MP_part（NOETHER 8 ↔ 5 MP 映射见 GLOSSARY §5）
4. 5D MR schema 用 BDD `.feature` + Gherkin tags 落地（不用 YAML mirror）
5. 现有 4 SUT 端到端跑通 workflow 验证（Phase 8.2.5）
6. 5 方程 cells 接入 + 完整研究工作流（Phase 8.3）
7. **暂缓**：BNCT（plan 内保留），故障注入 V3（独立模块挂起）
8. **目标**：cells 尽量填满，不空白；论文待真发现 bug 再考虑

---

## §2 5 方程归类（D₁，英文全称）

| 方程 | Cmrlibrary 编码 | 数学形式（PWR_MR_Analysis §1）|
|---|---|---|
| **boltzmann** 中子输运 | A | Ω·∇ψ + Σ_t ψ = ∫Σ_s ψ + χ/k Σ νΣ_f φ |
| **diffusion** 中子扩散 | B | -∇·D∇φ + Σ_r φ = Σ_s φ + χ/k Σ νΣ_f φ（输运的 P₁ 近似） |
| **bateman** 燃耗 | C | dN/dt = A(φ) N（线性 ODE，矩阵指数解 e^{At} N₀） |
| **fourier** 热传导 | D | ρc ∂T/∂t = ∇·(k∇T) + q''' |
| **NS** 简化 | E | 1D 系统级质量/动量/能量守恒 |

---

## §3 方程 × 程序类型 矩阵（cloud-friendly 程序候选）

3 个 D₂ MC cell 本质不适用（diffusion/fourier/NS 不走 MC）→ **17 实际可填 cells**。

| 方程 ↓ \ 程序类型 → | **Num** 数值 | **MC** 蒙特卡洛 | **Surr** ML 代理 | **PINN** |
|---|---|---|---|---|
| **boltzmann** | **OpenMOC** ✅ (现有) | **OpenMC** ✅ (现有) | home-grown GP (scikit-learn) | home-grown 简化 PINN |
| **diffusion** | **home-grown nodal**（PARCS 留 Stage 9） | ⬛ 不适用 | home-grown GP | home-grown 简化 PINN |
| **bateman** | **home-grown ODE**（ORIGEN/NUIT 不可获取） | **OpenMC depletion** ✅ (现有) | home-grown GP | (留 Stage 9) |
| **fourier** | **home-grown 1D**（升级现有 heat_equation；FRAPCON 不可获取） | ⬛ 不适用 | home-grown GP | home-grown 简化 PINN |
| **NS** | **home-grown subchannel**（RELAP5/CTF 不可获取） | ⬛ 不适用 | home-grown GP | home-grown CHF-PINN |

### §3.1 程序可获取性评估（从 PWR_MR_Analysis + MT-MR 文件）

| 程序 | 出处 | 方程 | 程序类型 | 获取 | Cloud 安装 | 推荐 |
|---|---|---|---|---|---|---|
| **OpenMC** ✅ | PWR + 现有 | boltzmann + bateman | MC | 开源 MIT | 已装 | ⭐ 主力 |
| **OpenMOC** ✅ | PWR + 现有 | boltzmann | Num (MOC) | 开源 MIT | 已装 | ⭐ 主力 |
| **PARCS** | PWR §3.1 | diffusion + 动力学 | Num (NEM/ANM/FDM) | USNRC 注册申请 (~2 周) | gfortran + LAPACK + 自编译 + 需 PMAXS 截面 | 🟡 Stage 9 |
| Serpent 2 | PWR §3.1 | boltzmann + bateman | MC | VTT 学术 license 申请 | 商业 ACE 截面库 | ❌ |
| SCALE/ORIGEN | PWR §3.1 | bateman | Num | ORNL 商业 + RSICC | — | ❌ |
| NUIT (Li Meng) | MT-MR §4.1 | bateman | Num | 未公开 source | — | ❌ MR 描述借鉴 |
| HTGR 多尺度耦合 (Zhao/Li 2026) | MT-MR §4.2 | 多尺度耦合 | Coupled | 未公开 source | — | ❌ MR 描述借鉴 |
| LLMORPH | MT-MR §1 | — (NLP MR tool) | — | GitHub 开源 | — | ❌ 不是反应堆 SUT |

**结论**：cloud-friendly 现成 = **OpenMOC + OpenMC**（+ depletion）。其余 cells 用 home-grown 填，借鉴 PARCS / NUIT / HTGR / PWR MR 描述。

---

## §4 5 MP × 5 方程 似然 MR 推导矩阵

✅ = 强 likely（方程算子直接推出）
⚠️ = 弱 likely（需适用域 / precondition）
— = 不适用

| MP \ 方程 | boltzmann | diffusion | bateman | fourier | NS |
|---|---|---|---|---|---|
| **MP_inv** 守恒/不变 | ✅ 旋转/平移/置换 + 守恒律 + 自伴 | ✅ 旋转/平移 + 自伴随（各向同性）+ ADF=1 退化 + CMFD on/off | ⚠️ 核素重排（Y_ji 对称限） + 质量守恒 | ✅ 镜像/旋转 + 热源守恒 | ✅ 几何对称 + 质量/能量守恒 |
| **MP_mono** 单调 | ⚠️ 截面区间限定 + 富集度 ↑→k ↑ | ✅ 硼浓度 ↑→k ↓ + 控制棒价值正 + Doppler/MTC + Dancoff | ✅ 燃耗 ↑→k_inf ↓（寿期内）+ 毒物含量 | ✅ 源 ↑→T ↑ | ✅ 流量 ↓→T_peak ↑ + 破口尺寸 ↑→喷放峰 ↑ |
| **MP_conv** 收敛/极限 | ✅ 源齐次 + 各向同性→扩散退化 + 角度细化 | ✅ 节块细化 + NEM 阶数 + 微分硼价值恒定 + 子群收敛 | ⚠️ 局部线性（小步长） + t=0→新堆退化 | ✅ 网格 h→h/2 二阶 + k→∞ 均匀化 | ✅ 空泡→0→单相退化 + 网格收敛 |
| **MP_traj** 轨迹 | ⚠️ 重启等价 + 多 batch 平均（MC） | ✅ 重启等价 + 控制棒微分价值曲线（先增后减） | ✅ 分段 vs 一段 + Gd S 曲线 + 燃耗 chain 时序 | ✅ 瞬态重启 | ✅ 瞬态重启 + ITC 硼依赖 + SDM 极值 |
| **MP_part** 偏序 | ⚠️ OpenMOC vs OpenMC 跨实现（强吸收偏序） | ✅ 输运-扩散一致（弱吸收 ≈，强吸收偏序） + NEM-FDM 极限 + Gd 棒数 vs 浓度 | ⚠️ ORIGEN vs PyNE 跨实现（链式截断处偏序） | ⚠️ 数值 vs 解析（1D 球） | ⚠️ 系统 vs 子通道 + 装载模式→功率峰 |

强 likely ✅ ≈ 30 条 / 弱 likely ⚠️ ≈ 18 条 → **总母集 ≈ 48 条 likely MR** 候选。

---

## §5 已识别 MR 母集（57 Cmrlibrary 种子 + 27 PWR 新增 = 84 条）

按 5 MP × 5 方程 cell 分类。⭐ = 高工程价值 / 非平凡（PWR_MR_Analysis §2.7 强调）。

### MP_inv (守恒 / 不变)

| Cell | MR 候选 | 来源 |
|---|---|---|
| (boltzmann, *) | S-N-01~07 几何对称 / C-N-01~05 守恒 / H-N-01 源齐次 / E-N-01~03 重述等价 | Cmrlibrary §C.14 |
| (diffusion, Num) | Dif-Phy-03 自伴随性 / Dif-Phy-07 ADF=1 退化 / Dif-Phy-09 装载对称 / Dif-Alg-05 CMFD 不改变收敛解 ⭐ | PWR §2.3 |
| (bateman, *) | C-N-03 重核+裂变碎片守恒 | Cmrlibrary |
| (fourier, *) | S-T-03 圆管对称 / C-T-03 能量平衡 | Cmrlibrary |
| (NS, *) | S-T-01,02,04,05 对称 / C-T-01~05 守恒 | Cmrlibrary |

### MP_mono (单调)

| Cell | MR 候选 | 来源 |
|---|---|---|
| (boltzmann, *) | M-N-02 富集度→k↑（区间）/ M-N-04 Doppler / I-N-02 吸收单调 | Cmrlibrary |
| (diffusion, Num) | Dif-Phy-02 控制棒区偏方向 ⭐ / Dif-Phy-04 硼浓度 / Dif-Phy-05 临界硼唯一 / Dif-Phy-08 ADF 异质性 / Dif-Phy-10 边缘低功率 / Dif-Phy-11 控制棒价值 / Dif-Phy-13 棒遮蔽 ⭐ / Dif-Alg-04 FDM 粗网偏低 / Res-Alg-02 稀释截面 / Res-Alg-03 Dancoff / Res-Alg-04 温度→展宽 | PWR §2.3-2.5 |
| (bateman, *) | M-N-05 燃耗→k↓（寿期内）/ M-N-06 可燃毒物 | Cmrlibrary |
| (fourier, *) | (热传导单调显然，源 ↑→T ↑) | 推导 |
| (NS, *) | M-T-01 流量→包壳温 / M-T-02 破口尺寸 / M-T-03 过冷度 / M-T-04 加热功率→CHF / M-T-05 压力→饱和裕量 / Cpl-App-01 Doppler / Cpl-App-02 MTC / Cpl-App-03 功率系数 / Cpl-App-07 Gd 含量 / Cpl-App-09 富集度→寿期 | Cmrlibrary + PWR |

### MP_conv (收敛 / 极限)

| Cell | MR 候选 | 来源 |
|---|---|---|
| (boltzmann, *) | H-N-03,04,05 时间/网格/能群收敛 / L-N-04 各向同性→扩散退化 / L-N-05 多群→连续能量 / L-N-06 SN 角度数→∞ | Cmrlibrary |
| (diffusion, Num) | Dif-Phy-06 微分硼价值恒定 / Dif-Alg-01 节块细化 / Dif-Alg-02 NEM 阶数 / Res-Alg-01 子群收敛 | PWR |
| (bateman, *) | L-N-01 燃耗=0→新 / L-N-02 控制棒全提→无棒 / L-N-03 时间步→0 Cauchy / E-N-03 燃耗段 vs 重启 | Cmrlibrary |
| (fourier, *) | H-T-01 几何相似 / L-T-03,04 网格/时间步收敛 | Cmrlibrary |
| (NS, *) | L-T-01 空泡→0→单相 / L-T-02 流速→0 自然循环 / L-T-05 子通道→CFD 极限 | Cmrlibrary |

### MP_traj (轨迹)

| Cell | MR 候选 | 来源 |
|---|---|---|
| (boltzmann, MC) | (重启 vs 多 batch 平均) | 推导 |
| (diffusion, Num) | Dif-Phy-12 控制棒微分价值曲线 ⭐（先增后减，余弦²）| PWR |
| (bateman, *) | E-N-03 燃耗分段 vs 一段 / Gd S 曲线 | Cmrlibrary + PWR Cpl-App-06 ⭐ |
| (fourier, *) | E-T-02 瞬态分段 vs 一段 | Cmrlibrary |
| (NS, *) | E-T-02 瞬态重启 / Cpl-App-04 ITC 硼依赖 ⭐ / Cpl-App-05 SDM 极值 | PWR |

### MP_part (偏序)

| Cell | MR 候选 | 来源 |
|---|---|---|
| (boltzmann, * vs *) | (OpenMOC vs OpenMC 跨实现，强吸收区偏序) | 推导（NEA benchmarks 启示） |
| (diffusion, Num vs Num) | Dif-Phy-01 输运-扩散一致（弱吸收 ≈，强吸收偏序） / Dif-Alg-03 NEM-FDM 极限 | PWR |
| (bateman, Num vs Num) | (ORIGEN vs PyNE 跨实现，链式截断处偏序) | 推导 |
| (fourier, Num vs analytic) | L-T-03 数值 vs 解析 | Cmrlibrary |
| (NS, Num vs Num) | (系统 vs 子通道) / Cpl-App-08 Gd 棒数 vs 浓度 / Cpl-App-10 装载模式→功率峰 | PWR |

---

## §6 完整研究工作流（Phase 8.3 per cell）

```
方程算子 algebraic property 分析
  → 适用 5 MP 选取（参考 §4 矩阵）
  → 扫描 SUT 输入参数（自动 schema 解析）
  → 元模式 × 参数 → meta-prompt（Goal 1 引擎）
  → LLM 识别 MR 实例（多家 consensus）
  → MetBench 执行 MT (src + flw)
  → 分支判定:
      ├─ 高支持度 (V1+V2 通过) → 入 MR 库（BDD .feature + LiteDB sync）
      ├─ 低支持度 + MP 数学应成立 → 分析:
      │    ├─ MR 错（适用域 / tol 设错）→ 改 MR + 重测
      │    └─ 程序错（疑似 bug）→ 反例归档（不刻意造 paper narrative）
      └─ MP 数学性质也不成立 → discard
```

**反例处理**：自动归档到 `MetBench_SystemMT.Tests/Features/Counterexamples/`，**不刻意为论文造势**。若数量积累，回头考虑独立论文（user 指令：先做实验，再考虑论文）。

---

## §7 三元组 deliverable per cell

```
MetBench_SystemMT.Tests/Features/MrLibrary/<cell>.feature  # BDD canonical，含 5D tags
SUT/<sut-name>/                                            # SUT runner + adapters + sample
MetBench_SystemMT.Tests/Steps/<sut-name>Steps.cs           # step bindings
```

LiteDB 索引 = `tools/feature_to_db.py` 扫 feature tags → `MetamorphicRelations` 表（无独立 YAML mirror）。

**Stage-level**:
- `docs/mr_library/INDEX.md`：17 cells 覆盖矩阵 + MR 数 + V1/V2 通过率
- `docs/mr_library/counterexamples/INDEX.md`：反例归档（如有）

---

## §8 与既有衔接

| 既有 | 衔接 |
|---|---|
| Goal 1 meta-prompt engine | Phase 8.2.5 验证 + 8.3 大规模用 |
| 旧 5-equation plan（PR #68） | supersede（保留 audit trail） |
| 4 SUT（OpenMOC/OpenMC/heat/projectile） | Phase 8.2 升级 5D tag；heat → fourier cell；projectile 标 demo |
| Cmrlibrary 57 种子 MR | Phase 8.2.5 优先入库（与 5 方程 17 cells 兼容） |
| PWR_MR_Analysis 27 PWR MR | 同上，按 §5 cell 分类 |
| BDD .feature + LiteDB 现有约定 | **沿用**，不引 YAML mirror |
| F11 m_adj 月度监控 | (boltzmann, Num) cell 加 m_adj MR 占位（→ MP_inv 自伴随归类） |
| BNCT | **搁置** Stage 9+ |
| 故障注入 V3 | **独立挂起** Stage 9+ |

---

## §9 决策点

| # | 项 | 推荐 |
|---|---|---|
| 1 | 首批 cells | **7 cells**（D₁ × 5 + D₂ × 2 = 必填） → 扩展 12 (+Surr) → 17 (+PINN) |
| 2 | MR 存储 | **BDD .feature + Gherkin tags + LiteDB sync**（沿用现有约定）|
| 3 | 首 PR 切片 | **Phase 8.0** 5D tag schema 扩展 + LiteDB sync 工具 |
| 4 | 启动时机 | **v2.1 发版后** |
| 5 | 论文 | **暂不绑定**（user 指令：先做实验，发现 bug 再说） |

—— 下一步 writing-plan。
