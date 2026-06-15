# SP2 变异体 T6 真实跑通 Runbook

日期：2026-06-13
依据：spec `docs/superpowers/specs/2026-06-13-sp2-mutation-real-run-design.md`

目标：在 SP1 的 `metbench-runtime` 容器内用 `tools/mutation_study.py` 真实跑全部 48 个变异体，
产出 kill/survive 矩阵 + per-MR 检出率，并机械验证 Mut00 零误杀 / equivalent 存活 / semantic 检出。

## 0. 前置

- Docker Desktop 引擎运行；`docker` CLI（本机 `C:\Program Files\Docker\Docker\resources\bin\docker.exe`）。
- `metbench-runtime:latest`（SP1 构建，含 `/opt/openmoc-venv` + `/opt/openmc-venv` + scipy）。
- 仓库根挂载到容器 `/work`。

## 1. 环境接线

`tools/mutation_study.py` 从环境变量读运行时（`mutation_study.py:507-511`），脚本默认 OPENMC 指向
不存在的 miniconda 路径，故**必须用 env 覆盖**：
```
OPENMOC_PYTHON=/opt/openmoc-venv/bin/python
OPENMC_PYTHON=/opt/openmc-venv/bin/python
```
`openmc_available()` 以 `import openmc` 判定（`:514-533`）；覆盖后两 solver 均可用。

## 2. 四阶段（容器内，全量 --force 重算）

`<DOCKER>` = `C:\Program Files\Docker\Docker\resources\bin\docker.exe`；`<REPO>` = `D:\Codes\MetBench-V2.1.4_2`。

```
& "<DOCKER>" run --rm -v "<REPO>:/work" -w /work metbench-runtime:latest `
  env OPENMOC_PYTHON=/opt/openmoc-venv/bin/python OPENMC_PYTHON=/opt/openmc-venv/bin/python `
  bash -lc "python3 tools/mutation_study.py baseline --force && \
            python3 tools/mutation_study.py screen --all --force && \
            python3 tools/mutation_study.py matrix --all-semantic --force && \
            python3 tools/mutation_study.py stats"
```

- `baseline`：原始 source case 跑 openmoc(1 rep) + openmc(3 reps) → `docs/experiments/_data/baseline.json`。
- `screen --all`：48 mutant 逐个应用到 SUT 临时副本、跑 source case，按 `|Δk|>max(3σ,0.5%·k)` 分类
  semantic/equivalent/error → `_data/candidates/<id>/screening.json`。
- `matrix --all-semantic`：semantic mutant × 全 scenario，每 cell 应用 mutant→跑 source→生成 follow-up→
  跑 follow-up→`evaluate_mr` 判 assertion → outcome ∈ {detected, missed, error, not-affected}
  → `_data/candidates/<id>/matrix.json`。
- `stats`：per-MR 检出率 + Wilson CI + 跨求解器 Cohen's κ → CSV/Markdown。

**耗时**：matrix 全量（semantic × scenario，openmc 多 rep）可能数小时——建议后台跑。脚本幂等
（默认跳过已存在结果；`--force` 强制重算），中断可续。

**已知上游噪声**：openmc 0.15.3 的 `add_temperature` 触发 openmc-pr-3712 bug，使
`openmc-pincell-fuel-temperature-*` 这一类 followup 基线记为 error——属上游缺陷，非 MetBench/SP2 故障，
脚本以 status=error 容错，stats 区分 error 与 missed。

## 3. 验收校验（机械）

```
& "<DOCKER>" run --rm -v "<REPO>:/work" -w /work metbench-runtime:latest python3 tools/sp2_verify_acceptance.py
```
判据：
- `OK(P1)`：每个 semantic mutant 有 matrix（≥1 cell）。
- `OK(P2)`：Mut00 恒等零误杀（无 detected cell、screening 非 semantic）。
- `hard_properties_ok=True` → 退出码 0。
- ANOMALIES：equivalent 被杀 / semantic 无检出（覆盖缺口）如实打印——属 T6 发现，记录不掩盖，不翻退出码。

P1/P2 FAIL 必须定位根因（Mut00 误杀 = MR/框架 bug）。

## 4. 证据采集

整理进 `docs/superpowers/specs/2026-06-13-sp2-mutation-real-run-evidence/`：
- `stats` 的 CSV/Markdown 输出；
- `sp2_verify_acceptance.py` 输出文本；
- `sp2-summary.md`：分类计数、kill/survive 概览、per-MR 检出率、κ、Mut00/equivalent/semantic 三类结论、
  存活缺口（如有，归因）、容器 digest/耗时、脚本最小修正（如有）。
- 全量 `_data/candidates/` JSON 体积较大：summary 给全量计数 + 路径指针，evidence 目录放 stats + 校验输出 +
  抽样若干 matrix.json 即可。

## 5. CI 边界

本流程**不进 CI**（离线科研运行），不改 `.github/workflows/`。CI 从不跑 `mutation_study.py`。
