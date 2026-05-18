# Stage 8 expanded MR library — implementation plan

> **Date**: 2026-05-18（rev：5 MP + 17 cells + BDD .feature 存储 + 无 paper writeup）
> **术语规范**: [`docs/GLOSSARY.md`](../../GLOSSARY.md)
> **Brainstorming**: [`2026-05-18-stage8-expanded-mr-library-brainstorming.md`](2026-05-18-stage8-expanded-mr-library-brainstorming.md)
> **Carryover**: Goal 1 meta-prompt 引擎 plan [`2026-05-18-meta-prompt-mr-discovery-plan.md`](2026-05-18-meta-prompt-mr-discovery-plan.md)

---

## §1 总目标 + 验收口径

**Stage 8 ship**（rev：cells 尽量填满，不空白；论文待真发现 bug 再考虑）:

| 维度 | 验收 |
|---|---|
| **cells 覆盖** | 17 实际可填 cells 中 ≥ 12 cells 不空白（每 cell ≥1 SUT + ≥1 MR + ≥1 BDD Scenario）|
| **程序** | OpenMOC + OpenMC + 4 home-grown (nodal / Bateman / 1D Fourier / 1D subchannel) = **6 程序** |
| **MR 库** | ≥ 15 条 MR 入库（5D tag 齐全 + V1+V2 通过）|
| **反例归档** | 出现就归档（不刻意造），无强制数量 |
| **测试态** | 全套 `dotnet test` 0 fail，cumulative < 120s |

**不在 Stage 8 范围**:

| 项 | 状态 | 计划 |
|---|---|---|
| BNCT | 暂缓 | Stage 9+ 候（保留 §10 章节作未来候） |
| 故障注入 V3 | 独立模块挂起 | Stage 9+ 候（§11） |
| 论文 writeup | 暂不绑定 | 反例积累后回头考虑 |
| 商业 / 学术申请程序（MCNP / Serpent / PARCS / RELAP5 / CTF / SCALE） | 不接 | Stage 9+ |
| D₃/D₄ 完整覆盖 | 部分 | Stage 8 试点；完整 Stage 9+ |

---

## §2 Phase 8.0: 5D tag schema + LiteDB sync 扩展（12-16h, 1 PR）

**沿用现有约定**：BDD `.feature` 是 canonical（含 5D Gherkin tags），LiteDB 是运行时索引（`tools/feature_to_db.py` 自动 sync）。不引 YAML mirror。

### 8.0.1 Domain entity 扩展

| 文件 | 改动 |
|---|---|
| `MetBench_Domain/V2/MetamorphicRelationV3.cs` (新) | 5D 字段：Equation / ProgramType / MetaPattern / SourceLevel / FailureCorrelation + RigorClass(A/B/C) + RelationType + Tolerance |
| `MetBench_Domain/V2/Enums/{Equation,ProgramType,MetaPattern,SourceLevel,FailureCorrelation,RelationType,RigorClass}.cs` (新) | 7 个 enum |

### 8.0.2 DAL repo + migration

| 文件 | 改动 |
|---|---|
| `MetBench_IDAL/IMetamorphicRelationV3Repository.cs` (新) | 按 5D 维度查询 |
| `MetBench_DAL/V2/LiteDbMetamorphicRelationV3Repository.cs` (新) | LiteDB 实现 + index on Equation / ProgramType / MetaPattern |
| `MetBench_DAL/V2/Migrations/V3MetamorphicRelationMigration.cs` (新) | 旧 V2 MR 自动迁移（默认 Equation=boltzmann, ProgramType=Num, MetaPattern=MP_inv，待人工 review）|

### 8.0.3 BDD tag 解析 + sync 工具

| 文件 | 改动 |
|---|---|
| `tools/feature_to_db.py` (扩展) | 解析 `@MR.Equation=boltzmann @MR.ProgramType=Num @MR.MetaPattern=MP_inv` 等 tag → 填 `MetamorphicRelationV3` 字段；BDD .feature 是 canonical 单源 |
| `tools/db_to_feature.py` (扩展) | 反向：从 DB 生成 .feature 模板（含 5D tags），辅助新 MR 录入 |
| `tools/mr_library_dashboard.py` (新) | 输出 cells × MP 覆盖矩阵 → `docs/mr_library/INDEX.md` |
| `tools/mr_counterexample_summary.py` (新) | 反例归档 + 统计 |

### 8.0.4 测试

`MetBench_SystemMT.Tests/V3MrLibrary/`:

| 测试类 | 覆盖 |
|---|---|
| `MetamorphicRelationV3SchemaTests` | 实体 round-trip + 5D 索引 |
| `V3MigrationTests` | 旧 V2 自动迁移 |
| `FeatureTagParserTests` | `@MR.*` tag 解析 |
| `LiteDbMetamorphicRelationV3RepositoryTests` | repo CRUD + 5D filter |

**Phase 8.0 deliverable**：5D tag schema 可用 + BDD ↔ LiteDB 双向 sync + 旧 V2 自动迁移 + 测试 ≥ 12 pass。

**工时**：12-16h。**1 PR**。

---

## §3 Phase 8.1: meta-prompt engine（carryover, 16h, 1 PR）

详见 [`2026-05-18-meta-prompt-mr-discovery-plan.md`](2026-05-18-meta-prompt-mr-discovery-plan.md)。alignment task：meta-prompt 输出对接 `MetamorphicRelationV3` schema（填 5D 字段，输出 .feature 模板而非 YAML）。

工时 14h + 2h alignment = **16h**。**1 PR**。

---

## §4 Phase 8.2: 现有 4 SUT 5D tag 升级（4h, 1 PR）

| Cell | 现有 .feature | 加 5D tag |
|---|---|---|
| (boltzmann, Num) OpenMOC | OpenMocPinCellNuSigmaF / OpenMocPinCellSigmaA | `@MR.Equation=boltzmann @MR.ProgramType=Num @MR.MetaPattern=MP_conv` |
| (boltzmann, MC) OpenMC + cross-program | CrossProgramNeutronTransportMrs | `@MR.Equation=boltzmann @MR.ProgramType=MC @MR.MetaPattern=MP_part`（OpenMOC↔OpenMC 跨实现）|
| (fourier, Num) heat_equation | HeatEquationAmplitude | `@MR.Equation=fourier @MR.ProgramType=Num @MR.MetaPattern=MP_conv` |
| (—) projectile | ProjectileRange | 标 `@MR.Demo=true`，不计入正式 cell |

**工时**：4h。**1 PR**。

---

## §5 Phase 8.2.5: 端到端核心 workflow 验证（8h, 1 PR）

**首要 — user 强调"现有示例上把核心功能跑通"。**

跑现有 4 SUT 一遍完整研究链路 (brainstorming §6)：

### 8.2.5.1 SUT × 元模式扫描

```bash
for sut in openmoc openmc heat_equation projectile; do
  dotnet run --project MetBench_Cli -- mr-identify \
      --sut SUT/$sut/ \
      --metapatterns MP_inv,MP_mono,MP_conv,MP_traj,MP_part \
      --output candidates/$sut-2026-05-XX.feature.draft
done
```

### 8.2.5.2 LLM-identified candidate 入 candidate pool

期望 ≥ 10 candidate（多 LLM consensus, 复用 W11.2 infra）。

### 8.2.5.3 MetBench 执行 MT per candidate

每个 candidate 跑 src + flw，记录 V2 结果。

### 8.2.5.4 三分支判定 + 归档

- **高支持度** ≥ 5 MR 入正式库（.feature + LiteDB）
- **反例**（强制 ≥ 1 例验证归档机制）：故意构造 factor=0.5 反向，触发 V2 fail，验证 `MetBench_SystemMT.Tests/Features/Counterexamples/` 归档机制工作
- **discard**（≥ 1 例）：m_rev 时间反演稳态 trivial 等

### 8.2.5.5 workflow 验证报告

`docs/mr_library/workflow-validation-2026-05-XX.md`：跑动统计 + 3 分支端到端 trace。

**Phase 8.2.5 deliverable**：≥ 5 MR 入库 + ≥ 1 反例归档 + ≥ 1 discard 记录 + workflow 报告。

**工时**：8h。**1 PR**。

---

## §6 Phase 8.3: 新 cells 接入 + 完整工作流（32-48h, 4 sub-PR）

按 brainstorming §3 minimum viable，4 个 home-grown 新 SUT：

### 8.3.1 (bateman, Num) home-grown Bateman ODE — 1 day (8h)

`SUT/bateman/`:
- `bateman_runner.py` (scipy.integrate)
- `sample/u235_chain.json` (简化 U-235 chain)
- input/output adapter

应用 §brainstorming §6 完整 workflow:
- 5 MP × bateman → MP_inv (mass conservation) + MP_mono (burnup→k_inf↓) + MP_conv (dt→0 Cauchy) + MP_traj (50d+50d 分段 vs 100d)
- meta-prompt → LLM 候选 ≥ 3 MR
- MetBench 执行 + 三分支

**预期入库 ≥ 2 MR**：`bateman-mass-conservation` (MP_inv) + `bateman-timestep-cauchy` (MP_conv) + `bateman-segment-equivalence` (MP_traj)。

### 8.3.2 (fourier, Num) home-grown 1D Fourier — 0.5 day (4h)

`SUT/fourier_1d/`：1D 径向 + 内热源（升级现有 heat_equation 简版）。

**预期入库 ≥ 2 MR**：`fourier-mirror-symmetry` (MP_inv) + `fourier-grid-richardson` (MP_conv)。

### 8.3.3 (diffusion, Num) home-grown nodal — 1-2 day (12-16h)

`SUT/diffusion_nodal/`：1D/2D 多群扩散 + nodal expansion（PARCS 简化版）。

**预期入库 ≥ 3 MR**：`diffusion-rotation-invariance` (MP_inv) + `diffusion-mesh-richardson` (MP_conv) + `diffusion-D-infinity-degeneration` (MP_conv) + `diffusion-transport-comparison` (MP_part, vs OpenMOC 跨实现)。

### 8.3.4 (NS, Num) home-grown 1D subchannel — 1 day (8h)

`SUT/subchannel_1d/`：1D 单通道质量/动量/能量守恒。

**预期入库 ≥ 2 MR**：`subchannel-flow-temperature-monotone` (MP_mono) + `subchannel-restart-equivalence` (MP_traj)。

每 sub-PR：SUT runner + adapters + sample + ≥ 1 BDD scenario（含 5D tags）+ ≥ 1 MR 入库。

**工时**：32-44h。**4 PR**。

---

## §7 Phase 8.4: D₃/D₄ 程序类型横切试点（16-24h, 2 sub-PR）

Stage 8 不做完整覆盖，只 2 个试点证可行：

### 8.4.1 (fourier, Surr) home-grown GP 代理 — 1 day (8h)

scikit-learn `GaussianProcessRegressor` 训练 1D Fourier surrogate（不依赖 PyTorch / 论文权重）。

MR：`fourier-mirror-symmetry` 训练域内等变性 + `fourier-cross-implementation` (Surr vs Num 跨实现，MP_part)。

### 8.4.2 (boltzmann, MC) OpenMC depletion — 1 day (8h)

OpenMC 已装，加 depletion sample + MR `bateman-timestep-cauchy` 跨 SUT（OpenMC depletion vs home-grown Bateman ODE, MP_part）。

D₄ PINN 留 Stage 9（公开 release 权重待找）。

**工时**：16h。**2 PR**。

---

## §8 Phase 8.5: cells 覆盖 dashboard + 反例归档（8h, 1 PR）

| 项 | 内容 |
|---|---|
| `docs/mr_library/INDEX.md` | 17 cells × 5 MP 覆盖矩阵 + 链接每 .feature |
| `docs/mr_library/dashboard.md` | 覆盖率 + V1/V2 通过率 + 反例数（无 paper 段） |
| `docs/mr_library/counterexamples/INDEX.md` | 反例索引（如有）|
| `docs/experiments/2026-XX-stage8-mr-library/README.md` | 实验报告：cells / MRs / tests / 反例 / cumulative wall |

**无 paper writeup**（user 指令：先做实验，发现 bug 再考虑）。

**工时**：8h。**1 PR**。

---

## §9 总工时 + Stage 8 ship 时间表

| Phase | 内容 | 工时 | PR |
|---|---|---|---|
| 8.0 | 5D tag schema + BDD↔LiteDB sync | 12-16h | 1 |
| 8.1 | meta-prompt engine | 16h | 1 |
| 8.2 | 现有 4 SUT 5D tag 升级 | 4h | 1 |
| 8.2.5 | 端到端 workflow 验证 | 8h | 1 |
| 8.3 | 4 home-grown cells | 32-44h | 4 |
| 8.4 | D₃/D₄ 横切试点 (Surr + MC depletion) | 16h | 2 |
| 8.5 | dashboard + 反例归档 | 8h | 1 |
| **合计** | | **96-112h** | **11** |

按每周 active 20h 计 → **5-6 周** ≈ **W14-W19** (2026-05-26 ~ 07-03)

**3 大里程碑**：
- **M1 (W15 end)**：Phase 8.0/8.1/8.2/8.2.5 → 范式 + 现有升级完成
- **M2 (W18 end)**：Phase 8.3 4 新 cells + 8.4 2 横切 → 12+ cells 不空白
- **M3 (W19 end)**：Phase 8.5 dashboard 收口 → Stage 8 ship

---

## §10 BNCT 暂缓（Stage 9+ 候，保留章节）

**Stage 8 内不实施 BNCT**。理由：

| 项 | 说明 |
|---|---|
| 数学物理 | 80% 是 boltzmann 输运（同 boltzmann cell），20% 是 post-process（剂量积分 + RBE + LQ），无新 PDE |
| 程序获取 | NCTPlan / SERA / MultiPlan 商业 / 申请 / 停维；TOPAS Geant4 重依赖 ~2GB；cloud-friendly 仅 OpenMC + Python post-processor |
| MR 新增有限 | BNCT-specific MR 只有 1-2 条（`m_mono`: ¹⁰B↑→D_α↑ + `m_inv`: 患者镜像对称），不足以撑独立 cell |

未来启动路径（Stage 9+）：
1. 现有 OpenMC SUT 加 BNCT sample case + 简化剂量后处理
2. 录入 1-2 条 BNCT-specific MR
3. 算在 (boltzmann, MC) cell 内，不开新方程 cell

---

## §11 故障注入 V3 独立模块（Stage 9+ 候）

**Stage 8 内不实施 V3**。Stage 8 MR 库只做 V1（数学可推导）+ V2（程序执行不违反 MR）。

V3 启动条件（待 Stage 9 详细 plan）：

| 触发 | 内容 |
|---|---|
| Stage 8 M3 完成后 | 启动 V3 设计 |
| 复用 mutmut 现有基础设施 | `tools/mutation_study.py` + `tools/mutations.py` 28 mutation 已有 |
| 每条 MR 注入 5 类语义变异 (CE / OS / HP / TF / SI) | 测 mutation kill rate ≥ 0.5 |
| 通过 V3 的 MR 标 "rigorous"，未通过的标 "fragile" | 入 MR 库元数据 |

---

## §12 衔接 P-series

| Cmrlibrary 章节 | Stage 8 对接物 |
|---|---|
| §C.1 五维索引 | Phase 8.0 5D tag schema |
| §C.6 V1/V2 验证 | Phase 8.3 每 cell 录入填 |
| §C.6.1 A/B/C 严谨性 | Phase 8.2 现有升级填 |
| §C.13 测试用例 5 维 | Phase 8.3 每 BDD .feature 5D tag |
| §C.14 57 种子 MR | Phase 8.2.5 + 8.3 优先入库（按 §brainstorming §5 cell 分类）|
| PWR §2.3-2.5 27 条 PWR MR | 同上 |

**论文绑定**：**暂不绑定**（user 指令：先做实验，发现 bug 再考虑）。

---

## §13 决策点（推荐 + 等 user 拍板）

| # | 项 | 推荐 |
|---|---|---|
| 1 | 首批 cells | **12 cells**（D₁ × 5 + D₂ × 2 + D₃ × 5 = Phase 8.3 + 8.4.1）|
| 2 | MR 存储 | **BDD .feature + Gherkin tags + LiteDB sync** |
| 3 | 首 PR 切片 | **Phase 8.0** 5D tag schema 扩展 |
| 4 | 启动时机 | **v2.1 发版后** |
| 5 | 论文 | **暂不绑定** |

—— 决策完后启动 Phase 8.0。

---

## §14 不在本 plan 范围（明确排除）

- BNCT cell（Stage 8 不实施，§10 保留）
- V3 故障注入（独立挂起，§11）
- 论文 writeup
- 商业 / 学术申请程序（MCNP / Serpent / PARCS / RELAP5 / CTF / SCALE / NCTPlan）— Stage 9+
- D₄ PINN 完整覆盖（仅 Stage 8 内 0 试点，全留 Stage 9）
- 全 Cmrlibrary 57 + PWR 27 = 84 MR 完整入库 — 只入 Stage 8 17 cells 覆盖子集

—— 文档结束。等 §13 决策 → 启动 Phase 8.0。
