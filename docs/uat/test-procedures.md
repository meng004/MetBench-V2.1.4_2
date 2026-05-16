# UAT 用例执行手册

> 每个用例编号 **UC-XX**，对应 [acceptance-rubric.md](acceptance-rubric.md) 同号评分行。
> 命令默认 `cd MetBench-V2.1.4_2/` 后执行；WPF 步骤默认在 Windows 11 + VS 2022。

## 验收用例索引

| 类别 | 范围 | 用例数 | 平台 |
|------|------|-------|------|
| A. **管理 CRUD**（应用 / 域 / MR / MetaPattern） | F、UI | 8 | Win |
| B. **MR 蜕变测试主流程**（选 → 生 → 跑 → 看 → 重跑） | F、UI + Linux | 9 | Win + Linux |
| C. **MR 发现 & 验证**（含 LLM / mutmut / 多家投票） | F | 7 | Linux |
| D. **R-Case 自动复现**（论文核心） | F | 2 | Linux |
| E. **可视化 & 报表**（趋势 / coverage / Word/Excel/PDF/HTML） | F、UI | 7 | Win |
| F. **持久化 & schema** | F | 5 | Linux |
| G. **运营 & 性能** | F | 5 | Linux |
| **合计** | | **43** | |

---

## 类别 A — 管理 CRUD

### UC-A1 Application 管理 — 新建

操作：

1. 启动 `dotnet run --project MetBench_Client`
2. 左侧导航点 **Application Management** 页
3. 点 "**+ New Application**" 按钮，弹出对话框
4. 填：`Name=UAT-App-1` `Code=uat-app-1` `Description=UAT smoke`
5. 点 "Save"

✅ 期望：

- 列表多一行 `UAT-App-1`
- DB `Applications` 集合可查到该行（用 [LiteDB Studio](https://github.com/mbdavid/LiteDB.Studio) 打开 `MR.Litedb` 验证）
- 操作 < 2 s 完成

---

### UC-A2 Application 管理 — 编辑

接 UC-A1：

1. 列表里双击 `UAT-App-1`
2. 改 `Description=UAT smoke v2`
3. 点 "Save"

✅ 期望：列表行的描述列已更新；DB 中同一 IdApplication 字段更新。

---

### UC-A3 Application 管理 — 删除

接 UC-A2：

1. 选中 `UAT-App-1` 行
2. 点 "Delete"，确认对话框点 "Yes"

✅ 期望：

- 行从列表消失
- DB `Applications` 不再含该行（硬删）**或** 行 `Status=deleted`（软删，取决于 V2 schema）

---

### UC-A4 Domain 管理 — 新建 + 绑定

1. 进 **Domain Management** 页
2. 新建 `Name=Neutronics`
3. 在 "Bound Applications" 多选框勾上 `UAT-App-1`
4. Save

✅ 期望：

- `ApplicationDomains` junction 表新增一行 (UAT-App-1, Neutronics)
- 在 Application Management 页里看 `UAT-App-1` 的 Domain 列含 `Neutronics`

---

### UC-A5 MR 管理 — 新建 method-level MR

1. 进 **MR Management** 页
2. "+ New" → 填表：
   - `Name=UAT-Identity-MR`
   - `Type=invariance`
   - `Granularity=method`
   - `Constraint=output==input`
3. Save

✅ 期望：列表多一行；可在 MR Display 页选中查看详情。

---

### UC-A6 MR 管理 — 列表筛选 / 搜索

1. 在 MR Management 页搜索框输入 `Identity`
2. 列表只剩匹配行

✅ 期望：搜索 < 500 ms 响应；输入框清空后列表恢复。

---

### UC-A7 MetaPattern 列表 — 显示 8 个 NOETHER

1. 进 **MetaPatterns** 页
2. 观察列表

✅ 期望：

- 列表恰好 8 行：`m_inv`, `m_mono`, `m_conv`, `m_cmp` (Status=active)；`m_adj`, `m_rev`, `m_dyn`, `m_rel` (Status=out-of-scope)
- 每行点开能看 `HypothesisTemplate` / `DefaultAssertionTypeCode` 字段
- 切换 "Show out-of-scope only" 过滤后只剩 4 行

---

### UC-A8 CRUD 端到端（无 UI 路径）

仅 Linux：通过测试套件验证 CRUD 在 DAL 层正确：

```bash
dotnet test MetBench_SystemMT.Tests \
  --filter "FullyQualifiedName~ApplicationRepositoryTests|FullyQualifiedName~MetaPatternEntityTests|FullyQualifiedName~MRBindingStatusTests"
```

✅ 期望：Passed > 0, Failed = 0。

---

## 类别 B — MR 蜕变测试主流程

### UC-B1 选择 MR — Discovery 页

1. 进 **Discovery** 页（method-level MR discovery）
2. 选 SUT = `amax.py` （仓库 `SUT/amax.py`）
3. 点 "Run Discovery" 触发离线/在线 sampler
4. 列表显示候选 MR

✅ 期望：列表 ≥ 1 行候选 MR，每行含 confidence / sample-pass-rate。

---

### UC-B2 选择 MR — System-MT (MTExecution 页)

1. 进 **MT Execution** 页
2. 选 SUT = `openmoc`，MR = `ScaleNuSigmaF`
3. 选 input 样本 = `pincell.json`

✅ 期望：

- 上方 "Selected MR" 区域显示完整 metamorphic relation 描述
- 中间 "Source Input Preview" 显示 `pincell.json` 文本内容前 N 行

---

### UC-B3 数据生成 — Followup 自动生成

接 UC-B2：

1. 点 "**Generate Follow-up**" 按钮（不立即跑）
2. 观察 followup 区域

✅ 期望：

- followup 输入 JSON 显示在右侧，与 source 仅有 `nuSigmaF` 等转换字段差异
- followup 文件落在 `temp/openmoc_followup_*.json`，调用方可手动 cat 验证
- 操作 < 1 s 完成

---

### UC-B4 测试执行 — 点 Run

接 UC-B3：

1. 点 "**Run**" 按钮
2. 观察底部状态栏 + 进度条

✅ 期望：

- 进度条按 source → followup → assertion 三步推进
- 单次 OpenMOC 跑时长 ~20-60 s（取决于硬件 / 网格大小）
- 结束后 `Status=ok` 或 `Status=anomaly`，对应底色为绿 / 红

---

### UC-B5 结果展示 — Result 面板

接 UC-B4：

1. 看 "Result" 面板

✅ 期望面板含以下字段：

- `Source k_eff` `Follow-up k_eff` （数值；两端必须 > 0）
- `Assertion Passed` 布尔
- `Observed Δ` `Expected Threshold`
- 失败时含 `Failure Reason` 文本

---

### UC-B6 结果可视化 — chart

接 UC-B5（同页底部）：

1. 滚到底部图表区

✅ 期望：

- 数值类 metric → CartesianChart 显示 source vs followup 两条线
- 类别类 metric → PieChart 显示对比
- 鼠标 hover 节点显示 tooltip

---

### UC-B7 异常点查看 — Anomaly Browser

前置：UC-B4 至少出过 1 次 `anomaly` 状态。

1. 进 **Anomaly List** 页
2. 列表显示历史异常

✅ 期望：

- 列表按时间倒序，最近的 anomaly 在最上
- 每行含 `Severity` `Category` `LinkedKnownBug` 列
- 点行展开右侧详情 + 触发的原 source/followup 输入

---

### UC-B8 异常点 commonality 分析

接 UC-B7：

1. 选 ≥ 2 个 anomaly 行（Ctrl + click 多选）
2. 点 "Analyze Commonality"

✅ 期望：

- 弹出 / 右侧面板显示 commonality 报告
- 含 `Shared MR` `Shared SUT` `Shared parameter range` 等共同维度
- 若选的两条 anomaly 完全无共同点，显示 "No commonality"

---

### UC-B9 异常点重新测试 — Replay

1. 在 Anomaly List 选一行
2. 点 "**Replay**" 按钮 → 跳转到 **Replay Result** 页
3. 点 "**Run Real Replay**"（走 ReplayContextBuilder + ReplayService）

✅ 期望：

- 上方显示原始 anomaly 的 src/flw values
- 下方显示 replay 跑出的 src/flw values
- 中间显示 `Reproduced=true/false` + 数值偏差百分比
- 若 reproduced=true：右侧 KnownBug 字段已自动 link

---

## 类别 C — MR 发现 & 验证

### UC-C1 MR Discovery 真实 python sidecar

```bash
dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~MetaPatternDiscovererIntegrationTests"
```

✅ 期望：Passed ≥ 4, Failed 0。

---

### UC-C2 EmpiricalValidator + TheoreticalLlmValidator

```bash
dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~ValidatorTests"
```

✅ 期望：Passed ≥ 8, Failed 0。

---

### UC-C3 MRPairing — m_cmp partner binding

```bash
dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~MRPairingServiceTests"
```

✅ 期望：Passed ≥ 11, Failed 0。

---

### UC-C4 Multi-LLM Consensus + Cohen's κ （论文加分）

```bash
dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~MultiLlmConsensusValidatorTests"
```

✅ 期望：Passed ≥ 15, Failed 0。验：strict majority / tie → null / 解析失败剔除 / 异常隔离 / κ unanimous=1。

---

### UC-C5 Validation Service 端到端

```bash
dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~ValidationServiceTests"
```

✅ 期望：Passed > 0, Failed = 0。

---

### UC-C6 Candidate Review 页 — UI

1. 进 **Candidate Review** 页
2. 列表显示 Discovery 产出的 candidate MR
3. 选一行点 "Validate"

✅ 期望：

- candidate 列表非空
- 点 Validate 后右侧显示 `EmpiricalSample` 通过率
- 点 "Promote" 后 candidate 进入正式 MR 表

---

### UC-C7 MR Recommendation 页

1. 进 **MR Recommendation** 页
2. 选某个 Application + Domain
3. 点 "Recommend"

✅ 期望：列表显示 top-K 推荐 MR，按 confidence 排序。

---

### UC-C8 AutoDetectMR 页 — 一键识别新 MR

1. 进 **Auto Detect MR** 页
2. 选 SUT = `amax.py`，identifier mode = "Random + Heuristic"
3. 设 sample size = 50，点 "**Detect**"

✅ 期望：

- 进度条推进 < 2 min
- 完成后列表显示候选 MR + 每行 `Confidence` `Type` `Hypothesis`
- 候选可以勾选 "Save to Candidate" 入库

---

### UC-C9 Mutation Campaign 页 — 抗变异验证

1. 进 **Mutation Campaign** 页
2. 选 MR = `ScaleNuSigmaF`，operators = `mutmut.all`
3. 点 "Start Campaign"

✅ 期望：

- 矩阵显示 `MR × Mutant × Pass/Kill` 网格
- "Kill Rate" 数值 ≥ 0（≥ 50% 说明 MR 有抗变异能力）
- 失败的 mutant 行可点开看 mutation diff

---

## 类别 D — R-Case 自动复现（论文核心）

### UC-D1 R-Case 自动复现 service 跑通

```bash
dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~RCaseReproductionServiceTests"
```

✅ 期望：Passed ≥ 9, Failed 0。详情见 [sample-data/uat-rcase-spec.json](sample-data/uat-rcase-spec.json)。

---

### UC-D2 R-Case audit log 落库

UC-D1 内含 fact `WriteAudit_records_r_case_reproduced` 自动覆盖。审计员可在 trx 中 grep 该行确认。

---

## 类别 E — 可视化 & 报表

### UC-E1 Trend Dashboard 页 — 时间序列图

1. 进 **Trend Dashboard** 页
2. 选 metric = `Anomaly Count`，时间窗口 = "最近 4 周"
3. 点 "Refresh"

✅ 期望：

- CartesianChart 显示 4 周折线
- 鼠标 hover 显示每点的 (date, count)
- WoW 变化 / burst 期高亮显示

---

### UC-E2 Coverage Dashboard 页 — 4 维饼图

1. 进 **Coverage Dashboard** 页
2. 点 "Refresh"

✅ 期望：

- 4 个 PieChart：`By Application` / `By MR` / `By Domain` / `By MetaPattern`
- 每图至少有 2 个扇区
- legend 显示百分比

---

### UC-E3 MT Report Generator — Word/Excel/PDF/HTML 四端导出

1. 进 **MT Report Generator** 页
2. 选 scope = `By MR`，时间窗口 = "All"
3. 点 "Generate All"

✅ 期望：

- 生成 4 个文件到 `Documents/MetBench_MTReport/`：
  - `MTTestReport_Word.docx`
  - `MTTestReport_Excel.xlsx`
  - `MTTestReport_Pdf.pdf`
  - `MTTestReport_Html.html`
- 每个文件可用对应工具打开，内容含：报告头 / 测试摘要 / MR 列表 / 结果统计 / 异常列表

---

### UC-E4 HTML 报告页内嵌（WebView2）

接 UC-E3：

1. 在 MT Report Generator 页内点 "View HTML in App"

✅ 期望：WebView2 在页内嵌渲染 HTML 报告，CSS / 表格正确显示。

---

### UC-E5 Dashboard 主页面 — 摘要 cards

1. 进 **Dashboard**（主页）
2. 观察顶部 cards

✅ 期望：4-6 个 card 显示：
`Total MRs` / `Total Executions Today` / `Anomalies This Week` / `Validation Pass Rate` / `Top SUT by traffic` 等。

---

### UC-E6 SystemMT 报告 service（CLI）

```bash
dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~SystemMtReportServiceTests"
```

✅ 期望：Passed ≥ 6, Failed 0。

---

### UC-E7 HtmlSystemMtResultReportRenderer

```bash
dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~HtmlSystemMtResultReport"
```

✅ 期望：Passed > 0, Failed 0。

---

## 类别 F — 持久化 & schema

### UC-F1 DbConfig 三级 override 优先级

```bash
dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~DbConfigTests"
```

✅ 期望：Passed ≥ 5, Failed 0。

---

### UC-F2 MetaPattern 实体 round-trip + Seed

```bash
dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~MetaPatternEntityTests"
```

✅ 期望：Passed ≥ 11, Failed 0。

---

### UC-F3 MRBinding.Status 软删 + 索引

```bash
dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~MRBindingStatusTests"
```

✅ 期望：Passed ≥ 7, Failed 0。

---

### UC-F4 V2 schema 软删 + 迁移

```bash
dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~V2SoftDeleteAndMigrationTests"
```

✅ 期望：Passed ≥ 9, Failed 0。

---

### UC-F5 V2 仓库 DI 注册完整性

```bash
dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~V2RepositoryDIBindingTests"
```

✅ 期望：所有 V2 IXxxRepository 都能从 `AddSystemMtRepositories()` 解析。

---

## 类别 G — 运营 & 性能

### UC-G1 LiteDB Keyset 分页

```bash
dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~KeysetPaginationTests"
```

✅ 期望：Passed ≥ 10, Failed 0。

---

### UC-G2 CI 性能基线（trx + tools/ci_perf_baseline.py）

```bash
dotnet test MetBench_SystemMT.Tests --logger "trx;LogFileName=uat-results.trx"
python3 tools/ci_perf_baseline.py MetBench_SystemMT.Tests/TestResults/uat-results.trx
```

✅ 期望：脚本 exit 0；输出 `Total test wall-time < 120s budget OK`。

---

### UC-G3 多维 burst 检测

```bash
dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~MultiDimBurstDetectionTests"
```

✅ 期望：Passed ≥ 4, Failed 0。

---

### UC-G4 Coverage 4 维报告 service

```bash
dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~CoverageServiceTests"
```

✅ 期望：Passed ≥ 5, Failed 0。

---

### UC-G5 Anomaly 服务 + commonality

```bash
dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~AnomalyServiceTests"
```

✅ 期望：Passed ≥ 8, Failed 0。

---

## 通用：测试执行后的产物归档

```bash
tar czf uat-evidence-$(date +%Y%m%d).tgz \
  MetBench_SystemMT.Tests/TestResults/*.trx \
  docs/uat/acceptance-rubric.md
```

Windows 端额外打包：

```
C:\Users\<you>\Documents\MetBench_MTReport\
%TEMP%\MetBench-UAT-Screenshots\         # 验收员截图存放
```

---

## 用例与代码 / PR 对应矩阵（审计员核对用）

| 用例 | 来源 PR / 代码位置 |
|------|--------------------|
| A1-A8 | 现有 `ApplicationRepository` / `MRManagementViewModel` / MetaPattern PR #34 |
| B1-B9 | 现有 BLL + MTExecutionPage + 新 ReplayResultViewModel |
| C1-C4 | PR #34 (F7) · PR #38 (F14 Pairing) · PR #45 (F12 LLM) |
| D1-D2 | PR #43 (F9 R-Case 复现) |
| E1-E7 | 现有 Trend/Coverage/MTReportGenerator + PR #34 (多维 burst) |
| F1-F5 | PR #37 (F18 DbConfig) · PR #34 (MetaPattern) · PR #35 (F19 Status) |
| G1-G5 | PR #46 (F10 keyset) · PR #38 (F16 CI perf) |
