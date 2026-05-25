# MetBench 活跃计划索引

> **日期**: 2026-05-25
> **状态**: 生效
> **目的**: 明确哪些计划仍指导当前开发，哪些只是历史记录，防止执行和监控误取旧计划。

---

## 1. 当前唯一活跃主计划

| Plan | Status | Scope | Expiry condition |
|---|---|---|---|
| `docs/superpowers/plans/2026-05-25-metbench-governed-next-stage-plan.md` | Active | Governance-first next-stage planning; blocks further implementation until the named design ambiguities are resolved | Expires when semantic-convergence, Evidence v2, Windows verification policy, and transition plan are completed and replaced by a new implementation plan |

Any new coding task must be derived from the active master plan or from a successor plan registered here.

---

## 2. 当前活跃设计文档

| Document | Status | Scope | Expiry condition |
|---|---|---|---|
| `docs/status/current.md` | Active | Single current-status ledger for monitoring, handoff, baseline, active plan, and open risks | Expires only when replaced by a newer status ledger path |
| `docs/superpowers/specs/2026-05-25-metbench-project-control-rules.md` | Active | Control charter: source hierarchy, gates, review, status refresh | Expires only by explicit replacement PR |
| `docs/superpowers/specs/2026-05-25-metbench-macro-assessment-and-risk-audit.md` | Active | Macro assessment and risk audit for the Stage 8 to next-stage transition | Expires when the next macro assessment supersedes it |
| `docs/superpowers/specs/2026-05-25-mr-verification-v1.2-codex-ready.md` | Active reference | v1.2 typed semantic model and verifier design already implemented through the current roadmap | Expires when a v1.3 verification design supersedes it |
| `docs/superpowers/templates/pr-gate-checklist.md` | Active | Required PR gate checklist for scope, facts, tests, Windows classification, review, and merge | Expires only by explicit replacement PR |

### 条件性活跃

| Document | Status | Scope | Expiry condition |
|---|---|---|---|
| `docs/superpowers/specs/2026-05-24-systemmt-catalog-convergence-design.md` | Conditional reference | Catalog convergence design history and evidence-model context | Expires when Evidence v2 design supersedes the relevant sections |
| `docs/superpowers/plans/2026-05-24-systemmt-catalog-convergence-plan.md` | Conditional reference | Catalog convergence implementation history | Expires when the active transition plan no longer references it |

---

## 3. 已完成、可参考但不再活跃的计划

### v1.2 执行链

- `docs/superpowers/plans/2026-05-25-mr-verification-v12-master-roadmap.md`
- `docs/superpowers/plans/2026-05-25-mr-verification-v12-pr1-typed-model-validators.md`
- `docs/superpowers/plans/2026-05-25-mr-verification-v12-pr2-runtime-scalar-kernels.md`
- `docs/superpowers/plans/2026-05-25-mr-verification-v12-pr3-applicability-status.md`
- `docs/superpowers/plans/2026-05-25-mr-verification-v12-pr4-reference-convergence.md`
- `docs/superpowers/plans/2026-05-25-mr-verification-v12-pr5-sequence-shapes-subadditive.md`
- `docs/superpowers/plans/2026-05-25-mr-verification-v12-pr6-field-derived-invariant.md`
- `docs/superpowers/plans/2026-05-25-mr-verification-v12-pr7-statistical-cross-method.md`
- `docs/superpowers/plans/2026-05-25-mr-verification-v12-pr8-property-checker.md`
- `docs/superpowers/plans/2026-05-25-mr-verification-v12-pr9-exponential-growth.md`
- `docs/superpowers/plans/2026-05-25-mr-verification-v12-pr10-migration-fixtures-coverage.md`

**原因**:

- 它们已经对应完成并入主线
- 仍适合用作执行样板
- 但不再代表“下一步做什么”

### 阶段性修复计划

- `docs/superpowers/plans/2026-05-25-v12-doc-alignment-plan.md`
- `docs/superpowers/plans/2026-05-24-metbench-doc-runtime-alignment-plan.md`

**原因**:

- 这些计划服务于一次性状态修正
- 对应问题已被 PR 收口

---

## 4. 历史计划

以下计划默认视为历史记录，不可直接用于当前开发排程：

- `docs/superpowers/plans/2026-05-21-next-stage-development-plan.md`
- `docs/superpowers/plans/2026-05-18-stage8-expanded-mr-library-plan.md`
- `docs/superpowers/plans/2026-05-18-meta-prompt-mr-discovery-plan.md`
- 以及其他早于当前主线事实、且未被本索引列为活跃/条件性活跃的旧计划

**原因**:

- 它们形成于旧基线、旧阶段或旧分母之上
- 仍有参考价值，但不能直接驱动当前执行

---

## 5. 使用规则

1. 如果一份计划未出现在本索引的 Active 或 条件性活跃 中，则默认视为历史计划。
2. 监控输出不得直接从历史计划中提取“当前状态”。
3. 新阶段开启时，必须先更新本索引，再开启实现。
4. 如果活跃计划与 `docs/status/current.md` 冲突，以状态账本为准，并同步修正索引和投影文档。
5. 如果投影文档之间冲突，先回到状态账本裁决，再修正对应投影文档。
