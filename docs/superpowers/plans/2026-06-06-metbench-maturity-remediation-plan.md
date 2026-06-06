# MetBench 成熟度修复计划（2026-06-06）

> **状态：In progress（updated 2026-06-06）。** 依据 `docs/superpowers/specs/2026-06-06-metbench-maturity-assessment.md`
> 的 Top 风险。按价值×可行性分 5 个 phase；每 phase 标 cloud/VM、TDD、验收。
> 遵循 CLAUDE.md §11 计划工作流、§9 cloud/VM 分工、§12 PR 门禁。
> **REQUIRED SUB-SKILL**：superpowers:executing-plans，逐 phase TDD-first。
>
> **Progress 2026-06-06：**
> - **P0 done**（PR #322 已合并）：文档漂移、`LegacyResultRecordParityTests`（3 测试入 main）、Domain `Expression` CS8618、IDAL `DatatoImage` 3 处 CS8603 全修。
> - **P1 descoped**（PR #323）：实施时核查发现原始风险"38 MR 仅 3 个 CI 真跑"是误读 V12 内部 fixture 计数当全覆盖。实测 `LauncherEndToEnd*Tests.cs` 15 个、38/38 MR 全有真 `RunAsync`/BDD 步骤、CI 内 32 pass / 8 env-gated skip。**P1 无真实缺口，撤销**。
> - **P2 done**（PR #324）：BLL §6 违规清理 —— 2 处 CS1998 假异步（`CopyCandidateProgrmsToDestination`）改 `Task.FromResult` + 3 处 CS0168 静默吞异常加 `Debug.WriteLine`，最小修改。
> - **P3 done**（PR #325 #326 #327）：遗留层警告棘轮全 4 层完成：Domain ✅（30 处 CS8618 修零 + TWAE）；IDAL ✅（28 处修零 + TWAE）；DAL ✅（CS8766/8618/8625 修真问题，CS0618 file-level pragma 隔离有意 v1 兼容，+ TWAE）；BLL 棘轮带白名单 ✅（224 现存债登记可见，非白名单警告码 fail-build，特别 CS0168/CS1998 严守 P2 已修；CI ubuntu-24.04 SDK 多分析 CS8601 一处，已补白名单）。
> - **P4 active**（VM 待运行）：WPF 死锁面提示词已就绪 `docs/superpowers/vm-prompts/2026-06-06-p4-wpf-deadlock-surface-vm-prompt.md` —— 18 处 `.ShowDialogAsync().Result` + 1 处 `async void` 精确清单 + 修复模式 + 验证步骤。VM 端 Claude 执行。
> - **下一步**：等 P4 VM 证据回；之后 P5 T6 变异落地。

## 目标 & 验收总纲

把评估发现的 7 项风险收敛为可执行、可验证、不改行为（除明确的缺陷修复外）的修复链。
**不做投机式重构、不扩大范围**（CLAUDE.md §0.5）。每个 cloud PR 必须全量回归绿 + CI 双绿；
WPF 项交 VM 提示词。**完成定义**：风险逐条要么修复并守护、要么显式 descope 并记账。

## Phase 0 — 快速高价值云端修复（低风险，先落）

目标：清掉"小而真"的缺陷与文档漂移。每项独立可测。

- [ ] **0.1 文档漂移修正**（risk 7-doc，cloud，docs-only）
  - CLAUDE.md §4：Stylet 改为"现用于 10 个 XAML 文件（清单见 assessment），收敛为单文件是 follow-up"；
    HandyControl 段改为"已由 `Controls/SimplePagination.xaml` 替代移除"。
  - 验收：grep 核对 §4 文字与实测一致。
- [ ] **0.2 `LegacyResultRecordParityTests`**（risk 6，cloud，TDD）
  - 给 `SystemMtResultRecord` 的两条写路径（legacy `SaveAsync(string,SystemMtResult)` 镜像 vs `SaveAsync(record)`）
    加 parity 测试，断言两路产出的 record 字段逐一相等（接 §12.4 R1 / §12.5 表内"planned"行）。
  - 验收：新测试存在且绿；§12.5 表把该行从 planned 改为 active。
- [ ] **0.3 Domain `Expression` NPE 风险**（risk 7，cloud，最小修改）
  - `MetamorphicRelation.Expression`（CS8618）：加 `= string.Empty;` 默认值（与同类字段一致），消除 LiteDB 反序列化 NPE。
  - 仅改该字段；不顺手改其它。验收：构建无该 CS8618；不引入行为变化（默认空串）。
- [ ] **0.4 IDAL `DatatoImage.cs` 归位评估**（risk 7，cloud，调研→小动作）
  - 先确认调用方；若仅 BLL/Client 用图表渲染，记录"应移至 BLL.Core/Reporting 或 BLL"为 follow-up（移动可能触发 WPF 引用→VM 验证），本 phase 只**修 3 处 CS8603**（加 `?? string.Empty` / 可空标注），不移动文件。
  - 验收：CS8603 消除；移动决策记入计划"不交付"。

## Phase 1 — ~~MR 运行时覆盖门~~ **DESCOPED 2026-06-06**

> **撤销原因（CLAUDE.md §0 / §6 据实记录）**：实施 P1 前调研发现原始风险陈述
> "38 MR 仅 3 个 CI 真跑"是误读 —— 把 `V12CoverageGateTests.RunnableFixtureCount = 3`
> （v1.2 typed-catalog 内 golden-fixture 计数）当成全 Launcher 覆盖率。
> **实测**：`MetBench_SystemMT.Tests/SystemMT/Launcher/LauncherEndToEnd*Tests.cs` 共
> 15 个文件，38/38 MR 全有 `RunAsync("<mr-id>")` 直接执行测试或 BDD `When/Then` 步骤；
> 跑 `dotnet test --filter "FullyQualifiedName~LauncherEndToEnd"` = **32 passed / 8 skipped**
> （8 个 env-gated：OpenMOC/OpenMC/scipy）。MR 运行时覆盖**已经是 Hardened 级**，不需要本 phase。
>
> **学到的教训**：未来在评估文档里写大颗粒结论前，必须 grep file:line 多重验证；
> 单个指标（V12 = 3）跨语义解释（"全覆盖 = 3"）是评估失误。
>
> 唯一保留作 follow-up 的相关项：**P1.2 `CoverageService` 4 维边界测试**（empty-bug-repo /
> zero-campaign / all-unbound-matrix）—— 这是真实的薄测试覆盖，但优先级低，未列入本计划主链。

## Phase 2 — BLL §6 违规与 NPE 清理（risk 2，cloud，TDD/最小修改）

目标：把"质"的债（吞异常/假异步/NPE）修掉；这比纯警告数更危险。

- [ ] **2.1 静默吞异常**：`SemanticSimilarityDetector.cs:368/60/230`、`SupportRateCalculator.cs:292` 等
  `catch(ex){return false}` —— 按 §6 显式报错：要么向上抛、要么记录并返回带原因的结果；不得裸吞。
  逐处评估调用方契约，最小修改。
- [ ] **2.2 假异步**：`SyntaxSimilarityDetector.cs:392` async 无 await —— 去掉 async（同步实现）或补真 await。
- [ ] **2.3 真 NPE 路径**：`SupportRateCalculator.cs` CS8600/8603 处加防护或可空标注。
- [ ] 每处尽量补/改一个断言其新行为的测试（§12.4 R4）。
- [ ] 验收：上述文件 CS0168/CS1998/CS8602/8603 清零；全量回归绿；行为变化（如改为抛异常）有测试覆盖并在 PR 说明。

## Phase 3 — 遗留层警告棘轮（risk 5，cloud，分批）

目标：让债收敛（只降不升）。按债从小到大、风险从低到高推进。

- [ ] **3.1 DAL**：先把**有意的 CS0618**（v1 读兼容）用 `#pragma warning disable CS0618`（精确包裹+注释原因）
  与真警告区分开；修真警告；然后 `MetBench_DAL.csproj` 上 `<TreatWarningsAsErrors>true`。
- [ ] **3.2 Domain + IDAL**：清零自身警告后各自上棘轮。
- [ ] **3.3 BLL**：最大债（458），放最后；可先上棘轮 + `WarningsNotAsErrors` 白名单逐步缩小，或分文件清零。
  本 phase **可只到 DAL/Domain/IDAL**，BLL 作为 Phase 3b 续作（视工时）。
- [ ] 验收：每个上棘轮的项目 `dotnet build` 0 errors；CI 绿。

## Phase 4 — WPF 异步/死锁清理（risk 3，VM 提示词）

目标：消除 18+ 处 `.ShowDialogAsync().Result` 死锁面 + `async void`。

- [ ] **4.1** cloud 写 VM 提示词 `docs/superpowers/vm-prompts/`：枚举 8 个 ViewModel 的 `.Result` 调用点与
  `async void HandleSelectionChange`，逐处改为 `await`（命令改 `[RelayCommand] async Task`）；VM 本地编译+冒烟+截图验证。
- [ ] **4.2** VM 执行并回 PR；cloud 消费证据。
- [ ] 验收：WPF build 0 errors；目标调用点无 `.Result`/`async void`（除 `OnNavigatedTo`）；交互冒烟无死锁。
- 注：5 套 MVVM 框架收敛是更大的独立重构，本 phase 只除死锁面，框架收敛记为 follow-up。

## Phase 5 — T6 变异落地（risk 4，cloud 设计先行 + 可能 VM）

目标：把 T6 从原型推进到 Functional（或显式标注原型）。

- [ ] **5.1 设计**：`IMutantApplicator`（把 `Mutant.AppliedDiff` 应用到 SUT 副本）+ 真 `MutationCellRunner`
  （应用变异→经 `ISystemMtLauncher` 跑 MR suite→判 killed/survived）。先出 spec。
- [ ] **5.2 实现 + TDD**：cloud 实现 applicator + cellRunner，fake SUT 测试；WPF 把 `StubCellRunner` 换成真实现（VM）。
- [ ] **5.3 最小 MR 完备子集搜索**：基于 kill 矩阵的贪心/集合覆盖算法（独立子项）。
- [ ] 验收：真变异体能被真 MR 杀；WPF 不再用 hash stub。
- 若工时不足：**显式在 UI/文档标注 T6 为"原型（编排壳）"**（接 §6 不粉饰），避免名实不符。

## Phase 6 — 链尾整体复审 + 收尾（cloud）

- [ ] 各 phase 为 ≥3-PR 链则按 §12.4 R2 跑 chain-end fresh-session review。
- [ ] 更新 `docs/status/current.md`：把已收敛的风险标 Controlled / 把 descope 的显式记账；同步本计划与 AGENTS.md。

## Cloud / VM 分工

| Phase | 主体 |
|---|---|
| 0,1,2,3,5(设计+引擎) | Cloud |
| 4，5(WPF cellRunner 替换) | VM（cloud 出提示词） |

## 排期建议（价值优先）

Phase 0（快赢）→ Phase 1（覆盖门，最高价值）→ Phase 2（§6 质量）→ Phase 3（棘轮）→ Phase 4（VM 死锁）→ Phase 5（T6，最大）。
每 phase 独立可交付；可按需停在任一 phase 边界。

## 不交付（明确排除/记账）

- 5 套 MVVM 框架收敛（大重构，单列）。
- `DatatoImage.cs` 物理移动（触发 WPF 引用，需 VM；本计划只修其警告）。
- T4 生产 LLM key / `EmpiricalRepoSampler` 接线（配置/部署事项，非代码缺陷；若需另立）。
- P2 历史边界项（结果/证据导入、资产 live 提升）—— 仍按既有决定排除。

## Self-Review

- 每条风险来自 assessment 的 file:line 证据，非凭记忆。
- cloud/VM 分工明确；WPF 不在云端编译。
- 缺陷修复（§6/NPE）与纯清理（棘轮）分 phase，互不混。
- T6 给了"做或显式标原型"两条诚实出路（§6）。
