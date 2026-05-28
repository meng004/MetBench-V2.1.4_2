# MetBench 代码治理 v2 章程（六模块 + 元规则集）

> **Date**: 2026-05-28
> **Status**: Active — 接替 v1 "4 层防御" 表述（CLAUDE.md §12 同 PR 改写）
> **Driver**: T2/T3 chain 与 CI Cat B chain 两次 post-merge holistic review 显示，当前 4-layer 模型对 Cat A 已覆盖 ~90%，Cat B 仍仅 ~50%。本章程把已有治理产物 + 5 项 ROI 评估通过的新机制重组为 6 个职责单一、边界清晰的模块，并以元规则集 §12.4 R1-R4 收敛它们。
> **Replaces** (不删，重组解释)：CLAUDE.md §12 "两层并行 + 三/四层防御" → 本章程 "六模块 + 元规则集"

---

## §1 设计原则

| 原则 | 含义 |
|---|---|
| **职责单一** | 每个模块只处理一类问题，模块间不重叠 |
| **触发显式** | 每模块清楚定义触发条件、读入物、产出物 |
| **代价对齐** | 高频跑 → 必须 0 token / 秒级；低频跑 → 可烧 LLM token |
| **Cat A 多重门、Cat B 全周期** | Cat A 单 PR 内可见，PR-time 多重拦截；Cat B 跨 PR / retrospective，覆盖 commit-time + schedule + chain-end + author-side 多时段 |
| **元规则与实现分离** | §12.4 R1-R4 仍是元约束，但具体落实从规则文本转移到模块代码 / 工具，每条规则附实现指针 |

---

## §2 总览图

```
                        ┌──────────────────────────────────────────┐
                        │  G. 元规则集 §12.4 R1-R4                  │
                        │  （A-F 模块都向它收敛）                     │
                        └──────────────────────────────────────────┘
                                          ▲
                                          │
   ┌──────────────────────── PR-time（每 PR 必跑）─────────────────────────┐
   │  A. 功能正确性       B. 机械模式守卫      C. 负空间守卫               │
   │  Hard `test` gate    grep + Roslyn       Stryker delta              │
   │  (build + xUnit)     parity tests        + R4 半自动 semantic       │
   └─────────────────────────────────────────────────────────────────────┘
   ┌──────────────────── Schedule / 链尾（异步、周期）─────────────────────┐
   │  D. 漂移侦测                       E. 链尾整体审查                    │
   │  weekly cron                       /code-review ultra +              │
   │  spec-freshness + orphan           §12.4 R2 ritual                   │
   └─────────────────────────────────────────────────────────────────────┘
   ┌──────────────────── Author-side（PR push 前可选）─────────────────────┐
   │  F. 作者侧顾问                                                        │
   │  /code-review low/medium/high (本地 diff)                            │
   └─────────────────────────────────────────────────────────────────────┘
```

---

## §3 六模块详表

### A. 功能正确性 (Functional Correctness)

| 字段 | 内容 |
|---|---|
| **职责** | 代码能 build + 既有测试套件不红 |
| **触发** | 每 PR push、main 推进 |
| **输入** | `MetBench.sln`（限 cross-platform 项目） |
| **输出** | GitHub Actions `test` job 状态 |
| **工具 / 现状** | `.github/workflows/dotnet-test.yml` 的 `test` job |
| **门类型** | **Required**（branch protection） |
| **主守 Cat** | Cat A（编译错 / 已有 fact 可捕获的回归） |
| **变化** | 保留原状，零改动 |

### B. 机械模式守卫 (Mechanical Pattern Guards)

| 字段 | 内容 |
|---|---|
| **职责** | 单 PR diff 内可机械检测的结构 / 模式违例 |
| **触发** | 每 PR push（同 `governance` job） |
| **输入** | PR diff、源码 AST、catalog 文件 |
| **输出** | `::warning::` 行（grep）、METBENCH00x 诊断（Roslyn）、`*ParityTests.cs` 红 |
| **工具 / 现状** | grep G6/G8/G9/G10/G11(新) + Roslyn METBENCH001 + METBENCH002(新) + 现有 ParityTests.cs / `Audit_*_providers_produce_identical_matrices` / `Render_*_renders_<contract>` + **catalog-derived 计数**(新) |
| **门类型** | Advisory（grep）+ Hard（Roslyn 诊断 Warning/Error + parity tests via test job） |
| **主守 Cat** | Cat A（结构性）+ Cat B 中 L1 多投影漂移子类 |
| **变化** | **G7 pinned-count grep 退役**（被 catalog-derived 计数取代）；**新增 G11 decision-record-or-die**；**METBENCH001 升级 METBENCH002 通用 field-flow tracer**；**catalog-derived 计数白名单**取代多处 `Assert.Equal(33, count)` |

### C. 负空间守卫 (Negative-Space Guards)

| 字段 | 内容 |
|---|---|
| **职责** | 检出"测试存在但没真断契约"（M5 类）+ "契约声称但没 fact"（B1/R4 类） |
| **触发** | (i) Stryker：每 PR label `mutation-testing` 或周一 cron；(ii) R4 半自动：PR 触碰 `MetBench_BLL.Core/SystemMT/Reporting/` 或 `Catalog/Editing/` 时 PR-checklist 推荐作者跑 `/code-review high` |
| **输入** | 变异 diff / 渲染器与契约改动 |
| **输出** | Stryker kill-rate delta 报告 / `/code-review` 输出 |
| **工具 / 现状** | `tools/mutation-testing/stryker-config.json` + `mutation-testing.yml`（**升级 break: 0 → -3pp PR-delta gate**，待 3 周 cron baseline 后） + PR Gate Checklist「Tests」节加 R4 sub-check |
| **门类型** | Stryker PR-delta：Advisory → Required（升级后）；R4 semantic：Advisory |
| **主守 Cat** | Cat B 中 M5 / B1 / R4 子类 |
| **变化** | **Stryker 从 informational 升 PR-delta gate**；**R4 contract-fact 配对加 `/code-review high` 推荐链路** |

### D. 漂移侦测 (Drift Detection)

| 字段 | 内容 |
|---|---|
| **职责** | docs ↔ impl ↔ plan 长期错位（D1/D2/A4/T3 类） |
| **触发** | 周一 cron + 任何 `docs/superpowers/specs/**` 或 `active-plan-index.md` 改动 |
| **输入** | spec doc 全集 + plan index + current.md |
| **输出** | 自动 GitHub issue (`governance:stale-spec` / `governance:orphan-spec`) |
| **工具 / 现状** | `tools/spec_freshness_audit.py` + `.github/workflows/spec-freshness-monitor.yml`（**扩 orphan-spec 检查**） |
| **门类型** | Async / Issue-based，永不阻塞 PR |
| **主守 Cat** | Cat B 中 D1 / D2 / A4 / T3 子类 |
| **变化** | **同一 Python 脚本扩 orphan-spec 检查**：spec 必须在 `active-plan-index.md §1` 或 §3 出现，否则自动 issue |

### E. 链尾整体审查 (Chain-End Holistic Review)

| 字段 | 内容 |
|---|---|
| **职责** | N-PR 累积 diff 的语义级审查，捕获 PR-time 看不到的全谱 Cat B |
| **触发** | 任何 ≥ 3-PR chain 的最后一个 PR 合入后（手动 + 自动喂入） |
| **输入** | `git diff origin/main~N..HEAD` 累积 diff |
| **输出** | `docs/superpowers/specs/YYYY-MM-DD-<chain>-post-merge-review.md` + cleanup PR |
| **工具 / 现状** | 人工 `Explore` subagent ritual（保留）+ `/code-review ultra` 喂入累积 diff（**新增自动化**）+ `chain-end-review-checklist.md` |
| **门类型** | 阻塞性 ritual：chain 在 ledger 标 Controlled **必须**在 review doc 落地后 |
| **主守 Cat** | Cat B 全谱兜底 |
| **变化** | **`/code-review ultra` 自动喂入累积 diff** 作为 ritual 的辅助产出，频率从"每链人工一次"变成"每链自动 + 人工核对 cleanup" |

### F. 作者侧顾问 (Author-Side Advisory)

| 字段 | 内容 |
|---|---|
| **职责** | PR push 前作者自查 Cat A 语义 bug |
| **触发** | 作者本地命令，可选 |
| **输入** | 本地 git diff |
| **输出** | `/code-review` 终端输出，作者决定改不改 |
| **工具 / 现状** | `/code-review low/medium/high`（superpowers skill） |
| **门类型** | 非门禁，作者 discretion |
| **主守 Cat** | Cat A（语义级，预防进 CI） |
| **变化** | **新增推荐**：PR Gate Checklist「Review」节加非强制 sub-check "touched code in `MetBench_BLL.Core/SystemMT/Catalog/` 或 `Reporting/` → 建议先跑 `/code-review high`" |

### G. 元规则集 §12.4 R1-R4

| 规则 | 摘要 | 由哪个模块实现 |
|---|---|---|
| **R1** Cross-projection parity test 强制 | 任何 record 加字段，所有投影路径必须同步 + 守 parity test | 模块 B（METBENCH001/002 + `*ParityTests.cs`） |
| **R2** ≥ 3-PR chain 必须 chain-end holistic review | chain 最后一个 PR 合入后开 fresh-session review | 模块 E（ritual + `/code-review ultra`） |
| **R3** Spec 偏离实施时 retrospective 改 spec doc | Phase-N 改用候选 Y，同 PR re-touch Phase-K spec | 模块 D（orphan + freshness cron） + PR Gate Checklist |
| **R4** Public-contract ↔ fact 配对 | XML doc 声称 X，必须有 fact 断言 X 可观测 | 模块 C（R4 半自动 + Stryker delta） |

R1-R4 文本内容保留，本章程不重述；每条规则在 CLAUDE.md §12.4 内加"由模块 X 实现"指针。

---

## §4 与 v1 4-layer 的映射

| v1 项 | v2 去向 | 备注 |
|---|---|---|
| Layer 1 Hard `test` gate | → 模块 A | 保留原样 |
| Layer 2 Grep G6 (silent-discard) | → 模块 B | 保留 |
| Layer 2 Grep G7 (pinned-count) | **退役** | 由 catalog-derived 计数取代 |
| Layer 2 Grep G8 (parity hint) | → 模块 B | 保留，配 METBENCH002 |
| Layer 2 Grep G9 (ledger guard) | → 模块 B | 保留 |
| Layer 2 Grep G10 (multi-projection registry) | → 模块 B | 保留 |
| Layer 3 §12.4 R1 | → 模块 G + 模块 B 实现 | |
| Layer 3 §12.4 R2 | → 模块 G + 模块 E 实现 | |
| Layer 3 §12.4 R3 | → 模块 G + 模块 D 实现 | |
| Layer 3 §12.4 R4 | → 模块 G + 模块 C 实现 | |
| Layer 4 `*ParityTests.cs` | → 模块 B | 保留 |
| Layer 4 `Audit_*_providers_*` | → 模块 B | 保留 |
| Layer 4 `Render_*_renders_*` | → 模块 B + 模块 C | 保留 + R4 半自动叠加 |
| Layer 4 METBENCH001 | → 模块 B；**升 METBENCH002 通用** | 001 仍跑特化，002 跑通用扫描 |
| Layer 4 Stryker pilot | → 模块 C；**升 PR-delta gate** | break=0 → -3pp（baseline 后） |
| Layer 4 spec-freshness cron | → 模块 D；**加 orphan-spec** | 同 Python 脚本扩 |
| Layer 4 chain-end-review-checklist.md | → 模块 E | 保留 |

**净变化**：退役 1（G7）；升级 3（METBENCH001/002、Stryker、spec-freshness）；新增 4（G11、catalog-derived 计数、`/code-review ultra` 自动化、`/code-review` 作者侧推荐）；其余保留。

---

## §5 Cat A / Cat B 覆盖矩阵

| 缺陷类 | v1 现状 | v2 改善 |
|---|---|---|
| Cat A — 编译错 / 已有测试可捕获 | 100%（模块 A） | 100%（不变） |
| Cat A — 单 PR 内语义 bug（无现成测试） | grep 部分 + 人工 review | **+ 模块 F `/code-review high` 前置 + 模块 C R4 半自动** |
| Cat B — L1 cross-projection 漂移 | METBENCH001（4 type registry） | **+ METBENCH002 通用 AST**（不限 registry） |
| Cat B — A4 spec 推荐与实际偏离 | spec-freshness cron 关键词正则 | 同（已足够） |
| Cat B — D1/D2 premature Controlled | G9 grep | 同（已足够） |
| Cat B — M5 contract-without-fact | `Render_*` 测试 + chain-end review | **+ 模块 C R4 半自动 + Stryker delta gate** |
| Cat B — N-bump pinned-count 漂移 | G7 grep | **+ catalog-derived 计数 → 整类消除** |
| Cat B — orphan spec | 无 | **+ 模块 D orphan auditor** |
| Cat B — 未经 spec 直接产新 module | 无 | **+ G11 decision-record-or-die** |
| Cat B — 跨 PR 链路累积漂移 | 人工 Explore ritual | **+ `/code-review ultra` 自动 N-PR diff 审查** |

按 T2/T3 chain 与 CI Cat B chain 两次 review 的 finding 类分布估算：
- Cat A 覆盖：~90% → **~95%**
- Cat B 覆盖：~50% → **~75%**

---

## §6 实施顺序（按 ROI）

| # | 任务 | 估时 | 风险 | 即效 |
|---|---|---|---|---|
| **P1** | 模块 B：catalog-derived 计数 + ID 白名单（替 G7 grep） | 半天 | 极低 | 消除一整类 Cat B drift |
| **P2** | 模块 D：扩 `spec_freshness_audit.py` 加 orphan-spec 检查 | 半天 | 极低 | 新增 D1/D2 漂移侦测 |
| **P3** | 模块 B：METBENCH002 通用 field-flow tracer Roslyn 分析器 | 2-3 天 | 中（AST 复杂度） | L1 Cat B 覆盖大幅扩 |
| **P4** | 模块 C：Stryker baseline 观察 3 周 → 升级 PR-delta gate（break=-3pp） | 时间（cron 自跑）+ 半天 workflow 改 | 中（baseline 漂移可能） | 激活第四层 §12.5 负空间守卫 |
| **P5** | 模块 E：`/code-review ultra` 喂入累积 diff 自动化脚本 + checklist 步骤 | 1 天 | 低（每 chain 一次 cost 可控） | Cat B 全谱再覆一层 |
| **P6** | 模块 F：CLAUDE.md §12 加作者侧 advisory + `pr-gate-checklist.md` Review 节加 advisory bullet | 半天 | 极低 | 作者侧 Cat A 前置 |
| **P7** | 模块 B：G11 decision-record-or-die grep + 模板 | 半天 | 低 | 流程纪律，预防新 Cat B |

**总计 ≈ 7-9 个工作日**（其中 P4 含 3 周 baseline 观察等待，可与其他 P 并行）。

每 P 走独立 PR；P1-P7 全部合入后跑一次 `/code-review ultra` 对全套 v2 diff 做整体审查（即用即验证）。

---

## §7 落地形态

1. 本 spec 是 v2 章程的**真相层**。CLAUDE.md §12 是其精简操作版。
2. CLAUDE.md §12 v2 改写在同一 PR 内完成。
3. P1-P7 各自 scoped plan 注册到 `docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md` §1。
4. 状态账本 `docs/status/current.md` 不在本 PR 改动——它在每 P 合入后随该 P 的 ledger refresh PR 更新。

---

## §8 风险 & Stop

- **Risk A** — P4 Stryker delta gate 阈值定错（过严 → noise，过松 → 无效）。Mitigation：3 周 baseline 严格观察后再定 break=-3pp，且保留一周 informational 缓冲期。
- **Risk B** — P3 METBENCH002 AST 分析器误报。Mitigation：先 Info 严重级 + 注册表白名单逃生口；至少 1 周观察期后才升 Warning。
- **Risk C** — `/code-review ultra` cost 超预算。Mitigation：仅在 ≥ 3-PR chain 触发；非链 PR 走模块 F 作者侧自费。
- **Stop** — 若 P1 catalog-derived 计数实施时发现 catalog 有 source-of-truth 多源问题（如 manifest JSON vs LegacyCatalogFactory.cs 两套），停下来先 R1 parity 化，再做 P1。

---

## §9 引用

- v1 章程：`CLAUDE.md §12`（被本 spec 同 PR 改写为 v2）
- §12.4 R1-R4 文本：`CLAUDE.md §12.4`（保留）
- 既有 §12.5 守卫表：`CLAUDE.md §12.5`（被本 spec §3 模块 B/C 接管解释）
- 既有 chain-end checklist：`docs/superpowers/templates/chain-end-review-checklist.md`
- PR Gate Checklist：`docs/superpowers/templates/pr-gate-checklist.md`（P5 / P6 中追加 sub-check）
- 现有 grep `governance` job：`.github/workflows/dotnet-test.yml`
- 现有 Roslyn analyzer 项目：`MetBench_Analyzers/`
- 现有 Stryker pilot：`tools/mutation-testing/`
- 现有 spec-freshness：`tools/spec_freshness_audit.py` + `.github/workflows/spec-freshness-monitor.yml`
- Worked example post-merge reviews：`docs/superpowers/specs/2026-05-27-t2-t3-chain-post-merge-review.md` + `docs/superpowers/specs/2026-05-27-ci-cat-b-hardening-chain-post-merge-review.md`
