"""Python TDD: P5.1 / P5.3 工具行为验证。

跑法：
    python3 tools/tests/test_p5_sync_tools.py

或与 pytest：
    pytest tools/tests/test_p5_sync_tools.py -v

不依赖 pytest 也能跑（手工 main 调度），方便 CI 中无 pytest 时 fallback。
"""

from __future__ import annotations

import json
import sys
import tempfile
import textwrap
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parent.parent.parent
sys.path.insert(0, str(REPO_ROOT / "tools"))


def test_feature_to_db_extracts_tags_and_examples():
    """P5.1 feature_to_db：tag + Feature header + Examples 提取。"""
    from feature_to_db import parse_directory

    with tempfile.TemporaryDirectory() as tmpdir:
        feat = Path(tmpdir) / "MR-T.feature"
        feat.write_text(textwrap.dedent("""\
            @metapattern:m_mono @assertion:less-noise-aware @value:k_eff
            @noise_aware:true @tolerance_rel:0.001
            Feature: MR-T — RaiseFuelTemperature

              Background:
                Doppler effect description.

              Scenario Outline: Apply MR-T to <sut>
                Given the MR Schema "MR-T" is bound to SUT "<sut>"
                When the MT pipeline runs with parameter "factor"="<factor>"
                Then the noise-aware "less" assertion holds on "k_eff"

                Examples:
                  | sut       | sample        | factor |
                  | openmoc   | pincell.json  | 1.5    |
                  | openmc    | pincell.json  | 1.5    |
            """), encoding="utf-8")

        catalog = parse_directory(Path(tmpdir))
        assert len(catalog["schemas"]) == 1
        s = catalog["schemas"][0]
        assert s["code"] == "MR-T"
        assert s["name"] == "RaiseFuelTemperature"
        assert s["meta_pattern_code"] == "m_mono"
        assert s["assertion_type_code"] == "less-noise-aware"
        assert s["value_name"] == "k_eff"
        assert s["noise_aware"] is True
        assert s["tolerance_rel"] == 0.001
        assert "Doppler" in s["description"]

        assert len(catalog["bindings"]) == 2
        assert catalog["bindings"][0]["sut"] == "openmoc"
        assert catalog["bindings"][1]["sut"] == "openmc"


def test_db_to_feature_renders_skeleton():
    """P5.1 db_to_feature: 从 catalog dict 渲染合法 .feature 文本。"""
    from db_to_feature import render_catalog

    catalog = {
        "schemas": [{
            "code": "MR-T",
            "name": "RaiseFuelTemperature",
            "meta_pattern_code": "m_mono",
            "assertion_type_code": "less-noise-aware",
            "value_name": "k_eff",
            "noise_aware": True,
            "tolerance_rel": 0.001,
            "description": "Doppler effect.",
        }],
        "bindings": [
            {"mr_code": "MR-T", "sut": "openmoc", "sample": "pincell.json", "factor": "1.5"},
        ],
    }
    with tempfile.TemporaryDirectory() as tmpdir:
        out_dir = Path(tmpdir)
        written = render_catalog(catalog, out_dir)
        assert len(written) == 1
        text = written[0].read_text(encoding="utf-8")
        assert "@metapattern:m_mono" in text
        assert "@assertion:less-noise-aware" in text
        assert "@noise_aware:true" in text
        assert "@tolerance_rel:0.001" in text
        assert "Feature: MR-T — RaiseFuelTemperature" in text
        assert "Doppler effect." in text
        assert "noise-aware" in text
        assert "openmoc" in text


def test_feature_to_db_round_trip_preserves_tags():
    """P5.1 round-trip: feature → JSON → feature 保持 tag 一致。"""
    from feature_to_db import parse_directory
    from db_to_feature import render_catalog

    with tempfile.TemporaryDirectory() as tmpdir:
        tmp = Path(tmpdir)
        feat = tmp / "src.feature"
        feat.write_text(textwrap.dedent("""\
            @metapattern:m_inv @assertion:approx-invariant @value:k_eff
            @noise_aware:false @tolerance_rel:0.0
            Feature: MR-Rot90 — Rotate90

              Background:
                Pin-cell rotational symmetry.

              Scenario Outline: Apply to <sut>
                Given the MR Schema "MR-Rot90" is bound to SUT "<sut>"
                When the MT pipeline runs with parameter "angle"="<angle>"
                Then the "approx-invariant" assertion holds on "k_eff"

                Examples:
                  | sut       | sample        | angle |
                  | openmoc   | pincell.json  | 90    |
            """), encoding="utf-8")

        # Round 1: feature → catalog
        catalog1 = parse_directory(tmp)

        # Round 2: catalog → 再生成 feature
        regen_dir = tmp / "regen"
        render_catalog(catalog1, regen_dir)

        # Round 3: 再读 regen feature → catalog2
        catalog2 = parse_directory(regen_dir)

        # 关键字段一致
        s1 = catalog1["schemas"][0]
        s2 = catalog2["schemas"][0]
        assert s1["code"] == s2["code"]
        assert s1["meta_pattern_code"] == s2["meta_pattern_code"]
        assert s1["assertion_type_code"] == s2["assertion_type_code"]
        assert s1["value_name"] == s2["value_name"]
        assert s1["noise_aware"] == s2["noise_aware"]
        assert s1["tolerance_rel"] == s2["tolerance_rel"]


def test_validate_feature_sync_reports_in_sync():
    """P5.1 validate_feature_sync: 一致时 is_in_sync=True。"""
    from feature_to_db import parse_directory
    from validate_feature_sync import diff_catalogs, is_in_sync

    with tempfile.TemporaryDirectory() as tmpdir:
        feat_dir = Path(tmpdir) / "features"
        feat_dir.mkdir()
        (feat_dir / "MR-X.feature").write_text(textwrap.dedent("""\
            @metapattern:m_mono @assertion:less @value:k_eff
            @noise_aware:false @tolerance_rel:0.0
            Feature: MR-X — Test

              Background:
                Test rationale.

              Scenario Outline: Apply
                Given the MR Schema "MR-X" is bound to SUT "<sut>"

                Examples:
                  | sut       |
                  | openmoc   |
            """), encoding="utf-8")

        features_catalog = parse_directory(feat_dir)
        # 与自身比对 → in sync
        diff = diff_catalogs(features_catalog, features_catalog)
        assert is_in_sync(diff)


def test_validate_feature_sync_detects_drift():
    """P5.1 validate_feature_sync: 不一致时 is_in_sync=False + 给出原因。"""
    from validate_feature_sync import diff_catalogs, is_in_sync

    cat_a = {
        "schemas": [{
            "code": "MR-X", "name": "Test",
            "meta_pattern_code": "m_mono",
            "assertion_type_code": "less",
            "value_name": "k_eff",
            "noise_aware": False, "tolerance_rel": 0.0,
        }],
        "bindings": [],
    }
    cat_b = {
        "schemas": [{
            "code": "MR-X", "name": "Test",
            "meta_pattern_code": "m_inv",      # 变了
            "assertion_type_code": "less",
            "value_name": "k_eff",
            "noise_aware": False, "tolerance_rel": 0.0,
        }],
        "bindings": [],
    }
    diff = diff_catalogs(cat_a, cat_b)
    assert not is_in_sync(diff)
    assert len(diff["schemas"]["changed"]) == 1
    assert diff["schemas"]["changed"][0]["code"] == "MR-X"


def test_migrate_python_scenarios_emits_14_schemas():
    """P5.3 migrate_python_scenarios: 29 v1 scenarios → 14 v2 MRSchema."""
    from migrate_python_scenarios_to_v2 import migrate
    pkg = migrate()
    assert pkg["total_scenarios"] == 29
    assert len(pkg["schemas"]) == 14
    assert len(pkg["bindings"]) == 29
    assert len(pkg["instances"]) == 29


def test_migrate_python_scenarios_known_codes_present():
    """P5.3 关键 MRSchema code 出现在迁移产物里。"""
    from migrate_python_scenarios_to_v2 import migrate
    pkg = migrate()
    codes = {s["code"] for s in pkg["schemas"]}
    expected = {
        "MR-NuSigmaF", "MR-SigmaA", "MR-SigmaT", "MR-SigmaS",
        "MR-FuelRadius", "MR-ModSigmaA",
        "MR-T", "MR-T-AddTemp", "MR-T-BoratedWater",
        "MR-Rot90", "MR-MirrorX", "MR-MirrorY",
        "MR-RefineParticles", "MR-PermuteEnergyGroups",
    }
    missing = expected - codes
    assert not missing, f"Missing MRSchema codes after migration: {missing}"


def test_migrate_mutations_emits_48_operators():
    """P5.3 migrate_mutations: 48 mutations → 48 operators + 48 mutants."""
    from migrate_mutations_to_v2 import migrate
    pkg = migrate()
    assert pkg["total"] == 48
    assert len(pkg["operators"]) == 48
    assert len(pkg["mutants"]) == 48


def test_migrate_real_bugs_emits_6_known_bugs():
    """P5.3 migrate_real_bugs: 6 R-Cases → 6 KnownBugs."""
    from migrate_real_bugs_to_v2 import migrate
    pkg = migrate()
    assert pkg["total"] == 6
    codes = {b["code"] for b in pkg["known_bugs"]}
    assert codes == {f"R-Case-{i}" for i in range(1, 7)}


def test_generated_feature_files_have_required_tags():
    """P5.4 生成的 .feature 文件含必需 v2 tags（catalog 真理源约定）。"""
    feature_dir = REPO_ROOT / "metbench" / "catalog" / "features"
    if not feature_dir.is_dir():
        # P5.4 还未跑过；跳过
        return
    feature_files = list(feature_dir.rglob("*.feature"))
    assert len(feature_files) >= 14, "P5.4 应该生成至少 14 个 .feature 文件"
    for f in feature_files:
        text = f.read_text(encoding="utf-8")
        assert "@metapattern:" in text, f"{f.name}: missing @metapattern tag"
        assert "@assertion:" in text, f"{f.name}: missing @assertion tag"
        assert "@value:" in text, f"{f.name}: missing @value tag"
        assert "Feature:" in text, f"{f.name}: missing Feature: header"


# =============== Manual main ===============

def main() -> int:
    tests = [
        test_feature_to_db_extracts_tags_and_examples,
        test_db_to_feature_renders_skeleton,
        test_feature_to_db_round_trip_preserves_tags,
        test_validate_feature_sync_reports_in_sync,
        test_validate_feature_sync_detects_drift,
        test_migrate_python_scenarios_emits_14_schemas,
        test_migrate_python_scenarios_known_codes_present,
        test_migrate_mutations_emits_48_operators,
        test_migrate_real_bugs_emits_6_known_bugs,
        test_generated_feature_files_have_required_tags,
    ]
    failures = []
    for t in tests:
        try:
            t()
            print(f"  ✓ {t.__name__}")
        except Exception as e:
            print(f"  ✗ {t.__name__}: {e}")
            failures.append((t.__name__, e))

    if failures:
        print(f"\n{len(failures)} / {len(tests)} tests FAILED")
        return 1
    print(f"\n{len(tests)} / {len(tests)} tests PASSED")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
