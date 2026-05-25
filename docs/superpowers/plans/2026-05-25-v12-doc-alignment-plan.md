# Stage 8 V1.2 文档一致性对齐 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 让 `CLAUDE.md`、`AGENTS.md`、`docs/requirements.md`、`docs/PROJECT-STRUCTURE.md` 与当前 `origin/main` 提交链（`8bd734f`）以及 MR 验证统一设计 v1.2 的已合并状态保持一致。

**Architecture:** 本轮只修“事实源”文档，不改运行时代码。对齐分两层：先统一主线基线（提交链、测试基线、Stage 8 当前状态），再把 v1.2 路线从“外部设计稿”提升为主仓路线图中的正式子主线，并明确当前只合入到 PR-6。

**Tech Stack:** Markdown / GitHub PR history / dotnet test baseline artifacts

---

### Task 1: 固定当前共享事实源

**Files:**
- Modify: `CLAUDE.md`
- Modify: `AGENTS.md`
- Modify: `docs/requirements.md`
- Modify: `docs/PROJECT-STRUCTURE.md`
- Check: `docs/superpowers/specs/2026-05-25-mr-verification-v1.2-codex-ready.md`
- Check: `docs/superpowers/plans/2026-05-25-mr-verification-v12-master-roadmap.md`

- [ ] **Step 1: 把所有“当前 main / origin/main”引用统一到 `8bd734f`**

```text
origin/main 当前提交链头 = 8bd734f
PR #97 = PR-0
PR #98 = v1.2 全量计划
PR #99 = PR-1
PR #100 = PR-2
PR #101 = PR-3
PR #102 = PR-4
PR #103 = PR-5
PR #104 = PR-6
```

- [ ] **Step 2: 把共享测试基线统一到 PR-6 合并后的精确值**

```text
最新共享精确绿基线：8bd734f / PR #104
dotnet test MetBench_SystemMT.Tests --no-restore
= 1015 pass / 0 fail / 0 skip
```

- [ ] **Step 3: 去掉“origin/main 仍停在 5691727 / 373bb59 仅本地已提交基线 / Windows 回执待补导致当前主线待核实”这类过期表述**

```text
这些说法在 PR #95、#97-#104 合并后已经失效。
保留历史基线可以，但必须降级为“历史参考”，不能再写成当前共享事实。
```

### Task 2: 把 v1.2 从设计稿提升为 Stage 8 的正式子主线

**Files:**
- Modify: `AGENTS.md`
- Modify: `CLAUDE.md`
- Modify: `docs/requirements.md`
- Modify: `docs/PROJECT-STRUCTURE.md`
- Check: `docs/superpowers/specs/2026-05-25-mr-verification-v1.2-codex-ready.md`

- [ ] **Step 1: 在 Stage 8 路线图中新增“MR 验证统一设计 v1.2”子主线**

```text
定位：
- 属于 Stage 8 的执行 IR / verification runtime 收敛
- 不是替代 catalog convergence，而是在其上继续推进 typed semantic model
```

- [ ] **Step 2: 明确当前只交付到 PR-6**

```text
已合并：
- PR-0 catalog foundation
- PR-1 typed model + fail-closed validators
- PR-2 scalar runtime + scaled equality
- PR-3 applicability + 5 态状态
- PR-4 reference / convergence
- PR-5 sequence shape + subadditive
- PR-6 field + derived invariant

未合并：
- PR-7 statistical + cross-method
- PR-8 property runtime
- PR-9 exponential growth runtime
- PR-10 migration + coverage gates
```

- [ ] **Step 3: 在 requirements / project-structure 中体现 V12Catalog 已成为正式代码面**

```text
必须至少反映：
- `MetBench_BLL.Core/SystemMT/V12Catalog/` 已不是 proposal-only
- 已有 scalar / applicability / convergence / sequence / field / derived invariant runtime
- 测试总数已提升到 1015
```

### Task 3: 文档自检与提交准备

**Files:**
- Modify: `docs/superpowers/plans/2026-05-25-v12-doc-alignment-plan.md`
- Check: `CLAUDE.md`
- Check: `AGENTS.md`
- Check: `docs/requirements.md`
- Check: `docs/PROJECT-STRUCTURE.md`

- [ ] **Step 1: 做一致性自检**

Run: `rtk rg -n "5691727|373bb59|961 pass|969 total|待补 Windows 回执|origin/main 当前仍停在" CLAUDE.md AGENTS.md docs/requirements.md docs/PROJECT-STRUCTURE.md`
Expected: 不再把这些旧状态写成“当前共享事实”

- [ ] **Step 2: 确认 v1.2 主线表述只声明到 PR-6**

Run: `rtk rg -n "PR-7|PR-8|PR-9|PR-10|已合并|未合并|1015 pass|8bd734f" CLAUDE.md AGENTS.md docs/requirements.md docs/PROJECT-STRUCTURE.md`
Expected: 仅把 PR-7..PR-10 标为未完成 / 后续计划

- [ ] **Step 3: 提交**

```bash
rtk git add CLAUDE.md AGENTS.md docs/requirements.md docs/PROJECT-STRUCTURE.md docs/superpowers/plans/2026-05-25-v12-doc-alignment-plan.md
rtk git commit -m "docs(stage8): align v1.2 roadmap with main baseline"
```
