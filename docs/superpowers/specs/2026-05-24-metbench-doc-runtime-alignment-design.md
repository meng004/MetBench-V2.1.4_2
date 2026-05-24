# MetBench 文档-实现对齐与开发清障设计

> **Date**: 2026-05-24
> **Status**: In Progress
> **Scope**: `AGENTS.md`、`CLAUDE.md`、`docs/PROJECT-STRUCTURE.md`、`docs/requirements.md`、`docs/design/v2-system-mt-architecture.md`
> **Repo baseline**: `main` @ `5691727`（2026-05-24 拉取确认 up-to-date）
> **CodeGraph**: synced on 2026-05-24, index already up to date

## 1. 背景

MetBench 在 2026-05-24 已完成一轮关键结构演进：

- `SystemMtLauncher` 已切到 provider-backed catalog 加载路径。
- `ManifestMrCatalogProvider` 已成为 WPF DI 默认实现。
- `SystemMtExecutionRecorder` 已开始写入 `ExecutionEvidence`，并已补上目标字段级 sample trace。
- Stage 8 / G-X3 的 PR #91、#92、#93、#94 已全部合入 `main`。
- 仓库已再次执行 `git pull --ff-only`，确认当前本地 `main` 与 `origin/main` 一致。
- CodeGraph 已执行 `sync`，确认图谱无需增量更新。

但当前核心文档与实现代码没有同步收敛，导致三类问题同时存在：

1. **路线图文档与运行时真相不一致**。
   - `docs/PROJECT-STRUCTURE.md` 仍停在 2026-05-17 的 4 SUT / 5 launcher MR / 521 baseline。
   - `docs/design/v2-system-mt-architecture.md` 仍把 Trend 子系统视为现行架构组成。
2. **受控开发事实源内部时间差扩大**。
   - `docs/requirements.md` 顶部 baseline 仍写 876 pass，但后文已登记到 G-X3 965 pass 任务链。
3. **后续 Stage 8 开发仍受过渡态债务阻塞**。
   - 当时的 launcher 仍保留 `HardcodedMrCatalogProvider` fallback；
   - importer 仍依赖 `SystemMtLauncher` 具体类；
   - 当时的 `ExecutionEvidence.SampleTraces` 仍未接入真实样本级证据。

如果在这个状态下继续推进 Stage 8，新开发会建立在过期文档与半收敛架构之上，导致评审、测试、交接和后续计划持续失真。

## 2. 目标

本轮工作只做两件事：

1. **把当前事实源文档恢复为与 `main` 一致的状态。**
2. **把当前阻塞收口成一个明确、可执行、可验证的开发计划。**

本轮不追求完成全部架构收敛实现，而是先建立一个可信的开发起点。

## 3. 非目标

以下内容不在本轮范围内：

- 不清理所有历史 plan/spec 中的旧说法。
- 不重写整个 `docs/design/` 文档族。
- 不在本轮直接完成 meta-prompt 引擎或新增 SUT/MR 扩展。
- 不在缺少运行证据时虚构新的精确测试通过数。

## 4. 设计原则

### 4.1 文档只写“已证实事实”

每一处更新都必须能被以下证据之一支撑：

- 当前 `main` 分支实现代码；
- 已合入提交；
- 当前仓库内可审计测试工件；
- 本轮本地验证命令结果。

没有证据支撑的内容统一标为“待核实”。

### 4.2 区分“当前架构”与“历史/迁移背景”

对齐文档时，需要把已经退出运行时的模块从“当前架构”降级为“历史背景”或直接移除，例如 Trend。

### 4.3 先收口真相，再推进功能

开发计划的优先级不按“想做什么”排，而按“什么会继续制造失真或阻塞后续开发”排。

## 5. 对齐策略

### 5.1 `AGENTS.md`

定位为路线图与当前状态总览。需要更新：

- Stage 8 当前交付状态；
- G-X3 当前已合入 PR 进度；
- 当前主阻塞从“是否已启动”改为“尚未完全收口的过渡态问题”。

### 5.2 `CLAUDE.md`

定位为冷启动 agent 的全局快照。需要更新：

- Stage 8 的事实状态；
- P-A / P-B / P-C / G-X3 的真实完成度；
- 当前仍未闭环的项，已从“是否有 sample trace”收敛为“sample trace 覆盖粒度还能扩展”以及 Windows 侧 build 回执补记。

### 5.3 `docs/PROJECT-STRUCTURE.md`

定位为结构事实源。需要更新：

- 项目布局中的现行模块说明；
- 当前 SUT 数、launcher catalog 规模、测试基线表述；
- 删除把 Trend 继续当作现行活跃模块的写法；
- 明确 `.codegraph/` 属于本地图谱索引产物，而非核心仓库架构的一部分。

### 5.4 `docs/requirements.md`

定位为“受控开发模式”事实源。需要更新：

- 顶部 baseline 改写为“可证实事实 + 待核实边界”；
- G-X3 索引与现有 `main` 状态对齐；
- 对当前阻塞单列为待完成项，不与已完成项混写。

### 5.5 `docs/design/v2-system-mt-architecture.md`

定位为“当前架构入口文档”。本轮只做有限修正：

- 把已经退出运行时的 Trend 从当前模块清单中移出；
- 增补 provider-backed catalog / recorder / evidence 当前实现路径；
- 保留历史设计背景，但不能继续把已退役模块写成现行真相。

## 6. 当前问题清单

### P0. 文档漂移

这是当前最高优先级问题。因为它会直接污染后续计划、评审和交接。

### P1. launcher catalog 双事实源残留

`SystemMtLauncher` 的生产 fallback 已移除，manifest/provider 成为唯一生产目录入口；但 `HardcodedMrCatalogProvider` 仍作为测试 / parity 对照存在，运行时收敛还差 importer 去耦与 evidence 闭环。

### P2. importer 对具体实现耦合

该问题已在本轮通过 `ISystemMtCatalogReader` 抽象收口；后续只剩 Windows 侧 build 结果补记与文档固化，不再是运行时主阻塞。

### P3. execution evidence 未完整闭环

`ExecutionEvidence` 已入 schema 和 repo，且 `SampleTraces` 已能记录目标字段级 source / transformed / output triple。后续提升点不再是“从 0 到 1”，而是扩展到更多变量 / 更多路径。

### P4. 测试基线表达不稳定

当前仓库文档同时存在 521 / 876 / 965 等多组测试数字。没有统一叙事规则前，任何后续文档都可能继续写乱。

### P5. 当前本地验证环境存在两个噪声源

- `codegraph query/context` 在该仓库当前索引上返回 `unable to open database file`，因此本轮不能把 CodeGraph CLI 当作稳定验证器，只能保留 `.codegraph/` 作为索引产物并回退到 `rg` + 直接读文件。
- `MetBench_SystemMT.Tests` 在本机需要先做一次 `dotnet restore` 才能编译；`rtk dotnet test` 目前仍只回 `completed`，不给精确计数，本轮验证只能据实写成“命令完成 / 精确计数待核实”。

## 7. 执行顺序

建议按下面顺序推进：

1. 先修核心事实源文档。
2. 再在 `docs/requirements.md` 中明确当前阻塞和待办边界。
3. 再处理运行时问题，顺序为：
   - importer 去耦前的 fallback removal 已完成，后续转入验证固化
   - importer 去耦
   - sample-level evidence 接线
4. 最后重建一份新的、可审计的测试基线，并回写文档。

当前已完成第 3 步的第一项：`SystemMtLauncher` production fallback removal。

## 8. 验证策略

本轮及后续计划验证分两层：

- **文档层**：所有更新后的状态必须能回指到具体实现文件或提交。
- **运行时层**：涉及 catalog、evidence、DI 的改动，必须补对应测试或现有测试回归验证。

补充约束：

- CodeGraph 的 `query` 结果可用于确认当前结构关系。
- 对本仓库当前这组关键文件，CodeGraph `affected` 没有稳定返回测试文件，因此**不能**把
  `affected` 当成唯一回归面发现手段；测试面仍需结合现有测试类、命名和直接文件阅读确认。

## 9. 产出

本设计对应两类产出：

1. 文档对齐改动：
   - `AGENTS.md`
   - `CLAUDE.md`
   - `docs/PROJECT-STRUCTURE.md`
   - `docs/requirements.md`
   - `docs/design/v2-system-mt-architecture.md`
2. 一份按依赖顺序组织的开发计划：
   - 先事实源对齐
   - 再架构清障
   - 再测试基线固化

## 10. 预期结果

完成本轮后，应满足以下条件：

- 新加入的开发者仅看核心文档，就能得到与 `main` 一致的项目状态；
- Stage 8 后续工作不再建立在“Trend 仍活跃”或“catalog 已完全收敛”这类错误前提上；
- 当前运行时债务被明确排序，后续开发可以直接按计划执行。
