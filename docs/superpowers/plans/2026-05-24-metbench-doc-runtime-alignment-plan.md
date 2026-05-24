# MetBench Doc And Runtime Alignment Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 对齐 MetBench 的核心事实源文档与 `main` 当前实现，并先收口继续阻塞 Stage 8 开发的运行时问题。

**Architecture:** 先把路线图、结构文档、需求追溯和架构入口统一到当前主干真实状态，再按“单一事实源、接口去耦、证据闭环、测试基线固化”的顺序清障。整个计划以当前 `main` 代码、已合入提交和可审计测试结果为唯一依据，不补写无证据事实。

**Tech Stack:** Markdown、Git、.NET 8、WPF DI、LiteDB、CodeGraph、xUnit / Reqnroll

**Refresh note:** 2026-05-24 已再次执行 `git pull --ff-only`，结果为 up-to-date；CodeGraph 已执行 `sync`，结果为 already up to date。

---

### Task 1: 对齐路线图级事实源

**Files:**
- Modify: `AGENTS.md`
- Modify: `CLAUDE.md`
- Test: `rtk rg -n "Stage 8|G-X3|Trend|P-A|P-B|P-C" AGENTS.md CLAUDE.md`

- [x] **Step 1: 先写出待修正事实清单**

把下面这些事实作为唯一允许写入的更新项：

```text
- main 已到 5691727
- G-X3 PR #91/#92/#93/#94 已合入
- Stage 8 已启动，不再是“未启动”
- ManifestMrCatalogProvider 已注册为默认 IMrCatalogProvider
- ExecutionEvidence 已接入 recorder，但 SampleTraces 仍未闭环
- Trend 不再是当前活跃运行时模块
```

- [x] **Step 2: 修改 `AGENTS.md` 的 Stage 8 段**

要求：

```text
- 把“是否已启动”改为“已启动且已完成哪些 slice”
- 把当前阻塞改写为 catalog fallback / importer coupling / sample-level evidence
- 对未完成项显式标注“待完成”或“待核实”
```

- [x] **Step 3: 修改 `CLAUDE.md` 的项目状态快照**

要求：

```text
- 删除或改写“Stage 8 未启动/仅计划中”的旧说法
- 回写 P-A / P-B / P-C / G-X3 的真实完成边界
- 保留仍未闭环项，不把“部分完成”写成“完成”
```

- [x] **Step 4: 运行 grep 自检**

Run: `rtk rg -n "未启动|TrendAnalysisService|HardcodedMrCatalogProvider|SampleTraces|G-X3" /Users/limeng/Codes/苏永成-蜕变测试系统代码与文档资料/MetBench-V2.1.4_2/AGENTS.md /Users/limeng/Codes/苏永成-蜕变测试系统代码与文档资料/MetBench-V2.1.4_2/CLAUDE.md`

Expected:

```text
- “未启动”只在历史语境或明确否定上下文中出现
- G-X3 与当前状态描述一致
- 对 Trend 的表述不再把它写作现行活跃模块
```

- [ ] **Step 5: 提交**

```bash
rtk git add AGENTS.md CLAUDE.md
rtk git commit -m "docs: align Stage 8 roadmap and project snapshot with main"
```

### Task 2: 对齐结构文档与架构入口文档

**Files:**
- Modify: `docs/PROJECT-STRUCTURE.md`
- Modify: `docs/design/v2-system-mt-architecture.md`
- Test: `rtk rg -n "521 Pass|Trend|launcher 注册|4 个|5 MR|ExecutionEvidence|IMrCatalogProvider" docs/PROJECT-STRUCTURE.md docs/design/v2-system-mt-architecture.md`

- [x] **Step 1: 先重建结构事实表**

以当前代码为准重建下面这些信息：

```text
- 当前 SUT 范围
- 当前 launcher catalog 绑定规模
- 当前 catalog 来源是 manifest provider 默认；生产 fallback 已删除，hardcoded provider 仅保留给测试 / parity
- 当前 execution evidence 架构已存在但 sample traces 未落地
- Trend 已退出现行运行时
```

- [x] **Step 2: 修改 `docs/PROJECT-STRUCTURE.md`**

要求：

```text
- 更新时间改到 2026-05-24
- 删掉 2026-05-17 的 4 SUT / 5 launcher MR / 521 baseline 旧表述
- 在结构说明里加入 provider-backed catalog 和 execution evidence 的现状
- 不把 `.codegraph/` 写进正式架构版图
```

- [x] **Step 3: 修改 `docs/design/v2-system-mt-architecture.md`**

要求：

```text
- 把 Trend 从“当前模块清单”降级或移除
- 补上 IMrCatalogProvider / ManifestMrCatalogProvider / SystemMtExecutionRecorder 的现行角色
- 明确区分当前实现与历史设计草案
```

- [x] **Step 4: 运行结构一致性检查**

Run: `rtk rg -n "TrendAnalysisService|521 Pass|5 MR|4 个|ManifestMrCatalogProvider|SystemMtExecutionRecorder" /Users/limeng/Codes/苏永成-蜕变测试系统代码与文档资料/MetBench-V2.1.4_2/docs/PROJECT-STRUCTURE.md /Users/limeng/Codes/苏永成-蜕变测试系统代码与文档资料/MetBench-V2.1.4_2/docs/design/v2-system-mt-architecture.md`

Expected:

```text
- 新文档中能找到 ManifestMrCatalogProvider / SystemMtExecutionRecorder 的现行描述
- 旧的 521 / 4 SUT / 5 MR 说法被清掉或明确标成历史基线
- Trend 不再被列为当前模块
```

- [ ] **Step 5: 提交**

```bash
rtk git add docs/PROJECT-STRUCTURE.md docs/design/v2-system-mt-architecture.md
rtk git commit -m "docs: resync project structure and architecture entry docs"
```

### Task 3: 对齐需求追溯事实源

**Files:**
- Modify: `docs/requirements.md`
- Test: `rtk rg -n "876 pass|965|pending|G-X3|baseline" docs/requirements.md`

- [x] **Step 1: 统一 baseline 叙事规则**

把 baseline 统一成下面这种写法：

```text
- 头部只写“当前可证实基线”或“最新可审计基线”
- 文内进度表允许记录历史 pass 数，但必须注明对应 commit/阶段
- 如果本轮本地测试没有返回精确通过数，就写“命令成功，精确通过数待核实”
```

- [x] **Step 2: 修改 `docs/requirements.md` 顶部与 G-X3 区段**

要求：

```text
- 头部不要再把 876 直接写成当前唯一基线
- 把 G-X3 已合入项与待完成项拆开
- 对 hotfix #94 的状态回写到 requirements 索引
- 明确 SampleTraces / importer decoupling 仍属未完成问题，并把 fallback removal 改写为已完成项
```

- [x] **Step 3: 运行追溯一致性检查**

Run: `rtk rg -n "876 pass|965 pass|pending|hotfix|SampleTraces|HardcodedMrCatalogProvider" /Users/limeng/Codes/苏永成-蜕变测试系统代码与文档资料/MetBench-V2.1.4_2/docs/requirements.md`

Expected:

```text
- 876 与 965 都带上下文，不会再混成单一现状
- G-X3 已完成与未完成分界清楚
- 当前 blocker 在 requirements 中有单独位置
```

- [ ] **Step 4: 提交**

```bash
rtk git add docs/requirements.md
rtk git commit -m "docs: align controlled requirements matrix with current main"
```

### Task 4: 删除 catalog 运行时 fallback

**Files:**
- Modify: `MetBench_BLL.Core/SystemMT/Launcher/SystemMtLauncher.cs`
- Modify: `MetBench_BLL.Core/SystemMT/Catalog/HardcodedMrCatalogProvider.cs`
- Test: `MetBench_SystemMT.Tests/SystemMT/Launcher/SystemMtLauncherTests.cs`
- Test: `MetBench_SystemMT.Tests/SystemMT/Catalog/ManifestMrCatalogProviderTests.cs`
- Test: `MetBench_SystemMT.Tests/SystemMT/Launcher/SystemMtLauncherProviderInjectionTests.cs`

- [x] **Step 1: 先写失败测试，锁定“不允许 fallback”**

测试目标：

```csharp
[Fact]
public void Ctor_throws_when_catalog_provider_is_missing()
{
    // Arrange + Act + Assert
}
```

- [ ] **Step 2: 运行单测确认当前行为仍允许 fallback**（保留历史 TDD 证据缺口说明；当前环境未能在改动前给出精确用例结果）

Run: `rtk dotnet test /Users/limeng/Codes/苏永成-蜕变测试系统代码与文档资料/MetBench-V2.1.4_2/MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --filter "FullyQualifiedName~SystemMtLauncherTests" --no-restore`

Expected:

```text
FAIL: 新增测试失败，因为当前构造函数仍会自动 new HardcodedMrCatalogProvider
```

实际情况（2026-05-24 本轮）：

```text
- 新增了 Constructor_without_catalog_provider_throws
- `rtk dotnet test` 当前环境只返回 completed，不返回该用例的精确执行结果
- 后续改用 restore + build/test 回归证明改动可编译；“红灯已观测”这一点在本机仍属待补强证据
```

- [x] **Step 3: 最小实现改动**

实现方向：

```csharp
public SystemMtLauncher(
    LauncherOptions options,
    ISystemMtPipeline pipeline,
    SystemMtExecutionRecorder recorder,
    IAnomalyService anomalyService,
    IMrCatalogProvider catalogProvider,
    AnomalySeverityThresholds? severityThresholds = null)
{
    _mrCatalog = catalogProvider.Load()
        .Select(entry => entry.ToBlueprint())
        .ToDictionary(b => b.Mr.Id, StringComparer.Ordinal);
}
```

- [x] **Step 4: 处理过渡类**

要求：

```text
- 如果仍保留 HardcodedMrCatalogProvider，则只允许保留给测试/迁移工具
- 不能再作为生产默认路径
```

- [x] **Step 5: 运行相关测试**

Run: `rtk dotnet test /Users/limeng/Codes/苏永成-蜕变测试系统代码与文档资料/MetBench-V2.1.4_2/MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --filter "FullyQualifiedName~SystemMtLauncherTests|FullyQualifiedName~ManifestMrCatalogProviderTests" --no-restore`

Expected:

```text
PASS: launcher 仅接受显式 provider，manifest tests 继续通过
PASS: provider injection tests 继续通过
```

实际情况（2026-05-24 本轮）：

```text
- 已先执行一次 `dotnet restore`，解决 CommunityToolkit.Mvvm 8.0.0 缺包
- unrestricted `dotnet build MetBench_SystemMT.Tests --no-restore -m:1` 通过：6 projects, 0 errors, 9 warnings
- focused unrestricted `dotnet test --filter "FullyQualifiedName~SystemMtLauncherProviderInjectionTests|FullyQualifiedName~SystemMtLauncherTests|FullyQualifiedName~ManifestMrCatalogProviderTests|FullyQualifiedName~SystemMtBootstrapTests|FullyQualifiedName~AnomalyCreationOnFailureTests" --no-build` 通过：51 tests passed, 0 warnings
```

- [ ] **Step 6: 提交**

```bash
rtk git add MetBench_BLL.Core/SystemMT/Launcher/SystemMtLauncher.cs MetBench_BLL.Core/SystemMT/Catalog/HardcodedMrCatalogProvider.cs MetBench_SystemMT.Tests/SystemMT/Launcher/SystemMtLauncherTests.cs
rtk git commit -m "refactor: remove production catalog fallback from launcher"
```

### Task 5: 让 importer 脱离具体 launcher 实现

**Files:**
- Modify: `MetBench_BLL.Core/SystemMT/Launcher/LauncherCatalogV2Importer.cs`
- Modify: `MetBench_Client/App.xaml.cs`
- Test: `MetBench_SystemMT.Tests/SystemMT/Launcher/LauncherCatalogV2ImporterTests.cs`

- [x] **Step 1: 先写失败测试，锁定 importer 只依赖目录读取接口**

测试目标：

```csharp
[Fact]
public void Import_reads_entries_from_catalog_reader_abstraction()
{
    // Arrange fake reader
    // Act import
    // Assert created entities
}
```

- [x] **Step 2: 引入只读抽象**

实现方向：

```csharp
public interface ISystemMtCatalogReader
{
    IReadOnlyList<MrCatalogEntry> GetCatalogEntries();
}
```

- [x] **Step 3: 让 launcher 实现该只读抽象，importer 改依赖接口**

实现方向：

```csharp
public sealed class LauncherCatalogV2Importer
{
    private readonly ISystemMtCatalogReader _catalogReader;
}
```

- [x] **Step 4: 更新 WPF DI 注册**

要求：

```text
- App.xaml.cs 不再对 ISystemMtLauncher 做具体类强转
- importer 只从容器拿 ISystemMtCatalogReader
```

- [x] **Step 5: 运行 importer 相关测试**

Run: `rtk dotnet test /Users/limeng/Codes/苏永成-蜕变测试系统代码与文档资料/MetBench-V2.1.4_2/MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --filter "FullyQualifiedName~LauncherCatalogV2ImporterTests" --no-restore`

Expected:

```text
PASS: importer 通过接口读取 catalog，现有导入语义不变
```

实际情况（2026-05-24 本轮）：

```text
- unrestricted `dotnet build MetBench_SystemMT.Tests --no-restore -m:1` 通过：6 projects, 0 errors, 286 warnings
- focused unrestricted `dotnet test --filter "FullyQualifiedName~LauncherCatalogV2ImporterTests" --no-build` 通过：11 tests passed, 0 warnings
- Windows 侧 `MetBench_Client` build 验证已发往 Parallels VM，但本轮尚未拿到可读回执，需后续补记
```

- [ ] **Step 6: 提交**

```bash
rtk git add MetBench_BLL.Core/SystemMT/Launcher/LauncherCatalogV2Importer.cs MetBench_Client/App.xaml.cs MetBench_SystemMT.Tests/SystemMT/Launcher/LauncherCatalogV2ImporterTests.cs
rtk git commit -m "refactor: decouple catalog importer from concrete launcher"
```

### Task 6: 补全 sample-level execution evidence

**Files:**
- Modify: `MetBench_BLL.Core/SystemMT/Pipeline/SystemMtExecutionRecorder.cs`
- Modify: `MetBench_BLL.Core/SystemMT/Persistence/ExecutionEvidence.cs`
- Modify: `MetBench_BLL.Core/SystemMT/Pipeline/SystemMtPipeline.cs`
- Test: `MetBench_SystemMT.Tests/V2Pipeline/SystemMtExecutionRecorderTests.cs`

- [x] **Step 1: 写失败测试，锁定 `SampleTraces` 不再为空**

测试目标：

```csharp
[Fact]
public void Record_writes_sample_traces_when_pipeline_supplies_input_samples()
{
    // Arrange outcome/context with sample inputs
    // Act record
    // Assert evidence.SampleTraces is not empty
}
```

- [x] **Step 2: 运行测试确认现状仍为空**

Run: `rtk dotnet test /Users/limeng/Codes/苏永成-蜕变测试系统代码与文档资料/MetBench-V2.1.4_2/MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --filter "FullyQualifiedName~SystemMtExecutionRecorderTests" --no-restore`

Expected:

```text
FAIL: 当前写入逻辑仍是 SampleTraces = new()
```

实际情况（2026-05-24 本轮）：

```text
- unrestricted focused `dotnet test` 明确失败：Assert.Single() Failure: The collection was empty
- 失败点定位准确：ExecutionEvidenceWriteThroughTests.Record_evidence_writes_sample_trace_for_target_field
```

- [x] **Step 3: 把 pipeline 可用的样本信息投影为 evidence**

实现方向：

```csharp
SampleTraces = outcome.InputSamples
    .Select(sample => new ExecutionSampleTrace { ... })
    .ToList();
```

- [x] **Step 4: 运行 recorder 相关测试**

Run: `rtk dotnet test /Users/limeng/Codes/苏永成-蜕变测试系统代码与文档资料/MetBench-V2.1.4_2/MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --filter "FullyQualifiedName~SystemMtExecutionRecorderTests" --no-restore`

Expected:

```text
PASS: evidence 中能看到样本级 trace
```

实际情况（2026-05-24 本轮）：

```text
- unrestricted `dotnet test --filter "FullyQualifiedName~ExecutionEvidenceWriteThroughTests"` 通过：6 tests passed, 0 warnings
- unrestricted `dotnet test --filter "FullyQualifiedName~SystemMtExecutionRecorderTests|FullyQualifiedName~ExecutionEvidenceWriteThroughTests" --no-build` 通过：12 tests passed, 0 warnings
```

- [ ] **Step 5: 提交**

```bash
rtk git add MetBench_BLL.Core/SystemMT/Pipeline/SystemMtExecutionRecorder.cs MetBench_BLL.Core/SystemMT/Persistence/ExecutionEvidence.cs MetBench_BLL.Core/SystemMT/Pipeline/SystemMtPipeline.cs MetBench_SystemMT.Tests/V2Pipeline/SystemMtExecutionRecorderTests.cs
rtk git commit -m "feat: persist sample-level execution traces"
```

### Task 7: 重建并固化新的测试基线

**Files:**
- Modify: `docs/requirements.md`
- Modify: `docs/PROJECT-STRUCTURE.md`
- Modify: `docs/uat/reports/dashboard.md` or current baseline note if needed
- Test: `MetBench_SystemMT.Tests`

- [x] **Step 1: 跑完整测试或当前允许的基线命令**

Run: `rtk dotnet test /Users/limeng/Codes/苏永成-蜕变测试系统代码与文档资料/MetBench-V2.1.4_2/MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --no-restore`

Expected:

```text
- 命令成功
- 若输出有精确计数，则写入文档
- 若仍是 binlog-only mode，则文档写“命令成功，精确通过数待核实”
```

实际情况（2026-05-24 本轮）：

```text
- unrestricted full `dotnet test MetBench_SystemMT.Tests --no-restore` 首次拿到精确结果：960 pass / 1 fail / 8 skip
- 唯一失败：OpenMocOutputAdapterTests.ParseAsync_returns_keff_iterations_and_converged
- 修复 openmoc_output_adapter.py 后，重跑 `--logger "trx;LogFileName=baseline-2026-05-24-current.trx"`：961 pass / 0 fail / 8 skip / 969 total
```

- [x] **Step 2: 回写基线文档**

要求：

```text
- requirements / project-structure 中对基线的叙述统一
- 所有 pass 数都带日期或 commit 语境
- 不再出现互相冲突的“当前唯一 baseline”
```

- [x] **Step 3: 最终一致性检查**

Run: `rtk rg -n "521 Pass|876 pass|965 pass|待核实|当前基线|latest baseline" /Users/limeng/Codes/苏永成-蜕变测试系统代码与文档资料/MetBench-V2.1.4_2/docs`

Expected:

```text
- 历史 baseline 仍可保留
- 但当前 baseline 表述只有一种口径
```

实际情况（2026-05-24 本轮）：

```text
- requirements / project-structure / dashboard 已统一到同一当前口径
- 当前口径：`373bb59` = 最新本地已提交可审计精确绿基线；`763e067` = 前一轮历史精确绿基线；2026-05-24 最新结果 = 961 pass / 0 fail / 8 skip
- 历史 521 / 876 / 965 仍保留在各自历史语境中，不再冒充“当前唯一 baseline”
```

- [x] **Step 4: 提交**

```bash
rtk git add docs/requirements.md docs/PROJECT-STRUCTURE.md docs/uat/reports/dashboard.md
rtk git commit -m "docs: rebuild current test baseline narrative"
```

### Task 8: 最终回归与交付检查

**Files:**
- Modify: none required
- Test: whole repo state

- [ ] **Step 1: 查看工作区状态**

Run: `rtk git status --short --branch`

Expected:

```text
- 只有预期改动
- 没有意外未跟踪文件，必要时把 .codegraph/ 保持本地不纳入提交
```

- [ ] **Step 2: 用图谱做受影响面回看**

Run: `rtk codegraph query SystemMtLauncher`

Run: `rtk codegraph query LauncherCatalogV2Importer`

Run: `rtk codegraph query SystemMtExecutionRecorder`

Expected:

```text
- 确认关键类和对应测试类仍能被图谱稳定检出
- 不把 codegraph affected 作为唯一测试面来源
- 最终补测范围仍以现有测试类和代码阅读交叉确认
```

- [ ] **Step 3: 形成交付说明**

说明至少包含：

```text
- 哪些文档已与 main 对齐
- 哪些运行时问题已解决
- 哪些问题仍待后续阶段处理
- 本轮验证命令及其结果边界
```

- [ ] **Step 4: 提交最终汇总**

```bash
rtk git add AGENTS.md CLAUDE.md docs/PROJECT-STRUCTURE.md docs/requirements.md docs/design/v2-system-mt-architecture.md
rtk git commit -m "docs: align facts and unblock next stage execution"
```

---

## Self-Review

- 本计划覆盖了两类需求：核心文档对齐，以及会继续阻塞 Stage 8 的三类运行时问题。
- 计划没有把“历史文档全量治理”扩进来，范围保持在用户确认的 5 份核心文档。
- 所有当前不可确认的测试通过数都要求以“待核实”表述，不允许伪造精确 baseline。
