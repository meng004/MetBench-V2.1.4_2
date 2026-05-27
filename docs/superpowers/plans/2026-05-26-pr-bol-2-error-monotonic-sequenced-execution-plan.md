# PR-Bol-2 — Bol-Alg-01 OpenMOC Ray/Track Convergence (Sequenced Execution Plan)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Land a metamorphic relation `openmoc-pincell-ray-track-convergence` (Bol-Alg-01) that exercises the deterministic Richardson-style ray-track convergence law: refining OpenMOC's angular discretization (`num_azim` / `azim_spacing_cm`) must drive `k_eff` monotonically toward a reference value. The MR uses the typed `ErrorMonotonicPredicate` (already shipped at `MetBench_BLL.Core/SystemMT/Catalog/Typed/Specs/PredicateSpec.cs:66`; first kernel test at `MetBench_SystemMT.Tests/SystemMT/Catalog/Typed/ErrorMonotonicKernelTests.cs`).

**Status:** queued — sequenced behind PR #176 ledger refresh.

**Tech Stack:** .NET 8, xUnit (`SkippableFact` via existing `OpenMocTestPaths.OpenMocImportable()` gate). No new Python adapter content (uses existing `openmoc_input_adapter*` family plus possibly one new ray-track adapter). No WPF.

---

## Why this is a sequenced 2-PR plan (not 1 PR)

PR-N2 (Bol-Alg-02 OpenMC variance-ratio) was a single-PR consumer because PR-VR (#168) had **already wired the variance-ratio launcher path before** PR-N2 landed. For Bol-Alg-01 (OpenMOC ErrorMonotonic), the equivalent wiring does **not** exist:

| Surface | variance-ratio (PR-VR / PR-N2) | error-monotonic (Bol-Alg-01) |
|---|---|---|
| Kernel + predicate + validator under `Catalog/Typed/` | ✅ shipped pre-PR-VR | ✅ shipped (`ErrorMonotonicKernel.cs`, `ErrorMonotonicPredicateValidator.cs`) |
| Kernel-level test | ✅ `VarianceRatioKernelTests` | ✅ `ErrorMonotonicKernelTests` (2 facts pinning monotone-pass / non-monotone-fail) |
| Constant in `AssertionTypeCodes.All` | ✅ `VarianceRatio = "variance-ratio"` | ❌ **missing** — no `"error-monotonic"` constant nor `All` entry (`MetBench_BLL.Core/SystemMT/Assertions/AssertionTypeCodes.cs:37–42`) |
| `LegacyAssertionPredicateMapper` arm | ✅ `MapVarianceRatio` (PR #124) | ❌ **missing** |
| `TypedSpecFactory.ForLegacyAssertion` dispatch arm | ✅ PR-VR | ❌ **missing** |
| `TypedVerificationContextFactory.FromScalarOutputs` promotion | ✅ promotes `Statistics` (PR-VR) | ❌ **missing** — must populate `RoleOutput.Metrics` for 3 distinct roles (OrderedRoles[≥2] + ReferenceRole), not 2 |
| Launcher pipeline runs N SUT calls | ⚠️ N=2 (source + followup) | ❌ requires N≥3 to populate 3 distinct roles unless we synthesize a reference value |
| Manifest catalog.json schema | ✅ flat `transform_steps` | ❌ no schema for an ordered refinement sequence |
| First catalog consumer | ✅ PR-N2 (`openmc-pincell-particle-count-convergence`) | — Bol-Alg-01 will be the first |

**Therefore PR-Bol-2 splits into:**

- **PR-Bol-2A** — Launcher / pipeline wiring for `error-monotonic`. No new MR catalog row. Mirrors the PR-VR pattern in scope and risk profile.
- **PR-Bol-2B** — First MR catalog consumer (`openmoc-pincell-ray-track-convergence`), pinned counts, end-to-end SkippableFact, status ledger update. Mirrors the PR-N2 pattern.

Cross-cutting constraints (apply to both PRs):

- No new Python `SUT/` directory, no new SUT runner.
- No WPF, no `App.xaml.cs`, no Windows-only file.
- The `SemanticCatalogBoundaryTests` architecture guard must remain green — string-code dispatch stays confined to `Catalog/Typed/Migration/` + `Assertions/` + `MrBindingDefinition.cs` + `Catalog/Binding/` (see `MetBench_SystemMT.Tests/Architecture/SemanticCatalogBoundaryTests.cs`).

---

## PR-Bol-2A — Launcher Pipeline Wiring for `error-monotonic`

### Scope and Non-Goals

This PR is **wiring-only**. No new MR catalog row. Cloud-CI safe. After this PR, an `MrBlueprint` with `AssertionTypeCode: "error-monotonic"` and the appropriate role-output shape will assert correctly against `ErrorMonotonicPredicate` via the typed dispatcher.

Must **not**:

- Add any `MrBlueprint` row, `MrMetadata` row, or `SUT/<sut>/catalog.json` MR entry.
- Touch the OpenMOC SUT runner or any Python script.
- Modify `VarianceRatioKernel`, `VarianceRatioPredicate`, or any code added by PR-VR (#168).
- Add a new C# `Transformation` type.

May (and will):

- Add `AssertionTypeCodes.ErrorMonotonic = "error-monotonic"` and append to the `All` array.
- Add `TypedSpecFactory.ForErrorMonotonic(...)` factory method in `Catalog/Typed/Migration/`.
- Add an `error-monotonic` dispatch arm to `TypedSpecFactory.ForLegacyAssertion(...)` (already the single migration-side dispatcher per PR-VR).
- Extend `TypedVerificationContextFactory.FromScalarOutputs(...)` to handle the **3-role shape** (OrderedRoles + ReferenceRole) when the synthesized spec carries an `ErrorMonotonicPredicate`. Non-error-monotonic specs unaffected (additive).
- Extend the manifest catalog.json schema additively to carry the ordered refinement sequence (see Open Q3 below).
- Possibly extend `SystemMtPipeline` / `SystemMtLauncher` to support N-step parameter sweeps for a single MR (see Open Q2 below). **Key design decision deferred to scoped PR-Bol-2A plan.**

### Open Questions to Resolve Before PR-Bol-2A Coding Starts

These need a scoped plan PR of their own (analogous to PR #167 PR-VR plan) before code lands. Surface them in the scoped plan; choose one direction per question.

**Q1 — Reference-value source.** Two viable strategies:

- **(A) Run the SUT a 3rd time at very fine settings** (e.g. `num_azim=64, azim_spacing_cm=0.0125`) to produce a numerical reference. Pro: physically meaningful, no human input. Con: 3× wall-clock; requires multi-step pipeline (Q2).
- **(B) Carry the reference as a spec parameter** (e.g. `"reference_k_eff": "1.43250"` from a textbook). Pro: 2-step pipeline still works. Con: brittle to spec authoring errors; not auto-validating.

Recommendation: **(A)** — physical robustness outweighs the runtime cost; PR-N2's 3-call lineage (2 OpenMC runs at different particle counts) demonstrates the cost is acceptable.

**Q2 — How the pipeline runs N SUT calls.** Three options:

- **(2a) New `SystemMtMultiPhasePipeline` pipeline alongside the existing 2-side pipeline.** Pro: clean separation. Con: maintenance overhead; the existing 30 MRs continue to route through 2-side path.
- **(2b) Extend `SystemMtPipeline.ExecuteAsync` to optionally take a `phases: IReadOnlyList<MrPhase>` argument.** When `phases.Count == 2` the existing source/followup behaviour is preserved byte-identically. When `phases.Count > 2` the runner loops, captures one `RoleOutput` per phase, and hands the 3-role typed context to the dispatcher.
- **(2c) Synthesize the 3rd output in-process** by re-running the same parameter-overridden SUT call via the launcher API. Pro: minimal pipeline change. Con: bypasses the typed pipeline's evidence-capture invariants; harder to reason about.

Recommendation: **(2b)** — additive change, preserves the 2-side test pinning (`Assert.Equal(31, descriptors.Count)` etc.), and isolates the multi-phase complexity to one method.

**Q3 — Manifest catalog.json schema for ordered refinement.** Current schema:

```json
{
  "transform_steps": [ { "transformation_name": "...", "target_field_path": "..." } ],
  "default_parameters": { "factor": "..." }
}
```

Extension needed for ErrorMonotonic:

- **(3a) New top-level array `refinement_phases`** carrying ordered `{role, factor}` tuples; `transform_steps` reused unchanged as the per-phase mutation template. Old MRs ignore the new field (additive).
- **(3b) New top-level field `assertion_phase_mapping`** that maps phase indices to `OrderedRoles` + `ReferenceRole`. More flexible but more complex.

Recommendation: **(3a)** — minimal schema delta; 1:1 correspondence with the new `phases` argument from Q2.

**Q4 — Norm kind for the kernel.** `ErrorMonotonicPredicate.NormKind` choices: `Absolute / Relative / L2 / Linf`. For scalar `k_eff` convergence the natural choice is `Relative`. Hard-code in `ForErrorMonotonic` until a second consumer needs configurability.

### Implementation Tasks (PR-Bol-2A)

Detail TBD in the scoped plan PR (working name `2026-MM-DD-error-monotonic-launcher-pipeline-wiring-plan.md`). High-level shape:

- [ ] Add `AssertionTypeCodes.ErrorMonotonic` constant + `All` entry.
- [ ] Add `TypedSpecFactory.ForErrorMonotonic(mrCode, metric, orderedRoles, referenceRole, normKind)`. Validates blank inputs, role-set hygiene (≥ 2 ordered, no duplicate with reference). Returns `MrSpec`.
- [ ] Add `error-monotonic` arm to `TypedSpecFactory.ForLegacyAssertion(...)` (consumes a `phases` parameter set or equivalent).
- [ ] Extend `TypedVerificationContextFactory.FromScalarOutputs(...)` to accept multiple role scalar maps when the spec carries `ErrorMonotonicPredicate` (additive; `null` for non-error-monotonic specs).
- [ ] Extend `SystemMtPipeline.ExecuteAsync` per Q2 recommendation. Add a `phases` arg or context method; preserve all 2-side semantics byte-identically when `phases.Count == 2`.
- [ ] Extend `MrBindingDefinition` / `MrCatalogEntry` + `ManifestMrCatalogProvider` per Q3 recommendation.
- [ ] Extend `LegacyCatalogFactory` `MrBlueprint` record with optional `RefinementPhases` (default `null`).
- [ ] Test surface (target ≥ 30 facts mirroring PR-VR's 44):
  - `TypedSpecFactoryErrorMonotonicTests` (≥ 10 facts: role validation, norm kind, factory output shape, validator pass-through, fail-closed inputs).
  - `TypedVerificationContextFactoryErrorMonotonicTests` (≥ 8 facts: 3-role promotion happy path, missing-metric handling, non-error-monotonic spec untouched, kernel dispatch Pass/Fail).
  - `ErrorMonotonicPipelineWiringTests` (≥ 8 facts: `ForLegacyAssertion` dispatch, phases parameter handling, invalid inputs).
  - `MultiPhasePipelineTests` (≥ 4 facts: 2-phase byte-identity with current behaviour, 3-phase happy path, partial-phase failure).

### Acceptance Criteria (PR-Bol-2A)

- [ ] `git grep -nE 'AssertionTypeCode:\s*"error-monotonic"' MetBench_BLL.Core/` returns zero (no MR consumer yet; only constants + factory).
- [ ] `AssertionTypeCodes.All` contains `"error-monotonic"`.
- [ ] All 30 pre-existing MRs continue to round-trip through the launcher unchanged (descriptor count `31` pinned-count assertions still pass).
- [ ] `SemanticCatalogBoundaryTests` 3 facts remain green (string dispatch confined to allowed directories).
- [ ] CI `test` baseline expected `~1349 / 0 / 14` (1319 prior + ~30 new always-passing facts; no new skips on CI).

---

## PR-Bol-2B — `openmoc-pincell-ray-track-convergence` MR (catalog consumer)

### Scope and Non-Goals

After PR-Bol-2A merges, this PR adds the first error-monotonic MR. Mirrors PR-N2 in shape and size. Cloud-CI safe.

Must **not**:

- Touch `Catalog/Typed/`, `Pipeline/`, or `Catalog/Typed/Migration/` (all wired by PR-Bol-2A).
- Add a new C# `Transformation` type — `ScaleField` already handles integer `/tracking/num_azim` multiplication.
- Touch the WPF client or `App.xaml.cs`.

May (and will):

- Add **exactly one** `MrBlueprint` row in `LegacyCatalogFactory.cs`.
- Add **exactly one** `MrMetadata` row in `SystemMtMetadataCatalog.cs`.
- Add **exactly one** new MR entry in `SUT/openmoc/catalog.json` (with the new `refinement_phases` field).
- Add **one** new Python adapter under `SUT/openmoc/` that scales `/tracking/num_azim` (similar to `openmoc_input_adapter_*` siblings) **or** reuse `ScaleField` C# transform if it suffices end-to-end. To verify at code time.
- Bump pinned counts 31 → 32 across the 6 test files + 1 production comment.
- Add **one** new end-to-end `SkippableFact` in `LauncherEndToEndOpenMocRayTrackConvergenceTests` gated on `OpenMocTestPaths.OpenMocImportable()`.
- Update `docs/status/current.md` §2 baseline + §3 PR-Bol-2 row.
- Update `docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md` to mark this plan Completed.

### Open Questions to Resolve Before PR-Bol-2B Coding Starts

**Q5 — Default refinement schedule.** Three phases minimum (Q1 recommendation A + Q2 recommendation 2b). Proposed schedule:

| Phase role | num_azim | azim_spacing_cm | Wall-clock (est.) |
|---|---|---|---|
| `coarse` (baseline) | 16 | 0.05 | ~10 s |
| `medium` | 32 | 0.025 | ~20 s |
| `reference` | 64 | 0.0125 | ~60 s |

Total per MR run: ~90 s; well within the 5 min OpenMOC timeout.

**Q6 — Will plateau hit on `medium` already?** OpenMOC pincell with sample materials typically converges to ~4 decimal places at `num_azim=32, azim_spacing=0.025`. If `|k_eff(medium) - k_eff(reference)| < |k_eff(coarse) - k_eff(reference)|` does not hold by ≥ a meaningful margin, the SkippableFact will be flaky. Mitigation: pick a slightly coarser baseline (`num_azim=8`) to ensure the error chain is monotonically clear.

**Q7 — Adapter scope.** Does `ScaleField` on `/tracking/num_azim` work end-to-end? Concerns:

- The OpenMOC runner reads `num_azim` as `int`; `ScaleField` produces a `double` (5000 × 4.0 = 20000.0 in PR-N2 worked because Python `int(...)` cast accepts floats). Need to verify OpenMOC's runner cast for `num_azim` accepts floats. Likely yes (line 146 `int(t["num_azim"])`).
- `azim_spacing_cm` needs to be **divided** by the same factor (not multiplied). `ScaleField` only multiplies. Options: (a) compose two `MrTransformStep`s — one `ScaleField` for `num_azim` (factor=2), one custom `InverseScaleField` for `azim_spacing_cm` (factor=2, but inverse). (b) Use a dedicated Python adapter `openmoc_input_adapter_refine_ray_tracks.py` that handles both fields in one shot.

Recommendation: **(b)** — mirror PR-N2's `openmc_input_adapter_refine_particles.py` precedent. Adds one Python file; keeps the catalog row simple.

### Implementation Tasks (PR-Bol-2B)

- [ ] Add `SUT/openmoc/openmoc_input_adapter_refine_ray_tracks.py` (scales `num_azim` × factor; divides `azim_spacing_cm` / factor).
- [ ] Add `openmoc-pincell-ray-track-convergence` row to `LegacyCatalogFactory.cs` (AssertionTypeCode `"error-monotonic"`, refinement_phases per Q5, MrFamily `NeutronTransport.Convergence.RayTracks`).
- [ ] Add corresponding `MrMetadata` row to `SystemMtMetadataCatalog.cs` (`ComparisonType.Relative`, parameters Symbol={factor, k_eff}).
- [ ] Add manifest entry in `SUT/openmoc/catalog.json` with the new `refinement_phases` field.
- [ ] Bump 31 → 32 across the 6 pinned-count test files + 1 production comment (HardcodedMrCatalogProvider.cs).
- [ ] Update `OpenMocCatalogParityTests` to accept the new 3rd MR (mirror what PR-N2 did for `OpenMcCatalogParityTests`).
- [ ] Update `SystemMtLauncherTests.ListAvailableAsync_returns_known_scenarios_in_id_order` positional pin to insert at the correct alphabetical slot.
- [ ] Update `LauncherCatalogV2ImporterTests.Import_writes_one_audit_log_entry_with_counts` string-literal pins (mrsCreated:31 → 32, bindingsCreated:31 → 32).
- [ ] Add `LauncherEndToEndOpenMocRayTrackConvergenceTests` — 2 SkippableFacts (default-phase pass; sanity guard that `|k_eff(coarse) - k_eff(reference)| > |k_eff(medium) - k_eff(reference)|`).
- [ ] Update `docs/status/current.md` §2 baseline + §3 PR-Bol-2 row + inventory (31 → 32).
- [ ] Update active plan index: this plan Completed.

### Acceptance Criteria (PR-Bol-2B)

- [ ] `git grep -nE 'AssertionTypeCode:\s*"error-monotonic"' MetBench_BLL.Core/` returns exactly one match.
- [ ] `git grep -nE 'openmoc-pincell-ray-track-convergence' MetBench_BLL.Core/ MetBench_SystemMT.Tests/ SUT/` returns matches in 4 files (blueprint + metadata + manifest + launcher test).
- [ ] `Assert.Equal(31,` fully replaced by `Assert.Equal(32,`.
- [ ] `dotnet test --filter "FullyQualifiedName~OpenMocRayTrackConvergence"` skips cleanly without `METBENCH_OPENMOC_PYTHON`.
- [ ] CI `test` baseline expected `~1351 / 0 / 15` (1349 post-PR-Bol-2A + 2 new SkippableFacts).
- [ ] `docs/status/current.md` baseline + PR-Bol-2 row + inventory updated.

---

## Cross-PR Coordination

- PR-Bol-2A must merge **before** PR-Bol-2B begins. If PR-Bol-2A surfaces design constraints that contradict this orchestration plan (e.g. the multi-phase pipeline costs more than expected), retract this plan and draft a successor — same protocol as PR-N2's retraction → PR-VR insertion → PR-N2 successor lineage.
- Both PRs must update `docs/status/current.md` and the active plan index in scope (not a separate ledger PR), per the lesson learned from PR-N2's 5-PR sequence.
- The MR inventory progression is `31 → 31 → 32` (Bol-Alg-02 → no change in PR-Bol-2A → +1 in PR-Bol-2B). Real-physics inventory: `30 → 30 → 31`.

---

## Out of Scope (deliberate deferrals)

- **OpenMC ray-track equivalent** — OpenMC's Monte-Carlo solver has no deterministic ray-tracking knob; not applicable.
- **Reactor-anchor depth expansion (multi-pin, lattice, full-core)** — separate PR with its own scoped plan; not blocked by this work.
- **Adaptive refinement** (`automatic phase escalation until plateau`) — would require pipeline changes well beyond Q2's scope; defer.
- **Multi-phase pipeline for variance-ratio** (run OpenMC at 3 particle counts instead of 2) — possible follow-up; not required for Bol-Alg-02 which is happy with 2 phases.

---

## Notes for Future Successors

- If the multi-phase pipeline design in PR-Bol-2A converges on `phases: IReadOnlyList<MrPhase>` as a launcher-facade contract change, the new contract should be documented in `CLAUDE.md` §6 (System-MT facade rules) at PR-Bol-2A merge time.
- The `refinement_phases` schema in catalog.json should be documented in the spec at `docs/superpowers/specs/2026-05-25-mr-verification-v1.2-codex-ready.md` if it lands as a public-facing schema field.
- If a future MR needs **non-monotonic** ordered-sequence shapes (e.g. growth-decay-growth), use `OrderedSequenceShapePredicate` instead of `ErrorMonotonicPredicate`. They share the multi-phase wiring infra from PR-Bol-2A.
