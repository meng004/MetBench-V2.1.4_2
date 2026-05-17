# Windows UAT round-1 操作指导

> **目标**：在 Windows 11 + VS 2022 + WPF 主程序上跑通 Part A/B/D/E 共 **26 个 UI 用例**，产出 round-1 验收证据包。
> **预计工时**：active 2 小时 + buffer 1 小时 = **3 小时**。
> **入口前提**：仓库 main 至少包含 commit `9b0a53b66cefd899f6dfd0f57311f3d9bd7d838e`（baseline-2026-05-17 land 后）。
> **本 runbook 写于**：cloud session 2026-05-17，对应 baseline-2026-05-17 reference。

## §1 环境准备（一次性，~30 分钟）

### 1.1 软件清单

| 软件 | 版本 | 用途 | 安装来源 |
|---|---|---|---|
| Windows | 11 22H2+ | 主操作系统 | — |
| Visual Studio 2022 | 17.8+ Community 即可 | 跑 WPF + 编译 | https://visualstudio.microsoft.com/ |
| .NET 8.0 SDK | 8.0.x | 编译 + 测试 | VS 安装时勾上 ".NET desktop development" + ".NET 8.0 Runtime" |
| Python 3.12 | 3.12.x | 跑 SUT runner (OpenMOC / heat_eq / projectile) | https://www.python.org/downloads/ ；勾 "Add to PATH" |
| LiteDB Studio | 最新 | 验 `MR.Litedb` 数据 | https://github.com/mbdavid/LiteDB.Studio/releases |
| Process Monitor | 最新 | （可选）测响应时间 | https://learn.microsoft.com/sysinternals/downloads/procmon |
| Git for Windows | 2.40+ | clone / commit | https://git-scm.com/download/win |

### 1.2 OpenMOC venv（B 类用例需要；可跳过 → 4 个 OpenMOC scenario 会 SKIP）

OpenMOC 在 Windows 上原生编译困难。两条路：

- **路径 A (推荐)**：在 WSL2 Ubuntu 24.04 跑 `bash .claude/web-setup.sh` 安装 `/opt/openmoc-venv`，然后 WPF 通过 `METBENCH_OPENMOC_PYTHON=\\wsl$\Ubuntu\opt\openmoc-venv\bin\python` 桥接。
- **路径 B**：B 类 OpenMOC 用例全跳过（标 ⚠️），论文里诚实说 "Windows 端 OpenMOC 因编译复杂度未上线，Linux baseline 已覆盖"。

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

期望：`Build succeeded. 0 Error(s)`。若失败先排查 VS 2022 是否装了 ".NET desktop development" workload。

### 1.4 启动 WPF

```powershell
dotnet run --project MetBench_Client
```

或在 VS 2022 打开 `MetBench.sln`，把 `MetBench_Client` 设为启动项，F5 启动。

**期望**：主窗口在 < 5 s 内打开；左侧导航有 ≥ 10 个页面（Dashboard / Application Management / Domain Management / MR Management / MetaPatterns / Discovery / MT Execution / Anomaly List / Replay Result / Trend Dashboard / Coverage Dashboard / MT Report Generator）。

### 1.5 证据包目录（手工建）

```powershell
$tester = "<your-name>"
$today = Get-Date -Format "yyyy-MM-dd"
$evidence = "C:\Work\MetBench-V2.1.4_2\docs\uat\reports\round-1-$tester-$today"
New-Item -ItemType Directory $evidence
New-Item -ItemType Directory "$evidence\screenshots"
```

之后所有 trx / 截图 / DB 快照都落到这里。

---

## §2 跑用例的总策略

26 用例分四组，**有依赖**：

```
A1 → A2 → A3 → A4 → A5 → A6 → A7 → A8
                              ↓
                              (B1 用 amax.py SUT)
                              ↓
B1 → B2 → B3 → B4 → B5 → B6 → B7 → B8 → B9
                         ↓        ↓
                       (产生 anomaly 给 D)
                              ↓
                              D1 → D2
                              ↓
              (有数据后 E 才有意义)
                              ↓
              E1 → E2 → E3 → E4 → E5 → E6 → E7
```

**强烈建议按 A → B → D → E 顺序跑**，前组产数据给后组用。中途**不要清 DB**（否则后面 E 类的可视化没数据可看）。

### 2.1 截图命名约定

每个 UI 用例至少 1 张截图，存到 `screenshots/`：

```
UC-A1-application-created.png        # 用例编号 + 关键状态描述
UC-A1-litedb-applications-row.png    # 同一用例多张就 -1 / -2 / ...
UC-B4-progressbar-mid.png
UC-B4-result-panel-ok.png
UC-E3-word-docx-opened.png
...
```

### 2.2 evidence 包最终结构（提交时）

```
docs/uat/reports/round-1-<tester>-<date>/
├── README.md              # 你写的总结 + 与 baseline-2026-05-17 对比
├── results-summary.md     # 26 用例逐行通过状态 (✅/⚠️/❌ + 备注)
├── screenshots/           # 各 UC 截图，命名见上
│   ├── UC-A1-*.png
│   ├── UC-A2-*.png
│   └── ...
├── trx/                   # CLI 用例的 trx 文件（A8 / E6 / E7 / D1 / D2）
│   ├── uc-a8.trx
│   ├── uc-d1.trx
│   ├── uc-e6.trx
│   └── uc-e7.trx
├── reports-export/        # E3 生成的 4 个文件
│   ├── MTTestReport_Word.docx
│   ├── MTTestReport_Excel.xlsx
│   ├── MTTestReport_Pdf.pdf
│   └── MTTestReport_Html.html
└── MR.Litedb-snapshot     # 跑完后整个 DB 的 copy（验数据完整性）
```

---

## §3 Part A — 管理 CRUD（8 用例，~24 分钟）

参考详细步骤见 [test-procedures.md UC-A1~A8](../test-procedures.md#类别-a--管理-crud)，本节只列**新增 / 简化**。

### 3.1 A1-A3（Application CRUD）

按 test-procedures **三段式**逐项跑：
- UC-A1 新建 `UAT-App-1`
- UC-A2 改 description
- UC-A3 删除

每步**截图 + 用 LiteDB Studio 截 Applications 集合的当前行**作为证据。

> ⚠ **重要**：A3 删除后**不要重启**，立即跑 A4（A4 会重新建 Application）。或先做 A4 / A5 再回头 A3 — 顺序不强制，只要每个用例的初始条件满足。

### 3.2 A4-A7（Domain / MR / MetaPattern）

- UC-A4 Domain `Neutronics` + 绑 App
- UC-A5 MR `UAT-Identity-MR`
- UC-A6 列表筛选 / 搜索（用秒表测响应 < 500 ms）
- UC-A7 MetaPatterns 8 行（4 active + 4 out-of-scope）

每个用例至少 1 张截图。

### 3.3 A8（CLI 用例）

```powershell
dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~V1CompatibilityTests|FullyQualifiedName~V2EntityRoundtripTests|FullyQualifiedName~MetaPatternEntityTests|FullyQualifiedName~MRBindingStatusTests" --logger "trx;LogFileName=$evidence\trx\uc-a8.trx"
```

**断言**：trx 含 Passed > 0, Failed = 0。

---

## §4 Part B — MR 蜕变测试主流程（9 用例，~45 分钟）

### 4.1 B1（Discovery 页）

UC-B1：选 SUT = `amax.py`，点 Run Discovery → 候选 MR 列表 ≥ 1 行 + 截图。

### 4.2 B2-B6（System-MT 主流程，OpenMOC）

> ⚠ 若 OpenMOC venv 未上（§1.2 路径 B），B2-B6 全部 ⚠️ 跳过；下面假设 venv OK。

按链路：
1. UC-B2 选 MR `ScaleNuSigmaF` + sample `pincell.json`
2. UC-B3 Generate Follow-up（< 1 s，截 followup JSON）
3. UC-B4 Run（30-60 s，截进度条 mid + 结束状态）
4. UC-B5 Result 面板 6 个字段（source/follow-up k_eff / passed / Δ / threshold / reason）
5. UC-B6 chart（CartesianChart + PieChart + hover tooltip）

> 💡 **小贴士**：B4 跑的时候用 PowerShell 另开窗口 `Get-Date` 记开始时间，结束时再看，算总时长，写进 README perf 段。

### 4.3 B7-B9（Anomaly 流程）

为了拿到 anomaly，**故意把 B4 的 factor 改成 0.5**（默认 1.5 → 改成 0.5 让 GreaterThan 必失败）：
- 在 MT Execution 页 factor 输入框改 `0.5`
- 点 Run → Status 显示 `anomaly`（红底）

然后：
- UC-B7 Anomaly List 看新增 anomaly 行
- UC-B8 多选 2+ anomaly（先把 factor=0.5 重跑 2 次）→ Analyze Commonality
- UC-B9 选一个 anomaly → Replay

每个用例**截图 + 记 Reproduced=true/false**。

---

## §5 Part D — R-Case 自动复现（2 用例，~10 分钟）

### 5.1 D1（CLI 测试）

```powershell
dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~RCaseReproductionServiceTests" --logger "trx;LogFileName=$evidence\trx\uc-d1.trx"
```

**断言**：trx 含 Passed ≥ 9, Failed = 0。

### 5.2 D2（meta 检查）

```powershell
Select-String -Path "$evidence\trx\uc-d1.trx" -Pattern "WriteAudit_records_r_case_reproduced"
```

**断言**：grep 命中且 outcome=Passed。

> 💡 加分项：用 LiteDB Studio 打开 `MR.Litedb` 看 `AuditLog` 集合是否含 `r-case.reproduced` 类型的行（B9 跑成功后会有）。

---

## §6 Part E — 可视化 & 报表（7 用例，~35 分钟）

E1-E5 依赖前面 A/B/D 已产生数据。**不要清 DB**。

### 6.1 E1-E2（两个 Dashboard）

- UC-E1 Trend Dashboard：选 `Anomaly Count` × "最近 4 周" → CartesianChart 折线 + hover tooltip + WoW 标注
- UC-E2 Coverage Dashboard：4 个 PieChart，每图 ≥ 2 扇区，legend 显示百分比

**截图**：每个 dashboard 各 1 张全屏。

### 6.2 E3-E4（报告导出 + WebView2）

- UC-E3：选 scope = By MR，点 Generate All
- **导出后从 `Documents\MetBench_MTReport\` 把 4 个文件 (Word/Excel/PDF/HTML) copy 到 `$evidence\reports-export\`**
- 每个文件打开看一眼 → 内容包含报告头 / 摘要 / MR 列表 / 异常列表，4 个都 OK = E3 ✅
- UC-E4：在 MT Report Generator 页内点 "View HTML in App" → WebView2 渲染正确 → 截图

### 6.3 E5（Dashboard 主页 cards）

回 Dashboard 主页，看顶部 4-6 个 card → 截图。数值有意义（不是全 0）。

### 6.4 E6-E7（CLI）

```powershell
dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~SystemMtReportServiceTests" --logger "trx;LogFileName=$evidence\trx\uc-e6.trx"
dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~HtmlSystemMtResultReport" --logger "trx;LogFileName=$evidence\trx\uc-e7.trx"
```

**断言**：E6 Passed ≥ 6, E7 Passed > 0，都 Failed = 0。

---

## §7 收尾（~15 分钟）

### 7.1 备份 MR.Litedb 快照

```powershell
Copy-Item "$env:LOCALAPPDATA\..\..\..\Work\MetBench-V2.1.4_2\MetBench_Client\bin\Debug\net8.0-windows7.0\MR.Litedb" "$evidence\MR.Litedb-snapshot"
# 路径取决于你 dotnet run 时 MR.Litedb 落在哪；可用 Process Monitor 查实际路径
```

### 7.2 写 evidence 包 README

模板 `$evidence\README.md`：

```markdown
# Windows UAT Round-1 — <tester> — <date>

| 项 | 值 |
|---|---|
| 仓库 commit | <git rev-parse HEAD 输出> |
| 平台 | Windows 11 22H2 + VS 2022 17.x + .NET 8.0.x |
| WPF 冷启动 | _____ s（应 < 5 s） |
| OpenMOC venv | ✅ (WSL2 桥接) / ❌ (跳过 B2-B6) |
| LLM API | not exercised |
| 总跑时长（active） | _____ 小时 _____ 分钟 |

## 结果汇总（对比 baseline-2026-05-17）

| 类别 | Pass | ⚠️ | ❌ | 备注 |
|---|---|---|---|---|
| A. 管理 CRUD (8) | _/8 | | | |
| B. MR 主流程 (9) | _/9 | | | |
| D. R-Case (2) | _/2 | | | |
| E. 可视化 & 报表 (7) | _/7 | | | |
| **合计** | _/26 | | | |

## 偏差说明

（列每个 ⚠️ 或 ❌ 的具体原因 + 截图引用）

## 性能实测（与 baseline 对比）

| 操作 | baseline | 实测 |
|---|---|---|
| WPF 冷启动 | < 5 s | _____ s |
| Application CRUD | < 2 s | _____ s |
| MR 列表搜索 | < 500 ms | _____ ms |
| OpenMOC 单次跑 | < 90 s | _____ s |
| 4 端报告导出 | < 30 s | _____ s |
```

### 7.3 写 results-summary.md

模板 `$evidence\results-summary.md`：每个 UC 一行：

```markdown
| UC | 类别 | 结果 | 备注 | 证据 |
|---|---|---|---|---|
| UC-A1 | A | ✅ | 操作 1.8 s | screenshots/UC-A1-application-created.png |
| UC-A2 | A | ✅ | — | screenshots/UC-A2-edited.png |
| UC-A3 | A | ⚠️ | DB 软删而非硬删（v2 schema 行为）| screenshots/UC-A3-soft-deleted.png |
...
```

### 7.4 在仓里 dashboard.md 加 round-1 行

打开 `docs/uat/reports/dashboard.md`，在 baseline-2 行下加：

```markdown
| round-1 | <date> | <commit> | <tester> | Windows | __/26 | __ | __ | __ | <PASS/CONDITIONAL/FAIL> | round-1-<tester>-<date>/ |
```

并在 Commentary 段加 2-3 句总结。

### 7.5 提交 + 开 PR

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

## §8 故障排查 cheat sheet

| 症状 | 可能原因 | 处理 |
|---|---|---|
| WPF 不启动 | VS 缺 .NET desktop workload | VS Installer → "修改" → 勾上 |
| Application Management 页空白 | DB 路径错（v1/v2 schema） | LiteDB Studio 看 `MR.Litedb` 是否在 `MetBench_Client/bin/Debug/` 下 |
| B4 OpenMOC 跑爆 | venv 不通 | 见 §1.2，跳过 B2-B6 |
| LLM API 失败 | UC-C4 默认走 fake gateway，**不影响 UAT** | — |
| E3 4 端导出缺一个 | NPOI / iText 缺包 | `dotnet restore`；查 csproj 是否含 NPOI / iTextSharp |
| CLI 测试 KeysetPagination 5 个 fail | `DbConfig.Instance` flake | 应该已在 PR #64 修复 (`[Collection("DbConfigGlobal")]`)；若仍现说明 commit 不含 #64，pull 最新 |
| 实测时间远超 baseline | 硬件慢 / 后台占用 | 关其他程序重测；< 2× baseline 算通过 |

---

## §9 与 v2.1 发版的关系

本 round-1 跑通 + dashboard `PASS` 后，加上 cloud-side `baseline-2026-05-17` 已是 100% pass，**v2.1 发版条件成立**：

```powershell
git tag -a release-v2.1.0 -m "MetBench v2.1.0 release: cloud baseline-2026-05-17 (521/521) + Windows round-1 PASS"
git push origin release-v2.1.0
```

---

## §10 worst-case 提早撤退条件

若以下任一发生，**立即停跑本轮**，开 issue + 改 dashboard `FAIL`：

- A1 / A2 / A3 / A5 / B2 / B4 任一 Blocker 用例失败
- 任一 Linux baseline 已 pass 的 CLI 用例（A8 / D1 / E6 / E7）在 Windows 上 Failed > 0
- 跑到一半 WPF crash 且重启复现
