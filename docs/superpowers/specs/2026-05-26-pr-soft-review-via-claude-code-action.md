# PR AI Review Gate via Codex + Claude Code

> ⚠️ **RETIRED on 2026-05-27** — this design has been replaced by the inline
> `governance` grep job inside `.github/workflows/dotnet-test.yml`. The
> corresponding workflow file `.github/workflows/pr-soft-review.yml` has been
> deleted. See `CLAUDE.md §12.1` for the post-retirement architecture and the
> rationale. The text below is kept as historical record so future
> contributors can understand what the AI-review attempt looked like and why
> it was retired.
>
> **Reasons for retirement** (summary):
> 1. OpenAI Codex action consistently failed with "Quota exceeded" in ~30 s
>    against the `OPENAI_API_KEY`-bound org, posting noisy advisory comments
>    on every PR push.
> 2. `anthropics/claude-code-action@v1`'s anti-injection guard (correctly)
>    refuses to issue an app token when the workflow file in the PR head
>    differs from `main` — so any workflow-touching PR self-fails both AI
>    review jobs with 401 (CLAUDE.md §12.2 R6).
> 3. The 2026-05-27 T2/T3 chain post-merge review found 11 findings; **0**
>    of them had been surfaced by the AI review layer. The §12.4 R1 + R4
>    parity / contract-honor facts (#199 + #195 cleanup PRs) mechanized
>    catching equivalents, making the AI layer redundant.
> 4. Per-PR cost: ~5 min runner-time × 2 jobs × ~140-package npm install ×
>    Claude OAuth + OpenAI API token spend. Mechanical grep (current
>    `governance` job) accomplishes the same checks in < 10 s.
>
> **Original date**: 2026-05-26; updated 2026-05-27 (twice — second update is this retirement notice)
> **Original status**: Active design; implementation lived in `.github/workflows/pr-soft-review.yml` (deleted 2026-05-27)
> **Original scope**: Define the advisory LLM-based PR review layer that ran in GitHub Actions on PR open / synchronize / reopen / ready-for-review / body edit.
> **Hard-gate counterpart**: `.github/workflows/dotnet-test.yml` (existing, blocking; now also hosts the `governance` grep job that replaces the design below).
> **Checklist counterpart**: `docs/superpowers/templates/pr-gate-checklist.md` (this spec was referenced from its "AI Review" section; checklist still keeps the section but it is no longer enforced by AI).

---

## 1. 动机

2026-05-26 的 retrospective review 暴露了一个 process gap：四个 PR (#138 / #140 / #141 / #142) 全部以"本地测试通过 + CI test job 绿 → squash 合并"的轻量流程合入主线，**没有任何代码 review 步骤**。CI 通过 ≠ review 通过。手工 PR Gate Checklist 已记录在 `pr-gate-checklist.md`，但纯靠 agent / 人记得贴 checklist 是不可靠的：上面四个 PR 全部漏贴。

本 spec 引入一个**自动化的、LLM-based 的 advisory review 层**，把"读 diff → 对照 checklist → 在 PR 留 comment"这个本应有人做的步骤变成 PR 生命周期的固定动作（结论仍是 advisory）。

2026-05-27 升级后，这一层拆成两名 reviewer：

- **Codex Governance Review**：主审项目控制面，检查 scope、需求 / 计划追溯、状态账本、projection docs、Windows classification、Method MT / System MT 边界、docs-only baseline 误报。
- **Claude Semantic Review**：副审代码语义面，检查 C# 逻辑、异常路径、System MT runtime 边界、Catalog/Typed predicate 使用、测试是否证明行为、WPF 语义风险。

---

## 2. 范围

### 在范围

- 在 `pull_request` 事件触发时自动起 `openai/codex-action@v1` 和 `anthropics/claude-code-action@v1`
- Codex 跑治理 prompt；Claude 跑语义 prompt
- 把两类审查发现以两个独立 PR top-level comment 贴回 PR
- 失败模式：action 本身 fail 不挡 merge（advisory 性质）

### 不在范围

- **不替代** `dotnet test` 硬门
- **不替代**人类对数学 / 物理 / 主观措辞的判断
- **不**直接 push 代码改动到 PR 分支（只贴 comment）
- **不**强制 merge 前 AI 必须报 "approved"（noise 误报会把 PR 永久卡住）
- **不**改 PR labels / milestones / assignees（最小权限）

---

## 3. 触发条件

- Event: `pull_request`
- Types: `opened`, `synchronize`, `reopened`, `ready_for_review`, `edited`
- Not on `draft: true` PRs（先标准 ready-for-review 再消耗 review 额度）
- Docs-only PRs also run：Codex governance review is specifically useful for status-ledger / plan-index drift
- Branch filter: `base: main` 才跑（feature-to-feature PR 不消耗配额）

---

## 4. 认证

| Reviewer | Secret 名 | 来源 | 失效条件 |
|---|---|---|---|
| Codex | `OPENAI_API_KEY` | OpenAI Platform project API key, stored as GitHub Actions secret | key revoked / quota exhausted / OpenAI API unavailable |
| Claude Code | `CLAUDE_CODE_OAUTH_TOKEN` | Repo owner 本地跑 `claude setup-token` 用 Max 账户登录生成 | OAuth token revoked / expired / Anthropic deprecates OAuth mode |

**前置 operator action（不可被 agent 完成）**：

1. 在 OpenAI Platform 创建 project API key，加入 GH repo Settings → Secrets and variables → Actions，命名 `OPENAI_API_KEY`。
2. Repo owner 本地执行 `claude setup-token`（一次性）。
3. 把生成的 token 加到 GH repo Settings → Secrets and variables → Actions，命名 `CLAUDE_CODE_OAUTH_TOKEN`。**粘贴时 token 字符串不可包含前导 / 尾部空白或换行**（GitHub Secrets UI 会原样保留），否则 Anthropic SDK 会拒绝 HTTP header 抛 `API Error: Header '14' has invalid value: '*** ***'`（脱敏后两个 `***` 之间有空格是这个 bug 的指纹）—— 详见 §11 R7。
4. 确认 Claude GH App 已安装并对本仓库开启 Contents / Issues / Pull requests 读写权限。

实现 PR 在 secret 配置完成之前 merge 也可以，workflow 会报告 advisory unavailable（不挡 merge）。

---

## 5. 输入 / 输出 契约

### 输入

- PR diff（base..head）
- PR 标题 + body
- 仓库代码（通过 `actions/checkout@v5` 拉到 runner）
- workflow 中显式传入的 Codex / Claude prompts
- Claude `claude_args`（限 max-turns + allowedTools 防 runaway）

### 输出

- **唯一允许的 side effect**：在 PR 上发 top-level advisory comments
- **不允许**：push commits、修 labels、open issues、merge、close PR
- review 结论格式固定（见 §7 prompt 模板）

### 失败行为

| 失败原因 | 表现 | 影响 merge？ |
|---|---|---|
| `OPENAI_API_KEY` 未配置 | Codex step fails; fallback comment says unavailable | 否 |
| `CLAUDE_CODE_OAUTH_TOKEN` 未配置 | Claude step fails; fallback comment says unavailable | 否 |
| OpenAI / Anthropic quota exhausted | action step fails; fallback comment says unavailable | 否（job 不在 required checks 列表里） |
| OpenAI / Anthropic API 5xx | action step fails; fallback comment says unavailable | 否 |
| Runner timeout（默认 6h） | job killed | 否 |
| LLM 误报 | review comment 仍发出 | 人工 override |

明确：**这个 workflow 永远不出现在 GH branch protection 的 required checks 列表里**。它的价值在于提示而非阻断。

---

## 6. 审查项映射

把 `pr-gate-checklist.md` 各项按机械可行性分类，并指明每项由谁来管：

| Checklist 项 | 由 Codex governance 检 | 由 Claude semantic 检 | 由 hard-gate (dotnet test) 检 | 由人 / 我 (agent) 判断 |
|---|---|---|---|---|
| Scope: 单一主目的 | ✅ PR body vs path diff | ⚠ only if code coupling suggests scope creep | — | — |
| Scope: 不混 feature / governance / cleanup | ✅ | ⚠ | — | ⚠ 边界情形人工 |
| Scope: 状态账本变动声明 | ✅ | — | — | — |
| Facts: origin/main head 检查 | ✅ PR body / diff evidence | — | — | — |
| Facts: 状态账本更新 | ✅ | — | — | — |
| Facts: projection 文档同步 | ✅ | — | — | — |
| Tests: focused tests 跑过 | ✅ PR body evidence | ✅ adequacy of tests | ✅ `dotnet test` | — |
| Tests: full baseline | ✅ PR body evidence | ✅ missing behavioral coverage | ✅ `dotnet test` 跑全套 | — |
| Tests: docs-only 不claim 新 baseline | ✅ | — | — | — |
| Windows Classification | ✅ path → required evidence | ✅ WPF semantic risk if touched | — | — |
| Review: Layer 1 | ✅ evidence present | ✅ code-level review comment | — | — |
| Review: Layer 2（独立人审） | ⚠ advisory, not replacement | ⚠ advisory, not replacement | — | ✅ 人 |
| Review: status drift / stale baseline | ✅ | — | — | — |
| Merge: required checks green | — | — | ✅ GH | — |
| Merge: merge method | — | — | ⚠ branch policy | ✅ 人 |
| Merge: 同步 main | — | — | — | ✅ agent |

**主要价值**：Codex 防止项目控制失真，Claude 防止代码语义失真。两者都不替代 hard `test` 和 human approval。

---

## 7. Workflow 实现口径

实现文件仍为 `.github/workflows/pr-soft-review.yml`，但 workflow name 升级为 `pr-ai-review`。保留原文件名是为了不打断既有文档链接和历史 PR 引用。

当前实现包含两个 advisory job：

- `codex-governance-review`：使用 `openai/codex-action@v1`，`OPENAI_API_KEY`，`sandbox: read-only`，`safety-strategy: read-only`。Codex 只读仓库和 PR diff，输出 `## Codex Governance Review (Advisory)` comment。
- `claude-semantic-review`：使用 `anthropics/claude-code-action@v1`，`CLAUDE_CODE_OAUTH_TOKEN`，`--max-turns 20`，并只允许 `gh pr comment/view/diff` 与只读 `git diff/log` Bash 工具。Claude 输出 `## Claude Semantic Review (Advisory)` comment。

两个 job 都 `continue-on-error: true`，失败时贴 advisory unavailable comment；它们不进入 branch protection required checks。

下方 YAML 块是 2026-05-26 的 bootstrap 历史模板，保留用于解释 R4/R5/R6/R7 风险来源；当前真实实现以 `.github/workflows/pr-soft-review.yml` 为准。

```yaml
# .github/workflows/pr-soft-review.yml
name: pr-soft-review

on:
  pull_request:
    types: [opened, synchronize, reopened]
    branches: [main]
    # Skip docs-only PRs: there is no compileable / pinned-count / Windows-classification
    # surface to check, and the merge-gate hard `test` job already covers everything else.
    # Saves ~5 min and ~140-package npm install per docs-only PR (e.g. scoped-plan PRs).
    paths-ignore:
      - 'docs/**'
      - '*.md'

permissions:
  contents: read
  pull-requests: write
  issues: write
  id-token: write  # required by claude-code-action@v1 internal OIDC handshake (see §11 R4)

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
      # Cache the npm package store so claude-code-action's transient install does not
      # re-download its ~140 dependencies on every PR push. Keyed on the action major
      # version so a bump to v2 invalidates the cache. Cache miss is fine — first run
      # populates it; subsequent runs hit and skip the network step inside the action.
      - name: Cache npm store
        uses: actions/cache@v4
        with:
          path: ~/.npm
          key: ${{ runner.os }}-claude-code-action-v1-npm
          restore-keys: |
            ${{ runner.os }}-claude-code-action-v1-npm
      - name: Claude soft review
        uses: anthropics/claude-code-action@v1
        with:
          claude_code_oauth_token: ${{ secrets.CLAUDE_CODE_OAUTH_TOKEN }}
          claude_args: |
            --max-turns 20
            --allowedTools "Bash(gh pr comment:*),Bash(gh pr view:*),Bash(gh pr diff:*),Bash(git diff:*),Bash(git log:*)"
          prompt: |
            REPO: ${{ github.repository }}
            PR NUMBER: ${{ github.event.pull_request.number }}

            You are reviewing a pull request against the MetBench PR Gate Checklist.
            The PR branch is already checked out in the current working directory.

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

            Post your findings as a single top-level PR comment using:
              gh pr comment ${{ github.event.pull_request.number }} --body "<your review>"
            with sections:
              ## Soft Review: PR Gate Checklist (Advisory)
              ### Mechanical checks
              ### MetBench-specific cross-checks
              ### Reviewer note for the human approver

            Be terse. Bullet points, not prose. Quote file:line for any FAIL.
            Only post the GitHub comment — do NOT submit review text as messages.

            Do NOT push commits. Do NOT change labels. Do NOT request changes via the
            review API — leave it as a top-level comment so a human stays in the
            approve loop.
```

---

## 8. 与 hard-gate 的关系

```
PR opened / synchronize
        ↓
   ┌────┬─────────────┐
   ↓    ↓             ↓
hard   Codex         Claude
test   governance    semantic
   ↓    ↓             ↓
required advisory     advisory
   ↓    ↓             ↓
green? findings → PR comments
   ↓
merge ok
```

三者**完全独立**：
- AI review 出错不影响 hard-gate 是否绿
- hard-gate 红时 AI review 仍然会跑（也可能有用 — 帮诊断为什么红）
- merge 阻断只由 hard-gate + branch protection 控制

---

## 9. 验收标准

| 项 | 验证方式 |
|---|---|
| Spec 合入 main | 本 PR merge |
| `pr-gate-checklist.md` 引用本 spec | grep 本 spec 文件名命中 checklist AI Review section |
| active plan index §2 注册本 spec | grep 文件名命中 index 表格 |
| Operator action 文档化 | §4 节存在 |
| 实现 PR（workflow YAML）作为独立 follow-up | 本 spec 不含 YAML 部署；另起 PR 时 secret 已配置 |
| AI review 真实首跑验证 | 修改 `pr-soft-review.yml` 的 PR **不算自验**（GitHub workflow-validation 安全门，详见 R6）；以**该 PR 合并后下一个开向 main 的 PR** 收到 "Codex Governance Review (Advisory)" 与 "Claude Semantic Review (Advisory)" 两条评论为准 |

---

## 10. 退役条件

本 spec 失效的可能触发：

1. OpenAI 弃用 `openai/codex-action@v1` 或 Anthropic 弃用 `anthropics/claude-code-action@v1` / OAuth 模式
2. OpenAI API key 或 Max 订阅条款变更不再覆盖 headless action 调用
3. OpenAI / Anthropic action 任一侧被项目替换为 self-hosted LLM 或别的 review pipeline
4. 团队规模扩大到需要专人 Layer-2 review，本层 advisory 价值缩水

任一触发 → 起替代 spec → 在本文件加 "Superseded by: …" 头注 → 在 active plan index §2 把本 spec 移到"条件性活跃"或历史区。

---

## 11. 未决问题 / 风险

- **风险 R1**：LLM 误报噪音。缓解：advisory only；review comment 可被人忽略；通过迭代调 prompt 收敛。
- **风险 R2**：Max 配额跟交互用量共享。缓解：先观察 30 天用量趋势再决定是否升级或限频。
- **风险 R3**：prompt 漂移导致 review 质量不稳定。缓解：本 spec 的 §7 prompt 模板是 single source of truth；workflow YAML 必须从这里复制；改 prompt = 改 spec = 走 PR review。
- **风险 R4（self-bootstrap 发现）**：`anthropics/claude-code-action@v1` 内部要做一次 GitHub OIDC handshake，即使走 `claude_code_oauth_token` 路径也需要 workflow `permissions:` 含 `id-token: write`。缺这一行 → 24 秒内挂掉、`Could not fetch an OIDC token` 报错。已在 §7 模板里加 `id-token: write`。任何复制本模板的新 workflow 必须保留这一行；任何对 v1 的 minor 升级要复查该要求是否变更。**首次落地的 PR #145 就是被这个缺权限挡住的**，记录在本 §11 以防重蹈。
- **风险 R5（self-bootstrap 第二次发现）**：claude-code-action `prompt:` 只声明意图不开通工具。即使 prompt 里说 "post a comment"，Claude 也必须通过 `claude_args: --allowedTools "..."` 显式拿到 `Bash(gh pr comment:*)` 等工具的执行权。缺这个 → action 12 秒"成功"退出但完全没贴评论，因为模型有话想说没工具可用。已在 §7 模板里：(a) `claude_args` 加 `--allowedTools "Bash(gh pr comment:*),Bash(gh pr view:*),Bash(gh pr diff:*),Bash(git diff:*),Bash(git log:*)"`，(b) prompt 头加 `REPO:` / `PR NUMBER:` 上下文，(c) prompt 显式写出 `gh pr comment ${{ github.event.pull_request.number }} --body "..."` 命令样板。任何复制本模板要保留这三处；扩展工具白名单时也只列严格必需的命令前缀（不要 `Bash(*)`，最小权限）。**PR #145 第二次跑就是被这个挡住的**。
- **风险 R6（self-bootstrap 第三次发现 / 真因）**：GitHub 对 `anthropics/claude-code-action@v1` 施加 **workflow-validation 反注入安全门**：workflow 文件必须**已经存在于 default branch 且内容与 PR 分支完全一致**，action 才会真正执行；否则立即跳过、退出码 0（job 显示 success），log 里出现 `Skipping action due to workflow validation: Workflow validation failed. The workflow file must exist and have identical content to the version on the repository's default branch. ... your workflow will begin working once you merge your PR.`。这意味着：(a) **引入 `pr-soft-review.yml` 自身的那一个 PR 永远无法被自己的 soft-gate 覆盖** —— 必须先合并、之后开的 PR 才是第一次真实跑；(b) **任何后续修改 §7 模板的 PR 也会在自身上 silent-skip**（PR 分支版本 ≠ default branch 版本 → 同一安全门），合并后下一个 PR 才会用上新模板验证；(c) 这层 silent-skip 与 OIDC / 工具白名单**无关**，是 GitHub 平台级反注入设计，不可绕过。**PR #145 第三次跑就是被这个挡住的**；R4 / R5 描述的两次"修复"叠加在 R6 之上、看上去像 fix 实际无关，保留是因为符合官方推荐用法（最小权限白名单 + OIDC 显式权限）。**实操含义**：spec §9 验收标准里"自举 PR 自身收到 Soft Review 评论"是无法满足的，应改为"合并后下一个 PR 收到评论"。
- **风险 R7（operator 操作坑 / 不是模板缺陷）**：`CLAUDE_CODE_OAUTH_TOKEN` secret 在 GitHub Settings → Secrets and variables → Actions 输入框里**保留**任何前导 / 尾部空白或换行字符；从 `claude setup-token` 终端输出复制时极易把 `\n` 一起带进来。Anthropic SDK 把 token 拼成 `Authorization` header 时被 Node `http.validateHeaderValue` 拦截，action 25 秒内 failure 退出，log 里出现 `error: Claude Code returned an error result: API Error: Header '14' has invalid value: '*** ***'` —— **脱敏后两个 `***` 之间有空格**是这个 bug 的指纹（GitHub 把含空白的 secret 还是当一整串脱敏成 `***`，但 SDK 里这一串包含 CR/LF/space，写到 log 时被 secret-masker 切成两段并保留中间的空白）。修复路径是 operator action，不是改 workflow / spec 模板：在 GH Secret UI Update token，粘贴前先 `printf %s "$TOKEN"` 验证无尾部换行；不要直接 `echo` 后粘贴，`echo` 默认带 `\n`。**PR #147 第一次跑就是被这个挡住的**；与 R4 / R5 / R6 都不同源（R7 不是模板缺，是 operator 输入卫生），但失败诊断对未来重新 rotate token 的人有用。已在 §4 operator action 第 2 步加 warning 行。
- **风险 R8（large-PR turn budget）**：`--max-turns 10` 对小型 docs / SUT PR 足够，但对 Windows/WPF UI PR 这类 15+ 文件 diff 不足。PR #171 首次和重跑 soft-review 均在没有产生代码级 finding 前以 `Reached maximum number of turns (10)` 失败，导致 advisory review check 阻塞而不是给出审查结论。模板将 max-turns 提高到 20；这仍然保留 runaway 上限，同时给 UI / mixed backend+WPF PR 足够空间完成 checklist、读取 diff、并发出单条 PR comment。
- **未决 Q1**：是否要让 LLM 也读 `AGENTS.md` / `CLAUDE.md` 全文判断"该改但没改的 projection 文档"？目前 prompt 已包含 plan index 路径但未强制全读，先观察是否够用。
- **未决 Q2**：要不要对 self-PR（agent 自己开的 PR）也跑？目前会跑 — 这是好事，能 catch 我自己漏掉的；但 LLM 给 LLM 审 LLM 的递归审美是否真有价值还要看。

---

## 12. Implementation order

1. **Spec PR #143**：merge spec + 更新 checklist + 注册 plan index ✅
2. **Operator action**：仓库 owner 跑 `claude setup-token`，加 `CLAUDE_CODE_OAUTH_TOKEN` secret，安装 GH App ✅
3. **Workflow PR #145**：把 §7 模板原样落到 `.github/workflows/pr-soft-review.yml`；自身**无法**自验（见 R6） ✅
4. **本 R6 PR**：回写 R4 / R5 / R6 三次 self-bootstrap 发现并修正 §9 验收口径。本 PR **不动** `.github/workflows/pr-soft-review.yml`（workflow YAML 在 #145 已落地、PR 分支与 default branch 字节一致），因此**应当能通过 R6 workflow-validation 安全门，作为第一个真实跑 soft-gate 的 PR** —— 它本身就是 §9 末行验收的首个数据点
5. **观察 1-2 周**：根据 review noise 调 §7 prompt；改动须回写本 spec。注意任何改 §7 模板 / 改 workflow YAML 的 PR 都会重新触发 R6（自身 silent-skip，下一个 PR 才会用上新版本验证）
