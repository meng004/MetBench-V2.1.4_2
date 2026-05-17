Feature: UAT UC-F2 — MetaPattern Seed (8 patterns)

  Rubric: docs/uat/acceptance-rubric.md §F2
  Pass criterion: Passed ≥ 11, Failed = 0
  Underlying suite: MetBench_SystemMT.Tests.V2Schema.MetaPatternEntityTests

  Scenario: UC-F2 rubric coverage and baseline are green
    Then UAT case "UC-F2" requires at least 11 verified facts in test class "MetBench_SystemMT.Tests.V2Schema.MetaPatternEntityTests"
    And UAT case "UC-F2" baseline trx "docs/uat/reports/baseline-2026-05-16/baseline-full.trx" shows test class "MetBench_SystemMT.Tests.V2Schema.MetaPatternEntityTests" with 0 Failed
