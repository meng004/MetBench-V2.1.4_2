# Windows UAT Round-2 — limeng — 2026-05-19

| 项 | 值 |
|---|---|
| 仓库 commit | `178e694` (main, 含 PR #71/#72/#75) |
| 平台 | Windows 11 Pro ARM (Parallels Desktop on macOS Apple Silicon) |
| .NET SDK | 9.0.306 (project targets net8.0 / net8.0-windows7.0; SDK forward-compatible) |
| 起跑时长 | ~30 分钟 |
| Round-1 → Round-2 scope | 3 个 Major bug fix 回归 (UC-A2 / UC-A5 / UC-B7) + 2 个加跑 (B8 / B9) |

## 通过判定

| UC | 类别 | 通过判定 | 结果 |
|---|---|---|---|
| **UC-A2** | A 管理 CRUD | description-only update + rename-to-unique → `修改记录 成功！`；true-dup 仍 reject (TDD 7/7) | ✅ **PASS** |
| **UC-A5** | A 管理 CRUD | ApplicationEx combo 显示业务 Name、不再显示 `MetBench_*.ApplicationEx` 类名（MR-Mgmt + Discovery 双 sibling check） | ✅ **PASS** |
| **UC-B7** | B 异常流 | factor=0.5 → System MT 失败 run → Anomalies 新增一行 (Severity=minor, Status=new, Category=single-point) | ✅ **PASS** (after fix) |
| **UC-B8** | B 异常流（加跑）| 多选 anomaly → Analyze Commonality 弹 report | ✅ **PASS** |
| **UC-B9** | B 异常流（加跑）| 选 anomaly → Replay → 写新 Result 行 | ✅ **PASS** |

## Round-2 结论

**PASS** — round-1 全部 3 个 Major bug 已在 WPF UI 端到端验证通过；
两个加跑 (UC-B8/B9) 同步通过。

UC-B7 round-1 的初次跑命中 **新 cross-track bug**：
`LiteDbSystemMtResultRepository.SaveAsync` 返回 BSON ObjectId 字符串
（24 hex 字符，例如 `6a0c5df903a05102cba3d4f1`），而 `AnomalyService.RecordAnomalyAsync`
要求 Guid 字符串。issue [#76](https://github.com/meng004/MetBench-V2.1.4_2/issues/76) 详细诊断；
按用户指示在 VM 端直接应用了结构性修复：

- `SystemMtResultRecord.Id` 从 `string` 改为 `Guid`（与 v2 其他 entity 一致）
- `LiteDbSystemMtResultRepository` 改用 `autoId: true`，让 LiteDB 自动生成 Guid
- 新增 ObjectId-string → Guid migration（一次性，幂等），保证 v2.0.x 历史快照仍可读
- 新增 3 个回归测试（SaveAsync Guid 契约 + ObjectId migration + HtmlRenderer fixture 修）

测试结果：`MetBench_SystemMT.Tests` 528/530 ✅（剩余 2 fail 为 KeysetPagination，
与本次修复无关，clean main 上同样 fail，标为 pre-existing）。

**release-v2.1.0 可以 tag** — 等 PR 合并 + CI 绿。

## 评审清单（给后续审阅）

- [x] UC-A2 PASS — 描述-only Update + 改名 Update 都返回 `修改记录 成功！`，TDD 7/7 ✅
- [x] UC-A5 PASS — ApplicationEx / Application combo 显示业务 Name，三个组件三处均通过
- [x] UC-B7 PASS — factor=0.5 失败 run → Anomaly 行自动创建（minor / new / single-point），fix 已 inline 合入本 PR
- [x] UC-B8 PASS — 2 个 anomaly 多选 + Analyze commonality → "2 anomalies analyzed. Dominant severity: minor. Dominant category: single-point."
- [x] UC-B9 PASS — Replay anomaly → SystemMt RecentRuns 行数 +1
- [ ] release-v2.1.0 tag — 等 PR 合并 + CI 绿

## 文件清单

```
round-2-windows-2026-05-19-limeng/
├── README.md                   # ← you are here
├── findings.md                 # 每个 UC 的实测 + screenshot 引用 + UC-B7 fix 叙述
├── cloud-issue-uc-b7.md        # 给 cloud 的 GitHub issue #76 草稿（fix 已 inline 合入本 PR）
├── _uat_helpers.ps1            # UI Automation 辅助函数（拷自 round-1 + 改 EvidenceDir）
├── uc_a2_driver.ps1            # UC-A2 驱动
├── uc_a5_driver.ps1            # UC-A5 驱动
├── uc_a5_probe.ps1             # UC-A5 combo 布局探测脚本
├── uc_b7_driver.ps1            # UC-B7 驱动（失败 run → 自动 anomaly）
├── uc_b8_b9_driver.ps1         # UC-B8/B9 驱动（commonality + replay）
├── dotnet-stdout.log           # MetBench_Client 启动 stdout
└── screenshots/                # ~35 张 PNG
```
