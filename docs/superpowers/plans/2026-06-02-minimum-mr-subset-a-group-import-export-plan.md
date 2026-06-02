# Minimum-MR-SubSet A-Group Import/Export Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the first controlled import/export version for `minimum-mr-subset` A group (`P5`, `P4`, `P9`) using a single-SUT import unit with explicit compatibility profiling.

**Architecture:** Import/export remains cloud-side and staged by default. Each package imports exactly one SUT and its MRs, IO groups, mutations, detection evidence, provenance, and compatibility profile. Runtime promotion is not part of this plan.

**Tech Stack:** .NET 8, System.Text.Json, xUnit, existing System-MT typed catalog boundary tests, docs-only Windows VM prompt for environment classification.

---

## Evidence Preconditions

- External source repository: `https://github.com/meng004/Minimum-MR-SubSet.git`
- Source commit observed locally: `b931b5f74d0f3f3cb704cd6fedbcb4f523cccd7f`
- Evidence files:
  - `experiments/puts/p5_pke.py`
  - `experiments/puts/p4_pendulum.py`
  - `experiments/puts/p9_openmc.py`
  - `tests/puts/test_smoke.py`
  - `scripts/llm/multi_llm_pipeline.py`
- Local limitation observed before this plan: system Python in `/private/tmp/minimum-mr-subset` lacked `pytest` and `numpy`, so external smoke execution was not completed in that environment.

## Task 0: Baseline And Scope Confirmation

**Preconditions**

- Work starts from a clean branch based on current `origin/main`.
- Read `docs/superpowers/specs/2026-06-02-minimum-mr-subset-a-group-import-export-design.md`.
- Read `docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md`.
- Confirm whether local uncommitted prototype files exist; do not silently include unrelated prototype files.

**Core steps**

- [ ] Run `rtk git status -sb`.
- [ ] Run `rtk git fetch origin` if network is available; if blocked, record the exact blocker.
- [ ] Confirm the PR scope is A group only: P5, P4, P9.
- [ ] Confirm no WPF / `MetBench_Client/` edits are planned.

**Acceptance standard**

- A short evidence note states branch, base head, dirty/clean state, external source commit, and whether external smoke can run.
- If worktree is dirty, unrelated files are not staged.

## Task 1: Formal Model And Records

**Preconditions**

- Task 0 evidence note exists.
- The agent has read existing `MetBench_BLL.Core/SystemMT/ImportExport/Put/` if present.

**Core steps**

- [ ] Add or update cloud-side model records for `SutImportUnit`, `SutAsset`, `MrAsset`, `IoGroup`, `MutationAsset`, `DetectionRecord`, `Provenance`, and `CompatibilityProfile`.
- [ ] Add `TransformBinding` and `AssertionBinding` to MR assets.
- [ ] Use immutable init-only properties and read-only collections.
- [ ] Add enums for `ProgramKind`, `CompatibilityStatus`, `RuntimeReadiness`, `MutationRepresentationKind`, `EvidenceKind`, and `DetectionResult`.
- [ ] Keep helper methods such as `SupportsObservable` public; keep parser/path helpers private or internal only when tests require.

**Acceptance standard**

- Models compile.
- Model tests cover single-SUT closure, duplicate ids, missing provenance, and read-only collection behavior.

## Task 2: A-Group Fixture Packages

**Preconditions**

- Task 1 models exist.
- External source commit and source paths are recorded.

**Core steps**

- [ ] Create one fixture package per SUT: P5, P4, and P9.
- [ ] Each package must contain one root `SutImportUnit`.
- [ ] P5 fixture records observables `t`, `power`, `precursor`, `power_extrema`.
- [ ] P4 fixture records observables `q`, `p`, `energy`.
- [ ] P9 fixture records observables `k_eff`, `sigma_k`, `reaction_balance` and `ProgramKind = Surrogate`.
- [ ] Detection evidence imported from `minimum-mr-subset` must be marked `ImportedResearchEvidence`.

**Acceptance standard**

- Each fixture validates as a single-SUT import unit.
- P9 fixture cannot be represented as real OpenMC; tests assert surrogate classification.

## Task 3: Import Validator

**Preconditions**

- Task 2 fixtures exist.
- Existing path traversal and schema checks are identified.

**Core steps**

- [ ] Implement fail-closed validation for schema version, package root paths, single-SUT closure, MR observable references, IO group references, mutation references, detection references, and provenance.
- [ ] Reject any MR whose `SutId` differs from root `Sut.SutId`.
- [ ] Reject any detection row that references an unknown MR, mutation, or IO group.
- [ ] Reject P9 packages that omit surrogate classification.

**Acceptance standard**

- Valid P5/P4/P9 fixtures pass.
- Negative tests fail closed with concrete error messages for cross-SUT reference, missing provenance, unknown observable, path traversal, and invalid P9 classification.

## Task 4: Export And Round Trip

**Preconditions**

- Task 3 validator passes for all A-group fixtures.

**Core steps**

- [ ] Implement export of a staged `SutImportUnit` package.
- [ ] Preserve provenance and compatibility bindings byte-stably where possible.
- [ ] Re-import exported packages and validate them.
- [ ] Assert exported P9 still has `ProgramKind = Surrogate`.

**Acceptance standard**

- P5, P4, and P9 each pass import -> export -> import round trip.
- Round-trip does not create or mutate live System-MT catalog rows.

## Task 5: Compatibility Profile

**Preconditions**

- Task 4 round trip passes.
- The agent has inspected current typed assertion and transform support.

**Core steps**

- [ ] Generate `CompatibilityProfile` for each imported MR.
- [ ] Default unknown assertion or transform mappings to `ImportedOnly`.
- [ ] Mark candidate MR bindings only when both `AssertionBinding` and `TransformBinding` are explicit.
- [ ] Do not promote anything to runtime catalog in this task.

**Acceptance standard**

- Tests show imported MRs default to non-runtime when bindings are missing.
- Tests show explicit P4 energy invariant can be classified as `RuntimeCandidate` if approximate-equality assertion and input paths are present.
- Tests show P9 statistical output can be classified as `RuntimeCandidate` only when `sigma_k` and variance/noise-aware semantics are explicit.

## Task 6: Boundary And Documentation Verification

**Preconditions**

- Tasks 1-5 pass focused tests.

**Core steps**

- [ ] Run focused A-group import/export tests.
- [ ] Run `SemanticCatalogBoundaryTests`.
- [ ] Run `rtk git diff --check`.
- [ ] Update active plan index and status ledger only with evidence actually produced.
- [ ] If external smoke was not run, record the blocker instead of claiming execution.

**Acceptance standard**

- Verification output reports exact pass/fail counts when available.
- No runtime catalog count changes are caused by staged import.
- Documentation states Windows classification explicitly: no Windows evidence for cloud-only import/export unless VM validation is separately run.

## Environment Task Files

Cloud/Linux agent should execute:

```text
Read docs/superpowers/tasks/2026-06-02-minimum-mr-subset-a-group-cloud-linux-task.md and execute the tasks in order.
```

Windows VM agent should execute only when VM evidence is requested:

```text
Read docs/superpowers/vm-prompts/2026-06-02-minimum-mr-subset-a-group-windows-vm-prompt.md and execute the verification prompt.
```
