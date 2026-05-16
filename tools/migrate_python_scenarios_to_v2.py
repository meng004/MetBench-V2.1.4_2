"""migrate_python_scenarios_to_v2: Stage 5 Python catalog → v2 JSON migration package.

把 `tools/mutation_study.SCENARIOS` 29 行 dict 拆成 v2 实体结构：
  • MRSchema (code, name, MetaPatternCode, TransformationName, AssertionTypeCode,
              ValueName, NoiseAware, ToleranceRel, ...)
  • MRBinding (mr_code, sut, adapter, runner, default_sample_case)
  • MRInstance (mr_binding, parameter_overrides like {factor: "1.5"})

按 v2 4 级 MR 语义层次（MetaPattern / Schema / Binding / Instance），
但 Schema 与 Binding 在 v1 `SCENARIOS` 字典里是混在一行的（含 solver + adapter +
transform），本脚本拆解：
  - 一行 SCENARIOS → 1 个 MRSchema（按 transform 去重合并）+ 1 个 MRBinding（含 solver）

CLI:
    python tools/migrate_python_scenarios_to_v2.py --output metbench/catalog/migration/scenarios.json
"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path
from typing import Any

REPO_ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(REPO_ROOT / "tools"))
from mutation_study import SCENARIOS  # noqa: E402


# transform → MetaPattern 映射（与 NOETHER catalog 一致）
_TRANSFORM_TO_METAPATTERN = {
    "ScaleNuSigmaF":              "m_mono",
    "ScaleFuelSigmaA":            "m_mono",
    "ScaleFuelSigmaT":            "m_mono",
    "ScaleFuelSigmaS":            "m_mono",
    "ScaleFuelRadius":            "m_mono",
    "ScaleModeratorSigmaA":       "m_mono",
    "RaiseFuelTemperature":       "m_mono",
    "RaiseFuelTemperatureViaAddTemperature": "m_mono",
    "RaiseModeratorTemperatureViaBoratedWater": "m_mono",
    "Rotate90":                   "m_inv",
    "MirrorX":                    "m_inv",
    "MirrorY":                    "m_inv",
    "GroupPermute":               "m_inv",
    "PermuteEnergyGroups":        "m_inv",
    "MirrorXTally":               "m_inv",
    "MirrorYTally":               "m_inv",
    "RefineParticles":            "m_conv",
}


def _transform_to_metapattern(transform: str) -> str:
    return _TRANSFORM_TO_METAPATTERN.get(transform, "unknown")


def _assertion_to_v2_code(scenario: dict[str, Any]) -> str:
    """v1 assertion → v2 AssertionTypeCode。

    v1 字段 'assertion' 可能是 'greater' / 'less' / 'approx' / 'variance-ratio' / ...
    v1 字段 'noise_aware' True/False 改变 less/greater 的 v2 code。
    """
    raw = scenario.get("assertion", "less")
    noise_aware = scenario.get("noise_aware", False)
    if raw == "less" and noise_aware:
        return "less-noise-aware"
    if raw == "greater" and noise_aware:
        return "greater-noise-aware"
    if raw == "approx":
        return "approx-invariant"
    if raw == "variance-ratio":
        return "variance-ratio"
    if raw == "flux-pointwise-approx":
        return "flux-pointwise-approx"
    return raw  # less / greater / approx


def _short_code(scenario: dict[str, Any]) -> str:
    """从 SCENARIOS id 派生 MRSchema Code。

    "openmoc-pincell-nu-sigma-f" → "MR-NuSigmaF" (跨 solver 共享同 schema)
    """
    transform = scenario.get("transform", "")
    if not transform:
        return scenario["id"]
    # 取 transform 转 short code
    short_map = {
        "ScaleNuSigmaF":              "MR-NuSigmaF",
        "ScaleFuelSigmaA":            "MR-SigmaA",
        "ScaleFuelSigmaT":            "MR05-mono-fuel-sigma-t-up",
        "ScaleFuelSigmaS":            "MR06-mono-fuel-sigma-s-down",
        "ScaleFuelRadius":            "MR08-mono-fuel-radius-up",
        "ScaleModeratorSigmaA":       "MR07-mono-moderator-sigma-a-up",
        "RaiseFuelTemperature":       "MR-T",
        "RaiseFuelTemperatureViaAddTemperature": "MR-T-AddTemp",
        "RaiseModeratorTemperatureViaBoratedWater": "MR-T-BoratedWater",
        "Rotate90":                   "MR01-inv-quarter-rotation-90",
        "MirrorX":                    "MR02-inv-mirror-x",
        "MirrorY":                    "MR03-inv-mirror-y",
        "GroupPermute":               "MR-GroupPermute",
        "PermuteEnergyGroups":        "MR04-inv-energy-group-relabel",
        "MirrorXTally":               "MR-MirrorX-Tally",
        "MirrorYTally":               "MR-MirrorY-Tally",
        "RefineParticles":            "MR12-conv-particles-refine",
    }
    return short_map.get(transform, f"MR-{transform}")


def migrate() -> dict[str, Any]:
    """SCENARIOS list → v2 migration package。"""
    schemas: dict[str, dict[str, Any]] = {}
    bindings: list[dict[str, Any]] = []
    instances: list[dict[str, Any]] = []

    for sc in SCENARIOS:
        code = _short_code(sc)
        transform = sc.get("transform", "Unknown")
        assertion = _assertion_to_v2_code(sc)
        value = sc.get("value", "k_eff")

        # 1) MRSchema (按 code 去重)
        if code not in schemas:
            schemas[code] = {
                "code": code,
                "name": transform,
                "meta_pattern_code": _transform_to_metapattern(transform),
                "transformation_name": transform,
                "assertion_type_code": assertion,
                "value_name": value,
                "noise_aware": sc.get("noise_aware", False),
                "tolerance_rel": sc.get("tolerance_rel", 0.0),
                "description": f"Migrated from mutation_study.SCENARIOS id={sc['id']}",
            }

        # 2) MRBinding
        bindings.append({
            "mr_code": code,
            "sut": sc.get("solver", "unknown"),
            "input_adapter_path": sc.get("adapter", ""),
            "runner_path": sc.get("runner", ""),
            "scenario_id_v1": sc["id"],  # 追溯 v1 id
            "is_active": True,
        })

        # 3) MRInstance (默认 factor=1.5 单实例)
        instances.append({
            "mr_binding_v1_id": sc["id"],
            "parameter_overrides": {"factor": "1.5"},
            "is_reusable": False,
            "name": f"{sc['id']}@factor=1.5",
        })

    return {
        "source": "mutation_study.SCENARIOS",
        "total_scenarios": len(SCENARIOS),
        "schemas": list(schemas.values()),
        "bindings": bindings,
        "instances": instances,
    }


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__,
                                     formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--output", required=False, help="Output JSON file (defaults to stdout)")
    args = parser.parse_args()

    pkg = migrate()
    out = json.dumps(pkg, indent=2, ensure_ascii=False)
    if args.output:
        Path(args.output).write_text(out + "\n", encoding="utf-8")
        print(f"wrote {args.output}: {len(pkg['schemas'])} schemas, "
              f"{len(pkg['bindings'])} bindings, {len(pkg['instances'])} instances")
    else:
        print(out)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
