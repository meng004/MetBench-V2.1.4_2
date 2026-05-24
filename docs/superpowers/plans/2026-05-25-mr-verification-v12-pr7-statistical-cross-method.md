# MR Verification v1.2 PR-7 Statistical And Cross Method Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 支持 proportional fit、statistical values/tolerances、variance ratio 和 cross-method comparison，完成 G3 / G5 / G9。

**Architecture:** 统计值与 cross-method 作为新观察类型，独立于 deterministic scalar/field。`MethodBinding` 是 validator 强制要求，不允许从 transform 链推断 solver 差异。

**Tech Stack:** .NET 8 / xUnit

---

### Task 1: 引入 statistical model

**Files:**
- Modify: `MetBench_BLL.Core/SystemMT/V12Catalog/Specs/ProjectionSpec.cs`
- Modify: `MetBench_BLL.Core/SystemMT/V12Catalog/Specs/ToleranceSpec.cs`
- Create: `MetBench_BLL.Core/SystemMT/V12Catalog/Runtime/StatisticalValue.cs`
- Test: `MetBench_SystemMT.Tests/SystemMT/V12Catalog/StatisticalModelTests.cs`

- [ ] **Step 1: 写失败测试**

```csharp
[Fact]
public void StatisticalTolerance_roundtrips_mean_and_std_error()
{
    var projection = new StatisticalProjectionSpec("/mean", "/stderr");
    Assert.Equal("/stderr", projection.StdErrorPath);
}
```

- [ ] **Step 2: 跑测试确认红**

Run: `rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~StatisticalModelTests" --no-build`
Expected: FAIL

- [ ] **Step 3: 写最小实现**

```csharp
public sealed record StatisticalProjectionSpec(string MeanPath, string StdErrorPath) : ProjectionSpec;
public sealed record StatisticalToleranceSpec(double SigmaMultiplier) : ToleranceSpec;
```

- [ ] **Step 4: 跑聚焦测试到绿**

Run: `rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~StatisticalModelTests"`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
rtk git add MetBench_BLL.Core/SystemMT/V12Catalog/Specs MetBench_BLL.Core/SystemMT/V12Catalog/Runtime/StatisticalValue.cs MetBench_SystemMT.Tests/SystemMT/V12Catalog/StatisticalModelTests.cs
rtk git commit -m "feat(v12-pr7): add statistical projection and tolerance model"
```

### Task 2: 实现 VarianceRatio 与 FieldProportionality

**Files:**
- Create: `MetBench_BLL.Core/SystemMT/V12Catalog/Runtime/VarianceRatioKernel.cs`
- Create: `MetBench_BLL.Core/SystemMT/V12Catalog/Runtime/FieldProportionalityKernel.cs`
- Test: `MetBench_SystemMT.Tests/SystemMT/V12Catalog/VarianceRatioKernelTests.cs`
- Test: `MetBench_SystemMT.Tests/SystemMT/V12Catalog/FieldProportionalityKernelTests.cs`

- [ ] **Step 1: 写失败测试**

```csharp
[Fact]
public void VarianceRatio_passes_when_high_sample_has_lower_variance()
{
    var result = Kernel().Evaluate(VarianceRatio(), Context(lowStdError: 0.20, highStdError: 0.10));
    Assert.True(result.Passed);
}
[Fact]
public void FieldProportionality_estimates_constant_ratio()
{
    var result = Kernel().Evaluate(FieldProportionality(), Context(source: new[,] { { 1.0, 2.0 } }, followup: new[,] { { 2.0, 4.0 } }));
    Assert.True(result.Passed);
}
```

- [ ] **Step 2: 跑测试确认红**

Run: `rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~VarianceRatioKernelTests|FullyQualifiedName~FieldProportionalityKernelTests" --no-build`
Expected: FAIL

- [ ] **Step 3: 写最小实现**

```csharp
ratio = followup.StdError / source.StdError;
```

- [ ] **Step 4: 跑聚焦测试到绿**

Run: `rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~VarianceRatioKernelTests|FullyQualifiedName~FieldProportionalityKernelTests"`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
rtk git add MetBench_BLL.Core/SystemMT/V12Catalog/Runtime/VarianceRatioKernel.cs MetBench_BLL.Core/SystemMT/V12Catalog/Runtime/FieldProportionalityKernel.cs MetBench_SystemMT.Tests/SystemMT/V12Catalog/VarianceRatioKernelTests.cs MetBench_SystemMT.Tests/SystemMT/V12Catalog/FieldProportionalityKernelTests.cs
rtk git commit -m "feat(v12-pr7): implement statistical and proportionality kernels"
```

### Task 3: 实现 CrossMethodComparison

**Files:**
- Create: `MetBench_BLL.Core/SystemMT/V12Catalog/Validation/CrossMethodPredicateValidator.cs`
- Create: `MetBench_BLL.Core/SystemMT/V12Catalog/Runtime/CrossMethodComparisonKernel.cs`
- Test: `MetBench_SystemMT.Tests/SystemMT/V12Catalog/CrossMethodComparisonTests.cs`

- [ ] **Step 1: 写失败测试**

```csharp
[Fact]
public void Validator_rejects_missing_method_binding()
{
    var result = Validator().Validate(CrossMethod(), SpecWithoutMethodBinding());
    Assert.False(result.IsValid);
}
```

- [ ] **Step 2: 跑测试确认红**

Run: `rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~CrossMethodComparisonTests" --no-build`
Expected: FAIL

- [ ] **Step 3: 写最小实现**

```csharp
// require MethodBinding on both roles
// compare same metric across methods
```

- [ ] **Step 4: 跑聚焦和全量测试**

Run:
- `rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~CrossMethod|FullyQualifiedName~VarianceRatio|FullyQualifiedName~FieldProportionality" --no-restore`
- `rtk dotnet test MetBench_SystemMT.Tests --no-restore`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
rtk git add MetBench_BLL.Core/SystemMT/V12Catalog/Validation/CrossMethodPredicateValidator.cs MetBench_BLL.Core/SystemMT/V12Catalog/Runtime/CrossMethodComparisonKernel.cs MetBench_SystemMT.Tests/SystemMT/V12Catalog/CrossMethodComparisonTests.cs
rtk git commit -m "feat(v12-pr7): implement cross-method comparison"
```
