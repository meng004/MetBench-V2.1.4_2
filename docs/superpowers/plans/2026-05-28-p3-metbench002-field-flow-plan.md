# P3 — METBENCH002 Generic Field-Flow Tracer Analyzer (Module B)

> **Date**: 2026-05-28
> **Status**: Draft (writing-plan stage)
> **Charter**: `docs/superpowers/specs/2026-05-28-code-governance-v2-charter.md` §3 模块 B 行, §6 P3 行, §4 METBENCH001/002 行
> **Scope tag**: v2 章程 P3（最大 P，估 2-3 天）
> **Implements**: 元规则 R1（cross-projection parity 强化）— 通用 AST 扫描的"更广 / 更严"补强

---

## §1 目标 & 验收

**目标**：v2 charter §6 P3：把 Cat B 中 L1（multi-projection record 字段不对称漂移）的检出从 METBENCH001 现有覆盖扩到更广 use-site 谱 + 更高阈值。新增 Roslyn 诊断 `METBENCH002`（DefaultSeverity `Info`），与 METBENCH001 同 DLL、同 csproj、同 ReleaseTracking 通道。

**关键澄清（提前挑明，需同 PR 在 v2 charter §4 retrospective）**：Plan inventory 阶段发现 **METBENCH001 当前实现已经是通用 syntax-only 扫描**（跨文件 `new R(...)` ≥ 2 distinct file），**不读** `.github/governance/multi-projection-types.txt` 注册表。这与 v2 charter §4 v1→v2 mapping 行对 METBENCH001 的描述("4 type registry")**不符**。

按 §12.4 R3 retrospective 责任：**本 PR 必须同时 re-touch v2 charter §4 行**，把 METBENCH001 描述改为"已是通用 syntax-only 扫描，threshold ≥ 2 跨文件 use site"，并标注 METBENCH002 的差异化角色（高阈值 + 扩 use-site 谱）。

**Roslyn analyzer 看不见 PR diff**——只看完整 compilation 快照。所以 P3 实际落到的可机械检测形态是 "**已存在 record 在 compilation 内 use site 数量超阈值**" 扫描，作为"应警惕未来加字段时的同步开销"的提前信号，不是 "PR diff 内新加字段" 检查。

**验收（§7 完整版）**：
- `MetBench_Analyzers/FieldFlowTracerAnalyzer.cs` 存在并按 `[DiagnosticAnalyzer(LanguageNames.CSharp)]` 注册 `METBENCH002`
- `AnalyzerReleases.Unshipped.md` 新增 `METBENCH002` 行
- `dotnet build MetBench_Analyzers/` 0 warning 0 error
- `dotnet build MetBench_BLL.Core/` 跑出 METBENCH002 Info 诊断（预估 ≤ 5 record）
- ≥ 3 个 xUnit reflection fact 通过（METBENCH001 + METBENCH002 注册 + 严重级 + 类别）
- 既有 `dotnet test` 套件 0 红
- METBENCH001 行为不变（基线 emit 数量与 P3 上线前一致）
- v2 charter §4 行 retrospective 修订（R3 合规）
- AnalyzerReleases RS2008 不报

---

## §2 当前状态 inventory

实测命令与数字（2026-05-28 在 `claude/p3-metbench002-field-flow` 分支跑）：

**B-1 METBENCH001 现状**：
- 实现：`MetBench_Analyzers/MultiProjectionRecordAnalyzer.cs`（143 行）
- 注册表 `.github/governance/multi-projection-types.txt`：4 个 type（MrCatalogEntry / MrSummary / SystemMtResultRecord / ExecutionEvidence）
- **但 analyzer 当前实现不读注册表**——它已经是通用扫描（syntax-only：scan 所有 `public sealed record` + 所有 `new TypeName(...)` 跨文件 use site，threshold ≥ 2 distinct external file）
- **重要现实**：METBENCH001 的算法**就是** P3 charter 想要的算法形态。导致 P3 不再是"新算法"而是"**职责差异化**"

**B-2 Record inventory（METBENCH002 上线后扫范围）**：
- `grep -rh "public sealed record" MetBench_BLL.Core/ MetBench_BLL/ MetBench_DAL/ | wc -l` = **136**
- 按项目：BLL.Core 134、BLL 2、DAL 0
- 形态分布：134 个 primary-ctor 形式，2 个 class-form
- 触及 METBENCH001 当前 emit 数量：**待 P3 PR 开本地 build 实测**（在 §6 step 1 执行）

**B-3 Analyzer 测试基础设施**：
- `find MetBench_SystemMT.Tests/ -iname "*nalyzer*"` = **0 命中**
- `grep -r "Microsoft.CodeAnalysis.Testing" --include="*.csproj"` = **0 命中**
- **结论**：无 analyzer testing harness；P3 采**选项 A**（reflection-only smoke fact），不引入新依赖

---

## §3 设计

### 3.1 Diagnostic Descriptor 草签

```csharp
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class FieldFlowTracerAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "METBENCH002";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Public sealed record with elevated cross-projection footprint",
        messageFormat: "Record '{0}' has {1} distinct cross-file construction sites: {2}. Per CLAUDE.md §12.4 R1 + v2 charter §6 P3, adding a field requires touching every site; verify ParityTests coverage or document a decision record.",
        category: "MetBench.Governance",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Generic AST-based field-flow tracer for public sealed records (v2 charter P3). Complements METBENCH001 with higher threshold + expanded use-site syntax detection.",
        helpLinkUri: "https://github.com/meng004/MetBench-V2.1.4_2/blob/main/docs/superpowers/specs/2026-05-28-code-governance-v2-charter.md",
        customTags: new[] { WellKnownDiagnosticTags.CompilationEnd });

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(Rule);
}
```

### 3.2 与 METBENCH001 的职责切割（**采备选路径**）

| 维度 | METBENCH001 (existing) | METBENCH002 (P3 new) |
|---|---|---|
| Use-site 谱 | `ObjectCreationExpressionSyntax` (new R(...)) | METBENCH001 全集 + `RecursivePatternSyntax` (is R r and { … }) |
| Threshold | ≥ 2 distinct external file | ≥ 5 distinct external file |
| 注册表过滤 | 不读 | 不读 |
| 严重度 | Info | Info |
| 目的 | 历史层位（保留向后兼容）；基础广覆盖 | "更广 use-site 谱 + 更严阈值"的精选高风险 record 信号 |

**理由**：METBENCH001 emit 集涵盖 threshold ≥ 2 的所有 record；METBENCH002 加 5-way 高阈值 = 显然真正"扩散广"record 的精选信号，控制 noise；扩 `RecursivePatternSyntax` use-site 覆盖 `is R r and { B: … }` 模式匹配场景，命中 METBENCH001 漏的纯 pattern-match-only consumer。

### 3.3 算法（syntax-only）

```
OnCompilationEnd:
  records: Dict<string, declFiles>   // record name → set of decl files
  useSites: Dict<string, useFiles>   // record name → set of use files

  Pass 1: collect public sealed record declarations
    forall SyntaxTree t:
      forall RecordDeclarationSyntax d:
        if !d.Modifiers.Any(Public) || !d.Modifiers.Any(Sealed): continue
        records[d.Identifier.ValueText].declFiles.Add(t.FilePath)

  Pass 2: collect use sites (expanded谱)
    forall SyntaxTree t:
      forall node in t.GetRoot().DescendantNodes():
        name = null
        switch node:
          case ObjectCreationExpressionSyntax oc:
            name = ExtractTypeName(oc.Type)
          case RecursivePatternSyntax rp when rp.Type != null:
            name = ExtractTypeName(rp.Type)
          default: continue
        if name != null && records.ContainsKey(name):
          useSites[name].Add(t.FilePath)

  Pass 3: emit
    forall (name, (declFiles, declLoc)) in records:
      external = useSites[name] \ declFiles
      if external.Count >= 5:           // THRESHOLD = 5
        ReportDiagnostic(Rule, declLoc, name, external.Count, sorted(external).Join(", "))
```

**RS1030/RS1037/RS2008 合规**：syntax-only 无 SemanticModel 调用规避 RS1030；`WellKnownDiagnosticTags.CompilationEnd` custom tag；AnalyzerReleases.Unshipped.md 加新行避 RS2008。

### 3.4 AnalyzerReleases.Unshipped.md 改动

在现有 METBENCH001 行下追加：

```
METBENCH002 | MetBench.Governance | Info | FieldFlowTracerAnalyzer — generic field-flow tracer for public sealed records with >= 5 cross-file construction sites (v2 charter §6 P3)
```

### 3.5 csproj 改动

**无变化**。同 DLL 多 `[DiagnosticAnalyzer]` 类自动加载。

### 3.6 测试基础设施决策

采**选项 A**（reflection-only smoke fact）。新增 `MetBench_SystemMT.Tests/Governance/Analyzers/AnalyzerRegistrationFacts.cs`，含 ≥ 3 个 fact：

```csharp
namespace MetBench_SystemMT.Tests.Governance.Analyzers;

public sealed class AnalyzerRegistrationFacts
{
    [Fact]
    public void METBENCH001_is_registered_with_Info_severity()
    {
        var a = new MetBench.Analyzers.MultiProjectionRecordAnalyzer();
        Assert.Contains(a.SupportedDiagnostics, d => d.Id == "METBENCH001" && d.DefaultSeverity == DiagnosticSeverity.Info);
    }

    [Fact]
    public void METBENCH002_is_registered_with_Info_severity()
    {
        var a = new MetBench.Analyzers.FieldFlowTracerAnalyzer();
        Assert.Contains(a.SupportedDiagnostics, d => d.Id == "METBENCH002" && d.DefaultSeverity == DiagnosticSeverity.Info);
    }

    [Fact]
    public void METBENCH002_category_is_MetBench_Governance()
    {
        var a = new MetBench.Analyzers.FieldFlowTracerAnalyzer();
        var d = a.SupportedDiagnostics.Single(d => d.Id == "METBENCH002");
        Assert.Equal("MetBench.Governance", d.Category);
    }
}
```

需在 `MetBench_SystemMT.Tests.csproj` 加 ProjectReference 到 `MetBench_Analyzers`（特殊形态）：

```xml
<ProjectReference Include="..\MetBench_Analyzers\MetBench_Analyzers.csproj">
  <ReferenceOutputAssembly>true</ReferenceOutputAssembly>
  <SkipGetTargetFrameworkProperties>true</SkipGetTargetFrameworkProperties>
</ProjectReference>
```

**降级方案**：若 netstandard2.0 → net8.0 兼容失败，改用 `Assembly.LoadFrom(<path>)` 显式加载 analyzer DLL；csproj 不动。

### 3.7 v2 charter §4 retrospective 改动（R3 合规）

修订 `docs/superpowers/specs/2026-05-28-code-governance-v2-charter.md` §4 v1→v2 mapping 表的 METBENCH001 行：

before：
```
| Layer 4 METBENCH001 | → 模块 B；**升级 METBENCH002 通用** | 001 仍跑特化，002 跑通用扫描 |
```

after：
```
| Layer 4 METBENCH001 | → 模块 B；保留 | 001 当前已是通用 syntax-only 扫描（threshold ≥ 2 跨文件 use site），并非 4-type 注册表特化；P3 实施时该 charter 描述被 retrospective 修订（见 P3 plan §1）。METBENCH002 作差异化补强：threshold ≥ 5 + 扩 `RecursivePatternSyntax` use-site。 |
```

同时修订 §6 P3 行的"即效"列措辞，把"L1 Cat B 覆盖大幅扩"改成更精准的"扩 use-site 谱 + 高阈值精选"。

---

## §4 测试策略

| 层位 | 内容 | 跑法 |
|---|---|---|
| 编译 | `dotnet build MetBench_Analyzers/MetBench_Analyzers.csproj -c Release` | local + CI；0 warning 0 error；RS2008 / RS1030 / RS1037 不报 |
| smoke fact | 3 个 reflection fact in `Governance/Analyzers/` | 既有 `dotnet test` 自带 |
| manual smoke | `dotnet build MetBench_BLL.Core/` 观察 `METBENCH002:` 输出行数与 record 列表 | local only |
| 既有套件 | `dotnet test MetBench.sln` 0 红 | CI |
| governance grep job | 不改 | CI |

**不**做：不引 Microsoft.CodeAnalysis.Analyzer.Testing；不写 emit count 的 hardcoded 断言；不动 METBENCH001 任何字节。

---

## §5 风险 & Stop

- **Risk A — syntax-only 误报**：THRESHOLD=5 已显著降噪。Info-level + 后续 ≥ 1 周观察期决定是否升 Warning。
- **Risk B — record 数量大 (136) → analyzer 性能**：常数级 record 数对中型 sln (~百 file) 总耗时 < 1s。
- **Risk C — METBENCH001 / METBENCH002 双 emit**：threshold 差 (2 vs 5) 天然差异化，预计 0-3 record 同时触发，可接受。
- **Risk D — Tests csproj ProjectReference 不兼容**：见 §3.6 末段，降级 Assembly.LoadFrom。
- **Stop A**：若 `dotnet build MetBench_Analyzers.csproj` 在 main 当前 SHA 已不通过，停下汇报。
- **Stop B**：若 inventory §2 B-2 实测 record 数 < 10，假设动摇，停下重评估。
- **Stop C**：若发现先于本 PR 的其他 PR 已触动 `MetBench_Analyzers/`（不应该），停下避冲突。

---

## §6 执行步骤（≤ 10）

1. **Inventory 复核**：跑 `dotnet build MetBench_BLL.Core/` 观察当前 METBENCH001 emit 行数与 record 列表，作为基线。
2. **新增 `MetBench_Analyzers/FieldFlowTracerAnalyzer.cs`**：syntax-only，DiagnosticId `METBENCH002`，THRESHOLD=5，扩 use-site 集到 `RecursivePatternSyntax`。
3. **更新 `AnalyzerReleases.Unshipped.md`**：追加 METBENCH002 行（§3.4）。
4. **新增 `MetBench_SystemMT.Tests/Governance/Analyzers/AnalyzerRegistrationFacts.cs`**：3 个 reflection fact（§3.6）。
5. **调整 `MetBench_SystemMT.Tests.csproj`**：加 ProjectReference 到 MetBench_Analyzers（§3.6 snippet）。若 build 失败，切 Assembly.LoadFrom 降级。
6. **R3 retrospective**：按 §3.7 修 v2 charter `docs/superpowers/specs/2026-05-28-code-governance-v2-charter.md` §4 METBENCH001/002 行 + §6 P3 即效列。
7. **本地验**：`dotnet build MetBench.sln -c Release` 0 warning；`dotnet test --filter "FullyQualifiedName~Governance.Analyzers"` 3/3 通过；`dotnet build MetBench_BLL.Core` 观察 METBENCH002 输出。
8. **注册 active-plan-index**：§1 加一行（Active scoped — 单 PR 改造）。
9. **commit & push**：`feat(governance): add METBENCH002 generic field-flow tracer + retrospective charter §4 (v2 charter P3)`。**不**改 `docs/status/current.md`。
10. **不开 PR**（由上层决定）。

---

## §7 验收标准

- [ ] `MetBench_Analyzers/FieldFlowTracerAnalyzer.cs` 存在 + syntax-only + 注册 METBENCH002
- [ ] `AnalyzerReleases.Unshipped.md` 新增 METBENCH002 行（RS2008 不报）
- [ ] `dotnet build MetBench_Analyzers/MetBench_Analyzers.csproj -c Release` 0 warning 0 error
- [ ] `dotnet test MetBench_SystemMT.Tests --filter "Governance.Analyzers"` ≥ 3 fact 全绿
- [ ] `dotnet test MetBench.sln` 既有套件 0 红
- [ ] `dotnet build MetBench_BLL.Core` 输出含 ≥ 0 行 `METBENCH002:` 诊断（实测数量纳 PR description）
- [ ] METBENCH001 行为不变：emit 数量与上线前一致
- [ ] v2 charter §4 METBENCH001 行 + §6 P3 即效列已 retrospective 修订（R3 合规）
- [ ] active-plan-index.md §1 新增本 plan 行
- [ ] **不**改 `docs/status/current.md`；**不**触碰 `MetBench_Client/`、`SemanticCatalogBoundaryTests`、Method MT

---

## §8 引用

- v2 章程：`docs/superpowers/specs/2026-05-28-code-governance-v2-charter.md` §3 模块 B / §6 P3 / §4 METBENCH001/002 / §8 Risk B
- 既有 analyzer：`MetBench_Analyzers/MultiProjectionRecordAnalyzer.cs`
- 既有注册表：`.github/governance/multi-projection-types.txt`
- ReleaseTracking 规则：https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md
- CLAUDE.md §12.2 / §12.4 R1 / R3
- 兄弟 plan 形态参考：`docs/superpowers/plans/2026-05-28-p1-catalog-derived-counts-plan.md`
