# SP3a UAT trx-backed 验收 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**状态**: 待执行
**Spec**: `docs/superpowers/specs/2026-06-15-sp3a-uat-trx-acceptance-design.md`
**分支**: `sp3a-uat-trx-acceptance`（已存在，spec 已提交）

**Goal:** 把 UAT rubric 的 22 个测试支撑类用例真实跑出 pass 计数、按判据如实判定、就地填 `acceptance-rubric.md` 结果/证据列，并归档 trx 证据。

**Architecture:** host 跑整套 `MetBench_SystemMT.Tests` 出 trx；`tools/sp3a_rubric_report.py` 按"用例→测试类→判据"映射解析 trx 得每用例真实 Passed/Failed 计数与达标判定；C10/F1 疑似缺口先看真实计数，确属缺口补有意义测试；C11 容器内复核 openmc；G2 跑 perf 脚本。CI 门禁不变。

**Tech Stack:** .NET 8 xUnit（trx）、Python（报告脚本 + G2 perf）、Docker（metbench-runtime，C11）。

**执行约定：**
- `dotnet`=`"C:\Program Files\dotnet\dotnet.exe"`；`docker`=`"C:\Program Files\Docker\Docker\resources\bin\docker.exe"`；测试 python 用 `$env:METBENCH_TEST_PYTHON="C:\Users\lemon\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe"`。
- 多行 commit：写 `.git/COMMIT_MSG.txt` 后 `git commit -F`，删除；trailer `Co-Authored-By: Claude <noreply@anthropic.com>`。
- §0.5 最小修改；§4 真实验证（不凑数）；§6 显式报错（缺口如实标）。
- 已核实：22 用例的测试类均存在于 `MetBench_SystemMT.Tests`（V2Schema/V2Discovery/V2RCaseRepro/MethodMT 等子目录），类名见 spec §1 映射表。`tools/ci_perf_baseline.py` 签名：`python tools/ci_perf_baseline.py --trx <trx> --total-budget-seconds 120.0`，exit 0 = 通过。

---

## File Structure

| 文件 | 动作 | 职责 |
|---|---|---|
| `tools/sp3a_rubric_report.py` | Create | trx 解析 + 22 用例映射 → 每用例 Passed/Failed 计数 + 达标判定，打印 + 退出码 |
| `MetBench_SystemMT.Tests/V2Discovery/ScgHeuristicDiscovererTests.cs` | Modify（仅 C10 确属缺口时） | 补三类 pattern 真实验证测试 |
| `MetBench_SystemMT.Tests/V2Schema/V2DbConfigRegistrationTests.cs` | Modify（仅 F1 确属缺口时） | 补 3 级 override 真实场景测试 |
| `docs/uat/acceptance-rubric.md` | Modify | 填 22 行结果/证据列（必要时 retro-touch 陈旧阈值） |
| `docs/superpowers/specs/2026-06-15-sp3a-uat-trx-evidence/` | Create | trx(host+容器) + G2 输出 + 报告脚本输出 + sp3a-summary.md |
| `docs/status/current.md`、active plan index | Modify | 状态投影 |

22 用例→测试类→判据映射（报告脚本内置同表）：
A8 `MethodMtCatalogCrudTests` >0 / C1 `RealSamplerTests` ≥4 / C2 `ValidatorTests` ≥5 /
C3 `MRPairingServiceTests` ≥11 / C4 `MultiLlmConsensusValidatorTests` ≥15 / C5 `ValidationServiceTests` >0 /
C10 `ScgHeuristicDiscovererTests` ≥29 / C11 `OpenMcRunnerSmokeTests`(+`CrossProgramNeutronTransportMrs` openmc) =1 /
D1 `RCaseReproductionServiceTests` ≥9 / D2 fact `WriteAudit_records_r_case_reproduced` /
E6 `SystemMtReportServiceTests` ≥6 / E7 `HtmlSystemMtResultReportRendererTests` >0 /
F1 `V2DbConfigRegistrationTests` ≥5 / F2 `MetaPatternEntityTests` ≥11 / F3 `MRBindingStatusTests` ≥7 /
F4 `V2SoftDeleteAndMigrationTests` ≥9 / F5 `V2RepositoryDIBindingTests` >0 / G1 `KeysetPaginationTests` ≥10 /
G2 `ci_perf_baseline.py` / G4 `CoverageServiceTests` ≥5 / G5 `AnomalyServiceTests` ≥8。

---

## Task 1: trx 解析报告脚本

**Files:** Create `tools/sp3a_rubric_report.py`

- [ ] **Step 1: 写脚本**

```python
#!/usr/bin/env python3
"""SP3a UAT report: parse a VSTest .trx and report real Passed/Failed
counts per test-backed rubric case, with pass/fail verdict vs criterion.

Usage:
    python tools/sp3a_rubric_report.py --trx <path-to.trx>

Each rubric case maps to a test-class name substring; the trx is grouped
by the class portion of each UnitTestResult testName. D2 is a single named
fact. Exit 0 iff every case whose tests are PRESENT in this trx meets its
criterion (cases with zero present tests are reported as MISSING, e.g.
C11 on a host trx where the openmc smoke is skipped/absent, and do not
flip the exit code so a host-only trx can still pass the non-openmc rows).
"""
from __future__ import annotations

import argparse
import xml.etree.ElementTree as ET
from pathlib import Path

NS = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}

# case_id -> (class substring, min_passed). min_passed None => ">0".
CASES = {
    "A8": ("MethodMtCatalogCrudTests", 1),
    "C1": ("RealSamplerTests", 4),
    "C2": ("ValidatorTests", 5),
    "C3": ("MRPairingServiceTests", 11),
    "C4": ("MultiLlmConsensusValidatorTests", 15),
    "C5": ("ValidationServiceTests", 1),
    "C10": ("ScgHeuristicDiscovererTests", 29),
    "C11": ("OpenMcRunnerSmokeTests", 1),
    "D1": ("RCaseReproductionServiceTests", 9),
    "E6": ("SystemMtReportServiceTests", 6),
    "E7": ("HtmlSystemMtResultReportRendererTests", 1),
    "F1": ("V2DbConfigRegistrationTests", 5),
    "F2": ("MetaPatternEntityTests", 11),
    "F3": ("MRBindingStatusTests", 7),
    "F4": ("V2SoftDeleteAndMigrationTests", 9),
    "F5": ("V2RepositoryDIBindingTests", 1),
    "G1": ("KeysetPaginationTests", 10),
    "G4": ("CoverageServiceTests", 5),
    "G5": ("AnomalyServiceTests", 8),
}
# D2 is a single named fact (substring match on test name).
D2_FACT = "WriteAudit_records_r_case_reproduced"


def parse(trx_path: Path):
    root = ET.parse(trx_path).getroot()
    results = []
    for r in root.iterfind(".//t:UnitTestResult", NS):
        results.append((r.get("testName", ""), r.get("outcome", "")))
    return results


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--trx", required=True)
    args = ap.parse_args()

    results = parse(Path(args.trx))

    def counts(sub: str):
        p = sum(1 for n, o in results if sub in n and o == "Passed")
        f = sum(1 for n, o in results if sub in n and o == "Failed")
        return p, f

    overall_ok = True
    print(f"{'case':6} {'class':45} {'passed':>6} {'failed':>6} {'min':>4} verdict")
    for case, (sub, mn) in CASES.items():
        p, f = counts(sub)
        present = (p + f) > 0
        if not present:
            verdict = "MISSING(present in another trx?)"
        elif f > 0:
            verdict = "FAIL(failed>0)"; overall_ok = False
        elif p >= mn:
            verdict = "PASS"
        else:
            verdict = f"SHORT({p}<{mn})"; overall_ok = False
        print(f"{case:6} {sub:45} {p:>6} {f:>6} {mn:>4} {verdict}")

    # D2 single fact
    d2 = [(n, o) for n, o in results if D2_FACT in n]
    if not d2:
        print(f"D2     {D2_FACT:45} {'-':>6} {'-':>6} {'1':>4} MISSING")
    else:
        ok = all(o == "Passed" for _, o in d2)
        print(f"D2     {D2_FACT:45} {sum(1 for _,o in d2 if o=='Passed'):>6} "
              f"{sum(1 for _,o in d2 if o=='Failed'):>6} {'1':>4} {'PASS' if ok else 'FAIL'}")
        overall_ok = overall_ok and ok

    print(f"\nRESULT: present_cases_ok={overall_ok}")
    return 0 if overall_ok else 1


if __name__ == "__main__":
    raise SystemExit(main())
```

- [ ] **Step 2: 语法自检**

Run: `python -c "import ast; ast.parse(open(r'D:\Codes\MetBench-V2.1.4_2\tools\sp3a_rubric_report.py').read())"`（host python 不可用则容器内 `python3 -c "import ast; ast.parse(open('tools/sp3a_rubric_report.py').read())"`）
Expected: 无语法错误。

- [ ] **Step 3: 提交** — `test(sp3a): add UAT trx rubric report parser`

---

## Task 2: host 跑全套件 → trx，得真实计数

**Files:** 无（运行产物）

- [ ] **Step 1: host 全套件跑出 trx**

Run:
```
$env:METBENCH_TEST_PYTHON="C:\Users\lemon\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe"
& "C:\Program Files\dotnet\dotnet.exe" test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --logger "trx;LogFileName=sp3a-host.trx"
```
Expected: 套件通过（参 SP1：~1895 passed）。trx 落 `MetBench_SystemMT.Tests/TestResults/sp3a-host.trx`。

- [ ] **Step 2: 跑报告脚本**

Run: `python tools/sp3a_rubric_report.py --trx MetBench_SystemMT.Tests/TestResults/sp3a-host.trx`
Expected: 打印 22 用例计数表。记录每用例 verdict。C11 在 host 上应为 MISSING（openmc skip）——Task 4 容器复核。

- [ ] **Step 3: 判定缺口** — 看 C10/F1（及任何 SHORT/FAIL 项）的真实 verdict：
  - 若全 PASS（C10/F1 的 Theory 展开已达标）→ 跳过 Task 3。
  - 若 C10/F1 SHORT → 进 Task 3 处理。
  - 若任何 FAIL(failed>0) → 按真实暴露，定位失败测试，记入 summary（不掩盖）。

本 Task 无提交（产物在 Task 6 归档）。

---

## Task 3: 处理 C10/F1 缺口（仅当 Task 2 实测 SHORT）

**Files:**（按需）Modify `MetBench_SystemMT.Tests/V2Discovery/ScgHeuristicDiscovererTests.cs`、`MetBench_SystemMT.Tests/V2Schema/V2DbConfigRegistrationTests.cs`

- [ ] **Step 1: 判定缺口性质**（先读对应测试类 + 被测类）：
  - **陈旧阈值**（suite 重构过，如 C2 的 "8→5" 先例）：retro-touch `acceptance-rubric.md` 该行判据为实测值并行内注明原因（R3），**不改测试**。Task 3 转纯文档。
  - **真实覆盖缺口**：补**有意义**测试（§4）：
    - C10：补 direct-cause / mediator / confounder 三类 pattern 各自从 `scg.json` 产 candidate 的验证 fact（读 `ScgHeuristicDiscoverer` 与现有 fact 模式，补足到 ≥29 个真实展开用例，每类 pattern ≥1 断言其 candidate 被产出）。
    - F1：补 system / user / test 三级 `DbConfig` override 场景 fact（读 `V2DbConfigRegistrationTests` 现有 2 个 + DbConfig override API，补足到 ≥5 真实场景）。
  - **禁止**：写 `Assert.True(true)` 之类凑数断言。

- [ ] **Step 2: 跑改动的类确认绿**

Run: `& "C:\Program Files\dotnet\dotnet.exe" test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~ScgHeuristicDiscovererTests|FullyQualifiedName~V2DbConfigRegistrationTests"`
Expected: 0 failed，对应类 Passed 达阈值。

- [ ] **Step 3: 提交**（补测试或改判据二选一对应措辞）：
  `test(sp3a): fill C10/F1 coverage to meet rubric threshold`
  或 `docs(sp3a): retro-touch stale C10/F1 rubric thresholds to measured counts`

---

## Task 4: C11 容器内复核（openmc 真跑）

**Files:** 无（运行产物）

- [ ] **Step 1: 容器内跑 C11 相关测试出 trx**

Run:
```
& "C:\Program Files\Docker\Docker\resources\bin\docker.exe" run --rm -v "D:\Codes\MetBench-V2.1.4_2:/work" -w /work metbench-runtime:latest env METBENCH_OPENMC_PYTHON=/opt/openmc-venv/bin/python METBENCH_OPENMOC_PYTHON=/opt/openmoc-venv/bin/python dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --filter "FullyQualifiedName~OpenMcRunnerSmokeTests|FullyQualifiedName~CrossProgramNeutronTransportMrs" --logger "trx;LogFileName=sp3a-c11.trx"
```
Expected: `OpenMcRunnerSmokeTests` Passed=1；`CrossProgramNeutronTransportMrs` 的 openmc scenario（ScaleNuSigmaF/ScaleFuelSigmaA on openmc）Passed；0 failed。

- [ ] **Step 2: 报告脚本核 C11**

Run: `python tools/sp3a_rubric_report.py --trx MetBench_SystemMT.Tests/TestResults/sp3a-c11.trx`
Expected: C11 = PASS（present 且 passed≥1）。

本 Task 无提交（Task 6 归档 trx）。

---

## Task 5: G2 性能基线

**Files:** 无（运行产物）

- [ ] **Step 1: 跑 perf 脚本**（喂 host 套件 trx）

Run: `python tools/ci_perf_baseline.py --trx MetBench_SystemMT.Tests/TestResults/sp3a-host.trx --total-budget-seconds 120.0`
Expected: exit 0；输出 total < 120s + top-10 慢测。记录 total 秒数。

- [ ] **Step 2: 判定** — exit 0 → G2 PASS；exit 1（total≥120s）→ G2 标 ❌ + 记 top-10，不放宽预算（§6）。

---

## Task 6: 填 rubric + 归档证据

**Files:** Modify `docs/uat/acceptance-rubric.md`；Create `docs/superpowers/specs/2026-06-15-sp3a-uat-trx-evidence/`

- [ ] **Step 1: 填 rubric 22 行**：A/C/D/E/F/G 各表的 trx 支撑行「结果」列填 ✅（或 ⚠️/❌）、「证据」列填 trx 路径 + 实测 passed 计数（如 `sp3a-host.trx · 12 passed`）。C11 证据指 `sp3a-c11.trx`。G2 指 perf 输出。UI 类行（A1-7/B1-9/C6-9/E2-5）保持留空（SP3b）。

- [ ] **Step 2: 归档** evidence 目录：`sp3a-host.trx`、`sp3a-c11.trx`、报告脚本输出文本、G2 输出文本、`sp3a-summary.md`（22 用例逐项 verdict + 实测计数 + 任何缺口/陈旧阈值处理记录 + 容器/host 环境）。

- [ ] **Step 3: 提交** — `docs(sp3a): fill UAT rubric trx rows + archive evidence`

---

## Task 7: 状态投影 + PR

**Files:** Modify `docs/status/current.md`、active plan index、本 plan 状态字段

- [ ] **Step 1: 三处更新**（SP3a 行：22 项 trx 用例真实验收结论 + 任何缺口处理；指针引证据）。
- [ ] **Step 2: 提交 + 推送 + PR**：
  ```
  git push -u origin sp3a-uat-trx-acceptance
  ```
  PR body 按 `docs/superpowers/templates/pr-gate-checklist.md` 7 节；Windows Classification=run-and-log；Tests 节贴报告脚本 22 用例 verdict + C11 容器结果 + G2 total；说明 SP3b（UI 类）后续。

---

## 最终验证（PR 前）

```
python tools/sp3a_rubric_report.py --trx MetBench_SystemMT.Tests/TestResults/sp3a-host.trx   # 非 C11 行全 PASS（C11 在 host MISSING）
python tools/sp3a_rubric_report.py --trx MetBench_SystemMT.Tests/TestResults/sp3a-c11.trx     # C11 PASS
git diff --check
```

## PR Gate Classification

- Scope：单一目的——SP3a UAT trx 支撑类 22 用例验收。
- Windows classification：`run-and-log`。代码仅 cloud-safe（`tools/` python 报告脚本 + 必要时 `MetBench_SystemMT.Tests` 补真实测试），不碰 WPF/CI 门禁。
- 模块 E：单 PR，非 ≥3-PR chain。
- 7 节 checklist；如实记录任何 SHORT/FAIL/陈旧阈值处理。
