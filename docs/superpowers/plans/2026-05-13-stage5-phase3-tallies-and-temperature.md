# Stage 5 Phase 3 — Tally-symmetry MRs + Temperature-broadening MRs

> Plan stub for the two MR families Phase 2 explicitly deferred:
> tally-symmetry (covers historical Case 3) and temperature-broadening
> (covers historical Case 2). Both fit MetBench's existing
> `MrTransformation(name, parameters)` taxonomy and live entirely in
> `SUT/*` + new input adapters; no facade or C# changes required.

## Context

Phase 1 surveyed three real upstream fix commits (`historical-bugs.md`):

| Case | Repo | Bug | Phase-1/2 verdict |
|------|------|-----|-------------------|
| 1 | OpenMOC 28008901 | `_k_eff *= ...` accumulation | detected (matrix) |
| 2 | OpenMC PR #3712 | `add_temperature` returns None | **out-of-coverage** |
| 3 | OpenMC PR #3708 | distribcell tally group-name collapse | **out-of-coverage** |
| 4 | OpenMOC `CPUSolver` (Phase-2 discovery) | power-iteration narrow basin | detected by MR14 |

Cases 2 and 3 remain uncovered. Phase-3 scope: add MR families that
exercise the relevant code paths so analogous future bugs would be
caught.

## Family A — Tally-symmetry MRs (covers Case 3)

### Bug recap

OpenMC PR #3708: when the same distribcell tally was placed in
multiple identical subdomains, the group-name string collision in
the HDF5 export silently overwrote earlier subdomains' data, leaving
only the **last** subdomain in the output. Eigenvalue calculation
unaffected; tally export silently wrong.

### Why our MR misses it

Our SUT runners output a single scalar `k_eff`. The bug is in the
per-cell **flux distribution** export, which we don't read.

### MR family proposal

Add per-cell flux tallies to both runners:

```json
"output": {
  "tally_per_cell_flux": true,
  "tally_per_group": true
}
```

Runner output schema gains:

```json
"flux_per_cell": {
  "fuel":      {"group_0": 0.000123, "group_1": 0.000456},
  "moderator": {"group_0": 0.000789, "group_1": 0.000234}
}
```

New MRs (NOETHER `m_inv` MetaPattern, B1 symmetry on tally output):

* **MR-tally-mirror-x** — under MirrorX transform, flux_per_cell
  values for fuel cell are invariant; moderator values are
  invariant (single moderator region in our SUT). For multi-pin
  geometries this would be permutation-equivariant; here it's
  pointwise.
* **MR-tally-mirror-y / MR-tally-rotate-90** — analogous.
* **MR-tally-distribcell-permutation** (the headliner for Case 3) —
  if we extend the SUT to a 2×2 pin assembly, fluxes in the four
  identical pins must be equal. The bug would replace three of them
  with the fourth, and any pairwise-equality MR would catch it.

### Steps

1. Extend `pincell.json` schema with optional `output.tallies` block.
2. OpenMOC: add `openmoc.Mesh` + `openmoc.MeshTally` per cell, write
   per-group integrated flux to a `flux_per_cell` field in the
   output JSON.
3. OpenMC: add `openmc.CellFilter` + `openmc.EnergyFilter` tallies,
   read from the StatePoint, write to the same JSON shape.
4. Add `evaluate_mr` assertion `flux-pointwise-approx`: pass iff
   max relative deviation across all (cell, group) entries is below
   `tolerance_rel`.
5. Add scenarios `openmoc/openmc-pincell-tally-mirror-x/y` and
   reuse the existing pincell-offcentre.json source.
6. Add at least one mutation that breaks tally symmetry but
   preserves k_eff (e.g. `tally_save_skip_first_cell`) so the new
   MR family demonstrably catches something Phase 1/2 misses.
7. Optional Phase 3+: 2×2 pin sample for distribcell permutation MR.

### Estimated scope

* Runner changes: ~30 lines OpenMOC, ~40 lines OpenMC.
* Adapters: identical to existing mirror adapters (no new transform
  semantics, just exercising tally code path).
* Mutations: ~3 (one for each tally code path: OpenMOC mesh export,
  OpenMC CellFilter export, JSON serialization).
* Discussion + reports: ~half a day.

## Family B — Temperature-broadening MRs (covers Case 2)

### Bug recap

OpenMC PR #3712: `Material.add_temperature(T)` returned `None`
instead of `self`, so chained calls like
`Material(...).add_temperature(900)` produced `NoneType` and crashed
downstream. Live on the cross-section loading path; doesn't surface
as a k_eff change because the chain crashes before the eigenvalue
calculation runs.

### Why our MR misses it

* Our SUT uses fixed multi-group cross sections (no temperature
  dependence baked into the data file).
* Even if we exercised the temperature path, the bug pattern
  (NoneType crash) wouldn't surface as a k_eff shift — it would
  surface as a runtime error, which our matrix already records as
  `status=error` and treats as detected. So the bug WOULD be
  detected if we exercised the path; the gap is purely "we don't
  exercise it".

### MR family proposal

Two complementary approaches:

#### B.1 (cheap, recommended first) — Doppler-style cross-section scaling MR

Add an optional `temperature_kelvin` field to materials. The runner
multiplies absorption / fission cross sections by a Doppler-broadening
factor (e.g. `1 + α·log(T/T_ref)`). The *physics* is fictitious
(real Doppler broadening needs proper thermal treatment), but the
*code path* is what we want to exercise — passing temperature
through chained material constructors.

NOETHER MR family `m_mono` on `T`:

* **MR-temp-fuel-up** — raising fuel.temperature should raise
  resonance absorption → drop k_eff (canonical Doppler effect).
* **MR-temp-moderator-up** — raising moderator.temperature should
  raise scattering kernel "looseness" → small k_eff change.

#### B.2 (proper) — multi-temperature OpenMC integration

Switch OpenMC runner from multi-group to continuous-energy with a
two-temperature library. Trigger the same PR #3712 code path as a
real user would. Heavier (~day of work, needs ENDF data file).

### Recommended path

Start with B.1 (Doppler scaling MR). If a downstream user actually
wants Case-2-class detection, add B.2 later. B.1 already covers the
common case "developer broke temperature plumbing"; B.2 covers
"developer broke OpenMC's temperature-handling internals".

### Steps for B.1

1. Add `materials.fuel.temperature_kelvin` (optional, default 600 K)
   to JSON schema.
2. Both runners: at material construction time, multiply absorption
   cross sections by `1 + 0.005·log(T/T_ref)` (`T_ref` = 600 K).
3. New adapter `..._raise_fuel_temperature.py`: `T → T·factor`.
4. New scenario, assertion=`less` (k_eff drops with rising T).
5. Add 2-3 mutations that target the temperature plumbing (e.g.
   `runner_temperature_no_default` that crashes when JSON omits the
   new field — the 'not chained' analog of the real PR #3712).

### Estimated scope

* Runner changes: ~10 lines per runner.
* Adapter: 50 lines (mirrors fuel-sigma-a pattern).
* Mutations: 3.
* New scenario × 2 solvers + κ pair: standard.

## Open questions

* **Numerical reproducibility on Doppler scaling**: with
  `1 + 0.005·log(T/600)`, T = 900 K gives `1 + 0.005·log(1.5) ≈
  1.00203`. k_eff shift will be ~0.2% of k_eff, near the noise
  floor on OpenMC. Need to verify the MR is detectable above the MC
  noise budget *before* committing to this exact functional form.
* **Tally symmetry on single-region geometry**: pointwise flux
  invariance is trivially true for a centred fuel rod. Off-centred
  geometry (Phase 2's `pincell-offcentre.json`) makes it
  non-trivial. Still worth checking that the MR isn't *vacuous*
  there — flux distribution should change asymmetrically under
  mirror transforms but per-cell *integrated* flux may not.
* **Distribcell permutation needs a 2×2 sample**: defer to
  Phase 3+; this plan ships only the single-pin tally invariance.

## Out of scope for Phase 3

* Adjoint MRs (`m_adj`) — runner does not wire up adjoint solves.
* Burnup / transient MRs (`m_dyn`) — static eigenvalue only.
* Anderson-accelerated OpenMOC reproducer for the Phase-2 Case 4
  pathology — requires upstream OpenMOC patch, out of MetBench scope.

## Hand-off

Phase 3 should be executed in two PRs:

1. **PR-1**: tally infrastructure + Family A MRs + 1-2 mutations.
   Closes historical-bug Case 3 coverage gap.
2. **PR-2**: temperature plumbing + Family B.1 MRs + 1-2 mutations.
   Closes historical-bug Case 2 coverage gap.

After both: historical-bug coverage rises to **4/4** (all four
sampled bugs would be caught by at least one MR family). At that
point Phase 3 is "done"; further MR families (`m_adj`, `m_dyn`,
multi-pin assembly) are net-new SUT capability rather than coverage
remediation.
