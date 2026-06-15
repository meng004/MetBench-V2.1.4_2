# SP2 设计：变异体 T6 真实跑通 + kill/survive 矩阵

日期：2026-06-13

## 0. 上位背景

大目标"为已导入全部 SUT/MR/算例/变异体建真实可异步运行环境并全部通过验收"的 **SP2**
（子项目 2/5）。SP1 已建好运行时底座（`metbench-runtime` 容器含 openmoc/openmc venv）。
SP2 真实跑 T6 变异测试，产出 kill/survive 矩阵并验证关键性质。SP3-SP5 各自开 spec。

## 1. 范围与目标

在容器内用权威的 `tools/mutation_study.py` 真实运行全部 48 个变异体的变异测试，
产出完整 kill/survive 矩阵 + per-MR 检出率统计，并验证三类关键性质（Mut00 零误杀、
equivalent 存活、semantic 被检出）。

现状（已核对）：
- `tools/mutations.py`：48 个 mutant（Mut00-47），`Mutation` 数据类含
  `apply(text)->str`、`predicted_classification`（semantic/equivalent/solver-dependent/error）、
  `predicted_detector`。Mut00 为恒等基线（`apply=lambda t: t`，预期不被任何 MR 杀）。
- `tools/mutation_study.py`：完整 CLI（baseline / screen / matrix / stats），在临时副本上
  应用 mutant（不改 `SUT/` 源树）、真跑 openmoc/openmc、`evaluate_mr` 判 kill、出 JSON + stats。
- .NET `MutationCampaignService`：框架在，生产 cellRunner 是 hash 模拟 stub（不碰真实 SUT）。
  **SP2 不动它**（Path B，单独立项）。

## 2. 关键约束（环境接线）

`mutation_study.py` 顶部默认 `OPENMC_PYTHON=/opt/miniconda3/envs/openmc-env/bin/python`，
该路径在 `metbench-runtime` 容器内**不存在**（容器为 `/opt/openmc-venv`）。`OPENMOC_PYTHON`
默认 `/opt/openmoc-venv/bin/python`（容器内存在）。SP2 通过**环境变量覆盖**这两个值运行；
若脚本未读环境变量或有其他容器内阻塞，做**最小修正**（仅改默认值/env 读取），以容器内实跑为准。

## 3. 架构与数据流

```
容器：metbench-runtime:latest（SP1 已构建；openmoc venv + openmc venv + scipy）
docker run --rm -v "<repo>:/work" -w /work metbench-runtime:latest \
  env OPENMOC_PYTHON=/opt/openmoc-venv/bin/python \
      OPENMC_PYTHON=/opt/openmc-venv/bin/python \
  bash -lc "
    python3 tools/mutation_study.py baseline &&
    python3 tools/mutation_study.py screen --all &&
    python3 tools/mutation_study.py matrix --all-semantic &&
    python3 tools/mutation_study.py stats
  "
        │
   ├─ baseline：原始 source case 跑 openmoc(1 rep) + openmc(3 reps) → baseline.json（k_eff 基线 + σ）
   ├─ screen --all：每个 mutant 应用到副本、跑 source case，按
   │   |Δk| > max(3σ, 0.5%·k_baseline) 判 semantic/equivalent/error → candidates/<id>/screening.json
   ├─ matrix --all-semantic：semantic mutant × 全 scenario（openmoc/openmc 各 MR 变体）；
   │   每 cell：应用 mutant→跑 source→apply_transformation 生成 follow-up→跑 follow-up→
   │   evaluate_mr 判 assertion → outcome ∈ {detected, missed, error, not-affected}
   │   → candidates/<id>/matrix.json
   └─ stats：汇总 per-MR 检出率 + Wilson CI + 跨求解器 Cohen's κ → CSV/Markdown
        │
   输出落 _data/candidates/（挂载目录，host 可见）。
```

变异目标是 `SUT/openmoc/` 与 `SUT/openmc/` 下的真实脚本（runner/adapter/parser）；
`stage_sut()` 把 `SUT/` copytree 到临时目录后在副本上 patch，源树不被改。

## 4. 验收判据

1. **完整矩阵产出**：全部 semantic mutant × 全 scenario 的 matrix.json 生成，stats 汇总成功。
2. **Mut00 零误杀**：matrix/screening 中 Mut00 对所有 scenario 的 outcome 均非 detected
   （任何 detected = MR 或框架 bug，必须暴露并定位，不掩盖）。
3. **equivalent mutant 存活**：被分类为 equivalent 的 mutant 不被 MR 杀死（被杀=记录为异常）。
4. **semantic mutant 检出**：每个 semantic mutant 至少被其 `predicted_detector` 中一个 MR 杀死；
   若某 semantic 全程存活 = 真实检出缺口，**如实记录**（SP2 的价值正是发现这类缺口，不伪装通过）。
5. **统计产出**：per-MR 检出率与跨求解器 κ 数值产出。

判据 1/2/5 是 SP2 的"通过"硬条件；3/4 的异常项以**如实记录 + 归因**满足（真实缺陷不掩盖，
属 T6 检出结果而非 SP2 失败）。

## 5. 错误处理

- 容器内某 venv 不可用 / 脚本阻塞：最小修正环境接线后重跑；仍不通则**显式报告**阻塞点与
  已完成/未完成阶段，不伪造矩阵。
- 单 cell 执行失败：脚本记 outcome=error，campaign 继续；stats 区分 error 与 missed。
- baseline 失败：终止并报告（无基线则 screen/matrix 无意义）。

## 6. 证据与 CI 边界

- 这是**离线科研运行**，**不是 CI 测试**——CI 从不跑 `mutation_study.py`，本 spec **不改 CI**。
- 证据归档 `docs/superpowers/specs/2026-06-13-sp2-mutation-real-run-evidence/`：
  - `baseline.json`、各 `candidates/<id>/{screening,matrix}.json` 的汇总或代表样本；
  - `stats` 的 CSV/Markdown 输出；
  - `sp2-summary.md`：mutant 总数与分类计数、kill/survive 矩阵概览、per-MR 检出率、
    Mut00 零误杀确认、equivalent 存活确认、semantic 检出结论（含存活缺口如有）、
    容器/venv/耗时、对 `mutation_study.py` 的最小修正（如有）。
- runbook `docs/uat/sp2-mutation-real-run-runbook.md`：容器内四阶段命令 + 证据采集。

## 7. 交付物 / 不交付

交付：
1. `mutation_study.py` 的最小环境接线修正（如容器内需要）。
2. runbook + 一次容器内全量实跑的证据（矩阵 + stats + summary）。
3. 状态账本 / 活跃计划索引投影。

不交付：.NET `MutationCampaignService` 接真实执行（Path B）、`IMutantApplicator`、
WPF 变异 campaign UI、SUT-root per-run 覆盖、SP3-SP5、新增/修改变异体定义。

## 8. Windows Classification

`run-and-log`：容器内真实运行变异测试并留矩阵/stats/summary 证据。代码改动（如有）仅限
`tools/mutation_study.py` 的环境默认值/读取（cloud-safe Python 工具），不碰 WPF/.NET/CI 门禁。
