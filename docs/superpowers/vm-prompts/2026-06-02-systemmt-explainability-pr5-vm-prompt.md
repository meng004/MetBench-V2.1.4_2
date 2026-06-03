# VM 提示词 — System MT Explainability PR-5: WPF display + final docs/status sync

> **使用方式**：在 Windows VM 中启动 Claude Code 会话后，粘贴本文件全部内容作为 prompt。VM agent 按 Step 1-8 执行。
> **目标分支**: `claude/systemmt-explainability-pr5-wpf-display`
> **基线**: latest `origin/main` after PR-4 report projection (`730723a`) and the docs sync commit that adds this prompt.
> **计划**: [`docs/superpowers/plans/2026-06-01-systemmt-explainability-pair-quality-plan.md`](../plans/2026-06-01-systemmt-explainability-pair-quality-plan.md) Task P6/P7.

---

## 项目背景与硬约束

你在 Windows VM 中，working dir 是 MetBench V2.1.4_2 的本地 clone。
项目：.NET 8 + WPF System-MT 平台。

PR-0 到 PR-4 已完成 cloud-side explanation/profile/evidence/report 工作：

| PR | Commit | Scope |
|---|---|---|
| PR-0 | `3129ecb` | scoped plan + active index registration |
| PR-1 | `59b37cc` | equation + SUT explanation profiles |
| PR-2 | `95f674d` | MR explanation profiles |
| PR-3 | `74a5292` | pair-quality evidence persisted beside typed verification |
| PR-4 | `730723a` | pair-quality projection in HTML and Markdown reports |

PR-5 的目标是把这些 explanation/profile/pair-quality surfaces 展示到 WPF client，并收集 Windows evidence；不是改运行时语义。

### 硬约束

- 只做 PR-5：WPF display + client localization + focused client tests + user/status docs sync。
- 不新增 SUT、不新增 MR、不改 typed semantic runtime、不改 launcher behavior。
- 不改 System MT pair-quality 计数公式；公式以计划文件 Metrics and Formulas 为准。
- 任何“已验证 / 已通过”必须配真实工具输出（dotnet output、截图文件、git SHA）。
- 截图必须真实来自 Windows VM，不允许伪造路径。
- 不使用 `--no-verify`，不跳过 hooks。

### 允许修改

- `MetBench_Client/ViewModels/SystemMtEquationCatalogViewModel.cs`
- `MetBench_Client/ViewModels/SystemMtSutCatalogViewModel.cs`
- `MetBench_Client/ViewModels/SystemMtMrCatalogViewModel.cs`
- `MetBench_Client/ViewModels/SystemMtExecutionHistoryViewModel.cs`
- `MetBench_Client/Views/Pages/SystemMtEquationCatalogPage.xaml`
- `MetBench_Client/Views/Pages/SystemMtSutCatalogPage.xaml`
- `MetBench_Client/Views/Pages/SystemMtMrCatalogPage.xaml`
- `MetBench_Client/Views/Pages/SystemMtExecutionHistoryPage.xaml`
- `MetBench_UI.Localization/Resources/Strings.resx`
- `MetBench_UI.Localization/Resources/Strings.zh-CN.resx`
- `MetBench_Client.Tests/`
- `MetBench_SystemMT.Tests --filter ClientI18n` only if localization resources require a SystemMT-side assertion
- `docs/usage/MetBench-T0-T5-操作指南.md`
- `docs/status/current.md`
- `docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md`
- `docs/superpowers/plans/2026-06-01-systemmt-explainability-pair-quality-plan.md`
- `docs/superpowers/specs/2026-06-02-systemmt-explainability-pr5-vm-verification/`

### 禁止修改

- `MetBench_BLL.Core/SystemMT/Pipeline/`
- `MetBench_BLL.Core/SystemMT/Catalog/Typed/`
- `MetBench_BLL.Core/SystemMT/Reporting/`
- `SUT/*/catalog.json`
- runtime MR inventory / expected catalog counts
- Method MT code

如果为了 WPF display 必须扩展 BLL editor/view DTO，请先证明现有 DTO 无法呈现已经落地的 explanation fields，并把扩展限制在 `Catalog/Editing` 或 `Metadata/Editing` 的 projection DTO；不要改 launcher/runtime semantics。

---

## Step 1 拉最新代码并建分支

```powershell
git fetch origin
git checkout main
git pull --ff-only
git log --oneline -5
git checkout -b claude/systemmt-explainability-pr5-wpf-display
```

报告：
- 当前 HEAD SHA
- `git status --short --branch`
- 是否包含 `730723a feat(systemmt): project pair quality in reports`

如果 `origin/main` 不包含 `730723a`，暂停并报告；不要从旧 main 开 PR-5。

---

## Step 2 先写/补测试

优先写 VM/client tests，避免只靠截图。

建议覆盖：

1. Equation catalog selected built-in row exposes explanation fields:
   - equation class
   - equation family
   - physical meaning
   - benchmark rationale
   - expected laws
   - empty values display localized unavailable text

2. SUT catalog selected row/draft exposes profile fields:
   - profile program type
   - solver method
   - runtime key
   - input contract
   - output contract
   - adapter
   - dependency risk

3. MR catalog selected draft exposes explanation profile fields:
   - meta-pattern rationale
   - transform semantics
   - observables
   - predicate/tolerance/applicability/failure meaning
   - empty values display localized unavailable text

4. Execution history evidence summary renders pair quality when `ExecutionEvidence.PairQuality` is non-empty:
   - planned/executed/valid/passed/failed/skipped/invalid counts
   - valid/all pass rates
   - skip and invalid-spec reason distributions
   - default-empty `PairQualitySummary` does not create noisy output

Run after writing tests:

```powershell
dotnet test MetBench_Client.Tests --filter ClientI18n
dotnet test MetBench_SystemMT.Tests --filter ClientI18n
```

If the first run fails because the feature is not implemented, keep the failure summary as red evidence, then implement.

---

## Step 3 实现 compact explanation cards

Use existing WPF page layout patterns:
- restrained bordered sections, no nested cards
- compact labels and wrapped text
- localized labels only
- stable dimensions where possible

### Equation page

Target files:
- `MetBench_Client/ViewModels/SystemMtEquationCatalogViewModel.cs`
- `MetBench_Client/Views/Pages/SystemMtEquationCatalogPage.xaml`

Display for selected equation:
- equation class / family
- primary variables
- physical meaning
- benchmark rationale
- expected laws

Important current-code caveat:
`SystemMtEquationCatalogViewModel.OnSelectedEquationChanged` currently fills built-in Draft with only `EquationKey`, `Name`, and `CanonicalForm`. Make built-in explanation fields visible too. Prefer loading a full draft/metadata via existing editor APIs if available. If not available, make the smallest projection-side DTO/editor extension needed, with tests.

### SUT page

Target files:
- `MetBench_Client/ViewModels/SystemMtSutCatalogViewModel.cs`
- `MetBench_Client/Views/Pages/SystemMtSutCatalogPage.xaml`

Display:
- profile program type
- solver method
- runtime key
- input/output contracts
- adapter
- dependency risk

Use `SystemMtSutProgramDraft` fields already added by PR-1:
`ProfileProgramType`, `SolverMethod`, `RuntimeKey`, `InputContract`, `OutputContract`, `Adapter`, `DependencyRisk`.

### MR page

Target files:
- `MetBench_Client/ViewModels/SystemMtMrCatalogViewModel.cs`
- `MetBench_Client/Views/Pages/SystemMtMrCatalogPage.xaml`

Display selected MR explanation/profile fields added by PR-2. If `SystemMtMrBindingDraft.FromBinding` already carries profile fields, bind directly. If not, add the smallest projection change required and pin it with tests.

---

## Step 4 实现 execution-history pair-quality indicators

Target files:
- `MetBench_Client/ViewModels/SystemMtExecutionHistoryViewModel.cs`
- `MetBench_Client/Views/Pages/SystemMtExecutionHistoryPage.xaml`
- localization resources

Existing path:
`SystemMtExecutionHistoryViewModel.LoadEvidenceAsync` loads `ExecutionEvidence`; `FormatEvidence` already renders typed verification text.

Add pair-quality display to evidence summary when `ev.PairQuality` is non-empty:
- localized header
- planned/executed/valid/pass/fail/skip/invalid counts
- `pass_rate_valid` and `pass_rate_all` with invariant numeric formatting
- skip reasons and invalid-spec reasons

Keep legacy/default-empty behavior quiet:
- if `PairQualitySummary` is null/default-empty, do not show the pair-quality section
- if evidence row is missing, keep existing no-evidence localized text

Optional UI improvement:
Add a compact pair-quality indicator in the detail panel above the text box if it can be done without disrupting the existing layout. The text summary is required; the extra visual badge is optional.

---

## Step 5 localization

Add English and Chinese resource keys for:
- explanation section headers
- field labels
- localized unavailable fallback
- pair-quality header and count/rate labels
- reason distribution labels

Files:
- `MetBench_UI.Localization/Resources/Strings.resx`
- `MetBench_UI.Localization/Resources/Strings.zh-CN.resx`

After editing resources, verify generated designer/build behavior through build/test; do not manually hand-edit generated resource designer unless the repo's existing workflow requires it.

---

## Step 6 Windows build and focused tests

Run:

```powershell
dotnet build MetBench.sln
dotnet test MetBench_Client.Tests --filter ClientI18n
dotnet test MetBench_SystemMT.Tests --filter ClientI18n
dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~SystemMtExecutionHistoryViewModel|FullyQualifiedName~SystemMtEquationCatalogViewModel|FullyQualifiedName~SystemMtSutCatalogViewModel|FullyQualifiedName~SystemMtMrCatalogViewModel"
```

Expected:
- build has 0 errors
- focused tests have 0 failures

If full `dotnet build MetBench.sln` reports warnings only, record warning count but do not call it a blocker.
If any command fails, paste the failing test/build output and fix only PR-5 scope.

---

## Step 7 VM UI verification and screenshots

Create:

```powershell
mkdir docs/superpowers/specs/2026-06-02-systemmt-explainability-pr5-vm-verification
dotnet run --project MetBench_Client
```

Capture real screenshots:

1. `01-equation-explanation-card.png`
   - System MT Equation Catalog page
   - a built-in equation selected
   - explanation card shows class/family/physical meaning/rationale/laws

2. `02-sut-profile-card.png`
   - System MT SUT Catalog page
   - an existing SUT selected
   - SUT profile shows runtime key, contracts, adapter, dependency risk

3. `03-mr-explanation-card.png`
   - System MT MR Catalog page
   - an MR selected
   - MR explanation/profile fields visible

4. `04-execution-history-pair-quality.png`
   - System MT Execution History page
   - selected execution with non-empty evidence
   - pair-quality counts/rates visible

5. `05-execution-history-no-evidence-or-empty-pair-quality.png`
   - selected row with missing evidence or default-empty pair quality
   - no noisy all-zero pair-quality section

6. `06-zh-cn-equation-or-history.png`
   - switch language to Chinese
   - one explanation or pair-quality surface localized

7. `07-en-us-equation-or-history.png`
   - switch language to English
   - same surface localized back to English

If the local DB has no non-empty pair-quality evidence row, create a minimal deterministic test fixture only if an existing seeding/test helper supports it. Otherwise document the blocker and provide screenshot evidence for no-evidence/default-empty behavior; do not fake a row by editing production DB files without explaining the exact script/command.

---

## Step 8 docs/status sync and commit

Update:
- `docs/usage/MetBench-T0-T5-操作指南.md`
  - document where users see equation/SUT/MR explanations and pair-quality evidence
- `docs/status/current.md`
  - mark System MT explainability and pair-quality reporting Controlled only if Step 6/7 evidence is complete
  - include exact commit, command outputs, screenshot path/count
- `docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md`
  - move the scoped plan to Completed only if PR-5 evidence is complete
- `docs/superpowers/plans/2026-06-01-systemmt-explainability-pair-quality-plan.md`
  - mark P6/P7 complete only if Windows evidence exists

Run:

```powershell
git diff --check
dotnet build MetBench.sln
dotnet test MetBench_Client.Tests --filter ClientI18n
dotnet test MetBench_SystemMT.Tests --filter ClientI18n
```

Commit and push:

```powershell
git status --short
git add MetBench_Client MetBench_Client.Tests MetBench_UI.Localization docs/usage docs/status/current.md docs/superpowers/plans docs/superpowers/specs/2026-06-02-systemmt-explainability-pr5-vm-verification
git -c commit.gpgsign=false commit -m "feat(client): surface System MT explanations and pair quality"
git push -u origin claude/systemmt-explainability-pr5-wpf-display
```

Open a PR against `main` with this body:

```markdown
## Summary
- Adds WPF explanation surfaces for System MT equation, SUT, and MR catalog pages.
- Adds pair-quality display for execution-history evidence while keeping missing/default-empty evidence quiet.
- Updates localization, user guide, status ledger, and active plan evidence for PR-5.

## Scope boundary
- Windows/WPF display + localization + docs/status only.
- No runtime MR semantics, SUT manifests, typed semantic runtime, launcher, or pair-counting formula changes.

## Windows evidence
- `dotnet build MetBench.sln`: <result>
- `dotnet test MetBench_Client.Tests --filter ClientI18n`: <result>
- `dotnet test MetBench_SystemMT.Tests --filter ClientI18n`: <result>
- Screenshots: `docs/superpowers/specs/2026-06-02-systemmt-explainability-pr5-vm-verification/` (<N> files)

## Screenshots
| File | Evidence |
|---|---|
| `01-equation-explanation-card.png` | equation explanation |
| `02-sut-profile-card.png` | SUT profile |
| `03-mr-explanation-card.png` | MR explanation |
| `04-execution-history-pair-quality.png` | pair-quality display |
| `05-execution-history-no-evidence-or-empty-pair-quality.png` | quiet empty path |
| `06-zh-cn-equation-or-history.png` | Chinese localization |
| `07-en-us-equation-or-history.png` | English localization |

## Follow-up
- None if all gates pass.
```

---

## 完成后回复 cloud/main thread

至少报告：
- PR URL
- branch name
- VM HEAD SHA
- exact build/test command results
- screenshot directory + file count
- whether docs/status row moved to Controlled
- any deviation from this prompt and why

If any gate fails, do not open/merge as complete. Report the failing command, exact error, and the smallest suspected file area.
