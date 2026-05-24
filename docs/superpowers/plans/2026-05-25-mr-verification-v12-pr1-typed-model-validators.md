# MR Verification v1.2 PR-1 Typed Model And Validators Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 在 `PR-0` 的 YAML/catalog foundation 之上，补齐 typed semantic model 与 fail-closed validator，使 `MrSpec.Validate()` / `PropertySpec.Validate()` 成为真正的装载 gate。

**Architecture:** 保持新 IR 在 `MetBench_BLL.Core/SystemMT/V12Catalog/`，把结构类型、引用解析、registry 可达性和 tolerance compatibility 固定为 load-time validation，不触碰当前 `SystemMtLauncher` 执行路径。validator 先只落最小闭环：BinaryComparison、BoundProperty、shared resolver、parameter/tolerance skeleton。

**Tech Stack:** .NET 8 / System.Text.Json polymorphism / xUnit

---

### Task 1: 扩展 typed semantic model 外壳

**Files:**
- Modify: `MetBench_BLL.Core/SystemMT/V12Catalog/Specs/MrSpec.cs`
- Modify: `MetBench_BLL.Core/SystemMT/V12Catalog/Specs/PropertySpec.cs`
- Create: `MetBench_BLL.Core/SystemMT/V12Catalog/Specs/FiveDTags.cs`
- Create: `MetBench_BLL.Core/SystemMT/V12Catalog/Specs/MethodBinding.cs`
- Create: `MetBench_BLL.Core/SystemMT/V12Catalog/Specs/TransformStepSpec.cs`
- Create: `MetBench_BLL.Core/SystemMT/V12Catalog/Specs/ShapeSpec.cs`
- Create: `MetBench_BLL.Core/SystemMT/V12Catalog/Specs/FieldPairing.cs`
- Test: `MetBench_SystemMT.Tests/SystemMT/V12Catalog/V12TypedModelTests.cs`

- [ ] **Step 1: 写失败测试，锁定新模型最小可反序列化和可访问字段**

```csharp
[Fact]
public void MrSpec_exposes_roles_projections_and_predicates()
{
    var spec = V12CatalogSerializer.DeserializeMrSpec(File.ReadAllText(TestAssetPaths.V12MrSample));
    Assert.NotNull(spec.Roles);
    Assert.NotNull(spec.Projections);
    Assert.NotEmpty(spec.Predicates);
}
```

- [ ] **Step 2: 跑聚焦测试确认红**

Run: `rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~V12TypedModelTests" --no-build`
Expected: FAIL，缺少新类型或字段

- [ ] **Step 3: 写最小实现**

```csharp
public sealed record FiveDTags(string EquationKey, string ProgramType, string Pattern, string SourceLevel, string FailureCorrelation);
public sealed record MethodBinding(string MethodCode, string? Variant);
public sealed record TransformStepSpec(string TransformationName, string? TargetPath, IReadOnlyDictionary<string, ParameterExpression>? Parameters);
```

- [ ] **Step 4: 跑聚焦测试到绿**

Run: `rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~V12TypedModelTests"`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
rtk git add MetBench_BLL.Core/SystemMT/V12Catalog/Specs MetBench_SystemMT.Tests/SystemMT/V12Catalog/V12TypedModelTests.cs
rtk git commit -m "feat(v12-pr1): expand typed semantic model shell"
```

### Task 2: 引入 validator contracts 与 shared reference resolver

**Files:**
- Create: `MetBench_BLL.Core/SystemMT/V12Catalog/Validation/ValidationResult.cs`
- Create: `MetBench_BLL.Core/SystemMT/V12Catalog/Validation/ValidationError.cs`
- Create: `MetBench_BLL.Core/SystemMT/V12Catalog/Validation/ISpecValidator.cs`
- Create: `MetBench_BLL.Core/SystemMT/V12Catalog/Validation/IPredicateValidator.cs`
- Create: `MetBench_BLL.Core/SystemMT/V12Catalog/Validation/IPropertyPredicateValidator.cs`
- Create: `MetBench_BLL.Core/SystemMT/V12Catalog/Validation/ValidationRegistry.cs`
- Create: `MetBench_BLL.Core/SystemMT/V12Catalog/Validation/SharedReferenceResolver.cs`
- Test: `MetBench_SystemMT.Tests/SystemMT/V12Catalog/V12ValidationContractTests.cs`

- [ ] **Step 1: 写失败测试，锁定 validator 接口和错误模型**

```csharp
[Fact]
public void ValidationResult_invalid_contains_errors()
{
    var result = ValidationResult.Invalid(new ValidationError("roles.baseline", "Missing role"));
    Assert.False(result.IsValid);
    Assert.Single(result.Errors);
}
```

- [ ] **Step 2: 跑测试确认红**

Run: `rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~V12ValidationContractTests" --no-build`
Expected: FAIL

- [ ] **Step 3: 写最小实现**

```csharp
public interface ISpecValidator<in TSpec> { ValidationResult Validate(TSpec spec); }
public interface IPredicateValidator<in TPredicate, in TSpec> { ValidationResult Validate(TPredicate predicate, TSpec spec); }
public sealed record ValidationError(string Path, string Message);
```

- [ ] **Step 4: 跑聚焦测试到绿**

Run: `rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~V12ValidationContractTests"`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
rtk git add MetBench_BLL.Core/SystemMT/V12Catalog/Validation MetBench_SystemMT.Tests/SystemMT/V12Catalog/V12ValidationContractTests.cs
rtk git commit -m "feat(v12-pr1): add validator contracts and shared resolver"
```

### Task 3: 实现 `MrSpec.Validate()` / `PropertySpec.Validate()` 最小闭环

**Files:**
- Modify: `MetBench_BLL.Core/SystemMT/V12Catalog/Specs/MrSpec.cs`
- Modify: `MetBench_BLL.Core/SystemMT/V12Catalog/Specs/PropertySpec.cs`
- Create: `MetBench_BLL.Core/SystemMT/V12Catalog/Validation/MrSpecValidator.cs`
- Create: `MetBench_BLL.Core/SystemMT/V12Catalog/Validation/PropertySpecValidator.cs`
- Create: `MetBench_BLL.Core/SystemMT/V12Catalog/Validation/BinaryComparisonPredicateValidator.cs`
- Create: `MetBench_BLL.Core/SystemMT/V12Catalog/Validation/BoundPropertyPredicateValidator.cs`
- Create: `MetBench_BLL.Core/SystemMT/V12Catalog/Validation/ToleranceCompatibilityChecker.cs`
- Create: `MetBench_BLL.Core/SystemMT/V12Catalog/Validation/ParameterExpressionResolver.cs`
- Test: `MetBench_SystemMT.Tests/SystemMT/V12Catalog/V12CatalogSemanticValidationTests.cs`

- [ ] **Step 1: 写失败测试，锁定 fail-closed 语义**

```csharp
[Fact]
public void Validate_rejects_missing_role()
{
    var spec = V12CatalogSerializer.DeserializeMrSpec(File.ReadAllText(TestAssetPaths.V12MrSample));
    var broken = spec with { Predicates = new[] { new BinaryComparisonPredicate("p1", "ghost", "baseline", "k_eff", "Less") } };
    var result = broken.Validate();
    Assert.False(result.IsValid);
}
```

- [ ] **Step 2: 跑聚焦测试确认红**

Run: `rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~V12CatalogSemanticValidationTests" --no-build`
Expected: FAIL

- [ ] **Step 3: 写最小实现**

```csharp
public ValidationResult Validate() => new MrSpecValidator(ValidationRegistry.Default).Validate(this);
// checks:
// - role exists
// - metric exists in projections
// - parameter refs resolve
// - tolerance kind compatible
```

- [ ] **Step 4: 跑聚焦测试到绿**

Run: `rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~V12CatalogSemanticValidationTests"`
Expected: PASS

- [ ] **Step 5: 跑全量验收**

Run:
- `rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~V12Catalog" --no-restore`
- `rtk dotnet test MetBench_SystemMT.Tests --no-restore`
Expected: focused PASS, full suite PASS

- [ ] **Step 6: Commit**

```bash
rtk git add MetBench_BLL.Core/SystemMT/V12Catalog MetBench_SystemMT.Tests/SystemMT/V12Catalog
rtk git commit -m "feat(v12-pr1): add fail-closed semantic validators"
```
