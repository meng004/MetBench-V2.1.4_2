# Round-2 Docker SUT — macOS (Apple Silicon, Rosetta amd64) — 2026-05-19 — limeng

## 元信息

| 项 | 值 |
|---|---|
| 轮次 | round-2 docker SUT (all-in-container) |
| 日期 | 2026-05-19 |
| 测试员 | limeng |
| 平台 | macOS 15.7.3 (Sequoia, Darwin 24.6.0) |
| 机器 | MacBook Pro 18,3 — Apple M1 Pro (10-core: 8P+2E), 32 GB RAM |
| Docker Desktop | 4.73.0 (Engine 29.4.3, client+server) |
| Docker VM 资源 | 10 CPU / 16 GB allocated |
| 仿真路径 | linux/amd64 via **Rosetta amd64 emulation**（`docker run --platform=linux/amd64 alpine uname -m` → `x86_64` 已验证）|
| Docker 镜像源 | `https://docker.m.daocloud.io`（注册到 daemon.json `registry-mirrors`）|
| 接手 commit | `5530813 feat(docker): all-in-container runtime image + VM task doc rewrite` |
| 测试分支 | `claude/metbench-w11-2-experiments-QNIl6` |
| 总工时 | ~3 h（其中 ~1.5 h 是 Docker Desktop 安装 + 反复解决中国境内代理 / GitHub clone 网络问题，**镜像 build + 全 3 个 Track 总耗时 ~7 min**）|

## 6 项通过判定

| 步骤 | 通过条件 | 实测 | 通过? |
|---|---|---|---|
| 3.2 sut build | `metbench-sut:latest` 存在，size ≤ 1.5 GB | **1.06 GB** (DISK USAGE) / 247.4 MB (CONTENT SIZE) | ☑ |
| 3.2 runtime build | `metbench-runtime:latest` 存在，size ≤ 2.0 GB | **1.76 GB** (DISK USAGE) / 431.2 MB (CONTENT SIZE) | ☑ |
| 4.1 OpenMOC smoke | JSON 含 `converged: true`，k_eff ∈ [1.10, 1.20] | k_eff = **1.13306**, converged=true | ☑ |
| 4.1 OpenMC smoke | JSON 含 `converged: true`，k_eff ∈ [1.08, 1.16] | k_eff = **1.12450 ± 0.00179** (60 batches × 5000 particles), converged=true | ☑ |
| 4.2 全 4 scenario | `Total tests: 4 / Passed: 4` | **4/4 ✅**, test time 34.29 s / wall 87.87 s | ☑ |
| 4.3 smoke | `Total tests: 3 / Passed: 3` | **3/3 ✅**, test time 9.46 s | ☑ |

**总评：☑ PASS**

## 与 cloud baseline 对比

| 指标 | cloud baseline (Linux x86_64) | macOS M1 Pro + Rosetta | 备注 |
|---|---|---|---|
| sut image size | ~1.07 GB | 1.06 GB | 严丝合缝 |
| runtime image size | ~1.76 GB | **1.76 GB** | 严丝合缝（见 findings.md §2.3：build 完成的瞬间 DISK USAGE 列显示 431 MB，几分钟后才稳定到 1.76 GB，疑似 layer dedup index 异步收敛）|
| Track A OpenMOC k_eff | ≈ 1.133 | **1.13306** | 确定性精匹（OpenMOC 是 deterministic transport, 无随机源）|
| Track A OpenMC k_eff | ≈ 1.12 | **1.12450 ± 0.00179** | 蒙卡，落在 1.96σ 内（baseline 估计应在 1.10-1.14 范围）|
| Track B 4-scenario | 4/4, 37.4 s | **4/4, 34.3 s** | 几乎等同 |
| 镜像 build 总耗时 (estimate) | 35-60 min | **<5 min**（attempt 4 含大量 BuildKit cache 命中）/ ~30 min wall（含 attempts 1-3 的失败重试）| Rosetta 在 M1 Pro 上比预期快得多；首跑预期 12-18 min |

## 文件

- [`findings.md`](findings.md) — 实测数值 + 偏差 + 中国网络下 macOS 环境的 setup gotchas
- [`evidence/`](evidence/) — `docker images` snapshot、build / test console tails、Track A JSON outputs

## 给 PR #73 的快速反馈

详见 findings.md。摘要：
1. macOS 在中国网络下 build SUT 需要 **registry mirror + build-arg HTTP_PROXY + NO_PROXY** 三件套。建议把这条作为 README/troubleshooting 章节补进 PR 文档。
2. 任务书 §4.3 Track C 的 `--no-build` 在 `docker run --rm` 模式下会让 dotnet test 找不到测试程序集（容器销毁后 build artifacts 不在容器内），实际跑要把 `--no-build` 去掉，让 rebuild 走 host bind-mount 上已存在的 bin/obj。建议任务书在 Track C 命令上加注释。
3. Apple Silicon + Rosetta 实测比任务书预估快 ~10×（OpenMC pincell 5.9 s vs 60-90 s estimate），可放宽 macOS 端的耐心阈值。
