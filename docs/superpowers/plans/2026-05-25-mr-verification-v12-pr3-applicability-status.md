# MR Verification v1.2 PR-3 Applicability And Status Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 在 typed runtime 前面补 Applicability 和 5 态结果模型，确保 `SkippedNotApplicable` / `SkippedMissingObservable` / `InvalidSpec` 都有明确的前置分流。

**Architecture:** Applicability 独立于 predicate，发生在 run planning 之前；status 不复用现有 `PipelineStatus` 语义，而是为 v1.2 verification 独立建模，然后映射到 report/diagnostics。

**Tech Stack:** .NET 8 / xUnit

---

### Task 1: 引入 ApplicabilitySpec 和 5 态枚举

**Files:**
- Create: `MetBench_BLL.Core/SystemMT/V12Catalog/Specs/ApplicabilitySpec.cs`
- Create: `MetBench_BLL.Core/SystemMT/V12Catalog/Specs/ConditionExpr.cs`
- Create: `MetBench_BLL.Core/SystemMT/V12Catalog/Specs/RefExpr.cs`
- Create: `MetBench_BLL.Core/SystemMT/V12Catalog/Runtime/VerifyStatus.cs`
- Create: `MetBench_BLL.Core/SystemMT/V12Catalog/Runtime/DiagnosticContext.cs`
- Test: `MetBench_SystemMT.Tests/SystemMT/V12Catalog/ApplicabilityModelTests.cs`

- [ ] **Step 1: 写失败测试**

```csharp
[Fact]
public void VerifyStatus_contains_five_states()
{
    Assert.Contains(VerifyStatus.SkippedNotApplicable, Enum.GetValues<VerifyStatus>());
}
```

- [ ] **Step 2: 跑测试确认红**

Run: `rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~ApplicabilityModelTests" --no-build`
Expected: FAIL

- [ ] **Step 3: 写最小实现**

```csharp
public enum VerifyStatus { Passed, Failed, SkippedNotApplicable, SkippedMissingObservable, InvalidSpec }
```

- [ ] **Step 4: 跑聚焦测试到绿**

Run: `rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~ApplicabilityModelTests"`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
rtk git add MetBench_BLL.Core/SystemMT/V12Catalog/Specs/ApplicabilitySpec.cs MetBench_BLL.Core/SystemMT/V12Catalog/Specs/ConditionExpr.cs MetBench_BLL.Core/SystemMT/V12Catalog/Specs/RefExpr.cs MetBench_BLL.Core/SystemMT/V12Catalog/Runtime/VerifyStatus.cs MetBench_BLL.Core/SystemMT/V12Catalog/Runtime/DiagnosticContext.cs MetBench_SystemMT.Tests/SystemMT/V12Catalog/ApplicabilityModelTests.cs
rtk git commit -m "feat(v12-pr3): add applicability model and verify statuses"
```

### Task 2: 实现 applicability evaluator

**Files:**
- Create: `MetBench_BLL.Core/SystemMT/V12Catalog/Validation/ApplicabilityEvaluator.cs`
- Test: `MetBench_SystemMT.Tests/SystemMT/V12Catalog/ApplicabilityEvaluatorTests.cs`

- [ ] **Step 1: 写失败测试**

```csharp
[Fact]
public void False_applicability_skips_before_run()
{
    var result = Evaluator().Evaluate(new ApplicabilitySpec(/* condition false */), Input("boron_ppm", 1600));
    Assert.False(result.ShouldRun);
}
```

- [ ] **Step 2: 跑测试确认红**

Run: `rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~ApplicabilityEvaluatorTests" --no-build`
Expected: FAIL

- [ ] **Step 3: 写最小实现**

```csharp
public sealed record ApplicabilityDecision(bool ShouldRun, string? Reason);
```

- [ ] **Step 4: 跑聚焦测试到绿**

Run: `rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~ApplicabilityEvaluatorTests"`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
rtk git add MetBench_BLL.Core/SystemMT/V12Catalog/Validation/ApplicabilityEvaluator.cs MetBench_SystemMT.Tests/SystemMT/V12Catalog/ApplicabilityEvaluatorTests.cs
rtk git commit -m "feat(v12-pr3): implement applicability evaluator"
```

### Task 3: 连接到 v1.2 runtime 前置路径

**Files:**
- Modify: `MetBench_BLL.Core/SystemMT/V12Catalog/Runtime/VerificationContext.cs`
- Modify: `MetBench_BLL.Core/SystemMT/V12Catalog/Runtime/PredicateDispatcher.cs`
- Test: `MetBench_SystemMT.Tests/SystemMT/V12Catalog/VerificationStatusFlowTests.cs`

- [ ] **Step 1: 写失败测试**

```csharp
[Fact]
public void Missing_projection_returns_skipped_missing_observable()
{
    var result = Dispatcher().Dispatch(validSpecWithoutMetricOutput, context);
    Assert.Equal(VerifyStatus.SkippedMissingObservable, result.Status);
}
```

- [ ] **Step 2: 跑测试确认红**

Run: `rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~VerificationStatusFlowTests" --no-build`
Expected: FAIL

- [ ] **Step 3: 写最小实现**

```csharp
if (!context.TryGetMetric(...)) return VerificationResult.SkippedMissingObservable(...);
if (!context.SpecValidation.IsValid) return VerificationResult.InvalidSpec(...);
```

- [ ] **Step 4: 跑聚焦和全量测试**

Run:
- `rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~VerificationStatusFlowTests|FullyQualifiedName~Applicability" --no-restore`
- `rtk dotnet test MetBench_SystemMT.Tests --no-restore`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
rtk git add MetBench_BLL.Core/SystemMT/V12Catalog/Runtime MetBench_SystemMT.Tests/SystemMT/V12Catalog/VerificationStatusFlowTests.cs
rtk git commit -m "feat(v12-pr3): route applicability and verification statuses"
```
