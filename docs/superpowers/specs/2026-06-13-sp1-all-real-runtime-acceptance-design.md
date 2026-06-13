# SP1 设计：全运行时真实异步跑通 + xUnit 端到端全真跑（0 skip）

日期：2026-06-13

## 0. 上位背景（本 spec 在大目标中的位置）

大目标：为 MetBench 已导入的全部 SUT / MR / 算例 / 变异体建立真实可异步运行的环境，
并让 xUnit + UAT + WPF UI 三层验收全部通过。该目标按运行时家族与交付物分解为 5 个子项目
（SP1-SP5），本 spec 只覆盖 **SP1**，其余各自开 spec：

- **SP1（本文）**：运行时装备 + xUnit 端到端全真跑（0 skip）+ 每运行时一个异步作业测试。
- SP2：变异体 T6 真跑（48 mutants × openmoc/openmc，kill 矩阵）。
- SP3：47 项 UAT rubric 全过。
- SP4：每 SUT/MR WPF 异步页 UI 证据。
- SP5：验收聚合总报告。

## 1. 范围与目标

让 catalog 内全部 **42 MR / 20 SUT** 的端到端测试在**真实运行时**下跑通、**0 skip 0 fail**，
并为三个外部运行时各加一条**异步作业路径**端到端测试，证明异步链路对真实运行时也通。

现状（已核对 `.github/governance/expected-catalog-counts.txt` 与各 `catalog.json`）：

| 运行时 | SUT | MR | 当前 |
|---|---|---|---|
| system（纯 stdlib） | 17 | 32（+ external minmr 6） | ✅ 无条件真跑 |
| scipy | 2 | 4 | ⚠️ `SkippableFact`，缺 scipy 则 skip |
| openmoc | 1 | 3 | ⚠️ skip，需 OpenMOC venv |
| openmc | 1 | 3 | ⚠️ skip，需 OpenMC venv |

SP1 把后三类（10 MR）从 skip 转为真跑。

## 2. 关键约束（决定执行底座）

openmoc/openmc 在 Windows 上没有原生可 `import` 的 python；现有端到端测试用
`Process.Start` 在本机跑 python。因此 `dotnet test` 进程必须运行在一个
openmoc/openmc/scipy 都可导入的环境里。**决策：在容器内跑整套 `dotnet test`**
（用户已确认；CLAUDE.md §8 即为此设计），不把执行路由出去、不改测试用 MCP。

## 3. 架构与数据流

```
docker build -f docker/Dockerfile.runtime docker/  →  metbench-runtime:latest
   （现成文件 = metbench-sut:latest + dotnet-sdk-8.0；镜像已含 openmoc venv +
     openmc venv + apt scipy，无需新装）
        │
docker run --rm -v <repo>:/work -w /work metbench-runtime:latest \
   env METBENCH_OPENMOC_PYTHON=/opt/openmoc-venv/bin/python \
       METBENCH_OPENMC_PYTHON=/opt/openmc-venv/bin/python \
   dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj \
       --logger "trx;LogFileName=sp1-all-real.trx"
        │
   ├─ 42 MR 现有端到端测试（含 scipy/openmoc/openmc 的 SkippableFact）：
   │  容器内 `import scipy/openmoc/openmc` 均成功 → `Skip.IfNot(...)` 条件为真 →
   │  自动从 skip 转真跑。运行时类 0 skipped / 0 failed。
   └─ 3 个新异步作业路径测试（§4）：scipy/openmoc/openmc 各一，容器内真跑。
```

**关键不变量**：现有 `SkippableFact` 的逻辑**不改**——`OpenMocTestPaths.OpenMocImportable()` /
`OpenMcTestPaths.OpenMcImportable()` / scipy 的 importable 检查在容器内为真即自动真跑。
SP1 不放松任何 skip 条件，只提供让它们为真的环境。

scipy 解析：容器 system `python3` 经 apt `python3-scipy` 提供 scipy；scipy 类 SUT 的
`python_executable_kind=scipy` 经 launcher 的 `ResolvePythonExecutable` 回退到可用 python
（实测以 plan 阶段确认的环境变量 / 默认解析为准）。

## 4. 新增异步作业路径测试（每运行时一个）

新文件 `MetBench_SystemMT.Tests/SystemMT/Jobs/LauncherAsyncJobRuntimeTests.cs`，
镜像同目录 `MinimumMrSubsetBGroupAsyncJobTests.cs` 的现成接线
（`InMemoryJobStore` + `ChannelJobQueue` + `SystemMtJobService` +
`SystemMtJobWorker(store, SystemMtAsyncPipeline(launcher))`）：

- 提交 `SubmitAsync(new SystemMtJobRequest(mrId))` → `queue.DequeueAsync` →
  `worker.RunJobAsync` → 断言 `State == Succeeded`、`ProgressPercent == 100`、
  `FailureReason == null`、`GetResultAsync != null` 且 `result.Passed`。
- 每个测试是 `[SkippableFact]`，门控复用对应 `*TestPaths.Importable()`：容器内为真→真跑，
  CI（无 venv）→ skip。launcher 用对应运行时 python 构建（`OpenMocPython` /
  `OpenMcPython` / scipy python）。
- 代表 MR（取已验证稳定者）：
  - scipy：`scipy-ivp-lv-prey-growth-monotone`
  - openmoc：`openmoc-pincell-nu-sigma-f`
  - openmc：`openmc-pincell-nu-sigma-f`

这三条新测试随代码进仓，**CI 仍 skip、仍绿**；容器内运行时它们真跑 pass。

## 5. 错误处理

- 容器内 `import` 失败 / venv 缺失：对应 `SkippableFact` 退回 skip（不伪装通过），
  运行摘要如实记录"未达成 0 skip"并报缺失运行时——不静默。
- 异步作业基础设施失败（非 MR 断言）：job 终态 `Failed` + `FailureReason`，测试失败暴露。
- MR 断言失败：`result.Passed == false`，测试失败，按真实业务意图暴露缺陷（不掩盖）。

## 6. 证据与 CI 边界

- **CI 不变**：`.github/workflows/dotnet-test.yml`（ubuntu，不装 venv）那 13 个运行时测试
  （10 现有 + 3 新）继续 skip、继续绿。**本 spec 不改 CI 门禁、不要求 CI 装 venv。**
- **"0 skip 全真跑" 证据**来自**容器内一次性运行**：trx + 运行摘要（skip/pass/fail 计数对照、
  各运行时 import 验证、镜像/venv 版本），归档
  `docs/superpowers/specs/2026-06-13-sp1-all-real-runtime-evidence/`（照三用例 vm-evidence 先例）。
- 容器运行步骤写入 runbook `docs/uat/sp1-all-real-runtime-runbook.md`。

## 7. 交付物 / 不交付

交付：
1. `LauncherAsyncJobRuntimeTests.cs`（3 个异步运行时测试，CI 仍 skip）。
2. `docs/uat/sp1-all-real-runtime-runbook.md`（容器内全真跑步骤）。
3. 一次容器内实跑证据（trx + 摘要）归档至 evidence 目录。
4. 状态账本记 SP1 状态。

不交付：改 CI workflow、CI 端装 venv、放松任何 `Skip.IfNot` 条件、SP2-SP5 内容、
新 SUT/MR、变异体运行。

## 8. 验收判据

1. 容器内整套 `dotnet test MetBench_SystemMT.Tests`：**运行时类（scipy/openmoc/openmc）
   端到端测试 0 skipped / 0 failed**，全套 0 failed（trx 为证）。
2. 3 个新异步作业测试在容器内 pass（`Succeeded` + `MrRunResult` 持久化）。
3. CI 的 `test` job 仍绿（运行时测试在 CI 上仍 skip，符合预期）。
4. 证据归档完整（trx + 摘要 + runbook）。

## 9. Windows Classification

需要"run-and-log"级证据：容器内整套测试真实运行并留 trx/摘要。新增生产代码仅在
cloud-safe `MetBench_SystemMT.Tests`（测试工程），不碰 WPF/`App.xaml.cs`/Windows 配置绑定；
新测试为 `SkippableFact`，CI 与现状一致。容器构建/运行在本机（Windows + Docker Desktop / WSL）。
