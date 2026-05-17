Feature: UAT UC-G3 — 多维 burst 检测

  Rubric: docs/uat/acceptance-rubric.md §G3
  Pass criterion: Passed ≥ 4, Failed = 0
  Underlying suite: MetBench_SystemMT.Tests.V2Trend.MultiDimBurstDetectionTests

  Scenario: UC-G3 rubric coverage and baseline are green
    Then UAT case "UC-G3" requires at least 4 verified facts in test class "MetBench_SystemMT.Tests.V2Trend.MultiDimBurstDetectionTests"
    And UAT case "UC-G3" baseline trx "docs/uat/reports/baseline-2026-05-16/baseline-full.trx" shows test class "MetBench_SystemMT.Tests.V2Trend.MultiDimBurstDetectionTests" with 0 Failed
