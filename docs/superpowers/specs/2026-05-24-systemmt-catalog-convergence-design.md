# SystemMT Catalog Convergence Design

> **Date**: 2026-05-24
> **Status**: proposed (v3 — applied 5 review amendments + class-name fix)
> **Scope**: System-MT execution catalog, Stage 8 metadata convergence, execution evidence expansion
> **Related**: [`CLAUDE.md`](../../../CLAUDE.md) · [`AGENTS.md`](../../../AGENTS.md) · [`docs/superpowers/plans/2026-05-21-next-stage-development-plan.md`](../plans/2026-05-21-next-stage-development-plan.md) · [`docs/superpowers/plans/2026-05-22-mr-program-metadata-persistence-plan.md`](../plans/2026-05-22-mr-program-metadata-persistence-plan.md) · [`docs/superpowers/plans/2026-05-24-systemmt-catalog-convergence-plan.md`](../plans/2026-05-24-systemmt-catalog-convergence-plan.md)

---

## Revision History

- **v1** (initial draft): 11-section spec; missed Tolerance/sunset/manifest-format/V3-linkage.
- **v2** (review amendments): added §5.1 tolerance fields, §5.2 transition policy, §5.3.1 manifest format/location, §5.4 canonical identity linkage, §8 Risk 5, §7.3 parity guard coverage; replaced hard-coded numbers with branch-agnostic wording.
- **v3** (this revision): renamed `SystemMtMrLauncher` → `SystemMtLauncher` throughout (PR #58 W12 rename); minor §10 acceptance addendum.

---

## 1. Problem Statement

MetBench is transitioning from a small hard-coded System-MT benchmark into a
larger Stage 8 MR/SUT asset platform. That transition is blocked by a structural
split:

1. The production execution entry point still defines runnable MR/SUT bindings
   inside `SystemMtLauncher.BuildBlueprints()`.
2. The v2 domain model and Stage 8 plans are already evolving toward a richer,
   queryable MR/SUT metadata model and 5D schema.
3. Execution persistence still stores a summary projection, but Stage 8 needs
   richer evidence for replay, reporting, defect archival, and auditability.

If this split continues, Stage 8 work will accumulate across multiple sources of
truth: launcher code, v2 schema, docs, tests, and future metadata tables.

This design converges those sources without replacing the existing launcher
façade or destabilizing the current test surface.

---

## 2. Goals

### 2.1 Primary Goal

Make System-MT runnable catalog definitions data-driven while preserving the
current launcher façade and behavior.

### 2.2 Secondary Goals

- Establish one authoritative source for runnable MR/SUT definitions.
- Prepare the execution path to absorb Stage 8 metadata:
  `Equation / ProgramType / MetaPattern / SourceLevel / FailureCorrelation`.
- Expand execution evidence so replay, reporting, and defect archival can rely
  on persisted run-time facts rather than only summary metrics.
- Keep the migration incremental and regression-safe.

### 2.3 Non-Goals

- No rewrite of `ISystemMtLauncher`.
- No replacement of the current WPF navigation or page model.
- No immediate implementation of the Stage 8 meta-prompt engine itself.
- No attempt to redesign all v2 domain entities in this phase.

---

## 3. Current-State Findings

### 3.1 Hard-coded execution catalog is the live runtime truth

`MetBench_BLL.Core/SystemMT/Launcher/SystemMtLauncher.cs` owns the executable
MR registry today. The launcher constructs and runs `MrBlueprint` instances from
hard-coded definitions. This is acceptable for a small catalog, but it does not
scale to Stage 8.

### 3.2 Domain schema already points toward assetization

The v2 domain model already expresses a richer data model:

- `MetaPattern`
- `MRBinding`
- `MRInstance`
- `Execution`
- `MetamorphicRelationV3` + 7 5D-tag enums (landed via PR #88)

These entities are moving toward durable MR/SUT assets and execution tracking,
but the launcher currently bypasses them for runtime catalog definition.

### 3.3 Stage 8 requires richer metadata than the launcher can currently host cleanly

The Stage 8 plans require:

- 5D metadata
- program-level metadata
- MR-level metadata
- richer execution evidence

Trying to add all of that directly into hard-coded launcher blueprints would
deepen coupling and create drift against schema and plans.

### 3.4 Execution evidence is still summary-oriented

`SystemMtResultRecord` stores list/report-friendly summary fields, but not the
sample-level input/transform/output evidence required by later plans.
`InputSamples` field exists on the record but is never populated by the pipeline
(PR #88 backlog #3).

---

## 4. Design Principles

### 4.1 Preserve stable boundaries

The public launcher façade remains the stable runtime boundary:

- `ListAvailableAsync`
- `RunAsync`
- `RunBatchAsync`

Consumers should not need to know whether catalog definitions came from code,
manifest files, or persistent metadata.

### 4.2 Separate definition from execution

Static executable definitions belong in a catalog source. Execution belongs in
the launcher and runner pipeline. The launcher should consume definitions, not
author them.

### 4.3 Separate summary from evidence

List/report-oriented result records and audit/replay-oriented execution evidence
serve different access patterns. They should not be forced into a single bloated
record unless that remains clearly maintainable.

### 4.4 Migrate by equivalence first, then extend

The first migration step is not to add new research capability. It is to make
the existing catalog data-driven with behavior preserved. Only after equivalence
is locked should Stage 8 metadata and richer evidence be layered in.

---

## 5. Proposed Architecture

## 5.1 Catalog Definition Layer

Introduce a catalog definition model that represents runnable MR/SUT bindings as
data.

Suggested responsibilities:

- `ProgramDefinition`
  - SUT identity
  - python/runtime executable selection
  - runner script path
  - output adapter path
  - equation/program-type metadata

- `MrDefinition`
  - MR id
  - display name
  - meta-pattern metadata
  - assertion type
  - output value name
  - descriptive metadata

- `MrBindingDefinition`
  - program + MR binding
  - input adapter path
  - sample case path
  - default parameters
  - tolerance configuration
  - noise-related comparison configuration
  - timeout
  - Stage 8 5D metadata

This layer becomes the authoritative source for static runnable definitions.

Recommended minimum tolerance/noise fields on `MrBindingDefinition`:

- `tolerance_rel` (double, default `0`)
- `tolerance_abs` (double, default `0`)
- `noise_aware` (bool, default `false`)
- `noise_multiplier` (double, default `3.0`)

Reason:

Approximate assertions must not silently fall back to bit-exact equality when a
binding is intended to be tolerance-bearing. Tolerance belongs to the binding
definition, not to ad hoc execution defaults.

### 5.2 Catalog Provider Layer

Introduce an abstraction between the launcher and the source of runnable
definitions:

- `IMrCatalogProvider`
- `ManifestMrCatalogProvider`
- `HardcodedMrCatalogProvider` as a transition adapter

Responsibilities:

- load definitions
- validate required fields
- map definitions into runtime launcher models
- surface clear load-time errors when definitions are invalid

`SystemMtLauncher` will depend on a provider, not on `BuildBlueprints()`.

Transition policy:

- `HardcodedMrCatalogProvider` exists only to preserve migration safety.
- Once manifest-backed loading reaches behavioral parity for the current branch,
  `HardcodedMrCatalogProvider` should be marked `[Obsolete]`.
- A parity test must remain in place while both providers coexist.
- The hard-coded provider must be removed before Stage 9 work begins; it is not
  allowed to become a permanent parallel authoring path.

### 5.3 Runtime Mapping Layer

Keep runtime execution models focused and executable.

Two acceptable implementation options:

1. Keep `MrBlueprint` as the launcher runtime model and map manifest definitions
   into it.
2. Rename/refactor `MrBlueprint` into a more neutral runtime record after the
   provider migration is stable.

Recommendation:

Use option 1 first. Preserve `MrBlueprint` during migration to minimize surface
area, then revisit naming and shape later if needed.

### 5.3.1 Manifest Format and Location

To keep authoring simple and colocated with SUT assets, use:

- **Format**: JSON
- **Location**: `SUT/<sut_name>/catalog.json`
- **Validation artifact**: `docs/design/mr-catalog-manifest.schema.json`

Reasons:

- JSON aligns with current repository conventions and `System.Text.Json`
  consumption.
- Per-SUT colocated catalogs keep runnable definitions near runners, adapters,
  and sample assets.
- A schema file gives IDE and CI/pre-commit validation a stable contract.

### 5.4 Execution Evidence Layer

Split execution persistence into:

- summary projection for list/report screens
- evidence projection for replay/audit/defect archival

Recommended structure:

- keep `SystemMtResultRecord` as the summary-facing record
- add dedicated evidence models such as:
  - `ExecutionEvidence`
  - `ExecutionSampleTrace`
  - `ExecutionMetadataSnapshot`

Minimum fields needed for Stage 8 alignment:

- program metadata snapshot
- MR metadata snapshot
- sample-level input values
- transformed input values
- output observations
- transformation parameters
- version linkage to catalog/SUT/MetBench runtime

Canonical identity linkage:

- The evidence layer must reference a stable metadata identity from the
  authoritative MR/SUT definition model.
- If a richer V3 MR schema is introduced or lands on a later branch, Phase D
  should bind evidence snapshots to that canonical identity rather than invent a
  second execution-only identifier.
- In the current branch, `MetamorphicRelationV3` has already landed via PR #88
  (entity + IDAL + DAL + V2→V3 migration), but is not yet wired into the
  execution path. Phase D should treat V3 wiring as part of the same delivery
  slice; the implementation plan records this branch-state-aware constraint.

### 5.5 Persistent Metadata Convergence

The persistent model should not become a second unrelated catalog authoring
system in this phase.

Recommendation:

- manifest/catalog files are the authoring source for runnable definitions
- persistence stores normalized metadata snapshots and run evidence
- later phases may add synchronization/import flows, but not dual-authoring

This avoids a situation where the same runnable MR exists in both code and DB
with independent edits.

---

## 6. Migration Strategy

### Phase A: Define the provider boundary

Add the catalog provider abstraction and definition models without changing
launcher behavior.

Outcome:

- new interfaces and models exist
- launcher can still use hard-coded catalog through a provider implementation

### Phase B: Externalize the current runnable MR bindings

Represent the current launcher catalog as manifest data.

Outcome:

- all currently runnable MR/SUT bindings in the current branch exist in the new
  definition format
- no behavior change yet

### Phase C: Switch launcher to provider-backed catalog loading

Replace direct `BuildBlueprints()` ownership with provider-based loading.

Outcome:

- `SystemMtLauncher` still behaves the same from the outside
- runtime definitions now come from provider-backed data

### Phase D: Add evidence-bearing persistence

Extend persistence to store richer metadata and sample-level execution evidence.

Outcome:

- replay/reporting/defect archival can rely on persisted evidence
- Stage 8 metadata has a place in the execution path

### Phase E: Remove obsolete hard-coded catalog ownership

Once provider-backed loading and regression coverage are stable, remove direct
hard-coded ownership from the launcher.

Outcome:

- one runtime catalog truth remains
- Stage 8 work has a clean extension point

---

## 7. Testing Strategy

### 7.1 Catalog Equivalence Tests

Purpose: prove the provider-backed catalog is behaviorally identical to the
current hard-coded catalog for the runnable MRs present on the current branch.

Coverage:

- same MR ids
- same display names
- same assertions
- same default parameters
- same sample paths
- same timeout/runtime paths

### 7.2 Provider Contract Tests

Purpose: ensure manifest/provider failures are deterministic and diagnosable.

Coverage:

- missing required fields
- invalid file paths
- unknown assertion names
- duplicate MR ids
- invalid metadata values

### 7.3 Launcher Regression Tests

Purpose: ensure existing execution behavior does not change during migration.

Coverage:

- `ListAvailableAsync` output parity
- `RunAsync` success path
- `RunAsync` failure path
- anomaly auto-recording
- batch pre-validation and partial-failure behavior
- hard-coded and manifest providers yield identical launcher-visible catalog
  entries while both providers coexist

### 7.4 Persistence Roundtrip Tests

Purpose: protect summary/evidence compatibility as richer fields are added.

Coverage:

- summary roundtrip still works
- evidence roundtrip works
- backward-compatible reads for existing records
- replay/reporting consumers continue to function

### 7.5 Documentation and Baseline Validation

Purpose: avoid drift between code, plans, and structure docs.

Coverage:

- update `PROJECT-STRUCTURE.md`
- update top-level state docs affected by the migration
- refresh test baseline artifacts when counts or runnable MRs change

---

## 8. Risks and Mitigations

### Risk 1: Provider migration breaks current launcher behavior

Mitigation:

- preserve façade
- use parity tests before switching default provider
- migrate current branch bindings first, with no semantic changes in the same
  batch

### Risk 2: Manifest format becomes too weak for Stage 8 metadata

Mitigation:

- include 5D fields in the initial definition shape even if some remain empty or
  provisional for current bindings
- validate schema early

### Risk 3: Evidence persistence bloats summary records

Mitigation:

- separate summary and evidence models
- keep list/report queries on the summary projection

### Risk 4: Dual authoring reappears through DB sync

Mitigation:

- explicitly define authoring source vs persistence sink
- do not introduce DB-first editing of runnable definitions in this phase

### Risk 5: Transition adapter becomes permanent architecture debt

Mitigation:

- mark the hard-coded provider `[Obsolete]` once manifest parity is achieved
- keep a parity guard test while both providers coexist
- define provider removal as an explicit completion gate before Stage 9

---

## 9. Recommended Implementation Order

Recommended first delivery slice:

1. `IMrCatalogProvider`
2. `HardcodedMrCatalogProvider`
3. manifest definition models
4. manifest representation of current runnable MRs
5. parity tests
6. launcher injection change

Recommended second delivery slice:

1. evidence model additions
2. persistence roundtrip tests
3. metadata snapshot linkage (with V3 MR id reference, given V3 schema already
   landed on this branch)
4. doc and baseline refresh

This order minimizes risk while directly addressing the real structural problem.

---

## 10. Acceptance Criteria

- The launcher no longer owns runnable MR definitions directly.
- The current runnable MR catalog can be loaded from a provider-backed data
  source with no user-visible behavior regression.
- Existing launcher tests continue to pass with parity assertions added.
- A path exists to persist Stage 8 metadata and sample-level execution evidence
  without overloading the current summary record.
- Documentation is updated so runtime truth, structure docs, and Stage 8 plans
  no longer point in different directions.
- The hard-coded provider is marked `[Obsolete]` once manifest parity is
  achieved, and a parity guard test is in place while both coexist.

---

## 11. Decision Summary

- Keep the launcher façade.
- Move runnable definitions out of `SystemMtLauncher`.
- Use a provider-backed catalog as the single runtime definition source.
- Keep summary and evidence persistence separate in responsibility.
- Migrate by equivalence first, then extend toward Stage 8.
