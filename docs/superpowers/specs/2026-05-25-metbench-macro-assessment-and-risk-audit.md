# MetBench 全貌版图评估与隐患审计

> **日期**: 2026-05-25  
> **方法**: 基于当前 `origin/main`、四份核心事实源、现行计划文档、测试基线与代码结构的顶层评估。  
> **目标**: 判断项目逻辑是否合理、已完成项是否达到验收、进行中计划是否足够清晰，以及未开发拼图需要怎样补计划才能避免偏差。

---

## 1. 当前全貌版图

### 1.1 主体结构

MetBench 当前已形成四层结构：

1. **运行与验证内核**
   - `MetBench_BLL.Core/SystemMT/`
   - 含 launcher、pipeline、catalog、persistence、assertion、v1.2 typed verifier
2. **领域与持久化**
   - `MetBench_Domain/`
   - `MetBench_IDAL/`
   - `MetBench_DAL/`
3. **发现、覆盖、变异、报告**
   - `MetBench_BLL.Core/Discovery/`
   - `Coverage/`
   - `Mutation/`
   - `Reporting/`
4. **WPF 客户端**
   - `MetBench_Client/`
   - Windows-only，作为 UI 和人工验收入口

### 1.2 运行模式

当前仓库实际上运行在两条轨道上：

- **Cloud 主轨**：跨平台逻辑、测试、CI、基线、typed IR、迁移 gate
- **Windows 辅轨**：WPF 编译、界面验证、Windows-only 回执

这个分层在顶层逻辑上是合理的，因为它把“能稳定自动化的部分”和“必须 Windows 现场确认的部分”分开了。

---

## 2. 顶层逻辑合理性评估

## 2.1 合理的地方

### A. 架构边界总体成立

`SystemMtLauncher`、`SystemMtPipeline`、LiteDB repository、typed `V12Catalog` IR、WPF `MetBench_Client` 的职责分界清楚，说明当前主线已经从“概念堆叠”进入“可维护系统”。

### B. v1.2 采用 staged rollout 是正确的

把 typed semantic model、validator、runtime、property、migration gate 分拆成 PR-0..PR-10 的方式，是当前仓库最合理的演进策略。它避免了一次性重写运行主路径，也让每个语义层都有测试锚点。

### C. 监控和执行开始收束到四份核心事实源

PR #111 之后，`AGENTS.md`、`CLAUDE.md`、`requirements`、`PROJECT-STRUCTURE` 已重新一致。这是项目从“能做功能”走向“能持续控制”的必要条件。

## 2.2 逻辑上仍然脆弱的地方

### A. 仍存在双验证语义栈

仓库里现在并存：

- 旧 `IMrAssertion` / `AssertionEvaluator` 路线
- 新 `V12Catalog` typed predicate / kernel 路线

这在演进期是可接受的，但如果不尽快定义二者的边界与收敛策略，后续会出现：

- 同一 MR 语义在两套系统中表达不一致
- 新 MR 走 v1.2，旧 launcher 仍走 legacy assertion
- 监控无法明确“哪条路径代表正式验证”

### B. 运行证据闭环只有一半

`ExecutionEvidence.SampleTraces` 已进入主线，但目前只是目标字段级 trace。对于复杂 MR，现有证据仍不足以支持强复盘、强诊断和审计级解释。

### C. 计划体系存在“活跃/历史未分层”问题

仓库里仍保留大量旧计划，其中有些内容仍引用旧基线、旧分母或旧阶段判断。它们作为历史记录没有问题，但若被当成当前执行依据，就会直接造成偏差。

结论：**架构主逻辑是合理的，但控制逻辑必须比现在更强，否则项目会再次进入“实现比状态管理走得快”的不稳定区。**

---

## 3. 已完成工作是否符合验收标准

## 3.1 符合验收标准

### T0 核心 System-MT 流程

已达到“流程端到端走通”的验收目标。当前不应再把“覆盖未满”算作 T0 未完成，因为覆盖已独立归入 T3。

### catalog convergence / runtime alignment

已完成“单一 provider 主路径 + metadata/evidence 写入 + importer 去耦 + 文档同步”的核心验收。剩余问题不是“没做完”，而是“证据粒度与 Windows 回执仍待增强”。

### v1.2 路线图

就当前路线图定义而言，已完成闭环。typed schema、fail-closed validator、runtime kernel、property path、migration gate、golden fixtures、coverage gate 与 retrospective review-fix 都已并入主线。

## 3.2 仅部分符合验收标准

### WPF / Windows 侧

Cloud 侧与文档侧都已经推进，但 Windows 侧最新主线代码的持续回执机制还不稳定。目前只有历史构建回执与阶段性验证，并非每次主线变化都有对应 Windows 证据。

### 样本级证据

现在能说“已有 evidence path”，不能说“已有审计级 sample-level 可复盘链路”。

---

## 4. 进行中计划的详细度与规范性评估

## 4.1 可以继续保留为有效计划的

### `2026-05-25-mr-verification-v12-master-roadmap.md`

这份计划的结构化程度高、分 PR 清楚、测试/边界/验收定义完整。它已经完成使命，现在应归类为“已执行完成的主路线图”，不应继续作为下一阶段活跃计划。

### `2026-05-25-mr-verification-v12-pr1` 到 `pr10`

这些文档可保留为高质量历史执行样板。它们证明当前团队已经具备“先定义边界，再用 TDD 和 PR 切分推进”的能力。

## 4.2 不能继续直接拿来当活跃计划的

### `2026-05-21-next-stage-development-plan.md`

这份计划的判断前提已经落后于当前主线：

- 它仍把 T3/T4 视为最大缺口
- 它的阶段排序写在 v1.2 完成前
- 它没有纳入 `44 MR + 4 Property`、PR #110 review-fix、PR #111 文档对齐这些新事实

结论：**这份计划只能作为历史参考，不能继续充当当前主计划。**

### `2026-05-25-v12-doc-alignment-plan.md`

它是阶段性修复计划，不是长期执行依据，而且文中基线已落后于最新主线。应归档为历史文档，不应再被监控读取为活跃状态。

---

## 5. 仍缺失的拼图

当前真正缺的不是“再写一个大计划”，而是补这四块缺口：

### A. 验证语义收敛计划

需要明确：

- legacy `IMrAssertion` 路径还能存在多久
- 新 `V12Catalog` 路径何时成为唯一正式验证语义
- `ScaledEquality` 与旧 `ApproxEqualAssertion`、`EqualityThresholds` 如何收敛

### B. 证据粒度升级计划

需要明确：

- `SampleTraces` 的正式目标粒度
- 多变量 / 多路径 / field 级 worst-offender 如何持久化
- 这些证据如何供 replay、异常调查、报告和 UI 共用

### C. Windows 验证制度化计划

需要明确：

- 何类 PR 必须补 Windows 回执
- 哪些回执只需 build，哪些需要 UI 观察
- 回执如何进入主线文档或 runbook

### D. 活跃计划索引

需要有一份“当前只认这些计划”的索引，否则历史计划过多会持续制造歧义。

---

## 6. deep-research 风格隐患挖掘

以下隐患不是小瑕疵，而是会直接影响可用性、进度和质量的顶层风险。

### 隐患 1：验证语义分裂

**现象**：

- legacy assertion 栈仍在
- v1.2 typed verifier 栈已完成
- 仓库没有一份正式设计把两者的长期关系写死

**为什么危险**：

- 新功能可能继续写进旧 assertion 栈，形成第三套语义
- MR 与 Property 的正式执行口径会再次分叉
- 测试虽然都绿，但“绿的是哪套语义”会越来越难解释

**建议方案**：

先写“验证语义收敛设计”，只回答三件事：

1. 谁是正式主路径  
2. 谁是过渡兼容层  
3. 何时、以什么验收条件下线旧路径

在这个设计定稿前，不继续扩 assertion 语义实现。

### 隐患 2：证据模型目标不清

**现象**：

- 已有 `ExecutionEvidence`
- 已有 `SampleTraces`
- 但缺“终态设计”：到底要做到 diagnostic-friendly 还是 audit-grade

**为什么危险**：

- 如果边实现边决定粒度，后续 schema 会反复变
- replay、anomaly、report、UI 可能各自要求不同字段
- 过早编码会造成二次迁移和回填成本

**建议方案**：

先写“Execution Evidence v2 设计”，把使用者反推清楚：

- replay 需要什么
- anomaly 需要什么
- report 需要什么
- UI 需要什么

按消费者倒推证据结构，再编码。

### 隐患 3：Windows 验证仍是人工经验，不是项目制度

**现象**：

- 已有双环境思路
- 但仓库里没有一份把“何时必须补 Windows 回执”写成项目规则的活跃文档

**为什么危险**：

- 人一忙就只看 Cloud 绿不绿
- WPF 回归会被长期滞后发现
- 监控会继续混写“旧 Windows 回执”和“新主线状态”

**建议方案**：

把 Windows 验证规则放进控制规则与下一阶段计划里，变成 merge gate 的一部分，而不是经验提醒。

### 隐患 4：计划入口过多，容易误取历史文档

**现象**：

- 仓库里计划文档多
- 一部分是历史事实
- 一部分是仍有价值的设计记录
- 但没有“当前活跃计划索引”

**为什么危险**：

- 监控、开发、评审会从不同文件读到不同结论
- 用户以为项目在执行 A，实际工程师拿的是 B

**建议方案**：

建立一份活跃计划索引，并在控制规则中写明：

- 无索引引用的计划一律视为历史计划

---

## 7. 顶层推进建议

后续不能再从“实现想法”开始，而要按下面顺序推进：

1. **先定控制规则**
2. **再定活跃计划索引**
3. **再定验证语义收敛设计**
4. **再定 evidence v2 设计**
5. **再定 Windows 验证制度**
6. **最后才允许继续编程**

这不是保守，而是降低返工和伪进度的最短路径。

---

## 8. 总结裁决

### 总体裁决

MetBench 当前不是“设计错误”，而是“阶段切换完成后，控制系统必须升级”。

### 可立即确认的结论

- 架构主逻辑合理
- 已完成主线大体符合验收标准
- v1.2 已不是主要风险源
- 当前最大风险已转移到 **控制逻辑、语义收敛、证据终态、Windows 制度化验证**

### 编程前必须消除的隐患

1. 验证语义收敛未设计清楚
2. 证据模型终态未设计清楚
3. Windows 回执制度未写成规则
4. 活跃计划索引不存在

这些不先解决，后续编程速度越快，偏差只会越大。
