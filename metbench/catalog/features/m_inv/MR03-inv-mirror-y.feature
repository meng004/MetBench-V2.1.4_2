@metapattern:m_inv @assertion:approx-invariant @value:k_eff
@noise_aware:false @tolerance_rel:1e-05
Feature: MR03-inv-mirror-y — MirrorY

  Background:
    Migrated from mutation_study.SCENARIOS id=openmoc-pincell-mirror-y

  Scenario Outline: Apply MR03-inv-mirror-y to <sut> with factor <factor>
    Given the MR Schema "MR03-inv-mirror-y" is bound to SUT "<sut>"
    And the binding uses sample case "<sample>"
    And the parameter mapping for "geometry.mirror_axis" is configured
    When the MT pipeline runs with parameter "factor"="<factor>"
    Then the "approx-invariant" assertion holds on "k_eff"

    Examples:
      | sut | input_adapter_path | runner_path | scenario_id_v1 | is_active |
      | openmoc | openmoc/openmoc_input_adapter_mirror_y.py | openmoc/openmoc_runner.py | openmoc-pincell-mirror-y | True |
      | openmc | openmc/openmc_input_adapter_mirror_y.py | openmc/openmc_runner.py | openmc-pincell-mirror-y | True |
      | openmoc | openmoc/openmoc_input_adapter_mirror_y.py | openmoc/openmoc_runner.py | openmoc-pincell-mirror-y-tally | True |
      | openmc | openmc/openmc_input_adapter_mirror_y.py | openmc/openmc_runner.py | openmc-pincell-mirror-y-tally | True |
