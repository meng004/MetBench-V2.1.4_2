# P5 — `/code-review ultra` Chain-End Automation (v2 charter §6 P5)

> **Date**: 2026-05-28
> **Status**: Active scoped — 单 PR 改造
> **Implements**: v2 章程 §6 P5 — 模块 E 链尾整体审查的 `/code-review ultra` 累积 diff 自动喂入
> **Parent spec**: `docs/superpowers/specs/2026-05-28-code-governance-v2-charter.md` §3 E + §6 P5
> **CLAUDE.md anchors**: §12.2 模块 E 行 + §12.4 R2

---

## §1 目标 & 验收

把 v2 章程 §3 E 与 CLAUDE.md §12.4 R2 已承诺的 "`/code-review ultra` 自动喂入累积 diff" 从口头约定变成可调用工具，使任何 ≥ 3-PR chain 在 chain-end 时不必手算 `origin/main~N` 的 N、不必手敲完整 ultra 调用、不必凭记忆推断 review doc 命名。

**Scope-in**：
- 一个 stdlib-only python 脚本 `tools/chain_end_ultra_invocation.py` 输出现成的 `/code-review ultra` 调用 + 累积 diff 元数据 + 建议的 review doc 路径。
- `docs/superpowers/templates/chain-end-review-checklist.md` 在 ritual 入口与出口各加一步把脚本接入。
- 一个最小单元测试 `tools/tests/test_p5_chain_end_ultra_invocation.py`（与 P2 测试形态对齐）。

**Scope-out**（明确不做）：
- **不**真实调用 `/code-review ultra`（它是 superpowers skill，仅在 Claude Code session 内可触发）。
- **不**新加 GitHub Actions workflow / cron / required check —— P5 是 author-driven 而非 CI-driven（章程 §3 E 门类型已锁定）。
- **不**改 ledger / `docs/status/current.md` / R1-R4 文本 / `MetBench_Client/` / `SemanticCatalogBoundaryTests` / Method MT。

**验收（≥ 5 checkbox 在 §7）**：脚本针对一段已合入的 chain (T2/T3 phase 1-6) 干跑能输出完整 ultra 命令 + 文件列表 + 建议 review-doc 路径，且检查列表两处插入位置可由 `grep -n "Step 0\|^## Output" docs/superpowers/templates/chain-end-review-checklist.md` 验证。

---

## §2 当前状态 inventory

### 2.1 chain-end-review-checklist.md 现状（共 59 行）

结构（`grep -n "^##" docs/superpowers/templates/chain-end-review-checklist.md`）：

| 行 | 段 | 当前 item 数 |
|---|---|---|
| 5  | `## Scope` | 3 |
| 11 | `## Cross-PR design coherence` | 4 |
| 18 | `## Public contract honesty` | 4 |
| 25 | `## Spec doc retrospective` | 3 |
| 31 | `## Test surface` | 3 |
| 37 | `## Status ledger & projection docs` | 4 |
| 44 | `## Output` | 5 |
| 54 | `## Reference` | — |

L1 段 `## Scope` 的第 1 个 item（line 7）是 ritual 起点；`## Output` 最后一个 item（line 50）是 ritual 终点。

### 2.2 既有 worked example review docs 命名约定

| 文件 | chain 名 |
|---|---|
| `docs/superpowers/specs/2026-05-27-t2-t3-chain-post-merge-review.md` | `t2-t3-chain` |
| `docs/superpowers/specs/2026-05-27-ci-cat-b-hardening-chain-post-merge-review.md` | `ci-cat-b-hardening-chain` |

**约定**：`YYYY-MM-DD-<chain-name>-post-merge-review.md`，落在 `docs/superpowers/specs/`，date = 跑 review 当日（不必等于 chain merge 日）。

### 2.3 既有 tools/ 风格基线

`tools/spec_freshness_audit.py`（pure stdlib，`argparse` + `pathlib` + `subprocess` + `re` + `json`，shebang `#!/usr/bin/env python3`，模块级 docstring 引用 v2 charter 行号）+ `tools/tests/test_p2_spec_freshness_orphan.py`（stdlib-only，`tempfile`/`subprocess`，无 pytest 强依赖，`if __name__ == "__main__": main()` 双跑模式）—— 本 plan 完全遵从。

### 2.4 `/code-review ultra` skill 接口（来自 available-skills + CLAUDE.md §12.4 R2）

CLAUDE.md §12.4 R2：`/code-review ultra --base origin/main~N --head HEAD`。available-skills 描述：`Pass --comment to post findings as inline PR comments, or --fix to apply the findings`，effort levels `low/medium/high → max/ultra`。脚本仅生成调用字符串，不调 skill。

---

## §3 设计

### 3.1 脚本设计：`tools/chain_end_ultra_invocation.py`

**语言**：python 3.11+ stdlib only（与 P2 一致），`#!/usr/bin/env python3`。

**CLI**（argparse）：

```
python3 tools/chain_end_ultra_invocation.py \
    --base <git-ref> \
    --head <git-ref> \
    [--chain-name <slug>] \
    [--review-date YYYY-MM-DD] \
    [--repo-root .] \
    [--diff-preview-lines 50]
```

- `--base` / `--head`: 必填。任何 git ref（SHA / branch / `origin/main~N`）。
- `--chain-name`: 可选；缺省时脚本推断为 `chain-<base-short>-to-<head-short>` 并明确警告"建议显式传 --chain-name"。
- `--review-date`: 可选；缺省 `date +%F`（系统当前日期）。
- `--repo-root`: 可选；缺省 `pathlib.Path(__file__).resolve().parent.parent`（与 spec_freshness_audit 对齐）。
- `--diff-preview-lines`: 可选；缺省 50；输出前 N 行 unified diff 作为预览。

**输出**（stdout，markdown-fenced，可贴回 review doc）：

```
## Chain-End Ultra Invocation (auto-generated)

Base: <full-base-sha>  (resolved from <input>)
Head: <full-head-sha>  (resolved from <input>)
Chain length: <N> commits  ( = `git rev-list --count <base>..<head>` )
Chain name: <chain-name>
Suggested review doc: docs/superpowers/specs/<YYYY-MM-DD>-<chain-name>-post-merge-review.md

### Invocation

    /code-review ultra --base <base-sha> --head <head-sha>

(If the superpowers skill requires stdin-form, use instead:
    git diff <base-sha>..<head-sha> | /code-review ultra)

### git diff --stat

<stdout of `git diff --stat <base>..<head>`>

### Files touched (--name-only)

<one path per line>

### Diff preview (first <K> lines of unified diff)

<diff fenced block>
```

**Refs 解析**：先 `git rev-parse --verify <ref>^{commit}`；失败 → `print(..., file=sys.stderr); sys.exit(2)`（与 spec_freshness_audit `sys.exit(2)` 一致）。

**`--base` `--head` 同一 commit** → exit 2 ("empty chain") 报错；不输出空 invocation。

**纯只读**：只用 `git rev-parse` / `git diff` / `git rev-list` / `git log`，无 `git checkout`、无 `git fetch`、无写盘。

**幂等**：脚本输出无副作用，可重复调用；缺省不创建任何文件。

### 3.2 chain-end-review-checklist.md 改动（仅 2 处）

**插入点 A** — Scope 段第一个 item 之前（**当前 line 7 之前**插入新章节，使 Step 0 在所有 ritual checkbox 之前）：

```
### Step 0 — Generate ultra invocation (P5 automation)

- [ ] Ran `python3 tools/chain_end_ultra_invocation.py --base <chain-base-ref> --head <chain-head-sha> --chain-name <chain-slug>` and captured the output into the working notes of this review session.
- [ ] The generated `/code-review ultra ...` line was invoked (or its stdin-piped variant if the skill's arg form requires that), with output saved alongside this checklist's findings.
- [ ] The suggested review-doc path (`docs/superpowers/specs/YYYY-MM-DD-<chain-name>-post-merge-review.md`) was reconciled against `§2.2` of the P5 plan and §2 chain-naming examples; if the slug differs, document why in the review doc header.
```

**插入点 B** — `## Output` 段最后一个 item（当前 line 50）之后，新加一行：

```
- [ ] Step 0 ultra-invocation output is cross-linked from the review doc as an "Auxiliary artifacts" section (immediately after §1 chain phases table), with the exact `/code-review ultra ...` command, full base + head SHAs, and finding-vs-ultra-finding reconciliation notes (which ultra findings the human ritual confirmed / dismissed / extended).
```

**注**：R1-R4 文本、`## Reference` 段、其他 5 段的 item 顺序与文字均不动（§0.5 ANTI-UNREQUESTED-EDIT）。

### 3.3 单元测试 `tools/tests/test_p5_chain_end_ultra_invocation.py`

**决定**：**加最小测试**（不省）。理由：脚本含 git subprocess 调用 + 错误分支（base 不存在 / base==head），人工 dry-run 只能覆盖 happy-path，错误分支需要单测 mock 一个临时 git repo。

**覆盖**（≥ 4 case，对齐 P2 测试 case 数量）：
1. **happy_path_two_commit_chain**：临时 `git init` + 2 commit，`--base HEAD~1 --head HEAD` → exit 0、stdout 含 `chain length: 1`、含 `## git diff --stat`、含建议 review-doc 路径模板。
2. **base_ref_invalid**：`--base bad-sha --head HEAD` → exit 2、stderr 含 "rev-parse failed"。
3. **empty_chain_same_sha**：`--base HEAD --head HEAD` → exit 2、stderr 含 "empty chain"。
4. **chain_name_inference**：无 `--chain-name` → stdout 含 `chain-<short>-to-<short>` 并打印 "warn: provide --chain-name explicitly".

形态：`subprocess.run([sys.executable, str(SCRIPT), ...])`，`tempfile.TemporaryDirectory()` 建临时 repo（`git init` + `GIT_AUTHOR_NAME=test git commit --allow-empty -m`），stdlib only，无 pytest 硬依赖，`if __name__ == "__main__": main()` 双跑（同 P2）。

---

## §4 测试策略

### 4.1 自动

- **CI**：当前 `.github/workflows/dotnet-test.yml` governance job 已跑 grep 守卫，不需新 workflow（P5 章程 §3 E 是 author-driven，不进 CI）。
- **本地单测**：`python3 tools/tests/test_p5_chain_end_ultra_invocation.py` 必须返回 exit 0、≥ 4 case 全 pass。

### 4.2 manual smoke（commit 前）

干跑两段已合入 chain：

| chain | base | head | 期望 chain length |
|---|---|---|---|
| T2/T3 6-phase | `7526407~1`（PR #184 父） | `d2e1c5d`（PR #193 merge） | 6（或近似，依实际 squash-merge 数）|
| CI Cat B 5-phase | `272c51d~1`（PR #207 父） | `4398a47`（PR #212 merge） | 5（或近似）|

每段验证：脚本 exit 0、`/code-review ultra` 字符串完整、`git diff --stat` 非空、建议 review-doc 路径与现有 `docs/superpowers/specs/2026-05-27-{t2-t3,ci-cat-b-hardening}-chain-post-merge-review.md` 文件名同 pattern。

不真跑 `/code-review ultra`（成本控制 + 仅在 chain end 跑一次）。

---

## §5 风险 & Stop

| # | Risk | Mitigation |
|---|---|---|
| R-1 | `/code-review ultra` skill 实际 arg 形态 ≠ CLAUDE.md §12.4 R2 描述的 `--base / --head`（可能只接 stdin diff） | 脚本输出**同时**给出两种调用形态（直接 args + stdin-piped fallback）；checklist Step 0 第二个 item 写明 "or its stdin-piped variant if required"，让 ritual 执行者按实际 skill 接受形态择一 |
| R-2 | `--chain-name` 推断默认 `chain-<short>-to-<short>` 与既有 worked example `t2-t3-chain` / `ci-cat-b-hardening-chain` 命名不一致 → 建议的 review-doc 路径误导 | 缺省时脚本明确 warn 建议传 `--chain-name`；checklist Step 0 第三个 item 强制 reconcile against §2.2 plan 表 |
| R-3 | base ref 在浅克隆 CI 环境不可达（`git rev-parse` 失败） | 章程已锁定 P5 为 author-driven、本地 / fresh-session 跑；CI 不调；脚本错误信息 "rev-parse failed: ensure full git history is available locally" 显式提示 |
| R-4 | 大 chain 的 `git diff` 巨大 → stdout 失控 | 脚本只输出 `--name-only` 全集 + `--stat` 全集 + 前 50 行 unified diff（`--diff-preview-lines` 可调）；不输出完整 diff |
| R-5 | review doc 命名约定后续变化 | checklist Step 0 第三个 item 把 reconcile 责任落在人，脚本只给 "suggested"；变更约定时只动 §2.2 plan 表与 checklist 文字，不动脚本 |

**Stop conditions**：

1. 若 `docs/superpowers/templates/chain-end-review-checklist.md` 文件不存在或位置不同：立刻停下汇报；可能 P1/P2/P6/P7 已重排 templates 目录。
2. 若 `/code-review ultra` skill 在 superpowers 实测中既不接 `--base/--head` 也不接 stdin diff（即只能在 current working tree 跑）：停下汇报；脚本设计需重新评估为 "preview-only"（仅打印 stat + filelist，不再喂 ultra）。
3. 若发现 v2 charter §3 E 行已被先于本 PR 的另一 PR 修订：停下汇报；可能 P5 范围已被改写。

---

## §6 执行步骤（≤ 8）

1. 创建 `tools/chain_end_ultra_invocation.py`（§3.1 CLI + 输出格式，stdlib only），`chmod +x` 并保留 shebang `#!/usr/bin/env python3`。
2. 创建 `tools/tests/test_p5_chain_end_ultra_invocation.py`（§3.3 ≥ 4 case），本地跑 `python3 tools/tests/test_p5_chain_end_ultra_invocation.py` 验证全 pass。
3. 干跑 manual smoke (§4.2)：两段 chain 各执行一次，把输出贴入 PR body 作为 evidence。
4. 修改 `docs/superpowers/templates/chain-end-review-checklist.md`：在 line 7 前插入 `### Step 0` 三 item 块；在 line 50 之后追加 Step N 一 item。**不动** Scope 现有 3 item、Cross-PR / Public contract / Spec doc / Test surface / Status ledger / Output 其余 item 文字 + R1-R4 任何文本。
5. 在 `docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md` §1 加一行，指向本 plan，状态 `Active scoped — 单 PR 改造`，summary "v2 charter P5：`/code-review ultra` 链尾自动喂入 + checklist Step 0/N 接入，落点模块 E + R2"，expiry `Expires on PR merge`。
6. `git add tools/chain_end_ultra_invocation.py tools/tests/test_p5_chain_end_ultra_invocation.py docs/superpowers/templates/chain-end-review-checklist.md docs/superpowers/plans/2026-05-28-p5-ultra-chainend-automation-plan.md docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md` —— 显式列文件，不 `git add -A`。
7. Commit：`feat(governance): add chain-end ultra-invocation helper + checklist Step 0/N (v2 charter P5)`；commit body 引用 charter §6 P5 + CLAUDE.md §12.2 模块 E + §12.4 R2。
8. `git push -u origin claude/p5-ultra-chainend-automation`。**不**开 PR、**不**改 ledger、**不**改 `docs/status/current.md`、**不**改 R1-R4 文本、**不**碰 G11 / decision record。

---

## §7 验收标准（≥ 5 checkbox）

- [ ] `tools/chain_end_ultra_invocation.py` 存在，stdlib-only，shebang + module docstring 引用 v2 charter §6 P5 + CLAUDE.md §12.4 R2；针对 `--base 7526407~1 --head d2e1c5d --chain-name t2-t3-chain` 干跑 exit 0 且 stdout 含 `/code-review ultra --base 7526407 --head d2e1c5d`（完整 SHA + 完整命令）+ `Suggested review doc: docs/superpowers/specs/2026-05-28-t2-t3-chain-post-merge-review.md`。
- [ ] `tools/tests/test_p5_chain_end_ultra_invocation.py` 含 ≥ 4 case 覆盖 happy / base-invalid / empty-chain / chain-name-inference；`python3 tools/tests/test_p5_chain_end_ultra_invocation.py` 在干净环境 exit 0。
- [ ] `docs/superpowers/templates/chain-end-review-checklist.md` 在 `## Scope` 之前/同段插入了 `### Step 0` 三 item（`grep -n "Step 0 — Generate ultra invocation" ...` 命中 1 次）；在 `## Output` 段尾新加 1 个关于 "Auxiliary artifacts" 的 item（`grep -n "Auxiliary artifacts" ...` 命中 1 次）。
- [ ] `docs/superpowers/templates/chain-end-review-checklist.md` 其它 5 段（Cross-PR / Public contract / Spec doc / Test surface / Status ledger）的现有 item 文字 + `## Reference` 全段，与 `origin/main` 比较 byte-for-byte 不变。
- [ ] `docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md` §1 多一行 P5 注册，行内引用本 plan 路径。
- [ ] CLAUDE.md / R1-R4 文本 / `docs/status/current.md` / `MetBench_Client/**` / `SemanticCatalogBoundaryTests/**` / Method MT / `.github/workflows/**` 任意 YAML 与 fact `.cs` 文件 0 改动。
- [ ] Plan 文件 ≤ 250 行。
