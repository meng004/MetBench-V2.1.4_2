@metapattern:m_mono @assertion:less @value:k_eff
@noise_aware:false @tolerance_rel:0.0
Feature: MR-T — RaiseFuelTemperature

  Background:
    Migrated from mutation_study.SCENARIOS id=openmoc-pincell-fuel-temperature

  Scenario Outline: Apply MR-T to <sut> with factor <factor>
    Given the MR Schema "MR-T" is bound to SUT "<sut>"
    And the binding uses sample case "<sample>"
    And the parameter mapping for "physics.fuel.temperature" is configured
    When the MT pipeline runs with parameter "factor"="<factor>"
    Then the "less" assertion holds on "k_eff"

    Examples:
      | sut | input_adapter_path | runner_path | scenario_id_v1 | is_active |
      | openmoc | openmoc/openmoc_input_adapter_fuel_temperature.py | openmoc/openmoc_runner.py | openmoc-pincell-fuel-temperature | True |
      | openmc | openmc/openmc_input_adapter_fuel_temperature.py | openmc/openmc_runner.py | openmc-pincell-fuel-temperature | True |
