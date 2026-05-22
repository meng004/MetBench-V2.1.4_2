# Plan — MR / 程序元信息持久化 + 相等断言

> **日期**: 2026-05-22
> **状态**: draft（待 user 审定）
> **关联**: [`CLAUDE.md`](../../../CLAUDE.md) §2 · [`docs/t3-program-selection.md`](../../t3-program-selection.md) ·
> [Stage 8 详细计划](2026-05-18-stage8-expanded-mr-library-plan.md) Phase 8.0（5D schema）

---

## 1. 目标 & 验收标准

承接 user 需求，4 件：

1. **相等断言** —— assertion 库新增 equality assertion：**绝对模式**（数值阈值，默认 `1e-5`）
   + **相对模式**（相对偏差比例）。补上 `GreaterThan`/`LessThan` 之外的等式判定，使
   MP_inv（不变性 / 齐次）类 MR 可被表达。
2. **程序元信息持久化** —— 程序求解的数学物理方程作为**程序元信息**：方程名称、
   符号系统、参数说明 —— 结构化持久化、可查询。
3. **MR 元信息持久化** —— 每条 MR 的说明：参数符号、物理含义、取值范围、输入转换
   关系、输出关系、比较类型（绝对 / 相对）—— 结构化持久化。
4. **运行记录增强** —— 每次 run 保存：输入样本点数、每个输入与其转换后的值、输出
   变量取值 —— 支持回归测试。

**验收**：① equality assertion 可用、并把 P1 的 3 条 MR 由 MP_mono 升到 MP_inv；
② 方程 / MR 元信息有结构化持久化（非仅源码 docstring）；③ 运行记录含样本点级
「输入 → 转换值 → 输出」三元组，可回放比对。

---

## 2. 决策点

- **DP-1 「相对比较 95%」语义** —— user 写「相对比较，有一个偏差比例，如 95%」。
  推荐解读：相对误差 ≤ 5%（即「95% 一致」），assertion 参数取「容许相对偏差」
  （默认 `0.05`）。**待 user 确认**。
- **DP-2 equality assertion 形态** —— 纯不变性（`flw ≈ src`，如几何对称、守恒律）
  不需变换 factor；缩放等式（`flw ≈ k·src`，如齐次 —— P1 的 3 条 MR 即此类）需拿到
  变换 `factor`。但 `IMrAssertion.Evaluate(valueName, src, flw)` 当前签名拿不到变换
  参数。推荐：先做不变性 equality（abs/rel 两模式）；缩放等式需扩 `IMrAssertion`
  签名或引入「期望比值」参数 —— 列为 P-A 的子项。
- **DP-3 元信息 schema 落点（关键张力）** —— System-MT 的 MR catalog 目前是
  **硬编码 C#**（`SystemMtMrLauncher.BuildMrCatalog`）。元信息要持久化，要么 (a) catalog
  转 data / 每 SUT 一份 manifest（配置优先），要么 (b) 元信息独立持久化、与 blueprint
  关联。推荐 **(a)**，与 Phase 8.0 的 5D schema 合流 —— 否则「硬编 catalog + 持久化
  元信息」两套并存必漂移。

---

## 3. Phase 序列（工作量升序）

| Phase | 内容 | 工作量 | Track |
|---|---|---|---|
| **P-A** 相等断言 | `IMrAssertion` 新增 `ApproxEqual`（绝对 / 相对两模式）+ TDD；P1 的 3 条 MR 升 MP_inv | ~1–2 天 | Cloud |
| **P-B** 运行记录增强 | `SystemMtResultRecord` / 持久化扩样本点级「输入 → 转换值 → 输出」三元组 | ~2–4 天 | Cloud |
| **P-C** 元信息持久化 | 方程 + MR 元信息 schema（实体 + DAL + BDD tag sync）—— **并入 Phase 8.0 5D schema** | ~1–2 周 | Cloud |

P-A 独立、最先做（也解 P1 的 MP_mono→MP_inv 落差）；P-B 中等；P-C 实质是 Phase 8.0。

---

## 4. 各 Phase 详情

### P-A 相等断言（~1–2 天）

`MetBench_BLL.Core/SystemMT/Assertions/` 下新增 equality assertion，实现 `IMrAssertion`：

- **绝对模式** `|flw − src| ≤ atol`（默认 `atol = 1e-5`）。
- **相对模式** `|flw − src| / |src| ≤ rtol`（默认 `rtol = 0.05`，见 DP-1）。
- 注册进 `SystemMtRunner` 的 assertion 集；blueprint 的 `AssertionName` 可选它。
- 缩放等式（`flw ≈ k·src`）形态见 DP-2 —— 需接口决策，可作 P-A 子项或单列。
- TDD：边界值、abs/rel 两模式、`src≈0` 时相对模式退化守卫。

### P-B 运行记录增强（~2–4 天）

扩 `SystemMtResultRecord`（及 `LiteDbSystemMtResultRepository`）记录样本点级数据：
输入样本点数、每个样本「源输入值 / 转换后输入值 / 输出变量值」三元组。用于回归
测试：同 MR 同输入再跑，逐样本点比对。

### P-C 元信息持久化（~1–2 周，并入 Phase 8.0）

- **程序元信息**：方程名称、符号系统、参数说明。
- **MR 元信息**：参数符号、物理含义、取值范围、输入转换关系、输出关系、比较类型。
- 落点见 DP-3 —— 推荐 catalog 转 manifest / 与 5D schema 合流。本 Phase 须先定 DP-3。

---

## 5. 与 Phase 8.0 / 现有实体的关系

- **P-C 实质是 Phase 8.0「5D tag schema」** 的 Equation + MetaPattern + MR 维 + 元信息
  字段。建议 P-C 不另起 schema，直接并入 Phase 8.0 统一设计、统一落库。
- 现状提醒：P1 的 3 个 SUT，方程仅记录在 runner `.py` docstring + blueprint
  `Description` —— **源码可读、未结构化持久化**。P-C / Phase 8.0 落地后补齐。

## 6. 不交付（scope 外）

- 缩放等式 assertion（`flw≈k·src`，需 `IMrAssertion` 接口变更）—— 待 DP-2 定。
- 5D schema 的其余维（SourceLevel / FailureCorrelation）—— 属 Phase 8.0 本体。
