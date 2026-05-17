Feature: UAT UC-C11 — OpenMC 第 3-SUT BDD smoke

  Rubric: docs/uat/acceptance-rubric.md §C11 (added in PR #57; row may not yet
  exist on main when this branch lands first — feature is intentionally written
  to reference the test class that PR #57 adds)
  Pass criterion: OpenMC pin-cell scenario 跑通 + smoke test 1/1 Pass + output
  k_eff ∈ [0.5, 2.0] + metadata.runner = "openmc"
  Underlying suite: MetBench_SystemMT.Tests.SystemMT.OpenMcRunnerSmokeTests
  (lands with PR #57)

  @ignore
  Scenario: UC-C11 rubric coverage (gated on PR #57 merge)
    # @ignore marks this scenario skipped until PR #57 (which adds
    # OpenMcRunnerSmokeTests + the C11 row in acceptance-rubric.md) is
    # merged. Drop @ignore in a follow-up commit once main contains both.
    Then UAT case "UC-C11" requires at least 1 verified facts in test class "MetBench_SystemMT.Tests.SystemMT.OpenMcRunnerSmokeTests"
