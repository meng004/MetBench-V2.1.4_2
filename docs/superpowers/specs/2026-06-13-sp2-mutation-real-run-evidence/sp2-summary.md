# SP2 变异体 T6 真实跑通 证据汇总

日期：2026-06-14
机器：Windows 11 + Docker Desktop 29.5.3；容器 `metbench-runtime:latest`（openmoc venv + openmc venv + scipy）
依据：spec/plan `docs/superpowers/{specs,plans}/2026-06-13-sp2-mutation-real-run-*`
工具：`tools/mutation_study.py`（baseline/screen/matrix/stats）+ `tools/sp2_verify_acceptance.py`

## 1. 运行概况

容器内全量真跑（`OPENMOC_PYTHON=/opt/openmoc-venv/bin/python`、`OPENMC_PYTHON=/opt/openmc-venv/bin/python`、
`METBENCH_MUTATION_TIMEOUT_S=300`）：

```
baseline --force  → docs/experiments/_data/baseline.json
screen --all --force → 48 个 candidates/<id>/screening.json
matrix --all --force → 48 个 candidates/<id>/matrix.json（347 ran cells, 73 detected, 976 not-affected, 41 error）
stats → screening-results.{csv,md} + mutation-detection-matrix.{csv,md}（Wilson CI + Cohen's κ）
```

> 注：matrix 用 `--all`（全 48）而非 spec 原写的 `--all-semantic`。原因见 §4 偏离 D1——source-only
> screening 把适配器变异误判 equivalent，`--all-semantic` 会漏掉它们；`--all` 让 kill 矩阵本身
> （含 follow-up 信号）决定杀伤。

## 2. 验收判据（`sp2_verify_acceptance.py`，按 predicted_classification）

```
OK(P1): 41 semantic-intent mutants each have a matrix
OK(P2): Mut00 identity has zero false-positive detections
INFO: applicable=46 semantic-intent=41 equivalent-intent=4 inapplicable(drifted)=2
RESULT: hard_properties_ok=True
```

- **P1（硬）✓**：全部 41 个 semantic-intent 变异都有矩阵（matrix --all 跑了全部）。
- **P2（硬）✓**：Mut00 恒等基线 0/29 scenario 被检出（stats 也独立确认 "Mut00 detected on 0/29 — ✓ PASS"）。
- 48 = 46 applicable + 2 drifted；applicable 中 41 semantic-intent + 4 equivalent-intent（+ Mut00 恒等）。

## 3. kill/survive 矩阵概览

- semantic-intent 变异 41 个：**33 个被 ≥1 MR 杀死**，**8 个全程存活（覆盖缺口）**。
- 每 MR 检出率（Wilson 95% CI，stats 表）：**9.1% – 41.7%**；最高
  `openmc-pincell-fuel-sigma-s` 41.7%、`openmoc-pincell-nu-sigma-f` 37.5%；最低
  `*-fuel-sigma-t` / 多个对称 MR 9.1%。
- **跨求解器一致性（Cohen's κ）**：匹配对（同一概念缺陷注入 openmoc/openmc 双方）各 MR 族
  **κ = 1.000（almost perfect）**——OpenMOC 与 OpenMC 孪生变异的检出结论高度一致。

## 4. 真实 T6 发现（如实记录，不掩盖）

**D1 · 8 个 semantic 变异无任何 MR 检出（检测盲区）**：
`Mut06-openmoc-runner-vacuum-boundary`、`Mut08-openmoc-adapter-nsf-square`、
`Mut14-openmoc-adapter-sa-moderator`、`Mut21-openmc-runner-fission-zero`、
`Mut23-openmc-adapter-nsf-square`、`Mut26-openmc-adapter-sa-no-sigt-update`、
`Mut43-openmc-runner-clamp-y-offset-positive`、`Mut44-openmc-runner-clamp-x-offset-positive`。
这是当前 MR 套件的真实检测缺口（例如 nsf-square 把 ν·Σf 平方、vacuum-boundary 改边界条件——
都改变物理但现有 MR 的变换/断言不覆盖），是 T6 价值所在，供 MR 库后续补强（指向 T6 "最小 MR 完备子集"目标）。

**D2 · 2 个 equivalent-intent 变异被检出（MR 过敏 / MC 噪声）**：
`Mut09-openmoc-adapter-nsf-moderator`、`Mut24-openmc-adapter-nsf-moderator`——作者预期为等价
（只改 moderator 的 nu_sigma_f，物理上 moderator 无裂变应无影响），却被 nu-sigma-f MR 检出
（k_followup 与 source 比值偏离）。需复核是 MR 过敏还是该变异实际非等价。

**D3 · 2 个变异目录与 SUT 源漂移（inapplicable）**：
`Mut02-openmoc-runner-sigt-from-siga`（找不到 `m.setSigmaT(mat["sigma_t"])`）、
`Mut15-openmc-runner-chi-zero`——补丁目标字符串与当前 SUT 源对不上，记 error，需更新变异定义。

## 5. 对 `mutation_study.py` 的最小修正（容器内可复现的必要改动）

把变异研究脚本从"参考机专用、遇错即停"改造为"容器内可复现、单点故障容错"：
1. `SUBPROCESS_TIMEOUT_S` 改为 `METBENCH_MUTATION_TIMEOUT_S` 可配置（默认 60 不变）——容器 CPU 较慢，
   10× 粒子加密 follow-up 实测 75s > 60s。
2. `cmd_baseline` 的 followup 循环单 scenario 容错——temperature 类触发上游 OpenMC 0.15.3
   PR#3712（add_temperature）/ PR#3662（borated_water）bug，记 error 继续而非整体中止。
3. `screen_one` / `matrix_one` 的 `stage_sut`（apply）纳入容错——漂移变异记 error 继续，不中止全量。
（默认环境变量未设时行为与参考机逐字节一致。）

## 6. CI 边界

离线科研运行，**不进 CI**，未改 `.github/workflows/`。证据：`screening-results.{csv,md}`、
`mutation-detection-matrix.{csv,md}`、`sp2-verify-output.txt`（本目录）；完整 per-mutant JSON 在
`docs/experiments/_data/candidates/`（随本 PR 刷新提交，48 mutant 一致的新鲜全量）。

## 7. 结论

SP2 达成：48 个变异体在容器内对真实 openmoc/openmc 全部真跑，产出完整 kill/survive 矩阵 + per-MR
检出率 + 跨求解器 κ；硬性质（Mut00 零误杀、semantic 全有矩阵）通过；并如实交付 3 类 T6 发现
（8 检测盲区、2 疑似过敏、2 目录漂移）供后续 MR 库与变异目录维护。
