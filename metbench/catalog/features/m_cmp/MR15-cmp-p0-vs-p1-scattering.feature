@metapattern:m_cmp @assertion:bound @value:k_eff
@noise_aware:true @tolerance_rel:0.005
Feature: MR15-cmp-p0-vs-p1-scattering — P0 vs P1 scattering order agreement

  m_cmp / B7 cross-program: 同一 OpenMC 案例改 xsdata.order=0 (isotropic, P0)
  vs xsdata.order=1 (linearly anisotropic, P1)，期望 k_P0 ≥ k_P1 within
  budget (Bol-Alg-04 textbook result, anisotropic scattering 增加泄漏)。

  Background:
    Reference: noether MR15 (block E*)
    Hypothesis: k_P0 ≥ k_P1 within 3·σ_MC + ε_truncation

  Scenario Outline: Compare scattering order P0 vs P1 on <case>
    Given the MR Schema "MR15-cmp-p0-vs-p1-scattering" is bound to SUT "openmc"
    And the binding uses sample case "<case>"
    And the parameter mapping for "openmc.xsdata.scattering_order" is configured
    When the MT pipeline runs once with order=0 and once with order=1
    Then the "bound" assertion holds on "k_eff" with tolerance "<tolerance>"

    Examples:
      | case                     | tolerance | scenario_id_v1            | is_active |
      | pincell-baseline         | 0.005     | openmc-p0-vs-p1-baseline  | True      |
      | pincell-mod-sigma-a-1.5  | 0.005     | openmc-p0-vs-p1-mod-1.5   | True      |
