# System MT Explainability PR5 Strict Acceptance VM Prompt

> Use this prompt in a Windows VM with Claude Code. The goal is strict acceptance and evidence closure for PR5, not a cosmetic documentation update.

```text
你在 Windows VM 中执行 MetBench PR5 严格验收与收口。目标不是“补一句说明”，而是用真实证据判断并修正：本轮 System MT explainability + pair-quality 更新是否真正解决以下问题：
1. 方程、SUT、MR 解释过于简单；
2. MT 执行结果缺少 pair 数量、通过率指示。

Repository: meng004/MetBench-V2.1.4_2
Worktree: 使用 VM 中已有仓库；先确认路径、分支、远端状态。

重要规则：
- 先读取 AGENTS.md / CLAUDE.md / docs/status/current.md / active plan index，再执行。
- 如果环境要求 rtk 前缀，就所有 shell 命令使用 rtk；Windows VM 若无 rtk，使用 PowerShell 原生命令，并在报告中明确说明。
- 不得伪造生产 LiteDB 数据；不得为了截图手写插入 fake PairQuality 到 production DB。
- 可以通过真实 UI / launcher / pipeline 运行一个 System MT 场景来生成新的真实 execution evidence。
- 不得在缺少真实验收证据时把状态写成 Controlled / Completed。
- 如果某项无法完成，保留 blocker，并写清楚 exact command、失败原因、下一步，不要粉饰为完成。

第一阶段：同步和定位
1. fetch 最新远端。
2. 确认 origin/main 是否包含 PR #265 / merge commit a58a72c。
3. 创建验收收口分支：
   claude/systemmt-explainability-pr5-strict-acceptance
4. 记录：
   - 当前 HEAD
   - git status
   - PR #265 状态
   - docs/status/current.md 对 PR5 的描述
   - docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md 对 PR5 的描述
   - docs/superpowers/plans/2026-06-01-systemmt-explainability-pair-quality-plan.md 对 P6/P7 的完成描述

第二阶段：严格功能验收
必须验证以下 UI 功能，不满足则修复：

A. 方程解释
- 打开 WPF 客户端。
- 进入 System MT Equation Catalog。
- 选择 built-in equation，例如 bateman。
- 必须能看到完整“Equation explanation / 方程说明”卡片。
- 截图必须显示至少：
  - equation class
  - equation family
  - primary variables
  - physical meaning
  - benchmark rationale
  - expected laws

B. SUT 解释
- 进入 System MT SUT Catalog。
- 选择 heat_equation 或其他已有 SUT。
- 必须能看到真实“SUT profile / SUT 概况”卡片，而不是只看到上方编辑表单。
- 若 200% DPI / 窗口高度导致卡片无法捕获，先尝试：
  - 调整 VM 分辨率；
  - 调整 Windows scaling；
  - 最大化/缩放窗口；
  - UIA scroll；
  - 必要时做最小 UI 修复，让右侧详情区域可滚动并能截图。
- 截图必须实际显示 SUT profile 字段。

C. MR 解释
- 进入 System MT MR Catalog。
- 选择 heat_equation manifest 和 heat-equation-amplitude MR。
- 必须能看到真实“MR explanation / 蜕变关系说明”卡片。
- 截图必须显示至少：
  - meta-pattern rationale
  - transformation semantics
  - observables
  - predicate
  - tolerance
  - applicability
  - failure meaning

D. Pair-quality 指示
- 必须产生或定位一条真实、非空 PairQuality 的 execution evidence。
- 优先方式：通过 WPF System MT Execution 页面真实运行一个 pure-stdlib 场景，例如 heat_equation / heat-equation-amplitude。
- 然后进入 System MT Execution History，选择新记录。
- 截图必须显示 pair-quality 区块，至少包括：
  - planned pairs
  - executed pairs
  - valid pairs
  - passed pairs
  - failed pairs
  - skipped pairs
  - invalid spec pairs
  - pass rate valid
  - pass rate all
- 旧记录/default-empty PairQuality 必须保持 quiet，不显示误导性 pair-quality 区块。

第三阶段：测试门禁
运行并记录完整输出摘要：

1. dotnet build MetBench.sln
   Expected: 0 errors

2. dotnet test MetBench_Client.Tests --filter ClientI18n
   Expected: all pass

3. dotnet test MetBench_SystemMT.Tests --filter ClientI18n
   Expected: all pass

4. 若你修改了 UI 布局或 ViewModel，还要运行相关 focused tests：
   - SystemMtExplanationCardTests
   - SystemMtPairQualityEvidenceTests
   - SystemMtExplanationLocalizationTests

5. 运行 git diff --check。

第四阶段：文档对齐
根据真实结果修正文档。必须重点修：

1. docs/status/current.md
- 如果 PR5 已合并，不能再写 pending。
- 如果 pair-quality 非空截图已补齐，可把状态推进为 Controlled。
- 如果仍未补齐，必须保留 Not yet Controlled / blocker，不得写完成。

2. docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md
- 不能再写 origin/main 停在 eafbb70 或 PR5 pending。
- 若验收全部通过，把 explainability plan row 移到 Completed。
- 若仍有 blocker，保持 Active scoped plan，并写清楚剩余 blocker。

3. docs/superpowers/plans/2026-06-01-systemmt-explainability-pair-quality-plan.md
- P6/P7 的 Complete 口径必须和证据一致。
- 若真实非空 pair-quality 截图补齐，删除“screenshot 04 missing”的 blocker 表述。
- 若没有补齐，不得把 P7/status sync 写成完成。

4. docs/superpowers/specs/2026-06-02-systemmt-explainability-pr5-vm-verification/README.md
- 更新截图矩阵。
- 必须加入新的非空 pair-quality 截图文件。
- 若修复了 SUT/MR 卡片截图，也更新 02/03 的说明，不能继续说“卡片没截到”。

第五阶段：验收截图要求
最终截图目录仍使用：
docs/superpowers/specs/2026-06-02-systemmt-explainability-pr5-vm-verification/

必须至少包含：
01-equation-explanation-card.png
02-sut-profile-card.png
03-mr-explanation-card.png
04-execution-history-non-empty-pair-quality.png
05-execution-history-no-evidence-or-empty-pair-quality.png
06-zh-cn-equation-or-history.png
07-en-us-equation-or-history.png

严格要求：
- 02 必须真的显示 SUT profile 卡片。
- 03 必须真的显示 MR explanation 卡片。
- 04 必须真的显示非空 pair-quality 区块。
- 不能把“页面选中目标行但卡片在屏幕外”算作通过。

第六阶段：提交与 PR
如果全部验收通过并修正文档：
1. git status 确认只包含本次验收相关改动。
2. commit message:
   docs(systemmt): close PR5 strict VM acceptance evidence
   或如有 UI 修复：
   fix(client): make System MT explanation cards VM-capturable
3. push 分支。
4. 创建 PR，标题：
   docs(systemmt): close PR5 strict VM acceptance evidence
5. PR body 必须包含：
   - build/test 命令和结果
   - 截图矩阵
   - 是否补齐 04 非空 pair-quality
   - 是否修正 docs/status/current.md 和 active index
   - remaining blocker：若无，写 None；若有，不能写 Controlled

最终报告格式：
- Summary
- Findings fixed
- Evidence
- Tests
- Screenshots
- Remaining blockers
- Commit / PR URL
```
