# Windows UAT round-1 操作指导

> **目标**：在 Windows 11 + VS 2022 + WPF 主程序上跑通 **21 个 WPF UI 用例**（Part A1-A7 / B1-B9 / E1-E5）— 即 **真正 Windows-only 的 UAT 范围**。
> **预计工时**：active 1.5–2 小时 + buffer 30 分钟 = **2–2.5 小时**。
> **入口前提**：仓库 main 含 commit `0dc5a42c401a0b5455bd7686ec46e2d82746a24a`（PROJECT-STRUCTURE.md land 后）。
> **本 runbook 写于**：cloud session 2026-05-17，对应 baseline-2026-05-17 reference。

---

## §0 范围裁剪：26 → 21

之前版本的 runbook 把 Part A/B/D/E **全部 26 用例** 拉到 Windows，但里面 **5 个是 CLI**（`dotnet test ...`），云端 baseline-2026-05-17 已 0 fail 跑过。Windows 重跑这 5 个无新增覆盖。

**真正 Windows-only 范围 = 21 个 WPF UI 用例**：

| 类别 | Windows UI 用例 | 数 |
|---|---|---|
| A. 管理 CRUD | A1-A7（**A8 是 CLI，云端覆盖** ⤳） | 7 |
| B. MR 蜕变测试主流程 | B1-B9（全部 WPF UI） | 9 |
| E. 可视化 & 报表 | E1-E5（**E6, E7 是 CLI，云端覆盖** ⤳） | 5 |
| **合计 Windows UI** | | **21** |

### 已云端覆盖的 5 个 CLI 用例（Windows 端跳过）

| UC | 测试套件 | trx 证据 | baseline 结果 |
|---|---|---|---|
| **UC-A8** | `V1CompatibilityTests` + `V2EntityRoundtripTests` + `MetaPatternEntityTests` + `MRBindingStatusTests` | `docs/uat/reports/baseline-2026-05-17/baseline-full.trx` | ✅ 0 fail |
| **UC-D1** | `RCaseReproductionServiceTests` (≥9 facts) | 同上 | ✅ 0 fail |
| **UC-D2** | `WriteAudit_records_r_case_reproduced` fact 命中 | 同上 | ✅ 命中 + Passed |
| **UC-E6** | `SystemMtReportServiceTests` (≥6 facts) | 同上 | ✅ 0 fail |
| **UC-E7** | `HtmlSystemMtResultReport*Tests` (>0 facts) | 同上 | ✅ 0 fail |

**Windows 测试员只需在 evidence 包 `results-summary.md` 把这 5 行标 ✅ + 备注 "cloud baseline-2026-05-17 已覆盖"**，不重跑。如要本地确认，可一次性跑：

```powershell
dotnet test MetBench_SystemMT.Tests --logger "trx;LogFileName=cloud-mirror.trx"
# 期望 521 Pass / 0 Fail（与 cloud baseline 一致）
```

——以上一次跑通就已经把 A8 / D1 / D2 / E6 / E7 全验完。

### Part D 全部云端覆盖

**Part D 整段（UC-D1 + UC-D2）都是 CLI**，Windows 端**整段跳过**，依赖云端 baseline。Windows runbook 不含 D 类章节。

---

## §1 环境准备（一次性，~10 分钟）

### 1.1 软件清单

| 软件 | 版本 | 用途 | 安装来源 |
|---|---|---|---|
| Windows | 11 22H2+ | 主操作系统 | — |
| Visual Studio 2022 | 17.8+ Community 即可 | 跑 WPF + 编译 | https://visualstudio.microsoft.com/ |
| .NET 8.0 SDK | 8.0.x | 编译 + 测试 | VS 安装勾上 ".NET desktop development" + ".NET 8.0 Runtime" |
| Python 3.12 | 3.12.x | 跑 heat_equation + projectile（stdlib + numpy；OpenMOC/OpenMC 不需要） | https://www.python.org/downloads/，勾 "Add to PATH" |
| LiteDB Studio | 最新 | 验 `MR.Litedb` 数据 | https://github.com/mbdavid/LiteDB.Studio/releases |
| Process Monitor | 最新 | （可选）测响应时间 | https://learn.microsoft.com/sysinternals/downloads/procmon |
| Git for Windows | 2.40+ | clone / commit | https://git-scm.com/download/win |

### 1.2 ❌ 无需 OpenMOC / OpenMC venv

OpenMOC + OpenMC 的端到端物理跑动已在 **cloud baseline 4 个 cross-program BDD scenarios** 全 Pass（cumulative 17.6s + 12.6s OpenMC + 2.2s + 2.6s OpenMOC，物理 k_eff ∈ 合理范围）。Windows 端 UAT **不重跑物理**，UI 用例（UC-B2-B6）默认走 **heat_equation** SUT 验 MT 主流程 UI 行为。

若你确实想在 Windows 验 OpenMOC 端到端（论文 reviewer 可能问），加跑：

```powershell
# 可选验证：在 WSL2 Ubuntu 跑 cloud 同款 baseline，确认本地 0 fail
# (然后 baseline trx 直接复用，无需 Windows 重跑物理)
wsl -d Ubuntu -- bash -c "cd /mnt/c/Work/MetBench-V2.1.4_2 && METBENCH_OPENMOC_PYTHON=/opt/openmoc-venv/bin/python METBENCH_OPENMC_PYTHON=/opt/openmc-venv/bin/python dotnet test MetBench_SystemMT.Tests --logger 'trx;LogFileName=wsl-mirror.trx'"
```

但这**不算 Windows-only 责任**，是可选 sanity。

### 1.3 Repo clone + build

```powershell
cd C:\Work
git clone https://github.com/meng004/MetBench-V2.1.4_2.git
cd MetBench-V2.1.4_2
git checkout main
git pull
git rev-parse HEAD     # 记录 commit hash 到证据包元数据
dotnet build MetBench.sln      # 完整 WPF + BLL build，仅 Windows 可
```

期望：`Build succeeded. 0 Error(s)`。

### 1.4 启动 WPF

```powershell
dotnet run --project MetBench_Client
```

或 VS 2022 F5 启动。

**期望**：主窗口 < 5 s 打开；左侧导航 ≥ 10 个页面。

### 1.5 证据包目录

```powershell
$tester = "<your-name>"
$today = Get-Date -Format "yyyy-MM-dd"
$evidence = "C:\Work\MetBench-V2.1.4_2\docs\uat\reports\round-1-$tester-$today"
New-Item -ItemType Directory $evidence
New-Item -ItemType Directory "$evidence\screenshots"
```

---

## §2 跑 21 用例的总策略

```
A1 → A2 → A3 → A4 → A5 → A6 → A7
                              ↓
B1 (Discovery 页) → B2 → B3 → B4 → B5 → B6 → B7 → B8 → B9
                         ↓        ↓
                       (产生 anomaly 数据)
                              ↓
              E1 → E2 → E3 → E4 → E5
```

**按 A → B → E 顺序跑**，前组产数据给后组用。中途**不清 DB**。

### 2.1 截图命名

```
UC-A1-application-created.png
UC-A1-litedb-applications-row.png
UC-B4-progressbar-mid.png
UC-B4-result-panel-ok.png
UC-E3-word-docx-opened.png
```

### 2.2 evidence 包最终结构

```
docs/uat/reports/round-1-<tester>-<date>/
├── README.md                 # 总结 + 与 baseline-2026-05-17 对比
├── results-summary.md        # 26 用例逐行（21 自跑 + 5 cloud 覆盖标 ✅）
├── screenshots/              # ~25-30 张 PNG
├── reports-export/           # E3 4 个文件 (Word/Excel/PDF/HTML)
└── MR.Litedb-snapshot        # 跑完后的 DB copy
```

无 `trx/` 目录 — CLI 用例由 cloud baseline 提供。

---

## §3 Part A — 管理 CRUD（7 个 UI 用例，~20 分钟）

按 [`test-procedures.md` UC-A1~A7](../test-procedures.md#类别-a--管理-crud) 三段式逐项跑。

### 3.1 A1-A3（Application CRUD chain）

- **UC-A1** 新建 `UAT-App-1`（Name + Description + ProgrammingLanguage 填写；**`SoftwareUnderTest` 必填 — 通过 "Upl" 按钮上传 `.py` 文件，再按 "Unzip" 解压**；填完 → Add → 截图 + LiteDB Applications 集合截图）
- **UC-A2** 改 description = `UAT smoke v2`
- **UC-A3** 删除（v2.1 实测为**硬删**，行直接从 DB 移除；早期 runbook 暗示可能是软删 `Status=deleted`，已修正）

### 3.2 A4-A7（Domain / MR / MetaPattern）

- **UC-A4** Domain `Neutronics` — 填 Name + Description → Add → 成功（⚠️ **v2.1 deviation**: 表单无 "Bound Applications" 多选框，Application 绑定功能缺口已列 backlog；另表单标签 "Desciption" 拼写错误，已列 backlog）
- **UC-A5** MR 新建（v2.1 实际表单字段：`Context` / `Granularity` / `Hierarchy` / `InputPattern`\* / `OutputPattern`\* / `DimensionOfInputPattern`\* / `DimensionOfOutputPattern`\* / `ApplicationName`（checkbox 多选）\* / `ArityOfMR`\* / `Operator`\* / `Expression`\*；\* = 必填；截图 + "添加记录 成功" toast）
- **UC-A6** 列表搜索 `Identity` < 500 ms
- **UC-A7** MetaPatterns 8 行（4 active + 4 out-of-scope）

每用例 ≥1 张截图。

> 🟢 **UC-A8 跳过**：cloud baseline-2026-05-17 已覆盖 4 类 CRUD 实体 round-trip + Seed + Status。在 results-summary 标 ✅ + 备注 "cloud baseline 已覆盖"。

---

## §4 Part B — MR 蜕变测试主流程（9 个 UI 用例，~45 分钟）

### 4.1 B1（Discovery 页，amax.py SUT）

UC-B1：进 Discovery 页 → SUT = `amax.py` → Run Discovery → 候选 MR ≥ 1 行 + confidence 字段。

### 4.2 B2-B6（System MT 主链路 — 默认 heat_equation SUT）

> 💡 **关键 scoping**：v2.1 的蜕变测试通过 **System MT 页**（左导航 "System MT"），不是旧版 MT Execution 页。runbook 默认用 **heat_equation** SUT，不需要 OpenMOC venv。
>
> 测试关注的是 WPF UI 行为（进度 spinner / Status / Recent Runs 表格），**不是物理正确性**。物理正确性由 cloud cross-program BDD 4/4 已覆盖。

按链路：
1. **UC-B2** 进 **System MT 页** → Scenario 下拉选 `1D heat equation — ScaleAmplitude (linearity)` → 查看 Description 面板（理论说明文本）；截图
2. **UC-B3** ⚠️ **N/A**：v2.1 System MT 新 UI 不分 "Generate Follow-up" / "Run" 两步，Run 内一次完成；此步跳过
3. **UC-B4** 点 **Run**（factor=2，默认）→ 进度 spinner 旋转 → 约 2–5 s → "Completed" + Recent Runs 表格新增行；截图
4. **UC-B5** Recent Runs 表行字段确认：`Run At` / `Scenario` / `Assertion`（GreaterThan/LessThan）/ `Value`（max_u 或 k_eff）/ `Source`（数值）/ `Follow-up`（数值）/ `Passed`（✓ 或空）；截图
5. **UC-B6** ⚠️ **N/A**：System MT 页面无图表区，结果以 Recent Runs 表格展示；chart hover tooltip 不适用

> 💡 **可选**：如果你想验 OpenMOC 走 System MT UI 路径，选 OpenMOC Scenario → Run 即可。nice-to-have，不算 round-1 必跑。

### 4.3 B7-B9（Anomaly 流程 — 故意造 anomaly）

为了拿到 anomaly，**改 factor=0.5**（默认 2 → 改 0.5 让 GreaterThan 必失败）：
- 重跑 2 次（产生 2 条 anomaly 供 commonality）

然后：
- **UC-B7** Anomaly List 看新增 anomaly 行（Severity / Category / LinkedKnownBug 列）
- **UC-B8** 多选 2+ anomaly → Analyze Commonality
- **UC-B9** 选一个 anomaly → Replay → Reproduced=true/false

每用例截图。

---

## §5 Part E — 可视化 & 报表（5 个 UI 用例，~25 分钟）

E1-E5 依赖前面 A/B 产的数据。**不清 DB**。

### 5.1 E1-E2（两个 Dashboard）

- **UC-E1** Trend Dashboard：`Anomaly Count` × "最近 4 周" → CartesianChart 折线 + hover + WoW 标注
- **UC-E2** Coverage Dashboard：4 个 PieChart，每图 ≥ 2 扇区 + legend 百分比

### 5.2 E3-E4（报告 4 端导出 + WebView2）

- **UC-E3**：进 **MR Report Generator 页** → Report Type 下拉选 `Pdf`（或 `Word` / `Excel` / `Html`，4 项对应 4 端）→ 点 ExportReport → 若有 method-level MR 数据，文件落到 `Documents\MetBench_MTReport\`，**copy 到 `$evidence\reports-export\`**；截图（⚠️ **v2.1 deviation**: 无 "Generate All" 单按钮，无 scope 下拉；4 端需逐一 Export；DB 数据为空时弹 "无目标文件！"，属正常 empty-data 行为）
- **UC-E4** ⚠️ **N/A**：v2.1 MR Report Generator 页无 "View HTML in App" 按钮，WebView2 内嵌入口缺失，功能缺口已列 backlog

### 5.3 E5（Dashboard 主页 cards）

- ⚠️ **N/A**：v2.1 左导航无 "Dashboard 主页" nav 项，主页打开后显示 MR Display 数据网格，不存在 runbook 描述的 Total MRs / Executions Today / Anomalies This Week / Pass Rate card 组件，功能缺口已列 backlog

> 🟢 **UC-E6 / UC-E7 跳过**：cloud baseline 已覆盖 `SystemMtReportServiceTests` + `HtmlSystemMtResultReport*`。在 results-summary 标 ✅ + 备注。

---

## §6 收尾（~15 分钟）

### 6.1 备份 MR.Litedb 快照

```powershell
Copy-Item "C:\Work\MetBench-V2.1.4_2\MetBench_Client\bin\Debug\net8.0-windows7.0\MR.Litedb" "$evidence\MR.Litedb-snapshot"
```

### 6.2 evidence 包 README 模板

`$evidence\README.md`：

```markdown
# Windows UAT Round-1 — <tester> — <date>

| 项 | 值 |
|---|---|
| 仓库 commit | <git rev-parse HEAD> |
| 平台 | Windows 11 22H2 + VS 2022 17.x + .NET 8.0.x |
| WPF 冷启动 | _____ s (期望 < 5 s) |
| 总跑时长 (active) | _____ 小时 _____ 分钟 |
| Windows 自跑用例 | 21 (A1-A7 + B1-B9 + E1-E5) |
| 云端覆盖用例（不重跑） | 5 (A8 / D1 / D2 / E6 / E7) |
| Baseline 引用 | docs/uat/reports/baseline-2026-05-17/baseline-full.trx |

## 结果汇总（21 自跑 + 5 cloud-covered = 26）

| 类别 | Windows UI Pass | Cloud Covered | ⚠️ | ❌ | 备注 |
|---|---|---|---|---|---|
| A. 管理 CRUD (8) | _/7 | 1 (UC-A8) | | | |
| B. MR 主流程 (9) | _/9 | 0 | | | |
| D. R-Case (2) | 0 | 2 (UC-D1, D2) | | | |
| E. 可视化 & 报表 (7) | _/5 | 2 (UC-E6, E7) | | | |
| **合计** | _/21 | 5/5 | | | |

## 偏差说明

（每个 ⚠️ 或 ❌ 的原因 + 截图引用）

## 性能实测（与 baseline 对比）

| 操作 | baseline | 实测 |
|---|---|---|
| WPF 冷启动 | < 5 s | _____ s |
| Application CRUD | < 2 s | _____ s |
| MR 列表搜索 | < 500 ms | _____ ms |
| heat_equation 单次跑 | < 5 s | _____ s |
| 4 端报告导出 | < 30 s | _____ s |
```

### 6.3 results-summary.md 模板

```markdown
| UC | 类别 | 结果 | 备注 | 证据 |
|---|---|---|---|---|
| UC-A1 | A | ✅ | 操作 1.8 s | screenshots/UC-A1-application-created.png |
| UC-A2 | A | ✅ | — | screenshots/UC-A2-edited.png |
...
| UC-A8 | A | ✅ | cloud baseline-2026-05-17 已覆盖 | (cloud trx) |
| UC-B1 | B | ✅ | — | screenshots/UC-B1-discovery-list.png |
...
| UC-D1 | D | ✅ | cloud baseline 已覆盖 | (cloud trx) |
| UC-D2 | D | ✅ | cloud baseline 已覆盖 | (cloud trx) |
...
| UC-E6 | E | ✅ | cloud baseline 已覆盖 | (cloud trx) |
| UC-E7 | E | ✅ | cloud baseline 已覆盖 | (cloud trx) |
```

### 6.4 dashboard.md 加 round-1 行

```markdown
| round-1 | <date> | <commit> | <tester> | Windows UI | __/21 + 5 cloud | __ | __ | __ | <PASS/CONDITIONAL/FAIL> | round-1-<tester>-<date>/ |
```

Commentary：
```
### <date> round-1 Windows
21 个 WPF UI 用例 (A1-A7 + B1-B9 + E1-E5) + 5 个 cloud baseline 覆盖 (A8/D1/D2/E6/E7) = 26/26. 总评 ___. Windows 端用 heat_equation SUT 验 MT 主流程 UI 行为；OpenMOC + OpenMC 端到端物理由 cloud cross-program BDD 4/4 覆盖.
```

### 6.5 提交 + 开 PR

```powershell
cd C:\Work\MetBench-V2.1.4_2
git checkout -b windows-uat-round-1-<tester>-<date>
git add docs/uat/reports/round-1-<tester>-<date>/
git add docs/uat/reports/dashboard.md
git commit -m "uat(round-1 Windows): <tester> <date> — <PASS/CONDITIONAL/FAIL>"
git push -u origin windows-uat-round-1-<tester>-<date>
gh pr create --base main --fill
```

---

## §7 故障排查 cheat sheet

| 症状 | 可能原因 | 处理 |
|---|---|---|
| WPF 不启动 | VS 缺 .NET desktop workload | VS Installer → "修改" → 勾上 |
| Application Management 页空白 | DB 路径错（v1/v2 schema） | LiteDB Studio 看 `MR.Litedb` 是否在 `MetBench_Client/bin/Debug/` 下 |
| B4 heat_equation 跑爆 | numpy 未装 | `pip install numpy` 然后重试 |
| 想验 OpenMOC 走 WPF UI 路径 | nice-to-have | 见 §1.2 WSL2 桥接（**可选**，不影响 round-1） |
| E3 4 端导出缺一个 | NPOI / iText 缺包 | `dotnet restore`，查 csproj 是否含 NPOI / iTextSharp |
| 实测时间远超 baseline | 硬件慢 / 后台占用 | 关其他程序重测；< 2× baseline 算通过 |

---

## §8 与 v2.1 发版的关系

本 round-1 跑通 + dashboard `PASS` 后，加上 cloud-side `baseline-2026-05-17` 已是 100% pass（含全部 5 个 CLI 用例），**v2.1 发版条件成立**：

```powershell
git tag -a release-v2.1.0 -m "MetBench v2.1.0: cloud baseline-2026-05-17 (521/521) + Windows round-1 PASS (21 UI + 5 cloud-covered)"
git push origin release-v2.1.0
```

---

## §9 worst-case 提早撤退条件

若以下任一发生，**立即停跑本轮**，开 issue + 改 dashboard `FAIL`：

- A1 / A2 / A3 / A5 / B2 / B4 任一 Blocker 用例失败
- 跑到一半 WPF crash 且重启复现
- 5 个 cloud-covered 用例在本地复跑出现 fail（说明 Windows 环境异常，超出 round-1 验收范围，应单独 issue）

---

## §10 责任边界总结（Windows vs Cloud）

| 测试类型 | 平台 | 覆盖范围 | 证据位置 |
|---|---|---|---|
| **CLI 单测 / 集成 / BDD smoke** | Cloud | 521 facts (含 5 CLI UAT 用例 + OpenMOC + OpenMC + 30 BDD scenarios + 4 cross-program) | `docs/uat/reports/baseline-2026-05-17/` |
| **WPF UI 用例** | Windows | 21 UAT 用例 (A1-A7 / B1-B9 / E1-E5) | `docs/uat/reports/round-1-<tester>-<date>/` |
| **物理正确性 (OpenMOC / OpenMC k_eff)** | Cloud | 4 cross-program BDD + 2 smoke test | 同 cloud baseline |
| **数据持久化 / Schema migration** | Cloud | LiteDb*Tests + V2Schema/* | 同 cloud baseline |
| **报表生成器 service / Renderer** | Cloud | E6 / E7 单测 | 同 cloud baseline |
| **报表 UI 渲染（Word/Excel/PDF 实际打开）** | Windows | UC-E3 / E4 4 端打开验证 | Windows evidence |
| **WebView2 嵌入** | Windows | UC-E4 | 同 |
| **Chart 可视化 (LiveCharts hover tooltip)** | Windows | UC-B6 / E1 / E2 | 同 |

**Windows 只负责"渲染 + 用户交互 + UI 状态机"**，不重复验云端已覆盖的逻辑。
