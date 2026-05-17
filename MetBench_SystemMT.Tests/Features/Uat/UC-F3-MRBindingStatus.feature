Feature: UAT UC-F3 — MRBinding.Status 软删

  Rubric: docs/uat/acceptance-rubric.md §F3
  Pass criterion: Passed ≥ 7, Failed = 0
  Underlying suite: MetBench_SystemMT.Tests.V2Schema.MRBindingStatusTests

  Scenario: UC-F3 rubric coverage and baseline are green
    Then UAT case "UC-F3" requires at least 7 verified facts in test class "MetBench_SystemMT.Tests.V2Schema.MRBindingStatusTests"
    And UAT case "UC-F3" baseline trx "docs/uat/reports/baseline-2026-05-16/baseline-full.trx" shows test class "MetBench_SystemMT.Tests.V2Schema.MRBindingStatusTests" with 0 Failed
