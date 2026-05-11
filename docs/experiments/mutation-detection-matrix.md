# Mutation-detection matrix

MR factor: 1.5. Status `not-affected` means the mutation patches a file the scenario does not exercise; status `ran` reports detected/missed; status `error` reports a runtime failure.


## Per-MR detection rate (Wilson 95% CI)

| Scenario | n (semantic mutants affecting it) | detected | missed | errors | rate | 95% CI |
|---|---|---|---|---|---|---|
| openmoc-pincell-nu-sigma-f | 11 | 7 | 4 | 0 | 63.6% | [35.4%, 84.8%] |
| openmc-pincell-nu-sigma-f | 8 | 3 | 5 | 0 | 37.5% | [13.7%, 69.4%] |
| openmoc-pincell-sigma-a | 9 | 4 | 5 | 0 | 44.4% | [18.9%, 73.3%] |
| openmc-pincell-sigma-a | 5 | 2 | 3 | 0 | 40.0% | [11.8%, 76.9%] |

## Identity false-positive sanity

M00 (identity) detected on 0 / 4 scenarios. Expected 0 — ✓ PASS.


## Per-mutation detail

| mutation | scenario | outcome | k_source | k_followup | ratio |
|---|---|---|---|---|---|
| M00-identity | openmoc-pincell-nu-sigma-f | missed | 1.13306 | 1.69990 | 1.50028 |
| M00-identity | openmc-pincell-nu-sigma-f | missed | 1.12450 | 1.68996 | 1.50286 |
| M00-identity | openmoc-pincell-sigma-a | missed | 1.13306 | 0.80690 | 0.71215 |
| M00-identity | openmc-pincell-sigma-a | missed | 1.12450 | 0.80272 | 0.71384 |
| M01-openmoc-runner-chi-zero | openmoc-pincell-nu-sigma-f | detected | 0.00059 | 0.00059 | 1.00000 |
| M01-openmoc-runner-chi-zero | openmc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| M01-openmoc-runner-chi-zero | openmoc-pincell-sigma-a | missed | 0.00059 | 0.00051 | 0.85942 |
| M01-openmoc-runner-chi-zero | openmc-pincell-sigma-a | _not-affected_ |  |  |  |
| M02-openmoc-runner-sigt-from-siga | openmoc-pincell-nu-sigma-f | detected | inf | inf | nan |
| M02-openmoc-runner-sigt-from-siga | openmc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| M02-openmoc-runner-sigt-from-siga | openmoc-pincell-sigma-a | detected | inf | inf | nan |
| M02-openmoc-runner-sigt-from-siga | openmc-pincell-sigma-a | _not-affected_ |  |  |  |
| M03-openmoc-runner-swap-fuel-moderator | openmoc-pincell-nu-sigma-f | missed | 1.43300 | 2.14972 | 1.50015 |
| M03-openmoc-runner-swap-fuel-moderator | openmc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| M03-openmoc-runner-swap-fuel-moderator | openmoc-pincell-sigma-a | missed | 1.43300 | 0.91350 | 0.63747 |
| M03-openmoc-runner-swap-fuel-moderator | openmc-pincell-sigma-a | _not-affected_ |  |  |  |
| M04-openmoc-runner-drop-nu-sigma-f | openmoc-pincell-nu-sigma-f | detected | nan | nan | nan |
| M04-openmoc-runner-drop-nu-sigma-f | openmc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| M04-openmoc-runner-drop-nu-sigma-f | openmoc-pincell-sigma-a | detected | nan | nan | nan |
| M04-openmoc-runner-drop-nu-sigma-f | openmc-pincell-sigma-a | _not-affected_ |  |  |  |
| M05-openmoc-runner-chi-swap-groups | openmoc-pincell-nu-sigma-f | missed | 1.27800 | 1.91687 | 1.49990 |
| M05-openmoc-runner-chi-swap-groups | openmc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| M05-openmoc-runner-chi-swap-groups | openmoc-pincell-sigma-a | missed | 1.27800 | 0.96468 | 0.75484 |
| M05-openmoc-runner-chi-swap-groups | openmc-pincell-sigma-a | _not-affected_ |  |  |  |
| M06-openmoc-runner-vacuum-boundary | openmoc-pincell-nu-sigma-f | missed | 0.00470 | 0.00704 | 1.49898 |
| M06-openmoc-runner-vacuum-boundary | openmc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| M06-openmoc-runner-vacuum-boundary | openmoc-pincell-sigma-a | missed | 0.00470 | 0.00466 | 0.99057 |
| M06-openmoc-runner-vacuum-boundary | openmc-pincell-sigma-a | _not-affected_ |  |  |  |
| M07-openmoc-adapter-nsf-inverse | openmoc-pincell-nu-sigma-f | detected | 1.13306 | 0.75516 | 0.66648 |
| M07-openmoc-adapter-nsf-inverse | openmc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| M07-openmoc-adapter-nsf-inverse | openmoc-pincell-sigma-a | _not-affected_ |  |  |  |
| M07-openmoc-adapter-nsf-inverse | openmc-pincell-sigma-a | _not-affected_ |  |  |  |
| M08-openmoc-adapter-nsf-square | openmoc-pincell-nu-sigma-f | missed | 1.13306 | 2.55018 | 2.25071 |
| M08-openmoc-adapter-nsf-square | openmc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| M08-openmoc-adapter-nsf-square | openmoc-pincell-sigma-a | _not-affected_ |  |  |  |
| M08-openmoc-adapter-nsf-square | openmc-pincell-sigma-a | _not-affected_ |  |  |  |
| M09-openmoc-adapter-nsf-moderator | openmoc-pincell-nu-sigma-f | detected | 1.13306 | 1.13306 | 1.00000 |
| M09-openmoc-adapter-nsf-moderator | openmc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| M09-openmoc-adapter-nsf-moderator | openmoc-pincell-sigma-a | _not-affected_ |  |  |  |
| M09-openmoc-adapter-nsf-moderator | openmc-pincell-sigma-a | _not-affected_ |  |  |  |
| M10-openmoc-adapter-nsf-identity | openmoc-pincell-nu-sigma-f | detected | 1.13306 | 1.13306 | 1.00000 |
| M10-openmoc-adapter-nsf-identity | openmc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| M10-openmoc-adapter-nsf-identity | openmoc-pincell-sigma-a | _not-affected_ |  |  |  |
| M10-openmoc-adapter-nsf-identity | openmc-pincell-sigma-a | _not-affected_ |  |  |  |
| M11-openmoc-adapter-nsf-fast-only | openmoc-pincell-nu-sigma-f | detected | 1.13306 | 0.52376 | 0.46226 |
| M11-openmoc-adapter-nsf-fast-only | openmc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| M11-openmoc-adapter-nsf-fast-only | openmoc-pincell-sigma-a | _not-affected_ |  |  |  |
| M11-openmoc-adapter-nsf-fast-only | openmc-pincell-sigma-a | _not-affected_ |  |  |  |
| M12-openmoc-adapter-sa-no-sigt-update | openmoc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| M12-openmoc-adapter-sa-no-sigt-update | openmc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| M12-openmoc-adapter-sa-no-sigt-update | openmoc-pincell-sigma-a | detected | 1.13306 | 1.13306 | 1.00000 |
| M12-openmoc-adapter-sa-no-sigt-update | openmc-pincell-sigma-a | _not-affected_ |  |  |  |
| M13-openmoc-adapter-sa-inverse | openmoc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| M13-openmoc-adapter-sa-inverse | openmc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| M13-openmoc-adapter-sa-inverse | openmoc-pincell-sigma-a | detected | 1.13306 | 1.51087 | 1.33344 |
| M13-openmoc-adapter-sa-inverse | openmc-pincell-sigma-a | _not-affected_ |  |  |  |
| M14-openmoc-adapter-sa-moderator | openmoc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| M14-openmoc-adapter-sa-moderator | openmc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| M14-openmoc-adapter-sa-moderator | openmoc-pincell-sigma-a | missed | 1.13306 | 0.47635 | 0.42041 |
| M14-openmoc-adapter-sa-moderator | openmc-pincell-sigma-a | _not-affected_ |  |  |  |
| M15-openmc-runner-chi-zero | openmoc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| M15-openmc-runner-chi-zero | openmc-pincell-nu-sigma-f | _error_ |  |  |  |
| M15-openmc-runner-chi-zero | openmoc-pincell-sigma-a | _not-affected_ |  |  |  |
| M15-openmc-runner-chi-zero | openmc-pincell-sigma-a | _error_ |  |  |  |
| M16-openmc-runner-scatter-transpose | openmoc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| M16-openmc-runner-scatter-transpose | openmc-pincell-nu-sigma-f | missed | 0.58237 | 0.87430 | 1.50128 |
| M16-openmc-runner-scatter-transpose | openmoc-pincell-sigma-a | _not-affected_ |  |  |  |
| M16-openmc-runner-scatter-transpose | openmc-pincell-sigma-a | missed | 0.58237 | 0.39868 | 0.68458 |
| M17-openmc-runner-vacuum-boundary | openmoc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| M17-openmc-runner-vacuum-boundary | openmc-pincell-nu-sigma-f | missed | 0.00491 | 0.00738 | 1.50153 |
| M17-openmc-runner-vacuum-boundary | openmoc-pincell-sigma-a | _not-affected_ |  |  |  |
| M17-openmc-runner-vacuum-boundary | openmc-pincell-sigma-a | missed | 0.00491 | 0.00486 | 0.98945 |
| M19-openmc-runner-hardcode-keff | openmoc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| M19-openmc-runner-hardcode-keff | openmc-pincell-nu-sigma-f | detected | 1.00000 | 1.00000 | 1.00000 |
| M19-openmc-runner-hardcode-keff | openmoc-pincell-sigma-a | _not-affected_ |  |  |  |
| M19-openmc-runner-hardcode-keff | openmc-pincell-sigma-a | detected | 1.00000 | 1.00000 | 1.00000 |
| M20-openmc-runner-chi-swap-groups | openmoc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| M20-openmc-runner-chi-swap-groups | openmc-pincell-nu-sigma-f | missed | 1.27937 | 1.91775 | 1.49898 |
| M20-openmc-runner-chi-swap-groups | openmoc-pincell-sigma-a | _not-affected_ |  |  |  |
| M20-openmc-runner-chi-swap-groups | openmc-pincell-sigma-a | missed | 1.27937 | 0.96310 | 0.75280 |
| M22-openmc-adapter-nsf-inverse | openmoc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| M22-openmc-adapter-nsf-inverse | openmc-pincell-nu-sigma-f | detected | 1.12450 | 0.75033 | 0.66726 |
| M22-openmc-adapter-nsf-inverse | openmoc-pincell-sigma-a | _not-affected_ |  |  |  |
| M22-openmc-adapter-nsf-inverse | openmc-pincell-sigma-a | _not-affected_ |  |  |  |
| M23-openmc-adapter-nsf-square | openmoc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| M23-openmc-adapter-nsf-square | openmc-pincell-nu-sigma-f | missed | 1.12450 | 2.53531 | 2.25461 |
| M23-openmc-adapter-nsf-square | openmoc-pincell-sigma-a | _not-affected_ |  |  |  |
| M23-openmc-adapter-nsf-square | openmc-pincell-sigma-a | _not-affected_ |  |  |  |
| M24-openmc-adapter-nsf-moderator | openmoc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| M24-openmc-adapter-nsf-moderator | openmc-pincell-nu-sigma-f | detected | 1.12450 | 1.12450 | 1.00000 |
| M24-openmc-adapter-nsf-moderator | openmoc-pincell-sigma-a | _not-affected_ |  |  |  |
| M24-openmc-adapter-nsf-moderator | openmc-pincell-sigma-a | _not-affected_ |  |  |  |
| M25-openmc-adapter-nsf-identity | openmoc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| M25-openmc-adapter-nsf-identity | openmc-pincell-nu-sigma-f | missed | 1.12450 | 1.12450 | 1.00000 |
| M25-openmc-adapter-nsf-identity | openmoc-pincell-sigma-a | _not-affected_ |  |  |  |
| M25-openmc-adapter-nsf-identity | openmc-pincell-sigma-a | _not-affected_ |  |  |  |
| M27-openmc-adapter-sa-inverse | openmoc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| M27-openmc-adapter-sa-inverse | openmc-pincell-nu-sigma-f | _not-affected_ |  |  |  |
| M27-openmc-adapter-sa-inverse | openmoc-pincell-sigma-a | _not-affected_ |  |  |  |
| M27-openmc-adapter-sa-inverse | openmc-pincell-sigma-a | detected | 1.12450 | 1.49882 | 1.33287 |

## Cross-solver agreement (Cohen's κ)

κ measured on matched-pair mutants where the same conceptual fault is applied to both solvers' files (see `mutation-catalogue.md` matched-pair index). Each row: did the OpenMOC scenario detect the OpenMOC mutant; did the OpenMC scenario detect its OpenMC twin.


### ScaleNuSigmaF pairs

Pairs evaluated: 4 / 4. Cohen's κ = **0.500** (moderate).


| OpenMOC mutant | OpenMC mutant | OpenMOC outcome | OpenMC outcome |
|---|---|---|---|
| M07-openmoc-adapter-nsf-inverse | M22-openmc-adapter-nsf-inverse | detected | detected |
| M08-openmoc-adapter-nsf-square | M23-openmc-adapter-nsf-square | missed | missed |
| M09-openmoc-adapter-nsf-moderator | M24-openmc-adapter-nsf-moderator | detected | detected |
| M10-openmoc-adapter-nsf-identity | M25-openmc-adapter-nsf-identity | detected | missed |


### ScaleFuelSigmaA pairs

Pairs evaluated: 1 / 2. Cohen's κ = **1.000** (almost perfect).


| OpenMOC mutant | OpenMC mutant | OpenMOC outcome | OpenMC outcome |
|---|---|---|---|
| M13-openmoc-adapter-sa-inverse | M27-openmc-adapter-sa-inverse | detected | detected |


### Runner-level pairs (chi/boundary) — NuSigmaF scenarios

Pairs evaluated: 3 / 3. Cohen's κ = **1.000** (almost perfect).


| OpenMOC mutant | OpenMC mutant | OpenMOC outcome | OpenMC outcome |
|---|---|---|---|
| M01-openmoc-runner-chi-zero | M15-openmc-runner-chi-zero | detected | detected |
| M05-openmoc-runner-chi-swap-groups | M20-openmc-runner-chi-swap-groups | missed | missed |
| M06-openmoc-runner-vacuum-boundary | M17-openmc-runner-vacuum-boundary | missed | missed |


## Threshold sensitivity

Re-classify candidates at tightened and relaxed relative thresholds using the matrix data;

how many flip relative to the 0.5% baseline?


| Relative threshold | # flips vs 0.5% baseline | Which |
|---|---|---|
| 0.20% | 0 | — |
| 0.50% (baseline) | 0 | — |
| 1.00% | 0 | — |
| 2.00% | 0 | — |

---

See [`discussion.md`](discussion.md) for hand-written analysis (coverage gaps, cross-solver κ interpretation, Phase 2 hand-off).

