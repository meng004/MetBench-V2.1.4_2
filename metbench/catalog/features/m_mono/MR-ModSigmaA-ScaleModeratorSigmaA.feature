@metapattern:m_mono @assertion:less @value:k_eff
@noise_aware:false @tolerance_rel:0.0
Feature: MR-ModSigmaA — ScaleModeratorSigmaA

  Background:
    Migrated from mutation_study.SCENARIOS id=openmoc-pincell-moderator-sigma-a

  Scenario Outline: Apply MR-ModSigmaA to <sut> with factor <factor>
    Given the MR Schema "MR-ModSigmaA" is bound to SUT "<sut>"
    And the binding uses sample case "<sample>"
    And the parameter mapping for "<TODO-abstract-field>" is configured
    When the MT pipeline runs with parameter "factor"="<factor>"
    Then the "less" assertion holds on "k_eff"

    Examples:
      | sut | input_adapter_path | runner_path | scenario_id_v1 | is_active |
      | openmoc | openmoc/openmoc_input_adapter_moderator_sigma_a.py | openmoc/openmoc_runner.py | openmoc-pincell-moderator-sigma-a | True |
      | openmc | openmc/openmc_input_adapter_moderator_sigma_a.py | openmc/openmc_runner.py | openmc-pincell-moderator-sigma-a | True |
