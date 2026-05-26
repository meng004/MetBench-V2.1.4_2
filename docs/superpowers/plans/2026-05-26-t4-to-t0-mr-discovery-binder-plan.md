# T4 To T0 MR Discovery Binder Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a controlled binder that converts validated T4 MR discovery candidates into T0 System MT catalog assets without bypassing typed catalog validation, provenance tracking, or human review.

**Architecture:** Keep discovery generation and System MT execution separated by an explicit binder boundary. The binder consumes existing discovery proposal DTOs, validates required fields against the System MT catalog schema, emits a draft manifest patch or draft catalog document, and records provenance; it must not directly mutate active `SUT/<sut>/catalog.json` without an explicit approval step.

**Tech Stack:** .NET 8, xUnit, existing `MetBench_BLL.Core/Discovery`, existing `MetBench_BLL.Core/SystemMT/Catalog`, JSON manifest schema, LiteDB discovery entities.

---

## Scope And Non-Goals

This is a cloud-side T4-to-T0 bridge plan. It is suitable for Linux/cloud execution because it should stay inside cross-platform projects and tests.

This plan must not create a new discoverer. It must not call LLM APIs. It must not execute SUTs. It must not merge candidates into production catalogs automatically. It must not alter Method MT. It must not touch WPF.

The purpose is to remove ambiguity between "MR was discovered" and "MR is executable by T0". A candidate becomes a T0 candidate only after binder validation and an auditable draft artifact.

## Preconditions

- [ ] Start from latest `origin/main`.
- [ ] Confirm T1 manifest-driven runtime-env work is either merged or explicitly not required for this binder PR.
- [ ] Confirm `MetBench_BLL.Core/Discovery/` contains existing candidate proposal/discovery infrastructure.
- [ ] Confirm `MetBench_BLL.Core/SystemMT/Catalog/` contains manifest catalog document types and validation used by `ManifestMrCatalogProvider`.
- [ ] Confirm no active status-ledger gate forbids T4 work.
- [ ] Do not proceed if this work requires direct WPF UI integration.

## Files

- Create: `MetBench_BLL.Core/SystemMT/Catalog/Binding/DiscoveredMrCatalogBinder.cs`
- Create: `MetBench_BLL.Core/SystemMT/Catalog/Binding/DiscoveredMrBindingDraft.cs`
- Create: `MetBench_BLL.Core/SystemMT/Catalog/Binding/DiscoveredMrBindingError.cs`
- Create: `MetBench_BLL.Core/SystemMT/Catalog/Binding/DiscoveredMrBindingResult.cs`
- Create: `MetBench_BLL.Core/SystemMT/Catalog/Binding/IDiscoveredMrCatalogBinder.cs`
- Modify: `MetBench_BLL.Core/SystemMT/Catalog/SystemMtCatalogDocument.cs` only if the binder needs to call existing validation from tests without duplicating schema checks.
- Modify: `MetBench_BLL.Core/SystemMT/Catalog/MrBindingDefinition.cs` only if its constructor/init surface prevents creating a manifest-compatible draft binding.
- Create: `MetBench_SystemMT.Tests/SystemMT/Catalog/Binding/DiscoveredMrCatalogBinderTests.cs`
- Modify: `docs/status/current.md`
- Modify: `docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md`

## Binder Contract

The binder is a gate, not a shortcut.

```csharp
public interface IDiscoveredMrCatalogBinder
{
    DiscoveredMrBindingResult Bind(DiscoveredMrBindingDraft draft);
}
```

The first implementation accepts a deterministic draft DTO rather than raw LLM output:

```csharp
public sealed record DiscoveredMrBindingDraft(
    string SutId,
    string MrId,
    string DisplayName,
    string EquationKey,
    string MetaPattern,
    string TransformationName,
    string AssertionTypeCode,
    string ValueName,
    string SampleCaseRelativePath,
    string WorkRootName,
    IReadOnlyDictionary<string, string> DefaultParameters,
    string DiscoveryMethod,
    string DiscoveryRunId,
    double Confidence);
```

`Bind` returns either:

- a draft `MrBindingDefinition` compatible with a specific SUT manifest, plus provenance fields, or
- fail-closed errors explaining which field is missing, unsafe, or inconsistent.

## Task 1: Pin Binder Boundary With Failing Tests

**Files:**
- Test: `MetBench_SystemMT.Tests/SystemMT/Catalog/Binding/DiscoveredMrCatalogBinderTests.cs`

- [ ] **Step 1: Add failing test for valid candidate binding**

Add a test that builds a valid draft and expects a manifest-compatible binding:

```csharp
[Fact]
public void Bind_valid_discovery_draft_returns_manifest_binding_with_provenance()
{
    var binder = new DiscoveredMrCatalogBinder();
    var draft = new DiscoveredMrBindingDraft(
        SutId: "heat_equation",
        MrId: "heat-equation-amplitude-linearity-discovered",
        DisplayName: "Heat equation amplitude linearity discovered candidate",
        EquationKey: "fourier",
        MetaPattern: "Mono",
        TransformationName: "ScaleAmplitude",
        AssertionTypeCode: "greater",
        ValueName: "max_temperature",
        SampleCaseRelativePath: "sample/base.json",
        WorkRootName: "heat-equation-discovered",
        DefaultParameters: new Dictionary<string, string> { ["factor"] = "2" },
        DiscoveryMethod: "MetaPattern-Structural",
        DiscoveryRunId: "run-001",
        Confidence: 0.82);

    var result = binder.Bind(draft);

    Assert.True(result.Success, string.Join("; ", result.Errors.Select(e => e.Message)));
    Assert.Equal("heat-equation-amplitude-linearity-discovered", result.Binding!.MrId);
    Assert.Equal("greater", result.Binding.AssertionTypeCode);
    Assert.Equal("run-001", result.Provenance!.DiscoveryRunId);
}
```

- [ ] **Step 2: Add failing tests for fail-closed invalid drafts**

Cover at least these invalid states:

- missing `SutId`
- missing `MrId`
- unknown or blank `AssertionTypeCode`
- missing `ValueName`
- missing `DiscoveryMethod`
- `Confidence` outside `[0,1]`

- [ ] **Step 3: Run focused tests and verify red**

Run:

```bash
dotnet test MetBench_SystemMT.Tests --no-restore --filter FullyQualifiedName~DiscoveredMrCatalogBinderTests
```

Expected: fail because binder types do not exist.

## Task 2: Implement Binder DTOs And Fail-Closed Validation

**Files:**
- Create: `MetBench_BLL.Core/SystemMT/Catalog/Binding/DiscoveredMrBindingDraft.cs`
- Create: `MetBench_BLL.Core/SystemMT/Catalog/Binding/DiscoveredMrBindingError.cs`
- Create: `MetBench_BLL.Core/SystemMT/Catalog/Binding/DiscoveredMrBindingResult.cs`
- Create: `MetBench_BLL.Core/SystemMT/Catalog/Binding/IDiscoveredMrCatalogBinder.cs`
- Create: `MetBench_BLL.Core/SystemMT/Catalog/Binding/DiscoveredMrCatalogBinder.cs`

- [ ] **Step 1: Create DTOs**

Create immutable records for draft, result, and errors. Include provenance in the result as a separate record:

```csharp
public sealed record DiscoveredMrBindingProvenance(
    string DiscoveryMethod,
    string DiscoveryRunId,
    double Confidence);
```

- [ ] **Step 2: Implement validation**

Validation must reject:

- blank required strings
- blank parameter keys
- unsupported assertion codes not currently mappable by `LegacyAssertionPredicateMapper`
- confidence outside `[0,1]`
- path traversal in `SampleCaseRelativePath`

- [ ] **Step 3: Run focused binder tests**

Run:

```bash
dotnet test MetBench_SystemMT.Tests --no-restore --filter FullyQualifiedName~DiscoveredMrCatalogBinderTests
```

Expected: pass.

## Task 3: Ensure Binder Output Passes Existing Catalog Validation

**Files:**
- Test: `MetBench_SystemMT.Tests/SystemMT/Catalog/Binding/DiscoveredMrCatalogBinderTests.cs`
- Modify: catalog DTO access only if needed.

- [ ] **Step 1: Add test that embeds binder output in a catalog document**

Build a `SystemMtCatalogDocument` with:

- a minimal valid `ProgramDefinition`
- one `MrBindingDefinition` from binder output
- `doc.Validate()`

Assert validation passes.

- [ ] **Step 2: Add test for legacy-dictionary rejection**

If the draft tries to carry a dictionary predicate payload or unsupported free-form assertion, the binder must return failure rather than embedding it in the manifest.

- [ ] **Step 3: Run catalog + binder tests**

Run:

```bash
dotnet test MetBench_SystemMT.Tests --no-restore --filter "FullyQualifiedName~DiscoveredMrCatalogBinderTests|FullyQualifiedName~ManifestMrCatalogProviderTests"
```

Expected: pass.

## Task 4: Record Candidate-To-Catalog Traceability Without Auto-Merge

**Files:**
- Create or modify only binder result DTOs and tests.

- [ ] **Step 1: Add provenance fields to result**

The result must expose:

- discovery method
- discovery run id
- confidence
- binder timestamp or deterministic injected clock if timestamp is needed
- target SUT id
- target MR id

- [ ] **Step 2: Add test that active catalogs are not mutated**

The binder must not write to `SUT/<sut>/catalog.json`. Write a test using a temporary manifest path and assert no file is modified after `Bind`.

- [ ] **Step 3: Run focused tests**

Run:

```bash
dotnet test MetBench_SystemMT.Tests --no-restore --filter FullyQualifiedName~DiscoveredMrCatalogBinderTests
```

Expected: pass.

## Task 5: Documentation And Status Update

**Files:**
- Modify: `docs/status/current.md`
- Modify: `docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md`

- [ ] **Step 1: Update status ledger**

Record that T4 remains a discovery subsystem and this binder is the controlled bridge into T0 catalog assets. Do not claim end-to-end automatic MR discovery-to-execution is complete unless a later PR actually adds approval and execution.

- [ ] **Step 2: Update active plan index**

After merge, retire this plan as completed and register any follow-up approval/UI work separately.

## Task 6: Two-Layer Review And PR

**Files:**
- All modified files.

- [ ] **Step 1: Layer 1 self-review**

Check:

- No direct active catalog mutation.
- No Method MT changes.
- No WPF changes.
- No LLM calls.
- Binder uses existing catalog validation instead of inventing another schema.
- Unsupported or ambiguous candidates fail closed.

- [ ] **Step 2: Layer 2 maintainer review**

Review as a maintainer preventing project narrative drift:

- Is "discovered" clearly distinct from "catalog-bound" and "executable"?
- Can monitoring trace a candidate to source discovery evidence?
- Could this PR accidentally let unreviewed candidates enter T0 execution?

- [ ] **Step 3: Commit and PR**

Run:

```bash
git status --short
git add MetBench_BLL.Core/SystemMT/Catalog/Binding MetBench_SystemMT.Tests/SystemMT/Catalog/Binding docs/status/current.md docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md
git commit -m "feat(t4): bind discovery candidates to draft System MT catalog assets"
```

Open a PR titled:

```text
feat(t4): bind discovery candidates to draft System MT catalog assets
```

PR body must include:

- Summary.
- Tests run.
- Explicit note that the binder does not auto-merge into active catalogs.
- Explicit note that no Method MT, WPF, or live SUT execution changed.

## Acceptance Criteria

- Valid discovery drafts can be converted into manifest-compatible draft MR bindings.
- Invalid, unsupported, or ambiguous drafts fail closed with field-level errors.
- Binder output passes existing System MT catalog validation.
- Binder records discovery provenance.
- Binder never directly mutates `SUT/<sut>/catalog.json`.
- Full `MetBench_SystemMT.Tests` is green.
- Status ledger clearly distinguishes T4 discovery, T4-to-T0 binding, and T0 executable catalog state.

## Stop Conditions

Stop and report without coding if:

- `origin/main` is unreachable.
- Current status ledger supersedes this plan.
- Binding requires direct LLM API calls.
- Binding requires WPF screens.
- The only feasible implementation would bypass `SystemMtCatalogDocument.Validate()` or typed validation.
