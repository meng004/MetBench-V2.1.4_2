# Plan — 基于元模式的结构化 meta-prompt MR 识别引擎

> **Stage 8 / Goal 1 — Writing-plan 阶段**
> **日期**: 2026-05-18
> **状态**: 正式实施计划，approved-to-execute pending user OK
> **关联**: [brainstorming](2026-05-18-meta-prompt-mr-discovery-brainstorming.md) · [AGENTS.md Stage 8 Goal 1](../../../AGENTS.md#goal-1-基于元模式的结构化-meta-prompt-mr-识别引擎)
> **总工时**: ~14h（4 phases × 3.5h 平均）

---

## 1. 目标 & 验收标准（重述）

**目标**：给定 SUT input schema + 参数说明 + 物理方程上下文，自动产 MR candidate（带 confidence + rationale）。

**验收标准**（v2.2 ship 条件）：

| # | criterion | 验法 |
|---|---|---|
| AC1 | 8 个 NOETHER MetaPattern 各有完整 meta-prompt 模板（含 placeholder） | grep `MetaPromptTemplates.cs` 含 8 个 const |
| AC2 | 给 amax.py SUT 跑 demo，产 ≥ 3 个 MR candidate，且 ≥ 1 个通过 EmpiricalValidator | `MetaPromptMrIdentifierIntegrationTests` 1 个 fact 跑通 |
| AC3 | 给 openmoc/pincell.json 跑，产 ≥ 5 个 candidate；含 m_inv + m_mono + m_conv 各 ≥ 1 个 | 同上，另一 fact |
| AC4 | LLM cost：单 SUT × 8 MetaPattern 总 cost < $0.10（two-phase: cheap 筛 + expensive 生） | manual log review |
| AC5 | TDD 覆盖：unit (fake gateway) + integration (real gateway sanity) | 新 test class ≥ 10 fact |
| AC6 | DI 在 `App.xaml.cs` 注册；WPF 端能 `App.GetService<ILlmMrIdentifier>()` 拿到 | grep + WPF 跑 demo |

---

## 2. 架构（最终化）

### 2.1 新建 component

```
MetBench_BLL.Core/Discovery/
├── MetaPromptTemplates.cs              # 8 NOETHER prompt 模板常量 (新)
├── ISutAwareLlmMrIdentifier.cs         # 接口 (新)
├── SutAwareLlmMrIdentifier.cs          # 实现 (新)
├── SutInputSchemaExtractor.cs          # JSON → flattened param tree (新)
├── EquationContextLoader.cs            # 读 SUT/<sut>/equation.md (新)
└── MetaPromptBuilder.cs                # 模板 + param + equation → prompt (新)

MetBench_SystemMT.Tests/V2Discovery/
├── MetaPromptTemplatesTests.cs         # 8 模板 placeholder 合规性 (新)
├── SutInputSchemaExtractorTests.cs     # JSON 平坦化 + array 处理 (新)
├── EquationContextLoaderTests.cs       # equation.md 解析 (新)
├── MetaPromptBuilderTests.cs           # 模板填充 + 长度限制 (新)
├── SutAwareLlmMrIdentifierTests.cs     # 端到端 with fake gateway (新)
└── SutAwareLlmMrIdentifierIntegrationTests.cs  # 真实 LLM sanity (新, env-gated)

SUT/amax/                                # 新 demo SUT (新)
├── amax.py                             # CLI runner
├── sample/list10.json                  # sample input
└── equation.md                         # 物理"方程" (退化为 array operation 描述)

SUT/openmoc/equation.md                  # OpenMOC 神子输运方程描述 (新, 同样 path)
SUT/openmc/equation.md                   # OpenMC 同方程 (新)
SUT/heat_equation/equation.md            # 1D Fourier 方程 (新)
SUT/projectile/equation.md               # 经典弹道 (新)
```

### 2.2 修改现有

```
MetBench_BLL.Core/Discovery/MetaPattern.cs   # 无改动 — HypothesisTemplate + ExampleParamHints 已就位
MetBench_BLL.Core/Discovery/DiscoveryService.cs  # 新增 RunWithMetaPromptAsync 方法
MetBench_Client/App.xaml.cs                  # 加 DI: ISutAwareLlmMrIdentifier → SutAwareLlmMrIdentifier
```

### 2.3 不改

```
LlmNativeDiscoverer.cs                   # 保留作 baseline 对比
所有 validator (4 类)                    # 沿用 promote pipeline
ILlmGateway 接口                         # 复用
```

---

## 3. 关键接口签名（plan-level draft）

### 3.1 ISutAwareLlmMrIdentifier

```csharp
namespace MetBench_BLL.Discovery;

public interface ISutAwareLlmMrIdentifier
{
    /// <summary>
    /// 给定 SUT input sample + equation context，产 MR candidate 列表。
    /// </summary>
    Task<IReadOnlyList<CandidateMrProposal>> IdentifyAsync(
        SutAwareIdentifyRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record SutAwareIdentifyRequest(
    string SutName,                    // "openmoc" / "amax" / ...
    string InputSamplePath,            // path to JSON file
    string EquationMdPath,             // path to equation.md (or null → skip phase 1 filter)
    IReadOnlyList<string>? MetaPatternFilter = null  // null = consider all 4 active
);
```

### 3.2 MetaPromptTemplates 结构

```csharp
namespace MetBench_BLL.Discovery;

public static class MetaPromptTemplates
{
    public const string MInvTemplate = """
        You are analyzing a scientific computing program for metamorphic testing.
        
        Program: {sut_name}
        Equation: {equation_description}
        Input parameters (flattened):
        {param_tree}
        
        MetaPattern: Invariance (m_inv) - "the program output should be invariant under
        a group action on the input (e.g., reflection, rotation, permutation, scaling)".
        
        Known symmetries from equation context:
        {known_symmetries}
        
        Propose 2-3 invariance-type metamorphic relations for this program.
        For each, specify:
        - transformation: which parameter to vary, how (mathematical expression)
        - assertion: how source output relates to followup output (=, within ε, etc.)
        - confidence: 0.0-1.0
        - rationale: why this MR holds based on the equation
        
        Reply with JSON array: [{"transformation": "...", "assertion": "...", "confidence": 0.8, "rationale": "..."}].
        """;
    
    public const string MMonoTemplate = """ ... """;  // 类似
    public const string MConvTemplate = """ ... """;
    public const string MCmpTemplate = """ ... """;
    // out-of-scope 4 个不写模板
}
```

### 3.3 equation.md 字段规范

```markdown
# Equation context for SUT: openmoc

## Equation name
2D pin-cell neutron transport (Boltzmann), MOC discretization

## Governing equation (LaTeX)
$$\Omega \cdot \nabla \phi(r, \Omega, E) + \Sigma_t \phi = \int_{\Omega'} \Sigma_s \phi \, d\Omega' + \frac{\chi}{4\pi} \int \nu \Sigma_f \phi \, d\Omega'$$

## Key quantities (input → output)
- Input: cross sections (Σ_t, Σ_a, Σ_s, νΣ_f), geometry (radius, extent), tracking params
- Output: k_eff (eigenvalue), φ(r) (flux)

## Known symmetries
- Reflection symmetry: pin-cell with reflective BC is invariant under 90°/180° rotation
- Mirror symmetry: x ↔ -x, y ↔ -y
- Permutation: material order in materials dict doesn't matter

## Known monotonicities
- νΣ_f ↑ → k_eff ↑ (more fission production)
- Σ_a ↑ → k_eff ↓ (more absorption)
- Σ_t = Σ_a + Σ_s (consistency)

## Boundary conditions
Reflective on all 4 sides

## Convergence
- Refining tracking (num_azim × azim_spacing) → k_eff converges to continuous limit
- Refining flux mesh → spatial convergence
```

---

## 4. Phase breakdown

### Phase 8.1.1 — 模板 + 抽取器（~3h）

| Deliverable | 内容 |
|---|---|
| `MetaPromptTemplates.cs` | 4 active MetaPattern 各一 const (m_inv / m_mono / m_conv / m_cmp)；out-of-scope 不写 |
| `SutInputSchemaExtractor.cs` | JSON → `Dictionary<string, ParamInfo>`（含路径 / 类型 / 是否数组） |
| `EquationContextLoader.cs` | 读 `SUT/<sut>/equation.md` → `EquationContext` record |
| 单测 3 类 ≥ 8 fact | placeholder 完整性 / 嵌套 JSON 平坦化 / equation.md 解析 |

### Phase 8.1.2 — MetaPromptBuilder + LlmMrIdentifier（~4h）

| Deliverable | 内容 |
|---|---|
| `MetaPromptBuilder.cs` | 模板 × 参数树 × equation → 单一 prompt string（含 truncation 控制） |
| `SutAwareLlmMrIdentifier.cs` | two-phase 编排：phase 1 cheap LLM 选 MetaPattern subset；phase 2 expensive LLM 逐 pattern 生 candidate |
| `Discovery.SutAwareIdentifyRequest` record | input schema |
| 单测 ≥ 8 fact + 1 integration | fake gateway 全路径覆盖；real LLM env-gated sanity（amax demo） |

### Phase 8.1.3 — SUT 配套 equation.md（~2h）

| Deliverable | 内容 |
|---|---|
| `SUT/openmoc/equation.md` | 神子输运 Boltzmann + 已知对称 + 单调 |
| `SUT/openmc/equation.md` | 同（OpenMC 共方程） |
| `SUT/heat_equation/equation.md` | 1D Fourier + 守恒律 |
| `SUT/projectile/equation.md` | 经典弹道 + 对称 |
| `SUT/amax/equation.md` + 整 amax SUT | 退化为 array operation 描述；amax.py 简版 runner |

### Phase 8.1.4 — DI / DiscoveryService 集成 + demo（~3h）

| Deliverable | 内容 |
|---|---|
| `App.xaml.cs` DI | `services.AddSingleton<ISutAwareLlmMrIdentifier, SutAwareLlmMrIdentifier>()` |
| `DiscoveryService.RunWithMetaPromptAsync(sutName, options)` | wrap identifier + 自动 promote pipeline（沿用 4 validator） |
| Integration test: amax 端到端 | env-gated, real LLM, expects ≥ 3 candidates, ≥ 1 promote-able |
| Integration test: openmoc 端到端 | 同，expects ≥ 5 candidates 含 m_inv + m_mono + m_conv |
| Cost log | 单跑 dump `phase_1_calls`/`phase_2_calls`/`tokens_in`/`tokens_out` per SUT |

### Phase 8.1.5 — UAT BDD wrapper + 文档（~2h）

| Deliverable | 内容 |
|---|---|
| `UC-C12-MetaPromptMrIdentification.feature` | rubric coverage step：底层 service 解析 amax demo 产 candidate |
| `acceptance-rubric.md` Part C 加 UC-C12 row | "MR Discovery via meta-prompt" / Passed ≥ 3 / 🟡 |
| `test-procedures.md` 加 UC-C12 三段式 | 初始条件 / 操作步骤 / 断言 |
| `PROJECT-STRUCTURE.md` §4 加 Discovery 行 | 反映新 ISutAwareLlmMrIdentifier |
| `RELEASE_NOTES.md` 加 v2.2 Highlights 草稿 | 引此 plan + brainstorming |

---

## 5. 工时汇总

| Phase | 内容 | 工时 |
|---|---|---|
| 8.1.1 | 模板 + 抽取器 + equation.md loader | 3h |
| 8.1.2 | Builder + Identifier (two-phase LLM) | 4h |
| 8.1.3 | 5 个 SUT equation.md 配套 + amax SUT 新建 | 2h |
| 8.1.4 | DI + DiscoveryService 集成 + 2 integration test | 3h |
| 8.1.5 | UAT BDD + 4 文档 sync | 2h |
| **合计** | | **14h** |

---

## 6. 依赖

| 依赖 | 状态 |
|---|---|
| MetaPattern 实体 + Seed 完整 | ✅ |
| ILlmGateway + 3 实现 | ✅ |
| 4 类 validator + promote pipeline | ✅ |
| LLM API key（DEEPSEEK / OPENAI / CLAUDE） | ✅ (`.env` ready) |
| `MultiLlmConsensusValidator` | ✅（plan 复用） |

**0 阻塞**。

---

## 7. Phase 顺序 / PR 切片

| PR | Phase | 描述 |
|---|---|---|
| #X1 | 8.1.1 | 模板 + 抽取器 + loader（纯 BLL.Core + 单测，无 LLM） |
| #X2 | 8.1.2 | Builder + Identifier（two-phase）+ fake gateway test |
| #X3 | 8.1.3 + 8.1.4 | 5 SUT equation.md + amax SUT + DI + integration test |
| #X4 | 8.1.5 | UAT BDD + 4 文档 sync + RELEASE_NOTES v2.2 草稿 |

每 PR 独立 mergeable / CI 通过。

---

## 8. 风险 + 缓解（plan 阶段细化）

| 风险 | 缓解 |
|---|---|
| Phase 1 LLM 选 MetaPattern 错（漏选 / 多选） | log Phase 1 选择 + 比对 ground truth；可对比手工选；amax 测试覆盖该路径 |
| equation.md 字段写得差 | 提供 5 SUT 模板作 reference；新 SUT 必填字段 ≤ 6 |
| Two-phase 跨 model 兼容性 | 强制 cheap model = `deepseek-v4-pro`，expensive = `gpt-5.5` / `claude-opus-4-7`，用现有 `.env` 配置 |
| Token cost 超预算 | per-pattern call max_tokens = 1024；总 cost log + assert < $0.50 / SUT |
| LLM 返回 non-JSON | retry 1 次；仍失败标 candidate 为 `parse_failed` 不阻塞 |
| amax SUT 太简化无法验复杂物理 | openmoc/pincell.json integration test 作 backup demo |

---

## 9. 测试策略

### TDD 顺序

1. `MetaPromptTemplatesTests`：8 const 各含必要 placeholder（{sut_name} / {param_tree} / {equation_description}）
2. `SutInputSchemaExtractorTests`：openmoc/pincell.json + heat_equation/gaussian.json + amax/list10.json → param tree 正确
3. `EquationContextLoaderTests`：合法 / 缺字段 / malformed markdown 各一 fact
4. `MetaPromptBuilderTests`：模板填充 + 截断 + escape 各一 fact
5. `SutAwareLlmMrIdentifierTests`：fake gateway，模拟 phase 1 + phase 2 响应 → 验 candidates 个数 / 字段
6. `SutAwareLlmMrIdentifierIntegrationTests`：env-gated（`METBENCH_META_PROMPT_DEMO=1`），real LLM，amax + openmoc

### CI 影响

新增 ~25 unit fact + 0 CI-跑的 integration（env-gated）。Cumulative wall +1-2s 估计。

---

## 10. 完成时的 main 状态

| 指标 | 目标 |
|---|---|
| baseline-2026-XX-YY | 521 + 25 unit = **546 facts**, 0 fail |
| UAT BDD | 48 + 1 (UC-C12) = **49 scenario** |
| 新 SUT | amax 接入（demo 用） |
| `equation.md` 入仓 | 5 个 SUT 各 1 |
| 新 BLL.Core class | 5 个（templates / extractor / loader / builder / identifier） |
| 文档同步 | acceptance-rubric / test-procedures / PROJECT-STRUCTURE / RELEASE_NOTES v2.2 草稿 |

---

## 11. 不交付（scope 外，明确）

- **不**改 8 NOETHER MetaPattern 内容（Status 等）
- **不**改现有 3 类 discoverer（LlmNativeDiscoverer / MetaPatternDiscoverer / ScgHeuristicDiscoverer 全保留）
- **不**改 ILlmGateway / validator 任何接口
- **不**改 WPF UI（仅 DI 一行加注册；新 UI 页面留 Goal 1 第 2 轮）
- **不**做 multi-language SUT support（仅 JSON 输入；YAML / TOML 留下版）
- **不**做 prompt 优化 / RAG / fine-tuning（基础版先跑通）
