# SP2 变异体 T6 真实跑通 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**状态**: 完成（Task 1-6 全交付；容器内全 48 变异真跑，347 ran/73 detected，硬性质通过，2026-06-14）
**Spec**: `docs/superpowers/specs/2026-06-13-sp2-mutation-real-run-design.md`
**分支**: `sp2-mutation-real-run`（已存在，spec 已提交）

**Goal:** 在 SP1 的 `metbench-runtime` 容器内用 `tools/mutation_study.py` 真实跑全部 48 个变异体，产出 kill/survive 矩阵 + per-MR 检出率，并验证 Mut00 零误杀 / equivalent 存活 / semantic 检出。

**Architecture:** 容器内 env 覆盖 `OPENMOC_PYTHON=/opt/openmoc-venv/bin/python`、`OPENMC_PYTHON=/opt/openmc-venv/bin/python` 后串行跑 `baseline → screen --all → matrix --all-semantic → stats`；脚本已从 env 读这两个路径（`mutation_study.py:507-511`），默认无需改代码，仅在真有容器阻塞时做最小修正。输出落 `docs/experiments/_data/`（挂载目录，host 可见），整理为证据。

**Tech Stack:** Python `tools/mutation_study.py`（stdlib + openmoc/openmc venv）、Docker（metbench-runtime）。

**执行约定：**
- 环境：本机 Windows；`docker` 在 `"C:\Program Files\Docker\Docker\resources\bin\docker.exe"`；镜像 `metbench-runtime:latest` 已由 SP1 构建（含 openmoc venv + openmc venv + scipy）。仓库根 `D:\Codes\MetBench-V2.1.4_2`。
- 提交：多行消息写 `.git/COMMIT_MSG.txt` 后 `git commit -F`，用完删；trailer `Co-Authored-By: Claude <noreply@anthropic.com>`。
- §0.5 最小修改。
- 长任务（matrix 全量可能数小时）用后台运行。
- 已核实事实：脚本 `resolve_pythons()` 读 `OPENMOC_PYTHON`/`OPENMC_PYTHON`（`mutation_study.py:507-511`）；`openmc_available()` 以 `import openmc` 判定（`:514-533`）；输出 `docs/experiments/_data/{baseline.json,candidates/<id>/{screening,matrix}.json}`（`:55-57`）；幂等（已存在则跳过）；`SUBPROCESS_TIMEOUT_S=60`（`:536`）；子命令 `baseline`/`screen [--all|<ids>]`/`matrix [--all-semantic|<ids>]`/`stats`（`:10-23`）。

---

## File Structure

| 文件 | 动作 | 职责 |
|---|---|---|
| `docs/uat/sp2-mutation-real-run-runbook.md` | Create | 容器内四阶段命令 + 证据采集 |
| `tools/mutation_study.py` | Modify（仅当容器内有阻塞） | 最小环境接线修正（默认值/env 读取） |
| `tools/sp2_verify_acceptance.py` | Create | 从 `_data` 读 baseline/screening/matrix，机械验证 SP2 三性质，打印结论 + 退出码 |
| `docs/superpowers/specs/2026-06-13-sp2-mutation-real-run-evidence/` | Create（运行后） | baseline/screening/matrix 汇总 + stats 输出 + `sp2-summary.md` |
| `docs/status/current.md`、`docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md` | Modify | 状态投影（含把 SP1 行从"待 PR"翻为已合并 #364） |

---

## Task 1: 容器内冒烟（baseline + 1 个 mutant screen），坐实环境可跑

**Files:** 无（仅运行验证）；如发现阻塞才改 `tools/mutation_study.py`。

- [ ] **Step 1: baseline 冒烟**

Run（`docker` 用全路径）:
```
& "C:\Program Files\Docker\Docker\resources\bin\docker.exe" run --rm -v "D:\Codes\MetBench-V2.1.4_2:/work" -w /work metbench-runtime:latest env OPENMOC_PYTHON=/opt/openmoc-venv/bin/python OPENMC_PYTHON=/opt/openmc-venv/bin/python python3 tools/mutation_study.py baseline
```
Expected: 生成 `docs/experiments/_data/baseline.json`，含 openmoc + openmc 的 k_eff 基线（openmc 3 reps）。host 端 `Test-Path docs/experiments/_data/baseline.json` 为真。

- [ ] **Step 2: 单 mutant screen 冒烟**

Run（先跑一个语义明确的，如 Mut01）:
```
& "C:\Program Files\Docker\Docker\resources\bin\docker.exe" run --rm -v "D:\Codes\MetBench-V2.1.4_2:/work" -w /work metbench-runtime:latest env OPENMOC_PYTHON=/opt/openmoc-venv/bin/python OPENMC_PYTHON=/opt/openmc-venv/bin/python python3 tools/mutation_study.py screen Mut01-openmoc-runner-chi-zero
```
（实际 mutant id 以 `tools/mutations.py` 的 `ALL_MUTATIONS` 为准——先在容器内 `python3 -c "import sys; sys.path.insert(0,'tools'); from mutations import ALL_MUTATIONS; print([m.id for m in ALL_MUTATIONS][:5])"` 取真实 id。）
Expected: 生成 `docs/experiments/_data/candidates/<id>/screening.json`，分类为 semantic/equivalent/error。

- [ ] **Step 3: 处置阻塞（仅当 Step 1/2 失败）**

若失败，定位原因（venv 路径、openmc binary 不在 PATH、写权限、源案例路径等）。**最小修正** `tools/mutation_study.py`（只改默认值或 env 读取/可用性检测），不重构。记录改动。若是镜像缺东西（如 openmc binary 不在 venv bin），在 runbook 注明并用 env/PATH 解决，不改脚本逻辑。

- [ ] **Step 4: 提交（仅当有脚本修正）**

`.git/COMMIT_MSG.txt`:
```
fix(mutation): wire mutation_study.py to container venv paths

Minimal env-default fix so tools/mutation_study.py runs inside
metbench-runtime (openmc venv at /opt/openmc-venv, not the legacy
miniconda default). No behavior change beyond runtime resolution.

Co-Authored-By: Claude <noreply@anthropic.com>
```
```
git add tools/mutation_study.py
git commit -F .git/COMMIT_MSG.txt
```
删除 `.git/COMMIT_MSG.txt`。若无脚本改动，跳过本步。

---

## Task 2: 验收校验脚本（机械验证 SP2 三性质）

**Files:**
- Create: `tools/sp2_verify_acceptance.py`

机械读取 `_data` 产物、判定 SP2 spec §4 的可机判项，避免人工读 JSON 出错。

- [ ] **Step 1: 写脚本**

```python
#!/usr/bin/env python3
"""SP2 acceptance verifier: reads docs/experiments/_data and checks the
SP2 spec §4 mechanically-checkable properties:

  1. matrix produced for every semantic mutant (screening said semantic
     -> a matrix.json exists with >=1 cell).
  2. Mut00 identity: zero false-positive -> no scenario cell has
     outcome == "detected".
  3. equivalent mutants survive -> no detected cell (survival is correct;
     a detected cell is recorded as an anomaly, not a hard fail here).
  4. semantic mutants detected -> each semantic mutant has >=1 cell with
     outcome == "detected"; semantic mutants with zero detections are
     reported as coverage gaps (recorded, not hidden).

Exit code 0 iff hard properties (1, 2) hold AND stats are present.
Properties 3/4 anomalies are printed and summarized but do not flip the
exit code (they are T6 findings to record per spec §4).
"""
from __future__ import annotations

import json
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parent.parent
DATA = REPO / "docs" / "experiments" / "_data"
CAND = DATA / "candidates"


def load(p: Path):
    return json.loads(p.read_text(encoding="utf-8")) if p.exists() else None


def mut_id_is_identity(mid: str) -> bool:
    return mid.lower().startswith("mut00")


def main() -> int:
    baseline = load(DATA / "baseline.json")
    if baseline is None:
        print("FAIL: baseline.json missing")
        return 1

    if not CAND.exists():
        print("FAIL: candidates/ missing")
        return 1

    screened = {}
    for d in sorted(CAND.iterdir()):
        s = load(d / "screening.json")
        if s is not None:
            screened[d.name] = s

    if not screened:
        print("FAIL: no screening.json found")
        return 1

    semantic = [k for k, v in screened.items()
                if v.get("classification") == "semantic"]
    equivalent = [k for k, v in screened.items()
                  if v.get("classification") == "equivalent"]

    hard_ok = True
    anomalies: list[str] = []

    # property 1: every semantic mutant has a matrix with >=1 cell
    missing_matrix = []
    for mid in semantic:
        m = load(CAND / mid / "matrix.json")
        if m is None or not m.get("cells"):
            missing_matrix.append(mid)
    if missing_matrix:
        hard_ok = False
        print(f"FAIL(P1): semantic mutants without matrix: {missing_matrix}")
    else:
        print(f"OK(P1): {len(semantic)} semantic mutants each have a matrix")

    # property 2: Mut00 identity zero false-positive
    id_detected = []
    for mid, s in screened.items():
        if not mut_id_is_identity(mid):
            continue
        m = load(CAND / mid / "matrix.json")
        if m:
            for c in m.get("cells", []):
                if c.get("outcome") == "detected":
                    id_detected.append((mid, c.get("scenario_id")))
        # screening should also be non-semantic for identity
        if s.get("classification") == "semantic":
            id_detected.append((mid, "screening=semantic"))
    if id_detected:
        hard_ok = False
        print(f"FAIL(P2): identity mutant flagged detected: {id_detected}")
    else:
        print("OK(P2): Mut00 identity has zero false-positive detections")

    # property 3: equivalent survive (anomaly if detected)
    for mid in equivalent:
        m = load(CAND / mid / "matrix.json")
        if m and any(c.get("outcome") == "detected" for c in m.get("cells", [])):
            anomalies.append(f"equivalent mutant {mid} was detected (unexpected kill)")

    # property 4: semantic detected by >=1 MR (gap if none)
    gaps = []
    for mid in semantic:
        m = load(CAND / mid / "matrix.json")
        detected = m and any(c.get("outcome") == "detected" for c in m.get("cells", []))
        if not detected:
            gaps.append(mid)
    if gaps:
        anomalies.append(f"semantic mutants with NO MR detection (coverage gaps): {gaps}")
    print(f"INFO: semantic={len(semantic)} equivalent={len(equivalent)} "
          f"detected-gaps={len(gaps)}")

    if anomalies:
        print("ANOMALIES (recorded per spec §4, do not flip exit code):")
        for a in anomalies:
            print(f"  - {a}")

    print(f"RESULT: hard_properties_ok={hard_ok}")
    return 0 if hard_ok else 1


if __name__ == "__main__":
    raise SystemExit(main())
```

- [ ] **Step 2: 语法自检**（不依赖数据，应能 import / --help 不崩）

Run（host 或容器）:
```
& "C:\Program Files\dotnet\dotnet.exe" --version  # 占位，无关
python -c "import ast; ast.parse(open(r'D:\Codes\MetBench-V2.1.4_2\tools\sp2_verify_acceptance.py').read())"
```
若 host 无可用 python，则在容器内 `python3 -c "import ast; ast.parse(open('tools/sp2_verify_acceptance.py').read())"`。
Expected: 无语法错误。

- [ ] **Step 3: 提交**

`.git/COMMIT_MSG.txt`:
```
test(sp2): add mechanical acceptance verifier for mutation kill matrix

Reads docs/experiments/_data and checks SP2 spec property 1 (matrix per
semantic mutant) + property 2 (Mut00 zero false-positive) as hard gates;
reports equivalent-survival and semantic-detection gaps as recorded
anomalies (T6 findings) without masking.

Co-Authored-By: Claude <noreply@anthropic.com>
```
```
git add tools/sp2_verify_acceptance.py
git commit -F .git/COMMIT_MSG.txt
```

---

## Task 3: runbook

**Files:**
- Create: `docs/uat/sp2-mutation-real-run-runbook.md`

- [ ] **Step 1: 写 runbook**，含逐字命令：
  1. 前置：Docker 引擎、`metbench-runtime:latest`（SP1 构建）。
  2. 四阶段命令（容器内，env 覆盖两个 venv 路径）：`baseline` → `screen --all` → `matrix --all-semantic` → `stats`。给出全路径 `docker run ... env OPENMOC_PYTHON=... OPENMC_PYTHON=... python3 tools/mutation_study.py <stage>`。
  3. 说明 matrix 全量可能数小时，建议后台跑；幂等可断点续跑。
  4. 验收校验：容器内或 host 跑 `python3 tools/sp2_verify_acceptance.py`，期望 `hard_properties_ok=True`，anomalies 如实记录。
  5. 证据采集：`docs/experiments/_data/` 下 baseline/screening/matrix + stats 输出，整理进 evidence 目录。
  6. CI 边界：本流程不进 CI（离线科研），不改 workflow。

- [ ] **Step 2: 提交**

`.git/COMMIT_MSG.txt`:
```
docs(uat): add SP2 mutation real-run container runbook

Co-Authored-By: Claude <noreply@anthropic.com>
```
```
git add docs/uat/sp2-mutation-real-run-runbook.md
git commit -F .git/COMMIT_MSG.txt
```

---

## Task 4: 执行全量变异测试（容器内）

**Files:** 运行产物落 `docs/experiments/_data/`（不直接提交原始 _data，整理后归档进 evidence）。

- [ ] **Step 1: baseline + screen --all**

Run:
```
& "C:\Program Files\Docker\Docker\resources\bin\docker.exe" run --rm -v "D:\Codes\MetBench-V2.1.4_2:/work" -w /work metbench-runtime:latest env OPENMOC_PYTHON=/opt/openmoc-venv/bin/python OPENMC_PYTHON=/opt/openmc-venv/bin/python bash -lc "python3 tools/mutation_study.py baseline && python3 tools/mutation_study.py screen --all"
```
Expected: 48 个 `candidates/<id>/screening.json`；记录 semantic/equivalent/error 计数。

- [ ] **Step 2: matrix --all-semantic（长任务，后台）**

Run（后台）:
```
& "C:\Program Files\Docker\Docker\resources\bin\docker.exe" run --rm -v "D:\Codes\MetBench-V2.1.4_2:/work" -w /work metbench-runtime:latest env OPENMOC_PYTHON=/opt/openmoc-venv/bin/python OPENMC_PYTHON=/opt/openmc-venv/bin/python python3 tools/mutation_study.py matrix --all-semantic
```
Expected: 每个 semantic mutant 一份 `matrix.json`（多 scenario cells）。幂等——中断可重跑续上。

- [ ] **Step 3: stats**

Run:
```
& "C:\Program Files\Docker\Docker\resources\bin\docker.exe" run --rm -v "D:\Codes\MetBench-V2.1.4_2:/work" -w /work metbench-runtime:latest env OPENMOC_PYTHON=/opt/openmoc-venv/bin/python OPENMC_PYTHON=/opt/openmc-venv/bin/python python3 tools/mutation_study.py stats
```
Expected: per-MR 检出率 + Wilson CI + Cohen's κ 的 CSV/Markdown 产出。

- [ ] **Step 4: 跑验收校验**

Run:
```
& "C:\Program Files\Docker\Docker\resources\bin\docker.exe" run --rm -v "D:\Codes\MetBench-V2.1.4_2:/work" -w /work metbench-runtime:latest python3 tools/sp2_verify_acceptance.py
```
Expected: `OK(P1)` + `OK(P2)` + `hard_properties_ok=True`；anomalies（equivalent 被杀 / semantic 缺口）如实打印。**若 P1/P2 FAIL，定位根因**（Mut00 误杀=MR/框架 bug，必须暴露），不伪装。

本 Task 无 git 提交（产物在下一步整理归档）。

---

## Task 5: 归档证据 + summary

**Files:**
- Create: `docs/superpowers/specs/2026-06-13-sp2-mutation-real-run-evidence/` 下 `sp2-summary.md` + stats 输出 + screening/matrix 汇总（或代表样本 + 全量计数）

- [ ] **Step 1: 整理证据**

把 `docs/experiments/_data/` 的 stats 输出（CSV/MD）、`baseline.json`、`sp2_verify_acceptance.py` 的输出文本复制/汇总进 evidence 目录。`candidates/` 全量 JSON 体积可能大——归档 stats + summary + 验收校验输出 + 抽样若干 matrix.json 即可（summary 里给全量计数与路径指针）。

- [ ] **Step 2: 写 `sp2-summary.md`**，含：
  - mutant 总数（48）与 screen 分类计数（semantic/equivalent/error 实际值）；
  - kill/survive 矩阵概览：semantic mutant 中被检出 / 存活计数；
  - **Mut00 零误杀**确认（验收校验 P2 OK）；
  - **equivalent 存活**确认（被杀的列为异常，如有）；
  - **semantic 检出**结论：被 ≥1 MR 杀死的数量；存活缺口 mutant 列表（如有，归因：等价? scenario 未覆盖? MR 缺口?）——如实，不掩盖；
  - per-MR 检出率与跨求解器 κ 关键数字（引 stats）；
  - 容器（metbench-runtime digest）、venv、耗时；
  - 对 `mutation_study.py` 的最小修正（如有）。

- [ ] **Step 3: 提交**

`.git/COMMIT_MSG.txt`:
```
docs(evidence): SP2 mutation real-run kill/survive matrix

Ran tools/mutation_study.py (baseline/screen --all/matrix --all-semantic/
stats) inside metbench-runtime against real openmoc/openmc. Archives the
stats output + acceptance-verifier result + summary: Mut00 zero
false-positive, equivalent survival, semantic detection (with any
coverage gaps recorded honestly).

Co-Authored-By: Claude <noreply@anthropic.com>
```
```
git add docs/superpowers/specs/2026-06-13-sp2-mutation-real-run-evidence/
git commit -F .git/COMMIT_MSG.txt
```

---

## Task 6: 状态投影 + PR

**Files:**
- Modify: `docs/status/current.md`（新增 SP2 行；并把 SP1 行从"待 PR"翻为"已合并 PR #364"）
- Modify: `docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md`（登记 SP2 plan；SP1 行标已合并）
- Modify: 本 plan 状态字段 → 完成

- [ ] **Step 1: 更新三处**（指针互引；SP2 行如实记 kill 矩阵关键数字 + 任何 semantic 缺口；不夸大）。
- [ ] **Step 2: 提交 + 推送 + 开 PR**

`.git/COMMIT_MSG.txt`:
```
docs(status): project SP2 mutation real-run status (+ mark SP1 merged)

Co-Authored-By: Claude <noreply@anthropic.com>
```
```
git add docs/status/current.md docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md docs/superpowers/plans/2026-06-13-sp2-mutation-real-run-plan.md
git commit -F .git/COMMIT_MSG.txt
git push -u origin sp2-mutation-real-run
```
- [ ] **Step 3: 开 PR**（gh，body 按 `docs/superpowers/templates/pr-gate-checklist.md` 7 节；Windows Classification = run-and-log；Tests 节贴 `sp2_verify_acceptance.py` 输出 + kill 矩阵关键数字；强调不改 CI、离线科研运行）。

---

## 最终验证（PR 前）

```
# 容器内验收校验通过（硬性质）
docker run ... python3 tools/sp2_verify_acceptance.py    # hard_properties_ok=True
git diff --check                                          # pass
```

## PR Gate Classification

- Scope：单一目的——SP2 变异体真实跑通 + kill 矩阵证据。
- Windows classification：`run-and-log`（容器内真实运行变异测试留矩阵/stats/summary）。代码仅 `tools/`（cloud-safe Python：验收校验脚本 + 可能的 env 最小修正），不碰 WPF/.NET/CI 门禁。
- 模块 E：单 PR，非 ≥3-PR chain。
- PR body 7 节；强调离线科研运行不进 CI；如实记录 semantic 存活缺口（T6 发现，非 SP2 失败）。
