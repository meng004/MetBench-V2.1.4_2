# Mutation Catalogue — Stage 5 Phase 1

> 28 candidate mutations spanning OpenMOC and OpenMC runners and input
> adapters. Source of truth: `tools/mutations.py`. This document is a
> human-readable companion; if the two disagree, **the module wins** and
> this doc should be updated to match.

## How to read this table

- **id**: stable identifier used in CSVs and reports.
- **target**: which file is patched (relative to repo root).
- **predicted classification** (`P-class`): the author's pre-screening guess.
  Baseline screening (Task 3) verifies it against the actual `|Δk_eff|`
  threshold.
- **predicted detector** (`P-detect`): which MR family is expected to
  detect the mutant, **if** it survives screening. Empty (`—`) means
  "the fault is real, but no MR is expected to catch it" — i.e. it is
  a coverage-gap probe.
- **rationale**: 1-3 sentences on why this candidate is included.

The set is deliberately biased toward realistic faults but seeds equivalent
mutants and coverage-gap probes for the screening validation and the
sensitivity discussion.

## Catalogue

### Identity control

| id | target | P-class | P-detect | rationale |
|----|--------|---------|----------|-----------|
| `Mut00-identity` | (none) | equivalent | — | False-positive control. Any MR reporting `detected` on Mut00 indicates an MR or harness bug. |

### OpenMOC runner

| id | target | P-class | P-detect | rationale |
|----|--------|---------|----------|-----------|
| `Mut01-openmoc-runner-chi-zero` | `SUT/openmoc/openmoc_runner.py` | semantic | — | Zero fission spectrum kills `k_eff` baseline; tests whether the MR ratio survives near-zero source case. |
| `Mut02-openmoc-runner-sigt-from-siga` | `SUT/openmoc/openmoc_runner.py` | semantic | — | Realistic indexing slip; `sigma_a < sigma_t` so total xs is under-estimated, `k_eff` rises. |
| `Mut03-openmoc-runner-swap-fuel-moderator` | `SUT/openmoc/openmoc_runner.py` | semantic | `nu_sigma_f`, `sigma_a` | Fuel and moderator material assignments swapped. Major fault, expected detection on both MRs. |
| `Mut04-openmoc-runner-drop-nu-sigma-f` | `SUT/openmoc/openmoc_runner.py` | semantic | — | Drop `setNuSigmaF` call entirely; `k_eff` collapses, both MR sides equally affected. |
| `Mut05-openmoc-runner-chi-swap-groups` | `SUT/openmoc/openmoc_runner.py` | semantic | — | Reverse the chi array. Fuel `chi = [1,0] → [0,1]`: fission emits into thermal group. |
| `Mut06-openmoc-runner-vacuum-boundary` | `SUT/openmoc/openmoc_runner.py` | semantic | — | Vacuum instead of reflective boundary; pin-cell leaks. |

### OpenMOC input adapter (`ScaleNuSigmaF`)

| id | target | P-class | P-detect | rationale |
|----|--------|---------|----------|-----------|
| `Mut07-openmoc-adapter-nsf-inverse` | `SUT/openmoc/openmoc_input_adapter.py` | semantic | `nu_sigma_f` | Scale by `1/factor`; direction flips. Strict `GreaterThan` should catch this. |
| `Mut08-openmoc-adapter-nsf-square` | `SUT/openmoc/openmoc_input_adapter.py` | semantic | — | Scale by `factor**2`; over-amplifies. `GreaterThan` still satisfied; expected miss (coverage gap). |
| `Mut09-openmoc-adapter-nsf-moderator` | `SUT/openmoc/openmoc_input_adapter.py` | **equivalent** | — | Scale moderator `nu_sigma_f` (which is `[0,0]`). No-op. **Screening test case.** |
| `Mut10-openmoc-adapter-nsf-identity` | `SUT/openmoc/openmoc_input_adapter.py` | semantic | `nu_sigma_f` | Ignore factor; source = follow-up. `GreaterThan` fails strictly. |
| `Mut11-openmoc-adapter-nsf-fast-only` | `SUT/openmoc/openmoc_input_adapter.py` | semantic | — | Scale only fast-group. `k_eff` increases but less; `GreaterThan` still passes. |

### OpenMOC input adapter (`ScaleFuelSigmaA`)

| id | target | P-class | P-detect | rationale |
|----|--------|---------|----------|-----------|
| `Mut12-openmoc-adapter-sa-no-sigt-update` | `SUT/openmoc/openmoc_input_adapter_sigma_a.py` | **solver-dependent** | `sigma_a` | OpenMOC ignores `sigma_a` (reads `sigma_t` directly), so this is equivalent on OpenMOC; OpenMC reads both, so semantic on OpenMC. Documents cross-solver split. |
| `Mut13-openmoc-adapter-sa-inverse` | `SUT/openmoc/openmoc_input_adapter_sigma_a.py` | semantic | `sigma_a` | Scale by `1/factor`; absorption decreases, `k_eff` rises but `LessThan` expected. |
| `Mut14-openmoc-adapter-sa-moderator` | `SUT/openmoc/openmoc_input_adapter_sigma_a.py` | semantic | `sigma_a` | Moderator `sigma_a = [0.0004, 0.020]` (non-zero), so this *does* change `k_eff` but in the wrong target. Direction depends on data. |

### OpenMC runner

| id | target | P-class | P-detect | rationale |
|----|--------|---------|----------|-----------|
| `Mut15-openmc-runner-chi-zero` | `SUT/openmc/openmc_runner.py` | semantic | — | OpenMC twin of Mut01. |
| `Mut16-openmc-runner-scatter-transpose` | `SUT/openmc/openmc_runner.py` | semantic | — | Transpose scatter matrix; up/down-scattering swap. |
| `Mut17-openmc-runner-vacuum-boundary` | `SUT/openmc/openmc_runner.py` | semantic | — | OpenMC twin of Mut06. |
| `Mut18-openmc-runner-batches-too-few` | `SUT/openmc/openmc_runner.py` | semantic | — | Massive MC noise: batches=5, inactive=2, particles=200. |
| `Mut19-openmc-runner-hardcode-keff` | `SUT/openmc/openmc_runner.py` | semantic | `nu_sigma_f`, `sigma_a` | Bypass solver, return `k=1.0`. Strict assertion fails. |
| `Mut20-openmc-runner-chi-swap-groups` | `SUT/openmc/openmc_runner.py` | semantic | — | OpenMC twin of Mut05. |
| `Mut21-openmc-runner-fission-zero` | `SUT/openmc/openmc_runner.py` | semantic | — | Set sigma_f to zero but keep nu_sigma_f; inconsistent fission data. |

### OpenMC input adapters (matched pairs for cross-solver κ)

| id | target | P-class | P-detect | rationale |
|----|--------|---------|----------|-----------|
| `Mut22-openmc-adapter-nsf-inverse` | `SUT/openmc/openmc_input_adapter.py` | semantic | `nu_sigma_f` | OpenMC twin of Mut07. |
| `Mut23-openmc-adapter-nsf-square` | `SUT/openmc/openmc_input_adapter.py` | semantic | — | OpenMC twin of Mut08. |
| `Mut24-openmc-adapter-nsf-moderator` | `SUT/openmc/openmc_input_adapter.py` | **equivalent** | — | OpenMC twin of Mut09 (screening test). |
| `Mut25-openmc-adapter-nsf-identity` | `SUT/openmc/openmc_input_adapter.py` | semantic | `nu_sigma_f` | OpenMC twin of Mut10. |
| `Mut26-openmc-adapter-sa-no-sigt-update` | `SUT/openmc/openmc_input_adapter_sigma_a.py` | semantic | `sigma_a` | OpenMC twin of Mut12 — but semantic on OpenMC. The asymmetry vs Mut12 documents the same patch landing differently on different solvers. |
| `Mut27-openmc-adapter-sa-inverse` | `SUT/openmc/openmc_input_adapter_sigma_a.py` | semantic | `sigma_a` | OpenMC twin of Mut13. |

## Matched-pair index (for cross-solver Cohen's κ in Task 5)

| OpenMOC twin | OpenMC twin |
|--------------|-------------|
| Mut07 (nsf-inverse) | Mut22 |
| Mut08 (nsf-square) | Mut23 |
| Mut09 (nsf-moderator, equivalent) | Mut24 |
| Mut10 (nsf-identity) | Mut25 |
| Mut12 (sa-no-sigt) | Mut26 |
| Mut13 (sa-inverse) | Mut27 |
| Mut01 (chi-zero) | Mut15 |
| Mut05 (chi-swap-groups) | Mut20 |
| Mut06 (vacuum-bc) | Mut17 |

These 9 matched pairs are the population for the cross-solver κ
computation, restricted further by what survives baseline screening.

## Out of scope for this catalogue

- Output adapter mutations (e.g., scaling k_eff in `*_output_adapter.py`).
  The orchestrator parses runner output JSON directly, so these mutations
  do not exercise the end-to-end path. They would be useful in a different
  study (testing the `PythonOutputAdapter` C# layer); not here.
- Geometry-altering mutations (fuel radius, lattice spacing). Those
  motivate a different MR family (rotation / mesh refinement) studied in
  Stage 5 Phase 2.
- Solver-parameter mutations beyond Mut18 (e.g., halving `max_iters` on
  OpenMOC). Worth investigating once the headline detection rate is in
  hand.
