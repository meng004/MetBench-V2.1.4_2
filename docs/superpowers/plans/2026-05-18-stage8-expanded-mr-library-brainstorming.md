# Stage 8 expanded MR library — brainstorming

> **Date**: 2026-05-18
> **Status**: Brainstorming（细化需求；下一步 writing-plan）
> **Supersedes**: [`2026-05-18-reactor-physics-five-equations-brainstorming.md`](2026-05-18-reactor-physics-five-equations-brainstorming.md)（旧 narrow 5-equation scope，理由：未覆盖 4 程序类型正交 + 未对接 P-series Cmrlibrary 5D schema + 缺 BNCT）
> **Upstream refs**（外部，未入仓）: P-series CLAUDE.md（论文协作硬约束）+ Cmrlibrary.md（5 维 MR schema + 12 网格 + 57 条种子 MR + 6 类来源）
> **Carryover refs**（入仓）: NOETHER 8 元模式 + `MetBench_BLL.SystemMT.Discovery.*` 现有 LLM gateway + 4 SUT（OpenMOC / OpenMC / heat / projectile）

---

## §1 需求重述

用户在 [现 session #N+1] 提出（中文原话节录）：

> 针对具备显式数学物理方程的科学计算程序，暂时分为**数值模拟、概率、机器学习代理模型、PINNs 四类**，专业域分为**中子输运、中子扩散、燃耗和热工**，现在准备**增加 BNCT 硼中子放疗**，请先按程序求解的数学物理方程归类，再根据程序类型、专业域分类，**基于已知 MR 元模式，推导各方程的似然 likely MR**，形成 MR 库，然后，搜索开源或公开的代表性程序，使用 **MetBench 作为载体**，存储 MR，执行 MT，最终形成**一组程序、一组 MR、一组测试用例**。为未来相似程序的验证奠定基础。

解析：

| 维度 | 取值 | 数 |
|---|---|---|
| 程序类型 (D₂) | 数值模拟 / 概率 (MC) / ML 代理 / PINNs | **4** |
| 专业域 | 中子输运 / 中子扩散 / 燃耗 / 热工 / **BNCT** | **5** |
| 元模式 (D₃) | NOETHER 8 块（m_inv / m_mono / m_conv / m_cmp / m_dyn / m_adj / m_rev / 候选 P₉） | **8** |
| 单 cell deliverable | 程序集 + MR 集 + 测试用例集 | 三元组 |

矩阵规模上界: 4 程序类型 × 5 域 × 8 元模式 = **160 cells**。务实约束（每元模式不一定每 cell 都成立）→ 实际 likely MR ≈ 30-50 条 + 程序候选 ≈ 15-25 个 + 测试用例 ≈ 20-40 条。

---

## §2 数学物理方程归类（D₁）

按 Cmrlibrary.md §C.1.1 编码：

| 编码 | 方程 | 专业域归属 | 备注 |
|---|---|---|---|
| **A** | Boltzmann 中子输运 ∂φ/∂t + ... = Sφ + Q | 中子输运 + BNCT 共享 | OpenMC / OpenMOC / MCNP 现有 |
| **B** | 多群中子扩散 -∇·D∇φ + Σ_a φ = ν Σ_f φ/k | 中子扩散 | PARCS / NESTLE / OpenNodal 候选 |
| **C** | Bateman 核素链 dN_i/dt = ΣY_ji λ_j N_j - λ_i N_i | 燃耗 | ORIGEN / OpenMC depletion 候选 |
| **D** | Fourier 热传导 ρc∂T/∂t = ∇·(k∇T) + q''' | 热工（固体燃料） | FRAPCON / BISON 候选 / home-grown 1D |
| **E** | NS 简化 1D 系统级（质量 / 动量 / 能量守恒） | 热工（冷却剂） | RELAP5 / CTF / OpenFOAM 子集 |
| **F** | 蒙特卡洛专有（非方程，统计抽样规则） | 跨域，仅 D₂=蒙特卡洛 cell 内 | OpenMC / MCNP 通用 |
| **G** | ML 代理 / PINN 专有（学习的 surrogate，加物理残差） | 跨域，仅 D₂=ML/PINN cell 内 | DeepONet / FNO / R²-PINN |
| **H** (新增) | **BNCT 剂量学**：D = D_n + D_p + D_α + D_γ + RBE 加权 | BNCT | TOPAS / MCNP-BNCT / NCTPlan / SERA |

BNCT 在方程层面**主要是 A (Boltzmann) + 新 H (剂量学)** 两段串接。H 不是独立 PDE，是 A 的能谱 + 反应产物 → 剂量积分 + RBE 生物加权。结构上：

```
BNCT pipeline:
  ¹⁰B 浓度分布 (经验输入) +
  Boltzmann 输运 (A) → 中子+光子+带电粒子通量 φ →
  剂量积分 (H) → D_n / D_p / D_α / D_γ →
  RBE 加权 (H) → 等效剂量 D_RBE →
  生物效应 (LQ 模型, 可选) → 杀伤率 / 治疗增益比 TER
```

---

## §3 程序类型 × 专业域 矩阵（D₂ × 域）

每格列**至少 1 个开源 / 公开代表性程序候选**：

| 程序类型 ↓ \ 专业域 → | 中子输运 (A) | 中子扩散 (B) | 燃耗 (C) | 热工 (D + E) | BNCT (A + H) |
|---|---|---|---|---|---|
| **D₁ 数值确定性** | OpenMOC ✅（MOC）, NEWT (SCALE 子集) | PARCS, NESTLE, **OpenNodal**, **Cerberus** | ORIGEN-S, **PyNE depletion**, home-grown Bateman | **OpenFOAM**（CFD 子集）, **CTF**（subchannel）, BISON-mini | NCTPlan（学术 release）, MCNP-BNCT |
| **D₂ 蒙特卡洛** | OpenMC ✅, MCNP（商业）, **Serpent**（学术 license） | — (扩散不走 MC) | OpenMC depletion ✅（已装）, MCNP-burnup | — (CFD 不走 MC) | **TOPAS**（开源 Geant4 wrapper）, MCNP-BNCT |
| **D₃ ML 代理** | **DeepONet for transport**（论文 release）, FNO 神经算子 | DeepONet for diffusion | ML burnup surrogate（论文） | DeepONet AP-1000（论文 release） | ML BNCT dose predictor（论文 release，OpenSource 待查） |
| **D₄ PINN** | R²-PINN, NAS-PINN, CNN-PINN | PINN diffusion | PINN burnup（少） | CHF-PINN（开源候选） | — (BNCT PINN 极少） |

**确定候选程序候选清单**（按优先级排序，cloud-friendly 优先）：

| 程序 | 类型 | 域 | 安装方式 | cloud 可行性 | 备注 |
|---|---|---|---|---|---|
| **OpenMOC** ✅ | D₁ | A | 已装 `/opt/openmoc-venv` | ✅ | 现有 SUT |
| **OpenMC** ✅ | D₂ | A + C | 已装 `/opt/openmc-venv` + binary 0.15.x | ✅ | 现有 SUT + 自带 depletion |
| **PyNE** | D₁ | C | `pip install pyne`（带 ENDF/B 二进制约 200MB） | ✅ | Bateman + 核素链工具 |
| **OpenNodal**（github.com/CamelEnergy/OpenNodal 或类似学术 release） | D₁ | B | git clone + Python 3 | 需查证（可能学术不开源） | nodal expansion 扩散候选 |
| **OpenFOAM** | D₁ | E | apt 装（30+ min 编译，~1 GB） | ⚠️ 重 | CFD 子集（SimpleFoam）够用 |
| **TOPAS** | D₂ | A + H | binary download + Geant4 依赖（≈ 2 GB） | ⚠️ 重 | BNCT 候选 |
| **home-grown Bateman ODE** | D₁ | C | Python stdlib + scipy | ✅✅ | 最稳，仅依赖 scipy.integrate |
| **home-grown 1D Fourier** | D₁ | D | Python stdlib + numpy | ✅✅ | 最稳 |
| **home-grown 1D subchannel** | D₁ | E | Python stdlib + scipy | ✅✅ | 简化版 |
| **home-grown BNCT dose 简化** | D₁ | H | Python + OpenMC 中子通量 → 剂量积分 | ✅ | 复用 OpenMC 输出 |
| **DeepONet 论文 release**（lululxvi/deepxde + 公开权重） | D₃ | A/B/D | `pip install deepxde` + 公开权重下载 | ⚠️ 需验权重可获取 | ML 代理 |
| **R²-PINN release**（github 论文 release） | D₄ | A | git clone + PyTorch | ⚠️ 需查证 | PINN 候选 |

**推荐 minimum viable matrix**（5 cells, 一个 session 内可成）:

| Cell | 程序 | 元模式 likely MR (选 1) |
|---|---|---|
| (D₁, A) | OpenMOC ✅ | m_inv: 几何旋转下 ∫φ 守恒 |
| (D₂, A) | OpenMC ✅ | m_cmp: OpenMC vs OpenMOC 同 pin-cell k_eff 一致（已有，强化） |
| (D₁, C) | home-grown Bateman ODE | m_conv: 时间步 → 0 时核素数 Cauchy 收敛 |
| (D₁, D) | home-grown 1D Fourier | m_inv: 镜像对称几何 + 对称源 → 对称温度场 |
| (D₁, H) | home-grown BNCT simplified | m_mono: ¹⁰B 浓度 ↑ → 肿瘤剂量 ↑ |

**完整矩阵 ≈ 20-30 cells**，分 W14-W20 落地。

---

## §4 元模式 × 方程 → likely MR 推导

按 Cmrlibrary.md §C.1.3 8 元模式 × 8 方程：

### §4.1 推导矩阵概览

✅ = 强 likely（直接 from algebraic property）
⚠️ = 弱 likely（need precondition / 限定）
— = 不适用
? = 需 LLM-driven 探索

| 元模式 ↓ \ 方程 → | A 输运 | B 扩散 | C 燃耗 | D 热传导 | E 热工 | F MC | G ML | H BNCT |
|---|---|---|---|---|---|---|---|---|
| **P₁ m_inv** 不变性 | ✅ 旋转/平移/置换 | ✅ 旋转/平移 | ⚠️ 核素置换 | ✅ 镜像/旋转 | ✅ 几何对称 | ⚠️ 同种子流 | ⚠️ 训练域内 | ✅ 患者解剖镜像 |
| **P₂ m_mono** 单调性 | ⚠️ 截面区间限定 | ⚠️ 同上 | ✅ 燃耗→k_inf 单调降（寿期内） | ✅ 源 ↑ → T ↑ | ✅ 流量 ↓ → T峰 ↑ | — | ⚠️ 须验等变 | ✅ ¹⁰B 浓度 ↑ → D_α ↑ |
| **P₃ m_conv** 仿射/线性 | ✅ 源齐次 φ→αφ | ✅ 同上 | ⚠️ Bateman 局部线性 | ✅ 源齐次 T→αT | ⚠️ 层流区线性压降 | — | ? 需训练验证 | ✅ ¹⁰B 浓度齐次 |
| **P₄ 退化/极限** | ✅ 各向同性散射→扩散 | ✅ D→∞ 退化 | ✅ 燃耗=0→新堆 | ✅ k→∞ 退化均匀 | ✅ 空泡→0→单相 | ✅ 历史数→∞ | ✅ 训练样本→∞ | ✅ ¹⁰B=0→纯 γ 剂量 |
| **P₅ m_cmp** 跨实现 | ✅ OpenMOC vs OpenMC | ⚠️ 扩散 vs 输运（粗-精对照） | ⚠️ ORIGEN vs PyNE | ⚠️ 数值 vs 解析（1D 球） | ⚠️ 系统 vs 子通道 | ✅ 不同 RNG seed | ? ML vs 真值 | ⚠️ MCNP-BNCT vs TOPAS |
| **P₅ m_dyn** 多执行自洽 | ⚠️ 重启等价 | ✅ 重启等价 | ✅ 燃耗分段 vs 一段 | ✅ 重启 | ✅ 瞬态重启 | ⚠️ 重跑同种子 | ⚠️ 重新训练同种子 | ⚠️ 重跑剂量 |
| **m_adj** 自伴反应度 | ✅ 但 OpenMOC adjoint 缺失 (F11) | ⚠️ 部分支持 | — | — | — | — | — | ⚠️ BNCT 中可考虑 BB10 灵敏度 |
| **m_rev** 时间反演 | ⚠️ 稳态 trivial | ⚠️ 同上 | ⚠️ Bateman 反向 ill-posed | — | — | — | — | — |

**强 likely MR 数 (✅)** ≈ 25 个。**弱 likely (⚠️)** ≈ 20 个。**总 candidate pool ≈ 45 个**。

### §4.2 元模式 meta-prompt 模板需求

Goal 1（meta-prompt engine）的 8 个模板要分别针对：

| 元模式 | meta-prompt 应注入的 SUT 信息 | 期望 LLM 输出 |
|---|---|---|
| P₁ m_inv | 输入参数清单 + 几何对称属性 + 守恒量列表 | "g(x) = R·x, R∈SO(3) 时 f(g(x)) = f(x)" 等候选 |
| P₂ m_mono | 输入参数 + 物理直觉单调对偶 | "x ↑ → y ↑ 适用域 [..]" |
| P₃ m_conv | 输入参数 + 算子线性候选 | "f(αx + βy) = αf(x) + βf(y)" |
| P₄ 退化 | 极限参数候选（h→0, T→0, n→∞） | "lim_{p→p*} f(x;p) = g(x)" |
| P₅ m_cmp | 跨实现参考对照表（同 cell 内程序候选） | "f₁(x) ≈ f₂(x) within tol(...)" |
| P₅ m_dyn | 多次执行 / 重启切点 | "f(x) = f(restart f, t/2)" |
| m_adj | 自伴算子检测 | "⟨Lφ, ψ⟩ = ⟨φ, L*ψ⟩" |
| m_rev | 时间反演候选 | "f(x; t) = f(g(x); -t)" |

---

## §5 MetBench 升级需求（5D schema 落地）

当前 MetBench `MetaPattern` 实体只有 `Code` / `Name` / `Status` / `HypothesisTemplate` / `DefaultAssertionTypeCode` 等。需扩展为：

**`MetamorphicRelation` v2 schema**（对照 Cmrlibrary §C.5.2）：

```yaml
mr_id: A.07_total_flux_rotation
class: A                       # 严谨性 A/B/C
relation_type: equation        # equation / inequality / monotone / convergence
D1_equation: A_boltzmann       # 8 方程编码
D2_program_types: [D1, D2]     # 适用程序类型
D3_metapattern: P1_invariance  # 8 元模式
D4_source_level: L2_physics_law # 5 来源层次
D5_lrca_targets: [C1, C2]      # 5 故障关联
mathematical_form: |
  ∫_V φ(r) dV = ∫_V φ(R·r) dV
input_transformation: rotation_geometry_so3
output_relation: integral_equality
tolerance:
  fp32: 1.0e-4
  fp64: 1.0e-12
implementation: SUT/openmoc/transformations/A07_rotate.py
test_subjects: [openmoc-pincell, openmc-pincell]
verification:
  V1_math: passed
  V2_implementation: passed
  V3_fault_injection: passed
references: [Bell-Glasstone 1970, Ch.1]
```

落地动作：

1. **新增 `MetBench_Domain/V2/MetamorphicRelationV3.cs`**（加 5D 字段；旧 V2 兼容）
2. **`MetBench_DAL` `IMetamorphicRelationRepository` 扩接口**（按 5D 维度查询）
3. **`MetBench_BLL.Core/SystemMT/Discovery/MrLibrary/*`** 新 namespace 存 MR 库 service
4. **YAML / JSON ↔ LiteDB 双向 sync 工具**（`tools/mr_library_sync.py`）
5. **5D 索引 dashboard**：可视化 cell 覆盖率（哪些 cell 已填 MR / 哪些空 / 哪些 verified）

---

## §6 三元组 deliverable 切片策略

**Per cell 工作流**:

```
Cell(D₂, D₁, 域) =
  step 1. 程序候选清单 (markdown表，含 url + install)
       2. 选 1 个程序接 MetBench (runner / adapter)
       3. 元模式 × 方程矩阵选 likely MR (≥1 条)
       4. meta-prompt engine 跑一遍 (可选自动) → 候选 MR
       5. 手工验证 + 写 mathematical_form + tolerance
       6. 录入 MetBench LiteDB + YAML mirror
       7. 写 1 个 BDD scenario → BDD trx baseline
       8. V1/V2/V3 三层验证（V3 故障注入留作扩展）
```

**Stage-level deliverable**:

- `docs/mr_library/<cell-id>.md` × N cells
- `docs/mr_library/INDEX.md`（5D 索引 + cell 覆盖矩阵）
- LiteDB `MetamorphicRelations` collection 填充 N 条
- `MetBench_SystemMT.Tests/Features/MrLibrary/*.feature` × N
- `tools/mr_library_*.py` 同步 / 校验 / 报表

**优先 cell 排序（资源消耗少 + 论文价值高）**:

1. **(D₁ 数值, A 输运)** = OpenMOC — 已有，强化 5D 元数据
2. **(D₂ MC, A 输运)** = OpenMC — 已有，加 m_cmp + m_dyn
3. **(D₁ 数值, C 燃耗)** = home-grown Bateman — 1 天内可成
4. **(D₁ 数值, D 热传导)** = home-grown 1D Fourier — 半天可成
5. **(D₁ 数值, H BNCT)** = home-grown simplified（复用 OpenMC 中子通量）— 1-2 天
6. **(D₁ 数值, B 扩散)** = OpenNodal 或 home-grown nodal — 1-2 天
7. **(D₁ 数值, E 热工)** = home-grown 1D subchannel — 1 天
8. **(D₃ ML, A 输运)** = DeepONet 公开权重 — 2-3 天（含权重下载）
9. **(D₄ PINN, A 输运)** = R²-PINN 公开 release — 2-3 天

**~9 cells / 14 工时** 是合理 stage-level scope。剩余 cells（高资源消耗如 OpenFOAM / TOPAS / 商业 MCNP）作为论文 limitation 段公开。

---

## §7 风险 + 缓解

| 风险 | 缓解 |
|---|---|
| 程序候选不可获取（学术 release 找不到） | 用 home-grown 简化版顶替；论文里诚实声明"代表性程序无法本地复现，用 surrogate validated against textbook benchmarks" |
| 元模式 × 方程矩阵推导漏关键 MR | meta-prompt engine 跑 8 元模式自动批扫一遍，LLM 候选补漏 |
| 5D schema 落地破坏现有 MetBench LiteDB | 走 LiteDB schema migration（PR #62 模式：新字段加 + 自动迁移；旧 MR 默认值兼容） |
| BNCT 物理 + 剂量学超出团队专长 | 与医学物理合作者 / 现有 BNCT 论文参考实现对照；先做 simplified dose model 顶替 |
| ML / PINN 公开权重无法下载 / 跨平台不兼容 | 用 pytorch 自带的最简 GP 代理顶替；论文里说明"用学术 surrogate 验证元模式" |
| Stage 8 scope 失控 | 每 cell 限 1-2 天；每周 review；优先级低 cell 留作论文 future work |

---

## §8 与既有工作的衔接

| 既有 | 衔接方式 |
|---|---|
| Goal 1 meta-prompt engine（旧 Stage 8） | **保留**，作为 Goal 2 推导矩阵的工具基础 |
| 旧 5-equation plan（PR #68 committed） | **supersede**；plan 文件保留作历史参考，在 doc 头加 deprecated notice |
| 4 SUT (OpenMOC / OpenMC / heat / projectile) | 全部 keep；OpenMOC + OpenMC 升级 5D 元数据；heat_equation 升级为正式 Fourier D cell；projectile 标 demo（非反应堆物理 cell） |
| Cmrlibrary.md 57 条种子 MR | 选与 4 现有 SUT 兼容的（如 H-N-01 源齐次性 / S-N-01 几何对称 / L-N-01 燃耗 0 退化）作为首批入库测试 |
| F11 m_adj 月度监控 | 衔接：m_adj 解锁后，A cell 增 m_adj MR 一条（NOETHER 预测） |

---

## §9 决策点（待 user 拍板）

| 项 | 选项 |
|---|---|
| **首批 N cells scope** | (a) 5 cells 最小可行（OpenMOC + OpenMC + 3 home-grown）<br>(b) 9 cells 推荐（含 BNCT + ML/PINN）<br>(c) 全 30 cells 矩阵（多月工程） |
| **MR 库存储格式** | (a) 仅 LiteDB 内（紧凑）<br>(b) **LiteDB + YAML 镜像** 入 git 仓（可 review，推荐）<br>(c) 仅 YAML 入 git（无运行时索引） |
| **三层验证 V3 故障注入** | (a) Stage 8 内只做 V1+V2<br>(b) V3 作为 Stage 9 单独立项<br>(c) Stage 8 末段做 V3 简化（OpenMOC 已有 mutmut 数据复用） |
| **BNCT 优先级** | (a) 与其它 cell 并列<br>(b) 作为 stage 8 末段压力测试 cell<br>(c) 单独 Goal 3 立项 |
| **论文绑定** | (a) P1 经验审计 / (b) P2 IST / (c) 新论文 P6 / (d) 不绑定，先工程 |

—— 下一步进入 writing-plan，固化具体 phase × deliverable × 工时。
