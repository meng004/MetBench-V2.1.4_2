@metapattern:m_cmp @assertion:bound @value:k_eff
@noise_aware:true @tolerance_rel:0.005
Feature: MR14-cmp-openmoc-vs-openmc — OpenMOC vs OpenMC k_eff agreement

  m_cmp / B7 cross-program: 同一案例分别在 OpenMOC 和 OpenMC 跑得到的
  k_eff 之差应被 σ_MC + ε_MOC 预算控制；超过即报告为跨求解器异议
  (cross-program disagreement)，是一份候选 bug。

  实测：mr_parameter_sweep.py 已经在 ScaleModeratorSigmaA(factor=1.5)
  发现 OpenMOC=0.4764 vs OpenMC=0.9683 (51% Δ)，对应 R-Case-4。

  Background:
    Cross-program reference: tools/cross_program_mr.py +
    docs/experiments/cross-program-report.md (Phase 2 baseline)

  Scenario Outline: Compare <case> across OpenMOC and OpenMC
    Given the MR Schema "MR14-cmp-openmoc-vs-openmc" is bound to SUT "openmoc"
    And the MR Schema "MR14-cmp-openmoc-vs-openmc" is also bound to SUT "openmc"
    # (Steps reference the long-form noether code MR14-cmp-openmoc-vs-openmc;
    # this matches the noether candidate id 1:1 for Discovery→promote alignment.)
    And the binding uses sample case "<case>"
    And the parameter mapping for "scenario.case_path" is configured
    When the MT pipeline runs OpenMOC and OpenMC on the same case
    Then the "bound" assertion holds on "k_eff" with tolerance "<tolerance>"

    Examples:
      | case                     | tolerance | scenario_id_v1                   | is_active |
      | pincell-baseline         | 0.005     | cross-program-baseline           | True      |
      | pincell-mod-sigma-a-1.0  | 0.005     | cross-program-mod-sigma-a-1.0    | True      |
      | pincell-mod-sigma-a-1.5  | 0.005     | cross-program-mod-sigma-a-1.5    | True      |
      | pincell-fuel-temp-1.0    | 0.010     | cross-program-fuel-temp-1.0      | True      |
      | pincell-fuel-temp-1.25   | 0.010     | cross-program-fuel-temp-1.25    | True      |
