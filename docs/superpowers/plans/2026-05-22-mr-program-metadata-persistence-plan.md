# Plan — MR / 程序元信息持久化 + 相等断言

> **日期**: 2026-05-22
> **状态**: ✅ 核心三 phase 全交付（2026-05-22）—— P-A 相等断言 + P-C 元信息持久化 +
> P-B 运行记录增强。缩放等式 assertion 已转入 Stage 8（DP-2，见 §6）；VM 接线为各
> phase 待续项（见下）
> **关联**: [`CLAUDE.md`](../../../CLAUDE.md) §2 · [`docs/t3-program-selection.md`](../../t3-program-selection.md) ·
> [Stage 8 详细计划](2026-05-18-stage8-expanded-mr-library-plan.md) Phase 8.0（5D schema）

---

## 1. 目标 & 验收标准

承接 user 需求，4 件：

1. **相等断言** —— assertion 库新增 equality assertion：**绝对模式**（数值阈值，默认
   `1e-5`）+ **相对模式**（相对偏差比例）。阈值经 `appsettings.json` 配置（改值不
   重编译）。补上 `GreaterThan`/`LessThan` 之外的等式判定，使 MP_inv（不变性 / 齐次）
   类 MR 可被表达。
2. **程序元信息持久化** —— 程序求解的数学物理方程作为**程序元信息**：方程名称、
   符号系统、参数说明 —— 结构化持久化、可查询。
3. **MR 元信息持久化** —— 每条 MR 的说明：参数符号、物理含义、取值范围、输入转换
   关系、输出关系、比较类型（绝对 / 相对）—— 结构化持久化。
4. **运行记录增强** —— 每次 run 保存：输入样本点数、每个输入与其转换后的值、输出
   变量取值 —— 支持回归测试。

**验收**：① equality assertion 可用 ✅（P-A 交付）—— 把 P1 的 3 条 MR 由 MP_mono 升到
MP_inv 属缩放等式形态，**转入 Stage 8**（见 DP-2 / §6）；② 方程 / MR 元信息有结构化
持久化（非仅源码 docstring）✅；③ 运行记录含样本点级「输入 → 转换值 → 输出」三元组，
可回放比对 ✅。

---

## 2. 决策点

- **DP-1 「相对比较 95%」语义** —— user 写「相对比较，有一个偏差比例，如 95%」。
  推荐解读：相对误差 ≤ 5%（即「95% 一致」），assertion 参数取「容许相对偏差」
  （默认 `0.05`）。**待 user 确认**。
- **DP-2 equality assertion 形态** —— ✅ **已定（user 2026-05-22）**。纯不变性
  （`flw ≈ src`，如几何对称、守恒律）不需变换 factor；缩放等式（`flw ≈ k·src`，如齐次
  —— P1 的 3 条 MR 即此类）需拿到变换 `factor`，而 `IMrAssertion.Evaluate(valueName,
  src, flw)` 当前签名拿不到。**决定**：不变性 equality 已在 P-A 交付（abs/rel 合一
  容差）；缩放等式（需扩 `IMrAssertion` 签名或引入「期望比值」参数，并据此把 P1 的
  3 条齐次 MR 由 MP_mono 升到 MP_inv）—— **转入 Stage 8**，并入其 MR 库工作。
- **DP-3 元信息 schema 落点（关键张力）** —— ✅ **已定（user 2026-05-22「走 b」）**。
  System-MT 的 MR catalog 仍是硬编码 C#（`SystemMtMrLauncher.BuildMrCatalog`）。
  **决定走 (b)**：元信息作**独立持久化层**，按 MR id / EquationKey 与 catalog 关联，
  不改写 catalog。「两套并存漂移」风险由 `SystemMtMetadataCatalogTests` 的漂移守卫
  消解 —— 它断言 seed 的 MR id 集与 launcher catalog 的 id 集**严格相等**，新增
  catalog MR 而漏配元信息即编译期 / 测试期失败。

---

## 3. Phase 序列（执行次序）

执行次序 **P-A → P-C → P-B**（下表已按此排）。P-C 提到 P-B 之前：MR / 程序元信息
schema 是运行记录的关联底座 —— 先有 schema、再扩运行记录才不返工；且 P-C 即 Phase 8.0。

| Phase | 内容 | 工作量 | Track |
|---|---|---|---|
| **P-A** ✅ 相等断言 | `ApproxEqualAssertion`（绝对 + 相对合一容差）+ `EqualityThresholds` record + 7 测试。缩放等式（升 P1 MR）转入 Stage 8 | ~1–2 天 | Cloud |
| **P-C** ✅ 元信息持久化 | 方程 + MR 元信息 schema（实体 + DAL + seed catalog + 漂移守卫）。DP-3=(b) 独立层。BDD tag sync 留 Phase 8.0 | ~1 天 | Cloud |
| **P-B** ✅ 运行记录增强 | `InputSamplePoint` + `InputCaseReader` + `SystemMtResultRecord.InputSamples` —— 样本点级「源输入 / 转换后输入」配对，与既有 output metrics 合成回放三元组 | ~1 天 | Cloud |

P-A 独立、最先做（也解 P1 的 MP_mono→MP_inv 落差）。

---

## 4. 各 Phase 详情

### P-A 相等断言 ✅ 已交付（2026-05-22）

> **已交付**：`EqualityThresholds` record（atol=1e-5 / rtol=0.05 + `.Default`）+
> `ApproxEqualAssertion`（`IMrAssertion`，numpy-isclose 风格合一容差 `|flw−src| ≤
> atol+rtol·|src|`）+ 7 个 TDD + 注册进 launcher assertion 集。全套 566 passed。
> **本次只做不变性形态**（`flw ≈ src`，MP_inv 不变性 MR）。
> **缩放等式转入 Stage 8**：`flw ≈ k·src`（升 P1 的 3 条齐次 MR 到 MP_inv）需给
> `IMrAssertion.Evaluate` 传变换 factor —— 见 DP-2，并入 Stage 8 MR 库工作。
> **待续（VM）**：`appsettings.json` 的 `EqualityThresholds` 段绑定（同 DP-3 模式）。

设计参考（已实现）：

`MetBench_BLL.Core/SystemMT/Assertions/` 下新增 equality assertion，实现 `IMrAssertion`：

- **绝对模式** `|flw − src| ≤ atol`（默认 `atol = 1e-5`）。
- **相对模式** `|flw − src| / |src| ≤ rtol`（默认 `rtol = 0.05`，见 DP-1）。
- **阈值经 `appsettings.json` 配置**：`atol` / `rtol` 收敛到一个 `EqualityThresholds`
  record（BLL.Core 持纯数据 record + `.Default`；WPF 侧绑 `appsettings.json` 段），
  改阈值不重编译 —— 沿用 `AnomalySeverityThresholds`（DP-3）的同一模式。
- 注册进 `SystemMtRunner` 的 assertion 集；blueprint 的 `AssertionName` 可选它。
- 缩放等式（`flw ≈ k·src`）形态见 DP-2 —— 需 `IMrAssertion` 接口决策，**转入 Stage 8**。
- TDD：边界值、abs/rel 两模式、`src≈0` 时相对模式退化守卫。

### P-C 元信息持久化 ✅ 已交付（2026-05-22）

> **已交付**（TDD 红→绿，3 cycle）：DP-3 走 (b) 独立持久化层。13 个新测试，全套 582 passed。

落点（`MetBench_BLL.Core/SystemMT/Metadata/`）：

- **`EquationMetadata`**（程序元信息）—— `EquationKey`（业务键）/ `Name` / `CanonicalForm` /
  `SymbolSystem` / `Parameters`（`EquationParameter` = 符号 + 说明 + 单位）。
- **`MrMetadata`**（MR 元信息）—— `MrId`（= catalog `MrSummary.Id`）/ `EquationKey`（关联方程）/
  `PhysicalMeaning` / `InputTransformation` 输入转换关系 / `OutputRelation` 输出关系 /
  `ComparisonType` 比较类型 / `Parameters`（`MrParameter` = 符号 + 物理含义 + 取值范围）。
- **`MrComparisonType`** 枚举 —— `Ordinal`（单调序，当前 8 条 catalog MR 全是此型）/
  `Absolute` / `Relative`（两种相等容差模式，留给 DP-2 延后的缩放等式 MR）。
- **`ISystemMtMetadataRepository`** + **`LiteDbSystemMtMetadataRepository`**（`MetBench_DAL/`）——
  upsert（按业务键幂等）/ get / list，唯一索引护键。沿用 `LiteDbSystemMtResultRepository`
  的私有 `BsonMapper` + 双构造器（connectionString / `ILiteDatabase`）模式。
- **`SystemMtMetadataCatalog`** —— 5 方程（neutron-transport / heat-equation-1d / bateman /
  damped-oscillator / lotka-volterra）+ 8 MR 的 seed 数据 + `SeedAsync`（幂等）。
- **漂移守卫**：`SystemMtMetadataCatalogTests` 断言 seed MR id 集 ≡ launcher catalog id 集。

**待续（VM）**：生产 DI 接线 —— 三个 System-MT 仓储（result / metadata）若指向同一
`.Litedb` 文件需共享 `ILiteDatabase` 句柄（direct 模式独占锁），或 metadata 用独立文件；
`App.xaml.cs` 注册 `ISystemMtMetadataRepository` + 启动时 `SystemMtMetadataCatalog.SeedAsync`。
**留 Phase 8.0**：BDD `@mr:` tag sync、5D schema 的 SourceLevel / FailureCorrelation 维。

### P-B 运行记录增强 ✅ 已交付（2026-05-22）

> **已交付**（TDD 红→绿，4 cycle）：10 个新测试，全套 592 passed。

落点（`MetBench_BLL.Core/SystemMT/`）：

- **`InputSamplePoint`** —— 一个输入变量在源 / 衍生两个用例间的配对：`Name`（点路径）/
  `SourceValue`（源输入值）/ `FollowUpValue`（转换后输入值）。
- **`InputCaseReader.ReadSamples(sourcePath, followUpPath)`** —— 读源 / 衍生输入用例文件，
  把数值叶子展平配对（嵌套对象点路径、数组带位序下标、非数值叶子跳过、根标量入
  `value`）。best-effort：文件缺失 / 非 JSON → 空列表，绝不因记录增强而让 run 失败。
- **`SystemMtResult.InputSamples`** —— `SystemMtRunner.RunAsync` 在成功路径上调
  `InputCaseReader` 捕获，挂到 result。
- **`SystemMtResultRecord.InputSamples`**（`List<InputSamplePoint>`）—— `FromResult` 投影；
  `LiteDbSystemMtResultRepository` 自动映射持久化。

「输入 → 转换值 → 输出」三元组：输入 / 转换值由 `InputSamples` 持有，输出由记录既有的
`SourceMetrics` / `FollowUpMetrics` 持有 —— 合起来即样本点级回放比对的数据底座。「输入
样本点数」= `InputSamples.Count`。MR 元信息关联经 `MrName` ↔ `MrMetadata.MrId`。

---

## 5. 与 Phase 8.0 / 现有实体的关系

- **P-C 实质是 Phase 8.0「5D tag schema」** 的 Equation + MetaPattern + MR 维 + 元信息
  字段。建议 P-C 不另起 schema，直接并入 Phase 8.0 统一设计、统一落库。
- 现状提醒：P1 的 3 个 SUT，方程仅记录在 runner `.py` docstring + blueprint
  `Description` —— **源码可读、未结构化持久化**。P-C / Phase 8.0 落地后补齐。

## 6. 不交付（scope 外）

- 缩放等式 assertion（`flw≈k·src`，需 `IMrAssertion` 接口变更；升 P1 的 3 条齐次 MR
  到 MP_inv）—— **转入 Stage 8**（DP-2 已定，2026-05-22）。
- 5D schema 的其余维（SourceLevel / FailureCorrelation）—— 属 Phase 8.0 本体。
