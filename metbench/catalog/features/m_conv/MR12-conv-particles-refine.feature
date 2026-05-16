@metapattern:m_conv @assertion:variance-ratio @value:k_eff_std
@noise_aware:false @tolerance_rel:0.3
Feature: MR12-conv-particles-refine — RefineParticles

  Background:
    Migrated from mutation_study.SCENARIOS id=openmc-pincell-particles-refine

  Scenario Outline: Apply MR12-conv-particles-refine to <sut> with factor <factor>
    Given the MR Schema "MR12-conv-particles-refine" is bound to SUT "<sut>"
    And the binding uses sample case "<sample>"
    And the parameter mapping for "numerics.particles" is configured
    When the MT pipeline runs with parameter "factor"="<factor>"
    Then the "variance-ratio" assertion holds on "k_eff_std"

    Examples:
      | sut | input_adapter_path | runner_path | scenario_id_v1 | is_active |
      | openmc | openmc/openmc_input_adapter_refine_particles.py | openmc/openmc_runner.py | openmc-pincell-particles-refine | True |
