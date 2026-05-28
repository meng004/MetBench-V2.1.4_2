# T1 收尾扩展 — 4 个非 MR 实体 CRUD (Windows VM) 计划

> **Date**: 2026-05-28
> **Status**: Draft — 待 PR-0 docs gate 合入后激活
> **Driver**: CLAUDE.md §2.2 T1 列举的 CRUD 实体「应用程序 / 方程 / MR / 基础算例 / 测试过程数据」中，**MR** 已经通过 `c7d9a6d` (SystemMtMrCatalogPage) + PR #214 收尾完成；剩余 4 个 System-MT 实体仍无专用 UI。本计划把这 4 个实体推到 Controlled。
> **Branch (cloud)**: `claude/t1-non-mr-crud-windows-vm`

---

## §1 目标 (Goal)

为以下 4 个 System-MT 实体提供 WPF UI 操作入口，每个实体的 CRUD 粒度按数据安全性逐项裁剪（见 §3）；与现有 `SystemMtMrCatalogPage` 同款 page↔VM 配对模式（CLAUDE.md §5）。

| # | 实体 | Truth source（已存在） | UI 缺口 |
|---|---|---|---|
| **a** | **SUT (System-level Application)** | `SUT/<name>/catalog.json` 的 `program` 段 + `ManifestMrCatalogProvider` 解析；on-disk Python runner / parser / adapter 脚本 | 无 System-MT 专用 SUT CRUD 页；现有 `ApplicationManagementPage` 是 v1 Method-MT 视角，997-line VM 已混进 v2 字段但 UI 不分 Kind |
| **b** | **Equation** | `SystemMtMetadataCatalog.Equations` 12 个 seed `EquationMetadata` + `ISystemMtMetadataRepository.UpsertEquationAsync` 已就位 | 无 Equation 列表 / 编辑页；equation 现在 hard-code 在源码 + 静态 seed |
| **c** | **Sample case** | `SUT/<sut>/sample/*.json` 文件（被 `MrBindingDefinition.SampleCaseRelativePath` 引用） | 无 sample case 浏览 / 编辑页；现在手编 JSON |
| **d** | **Test execution record** | `SystemMtResultRecord` in `SystemMT.Litedb` + `ExecutionEvidence`；`ISystemMtResultRepository.ListPagedAsync` / `ListPagedByMrNameAsync` 已就位 | 无历史执行查询 / 删除页；`SystemMtExecutionPage` 只显示**本次** run |

**100% 验收准则（Controlled 门槛）**：

1. 每个实体一个 cloud-only 后端 service / editor + Linux CI 单元测试覆盖（参照 `SystemMtManifestCatalogEditorTests` 的 fact 数量级，每实体 ≥ 8 facts）。
2. 每个实体一个 WPF page + VM + DI 注册 + nav menu 项（参照 `SystemMtMrCatalogPage` 三件套）。
3. Windows VM 全量套件 `dotnet test MetBench_SystemMT.Tests` 维持 `1467/0/12` 不退；新增的 backend facts 全绿。
4. Windows VM 上 4 个新页都能从 nav 进入、列出数据、完成各自允许的 CRUD 操作（截图或 RDP 验证）。
5. `docs/status/current.md` §3 新增 "T1 非 MR 实体 CRUD" 行标 Controlled，附 VM 验证 SHA。

**不在 scope**：
- 不动 `ApplicationManagementPage` / `DomainManagementPage` / `MRManagementPage` 任何 Method-MT 页（参照 CLAUDE.md §9 Cloud 不动 WPF 默认）。
- 不引入新 SUT、不改 catalog manifest schema 字段。
- 不做 T4 / T5 / T6 任何工作。
- 不引入 LiteDB 新 collection（execution 已用 `SystemMT.Litedb`，equation 已用 `MR.Litedb` metadata repo，SUT 与 sample case 全部 on-disk）。

---

## §2 PR 顺序（5 个 PR，单向依赖）

| PR | 标题 | 分支策略 | 验证位置 | 依赖 |
|---|---|---|---|---|
| **PR-0** | `docs(plan): gate T1 non-MR CRUD Windows VM work` | cloud, docs-only | Linux CI hard `test` | — |
| **PR-1** | `feat(t1): add SUT catalog CRUD (backend + WPF page)` | cloud + VM verify | Linux CI + Windows VM | PR-0 |
| **PR-2** | `feat(t1): add Equation catalog CRUD (backend + WPF page)` | cloud + VM verify | Linux CI + Windows VM | PR-0 |
| **PR-3** | `feat(t1): add SampleCase catalog CRUD (backend + WPF page)` | cloud + VM verify | Linux CI + Windows VM | PR-1（页内会复用 SUT 选择控件） |
| **PR-4** | `feat(t1): add ExecutionHistory R/D page (backend + WPF page)` | cloud + VM verify | Linux CI + Windows VM | PR-0 |
| **PR-5** | `docs(status): refresh ledger after T1 non-MR CRUD chain` | cloud, docs-only | Linux CI | PR-1..4 全部合入 |

PR-1 / PR-2 / PR-3 / PR-4 可在 PR-1 合入后并行起 PR；PR-3 等 PR-1 是因为 sample case 页要用 PR-1 的 SUT 列表数据源避免重复实现。

每个 feature PR 同时含：
- `MetBench_BLL.Core/SystemMT/<area>/` 新后端服务
- `MetBench_SystemMT.Tests/SystemMT/<area>/` 后端单元测试（Linux 跑）
- `MetBench_Client/Views/Pages/<Name>Page.xaml(.cs)` + `MetBench_Client/ViewModels/<Name>ViewModel.cs` + `App.xaml.cs` DI 注册 + `MainWindowViewModel` nav 项（Windows 跑）
- 自身 PR body 7 节 checklist（CLAUDE.md §12）

---

## §3 4 个 PR 各自的 CRUD 粒度 + 关键设计

### PR-1 · SUT CRUD（System-level Application）

**Truth source**：`SUT/<name>/catalog.json` 的 `sut_name` + `program` 段（program_name / equation / equation_key / program_type / runner_script_relative_path / input_parser_script_relative_path / output_parser_script_relative_path / input_adapter_script_relative_path / output_adapter_script_relative_path / python_executable_kind）。

**允许操作**：
- **R**（读）：列出 `SUT/*/catalog.json` 全部，按 sut_name 排序。
- **U**（改）：编辑 `program` 段的 9 个字段；不动 `mrs` 段（MR 编辑走 `SystemMtMrCatalogPage`）。
- **C**（建）：限定创建 `program` 段（提示用户必须手动放 Python 脚本到 `SUT/<new-sut>/` 目录；保存时 fail-closed 校验脚本文件是否存在）。
- **D**（删）：**禁止**；删 SUT 会孤立其下所有 MR + sample case + 历史执行记录，不通过 UI 暴露。删除走 git revert。

**Backend**：`MetBench_BLL.Core/SystemMT/Catalog/Editing/ISystemMtSutEditor` + `SystemMtSutEditor`（参照 `SystemMtManifestCatalogEditor` 的 List/Load/ValidateDraft/SaveDraft 4 方法表面）；`SystemMtSutProgramDraft` POCO。

**Frontend**：`SystemMtSutCatalogPage` + `SystemMtSutCatalogViewModel`；左 SUT 列表、右当前 SUT `program` 段表单 + Validate + Save + New 按钮。

---

### PR-2 · Equation CRUD

**Truth source**：`SystemMtMetadataCatalog.Equations` 12 个 seed `EquationMetadata`（reactor 5 锚定 + T3 7 扩展）作为只读 baseline；用户新增的 equation 落 LiteDB（已就位 `ISystemMtMetadataRepository`）。

**允许操作**：
- **R**：merged view — seed list (来源标 `Built-in`) ∪ LiteDB user-defined (来源标 `User`)，按 `EquationKey` 排序。
- **C**：仅添加 `User` equation 到 LiteDB；`EquationKey` 必须不与 seed 冲突（fail-closed）。
- **U**：仅可改 `User` equation；`Built-in` 行所有字段只读。
- **D**：仅可删 `User` equation；同时拒绝若任何 `MrMetadata.EquationKey` 仍引用该 key（已就位 `DeleteEquationAsync` 但需补 reference-guard）。

**Backend**：新 `MetBench_BLL.Core/SystemMT/Metadata/Editing/ISystemMtEquationEditor` + `SystemMtEquationEditor`（包 `ISystemMtMetadataRepository` 与 `SystemMtMetadataCatalog.Equations`，加 reference-guard 检查）。

**Frontend**：`SystemMtEquationCatalogPage` + VM；DataGrid 列出 merged view，行内 Edit/Delete 按 source 字段灰显或可点。

---

### PR-3 · Sample Case CRUD

**Truth source**：`SUT/<sut>/sample/*.json` on-disk。

**允许操作**：
- **R**：按 SUT（用 PR-1 的列表）+ sample 文件名列出；展示文件原始 JSON 内容。
- **C**：新建 `<sut>/sample/<filename>.json`；filename 校验 `^[a-z0-9][a-z0-9_-]*\.json$`；fail-closed JSON parse 校验。
- **U**：编辑 JSON 内容（raw textarea，参照 MR CRUD draft 模式 + Validate 按钮调 `JsonDocument.Parse`）。
- **D**：允许删除 sample 文件；但 fail-closed 若任何 manifest 的 `mrs[*].sample_case_relative_path` 仍引用（grep 所有 catalog.json）。

**Backend**：`MetBench_BLL.Core/SystemMT/Catalog/Editing/ISystemMtSampleCaseEditor`；4 方法 List/Load/Save/Delete + reference-guard。

**Frontend**：`SystemMtSampleCaseCatalogPage` + VM；上方 SUT ComboBox（数据源 = PR-1 sut list service）+ 下方 sample 列表 + 右侧 raw JSON 编辑区。

---

### PR-4 · Execution History R/D

**Truth source**：`SystemMT.Litedb` 中 `SystemMtResultRecord` 集合（已通过 `ISystemMtResultRepository.ListPagedAsync` / `ListPagedByMrNameAsync` 暴露）。

**允许操作**：
- **R**：分页列出全部执行（默认 50 条/页，按 RunAt desc）；可按 MrName 过滤；点击行展开 `ExecutionEvidence` 详细。
- **D**：单条删除 + multi-select batch 删除，**必须确认弹窗**；删执行记录同时删 `ExecutionEvidence`（同一 ExecutionId）。
- **C / U**：**禁止** — 执行记录是 pipeline 产出，不能手编；UI 不暴露 Create/Update 入口。

**Backend**：扩 `ISystemMtResultRepository` 加 `DeleteAsync(Guid id)` + `DeleteBatchAsync(IEnumerable<Guid> ids)`；同时扩 `IExecutionEvidenceRepository` 加 `DeleteByExecutionIdAsync(Guid id)`；新 `MetBench_BLL.Core/SystemMT/Persistence/Editing/IExecutionHistoryEditor` 包两者，保证删除事务性（两个 collection 同时删，任一失败回滚）。

**Frontend**：`SystemMtExecutionHistoryPage` + VM；DataGrid + 上方 MrName 过滤框 + 分页控件 + 行内 Delete + multi-select toolbar Delete + 行展开 evidence 详情。

---

## §4 Cloud-side vs Windows-side 工作切分

| 工作项 | Linux Cloud 可做 + 验 | Windows VM 必须验 |
|---|---|---|
| 4 个 backend service + 单元测试 | ✅ 在 Linux CI hard `test` 全绿 | — |
| 4 个 WPF page / VM / DI / nav 项 | ✅ 可编辑 XAML / .xaml.cs / .cs 源文件 | ❌ Linux dotnet SDK 不能编 WPF；必须在 VM `dotnet build MetBench_Client.csproj` 验 0 errors |
| WPF UI 交互（点击 / 列表 / 表单提交） | ❌ | ✅ 必须在 VM 截图或 RDP 验各 page CRUD 端到端 |
| Windows VM 全量测试套件 | ❌ | ✅ 每个 feature PR 合入前 + PR-5 ledger refresh 前 |

各 feature PR 的 Cloud-side 必须完成的事：
1. 后端类 + 后端测试 全绿（Linux CI）
2. WPF 文件**源码**写就（XAML + .cs），但不期望 Linux 能编（Linux dotnet 缺 WindowsDesktop targets，会 MSB4019 fail，预期之内）
3. PR body 7 节 + 「Windows」节明示「待 VM 验证」

各 feature PR 的 Windows VM 必须完成的事（在合入前）：
1. `dotnet build MetBench_Client.csproj` 0 errors
2. `dotnet test MetBench_SystemMT.Tests` 全绿（baseline 1467/0/12 不退）
3. `dotnet run --project MetBench_Client`，从 nav 进入新页，做一次 CRUD 操作，截图 / RDP 录屏存档
4. 把验证 SHA 写回 PR body「Windows」节

---

## §5 与 Linux CI governance grep 的关系

每个 feature PR 都会触发 `dotnet-test.yml` 的 `governance` job。预期所有 grep 检查均不 `::warning::`：
- **plan traceability**：每个 PR body 引用本计划路径
- **status truth**：feature PR 不动 `docs/status/current.md`（留给 PR-5），feature PR 的「Status」节明示「Pending — ledger refresh deferred to PR-5」
- **Windows classification**：feature PR 必须在「Windows」节列具体 VM 验证 SHA
- **docs-only baseline misclaim**：feature PR 非 docs-only
- **PR Gate Checklist 7 节**：必须齐全

---

## §6 6 个待确认默认（如不否决，按默认推进）

| # | 问题 | 默认 | 风险/理由 |
|---|---|---|---|
| **Q1** | SUT 删除是否暴露给 UI？ | 否（不开 Delete 按钮，仅靠 git revert 删 SUT） | SUT 删除孤立 MR + sample + execution 记录，是数据完整性风险高的破坏操作；保留 git 路径而非 UI 路径 |
| **Q2** | Equation seed list 与 user 行如何标识？ | merged view 中加 `Source` 列（值 `Built-in` / `User`），仅 `User` 行可编辑 / 删除 | 防止用户误改 seed equation 引发 catalog 一致性破坏 |
| **Q3** | Sample case 编辑用 raw JSON textarea 还是结构化表单？ | raw JSON textarea + Validate 按钮（同 MR CRUD draft） | 表单方案得为每个 SUT 推断 schema，复杂度高且 SUT 间 schema 不同；textarea + validate 是 MR CRUD 已验证的 pattern |
| **Q4** | Execution history 是否暴露 Update？ | 否（仅 R + D） | 执行记录是 pipeline 产出 ground truth，手编破坏审计追溯 |
| **Q5** | 各 feature PR 是否需要新增 `docs/superpowers/specs/` 设计文档？ | 否（本计划已含设计细节，每 PR 的 commit body 引用本计划路径足够） | 减少 docs-PR 噪声；本计划已是 specs 等级深度 |
| **Q6** | nav menu 4 个新项是否分组（如放进二级菜单"System MT Catalog"）？ | 否，与现有平级（参照 `System MT MR Catalog` 放在 `System MT` 之后） | `Wpf.Ui` `NavigationViewItem` 二级嵌套未在本项目使用过；平级降低 PR 风险，未来需要分组再开独立 UX PR |

---

## §7 风险

| ID | 风险 | 缓解 |
|---|---|---|
| **R1** | Windows VM 上 PR-4 `Microsoft.Web.WebView2` / LiteDB transactional delete 在 antivirus 下偶发 file-lock | 删除走 `LiteDatabase.Transaction()` 包裹；UI 层加重试 3 次 + 用户可见报错 |
| **R2** | PR-2 `EquationKey` 冲突检测漏判 case sensitivity | `OrdinalIgnoreCase` 比较；测试覆盖 `boltzmann` / `Boltzmann` / `BOLTZMANN` 三种输入 |
| **R3** | PR-3 reference-guard 需 grep 全部 `SUT/*/catalog.json` 的 `sample_case_relative_path`，IO 成本随 SUT 数量线性增长 | 16 SUT 当下尚未触及性能阈；缓存 5 秒 |
| **R4** | PR-1 SUT 创建时 Python 脚本不存在 → fail-closed 但用户体验差 | 表单加 "Browse" 按钮指向 `SUT/<sut>/` 目录，列已存在的 `*.py` |
| **R5** | 4 个 feature PR 并行起会触发 nav menu / `App.xaml.cs` 合并冲突 | PR-1 / PR-2 / PR-3 / PR-4 各自只追加自身 nav 行 + 自身 DI 行，且不动其他人的；冲突时按 PR 合入顺序逐个 rebase |

---

## §8 完成后状态（写入 `docs/status/current.md` §3 by PR-5）

> | T1 非 MR 实体 CRUD | Controlled — PR-1..4 全部合入 + VM 全量绿 | PR #{N1} (SUT CRUD)、PR #{N2} (Equation CRUD)、PR #{N3} (SampleCase CRUD)、PR #{N4} (ExecutionHistory R/D)。各 PR 含 backend `MetBench_BLL.Core/SystemMT/<area>/Editing/` + WPF page + VM 验证 SHA。CLAUDE.md §2.2 T1 列举的 5 个 CRUD 实体（应用程序 / 方程 / MR / 基础算例 / 测试过程数据）现已全部具备 UI 入口。|

同时 `docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md` 添加 §1 行标 Completed。

---

## §9 闭环验收（对照 CLAUDE.md §11.2）

- [ ] 所列事实已对当前分支核实：`SystemMtMetadataCatalog.Equations` 12 个 / `ISystemMtMetadataRepository` 已有 `DeleteEquationAsync` / `ISystemMtResultRepository` 缺 `DeleteAsync` （PR-4 待补）/ `SystemMtManifestCatalogEditor` 已是 4-方法 reference pattern
- [ ] `AGENTS.md` / 本 plan / `CLAUDE.md` 三者无内容复制：仅指针互引
- [ ] PR-5 合入后 plan 与 `AGENTS.md` Stage 8 完成记录同步
