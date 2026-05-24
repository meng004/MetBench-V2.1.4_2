# MR Verification v1.2 PR-2 Runtime Scalar Kernels Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 为 typed predicate 引入最小 runtime 执行骨架，先跑通 scalar 比较与缩放等式，不替换现有 launcher 主路径。

**Architecture:** 复用现有 pipeline / runner / parsed output 能力，在 `V12Catalog` 下引入独立的 verification context、dispatcher 和 kernel 接口。runtime 仅消费 `Validate()` 已通过的 spec；任何 InvalidSpec 都在进入 kernel 前被拒绝。

**Tech Stack:** .NET 8 / xUnit / current SystemMT pipeline abstractions

---

### Task 1: 建立 runtime contracts

**Files:**
- Create: `MetBench_BLL.Core/SystemMT/V12Catalog/Runtime/RoleOutput.cs`
- Create: `MetBench_BLL.Core/SystemMT/V12Catalog/Runtime/VerificationContext.cs`
- Create: `MetBench_BLL.Core/SystemMT/V12Catalog/Runtime/IPredicateDispatcher.cs`
- Create: `MetBench_BLL.Core/SystemMT/V12Catalog/Runtime/IVerifierKernel.cs`
- Create: `MetBench_BLL.Core/SystemMT/V12Catalog/Runtime/VerificationDiagnostic.cs`
- Test: `MetBench_SystemMT.Tests/SystemMT/V12Catalog/V12RuntimeContractTests.cs`

- [ ] **Step 1: 写失败测试**

```csharp
[Fact]
public void VerificationContext_requires_validated_spec()
{
    Assert.Throws<ArgumentException>(() => new VerificationContext(null!, new Dictionary<string, RoleOutput>()));
}
```

- [ ] **Step 2: 跑测试确认红**

Run: `rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~V12RuntimeContractTests" --no-build`
Expected: FAIL

- [ ] **Step 3: 写最小 contracts**

```csharp
public interface IVerifierKernel<in TPredicate>
{
    SystemMtAssertionResultV2 Evaluate(TPredicate predicate, VerificationContext context);
}
```

- [ ] **Step 4: 跑测试到绿**

Run: `rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~V12RuntimeContractTests"`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
rtk git add MetBench_BLL.Core/SystemMT/V12Catalog/Runtime MetBench_SystemMT.Tests/SystemMT/V12Catalog/V12RuntimeContractTests.cs
rtk git commit -m "feat(v12-pr2): add runtime contracts for typed predicates"
```

### Task 2: 实现 BinaryComparisonKernel

**Files:**
- Create: `MetBench_BLL.Core/SystemMT/V12Catalog/Runtime/BinaryComparisonKernel.cs`
- Create: `MetBench_BLL.Core/SystemMT/V12Catalog/Runtime/DeterministicScalarToleranceEvaluator.cs`
- Test: `MetBench_SystemMT.Tests/SystemMT/V12Catalog/BinaryComparisonKernelTests.cs`

- [ ] **Step 1: 写失败测试**

```csharp
[Fact]
public void Equal_operator_honors_tolerance()
{
    var result = Kernel().Evaluate(Predicate("Equal"), Context(100.0, 100.001));
    Assert.True(result.Passed);
}
```

- [ ] **Step 2: 跑测试确认红**

Run: `rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~BinaryComparisonKernelTests" --no-build`
Expected: FAIL

- [ ] **Step 3: 写最小实现**

```csharp
switch (predicate.Operator)
{
    case "Greater": ...
    case "Less": ...
    case "Equal": return _tolerance.Within(actual, expected, tolerance);
}
```

- [ ] **Step 4: 跑聚焦测试到绿**

Run: `rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~BinaryComparisonKernelTests"`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
rtk git add MetBench_BLL.Core/SystemMT/V12Catalog/Runtime/BinaryComparisonKernel.cs MetBench_BLL.Core/SystemMT/V12Catalog/Runtime/DeterministicScalarToleranceEvaluator.cs MetBench_SystemMT.Tests/SystemMT/V12Catalog/BinaryComparisonKernelTests.cs
rtk git commit -m "feat(v12-pr2): implement binary comparison kernel"
```

### Task 3: 实现 ScaledEqualityPredicate + dispatcher

**Files:**
- Modify: `MetBench_BLL.Core/SystemMT/V12Catalog/Specs/PredicateSpec.cs`
- Create: `MetBench_BLL.Core/SystemMT/V12Catalog/Runtime/ScaledEqualityKernel.cs`
- Create: `MetBench_BLL.Core/SystemMT/V12Catalog/Runtime/PredicateDispatcher.cs`
- Test: `MetBench_SystemMT.Tests/SystemMT/V12Catalog/ScaledEqualityKernelTests.cs`
- Test: `MetBench_SystemMT.Tests/SystemMT/V12Catalog/PredicateDispatcherTests.cs`

- [ ] **Step 1: 写失败测试**

```csharp
[Fact]
public void ScaledEquality_uses_factor_power_reference()
{
    var result = Kernel().Evaluate(Scaled(alpha: 2.0, exponent: 1.0), Context(10.0, 20.0));
    Assert.True(result.Passed);
}
```

- [ ] **Step 2: 跑测试确认红**

Run: `rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~ScaledEqualityKernelTests|FullyQualifiedName~PredicateDispatcherTests" --no-build`
Expected: FAIL

- [ ] **Step 3: 写最小实现**

```csharp
expected = Math.Pow(factor, exponent) * reference;
residual = Math.Abs(actual - expected);
```

- [ ] **Step 4: 跑聚焦测试到绿**

Run: `rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~ScaledEqualityKernelTests|FullyQualifiedName~PredicateDispatcherTests"`
Expected: PASS

- [ ] **Step 5: 跑全量验收**

Run:
- `rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~V12Catalog|FullyQualifiedName~Assertion" --no-restore`
- `rtk dotnet test MetBench_SystemMT.Tests --no-restore`
Expected: PASS

- [ ] **Step 6: Commit**

```bash
rtk git add MetBench_BLL.Core/SystemMT/V12Catalog/Specs/PredicateSpec.cs MetBench_BLL.Core/SystemMT/V12Catalog/Runtime MetBench_SystemMT.Tests/SystemMT/V12Catalog
rtk git commit -m "feat(v12-pr2): add scaled equality kernel and dispatcher"
```
