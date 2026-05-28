# T1 Non-MR CRUD Chain Post-Merge Review

> **Date**: 2026-05-28
> **Status**: Active review record
> **Chain**: PR #219 / #221 / #223 / #225 / #224 / #229 (6 PRs)
> **Plan**: [`docs/superpowers/plans/2026-05-28-t1-non-mr-crud-windows-vm-plan.md`](../plans/2026-05-28-t1-non-mr-crud-windows-vm-plan.md)
> **Follow-up plan**: [`docs/superpowers/plans/2026-05-28-t1-followups-plan.md`](../plans/2026-05-28-t1-followups-plan.md)
> **Reviewer**: fresh-session Explore agent per CLAUDE.md §12.4 R2
> **Authored by**: F1 chain-end review item in `docs/superpowers/plans/2026-05-28-t1-followups-plan.md` §3

decision-record: docs/superpowers/specs/2026-05-28-t1-non-mr-crud-chain-post-merge-review.md (this file)

---

## §1 Methodology

按 CLAUDE.md §12.4 R1-R4 元规则集逐条审查：

- **Diff range**: `origin/main~6..origin/main` → `90272df..aa4d11e` (累计 6-PR delta)
- **Anchored docs**: 主 plan `2026-05-28-t1-non-mr-crud-windows-vm-plan.md`、`chain-end-review-checklist.md`、`CLAUDE.md §12.4 R1-R4 + §12.5`
- **Cross-reference**: 4 个 PR body 含 VM verification 实测数据（13 seeds / fourier conflict test / 8-18 fact counts 等）
- **Mode**: read-only Explore agent (无代码改动权限)；findings 转 §3 action items

### 检查面

| 元规则 | 范围 |
|---|---|
| R1 cross-projection parity | 6 PR 新增 record / POCO 多投影路径检查 |
| R2 chain-end review 自身 | 即本次 review；不递归审 |
| R3 spec drift retrospective | plan 文档与实施措辞一致性 |
| R4 contract↔fact pairing | 新增 public method XML doc 是否有对应 fact |

---

## §2 Findings

### §2.1 Cat A — Single-PR visible (1 finding)

#### F1-CatA · plan 文档 spec drift（R3 触发）

**位置**：`docs/superpowers/plans/2026-05-28-t1-non-mr-crud-windows-vm-plan.md`

| Line | Plan 文字 | 实际实现 |
|---|---|---|
| 17 | `12 个 seed EquationMetadata` | 13 个（12 physics + `_test_csv` 合成）|
| 78 | `12 个 seed EquationMetadata（reactor 5 锚定 + T3 7 扩展）` | 同上，13 个；reactor 5 + T3 7 + `_test_csv` 1 |
| 174 | `测试覆盖 boltzmann / Boltzmann / BOLTZMANN 三种输入` | 实际 Theory InlineData 是 `fourier / Fourier / FOURIER / BoLtZmAnN` 四个（参见 `MetBench_SystemMT.Tests/SystemMT/Metadata/Editing/SystemMtEquationEditorTests.cs:60-63`）|

**根因**：PR-2 (#223) VM agent 在 PR body §4 已记录两个 substitution（seed count 12→13、conflict key boltzmann→bateman），但 plan 源文档没有 retrospective 修订。CLAUDE.md §12.4 R3 要求："Phase-K spec 文档若推荐了候选 X，而 Phase-N 实施时换成候选 Y，**Phase-N 的同一 PR 或紧随的 doc PR 必须 re-touch 该 spec**"。本链路 #223 / #225 / #224 都未触发 retrospective doc PR，本 chain-end review 后 doc PR 是补救路径。

**严重度**：低（不影响代码正确性，但破坏 plan 作为参考样板的可信度）

**修复**：本 review 同链 cleanup PR (PR-FUP-2) 修订 3 行 plan 措辞

### §2.2 Cat B — Cross-PR / Retrospective (2 findings)

#### F2-CatB · parity test gap on new editors（R1 触发）

**位置**：4 个新 editor 在 `MetBench_BLL.Core/SystemMT/{Catalog,Metadata,Persistence}/Editing/`

**现象**：

| Editor | 后端 fact 数 | Parity test (BLL Editor draft ↔ WPF VM ↔ persistence) |
|---|---|---|
| `SystemMtSutEditor` (PR-1) | 8 | ❌ |
| `SystemMtEquationEditor` (PR-2) | 11 | ❌ |
| `SystemMtSampleCaseEditor` (PR-3) | 9 | ❌ |
| `IExecutionHistoryEditor` (PR-4) | 18 (含 4 LegacyResultMirrorTests) | ❌（mirror parity 有，但跨 editor↔VM 投影 parity 无）|

每个 editor 现在都是单投影路径（BLL editor → WPF VM），按 CLAUDE.md §12.4 R1 / §12.5 标准是「single editor↔VM 投影 → 不需要 parity test」。**但 PR-4 引入了一个新多投影场景**：`SystemMtResultRecord` 现在被两条 write 路径产生（legacy `ISystemMtResultRepository.SaveAsync(string,SystemMtResult)` 历史路径 + 新 `SaveAsync(SystemMtResultRecord)` 镜像路径）。

虽然 `LegacyResultMirrorTests` (4 facts) 验证了 mirror 路径的 Id == ExecutionId 契约，但**没有 parity test 验证两条 write 路径产生的 `SystemMtResultRecord` 在结构上等价**（即如果未来调整了 `FromResult` 或 mirror 投影，会不会出现两路 `MrName` / `RunAt` / `SourceCaseName` 等字段填充策略发散）。

**严重度**：中（不影响当前正确性；若未来调整任一 write 路径会失去守护）

**修复建议**：新增 `LegacyResultRecordParityTests.cs`，断言：
- 同一 `SystemMtResult` 经 `SaveAsync(mrName, result)` 与同语义 `SystemMtResultRecord` 经 `SaveAsync(record)` 后，重新 `GetAsync` 的字段集等价（除 `Id` / `RunAt` 因 autoId / 时戳差异）
- 这是 R1 cross-projection parity 的标准应用

转 §12.5 §3 行：`LegacyResultRecordParityTests.cs` 守 R1（multi-write-path on `SystemMtResultRecord`）

#### F3-CatB · `SystemMtExecutionRecorder` XML doc 缺合约说明（R4 触发）

**位置**：`MetBench_BLL.Core/SystemMT/Pipeline/SystemMtExecutionRecorder.cs:13-27`

**现象**：类级 `<remarks>` 块当前说明：
- v2 结果 schema 统一写入口
- Execution 总写、Result 条件写、Anomaly 不写

**缺漏**：没有明确说明 PR-4 引入的 `_legacyResults != null` 分支语义：
- 当 ctor 注入 `ISystemMtResultRepository` 时，会在 V2 Result 写入后镜像一份到 legacy `SystemMtResults` 集合
- 镜像 `Id = executionId`（V2 `Execution.IdExecution`），保证跨 collection 删除 join 工作
- LegacyResultMirrorTests 守 4 个 fact（happy / null-repo / no-assertion / failure-reason）

ISystemMtResultRepository.SaveAsync(SystemMtResultRecord, CT) 新 overload XML doc 是清晰的（Id 保留 / autoId 语义），但 recorder 类作为该方法的唯一生产消费者，其类级说明没承接这条契约。

**严重度**：低（合约 + fact 都齐备，只缺类级文档化）

**修复建议**：在 SystemMtExecutionRecorder.cs `<remarks>` 块末尾追加 1 段说明 legacy mirror 语义 + 指引读者看 `LegacyResultMirrorTests`。

转 §12.5 §3 行：N/A（这是文档改进，不需新 guard test；改文档即闭环）

---

## §3 Action items

| ID | Item | 类别 | 优先级 | 行动 |
|---|---|---|---|---|
| **F1-CatA** | plan 文档 line 17/78/174 spec drift | Cat A R3 | P1 | 本 PR (PR-FUP-2) 同链修订 |
| **F2-CatB** | 缺多 write-path parity test on `SystemMtResultRecord` | Cat B R1 | P2 | Codify §12.5 行 → `LegacyResultRecordParityTests.cs`；defer 实施给后续 cleanup PR |
| **F3-CatB** | `SystemMtExecutionRecorder` XML doc 没说 legacy mirror 语义 | Cat B R4 | P2 | 本 PR 同链补 XML doc remarks（小改） |

### §3.1 §12.5 表新增行（codify）

```
| `LegacyResultRecordParityTests.cs` | B | multi-write-path SystemMtResultRecord 字段不对称（L1 类） | R1 | 计划：下一 cleanup PR |
```

加在 CLAUDE.md §12.5 表 `*ParityTests.cs` 行附近。

---

## §4 Closure conditions

T1 非 MR CRUD 链路在以下条件全部满足后可以从 "Controlled" 升级到 "Controlled with chain-end review closed"：

- [x] Chain-end review 报告落档（本文件）
- [ ] F1-CatA plan 修订 commit landed
- [ ] F3-CatB Recorder XML doc remarks 补充 commit landed
- [ ] F2-CatB `LegacyResultRecordParityTests.cs` 已开 issue / 入下个 cleanup PR backlog
- [ ] CLAUDE.md §12.5 表新增 R1 行
- [ ] `docs/status/current.md` §3 T1 非 MR CRUD chain 行追加 "chain-end review closed" 标记

PR-FUP-2 一次性完成 F1-CatA + F3-CatB 修复 + §12.5 codify + status 标记。F2-CatB 留 cleanup PR backlog（非阻塞）。

---

## §5 Statistics

- Cloud CI baseline: **1509 / 0 / 12** (PR #224 head)
- New test facts across 4 editors: 8 (SUT) + 11 (Equation) + 9 (SampleCase) + 18 (ExecHistory incl. 4 LegacyResultMirrorTests) = **46 facts**
- Windows VM verification: 38 screenshots across 4 PRs at `docs/superpowers/specs/2026-05-28-pr-{1,2,3,4}-vm-verification/`
- Cat A findings: **1** (P1, fix in PR-FUP-2)
- Cat B findings: **2** (P2; F3 fix in PR-FUP-2, F2 deferred to cleanup PR)

---

## §6 Conclusion

链路功能正确性 0 blocker。3 个 process-level findings：1 个 P1（spec drift, 必修在 chain-end closure 前），2 个 P2（parity 缺漏 + 文档化）。建议 PR-FUP-2 一次性处理 F1-CatA + F3-CatB + §12.5 codify；F2-CatB 进 backlog。

---

## §7 Out of scope

- 本 review 不审 v2 governance charter rollout (#215-#228) — 已在 PR #227 独立 review
- 本 review 不重新评估 SutRoot bin/Debug 决策 — F3 follow-up 已有独立 spec
- 本 review 不评估 T5 anomaly cleanup — F4 follow-up 已有 scoped plan
