# MR 协议与执行分层(v2 设计基线)

> **版本**:1.0 · 2026-05-22 · **状态**:基线
> **目标读者**:MR 库扩展者、新方程接入者、项目接手人
> **入口**:本文档定义 MR 在 method-level / system-level 两套执行栈下的
> 协议、分层、注册规则与边界。任何 MR / 方程函数 / 转换算子的新增,必须
> 与本文档对齐。

---

## 1. 范围与目的

本文档收口 MR(metamorphic relation)层的架构决策,覆盖:

- 方法级 MT 与系统级 MT 双轨并存的合理性与边界
- MR 的三个核心概念(输入转换 / 输出关系 / 断言)在协议层共享、在执行层分轨的方式
- 数学物理方程作为数学函数容器的角色
- 转换算子的 L0 / L1 / L2 分层(通用 / 用户级 Recipe / 工程级 C#)
- 注册与查找规则
- 集合形态的覆盖与延迟扩展面

不覆盖:

- 具体方程的 L2 算子实现细节(各方程文件内就近注释)
- 单个 MR 的物理论证(由 MR 的 `Description` + 相关论文链接承载)
- WPF UI 形态(由 `docs/uat/` + 客户端项目自身文档承载)

---

## 2. 双轨决策

### 2.1 method × system 双轨并存

**决策**:方法级 MT 与系统级 MT 使用不同的执行栈;不试图统一为一套。

| 维度 | 方法级(v1) | 系统级(v2) |
|---|---|---|
| 被测对象 | C# 方法 / 函数 | 整个 SUT 程序(subprocess) |
| 执行机制 | 反射 / 直调 in-proc | `SystemMtPipeline` + Python subprocess |
| 典型耗时 | < 1 ms | 秒至分钟 |
| 测试栈 | xUnit `[Theory]` / `[Fact]` | xUnit(单元/集成)+ Reqnroll BDD `.feature` |

**理由**:method 级的核心价值是 in-proc 快速验证(TDD inner-loop 秒级)。subprocess 启动 + 文件 IO 是物理开销,把 method 级也强行 subprocess 化会让单元测试套从秒升到小时,违背 method 级 MT 的存在意义。两类 MR 的颗粒度与使用场景天然不同,分轨而非合并。

**对比方案(已否决)**:把方法级 SUT 编译为 CLI 可执行文件、全部走 system 流程 —— 性能塌方 + 每个 method 要写 CLI wrapper + 失去 xUnit 工具链红利。

### 2.2 共享 MR 协议层

**决策**:`MetamorphicRelation` 实体跨 Kind 共用 schema,协议层(词表)共享。

- `MetBench_Domain.MetamorphicRelation` 一张表承载两种 Kind 的 MR,`Kind` 字段("method-level" / "system-level")分流。
- 共享字段:`Code` · `Description` · `TransformationName` · `AssertionTypeCode` · `ValueName` · `ToleranceRel` · `NoiseMultiplier` · `EquationKey` · `MetaPatternCode`(仅 system-level 填,见 §2.3)。
- 执行层分轨:每个 Kind 自己的 registry 把协议名解析到本 Kind 的 impl(见 §5)。

**效果**:Discovery 提议 / Coverage 统计 / Reporting / MR 库 CRUD 在协议层工作,不需要知道执行栈差异;只在执行时按 Kind 分派。

### 2.3 MetaPattern 仅 system-level

**决策**:`MetamorphicRelation.MetaPatternCode` 字段仅 `Kind="system-level"` 行可填;`Kind="method-level"` 行必须留空。

**理由**:NOETHER 元模式(`m_inv` / `m_mono` / `m_conv` / `m_cmp` / `m_adj` / `m_rev` / `m_dyn` / `m_rel`)是**程序级行为不变性**的抽象,描述 SUT 整体可观察行为的不变量。方法级 MR 描述的是函数代数关系,粒度上没有"元模式"层,强行附会会污染 Coverage 统计与 Discovery 检索。

**执行点**:`SystemMtCatalogService.CreateMr`(已强制 Kind=system-level)、待建 `MethodMtCatalogService.CreateMr`(强制 Kind=method-level + MetaPatternCode 必空)。

---

## 3. 三概念分层(语义 / 协议 / 执行)

MR 的三个核心概念在三个层次上的归属:

| 层 | 跨 Kind 共享? | 内容 |
|---|---|---|
| **语义层**(MR 是什么) | ✅ 完全共享 | `MetamorphicRelation` 实体行的字段语义。"输入转换是什么"、"输出关系是什么"、"断言怎么判定" 在两套 Kind 下含义一致。 |
| **协议层**(三概念的名字与契约) | ✅ 完全共享 | 词表(transformation names + assertion type codes)、参数 schema、容差语义。同一名字在两套 Kind 下指代同一语义操作。 |
| **执行层**(怎么把名字落到代码) | ❌ 分轨 | system 用 `IMRTransformation` + `JsonPointerResolver` 在 dict 上做;method 用反射 + `IEquationFunction` 直调。`AssertionEvaluator` 同理:system 走 FA-on-`AssertionInput`,method 走 FA-on-return-value。 |

### 3.1 输入转换

- **语义**:`Transform(source, params) → followup`,确定性、可重放。
- **协议**:`TransformationName: string` + `Parameters: Dictionary<string, string>`。
- **执行**:
  - system:`IMRTransformation.Apply(dict, targetFieldPath, params) → dict`
  - method:`IEquationFunction.Apply(input, params) → output`(同一接口,数据形态不同)

### 3.2 输出关系

- **语义**:`Relation(sourceOut, followupOut, params) → bool`。当前协议层把它表达为 `(SourceValue, FollowupValue, ValueName, ToleranceConfig) → pass/fail`。
- **协议**:`AssertionTypeCode: string`(参见 `AssertionTypeCodes` 常量集)+ `ValueName: string` + 容差三件套(`ToleranceRel` / `ToleranceAbs` / `NoiseMultiplier`)。
- **执行**:
  - system:`AssertionEvaluator.Evaluate(AssertionInput, ToleranceConfig, code)`
  - method:`MethodAssertionEvaluator.Evaluate(sourceReturn, followupReturn, ToleranceConfig, code)`(待建,词表是 system 词表的子集)

### 3.3 断言

- **语义**:关系成立 + 容差范围内 → pass;否则 fail。
- **协议**:与 §3.2 同一组协议字段。
- **执行**:FluentAssertions 在两套 Kind 下都可用(system 端已用扩展方法 + AssertionEvaluator;method 端直接 `.Should()`)。

---

## 4. 数学物理方程作为数学函数容器

### 4.1 决策

**MR 行不再藏数学函数实现**。方程实体(`EquationMetadata`)承载本方程的数学函数集合,MR 行仅声明"用哪个函数 + 怎么断言"。

LaTeX 字段(`InputPattern` / `OutputPattern` / `EquationFunctionDescriptor.DisplayLatex`)**仅用于展示**(论文 / UI / 报表),**不驱动执行**。`Latextosympy.cs` 旧路径标 `[Obsolete]`,新 MR 不再依赖。

**理由**:

- 同一方程的多个 MR 共享同一组数学函数(Bateman 的解析解、初值缩放、积分守恒...),放在方程上一份实现、多 MR 复用,优于在每个 MR 行藏一份 LaTeX-pattern 复制。
- LaTeX 一身两用(执行 + 展示)是耦合,执行回归到强类型函数 + 注册表后,LaTeX 回归本职。
- 跨 Kind 共享:同一函数(如 `bateman.ScaleInitial`)既用于验证 method 端函数实现,也用于驱动 system 端 SUT 输入变换。

### 4.2 实体形态

```
EquationMetadata {
  EquationKey: "bateman"
  Name: "Bateman decay chain"
  CanonicalForm: "dN/dt = -λN + ..."     // LaTeX, 仅展示
  SymbolSystem: ...
  Parameters: ...
  Functions: List<EquationFunctionDescriptor>  // ← 函数库
}

EquationFunctionDescriptor {
  EquationKey: "bateman"
  Name: "ScaleInitial"                    // 方程命名空间内唯一
  Signature: "(initial: {N_A,N_B,N_C}, factor: real) → {N_A,N_B,N_C}"
  Kind: "transformation" | "solution" | "invariant" | ...
  DisplayLatex: "\\hat N_i = \\alpha \\cdot N_i"   // 仅展示
  // 二选一:
  ImplKey: "bateman.ScaleInitial"          // L2: → C# IEquationFunction
  Recipe: { compose: [...], parameterSchema: [...] }   // L1: 数据驱动
}

MetamorphicRelation {
  Code, Description, Kind,
  EquationKey,                            // ← 新增,指向方程
  TransformationName,                     // ← 在方程函数库 + 通用 registry 中解析
  AssertionTypeCode, ValueName, Tolerance...,
  MetaPatternCode (system-level only)
}
```

---

## 5. 转换算子 L0 / L1 / L2 分层

| 层 | 谁写 | 写在哪 | 添加方式 | 例子 |
|---|---|---|---|---|
| **L0 通用原子算子** | 工程师(罕见,跨方程通用) | C# `IMRTransformation` + `TransformationRegistry` | 编译期注册 | `ScaleField` / `TranslateField` / `PermuteIndices` / `MirrorAxis` / `IdentityTransform` / `CompositeTransform` / 数学基础(`Negate` / `Add` / `Multiply` / `Power`) |
| **L1 方程级"组合函数"** | **普通研究者**(UI / JSON) | 数据库 `EquationFunctionRecipe` 表 | **无代码,在 UI / 导入 JSON** | `bateman.ScaleInitial` = `Composite[ScaleField /initial/{N_A,N_B,N_C}]`;`heat_eq.ScaleAmplitude` = `ScaleField /initial/amplitude` |
| **L2 方程专属"特殊算子"** | 工程师(罕见,需新数学) | C# `IEquationFunction` + 方程命名空间 | 编译期注册 | `bateman.AnalyticSolution`(解析三核素解);`openmoc.ScaleFuelAbsorption`(`sigma_t` 跨字段一致性) |

### 5.1 L1 Recipe 受控表达力

Recipe 用 JSON 表达 L0 原子算子的组合 + 参数绑定。**不引入任意表达式 / 反射 / IO** —— 这避免了"代码-在-数据"的安全/调试灾难。

```json
{
  "EquationKey": "bateman",
  "FunctionName": "ScaleInitial",
  "Kind": "composition",
  "Recipe": {
    "compose": [
      { "op": "ScaleField", "path": "/initial/N_A", "params": { "factor": "{factor}" } },
      { "op": "ScaleField", "path": "/initial/N_B", "params": { "factor": "{factor}" } },
      { "op": "ScaleField", "path": "/initial/N_C", "params": { "factor": "{factor}" } }
    ]
  },
  "ParameterSchema": [
    { "name": "factor", "type": "real", "required": true, "constraint": "> 0" }
  ],
  "DisplayLatex": "\\hat N_i = \\alpha \\cdot N_i"
}
```

**入库校验**(由 catalog service 强制):
- `op` 必须在 L0 `TransformationRegistry` 中存在
- 占位语法只允许 `{name}`,name 必须在 `ParameterSchema` 中声明
- 参数 schema 必须完整(name / type / required / constraint)

**Runtime**:`RecipeBasedEquationFunction.Apply(input, params)` 解析 `compose` → 按顺序调 `TransformationRegistry.Get(op)` → 用 params 替换占位 → 串行执行。零反射 / 零脚本求值。

### 5.2 添加新方程的两条路径

| 路径 | 适用 | 步骤 | 是否需代码 |
|---|---|---|---|
| **P1**(数据驱动) | 普通研究者;常规缩放/平移/排列类 MR | ① UI 添方程(EquationMetadata) → ② UI 添方程函数(Recipe,选 L0 算子 + 参数绑定) → ③ UI 添 MR(选方程 + 函数 + 断言码 + 容差)→ ④(可选)绑 SUT | **否** |
| **P2**(代码驱动) | 工程师;新数学算子 / 跨字段物理一致性 / 解析解 | 写 C# `IEquationFunction` impl + `[EquationFunction("...")]` 标注 + 编译入库 | **是**(每方程一次性投入,所有 MR 复用) |

**对 Stage 8 84 候选 MR 库的预测**:绝大多数(线性缩放系)落 P1 路径,L0 + Recipe 够用;3-4 个高级 MR 落 P2(每方程的解析解 / 守恒律 / 物理一致性)。

---

## 6. 注册与查找规则【决策 B】

**决策**:`MetamorphicRelation.TransformationName` 的解析顺序为:

1. **先查通用 L0** `TransformationRegistry`(`ScaleField` / `PermuteIndices` / `MirrorAxis` / ...)
2. **再查方程命名空间** `EquationFunctionRegistry[mr.EquationKey, mr.TransformationName]`(L1 Recipe 或 L2 C#)

**理由**:

- **保留跨方程通用名**:`ScaleField` 这种基础算子在任何方程下都是缩放,不该强制 fully-qualified(用户不必写 `bateman.ScaleField`)。
- **允许领域专属名**:`ScaleInitial` / `AnalyticSolution` 等只在该方程语境下有意义,放在方程命名空间避免污染全局。
- **命名冲突时通用优先**:若用户在方程命名空间下注册 `ScaleField`(同名通用),通用解析优先,避免领域名意外覆盖跨方程含义(典型类型系统的"小作用域不夺大作用域"原则的反用 —— 这里我们要的是名字稳定性,不是 shadowing)。
- **无 fully-qualified 负担**:MR 行只填 `TransformationName: "ScaleInitial"`,runtime 按 EquationKey 上下文解析,使用方零心智成本。

**实现点**:`TransformationResolver.Resolve(MetamorphicRelation mr) → IExecutable`(待建),作为 system / method 两侧 registry 的共同前置。Catalog service `CreateMr` / `UpdateMr` 必须调用 `Resolve(mr)` 校验 —— 解析失败拒绝入库。

**审计**:`Resolve` 命中哪个层应记录(`generic` / `equation:<key>`),便于 Coverage 统计与跨方程算子使用度量。

---

## 7. 集合形态边界

MR 的输入 / 输出 / 关系在四个集合层次上的覆盖:

| 层次 | 含义 | 输入转换 | 输出关系 | 状态 |
|---|---|---|---|---|
| **L1 字段内的数组/向量** | 一个 input/output 内某字段是数组 | ✅ `ScaleField` IList 分支 / `PermuteIndices` / `MirrorAxis` | ✅ `FluxPointwiseApprox` | **覆盖** |
| **L2 嵌套字段(dict of arrays / field on mesh)** | output 是网格上场;materials.fuel.{sigma_t, ...} | ✅ `JsonPointerResolver` + L1 在叶子上做 | ◐ 拍平到 `name_i` 形式塞 `ExtraAssertionValues` | **覆盖** |
| **L3 集合作为 input/output 本体** | input 是 N 个 sample / N 个 sweep 点 | ❌ `IMRTransformation` 签名只支持单 dict | ❌ `AssertionInput` 只支持 scalar pair | **deferred(Stage 8/9)** |
| **L4 分布/概率集合** | 同输入多次 MC → 分布 | ❌ | ◐ noise-aware 近似 | **deferred(Stage 8/9)** |

### 7.1 L3 / L4 延迟扩展面

为 L3 / L4 预留(未实现)的接口签名:

```csharp
// L3: 集合形态转换
public interface IMRSetTransformation
{
    IList<IReadOnlyDictionary<string, object?>> ApplySet(
        IList<IReadOnlyDictionary<string, object?>> sources,
        IReadOnlyDictionary<string, string> parameters);
}

// L3 / L4: 集合形态断言
public sealed record SetAssertionInput(
    IList<double> SourceCollection,
    IList<double> FollowupCollection,
    string ValueName,
    IReadOnlyDictionary<string, double>? ExtraValues);
```

`PipelineContext` 与 `PipelineOutcome` 在 L3 落地时需加 `IsSetMr: bool` + `SourceCasePaths: IList<string>` / `SourceOutputs: IList<...>`。状态机 fan-out 沿 collection 维度展开。

### 7.2 `MetamorphicRelation.ValueShape` 字段(预留)

为支持集合形态的演化,`MetamorphicRelation` 加 `ValueShape: string` 字段,取值:
- `"scalar"`(默认,当前 8 MR 全用此)
- `"vector"` / `"field"` / `"sample-set"` / `"case-set"`

预留即可,L3 / L4 落地时再启用。当前所有 MR 默认 `"scalar"`。

---

## 8. 实施序列(对接 plan)

下列序列是 MR 协议层落地的最小依赖图,不替代具体 phase 排期(由 `docs/superpowers/plans/` 持有):

1. **schema 入位**(可独立 commit):
   - `MetamorphicRelation` 加 `EquationKey: string` + `ValueShape: string`(默认 `"scalar"`)
   - 新建 `EquationFunctionRecipe` 实体表 + IDAL/DAL
   - `EquationMetadata.Functions: List<EquationFunctionDescriptor>` 字段

2. **接口与 registry**:
   - 新建 `IEquationFunction` 接口
   - 新建 `EquationFunctionRegistry`(keyed by `(EquationKey, Name)`)
   - 新建 `RecipeBasedEquationFunction`(从 Recipe 解析 L0 组合)
   - 新建 `TransformationResolver.Resolve(mr)`(决策 B:通用先、方程命名空间后)

3. **CRUD 扩展**:
   - `SystemMtCatalogService.CreateEquationFunction(Recipe)` + Recipe 校验
   - 待建 `MethodMtCatalogService`(对称 system,强制 Kind=method-level,拒绝 MetaPatternCode)

4. **样板方程**(推荐 Bateman):
   - L2 `bateman.AnalyticSolution`(C# `IEquationFunction`)
   - L1 `bateman.ScaleInitial`(Recipe,Composite × 3 ScaleField)
   - 改 launcher `decay-chain-scale-initial` MR 引用 `bateman.ScaleInitial`,替代当前内嵌的 3 步 CompositeTransform

5. **WPF 端**(VM 编译验证):
   - 方程管理页 / 方程函数管理页 / MR 管理页
   - 调 `*CatalogService` API

6. **legacy 路径标记**:
   - `Latextosympy.cs` + LaTeX-pattern → sympy 执行路径标 `[Obsolete]`,新 MR 不再走此

---

## 9. 不做(避免过度设计)

| 不做 | 原因 |
|---|---|
| Recipe 引入表达式 DSL(Roslyn / Lua / sympy 等任意求值) | 安全/调试/类型 — 受控原子算子组合够用 |
| L2 算子的插件 DLL 热加载 | 静态注册足够,WPF/CLI 无热加载需求 |
| 跨方程 MR(同一 MR 实例引用多个 EquationKey) | 罕见需求,等真实场景出现再设计 |
| 强行统一 method × system 执行栈 | 性能与 TDD 节奏的硬约束,见 §2.1 |
| 在 MR 行同时支持 LaTeX-pattern 执行与 EquationFunction 执行(双驱动) | 复杂度爆炸;旧路径标 Obsolete 自然死亡更经济 |
| L3 / L4 集合形态原生支持(当前) | 现阶段需求未到,见 §7;签名预留,需求出现再实现 |

---

## 10. 与其他设计文档的关系

| 文档 | 关系 |
|---|---|
| [`v2-system-mt-architecture.md`](v2-system-mt-architecture.md) | 整体架构基线;本文档是 MR 层细化 |
| [`entity-model.md`](entity-model.md) | 实体 schema;本文档涉及 `MetamorphicRelation` 新字段 + `EquationFunctionRecipe` 新表,须同步更新 |
| [`assertion-extensions.md`](assertion-extensions.md) | 断言扩展方法 API;本文档 §3.2-3.3 协议层使用其代码集 |
| [`glossary.md`](glossary.md) | 术语表;新增术语(L0/L1/L2 算子、Recipe、EquationFunction)须落到 glossary |
| [`migration-plan.md`](migration-plan.md) | 8 周路线;P4/P5 与本文档 §8 实施序列有交集 |
| `docs/superpowers/plans/2026-05-22-systemmt-engine-unification-plan.md` | W2 替代 W1 执行计划;本文档是其后续 MR 层基线 |

---

**本文档与 `v2-system-mt-architecture.md` 同级,作为后续 MR / 方程函数 / 算子注册类工作的依据。任何与本文档相左的实现需 RFC PR 修改本文件。**
