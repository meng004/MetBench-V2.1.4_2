# MetBench 活跃计划索引

> **日期**: 2026-05-25
> **状态**: 生效
> **目的**: 明确哪些计划仍指导当前开发，哪些只是历史记录，防止执行和监控误取旧计划。

---

## 1. 当前活跃主计划与范围计划

| Plan | Status | Scope | Expiry condition |
|---|---|---|---|
| `docs/superpowers/plans/2026-05-25-metbench-governed-next-stage-plan.md` | Active | Governance-first next-stage planning; blocks further implementation until the named design ambiguities are resolved | Expires when semantic-convergence, Evidence v2, Windows verification policy, and transition plan are completed and replaced by a new implementation plan |
| (no active scoped plan) | — | Verification-semantics convergence (PR-A → PR-D), ExecutionEvidence v2 (PR-A0 + PR-C0 + live wiring + evidence-aware reporting via PR #123 / #126), and dormant legacy code mapping (PR #124) are all merged. The ExecutionEvidence v2 design's full lifecycle is closed. Remaining follow-ups (noise-aware typed predicate when a binding adopts the codes; Windows verification policy when a Windows-touching PR is planned; T2 / T3 / T4 / T5 / T6 expansion) are tracked as `docs/status/current.md` §7 steps; no new scoped plan has been opened yet. | A new scoped plan must be registered here before any new cross-cutting System MT work begins. |

Any new coding task must be derived from the active master plan or from a scoped successor plan registered here.

---

## 2. 当前活跃设计文档

| Document | Status | Scope | Expiry condition |
|---|---|---|---|
| `docs/status/current.md` | Active | Single current-status ledger for monitoring, handoff, baseline, active plan, and open risks | Expires only when replaced by a newer status ledger path |
| `docs/superpowers/specs/2026-05-25-metbench-project-control-rules.md` | Active | Control charter: source hierarchy, gates, review, status refresh | Expires only by explicit replacement PR |
| `docs/superpowers/specs/2026-05-25-metbench-macro-assessment-and-risk-audit.md` | Active | Macro assessment and risk audit for the Stage 8 to next-stage transition | Expires when the next macro assessment supersedes it |
| `docs/superpowers/specs/2026-05-25-mr-verification-v1.2-codex-ready.md` | Active reference | v1.2 typed semantic model and verifier design already implemented through the current roadmap | Expires when a v1.3 verification design supersedes it |
| `docs/superpowers/specs/2026-05-25-systemmt-architecture-review-post-evidence-v2.md` | Active | Post-PR-D / post-PR-C0 System MT dependency-boundary audit; baseline reference for future architecture-impact reviews. PR #123 / #124 / #126 did not invalidate it. | Refresh required (not necessarily expiring) when the next System MT cross-cutting change lands. |
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

### 验证语义收敛链（已合并）

- `docs/superpowers/plans/2026-05-25-verification-semantics-convergence-implementation-plan.md`
- `docs/superpowers/specs/2026-05-25-verification-semantics-convergence-design.md`

**原因**:

- 全部四个 PR（PR-A 设计 lock #114 / PR-B 命名迁移 #115 / PR-C 运行时收敛 #118 / PR-D 守卫与清理 #119）已合并 `origin/main`。
- 设计目标“Method MT 隔离 + System MT 全部走 Typed Semantic Catalog + W1 IMrAssertion 路径下线”已闭环。
- `SemanticCatalogBoundaryTests` 与 `SemanticCatalogNamingBoundaryTests` 接管事后回潮守卫，参见 `docs/status/current.md` §6 Verification semantics convergence 行。
- 设计与计划保留为历史参考；新工作不得从这两个文件衍生执行排程。

### ExecutionEvidence v2 全生命周期（已合并）

- `docs/superpowers/plans/2026-05-25-executionevidence-v2-implementation-plan.md`
- `docs/superpowers/specs/2026-05-25-executionevidence-v2-design.md`

**原因**:

- PR-A0 设计 lock（#116）、PR-C0 schema/recorder（#121）、live wiring（#123）与 evidence-aware reporting（#126）均已合并 `origin/main`。
- 设计目标“nullable TypedVerification block + 三个支撑 POCO + 无生产端 schema break + live pipeline → recorder 写入 + 渲染器投影”已端到端闭环（数据→持久化→recorder→渲染器）。
- `MrRunResultShapeLockTests` 接管 launcher facade 防漂移守卫；`TypedVerificationEvidenceRoundtripTests` 接管 legacy / v2 双向兼容守卫；`Live_pipeline_outcome_carries_typed_triple_into_evidence_without_explicit_typed_args` 接管 live pipeline 写入守卫；`Render_without_evidence_dictionary_matches_legacy_overload_byte_identical` 接管渲染器无证据路径字节级守卫。
- 设计与实施计划均退役为历史参考；后续若需扩展 evidence 粒度或重大 schema 变更须另立 design + plan，再注册到本索引 §1 / §2。

### 验证语义收敛续作 + evidence-aware reporting + anomaly annotation/correctness + T3 expansion（已合并）

- 无独立文件（PR #123 / #124 / #126 / #128 / #130 / #132 / #134 / #136 直接在主线推进）

**原因**:

- PR #123（live typed verification wiring）、PR #124（dormant legacy code mapping）、PR #126（evidence-aware HTML report rendering）、PR #128（evidence-aware execution markdown）、PR #130（anomaly typed verification annotation）已合并 `origin/main`。
- PR #123 把 `SystemMtPipeline.ExecuteAsync` 产出的类型化三元组通过 `PipelineOutcome` 带回 recorder，live `ExecutionEvidence.TypedVerification` 不再 null。
- PR #124 把四个 dormant 旧 assertion code 映射到现有 typed predicates；剩余 `less-noise-aware` / `greater-noise-aware` 保留 fail-closed 并由 `Noise_aware_scalar_codes_fail_closed_with_documented_reason` 守卫。
- PR #126 给 `HtmlSystemMtResultReportRenderer` / `ISystemMtResultReportRenderer` 加 evidence-aware overload，渲染 `TypedVerification`（Spec / Predicate / Status / Diagnostic / Skip / PropertyPredicates）。Legacy 路径字节级不变。
- PR #128 给 `SystemMtReportService` 加 optional `IExecutionEvidenceRepository?` ctor 参数，markdown 执行报告同样投影 `TypedVerification`。Legacy 5-arg ctor 与 single-arg `BuildExecutionMarkdown` 仍存在；既有测试不需修改。
- PR #130 给 `IAnomalyService.RecordAnomalyAsync` 加 optional `string? typedVerificationSummary` 参数，`SystemMtLauncher.RecordAnomalyIfFailedAsync` 把 `PipelineOutcome.TypedVerification` 投影成 `typed=<Status> metric=<Metric> predicate=<id> (<Kind>) residual=… tolerance=…` 一行摘要，写入 `Anomaly.Notes` 与 `anomaly.created` 审计 `detailsJson`。无 typed verification 时 byte-identical to PR-129。
- PR #132 修正了一个 bug：`Status=SkippedMissingObservable / SkippedNotApplicable / InvalidSpec` 的 typed verification 之前会因 `SystemMtAssertionResultV2.Passed=false` 的 fallback 而被 launcher 错误归类为 Anomaly。`RecordAnomalyIfFailedAsync` 现在对这三种非-Failed 状态早返，避免误报。`Status=Failed` 与遗留非-typed 失败仍生成 Anomaly。
- PR #134 启动 T3 扩展，加首个椭圆 PDE SUT（`SUT/poisson_1d/`，pure-stdlib Thomas 三对角求解器，无需 venv，云端 CI 可跑），加 `poisson` `EquationMetadata` 和两条 MR（`poisson-source-superposition` m_mono 线性叠加 + `poisson-mesh-richardson` m_conv）。同时**通过演示验证 T1**：新 SUT 接入只改 SUT 文件 + catalog 接线 + 计数测试，未触 Pipeline / Persistence / Reporting / Launcher 类体 / DAL —— 证明 T1 的 CLI runner / Python adapter / catalog provider / launcher facade 已稳定可附加。Inventory 改为 10 SUT / 9 方程 / 19 MR；`EquationKind` enum 不变（poisson → `Other`；APPEND-ONLY 保持，5 反应堆方程仍为 canonical）。
- PR #136 续 T3 扩展，加首个双曲 PDE SUT（`SUT/advection_1d/`，一阶迎风 FD + 周期边界 + 内部 Courant=0.5 dt 选择，pure-stdlib），加 `advection` `EquationMetadata` 和两条 MR（`advection-amplitude-linearity` m_mono + `advection-mesh-conservation` m_inv via 守恒迎风格式）。再次仅改 SUT 文件 + catalog 接线 + 计数测试，二次确认 T1 by-demonstration 验证。Inventory 改为 11 SUT / 10 方程 / 21 MR；APPEND-ONLY 保持（advection → `Other`）。
- 后续触发条件（noise-aware 旧码被新增 catalog binding 采用 → 加噪声感知 typed predicate）记录在 `docs/status/current.md` §7。

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
