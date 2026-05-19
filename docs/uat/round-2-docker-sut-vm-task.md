# Docker SUT — VM 端验证 + 回归测试任务书

> **For**: 在 Windows VM 上运行的 Claude Code agent（或人类工程师）。
> **Mission**: 验证 `docker/Dockerfile` + `docker/Dockerfile.runtime` 在 Windows VM 上能够 build → run，并通过镜像 **一行 docker run** 跑通 4 条 OpenMOC/OpenMC system-MT 端到端回归测试 —— 无需 WSL2、无需主机 .NET SDK，只需 Docker Desktop。
> **预计耗时**: 首次 build ~20–30 min，回归测试 ~2 min，文档撰写 ~10 min。

---

## 1. 背景

Cloud 端（Linux）已完成：

- `MetBench_BLL.Core/SystemMT/Launcher/SystemMtMrLauncher.cs` 注册的 4 条 MR：
  - `openmoc-pincell-nu-sigma-f`（ScaleNuSigmaF → k_eff↑）
  - `openmoc-pincell-sigma-a`（ScaleFuelSigmaA → k_eff↓）
  - `openmc-pincell-nu-sigma-f`（ScaleNuSigmaF → k_eff↑）
  - `openmc-pincell-sigma-a`（ScaleFuelSigmaA → k_eff↓）
- BDD 回归 feature：`MetBench_SystemMT.Tests/Features/CrossProgramNeutronTransportMrs.feature`（4 scenarios，cloud 全 ✅）。
- Docker 化：
  - `docker/Dockerfile`（multi-stage Ubuntu 24.04 + OpenMOC + OpenMC 双 venv） → `metbench-sut:latest`，~1.07 GB
  - `docker/Dockerfile.runtime`（在 sut 之上加 .NET 8 SDK） → `metbench-runtime:latest`，~1.76 GB
- Cloud 已用 `metbench-runtime:latest` 端到端跑通 4-scenario：**4/4 ✅, 37.4 s**。

VM 端要做的事 = 在 Windows + Docker Desktop 上把同一套跑通。

> **设计取舍**：为什么把 .NET SDK 也塞进镜像？让 VM 端不再需要 WSL2 + dotnet SDK 装机 + env var dance + wrapper 脚本 —— PowerShell 一行 `docker run` 直接跑 dotnet test。详细对比见附录 A。

## 2. 前置条件

| 软件 | 版本 | 检查命令（PowerShell） |
|---|---|---|
| Windows 10/11 | — | `winver` |
| Docker Desktop | ≥ 4.30 | `docker --version` |
| git | — | `git --version` |

Docker Desktop 必须处于 **Running** 状态；后端 WSL2 / Hyper-V 都可，文中命令不依赖具体后端。

## 3. 部署步骤

### 3.1 拉分支（PowerShell）

```powershell
cd $HOME
if (Test-Path MetBench-V2.1.4_2) {
    cd MetBench-V2.1.4_2
    git fetch origin
    git checkout claude/metbench-w11-2-experiments-QNIl6
    git pull --rebase origin claude/metbench-w11-2-experiments-QNIl6
} else {
    git clone -b claude/metbench-w11-2-experiments-QNIl6 `
        https://github.com/meng004/MetBench-V2.1.4_2.git
    cd MetBench-V2.1.4_2
}
git log --oneline -1   # commit 必须 ≥ a1f8e4b（含 docker/Dockerfile.runtime）
```

### 3.2 Build 两个镜像

```powershell
# 1) SUT 镜像：OpenMOC + OpenMC 双 venv（首次 ~15–25 min）
docker build -t metbench-sut:latest docker/

# 2) Runtime 镜像：sut + .NET 8 SDK（在 sut 缓存命中后只需 ~3–5 min）
docker build -t metbench-runtime:latest -f docker/Dockerfile.runtime docker/

docker images | findstr metbench
```

**通过判定**：

```
metbench-sut       latest   ...   ~1.0–1.2 GB
metbench-runtime   latest   ...   ~1.7–1.9 GB
```

若 build 失败：把最后 50 行日志贴 PR #73 评论，**停在此处**，不要继续。

## 4. 验证步骤

### 4.1 Track A — 镜像 smoke（无 .NET 依赖，仅验证 SUT 镜像本身）

```powershell
$tmp = "$env:TEMP\metbench-dockerout"
New-Item -ItemType Directory -Force $tmp | Out-Null

# OpenMOC (~3 s)
docker run --rm -v "${PWD}/SUT:/work/SUT:ro" -v "${tmp}:/work/out" `
    metbench-sut:latest `
    /opt/openmoc-venv/bin/python /work/SUT/openmoc/openmoc_runner.py `
        --input /work/SUT/openmoc/sample/pincell.json `
        --output /work/out/openmoc-pincell.json
Get-Content "$tmp\openmoc-pincell.json" | Select-Object -First 5
# 预期: k_eff ≈ 1.133, converged: true

# OpenMC (~30 s, Monte Carlo 60 batches × 5000 particles)
docker run --rm -v "${PWD}/SUT:/work/SUT:ro" -v "${tmp}:/work/out" `
    metbench-sut:latest `
    /opt/openmc-venv/bin/python /work/SUT/openmc/openmc_runner.py `
        --input /work/SUT/openmc/sample/pincell.json `
        --output /work/out/openmc-pincell.json
Get-Content "$tmp\openmc-pincell.json" | Select-Object -First 5
# 预期: k_eff ≈ 1.12 ± 0.02, converged: true
```

**通过判定**：两个 JSON 都生成 + `converged: true` + k_eff 在容差内。

### 4.2 Track B — 完整 BDD 回归（核心交付，all-in-container）

整个 dotnet test runner + python venv 都在 `metbench-runtime` 容器里跑。Host 只负责 bind-mount 仓库 + 透传 env var。

```powershell
docker run --rm -v "${PWD}:/work" -w /work `
    -e METBENCH_OPENMOC_PYTHON=/opt/openmoc-venv/bin/python `
    -e METBENCH_OPENMC_PYTHON=/opt/openmc-venv/bin/python `
    metbench-runtime:latest `
    dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj `
        --filter "FullyQualifiedName~Cross_ProgramMetamorphicRelationsOnNeutron_TransportSolversFeature" `
        --logger "console;verbosity=normal"
```

**为什么这样能跑通？**

- 仓库 bind-mount 到容器 `/work`，所以 `dotnet test` 在容器里看到的就是宿主仓库的完整源码 + bin/obj 输出会写回宿主（首次有 ~30 s restore + ~20 s build）。
- env var `METBENCH_OPENM(O)C_PYTHON` 直接指 **容器内** 的 venv 路径（不是 host 路径），launcher 拉起的 python 子进程就是 sibling 进程，没有 docker-in-docker，也不需要 wrapper 脚本绕 `Process.Start` 的 single-path gate。
- launcher 的 artifacts dir 在容器内 `/tmp/MetBenchCrossProgramBdd-*/`，跟 SUT 脚本路径 `/work/SUT/...` 都在容器视角统一，无需路径翻译。

**预期输出尾部**：

```
  Passed ScaleNuSigmaF increases k_eff regardless of solver(solver: "openmoc", ...) [<N> s]
  Passed ScaleNuSigmaF increases k_eff regardless of solver(solver: "openmc",  ...) [<N> s]
  Passed ScaleFuelSigmaA decreases k_eff regardless of solver(solver: "openmoc", ...) [<N> s]
  Passed ScaleFuelSigmaA decreases k_eff regardless of solver(solver: "openmc",  ...) [<N> s]
Test Run Successful.
Total tests: 4
     Passed: 4
```

> **耗时基线**：cloud 上 in-container 4/4 ✅, **37.4 s**（重跑，bin 已缓存）。VM 首次跑会多 ~30 s nuget restore + ~20 s build，总耗时 ~90 s；再跑回到 ~40 s。

### 4.3 Track C（可选）— Runner smoke + sample case 兜底

```powershell
docker run --rm -v "${PWD}:/work" -w /work `
    -e METBENCH_OPENMOC_PYTHON=/opt/openmoc-venv/bin/python `
    -e METBENCH_OPENMC_PYTHON=/opt/openmc-venv/bin/python `
    metbench-runtime:latest `
    dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj `
        --filter "FullyQualifiedName~OpenMocRunnerSmokeTests|FullyQualifiedName~OpenMcRunnerSmokeTests|FullyQualifiedName~OpenMocSampleCaseTests" `
        --logger "console;verbosity=normal"
```

预期：3/3 ✅，约 50 s（含 restore + build，比 Track B 略快因没有 cross-program runner 调用）。

> **不要加 `--no-build`**：`docker run --rm` 是 ephemeral 容器，前一次 Track B 的 `bin/obj` 虽然 bind-mount 写回了宿主，但本次 fresh 容器内 nuget restore 状态丢失，dotnet test 会找不到 test discovery 元数据，结果为 `Total tests: 0`。让 fresh 容器自己重做 restore + build 多花 ~40 s 但稳。如要 `--no-build` 加速重跑，需要额外挂 `-v <host-nuget>:/root/.nuget/packages` 并保证两次容器使用同一份 nuget cache —— 不是 Track C 默认推荐配置。

## 5. 通过判定（Acceptance Criteria）

| 步骤 | 通过条件 |
|---|---|
| 3.2 sut build | `metbench-sut:latest` 存在，size ≤ 1.5 GB |
| 3.2 runtime build | `metbench-runtime:latest` 存在，size ≤ 2.0 GB |
| 4.1 OpenMOC smoke | JSON 含 `converged: true`，k_eff ∈ [1.10, 1.20] |
| 4.1 OpenMC smoke | JSON 含 `converged: true`，k_eff ∈ [1.08, 1.16] |
| 4.2 全 4 scenario | `Total tests: 4 / Passed: 4` |
| 4.3 smoke | `Total tests: 3 / Passed: 3` |

任一 ❌ → 视为 fail，**不要硬绕过**，把 stdout/stderr 末尾 50 行贴 PR #73 评论上等 cloud 端协助 diagnose。

## 6. 反馈

跑完后在 `docs/uat/reports/round-2-docker-sut-<日期>-<你的名字>/` 下新建：

```
README.md     # 平台元信息 + 6 项通过判定的勾选
findings.md   # 实际 k_eff 数值 + 总耗时 + 任何偏差 / 警告
evidence/     # 截图（docker images, dotnet test 末尾）+ JSON 输出副本
```

然后在 PR 上评论：

```
VM round-2 docker SUT (all-in-container) 完成：
- 平台: Windows <版本> + Docker Desktop <版本>
- 镜像 size: sut <N> GB, runtime <N> GB
- Track A: ✅ / ❌ (OpenMOC k_eff=<x>, OpenMC k_eff=<x>)
- Track B: <pass>/<total>, 总耗时 <N> s
- Track C: <pass>/<total>
- 偏差: <无 / 列举>
- 报告: docs/uat/reports/round-2-docker-sut-<日期>-<你的名字>/
```

---

## 附录 A — WSL2 + wrapper 备选路线（不推荐，但仍可用）

如果 VM 上已经习惯了 WSL2 + 主机 dotnet SDK 工作流，或者出于某种原因不愿把 .NET SDK 塞进容器，可以走 **dotnet 在主机 / python 在容器** 的混合路线：

1. 装 `dotnet-sdk-8.0` 到 WSL2 Ubuntu
2. 把 `docker/wrappers/openmoc-docker.sh` / `openmc-docker.sh` 当 python 解释器（single-path wrapper，内部 exec docker run）：

   ```bash
   cd ~/MetBench-V2.1.4_2     # WSL2 内仓库根
   export METBENCH_OPENMOC_PYTHON=$PWD/docker/wrappers/openmoc-docker.sh
   export METBENCH_OPENMC_PYTHON=$PWD/docker/wrappers/openmc-docker.sh
   dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj \
       --filter "FullyQualifiedName~Cross_Program..."
   ```

3. wrapper 默认 mount repo root（脚本所在目录的两层父目录）。如果 dotnet 在子目录里跑，`export METBENCH_HOST_REPO=/path/to/MetBench-V2.1.4_2` 兜底。

> 为什么不能直接把 `docker run --rm ... metbench-sut:latest /opt/openmoc-venv/bin/python` 多词字符串塞进 env var？测试代码里的 importability gate（`MetBench_SystemMT.Tests/SystemMT/OpenMocTestPaths.cs`）用 `Process.Start` + `UseShellExecute=false`，把 env var 值当 **single executable path** 来 `execve()`，多词不会被 shell parse → file-not-found → scenario 全 SKIP。Wrapper 脚本是单一可执行路径，内部 `exec docker run …`，gate 看见 single path 就放行。Cloud 端已 verify 此路径 4/4 ✅, 37.8 s。

## 附录 B — 故障排查

| 症状 | 原因 | 修法 |
|---|---|---|
| `docker build` 卡在 pip install setuptools wheel | 容器 DNS 出不去 / 公司代理 TLS MITM | Docker Desktop → Settings → Resources → Network；或在 Dockerfile build stage 临时注入企业 CA |
| `dotnet test` 报 `Unable to load the service index for nuget.org` | 容器内 nuget 出不去 / 企业代理证书未信任 | 同上 + `docker run -e HTTPS_PROXY=...`；离线场景可在主机预 restore + 挂 `~/.nuget/packages` 入容器再 `--no-restore` |
| 4 scenarios 全 Skipped (附录 A 路线) | env var 设成了多词 `docker run …` 字符串 | 改成 wrapper 脚本路径（见附录 A），或换主路线 |
| OpenMC scenario 偶发 fail（k_eff↑ 但 follow-up 没显著大于 source） | Monte Carlo 噪声，5000 particle 太少 | 重跑 1 次；连续 3 次 fail 才算真 bug |
| `docker run` 报 `permission denied: /work/...` | 仓库路径不在 Docker Desktop 共享列表 | Settings → Resources → File Sharing → 添加仓库所在盘符 |
| Track C 报 `Total tests: 0`，dotnet test 立即 exit | 沿用了 `--no-build`，但 ephemeral 容器内 nuget restore 状态丢失 | 去掉 `--no-build`（见 §4.3 注释） |

## 附录 C — 中国境内网络配置（macOS + Apple Silicon 已验证）

> 来源：round-2 UAT report on macOS 15.7.3 + M1 Pro（`docs/uat/reports/round-2-docker-sut-2026-05-19-limeng-macos/findings.md` §3）。海外/国际网络通常无需做以下任何一项。

中国境内 build SUT 镜像必须同时配齐 **registry mirror + build-arg HTTP proxy + Clash TUN OFF** 三件套，缺一会在不同阶段失败：

### C.1 Registry mirror（修 `docker pull ubuntu:24.04` 超时）

`registry-1.docker.io` / `auth.docker.io` 国内直连不稳。Docker Desktop → Settings → Docker Engine 加 `registry-mirrors` 字段：

```json
{
  "builder": { "gc": { "defaultKeepStorage": "20GB", "enabled": true } },
  "experimental": false,
  "registry-mirrors": ["https://docker.m.daocloud.io"]
}
```

Apply & restart。`docker pull ubuntu:24.04` 应 ~30 s 完成。

### C.2 build-arg HTTP proxy（修 `git clone github.com` TLS 中断）

`github.com` 国内对中大 pack 文件不稳，build 内的 `git clone` 容易在 ~480 s 后 TLS 中断（`curl 56 GnuTLS recv error / fatal: early EOF`）。给 `docker build` 传 BuildKit 预定义的 proxy ARG，让 build 内的 git 走宿主 HTTP proxy（如 Clash Verge 的 7890 端口），同时让 apt **直连不走代理**（apt 走 Cloudflare CDN 稳定）：

```bash
docker build \
  --platform=linux/amd64 \
  --build-arg HTTP_PROXY=http://host.docker.internal:7890 \
  --build-arg HTTPS_PROXY=http://host.docker.internal:7890 \
  --build-arg NO_PROXY="archive.ubuntu.com,security.ubuntu.com,ports.ubuntu.com,127.0.0.1,localhost" \
  -t metbench-sut:latest \
  docker/
```

`HTTP_PROXY` / `HTTPS_PROXY` / `NO_PROXY`（含大小写共 6 个 ARG）是 BuildKit 自动透传到每个 RUN 的预定义 build-arg，Dockerfile 不需要任何修改。

### C.3 Clash Verge TUN mode 必须 OFF

如果出现 apt 报错 IP 是 `198.18.x.x`（RFC 2544 reserved 网段，Clash/mihomo TUN fake-IP 标志），且在 sustained ~600 kB/s 下载下连接池崩，那是 TUN mode 在搞鬼。正确状态：

| 项 | 值 |
|---|---|
| Clash Verge GUI | **TUN Mode OFF** |
| Clash Verge GUI | HTTP proxy mode ON（保留 mixed proxy 7890） |
| macOS 系统 Network Proxies | 可全部关闭（不影响 docker build 路径） |
| Docker Desktop → Settings → Resources → Proxies | **No proxy**（让 daemon 直连，依赖 §D.1 的 mirror；apt 流量通过 NO_PROXY 直连，git 流量通过 §D.2 build-arg 走 Clash 7890） |

### C.4 失败模式速查

| Attempt 配置 | 失败点 | 修法 |
|---|---|---|
| Docker Desktop 全局 proxy 设到 Clash 7897, TUN ON | apt fetch ~140 s 后 IP=198.18.1.17 EOF | 关 TUN, Docker Desktop proxy 改 None |
| 同上但 Docker proxy=None, TUN OFF, 无 build-arg | apt + venv OK，git clone OpenMOC ~480 s TLS 中断 | 加 §D.2 三个 build-arg |
| 全配齐（mirror + build-arg + TUN OFF） | ✅ ~4 min 全 build 通 | — |

## 附录 D — 关键文件索引（VM Claude 走读用）

| 文件 | 作用 |
|---|---|
| `docker/Dockerfile` | Ubuntu 24.04 multi-stage，含 OpenMOC + OpenMC 双 venv |
| `docker/Dockerfile.runtime` | sut 镜像 + .NET 8 SDK，Track B 主路线用 |
| `docker/wrappers/openmoc-docker.sh` / `openmc-docker.sh` | 附录 A 备选路线用的 single-path wrapper |
| `.claude/web-setup.sh` | Cloud 端原生安装脚本，Dockerfile 行为基准 |
| `MetBench_BLL.Core/SystemMT/Launcher/SystemMtMrLauncher.cs` | 4 条 MR 的 BuildMrCatalog 注册点 |
| `MetBench_BLL.Core/SystemMT/Launcher/LauncherOptions.cs` | `OpenMocPython` / `OpenMcPython` 字段定义 |
| `MetBench_BLL.Core/SystemMT/Pipeline/DefaultProcessExecutor.cs` | `sh -c` / `cmd /c` 调度入口 |
| `MetBench_BLL.Core/SystemMT/Pipeline/SystemMtPipeline.cs` | 拼 `{PythonExecutable} {script} --input ...` 命令的地方 |
| `MetBench_SystemMT.Tests/Features/CrossProgramNeutronTransportMrs.feature` | 4 scenario 的 BDD source-of-truth |
| `MetBench_SystemMT.Tests/Steps/CrossProgramSteps.cs` | scenario 的 C# 步骤实现 |
| `MetBench_SystemMT.Tests/SystemMT/OpenMocTestPaths.cs` / `OpenMcTestPaths.cs` | importability gate（解释附录 A 为何需要 wrapper 而非多词 env var） |
| `SUT/openmoc/openmoc_runner.py` | OpenMOC CLI 入口 |
| `SUT/openmc/openmc_runner.py` | OpenMC CLI 入口 |
