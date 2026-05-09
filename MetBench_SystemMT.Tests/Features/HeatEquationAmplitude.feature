Feature: 1D heat equation MR - amplitude scaling preserves linearity

  Background:
    The 1D heat equation u_t = alpha * u_xx is linear in the initial profile
    when boundary conditions are homogeneous Dirichlet. Doubling the initial
    amplitude must therefore double the solution at every later time, in
    particular the peak value max_u(t_final). This is the directional
    counterpart of the OpenMOC ScaleNuSigmaF MR for a different SUT and
    proves the system-MT abstraction is reusable beyond OpenMOC.

  Scenario: Follow-up max_u exceeds source max_u after scaling initial amplitude
    Given a heat-equation source case from "heat_equation/sample/gaussian.json"
    And a heat-equation transformation "ScaleAmplitude" with parameter "factor" set to "2"
    When I run source and the generated follow-up through the heat-equation solver
    Then the heat-equation parsed value "max_u" of the generated follow-up should be greater than the source
    And the heat-equation follow-up max_u should be at least 1.95 times the source max_u
    And the heat-equation follow-up max_u should be at most 2.05 times the source max_u
