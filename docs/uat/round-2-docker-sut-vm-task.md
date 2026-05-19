# Docker SUT — VM 端验证 + 回归测试任务书

> **For**: 在 Windows VM 上运行的 Claude Code agent（或人类工程师）。
> **Mission**: 验证 `docker/Dockerfile` 在 Windows VM 上能够 build → run，并通过该镜像跑通 4 条 OpenMOC/OpenMC system-MT 端到端回归测试。
> **预计耗时**: 首次 build ~15–25 min，回归测试 ~2 min，文档撰写 ~10 min。

---

## 1. 背景

Cloud 端（Linux）已完成：

- `MetBench_BLL.Core/SystemMT/Launcher/SystemMtMrLauncher.cs` 注册的 4 条 MR：
  - `openmoc-pincell-nu-sigma-f`（ScaleNuSigmaF → k_eff↑）
  - `openmoc-pincell-sigma-a`（ScaleFuelSigmaA → k_eff↓）
  - `openmc-pincell-nu-sigma-f`（ScaleNuSigmaF → k_eff↑）
  - `openmc-pincell-sigma-a`（ScaleFuelSigmaA → k_eff↓）
- BDD 回归 feature：`MetBench_SystemMT.Tests/Features/CrossProgramNeutronTransportMrs.feature`（4 scenarios，cloud 全 ✅）。
- Docker 化：`docker/Dockerfile`（multi-stage Ubuntu 24.04，两栈 venv 并存，cloud 已 build + smoke 通过；镜像 ~1.07 GB）。

VM 端要做的事 = 在 Windows + Docker Desktop + WSL2 上把同一套跑通。

## 2. 前置条件

依次确认以下软件存在，缺啥装啥：

| 软件 | 版本 | 检查命令 |
|---|---|---|
| Windows 10/11 | — | `winver` |
| Docker Desktop | ≥ 4.30 | `docker --version` |
| WSL2 + Ubuntu 22.04/24.04 | — | `wsl -l -v`（STATE 必须 `Running`，VERSION 必须 `2`） |
| Docker Desktop WSL Integration | 已开启 | 设置 → Resources → WSL Integration → 勾选 Ubuntu |
| .NET SDK 8.0 | ≥ 8.0.100 | （进 WSL）`dotnet --version` |
| git | — | `git --version` |

不在 WSL 内的 `git`/`dotnet` 不可用 — 全部回归测试必须在 WSL2 内执行。这是因为 .NET 启动的 SUT 子进程要以 Linux 路径 `docker run -v <wsl-path>:<wsl-path>` 挂载源码树，Windows 原生 PowerShell 的盘符路径无法直接对齐。

> **WSL2 选哪个发行版？** Ubuntu 24.04 优先（与 cloud + Dockerfile 一致），22.04 也可。

## 3. 部署步骤

### 3.1 拉分支

```bash
# 在 WSL2 Ubuntu shell 里执行
cd ~
git clone -b claude/metbench-w11-2-experiments-QNIl6 \
    https://github.com/meng004/MetBench-V2.1.4_2.git
cd MetBench-V2.1.4_2
git log --oneline -3   # 应看到 ce06226 + 769417b
```

> 若 repo 已存在：`git fetch origin && git checkout claude/metbench-w11-2-experiments-QNIl6 && git pull --rebase`。

### 3.2 Build 镜像

```bash
docker build -t metbench-sut:latest docker/
```

**等候时间** ~15–25 min（OpenMC 的 cmake/make 是最慢的一步，~10–15 min）。
**通过判定**：

```bash
docker images metbench-sut:latest
# REPOSITORY    TAG       IMAGE ID   CREATED   SIZE
# metbench-sut  latest    <hash>     ...       ~1.0–1.2 GB
```

若 build 失败：把最后 50 行日志贴到 PR 评论上，**停在此处**，不要继续。

### 3.3 安装 .NET 依赖（WSL 端首次）

```bash
sudo apt-get update -qq
sudo apt-get install -y --no-install-recommends curl ca-certificates
# Microsoft repo（dot.net 在某些国内/cloud 网络下 403，apt 路径更稳）
if [ ! -f /etc/apt/sources.list.d/microsoft-prod.list ] \
   && [ ! -f /etc/apt/sources.list.d/microsoft-prod.sources ]; then
    TMPDEB=$(mktemp --suffix=.deb)
    curl -fsSL -o "$TMPDEB" \
      "https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/packages-microsoft-prod.deb"
    sudo dpkg -i "$TMPDEB" && rm -f "$TMPDEB"
    sudo apt-get update -qq
fi
sudo apt-get install -y --no-install-recommends dotnet-sdk-8.0
dotnet --version    # 应是 8.0.x
```

## 4. 验证步骤

### 4.1 Track A — 镜像 smoke（无 dotnet 依赖）

跑两个 SUT runner，验证镜像本身能跑。

```bash
mkdir -p /tmp/dockerout && chmod 777 /tmp/dockerout

# OpenMOC（~3 s）
docker run --rm \
  -v "$PWD/SUT:/work/SUT:ro" \
  -v /tmp/dockerout:/work/out \
  metbench-sut:latest \
  /opt/openmoc-venv/bin/python /work/SUT/openmoc/openmoc_runner.py \
    --input /work/SUT/openmoc/sample/pincell.json \
    --output /work/out/openmoc-pincell.json
cat /tmp/dockerout/openmoc-pincell.json | head -5
# 预期: k_eff ≈ 1.133, converged: true

# OpenMC（~30 s, Monte Carlo 60 batches × 5000 particles）
docker run --rm \
  -v "$PWD/SUT:/work/SUT:ro" \
  -v /tmp/dockerout:/work/out \
  metbench-sut:latest \
  /opt/openmc-venv/bin/python /work/SUT/openmc/openmc_runner.py \
    --input /work/SUT/openmc/sample/pincell.json \
    --output /work/out/openmc-pincell.json
cat /tmp/dockerout/openmc-pincell.json | head -5
# 预期: k_eff ≈ 1.12 ± 0.02, converged: true
```

**通过判定**：两个 JSON 都生成 + `converged: true` + k_eff 在容差内。

### 4.2 Track B — 完整 BDD 回归（核心交付）

用 docker 镜像里的 venv 当 python 解释器，跑 .NET 端的 4-scenario cross-program feature。

**为什么要走 wrapper 脚本 `docker/wrappers/openm{oc,c}-docker.sh`，而不是直接把 `docker run …` 塞进 `METBENCH_OPENMOC_PYTHON`？**

测试代码里的 importability gate（`MetBench_SystemMT.Tests/SystemMT/OpenMocTestPaths.cs`）用 `Process.Start` + `UseShellExecute=false`，把 env var 值当 **single executable path** 来 `execve()` —— 多词字符串 `"docker run --rm …"` 不会被 shell 解析，直接报 file-not-found → scenario 被 SKIP。Wrapper 脚本是单一可执行路径，内部 `exec docker run …`，gate 看见 single path 就放行，pipeline 调用也照常工作。

**关键技巧**：wrapper 把 host repo 路径以 **同名 path** 挂进容器（`-v $REPO:$REPO`），让 launcher 生成的绝对路径（`/home/<you>/MetBench-V2.1.4_2/SUT/...`）在容器内不需要翻译就 resolve。

```bash
cd ~/MetBench-V2.1.4_2     # WSL2 内仓库根，例如 /home/limeng/MetBench-V2.1.4_2

export METBENCH_OPENMOC_PYTHON=$PWD/docker/wrappers/openmoc-docker.sh
export METBENCH_OPENMC_PYTHON=$PWD/docker/wrappers/openmc-docker.sh

# 先单独验 wrapper 自身能 import（5 s 内必返回）
$METBENCH_OPENMOC_PYTHON -c "import openmoc; print('wrapper OK')"
$METBENCH_OPENMC_PYTHON  -c "import openmc;  print('wrapper OK', openmc.__version__)"

# 真正的 4-scenario 回归
dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj \
    --filter "FullyQualifiedName~Cross_ProgramMetamorphicRelationsOnNeutron_TransportSolversFeature" \
    --logger "console;verbosity=normal"
```

> Wrapper 默认 mount repo root（脚本所在目录的两层父目录）。如果 dotnet 在子目录里跑 / repo 在非常规位置，显式 `export METBENCH_HOST_REPO=/path/to/MetBench-V2.1.4_2` 兜底。镜像 tag 默认 `metbench-sut:latest`，自定义可 `export METBENCH_SUT_IMAGE=metbench-sut:vXYZ`。

**预期输出尾部**：

```
  Passed ScaleNuSigmaF increases k_eff regardless of solver(solver: "openmoc", sut: "openmoc", ...) [<N> s]
  Passed ScaleNuSigmaF increases k_eff regardless of solver(solver: "openmc",  sut: "openmc",  ...) [<N> s]
  Passed ScaleFuelSigmaA decreases k_eff regardless of solver(solver: "openmoc", sut: "openmoc", ...) [<N> s]
  Passed ScaleFuelSigmaA decreases k_eff regardless of solver(solver: "openmc",  sut: "openmc",  ...) [<N> s]
Test Run Successful.
Total tests: 4
     Passed: 4
```

> **耗时基线（参考）**：cloud 上直连 venv 33.6 s / docker-wrapper 48.4 s，4/4 ✅。VM 上每个 `docker run` 有 ~1 s 启动开销，4 scenario × 多个 docker run（parser/writer/runner），总时长大致 50–90 s。

### 4.3 Track C（可选）— Runner smoke + sample case 兜底

```bash
dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj \
    --filter "FullyQualifiedName~OpenMocRunnerSmokeTests|FullyQualifiedName~OpenMcRunnerSmokeTests|FullyQualifiedName~OpenMocSampleCaseTests" \
    --logger "console;verbosity=normal" --no-build
```

预期：3/3 ✅。

## 5. 通过判定（Acceptance Criteria）

| 步骤 | 通过条件 |
|---|---|
| 3.2 build | `docker images metbench-sut:latest` 存在，size ≤ 1.5 GB |
| 4.1 OpenMOC smoke | JSON 含 `converged: true`，k_eff ∈ [1.10, 1.20] |
| 4.1 OpenMC smoke | JSON 含 `converged: true`，k_eff ∈ [1.08, 1.16] |
| 4.2 全 4 scenario | `Total tests: 4 / Passed: 4` |
| 4.3 smoke | `Total tests: 3 / Passed: 3` |

任一 ❌ → 视为 fail，**不要硬绕过**，把 stdout/stderr 末尾 50 行贴 PR 评论上等 cloud 端协助 diagnose。

## 6. 反馈

跑完后在 `docs/uat/reports/round-2-docker-sut-<日期>-<你的名字>/` 下新建：

```
README.md     # 平台元信息 + 5 项通过判定的勾选
findings.md   # 实际 k_eff 数值 + 总耗时 + 任何偏差 / 警告
evidence/     # 截图（docker images, dotnet test 末尾）+ JSON 输出副本
```

然后在 PR 上评论：

```
VM round-2 docker SUT 完成：
- 镜像 size: <N> GB
- Track A: ✅ / ❌
- Track B: <pass>/<total>
- Track C: <pass>/<total>
- 偏差: <无 / 列举>
- 报告: docs/uat/reports/round-2-docker-sut-<日期>-<你的名字>/
```

---

## 附录 A — Windows native（不用 WSL2）的注意事项

> 不推荐。要让 dotnet 在 Windows native 跑 + python 解释器走 docker，需要写路径翻译 wrapper（`C:\path\foo.py` → `/work/foo.py`），并维护宿主-容器路径映射。Track B 的 launcher 拼 `runner --input "<src.json>" --output "<dst.json>"` 时这些路径都是绝对路径，容器内必须能 resolve。
>
> 若必须 Windows native，可参考的 wrapper 思路（**非项目正式部分**）：
>
> ```cmd
> @echo off
> set REPO=C:\dev\MetBench-V2.1.4_2
> set REPO_FW=%REPO:\=/%
> set CONTAINER_REPO=/work
> set ARGS=%*
> set ARGS=%ARGS:C:\dev\MetBench-V2.1.4_2=/work%
> set ARGS=%ARGS:\=/%
> docker run --rm -v "%REPO%:%CONTAINER_REPO%" -w %CONTAINER_REPO% metbench-sut:latest /opt/openmoc-venv/bin/python %ARGS%
> ```
>
> 然后 `set METBENCH_OPENMOC_PYTHON=C:\path\wrap-openmoc.cmd`。但该路径仍有引号 / 空格的 corner case；本任务书不要求实现，VM 工程师如有时间可独立探索。

## 附录 B — 故障排查

| 症状 | 原因 | 修法 |
|---|---|---|
| `docker build` 卡在 pip install setuptools wheel | 容器 DNS 出不去 / 公司代理 TLS MITM | Docker Desktop → Settings → Resources → Network；或在 Dockerfile build stage 临时注入企业 CA |
| `dotnet test` 报 `Process file not found: docker` | WSL2 内未启用 Docker Desktop integration | Settings → Resources → WSL Integration → 勾选当前发行版 → Apply & Restart |
| 4 scenarios 全 Skipped | env var 没传给 dotnet | 确认 `echo $METBENCH_OPENMOC_PYTHON` 非空；export 必须在同一 shell 里 |
| OpenMC scenario 偶发 fail（k_eff↑ 但 follow-up 没显著大于 source） | Monte Carlo 噪声，5000 particle 太少 | 重跑 1 次；连续 3 次 fail 才算真 bug |
| `docker run` 报 `permission denied: /work/...` | volume mount 路径不在 Docker Desktop 共享列表 | Settings → Resources → File Sharing → 添加 WSL repo 路径（一般 WSL2 backend 自动 OK） |

## 附录 C — 关键文件索引（VM Claude 走读用）

| 文件 | 作用 |
|---|---|
| `docker/Dockerfile` | Ubuntu 24.04 multi-stage，含 OpenMOC + OpenMC 双 venv |
| `docker/wrappers/openmoc-docker.sh` / `openmc-docker.sh` | Track B 用的 single-path wrapper（绕过 `Process.Start` 不走 shell 的限制） |
| `.claude/web-setup.sh` | Cloud 端原生安装脚本，Dockerfile 行为基准 |
| `MetBench_BLL.Core/SystemMT/Launcher/SystemMtMrLauncher.cs` | 4 条 MR 的 BuildMrCatalog 注册点 |
| `MetBench_BLL.Core/SystemMT/Launcher/LauncherOptions.cs` | `OpenMocPython` / `OpenMcPython` 字段定义 |
| `MetBench_BLL.Core/SystemMT/Pipeline/DefaultProcessExecutor.cs` | `sh -c` / `cmd /c` 调度入口 |
| `MetBench_BLL.Core/SystemMT/Pipeline/SystemMtPipeline.cs` | 拼 `{PythonExecutable} {script} --input ...` 命令的地方 |
| `MetBench_SystemMT.Tests/Features/CrossProgramNeutronTransportMrs.feature` | 4 scenario 的 BDD source-of-truth |
| `MetBench_SystemMT.Tests/Steps/CrossProgramSteps.cs` | scenario 的 C# 步骤实现 |
| `SUT/openmoc/openmoc_runner.py` | OpenMOC CLI 入口 |
| `SUT/openmc/openmc_runner.py` | OpenMC CLI 入口 |
