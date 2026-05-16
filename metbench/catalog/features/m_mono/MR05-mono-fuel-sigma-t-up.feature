@metapattern:m_mono @assertion:less @value:k_eff
@noise_aware:false @tolerance_rel:0.0
Feature: MR05-mono-fuel-sigma-t-up — ScaleFuelSigmaT

  Background:
    Migrated from mutation_study.SCENARIOS id=openmoc-pincell-fuel-sigma-t

  Scenario Outline: Apply MR05-mono-fuel-sigma-t-up to <sut> with factor <factor>
    Given the MR Schema "MR05-mono-fuel-sigma-t-up" is bound to SUT "<sut>"
    And the binding uses sample case "<sample>"
    And the parameter mapping for "physics.fuel.sigma_t" is configured
    When the MT pipeline runs with parameter "factor"="<factor>"
    Then the "less" assertion holds on "k_eff"

    Examples:
      | sut | input_adapter_path | runner_path | scenario_id_v1 | is_active |
      | openmoc | openmoc/openmoc_input_adapter_fuel_sigma_t.py | openmoc/openmoc_runner.py | openmoc-pincell-fuel-sigma-t | True |
      | openmc | openmc/openmc_input_adapter_fuel_sigma_t.py | openmc/openmc_runner.py | openmc-pincell-fuel-sigma-t | True |
