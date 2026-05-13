@metapattern:m_mono @assertion:less-noise-aware @value:k_eff
@noise_aware:true @tolerance_rel:0.0
Feature: MR-T-AddTemp — RaiseFuelTemperatureViaAddTemperature

  Background:
    Migrated from mutation_study.SCENARIOS id=openmc-pincell-fuel-temperature-via-add-temperature

  Scenario Outline: Apply MR-T-AddTemp to <sut> with factor <factor>
    Given the MR Schema "MR-T-AddTemp" is bound to SUT "<sut>"
    And the binding uses sample case "<sample>"
    And the parameter mapping for "<TODO-abstract-field>" is configured
    When the MT pipeline runs with parameter "factor"="<factor>"
    Then the noise-aware "less" assertion holds on "k_eff"

    Examples:
      | sut | input_adapter_path | runner_path | scenario_id_v1 | is_active |
      | openmc | openmc/openmc_input_adapter_fuel_temperature_via_add_temperature.py | openmc/openmc_runner.py | openmc-pincell-fuel-temperature-via-add-temperature | True |
