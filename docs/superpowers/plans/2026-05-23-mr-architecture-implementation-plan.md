---
状态: ✅ 全部完成(2026-05-23 P0–P7)
目标: 实现 `docs/design/mr-architecture.md` 基线 —— 双轨 MR 协议层 + 方程作为函数容器
  + L0/L1/L2 算子分层 + 决策 B 查找规则 + 数学/断言库纪律。
关联:
  - docs/design/mr-architecture.md (设计基线,本计划是其执行)
  - docs/superpowers/plans/2026-05-22-systemmt-engine-unification-plan.md (W2 替代 W1,本计划继之)
  - CLAUDE.md §2 T0–T6 (核心功能模型)
  - CLAUDE.md §11 计划工作流闭环
---

# MR 协议层 + 方程函数容器 实施计划

## 1. 背景

`docs/design/mr-architecture.md`(2026-05-22 落地)收口了 10 条 MR 层架构决策,
当前**仅存在于设计文档**,代码尚未实现。本计划是其工程落地序列。

依赖前置:**W2 替代 W1 步骤 1** 已完成(commit `21ab2c8`,launcher 全 8 MR
经 pipeline 跑、统一 Execution+Result schema);步骤 2 删 W1 待后续。
本计划与步骤 2 互不阻塞 —— 可并行推进。

## 2. 目标 & 验收准则(plan 级)

### 2.1 目标
将 mr-architecture.md §8 的实施序列落地为可工作的代码 + 测试 + DI 接线,使:
- 新方程的接入路径 P1(普通用户,无代码)可用
- L1 Recipe 表达力覆盖 ≥ 80% 的常规 MR(线性缩放系)
- L2 实现纪律落地(必须用 `MathNet.Numerics` / `System.Math` / FA,code review 把关)
- BDD steps 切到 W2 facade,清掉双引擎残留
- v1 LaTeX-pattern → sympy 执行路径标 `[Obsolete]`,新 MR 不再依赖

### 2.2 plan 级验收
- 全部 phase TDD 通过,cloud CI 全绿(`dotnet test MetBench_SystemMT.Tests`)
- Bateman 样板方程端到端跑通:launcher `decay-chain-scale-initial` 经 L1 Recipe → 解析 → 执行,产出 `Execution+Result`,数值正确
- mr-architecture.md 无与代码相左的陈述;实现 PR 在描述中引用对应设计章节
- AGENTS.md Stage 8 节点回写本计划状态

## 3. 工作分解

每个 phase 独立 PR、独立过 CI、可回滚。

### Phase P0 — Schema 入位(数据层,无行为变化)

**输出**:
- `MetamorphicRelation` 加 2 个字段:`EquationKey: string`(默认空) + `ValueShape: string`(默认 `"scalar"`)
- `EquationMetadata` 加 `Functions: List<EquationFunctionDescriptor>`(默认空集)
- 新建实体表 `EquationFunctionRecipe` + IDAL 接口 `IEquationFunctionRecipeRepository` + LiteDb 实现

**TDD**:
- schema round-trip 测试:写入 → 读取 → 字段一致
- 默认值测试:既有 MR 行读出 EquationKey="" / ValueShape="scalar"
- 新表的 CRUD 测试

**验收**:全量回归 0 fail;既有 launcher / pipeline / coverage / trend 测试不动一行仍过(纯数据加列,行为不变)。

### Phase P1 — L0 数学基元算子

**输出**:在 `MetBench_BLL.Core/SystemMT/Transformations/` 加 17 个 `IMRTransformation` 实现,
注册到 `TransformationRegistry`(命名前缀 `Math*` / 聚合类):

| 类别 | 算子 |
|---|---|
| 一元函数 | `MathExp` · `MathLog` · `MathSin` · `MathCos` · `MathSqrt` · `MathAbs` |
| 幂函数 | `MathPow`(底 + 指数) |
| 二/多元 | `MathAdd` · `MathSub` · `MathMul` · `MathDiv` · `MathLinComb`(线性组合) |
| 生成 | `Linspace`(等距序列) |
| 聚合 | `Sum` · `Mean` · `Max` · `Min` |

每个 ~10–20 行,**内部直接调 `System.Math` 或 `MathNet.Numerics`**,不自造算法。

**TDD**:
- 每算子 3–5 个用例(典型值、边界值、错误参数)
- 与 `System.Math` / `MathNet` 直接调用做交叉验证(testify the wrapper, not the underlying)
- `MathLinComb` 多元参数 schema 验证

**验收**:17 个算子单元测试全过;`TransformationRegistry.AvailableNames` 含新 17 个名字。

### Phase P2 — `IEquationFunction` + Registry + Recipe 执行器

**输出**:
- 新建 `MetBench_BLL.Core/Equations/IEquationFunction.cs`(接口)
- 新建 `EquationFunctionRegistry`(keyed by `(EquationKey, Name)`,与 `TransformationRegistry` 平级)
- 新建 `RecipeBasedEquationFunction`(解析 Recipe `compose` 数组 + 占位参数替换 + 串行调 L0 算子)
- 新建 `TransformationResolver.Resolve(MetamorphicRelation mr) → IExecutable`:
  1. 先查通用 `TransformationRegistry` (mr.TransformationName)
  2. 再查 `EquationFunctionRegistry[(mr.EquationKey, mr.TransformationName)]`
  3. 都未命中 → 抛 `UnknownTransformationException`

**TDD**:
- Recipe 解析正确性(占位替换 / 串行顺序 / 中间结果传递)
- Recipe 校验:op 不在 L0 → 拒;占位语法非法 → 拒;参数 schema 缺失 → 拒
- 决策 B 分级查找顺序:通用优先(命名冲突时不被方程命名空间夺走)
- 双重未命中 → 异常带可定位信息(EquationKey + Name)

**验收**:解析器 ≥ 10 测试通过;TransformationResolver ≥ 6 测试通过(三种命中 + 两种未命中 + 一种冲突)。

### Phase P3 — Catalog 服务扩展

**输出**:
- `SystemMtCatalogService.CreateEquationFunction(Recipe)` + 校验链(op 在 registry / 占位语法 / 参数 schema 完整)
- `SystemMtCatalogService.ListEquationFunctions(EquationKey)` / `GetEquationFunction(EquationKey, Name)`
- 待建 `MethodMtCatalogService`(对称 SystemMtCatalogService;强制 `Kind="method-level"`;拒 `MetaPatternCode` 非空)

**TDD**:
- 完整 CRUD 测试(模式同 `SystemMtCatalogServiceTests`)
- 校验拒绝路径每个一个用例
- `MethodMtCatalogService` 拒 MetaPatternCode 一例

**验收**:CRUD 测试全过;`SystemMtCatalogServiceTests` 既有 20 测试不破坏。

### Phase P4 — 样板方程 Bateman 落地

**输出**:
- L2:`MetBench_BLL.Core/Equations/Bateman/BatemanAnalyticSolution.cs`(`IEquationFunction`,
  ~30 行,**用 `System.Math.Exp`**,不写求解器)
- L1 Recipe:`bateman.ScaleInitial` JSON Recipe(Composite × 3 `ScaleField` /initial/{N_A,N_B,N_C}),
  通过 `SystemMtCatalogService.CreateEquationFunction` 入库
- launcher 改:`SystemMtLauncher.BuildMrCatalog` 的 `decay-chain-scale-initial` blueprint
  改用 `TransformationName="ScaleInitial"`(走 EquationFunctionRegistry 命中 L1 Recipe),
  替换当前 `TransformSteps: new[] { 3 个 MrTransformStep }` 的硬编码
- launcher ctor 不再为多步 MR 临时注册 `Composite-<mrId>` —— 由 Recipe 接管

**TDD**:
- `BatemanAnalyticSolution` 单元:对照解析公式核 N_A(10) / N_B(10) / N_C(10),误差 < 1e-9
- Bateman L1 Recipe 通过 `TransformationResolver.Resolve` 返回 `RecipeBasedEquationFunction`
- launcher `decay-chain-scale-initial` 端到端跑(继续过既有 `LauncherEndToEndOdeTests`)
- Recipe 输入 `{factor: "2"}` → 三个 N 被 scale → followup output = source × 2 ± 数值误差

**验收**:端到端测试与 P0–P3 测试 0 fail;`decay-chain-scale-initial` 跑出与改前一致的 Execution+Result。

### Phase P5 — method 侧执行栈对称

**输出**:
- `MetBench_BLL.MethodMT.Transformations.MethodTransformationRegistry`(同 system,查询顺序:通用 → 方程命名空间)
- `MethodAssertionEvaluator`(子集词表:`less` / `greater` / `approx`;噪声相关不含)
- 与 v1 既有 `AutoRunMR` 等 engine 共存,新 method-level MR 走新 registry,既有 LaTeX-pattern MR 走旧路径

**TDD**:
- `MethodTransformationRegistry.Resolve(mr)` 决策 B 顺序
- `MethodAssertionEvaluator` 三态(less/greater/approx)+ 拒 `*-noise-aware`

**验收**:新 registry 测试 ≥ 8 通过;v1 既有 method-level MT 测试不破坏。

### Phase P6 — BDD steps 切 W2 facade

**输出**:重写 `MetBench_SystemMT.Tests/Steps/` 下 7 个 step 文件:
- `HeatEquationAmplitudeSteps.cs` / `OpenMocPinCellNuSigmaFSteps.cs` /
  `OpenMocPinCellSigmaASteps.cs` / `CrossProgramSteps.cs` /
  `SystemLevelCliMtSteps.cs` / `SystemLevelGeneratedFollowupSteps.cs` /
  `ProjectileRangeSteps.cs`

改动:Steps 直 new `MrTransformation` / `SystemMtRunner` / `IMrAssertion` /
`PythonInputAdapter` 的代码全部替换为 `ISystemMtLauncher.RunAsync(mrId, params)`
或 `ISystemMtPipeline.ExecuteAsync(ctx)`。

**TDD**:
- 既有 11 个 `.feature` scenarios 不改一行,全过(BDD 是行为契约,实现切换不破契约)
- W1 类(SystemMtRunner / IMrAssertion / MrTransformation 等)在 Steps 中无 `new` 调用(grep 验证)

**验收**:BDD 测试 11/11 过;系统 MT 不再有"两条执行路径"残留。

### Phase P7 — legacy 路径标记 + 文档同步

**输出**:
- `MetBench_BLL/Latextosympy.cs` 等 LaTeX-pattern → sympy 执行路径加 `[Obsolete("...")]`
- v1 method-level MT 既有 MR 文档说明:既有 MR 继续走 LaTeX 路径(legacy 兼容);
  新 MR 走 `MethodTransformationRegistry` + EquationFunction
- `AGENTS.md` Stage 8 节点更新:本计划完成状态 + 指针
- `mr-architecture.md` 实施序列 §8 标"已落地"

**验收**:`grep -r "[Obsolete]" MetBench_BLL/Latextosympy.cs` 命中;`AGENTS.md` 反映完成。

## 4. 风险与缓解

| 风险 | 概率 | 缓解 |
|---|---|---|
| Recipe 表达力不够覆盖 80% MR(L0 数学基元集不全) | 低 | P1 评审时按 mr-architecture.md §5 列表对齐;若有缺,P1 内补,不留到 P4 |
| Bateman 解析解数值误差超容差导致 P4 测试 flaky | 低 | 用 `System.Math.Exp` 精度足够(double IEEE754 ≈ 15 位);误差容忍设 1e-9 |
| BDD Steps 改写后行为微差(原 W1 vs 新 pipeline 路径细节) | 中 | 在 P3.3.b 切换 launcher 时已验过 facade 契约不变;BDD 走同 facade 应一致;有差直接修测试 |
| 多步 Recipe 性能(解析 + 串行 invoke vs 直接 C# 调用)显著退化 | 低 | L1 Recipe 主要场景是常规缩放(3–5 步内),解析每次 µs 级,SUT 本身秒级 — 不在路径关键路径上 |
| MathNet.Numerics 包版本/许可证问题 | 低 | 已在 `MetBench_BLL.Core.csproj`(per migration-plan P4);许可证 MIT,商用兼容 |

## 5. 不在范围

- L3 集合作为 input/output 本体 / L4 分布形态 —— 设计已 deferred,签名预留,本计划不实现
- WPF UI 端的 方程管理页 / 方程函数管理页 / MR 管理页 —— VM 端工作,需在 cloud P3 落地后做;不属本计划
- W1 dead code 真正删除(步骤 2)—— 独立计划,不在本计划范围
- 新 SUT 接入 —— Stage 8 的代表性 SUT 计划处理,不在本计划
- Discovery 子系统改读 EquationFunction —— P7 之后的 follow-up,本计划不实现

## 6. 工时估计(参考,非硬约束)

| Phase | 工程师天 | 主要消耗 |
|---|---|---|
| P0 schema 入位 | 0.5 | 字段加 + 测试 + DI 注册扩展 |
| P1 L0 数学基元 17 个 | 2 | 每算子 ~15 分钟 + 测试 |
| P2 IEquationFunction + Registry + Recipe 执行器 + Resolver | 1.5 | 接口设计 + 解析器 + 决策 B + 测试 |
| P3 Catalog 扩展 | 1 | CRUD + 校验链 + 测试 |
| P4 Bateman 样板 | 1 | L2 解析解 + L1 Recipe + launcher 改 + 端到端 |
| P5 method 侧对称 | 1 | Registry + AssertionEvaluator + 测试 |
| P6 BDD steps 切 facade | 1.5 | 7 个 step 文件重写 + 回归 |
| P7 legacy 标记 + 文档同步 | 0.5 | Obsolete + AGENTS 回写 |
| **总计** | **9 工程师天** | 不含 code review / PR cycle |

## 7. 闭环回写(执行后填)

| Phase | 状态 | commit | 备注 |
|---|---|---|---|
| P0 | ✅ 已完成(2026-05-23) | `4c85b6c` | 13 TDD 全过,全量回归 680/0fail/0skip。schema 字段添加无破坏,既有数据按默认值兼容。 |
| P1 | ✅ 已完成(2026-05-23) | `16de54b` | 74 TDD 全过,全量回归 754/0fail/0skip。17 L0 数学基元注册入 TransformationRegistry。 |
| P2 | ✅ 已完成(2026-05-23) | `a004ac4` | 18 TDD 全过,全量回归 772/0fail/0skip。IEquationFunction + EquationFunctionRegistry + RecipeBasedEquationFunction + TransformationResolver + UnknownTransformationException。 |
| P3 | ✅ 已完成(2026-05-23) | `f05de05` | 11 TDD 全过,全量回归 783/0fail/0skip。SystemMtCatalogService 加 Recipe CRUD + 校验链；新建 MethodMtCatalogService（Kind=method-level 强制 + MetaPatternCode 拒绝）。 |
| P4 | ✅ 已完成(2026-05-23) | `cc779a9` | 7 新测试全过,全量回归 790/0fail/0skip。BatemanAnalyticSolution L2；bateman.ScaleInitial L1 Recipe；launcher 用 EquationFunctionRegistry + TransformationResolver 取代 Composite；PipelineContext 加 EquationKey/EquationFunctionRegistry。 |
| P5 | ✅ 已完成(2026-05-23) | `80c2317` | 19 TDD 全过,全量回归 809/0fail/0skip。MethodTransformationRegistry（决策 B，委托 TransformationResolver）+ MethodAssertionEvaluator（less/greater/approx，拒噪声感知代码）。 |
| P6 | ✅ 已完成(2026-05-23) | `3991c52` | 809/809 全过，0 fail/0 skip。7 个 step 文件全部移除 W1 引擎类型（SystemMtRunner/MrTransformation/IMrAssertion/PythonInputAdapter）；改用 ISystemMtPipeline.ExecuteAsync(ctx)。新增 example_cli_input_parser.py / example_cli_output_parser.py（TestAssets）和 projectile_input_parser.py / projectile_output_parser.py（SUT/projectile）支持简单 SUT 的 v2 解析协议。Then 步骤适配 PipelineOutcome.SourceMetrics/FollowupMetrics/AssertionResult。grep 确认 Steps/ 无 W1 new 调用。 |
| P7 | ✅ 已完成(2026-05-23) | `17a6093` | `[Obsolete]` 加 Latextosympy.cs + Latextosympy_Await.cs；AGENTS.md 指针回写；mr-architecture.md §8 实施序列全标 ✅ + landing status note。全量回归 809/809 全过。 |

执行后须更新本表 + AGENTS.md Stage 8 + mr-architecture.md §8 实施序列状态。
