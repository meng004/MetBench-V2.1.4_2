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


**Discard rate**: 3 of 48 candidates (6.2%) classified equivalent under the matrix rule.


Classification counts: semantic=32, equivalent=3, error=13, unknown=0


| mutation | predicted | source-only signal | matrix signal | source shifted? | follow-up shifted? | err cells |
|---|---|---|---|---|---|---|
| Mut00-identity | equivalent | equivalent | error | True | False | 2 |
| Mut01-openmoc-runner-chi-zero | semantic | semantic | semantic | True | True | 0 |
| Mut02-openmoc-runner-sigt-from-siga | semantic | error | equivalent | False | False | 0 |
| Mut03-openmoc-runner-swap-fuel-moderator | semantic | semantic | semantic | True | True | 0 |
| Mut04-openmoc-runner-drop-nu-sigma-f | semantic | equivalent | semantic | True | True | 0 |
| Mut05-openmoc-runner-chi-swap-groups | semantic | semantic | semantic | True | True | 0 |
| Mut06-openmoc-runner-vacuum-boundary | semantic | semantic | semantic | True | True | 0 |
| Mut07-openmoc-adapter-nsf-inverse | semantic | equivalent | semantic | False | True | 0 |
| Mut08-openmoc-adapter-nsf-square | semantic | equivalent | semantic | False | True | 0 |
| Mut09-openmoc-adapter-nsf-moderator | equivalent | equivalent | semantic | False | True | 0 |
| Mut10-openmoc-adapter-nsf-identity | semantic | equivalent | semantic | False | True | 0 |
| Mut11-openmoc-adapter-nsf-fast-only | semantic | equivalent | semantic | False | True | 0 |
| Mut12-openmoc-adapter-sa-no-sigt-update | solver-dependent | equivalent | semantic | False | True | 0 |
| Mut13-openmoc-adapter-sa-inverse | semantic | equivalent | semantic | False | True | 0 |
| Mut14-openmoc-adapter-sa-moderator | semantic | equivalent | semantic | False | True | 0 |
| Mut15-openmc-runner-chi-zero | semantic | error | error | False | False | 16 |
| Mut16-openmc-runner-scatter-transpose | semantic | semantic | error | True | True | 2 |
| Mut17-openmc-runner-vacuum-boundary | semantic | semantic | error | True | True | 2 |
| Mut18-openmc-runner-batches-too-few | semantic | equivalent | error | False | False | 2 |
| Mut19-openmc-runner-hardcode-keff | semantic | semantic | error | True | True | 2 |
| Mut20-openmc-runner-chi-swap-groups | semantic | semantic | error | True | True | 2 |
| Mut21-openmc-runner-fission-zero | semantic | equivalent | error | True | False | 2 |
| Mut22-openmc-adapter-nsf-inverse | semantic | equivalent | semantic | False | True | 0 |
| Mut23-openmc-adapter-nsf-square | semantic | equivalent | semantic | False | True | 0 |
| Mut24-openmc-adapter-nsf-moderator | equivalent | equivalent | semantic | False | True | 0 |
| Mut25-openmc-adapter-nsf-identity | semantic | equivalent | semantic | False | True | 0 |
| Mut26-openmc-adapter-sa-no-sigt-update | semantic | equivalent | equivalent | False | False | 0 |
| Mut27-openmc-adapter-sa-inverse | semantic | equivalent | semantic | False | True | 0 |
| Mut28-openmoc-runner-chi-fast-only | semantic | equivalent | semantic | True | True | 0 |
| Mut29-openmoc-adapter-fuel-sigt-no-siga-update | equivalent | equivalent | equivalent | False | False | 0 |
| Mut30-openmoc-adapter-moderator-sigma-a-no-sigt-update | semantic | equivalent | semantic | False | True | 0 |
| Mut31-openmoc-adapter-group-permute-fuel-only | semantic | equivalent | semantic | False | True | 0 |
| Mut32-openmoc-adapter-fuel-sigma-s-identity | semantic | equivalent | semantic | False | True | 0 |
| Mut33-openmoc-adapter-fuel-radius-shrink | semantic | equivalent | semantic | False | True | 0 |
| Mut34-openmc-adapter-particles-no-op | semantic | equivalent | semantic | False | False | 0 |
| Mut35-openmc-runner-chi-fast-only | semantic | equivalent | error | True | True | 2 |
| Mut36-openmc-adapter-group-permute-fuel-only | semantic | equivalent | semantic | False | True | 0 |
| Mut37-openmc-adapter-fuel-sigma-s-identity | semantic | equivalent | semantic | False | True | 0 |
| Mut38-openmc-adapter-fuel-radius-shrink | semantic | equivalent | semantic | False | True | 0 |
| Mut39-openmoc-runner-hardcode-y-from-x | semantic | equivalent | semantic | True | True | 0 |
| Mut40-openmc-runner-hardcode-y-from-x | semantic | equivalent | error | True | True | 2 |
| Mut41-openmoc-runner-clamp-y-offset-positive | semantic | equivalent | semantic | True | False | 0 |
| Mut42-openmoc-runner-clamp-x-offset-positive | semantic | equivalent | semantic | True | False | 0 |
| Mut43-openmc-runner-clamp-y-offset-positive | semantic | equivalent | error | True | False | 2 |
| Mut44-openmc-runner-clamp-x-offset-positive | semantic | equivalent | error | True | False | 2 |
| Mut45-openmoc-runner-ignore-temperature | semantic | equivalent | semantic | True | True | 0 |
| Mut46-openmc-runner-ignore-temperature | semantic | equivalent | error | True | True | 2 |
| Mut47-openmoc-runner-tally-y-sign-bucket | semantic | equivalent | semantic | True | False | 0 |

## Discarded (equivalent) mutants — why?

### Mut02-openmoc-runner-sigt-from-siga

*Pass mat["sigma_a"] to setSigmaT instead of mat["sigma_t"].*


**Predicted**: semantic.


**Rationale**: Realistic indexing slip: sigma_a < sigma_t by physics, so the runner would use a smaller total cross section, raising k_eff. The MR ratio for ScaleNuSigmaF should still hold qualitatively; this probes whether the MR is sensitive to absolute-scale corruption.

### Mut26-openmc-adapter-sa-no-sigt-update

*Update fuel.sigma_a but leave sigma_t unchanged (OpenMC twin of Mut12).*


**Predicted**: semantic.


**Rationale**: Matched pair with Mut12. OpenMC twin is semantic (OpenMC reads sigma_t and sigma_a independently). Together with Mut12 this documents the cross-solver split clearly.


Observed per-scenario shifts:

| scenario | Δsource | Δfollow-up | threshold |
|---|---|---|---|
| openmc-pincell-sigma-a | 0.000000 | 0.002759 | 0.005623 |

### Mut29-openmoc-adapter-fuel-sigt-no-siga-update

*Update fuel.sigma_t but skip the matching fuel.sigma_a bump.*


**Predicted**: equivalent.


**Rationale**: Adapter inconsistency analogous to Mut12 but for the new fuel-sigma_t MR. OpenMOC reads sigma_t directly and derives sigma_a from sigma_t − Σ sigma_s, so missing the JSON sigma_a write is silent on OpenMOC: the runner still sees the correct effective absorption. Pure documentation-vs-runtime split — predicted equivalent for OpenMOC and semantic only when a downstream consumer actually reads sigma_a from the JSON (none currently does, so this is in the catalogue mostly to document the parity with Mut12 — important when OpenMC support lands).


Observed per-scenario shifts:

| scenario | Δsource | Δfollow-up | threshold |
|---|---|---|---|
| openmoc-pincell-fuel-sigma-t | 0.000000 | 0.000000 | 0.005665 |

