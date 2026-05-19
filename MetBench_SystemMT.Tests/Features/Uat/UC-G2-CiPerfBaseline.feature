Feature: UAT UC-G2 — CI 性能基线

  Rubric: docs/uat/acceptance-rubric.md §G2
  Pass criterion: ci_perf_baseline.py exit 0 + total < 120 s
  Underlying helper: tools/ci_perf_baseline.py (Python script, not a [Fact])

  Scenario: UC-G2 helper script exists and is non-empty
    Then UAT case "UC-G2" helper "tools/ci_perf_baseline.py" exists and is non-empty
