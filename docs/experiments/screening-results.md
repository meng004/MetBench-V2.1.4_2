# Baseline screening results

Threshold rule: a mutation is **semantic** iff some affected scenario shows

`|k_source_mut − k_source_base| > max(3·σ, 0.5%·k_base)` **or**

`|k_followup_mut − k_followup_base| > max(3·σ, 0.5%·k_base)`.

σ is the larger of source / follow-up statepoint stds for the mutant.


**Methodology note**: plan PR #25 prescribed a source-only screening rule
(`|k_source_mut − k_source_base|`). Execution found that rule degenerate for
adapter mutations (adapter is not exercised by the source case). The screening
below uses the richer rule above, which combines both signals from the matrix runs.
Per-candidate source-only results are retained in `_data/candidates/<id>/screening.json`
as a transparency artifact. The discrepancy between the two columns below shows
exactly which mutations would have been miss-classified by the source-only rule.


**Discard rate**: 4 of 28 candidates (14.3%) classified equivalent under the matrix rule.


Classification counts: semantic=23, equivalent=4, error=1, unknown=0


| mutation | predicted | source-only signal | matrix signal | source shifted? | follow-up shifted? | err cells |
|---|---|---|---|---|---|---|
| M00-identity | equivalent | equivalent | equivalent | False | False | 0 |
| M01-openmoc-runner-chi-zero | semantic | semantic | semantic | True | True | 0 |
| M02-openmoc-runner-sigt-from-siga | semantic | semantic | semantic | True | True | 0 |
| M03-openmoc-runner-swap-fuel-moderator | semantic | semantic | semantic | True | True | 0 |
| M04-openmoc-runner-drop-nu-sigma-f | semantic | equivalent | semantic | True | True | 0 |
| M05-openmoc-runner-chi-swap-groups | semantic | semantic | semantic | True | True | 0 |
| M06-openmoc-runner-vacuum-boundary | semantic | semantic | semantic | True | True | 0 |
| M07-openmoc-adapter-nsf-inverse | semantic | equivalent | semantic | False | True | 0 |
| M08-openmoc-adapter-nsf-square | semantic | equivalent | semantic | False | True | 0 |
| M09-openmoc-adapter-nsf-moderator | equivalent | equivalent | semantic | False | True | 0 |
| M10-openmoc-adapter-nsf-identity | semantic | equivalent | semantic | False | True | 0 |
| M11-openmoc-adapter-nsf-fast-only | semantic | equivalent | semantic | False | True | 0 |
| M12-openmoc-adapter-sa-no-sigt-update | solver-dependent | equivalent | semantic | False | True | 0 |
| M13-openmoc-adapter-sa-inverse | semantic | equivalent | semantic | False | True | 0 |
| M14-openmoc-adapter-sa-moderator | semantic | equivalent | semantic | False | True | 0 |
| M15-openmc-runner-chi-zero | semantic | error | error | False | False | 2 |
| M16-openmc-runner-scatter-transpose | semantic | semantic | semantic | True | True | 0 |
| M17-openmc-runner-vacuum-boundary | semantic | semantic | semantic | True | True | 0 |
| M18-openmc-runner-batches-too-few | semantic | equivalent | equivalent | False | False | 0 |
| M19-openmc-runner-hardcode-keff | semantic | semantic | semantic | True | True | 0 |
| M20-openmc-runner-chi-swap-groups | semantic | semantic | semantic | True | True | 0 |
| M21-openmc-runner-fission-zero | semantic | equivalent | equivalent | False | False | 0 |
| M22-openmc-adapter-nsf-inverse | semantic | equivalent | semantic | False | True | 0 |
| M23-openmc-adapter-nsf-square | semantic | equivalent | semantic | False | True | 0 |
| M24-openmc-adapter-nsf-moderator | equivalent | equivalent | semantic | False | True | 0 |
| M25-openmc-adapter-nsf-identity | semantic | equivalent | semantic | False | True | 0 |
| M26-openmc-adapter-sa-no-sigt-update | semantic | equivalent | equivalent | False | False | 0 |
| M27-openmc-adapter-sa-inverse | semantic | equivalent | semantic | False | True | 0 |

## Discarded (equivalent) mutants — why?

### M00-identity

*Identity (no change).*


**Predicted**: equivalent.


**Rationale**: False-positive control. Any MR reporting `detected` on M00 is a bug in the MR or the harness, not in the SUT.


Observed per-scenario shifts:

| scenario | Δsource | Δfollow-up | threshold |
|---|---|---|---|
| openmoc-pincell-nu-sigma-f | 0.000000 | 0.000000 | 0.005665 |
| openmc-pincell-nu-sigma-f | 0.000000 | 0.000000 | 0.007550 |
| openmoc-pincell-sigma-a | 0.000000 | 0.000000 | 0.005665 |
| openmc-pincell-sigma-a | 0.000000 | 0.000000 | 0.005623 |

### M18-openmc-runner-batches-too-few

*Hard-code batches=5, inactive=2, particles=200 (very noisy MC).*


**Predicted**: semantic.


**Rationale**: Massive statistical-noise injection. Tests whether the MR's GreaterThan/LessThan assertion is robust to MC noise at low particle counts.


Observed per-scenario shifts:

| scenario | Δsource | Δfollow-up | threshold |
|---|---|---|---|
| openmc-pincell-nu-sigma-f | 0.032631 | 0.040132 | 0.154487 |
| openmc-pincell-sigma-a | 0.032631 | 0.058557 | 0.154487 |

### M21-openmc-runner-fission-zero

*Zero out fission cross section but keep nu_sigma_f.*


**Predicted**: semantic.


**Rationale**: Inconsistent fission data. OpenMC may warn / refuse / silently proceed; documents observed behaviour either way.


Observed per-scenario shifts:

| scenario | Δsource | Δfollow-up | threshold |
|---|---|---|---|
| openmc-pincell-nu-sigma-f | 0.000000 | 0.000000 | 0.007550 |
| openmc-pincell-sigma-a | 0.000000 | 0.000000 | 0.005623 |

### M26-openmc-adapter-sa-no-sigt-update

*Update fuel.sigma_a but leave sigma_t unchanged (OpenMC twin of M12).*


**Predicted**: semantic.


**Rationale**: Matched pair with M12. OpenMC twin is semantic (OpenMC reads sigma_t and sigma_a independently). Together with M12 this documents the cross-solver split clearly.


Observed per-scenario shifts:

| scenario | Δsource | Δfollow-up | threshold |
|---|---|---|---|
| openmc-pincell-sigma-a | 0.000000 | 0.002759 | 0.005623 |

