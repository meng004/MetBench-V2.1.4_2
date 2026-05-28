# P1 — Catalog-Derived Counts + ID Whitelist (Module B)

> **Date**: 2026-05-28
> **Status**: Draft (writing-plan stage)
> **Charter**: `docs/superpowers/specs/2026-05-28-code-governance-v2-charter.md` §3 模块 B, §6 P1 行, §4 G7 退役行
> **Scope tag**: v2 章程 P1
> **Implements**: 元规则 R1（cross-projection parity 强化）— 见 §3 模块 G

---

## §1 目标 & 验收

**目标**：消除 v2 charter §5 矩阵中"Cat B — N-bump pinned-count 漂移"这一整类缺陷。当前任意 PR 给 `LegacyCatalogFactory.cs` + `SUT/*/catalog.json` 加一个 MR 必须手工同步 bump **34 处** `Assert.Equal(33|16|13, …)`，全凭 G7 advisory grep 提醒，仍然 Cat B 漂移频发（PR-N2 / PR-Bol-2B / PR-T3-8 三次中招）。本计划把这 34 处 literal 改成"读单一事实源 whitelist"，并退役 G7 grep。落点：v2 章程模块 B（机械模式守卫）。

**验收（§9 复述）**：CI test 绿；governance job 不再出现 G7 输出；whitelist 文件存在；34 处 literal 全部改完；至少 5 个 test class 受益；新增 MR 时只需改 1 处 whitelist + 1 处 LegacyCatalogFactory.cs + 1 处 manifest catalog.json，CI 自动放行。

---

## §2 当前状态 inventory

实测命令：`grep -rn 'Assert\.Equal(\(33\|16\|13\),' MetBench_SystemMT.Tests/ --include='*.cs'`

**34 处 pinned literal，6 个 test file**：

| # | 文件 | 行号 | literal | 语义 |
|---|---|---:|---:|---|
| 1 | `MetBench_SystemMT.Tests/SystemMT/Catalog/CatalogParityTests.cs` | 32 | 33 | hardcoded.Count |
| 2 | `MetBench_SystemMT.Tests/SystemMT/Catalog/HardcodedMrCatalogProviderTests.cs` | 22 | 33 | entries.Count |
| 3 | 同上 | 33 | 16 | distinctSuts.Count |
| 4-7 | `MetBench_SystemMT.Tests/SystemMT/Bootstrap/SystemMtBootstrapTests.cs` | 52, 53, 54, 55 | 13/33/13/33 | EquationsSeeded / MrsSeeded / ListEquations / ListMrs |
| 8-10 | 同上 | 59, 60, 61 | 16/33/33 | ApplicationsCreated / MrsCreated / BindingsCreated |
| 11-14 | 同上 | 73, 74, 78, 80 | 13/33/16/33 | second-call (idempotency) |
| 15-16 | 同上 | 93, 101 | 16/13 | skip-importer / skip-metadata 分支 |
| 17 | 同上 | 102 | 33 | MrsSeeded skip-importer |
| 18-19 | 同上 | 122, 123 | 33/33 | V3MigrationSummary.Created / v3.Data.Count |
| 20-22 | `MetBench_SystemMT.Tests/SystemMT/Launcher/LauncherCatalogV2ImporterTests.cs` | 82, 83 | 16/16 | ApplicationsCreated / _apps.Data.Count |
| 23-25 | 同上 | 97, 98, 114 | 33/33/33 | MrsCreated / _mrs.Data.Count / _bindings.Data.Count |
| 26-29 | 同上 | 193, 194, 195 | 16/33/33 | first call importer |
| 30-32 | 同上 | 199, 201, 203 | 16/33/33 | second call existing |
| 33-35 | 同上 | 206, 207, 208 | 16/33/33 | final state |
| 36 | `MetBench_SystemMT.Tests/SystemMT/Launcher/SystemMtLauncherTests.cs` | 75 | 33 | descriptors.Count |
| 37 | `MetBench_SystemMT.Tests/SystemMT/Launcher/SystemMtLauncherProviderInjectionTests.cs` | 110 | 33 | items.Count via manifest provider |

实际计 37 行（部分行号属于扩展场景），收敛去重后 **34 项独立断言点 across 6 文件**。

**显式排除（v1.2 typed-migration coverage denominator，不归 P1）**：
- `MetBench_SystemMT.Tests/SystemMT/Catalog/Typed/V12CoverageGateTests.cs:13,14,17,18` — `Assert.Equal(44, report.MrCount)` / `Assert.Equal(4, PropertyCount)` / `Assert.Equal(3, ...)`：v1.2 inventory denominator，另一条 fact 路径。
- `MetBench_SystemMT.Tests/SystemMT/Catalog/Typed/PropertyCoverageSeparationTests.cs:13,14` — 同上。
- `MetBench_SystemMT.Tests/SystemMT/Catalog/Typed/TypedCatalogMigrationTests.cs:16,17` — `ValidMrSpecs=44`，同上。

**Source-of-truth 两源一致性已存在守卫**（v2 §8 Stop 检查通过）：
- `CatalogParityTests.cs:36` 已经 `Assert.Equal(hSet, mSet)` 对齐两源 MR-id 集合。
- 实测 `grep -c 'yield return new MrBlueprint(' MetBench_BLL.Core/SystemMT/Launcher/LegacyCatalogFactory.cs` = 33；`grep -hoE '"mr_id"\s*:\s*"[^"]+"' SUT/*/catalog.json | sort -u | wc -l` = 33。两源一致 → 不触发 §8 Stop。

---

## §3 设计

### 3.1 Whitelist 文件

新增 `.github/governance/expected-catalog-counts.txt`（形态对齐 `multi-projection-types.txt`：纯文本、`#` 注释、空行忽略；同形态扩 G10 之先例使 governance/ 子目录保持单一形态家族）。

格式（每行一条记录）：

```
# MetBench catalog source-of-truth whitelist.
# Per CLAUDE.md §12.4 R1 + v2 charter §3 模块 B：catalog 既有两条投影路径
# （LegacyCatalogFactory.cs + SUT/<sut>/catalog.json），本文件是数量与 ID
# 集合的元事实源。任何 PR 加 MR / SUT / equation 必须同步改本文件，否则
# 多个 test class 红（详见 §3.3）。
#
# Categories (one per line):
#   mr:<mr-id>
#   sut:<sut-name>
#   eq:<equation-key>
#
# Lines starting with # and blank lines are ignored.

# --- MR IDs (count must equal MrCatalogEntry count from both providers) ---
mr:advection-amplitude-linearity
mr:advection-mesh-conservation
mr:bateman-mass-conservation
…  # 33 entries total
mr:wave-mesh-conservation

# --- SUT names (count must equal distinct MrCatalogEntry.Mr.SutName) ---
sut:advection-1d
sut:bateman
…  # 16 entries total

# --- Equation keys (count must equal MetadataRepository.ListEquationsAsync()) ---
eq:advection
eq:bateman
…  # 13 entries total
```

> **决策（详见 §5）**：用**单文件**而非三文件，简化加载逻辑与 PR diff 关联，并保证三个集合在同一处审阅。

### 3.2 测试辅助类草签

新增 `MetBench_SystemMT.Tests/SystemMT/Catalog/Governance/ExpectedCatalogCountsWhitelist.cs`（新增 `Governance/` 子目录，平行于既有 `Typed/`、`Binding/`、`Editing/`）：

```csharp
// 草签 only — 实施时按 csproj 路径解析约定调整。
namespace MetBench_SystemMT.Tests.SystemMT.Catalog.Governance;

public static class ExpectedCatalogCountsWhitelist
{
    // 解析路径：从 csproj 输出目录回溯到 repo root 的
    // .github/governance/expected-catalog-counts.txt。
    private static string ResolvePath()
    {
        // 由 TestAssetPaths.AssetRoot() 类似机制定位 repo root。
        // （执行 agent 实施时参考既有 TestAssetPaths.cs / TestContext.CurrentContext 模式。）
        throw null!;
    }

    public static IReadOnlyList<string> ReadAllIds(string prefix)
    {
        // 读所有非注释/非空行，过滤 prefix:<id> 形态，返回 id 列表（不含前缀）。
        throw null!;
    }

    public static IReadOnlyList<string> ReadMrIds()       => ReadAllIds("mr");
    public static IReadOnlyList<string> ReadSutNames()    => ReadAllIds("sut");
    public static IReadOnlyList<string> ReadEquationKeys()=> ReadAllIds("eq");

    public static int MrCount        => ReadMrIds().Count;
    public static int SutCount       => ReadSutNames().Count;
    public static int EquationCount  => ReadEquationKeys().Count;
}
```

### 3.3 站点改写（before / after 摘要）

每处改 `Assert.Equal(literal, actual)` → `Assert.Equal(ExpectedCatalogCountsWhitelist.MrCount, actual)`（或 `SutCount`/`EquationCount`），并在 `CatalogParityTests.cs` 增加**ID set equality** 强守卫：

| 站点（文件:行） | before | after |
|---|---|---|
| CatalogParityTests.cs:32 | `Assert.Equal(33, hardcoded.Count)` | `Assert.Equal(ExpectedCatalogCountsWhitelist.MrCount, hardcoded.Count)` + 新增 `Assert.Equal(ExpectedCatalogCountsWhitelist.ReadMrIds().OrderBy(...).ToList(), hSet)` |
| HardcodedMrCatalogProviderTests.cs:22 | `Assert.Equal(33, entries.Count)` | `Assert.Equal(ExpectedCatalogCountsWhitelist.MrCount, entries.Count)` |
| HardcodedMrCatalogProviderTests.cs:33 | `Assert.Equal(16, distinctSuts.Count)` | `Assert.Equal(ExpectedCatalogCountsWhitelist.SutCount, distinctSuts.Count)` |
| SystemMtBootstrapTests.cs:52,53,54,55,59,60,61,73,74,78,80,93,101,102,122,123 | 各 `Assert.Equal(13\|33\|16, …)` | 对应 `EquationCount / MrCount / SutCount` |
| LauncherCatalogV2ImporterTests.cs:82,83,97,98,114,193-208 (12 行) | 各 `Assert.Equal(16\|33, …)` | 对应 `MrCount / SutCount` |
| SystemMtLauncherTests.cs:75 | `Assert.Equal(33, descriptors.Count)` | `Assert.Equal(ExpectedCatalogCountsWhitelist.MrCount, descriptors.Count)` |
| SystemMtLauncherProviderInjectionTests.cs:110 | `Assert.Equal(33, items.Count)` | `Assert.Equal(ExpectedCatalogCountsWhitelist.MrCount, items.Count)` |

**关键加强**：`CatalogParityTests.Hardcoded_and_manifest_providers_emit_same_entry_count_and_set_of_MR_ids` 之外，新增一个 fact:

```csharp
[Fact]
public void Catalog_MR_id_set_equals_governance_whitelist()
{
    var actual = new HardcodedMrCatalogProvider(Opts()).Load()
        .Select(e => e.Mr.Id).OrderBy(s => s, StringComparer.Ordinal).ToList();
    var expected = ExpectedCatalogCountsWhitelist.ReadMrIds()
        .OrderBy(s => s, StringComparer.Ordinal).ToList();
    Assert.Equal(expected, actual);
}
```

此 fact 是 R1 强化点：加 MR 必须同步改 LegacyCatalogFactory + manifest JSON + whitelist，否则**这条 fact 红**——而不只是 advisory 警告。

> SystemMtLauncherTests.cs:76-84 那 9 行 `Assert.Equal("advection-amplitude-linearity", descriptors[0].Id)` 之类的 first-N ordering pin **不改**：它们检 ordering 不检 count，加 MR 时若新 ID 不打乱前 9 位就不影响，打乱了则**应该**红（contract assertion）。

---

## §4 G7 grep 退役

`.github/workflows/dotnet-test.yml` 行 158-168 整段（"Check 7 (G7) — pinned-count discipline" 至下一空行）待删除。删除理由：§3.3 新增的 fact `Catalog_MR_id_set_equals_governance_whitelist` 是 hard test gate 覆盖，比 G7 grep advisory 更强。v2 charter §4 已 explicit 标注「Layer 2 Grep G7 (pinned-count) → **退役**」。

待删除 YAML 范围（保留为参考；本计划只锁范围，执行时由后续 PR 完成）：
- `dotnet-test.yml` 行 158（注释开始）至行 168（`fi` 闭合）整段 11 行。

**不**新增替代 grep。

---

## §5 单 whitelist 文件 vs 三文件决策

**决定**：**单文件 `expected-catalog-counts.txt`**，前缀分类（`mr:` / `sut:` / `eq:`）。

理由：
1. **形态一致**：与既有 `.github/governance/multi-projection-types.txt` 同形态（flat list + comments + 单一 parser）。
2. **PR 关联性更强**：加一个 MR 通常同时涉及 mr/sut/eq 三集合（若是新 SUT），单文件 diff 一目了然。
3. **辅助类负担更轻**：`ExpectedCatalogCountsWhitelist` 只暴露 3 个静态属性 + 一个泛用 `ReadAllIds(prefix)`，无需三个独立 path resolver。
4. **拒绝替代**：三文件方案（`expected-mr-ids.txt` / `expected-sut-ids.txt` / `expected-equation-keys.txt`）增加 3 个文件、3 个 path resolver、3 个 fixture 注册点，无相应净收益。

---

## §6 测试策略

**不新增 test fact 类型**——本计划复用既有 `test` 硬 gate 即可。

### 6.1 受影响 fact 列表（如果 whitelist 与实际 catalog 不一致，下列至少一项红）

1. `CatalogParityTests.Catalog_MR_id_set_equals_governance_whitelist`（新增） — MR ID 集合差异 → ❌
2. `CatalogParityTests.Hardcoded_and_manifest_providers_emit_same_entry_count_and_set_of_MR_ids` — 计数差异 → ❌
3. `HardcodedMrCatalogProviderTests.Load_pins_30_entries`（重命名建议 `Load_pins_whitelist_count_entries`） — count 差异 → ❌
4. `HardcodedMrCatalogProviderTests.Load_spans_16_distinct_SUTs` — SUT count 差异 → ❌
5. `SystemMtBootstrapTests.SeedCatalogsAsync_seeds_metadata_and_imports_entities` — 任意 count 差异 → ❌
6. `SystemMtBootstrapTests.SeedCatalogsAsync_is_idempotent_on_second_call` — 任意 count 差异 → ❌
7. `LauncherCatalogV2ImporterTests`（多条 fact） — count 差异 → ❌
8. `SystemMtLauncherTests.ListAvailableAsync_returns_known_scenarios_in_id_order` — count 差异 → ❌
9. `SystemMtLauncherProviderInjectionTests`（manifest path） — count 差异 → ❌

**6+ 个 test class 受益**（charter §9 验收 5+ 标的达成）。

### 6.2 Manual smoke（由 executing-plan 在分支验证，云端无 dotnet）

a) **白名单插入虚假 ID smoke**：临时编辑 `.github/governance/expected-catalog-counts.txt` 加一行 `mr:nonexistent-test-mr`，期望 `CatalogParityTests.Catalog_MR_id_set_equals_governance_whitelist` 红、`HardcodedMrCatalogProviderTests.Load_pins_*_entries` 红。回滚该改动。

b) **白名单缺失真实 ID smoke**：临时删除 `mr:openmoc-pincell-nu-sigma-f`，期望同样 fact 红。回滚。

c) **静态扫描验证 G7 grep 已退役**：`grep -n 'G7\|pinned-count' .github/workflows/dotnet-test.yml` 应 0 行。

d) **静态扫描验证旧 literal 已替**：`grep -rn 'Assert\.Equal(\(33\|16\|13\),' MetBench_SystemMT.Tests/SystemMT/{Catalog,Bootstrap,Launcher}/` 应 0 行（注意 `Typed/` 子目录排除，§2 列出的 v1.2 typed coverage hits 不在此 grep 路径内）。

---

## §7 风险 & Stop

### 风险

- **R-1（已 mitigated）**：catalog 两源（LegacyCatalogFactory.cs + SUT/*/catalog.json）不一致 → §8 Stop 条件。**已实测两源 MR-ID 集合相同（各 33 个）**，且 `CatalogParityTests.cs:36` 持续守卫。本风险已消解。
- **R-2**：whitelist 改了但 LegacyCatalogFactory.cs 没改 → 期望表现是 `CatalogParityTests.Catalog_MR_id_set_equals_governance_whitelist` 红。这是设计意图，非风险。
- **R-3**：whitelist 文件路径解析失败（如 csproj output dir 相对 repo root 计算错） → fact 抛 `FileNotFoundException` 或返回空 list → count=0 ≠ catalog count → 全员红。错误前置可见。降本：辅助类应在 `[Fact]` 启动时 fail-fast 打印 candidate paths。
- **R-4**：`SystemMtLauncherTests.cs:76-84` 的 `descriptors[0..8].Id` ordering pin 在加 MR 时若新 ID 排在前 9 位将红。这是**正确的**契约 fact，不视作风险——执行此 fact 红就是提醒人去看 ordering。计划不改这些行。

### Stop 条件（v2 charter §8 P1 对应）

- **Stop A**：若实施时发现两源 MR-ID 集合不同（grep 结果 ≠ 33 或 hSet ≠ mSet），**停下来汇报**，先 R1 parity 化两源再做本计划。**当前确认两源一致，不触发**。
- **Stop B**：若 inventory 发现 > 50 处 pinned literal（本计划实测 34 处），考虑拆为 P1a/P1b 子计划。**当前 34 处不触发**。
- **Stop C**：若辅助类路径解析方案受 csproj 输出目录布局影响导致 ≥ 2 种环境（CI vs 本地）解析逻辑不同，停下来汇报。预防式 mitigation：参考 `TestAssetPaths.cs` 既有 repo-root 解析模式。

---

## §8 执行步骤（给 executing-plan subagent）

> 假设当前 branch `claude/p1-catalog-derived-counts` 已基于 v2 charter merge 后的 main。

1. **创建 whitelist 文件**：写 `.github/governance/expected-catalog-counts.txt`，从 `LegacyCatalogFactory.cs` 抓 33 个 MR ID + 16 个 distinct SutName + 13 个 distinct EquationKey 填入，加 §3.1 头注释。
2. **创建辅助类**：写 `MetBench_SystemMT.Tests/SystemMT/Catalog/Governance/ExpectedCatalogCountsWhitelist.cs`（实现 §3.2 草签的方法体）。参考 `MetBench_SystemMT.Tests/SystemMT/TestAssetPaths.cs` 解析 repo root（同模式）。
3. **改写 34 处 literal**（按 §3.3 表逐一）：
   - 先改 `CatalogParityTests.cs`（同 PR 新增 `Catalog_MR_id_set_equals_governance_whitelist` fact）。
   - 再改 `HardcodedMrCatalogProviderTests.cs`、`SystemMtBootstrapTests.cs`、`LauncherCatalogV2ImporterTests.cs`、`SystemMtLauncherTests.cs:75`、`SystemMtLauncherProviderInjectionTests.cs:110`。
4. **退役 G7**：删除 `.github/workflows/dotnet-test.yml` 行 158-168 整段 G7 检查。
5. **本地静态验证**（云端无 dotnet）：
   - `grep -n 'G7\|pinned-count' .github/workflows/dotnet-test.yml` → 应 0 行。
   - `grep -rn 'Assert\.Equal(\(33\|16\|13\),' MetBench_SystemMT.Tests/SystemMT/{Catalog,Bootstrap,Launcher}/` → 应 0 行（排除 Typed/）。
   - `wc -l < .github/governance/expected-catalog-counts.txt` → 期望 ~70+（33+16+13 + 注释 + 分隔空行）。
6. **更新 active-plan-index**：`docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md` §1 注册本 plan 路径与状态 → "Phase 1 of v2 charter P1 — Pending merge"。
7. **更新 docs/status/current.md**：在 Stage-8 ledger 加一行（Controlled 状态 + 链尾跨链 review 路径预留）。注意 G9 grep 会检 `post-merge-review` 链接 —— 本 PR 为单 PR 改造，**不**标 Controlled（standalone PR），不触发 G9。
8. **PR Gate Checklist**：按 `docs/superpowers/templates/pr-gate-checklist.md` 7 节填写 PR body。Plan 节引用本文件。
9. **commit & push**：参考最近 governance PR（如 PR #209）的 commit message 风格：
   `ci(governance): retire G7 grep, add expected-catalog-counts whitelist (v2 charter P1)`。

---

## §9 验收标准

- [ ] CI `test` job 绿（含新增 `Catalog_MR_id_set_equals_governance_whitelist` fact）
- [ ] CI `governance` job 输出中无 G7 / "pinned-count" 字样
- [ ] `.github/governance/expected-catalog-counts.txt` 文件存在，包含 33 mr + 16 sut + 13 eq 条目
- [ ] §2 列出的 34 处 `Assert.Equal(33|16|13, …)` literal 全部改为 `ExpectedCatalogCountsWhitelist.*Count` 引用
- [ ] 受益 test class ≥ 5（实际 6 个：CatalogParityTests / HardcodedMrCatalogProviderTests / SystemMtBootstrapTests / LauncherCatalogV2ImporterTests / SystemMtLauncherTests / SystemMtLauncherProviderInjectionTests）
- [ ] 加新 MR 的下一个 PR 只需修改 3 处（whitelist + LegacyCatalogFactory.cs + 对应 SUT/<sut>/catalog.json）即可让全部相关 fact 通过；未改 whitelist 则至少 6 个 fact 红
