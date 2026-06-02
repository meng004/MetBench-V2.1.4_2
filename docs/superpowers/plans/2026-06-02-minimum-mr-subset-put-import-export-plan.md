# Minimum-MR-SubSet PUT Import/Export Implementation Plan (Superseded)

> **Superseded on 2026-06-02** by
> `docs/superpowers/plans/2026-06-02-minimum-mr-subset-a-group-import-export-plan.md`.
> The active design changed from a P5-then-P8 sequence to a single-SUT import
> unit model, with A group (`P5`, `P4`, `P9`) selected as the first safe batch.
> Keep this file as discussion history only; do not execute it as the current
> implementation plan.

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a controlled import/export capability for external PUT assets from `minimum-mr-subset`, starting with PUT P5 and then extending to PUT P8, without directly registering imported assets into the live System-MT runtime catalog.

**Architecture:** Define a formal PUT symbol system first, freeze a versioned storage format second, then implement a fail-closed engineering path: external package generation -> MetBench package validation -> staged import view -> MetBench export -> round-trip evidence. The first concrete fixture is P5 point-kinetics equation; the second fixture is P8 Schrodinger split-operator spectral solver.

**Tech Stack:** .NET 8, xUnit, System.Text.Json, Python 3 stdlib package generator, JSON package manifests, existing System-MT typed semantic catalog boundaries, existing PR gate checklist.

---

## 1. Scope And Sequencing

This plan is a scoped successor to the `minimum-mr-subset` T3 assessment: it treats that repository as an external research asset source, not as a catalog to wholesale import.

Execution order is fixed:

1. **P5 first**: import/export package support for `P5` (`stiff ODE / point kinetics`). P5 is the lower-risk first engineering fixture because it is scalar/time-series oriented and closer to the existing reactor-physics narrative.
2. **P8 second**: extend the same format and validators for `P8` (`complex PDE / spectral`). P8 is the architecture stress test because it introduces complex-valued state, probability density, norm conservation, FFT/spectral provenance, and denser array outputs.

Non-goals for this plan:

- Do not copy the full `minimum-mr-subset` repository into `SUT/`.
- Do not add P5 or P8 to `SUT/<sut>/catalog.json` in the first import/export PR chain.
- Do not create live T0 `MrBlueprint` rows from imported MR drafts until a later candidate-specific T0/T3/T6 plan approves them.
- Do not mix Method MT legacy assertion classes into the System-MT import path.

## 2. Formal Symbol System

Define a PUT package as:

```text
PUT = <id, source, domain, equation, algorithm, input_space, observable_space, canonical_cases, mr_drafts, mutation_study, provenance>
```

Where:

- `id`: stable external PUT id, for this plan `P5` or `P8`.
- `source`: external repository identity, including URL, branch, commit, source file path, and import timestamp.
- `domain`: controlled classification such as `stiff-ode-point-kinetics` or `complex-pde-spectral`.
- `equation`: human-readable equation family plus optional normalized symbols.
- `algorithm`: solver family and numerical method metadata.
- `input_space`: named parameters accepted by the adapter, including default canonical values and allowed perturbation metadata.
- `observable_space`: declared output values and shapes, including scalar, vector, matrix, time-series, or complex-derived observables.
- `canonical_cases`: executable reference cases used to verify importer/exporter behavior.
- `mr_drafts`: external or derived MR candidates, retained as drafts and never auto-registered into live runtime.
- `mutation_study`: optional adequacy and mutation artifacts, including mutant ids, operators, kill matrix, and minimal-subset claims.
- `provenance`: immutable import evidence.

Define a mutation study as:

```text
MutationStudy = <put_id, mutants, detections, killed_by, minimal_subsets, adequacy_metrics, provenance>
```

Define a MetBench import package as:

```text
ImportPackage(v1) =
  <package_manifest, put, cases, observables, mr_drafts, mutants, detection_matrix, provenance>
```

Validation rules:

- `package_manifest.schema_version` must equal `put-import.v1`.
- `put.id` must match every `put_id` in cases, observables, MR drafts, mutants, and detection rows.
- Every observable referenced by an MR draft or mutant detection row must be declared in `observables.json`.
- Complex-valued states are not stored as raw language-native complex numbers; they must be represented by explicit real/imag arrays or by declared derived real observables.
- Import validation fails closed on unknown schema versions, missing provenance, path traversal, duplicate ids, unsupported observable kinds, or MR draft semantics that cannot be represented as typed catalog drafts.

## 3. Storage Format

The versioned external package format lives under a package root such as:

```text
put-packages/minimum-mr-subset/<commit>/<put-id>/
  export-manifest.json
  put.json
  cases.json
  observables.json
  mr-drafts.json
  mutants.json
  detection-matrix.csv
  provenance.json
```

`export-manifest.json`:

```json
{
  "schema_version": "put-import.v1",
  "package_id": "minimum-mr-subset:P5:<commit>",
  "put_id": "P5",
  "source": {
    "repository_url": "captured from the external repository origin URL",
    "commit": "captured 40-character source commit SHA",
    "source_paths": ["experiments/puts/p5_pke.py"]
  },
  "files": {
    "put": "put.json",
    "cases": "cases.json",
    "observables": "observables.json",
    "mr_drafts": "mr-drafts.json",
    "mutants": "mutants.json",
    "detection_matrix": "detection-matrix.csv",
    "provenance": "provenance.json"
  }
}
```

`put.json`:

```json
{
  "schema_version": "put-import.v1",
  "put_id": "P5",
  "name": "Point kinetics equation",
  "domain": "stiff-ode-point-kinetics",
  "equation_family": "point kinetics",
  "algorithm": {
    "name": "minimum-mr-subset P5 adapter",
    "method": "external-python-adapter"
  },
  "input_space": [
    { "name": "t_end", "kind": "scalar", "unit": "s", "default": 1.0 },
    { "name": "num_steps", "kind": "integer", "default": 100 }
  ]
}
```

`observables.json` declares shapes:

```json
[
  { "put_id": "P5", "name": "t", "kind": "vector", "element_type": "real" },
  { "put_id": "P5", "name": "power", "kind": "time_series", "element_type": "real", "axis": "t" },
  { "put_id": "P5", "name": "precursor", "kind": "matrix", "element_type": "real", "axis": "t" },
  { "put_id": "P5", "name": "power_extrema", "kind": "object", "element_type": "real" }
]
```

P8 adds these observable kinds without changing the format version:

```json
[
  { "put_id": "P8", "name": "x", "kind": "vector", "element_type": "real" },
  { "put_id": "P8", "name": "probability_density", "kind": "field_1d", "element_type": "real", "axis": "x" },
  { "put_id": "P8", "name": "norm", "kind": "scalar", "element_type": "real" }
]
```

Raw complex arrays, if exported later, must use:

```json
{
  "name": "psi",
  "kind": "field_1d",
  "element_type": "complex_pair",
  "encoding": "real_imag_pairs"
}
```

## 4. PR Chain

### PR-0: Formalism, Schema, And Plan Registration

Files:

- `docs/superpowers/specs/2026-06-02-put-import-export-formalism.md`
- `schemas/put-import/v1/export-manifest.schema.json`
- `schemas/put-import/v1/put.schema.json`
- `schemas/put-import/v1/cases.schema.json`
- `schemas/put-import/v1/observables.schema.json`
- `schemas/put-import/v1/mr-drafts.schema.json`
- `schemas/put-import/v1/mutants.schema.json`
- `schemas/put-import/v1/provenance.schema.json`
- `docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md`

Tasks:

- [ ] Write the formal PUT and MutationStudy symbol system in the spec.
- [ ] Add JSON schema files for every package document.
- [ ] Add schema examples for P5 and P8.
- [ ] Register this plan in the active plan index.
- [ ] Run a doc hygiene search against the new spec and schema directory to catch unresolved authoring markers before PR creation.
- [ ] Run `rtk git diff --check`.

Acceptance:

- The format is versioned as `put-import.v1`.
- P5 and P8 can both be described without schema changes.
- The spec explicitly states imported MR drafts are not live runtime MRs.

### PR-1: P5 Package Generator And Fixture

Files:

- `tools/put_import/minimum_mr_subset_to_put_package.py`
- `tools/tests/test_minimum_mr_subset_to_put_package.py`
- `tests/fixtures/put-import/minimum-mr-subset/p5/export-manifest.json`
- `tests/fixtures/put-import/minimum-mr-subset/p5/put.json`
- `tests/fixtures/put-import/minimum-mr-subset/p5/cases.json`
- `tests/fixtures/put-import/minimum-mr-subset/p5/observables.json`
- `tests/fixtures/put-import/minimum-mr-subset/p5/mr-drafts.json`
- `tests/fixtures/put-import/minimum-mr-subset/p5/mutants.json`
- `tests/fixtures/put-import/minimum-mr-subset/p5/detection-matrix.csv`
- `tests/fixtures/put-import/minimum-mr-subset/p5/provenance.json`

Implementation shape:

```python
def build_put_package(source_root: Path, put_id: str, output_root: Path) -> Path:
    if put_id != "P5":
        raise ValueError("PR-1 supports only P5")
    source_file = source_root / "experiments" / "puts" / "p5_pke.py"
    package_root = output_root / "minimum-mr-subset" / read_git_commit(source_root) / "P5"
    write_manifest(package_root, put_id="P5", source_paths=["experiments/puts/p5_pke.py"])
    write_p5_put(package_root)
    write_p5_cases(package_root)
    write_p5_observables(package_root)
    write_empty_or_seeded_mr_drafts(package_root)
    write_empty_or_seeded_mutation_study(package_root)
    write_provenance(package_root, source_file)
    return package_root
```

Tasks:

- [ ] Implement a Python stdlib generator that reads the external repo path and emits a P5 package.
- [ ] Preserve external source provenance: repository URL, branch or detached head, commit, source file path, import time, generator version.
- [ ] Emit deterministic JSON key ordering and stable CSV ordering.
- [ ] Add Python tests for missing source file, unsupported PUT id, deterministic output, and provenance presence.
- [ ] Run `rtk python3 -m pytest tools/tests/test_minimum_mr_subset_to_put_package.py`.

Acceptance:

- A package generated twice from the same external commit is byte-stable except for explicitly documented import timestamp fields.
- The fixture can be used by C# tests without requiring the external repository at test time.

### PR-2: MetBench PUT Package Model And Fail-Closed Validator

Files:

- `MetBench_BLL.Core/SystemMT/ImportExport/Put/PutImportPackage.cs`
- `MetBench_BLL.Core/SystemMT/ImportExport/Put/PutAssetDocument.cs`
- `MetBench_BLL.Core/SystemMT/ImportExport/Put/PutObservableDocument.cs`
- `MetBench_BLL.Core/SystemMT/ImportExport/Put/PutMrDraftDocument.cs`
- `MetBench_BLL.Core/SystemMT/ImportExport/Put/PutMutationStudyDocument.cs`
- `MetBench_BLL.Core/SystemMT/ImportExport/Put/PutProvenanceDocument.cs`
- `MetBench_BLL.Core/SystemMT/ImportExport/Put/PutImportValidationResult.cs`
- `MetBench_BLL.Core/SystemMT/ImportExport/Put/PutImportValidator.cs`
- `MetBench_SystemMT.Tests/SystemMT/ImportExport/PutImportValidatorTests.cs`
- `MetBench_SystemMT.Tests/SystemMT/ImportExport/PutImportFixtureTests.cs`

Implementation shape:

```csharp
public sealed record PutImportPackage(
    string RootDirectory,
    PutExportManifest Manifest,
    PutAssetDocument Put,
    IReadOnlyList<PutCaseDocument> Cases,
    IReadOnlyList<PutObservableDocument> Observables,
    IReadOnlyList<PutMrDraftDocument> MrDrafts,
    PutMutationStudyDocument MutationStudy,
    PutProvenanceDocument Provenance);

public sealed class PutImportValidator
{
    public PutImportValidationResult Validate(PutImportPackage package)
    {
        // fail closed on schema_version, put_id mismatch, duplicate ids,
        // missing provenance, undeclared observables, and unsupported kinds
    }
}
```

Tasks:

- [ ] Load package documents with `System.Text.Json` and explicit required-property validation.
- [ ] Reject path traversal in manifest file entries before reading package files.
- [ ] Reject unknown observable kinds except the approved v1 set: `scalar`, `vector`, `matrix`, `time_series`, `field_1d`, `object`.
- [ ] Reject `complex_pair` unless `encoding` is `real_imag_pairs`.
- [ ] Add P5 fixture validation tests.
- [ ] Add negative tests for duplicate observable names, missing provenance, schema mismatch, path traversal, and undeclared MR observable references.
- [ ] Run `rtk dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --filter "FullyQualifiedName~PutImport"`.

Acceptance:

- Valid P5 fixture passes.
- Every malformed fixture fails closed with a concrete reason string.
- No imported MR draft reaches `SystemMtLauncher`, `ManifestMrCatalogProvider`, or live `MrBlueprint` registration.

### PR-3: Staged Import Service And Export Round Trip

Files:

- `MetBench_BLL.Core/SystemMT/ImportExport/Put/IPutImportService.cs`
- `MetBench_BLL.Core/SystemMT/ImportExport/Put/PutImportService.cs`
- `MetBench_BLL.Core/SystemMT/ImportExport/Put/IPutExportService.cs`
- `MetBench_BLL.Core/SystemMT/ImportExport/Put/PutExportService.cs`
- `MetBench_BLL.Core/SystemMT/ImportExport/Put/PutStagedImportRecord.cs`
- `MetBench_SystemMT.Tests/SystemMT/ImportExport/PutImportServiceTests.cs`
- `MetBench_SystemMT.Tests/SystemMT/ImportExport/PutExportRoundTripTests.cs`

Implementation shape:

```csharp
public interface IPutImportService
{
    PutStagedImportRecord StageImport(string packageRoot);
}

public interface IPutExportService
{
    void Export(PutStagedImportRecord stagedImport, string outputDirectory);
}
```

Tasks:

- [ ] Stage a valid package as an in-memory `PutStagedImportRecord`.
- [ ] Preserve all provenance and package ids during staging.
- [ ] Implement export from staged record back to the same v1 package document set.
- [ ] Add round-trip tests: fixture -> staged import -> export -> validate exported package.
- [ ] Assert live catalog counts do not change after staging.
- [ ] Run `rtk dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --filter "FullyQualifiedName~PutImport|FullyQualifiedName~PutExport"`.

Acceptance:

- P5 can be imported into a staged record and exported back to a valid v1 package.
- Exported package validates under the same validator.
- Runtime catalog inventory and existing System-MT launcher behavior remain unchanged.

### PR-4: P8 Package Generator Extension

Files:

- `tools/put_import/minimum_mr_subset_to_put_package.py`
- `tools/tests/test_minimum_mr_subset_to_put_package.py`
- `tests/fixtures/put-import/minimum-mr-subset/p8/export-manifest.json`
- `tests/fixtures/put-import/minimum-mr-subset/p8/put.json`
- `tests/fixtures/put-import/minimum-mr-subset/p8/cases.json`
- `tests/fixtures/put-import/minimum-mr-subset/p8/observables.json`
- `tests/fixtures/put-import/minimum-mr-subset/p8/mr-drafts.json`
- `tests/fixtures/put-import/minimum-mr-subset/p8/mutants.json`
- `tests/fixtures/put-import/minimum-mr-subset/p8/detection-matrix.csv`
- `tests/fixtures/put-import/minimum-mr-subset/p8/provenance.json`
- `MetBench_SystemMT.Tests/SystemMT/ImportExport/PutImportP8FixtureTests.cs`

Tasks:

- [ ] Extend the generator to support `--put P8`.
- [ ] Record P8 source path `experiments/puts/p8_schrodinger.py`.
- [ ] Declare P8 observables `x`, `probability_density`, and `norm`.
- [ ] Add at least one validator test that rejects raw complex values without explicit `complex_pair` encoding.
- [ ] Add P8 fixture validation and round-trip tests.
- [ ] Run `rtk python3 -m pytest tools/tests/test_minimum_mr_subset_to_put_package.py`.
- [ ] Run `rtk dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --filter "FullyQualifiedName~PutImport|FullyQualifiedName~PutExport"`.

Acceptance:

- P8 uses the same `put-import.v1` format as P5.
- P8 complex/spectral characteristics are represented through declared real observables or explicit complex-pair encoding.
- No P8 runtime catalog row is created in this PR.

### PR-5: Projection, Evidence, And Follow-Up Decision

Files:

- `docs/status/current.md`
- `docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md`
- `docs/superpowers/specs/2026-06-02-put-import-export-chain-post-merge-review.md`

Tasks:

- [ ] Record final import/export chain evidence in the status ledger.
- [ ] Mark this plan Completed in the active plan index after PR-0 through PR-4 land.
- [ ] Run a chain-end review focused on Cat B risks: direct catalog leakage, provenance loss, schema drift, P5/P8 semantic overclaiming, and mutation adequacy misclassification.
- [ ] Decide the next scoped plan: T3 candidate onboarding, T4 MR-draft binder, or T6 mutation/adequacy analytics.

Acceptance:

- Status ledger and active index agree on final state.
- The chain-end review states whether P5/P8 are still staged external PUT assets or whether a new candidate-specific runtime plan is authorized.

## 5. Verification Matrix

Cloud-safe commands:

```bash
rtk python3 -m pytest tools/tests/test_minimum_mr_subset_to_put_package.py
rtk dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --filter "FullyQualifiedName~PutImport|FullyQualifiedName~PutExport"
rtk dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --filter "FullyQualifiedName~SemanticCatalogBoundaryTests"
rtk git diff --check
```

Expected cloud evidence:

- Python generator tests pass.
- Focused C# import/export tests pass.
- Semantic catalog boundary tests pass, proving staged imports did not reintroduce legacy or direct runtime registration paths.
- Diff check passes.

Windows classification:

- PR-0 through PR-4 are cloud-safe if they avoid WPF UI.
- Any future UI for browsing staged PUT packages is Windows/VM work and requires a separate VM plan.

## 6. Risk Controls

- **Runtime leakage risk:** staged import services must not call `SystemMtLauncher`, `ManifestMrCatalogProvider`, or catalog mutation APIs.
- **Provenance erosion risk:** all exported packages must include external repo URL, commit, source path, generator version, and import timestamp.
- **Schema drift risk:** schema version is exact-match only; unknown versions fail closed.
- **P8 complexity risk:** complex values require explicit encoding; derived real observables are preferred for v1.
- **T3/T6 confusion risk:** P5/P8 package import is infrastructure; T3 runtime onboarding and T6 mutation adequacy require separate scoped plans.

## 7. Done Definition

This plan is complete only when:

- P5 and P8 package fixtures both validate.
- P5 and P8 staged import/export round trips both pass.
- No live runtime catalog inventory changes as a side effect of staged import.
- Status ledger and active plan index record the completed chain.
- A follow-up decision explicitly chooses whether the next plan is T3, T4, or T6.
