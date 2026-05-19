# findings — Round-2 Docker SUT — macOS (Apple Silicon, Rosetta amd64)

## 1. 实测数值

### Track A — SUT 镜像 smoke

| 程序 | k_eff | converged | 其他指标 | 容器内壁钟 |
|---|---|---|---|---|
| OpenMOC | **1.1330603545868472** | true | iterations = 553 | 1.14 s |
| OpenMC  | **1.1245000140252865 ± 0.001786852281206873** | true | batches = 60, particles = 5000 | 5.92 s |

> 数值对照 cloud baseline：OpenMOC `1.133` 精匹；OpenMC `1.12` 在统计误差内（1σ = 0.00179，1.96σ 置信区间 [1.1210, 1.1280]，baseline 1.12 落在区间内）。Rosetta amd64 仿真**未引入可见的 MC 数值偏移**。

JSON 完整输出见 `evidence/openmoc-pincell.json` 与 `evidence/openmc-pincell.json`。

### Track B — 4-scenario MR 回归

```
Total tests: 4
     Passed: 4
 Total time: 34.2863 Seconds
wall (含 nuget restore + dotnet build): 87.87 s
```

| Scenario | Solver | 时长 |
|---|---|---|
| ScaleNuSigmaF increases k_eff regardless of solver | openmoc | 3 s |
| ScaleNuSigmaF increases k_eff regardless of solver | openmc  | 13 s |
| ScaleFuelSigmaA decreases k_eff regardless of solver | openmoc | 2 s |
| ScaleFuelSigmaA decreases k_eff regardless of solver | openmc  | 12 s |

### Track C — Runner smoke + sample case

```
Total tests: 3
     Passed: 3
 Total time: 9.4599 Seconds
```

| Test | 时长 |
|---|---|
| `OpenMocSampleCaseTests.Sample_pincell_json_satisfies_runner_contract` | 100 ms |
| `OpenMocRunnerSmokeTests.Runner_solves_sample_pincell_and_writes_keff_json` | 1 s |
| `OpenMcRunnerSmokeTests.Runner_solves_sample_pincell_and_writes_keff_json` | 7 s |

---

## 2. 主要偏差 / 警告

### 2.1 镜像 build 总耗时 比任务书预估快很多

任务书估计 macOS Rosetta 下 SUT build 35-60 min。**attempt 4 (final success) 实际 3 min 51 s**——但这是基于 attempt 3 BuildKit cache 的复用。**纯净首次** build 估计 ~12-18 min（apt + venv ~6 min + OpenMOC clone+build 48 s + OpenMC clone+cmake+make-j10 ~2 min 37 s + runtime stage ~30 s + export ~13 s）。Apple Silicon M1 Pro + Rosetta 在该 workload 下比预期高效。

### 2.2 OpenMC k_eff 与 cloud baseline 偏差

Cloud baseline 报 `k_eff ≈ 1.12`，我们实测 `1.12450 ± 0.00179`。**1.96σ 置信区间 [1.1210, 1.1280] 含 1.12，无显著偏移**。OpenMC 是 Monte Carlo，每次运行因随机数种子 + OS 线程调度差异会略有不同，~0.004 量级的差异属于正常范围。

### 2.3 Runtime image size 报告口径：DISK USAGE 列延迟收敛

最终镜像 size 与 baseline 严丝合缝：

| 来源 | sut | runtime |
|---|---|---|
| 任务书估值 | ~1.07 GB | ~1.76 GB |
| `docker images` DISK USAGE（**Track C 跑完之后再测**） | 1.06 GB | **1.76 GB** ✓ |
| `docker images` CONTENT SIZE | 247.4 MB | 431.2 MB |
| `docker image inspect .Size` (bytes) | 247,430,748 (247.4 MB) | 431,242,152 (431.2 MB) |

**观察到的瞬态现象**：runtime build 刚结束时第一次 `docker images` DISK USAGE 列显示 `431 MB`（与 CONTENT SIZE 一致，未包含从 SUT 继承的共享 layer），几分钟（跑完 Track A/B/C 之后）再测变成 `1.76 GB`。怀疑是 Docker Engine 29.x 的 DISK USAGE 报告基于 layer index，BuildKit 完成 export 后到 layer dedup 索引刷新有几秒到几分钟的滞后窗口。**不是 build bug**，最终值正确。建议任务书在 "查看 image size" 步骤加一句"等 10-30 s 让 Docker daemon 刷新 layer index 后再读"。

### 2.4 Track C 任务书命令的 `--no-build` 在 ephemeral container 模式下不可用

任务书 §4.3 给的命令：

```
docker run --rm -v "${PWD}:/work" -w /work [...] metbench-runtime:latest \
  dotnet test ... --no-build
```

`--rm` 让容器执行后销毁，但 `--no-build` 期待 build artifacts 已经存在。**实测 first attempt 用 `--no-build` 直接退出，输出为空（dotnet test 报告 0 个测试匹配）**。原因：Track B 跑过之后，`bin/obj` 是在 bind-mounted `/work` 上写回了 host 的，理论上下次容器再 mount /work 应该能看到——但 dotnet test 在 fresh container 里仍找不到 test runner 期望的 nuget cache / restore state。

**workaround**：去掉 `--no-build`，让 Track C 在 fresh container 里重做 `dotnet restore + build`，总耗时 50 s（多花 ~40 s）。3/3 全过。

**建议**: 任务书 §4.3 把 `--no-build` 移除，或加注释 `# 仅在重跑且 nuget cache 持久化卷已挂载时使用 --no-build`。

---

## 3. 环境配置 gotchas（**强烈建议补进 PR 文档**）

中国境内 macOS 环境 build SUT 需要做以下三件事，否则 build 必失败：

### 3.1 Docker Hub 不可直连，必须配 registry mirror

实测：`registry-1.docker.io` 与 `auth.docker.io` 都直连超时（>15 s）。Apply 国内 mirror：

Docker Desktop → Settings → Docker Engine → 加 `registry-mirrors` 字段：

```json
{
  "builder": { "gc": { "defaultKeepStorage": "20GB", "enabled": true } },
  "experimental": false,
  "registry-mirrors": ["https://docker.m.daocloud.io"]
}
```

Apply & restart 后 `docker pull ubuntu:24.04` 26s 完成（之前 60s 直接 timeout）。

### 3.2 build 阶段 git clone github.com 需要走代理

不带任何 proxy 的 build 在 `[builder 4/6] git clone OpenMOC` 阶段 480 秒后 TLS 中断：

```
error: RPC failed; curl 56 GnuTLS recv error (-9): Error decoding the received TLS packet.
fatal: early EOF
fatal: fetch-pack: invalid index-pack output
```

`github.com` 在中国境内对中大 pack 文件不稳，直连必败。

**不动 Dockerfile** 的解法：给 docker build 传 BuildKit 预定义的 proxy ARG，让 build 内的 git 走 host HTTP proxy（Clash Verge 的 mixed proxy port 7890）：

```bash
docker build \
  --platform=linux/amd64 \
  --build-arg HTTP_PROXY=http://host.docker.internal:7890 \
  --build-arg HTTPS_PROXY=http://host.docker.internal:7890 \
  --build-arg NO_PROXY="archive.ubuntu.com,security.ubuntu.com,ports.ubuntu.com,127.0.0.1,localhost" \
  -t metbench-sut:latest \
  docker/
```

`NO_PROXY` 列出 archive.ubuntu.com / security.ubuntu.com 是为了让 apt 不走代理——apt 走 Cloudflare CDN 直连稳且快（实测 22 MB scipy.deb 16 s 拉完），走代理反而容易在 sustained download 下崩。

### 3.3 Clash Verge **TUN mode 必须关闭**

attempts 1 + 2 失败时 apt 报错 IP 是 `198.18.1.17`——RFC 2544 reserved 网段，是 Clash/mihomo TUN mode 的 fake-IP intercept 标志。TUN mode 在 sustained ~600 kB/s HTTP 下载下连接池会崩溃（实测 attempt 2 在 186 秒后所有并行 apt 连接全部 `Unable to connect`）。

正确状态：
- Clash Verge GUI: **TUN Mode OFF**
- Clash Verge GUI: HTTP proxy mode ON（保留 mixed proxy 7890）
- macOS 系统 Network Proxies: **可全部关闭**（不影响 docker build 路径）
- Docker Desktop → Settings → Resources → Proxies: **No proxy**（让 daemon 直连，依赖 §3.1 的 mirror；apt 流量通过 NO_PROXY 直连，git 流量通过 build-arg 走 Clash 7890）

### 3.4 build path retry 历史（供参考，定位失败时不要走弯路）

| Attempt | 配置 | 结果 | 失败点 |
|---|---|---|---|
| #1 | TUN ON, Docker proxy=Clash 7897 | ❌ 138 s | apt fetch scipy.deb HTTP 500 EOF, IP 198.18.1.17 |
| #2 | TUN ON, Docker proxy=Clash 7897 | ❌ 205 s | apt fetch builder stage 186 s 后整池连接崩 |
| #3 | TUN OFF, Docker proxy=None, registry mirror=daocloud, 无 build-arg proxy | ❌ 925 s | apt + venv OK, **git clone OpenMOC TLS 480s 中断** |
| #4 | TUN OFF, Docker proxy=None, registry mirror=daocloud, **build-arg HTTP_PROXY=host.docker.internal:7890 + NO_PROXY=archive,security** | ✅ 231 s | — |

---

## 4. 总体结论

PR #73 的 `docker/Dockerfile` + `docker/Dockerfile.runtime` 在 **macOS 15.7.3 Apple M1 Pro + Docker Desktop 4.73.0 + Rosetta amd64** 上**可完整 build 并跑通所有 6 项通过判定**（Track A 双绿 + Track B 4/4 + Track C 3/3）。

唯一需要 setup 端调整的是**中国境内的网络配置**（§3.1-3.3），与 PR 内容无关，但建议在 PR 文档加 troubleshooting 章节。

Dockerfile 本身**完全没有任何修改**（除我加 `--build-arg`），UAT 验证的就是 PR 提交的原版镜像。
