Feature: OpenMOC pin-cell MR - scaling fuel nu*sigma_f increases k_eff

  Background:
    The dominant eigenvalue (k_eff) of a fixed transport problem with
    reflective boundaries strictly increases when the production
    operator (nu*sigma_f) is uniformly scaled by a factor greater than
    one, with absorption and scattering held constant. Stage 3 validates
    this metamorphic relation end-to-end through OpenMOC.

  Scenario: Follow-up k_eff exceeds source k_eff after scaling fuel nu*sigma_f
    Given an OpenMOC pin-cell source case from "openmoc/sample/pincell.json"
    And an OpenMOC MR transformation "ScaleNuSigmaF" with parameter "factor" set to "1.5"
    When I run source and the generated follow-up through OpenMOC
    Then the OpenMOC parsed value "k_eff" of the generated follow-up should be greater than the source
    And the OpenMOC follow-up k_eff should be at least 1.2 times the source k_eff
