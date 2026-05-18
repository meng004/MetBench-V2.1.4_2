Feature: UAT UC-F1 — DbConfig 3-level override

  Rubric: docs/uat/acceptance-rubric.md §F1
  Pass criterion: Passed ≥ 5, Failed = 0
  Underlying suite: MetBench_SystemMT.Tests.V2Schema.DbConfigTests

  Scenario: UC-F1 rubric coverage and baseline are green
    Then UAT case "UC-F1" requires at least 5 verified facts in test class "MetBench_SystemMT.Tests.V2Schema.DbConfigTests"
    And UAT case "UC-F1" baseline trx "docs/uat/reports/baseline-2026-05-17/baseline-full.trx" shows test class "MetBench_SystemMT.Tests.V2Schema.DbConfigTests" with 0 Failed
