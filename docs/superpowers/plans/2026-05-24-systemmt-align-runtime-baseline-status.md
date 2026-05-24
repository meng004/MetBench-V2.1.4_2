# SystemMT Runtime Alignment & Baseline Solidification — Status

> **状态**: 🟢 已完成并合入 `main`（2026-05-24）
> **PR**: [#95](https://github.com/meng004/MetBench-V2.1.4_2/pull/95)
> **合并提交**: `0e88580` — `feat(systemmt): align runtime catalog and solidify baseline (#95)`
> **执行人**: Codex cloud + Parallels Windows 11

## 1. 本轮目标

本轮只解决三个问题：

1. 对齐核心事实源文档与当前实现。
2. 清理 Stage 8 当前阻塞：catalog fallback、importer 对具体类耦合、sample-level evidence 空壳、基线叙事漂移。
3. 补齐最新 Windows 构建回执，确保 cloud / Windows 两侧结论一致。

## 2. 已完成项

| 类别 | 结果 | 证据 |
|---|---|---|
| 文档对齐 | `AGENTS.md`、`CLAUDE.md`、`docs/PROJECT-STRUCTURE.md`、`docs/requirements.md`、`docs/design/v2-system-mt-architecture.md` 已同步到当前实现 | PR #95 / `0e88580` |
| runtime catalog 收口 | `SystemMtLauncher` 移除生产路径 `HardcodedMrCatalogProvider` fallback，生产侧改为显式 `IMrCatalogProvider` | `MetBench_BLL.Core/SystemMT/Launcher/SystemMtLauncher.cs` |
| importer 去耦 | 新增 `ISystemMtCatalogReader`，`LauncherCatalogV2Importer` 不再依赖 `SystemMtLauncher` 具体类，`App.xaml.cs` 去掉 cast | `MetBench_BLL.Core/SystemMT/Launcher/ISystemMtCatalogReader.cs`、`LauncherCatalogV2Importer.cs`、`MetBench_Client/App.xaml.cs` |
| evidence 闭环 | `ExecutionEvidence.SampleTraces` 开始写入目标字段级 source / transformed / output trace | `MetBench_BLL.Core/SystemMT/Pipeline/SystemMtExecutionRecorder.cs` |
| 回归修复 | 修复 OpenMOC `/var` vs `/private/var` 路径分歧 | `SUT/openmoc/openmoc_output_adapter.py` |
| 基线固化 | 2026-05-24 全量测试 TRX 已入库 | [`docs/uat/reports/round-3-limeng-2026-05-24/baseline-2026-05-24-current.trx`](/Users/limeng/Codes/苏永成-蜕变测试系统代码与文档资料/MetBench-V2.1.4_2/docs/uat/reports/round-3-limeng-2026-05-24/baseline-2026-05-24-current.trx) |
| Windows 回执 | `MetBench_Client.csproj` 在 Windows 11 上构建成功，0 错误，19.78s | PR #95 合并前 VM 验证回执 |

## 3. 验证结果

### 3.1 cloud / macOS

- `dotnet build MetBench_SystemMT.Tests --no-restore -m:1`
- `dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~SystemMtLauncherProviderInjectionTests|FullyQualifiedName~LauncherCatalogV2ImporterTests|FullyQualifiedName~SystemMtExecutionRecorderTests|FullyQualifiedName~ExecutionEvidenceWriteThroughTests" --no-build`
- `dotnet test MetBench_SystemMT.Tests --no-restore --logger "trx;LogFileName=baseline-2026-05-24-current.trx"`

结果：

- focused 回归：`28 tests passed`
- 全量基线：`961 pass / 0 fail / 8 skip / 969 total`

### 3.2 Windows 11 / Parallels

- 分支：`codex/systemmt-align-runtime-baseline-20260524`
- HEAD：`a63abe8`
- `dotnet build MetBench_Client/MetBench_Client.csproj`

结果：

- `0` 个编译错误
- 耗时约 `19.78s`
- `ISystemMtCatalogReader` 文件存在
- `App.xaml.cs` 已去掉 `SystemMtLauncher` 具体类 cast
- `LauncherCatalogV2Importer` 已改依赖接口

## 4. GitHub 合并结果

| 项 | 值 |
|---|---|
| PR | [#95](https://github.com/meng004/MetBench-V2.1.4_2/pull/95) |
| 合并方式 | squash merge |
| 必需检查 | `test` |
| 检查结果 | success |
| 合并时间 | `2026-05-24T07:51:02Z` |
| 远端 `main` | `0e88580` |

## 5. 剩余事项

本轮目标范围内已无未完成项。当前剩余的是后续开发，而不是这轮收口遗留。

建议下一优先级：

1. 继续推进 Stage 8 后续功能，而不是再回头清理本轮已收口项。
2. 若要继续扩 trace 能力，优先把 `SampleTraces` 从“目标字段级”扩到“多变量 / 多路径级”。
3. 若要继续扩 catalog 治理，优先删除仅供历史测试使用的过渡资产，而不是恢复任何生产 fallback。

## 6. 一句话结论

`SystemMT runtime alignment + baseline solidification` 已经完成，并已通过 PR #95 合入 `main`；当前仓库事实源、运行时代码、测试基线和 Windows 回执已重新一致。
