Feature: UAT UC-C11 — OpenMC 第 3-SUT BDD smoke

  Rubric: docs/uat/acceptance-rubric.md §C11
  Pass criterion: OpenMC pin-cell scenario 跑通 + smoke test 1/1 Pass + output
  k_eff ∈ [0.5, 2.0] + metadata.runner = "openmc"
  Underlying suite: MetBench_SystemMT.Tests.SystemMT.OpenMcRunnerSmokeTests

  Scenario: UC-C11 rubric coverage and baseline are green
    Then UAT case "UC-C11" requires at least 1 verified facts in test class "MetBench_SystemMT.Tests.SystemMT.OpenMcRunnerSmokeTests"
