# Stage 8 expanded MR library — brainstorming

> **Date**: 2026-05-18（rev：按 user 指令 BNCT 搁置 / V3 独立挂起 / Phase 8.2.5 端到端 / 8.3 完整研究工作流）
> **Status**: Brainstorming（细化需求；下一步 writing-plan）
> **Supersedes**: [`2026-05-18-reactor-physics-five-equations-brainstorming.md`](2026-05-18-reactor-physics-five-equations-brainstorming.md)（旧 narrow 5-equation scope，未覆盖程序类型正交 + 未对接 Cmrlibrary 5D schema）
> **Upstream refs**（外部上传，未入仓）: P-series CLAUDE.md（论文协作硬约束）+ Cmrlibrary.md（5 维 MR schema + 12 网格 + 57 条种子 MR + 6 类来源）
> **Carryover refs**（入仓）: NOETHER 8 元模式 + `MetBench_BLL.SystemMT.Discovery.*` 现有 LLM gateway + 4 SUT（OpenMOC / OpenMC / heat / projectile）

---

## §1 需求重述（rev）

User 指令分两段：**Phase 8.2.5 端到端验证**（用现有示例跑通核心 workflow）+ **Phase 8.3 大规模接入**（5 方程开源程序 + 完整研究工作流）。BNCT + V3 故障注入暂缓。

### §1.1 Phase 8.2.5 — 端到端核心 workflow 验证（**首要**）

> "在现有示例基础上，把核心功能跑通"

用 Goal 1 meta-prompt engine 跑现有 4 SUT 一遍：
- 验证主链路："方程算子 → 元模式选取 → 参数扫描 → prompt 构造 → LLM 识别 → MR 入库 / MT 执行" 全跑通
- 3 个分支结果（入库 / 反例 / 缺陷修复）至少各跑通 1 例作示范
- 这是 Phase 8.3 大规模接入的**最小可信范式**

### §1.2 Phase 8.3 — 5 方程开源程序接入 + 完整研究工作流

> "把 5 方程对应的开源或可获取程序作为下一阶段任务，目标是建立**可信 MR 库**"

完整研究工作流（每个 cell 跑一遍）：

```
1. 方程算子 → algebraic property 分析
2. 适用元模式选取（不变 / 单调 / 仿射 / 退化 / 一致 / ...）
3. 扫描 SUT 输入参数（自动 schema 解析 + 人工标注）
4. 代入元模式 → 构造 SUT-specific meta-prompt
5. LLM 识别 MR 实例（candidate pool, 多 LLM consensus）
6. MetBench 执行 MT (source + followup)
7. 分支判定（核心创新）:
   ├─ 高支持度 → MR 库
   ├─ 低支持度 + 元模式数学应成立 → 进入分析:
   │    ├─ MR 错（适用域 / tol 设错）→ 改 MR
   │    └─ 程序错（bug）→ 反例归档 → 积累 → 缺陷修复 → 论文
   └─ 元模式数学性质也不成立 → discard
```

**论文 narrative 升级**：从 "MR-based verification 演示"→ "**meta-pattern driven LLM-based MR identification with bug detection on open-source reactor physics codes**"。反例 + 缺陷反馈是论文 hard evidence。

### §1.3 暂缓项

| 项 | 状态 | 计划 |
|---|---|---|
| **BNCT 硼中子放疗** | Plan 内保留 + Stage 8 内**不实施** | Stage 9+ 候。理由：(a) 80% 中子输运（同 A 方程）+ 20% post-process，不构成新方程 cell；(b) BNCT 专属程序（NCTPlan / SERA / MultiPlan）大多商业 / 学术申请 / 停维，cloud 不可获取 |
| **故障注入 V3** | **独立模块挂起** | Stage 9 候。Stage 8 MR 库只做 V1（数学可推导）+ V2（程序执行）；V3（mutation kill rate）独立设计 + 复用 mutmut 现有基础设施 |

---

## §2 数学物理方程归类（D₁）

按 Cmrlibrary.md §C.1.1 编码，**Stage 8 主体聚焦 A-E 5 方程**:

| 编码 | 方程 | 专业域 | 备注 |
|---|---|---|---|
| **A** | Boltzmann 中子输运 ∂φ/∂t + Ω·∇φ + Σ_t φ = ∫Σ_s φ + νΣ_f φ/k | 中子物理 | OpenMC / OpenMOC 现有 |
| **B** | 多群中子扩散 -∇·D∇φ + Σ_a φ = ν Σ_f φ/k | 中子物理 | PARCS / NESTLE 学术 / home-grown nodal |
| **C** | Bateman 核素链 dN_i/dt = ΣY_ji λ_j N_j - λ_i N_i | 燃耗 | ORIGEN 学术 / OpenMC depletion / home-grown ODE |
| **D** | Fourier 热传导 ρc∂T/∂t = ∇·(k∇T) + q''' | 热工（固体燃料） | FRAPCON 学术 / BISON-mini / home-grown 1D |
| **E** | NS 简化 1D 系统级（质量 / 动量 / 能量守恒） | 热工（冷却剂） | RELAP5 学术 / CTF 学术 / home-grown 1D |
| ~~H BNCT~~ | ~~Boltzmann + dose 后处理~~ | ~~医学物理~~ | **Stage 8 不实施**，见 §1.3 |
| F (横切) | 蒙特卡洛专有 MR | D₂ cell 专属 | OpenMC + MCNP 共享，不算独立方程 cell |
| G (横切) | ML / PINN 专有 MR | D₃/D₄ cell 专属 | DeepONet / R²-PINN 等 |

---

## §3 程序类型 × 5 方程矩阵（D₂ × 域）

| 程序类型 ↓ \ 方程 → | A 输运 | B 扩散 | C 燃耗 | D 热传导 | E 热工 |
|---|---|---|---|---|---|
| **D₁ 数值确定性** | OpenMOC ✅, NEWT | **home-grown nodal**, PARCS (学术) | **home-grown Bateman ODE**, PyNE | **home-grown 1D Fourier**, FRAPCON-mini | **home-grown 1D subchannel**, CTF (重) |
| **D₂ 蒙特卡洛** | OpenMC ✅, MCNP (商业), Serpent | — | **OpenMC depletion** ✅, MCNP-burnup | — | — |
| **D₃ ML 代理** | DeepONet 论文 release | DeepONet | ML burnup surrogate | DeepONet AP-1000 | DeepONet AP-1000 |
| **D₄ PINN** | R²-PINN, NAS-PINN | PINN diffusion | (少) | CHF-PINN | CHF-PINN |

**Cloud-friendly 推荐程序候选**（按可获取性 + 安装成本排序）:

| 程序 | 类型 | 方程 | 安装 | Cloud 可行 | 备注 |
|---|---|---|---|---|---|
| **OpenMOC** ✅ | D₁ | A | 已装 `/opt/openmoc-venv` | ✅ | 现有 SUT |
| **OpenMC** ✅ | D₂ | A + C | 已装 `/opt/openmc-venv` | ✅ | 现有 SUT，自带 depletion |
| **home-grown Bateman ODE** | D₁ | C | Python stdlib + scipy | ✅✅ | 最稳，半天 |
| **home-grown 1D Fourier** | D₁ | D | Python stdlib + numpy | ✅✅ | 半天 |
| **home-grown 1D subchannel** | D₁ | E | Python stdlib + scipy | ✅✅ | 1 天 |
| **home-grown nodal 扩散** | D₁ | B | Python + numpy | ✅✅ | 1-2 天 |
| **PyNE** | D₁ | C | `pip install pyne`（ENDF/B 二进制 ≈ 200 MB） | ✅ | 替代 home-grown Bateman |
| **OpenFOAM** | D₁ | E | apt 装（30+ min 编译，~1 GB） | ⚠️ 重 | 留 Phase 8.4+ 候 |
| **PARCS / NESTLE** | D₁ | B | 学术申请 / 商业 | ❌ | Stage 9 候 |
| **CTF (PSU)** | D₁ | E | 学术申请 + C++ build | ❌ | Stage 9 候 |
| **TOPAS / NCTPlan**（BNCT 专属） | D₂ | A+H | Geant4 重 / 申请 | ❌ | BNCT 搁置 |

**Phase 8.3 minimum viable 6 cells**（每方程 1 个 + 中子输运双源）:

| Cell | 程序 | 论证 |
|---|---|---|
| (D₁, A) | OpenMOC ✅ | 现有，强化 5D + meta-prompt 验证 |
| (D₂, A+C) | OpenMC ✅ | 现有，加 depletion + m_cmp + meta-prompt |
| (D₁, B) | home-grown nodal | 替代 PARCS 不可获取 |
| (D₁, C) | home-grown Bateman | 替代 ORIGEN 不可获取 |
| (D₁, D) | home-grown 1D Fourier | 替代 FRAPCON 简化版 |
| (D₁, E) | home-grown 1D subchannel | 替代 RELAP5 不可获取 |

合计 **6 cells**（中子输运 OpenMOC + OpenMC 双源），覆盖 5 方程 D₁/D₂ 程序类型。

D₃/D₄ 横切作为 Phase 8.4 候（看精力 + 论文需要）。

---

## §4 元模式 × 5 方程 → likely MR 推导矩阵

按 Cmrlibrary §C.1.3 8 元模式 × 5 方程：

✅ = 强 likely（直接 from algebraic property）
⚠️ = 弱 likely（need precondition / 限定）
— = 不适用
? = 需 LLM-driven 探索

| 元模式 ↓ \ 方程 → | A 输运 | B 扩散 | C 燃耗 | D 热传导 | E 热工 |
|---|---|---|---|---|---|
| **P₁ m_inv** 不变性 | ✅ 旋转/平移/置换 | ✅ 旋转/平移 | ⚠️ 核素置换 | ✅ 镜像/旋转 | ✅ 几何对称 |
| **P₂ m_mono** 单调性 | ⚠️ 截面区间限定 | ⚠️ 同上 | ✅ 燃耗 → k_inf 单调（寿期内） | ✅ 源 ↑ → T ↑ | ✅ 流量 ↓ → T_peak ↑ |
| **P₃ m_conv** 仿射/线性 | ✅ 源齐次 φ→αφ | ✅ 同上 | ⚠️ Bateman 局部线性 | ✅ 源齐次 T→αT | ⚠️ 层流区线性压降 |
| **P₄ 退化/极限** | ✅ 各向同性 → 扩散 | ✅ D→∞ 退化 | ✅ 燃耗=0 → 新堆 | ✅ k→∞ 退化均匀 | ✅ 空泡→0 → 单相 |
| **P₅ m_cmp** 跨实现 | ✅ OpenMOC vs OpenMC | ⚠️ 扩散 vs 输运 | ⚠️ ORIGEN vs PyNE | ⚠️ 数值 vs 解析（1D 球） | ⚠️ 系统 vs 子通道 |
| **P₅ m_dyn** 多执行自洽 | ⚠️ 重启等价 | ✅ 重启等价 | ✅ 燃耗分段 vs 一段 | ✅ 重启 | ✅ 瞬态重启 |
| **m_adj** 自伴反应度 | ✅ OpenMOC adjoint 待 F11 | ⚠️ 部分支持 | — | — | — |
| **m_rev** 时间反演 | ⚠️ 稳态 trivial | ⚠️ 同上 | ⚠️ Bateman 反向 ill-posed | — | — |

**强 likely MR (✅)** ≈ 23 个。**弱 likely (⚠️)** ≈ 16 个。**总 candidate pool ≈ 39 个**（不含 BNCT，不含 D₃/D₄ 横切扩展）。

---

## §5 MetBench 5D schema 升级需求

详见 [plan §2 Phase 8.0]：

- **`MetamorphicRelationV3` entity**：5D 字段 + tolerance + V1/V2 验证状态
- **`IMetamorphicRelationV3Repository`**：按 5D 维度查询
- **LiteDB migration**：旧 V2 MR 自动迁移（默认 D₁=A, D₂=D1, D₃=P1）
- **YAML ↔ LiteDB 双向 sync**：`tools/mr_library_sync.py`
- **覆盖率 dashboard**：`docs/mr_library/INDEX.md` 按 5D 维度切片

---

## §6 Per-cell 完整自动化研究链路

```
Step 1: 方程算子 algebraic property 分析（人工 + LLM 协助）
        e.g., A 输运算子是线性、保正、自伴

Step 2: 适用元模式选取（基于 algebraic property）
        e.g., A 输运 → P₁ m_inv + P₃ m_conv + P₄ 退化 + P₅ m_cmp

Step 3: 扫描 SUT 输入参数
        e.g., OpenMC pin-cell.json → fuel.nu_sigma_f, fuel.sigma_a,
              geometry.radius, geometry.boundary, ...

Step 4: 元模式 × 参数 → 构造 meta-prompt
        e.g., m_inv × {geometry.rotation} →
              "Given f = OpenMC k_eff. Is f(rotate(geometry)) == f(geometry)?"

Step 5: LLM 识别 MR 实例（多家 consensus, fan-out 3 providers）
        e.g., DeepSeek + OpenAI + Claude → 候选 MR 列表

Step 6: candidates 入 CandidateRepository, 等程序验证

Step 7: MetBench 执行 MT (source + followup) per MR
        - 高支持度（V2 通过, MR 不被违反）→ 入 MR 库（如 V1 也通过加 stable rating）
        - 低支持度但元模式数学应成立 → 进入分析阶段:
            ├─ 7b-i. MR 错（适用域设错 / tolerance 太严 / parameter 越界）
            │        → 改 MR + 重测
            └─ 7b-ii. 程序错（实际 bug）
                     → 反例归档 `docs/mr_library/counterexamples/<sut>-<mr-id>.md`
                     → 积累
                     → 缺陷修复 issue → upstream PR 或本地 fix log
                     → 论文章节 "MR-based bug detection on open-source codes"
        - 元模式数学性质也不成立 → discard，记录 `docs/mr_library/discarded.md`
```

### 关键洞察

**分支 7b-ii 是论文 contribution 的硬证据**。工作流要**主动追踪**：

- 每个 cell 累计反例数
- 每个反例对应程序 + 版本 + commit hash
- 每个反例的"违反的元模式 + 数学根据 + 程序行为"对照表
- 缺陷修复后的 regression test

每月产 1-2 个反例 → 一年 12-24 反例 → P6 论文实证素材。

---

## §7 三元组 deliverable 切片

**Per cell deliverable**:

```
docs/mr_library/cells/<celcoord>.md         # cell 说明 (方程 + 程序 + 适用元模式 + MR 列表)
docs/mr_library/<mr-id>.yaml                # 每条 MR 的 5D YAML 元数据
docs/mr_library/counterexamples/<id>.md     # 每个反例的归档（如有）
SUT/<sut-name>/<sut>_runner.py              # SUT runner（新接入）
MetBench_SystemMT.Tests/Features/MrLibrary/  # BDD scenarios
docs/uat/reports/baseline-<date>/           # trx baseline 含 MR 测试结果
```

**Stage-level deliverable**:

- `docs/mr_library/INDEX.md`：5D 索引 + cell 覆盖矩阵 + 反例统计
- `docs/experiments/2026-XX-stage8-mr-library/README.md`：实验报告
- 论文 draft：MR 库 + 反例 + 缺陷修复实证

---

## §8 与既有工作的衔接

| 既有 | 衔接方式 |
|---|---|
| Goal 1 meta-prompt engine | **基础工具**，Phase 8.2.5 端到端验证 + Phase 8.3 大规模用 |
| 旧 5-equation plan（PR #68 committed） | **supersede**；旧文件保留 audit trail |
| 4 SUT (OpenMOC / OpenMC / heat / projectile) | Phase 8.2 全部升级 5D 元数据；Phase 8.2.5 跑端到端验证 |
| Cmrlibrary 57 条种子 MR | 选与 5 方程兼容的（如 H-N-01 源齐次 / S-N-01 几何对称 / L-N-01 燃耗 0 退化）优先入库 |
| F11 m_adj 月度监控 | A cell 加 m_adj MR 占位；OpenMOC 上游解锁后激活 |
| BNCT | **搁置**，plan 内保留章节作 Stage 9+ 候 |
| 故障注入 V3 | **独立挂起**，Stage 9 候；Stage 8 MR 库只做 V1+V2 |

---

## §9 决策点（推荐 + 等 user confirm）

| # | 项 | 推荐 |
|---|---|---|
| 1 | **首批 cells scope** | **6 cells**（5 方程 + OpenMOC + OpenMC 双 A 源 = Phase 8.2 现有 4 SUT 升级 + Phase 8.3 新增 4 home-grown） |
| 2 | **MR 库存储** | **LiteDB + YAML mirror**（review-friendly） |
| 3 | **首 PR 切片** | **Phase 8.0 5D schema infra**（单 PR） |
| 4 | **启动时机** | **v2.1 发版后**（Windows UAT round-1 PASS + tag release-v2.1.0 后）|
| 5 | **论文绑定** | **新 P6 论文**（Stage 8 独立成文 — "Meta-pattern driven LLM-based MR identification with bug detection on open-source reactor physics codes"） |

—— 下一步进入 writing-plan，固化 phase × deliverable × 工时。
