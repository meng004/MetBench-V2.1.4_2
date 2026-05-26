# MetBench 活跃计划索引

> **日期**: 2026-05-25
> **状态**: 生效
> **目的**: 明确哪些计划仍指导当前开发，哪些只是历史记录，防止执行和监控误取旧计划。

---

## 1. 当前活跃主计划与范围计划

| Plan | Status | Scope | Expiry condition |
|---|---|---|---|
| `docs/superpowers/plans/2026-05-25-metbench-governed-next-stage-plan.md` | Active | Governance-first next-stage planning; blocks further implementation until the named design ambiguities are resolved | Expires when semantic-convergence, Evidence v2, Windows verification policy, and transition plan are completed and replaced by a new implementation plan |
| `docs/superpowers/plans/2026-05-26-t1-t4-ui-sequenced-execution-plan.md` | Completed orchestration plan (PR-0 / PR-1 / PR-2 all merged) | Mandatory execution order for PR-0 docs-only gate → PR-1 T1 multi-env → PR-2 T4-to-T0 binder → separate Windows/VM UI MR CRUD plan. All three cloud PRs landed: PR-0 (#154 docs-only gate, commit `c776f1a`), PR-1 (#157 `feat(t1): resolve SUT runtime environments from manifest keys`, commit `008cc80`), PR-2 (this PR `feat(t4): bind discovery candidates into draft System MT catalog assets`). UI MR CRUD remains a separate Windows/VM plan and must not be folded into a cloud PR. | Already expired (orchestration complete); UI MR CRUD continues under its own Windows/VM plan row |
| `docs/superpowers/specs/2026-05-26-t3-coverage-assessment-and-next-sut-decision.md` | Active scoped reference | T3 representative-PDE-class coverage assessment + next-SUT gate. Inventory anchor pre-§5.2: **13 SUT / 12 equations / 25 MRs** after PR #134 (Poisson) / #136 (Advection) / #138 (Wave) / #140 (Burgers). Post-§5.2 IVP: **14 SUT / 12 equations / 27 MRs**. Post-§5.3 BVP: **15 SUT / 12 equations / 29 MRs** (real-physics inventory; subsequent PR-A added one synthetic non-physics test SUT `_test_csv` for I/O helper integration regression, bringing catalog total to **16 SUT / 13 equations / 30 MRs** without changing real-physics inventory). **Pure-stdlib expansion paused**; external-solver-pilot expansion proceeded one candidate at a time under §4 driver #1 through IVP and BVP. §5.1 backlog (MeshGraphNets cylinder-flow surrogate) is **anticipation only**. Any further T3 SUT PR must add a new §5.x decision plus a candidate-specific plan. | Expires only when a newer T3 decision record supersedes it |
| `docs/superpowers/plans/2026-05-26-t1-manifest-driven-runtime-environments-plan.md` | Completed (PR-1 merged) | T1 cloud-side scalability fix. `LauncherOptions.RuntimePythons` map + `ResolvePythonExecutable` resolver replaces the per-runtime hardcoded switch. Built-in `system` / `openmoc` / `openmc` / `scipy` behaviour preserved; unknown non-system keys fail closed at resolution time. Adding a new runtime family is now config-only. Status ledger row "T1 multi-env management" moved Open → Controlled. | Already expired (merged); referenced from §3 for historical context |
| `docs/superpowers/plans/2026-05-26-t4-to-t0-mr-discovery-binder-plan.md` | Completed (PR-2 merged) | T4 cloud-side binder. `DiscoveredMrCatalogBinder` is the controlled bridge from T4 discovery candidates to T0 catalog assets; pure / deterministic / IO-free; fails closed on schema and semantics violations; emits a manifest-compatible `MrBindingDefinition` + provenance; never mutates `SUT/<sut>/catalog.json`. Status ledger row "T4-to-T0 binder" moved Queued → Controlled. | Already expired (merged); referenced from §3 for historical context |
| `docs/superpowers/plans/2026-05-26-t1-ui-mr-crud-windows-vm-plan.md` | Gated Windows/VM implementation plan | UI MR CRUD is a separate T1 usability/adoption blocker. It is not cloud-side because WPF source can be edited in cloud but must be built and visually/functionally verified through Windows SSH/RDP or FlaUI. This plan targets System MT manifest MR CRUD and must not be mixed with PR-1 T1 multi-env or PR-2 T4 binder. | Execute only after explicit approval to start Windows/VM UI work; expires when UI MR CRUD lands and `docs/status/current.md` records it as controlled |
| `docs/superpowers/plans/2026-05-26-noise-aware-and-bol-alg-02-sequenced-execution-plan.md` | Retracted orchestration plan — PR-N2 unblocked claim invalid | Original claim: PR-N2 is operationally sequenced after PR-N1 but type-wise independent because variance-ratio is typed since PR #124. **Correction (2026-05-26)**: that claim was incomplete. While `LegacyAssertionPredicateMapper.MapVarianceRatio(...)` exists, `SystemMtPipeline.EvaluateAssertion` never reaches it (only calls `MapScalar`), and the launcher never populates `RoleOutput.Statistical` or `ExtraAssertionValues["refinement_factor"]`. PR-N2 is therefore blocked on a new prerequisite PR-VR (variance-ratio launcher pipeline wiring). PR-N1 still shipped cleanly as its own deliverable. | Already retracted; referenced from §3 for context |
| `docs/superpowers/plans/2026-05-26-typed-noise-aware-scalar-predicate-plan.md` | Completed (PR-N1 merged) | Shipped `NoiseAwareBinaryComparisonPredicate` + `NoiseAwareScalarToleranceEvaluator` + `NoiseAwareBinaryComparisonKernel` + validator + `LegacyAssertionPredicateMapper.MapNoiseAwareScalar(...)` overload under `MetBench_BLL.Core/SystemMT/Catalog/Typed/`. Closed `less-noise-aware` / `greater-noise-aware` legacy mappings. 66 new facts; status ledger row moved Open → Controlled. | Already expired (merged); referenced from §3 for historical context |
| `docs/superpowers/plans/2026-05-26-bol-alg-02-mc-particle-count-convergence-plan.md` | Retracted scoped implementation plan — blocked on PR-VR | Original intent: add the MR `openmc-pincell-particle-count-convergence` using a "already-typed" `variance-ratio` predicate. **Correction (2026-05-26)**: the variance-ratio path is mapped at the `LegacyAssertionPredicateMapper` level but is not reachable through the launcher / pipeline. Plan retracted; a new scoped plan must be drafted that **either** waits for PR-VR to ship the launcher wiring then revives the original blueprint additions, **or** merges the wiring + MR catalog row in a single larger PR. Gap detail in the plan file's new "Discovered blocker (2026-05-26)" section. | Expires once a successor scoped plan supersedes it and PR-N2 / PR-VR (or a combined PR) merges |
| `docs/superpowers/plans/2026-05-26-variance-ratio-launcher-pipeline-wiring-plan.md` | Completed (PR-VR merged) | **PR-VR** — wired `variance-ratio` assertion-type code end-to-end. Shipped: `TypedSpecFactory.ForLegacyAssertion` (single migration-side dispatch site for `variance-ratio` and the legacy scalar codes), `TypedSpecFactory.ForVarianceRatio` (mrCode/metric/factor/toleranceRel → MrSpec with `SigmaMultiplier = 1 + ToleranceRel`), and `TypedVerificationContextFactory.FromScalarOutputs` extension that promotes `scalars[StatisticalMetric]` into `RoleOutput.Statistics` only when the spec carries a `VarianceRatioPredicate` (additive — other specs see `Statistics = null`). `SystemMtPipeline.EvaluateAssertion` reduced to 2 branches: provided typed spec vs single `ForLegacyAssertion` call; string-code dispatch confined to `Catalog/Typed/Migration/` per the SemanticCatalogBoundaryTests architecture guard. 44 new always-passing facts across 3 test files. No new MR catalog row in PR-VR — PR-N2 is the first consumer. Defence-in-depth `ExtraAssertionValues["refinement_factor"]` deferred (typed kernel path does not consume it). | Already expired (merged at `befbe5f`, #168); referenced from §3 for historical context |
| `docs/superpowers/plans/2026-05-26-pr-n2-bol-alg-02-mc-particle-count-convergence-plan.md` | Active scoped implementation plan — Bol-Alg-02 MC particle count convergence MR | **PR-N2** — restart of the retracted Bol-Alg-02 plan, unblocked by PR-VR. One `MrBlueprint` row (`openmc-pincell-particle-count-convergence`: factor=4, ToleranceRel=0.30, NoiseMultiplier=1.0, AssertionTypeCode=variance-ratio, ValueName=k_eff_std, TransformSteps=ScaleField("/solver/particles")), one `MrMetadata` row (ComparisonType=Relative — fallback for missing `MrComparisonType.Statistical` enum value, semantically equivalent for variance-ratio's relative tolerance band), one `solver.particles: 5000` field added to `SUT/openmc/sample/pincell.json` (no behavior change — matches the runner's previously-implicit default), pinned-count bump 30 → 31 across 6 test files + 1 production comment, 2 SkippableFacts in `LauncherEndToEndOpenMcParticleCountConvergenceTests` gated on `OpenMcTestPaths.OpenMcImportable()`. No new SUT, no new Python script, no new transform C# class. | Expires once PR-N2 merges and `docs/status/current.md` records Bol-Alg-02 as Controlled |
| `docs/superpowers/plans/2026-05-26-t1-cross-method-and-io-sequenced-execution-plan.md` | Completed orchestration plan (PR-0 / PR-B / PR-A all merged) | Mandatory execution order for the cloud-side T1 PRs: PR-0 docs gate (`453e369`) → PR-B T1 same-equation cross-method differential runner (`2f997dd`) → PR-A T1 non-JSON I/O adapter (this PR). All three merged. Cloud-side only; no Method MT, no WPF, no UI MR CRUD entanglement. | Already expired (orchestration complete) |
| `docs/superpowers/plans/2026-05-26-t1-cross-method-differential-runner-plan.md` | Completed (PR-B merged) | T1 cloud-side same-equation cross-method differential. `IDifferentialTestRunner` + sealed `DifferentialTestRunner` shipped in `MetBench_BLL.Core/SystemMT/Differential/` with three deterministic criteria (BothPassed / DirectionConcordant / FollowUpRatioWithinTolerance), explicit fail-closed reasons, 28 facts. Status ledger row "T1 same-equation cross-method differential" moved Open → Controlled. | Already expired (merged); referenced from §3 for historical context |
| `docs/superpowers/plans/2026-05-26-t1-non-json-io-adapter-plan.md` | Completed (PR-A merged) | T1 cloud-side non-JSON SUT I/O. Shipped `SUT/_shared/metbench_io/` Python helper (CSV + plain-text round-trip via stdlib) plus synthetic `SUT/_test_csv/` test SUT (MR `csv-roundtrip-identity` in family `TestCsv.Scaling.Identity`). Pinned counts bumped 29 → 30 / 15 → 16 across the six descriptor-list files. Status ledger row "T1 non-JSON I/O adapter" moved Open → Controlled. | Already expired (merged); referenced from §3 for historical context |
| PR-Bol-2 (Bol-Alg-01 MOC ray/track convergence on OpenMOC) | Anticipated, not yet started | Reference-convergence MR `Bol-Alg-01 → ErrorMonotonicPredicate` on OpenMOC pin-cell. Depends on existing `ErrorMonotonicPredicate` kernel in typed catalog runtime (delivered by PR-PR-4). Requires new SUT input knob (ray density / track spacing) and adapter; will need its own implementation plan registered here before any code lands. | Expires once a candidate-specific implementation plan is registered and PR-Bol-2 merges |
| PR-Bol-3 (Bol-Alg-02 MC particle count convergence on OpenMC) | In flight — scoped implementation plan registered as `2026-05-26-pr-n2-bol-alg-02-mc-particle-count-convergence-plan.md` (row above); execution PR open | `Bol-Alg-02 → VarianceRatioPredicate` on OpenMC pin-cell. Earlier "blocked" framings (noise-aware mistake then "already typed since #124" mistake) corrected via PR-VR (#168). The successor scoped plan supersedes the retracted v1; the execution PR delivers the catalog row + metadata + pinned-count bump + SkippableFact end-to-end test. | Expires once PR-Bol-3 execution PR merges and `docs/status/current.md` records Bol-Alg-02 as Controlled |
| (no active scoped plan beyond the above) | — | Verification-semantics convergence (PR-A → PR-D), ExecutionEvidence v2 (PR-A0 + PR-C0 + live wiring + evidence-aware reporting via PR #123 / #126 / #128), dormant legacy code mapping (PR #124), anomaly typed annotation + correctness (PR #130 / #132), T3 representative-PDE-class expansion (PR #134 / #136 / #138 / #140), T3C-IVP, T3C-BVP, and PR-Bol-1 are merged. Remaining follow-ups must enter through explicit scoped plans registered here. | A new scoped plan must be registered here before any new cross-cutting System MT work begins. |

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
| `docs/superpowers/templates/pr-gate-checklist.md` | Active | Required PR gate checklist for scope, facts, tests, Windows classification, review, merge, and soft review | Expires only by explicit replacement PR |
| `docs/superpowers/specs/2026-05-26-pr-soft-review-via-claude-code-action.md` | Active design (implementation YAML pending operator action) | Advisory LLM-based PR review layer running in GH Actions on `pull_request` events; uses `anthropics/claude-code-action@v1` in OAuth mode against the repo owner's Max subscription | Expires when Anthropic deprecates the action or its OAuth mode, when Max coverage of headless action calls changes, or when the project replaces this review pipeline |

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

### T3C 外部求解器试点（已合并）

- `docs/superpowers/plans/2026-05-26-t3c-scipy-ivp-lotka-volterra-plan.md`（T3C-IVP，已合并）
- `docs/superpowers/plans/2026-05-26-t3c-scipy-bvp-poisson-1d-plan.md`（T3C-BVP，已合并）

**原因**:

- §5.2 decision record 选定的 SciPy `solve_ivp` Lotka-Volterra SUT 已通过 T3C-IVP 合入主线（inventory 13→14 SUT / 25→27 MR）。
- §5.3 decision record 选定的 SciPy `solve_bvp` Poisson 1D SUT 已通过 T3C-BVP 合入主线（real-physics inventory 14→15 SUT / 27→29 MR）。
- PR-A（#162）追加了合成非物理 SUT `_test_csv` 以端到端回归 `metbench_io` helper —— catalog 总计为 **16 SUT / 13 equations / 30 MRs**；真实物理 inventory 维持 **15 SUT / 12 equations / 29 MRs** 不变。
- 仍适合用作未来外部求解器候选的实施样板。
- 但不再代表”下一步做什么”。

### T1 manifest-driven runtime environments（已合并）

- `docs/superpowers/plans/2026-05-26-t1-manifest-driven-runtime-environments-plan.md`（PR-1，已合并）

**原因**:

- `LauncherOptions.RuntimePythons` + `ResolvePythonExecutable` 已替换原先逐 venv 字段的硬编码槽位；`ManifestMrCatalogProvider` 改为单点 resolver 调用。
- 内置 `system`/`openmoc`/`openmc`/`scipy` 行为保留；未知非 system 键在 resolver 处 fail-closed 并附带可定位的诊断信息。
- 新增 runtime family（FEniCS/FiPy/torch-surrogate 等）从此为纯配置变更，无需改 `LauncherOptions` 字段或 `PythonExecutableKinds.All`。
- 状态账本”T1 multi-env management”行已由 Open 改为 Controlled。

### Typed noise-aware scalar predicate（已合并）

- `docs/superpowers/plans/2026-05-26-typed-noise-aware-scalar-predicate-plan.md`（PR-N1，已合并）

**原因**:

- `MetBench_BLL.Core/SystemMT/Catalog/Typed/Specs/PredicateSpec.cs` 加 `NoiseAwareBinaryComparisonPredicate` 记录；`Catalog/Typed/Runtime/NoiseAwareScalarToleranceEvaluator.cs` 计算 `NoiseMultiplier · √(σ_source² + σ_followup²)`；`Catalog/Typed/Runtime/NoiseAwareBinaryComparisonKernel.cs` 评估带噪方向不等式（Greater / Less；Equal 显式 `VerifyStatus.InvalidSpec`，因为带噪等式是不同二端测试）；`Catalog/Typed/Validation/NoiseAwareBinaryComparisonPredicateValidator.cs` 校验四个 metric 与 NoiseMultiplier；在 `ValidationRegistry` / `MrSpecValidator` / `PredicateDispatcher`（dispatch + `HasObservable` 两处）登记。
- `LegacyAssertionPredicateMapper.MapNoiseAwareScalar(...)` overload 把 `less-noise-aware` / `greater-noise-aware` 映射到新 predicate；bare `MapScalar` 仍 fail-closed 但 throw 信息指向新 overload。
- 66 新 facts（evaluator 16 / kernel 33 / validator 15 + 6 mapper + 1 dispatcher）。
- 状态账本 "Noise-aware typed scalar predicate" 行已由 Open 改为 Controlled。
- "Legacy assertion code mapping" 行从 "4 mapped / 2 intentionally fail-closed" 改为 "6 mapped (4 deterministic + 2 noise-aware via overload)"。
- 下一阶段 PR-N2 可用 `MapNoiseAwareScalar` 路径，但 PR-N2 (Bol-Alg-02) 实际用 `variance-ratio`，并不依赖 PR-N1 type-wise；PR-N1 解锁的是未来 MC m_mono direction MR。

### T1 non-JSON I/O adapter（已合并）

- `docs/superpowers/plans/2026-05-26-t1-non-json-io-adapter-plan.md`（PR-A，已合并）

**原因**:

- `SUT/_shared/metbench_io/` Python helper（纯 stdlib，支持 `csv-row` 与 `plain-text` wire format）已落地为 MetBench non-JSON SUT I/O 的唯一集成点。Launcher / pipeline / `ManifestMrCatalogProvider` / 参数映射 `IFieldPathResolver` 栈不感知 wire format。
- `SUT/_test_csv/` 合成测试 SUT（前缀下划线标记非物理性质）+ `csv-roundtrip-identity` MR + `_test_csv` 合成 EquationMetadata 端到端证明 helper 经未改动 launcher 跑通。type-coercion 在 SUT parser 层完成（与 JSON SUT 隐式通过 JSON 原生数字类型同一边界），helper 本身严格保留字符串。
- 11 个 `MetBenchIoHelperTests` facts 覆盖 CSV round-trip / 头部验证 / 空 params / 引号 / plain-text 字节同一 / 未知格式 fail-closed / JSON 直通；`LauncherEndToEndTestCsvTests` 1 个 fact 覆盖 launcher 全链路。
- Pinned descriptor 计数 29 → 30 / 15 → 16 across 6 个文件。
- 状态账本 "T1 non-JSON I/O adapter" 行已由 Open 改为 Controlled。
- 至此 `2026-05-26-t1-cross-method-and-io-sequenced-execution-plan.md` orchestration 中的 PR-0 / PR-B / PR-A 已全部合并。

### T1 same-equation cross-method differential runner（已合并）

- `docs/superpowers/plans/2026-05-26-t1-cross-method-differential-runner-plan.md`（PR-B，已合并）

**原因**:

- `MetBench_BLL.Core/SystemMT/Differential/` 命名空间已落地：`IDifferentialTestRunner` + 6 个支撑 type，作为 T1 §2.1 element 3「同源异构差分测试」的唯一 cloud-side API。
- 三种 agreement criteria（BothPassed / DirectionConcordant / FollowUpRatioWithinTolerance）全部确定性、total — 28 个 facts 覆盖 NaN / ±∞ / zero-source / negative-tolerance / metric-name-mismatch / launcher-throws / null-request / null-launcher / same-MR-both-sides / overrides-forwarded-per-side。
- 不复用 typed catalog 的 `CrossMethodComparisonKernel`（intra-MR 两 role 形态不同），但通过 XML doc 明确两者区别。
- 现有 BDD `Features/CrossProgramNeutronTransportMrs.feature` + `Steps/CrossProgramSteps.cs` 完全未动 — 这是 orthogonal cleanup 而非本 PR 范围。
- 状态账本 "T1 same-equation cross-method differential" 行已由 Open 改为 Controlled。

### T4-to-T0 MR discovery binder（已合并）

- `docs/superpowers/plans/2026-05-26-t4-to-t0-mr-discovery-binder-plan.md`（PR-2，已合并）

**原因**:

- `MetBench_BLL.Core/SystemMT/Catalog/Binding/DiscoveredMrCatalogBinder` 已落地为 T4 → T0 唯一被授权的桥接面：纯函数、无 IO、deterministic；输入 `DiscoveredMrBindingDraft`（16 字段含 transform steps / discovery method / run id / confidence），输出 manifest-compatible `MrBindingDefinition` + `DiscoveredMrBindingProvenance`，或 field-level errors。
- 校验严格 fail-closed：required-string blank、`AssertionTypeCode` 未识别 / noise-aware fail-closed、`Confidence ∉ [0,1]` / NaN / ±∞、`DefaultParameters` blank key、`SampleCaseRelativePath` 有 `..` 段或为根路径、`TimeoutSeconds ≤ 0`、空 `TransformSteps`、`MrBindingDefinition.Validate()` 自身违反等都在 errors 列表中一次性返回。
- 不读写任何 `SUT/<sut>/catalog.json`；测试用 sentinel 目录证明 5 次 binding 后磁盘零变更。
- 状态账本”T4-to-T0 binder”行已由 Planned/Queued 改为 Controlled。
- 至此 PR-0 / PR-1 / PR-2 orchestration plan 中的三个 PR 已全部合并；下一步需要新的 scoped plan 才能继续 cloud-side 工作。

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

- 无独立文件（PR #123 / #124 / #126 / #128 / #130 / #132 / #134 / #136 / #138 / #140 直接在主线推进）

**原因**:

- PR #123（live typed verification wiring）、PR #124（dormant legacy code mapping）、PR #126（evidence-aware HTML report rendering）、PR #128（evidence-aware execution markdown）、PR #130（anomaly typed verification annotation）已合并 `origin/main`。
- PR #123 把 `SystemMtPipeline.ExecuteAsync` 产出的类型化三元组通过 `PipelineOutcome` 带回 recorder，live `ExecutionEvidence.TypedVerification` 不再 null。
- PR #124 把四个 dormant 旧 assertion code 映射到现有 typed predicates；剩余 `less-noise-aware` / `greater-noise-aware` 保留 fail-closed 并由 `Noise_aware_scalar_codes_fail_closed_with_documented_reason` 守卫。
- PR #126 给 `HtmlSystemMtResultReportRenderer` / `ISystemMtResultReportRenderer` 加 evidence-aware overload，渲染 `TypedVerification`（Spec / Predicate / Status / Diagnostic / Skip / PropertyPredicates）。Legacy 路径字节级不变。
- PR #128 给 `SystemMtReportService` 加 optional `IExecutionEvidenceRepository?` ctor 参数，markdown 执行报告同样投影 `TypedVerification`。Legacy 5-arg ctor 与 single-arg `BuildExecutionMarkdown` 仍存在；既有测试不需修改。
- PR #130 给 `IAnomalyService.RecordAnomalyAsync` 加 optional `string? typedVerificationSummary` 参数，`SystemMtLauncher.RecordAnomalyIfFailedAsync` 把 `PipelineOutcome.TypedVerification` 投影成 `typed=<Status> metric=<Metric> predicate=<id> (<Kind>) residual=… tolerance=…` 一行摘要，写入 `Anomaly.Notes` 与 `anomaly.created` 审计 `detailsJson`。无 typed verification 时 byte-identical to PR-129。
- PR #132 修正了一个 bug：`Status=SkippedMissingObservable / SkippedNotApplicable / InvalidSpec` 的 typed verification 之前会因 `SystemMtAssertionResultV2.Passed=false` 的 fallback 而被 launcher 错误归类为 Anomaly。`RecordAnomalyIfFailedAsync` 现在对这三种非-Failed 状态早返，避免误报。`Status=Failed` 与遗留非-typed 失败仍生成 Anomaly。
- PR #134 启动 T3 扩展，加首个椭圆 PDE SUT（`SUT/poisson_1d/`，pure-stdlib Thomas 三对角求解器，无需 venv，云端 CI 可跑），加 `poisson` `EquationMetadata` 和两条 MR（`poisson-source-superposition` m_mono 线性叠加 + `poisson-mesh-richardson` m_conv）。同时**通过演示验证 T1**：新 SUT 接入只改 SUT 文件 + catalog 接线 + 计数测试，未触 Pipeline / Persistence / Reporting / Launcher 类体 / DAL —— 证明 T1 的 CLI runner / Python adapter / catalog provider / launcher facade 已稳定可附加。Inventory 改为 10 SUT / 9 方程 / 19 MR；`EquationKind` enum 不变（poisson → `Other`；APPEND-ONLY 保持，5 反应堆方程仍为 canonical）。
- PR #136 续 T3 扩展，加首个一阶线性双曲 PDE SUT（`SUT/advection_1d/`，一阶迎风 FD + 周期边界 + 内部 Courant=0.5 dt 选择，pure-stdlib），加 `advection` `EquationMetadata` 和两条 MR（`advection-amplitude-linearity` m_mono + `advection-mesh-conservation` m_inv via 守恒迎风格式）。再次仅改 SUT 文件 + catalog 接线 + 计数测试，二次确认 T1 by-demonstration 验证。Inventory 改为 11 SUT / 10 方程 / 21 MR；APPEND-ONLY 保持（advection → `Other`）。
- PR #138 续 T3 扩展，加首个二阶线性双曲 PDE SUT（`SUT/wave_1d/`，规范 `u_tt = c²·u_xx`，二阶 leapfrog FD + Dirichlet 边界 + 零初始速度 + 内部 Courant=0.5 dt 选择，pure-stdlib），加 `wave` `EquationMetadata` 和两条 MR（`wave-amplitude-linearity` m_mono via 线性 leapfrog + `wave-mesh-energy-convergence` m_conv via L² 不变量 `0.5·∫u² dx`）。三次确认 T1 by-demonstration 验证。Inventory 改为 12 SUT / 11 方程 / 23 MR；APPEND-ONLY 保持（wave → `Other`）。
- PR #140 续 T3 扩展，加首个非线性 PDE SUT（`SUT/burgers_1d/`，无粘 Burgers `u_t + (u²/2)_x = 0`，守恒 Lax-Friedrichs 通量差分 + 周期边界 + 内部 Courant=0.5 dt 选择，pure-stdlib），加 `burgers` `EquationMetadata` 和两条 MR（`burgers-amplitude-peak-monotone` m_mono via 非线性幅值单调性（LxF 数值耗散使 peak_ratio < 2 但仍严格大）+ `burgers-mesh-conservation` m_inv via 守恒通量差分严格保 ∫u dx）。四次确认 T1 runner/adapter/catalog additivity by-demonstration 验证，且跨四类 PDE（椭圆 / 一阶线性双曲 / 二阶线性双曲 / 非线性双曲）；这不等同于 T1 多 env 管理或 UI MR CRUD 完成。Inventory 改为 13 SUT / 12 方程 / 25 MR；APPEND-ONLY 保持（burgers → `Other`）。T3 代表性 PDE-class 覆盖功能性完成。
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
