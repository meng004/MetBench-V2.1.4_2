# Stage 8 expanded MR library — implementation plan

> **Date**: 2026-05-18
> **Status**: Plan（待 user 决策 §11 项后启动）
> **Supersedes**: [`2026-05-18-reactor-physics-five-equations-plan.md`](2026-05-18-reactor-physics-five-equations-plan.md)
> **Upstream brainstorming**: [`2026-05-18-stage8-expanded-mr-library-brainstorming.md`](2026-05-18-stage8-expanded-mr-library-brainstorming.md)
> **Carryover**: Goal 1 meta-prompt 引擎 plan [`2026-05-18-meta-prompt-mr-discovery-plan.md`](2026-05-18-meta-prompt-mr-discovery-plan.md) 不变

---

## §1 总目标 + 验收口径

**Stage 8 ship**:

| 维度 | 验收 |
|---|---|
| **程序** | ≥ 5 个开源 / 公开程序接入 MetBench（含 2 现有 + 3 新增） |
| **MR 库** | ≥ 15 条 MR 入 LiteDB + YAML mirror，5D 元数据齐全，V1+V2 通过 |
| **测试用例** | ≥ 10 个 BDD scenario 跑通，baseline trx 入仓 |
| **覆盖** | 4 程序类型至少各 1 cell；5 专业域至少各 1 cell（含 BNCT） |
| **文档** | `docs/mr_library/INDEX.md` + per-cell `.md` + 5D 索引 dashboard |
| **测试态** | 全套 `dotnet test` 0 fail，cumulative < 120s（含新 cells） |

---

## §2 Phase 0: 5D schema + MR 库基础设施（2-3 day, blocker）

**Phase 8.0**: 把 Cmrlibrary.md 5D schema 在 MetBench 里落地，作为后续 cell 的存储载体。

### 8.0.1 Domain entity 扩展

| 文件 | 改动 |
|---|---|
| `MetBench_Domain/V2/MetamorphicRelationV3.cs` (新) | 5D 字段 + Cmrlibrary §C.5.2 完整 schema |
| `MetBench_Domain/V2/Enums/Equation.cs` (新) | A/B/C/D/E/F/G/H 8 编码 |
| `MetBench_Domain/V2/Enums/ProgramType.cs` (新) | D1/D2/D3/D4 |
| `MetBench_Domain/V2/Enums/SourceLevel.cs` (新) | L1-L5 |
| `MetBench_Domain/V2/Enums/RelationType.cs` (新) | equation / inequality / monotone / convergence |
| `MetBench_Domain/V2/MetamorphicRelationV3Verification.cs` (新) | V1/V2/V3 结果 |

### 8.0.2 DAL repo + migration

| 文件 | 改动 |
|---|---|
| `MetBench_IDAL/IMetamorphicRelationV3Repository.cs` (新) | 按 5D 维度查询 + filter |
| `MetBench_DAL/V2/LiteDbMetamorphicRelationV3Repository.cs` (新) | LiteDB 实现 + index on D₁/D₂/D₃ |
| `MetBench_DAL/V2/Migrations/V3MetamorphicRelationMigration.cs` (新) | 旧 V2 MR 自动迁移（默认 D₁=A, D₂=D1, D₃=P1，待人工 review） |

### 8.0.3 BLL service

| 文件 | 改动 |
|---|---|
| `MetBench_BLL.Core/SystemMT/MrLibrary/MrLibraryService.cs` (新) | CRUD + 5D 索引查询 + cell 覆盖率统计 |
| `MetBench_BLL.Core/SystemMT/MrLibrary/MrYamlSerializer.cs` (新) | YAML ↔ entity 双向 |
| `MetBench_BLL.Core/SystemMT/MrLibrary/MrLibrarySyncService.cs` (新) | LiteDB ↔ `docs/mr_library/*.yaml` 双向 sync |

### 8.0.4 工具

| 文件 | 改动 |
|---|---|
| `tools/mr_library_sync.py` (新) | git → LiteDB（启动时 / CI 时跑） |
| `tools/mr_library_dashboard.py` (新) | 输出 cell 覆盖 markdown 表 → `docs/mr_library/INDEX.md` |

### 8.0.5 测试

| 文件 | 测试目标 |
|---|---|
| `MetBench_SystemMT.Tests/V3MrLibrary/MetamorphicRelationV3SchemaTests.cs` | 实体 round-trip + 5D 索引 |
| `MetBench_SystemMT.Tests/V3MrLibrary/MrLibraryServiceTests.cs` | service CRUD + 查询 |
| `MetBench_SystemMT.Tests/V3MrLibrary/MrYamlRoundtripTests.cs` | YAML ↔ entity |
| `MetBench_SystemMT.Tests/V3MrLibrary/V3MigrationTests.cs` | 旧 V2 自动迁移 |

**Phase 0 deliverable**: 5D schema 可用 + YAML/LiteDB 双向 sync + 旧 V2 数据自动迁移 + 测试 ≥ 12 pass。

**工时**: 16-24h（2-3 全 session）。**1 PR**。

---

## §3 Phase 1: meta-prompt engine（carryover from prior Goal 1, 14h）

详见 [`2026-05-18-meta-prompt-mr-discovery-plan.md`](2026-05-18-meta-prompt-mr-discovery-plan.md)，**无变更**。

加 1 个 alignment task：meta-prompt 输出对接到 Phase 0 的 `MetamorphicRelationV3` schema（旧版输出是简单 candidate 行；新版要填 5D 字段）。

工时 14h + 2h alignment = **16h**。**1 PR**（或拆 carryover PR + alignment PR）。

---

## §4 Phase 2: 现有 SUT 5D 元数据升级（4h）

OpenMOC + OpenMC + heat_equation 三个现有 SUT 的 MR 升级到 5D。

| Cell | 现有 MR | 5D 元数据补全 |
|---|---|---|
| (D₁, A) OpenMOC | `ScaleNuSigmaF` + `ScaleFuelSigmaA` | D₁=A, D₂=D1, D₃=P3 m_conv (线性) + P2 m_mono, L₂ physics law, C1+C2 |
| (D₂, A) OpenMC | 同上 cross-program | D₁=A, D₂=D2, D₃=P5 m_cmp, L₂, C1+C4 (MC 统计) |
| (D₁, D) heat_equation | `ScaleAmplitude` | D₁=D, D₂=D1, D₃=P3 m_conv (线性), L₂, C1 |

也填 Cmrlibrary §C.6.1 严谨性 class（A/B/C）+ V1/V2/V3 验证结果（V3 = mutmut 数据复用，PR #59 baseline 已有部分）。

**Deliverable**: 5 条 v3 schema YAML + LiteDB 入库 + 现有 BDD scenario reference v3 MR id。

**工时**: 4h。**1 PR**（与 Phase 0 同 batch 或单独）。

---

## §5 Phase 3: 推导矩阵填充 + meta-prompt 自动跑（4h）

跑 Phase 1 的 meta-prompt engine over Phase 2 的 5 SUT，自动产候选 MR。LLM 候选 + 8 元模式 ⊕ 8 方程 = candidate 池。

人工 review + 选 ≥ 10 条入库作 Phase 4 cells 的 seed。

**Deliverable**: `docs/mr_library/candidates-2026-05-XX.md` 记录候选 + 人工 review 结果。

**工时**: 4h（其中 1h 人工 review）。**1 PR**。

---

## §6 Phase 4: 新 cells 接入（W14-W18，分 5 个 sub-PR）

按 §brainstorming §6 优先级，每个 sub-phase 1 cell:

### 8.4.1 (D₁, C) home-grown Bateman ODE — 1 day

| 文件 | 内容 |
|---|---|
| `SUT/bateman/bateman_runner.py` | scipy.integrate ODE solver |
| `SUT/bateman/sample/u235_chain.json` | 简化 U-235 chain |
| `SUT/bateman/bateman_input_adapter.py` | 入参 transform |
| `SUT/bateman/bateman_output_adapter.py` | 出参 normalize |
| `MetBench_BLL.Core/SystemMT/Launcher/SystemMtMrLauncher.cs` | 加 `bateman-decay-conv` MR |
| `MetBench_SystemMT.Tests/SystemMT/BatemanRunnerSmokeTests.cs` | smoke test |
| `MetBench_SystemMT.Tests/Features/BatemanDecayConv.feature` | BDD scenario |
| `docs/mr_library/C01-bateman-decay-cauchy.yaml` | 5D YAML |
| `docs/mr_library/cells/D1_C_bateman.md` | cell 说明 |

**MR 选定**: `m_conv` — 时间步 → 0 时核素数序列 Cauchy 收敛。

### 8.4.2 (D₁, D) home-grown 1D Fourier — half day

`SUT/fourier_1d/` + MR `m_inv` 镜像对称（区别于现 `heat_equation`，是 1D 径向 with internal heat source）。

### 8.4.3 (D₁, B) home-grown nodal 扩散 — 1-2 day

`SUT/diffusion_nodal/` + MR `m_inv` 几何旋转 / `m_conv` 网格细化 Richardson。

### 8.4.4 (D₁, E) home-grown 1D subchannel — 1 day

`SUT/subchannel_1d/` + MR `m_mono` 流量↓ → 包壳温↑ / `m_inv` 镜像。

### 8.4.5 (D₁, H) **BNCT simplified** — 2 day（含上游 OpenMC 中子通量耦合）

`SUT/bnct_simple/` 复用 OpenMC 中子通量 + 加 ¹⁰B 浓度分布 + 剂量积分。MR `m_mono` ¹⁰B↑→D_α↑ + `m_inv` 患者解剖镜像对称。

**每 sub-phase deliverable**: 1 SUT + ≥1 BDD scenario + ≥1 v3 YAML MR + cell `.md` + smoke test pass。

**工时**: 5 sub-phases × 1-2 day = **6-8 day** ≈ 48-64h。**5 PR**（每 sub-phase 一个）。

---

## §7 Phase 5: 程序类型 D₂ D₃ D₄ 横切（W19-W20，3 sub-PR）

### 8.5.1 D₂ MC 横切 — (D₂, C) OpenMC depletion — 1 day

OpenMC 已装，加 depletion sample 跑通 + MR `m_conv` 时间步收敛。

### 8.5.2 D₃ ML 代理 — 1 公开 DeepONet release — 2-3 day

下载论文公开权重（待找），写 wrapper runner + MR `m_inv` 训练域内等变性。

### 8.5.3 D₄ PINN — 1 公开 R²-PINN release — 2-3 day

同上 PINN release。MR `m_conv` 训练损失收敛 + `m_inv` 物理对称。

**工时**: 5-7 day = **40-56h**。**3 PR**。

---

## §8 Phase 6: 5D 索引 dashboard + 论文 writeup（W20-W21，2-3 day）

| 项 | 内容 |
|---|---|
| `docs/mr_library/INDEX.md` | 全 cell 5D 覆盖矩阵 + 链接 |
| `docs/mr_library/dashboard.md` | 覆盖率 + 每 cell verified 状态 + V1/V2/V3 通过率 |
| `docs/experiments/2026-XX-stage8-mr-library/README.md` | 实验报告：cells N / MR M / tests K / cumulative wall |
| Paper appendix draft | P-series P2 IST 或 P1 经验审计的实证段落 draft |

**工时**: 16-24h。**1 PR**。

---

## §9 总工时 + Stage 8 ship 时间表

| Phase | 内容 | 工时 | PR |
|---|---|---|---|
| 8.0 | 5D schema infrastructure | 16-24h | 1 |
| 8.1 | meta-prompt engine (carryover) | 16h | 1 |
| 8.2 | 现有 SUT 5D 升级 | 4h | 1 |
| 8.3 | 推导矩阵 + meta-prompt 自动 | 4h | 1 |
| 8.4 | 5 新 cell 接入 | 48-64h | 5 |
| 8.5 | D₂/D₃/D₄ 横切 | 40-56h | 3 |
| 8.6 | dashboard + writeup | 16-24h | 1 |
| **合计** | | **144-192h** | **13** |

按每周 active 20h 计 → **7-10 周** ≈ **W14 ~ W23** (2026-05-26 ~ 07-31)

**3 大里程碑**:

- **M1 (W15)**: 5D schema + meta-prompt engine + 现有升级 ship → MR 库基础设施齐
- **M2 (W18)**: 5 新 cells 全 ship → 5 专业域覆盖完整（含 BNCT）
- **M3 (W21)**: D₂/D₃/D₄ 横切 + 论文 writeup → Stage 8 收口，喂 P-series

---

## §10 衔接 P-series

| Cmrlibrary.md 章节 | Stage 8 对接物 |
|---|---|
| §C.1 五维索引 | Phase 0 schema |
| §C.6 V1/V2/V3 验证 | Phase 4 每 cell 录入时填 |
| §C.6.1 A/B/C 严谨性 | Phase 2 现有升级时填 |
| §C.7 候选第九块发现 | Stage 9 候选（Stage 8 末段标 orphan MR） |
| §C.13 测试用例 5 维 | Phase 4 每 BDD scenario 5 维元数据 |
| §C.14 57 条种子 MR | Phase 3 优先入库（与现有 SUT 兼容的） |

**论文绑定** (待 §11 决策)：

- **P1 经验审计**：Stage 8 MR 库提供"经验确认的 SCP 域覆盖"数据
- **P2 IST SMS 度量**：Stage 8 12 网格选定（Cmrlibrary §C.2）的 metrics 实证
- **P6 (新)**：Stage 8 本身写成论文："元模式驱动 LLM-based MR 自动识别 + 5D MR 库 + 多程序类型 / 多专业域矩阵"

---

## §11 决策点（待 user 拍板，启动前 confirm）

| # | 项 | 选项 |
|---|---|---|
| 1 | **首批 cells scope** | (a) Phase 4 5 cells（最小可行）<br>(b) Phase 4 + Phase 5 = 8 cells（推荐）<br>(c) 全 30 cells 矩阵（多月） |
| 2 | **MR 库存储** | (a) LiteDB only<br>**(b) LiteDB + YAML mirror 入仓**（review-friendly，推荐）<br>(c) YAML only |
| 3 | **V3 故障注入范围** | (a) 仅 V1+V2 in Stage 8<br>(b) Stage 8 末段 V3 (复用 mutmut)<br>**(c) Stage 9 单独立项** |
| 4 | **BNCT 优先级** | (a) 跟 Phase 4 其它 cell 并列<br>**(b) Phase 4 末段压力测试**<br>(c) 单独 Goal 3 |
| 5 | **论文绑定** | (a) P1 / (b) P2 / **(c) 新 P6**（推荐：Stage 8 独立成文）/ (d) 不绑定 |
| 6 | **启动时机** | (a) **v2.1 发版后**（Windows UAT round-1 PASS + tag release-v2.1.0 后）<br>(b) 立即并行（v2.1 / Stage 8 不冲突） |
| 7 | **首 PR 切片** | (a) **Phase 8.0 schema infra**（单 PR，base 设施）<br>(b) Phase 8.0 + 8.1 合 PR（耦合）<br>(c) 先做 Phase 8.4.1 Bateman demo 提早验证 |

—— 决策完后启动 Phase 8.0。

---

## §12 不在本 plan 范围内（明确排除）

- 商业程序接入（MCNP / Serpent commercial / SCALE 商业 license）— 留 future work
- BISON 完整 multiphysics（重量级） — simplified Fourier 替代
- OpenFOAM 完整接入（编译 1GB+） — 1D subchannel 替代
- TOPAS（Geant4 wrapper, 2GB+） — BNCT simplified 替代
- 全 Cmrlibrary 57 条种子 MR 入库 — 只入与现有 SUT 兼容的子集
- V3 故障注入完整覆盖 — Stage 9 候

—— 文档结束。等 §11 决策 → 启动 Phase 8.0。
