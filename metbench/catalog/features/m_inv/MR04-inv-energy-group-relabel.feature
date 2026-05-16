@metapattern:m_inv @assertion:approx-invariant @value:k_eff
@noise_aware:false @tolerance_rel:1e-06
Feature: MR04-inv-energy-group-relabel — PermuteEnergyGroups

  Background:
    Migrated from mutation_study.SCENARIOS id=openmoc-pincell-group-permute

  Scenario Outline: Apply MR04-inv-energy-group-relabel to <sut> with factor <factor>
    Given the MR Schema "MR04-inv-energy-group-relabel" is bound to SUT "<sut>"
    And the binding uses sample case "<sample>"
    And the parameter mapping for "physics.energy_group_permutation" is configured
    When the MT pipeline runs with parameter "factor"="<factor>"
    Then the "approx-invariant" assertion holds on "k_eff"

    Examples:
      | sut | input_adapter_path | runner_path | scenario_id_v1 | is_active |
      | openmoc | openmoc/openmoc_input_adapter_group_permute.py | openmoc/openmoc_runner.py | openmoc-pincell-group-permute | True |
      | openmc | openmc/openmc_input_adapter_group_permute.py | openmc/openmc_runner.py | openmc-pincell-group-permute | True |
