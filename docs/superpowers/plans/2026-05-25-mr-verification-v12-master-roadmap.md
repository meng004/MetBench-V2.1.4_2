# MR Verification v1.2 Master Roadmap Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 把 v1.2 从 `PR-0` 的 catalog foundation 推进到 `PR-10` 的 47/47 migration + coverage gate，并把每个阶段拆成独立可执行 PR 计划。

**Architecture:** 当前主线继续保持 `ManifestMrCatalogProvider` + `IMrAssertion` 运行路径稳定，新 IR 放在 `MetBench_BLL.Core/SystemMT/V12Catalog/`，按 `schema -> validate -> runtime -> property -> migration` 的顺序增量落地。每个 PR 只收一层语义边界，所有共享类型边界只能在前置 PR 中建立，后续 PR 只能消费，不允许并行重写。

**Tech Stack:** .NET 8 / System.Text.Json / xUnit / LiteDB / GitHub Actions / Windows SSH + RDP for WPF-only validation

---

### Task 1: 锁定基线与执行顺序

**Files:**
- Check: `docs/superpowers/specs/2026-05-25-mr-verification-v1.2-codex-ready.md`
- Check: `MetBench_BLL.Core/SystemMT/V12Catalog/`
- Check: `MetBench_BLL.Core/SystemMT/Launcher/SystemMtLauncher.cs`
- Test: `MetBench_SystemMT.Tests/SystemMT/V12Catalog/V12CatalogSerializationTests.cs`

- [ ] **Step 1: 核对 `PR-0` 已合并且当前主线可作为新计划基线**

Run: `rtk git log --oneline -1 && rtk dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~V12Catalog" --no-restore`
Expected: `ba7a9a1` 在主线，V12Catalog 聚焦测试为 PASS

- [ ] **Step 2: 记录后续 PR 的固定顺序与依赖**

```text
PR-1 typed model + validators
PR-2 scalar runtime kernels
PR-3 applicability + statuses
PR-4 reference/convergence
PR-5 sequence + subadditive
PR-6 field + derived invariant
PR-7 statistical + cross-method
PR-8 property runtime
PR-9 exponential growth
PR-10 migration + coverage gates
```

- [ ] **Step 3: 确认共享边界只能在前置 PR 中建立**

```text
PR-1 defines: typed model / validator interfaces
PR-2 defines: runtime dispatcher / kernel interfaces
PR-8 defines: property checker interfaces
PR-10 consumes all prior layers, does not redesign them
```

- [ ] **Step 4: Commit**

```bash
rtk git add docs/superpowers/plans/2026-05-25-mr-verification-v12-master-roadmap.md
rtk git commit -m "docs(v12): add master roadmap for PR-1 to PR-10"
```

### Task 2: 固定每个 PR 的执行规范

**Files:**
- Modify: `docs/superpowers/plans/2026-05-25-mr-verification-v12-master-roadmap.md`

- [ ] **Step 1: 写入统一前置条件**

```text
1. 基于最新 main
2. 上一 PR 已 merge
3. 当前 worktree 干净
4. 本 PR 范围外不动
```

- [ ] **Step 2: 写入统一 TDD 节奏**

```text
1. 先写失败测试
2. 跑聚焦测试确认红
3. 写最小实现
4. 跑聚焦测试转绿
5. 跑相关集成测试
6. 跑全量 MetBench_SystemMT.Tests
7. review
8. push / PR / CI / merge
```

- [ ] **Step 3: 写入统一 review checklist**

```text
- 禁止回退到 stringly typed / dictionary predicate
- 禁止绕过 load-time validator
- 禁止把 Property 混入 MR path
- 禁止让 runtime 修补 spec 错误
- 禁止引入自由字符串表达式 DSL
```

- [ ] **Step 4: Commit**

```bash
rtk git add docs/superpowers/plans/2026-05-25-mr-verification-v12-master-roadmap.md
rtk git commit -m "docs(v12): lock shared execution rules for all PR plans"
```

### Task 3: 固定 Cloud / Windows 分工

**Files:**
- Check: `docs/uat/runbooks/2026-05-24-cloud-windows-dual-environment.md`
- Modify: `docs/superpowers/plans/2026-05-25-mr-verification-v12-master-roadmap.md`

- [ ] **Step 1: 规定 Cloud 为主执行环境**

```text
Cloud:
- 所有 typed model / validator / runtime / fixture / CI gate 实现
- dotnet build / dotnet test / TRX / baseline artifacts
- GitHub PR / CI / merge
```

- [ ] **Step 2: 规定 Windows 只在触达 WPF 时介入**

```text
Windows SSH:
- dotnet build MetBench_Client
- 启动程序 / 收集日志

Windows RDP:
- WPF UI 可见性与交互验证
```

- [ ] **Step 3: 明确非 WPF PR 不要求 Windows 回执**

Run: `rtk rg -n "MetBench_Client|App.xaml.cs|Windows" docs/superpowers/plans/2026-05-25-mr-verification-v12-*.md`
Expected: 仅触达 WPF 的 PR 文档才要求 Windows 验收

- [ ] **Step 4: Commit**

```bash
rtk git add docs/superpowers/plans/2026-05-25-mr-verification-v12-master-roadmap.md
rtk git commit -m "docs(v12): record cloud/windows execution split"
```
