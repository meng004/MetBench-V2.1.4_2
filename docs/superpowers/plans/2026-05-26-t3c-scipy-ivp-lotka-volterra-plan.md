# T3C-IVP — `scipy-ivp-lotka-volterra` Implementation Plan

> **Date**: 2026-05-26
> **Status**: Active scoped implementation plan
> **Driver**: §4 driver #1 — External solver pilot (per `docs/superpowers/specs/2026-05-26-t3-coverage-assessment-and-next-sut-decision.md` §5.2)
> **Successor**: BVP / elliptic external-solver pilot (`scipy-bvp-poisson-1d`) is anticipated as §5.3 backlog and is **not** enabled by this plan.

## 1. Goal

Add a SciPy `solve_ivp`-backed Lotka-Volterra ODE SUT as the first external-library-dependent SUT in the catalog. Validates the External-solver-pilot T1 surface: external Python library install + clean-skip policy + heavier runtime path that none of the pure-stdlib SUTs exercises.

## 2. Equation semantics

Re-uses the existing `lotka-volterra` `EquationMetadata` in `MetBench_BLL.Core/SystemMT/Metadata/SystemMtMetadataCatalog.cs` (no new equation row). Same model:

```
dx/dt = α·x − β·x·y      (prey)
dy/dt = δ·x·y − γ·y      (predator)
```

The classic time-average identity `<prey> = γ/δ` is the same as for the pure-stdlib counterpart. Equation count stays at 12.

## 3. MR semantics

Two new MRs, both reusing existing typed predicates via `LegacyAssertionPredicateMapper` — **no new verification semantics required**.

### 3.1 `scipy-ivp-lv-prey-growth-monotone` (meta-pattern `Mono`)

- Transformation: `ScaleField` on `/params/gamma` with `factor=2`.
- Metric: `mean_prey`.
- Assertion code: `greater` → typed `BinaryComparisonKernel(Greater)`.
- Expectation: by `<prey> = γ/δ`, doubling γ doubles the time-averaged prey population. Strictly `mean_prey(flw) > mean_prey(src)`.
- Mirrors the existing pure-stdlib `lotka-volterra-scale-gamma` MR's semantics; under SciPy `solve_ivp` (default RK45 adaptive) the identity is preserved within solver tolerance, which is far below the strict greater-than comparison.

### 3.2 `scipy-ivp-lv-step-convergence` (meta-pattern `Conv`)

- Transformation: `ScaleField` on `/params/num_eval_points` with `factor=2`.
- Metric: `mean_prey` (same metric reused; doubling the sampling grid refines the trapezoidal average without changing the underlying continuous solution).
- Assertion code: `approx` → typed `ScaledEqualityKernel`.
- Tolerance: `ToleranceRel=1e-3`, `ToleranceAbs=1e-6` (parity with `poisson-mesh-richardson` / `diffusion-mesh-richardson`).
- Expectation: SciPy's adaptive integrator decouples solution accuracy from `num_eval_points` (which only controls the output evaluation grid). Doubling `num_eval_points` leaves `mean_prey` within tolerance because the underlying integration step is adaptive; the trapezoidal average over a doubled grid converges to the same continuous time-average to within tolerance.

## 4. Catalog binding shape

Two `MrBlueprint` rows in `LegacyCatalogFactory.cs` mirroring the existing pure-stdlib LV blueprint. PythonExecutable: `options.EffectiveScipyPython`. `python_executable_kind` in `catalog.json`: `"scipy"`.

Two `MrMetadata` rows in `SystemMtMetadataCatalog.cs` mirroring `lotka-volterra-scale-gamma`. `EquationKey = "lotka-volterra"` (reuse).

## 5. SUT directory layout

```
SUT/scipy_ivp_lotka_volterra/
  catalog.json                                       (manifest)
  scipy_ivp_lotka_volterra.py                        (runner — calls scipy.integrate.solve_ivp)
  scipy_ivp_lotka_volterra_input_parser.py           (parse/write JSON I/O)
  scipy_ivp_lotka_volterra_output_parser.py          ({values, metadata} normalisation)
  sample/standard.json                               (initial conditions + params)
```

## 6. Tests

- `LauncherEndToEndScipyIvpLotkaVolterraTests.cs` — `[SkippableFact]` per MR, gated by `ScipyTestPaths.ScipyImportable()`, skip reason verbatim `"SciPy runtime not configured for scipy-ivp-lotka-volterra."`.
- `SystemMtLauncherTests.cs` — pinned descriptor count `25 → 27`; add ordered ids `scipy-ivp-lv-prey-growth-monotone`, `scipy-ivp-lv-step-convergence`; add two descriptor-metadata facts.
- `CatalogParityTests.cs` — pinned count `25 → 27` (hardcoded vs manifest providers stay in lockstep).
- New helper `MetBench_SystemMT.Tests/SystemMT/ScipyTestPaths.cs` — mirrors `OpenMocTestPaths.cs` with env var `METBENCH_SCIPY_PYTHON`, importability gate via `python -c "import scipy.integrate"` with 10 s timeout.

## 7. CI strategy & skip policy

- `.github/workflows/dotnet-test.yml` runs Ubuntu-24.04 .NET 8 cross-platform tests. SciPy is **not** installed there.
- The launcher end-to-end test class for this SUT calls `Skip.IfNot(ScipyTestPaths.ScipyImportable(), "SciPy runtime not configured for scipy-ivp-lotka-volterra.")` so cloud CI marks the 2 SciPy tests as `Skip`, not `Failed`.
- Parser-shape unit tests do not invoke SciPy and run unconditionally.
- All non-end-to-end tests (descriptor / metadata / catalog-parity) run unconditionally and must pass on cloud CI.

## 8. Scope guard

- No Method MT changes.
- No Typed Semantic Catalog runtime changes (both MRs map through `LegacyAssertionPredicateMapper`).
- No WPF / `MetBench_Client/` edits. `LauncherOptions.ScipyPython` is a new **optional** parameter (defaults to `null` → falls back to `SystemPython`), so the existing `App.xaml.cs` construction remains source-compatible.
- BVP / elliptic external solver pilot (`scipy-bvp-poisson-1d`) NOT implemented here; that is a future plan.

## 9. Inventory delta

- SUT: 13 → 14 (`scipy-ivp-lotka-volterra` added; same equation `lotka-volterra` re-used).
- Equations: 12 → 12 (no new equation).
- MRs: 25 → 27 (`scipy-ivp-lv-prey-growth-monotone`, `scipy-ivp-lv-step-convergence`).

## 10. TDD order

1. Pinned-count + ordered-id failing tests in `SystemMtLauncherTests.cs` + `CatalogParityTests.cs`.
2. Parser contract failing tests (`ScipyIvpLotkaVolterraParserTests.cs` — uses the runner's pure-Python parser scripts, no SciPy needed for parser shape).
3. Launcher end-to-end failing tests (`LauncherEndToEndScipyIvpLotkaVolterraTests.cs` — `[SkippableFact]`, will Skip without SciPy and Pass with SciPy).
4. Minimal SUT runner + parser + sample.
5. `LauncherOptions.ScipyPython` field + `EffectiveScipyPython` property + `PythonExecutableKinds.Scipy` constant + `ManifestMrCatalogProvider` switch case.
6. `MrMetadata` rows in `SystemMtMetadataCatalog.cs`.
7. `MrBlueprint` rows in `LegacyCatalogFactory.cs`.
8. `csproj` `<None Include>` entries for `SUT/scipy_ivp_lotka_volterra/*.py` + `sample/*.json`.
9. Green local test run (full suite).
10. Projection-doc updates (`docs/status/current.md`, `docs/requirements.md`, `docs/PROJECT-STRUCTURE.md`).

## 11. Expiry

This plan expires once T3C-IVP is merged and the inventory delta lands in `docs/status/current.md`. Move this row from §1 to §3 of the active plan index at that point.
