@metapattern:m_inv @assertion:approx-invariant @value:k_eff
@noise_aware:false @tolerance_rel:0.0001
Feature: MR01-inv-quarter-rotation-90 — Rotate90

  Background:
    Migrated from mutation_study.SCENARIOS id=openmoc-pincell-rotate-90

  Scenario Outline: Apply MR01-inv-quarter-rotation-90 to <sut> with factor <factor>
    Given the MR Schema "MR01-inv-quarter-rotation-90" is bound to SUT "<sut>"
    And the binding uses sample case "<sample>"
    And the parameter mapping for "geometry.rotation_deg" is configured
    When the MT pipeline runs with parameter "factor"="<factor>"
    Then the "approx-invariant" assertion holds on "k_eff"

    Examples:
      | sut | input_adapter_path | runner_path | scenario_id_v1 | is_active |
      | openmoc | openmoc/openmoc_input_adapter_rotate_90.py | openmoc/openmoc_runner.py | openmoc-pincell-rotate-90 | True |
      | openmc | openmc/openmc_input_adapter_rotate_90.py | openmc/openmc_runner.py | openmc-pincell-rotate-90 | True |
