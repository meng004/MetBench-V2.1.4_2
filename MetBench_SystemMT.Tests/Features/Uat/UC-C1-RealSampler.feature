Feature: UAT UC-C1 — 真实 python sidecar 发现

  Rubric: docs/uat/acceptance-rubric.md §C1
  Pass criterion: Passed ≥ 4, Failed = 0
  Underlying suite: MetBench_SystemMT.Tests.V2Discovery.RealSamplerTests

  Scenario: UC-C1 rubric coverage and baseline are green
    Then UAT case "UC-C1" requires at least 4 verified facts in test class "MetBench_SystemMT.Tests.V2Discovery.RealSamplerTests"
    And UAT case "UC-C1" baseline trx "docs/uat/reports/baseline-2026-05-17/baseline-full.trx" shows test class "MetBench_SystemMT.Tests.V2Discovery.RealSamplerTests" with 0 Failed
