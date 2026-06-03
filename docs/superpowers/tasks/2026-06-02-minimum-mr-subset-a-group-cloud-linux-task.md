# Cloud/Linux Task: Minimum-MR-SubSet A-Group Import/Export

Use this file as the complete cloud-side task instruction.

User instruction to run this task:

```text
Read docs/superpowers/tasks/2026-06-02-minimum-mr-subset-a-group-cloud-linux-task.md and execute the tasks in order.
```

## Scope

Implement the A-group import/export plan for `minimum-mr-subset`: P5, P4, and P9 only.

Do not implement P8, P3, P10, P1, P2, P6, or P7 in this task.

Do not edit WPF, XAML, `MetBench_Client/`, or Windows startup/config binding.

Do not promote imported assets into the live System-MT catalog.

## Required Reading

1. `AGENTS.md`
2. `docs/status/current.md`
3. `docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md`
4. `docs/superpowers/specs/2026-06-02-minimum-mr-subset-a-group-import-export-design.md`
5. `docs/superpowers/plans/2026-06-02-minimum-mr-subset-a-group-import-export-plan.md`
6. Existing import/export prototype files under `MetBench_BLL.Core/SystemMT/ImportExport/Put/`, if present.

## Task C0: Evidence Refresh

**Preconditions**

- You are in a git branch, not implementing directly on `main`.
- The worktree status is known.

**Core steps**

- Run `rtk git status -sb`.
- Run `rtk git fetch origin` if network access is available.
- Record whether fetch succeeded.
- Locate the external `Minimum-MR-SubSet` clone or obtain a read-only copy if the task environment provides one.
- Record external repository URL, commit, and source paths for P5, P4, P9.
- Attempt external smoke only if dependencies are already available; do not install dependencies without explicit approval.

**Acceptance standard**

- Evidence note states exact branch, base, source commit, source files, and smoke status.
- If smoke cannot run, the blocker is explicit, such as missing `numpy` or `pytest`.

## Task C1: Single-SUT Import Model

**Preconditions**

- C0 evidence note exists.

**Core steps**

- Add model records under a cloud-safe namespace such as `MetBench_BLL.Core/SystemMT/ImportExport/Put/`.
- Required model objects: `SutImportUnit`, `SutAsset`, `MrAsset`, `IoGroup`, `MutationAsset`, `DetectionRecord`, `Provenance`, `CompatibilityProfile`, `TransformBinding`, `AssertionBinding`.
- Use `public sealed record` for domain records.
- Use `public get; init;` for properties.
- Use `IReadOnlyList<T>` and `IReadOnlyDictionary<TKey,TValue>` for collections.
- Add enums required by the design spec.

**Acceptance standard**

- Focused model tests compile and pass.
- No Method MT legacy assertion class is introduced.

## Task C2: A-Group Fixtures

**Preconditions**

- C1 model records exist.

**Core steps**

- Create fixture packages for P5, P4, and P9.
- Each fixture is exactly one `SutImportUnit`.
- P5 observables: `t`, `power`, `precursor`, `power_extrema`.
- P4 observables: `q`, `p`, `energy`.
- P9 observables: `k_eff`, `sigma_k`, `reaction_balance`.
- P9 `ProgramKind` must be `Surrogate`.
- Add imported detection evidence only as imported research evidence.

**Acceptance standard**

- Fixture validation tests pass for all three SUTs.
- A negative test proves P9 without `Surrogate` classification is rejected.

## Task C3: Validator And Safety Rules

**Preconditions**

- C2 fixtures exist.

**Core steps**

- Implement fail-closed validation for schema version, safe package paths, single-SUT closure, observable refs, IO refs, mutation refs, detection refs, and provenance.
- Reject cross-SUT references.
- Reject path traversal before reading package files.
- Reject missing provenance.
- Reject detection records referencing unknown MR, mutation, or IO group.

**Acceptance standard**

- Valid P5/P4/P9 fixtures pass.
- Negative tests cover cross-SUT MR, unknown observable, unknown detection reference, path traversal, missing provenance, and invalid P9 classification.

## Task C4: Export And Round Trip

**Preconditions**

- C3 validator tests pass.

**Core steps**

- Implement package export.
- Re-import exported packages.
- Assert fixture identity for core ids, provenance, observables, MR ids, mutation ids, detection rows, and compatibility profile.

**Acceptance standard**

- P5, P4, and P9 round-trip tests pass.
- Export does not write into `SUT/`, live manifest catalog, LiteDB, or execution evidence repositories.

## Task C5: Compatibility Profile

**Preconditions**

- C4 round trip passes.

**Core steps**

- Compute or load compatibility profile for each MR.
- Default unknown assertion or transform mappings to `ImportedOnly`.
- Mark `RuntimeCandidate` only when transform and assertion bindings are both explicit.
- Do not create runtime catalog rows.

**Acceptance standard**

- Tests prove missing assertion binding prevents runtime readiness.
- Tests prove missing transform binding prevents runtime readiness.
- Tests prove explicit P4 energy invariant binding can be classified as `RuntimeCandidate`.
- Tests prove P9 candidate readiness requires explicit statistical/noise semantics.

## Task C6: Verification

**Preconditions**

- C1-C5 are implemented.

**Core steps**

- Run focused import/export tests for A group.
- Run `SemanticCatalogBoundaryTests`.
- Run `rtk git diff --check`.
- Report exact commands, pass/fail counts, skips, and blockers.

**Acceptance standard**

- Focused A-group tests pass.
- Semantic catalog boundary tests pass.
- Diff check passes.
- Report explicitly states whether external smoke was executed or blocked.
