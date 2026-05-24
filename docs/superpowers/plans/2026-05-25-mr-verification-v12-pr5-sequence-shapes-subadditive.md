# MR Verification v1.2 PR-5 Sequence Shapes And Subadditive Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 支持 sequence projections、5 种 shape evaluator、SubadditivePredicate 和 finite-difference / coefficient-of-variation 派生表达式。

**Architecture:** 序列值与 shape evaluator 保持独立于 property checker，先作为 MR runtime 复用层。`ExponentialGrowth` 这一轮只允许 schema/validator 表达，不允许 runtime evaluator 落地。

**Tech Stack:** .NET 8 / xUnit

---

### Task 1: 引入 SequenceValue 与 ShapeSpec 子类

**Files:**
- Modify: `MetBench_BLL.Core/SystemMT/V12Catalog/Specs/ProjectionSpec.cs`
- Modify: `MetBench_BLL.Core/SystemMT/V12Catalog/Specs/ShapeSpec.cs`
- Create: `MetBench_BLL.Core/SystemMT/V12Catalog/Runtime/SequenceValue.cs`
- Test: `MetBench_SystemMT.Tests/SystemMT/V12Catalog/SequenceShapeModelTests.cs`

- [ ] **Step 1: 写失败测试**

```csharp
[Theory]
[InlineData("BellShape")]
[InlineData("SShape")]
[InlineData("SignChange")]
[InlineData("NonMonotonic")]
[InlineData("ConstantSlope")]
public void ShapeSpec_roundtrips(string kind)
{
    var yaml = $$"""
    kind: PropertySpec
    property_id: shape-smoke
    name: shape-smoke
    assertions:
      - kind: ShapeProperty
        predicate_id: shape-check
        metric: sequence_metric
        shape: { kind: {{kind}} }
    """;
    var spec = V12CatalogSerializer.DeserializePropertySpec(yaml);
    Assert.Equal(kind, spec.Assertions[0].Kind);
}
```

- [ ] **Step 2: 跑测试确认红**

Run: `rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~SequenceShapeModelTests" --no-build`
Expected: FAIL

- [ ] **Step 3: 写最小模型**

```csharp
public sealed record SequenceProjectionSpec(string Path) : ProjectionSpec;
public sealed record BellShapeSpec() : ShapeSpec;
```

- [ ] **Step 4: 跑聚焦测试到绿**

Run: `rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~SequenceShapeModelTests"`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
rtk git add MetBench_BLL.Core/SystemMT/V12Catalog/Specs MetBench_BLL.Core/SystemMT/V12Catalog/Runtime/SequenceValue.cs MetBench_SystemMT.Tests/SystemMT/V12Catalog/SequenceShapeModelTests.cs
rtk git commit -m "feat(v12-pr5): add sequence and shape model"
```

### Task 2: 实现五类 shape evaluator

**Files:**
- Create: `MetBench_BLL.Core/SystemMT/V12Catalog/Runtime/SequenceShapeKernel.cs`
- Test: `MetBench_SystemMT.Tests/SystemMT/V12Catalog/SequenceShapeKernelTests.cs`

- [ ] **Step 1: 写失败测试**

```csharp
[Fact]
public void BellShape_passes_on_peak_in_middle()
{
    var result = Kernel().Evaluate(BellShape(), Sequence(1.0, 3.0, 5.0, 3.0, 1.0));
    Assert.True(result.Passed);
}
[Fact]
public void SignChange_passes_on_crossing_zero()
{
    var result = Kernel().Evaluate(SignChange(), Sequence(2.0, 1.0, -1.0, -3.0));
    Assert.True(result.Passed);
}
```

- [ ] **Step 2: 跑测试确认红**

Run: `rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~SequenceShapeKernelTests" --no-build`
Expected: FAIL

- [ ] **Step 3: 写最小实现**

```csharp
// compute first differences
// evaluate each shape independently
// emit sample_count and worst_point diagnostics
```

- [ ] **Step 4: 跑聚焦测试到绿**

Run: `rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~SequenceShapeKernelTests"`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
rtk git add MetBench_BLL.Core/SystemMT/V12Catalog/Runtime/SequenceShapeKernel.cs MetBench_SystemMT.Tests/SystemMT/V12Catalog/SequenceShapeKernelTests.cs
rtk git commit -m "feat(v12-pr5): implement sequence shape evaluator"
```

### Task 3: 实现 Subadditive 和 derived expressions

**Files:**
- Modify: `MetBench_BLL.Core/SystemMT/V12Catalog/Specs/PredicateSpec.cs`
- Create: `MetBench_BLL.Core/SystemMT/V12Catalog/Runtime/SubadditiveKernel.cs`
- Create: `MetBench_BLL.Core/SystemMT/V12Catalog/Derived/FiniteDifferenceDerivedExpression.cs`
- Create: `MetBench_BLL.Core/SystemMT/V12Catalog/Derived/CoefficientOfVariation.cs`
- Test: `MetBench_SystemMT.Tests/SystemMT/V12Catalog/SubadditiveKernelTests.cs`
- Test: `MetBench_SystemMT.Tests/SystemMT/V12Catalog/DerivedExpressionTests.cs`

- [ ] **Step 1: 写失败测试**

```csharp
[Fact]
public void Subadditive_passes_when_combined_effect_is_smaller()
{
    var result = Kernel().Evaluate(Subadditive(), Effects(deltaA: 10.0, deltaB: 6.0, deltaAB: 13.0));
    Assert.True(result.Passed);
}
```

- [ ] **Step 2: 跑测试确认红**

Run: `rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~SubadditiveKernelTests|FullyQualifiedName~DerivedExpressionTests" --no-build`
Expected: FAIL

- [ ] **Step 3: 写最小实现**

```csharp
delta_ab <= delta_a + delta_b
```

- [ ] **Step 4: 跑聚焦和全量测试**

Run:
- `rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~Sequence|FullyQualifiedName~Subadditive|FullyQualifiedName~DerivedExpression" --no-restore`
- `rtk dotnet test MetBench_SystemMT.Tests --no-restore`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
rtk git add MetBench_BLL.Core/SystemMT/V12Catalog MetBench_SystemMT.Tests/SystemMT/V12Catalog
rtk git commit -m "feat(v12-pr5): add subadditive and sequence derived expressions"
```
