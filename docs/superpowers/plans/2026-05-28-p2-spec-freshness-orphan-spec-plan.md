# P2 计划 — Spec-Freshness Orphan-Spec 守卫扩展

> **Date**: 2026-05-28
> **Status**: Active scoped — 单 PR 改造
> **Implements**: v2 章程 §6 P2 行 — 模块 D（漂移侦测）扩 `spec_freshness_audit.py`
> 加 orphan-spec 检查；落点元规则 §12.4 R3（spec 偏离 retrospective 责任）
> **Parent spec**: `docs/superpowers/specs/2026-05-28-code-governance-v2-charter.md` §3 D + §6 P2 + §4 v1→v2 spec-freshness 行
> **CLAUDE.md anchor**: §12.2 模块 D 行

---

## §1 目标 & 验收

消除一类目前无任何机械守卫覆盖的 Cat B drift：**orphan spec** —— 即 `docs/superpowers/specs/**/*.md` 内存在但没有任何活跃 / 历史 plan 在 `active-plan-index.md` 内引用它的 spec 文档。

当前后果：spec 仅由文件系统存在维持"活着"的错觉；读者（人或 agent）无法判断该 spec 是 (a) 真正在指导某个 active 计划、(b) 已 retired 但忘记标注、(c) 漏注册到 index 的新 spec。R3 要求 retrospective 责任，但 R3 自身没有自动化 enforcement —— 本 PR 补这一层。

验收：本 PR 合并后，cron + spec/plan 改动触发的 spec-freshness-monitor workflow 在检测到任何 spec 未在 `active-plan-index.md` 出现时自动开（或更新）`governance:orphan-spec` GitHub issue，且 idempotent（重跑不重开、不复制）。

---

## §2 当前状态 inventory

### 2.1 spec 全集（`ls docs/superpowers/specs/*.md`）

```
2026-05-07-system-level-mt-bdd-design.md
2026-05-24-metbench-doc-runtime-alignment-design.md
2026-05-24-systemmt-catalog-convergence-design.md
2026-05-25-executionevidence-v2-design.md
2026-05-25-metbench-macro-assessment-and-risk-audit.md
2026-05-25-metbench-project-control-rules.md
2026-05-25-mr-verification-retrospective-review.md
2026-05-25-mr-verification-two-layer-review-policy.md
2026-05-25-mr-verification-v1.2-codex-ready.md
2026-05-25-systemmt-architecture-review-post-evidence-v2.md
2026-05-25-v12-pwr-migration-map.md
2026-05-25-verification-semantics-convergence-design.md
2026-05-26-pr-soft-review-via-claude-code-action.md
2026-05-26-t3-coverage-assessment-and-next-sut-decision.md
2026-05-27-ci-cat-b-hardening-chain-post-merge-review.md
2026-05-27-meta-pattern-coverage-audit.md
2026-05-27-t2-t3-chain-post-merge-review.md
2026-05-28-code-governance-v2-charter.md
```
共 18 个 spec。

### 2.2 当前 referenced 矩阵

按 `grep <basename> docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md`：

| spec basename | hit count | 备注 |
|---|---|---|
| 2026-05-24-systemmt-catalog-convergence-design.md | 1 | §2 条件性活跃 |
| 2026-05-25-executionevidence-v2-design.md | 1 | §3 已合并段 |
| 2026-05-25-metbench-macro-assessment-and-risk-audit.md | 1 | §2 active 设计 |
| 2026-05-25-metbench-project-control-rules.md | 1 | §2 active 设计 |
| 2026-05-25-mr-verification-v1.2-codex-ready.md | 1 | §2 active 设计 |
| 2026-05-25-systemmt-architecture-review-post-evidence-v2.md | 1 | §2 active 设计 |
| 2026-05-25-verification-semantics-convergence-design.md | 1 | §3 已合并段 |
| 2026-05-26-pr-soft-review-via-claude-code-action.md | 1 | §2 active 设计 |
| 2026-05-26-t3-coverage-assessment-and-next-sut-decision.md | 1 | §1 active 计划行 |
| 2026-05-27-ci-cat-b-hardening-chain-post-merge-review.md | 1 | §1 完成行的内联链接 |
| 2026-05-27-meta-pattern-coverage-audit.md | 1 | §1 完成行的内联链接 |
| 2026-05-27-t2-t3-chain-post-merge-review.md | 1 | §1 完成行的内联链接 |

### 2.3 当前真实 orphan（baseline）

6 个 spec 在 `active-plan-index.md` 内 **零引用**：

1. `2026-05-07-system-level-mt-bdd-design.md`（早于 active-plan-index 创立日期，可能是 pre-index 历史 spec）
2. `2026-05-24-metbench-doc-runtime-alignment-design.md`（已有 retired plan `2026-05-24-metbench-doc-runtime-alignment-plan.md` 在 §3 阶段性修复段；spec 自身未提）
3. `2026-05-25-mr-verification-retrospective-review.md`
4. `2026-05-25-mr-verification-two-layer-review-policy.md`
5. `2026-05-25-v12-pwr-migration-map.md`
6. `2026-05-28-code-governance-v2-charter.md`（**v2 章程本身**！CLAUDE.md §12 link 直接引用，但 active-plan-index 没收录 — 这是 #216 合入后的 gap，本 PR 必须连带修）

⚠️ **baseline = 6** —— 大于 §6 stop 阈值 5。**触发 stop 处理**：在 §7 执行步骤里加一步「先把 v2 charter 与本 P2 plan 一并注册到 `active-plan-index.md` §2，再决定剩余 5 个 spec 怎么处理」，把 baseline 降到 ≤ 5 后再开 PR。处理建议（不在本 plan 决断，留给 executor）：
- spec #1 / #3 / #4 / #5 → 加 `governance/orphan-spec-allowlist.txt` 条目（"pre-index 历史 spec，永不会有 active plan"）或在 active-plan-index §4 历史段加一行明确归类。
- spec #2 → 在 §3 阶段性修复段已存在的 plan 行后补一条 "对应 design spec：…"。
- spec #6（v2 charter）→ active-plan-index §2 加一行 Active 设计，scope "v2 治理章程，P1-P7 的真相层"。

---

## §3 设计

### 3.1 orphan-spec 检测算法（伪代码）

```
def audit_orphan_specs(repo_root, plan_index_path) -> list[dict]:
    spec_dir = repo_root / "docs/superpowers/specs"
    assert spec_dir.is_dir(), f"spec dir missing: {spec_dir}"

    plan_index_text = plan_index_path.read_text(encoding="utf-8")
    allowlist = load_allowlist(repo_root / ".github/governance/orphan-spec-allowlist.txt")

    orphans = []
    for spec_path in sorted(spec_dir.glob("*.md")):
        rel = spec_path.relative_to(repo_root).as_posix()       # 形如 docs/superpowers/specs/X.md
        basename = spec_path.name                                # 形如 X.md

        if rel in allowlist or basename in allowlist:
            continue

        # 引用判定：plan_index_text 任意位置出现 basename 或 rel 即视为 referenced
        if basename in plan_index_text or rel in plan_index_text:
            continue

        orphans.append({
            "spec_file": rel,
            "spec_age_days": spec_age_days(spec_path, repo_root),
            "reason": "spec not referenced by active-plan-index.md (by basename or path)",
        })
    return orphans
```

**边界情况**：
- spec 在 active-plan-index 内**作为内联链接** `[..](../specs/X.md)` 出现：basename `X.md` 仍命中 → referenced ✓
- spec 在 §4 历史段提及：本算法不区分 §1/§2/§3/§4，任意 section 出现即 referenced（避免误把 retired-but-known spec 当 orphan）
- spec 文件名含特殊正则字符：本算法用 `in` 子串比对，不走正则；安全
- active-plan-index.md 本身被 rename / 移动：脚本入口断言 `plan_index_path.is_file()`，否则 sys.exit(2)（fail-closed）
- spec 在 README / CLAUDE.md / current.md 等**其它** doc 引用但 active-plan-index 没收 —— 仍标 orphan（这是规则：active-plan-index 是 single source of truth for plan-spec 注册）

### 3.2 `spec_freshness_audit.py` 扩展位置

- 新加顶层函数 `audit_orphan_specs(repo_root: pathlib.Path, plan_index_path: pathlib.Path) -> list[dict]`，与既有 `find_claims / collect_known_mr_ids / spec_age_days` 同层
- 新加顶层函数 `load_orphan_allowlist(allowlist_path: pathlib.Path) -> set[str]`（与 `expected-catalog-counts.txt` 的 P1 helper 风格一致：#-注释 / 空行 / 一行一项 / strip）
- `main()` 内：
  1. 现有 stale-claim 输出 → 写 `/tmp/stale.json`（行为不变）
  2. **新增** 调用 `audit_orphan_specs(...)` → 写 `/tmp/orphan.json`（仅当 `--check orphan` 或默认全跑；见 3.3）
  3. 新加 `--check {stale,orphan,all}` arg，默认 `all`，向后兼容；workflow 显式用 `--check orphan` / `--check stale` 分别跑
  4. 新加 `--plan-index PATH` arg，默认 `docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md`
  5. fail-closed：若 `--check orphan` 但 plan-index 文件缺失，`sys.exit(2)`；spec_dir 缺失同样 exit
- 启动断言（新加在 `main` 开头）：`assert (repo_root / "docs/superpowers/specs").is_dir(), …`
- 输出格式：list of dict，每项 keys `spec_file / spec_age_days / reason`（与 stale 输出风格平行，方便 workflow jq 复用）

### 3.3 workflow YAML 改动（`.github/workflows/spec-freshness-monitor.yml`）

cron / trigger / permissions 都**不动**（v2 charter §3 D 已声明 weekly cron + spec/plan 改动触发即可；P2 复用同一 schedule）。新增：

1. 在 `Run spec-freshness audit` step 后插入第二 step `Run orphan-spec audit`：
   ```yaml
   - name: Run orphan-spec audit
     id: orphan
     run: |
       python3 tools/spec_freshness_audit.py --check orphan > /tmp/orphan.json
       cat /tmp/orphan.json
       echo "orphan_count=$(jq 'length' /tmp/orphan.json)" >> "$GITHUB_OUTPUT"
   ```
2. 新加 step `Open or update orphan-spec issues`（结构镜像现有 `Open or update issues` step）：
   - label：`governance:orphan-spec`（颜色另选，如 `d4c5f9`，描述 "Spec doc not referenced by active-plan-index.md"）
   - title 模板：`governance: orphan spec — <spec_file>`
   - body 模板（HEREDOC）：
     ```
     Spec file: `<spec_file>`
     Spec age on main: <N> days
     Reason: <reason>

     ## Action
     Per CLAUDE.md §12.4 R3 + v2 charter §3 module D: either
     (a) register this spec in docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md
         (§1 / §2 / §3 / §4 — whichever fits its lifecycle stage), or
     (b) add it to .github/governance/orphan-spec-allowlist.txt with a one-line justification, or
     (c) delete the spec if it is truly dead.

     Auto-generated by `.github/workflows/spec-freshness-monitor.yml`.
     Closing without action will cause this issue to reappear on the next weekly run.
     ```
   - dedup pattern 完全复用现有 stale-claim step 的写法：`gh issue list --state open --label "$label" --search "in:title \"${title}\"" --json number --jq '.[0].number'`
3. 新加 `Report no-op when no orphans found` step（与 stale-claim 平行）
4. 顺序：stale-claim step → orphan-spec step。两个 step 互不依赖，串行即可（更易读，且 5-min timeout 充裕）

### 3.4 dedup / idempotency

完全复用 stale-claim 套路：
- 一个 spec 一个 issue（title 含 spec 路径 → 天然唯一）
- 已 open 的同 title issue → `gh issue edit --body-file` 覆盖 body（保留 issue number、保留人工 comment）
- 已 close 的同 title issue → 不复活（下次扫到会**开新 issue**，因为 search 只查 `--state open`）。这是 acceptable trade-off：人工 close 后若问题再次出现，issue 重开是 feature 不是 bug

---

## §4 idempotency / dedup 行为细节

- **现有 stale-claim** 的 dedup 锚点：`gh issue list --state open --label "$label" --search "in:title \"${title}\"" --json number --jq '.[0].number'`。orphan-spec 用**完全相同**的 query 模板，只换 label。
- **关闭策略 — 决断**：spec 被注册到 active-plan-index 后下次 cron 跑 orphan 输出该 spec 不再出现 → workflow **不自动 `gh issue close`**。理由：
  1. 自动 close 需要在 workflow 内额外查询历史 issues 并 diff 当前 orphan set，复杂度上升不止 2x；
  2. 人工 close 是 R3 retrospective ritual 的一部分（开 issue → 人写 PR 注册 spec → 人 close issue 附 PR link）有审计价值；
  3. 误 close 风险（脚本路径解析 bug 等）远高于人工 close 成本。
- 注释明确写进 workflow YAML 该 step 的 leading comment 内。

---

## §5 测试策略

### 5.1 单测基础设施

仓库已有 `tools/tests/test_p5_sync_tools.py` / `test_p6_*.py` / `test_p7_*.py` / `test_p8_*.py` 4 个文件，约定 `python3 tools/tests/test_<file>.py` 可独立跑，也兼容 `pytest`。`tools/spec_freshness_audit.py` 自身**目前没有**对应单测（手工 inspection）。

**本 PR 范围决断**：**加最小 unit test 文件 `tools/tests/test_p2_spec_freshness_orphan.py`**（参照 `test_p5_*.py` 形态），仅覆盖 `audit_orphan_specs(...)` 与 `load_orphan_allowlist(...)`；**不**为既有 `find_claims / collect_known_mr_ids` 倒补测试（out of scope，避免范围蔓延）。覆盖用例：
- spec 在 plan-index 内出现 → 不报 orphan
- spec 不在 plan-index 内、不在 allowlist → 报 orphan
- spec 在 allowlist → 不报 orphan
- spec dir 缺失 → fail-closed（sys.exit）
- plan-index 文件缺失 → fail-closed
- 内联 markdown 链接 `[..](../specs/X.md)` 形态命中 basename → 不报 orphan

CI 接入：tools/tests 当前**不在** GitHub Actions 任何 workflow 内（grep `.github/workflows/*.yml` 无 hit）。本 PR **不**引入 tools/tests CI runner（out of scope）；新单测仍可手跑验证，未来若 P5+ 引入 tools/tests CI 一并打包。

### 5.2 manual smoke

执行步骤（在 PR 描述里粘 transcript）：
1. `python3 tools/spec_freshness_audit.py --check orphan` → 期望输出当前 baseline orphan 列表（处理后应 ≤ 5；若 §2 处理已收口则期望 = 0）
2. 临时把任一已注册 spec 从 active-plan-index.md 删掉（如删 `2026-05-26-pr-soft-review-via-claude-code-action.md` 行），重跑 → 期望多出 1 条 orphan，spec_file 字段命中该 spec
3. 恢复 active-plan-index.md，重跑 → 期望回到 baseline
4. 在 allowlist 加一行 `2026-05-07-system-level-mt-bdd-design.md`，重跑 → 期望 baseline 减 1
5. 撤回 allowlist 改动

### 5.3 workflow dry-run

不能在 PR 跑 cron，但 `workflow_dispatch` trigger 已存在，本 PR 合并前在自己的 fork 或 PR head ref 上手动 `gh workflow run spec-freshness-monitor.yml` 一次，verify issue 是否正确开（包括 title / body / label / dedup）。若不便在 fork 演示，至少在 PR 描述粘 step 4 的 jq 输出证明 JSON 结构正确。

---

## §6 风险 & Stop

### Risks

- **R-A**：orphan 判定太严，把合理 spec 也标 orphan → noise → 抑制信任。
  - 例：`2026-05-26-pr-soft-review-via-claude-code-action.md` 已 retired（dual AI review 撤除）但仍在 active-plan-index §2 内 — 当前**算 referenced** ✓，无误报。
  - **Mitigation 1**：算法明确"任意 section 出现即 referenced"（包括 §4 历史段），避免要求 spec 必须 active。
  - **Mitigation 2**：引入 `.github/governance/orphan-spec-allowlist.txt`（format mirror `multi-projection-types.txt`：#-注释 + 一行一项 spec basename 或 path）作为逃生口，承认"有些 spec 永远不会有 active plan"（如纯 retrospective review、pre-index 历史 spec）。
- **R-B**：脚本启动路径错（CI working directory vs repo root）→ `spec_dir` / `plan_index_path` 解析到错位置 → 假阳性大爆发。
  - **Mitigation**：脚本启动断言 `(REPO_ROOT / 'docs/superpowers/specs').is_dir()` 与 `plan_index_path.is_file()`，二者任一失败 `sys.exit(2)`（workflow 看到非零 exit 不会调 jq、不会调 `gh issue create`，整 step 失败可见而非静默错开 issue）。
- **R-C**：active-plan-index.md rename / 路径漂移 → 脚本默认值失效。
  - **Mitigation**：默认值显式注释为"P2 charter 锁定的索引路径"；rename 时此 PR 也跟着改 default arg 视为单 PR scope。
- **R-D**：v2 charter §3 D 的措辞 "spec 必须在 `active-plan-index.md §1` 或 §3 出现" 比本 plan 的 "任意 section 出现即 referenced" **更严**。本 plan 选更宽松规则，理由：实际样本中 §2（active 设计文档表）、§4（历史段）都是合法注册形态，charter 文本是简写。Charter 不需改动，本 plan 的算法描述即新真相层；executor 在 §7 步骤里把这一点解释贴进 PR description。

### Stop

- **当前 baseline orphan 数 = 6**，**超过 stop 阈值 5** → **本 PR 启动时必须先做 §2.3 列出的预处理**（注册 v2 charter + 4 个历史 spec 处理），把 baseline 降到 ≤ 5 后才能开 PR。预处理失败（如发现某 spec 无法决断归类）→ 停下汇报，可能需要先开一个 docs-only PR 整理 active-plan-index §4，再做本 P2。
- 若脚本扩展过程中发现 active-plan-index.md 格式与脚本预期严重不符（如表格被改成 yaml），停下来先调脚本格式，再开 PR。

---

## §7 执行步骤（for executing-plan subagent）

1. `git status` / `git log -1` 确认在 `claude/p2-spec-freshness-orphan` 分支、tree clean。
2. 读 `tools/spec_freshness_audit.py` 全文；读 `.github/workflows/spec-freshness-monitor.yml` 全文；读 `docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md` 全文。
3. **预处理 active-plan-index.md（先于代码改动）**：
   - 在 §2 active 设计文档表加一行：`docs/superpowers/specs/2026-05-28-code-governance-v2-charter.md | Active | v2 治理章程（六模块 + 元规则集），P1-P7 的真相层 | Replaced by future v3 charter`
   - 在 §1 active 计划表加一行注册**本 plan**：`docs/superpowers/plans/2026-05-28-p2-spec-freshness-orphan-spec-plan.md | Active scoped — 单 PR 改造 | v2 charter P2：orphan-spec 守卫扩展，落点模块 D + R3 | Expires on PR merge`
   - 对剩余 5 个 orphan spec（按 §2.3 列）逐个决断 register-or-allowlist：
     - `2026-05-07-system-level-mt-bdd-design.md` → §4 历史计划段加一行 "对应历史 design spec：…"
     - `2026-05-24-metbench-doc-runtime-alignment-design.md` → §3 阶段性修复段（已存对应 plan）行后补 "design spec：…"
     - `2026-05-25-mr-verification-retrospective-review.md` / `…-two-layer-review-policy.md` / `2026-05-25-v12-pwr-migration-map.md` → §4 历史段加 "对应历史 spec" 集中段
   - 跑 `python3 tools/spec_freshness_audit.py --check orphan`（**预跑无 orphan 逻辑前先把脚本加上**；这步在 step 4 之后做）
4. **创建 allowlist 文件**：`.github/governance/orphan-spec-allowlist.txt`（mirror `multi-projection-types.txt` header；初始内容仅注释 + 空列表；任何条目都要附 one-line justification）
5. **改 `tools/spec_freshness_audit.py`**：
   - 加 `load_orphan_allowlist(...)` / `audit_orphan_specs(...)` 顶层函数（按 §3.1 + §3.2）
   - `main()` 加 `--check` / `--plan-index` arg，加启动断言
   - 既有 stale-claim 行为完全不动，`--check stale` 输出与原默认输出位级一致
6. **改 `.github/workflows/spec-freshness-monitor.yml`**：按 §3.3 加 2 个 step + 1 个 no-op step；既有 stale-claim 3 step 完全不动；workflow comment 顶部更新到提及 v2 charter §3 D + P2 plan link
7. **加 `tools/tests/test_p2_spec_freshness_orphan.py`**：按 §5.1 用例（不引入 pytest 依赖，沿用 `test_p5_*.py` fallback main pattern）
8. 跑 §5.2 manual smoke step 1-5，把 transcript 贴进 commit message 或 PR draft
9. `git add` 列具体文件（**不**用 `git add -A`）；`git commit` with HEREDOC 形态 commit message；`git push -u origin claude/p2-spec-freshness-orphan`
10. **本 PR 不**改 `docs/status/current.md`（v2 charter §7.4 明确状态账本随每 P 合入后的 ledger refresh PR 更新）
11. **本 PR 不**开 PR（按上层 prompt：写 plan + 执行代码，开 PR 是另一阶段）

---

## §8 验收标准

- [ ] `tools/spec_freshness_audit.py` 新加 `audit_orphan_specs(...)` + `load_orphan_allowlist(...)` 函数，既有 stale-claim 行为零回归（`--check stale` 输出位级等价于改造前默认输出）
- [ ] `tools/spec_freshness_audit.py` 启动断言两条（spec_dir 存在、plan_index 文件存在，缺则 `sys.exit(2)`）已落地
- [ ] `.github/governance/orphan-spec-allowlist.txt` 文件存在；header 注释解释 format 与 R3 关系；条目（如有）每条附 one-line justification
- [ ] `.github/workflows/spec-freshness-monitor.yml` 新加 2 个 step（orphan audit + open/update issues）+ 1 个 no-op step；既有 stale-claim 3 step 内容 0 改动
- [ ] orphan-spec issue title / body / label 模板已落地（label = `governance:orphan-spec`，颜色 `d4c5f9`，title 含 spec path）
- [ ] `tools/tests/test_p2_spec_freshness_orphan.py` 跑通（手工 `python3 tools/tests/test_p2_spec_freshness_orphan.py`），≥ 6 个测试用例（§5.1 列）
- [ ] active-plan-index.md 已注册本 P2 plan（§1）+ v2 charter spec（§2）+ 5 个历史 orphan spec 妥善归类（§3 或 §4）
- [ ] `python3 tools/spec_freshness_audit.py --check orphan` 在 PR head ref 上输出 `[]`（baseline 已清零）
- [ ] PR description 含 §5.2 manual smoke step 4 的 transcript（证明 allowlist + 注册 + dedup 三条路径都工作）

---

## §9 引用

- v2 charter：`docs/superpowers/specs/2026-05-28-code-governance-v2-charter.md`（§3 D + §6 P2 + §4 v1→v2 spec-freshness 行）
- CLAUDE.md §12.2 模块 D 行：`CLAUDE.md`
- CLAUDE.md §12.4 R3
- 现有脚本：`tools/spec_freshness_audit.py`
- 现有 workflow：`.github/workflows/spec-freshness-monitor.yml`
- 既有 allowlist 模板：`.github/governance/multi-projection-types.txt` + `.github/governance/expected-catalog-counts.txt`
- 既有 tools/tests 风格：`tools/tests/test_p5_sync_tools.py`
- active-plan-index：`docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md`
- P1 plan（同 v2 charter 直系兄弟）：`docs/superpowers/plans/2026-05-28-p1-catalog-derived-counts-plan.md`
