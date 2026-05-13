@metapattern:m_mono @assertion:less @value:k_eff
@noise_aware:false @tolerance_rel:0.0
Feature: MR-SigmaS — ScaleFuelSigmaS

  Background:
    Migrated from mutation_study.SCENARIOS id=openmoc-pincell-fuel-sigma-s

  Scenario Outline: Apply MR-SigmaS to <sut> with factor <factor>
    Given the MR Schema "MR-SigmaS" is bound to SUT "<sut>"
    And the binding uses sample case "<sample>"
    And the parameter mapping for "<TODO-abstract-field>" is configured
    When the MT pipeline runs with parameter "factor"="<factor>"
    Then the "less" assertion holds on "k_eff"

    Examples:
      | sut | input_adapter_path | runner_path | scenario_id_v1 | is_active |
      | openmoc | openmoc/openmoc_input_adapter_fuel_sigma_s.py | openmoc/openmoc_runner.py | openmoc-pincell-fuel-sigma-s | True |
      | openmc | openmc/openmc_input_adapter_fuel_sigma_s.py | openmc/openmc_runner.py | openmc-pincell-fuel-sigma-s | True |
