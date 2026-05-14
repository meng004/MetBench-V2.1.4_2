@metapattern:m_mono @assertion:greater @value:k_eff
@noise_aware:false @tolerance_rel:0.0
Feature: MR-NuSigmaF — ScaleNuSigmaF

  Background:
    Migrated from mutation_study.SCENARIOS id=openmoc-pincell-nu-sigma-f

  Scenario Outline: Apply MR-NuSigmaF to <sut> with factor <factor>
    Given the MR Schema "MR-NuSigmaF" is bound to SUT "<sut>"
    And the binding uses sample case "<sample>"
    And the parameter mapping for "physics.fuel.nu_sigma_f" is configured
    When the MT pipeline runs with parameter "factor"="<factor>"
    Then the "greater" assertion holds on "k_eff"

    Examples:
      | sut | input_adapter_path | runner_path | scenario_id_v1 | is_active |
      | openmoc | openmoc/openmoc_input_adapter.py | openmoc/openmoc_runner.py | openmoc-pincell-nu-sigma-f | True |
      | openmc | openmc/openmc_input_adapter.py | openmc/openmc_runner.py | openmc-pincell-nu-sigma-f | True |
