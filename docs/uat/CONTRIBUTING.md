# 新增 / 修改 UAT 用例的规范

> 测试用例的演进遵循 doc-as-code：所有改动走 PR，CI 跑 markdown lint，下发人 review 后 merge。

## 谁可以加新用例

| 角色 | 可以做什么 |
|------|-----------|
| 测试工程师 | 提案新 UC（写在 GitHub Issue `uat-enhancement`） + 起 PR |
| 开发负责人 | 发版前主动补缺 UC（如 W11 新功能） |
| 项目负责人 | review + merge UAT 改动 |
| 任何 contributor | 通过 PR 提案 |

## 加新 UC 的流程

### 1. 先开 Issue 讨论（避免重复 / 范围漂移）

```
Title: uat-enhancement: <一句话描述新覆盖点>
Body:
  - 为什么需要：
  - 涉及的功能：
  - 预期通过条件：
  - 平台：Linux / Windows / 双端
  - 严重度：🔴 / 🟡 / 🟢
```

Label: `uat-enhancement`。

### 2. 选用例编号

| 现有 | 范围 | 下一个空号 |
|------|------|-----------|
| UC-A1 .. UC-A8 | 管理 CRUD | UC-A9 |
| UC-B1 .. UC-B9 | MR 主流程 | UC-B10 |
| UC-C1 .. UC-C9 | MR 发现 & 验证 | UC-C10 |
| UC-D1 .. UC-D2 | R-Case 复现 | UC-D3 |
| UC-E1 .. UC-E7 | 可视化 & 报表 | UC-E8 |
| UC-F1 .. UC-F5 | 持久化 & schema | UC-F6 |
| UC-G1 .. UC-G5 | 运营 & 性能 | UC-G6 |
| UC-H1+ | 新类别（如 multi-tenant / cluster） | UC-H1 |

不要插入空号 / 不要复用废弃号；保持单调递增。

### 3. 同时改三处

新增一个 UC 必须同步改动：

| 文件 | 加什么 |
|------|--------|
| [`test-procedures.md`](test-procedures.md) | 整段执行步骤 + 期望输出 |
| [`acceptance-rubric.md`](acceptance-rubric.md) | 评价表对应类别表里加一行（含严重度标记） |
| [`sample-data/`](sample-data/) | 若需要输入样本，加一个 `uat-<uc-id>-<purpose>.json/txt` |

下面是 **新 UC PR 检查清单**（CI 会跑，本地也可对照）：

- [ ] UC 号在 test-procedures + acceptance-rubric 都出现且严格一致
- [ ] 严重度标记（🔴/🟡/🟢）在 rubric 行右侧
- [ ] 包含可复现命令 / 步骤（无歧义）
- [ ] 含明确的 "✅ 期望"
- [ ] 平台标识（Linux only / Windows only / 双端）
- [ ] 若新增 sample data，文件名前缀 `uat-`，`.env` 等密钥**不**入仓
- [ ] **本地 dry-run 跑通**（CLI 用例必须真跑过；UI 用例至少一名 reviewer click 通过）

### 4. PR 标题约定

```
docs(uat): + UC-<X>N <一句话>
```

例如：

- `docs(uat): + UC-C10 SCG-Heuristic discoverer 跑通`
- `docs(uat): + UC-H1 multi-tenant DB 隔离`

### 5. Merge 后

- dashboard.md "Commentary" 写一行：N 个新 UC 在第几轮起生效
- 通知下发人 + 测试工程师，下轮起按新 list 跑

## 改既有 UC 的流程

| 改动类型 | 流程 |
|---------|------|
| **修 typo / 命令 flag 错误** | 直接 PR，标题 `docs(uat): fix UC-<X>N <reason>` |
| **改严重度** | 必须先开 Issue 讨论（影响 Release 决策） |
| **改通过准则** | 必须先开 Issue + 拿到下发人显式批准（影响历史 baseline 可比性） |
| **废弃 UC** | 不删除 —— 改成 `[DEPRECATED]` 前缀 + 注明原因，保留号位 |

## 版本控制

- 每次 UAT round 发起前，下发人在 dashboard 标记**本轮基线 commit**
- 测试员按那个 commit 跑（避免边跑边改的歧义）
- UAT 改动 merge 后**不**追溯历史轮次 —— 历史轮次的 rubric / procedure 永远是当时 commit 的版本

`git log docs/uat/` 给出所有 UAT 文档演进；`git blame docs/uat/test-procedures.md` 给出每条 UC 是谁、什么时候加的、关联哪个 Issue。

## 持续改进

每轮 UAT merge 后，下发人在 dashboard.md "Commentary" 段写：

1. 本轮新发现的 issue 是否反映了 UAT 覆盖盲区？→ 开 `uat-enhancement` Issue
2. 哪些 UC 多次 flaky？→ 改成 `[FLAKY]` 标记 + 起 Issue 修底层不稳定
3. 哪些 UC 多轮全通过？→ 考虑收编进 CI 自动化（迁出 UAT，进 `MetBench_SystemMT.Tests`）

UAT → CI 收编是健康循环：UAT 用例固化、CI 自动跑、测试员关注新 / 不稳定的点。
