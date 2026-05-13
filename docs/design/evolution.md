# MetBench 从最初版到当前 v2 设计的演化纪实

> **目标读者**：项目交接者、论文作者、协作者、未来研究生
> **版本范围**：commit `ec7f658` (2026-05-07) → commit `b8401fd` (2026-05-13)
> **文档定位**：纵贯式回顾，不是横截式设计；与 `docs/design/v2-system-mt-architecture.md`（v2 横截设计）配套

---

## 0. 摘要 — 一图概览

```
2026-05-07              2026-05-08         2026-05-09 ─ 2026-05-11        2026-05-12          2026-05-13
   │                        │                     │                            │                    │
   ▼                        ▼                     ▼                            ▼                    ▼
┌────────┐   ┌──────────┐   ┌─────────┐  ┌──────────────┐   ┌─────────────────────┐   ┌────────────┐
│ v1.0   │ → │ Stage 1  │ → │ Stage 2 │→ │ Stage 3 / 3+ │ → │ Stage 4              │ → │ Stage 5    │
│ method │   │ BDD      │   │ Input   │  │ OpenMOC      │   │ 平台 + WPF Launch    │   │ 实证研究    │
│-level  │   │ 系统级    │   │ Gen     │  │ ScaleNuSigF  │   │ + LiteDB + OpenMC    │   │ + NOETHER  │
│ MT     │   │ MT 闭环  │   │         │  │ + SigmaA     │   │ + heat-equation      │   │ + 真实 bug │
│ 教学    │   │          │   │         │  │              │   │                      │   │ + dashboard│
└────────┘   └──────────┘   └─────────┘  └──────────────┘   └──────────────────────┘   └────────────┘
                                                                                              │
                                                                                              ▼
                                                                                       ┌──────────────┐
                                                                                       │ v2 设计      │
                                                                                       │ (2026-05-13) │
                                                                                       │ C# 编排回归   │
                                                                                       │ + 23 LiteDB  │
                                                                                       │ + 4 级 MR    │
                                                                                       │ + Discovery  │
                                                                                       │ + Mutation   │
                                                                                       └──────────────┘
```

### 六段演化结论

1. **v1.0 是方法级 MT 教学工具**——WPF + LiteDB + HandyControl 经典三层；与系统级 MT 无直接关联。
2. **Stage 1-4 在 v1.0 之上加了一条平行的"系统级 MT pipeline"**——C# 编排 + Python adapter，确立了"BDD-driven system-level MT"叙事。
3. **Stage 5 把研究主线推到 Python 矩阵脚本里**——C# 编排被绕过，pipeline 出现"两套并行"漂移。
4. **AI 编程让两个方向都加速演化，但加深了漂移**——AI 在 Python 研究代码上的产能 5-10× 于 C# scaffolding，让团队习惯了"加新 MR 走 Python"。
5. **v2 设计回归 C# 编排 + LiteDB 中心化**，把两套系统统一在 3NF 数据模型 + 模块化 C# 业务层上。
6. **HandyControl 仍在 6 个 v1 XAML 文件中**，是 v1.0 投资的遗留；v2 不强制移除但建议分阶段替换。

---

## 1. v1.0 最初版（commit `ec7f658`，2026-05-07 21:38）

### 1.1 项目定位

**方法级 metamorphic testing 教学工具**。被测对象是 C# 方法（函数），不是外部程序。用户通过 WPF UI 选 MR、配置参数、运行检测、看图表。

### 1.2 技术栈

| 层 | 选型 |
|---|------|
| UI | WPF + **HandyControl**（含 `hc:Pagination`）+ LiveCharts + ~~Wpf.Ui~~（后期加入） |
| 业务编排 | C# 经典三层 `MetBench_BLL`（90+ 文件，~10000 行） |
| 持久化 | LiteDB + 经典 DAL/IDAL 模式 (`MetBench_DAL`、`MetBench_IDAL`) |
| 实体 | `MetBench_Domain` 三个核心类：`MetamorphicRelation` / `Application` / `Domain` |
| 数据库文件 | `MR.litedb` |

### 1.3 分页功能（HandyControl Pagination）

`hc:Pagination` 在 6 个 XAML 文件使用：

```
MetBench_Client/Views/Pages/
├── ApplicationManagementPage.xaml
├── AutoDetectMRPage.xaml
├── DomainManagementPage.xaml
├── MRDisplayPage.xaml
├── MRManagementPage.xaml
└── MRRecommendationPage.xaml
```

典型用法（来自 `MRManagementPage.xaml`）：

```xml
<hc:Pagination x:Name="pagination" Margin=" 0 2 5 0" Height="30"
               IsJumpEnabled="False" Visibility="Visible"
               HorizontalAlignment="Right"
               MaxPageCount="{Binding ViewModel.MaxPageCount,UpdateSourceTrigger=PropertyChanged}"
               PageIndex="{Binding ViewModel.PageIndex,UpdateSourceTrigger=PropertyChanged,Mode=TwoWay}" />
```

**用途**：MR / Application / Domain 列表分页展示。

### 1.4 系统 MT pipeline：⚠ 不存在

v1.0 **没有系统级 MT 概念**。所有 MT 在 C# 进程内调函数完成：

```
[v1.0 方法级 MT 数据流]

用户 (WPF) → MTExecutionPage → AutoRunMR_Await
                                      │
                                      ▼
                            FunctionProgram.Execute() (C# 进程内反射)
                                      │
                                      ▼
                            MRDetector / DetectionResult
                                      │
                                      ▼
                            LiveCharts 渲染 + ExcelTestReport / HTMLTestReport
                                      │
                                      ▼
                            LiteDB 持久化
```

**关键观察**：被测程序是 C# 方法对象，不存在跨进程边界，不存在文件 IO，不存在 SUT 概念。

### 1.5 设计意图

- **教学价值优先**：演示"经典工程化软件"的姿态（DAL 模式、DI、MVVM、WPF）
- **研究价值次之**：MR 推荐 / 自动检测算法（`AutoMRAlgorithm.cs` 482 行、`AutoMRParser.cs` 546 行）
- **范围明确**：method-level only；系统级是未来工作

### 1.6 关键文件清单（v1.0 baseline）

```
MetBench_Domain/
├── MetamorphicRelation.cs   ← 蜕变关系实体（含 ApplicationName 多值字符串）
├── Application.cs            ← 应用程序实体（含 DomainName 多值字符串）
└── Domain.cs                 ← 领域实体

MetBench_BLL/                 ← 90+ 文件
├── AutoMRAlgorithm.cs        (482 lines)
├── AutoMRParser.cs           (546 lines)
├── AutoRunMR_Await.cs        (107 lines)
├── FunctionProgram.cs        (93 lines)
├── MRDetector.cs / MRDetectorCollection.cs
├── HTMLTestReport.cs / ExcelTestReport.cs
└── ...

MetBench_DAL/
├── DbConfig.cs               ← LiteDB 配置 + collection 注册
├── DomainRepository.cs
├── ApplicationRepository.cs
└── MetamorphicRelationRepository.cs
```

### 1.7 关键 commit

- `ec7f658` Initial MetBench project baseline (2026-05-07 21:38:47)

---

## 2. Stage 1（2026-05-07 → 2026-05-08）— BDD 系统级 MT 引入

### 2.1 演化目标

**把 MR 从方法级扩展到系统级**——被测对象从 C# 方法换成外部 CLI 程序，输入输出走文件，MT 流程通过 Gherkin/Reqnroll BDD 表达。

### 2.2 重大变更

| 变更 | 影响 |
|------|------|
| 新增 `MetBench_BLL.Core`（net8.0 跨平台 lib） | 把系统级 MT 业务层独立出来，与 net8.0-windows WPF 解耦 |
| 引入 **Reqnroll** BDD 框架 | `.feature` 文件 + step bindings 表达 MR scenario |
| 新增 `MetBench_SystemMT.Tests` 项目 | xUnit + Reqnroll，CI 跑 |
| **确立 Python adapter 模式** | Python 仅处理输入输出文件解析；workflow 留 C# |
| 新增 5 个 C# 核心类承担系统级 MT | `SystemMtTask` / `CliProgramRunner` / `PythonOutputAdapter` / `GreaterThanAssertion` / `SystemMtRunner` |
| 跨平台 build | `chore: enable cross-platform build of the full solution` 让 Linux CI 跑 BLL.Core 子集 |

### 2.3 系统 MT Pipeline v0.1

```
[Stage 1 — v0.1 pipeline]

  .feature 文件 (Gherkin)
       │
       ▼
  Reqnroll step bindings (C#)
       │
       ▼
  SystemMtTask (源 case + followup case + assertion 名)
       │
       ▼
  SystemMtRunner.RunAsync()
       ├─→ CliProgramRunner.RunAsync(source)  → source.out
       ├─→ CliProgramRunner.RunAsync(followup) → followup.out
       ├─→ PythonOutputAdapter.ParseAsync(source.out)
       ├─→ PythonOutputAdapter.ParseAsync(followup.out)
       └─→ GreaterThanAssertion.Evaluate(source, followup)
                      │
                      ▼
              pass/fail
```

**Pipeline 边界**：
- 输入 case 文件**由 .feature 静态指定**（无 followup 生成）
- assertion **只有 GreaterThan 一种**
- adapter **只有 OutputAdapter**（无 InputAdapter）
- **无持久化**（结果只在内存 + 控制台）

### 2.4 设计说明

- **Python 适配器模式**：在 `SystemMtTask.OutputAdapterPath` 字段存 Python 脚本路径；C# 通过 `Process.Start` 调用，约定 stdin/stdout JSON 协议。
- **BDD 一等公民**：MR 表达落在 `.feature` 文件，研究者写 Gherkin 描述源-后继关系，Reqnroll 自动调度。
- **"workflow 在 C#"原则**：Python 不做 control flow，只做数据搬运 — 这条原则在后续阶段被压力测试。

### 2.5 关键 commit

```
df3b8a5  docs: add system-level MT BDD design spec
8ca152a  docs: add system-level MT BDD implementation plan
ec4d8ad  test: add system-level MT BDD harness
bb8e669  feat: add system-level MT task models
6337abb  feat: add CLI runner for system-level MT
35588b8  feat: add Python output adapter invoker
321c228  feat: add greater-than system MT assertion
995b0ad  feat: add system-level MT runner
6e49755  test: wire system-level MT Reqnroll steps
8fb00a1  chore: register system-level MT services
591f32d  chore: enable cross-platform build of the full solution
25f35ff  Merge pull request #1 (Stage 1 完成)
```

---

## 3. Stage 2（2026-05-08）— 输入数据生成

### 3.1 演化目标

**自动从源 case 生成 followup case**。Stage 1 要求用户手写两个 case 文件，繁琐且易出错；Stage 2 让 Python 端做"输入变换"，C# 提供编排。

### 3.2 重大变更

| 变更 | 影响 |
|------|------|
| 引入 `MrTransformation(Name, Parameters)` IR | MR 的"输入变换部分"被抽象为不可变 record |
| 新增 `InputGenerator` 服务 | C# 编排"transform-input"调用 |
| 新增 `PythonInputAdapter` | C# 调 Python 脚本做实际变换 |
| 扩展 `SystemMtTask` 字段 | 加 `InputTransformation` 与 `GeneratedFollowUpInputPath` |
| 适配器约定升级 | Python 适配器同时支持 `parse-output` 和 `transform-input` 子命令 |
| 新 .feature 案例 | `SystemLevelGeneratedFollowup.feature` 验证从 source 自动生成 followup |

### 3.3 系统 MT Pipeline v0.2

```
[Stage 2 — v0.2 pipeline] (新增 InputGenerator 节点)

  .feature
       │
       ▼
  Reqnroll → SystemMtTask (含 MrTransformation)
       │
       ▼
  SystemMtRunner.RunAsync()
       ├─→ ★ InputGenerator.GenerateAsync()
       │        ├─→ PythonInputAdapter.TransformInputAsync()  ← 新
       │        │        └─→ Python adapter "transform-input"
       │        └─→ followup.in
       │
       ├─→ CliProgramRunner(source.in)
       ├─→ CliProgramRunner(followup.in)
       ├─→ PythonOutputAdapter.ParseAsync × 2
       └─→ GreaterThanAssertion.Evaluate()
```

**Pipeline 边界**：
- 源 case 仍由用户提供；followup **自动从 source 生成**
- `MrTransformation` 是不可变 record（`d98b90a fix(stage2): make MrTransformation.Parameters truly immutable`）
- 失败处理：`InputGenerationResult` record 携带 `Succeeded` + `FailureReason`

### 3.4 设计说明

- **`MrTransformation` 是首个"MR IR"形态**：仅 `Name` + `Parameters` 两字段；几年后这成为 v2 `MRInstance.ParameterOverrides` 的雏形。
- **Python adapter 双角色**：同一脚本可被以 `transform-input` 调用（生成 followup）也可被以 `parse-output` 调用（解析输出）。这种"多角色适配器"在 Stage 3 之后被拆分。
- **`InputGenerator` 是 v0.2 的关键 pipeline 节点**：把"输入变换"形式化为一个 step，pipeline 状态机延伸。

### 3.5 关键 commit

```
5db862d  docs: add Stage 2 (input data generation) implementation plan
72855f7  feat(stage2): add MrTransformation configuration type
d98b90a  fix(stage2): make MrTransformation.Parameters truly immutable
8594b25  feat(stage2): add InputGenerationResult record
051460f  feat(stage2): add transform-input subcommand to example adapter
ee8db0e  feat(stage2): add PythonInputAdapter for transform-input invocations
6dc969c  feat(stage2): add InputGenerator orchestrator
6d83a2e  feat(stage2): allow SystemMtTask to carry an MR transformation
a4f71ab  feat(stage2): generate follow-up inputs from MrTransformation in SystemMtRunner
3cde602  test(stage2): add BDD scenario for generated follow-up input
9b65483  chore(stage2): register PythonInputAdapter and InputGenerator factory
efe3e01  Merge pull request #4 (Stage 2 完成)
```

---

## 4. Stage 3 / 3+（2026-05-08）— OpenMOC 真实科学计算 SUT

### 4.1 演化目标

**把 Stage 1-2 框架应用到真实科学计算程序**。被测对象第一次从 demo 程序（projectile-range）变成真实 nuclear transport solver（OpenMOC pin-cell 2D）。

### 4.2 重大变更

#### Stage 3 — OpenMOC ScaleNuSigmaF MR（2026-05-08 上午）

| 变更 | 影响 |
|------|------|
| `SUT/openmoc/openmoc_runner.py` | OpenMOC CLI runner（Python 包装器，调用 openmoc 库） |
| `SUT/openmoc/openmoc_input_adapter.py` | `ScaleNuSigmaF` transform-input |
| `SUT/openmoc/openmoc_output_adapter.py` | k_eff 解析 |
| `SUT/openmoc/sample/pincell.json` | 2-group pin-cell 案例 |
| `OpenMocPinCellNuSigmaF.feature` | 第一个真实物理 MR scenario |
| `.claude/web-setup.sh` | Linux cloud OpenMOC venv 一键安装脚本（解决跨平台部署） |

#### Stage 3+ — OpenMOC ScaleFuelSigmaA MR + IMrAssertion 接口（2026-05-08 下午）

| 变更 | 影响 |
|------|------|
| `IMrAssertion` 接口 | 抽象 GreaterThanAssertion + LessThanAssertion 两种方向 |
| `LessThanAssertion` 类 | 双向 MR 成为可能（"scaling 增加 ⇒ k 降低"） |
| `SystemMtRunner` 重构 | 接受 `IEnumerable<IMrAssertion>` + assertion 注册表 |
| `openmoc_input_adapter_sigma_a.py` | `ScaleFuelSigmaA` transform-input |
| `OpenMocPinCellSigmaA.feature` | 反向 MR scenario |

### 4.3 系统 MT Pipeline v0.3

```
[Stage 3 / 3+ — v0.3 pipeline] (assertion 注册表化 + 真实物理 SUT)

  .feature (OpenMocPinCellNuSigmaF / OpenMocPinCellSigmaA)
       │
       ▼
  Reqnroll → SystemMtTask
              ├─ InputTransformation: ScaleNuSigmaF 或 ScaleFuelSigmaA
              └─ AssertionName: "GreaterThan" 或 "LessThan"
       │
       ▼
  SystemMtRunner (assertion 注册表 by name)
       ├─→ InputGenerator → PythonInputAdapter ★ 现在调 openmoc_input_adapter*.py
       │                                          ↓
       │                                  对 sigma_t / sigma_f / sigma_a / nu_sigma_f
       │                                  / chi / sigma_s 字段做 scale
       ├─→ CliProgramRunner → openmoc_runner.py → k_eff
       ├─→ PythonOutputAdapter → openmoc_output_adapter.py 解析 k_eff
       └─→ assertions[name].Evaluate()
              ├─ GreaterThanAssertion: k_followup > k_source
              └─ LessThanAssertion:    k_followup < k_source
```

**Pipeline 边界**：
- 第一次跑通**真实科学计算 SUT** — OpenMOC pin-cell deterministic transport
- 第一次有**双向 MR**（greater / less）
- assertion 注册表是首个"运行时多态"实现
- adapter **仍未拆分**为 input parser / output parser（每个 MR 一个 input adapter）

### 4.4 设计说明

- **Python venv 复杂度**：OpenMOC 装机困难（C++ 编译 + SWIG + venv 兼容），写了 `.claude/web-setup.sh` 30 min 一键装；这是项目唯一一段需要专门关怀的运行时依赖。
- **`IMrAssertion` 接口诞生**：从"假设只有一种 assertion"演化到"按名字 lookup"。这个 abstraction 撑了 Stage 4-5；在 v2 设计中改用 FluentAssertions 扩展方法替代（参见 §7）。
- **adapter 文件命名**：Stage 3+ 开始有 `openmoc_input_adapter_sigma_a.py` 这种"按 MR 命名"的 input adapter；Stage 5 时这成为 25+ 个文件，催生 v2 的 "input parser + ParameterMapping 数据驱动" 拆分。

### 4.5 关键 commit

```
b17185a  plan(stage3): pin-cell ScaleNuSigmaF MR for OpenMOC
ca59fb3  feat(stage3): openmoc output adapter (parse-output) + tests
8bf7cc6  feat(stage3): openmoc input adapter (transform-input ScaleNuSigmaF) + tests
e0f4a02  feat(stage3): sample pin-cell case JSON for openmoc MR
d57d57d  feat(stage3): openmoc runner + smoke test (k_eff via 2D pin-cell)
9f7789a  feat(stage3): openmoc pin-cell MR BDD scenario
2acc208  chore(stage3): apply code-review fixes (pipe-deadlock, JSON guards, asserts)
00c8561  chore(setup): make web-setup.sh actually work in the cloud sandbox
99e73cb  Merge pull request #7 (Stage 3 完成)

244dc1d  plan(stage3+): second OpenMOC MR with IMrAssertion refactor
1d7b4e5  refactor(systemmt): IMrAssertion interface + assertion-registry runner
0bb8e8b  feat(stage3+): LessThanAssertion unit tests
18eb9ad  feat(stage3+): openmoc input adapter (transform-input ScaleFuelSigmaA) + tests
8fd17c3  feat(stage3+): openmoc pin-cell sigma_a MR BDD scenario
0c73f9c  chore(stage3+): apply review fixes
694e2ab  Merge pull request #9 (Stage 3+ 完成)
```

---

## 5. Stage 4（2026-05-09 → 2026-05-11）— 平台特性

### 5.1 演化目标

**把系统级 MT 从"能跑"提升到"可用平台"**。补六个验收 criteria：
- AC #1：WPF 启动 system-MT 任务
- AC #2：结果持久化 + 可回溯
- AC #3：≥1 种报告格式
- AC #4：批量执行
- AC #5：第二个 SUT
- AC #6：跨程序 MR / IR

### 5.2 重大变更

| AC | 变更 | commit |
|----|------|--------|
| #2 | LiteDB 持久化 `SystemMtResultRecord` + 隔离 BsonMapper | `881b8cd feat(stage4): LiteDB persistence for SystemMtResult` |
| #3 | HTML 单跑报告 renderer | `5e628d9 feat(stage4): HTML report renderer for SystemMtResult` |
| #5 | 第二个 demo SUT — 1D heat equation | `9108c63 feat: 1D heat-equation SUT with amplitude-scaling MR` |
| #1-A | Launcher facade `ISystemMtScenarioLauncher` + type-leakage rule | `0dd96c2 feat(stage4): ISystemMtScenarioLauncher facade for VM-side WPF` |
| #1-B | WPF `SystemMtExecutionPage` 启动 UI | `52299f0 feat(stage4): WPF launch UI for system-level MT` |
| #1-B+ | Paging viewmodel base class (Stage 4 PagingViewModel<T>) | `8e8f7a2 feat(stage4): paging contracts + LiteDB ListPagedAsync` + `34bcbe4 feat(stage4): PagingViewModel<T> base class` |
| #4 | `RunBatchAsync` 批量执行 | `4f2d00c feat(stage4): batch execution via ISystemMtScenarioLauncher.RunBatchAsync` |
| #5 二号 SUT | OpenMC（Monte Carlo），mirror OpenMOC | `0c5260f feat(stage4): OpenMC SUT mirroring OpenMOC for cross-program MR` |
| #6 跨程序 IR | `MrFamily: string` slug + cross-program BDD | `c34c4b4 fix(openmc): make runner actually run + add cross-program comparison report` |
| CI | `dotnet test workflow` | `95cb8d9 ci: add dotnet test workflow` |

### 5.3 系统 MT Pipeline v1.0（成熟形态）

```
[Stage 4 — v1.0 pipeline] (持久化 + WPF + 批量 + 跨程序)

  WPF SystemMtExecutionPage
       │
       │ user 选 scenario + parameter overrides
       │
       ▼
  ISystemMtScenarioLauncher.RunAsync(scenarioId, parameterOverrides?)
       │
       │ scenario 注册表：5 个硬编码 ScenarioBlueprint
       │   • openmoc-pincell-nu-sigma-f
       │   • openmoc-pincell-sigma-a
       │   • openmc-pincell-nu-sigma-f
       │   • openmc-pincell-sigma-a
       │   • heat-equation-amplitude
       │
       ▼
  SystemMtTask (含 MrTransformation + IMrAssertion 名 + Python paths)
       │
       ▼
  SystemMtRunner.RunAsync()
       ├─→ InputGenerator → openmoc_input_adapter_*.py 或 openmc_input_adapter*.py
       ├─→ CliProgramRunner → SUT (OpenMOC / OpenMC / heat-eq)
       ├─→ PythonOutputAdapter
       └─→ assertions[name].Evaluate()
                │
                ▼
       SystemMtResult
                │
                ▼
       ★ SystemMtResultRecord (persisted to SystemMt.litedb)
                │
                ▼
       ★ HtmlSystemMtResultReportRenderer → 单跑 HTML 报告
                │
                ▼
       ★ WPF UI: 展示结果 + 可历史 review

  Batch path: ISystemMtScenarioLauncher.RunBatchAsync([scenarios...]) → 多 ScenarioRunResult
  Cross-program: 同 MrFamily slug 跑两个 SUT → tools/cross_program_mr.py
```

**Pipeline 成熟标志**：
- C# 编排 + LiteDB 持久化 + WPF UI + Batch 执行 = **完整闭环**
- 5 个 scenario 数据驱动地通过 `ScenarioBlueprint` 注册
- 跨程序 IR 落实为 `MrFamily` 字符串 slug — **简单可工作**
- `IMrAssertion` 接口稳定承载 GreaterThan/LessThan 两种 + 未来扩展

### 5.4 设计说明

- **Type-leakage rule**（在 `CLAUDE.md` 明文）：facade 公共方法签名只用 DTO 类型，不漏 `MrTransformation` / `SystemMtTask` / `SystemMtRunner` 内部类型。**保护意图**：WPF 端不被 BLL.Core 内部重构波及。**实际后果**：Stage 5 想加 `noise_aware` assertion 时，C# 端的 DTO 容器表达不了，新功能事实上"被门禁挡在 C# 外面"。
- **BsonMapper 隔离**：v1 `MR.litedb` 和 Stage 4 `SystemMt.litedb` 用各自隔离的 mapper（无 `BsonMapper.Global` 串扰）。这是当时的良性 over-engineering — 防患于未然。
- **HandyControl 仍然在**：Stage 4 加的 paging viewmodel 是 **`PagingViewModel<T>`**（C# 自研），但 6 个 v1 XAML 文件的 `hc:Pagination` **没有迁移**。这是项目首次出现"新旧 UI 模式并存"，至今未解决。
- **Cross-platform 边界明确**：CI 跑 Linux + .NET 8 + cross-platform projects（`MetBench_BLL.Core` / `MetBench_DAL` / `MetBench_SystemMT.Tests`）；WPF 项目 `MetBench_Client` / `MetBench_BLL` 仍 Windows-only。Linux cloud session 可写 BLL.Core 代码但不能验证 WPF 改动。

### 5.5 ⚠ HandyControl 未移除

Stage 4 没有 HandyControl 移除任务。`hc:Pagination` 在 6 个 v1 XAML 文件继续使用：
- ApplicationManagementPage / MRManagementPage / DomainManagementPage（CRUD 页）
- AutoDetectMRPage / MRRecommendationPage（推荐页）
- MRDisplayPage（展示页）

新加的 `SystemMtExecutionPage`（Stage 4 #1-B）**没用** HandyControl；它走纯 Wpf.Ui + `PagingViewModel<T>`。**这是新旧 UI 范式分裂的起点**。

### 5.6 关键 commit

```
95cb8d9  ci: add dotnet test workflow (#10)
1c59970  chore: ignore .env files (#11)
881b8cd  feat(stage4): LiteDB persistence for SystemMtResult (#12)
9108c63  feat: 1D heat-equation SUT with amplitude-scaling MR (#13)
5e628d9  feat(stage4): HTML report renderer for SystemMtResult (#14)
8358ca8  chore: add Apache 2.0 LICENSE and README (#15)
0dd96c2  feat(stage4): ISystemMtScenarioLauncher facade (Phase 2A)
52299f0  feat(stage4): WPF launch UI for system-level MT (AC #1-B)
8e8f7a2  feat(stage4): paging contracts + LiteDB ListPagedAsync
34bcbe4  feat(stage4): PagingViewModel<T> base class
4f2d00c  feat(stage4): batch execution (AC #4)
0c5260f  feat(stage4): OpenMC SUT mirroring OpenMOC (AC #6 prep)
c34c4b4  fix(openmc): cross-program comparison report (#23)
```

---

## 6. Stage 5（2026-05-12 → 2026-05-13）— 实证研究爆发期

> 这是项目演化中**最关键**的一段：Pipeline 出现"两套并行"漂移。

### 6.1 演化目标

**用 MetBench 自己验证 MR 套件的有效性**：跑 mutation testing、应用 NOETHER MetaPattern 框架、抓真实 bug。这是从"工具能跑"到"实证科研产出"的跨越。

### 6.2 Phase 1（2026-05-12 早）— Mutation Detection Study

| 内容 | 形态 |
|------|------|
| 28 个 mutations × 4 个 MR scenarios = 112 cells | 全部用 Python (`tools/mutation_study.py`) 跑 |
| `tools/mutations.py` | mutation 定义集合，Python dict |
| `docs/experiments/_data/baseline.json` | 28 baseline run 缓存 |
| `docs/experiments/_data/matrix.csv` | 112 cell 结果矩阵 |
| `docs/experiments/mutation-detection-matrix.md` | 自动渲染报告 |

**这是首次完全脱离 C# pipeline 的研究产出**。原因：要在 BDD/C# 框架里跑 28×4 = 112 cell，要 28 个 mutation 触发 + 28 .feature × 4 Example，工作量是 Python 矩阵脚本的 10+ 倍。**AI 选择了 Python 路径**。

### 6.3 Phase 2（2026-05-12 全天）— NOETHER MetaPattern + 25+ scenarios

| 子阶段 | 内容 |
|-------|------|
| NOETHER catalogue scaffolding | `c746a0f wip(stage5-phase2): NOETHER MetaPattern catalogue + LLM filter scaffolding` |
| MR04-MR08 实现 | `e8de712 feat(stage5-phase2): NOETHER MR 实现 + 矩阵化（OpenMOC 子集）` |
| MR06/MR08 适配器 + variance-ratio 断言 + Mut32-34 | `24b5ed0 feat(stage5-phase2): N06/N08 适配器 + variance-ratio 断言 + M32-M34` |
| OpenMC 矩阵补齐 + N12 variance-ratio | `7254d34 feat(stage5-phase2): OpenMC 矩阵补齐` |
| 符号统一 M→Mut / N→MR | `ea9d02a refactor(stage5-phase2): 统一符号缩写` |
| MR14 跨程序 + LLM filter calibration | `91f0d44 feat(stage5-phase2): MR14 跨程序报告 + LLM 过滤器对抗性校准` |
| Mut15-21 + MR02/MR03 镜像 + per-mutation MR14 | `299ac2b feat(stage5-phase2): Mut15-21 长跑补齐 + MR02/MR03 镜像激活` |
| Tolerance-aware noise margins + OpenMOC 病理 | `a03e5ef feat(stage5-phase2): tolerance-aware noise margins` |
| Phase-2 README + Phase-3 plan | `641e65f docs(stage5-phase2): Phase-2 顶层 README` |

**Pipeline 的根本性偏移在此发生**：
- 25+ 新 scenario **全部在 Python 端**（`mutation_study.SCENARIOS` dict）
- C# `ScenarioBlueprint` 注册表停留在 4-5 个
- Python `evaluate_mr()` 实现 5+ 种 assertion（含 `noise_aware`、`variance-ratio`、`flux-pointwise-approx`）
- C# `IMrAssertion` 接口物理上**无法表达**带 σ 的 assertion（签名 `Evaluate(string valueName, ParsedOutput source, ParsedOutput followUp)` 拿不到 std）
- Cohen's κ / Wilson CI / NOETHER MetaPattern 表 / LLM filter calibration — **全部 Python**

### 6.4 Phase 3（2026-05-12 晚 → 2026-05-13）— 真实 bug + Dashboard

| 子阶段 | 内容 |
|-------|------|
| Family A tally-symmetry MR + Mut47 | `96ee53d feat(stage5-phase3): Family A tally-symmetry MR + Mut47` |
| 历史 bug 系统调研 + Case 2 / Case 4 live | `3f9a17e docs(stage5): 历史 bug 系统调研 + Case 2/Case 4 live 复现` |
| MR 参数扫描 ≥5 点 + Case 5/6 第二个 OpenMOC 病理 | `af96375 feat(stage5): MR 参数扫描 + Case 5/6 — 第二个 OpenMOC 病理` |
| Bug 清单 + MR×MetaPattern 效果分析 + 可视化 plan | `84bd989 docs(stage5): bug 总清单` |
| Case 2 live in matrix + 三张静态图 | `fc1631d feat(stage5-phase3): Case 2 live in matrix + 三张图渲染` |
| Case 5 进矩阵 + Case 1 C++ rebuild 解除阻塞 | `4b68457 feat(stage5-phase3): Case 5 进矩阵` |
| Plotly 单文件交互式 dashboard | `463df96 feat(stage5-phase3): 4a — Plotly 单文件交互式 dashboard` |
| v2 系统级 MT 架构设计基线文档 | `b8401fd docs(v2-design): 系统级 MT v2 架构设计基线文档` |

### 6.5 系统 MT Pipeline v1.5（两套并行）

```
[Stage 5 — v1.5 pipeline] (★ 两套并行)

┌─ 主流 C# Pipeline (Stage 4 留下，少量更新) ─────────────────┐
│                                                              │
│  WPF → Launcher.RunAsync(scenarioId)                         │
│             ↓                                                │
│      ScenarioBlueprint 注册表 (5 个 Phase-1 scenario)        │
│             ↓                                                │
│      SystemMtRunner / IMrAssertion (2 种 greater/less)       │
│             ↓                                                │
│      LiteDB SystemMtResultRecord                             │
│                                                              │
│  ← WPF 用户能看到的全部世界                                    │
└──────────────────────────────────────────────────────────────┘

┌─ 旁路 Python 研究 Pipeline (Stage 5 新建) ──────────────────┐
│                                                              │
│  python3 tools/mutation_study.py matrix --all                │
│             ↓                                                │
│      mutation_study.SCENARIOS dict (29 scenarios)            │
│             ↓                                                │
│      evaluate_mr(cell, scenario) (5+ assertion types,        │
│                                    含 noise-aware,           │
│                                    variance-ratio,           │
│                                    flux-pointwise)            │
│             ↓                                                │
│      mutation × scenario 矩阵 (1392 cells)                   │
│             ↓                                                │
│      docs/experiments/_data/*.json + *.csv                   │
│             ↓                                                │
│      tools/render_figures.py + render_dashboard.py           │
│             ↓                                                │
│      docs/experiments/figures/*.png +                        │
│      docs/experiments/dashboard.html                         │
│                                                              │
│  ← 研究人员的真实工作世界                                      │
└──────────────────────────────────────────────────────────────┘

★ 两个 Pipeline 共享：SUT runner + adapter 脚本
★ 两个 Pipeline 不共享：scenario 注册表 / assertion 实现 /
                         持久化 / 报告 / WPF 可见度
```

**Stage 5 真正交付的产物**（全部 Python 一侧）：
- 28 mutations × 29 scenarios × 2 solvers = 矩阵（fc1631d）
- 11 个 sweep MR × 5 个 factor sample 点 = 参数扫描（af96375）
- 6 个 R-Case 真实 bug，其中 4 个 live-triggered，3 个进矩阵（4b68457）
- 1 个 plotly dashboard，含 4 个 section（463df96）
- 28+ 实验 markdown 报告
- 2 个 OpenMOC `CPUSolver` 病理盆地的发现（Case 4 + Case 6 — MetBench 首例**未知** bug 检出）

### 6.6 设计说明

- **AI 编程改变了博弈**：Phase 1 的 28×4 矩阵在 BDD 框架里要写 ~75 个文件 + 自定义 mutation 触发；在 Python 里是 ~400 行单脚本。**AI 在后者上的产能高一个数量级**，团队不知不觉就接受了 Python 路径。
- **`noise_aware` 是第一个 C# 无法表达的概念**：MC 噪声底 = max(3σ, tol·|k|)，C# `IMrAssertion.Evaluate(string, ParsedOutput, ParsedOutput)` 拿不到 σ；要么改 facade（违 type-leakage rule），要么在 Python 一侧做。Python 胜。
- **未知 bug 的发现路径**：Case 4 (factor=1.5 moderator-σ_a) 由 m_cmp / 跨程序对比发现；Case 6 (factor=1.25 fuel-T) 由 m_mono / 参数扫描发现。**这两个发现都不通过 C# pipeline**——它们存在于 `tools/cross_program_mr.py` 和 `tools/mr_parameter_sweep.py` 的输出里。

### 6.7 关键 commit（汇总）

```
cac3b0b  feat(stage5-phase1): mutation-detection study — 28 mutants × 4 MR scenarios
c746a0f  wip(stage5-phase2): NOETHER MetaPattern catalogue + LLM filter scaffolding
e8de712  feat(stage5-phase2): NOETHER MR 实现 + 矩阵化（OpenMOC 子集）
24b5ed0  feat(stage5-phase2): N06/N08 适配器 + variance-ratio 断言 + M32-M34
7254d34  feat(stage5-phase2): OpenMC 矩阵补齐 + N12 variance-ratio
ea9d02a  refactor(stage5-phase2): 统一符号缩写 M→Mut、N→MR
91f0d44  feat(stage5-phase2): MR14 跨程序报告 + LLM 过滤器对抗性校准
299ac2b  feat(stage5-phase2): Mut15-21 长跑补齐 + MR02/MR03 镜像激活
a03e5ef  feat(stage5-phase2): tolerance-aware noise margins
641e65f  docs(stage5-phase2): Phase-2 顶层 README + Phase-3 plan
96ee53d  feat(stage5-phase3): Family A tally-symmetry MR + Mut47
3f9a17e  docs(stage5): 历史 bug 系统调研
af96375  feat(stage5): MR 参数扫描 (≥5 点/MR) + Case 5/6
84bd989  docs(stage5): bug 总清单 + MR×MetaPattern 效果分析
fc1631d  feat(stage5-phase3): Case 2 live in matrix + 三张图渲染
4b68457  feat(stage5-phase3): Case 5 进矩阵 + Case 1 C++ rebuild 解除阻塞
463df96  feat(stage5-phase3): 4a — Plotly 单文件交互式 dashboard
```

---

## 7. v2 设计（2026-05-13）— 回归与统一

### 7.1 演化目标

**把 Stage 5 漂移收拢回来**，让方法级 MT + 系统级 MT + 研究矩阵在同一 C# 编排 + LiteDB 数据中心化架构下并存。**不抛弃 v1 投资，不抛弃 Stage 5 产出，让二者各得其所**。

### 7.2 重大变更（设计层面，未实施）

| 变更 | 影响 |
|------|------|
| **MR 4 级语义显式建模** | MetaPattern → MRSchema → MRBinding → MRInstance，对应 4 个 LiteDB collection |
| **LiteDB 扩展到 23 collection (3NF)** | 修正 `ApplicationName` / `DomainName` 多值字符串反模式 |
| **既有 `MetamorphicRelation` / `Application` 扩展，不重新发明** | 新字段加在原类，旧字段标 `[Obsolete]` 但保留读取兼容 |
| **Adapter 拆分为 Input Parser + Output Parser + ParameterMapping** | 文件 IO 与 MR 变换解耦 |
| **MR Transformation 移入 C# Pipeline** | 不在 Python adapter 里；`IMRTransformation` C# 接口 |
| **断言系统改用 FluentAssertions 扩展方法** | 废除 `IMrAssertion` 接口；API 风格与 FA 原生一致 |
| **Discovery 子系统首次显式建模** | `IMRDiscoverer` 接口 + MetaPattern 结构化 + LLM-Native 启发式 + 3 个 Validator |
| **Mutation 子系统作为一等公民** | 4 个新 collection；跨 SUT 差分分析 |
| **BDD `.feature` ↔ LiteDB 双向同步** | `.feature` 是 MR 视图，LiteDB 是真理源 |
| **Anomaly + Replay 服务** | 异常调查工作流；一键重放 + 自动对比新旧 |

### 7.3 系统 MT Pipeline v2.0（设计图）

```
[v2 — Pipeline 设计] (C# 编排回归 + 23 collection 数据中心)

  WPF 用户 → Scenarios 页 → 选 MRInstance + Run
       │
       ▼
  SystemMtPipeline.ExecuteAsync(mrInstanceId)
       │
       │ status: queued → parsing-source → transforming →
       │         writing-followup → running-source → running-followup →
       │         parsing-outputs → asserting → ok / anomaly / error
       │
       ▼
  ① Load MRInstance from LiteDB
       ↓ joins MRBinding ← MRSchema ← MetaPattern
       ↓                ← Application (SUT) ← Runtime
       ↓                ← Adapter (Input/Output)
       ↓                ← ParameterMappings (embedded)
       ↓
  ② Read source case file via Application.InputParserPath
       ↓ (subprocess Python: input_parser.parse → dict)
       ↓
  ③ Apply IMRTransformation (C# 内存 dict 上)
       ↓ uses ParameterMapping to resolve abstract field → concrete path
       ↓ applies transformation (e.g. ScaleField factor=1.5)
       ↓ returns transformed dict
       ↓
  ④ Write followup file via Application.InputParserPath
       ↓ (subprocess Python: input_parser.write)
       ↓
  ⑤ Invoke SUT (subprocess via Runtime.InvokeTemplate)
       ↓ source.in  → source.out
       ↓ followup.in → followup.out
       ↓
  ⑥ Parse outputs via Application.OutputParserPath
       ↓ (subprocess Python: output_parser.parse)
       ↓ → SourceMetrics / FollowupMetrics dicts
       ↓
  ⑦ AssertionEvaluator.Evaluate (FA extension methods)
       ↓ switch on AssertionTypeCode:
       ↓   "less-noise-aware" → BeLessThanWithNoiseFloor()
       ↓   "variance-ratio"   → HaveVarianceRatio()
       ↓   "approx"           → BeApproximately() (FA 原生)
       ↓   ...
       ↓
  ⑧ Persist Execution + Result + (if failed) Anomaly
       ↓ + AuditLog
       ↓
  ⑨ Return ScenarioRunResult to UI

  Anomaly → 异常 viewer → drill-down → ★ Replay button →
                                       new Execution (same MRInstance) →
                                       compare → reproduced / flaky / fixed
```

### 7.4 关键设计决策表

| 决策 | 选择 | 文档 |
|------|------|------|
| MT 编排 | C# `SystemMtPipeline` | `v2-system-mt-architecture.md` §3 |
| Adapter 语言 | Python（仅 parse / write） | `v2-system-mt-architecture.md` §3.2 |
| MR 变换位置 | **C# pipeline**（不在 adapter） | `glossary.md` §2 |
| 持久化 | LiteDB 23 collection (3NF) | `entity-model.md` |
| 断言系统 | FluentAssertions 扩展方法 | `assertion-extensions.md` |
| Discovery 子系统 | `IMRDiscoverer` + 2 实现 + 3 validator | `v2-system-mt-architecture.md` §7 |
| Mutation 子系统 | 4 新实体 + 跨 SUT 差分 | `v2-system-mt-architecture.md` §8 |
| BDD | `.feature` 作 MR 视图，双向同步 | `v2-system-mt-architecture.md` §6 |
| Anomaly | Anomaly collection + Replay service | `v2-system-mt-architecture.md` §9 |
| 实施周期 | 8 周（P1-P8） | `migration-plan.md` |

### 7.5 待执行任务

详见 [`migration-plan.md`](migration-plan.md)。本文档不重复。

### 7.6 关键 commit

```
b8401fd  docs(v2-design): 系统级 MT v2 架构设计基线文档
```

---

## 8. HandyControl 移除路线（建议，非承诺）

### 8.1 现状

6 个 v1 XAML 文件依赖 `hc:Pagination`：

| 文件 | 当前角色 | 替代难度 |
|------|--------|--------|
| `ApplicationManagementPage.xaml` | v1 应用程序 CRUD | ★ 低（Stage 4 已有 `PagingViewModel<T>` 范式） |
| `MRManagementPage.xaml` | v1 MR CRUD | ★ 低 |
| `DomainManagementPage.xaml` | v1 领域 CRUD | ★ 低 |
| `MRDisplayPage.xaml` | v1 MR 展示（含图像渲染） | ★★ 中（含 InputPattern 图像） |
| `MRRecommendationPage.xaml` | v1 MR 推荐结果 | ★★ 中（特殊布局） |
| `AutoDetectMRPage.xaml` | v1 自动检测 | ★★★ 高（复杂 UI） |

### 8.2 替代方案

Stage 4 已确立**新 UI 范式**：Wpf.Ui 原生组件 + `PagingViewModel<T>` (C# 自研 paging 基类)。

替代 `hc:Pagination` 的 Wpf.Ui 模式：

```xml
<!-- 旧 -->
<hc:Pagination MaxPageCount="{Binding MaxPageCount}"
               PageIndex="{Binding PageIndex, Mode=TwoWay}" />

<!-- 新（参考 SystemMtExecutionPage Stage 4 #1-B + paging viewmodel #18/#19）-->
<StackPanel Orientation="Horizontal" HorizontalAlignment="Right">
  <ui:Button Icon="ChevronLeft24" Command="{Binding PrevPageCommand}" />
  <TextBlock Text="{Binding PageIndex}" />
  <TextBlock Text=" / " />
  <TextBlock Text="{Binding MaxPageCount}" />
  <ui:Button Icon="ChevronRight24" Command="{Binding NextPageCommand}" />
</StackPanel>
```

### 8.3 分阶段移除策略

| 阶段 | 操作 |
|------|------|
| **R1** | 在 6 个 v1 ViewModel 上换上 `PagingViewModel<T>` 基类（C# 已存在，零功能损失） |
| **R2** | 替换 `hc:Pagination` 为 Wpf.Ui 三按钮分页（XAML 改动）；按页面逐个迁移 |
| **R3** | 从 `MetBench_Client.csproj` 删除 `HandyControl` 包引用 |
| **R4** | 验证 6 页面无视觉/功能回归 |

**工时估算**：每页 ~半天 × 6 + 整体测试 1 天 = **4 工日**。

**建议**：不在 v2 8 周路线内做，作为独立 R 阶段（"refactor"）。优先级低于 P1-P8。

---

## 9. 系统 MT Pipeline 纵贯对比图

```
v0   v1.0 baseline       │ method-level only │ 无 system pipeline
                         │                   │
v0.1 Stage 1 (2026-05-07)│ BDD scenario      │ Task→CliRunner→OutputAdapter→GreaterThan
                         │                   │
v0.2 Stage 2 (2026-05-08)│ + InputGenerator  │ Task→★InputGen→CliRunner→OutputAdapter→Greater/Less
                         │                   │
v0.3 Stage 3/3+ (05-08)  │ + OpenMOC SUT     │ 同 v0.2 + ★assertion 注册表 + 真实物理 SUT
                         │                   │
v1.0 Stage 4 (2026-05-09)│ + LiteDB + UI     │ WPF→Launcher→Task→Runner→Adapter→Assertion
                         │                   │ →★LiteDB→★HTML报告
                         │                   │ + Batch + Cross-program + OpenMC
                         │                   │
v1.5 Stage 5 (2026-05-12)│ ⚠ 两套并行         │ C# 主流 (5 scenarios, 2 assertions, LiteDB)
                         │                   │       +
                         │                   │ Python 旁路 (29 scenarios, 5 assertions,
                         │                   │   noise_aware, variance-ratio, sweep, κ,
                         │                   │   LLM filter, dashboard.html)
                         │                   │
v2.0 v2 设计 (2026-05-13)│ 统一 C# pipeline   │ MRInstance→Pipeline (parse→transform→write
                         │                   │   →run→parse→assert)→LiteDB(23 col, 3NF)
                         │                   │ + 4 级 MR 显式 + Discovery + Mutation +
                         │                   │   Anomaly + Replay + Trend + Coverage
```

### 关键演化轴

| 维度 | v0.1 | v0.2 | v0.3 | v1.0 | v1.5 | v2.0 |
|------|------|------|------|------|------|------|
| Pipeline 步骤数 | 4 | 5 | 5 | 7 | 5+10（Python） | 9 |
| Assertion 实现数 | 1 | 1 | 2 | 2 | 2+5（Python） | 9（FA 扩展） |
| Scenario 数 | 1 | 2 | 4 | 5 | 5+29 | 200-500（目标） |
| SUT 数 | 1 (demo) | 1 | 1 | 3 | 3 | 任意（可插拔） |
| 持久化 | 无 | 无 | 无 | LiteDB | LiteDB + JSON 双轨 | LiteDB 统一 |
| WPF 可见 | ✗ | ✗ | ✗ | 5 scenario | 5 scenario | 全部 |
| 真实 bug 检出 | 0 | 0 | 0 | 0 | 2（Case 4, 6）+ 4 已知 | 设计支持 |

---

## 10. 经验教训

### 10.1 AI 编程对项目演化的结构性影响

**正面**：
- AI 在 Python 研究代码上的产能比 C# scaffolding 高 5-10×（典型一日：500-1500 行有效 Python vs 200-300 行有效 C#）
- AI 让 Stage 5 一周内完成了原本需要数月的实证研究（mutation 矩阵 + NOETHER + 跨程序 + 真实 bug + dashboard）
- AI 在文档撰写、commit 消息、跨语言协调上**填了大量隐性成本**
- AI 能在 C# / Python / XAML / Gherkin / SQL / Markdown / JSON Schema 之间无缝切换

**负面**：
- AI 在 BDD/C# scaffolding 上太擅长，让团队**没有及时质疑"加新 MR = 5 文件协调"的仪式成本**
- AI 在 Python 研究代码上太擅长，让 Stage 5 演化路径自然偏向 Python，**没有人主动调度"这是不是该往 C# 收敛了"**
- AI 不会自动告诉你"认知超载了"或"两套系统正在漂移"——需要人主动复盘
- AI 让"维护两套并行系统"看起来可承受，**直到不可承受**——这次复盘就是临界点

**根本规律**：AI 加速演化的方向是它**最擅长的方向**，不一定是项目最需要的方向。**架构警觉必须由人持有**。

### 10.2 设计决策的"半衰期"

回顾每个阶段的关键设计决策，看哪些 hold up：

| 决策 | 当时 | 现在评价 |
|------|------|--------|
| WPF + HandyControl（v1） | 教学投资 | ✓ 保留（v2 不强制改） |
| LiteDB（v1） | 嵌入式简单 | ✓ 保留（v2 继续用） |
| MetBench_BLL.Core 跨平台（Stage 1） | 隔离 WPF | ✓ 价值满分 |
| Reqnroll BDD（Stage 1） | 业界标准 | △ 降级为 Phase-1 smoke test |
| Python adapter 模式（Stage 1） | "workflow C#，IO Python" | △ 修正为 Parser + Mapping 拆分 |
| `MrTransformation` IR（Stage 2） | 简单 record | ✓ 演化为 `MRInstance.ParameterOverrides` |
| `IMrAssertion` 接口（Stage 3+） | 多 assertion 支持 | ✗ 废除，改用 FluentAssertions 扩展 |
| `MrFamily` slug（Stage 4） | 简单 string | ✓ 演化为 4 级 MR 层次中的一部分 |
| Type-leakage rule（Stage 4） | 保护 WPF | △ 反咬一口，但本意正确 |
| LiteDB BsonMapper 隔离（Stage 4） | 防患未然 | ✓ 完全正确，v2 继续 |
| `SystemMtResultRecord` 平铺（Stage 4） | 简单 | ✗ 拆为 Execution + Result + Anomaly |
| Python 矩阵脚本（Stage 5） | 快速研究 | △ 数据要回 LiteDB，但脚本作为辅助保留 |
| NOETHER MetaPattern（Stage 5） | 学术框架 | ✓ 进 v2 实体 |
| dashboard.html（Stage 5） | 单文件交互视图 | ✓ 进 v2 嵌 WebView2 |

**规律**：**简单 + 数据驱动**的决策半衰期长（LiteDB、跨平台 lib、MrTransformation IR）；**接口型**的决策半衰期短（IMrAssertion 在两年内被废除）。**v2 设计偏向前者**。

### 10.3 项目身份的两层

MetBench 同时是：
1. **教学项目**：演示 WPF + Reqnroll + LiteDB + C# 业务编排的工程化姿态
2. **研究平台**：实验室真实跑 MR × SUT × params 矩阵的实证基础设施

这两层在 Stage 1-4 时合一，在 Stage 5 时分裂，v2 设计想再次合一——**但接受方法级 MT 仍走 v1 C#，系统级 MT 走 v2 C#，两者不串扰**。

**这是健康的**。一个工具兼具教学价值与研究价值，本身就难得。**只要边界明确、文档诚实，两层可以共存**。

### 10.4 给未来研究生的话

如果你在 2027 年或之后接手 MetBench：

1. **先读 `docs/design/glossary.md`** — MR 这个词有 4 层语义，搞不清会浪费一周
2. **看 `docs/design/v2-system-mt-architecture.md`** — 当前架构总览
3. **本文件（evolution.md）** — 理解为什么是这个架构，而不是别的
4. **不要重写 v1 method-level MT** — 它有自己的存在价值
5. **加新 MR 走 `.md` + `.feature` 同步** — 不要直接改 LiteDB
6. **AI 是协作者不是 architect** — 重大架构决定需要人拍板
7. **诚实评估你的偏好** — 你想做工程平台 vs 研究工具，决定了应该在 v2 上加什么 / 减什么

---

## 11. 文档交叉引用

| 想了解 | 看 |
|--------|---|
| 当前 v2 整体架构 | `docs/design/v2-system-mt-architecture.md` |
| 术语精确定义 | `docs/design/glossary.md` |
| LiteDB schema 完整规格 | `docs/design/entity-model.md` |
| 断言扩展方法 API | `docs/design/assertion-extensions.md` |
| 8 周迁移路线 | `docs/design/migration-plan.md` |
| Stage 5 实证产物 | `docs/experiments/PHASE2.md` + `bug-inventory.md` + `dashboard.html` |
| 各阶段历史设计文档 | `docs/superpowers/plans/2026-05-*.md` |
| 项目阶段定义（旧） | `AGENTS.md` |
| 协作约定 | `CLAUDE.md` |
| 项目入口 | `README.md` |

---

**本演化文档与 git 历史 91 个 commit、`docs/design/` 5 份基线文档、`docs/experiments/` 28+ 实证报告同步**。任何重大架构变动（v3 或之后）应在本文件末尾追加新章节，保留完整演化纪录。
