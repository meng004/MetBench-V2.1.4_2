# Minimum-MR-SubSet A-group Live Runtime Promotion

Date: 2026-06-03

Branch: `codex/minimum-mr-subset-a-group-live-runtime`

Baseline: `origin/main` fast-forwarded to `a3f5c65ed77996437b3e004773185ae339501d25` after PR #273.

`rtk` status: unavailable in this Windows VM; commands were run with native PowerShell.

## Classification

| Category | Result |
|---|---|
| AGroupImportExportStaging | PASS - PR #273 keeps P5/P4/P9 imported staging packages as `ImportedResearchEvidence`. |
| ExternalSourceCanonicalRun | PASS - prior VM runtime report records P5/P4/P9 direct external canonical smoke at `Minimum-MR-SubSet` commit `b931b5f74d0f3f3cb704cd6fedbcb4f523cccd7f`. |
| MetBenchLauncherRuntimeRun | PASS - this promotion adds live SUT manifests and launcher E2E tests for P5/P4/P9. |
| PromotedLiveMrs | `p5-power-response`, `p4-energy-invariant`, `p9-k-eff-noise-aware`. |
| NotPromotedMrs | P8/P3/P10/P1/P2/P6/P7. |
| ReasonNotPromoted | Outside this A-group hard scope; no runtime relation was formalized or implemented for them. |

## Promotion Shape

### P5

- live SUT name: `minimum-mr-subset-p5`
- MR id to promote: `p5-power-response`
- transform target path: `/kinetics/rho`
- assertion_type_code: `greater`
- value_name: `max_power`
- tolerance: rel `0`, abs `0`
- expected source/follow-up relation: scaling positive reactivity by `factor=2` strictly increases transient `max_power`.

### P4

- live SUT name: `minimum-mr-subset-p4`
- MR id to promote: `p4-energy-invariant`
- transform target path: `/integration/n_steps`
- assertion_type_code: `less`
- value_name: `energy_drift`
- tolerance: rel `0`, abs `0`
- expected source/follow-up relation: doubling velocity-Verlet `n_steps` for the same physical interval halves `dt` and reduces bounded Hamiltonian `energy_drift`.

### P9

- live SUT name: `minimum-mr-subset-p9-surrogate`
- MR id to promote: `p9-k-eff-noise-aware`
- transform target path: `/simulation/particles`
- assertion_type_code: `variance-ratio`
- value_name: `sigma_k`
- tolerance: rel `0.05`, abs `0`
- why this remains surrogate: the runner is a deterministic pure-stdlib formula for `k_eff` and `sigma_k`; it does not invoke OpenMC and must not be treated as real OpenMC execution.
- whether existing typed runtime supports the selected assertion without Core changes: yes. Existing `variance-ratio` launcher wiring and typed runtime are used; no files under `MetBench_BLL.Core/SystemMT/Catalog/Typed/*` were changed.

## Evidence Notes

- The live SUT catalogs intentionally add three runtime catalog rows and three SUT directories under `SUT/`.
- `ImportedResearchEvidence` remains staging/import provenance only and is not reused as MetBench `ExecutionEvidence`.
- Governance counts changed from `33 MR / 16 SUT / 13 equations` to `36 MR / 19 SUT / 15 equations`.
- P9 is explicitly named and described as a surrogate in directory name, SUT name, profile text, and manifest description.
