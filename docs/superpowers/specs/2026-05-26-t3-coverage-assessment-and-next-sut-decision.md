# T3 Coverage Assessment and Next-SUT Decision Record

> **Date**: 2026-05-26
> **Status**: Active
> **Role**: Single current T3 coverage truth + next-SUT gate. Governs whether T3 SUT expansion continues, pauses, or selects a uniquely named next SUT. Any new T3 SUT PR must cite this record. Supersedes any ad-hoc "next candidate" mention in older plans.
> **Inventory anchor (canonical via `docs/status/current.md` §2)**: **13 SUT / 12 equations / 25 MRs** after PR #134 (Poisson 1D) / #136 (Advection 1D) / #138 (Wave 1D) / #140 (Burgers 1D).

---

## 1. Scope

T3 (per [`CLAUDE.md`](../../../CLAUDE.md) §2) — "代表性方程 × 程序类型" coverage. This record assesses whether the existing executable SUT set is sufficient under the four-PDE-class + reactor-anchor framing, and gates any further SUT addition behind a written justification.

This record does **not** revisit T0–T2 / T4–T6 priorities; those are governed by the [active plan index](../plans/2026-05-25-metbench-active-plan-index.md).

## 2. Executable T3 coverage matrix (2026-05-26)

| SUT directory | Equation | Math class | Runtime | Onboarded by | Launcher end-to-end test |
|---|---|---|---|---|---|
| `SUT/decay_chain/` | Bateman | ODE — linear system | Pure-stdlib | Stage 8 P1 | `LauncherEndToEndOdeTests` |
| `SUT/damped_oscillator/` | 2nd-order linear ODE (damping) | ODE — linear | Pure-stdlib | Stage 8 P1 | `LauncherEndToEndOdeTests` |
| `SUT/lotka_volterra/` | Lotka-Volterra | ODE — nonlinear | Pure-stdlib | Stage 8 P1 | `LauncherEndToEndOdeTests` |
| `SUT/projectile/` | Projectile motion | Closed-form (algebraic) | Pure-stdlib | G-09 | — (BDD + launcher catalog) |
| `SUT/heat_equation/` | 1D heat / Fourier | PDE — parabolic | Pure-stdlib finite difference | Stage 4 / Stage 8 | `HeatEquationAmplitude.feature` |
| `SUT/diffusion_1d/` | 1D diffusion | PDE — parabolic | Pure-stdlib finite difference | Stage 8 P4 | — |
| `SUT/poisson_1d/` | Poisson | PDE — elliptic | Pure-stdlib Thomas tridiagonal | PR #134 | `LauncherEndToEndPoissonTests` |
| `SUT/advection_1d/` | Linear advection | PDE — first-order linear hyperbolic | Pure-stdlib first-order upwind FD + periodic BC | PR #136 | `LauncherEndToEndAdvectionTests` |
| `SUT/wave_1d/` | Wave (`u_tt = c²·u_xx`) | PDE — second-order linear hyperbolic | Pure-stdlib leapfrog FD + Dirichlet BC | PR #138 | `LauncherEndToEndWaveTests` |
| `SUT/burgers_1d/` | Inviscid Burgers (`u_t + (u²/2)_x = 0`) | PDE — nonlinear hyperbolic | Pure-stdlib Lax-Friedrichs flux differencing + periodic BC | PR #140 | `LauncherEndToEndBurgersTests` |
| `SUT/openmoc/` | Boltzmann (neutron transport) | Integro-differential (deterministic MoC) | OpenMOC (venv) | Stage 3 / Stage 8 | `OpenMocPinCellNuSigmaF.feature` + `CrossProgramNeutronTransportMrs.feature` |
| `SUT/openmc/` | Boltzmann (neutron transport) | Monte Carlo | OpenMC (venv + binary) | #57 / Stage 8 | `CrossProgramNeutronTransportMrs.feature` |
| `SUT/subchannel_1d/` | Navier-Stokes (surrogate) | PDE — fluid surrogate | Pure-stdlib | Stage 8 P3 | — |

PDE-class coverage of the executable set:

| PDE class | Covered by |
|---|---|
| Elliptic | `poisson_1d` (PR #134) |
| Parabolic | `heat_equation` (Stage 4 / Stage 8), `diffusion_1d` (Stage 8 P4) |
| First-order linear hyperbolic | `advection_1d` (PR #136) |
| Second-order linear hyperbolic | `wave_1d` (PR #138) |
| Nonlinear hyperbolic | `burgers_1d` (PR #140) |
| Integro-differential transport (Boltzmann) | `openmoc`, `openmc` |

The five reactor-physics anchors (per [`docs/t3-program-selection.md`](../../t3-program-selection.md)) are each tied to at least one executable SUT: bateman → `decay_chain`; fourier → `heat_equation`; diffusion → `diffusion_1d`; navier-stokes → `subchannel_1d`; boltzmann → `openmoc` / `openmc`.

## 3. MR meta-pattern coverage matrix

Evidence is the `mr_id` in `SUT/<sut>/catalog.json` (25 distinct `mr_id` values across the 13 catalogs as of PR #140).

| Meta-pattern | Evidence (SUT / MR / PR) |
|---|---|
| `m_mono` (amplitude / parameter monotonicity) | `decay-chain-scale-initial` (decay_chain, Stage 8 P1), `damped-oscillator-scale-state` (damped_oscillator, Stage 8 P1), `lotka-volterra-scale-gamma` (lotka_volterra, Stage 8 P1), `heat-equation-amplitude` (heat_equation, Stage 4 / Stage 8), `projectile-scale-v0` (projectile, G-09), `diffusion-source-linearity` (diffusion_1d, Stage 8 P4), `subchannel-flow-temperature-monotone` (subchannel_1d, Stage 8 P3), `subchannel-heat-flux-linearity` (subchannel_1d, Stage 8 P3), `fourier-alpha-monotonic` (Stage 8 P2), `poisson-source-superposition` (poisson_1d, PR #134), `advection-amplitude-linearity` (advection_1d, PR #136), `wave-amplitude-linearity` (wave_1d, PR #138), `burgers-amplitude-peak-monotone` (burgers_1d, PR #140) |
| `m_inv` (conservation / invariant under transformation) | `bateman-mass-conservation` (decay_chain, Stage 8 P1), `advection-mesh-conservation` (advection_1d, PR #136), `burgers-mesh-conservation` (burgers_1d, PR #140) |
| `m_conv` (mesh / step convergence, Richardson, L²) | `bateman-timestep-cauchy` (decay_chain, Stage 8 P1), `fourier-timestep-convergence` (Stage 8 P2), `diffusion-mesh-richardson` (diffusion_1d, Stage 8 P4), `poisson-mesh-richardson` (poisson_1d, PR #134), `wave-mesh-energy-convergence` (wave_1d, PR #138) |
| Cross-program agreement (Boltzmann pair) | `openmoc-pincell-nu-sigma-f` × `openmc-pincell-nu-sigma-f`, `openmoc-pincell-sigma-a` × `openmc-pincell-sigma-a` — via `CrossProgramNeutronTransportMrs.feature` |

All three primary meta-patterns (`m_mono`, `m_inv`, `m_conv`) are exercised across at least three SUTs each, spanning ODE + parabolic + elliptic + first-order linear hyperbolic + second-order linear hyperbolic + nonlinear hyperbolic. No primary meta-pattern is uncovered by the existing executable set.

## 4. Boundary statement

**Pure-stdlib PDE class coverage is complete** at the representative-family granularity:

- Elliptic, parabolic, first-order linear hyperbolic, second-order linear hyperbolic, and nonlinear hyperbolic PDE classes each have at least one executable SUT.
- All three primary MR meta-patterns (`m_mono` / `m_inv` / `m_conv`) are exercised in this set.
- T1's adapter / launcher / catalog layers were demonstrated stable across four consecutive new SUTs spanning four distinct PDE classes (PR #134 → #136 → #138 → #140) without any T1 framework change.

The marginal value of adding another 1D pure-stdlib PDE SUT is **low**: it would not extend PDE-class coverage, would not add a new meta-pattern, and would not change T1 stability evidence. Further T3 expansion must therefore be driven by at least one of the following, not by adding another 1D pure-stdlib PDE:

1. **External solver pilot** — onboarding a real external solver (FEniCS / OpenFOAM / Clawpack / SUNDIALS CVODE) as a packaging + adapter + CI-skip-policy exercise. This stresses different parts of T1 (venv install, binary path resolution, longer runtimes, larger output payloads) than pure-stdlib SUTs have yet exercised.
2. **ML/PINN / data-driven SUT pilot** — a surrogate model (DeepXDE / PDEBench-backed FNO / PINN) whose verification semantics are statistical or noise-aware, exposing the currently fail-closed noise-aware predicate path (`less-noise-aware` / `greater-noise-aware`, see `docs/status/current.md` §6).
3. **Reactor anchor deepening** — extending an existing reactor anchor with a higher-fidelity SUT (e.g. OpenMC depletion, PARCS / OpenMOC adjoint when upstream supports it). This is also the only path that can close the m_adj follow-up.
4. **Missing meta-pattern** — if a future MR family discovered via T4 cannot be exercised by any existing SUT, a new SUT may be onboarded specifically to enable it.

## 5. Next-SUT gate (decision)

**Decision (2026-05-26): T3 SUT expansion is paused for pure-stdlib candidates.** Adding another 1D pure-stdlib PDE SUT remains paused per the rationale below. **External-solver-pilot expansion is now selectively re-opened** for one uniquely named candidate at a time; see §5.2.

Rationale:

- The executable set already covers four representative PDE classes plus the reactor anchors.
- All three primary meta-patterns are exercised; no current gap demands a new SUT.
- T1's adapter / launcher / catalog scaffolding is empirically validated four times across distinct PDE classes; no further T1-stability evidence is needed before pivoting to other tracks.
- The platform-level priorities that move user-visible value next are **T2** (visualization extensions), **T4** (`IMRDiscoverer` framework deepening — meta-prompt / multi-LLM / SCG heuristics), **T5** (anomaly investigation analytics on top of the existing typed-verification evidence), and **T6** (mutation testing). Each of these moves the platform more than a fifth 1D PDE SUT.
- Pausing also reduces the maintenance footprint of additional SUTs (sample fixtures, runner scripts, catalog rows, end-to-end tests) while no concrete gap demands them.
- External-solver-pilot expansion is re-openable under §4 driver #1 because it exercises a genuinely different T1 surface (external Python library install + clean-skip policy + heavier runtime) that pure-stdlib SUTs cannot. Each external-solver candidate is gated by its own §5.x sub-decision and a candidate-specific implementation plan.

Selection criterion if this decision is later revisited (one SUT at a time; never batch):

- (i) Falls under one of the four drivers listed in §4 (external solver pilot / ML/PINN / reactor anchor deepening / missing meta-pattern).
- (ii) Equation semantics, MR semantics, catalog bindings, tests, CI strategy, and skip policy are written in a candidate-specific implementation plan registered in the [active plan index](../plans/2026-05-25-metbench-active-plan-index.md) §1 **before** any code lands.
- (iii) Does not require new verification semantics. If new semantics are needed (e.g. noise-aware typed predicate for an ML/PINN candidate), an independent verification-semantics PR ships **first** under `MetBench_BLL.Core/SystemMT/Catalog/Typed/`.
- (iv) Tested venv / install path that can either run on cloud CI or skips cleanly per `CLAUDE.md` §8.

Until those four conditions are jointly satisfied for a uniquely named candidate, no T3 SUT PR may be opened.

## 5.2 Selected SUT — External solver pilot (2026-05-26): `scipy-ivp-lotka-volterra` — COMPLETED

**Status: Completed (T3C-IVP merged).** Under §4 driver #1 (External solver pilot), the SUT `scipy-ivp-lotka-volterra` — a SciPy `solve_ivp`-backed Lotka-Volterra ODE solver — exercises the External-solver-pilot T1 surface against the existing pure-stdlib `SUT/lotka_volterra/` for the same equation. Implementation merged via T3C-IVP; the candidate-specific plan is archived in the active plan index.

The §5 selection criteria (i)–(iv) are addressed as follows:

- (i) Falls under §4 driver #1 (External solver pilot) — first SciPy-backed SUT in the repository; tests the External-Python-library install + clean-skip path that none of the pure-stdlib SUTs exercises.
- (ii) Candidate-specific implementation plan registered at [`docs/superpowers/plans/2026-05-26-t3c-scipy-ivp-lotka-volterra-plan.md`](../plans/2026-05-26-t3c-scipy-ivp-lotka-volterra-plan.md), covering equation reference (re-uses the existing `lotka-volterra` `EquationMetadata`), MR semantics for the two new MRs, catalog binding shape, test classes, CI strategy, and clean-skip policy with the verbatim skip reason `"SciPy runtime not configured for scipy-ivp-lotka-volterra."`.
- (iii) No new verification semantics are required. Both MRs reuse existing typed predicates via `LegacyAssertionPredicateMapper`: `scipy-ivp-lv-prey-growth-monotone` uses `AssertionTypeCode="greater"` (→ `BinaryComparisonKernel`); `scipy-ivp-lv-step-convergence` uses `AssertionTypeCode="approx"` (→ `ScaledEqualityKernel`).
- (iv) Tested venv / install path: env var `METBENCH_SCIPY_PYTHON` (default falls back to `LauncherOptions.SystemPython`); when SciPy is missing, the launcher end-to-end tests skip cleanly via `[SkippableFact]` + `ScipyTestPaths.ScipyImportable()` with the verbatim skip reason above. The `dotnet-test.yml` cloud CI does not currently install SciPy; tests will skip cleanly there. The Method MT, Typed Semantic Catalog runtime, and WPF surfaces are not modified by this candidate.

**Out of scope for §5.2**: BVP / elliptic external-solver expansion was registered as a future candidate at §5.2 closure; it is now active and selected via §5.3 below. The pure-stdlib §5 pause is unchanged.

## 5.3 Selected next SUT — External solver pilot continuation (2026-05-26, post-IVP): `scipy-bvp-poisson-1d`

**Status: Active.** Under §4 driver #1 (External solver pilot), the unique next SUT selected for T3 expansion after §5.2 IVP completion is **`scipy-bvp-poisson-1d`** — a SciPy `solve_bvp`-backed 1D Poisson elliptic SUT that exercises the BVP / elliptic external-solver-pilot T1 surface against the existing pure-stdlib `SUT/poisson_1d/` for the same equation.

The §5 selection criteria (i)–(iv) are addressed as follows:

- (i) Falls under §4 driver #1 (External solver pilot) — second SciPy-backed SUT, validating that the External-solver-pilot path opened in §5.2 extends to BVP / elliptic problems and to a sparse / dense linear-system internal call shape that the IVP RK45 surface did not exercise.
- (ii) Candidate-specific implementation plan registered at [`docs/superpowers/plans/2026-05-26-t3c-scipy-bvp-poisson-1d-plan.md`](../plans/2026-05-26-t3c-scipy-bvp-poisson-1d-plan.md), covering equation reference (re-uses the existing `poisson` `EquationMetadata`), MR semantics for the two new MRs, catalog binding shape, test classes, CI strategy, and clean-skip policy with the verbatim skip reason `"SciPy runtime not configured for scipy-bvp-poisson-1d."`.
- (iii) No new verification semantics are required. Both MRs reuse existing typed predicates via `LegacyAssertionPredicateMapper`: `scipy-bvp-poisson-source-superposition` uses `AssertionTypeCode="greater"` (→ `BinaryComparisonKernel`); `scipy-bvp-poisson-mesh-richardson` uses `AssertionTypeCode="approx"` (→ `ScaledEqualityKernel`).
- (iv) Tested venv / install path: env var `METBENCH_SCIPY_PYTHON` (already introduced by §5.2; default falls back to `LauncherOptions.SystemPython`); when SciPy is missing, the launcher end-to-end tests skip cleanly via `[SkippableFact]` + `ScipyTestPaths.ScipyImportable()` with the verbatim skip reason above. The `dotnet-test.yml` cloud CI does not currently install SciPy; tests will skip cleanly there. The Method MT, Typed Semantic Catalog runtime, and WPF surfaces are not modified by this candidate.

**Out of scope for §5.3**: no other external-solver candidates are introduced. Any further external-solver SUT (e.g. FEniCS, OpenFOAM, Clawpack) requires its own §5.x sub-decision plus a candidate-specific implementation plan.

## 5.1 Candidate Backlog / 候选池

This backlog enumerates candidates whose **future** evaluation is anticipated. Listing here is **not** selection. **Pure-stdlib T3 SUT expansion remains paused; §5.2 IVP is completed and §5.3 BVP is the active external-solver-pilot selection.** Nothing in this section unlocks any other code PR. A backlog entry becomes a real candidate only after a follow-up decision-record revision (or successor record) explicitly picks it as the unique next SUT under one of the §4 drivers, and only after the entry's start conditions are jointly satisfied. Start timing is decided by the user, not inferred from this list.

### 5.1.1 MeshGraphNets — cylinder flow surrogate (ML/data-driven SUT pilot candidate)

| Field | Value |
|---|---|
| Backlog category | §4 driver #2 — ML/PINN / data-driven SUT pilot |
| Equation surface | Incompressible Navier-Stokes (cylinder wake, vortex shedding) via the DeepMind MeshGraphNets surrogate trained on the published `cylinder_flow` dataset |
| Status | Backlog only — **not selected**, does not unlock PR-T3C |
| Why this is anticipated | First non-pure-stdlib, non-classical-solver SUT; exercises (a) data-driven runtime path, (b) checkpoint-driven inference, (c) mesh-output handling, (d) the currently fail-closed noise-aware predicate path (`less-noise-aware` / `greater-noise-aware`, see `docs/status/current.md` §6) |

**Start conditions (ALL must be satisfied before a candidate-specific plan is opened, and the plan itself is then prerequisite to any code PR):**

1. **Runtime venv available** — a tested Python venv (e.g. `METBENCH_MESHGRAPHNETS_PYTHON`) with the model framework (TensorFlow / JAX / PyTorch as upstream requires) installable on at least one supported platform; cloud CI must either install it or skip cleanly per `CLAUDE.md` §8. The venv strategy is owned by the candidate-specific implementation plan; it MAY reuse the §5.1.2 multi-venv capability if that capability already exists, otherwise the plan must specify a self-contained venv setup.
2. **Checkpoint prepared** — a reproducible trained checkpoint identified by source, license, hash, and provenance. Either bundled (size-permitting) or fetched by a documented script with caching.
3. **Tiny fixture prepared** — a small mesh / initial-condition fixture that runs in seconds, suitable for end-to-end tests; not the full benchmark dataset.
4. **Asset manifest provided** — an asset-provenance manifest is provided in a form agreed by the candidate-specific implementation plan, listing every required asset (venv ref, checkpoint hash, fixture path, parser scripts) with verification commands so onboarding is reproducible; the §5.1.2 heavy-dependency SUT onboarding specification, if it has shipped by then, MAY standardise the manifest format, but is not a prerequisite.
5. **First MR set selected** — the initial MR family is named in writing (e.g. inlet-velocity scaling monotonicity, mesh-refinement noise-aware comparability, cross-method agreement against another NS-surrogate anchor solving the same cylinder-wake instance, if such an anchor is later added). Each MR's meta-pattern is identified.
6. **Clean-skip policy explicit** — when the venv, checkpoint, or fixture is missing, the launcher end-to-end test class must skip cleanly (xUnit `[SkippableFact]` or equivalent), with the skip reason logged. No false failures on cloud CI without the assets.
7. **Verification-semantics prerequisite** — if the first MR set includes any MR that requires noise-aware typed predicates (highly likely given §5.1.1 motivation (d)), an independent verification-semantics PR adding a noise-aware typed predicate under `MetBench_BLL.Core/SystemMT/Catalog/Typed/` ships **first**, per §5(iii).
8. **Candidate-specific implementation plan registered** — `docs/superpowers/plans/<meshgraphnets-cylinder-flow-plan>.md` is written and registered in [`docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md`](../plans/2026-05-25-metbench-active-plan-index.md) §1, covering all of: equation semantics, MR semantics, catalog bindings (manifest + `LegacyCatalogFactory` blueprint), tests (parser/adapter smoke + launcher end-to-end), CI strategy, and skip policy. This is the §5 (ii) gate condition.

Until **all eight** are satisfied and a follow-up decision-record revision explicitly picks this candidate as the unique next SUT, no T3 SUT PR for MeshGraphNets may be opened. Start timing is at the user's discretion.

### 5.1.2 Non-T3 product capabilities listed for traceability

The following are **new T-level product capabilities** that interact with T3 onboarding but are **not themselves T3 candidates**. They neither unlock nor block PR-T3C; they are listed here only so that future T3 candidates (especially MeshGraphNets) can reference them without confusion. Start timing is decided by the user.

| Capability | Why it interacts with T3 (but is not T3C) | Trace |
|---|---|---|
| **Multi-venv configuration & management** | T1 (process/venv handling) needs to address heavy, conflicting dependencies (TF / JAX / PyTorch / OpenMC / OpenMOC venvs coexisting); MeshGraphNets cannot land cleanly without this | T1 extension; see `docs/requirements.md` gap entry |
| **UI-only MR / SUT onboarding** | A WPF-side data-entry surface so users can register MRs and (with constraints) SUTs without editing source; orthogonal to T3 — T3C is about adding a runnable SUT, this is about how *any* SUT/MR enters the catalog | T1 / T2 extension; see `docs/requirements.md` gap entry |
| **Heavy-dependency / data-driven SUT onboarding specification** | A normative onboarding spec covering checkpoint provenance, asset manifests, clean-skip policy, fixture sizing, license tracking — the prerequisite framework that lets MeshGraphNets and future similar SUTs land safely | T1 / T3 boundary; see `docs/requirements.md` gap entry |
| **SUT onboarding + MR usage documentation as a productised surface** | Treats per-SUT onboarding documentation + per-MR usage documentation as a maintained product artifact (template + index + lint), not ad-hoc per-PR text | T2 / docs productisation; see `docs/requirements.md` gap entry |

None of these four capabilities changes the §5 paused decision. None unlocks PR-T3C. None blocks PR-T3C either. They are listed so that a future MeshGraphNets implementation plan (or any other future T3 candidate plan) can reference whichever of these capabilities its start conditions depend on, instead of inventing the requirements ad-hoc.

## 6. Cross-references

- [`docs/status/current.md`](../../status/current.md) §2 (canonical inventory), §4 (active control documents), §7 (current execution order).
- [`docs/t3-program-selection.md`](../../t3-program-selection.md) §6 (boundary summary).
- [`docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md`](../plans/2026-05-25-metbench-active-plan-index.md) §1.
- [`docs/requirements.md`](../../requirements.md) F-T3-02 (traceability row).
- [`docs/PROJECT-STRUCTURE.md`](../../PROJECT-STRUCTURE.md) §2 SUT inventory + §3 test matrix.
