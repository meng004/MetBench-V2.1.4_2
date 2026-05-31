# System-MT Execution & Catalog Pages — Full Localization Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or a Workflow to implement this plan. Steps use checkbox (`- [ ]`) syntax. Pages are independent files → the per-page XAML wiring tasks (Tasks 2–6) run in PARALLEL after Task 1.

**Goal:** Fully localize (zh-CN / en-US) all user-facing static chrome on the System-MT Execution page and the four Catalog pages (MR / SUT / Equation / Sample Case), building on the existing `MetBench_UI.Localization` infrastructure.

**Architecture:** Add the remaining page strings as `.resx` keys (en + zh), then bind each hardcoded XAML literal to `{Binding ViewModel.Localization[Key]}`. DataGrid column headers use the existing `BindingProxy` (Freezable) pattern because `DataGridColumn` is not in the visual tree. All five VMs already expose `public LocalizedTextProvider Localization {get;}` — no VM/DI changes needed. Dynamic data, scientific identifiers, and metric values are NOT localized.

**Tech Stack:** .NET 8 WPF, Wpf.Ui, `.resx` + `ResourceManager`, `MetBench_UI.Localization`, `BindingProxy`, xUnit, UIA smokeshot.

---

## Scope

**Pages (5):**
1. `MetBench_Client/Views/Pages/SystemMtExecutionPage.xaml` (VM `SystemMtExecutionViewModel`, BindingProxy already present)
2. `MetBench_Client/Views/Pages/SystemMtMrCatalogPage.xaml` (VM `SystemMtMrCatalogViewModel`)
3. `MetBench_Client/Views/Pages/SystemMtSutCatalogPage.xaml` (VM `SystemMtSutCatalogViewModel`)
4. `MetBench_Client/Views/Pages/SystemMtEquationCatalogPage.xaml` (VM `SystemMtEquationCatalogViewModel`)
5. `MetBench_Client/Views/Pages/SystemMtSampleCaseCatalogPage.xaml` (VM `SystemMtSampleCaseCatalogViewModel`)

**Out of scope:** bound `{Binding ViewModel.X}` dynamic data, MR/SUT/equation ids, assertion-type codes, Python-kind strings, metric names/values, persisted JSON content, `Title=` page-internal name, `AutomationId`, runtime C# status-message interpolations.

**Reuse existing keys (do NOT create duplicates):** `Legacy_Description` ("Description"), `Legacy_Save` ("Save"), `Legacy_Delete` ("Delete"), `Legacy_Name` ("Name"), `SystemMt_Run`, `SystemMt_Source`, `SystemMt_FollowUp`.

---

## File Structure

- Modify `MetBench_UI.Localization/Resources/Strings.resx` + `Strings.zh-CN.resx` — add new keys (Task 1).
- Modify each of the 5 page `.xaml` — convert literals to bindings; add `BindingProxy` resource on the 4 catalog pages (Tasks 2–6).
- Create `MetBench_SystemMT.Tests/ClientI18n/SystemMtPageFullI18nResourceTests.cs` — resource-key existence test (Task 1).
- Update UIA evidence under `docs/superpowers/specs/2026-05-30-client-i18n-vm-evidence/` (Task 7).

`BindingProxy` already exists: `MetBench_Client/Helpers/BindingProxy.cs` (namespace `MetBench_Client.Helpers`). The catalog pages add, inside `<Page.Resources>`:
```xml
<helpers:BindingProxy x:Key="LocalizationProxy" Data="{Binding}" />
```
with `xmlns:helpers="clr-namespace:MetBench_Client.Helpers"`, and bind DataGrid headers as
`Header="{Binding Data.ViewModel.Localization[Key], Source={StaticResource LocalizationProxy}}"`.
Non-grid chrome binds normally: `Text|Content|PlaceholderText="{Binding ViewModel.Localization[Key]}"`.

---

## New Resource Keys (add ALL to BOTH resx in Task 1)

### Execution page (`SystemMt_*`)
| Key | en-US | zh-CN |
|---|---|---|
| SystemMt_PageTitle | System-Level Metamorphic Testing | 系统级蜕变测试 |
| SystemMt_PageSubtitle | Pick a scenario, optionally override the transformation parameter, run, and review persisted results. | 选择一个场景，可选地覆盖变换参数，运行并查看持久化结果。 |
| SystemMt_Scenario | Scenario | 场景 |
| SystemMt_FactorParameter | Factor parameter | 变换因子 |
| SystemMt_FactorParameterPlaceholder | e.g. 2 | 例如 2 |
| SystemMt_RefreshRecent | Refresh recent | 刷新最近记录 |
| SystemMt_LastResult | Last result | 最近结果 |
| SystemMt_Status | Status | 状态 |
| SystemMt_RunAt | Run At | 运行时间 |
| SystemMt_Assertion | Assertion | 断言 |
| SystemMt_Value | Value | 值 |
| SystemMt_Passed | Passed | 是否通过 |
| SystemMt_ExportHtmlReport | Export HTML report | 导出 HTML 报告 |

### Cross-page shared catalog keys (define once)
| Key | en-US | zh-CN |
|---|---|---|
| Catalog_Reload | Reload | 重新加载 |
| Catalog_Validate | Validate | 验证 |
| Catalog_EquationKey | Equation key | 方程键 |

### MR Catalog (`Catalog_*`)
| Key | en-US | zh-CN |
|---|---|---|
| Catalog_MrCatalogTitle | System MT MR Catalog | 系统级 MR 目录 |
| Catalog_MrCatalogSubtitle | Edit manifest-backed System MT MR bindings. Validation must pass before save. | 编辑清单驱动的系统级 MR 绑定，保存前必须通过验证。 |
| Catalog_Manifest | Manifest | 清单 |
| Catalog_NewMrDraft | New MR draft | 新建 MR 草稿 |
| Catalog_MrId | MR ID | MR ID |
| Catalog_Assertion | Assertion | 断言类型 |
| Catalog_Value | Value | 值名称 |
| Catalog_DisplayName | Display name | 显示名称 |
| Catalog_Transformation | Transformation | 变换 |
| Catalog_AssertionName | Assertion name | 断言名称 |
| Catalog_ValueName | Value name | 值名称 |
| Catalog_MetaPattern | Meta pattern | 元模式 |
| Catalog_SampleCase | Sample case | 样例文件 |
| Catalog_WorkRoot | Work root | 工作根目录 |
| Catalog_TimeoutSeconds | Timeout seconds | 超时秒数 |
| Catalog_Factor | Factor | 变换因子 |
| Catalog_TransformStep | Transform step | 变换步骤 |
| Catalog_TargetFieldPath | Target field path | 目标字段路径 |

### SUT Catalog
| Key | en-US | zh-CN |
|---|---|---|
| Catalog_SutCatalogTitle | System MT SUT Catalog | 系统级 SUT 目录 |
| Catalog_SutCatalogSubtitle | Edit the program section of SUT/<sut>/catalog.json. MR bindings are edited on the MR Catalog page. | 编辑 SUT/<sut>/catalog.json 的程序段。MR 绑定在 MR 目录页编辑。 |
| Catalog_NewSutDraft | New SUT draft | 新建 SUT 草稿 |
| Catalog_SutId | SUT id | SUT 标识 |
| Catalog_Equation | Equation | 方程 |
| Catalog_Program | Program | 程序 |
| Catalog_SutName | SUT name | SUT 名称 |
| Catalog_ProgramName | Program name | 程序名称 |
| Catalog_ProgramType | Program type | 程序类型 |
| Catalog_PythonRuntimeKind | Python runtime kind | Python 运行环境类型 |
| Catalog_RunnerScript | Runner script | 运行脚本 |
| Catalog_InputParserScript | Input parser script | 输入解析脚本 |
| Catalog_OutputParserScript | Output parser script | 输出解析脚本 |
| Catalog_InputAdapterScript | Input adapter script | 输入适配脚本 |
| Catalog_OutputAdapterScript | Output adapter script | 输出适配脚本 |

### Equation Catalog (Name reuses `Legacy_Name`)
| Key | en-US | zh-CN |
|---|---|---|
| Catalog_EquationCatalogTitle | System MT Equation Catalog | 系统级方程目录 |
| Catalog_EquationCatalogSubtitle | Built-in seed equations are read-only. User-defined equations can be created, edited, and deleted (unless referenced by an MR). | 内置种子方程为只读。用户自定义方程可创建、编辑和删除（被 MR 引用的除外）。 |
| Catalog_NewEquation | New equation | 新建方程 |
| Catalog_Key | Key | 键 |
| Catalog_CanonicalForm | Canonical form | 规范形式 |
| Catalog_Source | Source | 来源 |
| Catalog_SymbolSystem | Symbol system | 符号系统 |

### Sample Case Catalog (Save reuses `Legacy_Save`, Delete reuses `Legacy_Delete`)
| Key | en-US | zh-CN |
|---|---|---|
| Catalog_SampleCatalogTitle | System MT Sample Case Catalog | 系统级样例目录 |
| Catalog_SampleCatalogSubtitle | Browse and edit sample JSON files under SUT/<sut>/sample/. Files referenced by an MR binding cannot be deleted. | 浏览并编辑 SUT/<sut>/sample/ 下的样例 JSON 文件。被 MR 绑定引用的文件不可删除。 |
| Catalog_SutLabel | SUT | SUT |
| Catalog_NewSample | New sample | 新建样例 |
| Catalog_FileName | File name | 文件名 |

> `Catalog_Source` ("来源", equation provenance) is DISTINCT from `SystemMt_Source` ("源输入"). Do not conflate.

---

## Task 1: Resource keys + RED/GREEN resource test (SEQUENTIAL — owns the shared resx)

**Files:** `Strings.resx`, `Strings.zh-CN.resx`, `MetBench_SystemMT.Tests/ClientI18n/SystemMtPageFullI18nResourceTests.cs`.

- [ ] **Step 1: Write failing resource test** — assert every NEW key above exists non-blank in en-US AND zh-CN via `ResourceManager` "MetBench_UI.Localization.Resources.Strings". (List all the new keys from the tables in the `keys` array.)
- [ ] **Step 2: RED** — `dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~ClientI18n.SystemMtPageFullI18nResourceTests"` → fails (keys missing).
- [ ] **Step 3: Add all new keys** to BOTH resx with the exact en/zh values from the tables. No trailing whitespace on added lines.
- [ ] **Step 4: GREEN** — same filter → 1 passed. Also run `--filter ClientI18n` → all prior pass (parity tests still green).
- [ ] **Step 5: Commit** — `feat(client): add system-mt page localization keys`.

**Acceptance:** all new keys present with en/zh parity; resource test green; no existing test broken.

## Tasks 2–6: Per-page XAML wiring (PARALLEL — each edits ONE .xaml only)

For each page: convert the page's hardcoded chrome literals (titles, subtitles, section/field labels, button captions, placeholders, DataGrid headers) to bindings using the key map from the survey/tables. Non-grid → `{Binding ViewModel.Localization[Key]}`. DataGrid headers → add `xmlns:helpers` + `<Page.Resources><helpers:BindingProxy x:Key="LocalizationProxy" Data="{Binding}"/></Page.Resources>` (catalog pages; exec page already has it) and bind `Header="{Binding Data.ViewModel.Localization[Key], Source={StaticResource LocalizationProxy}}"`. Do NOT touch `{Binding ViewModel.<data>}`, ids, metric values, `Title=`, `AutomationId`. No resx edits (done in Task 1). No VM edits (Localization already exposed).

- [ ] **Task 2** `SystemMtExecutionPage.xaml`: PageTitle, PageSubtitle, Scenario(label), FactorParameter, FactorParameterPlaceholder, RefreshRecent, Description(=Legacy_Description), LastResult, Status; DataGrid headers RunAt/Scenario/Assertion/Value/Passed (proxy present); ExportHtmlReport. (Run/Source/FollowUp already done.)
- [ ] **Task 3** `SystemMtMrCatalogPage.xaml`: add proxy; title, subtitle, Manifest, Reload, NewMrDraft, form labels (MrId, DisplayName, Description=Legacy_Description, Transformation, Assertion, AssertionName, ValueName, EquationKey, MetaPattern, SampleCase, WorkRoot, TimeoutSeconds, Factor, TransformStep, TargetFieldPath), Validate, Save(=Legacy_Save); DataGrid headers MrId/Assertion/Value.
- [ ] **Task 4** `SystemMtSutCatalogPage.xaml`: add proxy; title, subtitle, Reload, NewSutDraft, form labels (SutName, ProgramName, Equation, EquationKey, ProgramType, PythonRuntimeKind, RunnerScript, InputParserScript, OutputParserScript, InputAdapterScript, OutputAdapterScript), Validate, Save(=Legacy_Save); DataGrid headers SutId/Equation/Program.
- [ ] **Task 5** `SystemMtEquationCatalogPage.xaml`: add proxy; title, subtitle, Reload, NewEquation, form labels (EquationKey, Name=Legacy_Name, CanonicalForm, SymbolSystem), Validate, Save(=Legacy_Save), Delete(=Legacy_Delete); DataGrid headers Key/Name(=Legacy_Name)/CanonicalForm/Source(=Catalog_Source).
- [ ] **Task 6** `SystemMtSampleCaseCatalogPage.xaml`: add proxy; title, subtitle, SutLabel, Reload, NewSample, FileName(form), Validate, Save(=Legacy_Save), Delete(=Legacy_Delete); DataGrid header FileName.

Each task: edit XAML, then commit `feat(client): localize <page>`. (In the parallel workflow, all five edits land before the single build in Task 7; a per-page commit is optional — the workflow may stage all and let Task 7 build+commit.)

**Acceptance per page:** every targeted literal becomes a localized binding; no data binding altered; XAML well-formed; key names match Task 1.

## Task 7: Build, test, UIA verify, evidence (SEQUENTIAL)

**Files:** evidence dir.

- [ ] **Step 1:** stop running client; `dotnet build MetBench.sln` → 0 errors. Fix any binding/markup error introduced (identify offending page).
- [ ] **Step 2:** `dotnet test MetBench_SystemMT.Tests --filter ClientI18n` (all green) and `dotnet test MetBench_Client.Tests --filter ClientI18n` (all green).
- [ ] **Step 3:** launch client; via smokeshot capture each of the 5 pages in zh AND en (10 screenshots, e.g. `10-exec-page-zh.png` … `19-samplecase-en.png`) into the evidence dir. Confirm titles/labels/headers render localized in both languages (no `??Key??`, no blank headers).
- [ ] **Step 4:** append a `full-page-i18n` event to `vm-status.jsonl`; note in `vm-summary.md`.
- [ ] **Step 5:** Commit — `test(client): system-mt full-page i18n evidence`.

**Acceptance:** sln builds; all ClientI18n tests green; 10 screenshots show fully bilingual System-MT exec + 4 catalog pages; no `??Key??` / blank headers.

---

## Self-Review
- **Spec coverage:** all 5 pages have a wiring task; every survey string maps to a key (new or reused); Task 1 adds keys, Tasks 2–6 wire, Task 7 verifies.
- **Placeholder scan:** none — every key has explicit en/zh; every page lists its key→control set.
- **Type consistency:** key names identical across Task 1 tables and Tasks 2–6 references; reused keys (`Legacy_*`, `SystemMt_Run/Source/FollowUp`) are not redefined; `Catalog_Source` ≠ `SystemMt_Source` flagged.
- **Parallelism safety:** shared resx mutated only in Task 1 (sequential); Tasks 2–6 edit disjoint `.xaml` files (safe parallel); build/commit centralized in Task 7.
