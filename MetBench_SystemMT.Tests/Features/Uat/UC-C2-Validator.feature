Feature: UAT UC-C2 — Empirical + LLM Validator

  Rubric: docs/uat/acceptance-rubric.md §C2
  Pass criterion: Passed ≥ 8, Failed = 0
  Underlying suite: MetBench_SystemMT.Tests.V2Discovery.ValidatorTests

  Scenario: UC-C2 rubric coverage and baseline are green
    Then UAT case "UC-C2" requires at least 8 verified facts in test class "MetBench_SystemMT.Tests.V2Discovery.ValidatorTests"
    And UAT case "UC-C2" baseline trx "docs/uat/reports/baseline-2026-05-17/baseline-full.trx" shows test class "MetBench_SystemMT.Tests.V2Discovery.ValidatorTests" with 0 Failed
