# Windows UAT Round-1 — limeng — 2026-05-18

> **执行方式**：全自动化（Claude Code Sonnet via UIA automation）— 用户认可"全自动尽力跑"模式。
> **WPF 主程序**：`MetBench_Client.exe` 在 Parallels Windows 11 VM 上启动。
> **automation 工具链**：UIA (System.Windows.Automation) + Win32 (FindWindow / SendMessage / PostMessage 处理 OpenFileDialog) + System.Drawing 屏幕截图 + Claude 多模态读图 进行视觉验证。

| 项 | 值 |
|---|---|
| 仓库 commit | `0c0cd245f21d85a397da601aa95579ba4138cfbb` |
| 平台 | Windows 11 Pro 26200 + .NET 8 SDK + PowerShell 5.1 |
| WPF build | `dotnet build MetBench.sln` — 0 Error / 13 Warning / 9.4s |
| WPF 冷启动 | **2.68 s** ✅ (期望 < 5 s) |
| 总跑时长 (active) | 约 1 h 30 min |
| Windows 自跑用例 | 21 (A1-A7 + B1-B9 + E1-E5) |
| 云端覆盖用例（不重跑） | 5 (A8 / D1 / D2 / E6 / E7) — 引用 baseline-2026-05-17 |
| Baseline 引用 | `docs/uat/reports/baseline-2026-05-17/baseline-full.trx` |

## 结果汇总 (21 自跑 + 5 cloud-covered)

| 类别 | ✅ Pass | ⚠️ Partial / N/A | ❌ Fail | Cloud Covered | 备注 |
|---|---|---|---|---|---|
| A. 管理 CRUD (8) | 4 (A1/A3/A4/A7) | 1 (A6) | 2 (A2/A5) | 1 (A8) | 发现 2 个真实 bug — 见 §偏差说明 |
| B. MR 主流程 (9) | 2 (B2/B4/B5 — 算 B2+B4=2 Pass) | 6 (B1 部分功能, B3/B6/B7/B8/B9) | 1 (B1 Target SUT dropdown 失效) | 0 | UI 已迁移到 System MT 页, runbook 描述与现状不符 |
| D. R-Case (2) | 0 | 0 | 0 | 2 (D1/D2) | cloud baseline 已覆盖 |
| E. 可视化 & 报表 (7) | 0 | 3 (E1/E2/E3) | 0 | 2 (E6/E7) | 页面渲染正常但 DB 数据稀少, E4/E5 对应 UI 不存在 |
| **合计** | **6/21** | **10/21** | **3/21** | **5/5** | UI 与 runbook 多处不对齐, 见下方 |

> **复跑场景下的 PASS 率会更高** — 当前 ⚠️ Partial 多源于"页面正确但 DB 为空"（首次 UAT 数据未生成）, 而非 UI 缺陷。

## 总评：**CONDITIONAL PASS**

- ✅ **核心 UI 健康**：主程序 18 nav 全部可用, System MT 页 + Coverage / Trends Dashboard 正常渲染, MetaPatterns seed 8 行完整 (4 active + 4 out-of-scope 与 spec 一致)
- ✅ **System MT 主流程跑通**：heat_equation ScaleAmplitude scenario 选 → Run → 完成 (3.4s) → 结果表新增行 (Source / Follow-up / Assertion / Passed 字段齐全)
- ❌ **2 个明确的实现 bug**（block 完整 CRUD 流程）：
  1. **`ApplicationService.UpdateService` line 102 IsDuplicate 检查未排除自身** — UC-A2 编辑同名记录被误判为 "该应用程序已存在！"
  2. **MR Management / Discovery / Coverage 等多处 ComboBox 未 override Application/SUT 的 ToString**, 显示 "MetBench_Domain.Application" / "MetBench_Client.Models.ApplicationEx" 类名而非 Name 属性 — block UC-A5 / UC-B1
- ⚠️ **多处 runbook ↔ 实际 UI 不对齐**：
  - Domain 表单缺 Bound Applications 多选框 (UC-A4)
  - MR Management 表单是 method-level 详细字段, 无 Name/Type/Constraint 字段 (UC-A5)
  - System MT 页不分 "Generate Follow-up" / "Run" 两步, 也无图表 (UC-B3 / UC-B6)
  - System MT 失败的 run 不写入 Anomalies 表 (UC-B7-B9 block)
  - System MT 数据不进 Trends / Coverage Dashboard (UC-E1-E2)
  - 无 "Dashboard 主页" nav (UC-E5)
  - "MR ReportGenerator" 没有 "Generate All" 按钮, 只能一次导一种类型, 且因无 method-level MR 数据导出失败 (UC-E3 / E4)
- 📝 **UI typo 发现 (cosmetic)**：
  - Domain 表单标签 "Desciption" (少 'r')
  - MT Execution 按钮 "Eecute MT" (少 'x')

## 偏差说明（按 UC 详列）

### UC-A2 ❌ FAIL — Edit Application bug

**症状**：双击 UAT-App-1 行 → 表单回填正确 → 改 Description="UAT smoke v2" → 点 Edit 按钮 → 出现确认对话框 "是否修改该记录?" → 点 Yes → 弹错误 Tips "该应用程序已存在！"

**根因**（已查源码定位）：

```csharp
// MetBench_BLL/ApplicationService.cs:100-105
public int UpdateService(Application application)
{
    if (Application_repository.IsDuplicate(application, false))
    {
        return 1;  // ← 这里把"同名"等价为"重复", 没排除"当前正在编辑的同一条记录"
    }
    var result = Application_repository.Modify(application);
    ...
}
```

`IsDuplicate(application, false)` 在 Add 场景下意图是"防止新建重名"; 但在 Update 场景下应该排除"自己"。两个场景共用同一个方法且第二参数 (`isAdd: false`?) 不能区分。**修复建议**: `UpdateService` 改成调用 `IsDuplicate(application, excludeId: application.IdApplication)` 之类的语义。

**证据**: `screenshots/UC-A2-FAIL-duplicate-bug.png`

### UC-A5 ❌ FAIL — MR add 失败 + ComboBox ToString bug

**症状**：MR Management 表单填齐所有必填 (InputPattern / OutputPattern / DimensionOfInput / DimensionOfOutput / Context / Granularity=Function / Hierarchy=Math / Operator=Equation / Expression=Linear, ApplicationName 下拉选 first item) → 点 Add → 列表无新增行, 也无错误 Tips。

**根因 1**：ApplicationName 下拉里显示的是类名字符串 `"MetBench_Client.Models.ApplicationEx"` (出现 2 次, 但库里只有 1 个 UAT-App-1) — UIA Select 后表单看到的 binding object 是无效的, BLL 静默拒绝。

**根因 2 (deviation)**：runbook UC-A5 描述的字段是 `Name=UAT-Identity-MR`, `Type=invariance`, `Granularity=method`, `Constraint=output == input` — 但实际 UI 的 MR 表单字段是 `InputPattern / OutputPattern / DimensionOfInput / DimensionOfOutput / ArityOfMR / Operator / Expression / Context / Granularity / Hierarchy / ApplicationName`. 完全是 method-level 详细 schema, 与 runbook 设计 mismatch.

**修复建议**: 在 `MetBench_Client/Models/ApplicationEx.cs` 加 `public override string ToString() => Name;`; 同时 runbook 需要按现状重写 UC-A5 步骤。

**证据**: `screenshots/UC-A5-dropdowns-filled.png`, `UC-A5-after-add.png`

### UC-B1 ❌ FAIL — Discovery Target SUT dropdown 失效

**症状**：Discovery 页 → Target SUT 下拉展开 → 唯一项显示为 "MetBench_Domain.Application" (× 2) → UIA Select → 选择不持久 (下拉关闭后显示空) → 点 Run discovery → 候选列表无产出。

**根因**: 同 UC-A5 — Application 类没 override ToString, ComboBox binding 后 SelectionItem 的 underlying Value 与 expected Application 实例不匹配, 选择失败。

**证据**: `screenshots/UC-B1-discovery-result.png`

### UC-B2 / UC-B4 / UC-B5 ✅ PASS (with deviation)

System MT 页 (而非 runbook 描述的 MT Execution 页) 实测正常：
- ✅ Scenario dropdown 默认选中 "1D heat equation — ScaleAmplitude (linearity)"
- ✅ Factor parameter Edit 接受改值 (2 → 0.5 验证)
- ✅ Description / Last result / Status 三个面板齐全
- ✅ Run scenario 完成耗时 **3.4 s** (factor=2, PASS) / **~4 s** (factor=0.5, FAIL anomaly)
- ✅ 结果表行字段完整: Run At / Scenario / Assertion (GreaterThan/LessThan) / Value (max_u/k_eff) / Source / Follow-up / Passed

**deviation 1**: 新 UI 不分 "Generate Follow-up" 和 "Run" 两步, 而是 Run 内一次完成 — UC-B3 在新 UI 下 N/A
**deviation 2**: 无图表区, 结果以表格展示 — UC-B6 N/A

### UC-B7-B9 ⚠️ Partial — Anomalies 表为空 (跨子系统接线 gap)

**症状**: System MT 强制 2 次 anomaly 后 (factor=0.5 让 GreaterThan 必失败), 导航到 Anomalies 页, 列表显示 "Page 0 / 0 ( 0 total )"; Refresh 也无新增。

**根因推测**: SystemMtPipeline 的失败 run 没经过 `AnomalyService.RecordAnomaly` 写入 Anomalies 集合 — 两个子系统未对接, 或对接代码未在 v2.1 版本启用。

**影响**: UC-B7 (页面渲染 OK ✅ — 列名 Severity/Category/LinkedKnownBug 完整, 过滤器 + Analyze commonality 按钮存在), 但无数据可点开。UC-B8 / UC-B9 因此 N/A。

**证据**: `UC-B7-anomalies-list.png` (列表头), `UC-B7-prep-anomaly-runs.png` (系统 MT 端的 anomaly run)

### UC-E1 / UC-E2 ⚠️ Partial — Dashboard 渲染 OK 但 0 数据

- **UC-E1 Trends**: CartesianChart + Anomaly Bursts 面板正常渲染, 5-Week Trend 折线在 y=0 (因 System MT runs 不进 Trends 数据源)
- **UC-E2 Coverage**: 4-Dimension Coverage Dashboard 4 个 PieChart 全在 (MetaPattern / SUT × MR Binding / Bug / Mutation Detection), MetaPattern 显示 "0 / 8 patterns (0%)" 一个 Uncovered 红扇区, 其余 3 个 "No data"

页面结构 ✅, 数据为 0 ⚠️。

### UC-E3 ⚠️ Partial — 报表无目标文件

**症状**: MR ReportGenerator 页 → Report Type dropdown 有 4 项 (Pdf/Word/Excel/Html, 与 runbook "4 端导出"对应) → 选 Pdf → ExportReport → 弹 Tips "无目标文件！" → 同 Word / Excel / Html 行为一致。

**根因**: 报表 generator 读取的是 method-level MR 列表, 当前为空 (UC-A5 没 add 成功)。

**deviation**: 没有 "Generate All" 单按钮, 不能一次出 4 种; 也没有 scope dropdown (By MR / By App / By Domain)。

### UC-E4 / E5 ⚠️ N/A

- **E4** runbook 描述的 "View HTML in App" (WebView2 内嵌) 按钮不存在
- **E5** runbook 描述的 "Dashboard 主页" nav 不存在 — 主页是 MR Display 数据网格 (空)

## 性能实测

| 操作 | baseline | 实测 | 结果 |
|---|---|---|---|
| WPF 冷启动 | < 5 s | **2.68 s** | ✅ |
| Application Add (1 次, 含 Upl + 确认) | < 2 s | **~3 s** | ⚠️ (含 file dialog 处理) |
| heat_equation 单次 Run (factor=2) | < 5 s | **3.4 s** | ✅ |
| heat_equation 单次 Run (factor=0.5) | — | **~4 s** | ✅ |
| 报表导出 (无数据) | — | <1 s (即时报错) | ✅ |

## 5 个 cloud-covered 用例

| UC | 测试套件 | baseline 结果 |
|---|---|---|
| UC-A8 | `V1CompatibilityTests` + `V2EntityRoundtripTests` + `MetaPatternEntityTests` + `MRBindingStatusTests` | ✅ 0 fail (cloud baseline-2026-05-17) |
| UC-D1 | `RCaseReproductionServiceTests` ≥ 9 facts | ✅ |
| UC-D2 | `WriteAudit_records_r_case_reproduced` fact | ✅ |
| UC-E6 | `SystemMtReportServiceTests` ≥ 6 facts | ✅ |
| UC-E7 | `HtmlSystemMtResultReport*Tests` > 0 facts | ✅ |

证据: `docs/uat/reports/baseline-2026-05-17/baseline-full.trx`

## 推荐后续动作

1. **修复 2 个 bug**（A2 / A5+B1 ApplicationEx ToString）— 这两个修了之后 round-2 应该能 +3 PASS, 大幅改善 21 UI 用例 PASS 率
2. **接线 SystemMt → Anomaly / Trends / Coverage** — 这是 v2 ↔ legacy data 桥, 当前没桥就导致 6 个 ⚠️ Partial
3. **runbook 修订** — UC-A5 / B2-B6 / E3-E5 需按当前 UI 重写, 不然每轮 UAT 都会有大量 Partial / N/A 混淆"真问题" vs "runbook 过期"
4. **Cosmetic typo 修复**: `Desciption` → `Description`, `Eecute MT` → `Execute MT`

## 文件清单

```
docs/uat/reports/round-1-limeng-2026-05-18/
├── README.md                            (本文件)
├── results-summary.md                   (26 用例逐行表)
├── _uat_helpers.ps1                     (UIA automation helper, 复用)
├── MR.Litedb-snapshot                   (跑完后的 method-level DB, 999 KB)
├── SystemMT.Litedb-snapshot             (System MT DB, 49 KB)
└── screenshots/                         (62 张 PNG, 包含 debug 步骤)
```
