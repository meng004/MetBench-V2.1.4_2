# Stage 8 expanded MR library — implementation plan

> **Date**: 2026-05-18（rev：按 user 指令 BNCT 搁置 + V3 独立挂起 + 8.2.5 端到端 + 8.3 完整工作流）
> **Status**: Plan（待 user 决策 §11 后启动）
> **Supersedes**: [`2026-05-18-reactor-physics-five-equations-plan.md`](2026-05-18-reactor-physics-five-equations-plan.md)
> **Upstream brainstorming**: [`2026-05-18-stage8-expanded-mr-library-brainstorming.md`](2026-05-18-stage8-expanded-mr-library-brainstorming.md)
> **Carryover**: Goal 1 meta-prompt 引擎 plan [`2026-05-18-meta-prompt-mr-discovery-plan.md`](2026-05-18-meta-prompt-mr-discovery-plan.md) 不变

---

## §1 总目标 + 验收口径

**Stage 8 ship**（rev：聚焦反应堆物理 5 方程 A/B/C/D/E）:

| 维度 | 验收 |
|---|---|
| **程序** | ≥ 6 个开源 / home-grown 程序接入 MetBench（2 现有 + 4 home-grown 跨 5 方程） |
| **MR 库** | ≥ 15 条 MR 入 LiteDB + YAML mirror，5D 元数据齐全，V1+V2 通过 |
| **测试用例** | ≥ 10 个 BDD scenario 跑通，baseline trx 入仓 |
| **反例归档** | ≥ 2 个反例（违反但元模式数学应成立）+ 至少 1 个 upstream / 本地缺陷修复 |
| **覆盖** | 5 方程 A/B/C/D/E 每个至少 1 cell；D₁/D₂ 程序类型至少各 1 cell |
| **文档** | `docs/mr_library/INDEX.md` + per-cell `.md` + 5D 索引 dashboard + 反例归档 |
| **测试态** | 全套 `dotnet test` 0 fail，cumulative < 120s |

**不在 Stage 8 范围**:

| 项 | 状态 | 计划 |
|---|---|---|
| BNCT cell | 暂缓 | 本 plan §10 保留章节作 Stage 9+ 候，不在 Stage 8 实施 |
| 故障注入 V3 | 暂缓 | 独立模块，Stage 9 单独立项；本 plan §11 列待启动条件 |
| D₃ ML 代理 + D₄ PINN 完整覆盖 | 部分 | Stage 8 仅试点 1 个；完整覆盖 Stage 9+ |
| 商业 / 学术申请程序（MCNP / Serpent / PARCS / RELAP5 / CTF） | 不接 | Stage 9+ 候 |

---

## §2 Phase 8.0: 5D schema + MR 库基础设施（16-24h, 1 PR）

把 Cmrlibrary 5D schema 落地 MetBench，作为后续 cell 存储载体。

### 8.0.1 Domain entity

| 文件 | 改动 |
|---|---|
| `MetBench_Domain/V2/MetamorphicRelationV3.cs` (新) | 5D 字段 + Cmrlibrary §C.5.2 完整 schema |
| `MetBench_Domain/V2/Enums/{Equation,ProgramType,SourceLevel,RelationType,RigorClass}.cs` (新) | 5 个 enum |
| `MetBench_Domain/V2/MetamorphicRelationV3Verification.cs` (新) | V1/V2 结果（V3 暂搁置） |

### 8.0.2 DAL repo + migration

| 文件 | 改动 |
|---|---|
| `MetBench_IDAL/IMetamorphicRelationV3Repository.cs` (新) | 按 5D 维度查询 + filter |
| `MetBench_DAL/V2/LiteDbMetamorphicRelationV3Repository.cs` (新) | LiteDB 实现 + index on D₁/D₂/D₃ |
| `MetBench_DAL/V2/Migrations/V3MetamorphicRelationMigration.cs` (新) | 旧 V2 MR 自动迁移（默认 D₁=A, D₂=D1, D₃=P1，待人工 review） |

### 8.0.3 BLL service

| 文件 | 改动 |
|---|---|
| `MetBench_BLL.Core/SystemMT/MrLibrary/MrLibraryService.cs` (新) | CRUD + 5D 索引 + cell 覆盖率 |
| `MetBench_BLL.Core/SystemMT/MrLibrary/MrYamlSerializer.cs` (新) | YAML ↔ entity |
| `MetBench_BLL.Core/SystemMT/MrLibrary/MrLibrarySyncService.cs` (新) | LiteDB ↔ `docs/mr_library/*.yaml` 双向 sync |
| `MetBench_BLL.Core/SystemMT/MrLibrary/CounterexampleArchive.cs` (新) | 反例归档 + 分类 |

### 8.0.4 工具

| 文件 | 改动 |
|---|---|
| `tools/mr_library_sync.py` (新) | git → LiteDB（启动 / CI 时跑） |
| `tools/mr_library_dashboard.py` (新) | 输出 cell 覆盖 markdown 表 → `docs/mr_library/INDEX.md` |
| `tools/mr_counterexample_summary.py` (新) | 反例统计输出（per cell / per 程序） |

### 8.0.5 测试

`MetBench_SystemMT.Tests/V3MrLibrary/`：

| 测试类 | 覆盖 |
|---|---|
| `MetamorphicRelationV3SchemaTests` | 实体 round-trip + 5D 索引 |
| `MrLibraryServiceTests` | service CRUD + 查询 |
| `MrYamlRoundtripTests` | YAML ↔ entity |
| `V3MigrationTests` | 旧 V2 自动迁移 |
| `CounterexampleArchiveTests` | 反例 CRUD + 分类 |

**Phase 8.0 deliverable**: 5D schema 可用 + YAML/LiteDB 双向 sync + 旧 V2 自动迁移 + 反例归档机制 + 测试 ≥ 15 pass。

**工时**: 20-24h。**1 PR**。

---

## §3 Phase 8.1: meta-prompt engine（carryover, 16h, 1 PR）

详见 [`2026-05-18-meta-prompt-mr-discovery-plan.md`](2026-05-18-meta-prompt-mr-discovery-plan.md)，**无变更**。

加 1 个 alignment task：meta-prompt 输出对接 Phase 8.0 的 `MetamorphicRelationV3` schema（填 5D 字段）。

工时 14h + 2h alignment = **16h**。**1 PR**。

---

## §4 Phase 8.2: 现有 4 SUT 5D 元数据升级（4h, 1 PR）

4 SUT 的现有 MR 升级到 V3 5D 元数据：

| Cell | 现有 MR | 5D 元数据 |
|---|---|---|
| (D₁, A) OpenMOC | ScaleNuSigmaF + ScaleFuelSigmaA | D₁=A, D₂=D1, D₃=P3 m_conv + P2 m_mono, L₂, C1+C2 |
| (D₂, A) OpenMC | 同上 cross-program | D₁=A, D₂=D2, D₃=P5 m_cmp, L₂, C1+C4 |
| (D₁, D) heat_equation | ScaleAmplitude | D₁=D, D₂=D1, D₃=P3 m_conv, L₂, C1 |
| (D₁, ?) projectile | (demo, 不在 5 方程内) | 标 demo，不计入 cell |

填 Cmrlibrary §C.6.1 严谨性 class（A/B/C）+ V1/V2 验证结果（V3 占位）。

**Deliverable**: 3 条 v3 YAML + LiteDB 入库 + 现有 BDD scenario reference v3 MR id。

**工时**: 4h。**1 PR**（或与 Phase 8.3 首 cell 合并）。

---

## §5 Phase 8.2.5: **端到端核心 workflow 验证**（8h, 1 PR）

**首要任务 — user 强调"在现有示例基础上把核心功能跑通"**。

用 Goal 1 meta-prompt engine 对 4 SUT 跑一遍完整研究链路（[brainstorming §6 的 Step 1-7]），验证 3 个分支都至少各跑通 1 例：

### 8.2.5.1 SUT × 元模式扫描

```bash
for sut in openmoc openmc heat_equation projectile; do
  dotnet run --project MetBench_Cli -- mr-identify \
      --sut SUT/$sut/ \
      --metapatterns m_inv,m_mono,m_conv,m_cmp \
      --output docs/mr_library/candidates/$sut-2026-05-XX.yaml
done
```

### 8.2.5.2 LLM-identified MR candidate 入 candidate pool

期望 ≥ 10 个 candidate。多 LLM consensus 投票（已有 W11.2 infra）。

### 8.2.5.3 MetBench 执行 MT per candidate

每个 candidate 用 MetBench 跑 source + followup，记录 V2 结果。

### 8.2.5.4 三分支判定 + 归档（**关键**）

- **高支持度** 分支：≥ 5 MR 入正式库（YAML + LiteDB）
- **反例** 分支：至少**故意构造 1 例** 验证归档机制（如 ScaleNuSigmaF 用 factor=0.5 反向 → 验证 V2 失败 + 自动归档 + 推到 counterexamples/）
- **discard** 分支：至少 1 例（如 m_rev 在稳态 trivial）

### 8.2.5.5 Workflow 报告

`docs/mr_library/workflow-validation-2026-05-XX.md`:
- 跑动统计（candidate 数 / 入库 / 反例 / discard）
- 3 个分支各 ≥ 1 例的端到端 trace（含 prompt / LLM 响应 / V2 结果 / 决策）
- 经验教训 + Phase 8.3 范式调整

**Deliverable**: workflow 验证报告 + ≥5 MR 入库 + ≥1 反例归档 + ≥1 discard 记录。

**工时**: 8h（含 LLM 实跑 ~30 min × 4 SUT + 分析 + 报告 ）。**1 PR**。

---

## §6 Phase 8.3: 5 方程开源程序接入 + 完整工作流（48-64h, 5 sub-PR）

按 brainstorming §3 "minimum viable 6 cells"，每方程 1 cell。8.3.1 已由 Phase 8.2 现有 SUT 覆盖（OpenMOC + OpenMC 都是 A 方程），8.3.2-8.3.5 为新接入。

每 sub-PR 跑 brainstorming §6 完整 workflow：

### 8.3.2 (D₁, C) home-grown Bateman ODE — 1 day (8h)

`SUT/bateman/`:

| 文件 | 内容 |
|---|---|
| `bateman_runner.py` | scipy.integrate ODE solver, 输入 ¹⁰B / ²³⁵U / ²³⁸U / ²³⁹Pu 链 |
| `sample/u235_chain.json` | 简化 U-235 burnup chain |
| `bateman_input_adapter.py` | 入参 transform（dt / N_initial / σ_a） |
| `bateman_output_adapter.py` | 出参 normalize |

**应用工作流 Phase 8.2.5 范式**:
- Step 1: Bateman 算子是线性 ODE → 适用元模式：P₁ 不变性（核素重排）+ P₃ m_conv（dt → 0 收敛）+ P₄ 极限退化（dt = 0 → 初始态）+ P₅ m_dyn（分段 vs 一段一致）
- Step 2-5: meta-prompt × {N_initial, σ_a, dt} → LLM 候选 ≥ 3 MR
- Step 6-7: MetBench 执行 + 三分支归档

**预期入库 MR ≥ 2 条**：`bateman-mass-conservation` (P₁) + `bateman-timestep-cauchy` (P₃)。

### 8.3.3 (D₁, D) home-grown 1D Fourier — 0.5 day (4h)

`SUT/fourier_1d/`：完整 1D 径向热传导 with internal heat source（区别于现有 `heat_equation` demo SUT，加燃料几何 + 内热源）。

**预期入库**：`fourier-mirror-symmetry` (P₁) + `fourier-source-linearity` (P₃)。

### 8.3.4 (D₁, B) home-grown nodal 扩散 — 1-2 day (12-16h)

`SUT/diffusion_nodal/`：1D / 2D 多群扩散 + nodal expansion（简化 PARCS）。

**预期入库**：`diffusion-rotation-invariance` (P₁) + `diffusion-mesh-richardson` (P₃) + `diffusion-D-infinity-degeneration` (P₄)。

### 8.3.5 (D₁, E) home-grown 1D subchannel — 1 day (8h)

`SUT/subchannel_1d/`：1D 单通道质量 / 动量 / 能量守恒 + simplified two-phase。

**预期入库**：`subchannel-flow-temperature-monotone` (P₂) + `subchannel-restart-equivalence` (P₅ m_dyn)。

每 sub-PR 包含：
- SUT runner + adapters + sample
- LauncherOptions + MrLauncher 注册
- ≥ 1 BDD scenario in `MetBench_SystemMT.Tests/Features/MrLibrary/`
- ≥ 1 YAML MR per cell + LiteDB 入库
- per-cell `.md`：`docs/mr_library/cells/<cell-id>.md`
- 至少跑过一次 brainstorming §6 workflow（含反例分支检验）

**工时**: 4 sub-phases × 8-16h ≈ **40-56h**。**4 PR**。

---

## §7 Phase 8.4: D₂/D₃/D₄ 横切（试点）（16-24h, 2 sub-PR）

Stage 8 不做完整 D₃/D₄ 横切覆盖（留 Stage 9）；只做 2 个试点 cell 证明可行：

### 8.4.1 (D₂, C) OpenMC depletion — 1 day (8h)

OpenMC 已装，加 depletion sample + MR `bateman-timestep-cauchy` cross-program (vs home-grown)。

### 8.4.2 (D₃, D) 简化 ML 代理 — 1-2 day (8-16h)

最简代理：用 scikit-learn `GaussianProcessRegressor` 训练 1D Fourier 的 surrogate（不依赖 PyTorch / 公开权重）。MR `fourier-mirror-symmetry` 训练域内等变性验证。

D₄ PINN 留 Stage 9（公开 release 权重待找）。

**工时**: 16-24h。**2 PR**。

---

## §8 Phase 8.5: 反例归档 + 论文 writeup（16-24h, 1 PR）

| 项 | 内容 |
|---|---|
| `docs/mr_library/INDEX.md` | 全 cell 5D 覆盖矩阵 + 链接 |
| `docs/mr_library/dashboard.md` | 覆盖率 + V1/V2 通过率 + 反例数 |
| `docs/mr_library/counterexamples/INDEX.md` | 反例索引（违反 MR + 数学根据 + 程序行为 + 修复 commit） |
| `docs/experiments/2026-XX-stage8-mr-library/README.md` | 实验报告（cells / MRs / tests / 反例 / cumulative wall） |
| **P6 Paper draft** | "Meta-pattern driven LLM-based MR identification with bug detection on open-source reactor physics codes" |

P6 论文 draft 段落:
1. Background: metamorphic testing + LLM + 反应堆物理 SUT
2. Method: 元模式 × 方程 → meta-prompt 自动构造 → LLM 识别 → MT 执行 → 三分支判定
3. Results:
   - MR 库：15+ MR × 5 方程 × 6 cells
   - 反例：≥ 2 真实程序行为偏离元模式预期 → 缺陷分析
   - 缺陷修复：≥ 1 个 commit / upstream PR
4. Threats to validity: tolerance 选取 / LLM 共识依赖 / SUT 简化
5. Future work: Stage 9 (BNCT / V3 故障注入 / 完整 D₃/D₄)

**工时**: 16-24h。**1 PR**。

---

## §9 总工时 + Stage 8 ship 时间表

| Phase | 内容 | 工时 | PR |
|---|---|---|---|
| 8.0 | 5D schema infrastructure | 20-24h | 1 |
| 8.1 | meta-prompt engine | 16h | 1 |
| 8.2 | 现有 4 SUT 5D 升级 | 4h | 1 |
| **8.2.5** | **端到端 workflow 验证** | **8h** | **1** |
| 8.3 | 4 新方程 cells（B/C/D/E） | 40-56h | 4 |
| 8.4 | D₂/D₃ 横切试点 | 16-24h | 2 |
| 8.5 | 反例归档 + 论文 writeup | 16-24h | 1 |
| **合计** | | **120-156h** | **11** |

按每周 active 20h 计 → **6-8 周** ≈ **W14 ~ W22** (2026-05-26 ~ 07-17)

**3 大里程碑**:

- **M1 (W15 end)**: Phase 8.0 + 8.1 + 8.2 + **8.2.5 端到端跑通** → 范式验证完
- **M2 (W19 end)**: Phase 8.3 4 新 cells + 8.4 1 试点 → 5 方程覆盖完整
- **M3 (W22 end)**: Phase 8.5 反例归档 + P6 paper draft → Stage 8 收口

---

## §10 BNCT 暂缓段（保留作 Stage 9+ 候）

**Stage 8 内不实施 BNCT。** 保留章节作未来候。

BNCT 数学物理 80% = Boltzmann 中子输运（同 A 方程），20% = post-process（剂量积分 + RBE + LQ 生物模型，**无新 PDE**）。

可获取性现状（cloud-friendly 仅 1 个）:
| 程序 | 可获取 |
|---|---|
| **OpenMC + Python post-processor** | ✅✅ 唯一可行 |
| TOPAS (Geant4) | ⚠️ 2GB+ 重 |
| NCTPlan / SERA / MultiPlan | ❌ 商业 / 申请 / 停维 |

若未来启动（Stage 9+），路径建议：
1. 在现有 OpenMC SUT 加 BNCT sample case（含 ¹⁰B tally 配置 + 简化剂量后处理 Python）
2. 录入 1-2 条 BNCT-specific MR:
   - m_mono: ¹⁰B 浓度 ↑ → D_α ↑
   - m_inv: 患者解剖镜像 → 剂量场镜像对称
3. 算在 OpenMC cell（D₂, A）内，**不开新方程 cell**
4. 论文中作为"框架可扩展到放射肿瘤"的轻量演示

---

## §11 故障注入 V3 独立模块（Stage 9+ 候）

**Stage 8 内不实施 V3。** Stage 8 MR 库只做 V1（数学可推导）+ V2（程序执行不违反 MR）。

V3（mutation kill rate）作为独立 Stage 9 模块，启动条件:

| 触发条件 | 内容 |
|---|---|
| Stage 8 ship 后 | M3 里程碑完成 → 启动 |
| 复用 mutmut 现有基础设施 | `tools/mutation_study.py` + `tools/mutations.py` 28 个 hand-built mutation 已有 |
| 独立 P-series 论文驱动 | 对应 P2 IST SMS 度量论文的实证基础 |

V3 工作流（待 Stage 9 详细 plan）:
- 每条 MR 注入 5 类语义变异（CE / OS / HP / TF / SI）
- 测 mutation kill rate ≥ 阈值（默认 0.5）
- 通过 V3 的 MR 标 "rigorous"，未通过的标 "fragile"

---

## §12 衔接 P-series（rev）

| Cmrlibrary 章节 | Stage 8 对接物 |
|---|---|
| §C.1 五维索引 | Phase 8.0 schema |
| §C.6 V1/V2 验证 | Phase 8.3 每 cell 录入时填 |
| §C.6.1 A/B/C 严谨性 | Phase 8.2 现有升级时填 |
| §C.7 候选第九块发现 | Stage 9 候（Stage 8 末段标 orphan MR） |
| §C.13 测试用例 5 维 | Phase 8.3 每 BDD scenario 5 维元数据 |
| §C.14 57 条种子 MR | Phase 8.2.5 + 8.3 优先入库（与 5 方程兼容的） |

**论文绑定（推荐）**:

- **P6（新）**：Stage 8 本身写成论文："元模式驱动 LLM-based MR 自动识别 + 5 方程反应堆物理 MR 库 + 反例缺陷检测"
- P1 经验审计（继续）：Stage 8 反例数据喂 P1
- P2 IST SMS：Stage 9 V3 启动后喂

---

## §13 决策点（推荐 + 等 user 拍板）

| # | 项 | 推荐 |
|---|---|---|
| 1 | **首批 cells scope** | **6 cells**（5 方程 A/B/C/D/E + OpenMOC + OpenMC 双 A 源） |
| 2 | **MR 库存储** | **LiteDB + YAML mirror** |
| 3 | **首 PR 切片** | **Phase 8.0 5D schema infra**（单 PR） |
| 4 | **启动时机** | **v2.1 发版后** |
| 5 | **论文绑定** | **新 P6 论文** |
| 6 | **D₃/D₄ 横切深度** | **2 试点**（D₂ depletion + D₃ GP 代理）；D₄ PINN 留 Stage 9 |

—— 决策完后启动 Phase 8.0。

---

## §14 不在本 plan 范围内（明确排除）

- BNCT cell（**Stage 8 不实施**，§10 保留章节）
- V3 故障注入（**独立模块挂起**，§11 列启动条件）
- 商业 / 学术申请程序（MCNP / Serpent / PARCS / RELAP5 / CTF / NCTPlan）— Stage 9 候
- BISON 完整 multiphysics — Stage 9 候
- OpenFOAM 完整接入 — Stage 9 候
- D₄ PINN 完整覆盖（仅 D₃ GP 代理 1 试点）
- 全 Cmrlibrary 57 条种子 MR 入库 — 只入与 5 方程 + 6 cells 兼容子集

—— 文档结束。等 §13 决策 → 启动 Phase 8.0。
