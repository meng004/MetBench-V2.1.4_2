# SutRoot bin/Debug 解析决策 (F3 follow-up)

> **Date**: 2026-05-28
> **Status**: Active decision — accept current behavior + document
> **Source**: T1 非 MR CRUD 链路 4 项 follow-up 计划 §5 (`docs/superpowers/plans/2026-05-28-t1-followups-plan.md`)
> **Discovered in**: PR #221 / #223 / #225 VM verification — `dotnet run` 模式下 catalog 编辑写入 bin/Debug 副本，源 `SUT/` 保持 git-clean

decision-record: docs/superpowers/specs/2026-05-28-sutroot-bin-debug-decision.md (this file)

---

## §1 现象

`MetBench_Client/App.xaml.cs:134-141` 注册 `LauncherOptions`：

```csharp
SutRoot: Path.Combine(
    Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!,
    "SUT")
```

`Assembly.GetEntryAssembly().Location` 在 `dotnet run` 模式下解析为
`bin/Debug/net8.0-windows7.0/MetBench_Client.dll`，因此 `SutRoot` 解析为
`bin/Debug/net8.0-windows7.0/SUT/`（构建时由 `MetBench_Client.csproj` 的
`<None Include="..\SUT\**" CopyToOutputDirectory="PreserveNewest" />` 拷贝来的副本）。

PR-1 (#221) / PR-2 (#223) / PR-3 (#225) VM verification 都观察到此行为：
catalog / sample / equation 编辑后保存写入 bin/Debug 副本，源 `SUT/` 目录保持 git-clean。

---

## §2 三个候选选项

### 选项 A — 接受现状 + 文档化

- **优**：生产部署（已发布 .exe + SUT/ 同级目录）时 SutRoot 正好指向 .exe 同级，行为正确。dev 与 prod 路径解析逻辑统一，无分支。
- **劣**：dev 体验差。研究员在 IDE 跑 `dotnet run` 改 catalog，git diff 看不到变更；需手动 `xcopy /Y bin\Debug\net8.0-windows7.0\SUT\<name>\catalog.json SUT\<name>\` 才能 commit。
- **影响面**：T1 4 个新页 (SUT / Equation / SampleCase / MR Catalog) 都需要文档化此约束。

### 选项 B — dev 模式启发探测，回写源 SUT/

实现思路：

```csharp
SutRoot: ResolveSutRoot(),
// ...
private static string ResolveSutRoot()
{
    var asmDir = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!;
    // dev mode heuristic: asm path contains "bin/Debug" or "bin/Release"
    // AND .git directory exists 4 levels up → dev tree.
    if (Regex.IsMatch(asmDir, @"[/\\]bin[/\\](Debug|Release)") &&
        TryFindRepoRoot(asmDir, out var repoRoot))
    {
        return Path.Combine(repoRoot, "SUT");
    }
    return Path.Combine(asmDir, "SUT");
}
```

- **优**：dev 流程顺畅，研究员改 catalog 直接 git diff 可见。
- **劣**：启发式脆。`publish -c Release` 输出可能命中 `bin/Release` 但用户期望 publish 副本而非源；CI build 路径可能包含 `bin/Debug`。误判概率非零，且失败模式（写错地方）破坏数据完整性比 UX 差更糟。
- **影响面**：所有 catalog 编辑器，CRUD 行为依路径解析改变。

### 选项 C — 显式 `--sut-root` 命令行 / 配置覆盖

实现思路：

```csharp
SutRoot: configuration.GetValue<string>("LauncherOptions:SutRoot")
         ?? Path.Combine(asmDir, "SUT"),
```

- **优**：显式、可测、生产 / dev / CI 各自配置不冲突。
- **劣**：增加 `appsettings.local.json` 表面，dev 需手动配置才能享受回写源 SUT/；UX 改进有限。

---

## §3 决策（选 A）

**接受现状**：保持 `SutRoot = Assembly.GetEntryAssembly().Location/SUT` 不变。

**理由**：
1. 生产部署路径行为正确，是 first-principles 的对照参照
2. 选项 B 启发式误判风险（错位写入）比当前 dev UX 不便更危险，**安全 > 便利**
3. 选项 C 增加配置表面但 dev UX 改善有限，且 CRUD 是低频操作，dev 用户改完后 xcopy 一次成本可接受
4. T1 4 个 catalog 页测试已显示编辑流程端到端可用，问题仅在「源码层 git diff 不可见」这一中间环节

---

## §4 文档化点（配套实施）

本 spec 只决策，不动代码。配套文档化由后续 VM-track PR 视情况实施：

1. 各 catalog 页（SUT / Equation / SampleCase / MR / SampleCase）footer 文本框加灰色提示：

   ```
   保存位置：bin\Debug\net8.0-windows7.0\SUT\（dev 模式）
   提交源码：xcopy bin\Debug\net8.0-windows7.0\SUT\<name>\ SUT\<name>\ /E /Y
   ```

2. `CLAUDE.md` §5 page↔VM 配对模式增加 1 行：

   > catalog 编辑器写入 `LauncherOptions.SutRoot`；dev 模式下为 bin/Debug 副本，源 `SUT/` 不变 — 见 [`docs/superpowers/specs/2026-05-28-sutroot-bin-debug-decision.md`](docs/superpowers/specs/2026-05-28-sutroot-bin-debug-decision.md)

3. `MetBench_Client/Views/Pages/SystemMt*CatalogPage.xaml.cs` 类 XML doc 引用本 spec

---

## §5 验收

- [x] 本 spec 文件落档
- [ ] 文档化点 (1) (2) (3) 由后续 PR 视情况实施（非阻塞）
- [ ] 选 B 或 C 如未来需求驱动可重启决策，开新 spec 覆盖本文

---

## §6 重新评估触发条件

下列任一情形发生可重启决策：
- 研究员在 30 天内 ≥ 5 次反馈"改了 catalog 但 git diff 看不到"
- 出现因路径误读导致的数据丢失事件
- WPF 客户端打包 publish 工作流变更，路径假设失效
