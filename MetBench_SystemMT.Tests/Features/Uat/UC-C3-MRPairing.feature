Feature: UAT UC-C3 — MRPairing m_cmp partner

  Rubric: docs/uat/acceptance-rubric.md §C3
  Pass criterion: Passed ≥ 11, Failed = 0
  Underlying suite: MetBench_SystemMT.Tests.V2Discovery.MRPairingServiceTests

  Scenario: UC-C3 rubric coverage and baseline are green
    Then UAT case "UC-C3" requires at least 11 verified facts in test class "MetBench_SystemMT.Tests.V2Discovery.MRPairingServiceTests"
    And UAT case "UC-C3" baseline trx "docs/uat/reports/baseline-2026-05-17/baseline-full.trx" shows test class "MetBench_SystemMT.Tests.V2Discovery.MRPairingServiceTests" with 0 Failed
