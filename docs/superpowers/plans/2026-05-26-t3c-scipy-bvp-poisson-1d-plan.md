# T3C-BVP — `scipy-bvp-poisson-1d` Implementation Plan

> **Date**: 2026-05-26
> **Status**: Active scoped implementation plan
> **Driver**: §4 driver #1 — External solver pilot (per `docs/superpowers/specs/2026-05-26-t3-coverage-assessment-and-next-sut-decision.md` §5.3)
> **Predecessor**: §5.2 IVP candidate `scipy-ivp-lotka-volterra` (merged via T3C-IVP).

## 1. Goal

Add a SciPy `solve_bvp`-backed 1D Poisson elliptic SUT as the second external-library-dependent SUT in the catalog. Validates that the External-solver-pilot path opened in §5.2 extends to **BVP / elliptic** problems and to a sparse / dense linear-system internal call shape that the IVP RK45 surface did not exercise.

## 2. Equation semantics

Re-uses the existing `poisson` `EquationMetadata` in `MetBench_BLL.Core/SystemMT/Metadata/SystemMtMetadataCatalog.cs` (no new equation row). Same model:

```
-u''(x) = f,   x in [0, L],   u(0) = u(L) = 0
```

The analytic solution for constant `f` is `u(x) = f * x * (L - x) / 2`; peak amplitude `u_max = f * L² / 8`. Equation count stays at 12.

The 1st-order system reformulation `solve_bvp` consumes:

```
y0 = u
y1 = u'
y0' = y1
y1' = -f
```

with Dirichlet BC `y0(0) = 0`, `y0(L) = 0`.

## 3. MR semantics

Two new MRs, both reusing existing typed predicates via `LegacyAssertionPredicateMapper` — **no new verification semantics required**.

### 3.1 `scipy-bvp-poisson-source-superposition` (meta-pattern `Mono`)

- Transformation: `ScaleField` on `/source/strength` with `factor=2`.
- Metric: `u_max`.
- Assertion code: `greater` → typed `BinaryComparisonKernel(Greater)`.
- Expectation: Poisson `-u'' = f` is linear in `f`. Doubling `f` doubles `u_max` exactly. Strictly `u_max(flw) > u_max(src)`.
- Mirrors `poisson-source-superposition` (pure-stdlib) semantics under a different solver.

### 3.2 `scipy-bvp-poisson-mesh-richardson` (meta-pattern `Conv`)

- Transformation: `ScaleField` on `/geometry/num_points` with `factor=2`.
- Metric: `u_max`.
- Assertion code: `approx` → typed `ScaledEqualityKernel`.
- Tolerance: `ToleranceRel=1e-3`, `ToleranceAbs=1e-6` (parity with `poisson-mesh-richardson` / `diffusion-mesh-richardson`).
- Expectation: SciPy `solve_bvp` is adaptive on its own mesh and refines until residual is below tolerance. The initial mesh passed in (built from `num_points`) is the user's seed; the solver's adaptive mesh refinement converges to the same continuous solution regardless of seed mesh resolution. Doubling `num_points` leaves `u_max` within tolerance because the BVP solver converges to the same `f·L²/8` plateau.

## 4. Catalog binding shape

Two `MrBlueprint` rows in `LegacyCatalogFactory.cs` mirroring the existing pure-stdlib Poisson blueprints. `PythonExecutable: options.EffectiveScipyPython` (re-uses field added in T3C-IVP). `python_executable_kind: "scipy"` in `catalog.json`.

Two `MrMetadata` rows in `SystemMtMetadataCatalog.cs` mirroring `poisson-source-superposition` / `poisson-mesh-richardson`. `EquationKey = "poisson"` (reuse).

## 5. SUT directory layout

```
SUT/scipy_bvp_poisson_1d/
  catalog.json                                       (manifest)
  scipy_bvp_poisson_1d.py                            (runner — calls scipy.integrate.solve_bvp)
  scipy_bvp_poisson_1d_input_parser.py               (parse/write JSON I/O)
  scipy_bvp_poisson_1d_output_parser.py              ({values, metadata} normalisation)
  sample/standard.json                               (geometry + source)
```

## 6. Tests

- `LauncherEndToEndScipyBvpPoissonTests.cs` — `[SkippableFact]` per MR, gated by `ScipyTestPaths.ScipyImportable()`, skip reason verbatim `"SciPy runtime not configured for scipy-bvp-poisson-1d."`.
- `SystemMtLauncherTests.cs` — pinned descriptor count `27 → 29`; add ordered ids `scipy-bvp-poisson-mesh-richardson`, `scipy-bvp-poisson-source-superposition`; add two descriptor-metadata facts.
- `CatalogParityTests.cs` — pinned count `27 → 29`.
- `HardcodedMrCatalogProviderTests.cs` — pinned count `27 → 29`, SUT count `14 → 15`.
- `SystemMtBootstrapTests.cs` + `LauncherCatalogV2ImporterTests.cs` + `SystemMtLauncherProviderInjectionTests.cs` — pinned-count bumps.
- New `ScipyBvpPoissonParserTests.cs` — parser-shape contract (3 unconditional facts).
- `ScipyTestPaths.cs` is **reused** as-is from T3C-IVP (importability gate is the same).

## 7. CI strategy & skip policy

- Identical to T3C-IVP: SciPy not installed on cloud CI; `[SkippableFact]` + `Skip.IfNot(ScipyTestPaths.ScipyImportable(), "SciPy runtime not configured for scipy-bvp-poisson-1d.")` so the 2 BVP end-to-end facts skip cleanly.
- Parser-shape tests run unconditionally.
- Catalog / descriptor / pinned-count tests run unconditionally.

## 8. Scope guard

- No Method MT changes.
- No Typed Semantic Catalog runtime changes (both MRs map through `LegacyAssertionPredicateMapper`).
- No WPF / `MetBench_Client/` edits. `LauncherOptions.ScipyPython` already exists from T3C-IVP — no further `LauncherOptions` change in this PR.
- No `PythonExecutableKinds` change (the `"scipy"` constant already exists).
- No `ManifestMrCatalogProvider` change (the switch arm already exists).
- T3C-IVP code is not modified.

## 9. Inventory delta

- SUT: 14 → 15 (`scipy-bvp-poisson-1d` added; same equation `poisson` re-used).
- Equations: 12 → 12 (no new equation).
- MRs: 27 → 29 (`scipy-bvp-poisson-source-superposition`, `scipy-bvp-poisson-mesh-richardson`).

## 10. TDD order

1. Pinned-count + ordered-id failing tests in `SystemMtLauncherTests.cs` + `CatalogParityTests.cs` + 4 other pinned files.
2. Parser contract failing tests (`ScipyBvpPoissonParserTests.cs`).
3. Launcher end-to-end failing tests (`LauncherEndToEndScipyBvpPoissonTests.cs`).
4. Minimal SUT runner + parser + sample.
5. `MrMetadata` rows in `SystemMtMetadataCatalog.cs`.
6. `MrBlueprint` rows in `LegacyCatalogFactory.cs`.
7. `csproj` `<None Include>` entries for `SUT/scipy_bvp_poisson_1d/*.py` + `sample/*.json`.
8. Green local test run (full suite).
9. Projection-doc updates (`docs/status/current.md`, `docs/requirements.md`, `docs/PROJECT-STRUCTURE.md`).

## 11. Expiry

This plan expires once T3C-BVP is merged and the inventory delta lands in `docs/status/current.md`. Move this row from active to §3 of the active plan index at that point.
