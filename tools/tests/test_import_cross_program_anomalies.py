"""Tests for tools/import_cross_program_anomalies.py.

T5 PR-2 cloud fact: covers parse_disagree_rows / attach_scenario_details /
classify_severity. Uses inline markdown fixtures so the test never touches
the real cross-program-report.md and is hermetic.
"""
from __future__ import annotations

import importlib.util
import json
import sys
import unittest
from pathlib import Path


_ROOT = Path(__file__).resolve().parents[2]
_MODULE_PATH = _ROOT / "tools" / "import_cross_program_anomalies.py"
spec = importlib.util.spec_from_file_location("import_cross_program_anomalies", _MODULE_PATH)
assert spec is not None and spec.loader is not None
icpa = importlib.util.module_from_spec(spec)
sys.modules["import_cross_program_anomalies"] = icpa
spec.loader.exec_module(icpa)


_FIXTURE = """# Cross-program MR14 (OpenMOC vs OpenMC) — baseline pairs

| Transform | k(OpenMOC) | k(OpenMC) | σ(OpenMC) | |Δk| | budget | verdict |
|---|--:|--:|--:|--:|--:|--:|
| ScaleNuSigmaF | 1.69990 | 1.68996 | 0.00252 | 0.00994 | 0.01690 | agree |
| ScaleFuelSigmaT | 0.11127 | 0.11292 | 0.00022 | 0.00165 | 0.00113 | **DISAGREE** |
| ScaleModeratorSigmaA | 0.47635 | 0.96831 | 0.00166 | 0.49196 | 0.00968 | **DISAGREE** |
| Rotate90 | 1.15949 | 1.15354 | 0.00193 | 0.00596 | 0.01154 | agree |

## Disagreements

### ScaleFuelSigmaT

* OpenMOC scenario: `openmoc-pincell-fuel-sigma-t` → k = 0.11127
* OpenMC scenario:  `openmc-pincell-fuel-sigma-t` → k = 0.11292 ± 0.00022
* |Δk| = 0.00165 (1.5% of OpenMC), budget = 0.00113

### ScaleModeratorSigmaA

* OpenMOC scenario: `openmoc-pincell-moderator-sigma-a` → k = 0.47635
* OpenMC scenario:  `openmc-pincell-moderator-sigma-a` → k = 0.96831 ± 0.00166
* |Δk| = 0.49196 (50.8% of OpenMC), budget = 0.00968
"""


class ImportCrossProgramAnomaliesTests(unittest.TestCase):
    def test_parses_two_disagree_rows_and_skips_agree(self):
        rows = icpa.parse_disagree_rows(_FIXTURE)
        self.assertEqual(2, len(rows))
        self.assertEqual("ScaleFuelSigmaT", rows[0]["transform"])
        self.assertEqual("ScaleModeratorSigmaA", rows[1]["transform"])

    def test_parses_numeric_fields_correctly(self):
        rows = icpa.parse_disagree_rows(_FIXTURE)
        moderator = rows[1]
        self.assertAlmostEqual(0.47635, moderator["k_openmoc"])
        self.assertAlmostEqual(0.96831, moderator["k_openmc"])
        self.assertAlmostEqual(0.49196, moderator["delta_k"])
        self.assertAlmostEqual(0.00968, moderator["budget"])

    def test_attach_scenario_details_lifts_openmoc_openmc_ids_and_percent(self):
        rows = icpa.parse_disagree_rows(_FIXTURE)
        rows = icpa.attach_scenario_details(_FIXTURE, rows)
        moderator = rows[1]
        self.assertEqual("openmoc-pincell-moderator-sigma-a", moderator["openmoc_scenario"])
        self.assertEqual("openmc-pincell-moderator-sigma-a", moderator["openmc_scenario"])
        self.assertAlmostEqual(50.8, moderator["percent_of_openmc"])

    def test_classify_severity_critical_for_50_percent(self):
        rows = icpa.parse_disagree_rows(_FIXTURE)
        rows = icpa.attach_scenario_details(_FIXTURE, rows)
        self.assertEqual("critical", icpa.classify_severity(rows[1]))

    def test_classify_severity_major_for_1_to_5_percent(self):
        rows = icpa.parse_disagree_rows(_FIXTURE)
        rows = icpa.attach_scenario_details(_FIXTURE, rows)
        self.assertEqual("major", icpa.classify_severity(rows[0]))

    def test_classify_severity_falls_back_to_delta_over_k_when_percent_missing(self):
        row = {"delta_k": 0.001, "k_openmc": 1.0}  # 0.1% < 1% → minor
        self.assertEqual("minor", icpa.classify_severity(row))

    def test_main_writes_json_file_with_two_anomalies(self):
        import tempfile

        with tempfile.TemporaryDirectory() as tmp:
            tmp_path = Path(tmp)
            input_path = tmp_path / "report.md"
            output_path = tmp_path / "out.json"
            input_path.write_text(_FIXTURE, encoding="utf-8")
            sys.argv = [
                "import_cross_program_anomalies.py",
                "--input",
                str(input_path),
                "--output",
                str(output_path),
            ]
            self.assertEqual(0, icpa.main())
            payload = json.loads(output_path.read_text(encoding="utf-8"))
            self.assertEqual(2, len(payload["anomalies"]))
            self.assertEqual("cross-program-disagreement", payload["anomalies"][0]["category"])
            self.assertEqual("investigating", payload["anomalies"][0]["status_after_seed"])


if __name__ == "__main__":
    unittest.main()
