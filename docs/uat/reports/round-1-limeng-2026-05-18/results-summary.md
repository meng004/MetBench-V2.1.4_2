# Windows UAT Round-1 — 26 用例逐行结果

| UC | 类别 | 结果 | 备注 | 证据 |
|---|---|---|---|---|
| UC-A1 | A 管理 CRUD | ✅ PASS | Application Add 成功 (UAT-App-1, amax.py 上传, 100 LoC); 表单 → Upl → 确认 → Add → 列表新行可见; SoftwareUnderTest 在 v2.1 UI 是必填项 + 文件上传 (runbook 缺描述) | `UC-A1-application-created.png`, `UC-A1-query-result.png` |
| UC-A2 | A | ❌ FAIL | **BUG**: ApplicationService.UpdateService line 102 `IsDuplicate(application, false)` 没排除自身, 同名 update 误判为重复; 弹 "该应用程序已存在！". 详见 README §偏差说明 | `UC-A2-FAIL-duplicate-bug.png`, `UC-A2-form-edited.png` |
| UC-A3 | A | ✅ PASS | 选 UAT-App-1 行 → Del → 确认 Yes → "删除记录 成功" → Query 列表为空; 实测是硬删 (runbook 暗示可能是软删, 实际是硬删 ⚠️) | `UC-A3-after-delete.png` |
| UC-A4 | A | ✅ PASS (with deviation) | Neutronics Domain 创建成功 (Name + Desciption "Neutron transport (UAT)"); **deviation**: 表单缺 "Bound Applications" 多选框, 无法绑定 Application 到 Domain; **typo**: 表单标签 "Desciption" (少 'r') | `UC-A4-domain-created.png` |
| UC-A5 | A | ❌ FAIL | **BUG**: ApplicationName ComboBox 显示 "MetBench_Client.Models.ApplicationEx" 类名 (missing ToString); 即便选择后 Add 也无效 (列表无新增, 无错误 Tips). **deviation**: 表单字段与 runbook 完全不同 (实际是 InputPattern/OutputPattern/Operator/Expression 等 method-level 详细 schema) | `UC-A5-dropdowns-filled.png`, `UC-A5-mr-list-final.png` |
| UC-A6 | A | ⚠️ Partial | Query 接受搜索输入 + 不 crash; 但 DB 中无 MR 数据无法验证 filter 行为; 性能未达精度测量 (sleep 阻塞) | `UC-A6-search-filtered.png`, `UC-A6-search-cleared.png` |
| UC-A7 | A | ✅ PASS | MetaPatterns 列表完整 8 行: page 1 (m_inv/m_mono/m_adj/m_rev/m_conv) + page 2 (m_dyn/m_cmp/m_rel); active=4 (m_inv/m_mono/m_conv/m_cmp), out-of-scope=4 (m_adj/m_rev/m_dyn/m_rel) — 与 runbook spec 完全一致 ✅; 分页 "8 total" footer 确认 | `UC-A7-metapatterns-list.png`, `UC-A7-metapatterns-page2.png` |
| UC-A8 | A | ✅ Cloud Covered | cloud baseline-2026-05-17 已覆盖 (V1CompatibilityTests + V2EntityRoundtripTests + MetaPatternEntityTests + MRBindingStatusTests, 0 fail) | (cloud trx) |
| UC-B1 | B MR 主流程 | ❌ FAIL | Discovery 页 Target SUT 下拉同 UC-A5 ApplicationName bug, 显示 "MetBench_Domain.Application" 类名; UIA Select 后选择不持久; Run discovery 无产出 | `UC-B1-discovery-result.png` |
| UC-B2 | B | ✅ PASS | **使用 System MT 页** (而非 runbook 描述的 MT Execution 页 — 那个是 legacy method-level UI). Scenario dropdown 默认 "1D heat equation — ScaleAmplitude (linearity)", Description 面板显示完整理论说明 | `UC-B2-system-mt-page.png` |
| UC-B3 | B | ⚠️ N/A | System MT 新 UI 不分 Generate Follow-up / Run 两步, Run scenario 内一次完成; 故 UC-B3 在新 UI 下不适用 | — |
| UC-B4 | B | ✅ PASS | Run scenario (factor=2) 完成于 **3.4 s** (< 5s 期望); Status 面板显示 "Completed in source=0.05s follow-up=0.05s"; 结果表新增行 | `UC-B4-result.png`, `UC-B4-running.png` |
| UC-B5 | B | ✅ PASS | Result 表行字段: Run At / Scenario / Assertion (GreaterThan/LessThan) / Value (max_u 或 k_eff) / Source (数值) / Follow-up (数值) / Passed (✓/空). 失败 run "Last result" 显示完整 reason: "Assertion failure: Expected follow-up value 0.63... to be greater than source value 0.97... for max_u" | `UC-B4-result.png`, `UC-B7-prep-anomaly-runs.png` |
| UC-B6 | B | ⚠️ N/A | System MT 页面没有图表区 (结果以表格展示); 不适用 chart hover tooltip 验证 | — |
| UC-B7 | B | ⚠️ Partial | Anomalies 页正确渲染 (Severity/Status 过滤器, Id/Severity/Status/Category/Discovered/Linked Bug/Replay #/Notes 列, Analyze commonality + Transition + Replay this anomaly 按钮); BUT 列表 "0 total" — System MT 失败 run 未写入 Anomalies 表 (data 接线 gap) | `UC-B7-anomalies-list.png`, `UC-B7-anomalies-after-refresh.png` |
| UC-B8 | B | ⚠️ N/A | 无 anomaly 数据可多选 + Analyze; 同 UC-B7 根因 | — |
| UC-B9 | B | ⚠️ N/A | 无 anomaly 数据可 Replay; 同 UC-B7 根因 | — |
| UC-D1 | D | ✅ Cloud Covered | cloud baseline 已覆盖 (`RCaseReproductionServiceTests` ≥ 9 facts) | (cloud trx) |
| UC-D2 | D | ✅ Cloud Covered | cloud baseline 已覆盖 (`WriteAudit_records_r_case_reproduced` fact + Passed) | (cloud trx) |
| UC-E1 | E 可视化报表 | ⚠️ Partial | Trends 页渲染 ✅ — Week start 选择器, 5 summary cards (Executions/Anomalies/Anomaly Rate/Bursts/Promoted MRs), CartesianChart (Executions/Anomalies 两条曲线 + legend), Anomaly Bursts 面板; 所有数值 0 — 数据 0 因 System MT runs 未进 Trends 数据源 | `UC-E1-trends-page.png` |
| UC-E2 | E | ⚠️ Partial | Coverage 页渲染 ✅ — 4 PieChart (MetaPattern Coverage / SUT × MR Binding / Bug / Mutation Detection); MetaPattern 显示 1 个 Uncovered 红扇区 ("0 / 8 patterns"); 其余 3 个 "No data" — runbook 期望每图 ≥ 2 扇区, 本轮 DB 数据不足 | `UC-E2-coverage-page.png` |
| UC-E3 | E | ⚠️ Partial | MR ReportGenerator 页有 Report Type 下拉 (Pdf/Word/Excel/Html 4 项, 与 "4 端"对应) + ExportReport 按钮; 选任一类型 → Export → 弹 "无目标文件！" — 因 method-level MR 表空; **deviation**: 没有 "Generate All" 单按钮, 没有 scope dropdown | `UC-E3-after-pdf-export.png`, `UC-E3-report-page.png` |
| UC-E4 | E | ⚠️ N/A | runbook 描述的 "View HTML in App" (WebView2 内嵌) 按钮在 MR ReportGenerator 页不存在 | — |
| UC-E5 | E | ⚠️ N/A | runbook 描述的 "Dashboard 主页" nav 项不存在; 主页是 MR Display (数据网格) | `UC-00-main-window.png` |
| UC-E6 | E | ✅ Cloud Covered | cloud baseline 已覆盖 (`SystemMtReportServiceTests` ≥ 6 facts) | (cloud trx) |
| UC-E7 | E | ✅ Cloud Covered | cloud baseline 已覆盖 (`HtmlSystemMtResultReport*Tests` > 0 facts) | (cloud trx) |

## 汇总

- **PASS (含 cloud-covered)**: 11/26 (= 6 Windows + 5 cloud)
  - A1, A3, A4, A7, B2, B4, B5, A8c, D1c, D2c, E6c, E7c (= 12 — 实际算 11 因 B4/B5 共享 run)
- **PARTIAL / N/A**: 10/26 — 多为"页面渲染 OK + DB 数据不足或 UI runbook 不对齐"
- **FAIL**: 3/26 — A2, A5, B1, 已定位 2 个真实 bug (UpdateService 自身排除 + ApplicationEx ToString)

**Round-1 结论**: WPF 主程序整体 **CONDITIONAL PASS** — 核心 System MT 流程跑通 ✅, 但 method-level CRUD 链 (A1-A5) 受 2 个 bug 阻塞, 部分 dashboard / report UI 因 SystemMt ↔ legacy data 接线 gap 显示空数据。建议 round-2 修完 2 个 bug + 接线 gap 后复跑。
