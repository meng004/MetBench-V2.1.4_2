Feature: UAT UC-C2 — Empirical + LLM Validator

  Rubric: docs/uat/acceptance-rubric.md §C2
  Pass criterion: Passed ≥ 5, Failed = 0
  Underlying suite: MetBench_SystemMT.Tests.V2Discovery.ValidatorTests
  Note: 2026-05-23 next-stage P0 移除 AdversarialMutmutValidator 后 [Fact] 数由 8 降至 5；
        baseline trx 来自 2026-05-17（mutmut 删除前），shows test class with 0 Failed 仍成立。

  Scenario: UC-C2 rubric coverage and baseline are green
    Then UAT case "UC-C2" requires at least 5 verified facts in test class "MetBench_SystemMT.Tests.V2Discovery.ValidatorTests"
    And UAT case "UC-C2" baseline trx "docs/uat/reports/baseline-2026-05-17/baseline-full.trx" shows test class "MetBench_SystemMT.Tests.V2Discovery.ValidatorTests" with 0 Failed
