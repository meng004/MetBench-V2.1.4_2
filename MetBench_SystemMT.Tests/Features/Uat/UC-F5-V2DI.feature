Feature: UAT UC-F5 — V2 DI 完整性

  Rubric: docs/uat/acceptance-rubric.md §F5
  Pass criterion: 所有 V2 IXxxRepo 解析 OK
  Underlying suite: MetBench_SystemMT.Tests.V2Schema.V2RepositoryDIBindingTests

  Scenario: UC-F5 rubric coverage and baseline are green
    Then UAT case "UC-F5" requires at least 5 verified facts in test class "MetBench_SystemMT.Tests.V2Schema.V2RepositoryDIBindingTests"
    And UAT case "UC-F5" baseline trx "docs/uat/reports/baseline-2026-05-17/baseline-full.trx" shows test class "MetBench_SystemMT.Tests.V2Schema.V2RepositoryDIBindingTests" with 0 Failed
