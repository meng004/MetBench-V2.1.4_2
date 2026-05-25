# Verification Semantics Convergence Design

> **Date**: 2026-05-25
> **Status**: Approved design direction with registered implementation plan
> **Scope**: System MT verification semantics, Method MT isolation, typed catalog naming, and legacy assertion migration policy.

---

## 1. Decision Summary

MetBench will converge verification semantics onto one System MT path:

**System MT uses Typed Semantic Catalog predicates and typed runtime kernels as the only long-term verification semantics.**

Legacy System MT assertion runtime is not retained as a long-term compatibility layer. It must be migrated to the new typed path and then removed from production execution.

Method MT remains isolated and does not participate in this convergence.

## 2. Current Facts

The repository currently contains three related but different surfaces:

1. **System MT launcher and manifest catalog**
   - Entry: `MetBench_BLL.Core/SystemMT/Launcher/ISystemMtLauncher.cs`
   - Manifest/provider catalog: `MetBench_BLL.Core/SystemMT/Catalog/`
   - Current role: list and run system-level MRs through UI/API-facing launcher contracts.

2. **System MT legacy assertion path**
   - `MetBench_BLL.Core/SystemMT/IMrAssertion.cs`
   - `MetBench_BLL.Core/SystemMT/ApproxEqualAssertion.cs`
   - `MetBench_BLL.Core/SystemMT/GreaterThanAssertion.cs`
   - `MetBench_BLL.Core/SystemMT/LessThanAssertion.cs`
   - `MetBench_BLL.Core/SystemMT/Assertions/AssertionEvaluator.cs`
   - `MetBench_BLL.Core/SystemMT/Assertions/AssertionTypeCodes.cs`
   - Current role: old string-code assertion dispatch and obsolete W1 assertion classes.

3. **System MT typed semantic verification path**
   - Current path: `MetBench_BLL.Core/SystemMT/V12Catalog/`
   - Contains typed specs, fail-closed validators, predicate dispatcher, runtime kernels, property checker, migration gates, and coverage gates.
   - Current role: completed v1.2 typed verification roadmap and the intended formal verification semantics.

Method MT is separate:

- `MetBench_BLL/MethodMT/MethodMtPipeline.cs`
- `MetBench_BLL/MethodMT/Assertions/MethodAssertionEvaluator.cs`
- It uses C# delegates and in-memory dictionaries, not process/file/parser orchestration.
- It does not use `IMrAssertion`.

## 3. Target Naming

The current `V12Catalog` name is a phase/version name and should not become the permanent architecture name.

The long-term target is:

```text
MetBench_BLL.Core/SystemMT/Catalog/Typed/
```

The long-term namespace target is:

```csharp
MetBench_BLL.SystemMT.Catalog.Typed
MetBench_BLL.SystemMT.Catalog.Typed.Specs
MetBench_BLL.SystemMT.Catalog.Typed.Validation
MetBench_BLL.SystemMT.Catalog.Typed.Runtime
MetBench_BLL.SystemMT.Catalog.Typed.Property
MetBench_BLL.SystemMT.Catalog.Typed.Derived
MetBench_BLL.SystemMT.Catalog.Typed.Serialization
MetBench_BLL.SystemMT.Catalog.Typed.Lint
```

Documentation should call this subsystem **Typed Semantic Catalog**.

`SystemMT/Catalog/` by itself remains the broader catalog area. It must not hide the distinction between:

- manifest/provider catalog entries used by launcher wiring
- typed semantic specs used for validation and runtime verification

## 4. Method MT Boundary

Method MT remains isolated.

Allowed Method MT responsibilities:

- execute method-level C# function checks through `MethodMtPipeline`
- use `MethodAssertionEvaluator`
- keep the existing `less`, `greater`, and `approx` method-level assertion vocabulary
- share low-level transformation helpers only where already intentional

Disallowed Method MT responsibilities:

- no dependency on `IMrAssertion`
- no dependency on System MT typed kernel runtime
- no participation in System MT catalog migration gates
- no expansion as the next project mainline unless a future design explicitly reopens it

This makes Method MT a lightweight auxiliary capability, not the main verification architecture.

## 5. System MT Boundary

System MT is the active verification mainline.

Allowed System MT responsibilities:

- launcher-facing execution through `ISystemMtLauncher`
- manifest/provider catalog loading for listing and run orchestration
- typed MR and Property verification through Typed Semantic Catalog specs, validators, dispatchers, and kernels
- evidence recording and reporting from typed verification results

Disallowed System MT responsibilities after convergence:

- production runtime must not call `AssertionEvaluator`
- production runtime must not depend on `IMrAssertion`
- production runtime must not add new `AssertionTypeCode` semantics
- new System MT catalog entries must not use legacy string assertion codes as their verification semantics

## 6. Migration Policy

Legacy System MT assertions are migration input only.

During migration, a temporary mapper may translate old assertion codes into typed predicates:

| Legacy source | Typed target |
|---|---|
| `less` | `BinaryComparisonPredicate(Operator=Less)` |
| `greater` | `BinaryComparisonPredicate(Operator=Greater)` |
| `approx` | `BinaryComparisonPredicate(Operator=Equal)` with deterministic tolerance |
| `approx-invariant` | `BinaryComparisonPredicate(Operator=Equal)` or `DerivedInvariantPredicate`, depending on metric shape |
| `variance-ratio` | `VarianceRatioPredicate` |
| `flux-pointwise-approx` | `FieldEqualityPredicate` or `FieldProportionalityPredicate`, depending on catalog semantics |
| `cross-program-agree` | `CrossMethodComparisonPredicate` |
| scaling such as `flw = k * src` | `ScaledEqualityPredicate` |

The mapper is not a permanent runtime abstraction. It may exist only in a migration PR or test fixture that proves the migration.

After convergence:

- old assertion codes may remain only in historical documents, migration snapshots, or test assets explicitly marked legacy
- production System MT run path must consume typed specs and typed kernel results
- architecture tests must fail if new production System MT code introduces `IMrAssertion`, `AssertionEvaluator`, or new string assertion-code dispatch

## 7. Runtime Convergence Target

The target System MT runtime shape is:

```text
ISystemMtLauncher
  -> manifest/provider catalog read
  -> typed spec lookup
  -> fail-closed typed validation
  -> run planning / execution
  -> RoleOutput collection
  -> PredicateDispatcher / IVerifierKernel<TPredicate>
  -> typed VerificationResult
  -> ExecutionEvidence / report / persistence
```

The launcher owns the entry point. It does not own verification semantics.

The manifest/provider catalog owns catalog discovery and wiring. It does not own runtime assertion semantics.

Typed Semantic Catalog owns:

- typed MR and Property schema
- semantic validation
- predicate and property dispatch
- runtime kernel evaluation
- typed diagnostics
- migration and coverage gates

## 8. Execution Order

Implementation must proceed in small PRs:

1. **PR-A: Design lock**
   - Add this convergence design.
   - Update planning/status docs if needed.
   - No runtime behavior change.

2. **PR-B: Naming migration**
   - Rename `SystemMT/V12Catalog` to `SystemMT/Catalog/Typed`.
   - Rename namespaces and tests.
   - Preserve behavior exactly.

3. **PR-C: Runtime convergence**
   - Route System MT verification through typed specs and kernels.
   - Migrate legacy assertion-code semantics into typed predicates.
   - Remove production dependency on `IMrAssertion`, `AssertionEvaluator`, and string-code runtime dispatch.

4. **PR-D: Guard and cleanup**
   - Add architecture tests blocking new legacy assertion dependencies.
   - Remove obsolete production classes when no longer referenced.
   - Keep only historical/migration references explicitly marked legacy.

## 9. Acceptance Criteria

Design PR acceptance:

- This document exists and is linked from the active plan or status ledger before implementation work starts.
- It explicitly states that Method MT does not use `IMrAssertion`.
- It explicitly states that System MT legacy assertions are migrated, not retained.
- It explicitly names `SystemMT/Catalog/Typed` as the target for the current `V12Catalog`.
- It does not change runtime behavior.

Naming PR acceptance:

- `V12Catalog` production namespaces are renamed to `Catalog.Typed`.
- Tests are renamed or updated consistently.
- No behavior changes are introduced.
- Full `MetBench_SystemMT.Tests` passes.

Runtime convergence PR acceptance:

- System MT production runtime no longer calls `AssertionEvaluator`.
- System MT production runtime no longer depends on `IMrAssertion`.
- System MT catalog verification result comes from typed predicate dispatch and typed kernels.
- Legacy assertion-code migration fixtures prove equivalent semantics for migrated cases.
- `flw = k * src` is represented as `ScaledEqualityPredicate`, not custom string dispatch.
- Deterministic equality uses explicit `Atol` and `Rtol` through typed tolerance specs.

Guard PR acceptance:

- Architecture tests reject new System MT production references to `IMrAssertion`.
- Architecture tests reject new System MT production references to `AssertionEvaluator`.
- Architecture tests reject new System MT production string assertion dispatch except in explicitly marked migration tests or historical fixtures.
- Documentation states that new System MT MR and Property work must use Typed Semantic Catalog.

## 10. Non-Goals

This convergence does not:

- redesign Method MT
- add property-based testing
- change WPF UI behavior
- change Windows verification policy
- redesign ExecutionEvidence v2
- alter the scientific meaning of the existing 44 MR + 4 Property inventory

Those topics require separate design documents or implementation plans.

## 11. Risks and Controls

| Risk | Control |
|---|---|
| `Catalog/Typed` confused with manifest provider catalog | Keep `Manifest` and `Typed` roles separate in docs and namespaces |
| Runtime migration changes behavior invisibly | Add legacy-to-typed equivalence fixtures before replacing runtime |
| Method MT pulled into System MT convergence | State Method MT isolation in design and guard docs |
| String assertion codes survive as hidden main path | Add architecture tests after convergence |
| Large PR destabilizes project | Use PR-A through PR-D sequencing |

## 12. Final Decision

The accepted direction is:

```text
Method MT stays isolated.
System MT converges to Typed Semantic Catalog.
V12Catalog becomes Catalog/Typed.
Legacy System MT assertion runtime is migrated and removed from production path.
Design precedes implementation.
```
