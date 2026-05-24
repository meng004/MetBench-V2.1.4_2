# MR Verification v1.2 PR-4 Reference Convergence Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 支持 reference role、ordered roles 和 `ErrorMonotonicPredicate`，覆盖 G4 收敛型 MR。

**Architecture:** 在 typed role model 上引入显式 `ReferenceRole` 与 ordered role sequence，不允许依赖 dictionary 枚举顺序。误差单调性由 validator 保证 role 结构正确，再由 kernel 对相邻 pair 与 reference 计算 diagnostics。

**Tech Stack:** .NET 8 / xUnit

---

### Task 1: 扩展 role model 与 predicate shape

**Files:**
- Modify: `MetBench_BLL.Core/SystemMT/V12Catalog/Specs/MrSpec.cs`
- Modify: `MetBench_BLL.Core/SystemMT/V12Catalog/Specs/PredicateSpec.cs`
- Create: `MetBench_BLL.Core/SystemMT/V12Catalog/Specs/NormKind.cs`
- Test: `MetBench_SystemMT.Tests/SystemMT/V12Catalog/ReferenceRoleModelTests.cs`

- [ ] **Step 1: 写失败测试**

```csharp
[Fact]
public void ErrorMonotonic_requires_ordered_roles_and_reference()
{
    var predicate = new ErrorMonotonicPredicate("p1", new[] { "coarse", "fine" }, "reference", "k_eff", NormKind.Absolute);
    Assert.Equal("reference", predicate.ReferenceRole);
}
```

- [ ] **Step 2: 跑测试确认红**

Run: `rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~ReferenceRoleModelTests" --no-build`
Expected: FAIL

- [ ] **Step 3: 写最小模型**

```csharp
public enum NormKind { Absolute, Relative, L2, Linf }
```

- [ ] **Step 4: 跑聚焦测试到绿**

Run: `rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~ReferenceRoleModelTests"`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
rtk git add MetBench_BLL.Core/SystemMT/V12Catalog/Specs MetBench_SystemMT.Tests/SystemMT/V12Catalog/ReferenceRoleModelTests.cs
rtk git commit -m "feat(v12-pr4): add reference role and convergence model"
```

### Task 2: 实现 validator

**Files:**
- Create: `MetBench_BLL.Core/SystemMT/V12Catalog/Validation/ErrorMonotonicPredicateValidator.cs`
- Test: `MetBench_SystemMT.Tests/SystemMT/V12Catalog/ErrorMonotonicPredicateValidatorTests.cs`

- [ ] **Step 1: 写失败测试**

```csharp
[Fact]
public void Validate_rejects_duplicate_ordered_roles()
{
    var result = Validator().Validate(Predicate("coarse", "coarse"), spec);
    Assert.False(result.IsValid);
}
```

- [ ] **Step 2: 跑测试确认红**

Run: `rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~ErrorMonotonicPredicateValidatorTests" --no-build`
Expected: FAIL

- [ ] **Step 3: 写最小实现**

```csharp
// checks:
// ordered roles count >= 2
// reference role exists
// all metrics exist
```

- [ ] **Step 4: 跑聚焦测试到绿**

Run: `rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~ErrorMonotonicPredicateValidatorTests"`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
rtk git add MetBench_BLL.Core/SystemMT/V12Catalog/Validation/ErrorMonotonicPredicateValidator.cs MetBench_SystemMT.Tests/SystemMT/V12Catalog/ErrorMonotonicPredicateValidatorTests.cs
rtk git commit -m "feat(v12-pr4): validate error monotonic predicates"
```

### Task 3: 实现 kernel 与 diagnostics

**Files:**
- Create: `MetBench_BLL.Core/SystemMT/V12Catalog/Runtime/ErrorMonotonicKernel.cs`
- Test: `MetBench_SystemMT.Tests/SystemMT/V12Catalog/ErrorMonotonicKernelTests.cs`

- [ ] **Step 1: 写失败测试**

```csharp
[Fact]
public void Non_monotonic_error_reports_offending_pair()
{
    var result = Kernel().Evaluate(predicate, context);
    Assert.False(result.Passed);
    Assert.Contains("coarse->fine", result.FailureReason);
}
```

- [ ] **Step 2: 跑测试确认红**

Run: `rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~ErrorMonotonicKernelTests" --no-build`
Expected: FAIL

- [ ] **Step 3: 写最小实现**

```csharp
// compute error(role_i, reference)
// require error[i+1] <= error[i]
// capture offending pair in diagnostics
```

- [ ] **Step 4: 跑聚焦和全量测试**

Run:
- `rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~ErrorMonotonic" --no-restore`
- `rtk dotnet test MetBench_SystemMT.Tests --no-restore`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
rtk git add MetBench_BLL.Core/SystemMT/V12Catalog/Runtime/ErrorMonotonicKernel.cs MetBench_SystemMT.Tests/SystemMT/V12Catalog/ErrorMonotonicKernelTests.cs
rtk git commit -m "feat(v12-pr4): implement convergence kernel"
```
