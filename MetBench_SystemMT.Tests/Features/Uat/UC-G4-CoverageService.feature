Feature: UAT UC-G4 — Coverage service 单测

  Rubric: docs/uat/acceptance-rubric.md §G4
  Pass criterion: Passed ≥ 5, Failed = 0
  Underlying suite: MetBench_SystemMT.Tests.V2Coverage.CoverageServiceTests

  Scenario: UC-G4 rubric coverage and baseline are green
    Then UAT case "UC-G4" requires at least 5 verified facts in test class "MetBench_SystemMT.Tests.V2Coverage.CoverageServiceTests"
    And UAT case "UC-G4" baseline trx "docs/uat/reports/baseline-2026-05-17/baseline-full.trx" shows test class "MetBench_SystemMT.Tests.V2Coverage.CoverageServiceTests" with 0 Failed
