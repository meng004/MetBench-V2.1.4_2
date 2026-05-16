@metapattern:m_inv @assertion:approx-invariant @value:k_eff
@noise_aware:false @tolerance_rel:1e-05
Feature: MR02-inv-mirror-x — MirrorX

  Background:
    Migrated from mutation_study.SCENARIOS id=openmoc-pincell-mirror-x

  Scenario Outline: Apply MR02-inv-mirror-x to <sut> with factor <factor>
    Given the MR Schema "MR02-inv-mirror-x" is bound to SUT "<sut>"
    And the binding uses sample case "<sample>"
    And the parameter mapping for "geometry.mirror_axis" is configured
    When the MT pipeline runs with parameter "factor"="<factor>"
    Then the "approx-invariant" assertion holds on "k_eff"

    Examples:
      | sut | input_adapter_path | runner_path | scenario_id_v1 | is_active |
      | openmoc | openmoc/openmoc_input_adapter_mirror_x.py | openmoc/openmoc_runner.py | openmoc-pincell-mirror-x | True |
      | openmc | openmc/openmc_input_adapter_mirror_x.py | openmc/openmc_runner.py | openmc-pincell-mirror-x | True |
      | openmoc | openmoc/openmoc_input_adapter_mirror_x.py | openmoc/openmoc_runner.py | openmoc-pincell-mirror-x-tally | True |
      | openmc | openmc/openmc_input_adapter_mirror_x.py | openmc/openmc_runner.py | openmc-pincell-mirror-x-tally | True |
