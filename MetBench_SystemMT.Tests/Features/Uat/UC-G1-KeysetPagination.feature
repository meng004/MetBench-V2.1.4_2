Feature: UAT UC-G1 — LiteDB Keyset 分页

  Rubric: docs/uat/acceptance-rubric.md §G1
  Pass criterion: Passed ≥ 10, Failed = 0
  Underlying suite: MetBench_SystemMT.Tests.V2Pagination.KeysetPaginationTests

  Scenario: UC-G1 rubric coverage and baseline are green
    Then UAT case "UC-G1" requires at least 10 verified facts in test class "MetBench_SystemMT.Tests.V2Pagination.KeysetPaginationTests"
    And UAT case "UC-G1" baseline trx "docs/uat/reports/baseline-2026-05-17/baseline-full.trx" shows test class "MetBench_SystemMT.Tests.V2Pagination.KeysetPaginationTests" with 0 Failed
