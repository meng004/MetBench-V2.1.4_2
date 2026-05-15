"""cold_start_demo: 在 cloud 端走一遍 MetBench v2 核心功能，逐步发现问题。

不依赖 OpenMOC/OpenMC venv —— 只用 Python tools + LiteDB 模拟工作流，
覆盖以下 metbench 核心功能链路：
  Phase 0  环境检查
  Phase 1  Catalog 迁移 (.feature → JSON → 验证 schema)
  Phase 2  Anchor 案例完整性核查
  Phase 3  Noether 候选 → Discovery 模拟
  Phase 4  Mutation 矩阵 stub 演练
  Phase 5  Coverage 报告生成
  Phase 6  Trend 周报生成
  Phase 7  Paper-package 复现包打包
  Phase 8  问题清单 + 修复建议

每个 phase 收集成功/失败状态，最后汇总成 markdown report。

CLI:
    python3 cold_start_demo/cold_start_demo.py
"""
from __future__ import annotations

import json
import shutil
import subprocess
import sys
import tempfile
import traceback
from dataclasses import dataclass, field
from pathlib import Path
from typing import Any

REPO_ROOT = Path(__file__).resolve().parent.parent
ANCHORS_PATH = REPO_ROOT / "cold_start_demo" / "anchors" / "anchors.json"
REPORT_DIR = REPO_ROOT / "cold_start_demo" / "reports"
TOOLS = REPO_ROOT / "tools"


@dataclass
class PhaseResult:
    phase: str
    status: str           # "ok" | "warn" | "error"
    summary: str
    details: dict[str, Any] = field(default_factory=dict)
    issues: list[str] = field(default_factory=list)


class ColdStartRun:
    def __init__(self):
        self.results: list[PhaseResult] = []
        self.tmp = Path(tempfile.mkdtemp(prefix="cold-start-"))
        REPORT_DIR.mkdir(parents=True, exist_ok=True)

    def cleanup(self):
        shutil.rmtree(self.tmp, ignore_errors=True)

    def _run_tool(self, args: list[str], *, capture=True) -> tuple[int, str, str]:
        proc = subprocess.run([sys.executable, *args],
                              capture_output=True, text=True, cwd=REPO_ROOT)
        return proc.returncode, proc.stdout, proc.stderr

    # ===== Phase 0 =====
    def phase0_environment(self) -> PhaseResult:
        details: dict[str, Any] = {}
        issues: list[str] = []
        details["python"] = sys.version.split()[0]
        details["repo_root"] = str(REPO_ROOT)
        details["anchors"] = ANCHORS_PATH.exists()
        details["catalog_dir"] = (REPO_ROOT / "metbench/catalog/features").is_dir()
        details["dotnet"] = shutil.which("dotnet") is not None
        if not ANCHORS_PATH.exists():
            issues.append("anchors.json missing")
        if not (REPO_ROOT / "metbench/catalog/features").is_dir():
            issues.append("metbench/catalog/features missing")
        return PhaseResult(
            phase="0 environment",
            status="ok" if not issues else "error",
            summary=f"py={details['python']} dotnet={details['dotnet']}",
            details=details, issues=issues)

    # ===== Phase 1 =====
    def phase1_catalog_migration(self) -> PhaseResult:
        out = self.tmp / "catalog.json"
        rc, stdout, stderr = self._run_tool([
            str(TOOLS / "feature_to_db.py"),
            "--features", "metbench/catalog/features/",
            "--output", str(out),
        ])
        details: dict[str, Any] = {"rc": rc, "output_path": str(out)}
        issues: list[str] = []
        if rc != 0:
            issues.append(f"feature_to_db rc={rc}: {stderr.strip()[:200]}")
            return PhaseResult("1 catalog-migration", "error",
                "feature_to_db.py failed", details, issues)

        data = json.loads(out.read_text(encoding="utf-8"))
        schemas = data.get("schemas", [])
        bindings = data.get("bindings", [])
        details["schemas"] = len(schemas)
        details["bindings"] = len(bindings)
        details["metapatterns"] = sorted({s.get("meta_pattern_code") for s in schemas if s.get("meta_pattern_code")})

        if not schemas:
            issues.append("0 schemas extracted")
        if any(not s.get("code") for s in schemas):
            issues.append("schema(s) without code field")
        return PhaseResult(
            "1 catalog-migration",
            "ok" if not issues else "warn",
            f"{len(schemas)} schemas / {len(bindings)} bindings / {len(details['metapatterns'])} metapatterns",
            details, issues)

    # ===== Phase 2 =====
    def phase2_anchor_integrity(self) -> PhaseResult:
        anchors = json.loads(ANCHORS_PATH.read_text(encoding="utf-8"))["anchors"]
        details: dict[str, Any] = {"total": len(anchors), "checked": []}
        issues: list[str] = []
        for a in anchors:
            fp = REPO_ROOT / a["feature_path"]
            ok = fp.exists()
            text = fp.read_text(encoding="utf-8") if ok else ""
            mp_ok = ok and f"@metapattern:{a['metapattern']}" in text
            sut_ok = ok and all(s in text for s in a["suts"])
            details["checked"].append({
                "id": a["id"],
                "feature_exists": ok,
                "metapattern_tag_ok": mp_ok,
                "suts_present": sut_ok,
            })
            if not ok:
                issues.append(f"{a['id']}: feature file missing — {a['feature_path']}")
            elif not mp_ok:
                issues.append(f"{a['id']}: @metapattern:{a['metapattern']} tag missing")
            elif not sut_ok:
                issues.append(f"{a['id']}: SUT(s) {a['suts']} not all in feature")
        return PhaseResult(
            "2 anchor-integrity",
            "ok" if not issues else "error",
            f"{len(anchors) - len(issues)}/{len(anchors)} anchors clean",
            details, issues)

    # ===== Phase 3 =====
    def phase3_noether_discovery(self) -> PhaseResult:
        rc, stdout, stderr = self._run_tool([str(TOOLS / "noether_candidates.py")])
        details: dict[str, Any] = {"rc": rc}
        issues: list[str] = []
        if rc != 0:
            issues.append(f"noether_candidates rc={rc}: {stderr.strip()[:200]}")
            return PhaseResult("3 noether-discovery", "error",
                "noether_candidates.py failed", details, issues)
        data = json.loads(stdout)
        candidates = data.get("candidates", [])
        details["total"] = len(candidates)
        realizable = [c for c in candidates if c.get("realizable_with_current_sut")]
        details["realizable"] = len(realizable)
        by_mp: dict[str, int] = {}
        for c in realizable:
            by_mp[c["meta_pattern"]] = by_mp.get(c["meta_pattern"], 0) + 1
        details["by_metapattern"] = by_mp
        return PhaseResult("3 noether-discovery", "ok",
            f"{len(realizable)} realizable / {len(candidates)} total",
            details, issues)

    # ===== Phase 4 =====
    def phase4_mutation_stub(self) -> PhaseResult:
        """模拟 5×5 mutation matrix — outcomes 全 fake，用于走通 C# 端逻辑契约。"""
        mutants = [{"id": i, "outcome": "detected" if i < 3 else "missed"} for i in range(1, 6)]
        bindings = [{"id": i} for i in range(1, 6)]
        cells = []
        for m in mutants:
            for b in bindings:
                cells.append({"mutant_id": m["id"], "binding_id": b["id"], "outcome": m["outcome"]})
        detected = sum(1 for c in cells if c["outcome"] == "detected")
        missed = sum(1 for c in cells if c["outcome"] == "missed")
        rate = detected / (detected + missed) if (detected + missed) else 0
        details = {
            "mutants": 5, "bindings": 5, "cells": len(cells),
            "detected": detected, "missed": missed, "detection_rate": round(rate, 3),
        }
        # 写一份 fake mutation JSON 给后续 phase 用
        (self.tmp / "mutation.json").write_text(json.dumps({"cells": cells}, indent=2))
        return PhaseResult("4 mutation-stub", "ok",
            f"5×5 = {len(cells)} cells, detection-rate {rate:.1%}",
            details)

    # ===== Phase 5 =====
    def phase5_coverage_report(self) -> PhaseResult:
        """从 catalog.json 合成 CoverageReport 风格的 JSON（不调 C#，仅算式核对）。

        Density 用唯一 (mr_code, sut) pair 计，排除 (cross-program) 伪 SUT，
        避免 m_cmp 行虚增分子超过分母。
        """
        cat = json.loads((self.tmp / "catalog.json").read_text(encoding="utf-8"))
        schemas = cat.get("schemas", [])
        bindings = cat.get("bindings", [])
        real_suts = sorted({b.get("sut") for b in bindings
                            if b.get("sut") and b.get("sut") != "(cross-program)"})
        all_metapatterns = ["m_inv", "m_mono", "m_conv", "m_cmp",
                            "m_adj", "m_rev", "m_dyn", "m_rel"]
        covered_mp = sorted({s["meta_pattern_code"] for s in schemas
                             if s.get("meta_pattern_code") in all_metapatterns})
        uncovered_mp = sorted(set(all_metapatterns) - set(covered_mp))

        # Density 算式 = 唯一 (mr, real_sut) pair / (#mrs × #real_suts)
        unique_pairs = {(b.get("mr_code"), b.get("sut")) for b in bindings
                        if b.get("sut") in real_suts}
        possible_cells = len(real_suts) * len(schemas)
        bound_cells = len(unique_pairs)
        cross_program_bindings = sum(1 for b in bindings if b.get("sut") == "(cross-program)")

        details = {
            "metapattern": {
                "total": len(all_metapatterns),
                "covered": len(covered_mp),
                "covered_codes": covered_mp,
                "uncovered_codes": uncovered_mp,
                "ratio": round(len(covered_mp) / len(all_metapatterns), 3),
            },
            "sut_mr": {
                "total_suts": len(real_suts),
                "suts": real_suts,
                "total_mrs": len(schemas),
                "unique_bindings": bound_cells,
                "possible_cells": possible_cells,
                "density": round(bound_cells / possible_cells, 3) if possible_cells else 0,
                "cross_program_bindings_excluded": cross_program_bindings,
            },
        }
        if details["sut_mr"]["density"] > 1.0:
            details["sut_mr"]["density_anomaly"] = True
        return PhaseResult("5 coverage-report", "ok",
            f"MetaPattern {len(covered_mp)}/{len(all_metapatterns)} | SUT×MR density={details['sut_mr']['density']:.0%}",
            details)

    # ===== Phase 6 =====
    def phase6_trend_report(self) -> PhaseResult:
        """合成上周/本周 anomaly 数模拟 WoW + burst detection 公式。"""
        # 模拟数据：上周 5 anomaly，本周 8 anomaly（其中 basin 维度从 1 → 6 触发 burst）
        prev_anomalies = [{"category": "basin"}, {"category": "mc-floor"}, {"category": "mc-floor"},
                          {"category": "uncategorized"}, {"category": "cross-program"}]
        this_anomalies = [{"category": "basin"}] * 6 + [{"category": "mc-floor"}, {"category": "cross-program"}]
        prev_count, this_count = len(prev_anomalies), len(this_anomalies)
        delta_pct = (this_count - prev_count) * 100 / prev_count
        # 简化 burst: this-week basin=6, baseline (4 prev weeks) 假设均值=1, σ=0.5
        baseline = 1.0
        this_basin = sum(1 for a in this_anomalies if a["category"] == "basin")
        sigmas = (this_basin - baseline) / 0.5
        burst = sigmas >= 2.0
        details = {
            "executions": 100,
            "anomalies_this_week": this_count,
            "anomalies_prev_week": prev_count,
            "delta_pct": round(delta_pct, 1),
            "burst": {
                "dimension": "category",
                "key": "basin",
                "count": this_basin,
                "baseline": baseline,
                "sigmas": round(sigmas, 1),
                "flagged": burst,
            },
        }
        return PhaseResult("6 trend-report", "ok",
            f"this={this_count} prev={prev_count} Δ={delta_pct:+.0f}% burst={burst}",
            details)

    # ===== Phase 7.5 — naming consistency =====
    def phase75_naming_consistency(self) -> PhaseResult:
        """Catalog code (.feature Feature: <code> —) vs noether 候选 id 命名对齐检查。

        交叉子系统 (Discovery → Validation promote → MetamorphicRelation lookup) 要求
        二者一致；不一致会让候选 promote 写入新 row 而非合并到已有 schema。
        """
        cat = json.loads((self.tmp / "catalog.json").read_text(encoding="utf-8"))
        catalog_codes = {s["code"] for s in cat["schemas"]}

        rc, stdout, _ = self._run_tool([str(TOOLS / "noether_candidates.py")])
        noether_codes = {c["id"] for c in json.loads(stdout)["candidates"]} if rc == 0 else set()

        # 找潜在 alias：catalog "MR14-cmp" 是否对应 noether "MR14-cmp-openmoc-vs-openmc"
        aliases: list[dict[str, str]] = []
        for nc in noether_codes:
            for cc in catalog_codes:
                if nc.startswith(cc) and nc != cc:
                    aliases.append({"catalog": cc, "noether": nc})

        details = {
            "catalog_codes": sorted(catalog_codes),
            "noether_codes_total": len(noether_codes),
            "exact_matches": sorted(catalog_codes & noether_codes),
            "aliased": aliases,
            "catalog_only": sorted(catalog_codes - noether_codes -
                                   {a["catalog"] for a in aliases}),
            "noether_only": sorted(noether_codes - catalog_codes -
                                   {a["noether"] for a in aliases}),
        }
        issues: list[str] = []
        if aliases:
            for a in aliases:
                issues.append(f"naming-drift: catalog='{a['catalog']}' vs noether='{a['noether']}'")
        return PhaseResult(
            "7.5 naming-consistency",
            "warn" if issues else "ok",
            f"{len(details['exact_matches'])} exact / {len(aliases)} aliased / "
            f"{len(details['catalog_only'])} catalog-only / {len(details['noether_only'])} noether-only",
            details, issues)

    # ===== Phase 7 =====
    def phase7_paper_package(self) -> PhaseResult:
        out = self.tmp / "metbench-paper.tar.gz"
        rc, stdout, stderr = self._run_tool([
            str(TOOLS / "build_paper_package.py"),
            "--output", str(out),
        ])
        details: dict[str, Any] = {"rc": rc, "output": str(out)}
        issues: list[str] = []
        if rc != 0:
            issues.append(f"build_paper_package rc={rc}: {stderr.strip()[:200]}")
        if out.exists():
            details["size_bytes"] = out.stat().st_size
            details["size_mb"] = round(out.stat().st_size / 1_048_576, 2)
        else:
            issues.append("tarball not produced")
        return PhaseResult("7 paper-package",
            "ok" if not issues else "error",
            f"{details.get('size_mb', 0)} MB tarball",
            details, issues)

    # ===== 汇总 =====
    def write_report(self) -> Path:
        md_lines = [
            "# Cold-Start Demo Report",
            f"Generated: 2026-05-15",
            "",
            f"## Summary",
            "",
            f"| # | Phase | Status | Summary |",
            f"|---|-------|--------|---------|",
        ]
        ok_count = 0
        for r in self.results:
            badge = {"ok": "✅", "warn": "⚠️", "error": "❌"}.get(r.status, "?")
            md_lines.append(f"| {r.phase} |  | {badge} {r.status} | {r.summary} |")
            if r.status == "ok":
                ok_count += 1
        md_lines += [
            "",
            f"**{ok_count}/{len(self.results)}** phases ok",
            "",
            "## Issues found",
            "",
        ]
        any_issues = False
        for r in self.results:
            if r.issues:
                any_issues = True
                md_lines.append(f"### {r.phase}")
                for i in r.issues:
                    md_lines.append(f"- {i}")
                md_lines.append("")
        if not any_issues:
            md_lines.append("_(none)_")
            md_lines.append("")

        md_lines.append("## Detail per phase")
        md_lines.append("")
        for r in self.results:
            md_lines.append(f"### {r.phase}")
            md_lines.append(f"- status: **{r.status}**")
            md_lines.append(f"- summary: {r.summary}")
            md_lines.append(f"- details:")
            md_lines.append(f"  ```json")
            md_lines.append(json.dumps(r.details, indent=2, ensure_ascii=False))
            md_lines.append(f"  ```")
            md_lines.append("")

        report_path = REPORT_DIR / "cold_start_run.md"
        report_path.write_text("\n".join(md_lines), encoding="utf-8")

        json_path = REPORT_DIR / "cold_start_run.json"
        json_path.write_text(json.dumps(
            [{"phase": r.phase, "status": r.status, "summary": r.summary,
              "details": r.details, "issues": r.issues}
             for r in self.results],
            indent=2, ensure_ascii=False), encoding="utf-8")
        return report_path


def main() -> int:
    run = ColdStartRun()
    print("=== MetBench v2 Cold-Start Demo ===\n")
    phases = [
        run.phase0_environment,
        run.phase1_catalog_migration,
        run.phase2_anchor_integrity,
        run.phase3_noether_discovery,
        run.phase4_mutation_stub,
        run.phase5_coverage_report,
        run.phase6_trend_report,
        run.phase75_naming_consistency,
        run.phase7_paper_package,
    ]
    try:
        for phase_fn in phases:
            try:
                result = phase_fn()
            except Exception as e:
                result = PhaseResult(
                    phase=phase_fn.__name__,
                    status="error",
                    summary=f"exception: {type(e).__name__}: {e}",
                    details={"traceback": traceback.format_exc()},
                    issues=[f"unhandled exception in {phase_fn.__name__}"])
            run.results.append(result)
            badge = {"ok": "✅", "warn": "⚠️", "error": "❌"}.get(result.status, "?")
            print(f"  {badge}  {result.phase:32s} {result.summary}")
            for issue in result.issues:
                print(f"       └─ {issue}")

        report = run.write_report()
        print(f"\n→ {report}")
        ok = sum(1 for r in run.results if r.status == "ok")
        return 0 if ok == len(run.results) else 1
    finally:
        run.cleanup()


if __name__ == "__main__":
    raise SystemExit(main())
