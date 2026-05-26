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

**Decision (2026-05-26): T3 SUT expansion is paused.**

Rationale:

- The executable set already covers four representative PDE classes plus the reactor anchors.
- All three primary meta-patterns are exercised; no current gap demands a new SUT.
- T1's adapter / launcher / catalog scaffolding is empirically validated four times across distinct PDE classes; no further T1-stability evidence is needed before pivoting to other tracks.
- The platform-level priorities that move user-visible value next are **T2** (visualization extensions), **T4** (`IMRDiscoverer` framework deepening — meta-prompt / multi-LLM / SCG heuristics), **T5** (anomaly investigation analytics on top of the existing typed-verification evidence), and **T6** (mutation testing). Each of these moves the platform more than a fifth 1D PDE SUT.
- Pausing also reduces the maintenance footprint of additional SUTs (sample fixtures, runner scripts, catalog rows, end-to-end tests) while no concrete gap demands them.

Selection criterion if this decision is later revisited (one SUT at a time; never batch):

- (i) Falls under one of the four drivers listed in §4 (external solver pilot / ML/PINN / reactor anchor deepening / missing meta-pattern).
- (ii) Equation semantics, MR semantics, catalog bindings, tests, CI strategy, and skip policy are written in a candidate-specific implementation plan registered in the [active plan index](../plans/2026-05-25-metbench-active-plan-index.md) §1 **before** any code lands.
- (iii) Does not require new verification semantics. If new semantics are needed (e.g. noise-aware typed predicate for an ML/PINN candidate), an independent verification-semantics PR ships **first** under `MetBench_BLL.Core/SystemMT/Catalog/Typed/`.
- (iv) Tested venv / install path that can either run on cloud CI or skips cleanly per `CLAUDE.md` §8.

Until those four conditions are jointly satisfied for a uniquely named candidate, no T3 SUT PR may be opened.

## 6. Cross-references

- [`docs/status/current.md`](../../status/current.md) §2 (canonical inventory), §4 (active control documents), §7 (current execution order).
- [`docs/t3-program-selection.md`](../../t3-program-selection.md) §6 (boundary summary).
- [`docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md`](../plans/2026-05-25-metbench-active-plan-index.md) §1.
- [`docs/requirements.md`](../../requirements.md) F-T3-02 (traceability row).
- [`docs/PROJECT-STRUCTURE.md`](../../PROJECT-STRUCTURE.md) §2 SUT inventory + §3 test matrix.
