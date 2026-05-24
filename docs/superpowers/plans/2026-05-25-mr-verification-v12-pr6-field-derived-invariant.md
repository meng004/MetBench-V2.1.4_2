# MR Verification v1.2 PR-6 Field Derived Invariant Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 支持 field projections、field pairings、field tolerance 和 derived invariant，使 G8 / G10 可执行。

**Architecture:** field runtime 与 scalar/sequence 分离，pairing 和 tolerance 先在 validator 阶段 fail-closed，再由 kernel 做 residual 计算与 worst-offender 诊断。derived operators 放独立目录，供 MR 与后续 property checker 共用。

**Tech Stack:** .NET 8 / xUnit

---

### Task 1: 引入 Field2DValue、FieldPairing、FieldNormTolerance

**Files:**
- Modify: `MetBench_BLL.Core/SystemMT/V12Catalog/Specs/FieldPairing.cs`
- Modify: `MetBench_BLL.Core/SystemMT/V12Catalog/Specs/ToleranceSpec.cs`
- Create: `MetBench_BLL.Core/SystemMT/V12Catalog/Runtime/Field2DValue.cs`
- Test: `MetBench_SystemMT.Tests/SystemMT/V12Catalog/FieldModelTests.cs`

- [ ] **Step 1: 写失败测试**

```csharp
[Fact]
public void FieldNormTolerance_requires_norm_kind()
{
    var tol = new FieldNormToleranceSpec("L2", atol: 1e-6, rtol: 1e-4);
    Assert.Equal("L2", tol.Norm);
}
```

- [ ] **Step 2: 跑测试确认红**

Run: `rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~FieldModelTests" --no-build`
Expected: FAIL

- [ ] **Step 3: 写最小模型**

```csharp
public sealed record FieldNormToleranceSpec(string Norm, double Atol, double Rtol) : ToleranceSpec;
```

- [ ] **Step 4: 跑聚焦测试到绿**

Run: `rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~FieldModelTests"`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
rtk git add MetBench_BLL.Core/SystemMT/V12Catalog/Specs MetBench_BLL.Core/SystemMT/V12Catalog/Runtime/Field2DValue.cs MetBench_SystemMT.Tests/SystemMT/V12Catalog/FieldModelTests.cs
rtk git commit -m "feat(v12-pr6): add field value and tolerance model"
```

### Task 2: 实现 FieldEqualityKernel 与 validator

**Files:**
- Create: `MetBench_BLL.Core/SystemMT/V12Catalog/Validation/FieldEqualityPredicateValidator.cs`
- Create: `MetBench_BLL.Core/SystemMT/V12Catalog/Runtime/FieldEqualityKernel.cs`
- Test: `MetBench_SystemMT.Tests/SystemMT/V12Catalog/FieldEqualityKernelTests.cs`

- [ ] **Step 1: 写失败测试**

```csharp
[Fact]
public void Validator_rejects_dimension_mismatch()
{
    var result = Validator().Validate(FieldEquality(), SpecWithFields(rowsLeft: 10, rowsRight: 8));
    Assert.False(result.IsValid);
}
[Fact]
public void Kernel_reports_worst_offender_location()
{
    var result = Kernel().Evaluate(FieldEquality(), ContextWithWorstCell(row: 3, col: 7));
    Assert.Contains("3", result.FailureReason);
    Assert.Contains("7", result.FailureReason);
}
```

- [ ] **Step 2: 跑测试确认红**

Run: `rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~FieldEqualityKernelTests" --no-build`
Expected: FAIL

- [ ] **Step 3: 写最小实现**

```csharp
// apply pairing
// compute norm residual
// capture worst i,j and residual
```

- [ ] **Step 4: 跑聚焦测试到绿**

Run: `rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~FieldEqualityKernelTests"`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
rtk git add MetBench_BLL.Core/SystemMT/V12Catalog/Validation/FieldEqualityPredicateValidator.cs MetBench_BLL.Core/SystemMT/V12Catalog/Runtime/FieldEqualityKernel.cs MetBench_SystemMT.Tests/SystemMT/V12Catalog/FieldEqualityKernelTests.cs
rtk git commit -m "feat(v12-pr6): implement field equality kernel"
```

### Task 3: 实现 DerivedInvariant 与 field-derived operators

**Files:**
- Create: `MetBench_BLL.Core/SystemMT/V12Catalog/Runtime/DerivedInvariantKernel.cs`
- Create: `MetBench_BLL.Core/SystemMT/V12Catalog/Derived/FieldRegionMean.cs`
- Create: `MetBench_BLL.Core/SystemMT/V12Catalog/Derived/ScalarSubtract.cs`
- Create: `MetBench_BLL.Core/SystemMT/V12Catalog/Derived/MassNumberSum.cs`
- Create: `MetBench_BLL.Core/SystemMT/V12Catalog/Derived/L2Norm.cs`
- Create: `MetBench_BLL.Core/SystemMT/V12Catalog/Derived/LinfNorm.cs`
- Test: `MetBench_SystemMT.Tests/SystemMT/V12Catalog/DerivedInvariantKernelTests.cs`

- [ ] **Step 1: 写失败测试**

```csharp
[Fact]
public void Mass_invariant_passes_when_sum_constant()
{
    var result = Kernel().Evaluate(MassInvariant(), ContextWithIsotopes(source: new[] { 2.0, 3.0 }, followup: new[] { 1.0, 4.0 }));
    Assert.True(result.Passed);
}
```

- [ ] **Step 2: 跑测试确认红**

Run: `rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~DerivedInvariantKernelTests" --no-build`
Expected: FAIL

- [ ] **Step 3: 写最小实现**

```csharp
// derived operator registry:
// mass_number_sum, l2_norm, linf_norm, field_region_mean, scalar_subtract
```

- [ ] **Step 4: 跑聚焦和全量测试**

Run:
- `rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~Field|FullyQualifiedName~DerivedInvariant" --no-restore`
- `rtk dotnet test MetBench_SystemMT.Tests --no-restore`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
rtk git add MetBench_BLL.Core/SystemMT/V12Catalog/Runtime MetBench_BLL.Core/SystemMT/V12Catalog/Derived MetBench_SystemMT.Tests/SystemMT/V12Catalog
rtk git commit -m "feat(v12-pr6): add derived invariant and field operators"
```
