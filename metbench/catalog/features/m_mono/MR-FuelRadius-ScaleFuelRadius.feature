@metapattern:m_mono @assertion:greater @value:k_eff
@noise_aware:false @tolerance_rel:0.0
Feature: MR-FuelRadius — ScaleFuelRadius

  Background:
    Migrated from mutation_study.SCENARIOS id=openmoc-pincell-fuel-radius

  Scenario Outline: Apply MR-FuelRadius to <sut> with factor <factor>
    Given the MR Schema "MR-FuelRadius" is bound to SUT "<sut>"
    And the binding uses sample case "<sample>"
    And the parameter mapping for "geometry.fuel.radius" is configured
    When the MT pipeline runs with parameter "factor"="<factor>"
    Then the "greater" assertion holds on "k_eff"

    Examples:
      | sut | input_adapter_path | runner_path | scenario_id_v1 | is_active |
      | openmoc | openmoc/openmoc_input_adapter_fuel_radius.py | openmoc/openmoc_runner.py | openmoc-pincell-fuel-radius | True |
      | openmc | openmc/openmc_input_adapter_fuel_radius.py | openmc/openmc_runner.py | openmc-pincell-fuel-radius | True |
