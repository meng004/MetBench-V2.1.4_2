# SP1 容器内全运行时真实跑通 Runbook

日期：2026-06-13
依据：spec `docs/superpowers/specs/2026-06-13-sp1-all-real-runtime-acceptance-design.md`

目标：在一个 openmoc / openmc / scipy 都可导入的容器里跑整套 `MetBench_SystemMT.Tests`，
让运行时类端到端测试从 CI 上的 skip 转为**真实运行、0 skip 0 fail**。

## 0. 前置

- Docker Desktop 引擎运行；`docker` CLI（本机在 `C:\Program Files\Docker\Docker\resources\bin\docker.exe`，未在 PATH 时用全路径）。
- `metbench-sut:latest` 已在引擎内（`docker images metbench-sut:latest`）。该镜像含
  `/opt/openmoc-venv`、`/opt/openmc-venv`（openmc venv 经 system-site-packages 带 numpy/scipy）。

## 1. 构建 metbench-runtime 镜像（一次性）

`docker/Dockerfile.runtime` = `metbench-sut:latest` + .NET 8 SDK：

```
docker build -t metbench-runtime:latest -f docker/Dockerfile.runtime docker/
```

## 2. 容器内验证三运行时可导入（预检）

```
docker run --rm -v "<repo-abs>:/work" -w /work metbench-runtime:latest bash -lc \
  "/opt/openmoc-venv/bin/python -c 'import openmoc' && echo OPENMOC_OK; \
   /opt/openmc-venv/bin/python -c 'import openmc' && echo OPENMC_OK; \
   /opt/openmc-venv/bin/python -c 'import scipy.integrate' && echo SCIPY_OK"
```
期望三行 `*_OK`。（scipy 取自 openmc venv 的 system-site-packages；故 `METBENCH_SCIPY_PYTHON`
指向 `/opt/openmc-venv/bin/python`。）

## 3. 容器内跑整套测试

`<repo-abs>` 在本机为 `D:\Codes\MetBench-V2.1.4_2`：

```
docker run --rm -v "<repo-abs>:/work" -w /work metbench-runtime:latest `
  env METBENCH_OPENMOC_PYTHON=/opt/openmoc-venv/bin/python `
      METBENCH_OPENMC_PYTHON=/opt/openmc-venv/bin/python `
      METBENCH_SCIPY_PYTHON=/opt/openmc-venv/bin/python `
  dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj `
    --logger "trx;LogFileName=sp1-all-real.trx"
```

trx 落在挂载目录 `MetBench_SystemMT.Tests/TestResults/sp1-all-real.trx`，host 可直接读。

## 4. 通过判据

- 全套 `Failed: 0`。
- **运行时类**测试（scipy/openmoc/openmc 端到端 + 跨程序 BDD + 3 个新 `LauncherAsyncJobRuntimeTests`）
  全部 `Passed`、**不在 Skipped 内**。用以下核验（PowerShell 解析 trx）：
  ```
  [xml]$x = Get-Content <trx>; $x.TestRun.Results.UnitTestResult |
    Where-Object { $_.testName -match "Scipy|OpenMoc|OpenMc|AsyncJobRuntime|RayTrack|ParticleCount|CrossProgram" } |
    ForEach-Object { "$($_.outcome)  $($_.testName.Split('.')[-1])" } | Sort-Object -Unique
  ```
  期望全为 `Passed`。

## 5. 范围外的合法 skip（非 SP1 判据）

整套仍可能有少量 skip，但**不属于运行时类**，SP1 不负责消除：
- `McpThreeCaseAcceptanceTests`（3 个）：需 `METBENCH_MCP_ACCEPTANCE_*` 指向**实时 MCP server**——
  由 MCP 三用例验收单独覆盖（见 `2026-06-12-mcp-three-case-acceptance-vm-evidence/`）。
- `MinimumMrSubsetBGroupExternalSourceSmokeTests`（3 个）：需**外部 P3/P8 源** + pytest，且外部 P8
  依赖已移除的 `np.trapz`，状态账本记为 BLOCKED——属外部源接入议题，非 MetBench 自有运行时。

## 6. 证据归档

trx + 运行摘要存 `docs/superpowers/specs/2026-06-13-sp1-all-real-runtime-evidence/`：
`sp1-all-real.trx` + `sp1-summary.md`（计数对照、运行时类逐项 Passed、预检 import 输出、镜像 digest）。

## 7. CI 边界（重要）

本流程**不改 CI**。`.github/workflows/dotnet-test.yml`（ubuntu，不装 venv）上这些运行时测试
继续 skip、继续绿。"0 skip 全真跑"是**容器内**才成立的事实，作为归档证据，不进 CI 必跑门禁。
