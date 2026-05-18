Feature: UAT UC-C4 — Multi-LLM Consensus + κ

  Rubric: docs/uat/acceptance-rubric.md §C4
  Pass criterion: Passed ≥ 15, Failed = 0
  Underlying suite: MetBench_SystemMT.Tests.V2Discovery.MultiLlmConsensusValidatorTests

  Scenario: UC-C4 rubric coverage and baseline are green
    Then UAT case "UC-C4" requires at least 15 verified facts in test class "MetBench_SystemMT.Tests.V2Discovery.MultiLlmConsensusValidatorTests"
    And UAT case "UC-C4" baseline trx "docs/uat/reports/baseline-2026-05-17/baseline-full.trx" shows test class "MetBench_SystemMT.Tests.V2Discovery.MultiLlmConsensusValidatorTests" with 0 Failed
