Feature: Cross-program metamorphic relations on neutron-transport solvers

  Background:
    The OpenMOC (deterministic method-of-characteristics) and OpenMC (Monte
    Carlo) solvers share the same input JSON schema and the same physical
    meaning of cross-section quantities. Stage 4 AC #6 is satisfied by
    demonstrating that one MR transformation, named identically across both
    SUTs and parameterised identically, flows through both solvers' input
    adapters unchanged and produces an assertion result consistent with the
    MR's directional claim.

    These scenarios skip in CI and in environments where neither OpenMOC nor
    OpenMC is importable (no end-to-end physics runs there). On a developer
    workstation with `METBENCH_OPENMOC_PYTHON` and `METBENCH_OPENMC_PYTHON`
    set to venvs with each solver installed, all four scenario instances run.

  Scenario Outline: ScaleNuSigmaF increases k_eff regardless of solver
    Given a pin-cell neutron-transport source case from "<sut>/sample/pincell.json" for solver "<solver>"
    And an MR transformation "ScaleNuSigmaF" with factor "1.5"
    When I run source and the generated follow-up through the "<solver>" solver
    Then the follow-up k_eff should be strictly greater than the source k_eff

    Examples:
      | solver  | sut     |
      | openmoc | openmoc |
      | openmc  | openmc  |

  Scenario Outline: ScaleFuelSigmaA decreases k_eff regardless of solver
    Given a pin-cell neutron-transport source case from "<sut>/sample/pincell.json" for solver "<solver>"
    And an MR transformation "ScaleFuelSigmaA" with factor "1.5"
    When I run source and the generated follow-up through the "<solver>" solver
    Then the follow-up k_eff should be strictly less than the source k_eff

    Examples:
      | solver  | sut     |
      | openmoc | openmoc |
      | openmc  | openmc  |
