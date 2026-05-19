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
| **UC-B7** | B 异常流 | factor=0.5 → System MT 失败 run → Anomalies 新增一行 (Severity=minor, Status=new, Category=single-point) | ❌ **FAIL — BLOCKED** |
| UC-B8 | B 异常流（加跑）| 多选 anomaly → Analyze Commonality 弹 report | ⏸ N/A — 依赖 B7 |
| UC-B9 | B 异常流（加跑）| 选 anomaly → Replay → 写新 Result 行 | ⏸ N/A — 依赖 B7 |

## Round-2 结论

**CONDITIONAL** — round-1 的两个 Major bug（UC-A2 + UC-A5）已在 WPF UI 端到端验证通过；
但 PR #75 (UC-B7 修) 在生产路径上引入了一个 **新 cross-track bug**：
`LiteDbSystemMtResultRepository.SaveAsync` 返回 BSON ObjectId 字符串
（24 hex 字符，例如 `6a0c5df903a05102cba3d4f1`），而 `AnomalyService.RecordAnomalyAsync`
要求 Guid 字符串 → 任何失败的 System-MT run 都会抛 `ArgumentException`。

PR #75 的单元测试使用 `StubResultRepository`（返回 `Guid.NewGuid().ToString()`），
mask 掉了生产 LiteDB 实现的不兼容。详见 [`findings.md`](findings.md) §UC-B7 与
[issue #76](https://github.com/meng004/MetBench-V2.1.4_2/issues/76) (draft body in [`cloud-issue-uc-b7.md`](cloud-issue-uc-b7.md))。

**release-v2.1.0 tag 未打** — 等 cloud补 PR 修复 UC-B7 后，本机再跑一轮 round-2.5
扫尾 B7/B8/B9 再 tag。

## 评审清单（给后续审阅）

- [x] UC-A2 PASS — 描述-only Update + 改名 Update 都返回 `修改记录 成功！`，TDD 7/7 ✅
- [x] UC-A5 PASS — ApplicationEx / Application combo 显示业务 Name，三个组件三处均通过
- [ ] UC-B7 BLOCKED — cloud补 PR 还没合
- [ ] UC-B8 — 阻塞
- [ ] UC-B9 — 阻塞
- [ ] release-v2.1.0 tag — 阻塞

## 文件清单

```
round-2-windows-2026-05-19-limeng/
├── README.md                   # ← you are here
├── findings.md                 # 每个 UC 的实测 + 现象 + screenshot 引用
├── cloud-issue-uc-b7.md        # 提给 cloud 的 GitHub issue 草稿
├── _uat_helpers.ps1            # UI Automation 辅助函数（拷自 round-1 + 改 EvidenceDir）
├── uc_a2_driver.ps1            # UC-A2 自动化驱动
├── uc_a5_driver.ps1            # UC-A5 自动化驱动
├── uc_a5_probe.ps1             # UC-A5 combo 布局探测脚本（找到正确的 combo）
├── uc_b7_driver.ps1            # UC-B7 自动化驱动（执行到 Run scenario，捕获错误）
├── dotnet-stdout.log           # MetBench_Client 启动 stdout
└── screenshots/                # 26 张 PNG（UC-A2 9 张 + UC-A5 5 张 + UC-B7 8 + DEBUG 1 + step-final 1）
```
