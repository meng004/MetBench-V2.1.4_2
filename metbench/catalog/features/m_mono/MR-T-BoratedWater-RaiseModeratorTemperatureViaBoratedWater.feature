@metapattern:m_mono @assertion:less-noise-aware @value:k_eff
@noise_aware:true @tolerance_rel:0.0
Feature: MR-T-BoratedWater — RaiseModeratorTemperatureViaBoratedWater

  Background:
    Migrated from mutation_study.SCENARIOS id=openmc-pincell-moderator-temperature-via-borated-water

  Scenario Outline: Apply MR-T-BoratedWater to <sut> with factor <factor>
    Given the MR Schema "MR-T-BoratedWater" is bound to SUT "<sut>"
    And the binding uses sample case "<sample>"
    And the parameter mapping for "<TODO-abstract-field>" is configured
    When the MT pipeline runs with parameter "factor"="<factor>"
    Then the noise-aware "less" assertion holds on "k_eff"

    Examples:
      | sut | input_adapter_path | runner_path | scenario_id_v1 | is_active |
      | openmc | openmc/openmc_input_adapter_moderator_temperature_via_borated_water.py | openmc/openmc_runner.py | openmc-pincell-moderator-temperature-via-borated-water | True |
