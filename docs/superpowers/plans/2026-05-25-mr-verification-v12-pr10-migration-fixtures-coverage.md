# MR Verification v1.2 PR-10 Migration Fixtures Coverage Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 完成 43 MR + 4 Property 全量迁移、golden fixtures、coverage report 和 CI gate，把 v1.2 从局部能力收口为完整 catalog 体系。

**Architecture:** PR-10 只消费 PR-1..PR-9 已稳定的 typed model、validators、kernels 和 property checker，不再改共享接口。迁移、fixture、coverage gate 和 CI gate 必须一起进，不允许“先迁 catalog，后补 gate”。

**Tech Stack:** .NET 8 / xUnit / GitHub Actions / TRX / JSON/YAML catalog

---

### Task 1: 迁移 catalog 到 typed schema

**Files:**
- Create: `SUT/*/catalog-v12/*.yaml`
- Create: `MetBench_SystemMT.Tests/TestAssets/V12Catalog/migration/`
- Test: `MetBench_SystemMT.Tests/SystemMT/V12Catalog/V12CatalogMigrationTests.cs`

- [ ] **Step 1: 写失败测试**

```csharp
[Fact]
public void All_43_mr_and_4_property_entries_deserialize_and_validate()
{
    var report = MigrationLoader.LoadAll();
    Assert.Equal(43, report.ValidMrSpecs);
    Assert.Equal(4, report.ValidPropertySpecs);
}
```

- [ ] **Step 2: 跑测试确认红**

Run: `rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~V12CatalogMigrationTests" --no-build`
Expected: FAIL

- [ ] **Step 3: 写最小迁移资产**

```text
43 MR YAML files
4 Property YAML files
no assertion_name
no kernel_code
```

- [ ] **Step 4: 跑聚焦测试到绿**

Run: `rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~V12CatalogMigrationTests"`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
rtk git add SUT MetBench_SystemMT.Tests/TestAssets/V12Catalog/migration MetBench_SystemMT.Tests/SystemMT/V12Catalog/V12CatalogMigrationTests.cs
rtk git commit -m "feat(v12-pr10): migrate catalog to typed v12 schema"
```

### Task 2: 建 golden validation / execution fixtures

**Files:**
- Create: `MetBench_SystemMT.Tests/TestAssets/V12Catalog/golden/pass/`
- Create: `MetBench_SystemMT.Tests/TestAssets/V12Catalog/golden/fail/`
- Create: `MetBench_SystemMT.Tests/TestAssets/V12Catalog/golden/missing/`
- Create: `MetBench_SystemMT.Tests/TestAssets/V12Catalog/golden/invalid/`
- Test: `MetBench_SystemMT.Tests/SystemMT/V12Catalog/V12GoldenFixtureTests.cs`

- [ ] **Step 1: 写失败测试**

```csharp
[Fact]
public void Golden_fixtures_cover_pass_fail_missing_and_invalid()
{
    var fixtures = GoldenFixtureInventory.Load();
    Assert.Contains("pass", fixtures.Buckets);
    Assert.Contains("invalid", fixtures.Buckets);
}
```

- [ ] **Step 2: 跑测试确认红**

Run: `rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~V12GoldenFixtureTests" --no-build`
Expected: FAIL

- [ ] **Step 3: 写最小 fixtures**

```text
pass: runnable valid sample
fail: valid spec but failing relation
missing: projection absent -> SkippedMissingObservable
invalid: bad spec -> InvalidSpec
```

- [ ] **Step 4: 跑聚焦测试到绿**

Run: `rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~V12GoldenFixtureTests"`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
rtk git add MetBench_SystemMT.Tests/TestAssets/V12Catalog/golden MetBench_SystemMT.Tests/SystemMT/V12Catalog/V12GoldenFixtureTests.cs
rtk git commit -m "test(v12-pr10): add golden fixtures for all verification states"
```

### Task 3: 接 coverage report 与 CI gate

**Files:**
- Modify: `.github/workflows/dotnet-test.yml`
- Create: `MetBench_SystemMT.Tests/SystemMT/V12Catalog/V12CoverageGateTests.cs`
- Create: `docs/uat/reports/v12-coverage-dashboard.md`

- [ ] **Step 1: 写失败测试**

```csharp
[Fact]
public void Coverage_denominators_are_43_mr_and_4_property()
{
    var report = CoverageReport.Build(mrCount: 43, propertyCount: 4);
    Assert.Equal(43, report.MrCount);
    Assert.Equal(4, report.PropertyCount);
}
```

- [ ] **Step 2: 跑测试确认红**

Run: `rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~V12CoverageGateTests" --no-build`
Expected: FAIL

- [ ] **Step 3: 写最小 gate**

```text
CI must assert:
- all catalog deserialize
- all Validate() pass
- no legacy fields
- registry complete
- MR denominator 43
- Property denominator 4
- InvalidSpec == 0
```

- [ ] **Step 4: 跑全量验收**

Run:
- `rtk dotnet build MetBench_SystemMT.Tests --no-restore -m:1`
- `rtk dotnet test MetBench_SystemMT.Tests --no-restore`
Expected: PASS

- [ ] **Step 5: 做最终 review**

Run:
- `rtk git diff --check`
- `rtk rg -n "kernel_code|assertion_name|role_bindings|projection_bindings" SUT MetBench_SystemMT.Tests/TestAssets/V12Catalog`
Expected: no patch issues; legacy fields only remain in explicit negative fixtures

- [ ] **Step 6: Commit**

```bash
rtk git add .github/workflows/dotnet-test.yml MetBench_SystemMT.Tests/SystemMT/V12Catalog/V12CoverageGateTests.cs docs/uat/reports/v12-coverage-dashboard.md
rtk git commit -m "test(v12-pr10): add migration coverage gates and reporting"
```
