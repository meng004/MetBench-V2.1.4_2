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
| A. 管理 CRUD | 8 | | | | / |
| B. MR 测试主流程 | 9 | | | | / |
| C. MR 发现 & 验证 | 10 | | | | / |
| D. R-Case 自动复现 | 2 | | | | / |
| E. 可视化 & 报表 | 7 | | | | / |
| F. 持久化 & schema | 5 | | | | / |
| G. 运营 & 性能 | 5 | | | | / |
| **合计** | **46** | | | | / |

**Release 判定**（验收员勾选）：

- [ ] **PASS** — 全部 ✅ 或仅有 ⚠️ + Minor ❌（≤ 3）
- [ ] **CONDITIONAL PASS** — 有 Major ❌ 但已有修复 ticket
- [ ] **FAIL** — 有 Blocker ❌

---

## A. 管理 CRUD（应用程序 / 域 / 蜕变关系 / MetaPattern）

| # | 用例 | 通过准则（行为 + 产物） | 阻断 | 结果 | 证据 (截图/日志) |
|---|------|----------------------|------|------|---------------|
| A1 | 新建 Application | 列表行 + DB 中 `Applications` 集合可查；操作 < 2 s | 🔴 | | |
| A2 | 编辑 Application | 列表更新 + DB 同行字段更新 | 🔴 | | |
| A3 | 删除 Application | 列表行消失 + DB 硬删 / 软删 `Status=deleted` | 🔴 | | |
| A4 | 新建 Domain 并绑定 App | `ApplicationDomains` 多对多 junction 新行 | 🟡 | | |
| A5 | 新建 method-level MR | MR 列表多行 + 详情页可看 | 🔴 | | |
| A6 | MR 列表搜索 / 筛选 | < 500 ms 响应，输入清空后恢复 | 🟢 | | |
| A7 | MetaPattern 列表显示 8 个 | 4 active + 4 out-of-scope，含 hypothesis 字段 | 🔴 | | |
| A8 | CRUD CLI 测试套件 | `Passed > 0, Failed = 0` | 🔴 | | trx 文件 |

---

## B. MR 蜕变测试主流程

| # | 用例 | 通过准则（行为 + 产物） | 阻断 | 结果 | 证据 |
|---|------|----------------------|------|------|------|
| B1 | Discovery 页选 MR | 候选 MR 列表 ≥ 1 行，含 confidence | 🟡 | | |
| B2 | System-MT 选 MR + input | "Selected MR" + "Source Input Preview" 都显示 | 🔴 | | |
| B3 | 生成 followup 输入 | followup JSON 显示 + 落 temp 文件 + < 1 s | 🔴 | | followup.json |
| B4 | 跑测试 | 进度条三阶段推进 + 结束 status=ok/anomaly | 🔴 | | |
| B5 | 结果面板字段齐全 | src / flw / passed / Δ / threshold / reason | 🔴 | | 截图 |
| B6 | Result chart 可视化 | CartesianChart + PieChart 显示 + hover tooltip | 🟡 | | 截图 |
| B7 | Anomaly List 浏览 | 倒序，含 Severity/Category/LinkedBug 列 | 🔴 | | 截图 |
| B8 | 多选 anomaly 做 commonality | 共同维度报告非空 (或正确提示 "No commonality") | 🟡 | | 截图 |
| B9 | Anomaly Replay 重跑 | Replay 页显示 old vs new + Reproduced 布尔 | 🔴 | | 截图 |

---

## C. MR 发现 & 识别 & 验证

| # | 用例 | 通过准则 | 阻断 | 结果 | 证据 |
|---|------|---------|------|------|------|
| C1 | 真实 python sidecar 发现 | Passed ≥ 4, Failed 0 | 🔴 | | trx |
| C2 | Empirical + LLM Validator | Passed ≥ 8, Failed 0 | 🔴 | | trx |
| C3 | MRPairing m_cmp partner | Passed ≥ 11, Failed 0 | 🔴 | | trx |
| C4 | Multi-LLM Consensus + κ | Passed ≥ 15, Failed 0 | 🟡 | | trx |
| C5 | Validation Service E2E | Passed > 0, Failed 0 | 🔴 | | trx |
| C6 | Candidate Review UI | 列表非空 + Promote 入正式 MR 表 | 🟡 | | 截图 |
| C7 | MR Recommendation UI | top-K 推荐 + 按 confidence 排序 | 🟢 | | 截图 |
| C8 | AutoDetectMR UI | 进度条 < 2 min + 候选可入库 | 🟡 | | 截图 |
| C9 | Mutation Campaign UI | Kill Rate ≥ 0 + diff 可看 | 🟡 | | 截图 |
| C10 | SCG-Heuristic Discoverer | Passed ≥ 29, Failed 0 + 三类 pattern 都产 candidate | 🟡 | | trx |

---

## D. R-Case 自动复现（论文核心）

| # | 用例 | 通过准则 | 阻断 | 结果 | 证据 |
|---|------|---------|------|------|------|
| D1 | R-Case service 跑通 | Passed ≥ 9, Failed 0 | 🔴 | | trx |
| D2 | r-case.reproduced audit | trx 含 fact `WriteAudit_records_r_case_reproduced` 通过 | 🔴 | | trx |

---

## E. 可视化 & 报表

| # | 用例 | 通过准则 | 阻断 | 结果 | 证据 |
|---|------|---------|------|------|------|
| E1 | Trend Dashboard 时间序列 | CartesianChart 折线 + hover + WoW 标注 | 🟡 | | 截图 |
| E2 | Coverage Dashboard 4 维饼图 | 4 个 PieChart 均含 ≥ 2 扇区 + legend | 🟡 | | 截图 |
| E3 | 报表导出 4 端 | `Word/Excel/PDF/HTML` 4 文件均生成 + 可打开 | 🔴 | | 4 文件 |
| E4 | HTML 嵌入 WebView2 | 页内渲染正确，CSS / 表格无错位 | 🟡 | | 截图 |
| E5 | Dashboard 主页 cards | ≥ 4 个 summary card + 数值有意义 | 🟢 | | 截图 |
| E6 | SystemMtReport service CLI | Passed ≥ 6, Failed 0 | 🟡 | | trx |
| E7 | HtmlReportRenderer 单测 | Passed > 0, Failed 0 | 🟡 | | trx |

---

## F. 持久化 & schema

| # | 用例 | 通过准则 | 阻断 | 结果 | 证据 |
|---|------|---------|------|------|------|
| F1 | DbConfig 3 级 override | Passed ≥ 5, Failed 0 | 🔴 | | trx |
| F2 | MetaPattern Seed 8 个 | Passed ≥ 11, Failed 0 | 🔴 | | trx |
| F3 | MRBinding.Status 软删 | Passed ≥ 7, Failed 0 | 🔴 | | trx |
| F4 | V2 schema migration | Passed ≥ 9, Failed 0 | 🔴 | | trx |
| F5 | V2 DI 完整性 | 所有 V2 IXxxRepo 解析 OK | 🔴 | | trx |

---

## G. 运营 & 性能

| # | 用例 | 通过准则 | 阻断 | 结果 | 证据 |
|---|------|---------|------|------|------|
| G1 | LiteDB Keyset 分页 | Passed ≥ 10, Failed 0 | 🟡 | | trx |
| G2 | CI 性能基线 | `ci_perf_baseline.py` exit 0 + total < 120 s | 🟡 | | 日志 |
| G3 | 多维 burst 检测 | Passed ≥ 4, Failed 0 | 🟢 | | trx |
| G4 | Coverage service 单测 | Passed ≥ 5, Failed 0 | 🟡 | | trx |
| G5 | Anomaly service + commonality | Passed ≥ 8, Failed 0 | 🟡 | | trx |

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
