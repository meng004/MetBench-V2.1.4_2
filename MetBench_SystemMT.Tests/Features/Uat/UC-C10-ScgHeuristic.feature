Feature: UAT UC-C10 — SCG-Heuristic Discoverer

  Rubric: docs/uat/acceptance-rubric.md §C10
  Pass criterion: Passed ≥ 29, Failed = 0 + 三类 pattern 都产 candidate
  Underlying suite: MetBench_SystemMT.Tests.V2Discovery.ScgHeuristicDiscovererTests

  Scenario: UC-C10 rubric coverage and baseline are green
    Then UAT case "UC-C10" requires at least 14 verified facts in test class "MetBench_SystemMT.Tests.V2Discovery.ScgHeuristicDiscovererTests"
    And UAT case "UC-C10" baseline trx "docs/uat/reports/baseline-2026-05-16/baseline-full.trx" shows test class "MetBench_SystemMT.Tests.V2Discovery.ScgHeuristicDiscovererTests" with 0 Failed
