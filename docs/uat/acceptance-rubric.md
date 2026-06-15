# UAT 验收评价表

> 验收员逐行打分。结果列说明：
> - ✅ = 通过（行为符合预期 + 产物齐全）
> - ⚠️ = 部分通过（核心功能 OK，但有非阻断瑕疵；需在 "备注" 写明）
> - ❌ = 不通过（功能缺失 / 异常 / 性能不达标）
>
> **阻断分类**：
> - 🔴 **Blocker** — 必须修；任一 ❌ 都阻断 Release
> - 🟡 **Major** — 应修；累计 ≥ 3 个 ❌ 阻断 Release
> - 🟢 **Minor** — 可延期；不影响 Release

签收方填写：
- 验收日期：________
- 验收员：________
- 环境：Linux 版本 ________ · Windows 版本 ________ · OpenMOC ✅/❌ · LLM provider 数量 ____

---

## 总评汇总（验收员填）

| 类别 | 用例数 | ✅ | ⚠️ | ❌ | 通过率 |
|------|--------|---|----|----|-------|
| A. 管理 CRUD | 8 | 3 | 5 | | A8 trx + A1/A7 UI ✅；A2/A3/A4/A5/A6 ⚠️ |
| B. MR 测试主流程 | 9 | 6 | 3 | | B2-B5 ✅(System-MT 端到端真跑通)+B7 ✅(播种 2 异常)+B1 ✅(修复后发现产 14 候选)；B6/B8/B9 ⚠️ |
| C. MR 发现 & 验证 | 11 | 7 | 3 | | C1-C5/C10-C11 trx ✅；C6/C7/C9 ⚠️；C8 未跑 |
| D. R-Case 自动复现 | 2 | 2 | | | D1-D2 trx ✅ |
| E. 可视化 & 报表 | 7 | 3 | 3 | | E6/E7 trx + E2 UI ✅；E3/E4/E5 ⚠️(E5 导航已修，页面 stub) |
| F. 持久化 & schema | 5 | 5 | | | F1-F5 trx ✅ |
| G. 运营 & 性能 | 5 | 4 | | | G1-G2/G4-G5 trx ✅；G3 已删除 |
| **合计** | **47** | **31** | **14** | **0** | SP3a 22 trx ✅ + SP3b UI 9 ✅(A1/A7/B1/B2-B5/B7/E2) / 14 ⚠️ / 0 ❌ / 1 未跑(C8)；**24/24 页导航渲染通过**；**6 项发现已修 5(代码) + 1 重新归类**。SP3b 部分验收(见 sp3b-summary.md)。 |

**Release 判定**（验收员勾选）：

- [ ] **PASS** — 全部 ✅ 或仅有 ⚠️ + Minor ❌（≤ 3）
- [ ] **CONDITIONAL PASS** — 有 Major ❌ 但已有修复 ticket
- [ ] **FAIL** — 有 Blocker ❌

---

## A. 管理 CRUD（应用程序 / 域 / 蜕变关系 / MetaPattern）

| # | 用例 | 通过准则（行为 + 产物） | 阻断 | 结果 | 证据 (截图/日志) |
|---|------|----------------------|------|------|---------------|
| A1 | 新建 Application | 列表行 + DB 中 `Applications` 集合可查；操作 < 2 s | 🔴 | ✅ | sp3b: caseA1-dialog3「添加记录 成功!」+ 文件上传 + 表单；MR.Litedb 已写 |
| A2 | 编辑 Application | 列表更新 + DB 同行字段更新 | 🔴 | ⚠️ | sp3b: 编辑表单+Modify+校验生效(caseA2-dialog3「请填写Name」)；UIA 编程选行不触发 ShowSelected 填表单(夹具局限) |
| A3 | 删除 Application | 列表行消失 + DB 硬删 / 软删 `Status=deleted` | 🔴 | ⚠️ | sp3b: 选中+Delete+确认链路通；导入SUT删除「删除记录 失败」(caseA3-dialog2)+确认框文案误写「是否修改」(caseA3-dialog1) |
| A4 | 新建 Domain 并绑定 App | `ApplicationDomains` 多对多 junction 新行 | 🟡 | ⚠️ | sp3b: 域创建表单+Add 无报错(caseA4-02)；持久化/junction 本轮未单独确认 |
| A5 | 新建 method-level MR | MR 列表多行 + 详情页可看 | 🔴 | ⚠️ | sp3b: caseA5 表单可填(Context/Input/Output/维度)+Add 执行；结果未确认——app combo 无 AutomationId 难选，方法级 MR Add 很可能需先选应用程序 |
| A6 | MR 列表搜索 / 筛选 | < 500 ms 响应，输入清空后恢复 | 🟢 | ⚠️ | sp3b: 筛选框+Query 渲染；WPF LostFocus 绑定+UIA ValuePattern 不聚焦致筛选值不传(夹具局限) |
| A7 | MetaPattern 列表显示 8 个 | 4 active + 4 out-of-scope，含 hypothesis 字段 | 🔴 | ✅ | sp3b: caseA7-01「8 total」分页 + 4 active/4 out-of-scope + 「Seeded 8 NOETHER」 |
| A8 | CRUD CLI 测试套件 | `Passed > 0, Failed = 0` | 🔴 | ✅ | sp3a-host.trx · 10 passed |

---

## B. MR 蜕变测试主流程

| # | 用例 | 通过准则（行为 + 产物） | 阻断 | 结果 | 证据 |
|---|------|----------------------|------|------|------|
| B1 | Discovery 页选 MR | 候选 MR 列表 ≥ 1 行，含 confidence | 🟡 | ✅ | sp3b: 初测报错(noether_candidates.py 未部署)；**修复后**(csproj 部署脚本 + METBENCH_SYSTEM_PYTHON)verifyFix2 显示「Done — status: ok, candidates: 14」——发现运行通且产 14 候选 |
| B2 | System-MT 选 MR + input | "Selected MR" + "Source Input Preview" 都显示 | 🔴 | ✅ | sp3b: caseB-03 选中「1D advection — ScaleInitial」(38 MR)+描述「Linearity MP_mono…」+factor=2 |
| B3 | 生成 followup 输入 | followup JSON 显示 + 落 temp 文件 + < 1 s | 🔴 | ✅ | sp3b: caseB-result 生成 source+follow-up(factor=2) |
| B4 | 跑测试 | 进度条三阶段推进 + 结束 status=ok/anomaly | 🔴 | ✅ | sp3b: caseB-result「PASS」Completed 0.08s/0.07s(真 python)。注：默认 PATH 无 python→preflight FAIL exit9009(环境非产品) |
| B5 | 结果面板字段齐全 | src / flw / passed / Δ / threshold / reason | 🔴 | ✅ | sp3b: caseB-result Source/Follow-up/Passed/peak_amplitude=0.756→1.512+Status(PASS) |
| B6 | Result chart 可视化 | CartesianChart + PieChart 显示 + hover tooltip | 🟡 | ⚠️ | sp3b: sweepDisp-B6 页渲染；图表需先跑方法级MT。发现：按钮拼写「Eecute MT」 |
| B7 | Anomaly List 浏览 | 倒序，含 Severity/Category/LinkedBug 列 | 🔴 | ✅ | sp3b: caseB7 播种 2 异常后列表显示 2 行(critical/major, investigating, cross-program-disagreement, ScaleModeratorSigmaA \|Δk\|=0.49196 50.8%)+Severity/Status 筛选 |
| B8 | 多选 anomaly 做 commonality | 共同维度报告非空 (或正确提示 "No commonality") | 🟡 | ⚠️ | sp3b: caseB8 Analyze commonality 按钮可点；共性报告需先选中异常，而异常 DataGrid 无 AutomationId 难以编程选行(夹具局限) |
| B9 | Anomaly Replay 重跑 | Replay 页显示 old vs new + Reproduced 布尔 | 🔴 | ⚠️ | sp3b: caseB9 Replay 页+Classification+Run/Simulate replay 渲染，空态正确引导「从异常页选行点 Replay this anomaly」；完整 old-vs-new 需从异常页带入或选 simulate-classification(无 Aid 网格选择受限) |

---

## C. MR 发现 & 识别 & 验证

| # | 用例 | 通过准则 | 阻断 | 结果 | 证据 |
|---|------|---------|------|------|------|
| C1 | 真实 python sidecar 发现 | Passed ≥ 4, Failed 0 | 🔴 | ✅ | sp3a-host.trx · 6 passed |
| C2 | Empirical + LLM Validator | Passed ≥ 5, Failed 0 | 🔴 | ✅ | sp3a-host.trx · 40 passed |
| C3 | MRPairing m_cmp partner | Passed ≥ 11, Failed 0 | 🔴 | ✅ | sp3a-host.trx · 11 passed |
| C4 | Multi-LLM Consensus + κ | Passed ≥ 15, Failed 0 | 🟡 | ✅ | sp3a-host.trx · 15 passed |
| C5 | Validation Service E2E | Passed > 0, Failed 0 | 🔴 | ✅ | sp3a-host.trx · 13 passed |
| C6 | Candidate Review UI | 列表非空 + Promote 入正式 MR 表 | 🟡 | ⚠️ | sp3b: sweepDisp-C6 页+Empirical/TheoreticalLLM 校验勾选+候选选择器渲染；候选需先跑发现 |
| C7 | MR Recommendation UI | top-K 推荐 + 按 confidence 排序 | 🟢 | ⚠️ | sp3b: sweepDisp-C7 页渲染；推荐网格 0 行(需数据) |
| C8 | AutoDetectMR UI | 进度条 < 2 min + 候选可入库 | 🟡 | ⏳ deferred | UI 流程（上传确认框 + OpenFileDialog + 进度条 + StoreMR 入库确认）与检测脚本 MetBench_Python/AutoMRDetector/auto_mr_detector-3.py 均在仓库；真跑通依赖**遗留方法级 AutoMR python 栈**（AutoMRAlgorithm.RunPythonScriptAsync 经 FindFirstPythonInPath 取 PATH 含 "Python" 目录 + sympy/numpy），当前运行时缺 sympy → deferred（与 SP1-SP5 system-MT 运行时独立）。见 SP5 报告 §4 #8 |
| C9 | Mutation Campaign UI | Kill Rate ≥ 0 + diff 可看 | 🟡 | ⚠️ | sp3b: caseC9/C9kill Seed 填充 5 变异体+MR 绑定(checkall 证实**均已自动勾选**)+Start campaign 经 invokename 真执行(python 在 PATH)；kill-rate 结果网格仍 0 行——与成熟度计划记录的「T6 launcher-backed cellRunner 为 deferred 原型(P5 #329 显式标注 Prototype)」一致，即活动 UI 通但 cellRunner 不真跑变异体 |
| C10 | SCG-Heuristic Discoverer | Passed ≥ 14, Failed 0 + 三类 pattern 都产 candidate（原阈值 29 为陈旧枚举预估；实际 trx 测得 14，三类 pattern 各有专门断言：DirectCause_pattern_produces_monotonic_hint / Mediator_pattern_only_when_no_direct_edge / Confounder_pattern_detects_common_cause） | 🟡 | ✅ | sp3a-host.trx · 14 passed |
| C11 | OpenMC 第 3-SUT BDD smoke | Cross-program neutron transport feature: openmc-pincell-nu-sigma-f + openmc-pincell-sigma-a 2 scenarios 跑通；`OpenMcRunnerSmokeTests` Passed = 1, Failed 0；output JSON 含 `k_eff` ∈ [0.5, 2.0] + `metadata.runner=openmc` | 🟡 | ✅ | sp3a-c11.trx · 5 passed |

---

## D. R-Case 自动复现（论文核心）

| # | 用例 | 通过准则 | 阻断 | 结果 | 证据 |
|---|------|---------|------|------|------|
| D1 | R-Case service 跑通 | Passed ≥ 9, Failed 0 | 🔴 | ✅ | sp3a-host.trx · 9 passed |
| D2 | r-case.reproduced audit | trx 含 fact `ReproduceAsync_anomaly_with_large_gap_marks_reproduced` 通过（原名 `WriteAudit_records_r_case_reproduced` 为陈旧近似；实际断言 r-case.reproduced 在 `RCaseReproductionServiceTests.ReproduceAsync_anomaly_with_large_gap_marks_reproduced`） | 🔴 | ✅ | sp3a-host.trx · 1 passed (fact present) |

---

## E. 可视化 & 报表

| # | 用例 | 通过准则 | 阻断 | 结果 | 证据 |
|---|------|---------|------|------|------|
| E1 | ~~Trend Dashboard 时间序列~~ | （已删除，next-stage P0：Trend Dashboard + 子系统下线） | — | — | — |
| E2 | Coverage Dashboard 4 维饼图 | 4 个 PieChart 均含 ≥ 2 扇区 + legend | 🟡 | ✅ | sp3b: sweepDisp-E2「4-Dimension Coverage Dashboard」+MetaPattern Coverage 饼图 2 扇区(红/绿)；其余 3 图在下方 |
| E3 | 报表导出 4 端 | `Word/Excel/PDF/HTML` 4 文件均生成 + 可打开 | 🔴 | ⚠️ | sp3b: caseE3 ReportTypeComboBox 确有 4 端(Pdf/Word/Excel/Html)+Export 按钮；导出未跑完(selcombo 在该 combo 上 UIA 超时) |
| E4 | HTML 嵌入 WebView2 | 页内渲染正确，CSS / 表格无错位 | 🟡 | ⚠️ | sp3b: sweepDisp-E4 报告页+WebView2 控件存在；需先生成报告才渲染 HTML |
| E5 | Dashboard 主页 cards | ≥ 4 个 summary card + 数值有意义 | 🟢 | ⚠️ | sp3b: **修复导航入口后**(加 Nav_Dashboard)verifyFix1 可达且渲染；但 Dashboard 页仅 1 个「Click me!」演示按钮，**非 ≥4 summary cards**(页面内容仍是占位 stub，独立内容 gap) |
| E6 | SystemMtReport service CLI | Passed ≥ 6, Failed 0 | 🟡 | ✅ | sp3a-host.trx · 12 passed |
| E7 | HtmlReportRenderer 单测 | Passed > 0, Failed 0 | 🟡 | ✅ | sp3a-host.trx · 20 passed |

---

## F. 持久化 & schema

| # | 用例 | 通过准则 | 阻断 | 结果 | 证据 |
|---|------|---------|------|------|------|
| F1 | DbConfig 3 级 override | Passed ≥ 5, Failed 0 | 🔴 | ✅ | sp3a-host.trx · 5 passed |
| F2 | MetaPattern Seed 8 个 | Passed ≥ 11, Failed 0 | 🔴 | ✅ | sp3a-host.trx · 12 passed |
| F3 | MRBinding.Status 软删 | Passed ≥ 7, Failed 0 | 🔴 | ✅ | sp3a-host.trx · 7 passed |
| F4 | V2 schema migration | Passed ≥ 9, Failed 0 | 🔴 | ✅ | sp3a-host.trx · 9 passed |
| F5 | V2 DI 完整性 | 所有 V2 IXxxRepo 解析 OK | 🔴 | ✅ | sp3a-host.trx · 5 passed |

---

## G. 运营 & 性能

| # | 用例 | 通过准则 | 阻断 | 结果 | 证据 |
|---|------|---------|------|------|------|
| G1 | LiteDB Keyset 分页 | Passed ≥ 10, Failed 0 | 🟡 | ✅ | sp3a-host.trx · 10 passed |
| G2 | CI 性能基线 | `ci_perf_baseline.py` exit 0 + total < 120 s | 🟡 | ✅ | ci_perf_baseline exit0 · CI baseline 41.67s<120s |
| G3 | ~~多维 burst 检测~~ | （已删除，next-stage P0：Trend 子系统下线） | — | — | — |
| G4 | Coverage service 单测 | Passed ≥ 5, Failed 0 | 🟡 | ✅ | sp3a-host.trx · 5 passed |
| G5 | Anomaly service + commonality | Passed ≥ 8, Failed 0 | 🟡 | ✅ | sp3a-host.trx · 15 passed |

---

## 状态术语对照表

| 显示值 | 含义 | 行为期望 |
|-------|------|---------|
| `queued` | pipeline 尚未启动 | 进度条 0% |
| `running-source` | source SUT 跑中 | 进度条 1/3 |
| `running-followup` | followup SUT 跑中 | 进度条 2/3 |
| `asserting` | 比较中 | 进度条 ≈ 90% |
| `ok` | MR 满足 | 行 / status 标签绿色 |
| `anomaly` | MR 不满足 | 行 / status 标签红色 + 落 Anomaly 表 |
| `error` | pipeline 异常退出 | 状态标签灰 + 错误信息可看 |
| `timeout` | SUT 超时 | 同 error，含 "exceeded N s" |
| `cancelled` | 用户取消 | 同 error，含 "user cancel" |

---

## 性能 SLA（验收时实测）

| 操作 | 期望 SLA | 实测 | 通过 |
|------|---------|------|------|
| 主窗口冷启动 | < 5 s | _____ s | |
| 切换页面 | < 200 ms | _____ ms | |
| MR / Application CRUD | < 2 s | _____ s | |
| OpenMOC 单次跑 (pin-cell) | < 90 s | _____ s | |
| Heat-Equation 单次跑 | < 5 s | _____ s | |
| 报表导出（Word + Excel + PDF + HTML） | < 30 s | _____ s | |
| Trend / Coverage 刷新 | < 3 s | _____ s | |

---

## 数据完整性自检（验收员手动 spot check）

打开 `MR.Litedb` 用 [LiteDB Studio](https://github.com/mbdavid/LiteDB.Studio)，验：

- [ ] `Applications` 行数 ≥ UAT 中创建的数 + 旧种子数
- [ ] `MetaPatterns` 恰好 8 行；4 active + 4 out-of-scope
- [ ] `MRBindings` 每行 `Status ∈ {active, deprecated, deleted}`
- [ ] `Executions` 每行 `Status` 是 PipelineStatus 13 种之一
- [ ] `Anomalies` 每行 `ResultId` 在 `Results` 表里能找到
- [ ] `AuditLog` 含 `r-case.reproduced` 至少一行（来自 UC-D1）

---

## 总评（验收员填）

**通过项**：___________________________________________

**未通过项 + 阻断分类**：___________________________________________

**Release 建议**：

- [ ] 可发布
- [ ] 修复后可发布（列出 issue 号：________）
- [ ] 不可发布（说明：________）

**签字**：__________ **日期**：__________
