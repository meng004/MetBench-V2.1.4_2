Feature: UAT UC-G5 — Anomaly service + commonality

  Rubric: docs/uat/acceptance-rubric.md §G5
  Pass criterion: Passed ≥ 8, Failed = 0
  Underlying suite: MetBench_SystemMT.Tests.V2Anomaly.AnomalyServiceTests

  Scenario: UC-G5 rubric coverage and baseline are green
    Then UAT case "UC-G5" requires at least 8 verified facts in test class "MetBench_SystemMT.Tests.V2Anomaly.AnomalyServiceTests"
    And UAT case "UC-G5" baseline trx "docs/uat/reports/baseline-2026-05-16/baseline-full.trx" shows test class "MetBench_SystemMT.Tests.V2Anomaly.AnomalyServiceTests" with 0 Failed
