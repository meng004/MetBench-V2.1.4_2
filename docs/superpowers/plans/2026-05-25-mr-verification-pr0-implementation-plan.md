# MR Verification PR-0 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 把 `2026-05-25-mr-verification-v1.2-codex-ready.md` 的 PR-0 基础设施落到 MetBench：签入规范、建立 YAML catalog DTO / typed JSON schema / anti-legacy lint / 样例与 CI 校验。

**Architecture:** 这一轮只做 authoring 与 schema 基础，不碰 runtime verifier kernel。现有 `catalog.json` 与 `ManifestMrCatalogProvider` 保持可运行；新增一套平行的 v1.2 catalog foundation，用 schema + lint 先把 typed union 边界和 anti-legacy gate 建起来，为后续 PR-1 typed model 与 validator 铺路。

**Tech Stack:** .NET 8 / System.Text.Json polymorphism / YamlDotNet / xUnit / GitHub Actions

---

## 前置条件

- [x] 设计规范已签入：`docs/superpowers/specs/2026-05-25-mr-verification-v1.2-codex-ready.md`
- [x] 实现范围锁定为 **PR-0**，不跨到 PR-1 runtime / validator 主体
- [x] 当前 worktree 隔离分支：`codex-mr-verification-v12`
- [x] 仓库当前没有 YAML 依赖，需要在本 PR 明确引入

## 验收标准

1. 一份 MR YAML 能反序列化为 typed `MrSpec` DTO。
2. 一份 Property YAML 能反序列化为 typed `PropertySpec` DTO。
3. legacy 字段 `kernel_code` / `role_bindings` / `projection_bindings` / `assertion_name` 会被 schema 或 lint 拒绝。
4. 缺少 discriminator `kind` 的样例会被拒绝。
5. CI 能跑 v1.2 schema/lint 测试并为 green。

## Task 1: 签入规范并建立 PR-0 文件骨架

**Files:**
- Keep: `docs/superpowers/specs/2026-05-25-mr-verification-v1.2-codex-ready.md`
- Create: `MetBench_BLL.Core/SystemMT/V12Catalog/`
- Create: `MetBench_SystemMT.Tests/SystemMT/V12Catalog/`
- Create: `docs/superpowers/plans/2026-05-25-mr-verification-pr0-implementation-plan.md`

- [ ] **Step 1: 盘点并确认 PR-0 只新增基础设施，不替换现有 runtime**

Run: `rtk rg -n "ManifestMrCatalogProvider|MrBindingDefinition|CatalogValidationException" MetBench_BLL.Core/SystemMT MetBench_SystemMT.Tests/SystemMT`
Expected: 现有 catalog / validation 链仍在，作为并行基础存在

- [ ] **Step 2: 建立 v1.2 基础目录**

Create:
- `MetBench_BLL.Core/SystemMT/V12Catalog/Specs/`
- `MetBench_BLL.Core/SystemMT/V12Catalog/Schema/`
- `MetBench_BLL.Core/SystemMT/V12Catalog/Serialization/`
- `MetBench_BLL.Core/SystemMT/V12Catalog/Lint/`
- `MetBench_SystemMT.Tests/SystemMT/V12Catalog/`
- `MetBench_SystemMT.Tests/TestAssets/V12Catalog/`

- [ ] **Step 3: Commit**

```bash
rtk git add docs/superpowers/specs/2026-05-25-mr-verification-v1.2-codex-ready.md docs/superpowers/plans/2026-05-25-mr-verification-pr0-implementation-plan.md
rtk git commit -m "docs(v12): sign in MR verification v1.2 spec and PR-0 plan"
```

## Task 2: 建立 typed DTO 与 polymorphic JSON/YAML 入口

**Files:**
- Create: `MetBench_BLL.Core/SystemMT/V12Catalog/Specs/MrSpec.cs`
- Create: `MetBench_BLL.Core/SystemMT/V12Catalog/Specs/PropertySpec.cs`
- Create: `MetBench_BLL.Core/SystemMT/V12Catalog/Specs/PredicateSpec.cs`
- Create: `MetBench_BLL.Core/SystemMT/V12Catalog/Specs/PropertyPredicateSpec.cs`
- Create: `MetBench_BLL.Core/SystemMT/V12Catalog/Specs/ParameterExpression.cs`
- Create: `MetBench_BLL.Core/SystemMT/V12Catalog/Specs/ToleranceSpec.cs`
- Create: `MetBench_BLL.Core/SystemMT/V12Catalog/Specs/ShapeSpec.cs`
- Create: `MetBench_BLL.Core/SystemMT/V12Catalog/Specs/FieldPairing.cs`
- Create: `MetBench_BLL.Core/SystemMT/V12Catalog/Specs/ProjectionSpec.cs`
- Create: `MetBench_BLL.Core/SystemMT/V12Catalog/Serialization/V12CatalogSerializer.cs`
- Modify: `MetBench_BLL.Core/MetBench_BLL.Core.csproj`
- Test: `MetBench_SystemMT.Tests/SystemMT/V12Catalog/V12CatalogSerializationTests.cs`

- [ ] **Step 1: 写失败测试，锁定 MR/Property YAML 可反序列化**

Test cases:
- `MR YAML -> MrSpec`
- `Property YAML -> PropertySpec`
- `missing kind -> reject`

Run: `rtk dotnet test MetBench_SystemMT.Tests --filter FullyQualifiedName~V12CatalogSerializationTests --no-build`
Expected: FAIL，类型/序列化器尚不存在

- [ ] **Step 2: 引入 YAML 依赖**

Add package to `MetBench_BLL.Core/MetBench_BLL.Core.csproj`:
- `YamlDotNet`

- [ ] **Step 3: 实现最小 typed DTO**

Requirements:
- 只实现 PR-0 需要的 DTO 外壳和 discriminator
- 先覆盖 spec 中最小样例：
  - `MrSpec`
  - `PropertySpec`
  - `BinaryComparisonPredicate`
  - `BoundPropertyPredicate`
  - `DeterministicToleranceSpec`
  - `ConstantParameterExpression`
  - `ScalarProjectionSpec`

- [ ] **Step 4: 实现 YAML -> JSON -> typed object 入口**

Requirements:
- 解析 YAML
- 保留 `kind` discriminator
- 使用 `System.Text.Json` polymorphism 反序列化 typed object

- [ ] **Step 5: 跑测试到通过**

Run: `rtk dotnet test MetBench_SystemMT.Tests --filter FullyQualifiedName~V12CatalogSerializationTests`
Expected: PASS

- [ ] **Step 6: Commit**

```bash
rtk git add MetBench_BLL.Core/MetBench_BLL.Core.csproj MetBench_BLL.Core/SystemMT/V12Catalog MetBench_SystemMT.Tests/SystemMT/V12Catalog
rtk git commit -m "feat(v12): add typed catalog DTOs and YAML serialization entry"
```

## Task 3: 建立 schema + anti-legacy lint

**Files:**
- Create: `MetBench_BLL.Core/SystemMT/V12Catalog/Schema/V12CatalogSchema.cs`
- Create: `MetBench_BLL.Core/SystemMT/V12Catalog/Lint/V12CatalogAntiLegacyLinter.cs`
- Create: `MetBench_SystemMT.Tests/SystemMT/V12Catalog/V12CatalogSchemaTests.cs`
- Create: `MetBench_SystemMT.Tests/SystemMT/V12Catalog/V12CatalogLintTests.cs`
- Create: `MetBench_SystemMT.Tests/TestAssets/V12Catalog/samples/*.yaml`
- Create: `MetBench_SystemMT.Tests/TestAssets/V12Catalog/invalid/*.yaml`

- [ ] **Step 1: 写失败测试，锁定 legacy 字段与 missing kind 被拒绝**

Cases:
- `kernel_code`
- `role_bindings`
- `projection_bindings`
- `assertion_name`
- missing top-level `kind`
- missing predicate `kind`

Run: `rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~V12CatalogSchemaTests|FullyQualifiedName~V12CatalogLintTests" --no-build`
Expected: FAIL

- [ ] **Step 2: 实现 schema 生成/检查最小骨架**

Requirements:
- 不必一次生成完整 JSON Schema 文件
- 但必须有可执行的 schema/structural gate
- 明确 oneOf/discriminator 边界

- [ ] **Step 3: 实现 anti-legacy lint**

Rules:
- reject `kernel_code`
- reject `role_bindings`
- reject `projection_bindings`
- reject `assertion_name`

- [ ] **Step 4: 增加一份 MR sample 与一份 Property sample**

Files:
- `mr-sample.yaml`
- `property-sample.yaml`

- [ ] **Step 5: 跑测试到通过**

Run: `rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~V12CatalogSchemaTests|FullyQualifiedName~V12CatalogLintTests"`
Expected: PASS

- [ ] **Step 6: Commit**

```bash
rtk git add MetBench_BLL.Core/SystemMT/V12Catalog/Schema MetBench_BLL.Core/SystemMT/V12Catalog/Lint MetBench_SystemMT.Tests/SystemMT/V12Catalog MetBench_SystemMT.Tests/TestAssets/V12Catalog
rtk git commit -m "feat(v12): add schema gate and anti-legacy catalog lint"
```

## Task 4: 接入 CI 与验收回归

**Files:**
- Modify: `.github/workflows/dotnet-test.yml`
- Test: existing + new v1.2 test suites

- [ ] **Step 1: 找出现有 CI 中最合适的接入点**

Run: `rtk sed -n '1,240p' .github/workflows/dotnet-test.yml`
Expected: 现有测试流程可复用

- [ ] **Step 2: 确保 v1.2 tests 被默认 `dotnet test MetBench_SystemMT.Tests` 覆盖**

Implementation:
- 若现有 workflow 已跑整个 `MetBench_SystemMT.Tests`，则无需特殊命令
- 只需保证新测试资产进入测试项目

- [ ] **Step 3: 执行本地验收测试**

Run:
- `rtk dotnet build MetBench_SystemMT.Tests --no-restore -m:1`
- `rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~V12Catalog"`
- `rtk dotnet test MetBench_SystemMT.Tests --no-restore`

Expected:
- build PASS
- focused PASS
- full suite PASS

- [ ] **Step 4: Commit**

```bash
rtk git add .github/workflows/dotnet-test.yml MetBench_SystemMT.Tests
rtk git commit -m "test(v12): wire PR-0 catalog schema checks into CI"
```

## Task 5: Review、push、CI、merge

**Files:**
- Review diff only

- [ ] **Step 1: 自检**

Run:
- `rtk git diff --stat main..HEAD`
- `rtk git diff --check`

Expected: no whitespace / patch issues

- [ ] **Step 2: 代码 review**

Review checklist:
- 是否保持 PR-0 范围，没有提前实现 runtime kernel
- schema/lint 是否 fail-closed
- legacy 字段是否被拒绝
- typed DTO 是否没有回退到 `Dictionary<string,string>` predicate

- [ ] **Step 3: push**

```bash
rtk git push origin codex-mr-verification-v12
```

- [ ] **Step 4: 创建 PR**

PR title:
- `feat(v12): add MR verification PR-0 catalog foundation`

- [ ] **Step 5: 等待 CI green 后 merge**

Acceptance:
- GitHub required checks green
- PR review无阻塞
- merge 到 `main`

---

## 本次范围外

- 不实现 `MrSpec.Validate()` / `PropertySpec.Validate()` 语义 validator 主体
- 不实现 `IVerifierKernel<TPredicate>` runtime
- 不做 43 MR + 4 Property 全量 migration
- 不做 `ExponentialGrowth` runtime

