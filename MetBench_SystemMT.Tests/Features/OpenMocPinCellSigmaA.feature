Feature: OpenMOC pin-cell MR - scaling fuel sigma_a decreases k_eff

  Background:
    The dominant eigenvalue (k_eff) strictly decreases when fuel
    absorption is uniformly scaled up while production (nu*sigma_f) and
    scattering (sigma_s) are held constant. This is the directional
    counterpart to the Stage 3 ScaleNuSigmaF MR and validates the
    LessThan assertion plus the IMrAssertion registry.

  Scenario: Follow-up k_eff is less than source k_eff after scaling fuel sigma_a
    Given an OpenMOC pin-cell sigma_a source case from "openmoc/sample/pincell.json"
    And the OpenMOC sigma_a transformation "ScaleFuelSigmaA" with parameter "factor" set to "1.5"
    When I run source and the generated follow-up through OpenMOC for sigma_a
    Then the OpenMOC parsed value "k_eff" of the generated follow-up should be less than the source
    And the OpenMOC follow-up k_eff should be at most 0.85 times the source k_eff
