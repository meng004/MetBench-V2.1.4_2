# MR Verification v1.2 PR-9 Exponential Growth Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 为 `ExponentialGrowth` shape 提供 runtime evaluator，让 `Kin-Phy-02` 从 schema-valid 升到 executable。

**Architecture:** `ExponentialGrowth` 作为 sequence/property 共用 evaluator，不引入任意数学表达式 DSL，只做正数序列上的 log-linear least squares fit，并输出完整诊断字段。

**Tech Stack:** .NET 8 / xUnit / current sequence shape runtime

---

### Task 1: 建立 fit utility 与模型

**Files:**
- Create: `MetBench_BLL.Core/SystemMT/V12Catalog/Runtime/LogLinearFit.cs`
- Modify: `MetBench_BLL.Core/SystemMT/V12Catalog/Specs/ShapeSpec.cs`
- Test: `MetBench_SystemMT.Tests/SystemMT/V12Catalog/ExponentialGrowthModelTests.cs`

- [ ] **Step 1: 写失败测试**

```csharp
[Fact]
public void ExponentialGrowthSpec_roundtrips_expected_rate_and_thresholds()
{
    var spec = new ExponentialGrowthSpec(new ConstantParameterExpression(0.2), residualRelTolerance: 1e-3, minRSquared: 0.99);
    Assert.Equal(0.99, spec.MinRSquared);
}
```

- [ ] **Step 2: 跑测试确认红**

Run: `rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~ExponentialGrowthModelTests" --no-build`
Expected: FAIL

- [ ] **Step 3: 写最小模型**

```csharp
public sealed record ExponentialGrowthSpec(ParameterExpression ExpectedRate, double ResidualRelTolerance, double MinRSquared) : ShapeSpec;
```

- [ ] **Step 4: 跑聚焦测试到绿**

Run: `rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~ExponentialGrowthModelTests"`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
rtk git add MetBench_BLL.Core/SystemMT/V12Catalog/Runtime/LogLinearFit.cs MetBench_BLL.Core/SystemMT/V12Catalog/Specs/ShapeSpec.cs MetBench_SystemMT.Tests/SystemMT/V12Catalog/ExponentialGrowthModelTests.cs
rtk git commit -m "feat(v12-pr9): add exponential growth model and fit utility"
```

### Task 2: 实现 ExponentialGrowth evaluator

**Files:**
- Modify: `MetBench_BLL.Core/SystemMT/V12Catalog/Runtime/SequenceShapeKernel.cs`
- Modify: `MetBench_BLL.Core/SystemMT/V12Catalog/Property/ShapePropertyChecker.cs`
- Test: `MetBench_SystemMT.Tests/SystemMT/V12Catalog/ExponentialGrowthEvaluatorTests.cs`

- [ ] **Step 1: 写失败测试**

```csharp
[Fact]
public void Perfect_exponential_passes()
{
    var result = Evaluator().Evaluate(Exponential(), Sequence((0.0, 1.0), (1.0, 2.0), (2.0, 4.0)));
    Assert.True(result.Passed);
}
[Fact]
public void Non_positive_sequence_fails_with_clear_diagnostic()
{
    var result = Evaluator().Evaluate(Exponential(), Sequence((0.0, 1.0), (1.0, 0.0), (2.0, 4.0)));
    Assert.False(result.Passed);
    Assert.Contains("positive", result.FailureReason, StringComparison.OrdinalIgnoreCase);
}
```

- [ ] **Step 2: 跑测试确认红**

Run: `rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~ExponentialGrowthEvaluatorTests" --no-build`
Expected: FAIL

- [ ] **Step 3: 写最小实现**

```csharp
z_i = log(y_i)
fit z = a + b*x
rate_residual = abs(b - expectedRate)
```

- [ ] **Step 4: 跑聚焦测试到绿**

Run: `rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~ExponentialGrowthEvaluatorTests"`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
rtk git add MetBench_BLL.Core/SystemMT/V12Catalog/Runtime/SequenceShapeKernel.cs MetBench_BLL.Core/SystemMT/V12Catalog/Property/ShapePropertyChecker.cs MetBench_SystemMT.Tests/SystemMT/V12Catalog/ExponentialGrowthEvaluatorTests.cs
rtk git commit -m "feat(v12-pr9): implement exponential growth evaluator"
```

### Task 3: 启用 Kin-Phy-02 executable fixture

**Files:**
- Modify: `MetBench_SystemMT.Tests/TestAssets/V12Catalog/property/kin-phy-02.yaml`
- Test: `MetBench_SystemMT.Tests/SystemMT/V12Catalog/KinPhy02ExecutableTests.cs`

- [ ] **Step 1: 写失败测试**

```csharp
[Fact]
public void KinPhy02_is_executable_after_pr9()
{
    var result = Checker().Check(KinPhy02(), ContextWithSequence((0.0, 1.0), (1.0, 2.0), (2.0, 4.1)));
    Assert.NotEqual(PropertyStatus.InvalidSpec, result.Status);
}
```

- [ ] **Step 2: 跑测试确认红**

Run: `rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~KinPhy02ExecutableTests" --no-build`
Expected: FAIL

- [ ] **Step 3: 写最小实现与 fixture**

```yaml
kind: PropertySpec
property_id: kin-phy-02
assertions:
  - kind: ShapeProperty
    shape: { kind: ExponentialGrowth, ... }
```

- [ ] **Step 4: 跑聚焦和全量测试**

Run:
- `rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~ExponentialGrowth|FullyQualifiedName~KinPhy02" --no-restore`
- `rtk dotnet test MetBench_SystemMT.Tests --no-restore`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
rtk git add MetBench_SystemMT.Tests/TestAssets/V12Catalog/property/kin-phy-02.yaml MetBench_SystemMT.Tests/SystemMT/V12Catalog/KinPhy02ExecutableTests.cs
rtk git commit -m "feat(v12-pr9): enable kin-phy-02 executable property"
```
