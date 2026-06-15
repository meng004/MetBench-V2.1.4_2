# SP3a 设计：UAT rubric 测试支撑类（trx）用例验收

日期：2026-06-15

## 0. 上位背景

大目标"为已导入全部 SUT/MR/算例/变异体建真实可异步运行环境并全部通过验收"的 **SP3a**。
SP3（让 `docs/uat/acceptance-rubric.md` 47 项全过）已分解为：
- **SP3a（本文）**：测试支撑类（trx）22 项——可自动跑 xUnit/脚本、产 .trx 证据。
- SP3b（后续）：WPF UI 类（A1-A7 CRUD / B1-B9 主流程 / C6-C9 发现复核 UI / E2-E5 可视化 / 数据 spot-check / SLA）——FlaUI 驱动 + 截图。
SP1（运行时全真跑）/SP2（变异 kill 矩阵）已合入 main，为本子项目提供运行底座。

## 1. 范围与目标

把 rubric 中 22 个测试支撑类用例真实跑出 pass 计数、按判据如实标 ✅/⚠️/❌、归档 trx，
并就地填写 `acceptance-rubric.md` 这 22 行的「结果/证据」列。

映射（已核实，filter = `--filter "FullyQualifiedName~<Class>"`）：

| 用例 | 判据 | 测试类 / 脚本 | 环境 |
|---|---|---|---|
| A8 | Passed>0 | `MethodMtCatalogCrudTests` | 纯 .NET |
| C1 | ≥4 | `RealSamplerTests` | 纯 .NET |
| C2 | ≥5 | `ValidatorTests`（NullLlmGateway fake） | 纯 .NET |
| C3 | ≥11 | `MRPairingServiceTests` | 纯 .NET |
| C4 | ≥15 | `MultiLlmConsensusValidatorTests`（ScriptedGateway fake） | 纯 .NET |
| C5 | >0 | `ValidationServiceTests` | 纯 .NET |
| C10 | ≥29 + 三类 pattern | `ScgHeuristicDiscovererTests` | 纯 .NET（疑似缺口，见 §3） |
| C11 | OpenMcRunnerSmokeTests=1 + 跨程序 2 openmc scenario | `OpenMcRunnerSmokeTests` + `CrossProgramNeutronTransportMrs` | **容器（openmc）** |
| D1 | ≥9 | `RCaseReproductionServiceTests` | 纯 .NET |
| D2 | fact `WriteAudit_records_r_case_reproduced` | （D1 同类内该 fact） | 纯 .NET |
| E6 | ≥6 | `SystemMtReportServiceTests` | 纯 .NET |
| E7 | >0 | `HtmlSystemMtResultReportRendererTests` | 纯 .NET |
| F1 | ≥5 | `V2DbConfigRegistrationTests` | 纯 .NET（疑似缺口，见 §3） |
| F2 | ≥11 | `MetaPatternEntityTests` | 纯 .NET |
| F3 | ≥7 | `MRBindingStatusTests` | 纯 .NET |
| F4 | ≥9 | `V2SoftDeleteAndMigrationTests` | 纯 .NET |
| F5 | 全 V2 repo 解析 | `V2RepositoryDIBindingTests` | 纯 .NET |
| G1 | ≥10 | `KeysetPaginationTests` | 纯 .NET |
| G2 | exit 0 + total<120s | `tools/ci_perf_baseline.py`（喂套件 trx） | Python 脚本 |
| G4 | ≥5 | `CoverageServiceTests` | 纯 .NET |
| G5 | ≥8 | `AnomalyServiceTests` | 纯 .NET |

（E1/G3 已删除，不计入。）

## 2. 数据流

```
1. host：dotnet test 整套 MetBench_SystemMT.Tests --logger trx（一次产全量 trx，含上面 20 个 .NET 类）
   —— 比逐 filter 跑省时；逐用例 pass 计数从 trx 按类名筛出。
2. C11 容器内复核：metbench-runtime 内同套件（openmc 可导入），确认 OpenMcRunnerSmokeTests +
   CrossProgram 2 个 openmc scenario 真跑 Passed（host 上它们 SkippableFact skip）。
3. G2：python tools/ci_perf_baseline.py --trx <套件 trx> --total-budget-seconds 120 → exit 0。
4. 解析 trx：每个 rubric 用例按其测试类名筛 outcome=Passed/Failed 计数，与判据比对。
5. 填 acceptance-rubric.md 这 22 行「结果」(✅/⚠️/❌) + 「证据」(trx 路径/计数)。
```

trx 解析用一个小脚本 `tools/sp3a_rubric_report.py`：读 trx + 一张"用例→类名→判据"映射表，
输出每用例真实 Passed/Failed 计数与达标判定，供填表与归档（机械、可复核，避免人工数错）。

## 3. 缺口的诚实处理（§4 真实验证 / §6 显式报错）

C10（静态 14 [Fact] < 29）、F1（2 < 5）疑似不达标，但静态 [Fact] 数会低估（[Theory]+InlineData
运行时展开成多条）。流程：
1. **先实测**真实 `dotnet test` 计数（很可能 Theory 展开后已 ≥ 阈值）。
2. 若实测仍 < 阈值且确属**真实覆盖缺口**：补**有意义**的测试达阈值（C10 补 direct-cause/feedback/
   confounding 三类 pattern 各自产 candidate 的验证；F1 补 system/user/test 三级 DbConfig override
   场景），**不写凑数断言**（§4）。
3. 若判定是**阈值陈旧**（suite 重构过，如 C2 已有 "8→5" 先例）：retro-touch 该 rubric 行判据为实测值
   并在行内注明原因（R3）。
两条路径都不伪装：补测试是补真实覆盖，改判据须有重构依据。

## 4. 错误处理

- 某 .NET 类实测 Failed>0：rubric 标 ❌ + 记失败测试名/原因，按真实暴露（不掩盖）。
- C11 容器内仍 skip（openmc 不可用）：报告未达成 C11 并定位，不伪装 Passed。
- G2 total≥120s：标 ❌ + 给 top-10 慢测，不放宽预算掩盖。

## 5. 证据与 CI 边界

- CI 不变：20 个 .NET 类本就在 CI `test` job 跑（绿）；本 PR 若补 C10/F1 真实测试，随 CI 一起跑。
  C11 仍 `SkippableFact`（CI skip、容器内真跑）；G2 是既有脚本。不改 CI 门禁。
- 证据 `docs/superpowers/specs/2026-06-15-sp3a-uat-trx-evidence/`：套件 trx（host + 容器各一）、
  G2 输出、`sp3a_rubric_report.py` 输出（22 用例计数表）、`sp3a-summary.md`。
- 就地填 `docs/uat/acceptance-rubric.md` 这 22 行结果/证据列；UI 类（SP3b）行留空。

## 6. 交付物 / 不交付

交付：(a) C10/F1 真实计数核实 + 必要时补有意义测试（或 retro-touch 陈旧阈值）；
(b) `tools/sp3a_rubric_report.py` + 22 行 rubric 填表 + SP3a UAT 报告 + trx/perf 证据归档；
(c) 状态投影。
不交付：SP3b（全部 WPF UI 类 + 数据 spot-check + SLA 实测）、改 CI 门禁、E1/G3（已删）、
新功能/无关重构。

## 7. Windows Classification

`run-and-log`：host + 容器真实跑测试并留 trx/报告。新增代码仅 cloud-safe（`tools/` python 报告脚本，
+ 必要时 `MetBench_SystemMT.Tests` 补真实测试），不碰 WPF/`App.xaml.cs`/CI 门禁。
