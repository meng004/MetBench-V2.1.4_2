# Brainstorming — 基于元模式的结构化 meta-prompt MR 识别引擎

> **Stage 8 / Goal 1 — Brainstorming 阶段**
> **日期**: 2026-05-18
> **状态**: 需求探索 + 设计空间扫描；待 writing-plan 阶段固化
> **关联**: [AGENTS.md Stage 8 Goal 1](../../../AGENTS.md#goal-1-基于元模式的结构化-meta-prompt-mr-识别引擎)

---

## 1. 目标重申

把 8 个 NOETHER MetaPattern 从"数据库 seed 行"升级成"可驱动 LLM 自动识别 MR candidate 的 prompt 模板"。给定一个新 SUT 的：
- 输入文件 schema（JSON / YAML / 自定义）
- 参数说明（类型 / 单位 / 物理含义）
- 数学物理方程上下文

自动产出**该 SUT 特定的 MR candidate 列表**，每条含：
- 触发的 MetaPattern code (`m_inv` / `m_mono` / …)
- 输入变换（"对 fuel.nu_sigma_f 缩放 1.5 倍"）
- 期望断言（"k_eff 单调增"）
- LLM 给的 plausibility confidence + rationale

---

## 2. 现状（Explore agent 2026-05-18 调研）

### 2.1 MetaPattern 实体已就位

`MetBench_Domain/V2/MetaPattern.cs:17-54` 含 **12 字段**，其中两个为 prompt 生成做的预备：

- `HypothesisTemplate` (string)：如 `"k_followup ≈ k_source within ε (group-action invariance)"`
- `ExampleParamHints` (List<string>)：如 `["geometry.rotation_deg", "geometry.mirror_axis"]`

8 个 NOETHER 全 seeded（`MetaPatternSeed.cs:21-107`）：
- **active (4)**：m_inv / m_mono / m_conv / m_cmp
- **out-of-scope (4)**：m_adj / m_rev / m_dyn / m_rel（已有 OutOfScopeReason 字段说明原因）

### 2.2 LLM Discovery 链路 90% 就位

`MetBench_BLL.Core/Discovery/` 下 20 个 .cs 文件覆盖完整 3 类 discoverer + validator + gateway：

| 已有 | 文件 / 类 | 状态 |
|---|---|---|
| **IMRDiscoverer** + 3 实现 | `IMRDiscoverer.cs` + MetaPatternDiscoverer / LlmNativeDiscoverer / ScgHeuristicDiscoverer | ✅ ready |
| ILlmGateway + 3 网关 | OpenAiCompatibleLlmGateway / DeepSeekLlmGateway / NullLlmGateway | ✅ ready |
| 4 类 validator | EmpiricalValidator / TheoreticalLlmValidator / AdversarialMutmutValidator / MultiLlmConsensusValidator | ✅ ready |
| DiscoveryService + ValidationService 编排 | 2 个 service | ✅ ready |

**关键缺口**：`LlmNativeDiscoverer:91-94` 的 prompt 是 **硬编码** "List 5 new MR on k_eff"，没有 SUT-aware 上下文注入。

### 2.3 SUT 输入 + 参数映射

- 4 SUT 全 JSON 格式（OpenMOC / OpenMC / heat_equation / projectile），可统一解析
- `ParameterMapping` 实体（`MetBench_Domain/V2/ParameterMapping.cs:14-36`）已有 `AbstractParamName` ↔ `ConcreteFieldPath` 映射
- 3 个 path resolver 已实装：JsonPointerResolver / McnpCardResolver / NamelistKeyResolver
- **缺**：自动从 SUT input JSON 抽参数树的工具（当前 ParameterMapping 是手编）

---

## 3. 设计空间扫描

### 3.1 核心架构问题

#### Q1: prompt 是模板还是 LLM 自生成？

| 选项 | 描述 | 优 | 劣 |
|---|---|---|---|
| **A. 纯模板**（推荐） | 每个 MetaPattern 一个 fill-in-the-blank 模板，C# 端填 SUT 参数 | 可控、可重复、低成本（1 次 LLM call/MR）| 模板灵活度有限；新 MetaPattern 要手写模板 |
| B. LLM 自生 prompt | 给 LLM 提供 MetaPattern 描述 + SUT context，让 LLM 自己写识别 prompt | 灵活，处理新 pattern 容易 | 不可重复，成本翻倍（2 次 call/MR），prompt 漂移 |
| C. 混合 | 模板 + LLM 提示词 enhancement | 兼具 | 复杂度高 |

**倾向 A**（纯模板）— 符合"基于元模式"的 framework 卖点，**可重复 + 可比较**是论文重点。

#### Q2: prompt 模板放哪？

| 选项 | 描述 |
|---|---|
| **A. C# 代码常量** | 每个 MetaPattern 一个 `static readonly string`，跟代码同 commit |
| B. DB seed | `MetaPattern.PromptTemplate` 新增字段，seed 时填 |
| C. 外部 .md/.txt 文件 | `docs/meta-prompts/m_inv.md` 等，C# 读文件 |
| D. 混合 | 默认 C# 常量，DB 字段 override |

**倾向 D**（mix）— 默认走代码常量保证 git diff 可见；DB override 留给运行时实验。

#### Q3: SUT 参数树怎么抽？

| 选项 | 描述 | 适用 |
|---|---|---|
| **A. JSON 自动平坦化** | 用 `JsonElement` 递归列所有叶节点 → 输出 `Dictionary<string, ParamInfo>` | JSON 格式 SUT（当前 4/4 都是） |
| B. 显式 schema 描述 | 用户手写 `<sut>_input_schema.json` 描述每参数的类型 / 单位 / 物理含义 | 复杂、跨格式 |
| C. LLM 抽 schema | 把 sample 输入丢给 LLM 让它返回 schema | 慢、不可重复 |

**倾向 A + B 渐进式**：自动平坦化默认；可选增量 `<sut>_input_schema.json` 给 hint（类似 OpenAPI tags）。

#### Q4: 物理方程上下文怎么注入？

| 选项 | 描述 |
|---|---|
| **A. SUT 元数据文件** | `SUT/<sut>/equation.md` 写方程 + 关键守恒律 + 已知对称性，C# 读文件嵌入 prompt |
| B. 用户运行时输入 | UI / CLI 让用户每次跑前粘贴方程描述 |
| C. 从 scg.json 推导 | 用 SCG 因果图节点描述代替方程 |

**倾向 A**：`equation.md` 入仓 + 一次写多次用，比 B 可重复，比 C 更显式（SCG 是工程层不是物理层）。

#### Q5: 一次 LLM call 还是多次？

- **单 call**：把所有 8 个 MetaPattern + SUT context 一次性丢给 LLM，让它返回所有候选
  - 优：成本低（1 call/SUT）；劣：prompt 长，LLM 容易漏 MetaPattern
- **每 MetaPattern 一 call**：8 calls/SUT
  - 优：每 MetaPattern 独立、可并发、产候选质量高；劣：成本 8x
- **混合**：先 1 call 让 LLM 选适用 MetaPattern subset（"哪几条 NOETHER 适用？"），再对每选中的 MetaPattern 单 call
  - 优：成本中等，质量好；劣：2 阶段架构稍复杂

**倾向**：混合（two-phase）。Phase 1 用低成本 model（如 deepseek-v4）筛 MetaPattern；Phase 2 用 high-end model（如 gpt-5.5 / claude-opus-4-7）逐 pattern 生成 MR。

### 3.2 集成现有 validator 链

新引擎产出的 candidate 自动喂给现有 validator：
1. `EmpiricalValidator` —— sample 跑通过率 ≥ 阈值
2. `TheoreticalLlmValidator` —— 第二组 LLM 判 plausibility（避免同一 LLM 自吹）
3. `MultiLlmConsensusValidator` —— 3 LLM 投票
4. `AdversarialMutmutValidator` —— mutmut 注入测 vacuousness

≥ 2 个 validator 通过才 promote 进正式 MR 表。**沿用现有 promote pipeline，零改动**。

### 3.3 输出格式

新 service 返回 `IReadOnlyList<CandidateMrProposal>`（已有 record）— 直接喂 `DiscoveryService.PromoteAsync` 入库。

---

## 4. 替选方案比较

| 方案 | 描述 | 工时 | 论文价值 |
|---|---|---|---|
| **A. 增量** (推荐) | 沿用 `LlmNativeDiscoverer` 架构，加 `SutAwareMetaPromptGenerator` 替代硬编码 prompt | 8-12h | 中：清楚的"meta-prompt"贡献 |
| B. 替换 | 新建 `MetaPromptMrIdentifier` service 跟 `LlmNativeDiscoverer` 并列 | 16-20h | 高：独立 component，论文易引用 |
| C. 重构 | 把所有 3 个现有 discoverer 抽公共基类 + meta-prompt 改为 strategy 模式 | 30-40h | 高：cleanest architecture，但 over-engineering |

**倾向 B**（独立 service） —— 跟 LlmNativeDiscoverer 并列 ≠ 替换。论文可以独立引用，老 discoverer 留作 baseline 对比。

---

## 5. 风险

| 风险 | 缓解 |
|---|---|
| LLM cost 失控 | 单 SUT × 8 MetaPattern × 3 provider = 24 calls；用 phase 1 筛 MetaPattern 降到 ~10 calls。预算每 SUT < $0.10 |
| Prompt 漂移（模型版本变 / 时间变） | 每 prompt 模板含 model id；run record 保留 raw response；可重跑对比 |
| 用户写不出好的 `equation.md` | 提供示例 `SUT/openmoc/equation.md` 作为 template；新 SUT 必填字段 ≤ 5 |
| LLM 生成 MR 全是"too generic" | TheoreticalLlmValidator + EmpiricalValidator 联合过滤；adversarial mutmut 防 vacuous |
| 8 个 MetaPattern 4 out-of-scope | 仅对 4 active 跑 phase 2；out-of-scope 跳过（但 phase 1 提示让 LLM 判别） |
| schema 自动抽参数遇到嵌套数组 | 平坦化到 `materials.fuel.sigma_t[]` 形式 + 标 array 类型；LLM 能识别 |

---

## 6. 推荐方向（待 plan 阶段固化）

| 维度 | 选择 |
|---|---|
| **架构** | 增量 B：新建 `SutAwareLlmMrIdentifier` 独立 service |
| **prompt 来源** | C# 代码常量 + DB override（混合 D） |
| **SUT schema 提取** | JSON 自动平坦化 + 可选 `<sut>_input_schema.json` |
| **物理方程注入** | `SUT/<sut>/equation.md` 入仓 |
| **LLM call 策略** | Two-phase（cheap model 筛 → expensive model 生成） |
| **验证集成** | 沿用 4 类 validator 链，≥ 2 pass 才 promote |
| **输出** | `CandidateMrProposal[]` |
| **demo SUT** | amax.py（已有，最简单）→ heat_equation（验复杂度）→ openmoc（真实工程） |

---

## 7. 待 plan 阶段决定的细节

- 8 个 MetaPattern 各自的 meta-prompt 模板**具体怎么写**？需要逐条 draft（plan 阶段附录）
- `equation.md` 字段固定哪些？建议：`equation_name` / `governing_equation_latex` / `key_quantities` / `known_symmetries` / `known_monotonicities` / `boundary_conditions`
- DI 注册名 / 接口签名细节
- TDD test 顺序：unit → fake LLM → real LLM sanity
- demo 跑通后入 UAT 哪一类？建议加 UC-C12

---

## 8. Ready-to-plan checklist

- [x] 现状调研完整
- [x] 设计空间问题穷举 (Q1-Q5)
- [x] 替选方案比较 (A/B/C)
- [x] 风险识别 + 缓解
- [x] 推荐方向明确
- [ ] **下一步**：[`2026-05-18-meta-prompt-mr-discovery-plan.md`](2026-05-18-meta-prompt-mr-discovery-plan.md) — 落实施 phases + deliverables + 工时
