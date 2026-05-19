VM round-2 docker SUT (macOS Apple Silicon, Rosetta amd64) 完成 ✅

- **平台**: macOS 15.7.3 (Sequoia) + MacBook Pro M1 Pro (10-core: 8P+2E) / 32 GB RAM + Docker Desktop 4.73.0 (Engine 29.4.3)
- **仿真路径**: `linux/amd64` via Rosetta amd64 emulation（已验证 `docker run --platform=linux/amd64 alpine uname -m` → `x86_64`），Docker VM 配 10 CPU / 16 GB RAM
- **镜像 size**: sut **1.06 GB** (✓ ≤ 1.5 GB) / runtime **1.76 GB** (✓ ≤ 2.0 GB) — 与 cloud baseline 严丝合缝
- **Build 耗时**: sut 最终成功 attempt **3 min 51 s**（含大量 BuildKit cache 命中），runtime **1 min 18 s**。纯净首跑估算 sut ~12-18 min，比任务书预估 35-60 min 快很多——Apple Silicon Rosetta 实测高效
- **Track A**: ✅
  - OpenMOC k_eff = **1.13306**, iterations=553, converged=true（1.14 s）
  - OpenMC k_eff = **1.12450 ± 0.00179** (60 batches × 5000 particles), converged=true（5.92 s）
- **Track B**: ✅ **4/4 pass**, test time **34.29 s** / wall **87.87 s**（含 nuget restore + build），cloud baseline 37.4 s 几乎等同
- **Track C**: ✅ **3/3 pass**, test time 9.46 s（rebuild path，见偏差 4）
- **偏差 / Findings**（**对 PR 文档有补充建议**）:
  1. **中国境内 macOS 环境**: build SUT 需要"registry mirror + build-arg HTTP_PROXY + NO_PROXY"三件套，否则 docker pull 超时、git clone github.com 480s 后 TLS 中断、apt 经 Clash TUN 在持续负载下崩溃。详见报告 §3。**建议任务书加 troubleshooting 章节**。
  2. **任务书 §4.3 Track C 的 `--no-build`**: 在 `docker run --rm` 模式下会让 dotnet test 找不到 build artifacts，实际跑要去掉 `--no-build` 让 rebuild 路径走（多 ~40 s）。建议任务书改命令或加注释。
  3. **OpenMC k_eff baseline**: 任务书估值 1.12，实测 1.12450 ± 0.00179 落在统计置信区间内，**Rosetta 仿真未引入可见 MC 数值偏移** —— 这条对 PR 是正向证据。
  4. **runtime image DISK USAGE 报告口径**: build 结束的瞬间 `docker images` DISK USAGE 列显示 431 MB（与 CONTENT SIZE 一致），几分钟后才稳定到 1.76 GB——疑似 Docker Engine 29.x layer dedup index 异步收敛。最终值正确，建议任务书 "查看 size" 步骤加一句"等 30s 让 daemon 刷新 index 再读"。
- **Dockerfile 没有任何修改**，UAT 验证的就是 PR 提交的原版镜像。
- **报告**: `docs/uat/reports/round-2-docker-sut-2026-05-19-limeng-macos/`
  - `README.md` — 6 项判定 + 元信息 + cloud baseline 对比表
  - `findings.md` — 实测数值 + 偏差详解 + 中国网络下的 setup gotchas + 4 次 build attempt 历史
  - `evidence/` — docker images snapshot、build/test console tails、Track A 两份 JSON
