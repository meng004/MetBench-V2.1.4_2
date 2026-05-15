# Cold-Start Demo Findings — 2026-05-15

> Goal: 选 5 个 anchor 案例 + 把 OpenMOC/OpenMC 当新项目走一遍 MetBench v2 核心功能，
> 同时逐步发现 bug、修复、记录。

## Anchor 案例（5 个，覆盖 4 NOETHER MetaPattern + R-Case 复现）

| ID | MR | MetaPattern | SUTs | 价值 |
|---|---|---|---|---|
| **A1** | MR-Rot90 | m_inv | OpenMOC + OpenMC | 几何对称性最快验证（C4） |
| **A2** | MR-NuSigmaF | m_mono | OpenMOC + OpenMC | 物理单调性，覆盖率最高 |
| **A3** | MR-RefineParticles | m_conv | OpenMC | 蒙卡收敛 σ ∝ 1/√N |
| **A4** | MR14-cmp-openmoc-vs-openmc | m_cmp | OpenMOC ↔ OpenMC | 跨求解器一致 |
| **A5** | MR14 @ ModSigmaA(1.5) | m_cmp | OpenMOC vs OpenMC | R-Case-4 narrow basin 复现 |

Anchor JSON: `cold_start_demo/anchors/anchors.json`

## 8 阶段冷启动演练（Linux cloud 端）

| # | Phase | 通过 | 暴露 bug |
|---|-------|------|---------|
| 0 | environment | ✅ | — |
| 1 | catalog-migration (`feature_to_db.py`) | ✅ | C2 / C1 |
| 2 | anchor-integrity | ✅ | — |
| 3 | noether-discovery (`noether_candidates.py`) | ✅ | — |
| 4 | mutation-stub | ✅ | — |
| 5 | coverage-report | ✅ | C3 (density 算式) |
| 6 | trend-report | ✅ | — |
| 7.5 | naming-consistency | ✅ → ⚠️ → ✅ (部分) | C4 |
| 7 | paper-package (`build_paper_package.py`) | ✅ | — |

## 发现的 Bug + 修复

### ✅ C2 (Medium) — feature_to_db.py 产生重复 binding
**根因**: `_extract_examples` 每行 Examples 表都创建一条 binding，但
MR-MirrorX 等 .feature 表里同一 (mr_code, sut) 有 2-3 行（不同 factor / sample），
逻辑应折叠为一条 binding 含多个 sample。

**症状**: catalog.bindings = 36，其中 9 行重复，唯一 (mr_code, sut) pairs = 27。

**修复**: `parse_directory` 加 `seen: dict[(mr_code,sut)]` 去重 +
sample 行折叠成 `sample_cases: [...]` 数组。

### ✅ C3 (Medium) — Phase 5 SUT×MR density 算法 bug → 112%
**根因**: 用裸 binding 数当分子，分母却是 #suts × #mrs；
binding 重复（C2）+ m_cmp 假 SUT 同时贡献，导致 density > 100%。

**修复**: 切到唯一 (mr_code, real_sut) pair 计数 +
排除 `(cross-program)` 伪 SUT（m_cmp 跨求解器场景）。

**结果**: density 112% → 78%。

### ✅ C4 (Architectural, part fix) — Catalog ↔ Noether 命名漂移
**根因**: catalog .feature 用短码（`MR-Rot90`、`MR14-cmp`），
noether `tools/noether_candidates.py` 用全描述码（`MR01-inv-quarter-rotation-90`、
`MR14-cmp-openmoc-vs-openmc`）→ exact match = 0。

**影响**: Discovery 子系统跑 noether → 产 `CandidateMR.ProposedCode="MR14-cmp-openmoc-vs-openmc"`
→ Validation 后 promote 进 MetamorphicRelations 表时 lookup `Code` 字段 → **找不到**
现有 `MR14-cmp` schema → **创建新 row** 而非合并。Discovery 形同孤岛。

**部分修复（本 PR）**: 把 m_cmp 两个 .feature 的 `Feature: <code>` 改为 noether 长码
（`MR14-cmp-openmoc-vs-openmc` + `MR15-cmp-p0-vs-p1-scattering`）。
重跑：exact match 0 → 2。

**完整修复（follow-up F15）**: 其余 14 个 catalog-only + 13 noether-only 名字需统一。
建议方向：以 noether 长码为权威 → 改全部 .feature 的 Feature: 行 + scenario_id_v1。

### 🛑 C1 (Architectural, design issue) — m_cmp binding 模型
**根因**: 现有 `MRBindings(MRId, ApplicationId)` 二元约束 → m_cmp（同一 MR 同时绑两个 SUT 对比）
没有合适的 schema 表达。`feature_to_db.py` 之前直接 emit `sut=None` 的 binding 行，
被认为是数据 dirty。本 PR 标 `sut="(cross-program)"` 占位。

**影响**: m_cmp 系列 MR 的真实绑定关系（"对 SUT pair 而非单 SUT"）目前在数据模型上**没有正确表达**。

**修复方案（follow-up F16）**: 三种可选：
1. **加 SutPair 字段**: `MRBinding.SutPairId? → SutPairs(SutPairId, LeftAppId, RightAppId)`
2. **多行绑定**: m_cmp 一份 MR 产 2 条 binding（左/右各一），加 `Role: "left"/"right"`
3. **MRBinding.IsComparison: bool**: 标记位 + JSON `ParameterMappings` 表达对比逻辑

每种方案都改 schema + IDAL + UI。最小影响是 #3。

## 未发现的潜在风险

- 没跑真实 OpenMOC/OpenMC pipeline（需 VM venv）
- 没测 LiteDB 实际持久化（需 dotnet 端集成测试）
- 没跑端到端 cold-start full reset → 重新落库（需 C# import 工具）

## Follow-up 任务（追加到 PR #28 跟踪表）

- **F15** Catalog/Noether 命名 canonicalization（其余 14+13 个 MR 改名为 noether 长码）
- **F16** m_cmp MRBinding 数据模型修复（C1 三种方案之一）
- **F17** cold_start_demo 在 CI 跑（GitHub Actions 加 step → 任何 phase 失败阻塞 PR）

## 总结

- **5 anchor 案例选定，覆盖 4 NOETHER MetaPattern + R-Case-4 复现**
- **3 bug 已 fix**：C2 binding dedup / C3 density 算式 / C4 m_cmp 命名（部分）
- **2 架构问题已 surface**：C1 m_cmp 数据模型 / C4-rest 命名 canonicalization
- **0 测试回归**：全部 27 Python + 325 xUnit pass
- **cold_start_demo.py 可重复运行**，每次产出 markdown + JSON 报告，可入 CI
