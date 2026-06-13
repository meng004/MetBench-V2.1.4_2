# SP1 全运行时真实异步跑通 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**状态**: 完成（Task 1-4 全交付；容器内全真跑 1895 passed / 0 failed，运行时类 0 skip，2026-06-13）
**Spec**: `docs/superpowers/specs/2026-06-13-sp1-all-real-runtime-acceptance-design.md`
**分支**: `sp1-all-real-runtime-acceptance`（已存在，spec 已提交 `3200906`）

**Goal:** 让 catalog 全部 42 MR 的端到端测试在容器内真实运行时下 0 skip 0 fail，并为 scipy/openmoc/openmc 各加一条异步作业路径端到端测试。

**Architecture:** 用现成 `docker/Dockerfile.runtime`（= `metbench-sut:latest` + .NET 8 SDK，镜像已含 openmoc venv + openmc venv + apt scipy）构建 `metbench-runtime:latest`，在容器内跑整套 `dotnet test`；现有 10 个运行时 `SkippableFact` 的 `Skip.IfNot` 条件在容器内为真→自动转真跑；另加 3 个异步作业测试。CI 不变（仍 skip 仍绿）。

**Tech Stack:** .NET 8 xUnit（SkippableFact）、Docker（metbench-runtime 镜像）、容器内 dotnet test。

**执行约定：**
- 环境：本机 Windows，`dotnet` 在 `"C:\Program Files\dotnet\dotnet.exe"`；Docker Desktop 已装且引擎运行（`docker` CLI 在 `"C:\Program Files\Docker\Docker\resources\bin\docker.exe"`，未在 PATH 时用全路径或先 `$env:Path` 注入）。镜像 `metbench-sut:latest` 已在 Docker Desktop 引擎内。
- 提交信息末尾加：`Co-Authored-By: Claude <noreply@anthropic.com>`（作者 meng004）。
- §0.5 最小修改：只改各 Task 列出的文件。
- PowerShell 不支持 heredoc；多行 commit message 写入 `.git/COMMIT_MSG.txt` 后 `git commit -F`，用完删除。

---

## File Structure

| 文件 | 动作 | 职责 |
|---|---|---|
| `MetBench_SystemMT.Tests/SystemMT/Jobs/LauncherAsyncJobRuntimeTests.cs` | Create | 3 个异步作业路径运行时测试（scipy/openmoc/openmc 各一，SkippableFact） |
| `docs/uat/sp1-all-real-runtime-runbook.md` | Create | 容器内全真跑步骤 + 证据采集 |
| `docs/superpowers/specs/2026-06-13-sp1-all-real-runtime-evidence/` | Create（运行后） | trx + 运行摘要 |
| `docs/status/current.md` | Modify | SP1 状态行 |
| `docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md` | Modify | 登记本 plan |

依赖事实（已核实，subagent 落笔前可复读确认）：
- scipy 解析：`ScipyTestPaths.ScipyPython()`（`METBENCH_SCIPY_PYTHON` 或 `TestAssetPaths.PythonExecutable()`）+ `ScipyTestPaths.ScipyImportable()`（跑 `-c "import scipy.integrate"`）。`LauncherOptions` 有 `ScipyPython` 具名参数（见 `LauncherEndToEndScipyIvpLotkaVolterraTests.cs:33-37`）。
- openmoc：`OpenMocTestPaths.OpenMocPython()` / `OpenMocImportable()`；`LauncherOptions(OpenMocPython:...)`。
- openmc：`OpenMcTestPaths.OpenMcPython()` / `OpenMcImportable()`；`LauncherOptions(OpenMcPython:...)`。
- 异步作业接线（`MinimumMrSubsetBGroupAsyncJobTests.cs:21-45`）：`new SystemMtJobService(store, queue)` → `SubmitAsync(new SystemMtJobRequest(mrId), default)` → `queue.DequeueAsync(default)` → `new SystemMtJobWorker(store, new SystemMtAsyncPipeline(launcher))` → `worker.RunJobAsync(id, default)` → `service.GetStatusAsync(id, default)`（`.State`/`.ProgressPercent`/`.FailureReason`）+ `service.GetResultAsync(id, default)`（`.Passed`/`.MrId`）。
- 镜像内路径：`/opt/openmoc-venv/bin/python`、`/opt/openmc-venv/bin/python`；scipy 经容器 `python3`（apt `python3-scipy`）。

---

## Task 1: 新增 3 个异步作业路径运行时测试

**Files:**
- Create: `MetBench_SystemMT.Tests/SystemMT/Jobs/LauncherAsyncJobRuntimeTests.cs`

- [ ] **Step 1: 写测试**（先读 `MetBench_SystemMT.Tests/SystemMT/Jobs/MinimumMrSubsetBGroupAsyncJobTests.cs`、`LauncherEndToEndScipyIvpLotkaVolterraTests.cs`、`OpenMocTestPaths.cs`、`OpenMcTestPaths.cs` 确认签名，再落笔）：

```csharp
using MetBench_BLL.SystemMT.Catalog;
using MetBench_BLL.SystemMT.Jobs;
using MetBench_BLL.SystemMT.Launcher;
using MetBench_BLL.SystemMT.Pipeline;
using MetBench_SystemMT.Tests.V2Anomaly;
using MetBench_SystemMT.Tests.V2Pipeline;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Jobs;

/// <summary>
/// SP1: async job path (SystemMtJobService → SystemMtJobWorker → SystemMtAsyncPipeline
/// → SystemMtLauncher) exercised against the REAL external runtimes (scipy / openmoc /
/// openmc). Each test is gated by the same importable check as the sync end-to-end
/// tests, so it skips cleanly on CI (no venv) and runs for real inside metbench-runtime.
/// Proves the async chain works on real runtimes, not just the sync RunAsync path.
/// </summary>
public sealed class LauncherAsyncJobRuntimeTests
{
    [SkippableFact]
    public async Task Async_job_runs_scipy_mr_end_to_end()
    {
        Skip.IfNot(ScipyTestPaths.ScipyImportable(),
            "SciPy runtime not configured for scipy async job test.");
        var options = new LauncherOptions(
            SutRoot: TestAssetPaths.AssetRoot(),
            SystemPython: TestAssetPaths.PythonExecutable(),
            OpenMocPython: TestAssetPaths.PythonExecutable(),
            ScipyPython: ScipyTestPaths.ScipyPython());
        await RunAsyncJobAndAssertSucceeded("scipy-ivp-lv-prey-growth-monotone", options);
    }

    [SkippableFact]
    public async Task Async_job_runs_openmoc_mr_end_to_end()
    {
        Skip.IfNot(OpenMocTestPaths.OpenMocImportable(),
            "OpenMOC runtime not importable for openmoc async job test.");
        var options = new LauncherOptions(
            SutRoot: TestAssetPaths.AssetRoot(),
            SystemPython: TestAssetPaths.PythonExecutable(),
            OpenMocPython: OpenMocTestPaths.OpenMocPython());
        await RunAsyncJobAndAssertSucceeded("openmoc-pincell-nu-sigma-f", options);
    }

    [SkippableFact]
    public async Task Async_job_runs_openmc_mr_end_to_end()
    {
        Skip.IfNot(OpenMcTestPaths.OpenMcImportable(),
            "OpenMC runtime not importable for openmc async job test.");
        var options = new LauncherOptions(
            SutRoot: TestAssetPaths.AssetRoot(),
            SystemPython: TestAssetPaths.PythonExecutable(),
            OpenMocPython: TestAssetPaths.PythonExecutable(),
            OpenMcPython: OpenMcTestPaths.OpenMcPython());
        await RunAsyncJobAndAssertSucceeded("openmc-pincell-nu-sigma-f", options);
    }

    private static async Task RunAsyncJobAndAssertSucceeded(string mrId, LauncherOptions options)
    {
        var launcher = new SystemMtLauncher(
            options,
            new SystemMtPipeline(),
            new SystemMtExecutionRecorder(new FakeExecRepo(), new FakeResultRepo()),
            new RecordingAnomalyService(),
            new ManifestMrCatalogProvider(options));

        var store = new InMemoryJobStore();
        var queue = new ChannelJobQueue();
        var service = new SystemMtJobService(store, queue);
        var worker = new SystemMtJobWorker(store, new SystemMtAsyncPipeline(launcher));

        var handle = await service.SubmitAsync(new SystemMtJobRequest(mrId), default);
        var queuedId = await queue.DequeueAsync(default);
        Assert.Equal(handle.JobId, queuedId);

        await worker.RunJobAsync(queuedId, default);

        var status = await service.GetStatusAsync(handle.JobId, default);
        var result = await service.GetResultAsync(handle.JobId, default);

        Assert.NotNull(status);
        Assert.Equal(SystemMtJobState.Succeeded, status!.State);
        Assert.Equal(100, status.ProgressPercent);
        Assert.Null(status.FailureReason);

        Assert.NotNull(result);
        Assert.True(result!.Passed, result.FailureReason);
        Assert.Equal(mrId, result.MrId);
    }
}
```

注意：若 `LauncherOptions` 的具名参数名与实际不符（如 `OpenMcPython` vs `OpenMcPython`），以 `LauncherOptions.cs` 实际定义为准微调；`SystemMtAsyncPipeline` 构造若需第二参数（evidence repo），照 `MinimumMrSubsetBGroupAsyncJobTests` 实际写法（该文件用单参 `new SystemMtAsyncPipeline(launcher)`，照抄）。

- [ ] **Step 2: 跑测试确认 skip（host 行为 = 红的代理）**

Run（host，无 openmoc/openmc venv）:
```
& "C:\Program Files\dotnet\dotnet.exe" test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~LauncherAsyncJobRuntimeTests"
```
Expected: 编译通过；3 个测试中 openmoc/openmc **skipped**（host 无 venv），scipy 视本机是否装 scipy 而 pass 或 skip。**0 failed**。这是"门控正确"的证据（无运行时即 skip，不伪装）。若编译失败→按报错修签名。

- [ ] **Step 3: 提交**

`.git/COMMIT_MSG.txt`:
```
test(sp1): add async-job-path end-to-end tests for scipy/openmoc/openmc

Exercises SystemMtJobService -> Worker -> SystemMtAsyncPipeline -> launcher
against the real external runtimes. SkippableFact-gated like the sync
end-to-end tests, so CI keeps skipping; runs for real inside
metbench-runtime where the venvs are importable.

Co-Authored-By: Claude <noreply@anthropic.com>
```
```
git add MetBench_SystemMT.Tests/SystemMT/Jobs/LauncherAsyncJobRuntimeTests.cs
git commit -F .git/COMMIT_MSG.txt
```
删除 `.git/COMMIT_MSG.txt`。

---

## Task 2: 容器内全真跑 runbook

**Files:**
- Create: `docs/uat/sp1-all-real-runtime-runbook.md`

- [ ] **Step 1: 写 runbook**，含逐字命令：

1. **前置**：Docker Desktop 引擎运行；`metbench-sut:latest` 已在引擎内（`docker images metbench-sut:latest`）。
2. **构建 runtime 镜像**（一次性，~250MB 叠加）：
   ```
   docker build -t metbench-runtime:latest -f docker/Dockerfile.runtime docker/
   ```
3. **容器内跑整套测试**（挂载仓库源码，设运行时环境变量）：
   ```
   docker run --rm -v "<repo-abs>:/work" -w /work metbench-runtime:latest `
     env METBENCH_OPENMOC_PYTHON=/opt/openmoc-venv/bin/python `
         METBENCH_OPENMC_PYTHON=/opt/openmc-venv/bin/python `
         METBENCH_SCIPY_PYTHON=/opt/openmc-venv/bin/python `
     dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj `
       --logger "trx;LogFileName=sp1-all-real.trx"
   ```
   （注：scipy 经 openmc venv 的 system-site-packages 提供，或容器 `python3`；以容器内 `import scipy.integrate` 成功为准，runbook 注明若 `METBENCH_SCIPY_PYTHON` 指向的 python 无 scipy 则改指 `python3`。）
4. **通过判据**：输出 `Passed: N, Failed: 0, Skipped: 0`（运行时类 0 skip）。逐项确认 scipy/openmoc/openmc 端到端测试与 3 个新异步测试均在 Passed 内、不在 Skipped 内。
5. **采集证据**：从容器内 `TestResults/sp1-all-real.trx` 取出（挂载目录可直接在 host 看到），连同运行摘要（总数/passed/failed/skipped、各 venv `import` 验证、镜像 digest）归档。

- [ ] **Step 2: 提交**

`.git/COMMIT_MSG.txt`:
```
docs(uat): add SP1 all-real-runtime container runbook

Co-Authored-By: Claude <noreply@anthropic.com>
```
```
git add docs/uat/sp1-all-real-runtime-runbook.md
git commit -F .git/COMMIT_MSG.txt
```

---

## Task 3: 执行容器内全真跑并归档证据

**Files:**
- Create: `docs/superpowers/specs/2026-06-13-sp1-all-real-runtime-evidence/` 下 `sp1-summary.md` + `sp1-all-real.trx`

- [ ] **Step 1: 构建 metbench-runtime 镜像**

Run（docker 全路径或注入 PATH）:
```
& "C:\Program Files\Docker\Docker\resources\bin\docker.exe" build -t metbench-runtime:latest -f docker/Dockerfile.runtime docker/
```
Expected: 成功；`dotnet --info` 输出在构建日志尾部（Dockerfile.runtime 末尾有验证）。

- [ ] **Step 2: 容器内跑整套测试**

Run（`<repo-abs>` = `D:\Codes\MetBench-V2.1.4_2`）:
```
& "C:\Program Files\Docker\Docker\resources\bin\docker.exe" run --rm -v "D:\Codes\MetBench-V2.1.4_2:/work" -w /work metbench-runtime:latest env METBENCH_OPENMOC_PYTHON=/opt/openmoc-venv/bin/python METBENCH_OPENMC_PYTHON=/opt/openmc-venv/bin/python METBENCH_SCIPY_PYTHON=/opt/openmc-venv/bin/python dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --logger "trx;LogFileName=sp1-all-real.trx"
```
Expected: `Passed: N, Failed: 0, Skipped: 0`（运行时类 0 skip）。
- 若某运行时仍 skip：读 trx / 容器内 `import` 验证定位缺失（如 scipy python 选错），按 runbook 调 `METBENCH_SCIPY_PYTHON`，**显式报告**未达成 0 skip 及原因，重跑——不伪装通过。
- 若有 failed：按真实业务暴露，记录 MR id + FailureReason，不掩盖。

- [ ] **Step 3: 归档证据**

把容器产出的 `MetBench_SystemMT.Tests/TestResults/sp1-all-real.trx` 复制到 evidence 目录，并写 `sp1-summary.md`：
- 总数 / passed / failed / skipped 计数；
- scipy/openmoc/openmc 三类端到端测试 + 3 个新异步测试逐一列"真跑通"（从 trx 摘出）；
- 容器内 `import scipy.integrate` / `import openmoc` / `import openmc` 验证输出；
- `metbench-runtime` 镜像 digest、构建时间；
- 偏离（如有，例如 scipy python 来源）如实记录。

- [ ] **Step 4: 提交**

`.git/COMMIT_MSG.txt`:
```
docs(evidence): SP1 all-real-runtime container run (0 skip)

Ran the full MetBench_SystemMT.Tests suite inside metbench-runtime with
openmoc/openmc/scipy importable: runtime-gated end-to-end tests and the
3 new async-job tests all run for real (0 skipped, 0 failed). trx +
summary archived.

Co-Authored-By: Claude <noreply@anthropic.com>
```
```
git add docs/superpowers/specs/2026-06-13-sp1-all-real-runtime-evidence/
git commit -F .git/COMMIT_MSG.txt
```

---

## Task 4: 状态投影

**Files:**
- Modify: `docs/status/current.md`（新增一行：SP1 全运行时真跑 —— 状态、引 spec/plan/evidence）
- Modify: `docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md`（登记本 plan）
- Modify: 本 plan「状态」字段 → 完成

- [ ] **Step 1: 三处更新**（指针互引，不复制结论）。`current.md` 行内容示例：
  `| SP1 全运行时真实异步跑通（0 skip） | 实现完成 + 容器内全真跑通过（2026-06-13） | spec/plan 见 docs/superpowers/{specs,plans}/2026-06-13-sp1-*；容器内整套 dotnet test 运行时类 0 skip 0 fail，3 个新异步作业测试真跑通，证据 docs/superpowers/specs/2026-06-13-sp1-all-real-runtime-evidence/sp1-summary.md。CI 仍 skip 仍绿（未改门禁）。SP2-SP5 后续。 |`
- [ ] **Step 2: 提交**

`.git/COMMIT_MSG.txt`:
```
docs(status): project SP1 all-real-runtime status

Co-Authored-By: Claude <noreply@anthropic.com>
```
```
git add docs/status/current.md docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md docs/superpowers/plans/2026-06-13-sp1-all-real-runtime-acceptance-plan.md
git commit -F .git/COMMIT_MSG.txt
```

---

## 最终验证（PR 前）

```
# host：编译 + 新测试门控正确（CI 行为）
& "C:\Program Files\dotnet\dotnet.exe" test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~LauncherAsyncJobRuntimeTests"   # 0 failed（openmoc/openmc skip）
# 容器：全真跑 0 skip 0 fail（Task 3 已执行，trx 归档）
git diff --check    # pass
```

## PR Gate Classification

- Scope：单一目的——SP1 全运行时真实异步跑通 + 0-skip 容器证据。
- Windows classification：`run-and-log`（容器内整套测试真跑留 trx/摘要）；新增生产代码仅在测试工程（cloud-safe），不碰 WPF/App.xaml.cs/CI 门禁。
- 模块 E：单 PR，非 ≥3-PR chain。
- PR body 按 `docs/superpowers/templates/pr-gate-checklist.md` 7 节填；Tests 节贴容器全真跑 0 skip 摘要 + host 门控 skip 行为作 fact。
- CI 边界强调：本 PR 不改 `.github/workflows/dotnet-test.yml`，CI 运行时测试仍 skip 仍绿。
