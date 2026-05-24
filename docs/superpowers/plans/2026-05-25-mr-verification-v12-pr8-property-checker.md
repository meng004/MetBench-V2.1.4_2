# MR Verification v1.2 PR-8 Property Checker Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 为 `PropertySpec` 建独立 runtime path，先交付 3 个可执行 property，并确保 property coverage 与 MR coverage 严格分离。

**Architecture:** Property checker 不复用 MR run planner，不引入隐式 `$only` role；它直接消费单次输出观测和 derived operators。目录、结果类型、coverage 报表都和 MR 分开。

**Tech Stack:** .NET 8 / xUnit

---

### Task 1: 建立 property runtime contracts

**Files:**
- Create: `MetBench_BLL.Core/SystemMT/V12Catalog/Property/IPropertyChecker.cs`
- Create: `MetBench_BLL.Core/SystemMT/V12Catalog/Property/PropertyVerificationContext.cs`
- Create: `MetBench_BLL.Core/SystemMT/V12Catalog/Property/PropertyResult.cs`
- Create: `MetBench_BLL.Core/SystemMT/V12Catalog/Property/PropertyStatus.cs`
- Test: `MetBench_SystemMT.Tests/SystemMT/V12Catalog/PropertyRuntimeContractTests.cs`

- [ ] **Step 1: 写失败测试**

```csharp
[Fact]
public void PropertyStatus_is_separate_from_mr_status()
{
    Assert.DoesNotContain("SkippedNotApplicable", Enum.GetNames<PropertyStatus>());
}
```

- [ ] **Step 2: 跑测试确认红**

Run: `rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~PropertyRuntimeContractTests" --no-build`
Expected: FAIL

- [ ] **Step 3: 写最小实现**

```csharp
public interface IPropertyChecker { PropertyResult Check(PropertySpec spec, PropertyVerificationContext context); }
```

- [ ] **Step 4: 跑聚焦测试到绿**

Run: `rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~PropertyRuntimeContractTests"`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
rtk git add MetBench_BLL.Core/SystemMT/V12Catalog/Property MetBench_SystemMT.Tests/SystemMT/V12Catalog/PropertyRuntimeContractTests.cs
rtk git commit -m "feat(v12-pr8): add property runtime contracts"
```

### Task 2: 实现 BoundProperty 与 ShapeProperty checker

**Files:**
- Create: `MetBench_BLL.Core/SystemMT/V12Catalog/Property/BoundPropertyChecker.cs`
- Create: `MetBench_BLL.Core/SystemMT/V12Catalog/Property/ShapePropertyChecker.cs`
- Test: `MetBench_SystemMT.Tests/SystemMT/V12Catalog/BoundPropertyCheckerTests.cs`
- Test: `MetBench_SystemMT.Tests/SystemMT/V12Catalog/ShapePropertyCheckerTests.cs`

- [ ] **Step 1: 写失败测试**

```csharp
[Fact]
public void Range_property_supports_two_bound_assertions()
{
    var result = Checker().Check(RangeProperty(), ContextWithScalar("dancoff", 0.42));
    Assert.True(result.Passed);
    Assert.Equal(2, result.PredicateResults.Count);
}
```

- [ ] **Step 2: 跑测试确认红**

Run: `rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~BoundPropertyCheckerTests|FullyQualifiedName~ShapePropertyCheckerTests" --no-build`
Expected: FAIL

- [ ] **Step 3: 写最小实现**

```csharp
// BoundProperty reuses scalar comparison semantics
// ShapeProperty reuses sequence shape evaluator
```

- [ ] **Step 4: 跑聚焦测试到绿**

Run: `rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~BoundPropertyCheckerTests|FullyQualifiedName~ShapePropertyCheckerTests"`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
rtk git add MetBench_BLL.Core/SystemMT/V12Catalog/Property/BoundPropertyChecker.cs MetBench_BLL.Core/SystemMT/V12Catalog/Property/ShapePropertyChecker.cs MetBench_SystemMT.Tests/SystemMT/V12Catalog/BoundPropertyCheckerTests.cs MetBench_SystemMT.Tests/SystemMT/V12Catalog/ShapePropertyCheckerTests.cs
rtk git commit -m "feat(v12-pr8): implement property checkers"
```

### Task 3: 落 3 个 executable property 与分离式 coverage

**Files:**
- Create: `MetBench_SystemMT.Tests/TestAssets/V12Catalog/property/dif-phy-08.yaml`
- Create: `MetBench_SystemMT.Tests/TestAssets/V12Catalog/property/bur-phy-04.yaml`
- Create: `MetBench_SystemMT.Tests/TestAssets/V12Catalog/property/res-alg-03.yaml`
- Create: `MetBench_SystemMT.Tests/TestAssets/V12Catalog/property/kin-phy-02.yaml`
- Test: `MetBench_SystemMT.Tests/SystemMT/V12Catalog/PropertyCoverageSeparationTests.cs`
- Test: `MetBench_SystemMT.Tests/SystemMT/V12Catalog/ExecutablePropertyFixturesTests.cs`

- [ ] **Step 1: 写失败测试**

```csharp
[Fact]
public void Property_catalog_entries_are_not_counted_as_mr_entries()
{
    var report = CoverageReport.Build(mrCount: 43, propertyCount: 4);
    Assert.Equal(43, report.MrCount);
    Assert.Equal(4, report.PropertyCount);
}
```

- [ ] **Step 2: 跑测试确认红**

Run: `rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~PropertyCoverageSeparationTests|FullyQualifiedName~ExecutablePropertyFixturesTests" --no-build`
Expected: FAIL

- [ ] **Step 3: 写最小实现与样例**

```yaml
kind: PropertySpec
property_id: res-alg-03
assertions:
  - kind: BoundProperty
```

- [ ] **Step 4: 跑聚焦和全量测试**

Run:
- `rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~Property" --no-restore`
- `rtk dotnet test MetBench_SystemMT.Tests --no-restore`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
rtk git add MetBench_SystemMT.Tests/TestAssets/V12Catalog/property MetBench_SystemMT.Tests/SystemMT/V12Catalog
rtk git commit -m "feat(v12-pr8): add executable property fixtures and coverage separation"
```
