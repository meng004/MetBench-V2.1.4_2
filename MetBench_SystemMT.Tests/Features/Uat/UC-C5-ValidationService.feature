Feature: UAT UC-C5 — Validation Service E2E

  Rubric: docs/uat/acceptance-rubric.md §C5
  Pass criterion: Passed > 0, Failed = 0
  Underlying suite: MetBench_SystemMT.Tests.V2Discovery.ValidationServiceTests

  Scenario: UC-C5 rubric coverage and baseline are green
    Then UAT case "UC-C5" requires at least 1 verified facts in test class "MetBench_SystemMT.Tests.V2Discovery.ValidationServiceTests"
    And UAT case "UC-C5" baseline trx "docs/uat/reports/baseline-2026-05-16/baseline-full.trx" shows test class "MetBench_SystemMT.Tests.V2Discovery.ValidationServiceTests" with 0 Failed
