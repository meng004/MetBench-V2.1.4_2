# PR Soft-Review Gate via claude-code-action (Max OAuth)

> **Date**: 2026-05-26
> **Status**: Active design (implementation YAML pending operator action)
> **Scope**: Define the advisory LLM-based PR review layer that runs in GitHub Actions on every PR open / synchronize.
> **Hard-gate counterpart**: `.github/workflows/dotnet-test.yml` (existing, blocking).
> **Checklist counterpart**: `docs/superpowers/templates/pr-gate-checklist.md` (this spec is referenced from its new "Soft Review" section).

---

## 1. 动机

2026-05-26 的 retrospective review 暴露了一个 process gap：四个 PR (#138 / #140 / #141 / #142) 全部以"本地测试通过 + CI test job 绿 → squash 合并"的轻量流程合入主线，**没有任何代码 review 步骤**。CI 通过 ≠ review 通过。手工 PR Gate Checklist 已记录在 `pr-gate-checklist.md`，但纯靠 agent / 人记得贴 checklist 是不可靠的：上面四个 PR 全部漏贴。

本 spec 引入一个**自动化的、LLM-based 的 advisory review 层**，把"读 diff → 对照 checklist → 在 PR 留 comment"这个本应有人做的步骤变成 PR 生命周期的硬约束（虽然结论本身仍是 advisory）。

---

## 2. 范围

### 在范围

- 在 `pull_request` 事件触发时自动起 `anthropics/claude-code-action@v1`（headless 模式）
- 跑一段固定的 review prompt，对照 `pr-gate-checklist.md` 的 Scope / Facts / Tests / Windows Classification 四节做机械审查
- 把审查发现以 PR review comment 形式贴回 PR
- 失败模式：action 本身 fail 不挡 merge（advisory 性质）

### 不在范围

- **不替代** `dotnet test` 硬门
- **不替代**人类对数学 / 物理 / 主观措辞的判断
- **不**直接 push 代码改动到 PR 分支（只贴 comment）
- **不**强制 merge 前 LLM 必须报 "approved"（noise 误报会把 PR 永久卡住）
- **不**改 PR labels / milestones / assignees（最小权限）

---

## 3. 触发条件

- Event: `pull_request`
- Types: `opened`, `synchronize`, `reopened`
- Not on `draft: true` PRs（先标准 ready-for-review 再消耗 review 额度）
- Not on PR 仅改 `.github/`、`*.md` 之外的纯 docs 路径时也跑（cheap 且能捕获 doc drift）
- Branch filter: `base: main` 才跑（feature-to-feature PR 不消耗配额）

---

## 4. 认证

| 项 | 值 |
|---|---|
| Secret 名 | `CLAUDE_CODE_OAUTH_TOKEN` |
| 来源 | 仓库 owner 在本地跑 `claude setup-token` 用 Max 账户登录生成 |
| Repo scope | `meng004/metbench-v2.1.4_2` |
| 计费 | 消耗 Max 订阅配额，**不**另外消耗 Anthropic API token billing |
| 失效条件 | OAuth token 被撤销 / 过期 / Anthropic 弃用 OAuth 模式 |

**前置 operator action（不可被 agent 完成）**：

1. Repo owner 本地执行 `claude setup-token`（一次性）
2. 把生成的 token 加到 GH repo Settings → Secrets and variables → Actions，命名 `CLAUDE_CODE_OAUTH_TOKEN`
3. 确认 Claude GH App 已安装并对本仓库开启了 Contents / Issues / Pull requests 读写权限

实现 PR 在 secret 配置完成之前 merge 也可以，workflow 会因缺 secret 直接跳过（不挡 merge）。

---

## 5. 输入 / 输出 契约

### 输入

`anthropics/claude-code-action@v1` 自动注入：
- PR diff（base..head）
- PR 标题 + body
- 仓库代码（通过 `actions/checkout@v4` 拉到 runner）

加上 workflow 中显式传入的 `prompt`（见 §7）和 `claude_args`（限 max-turns 防 runaway）。

### 输出

- **唯一允许的 side effect**：通过 GH App API 在 PR 上发 review comments
- **不允许**：push commits、修 labels、open issues、merge、close PR
- review 结论格式固定（见 §7 prompt 模板）

### 失败行为

| 失败原因 | 表现 | 影响 merge？ |
|---|---|---|
| `CLAUDE_CODE_OAUTH_TOKEN` 未配置 | workflow 跳过 step | 否 |
| Max 配额耗尽 | action step fails | 否（job 不在 required checks 列表里） |
| Anthropic API 5xx | action step fails | 否 |
| Runner timeout（默认 6h） | job killed | 否 |
| LLM 误报 | review comment 仍发出 | 人工 override |

明确：**这个 workflow 永远不出现在 GH branch protection 的 required checks 列表里**。它的价值在于提示而非阻断。

---

## 6. 审查项映射

把 `pr-gate-checklist.md` 各项按机械可行性分类，并指明每项由谁来管：

| Checklist 项 | 由 claude-code-action 检 | 由 hard-gate (dotnet test) 检 | 由人 / 我 (agent) 判断 |
|---|---|---|---|
| Scope: 单一主目的 | ✅ regex PR title + 路径 vs claim | — | — |
| Scope: 不混 feature / governance / cleanup | ✅ 跨目录路径混合检测 | — | ⚠ 边界情形人工 |
| Scope: 状态账本变动声明 | ✅ 检 PR body 是否声明 + 改动是否一致 | — | — |
| Facts: origin/main head 检查 | ⚠ 仅检 PR body 含此声明 | — | — |
| Facts: 状态账本更新 | ✅ 若改动含 source 文件，验 `docs/status/current.md` 是否在 PR 内或紧随其后的 PR | — | — |
| Facts: projection 文档同步 | ✅ 检 `AGENTS.md` / `CLAUDE.md` 等是否需要同步 | — | — |
| Tests: focused tests 跑过 | — | ✅ `dotnet test` | — |
| Tests: full baseline | — | ✅ `dotnet test` 跑全套 | — |
| Tests: docs-only 不claim 新 baseline | ✅ 检 PR body 是否违反 | — | — |
| Windows Classification | ✅ 改动文件路径 → 推断分类 → 对比 PR body 声明 | — | — |
| Review: Layer 1 | ✅ 这层就是 Layer 1 | — | — |
| Review: Layer 2（独立人审） | ⚠ LLM 给出建议但不替代 | — | ✅ 人 |
| Review: 显式检 status drift / stale baseline | ✅ 解析 ledger baseline commit vs git head | — | — |
| Merge: required checks green | — | ✅ GH | — |
| Merge: merge method | — | ⚠ branch policy | ✅ 人 |
| Merge: 同步 main | — | — | ✅ agent |

**主要价值**：上表中 ✅ 在 `claude-code-action` 列的 11 项，是过去四个 PR **完全漏掉**的检查。把它们自动化后，再漏掉就是 action 没跑或 LLM 出 bug，而不是流程缺失。

---

## 7. Workflow 模板

```yaml
# .github/workflows/pr-soft-review.yml
name: pr-soft-review

on:
  pull_request:
    types: [opened, synchronize, reopened]
    branches: [main]

permissions:
  contents: read
  pull-requests: write
  issues: write

jobs:
  review:
    if: github.event.pull_request.draft == false
    runs-on: ubuntu-24.04
    timeout-minutes: 15
    steps:
      - name: Checkout repo
        uses: actions/checkout@v4
        with:
          fetch-depth: 0
      - name: Claude soft review
        uses: anthropics/claude-code-action@v1
        with:
          claude_code_oauth_token: ${{ secrets.CLAUDE_CODE_OAUTH_TOKEN }}
          claude_args: "--max-turns 10"
          prompt: |
            You are reviewing a pull request against the MetBench PR Gate Checklist.

            Authoritative checklist: docs/superpowers/templates/pr-gate-checklist.md
            Authoritative ledger:    docs/status/current.md
            Authoritative plan index: docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md

            Run, in order, the mechanical sections of the PR Gate Checklist (Scope,
            Facts, Tests, Windows Classification) against THIS PR's actual diff and
            body. For each section, report PASS / WARN / FAIL with one-line evidence
            (file path, line, or PR body excerpt). Skip the Review section — that is
            your job. Skip the Merge section — that is the human's job.

            Then run these MetBench-specific cross-checks:
            1. If the diff touches MetBench_Client/** or any *.xaml*, the PR body
               Windows Classification must NOT be "No Windows evidence required".
            2. If the diff touches MetBench_BLL.Core/SystemMT/Catalog/** or
               LegacyCatalogFactory.cs, the description text in SUT/<sut>/catalog.json
               for any added MR must match the corresponding MrSummary.Description
               byte-for-byte.
            3. If the PR claims a new code-test baseline in its body but the diff
               contains zero changes outside docs/ or SUT/<sut>/sample/, flag it.
            4. If the PR base..head diff scope-creeps beyond what the PR body claims
               (e.g. body says "Burgers SUT" but diff also touches openmoc), flag it.
            5. If docs/status/current.md is in the diff, verify the baseline commit
               field references a commit that is reachable from origin/main as of the
               PR base; flag obvious stale-baseline copy-paste.

            Post your findings as a single PR review comment with sections:
              ## Soft Review: PR Gate Checklist (Advisory)
              ### Mechanical checks
              ### MetBench-specific cross-checks
              ### Reviewer note for the human approver

            Be terse. Bullet points, not prose. Quote file:line for any FAIL.

            Do NOT push commits. Do NOT change labels. Do NOT request changes via the
            review API — leave it as a comment so a human stays in the approve loop.
```

---

## 8. 与 hard-gate 的关系

```
PR opened / synchronize
        ↓
   ┌────┴────┐
   ↓         ↓
hard-gate  soft-gate
(dotnet)   (claude)
   ↓         ↓
required    advisory
   ↓         ↓
green?    findings → PR comment
   ↓
merge ok
```

两者**完全独立**：
- soft-gate 出错不影响 hard-gate 是否绿
- hard-gate 红时 soft-gate 仍然会跑（也可能有用 — 帮诊断为什么红）
- merge 阻断只由 hard-gate + branch protection 控制

---

## 9. 验收标准

| 项 | 验证方式 |
|---|---|
| Spec 合入 main | 本 PR merge |
| `pr-gate-checklist.md` 引用本 spec | grep 本 spec 文件名命中 checklist |
| active plan index §2 注册本 spec | grep 文件名命中 index 表格 |
| Operator action 文档化 | §4 节存在 |
| 实现 PR（workflow YAML）作为独立 follow-up | 本 spec 不含 YAML 部署；另起 PR 时 secret 已配置 |

---

## 10. 退役条件

本 spec 失效的可能触发：

1. Anthropic 弃用 `anthropics/claude-code-action@v1` 或其 OAuth 模式
2. Max 订阅条款变更不再覆盖 headless action 调用
3. 项目转向 self-hosted LLM 或别的 review pipeline
4. 团队规模扩大到需要专人 Layer-2 review，本层 advisory 价值缩水

任一触发 → 起替代 spec → 在本文件加 "Superseded by: …" 头注 → 在 active plan index §2 把本 spec 移到"条件性活跃"或历史区。

---

## 11. 未决问题 / 风险

- **风险 R1**：LLM 误报噪音。缓解：advisory only；review comment 可被人忽略；通过迭代调 prompt 收敛。
- **风险 R2**：Max 配额跟交互用量共享。缓解：先观察 30 天用量趋势再决定是否升级或限频。
- **风险 R3**：prompt 漂移导致 review 质量不稳定。缓解：本 spec 的 §7 prompt 模板是 single source of truth；workflow YAML 必须从这里复制；改 prompt = 改 spec = 走 PR review。
- **未决 Q1**：是否要让 LLM 也读 `AGENTS.md` / `CLAUDE.md` 全文判断"该改但没改的 projection 文档"？目前 prompt 已包含 plan index 路径但未强制全读，先观察是否够用。
- **未决 Q2**：要不要对 self-PR（agent 自己开的 PR）也跑？目前会跑 — 这是好事，能 catch 我自己漏掉的；但 LLM 给 LLM 审 LLM 的递归审美是否真有价值还要看。

---

## 12. Implementation order

1. **本 PR**：merge spec + 更新 checklist + 注册 plan index
2. **Operator action**：仓库 owner 跑 `claude setup-token`，加 `CLAUDE_CODE_OAUTH_TOKEN` secret
3. **Follow-up PR**：把 §7 模板原样落到 `.github/workflows/pr-soft-review.yml`；本 PR 自身是 soft-gate 落地后的第一个被审 PR — 自举验证
4. **观察 1-2 周**：根据 review noise 调 §7 prompt；改动须回写本 spec
