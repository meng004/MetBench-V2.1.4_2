# MetBench 术语与缩写规范（GLOSSARY）

> **目的**：统一 MetBench + Stage 8 MR 库 + P-series 论文协作语境下的术语，消除歧义（如 BDD "scenario" vs launcher 历史 "scenario"），固化缩写规则。
> **更新**：2026-05-18 (Stage 8 启动)
> **上游对接**：P-series Cmrlibrary.md（外部上传，未入仓）+ NOETHER 8 元模式

---

## §1 MT 框架

| 中文 | 英文 | 缩写 | 含义 |
|---|---|---|---|
| 蜕变测试 | Metamorphic Testing | **MT** | 通过 MR 验证程序行为 |
| 蜕变关系 | Metamorphic Relation | **MR** | 输入变换 g + 输出关系 R |
| 被测系统 | System Under Test | **SUT** | 真实被测程序 / runner |
| 源用例 | Source Case | **src** | 原始输入 |
| 跟随用例 | Follow-up Case | **flw** | 变换后输入 |
| 输入生成器 | Input Generator | — | 从 src 生成 flw |
| 输出适配器 | Output Adapter | — | 解析 SUT 输出 |
| 判定准则 | Oracle / Assertion | — | 判定 MR 是否成立 |

---

## §2 5 MetaPattern（MP）定义 + 实例

> **元模式（MetaPattern, MP）**：从方程算子的代数性质推导的 MR 大类。共 **5 类**，覆盖 NOETHER 8 元素（见 §6 映射表）。
>
> **关键约定**：元模式的适用性由**方程数学性质**决定，跟程序类型（Num/MC/Surr/PINN）无关；程序类型只影响 **oracle 判定方式 + tolerance 量级**。

### MP_inv 守恒性 / 不变性

**定义**：对变换 g（旋转 / 平移 / 置换 / 源齐次 / 守恒律 / 跨实现严格等价 / 自伴算子），输出关系是**等式或不变量**：

```
R(SUT(src), SUT(g(src))) = equality / conservation
```

涵盖：几何对称、代数齐次、守恒律（质量/能量/中子）、跨实现严格相等、自伴算子。

**实例**（boltzmann + Num + OpenMOC）：

```
g = 几何旋转 90° around z 轴
SUT = OpenMOC pin-cell k_eff calculation
R: |k_eff(src) - k_eff(rotate(src))| < 1e-6
数学根据：boltzmann 输运算子在 SO(3) 群作用下不变
```

---

### MP_mono 单调性

**定义**：单参数变化下输出有**确定的全序方向**：

```
x₁ < x₂ ⇒ SUT(x₁) < SUT(x₂)   或   SUT(x₁) > SUT(x₂)
```

**实例**（boltzmann + Num + OpenMOC）：

```
x = 燃料富集度（3% → 5%，区间内）
SUT = OpenMOC k_eff
R: enrichment ↑ ⇒ k_eff ↑（仅符号判定）
数学根据：Σ_f 单调升 + 中子产生项主导 + 区间内未触发反向效应
适用域：富集度 ∈ [3%, 20%]，超出可能反向
```

---

### MP_conv 收敛性 / 极限退化

**定义**：对离散化参数（时间步 dt / 网格 h / 样本数 N）按预期**收敛阶**逼近极限，或参数极值时退化到已知简单情形：

```
SUT(p) - SUT_exact = O(p^q)   (q 阶收敛)
   或   lim_{p → p*} SUT(p) = SUT_degenerate
```

涵盖：Cauchy 序列、Richardson 外推、退化极限（k → ∞ 均匀化 / 燃耗 t=0 → 新堆）。

**实例**（fourier + Num + home-grown 1D Fourier）：

```
p = 空间网格 h
SUT = 1D 径向 fourier 热传导 T(r)
R: 三网格序列 (h, h/2, h/4) → ‖T_h - T_{h/2}‖ : ‖T_{h/2} - T_{h/4}‖ ≈ 4 : 1（二阶中心差分）
数学根据：FDM 离散误差 O(h²)
```

---

### MP_traj 轨迹性

**定义**：多次执行（分段 / 重启 / 时间序列）的**轨迹结构一致性**：

```
SUT(0 → T) = SUT(T₁ → T) ∘ SUT(0 → T₁)    (重启等价)
   或   Σ_i SUT(batch_i) ≡ SUT(all batches) (统计聚合)
   或   SUT(forward) ↔ SUT(reverse)         (时间反演，如适用)
```

**实例**（bateman + Num + home-grown Bateman ODE）：

```
SUT = U-235 burnup chain (bateman ODE 求解 100 day)
g = 时间分段 (50 day + 50 day 重启) vs 一段 (100 day 连续)
R: ‖N_segmented_end - N_single_end‖_∞ / N_max < 1e-5
数学根据：bateman ODE 是线性 autonomous 系统，分段积分 = 累积积分
```

---

### MP_part 偏序性

**定义**：偏序集（partial order, poset）上**仅可比元素**的序关系保持：

```
x ≼ y in input poset   ⇒   SUT(x) ≼ SUT(y) in output poset
（不要求 ∀x, y 可比；仅对 ≼ 关系有定义的对成立）
```

与 MP_mono 区别：全序 (total) vs 偏序 (partial)。

**实例**（boltzmann + Num + OpenMOC，多组件控制棒）：

```
x = 控制棒插入深度向量 (d₁, d₂, ..., dₙ)
   A = (5, 0, 0, 0, ...)  insert rod 1 by 5 cm
   B = (5, 5, 0, 0, ...)  insert rod 1 AND rod 2 by 5 cm
   A ≼ B（逐分量 A_i ≤ B_i）
   C = (10, 0, 0, ...)    insert only rod 1 by 10 cm
   B 与 C 不可比（B 在 d₂ 更大，C 在 d₁ 更大）
SUT = OpenMOC 反应性 ρ
R: A ≼ B ⇒ ρ(A) ≽ ρ(B)（反应性偏序反向，吸收上升）
   B 与 C 不强制 ρ(B) ≼ ρ(C) 或反向
数学根据：吸收截面对中子保平衡偏序保持，但全序 (single rod) 反例存在
```

---

## §3 BDD 术语（Reqnroll / Gherkin）

> ⚠️ **Feature ≠ Scenario** — 不同义，严格区分。

| 术语 | 含义 | 项目内用法 |
|---|---|---|
| **Feature** | 一个 `.feature` 文件 / 高层目标 | 1 Feature = 1 MR family |
| **Scenario** | Feature 下的具体测例 | 1 Scenario = 1 MR 实例 |
| **Scenario Outline** | 参数化 Scenario | 配 Examples 表 |
| **Examples** | Outline 的参数化样本表 | 一行 = 一个 Scenario instance |
| **Step** | Given / When / Then 一行 | — |
| **Background** | 所有 Scenario 共享前置 | 文件顶部 |
| **Tag** | `@` 标注，过滤 / 元数据 | 用于 5D 元数据如 `@MR.Equation=boltzmann` |

### 完整 .feature 实例

```gherkin
@MR.Equation=boltzmann @MR.ProgramType=Num @MR.MetaPattern=MP_inv
@MR.SourceLevel=L2 @MR.FailureCorrelation=C1,C2
@MR.Class=A @MR.Rel=equation @MR.Tol=1e-6
Feature: boltzmann transport — geometric rotation invariance (OpenMOC pin-cell)

  Background:
    Given OpenMOC venv is available at METBENCH_OPENMOC_PYTHON
    And the SUT runner is "SUT/openmoc/openmoc_runner.py"

  # 1 Scenario = 1 MR 实例（固定参数 90°）
  Scenario: Rotate pin-cell by 90 degrees preserves k_eff
    Given a pin-cell input "SUT/openmoc/sample/pincell.json"
    And a rotation of 90 degrees around z-axis
    When source case runs through OpenMOC
    And follow-up (rotated) case runs through OpenMOC
    Then |k_eff_src - k_eff_flw| < 1e-6

  # 1 Scenario Outline + 4 Examples rows = 4 MR 实例
  Scenario Outline: Rotate pin-cell by arbitrary angle preserves k_eff
    Given a pin-cell input "SUT/openmoc/sample/pincell.json"
    And a rotation of <angle> degrees around z-axis
    When source case runs through OpenMOC
    And follow-up (rotated) case runs through OpenMOC
    Then |k_eff_src - k_eff_flw| < <tolerance>

    Examples:
      | angle | tolerance |
      | 45    | 1e-6      |
      | 90    | 1e-6      |
      | 180   | 1e-6      |
      | 270   | 1e-6      |
```

**计数**：1 Feature 文件 = 1 MR family；含 1 Scenario + 4 Examples rows = **5 MR 实例**。

---

## §4 5 方程 / 程序类型 / 5D 索引

### 4.1 5 方程（英文全称 + 前缀 `E_`）

| Cmrlibrary 编码 | 项目命名 | 中文 |
|---|---|---|
| A | **boltzmann** | 玻尔兹曼中子输运 |
| B | **diffusion** | 中子扩散 |
| C | **bateman** | 核素链燃耗 |
| D | **fourier** | 热传导 |
| E | **NS** | Navier-Stokes 简化（1D 系统级） |

> 元数据字段可用 `E_boltzmann`、`E_diffusion` 等前缀化形式（避免与项目内其它名字撞）。日常引用用方程全称（小写）。

### 4.2 程序类型

| Cmrlibrary 编码 | 项目命名 | 中文 |
|---|---|---|
| D1 | **Num** | numerical deterministic / 数值确定性 |
| D2 | **MC** | Monte Carlo / 蒙特卡洛 |
| D3 | **Surr** | ML surrogate / 机器学习代理 |
| D4 | **PINN** | Physics-Informed Neural Network |

### 4.3 5D 索引（全称字段名）

| 维度 | 字段名 | 取值 |
|---|---|---|
| 1 | **Equation** | boltzmann / diffusion / bateman / fourier / NS |
| 2 | **ProgramType** | Num / MC / Surr / PINN |
| 3 | **MetaPattern** | MP_inv / MP_mono / MP_conv / MP_traj / MP_part |
| 4 | **SourceLevel** | L1 程序规约 / L2 物理定律 / L3 算法性质 / L4 实现细节 / L5 训练性质（SciML） |
| 5 | **FailureCorrelation** | C1 真语义 / C2 数值容差 / C3 OOD / C4 统计假设违反 / C5 mutator artefact |

### 4.4 程序类型 × Oracle/Tolerance 约定

| 程序类型 | Oracle 判定方式 | 默认 tolerance | 特殊处理 |
|---|---|---|---|
| **Num** | 绝对差 / 相对差 | fp32: 1e-4 ~ 1e-5；fp64: 1e-8 ~ 1e-12 | 守恒律：机器精度；尺度律：扣除离散误差 |
| **MC** | 统计显著性 (3σ test) | 统计误差 σ ∝ 1/√N | 必须固定同 RNG seed；single-run 不能判 MR；MP_traj 用 "多 batch 平均" 代替 "重启" |
| **Surr** | 训练域内相对误差 | 训练域内 5%；域外不判 | 必须先验证输入点在训练域；MR 失败先排查 OOD 再判 bug |
| **PINN** | 物理残差 / 训练损失 | 残差 < 1e-3；损失 stagnate 后判定 | MR 严格成立度依赖 enforce-equivariance 训练设计；通常只能近似 |

---

## §5 NOETHER 8 ↔ 5 MP 映射

PWR_MR_Analysis.md §2.7 验证：27 条 PWR 新增 MR 全部自然落入 5 MP，**P9 候选第九块**不需要新增。

| NOETHER 8 | 5 MP 归属 | 实例 |
|---|---|---|
| **m_inv** 不变性 | **MP_inv** | (boltzmann, Num, OpenMOC) 几何旋转 90° → k_eff 不变 |
| **m_mono** 单调性 | **MP_mono** | (boltzmann, Num, OpenMOC) 燃料富集度 3%→5% → k_eff 单调升 |
| **m_conv** 收敛性 | **MP_conv** | (fourier, Num, 1D Fourier) 网格 h→h/2→h/4 → 解按 O(h²) 收敛 |
| **m_cmp** 跨实现一致 — 情形 A | **MP_inv**（严格相等） | (diffusion, Num, PARCS) CMFD on/off 收敛解相同（等式）|
| **m_cmp** 跨实现一致 — 情形 B | **MP_part**（偏序） | (boltzmann↔diffusion, Num, OpenMOC↔PARCS) 强吸收区 k_eff^diff > k_eff^trans（偏序） |
| **m_dyn** 多执行自洽 | **MP_traj** | (bateman, Num, ODE) 50d + 50d 重启 vs 100d 一段 → 末态一致 |
| **m_adj** 自伴反应度 | **MP_inv** | (diffusion, Num, PARCS) 扩散方程自伴随：正向通量 ∝ 伴随通量（各向同性散射时） |
| **m_rev** 时间反演 | **MP_traj** | (boltzmann 瞬态) 理论上线性输运算子时间反演（实际很少强成立）|
| **P9** 候选第九块 | — 不需要 | PWR 27 条 MR 全落入 P1~P5（PWR_MR_Analysis §2.7 验证）|

---

## §6 MetBench 内部命名（post W11-W12）

| 概念 | 项目命名 | 历史命名（废弃）|
|---|---|---|
| MR-on-SUT UI 投影 | `MrSummary` | ~~`ScenarioDescriptor`~~ (PR #58) |
| MR 单次执行结果 | `MrRunResult` | ~~`ScenarioRunResult`~~ |
| Launcher 接口 | `ISystemMtMrLauncher` | ~~`ISystemMtScenarioLauncher`~~ |
| 持久化字段 (MR 名) | `MrName` | ~~`ScenarioName`~~ (PR #62) |
| MR id | `MrId` | ~~`ScenarioId`~~ |
| 批量请求 | `BatchMrRunRequest` | ~~`BatchScenarioRequest`~~ |

> **项目内 "scenario" 仅指 Gherkin Scenario**，不再指 launcher / persistence 域。

---

## §7 外部上传文档（参考）

| 文件 | 用途 | 不入仓 |
|---|---|---|
| `P-series CLAUDE.md` | P1-P5 论文协作硬约束（ANTI-CLAIM 规则 / IST 合规 / proofread pipeline / ARS Reviewer 2 视角） | 是 |
| `Cmrlibrary.md` | 附录 C MR 库分支：5D 索引 + 12 网格 + 57 条种子 MR + 6 类来源 + 三层验证 V1/V2/V3 | 是 |
| `PWR_MR_Analysis.md` | PWR 反应堆物理 5 层方程 + 27 条 PWR 新增 MR + PARCS/Serpent 适用性评估 | 是 |
| `MT____MR_______1998_2025_.md` | MT 文献 1998-2025 MR 数据源清单 + NUIT/HTGR 私有程序 MR 描述 | 是 |

---

## §8 引用

- PR #58: scenario → MR launcher 改名（消除与 Gherkin Scenario 撞名）
- PR #62: ScenarioName → MrName + LiteDB schema migration
- PR #68: Stage 8 初版 plan（已 supersede）
- PR #69（本）: Stage 8 expanded plan + 5 MP + 17 cells + 84 MR 母集

— 文档结束。
