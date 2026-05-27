# (Equation × Meta-Pattern) Coverage Audit

> **Date**: 2026-05-27
> **Status**: Active reference (PR-T3-7 / Phase 4 of the T2-T3 visualization + gap-fill plan)
> **Source plan**: [`docs/superpowers/plans/2026-05-27-t2-systemmt-visualization-and-t3-gap-fill-plan.md`](../plans/2026-05-27-t2-systemmt-visualization-and-t3-gap-fill-plan.md)
> **Producer**: `MetBench_BLL.SystemMT.Coverage.MetaPatternMatrixAuditor.Audit(IMrCatalogProvider)` ([`MetBench_BLL.Core/SystemMT/Coverage/MetaPatternMatrixAuditor.cs`](../../../MetBench_BLL.Core/SystemMT/Coverage/MetaPatternMatrixAuditor.cs))

---

## §1 Purpose

Snapshot the current (equation × meta-pattern) coverage of the system-MT MR catalog and surface the empty cells so Phase 5 (PR-T3-8) can pick a concrete gap-fill candidate. The audit is data-driven — it reads `IMrCatalogProvider.Load()` and groups by `(EquationKey, MetaPattern)` — so it cannot drift from the catalog: re-running it after every new MR refreshes the matrix.

Meta-pattern slugs follow `CLAUDE.md §2.2 T4`: `Mono` (monotonicity), `Inv` (invariance), `Conv` (convergence).

---

## §2 Matrix snapshot (2026-05-27 / 32 MRs / 11 equation keys)

Generated from the manifest catalog under `SUT/<sut>/catalog.json`. The empty-string equation key (first row) groups MRs that pre-date the `equation_key` convention and only carry the prose `equation` field (e.g. `Boltzmann`, `Linear ODE`, `Ballistics`). Future PRs may migrate these by populating `equation_key`; until then they share one bucket.

| equation_key | Mono | Inv | Conv |
|---|---|---|---|
| *(empty)* | 8 — `damped-oscillator-scale-state`, `heat-equation-amplitude`, `lotka-volterra-scale-gamma`, `openmc-pincell-{nu-sigma-f \| sigma-a}`, `openmoc-pincell-{nu-sigma-f \| sigma-a}`, `projectile-scale-v0` | ❌ | 2 — `openmc-pincell-particle-count-convergence`, `openmoc-pincell-ray-track-convergence` |
| `_test_csv` | 1 — `csv-roundtrip-identity` | ❌ | ❌ |
| `advection` | 1 — `advection-amplitude-linearity` | 1 — `advection-mesh-conservation` | ❌ |
| `bateman` | 1 — `decay-chain-scale-initial` | 1 — `bateman-mass-conservation` | 1 — `bateman-timestep-cauchy` |
| `burgers` | 1 — `burgers-amplitude-peak-monotone` | 1 — `burgers-mesh-conservation` | ❌ |
| `diffusion` | 1 — `diffusion-source-linearity` | ❌ | 1 — `diffusion-mesh-richardson` |
| `heat-equation-1d` | 1 — `fourier-alpha-monotonic` | ❌ | 1 — `fourier-timestep-convergence` |
| `lotka-volterra` | 1 — `scipy-ivp-lv-prey-growth-monotone` | ❌ | 1 — `scipy-ivp-lv-step-convergence` |
| `navier-stokes` | 2 — `subchannel-flow-temperature-monotone`, `subchannel-heat-flux-linearity` | ❌ | ❌ |
| `poisson` | 2 — `poisson-source-superposition`, `scipy-bvp-poisson-source-superposition` | ❌ | 2 — `poisson-mesh-richardson`, `scipy-bvp-poisson-seed-mesh-insensitivity` |
| `wave` | 1 — `wave-amplitude-linearity` | ❌ | 1 — `wave-mesh-energy-convergence` |

**Totals**: 32 MRs binned across 21 filled cells; 12 empty cells (gaps) in the 11 × 3 = 33 Cartesian product.

---

## §3 Gap list, ranked by feasibility

Ranking follows plan §Phase 4 step 5: prefer **no new SUT** > existing SUT with no venv > existing SUT requiring venv. The `_test_csv` row is excluded from gap-fill candidates because it's a synthetic I/O-regression SUT, not a physical equation.

### Tier A — existing SUT, pure-stdlib (no venv needed)

These all build on SUTs already shipping pure-stdlib runners on Linux CI; a new MR drops in via input adapter + manifest entry + blueprint + metadata, no environment work.

| # | (equation, pattern) | Candidate MR id | Physical claim | SUT |
|---|---|---|---|---|
| A1 | `(burgers, Conv)` | `burgers-timestep-convergence` | Doubling internal `num_steps` shrinks the residual between two solutions monotonically (`ErrorMonotonicPredicate` on `peak_amplitude` or `mass_integral`). Conservative Lax-Friedrichs scheme is first-order accurate in time → expected halving residual. | `SUT/burgers_1d/` |
| A2 | `(advection, Conv)` | `advection-timestep-convergence` | Same convergence shape as A1 but on the linear upwind scheme. Linear scheme makes the convergence rate analytically clean. | `SUT/advection_1d/` |
| A3 | `(diffusion, Inv)` | `diffusion-source-symmetry-invariance` | Spatially mirrored source → spatially mirrored solution (parity invariance). `FieldEqualityPredicate(MirrorPairing)` on `phi` array. | `SUT/diffusion_1d/` |
| A4 | `(navier-stokes, Inv)` | `subchannel-power-rebalance-invariance` | Total enthalpy in − out equals total power deposited regardless of axial power profile (steady-state energy balance). `DerivedInvariantPredicate(EnthalpyBalance)`. | `SUT/subchannel_1d/` |
| A5 | `(navier-stokes, Conv)` | `subchannel-mesh-convergence` | Doubling the axial node count → residual on `T_exit` shrinks (Richardson). | `SUT/subchannel_1d/` |
| A6 | `(poisson, Inv)` | `poisson-source-superposition-invariance` | Adding sources `f1 + f2` → solution `u1 + u2` (linearity invariance, distinct from the existing source-scaling Mono MR). `BinaryComparisonPredicate(Equal)` on `u_max`. | `SUT/poisson_1d/` |
| A7 | `(wave, Inv)` | `wave-amplitude-symmetry-invariance` | Mirror IC `u0(x) → u0(L−x)` → mirrored solution at every t (parity invariance). `FieldEqualityPredicate(MirrorPairing)`. | `SUT/wave_1d/` |
| A8 | `(heat-equation-1d, Inv)` | `fourier-energy-decay-invariance` | Total `∫u dx` strictly monotonically decreases (or is bounded above) under pure diffusion — an invariant inequality. `BinaryComparisonPredicate(LessEqual)`. | `SUT/heat_equation/` |
| A9 | `(burgers, Conv)` | (covered by A1) | — | — |

### Tier B — existing SUT, requires Python venv on CI

These need SciPy / OpenMOC / OpenMC; viable but CI must skip cleanly when the venv is absent (existing `[SkippableFact]` pattern).

| # | (equation, pattern) | Candidate MR id | Notes |
|---|---|---|---|
| B1 | `(lotka-volterra, Inv)` | `scipy-ivp-lv-poincare-invariance` | Lotka-Volterra has a conserved quantity `V(x,y) = δx − γ ln x + βy − α ln y`. SciPy venv. |
| B2 | `(_test_csv, Inv)` & `(_test_csv, Conv)` | (excluded — synthetic SUT) | The `_test_csv` SUT exists to regression-guard the `metbench_io` helper, not to claim a physical MR; do not gap-fill these cells. |
| B3 | `(empty equation_key, Inv)` | (not a single SUT — see §4) | Pre-`equation_key` MRs (OpenMOC / OpenMC / heat-equation / projectile / damped-oscillator / lotka-volterra-stdlib). Each would slot into a different MR id under its own SUT; reduce to a Tier-A or Tier-B row above by picking the SUT first. |

### Excluded

- All `_test_csv` cells (synthetic SUT, not a physical equation)
- All cells under the empty-string equation key (these are book-keeping debt — the right fix is to populate `equation_key` on existing MRs, not to add new MRs there)

---

## §4 Recommended top-1 candidate for Phase 5

**`A1` — `burgers-timestep-convergence`** on `SUT/burgers_1d/`.

### Why this one over the rest of Tier A

| Criterion | A1 (burgers Conv) | A2 (advection Conv) | A3 (diffusion Inv) | A4 (subchannel Inv) |
|---|---|---|---|---|
| Existing SUT, no venv | ✅ | ✅ | ✅ | ✅ |
| Existing convergence kernel reusable (`ErrorMonotonicPredicate`) | ✅ shares predicate with `bateman-timestep-cauchy` / `wave-mesh-energy-convergence` / etc. | ✅ same | ❌ new `FieldEqualityPredicate(MirrorPairing)` kernel work | ❌ new `DerivedInvariantPredicate` derived quantity |
| Physical claim deterministic in numeric check | ✅ residual at `num_steps × 2` ≤ residual at `num_steps` within tolerance | ✅ same | ⚠ depends on whether mirror invariance holds bit-exactly or within FP tol — needs care | ⚠ enthalpy balance has its own tolerance budget |
| MR id naming aligned with sibling SUTs | ✅ `burgers-timestep-convergence` mirrors `bateman-timestep-cauchy`, `fourier-timestep-convergence` | ✅ also good | — | — |
| Coverage value (closes a Conv gap on a nonlinear PDE) | ✅✅ — Conv on the only nonlinear-hyperbolic SUT, sharper test than the linear A2 case | ⚠ linear hyperbolic Conv is easier to derive analytically; less defect-catching power | ✅ but Inv | ✅ but Inv on a different family |

A1 wins on **reuse of existing typed-predicate kernels** (no new `Typed/Predicates/` C# work in Phase 5; just one new MR row + blueprint + manifest + a Python input adapter that varies `num_steps`) **and** on **defect-detection signal** (Conv on nonlinear hyperbolic catches more variants of time-integration bugs than the linear cases).

### Concrete Phase 5 outline (pinned shape — Phase 5 will validate / adjust before code lands)

1. `SUT/burgers_1d/burgers_1d_input_adapter_num_steps.py` — vary `num_steps` (the existing parameter that internally selects dt under `Courant = 0.5`).
2. `LegacyCatalogFactory.cs` — append one `MrBlueprint` for `burgers-timestep-convergence` with `AssertionTypeCode = "less-equal"` (Error-Monotonic predicate) and `EquationKey = "burgers"`.
3. `SystemMtMetadataCatalog.cs` — append one `MrMetadata`.
4. `SUT/burgers_1d/catalog.json` — manifest entry with `equation_key: "burgers"`, `meta_pattern: "Conv"`, transform-step list pointing at the new adapter.
5. Pinned-count bump 32 → 33 across the six test sites the existing `PR-Bol-2B` / `PR-N2` precedent updated (see plan §Phase 5 step 5).
6. End-to-end fact: `LauncherEndToEndBurgersTimestepConvergenceTests` running both reference and refined phases on cloud CI without venv.

### Fallback if A1 hits a snag during Phase 5

In order: **A2** (`advection-timestep-convergence`) → **A4** (`subchannel-mesh-convergence`) → **A6** (`poisson-source-superposition-invariance`, Inv but linear and easy to verify). All Tier-A; all pure-stdlib; all closeable in a single PR.

---

## §5 Re-running the audit

```csharp
var provider = new ManifestMrCatalogProvider(launcherOptions);
var matrix = MetaPatternMatrixAuditor.Audit(provider);
// matrix.Cells   — filled (equation × pattern) buckets with MR ids
// matrix.Gaps    — empty cells in the {distinct equations} × {Mono, Inv, Conv} product
// matrix.Equations / matrix.MetaPatterns — axis labels in stable order
```

The auditor is stateless; calling it after any new MR PR (Phase 5, future T3 gap-fills, future Bol-* PRs) refreshes the snapshot. `MetaPatternMatrixAuditorTests` pins the structural contract; a regression that miscounts or silently drops MRs would fail those facts.

---

## §6 Update triggers

Refresh this document (matrix table + top-1 candidate) when any of the following changes:

- A new MR is added to the catalog (filled cell count moves)
- An MR's `equation_key` or `meta_pattern` is migrated (Unclassified bucket shrinks; gap list shifts)
- Phase 5 picks and ships a Tier-A candidate (the corresponding cell moves from gap → filled; recommend the next gap)


---

## §7 Known data debt: 8 MRs with empty `equation_key` (deferred)

The §2 matrix shows 8/32 MRs falling under the empty-string `equation_key` bucket. This is **catalog metadata debt**, not a code defect — the auditor faithfully reports what the manifests carry. Once those 8 MRs gain proper `equation_key` values, the matrix collapses by one row and the gap list shrinks correspondingly (currently 12 gaps; estimated 6–8 after migration).

Proposed mapping (to be confirmed by a future data-only PR):

| MR id | Proposed `equation_key` | Source manifest |
|---|---|---|
| `damped-oscillator-scale-state` | `damped-oscillator` | `SUT/damped_oscillator/catalog.json` |
| `heat-equation-amplitude` | `heat-equation-1d` | `SUT/heat_equation/catalog.json` |
| `lotka-volterra-scale-gamma` | `lotka-volterra` | `SUT/lotka_volterra/catalog.json` (the pure-stdlib variant) |
| `openmc-pincell-nu-sigma-f` | `neutron-transport` | `SUT/openmc/catalog.json` |
| `openmc-pincell-sigma-a` | `neutron-transport` | `SUT/openmc/catalog.json` |
| `openmoc-pincell-nu-sigma-f` | `neutron-transport` | `SUT/openmoc/catalog.json` |
| `openmoc-pincell-sigma-a` | `neutron-transport` | `SUT/openmoc/catalog.json` |
| `projectile-scale-v0` | `projectile` (new EquationMetadata) | `SUT/projectile/catalog.json` |

Deferred because: each maps to a different SUT, so the migration is a per-manifest edit, not a single sweep. The Phase 5 gap-fill (`subchannel-friction-invariance`, #192) does not depend on this migration. A docs+data PR can land independently after Phase 5 / 6.

When the migration ships, refresh §2 + §3 + §4 here and re-run `MetaPatternMatrixAuditorTests` to lock the new numbers.
