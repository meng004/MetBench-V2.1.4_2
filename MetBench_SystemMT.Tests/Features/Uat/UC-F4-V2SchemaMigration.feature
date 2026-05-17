Feature: UAT UC-F4 — V2 schema migration

  Rubric: docs/uat/acceptance-rubric.md §F4
  Pass criterion: Passed ≥ 9, Failed = 0
  Underlying suite: MetBench_SystemMT.Tests.V2Schema.V2SoftDeleteAndMigrationTests

  Scenario: UC-F4 rubric coverage and baseline are green
    Then UAT case "UC-F4" requires at least 9 verified facts in test class "MetBench_SystemMT.Tests.V2Schema.V2SoftDeleteAndMigrationTests"
    And UAT case "UC-F4" baseline trx "docs/uat/reports/baseline-2026-05-16/baseline-full.trx" shows test class "MetBench_SystemMT.Tests.V2Schema.V2SoftDeleteAndMigrationTests" with 0 Failed
