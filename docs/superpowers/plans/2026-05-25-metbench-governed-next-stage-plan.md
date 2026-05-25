# MetBench Governed Next-Stage Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 在继续编码前，先把 MetBench 下一阶段的控制逻辑、活跃计划索引、验证语义收敛和证据模型终态设计补齐，确保后续开发不偏航。

**Architecture:** 本计划先做治理和消歧，再做实现。顺序固定为 `governance -> active-plan index -> semantic convergence design -> evidence design -> windows verification rules -> implementation backlog`。在前四项未完成前，不得继续扩新的 assertion/runtime 代码。

**Tech Stack:** Markdown specs/plans / GitHub PR workflow / Cloud + Windows dual-environment / current MetBench .NET 8 codebase

---

## Phase 0: 控制规则与活跃计划索引

### Task 1: 固化项目控制规则

**Files:**
- Create: `docs/superpowers/specs/2026-05-25-metbench-project-control-rules.md`
- Modify: `AGENTS.md`
- Modify: `CLAUDE.md`

- [ ] **Step 1: 在仓库中写入控制规则文档**

要求至少覆盖：

- 单一事实源层级
- 活跃计划注册制度
- PR 闸门
- 两层 review
- 状态刷新制度
- 双环境回执规则

- [ ] **Step 2: 在 `AGENTS.md` 中加入指向控制规则的入口**

要求：

- 放在“详细计划”或“执行规则”附近
- 明确写“后续执行以控制规则为准”

- [ ] **Step 3: 在 `CLAUDE.md` 中加入一句执行约束**

要求：

- 明确历史计划不是当前状态真相层
- 当前执行先看控制规则 + 四份核心事实源

- [ ] **Step 4: Run review**

Run: `rtk rg -n "控制规则|project control rules|单一事实源" AGENTS.md CLAUDE.md docs/superpowers/specs/2026-05-25-metbench-project-control-rules.md`
Expected: 入口和规则文档互相可追踪

### Task 2: 建立活跃计划索引

**Files:**
- Create: `docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md`
- Check: `docs/superpowers/plans/*.md`

- [ ] **Step 1: 列出当前仍有效的活跃计划**

至少分成：

- Active
- Historical
- Closed but reference-worthy

- [ ] **Step 2: 为每份活跃计划标记用途**

至少说明：

- 它解决什么问题
- 何时失效
- 它依赖哪份 spec

- [ ] **Step 3: 明确以下文档不再作为当前活跃计划**

- `2026-05-21-next-stage-development-plan.md`
- `2026-05-25-v12-doc-alignment-plan.md`
- 已完成的 `v12-pr1` 到 `v12-pr10` 执行计划

- [ ] **Step 4: Run review**

Run: `rtk sed -n '1,220p' docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md`
Expected: 读者无需翻历史文档即可知道当前只该跟哪几份计划走

---

## Phase 1: 编程前消歧设计

### Task 3: 验证语义收敛设计

**Files:**
- Create: `docs/superpowers/specs/2026-05-25-metbench-verification-semantics-convergence-design.md`
- Check: `MetBench_BLL.Core/SystemMT/IMrAssertion.cs`
- Check: `MetBench_BLL.Core/SystemMT/Assertions/*`
- Check: `MetBench_BLL.Core/SystemMT/V12Catalog/*`

- [ ] **Step 1: 写清 legacy assertion 与 v1.2 verifier 的正式关系**

设计必须回答：

1. 当前正式语义主路径是谁  
2. legacy 路径保留到什么时候  
3. 哪些 MR/Property 只能进 v1.2  
4. 哪些现有执行入口仍依赖 legacy

- [ ] **Step 2: 写清 `ScaledEquality` 与 `ApproxEqualAssertion` 的边界**

至少回答：

- 标量等式与缩放等式是否共用阈值模型
- `EqualityThresholds` 是否只服务 legacy，还是迁入统一配置模型
- `flw = k * src` 最终挂在哪条正式路径

- [ ] **Step 3: 写清迁移策略**

至少给出：

- 保守方案
- 收敛方案
- 推荐方案

- [ ] **Step 4: Run review**

Run: `rtk rg -n "ScaledEquality|ApproxEqualAssertion|EqualityThresholds|IMrAssertion|V12Catalog" docs/superpowers/specs/2026-05-25-metbench-verification-semantics-convergence-design.md`
Expected: 关键边界都有显式裁决

### Task 4: Execution Evidence v2 设计

**Files:**
- Create: `docs/superpowers/specs/2026-05-25-metbench-execution-evidence-v2-design.md`
- Check: `MetBench_BLL.Core/SystemMT/Persistence/ExecutionEvidence.cs`
- Check: `MetBench_BLL.Core/SystemMT/Persistence/ExecutionSampleTrace.cs`
- Check: `MetBench_BLL.Core/SystemMT/Pipeline/SystemMtExecutionRecorder.cs`

- [ ] **Step 1: 先从消费者倒推证据模型**

必须逐项回答：

- replay 需要什么字段
- anomaly 需要什么字段
- report 需要什么字段
- UI 需要什么字段

- [ ] **Step 2: 定义终态证据粒度**

至少明确：

- target-field trace
- multi-variable trace
- field worst-offender
- missing observable diagnostics
- retained vs derived evidence

- [ ] **Step 3: 定义 schema 演进约束**

至少明确：

- 哪些字段追加即可
- 哪些字段需要 migration
- 哪些信息不应进入持久化

- [ ] **Step 4: Run review**

Run: `rtk rg -n "replay|anomaly|report|UI|trace|field|worst" docs/superpowers/specs/2026-05-25-metbench-execution-evidence-v2-design.md`
Expected: 使用者驱动的证据模型清晰可执行

### Task 5: Windows 验证制度设计

**Files:**
- Create: `docs/superpowers/specs/2026-05-25-metbench-windows-verification-policy.md`
- Check: `docs/uat/runbooks/windows-uat-round-1.md`
- Check: `docs/requirements.md`

- [ ] **Step 1: 定义必须补 Windows 回执的变更类型**

至少包括：

- `MetBench_Client`
- WPF 配置绑定
- `App.xaml.cs`
- page/viewmodel wiring
- Windows-only report/UI paths

- [ ] **Step 2: 定义三档回执**

- build-only
- run-and-log
- UI-visible validation

- [ ] **Step 3: 定义回执落位**

至少说明：

- 何时写进 requirements
- 何时写进 runbook
- 何时只在 PR 留痕

- [ ] **Step 4: Run review**

Run: `rtk sed -n '1,220p' docs/superpowers/specs/2026-05-25-metbench-windows-verification-policy.md`
Expected: Windows 验证从经验变成制度

---

## Phase 2: 进入下一轮实现前的主计划刷新

### Task 6: 生成新的唯一活跃主计划

**Files:**
- Create: `docs/superpowers/plans/2026-05-25-metbench-stage8-stage9-transition-plan.md`
- Check: `docs/superpowers/specs/2026-05-25-metbench-project-control-rules.md`
- Check: `docs/superpowers/specs/2026-05-25-metbench-verification-semantics-convergence-design.md`
- Check: `docs/superpowers/specs/2026-05-25-metbench-execution-evidence-v2-design.md`
- Check: `docs/superpowers/specs/2026-05-25-metbench-windows-verification-policy.md`

- [ ] **Step 1: 把下一阶段实现工作重排成单一主计划**

只允许包含：

- assertion semantics 收敛
- evidence v2 implementation
- Windows verification policy 落地
- 配置接线
- 之后才允许继续扩新功能

- [ ] **Step 2: 把计划分为“设计已清晰”和“禁止编码”两类**

要求：

- 设计未清晰的任务必须写成 blocked
- 不得把 blocked 项写进可直接开发的任务序列

- [ ] **Step 3: 定义首批可实施 backlog**

预期第一批只允许：

1. 活跃计划索引落地  
2. 控制规则接入核心文档  
3. Windows 验证 policy 接线  
4. 配置绑定的设计确认后实现

- [ ] **Step 4: Run review**

Run: `rtk sed -n '1,260p' docs/superpowers/plans/2026-05-25-metbench-stage8-stage9-transition-plan.md`
Expected: 新主计划不再依赖旧假设，也不再存在“边实现边拍板”的任务

---

## Phase 3: 进入编码前的总验收

### Task 7: 编码前总闸门检查

**Files:**
- Check: `docs/superpowers/specs/2026-05-25-metbench-project-control-rules.md`
- Check: `docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md`
- Check: `docs/superpowers/specs/2026-05-25-metbench-verification-semantics-convergence-design.md`
- Check: `docs/superpowers/specs/2026-05-25-metbench-execution-evidence-v2-design.md`
- Check: `docs/superpowers/specs/2026-05-25-metbench-windows-verification-policy.md`
- Check: `docs/superpowers/plans/2026-05-25-metbench-stage8-stage9-transition-plan.md`

- [ ] **Step 1: 检查控制规则、活跃计划索引、三份消歧设计、唯一主计划都已存在**

Run: `rtk ls docs/superpowers/specs docs/superpowers/plans`
Expected: 上述文档全部存在

- [ ] **Step 2: 检查核心文档是否引用了新的治理入口**

Run: `rtk rg -n "project control rules|active plan index|windows verification policy|semantics convergence|evidence v2" AGENTS.md CLAUDE.md docs/requirements.md docs/PROJECT-STRUCTURE.md`
Expected: 至少 `AGENTS.md` 能追踪到新的治理入口

- [ ] **Step 3: 只有全部通过后，才允许开启下一轮编码**

```text
If any of the following is missing:
- control rules
- active plan index
- semantic convergence design
- evidence v2 design
- windows verification policy
- single active transition plan

Then: stop before implementation.
```

