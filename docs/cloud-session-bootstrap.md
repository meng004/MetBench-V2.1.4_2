# Cloud Session Bootstrap — MetBench-V2

> 复制下方「---」之间的整段，作为新 cloud (Linux) Claude Code session 的启动 prompt。自包含——session 无历史。
> **具体任务由启动对话给出**：本文档只负责环境准备、上下文与约定，不写死任务，也不嵌入会过期的状态快照（HEAD / 测试基线实时从 git 与状态账本读取）。

---

你是一个在 cloud Linux 环境运行的 Claude Code session，接手 **MetBench-V2** 项目。无本会话历史，以下自包含。

## 项目
系统级蜕变测试（System-MT）平台，.NET 8 + WPF。仓库 `meng004/MetBench-V2.1.4_2`。**先读** `CLAUDE.md`（编码/协作约定）+ `AGENTS.md`（路线图）+ `docs/status/current.md`（状态账本：当前主线状态、代码测试基线、活跃风险）。当前 main 头与测试基线**实时**取自 `git rev-parse origin/main` 与 `docs/status/current.md`，不要假设固定值。

## 环境准备（首次必跑）
```bash
git fetch origin && git switch main && git pull --ff-only
bash .claude/web-setup.sh         # .NET 8 + OpenMOC/OpenMC venv + Python 科学栈，首次 5–15min，幂等
export METBENCH_OPENMOC_PYTHON=/opt/openmoc-venv/bin/python
export METBENCH_OPENMC_PYTHON=/opt/openmc-venv/bin/python
export METBENCH_DB_PATH=/tmp/metbench.litedb
# 只跑跨平台测试、跳过重编译: SKIP_OPENMOC=1 SKIP_OPENMC=1 bash .claude/web-setup.sh
```
验证：`dotnet test MetBench_SystemMT.Tests` 应全绿（通过数见 `docs/status/current.md` 的 baseline；OpenMOC/OpenMC 视 venv 自动 skip）。`git pull` 后若用 codegraph，先 `codegraph index --force`（`sync` 因 mtime 漏检）。

## cloud 能做 / 不能做（CLAUDE.md §9）
- ✅ `MetBench_BLL.Core` / `MetBench_DAL` / `MetBench_SystemMT.Tests` / docs / CI（net8.0，Linux 可编译 + 测试）。
- ❌ **不能编译 WPF**（`MetBench_Client`，net8.0-windows7.0，Linux 无 WindowsDesktop targets → MSB4019）。WPF 只能改源码 + 标 VM-track，交 Windows VM 验证。

## 关键约定
- **PR 门禁 §12**：所有 main 改动走 PR、填 `docs/superpowers/templates/pr-gate-checklist.md` 各节、required check 名 `test`。
- **GraphQL 限流**：`gh pr create/merge/checks` 走 GraphQL，限流时改 REST（core 配额独立）：建 `gh api repos/{o}/{r}/pulls -X POST -f title= -f head= -f base=main -f body=`、合 `PUT /pulls/{n}/merge`、查 CI `GET /commits/{sha}/check-runs`、查额度 `gh api rate_limit`。
- **merge 守卫**：`merge` 与删分支**分步**——`mergeable_state=clean` 才 merge、返回 `merged:true` 才删 head 分支；`405/blocked` = base 落后，先 `git rebase origin/main` + force-push。
- TDD + systematic-debugging（先根因后修）+ 最小修改（§0.5）+ 显式报错（§0.6）。

## 任务
具体任务由本次启动对话给出。先完成上面的环境准备与上下文阅读，再等待 / 执行对话中的指派。

---
