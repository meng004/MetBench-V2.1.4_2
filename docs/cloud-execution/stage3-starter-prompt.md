# Stage 3 starter prompt for Claude Code on web

Paste the prompt below as the first message in a new Claude Code on
web session attached to `meng004/MetBench-V2.1.4_2`.

---

```
你是要在 Claude Code on web 云端会话中实施 MetBench Stage 3：把 OpenMOC（mit-crpg/OpenMOC）作为 Stage 1+2 系统级 MT 流程的真实 SUT。

## 一次性启动流程

1. 先跑 `bash .claude/web-setup.sh`，等"Setup complete"。该脚本一次性安装 .NET 9 SDK、Python 3.12 + 科学栈、OpenMOC（branch 3D-MOC）、gh CLI，并预热 NuGet restore。失败重跑即可（idempotent）。首次约 5-10 分钟。
2. `gh auth status` 确认 GITHUB_TOKEN 已注入；否则 `echo "$GITHUB_TOKEN" | gh auth login --with-token`。
3. `dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj` —— 期望 25/25 passed（Stage 1+2 测试基线，假设 PR #4 已合并到 main；若仅 Stage 1 在 main 则 13/13 也可接受）。

## 工作目标

依据 `docs/superpowers/specs/2026-05-07-system-level-mt-bdd-design.md` 的 Stage 3 验收标准：

- MetBench/Reqnroll 启动 OpenMOC 跑一个 source case
- 系统准备或生成 follow-up case 并启动第二次运行
- OpenMOC 输出文件被解析出 MR-相关数值
- 至少一个 OpenMOC MR 端到端跑通并返回 pass/fail
- OpenMOC-specific 逻辑隔离在 Python adapter 中（C# 不感知 OpenMOC 细节）

## 推荐做法

1. 调用 superpowers:brainstorming 与我协作把 Stage 3 范围锁定到 1 个最小的 OpenMOC MR scenario（候选：2D pin-cell k_eff 对均匀稀释的单调性，或 1D slab 的截面变换）。
2. 用 superpowers:writing-plans 写出 Stage 3 实现 plan，落到 `docs/superpowers/plans/YYYY-MM-DD-stage3-openmoc.md`。
3. 用 superpowers:using-git-worktrees 创建 `feature/stage3-openmoc` worktree 隔离工作。
4. 用 superpowers:subagent-driven-development 按 plan 一个 task 一个 task 推进；每个 task 都用 TDD + 两阶段 review（spec compliance + code quality）。
5. 完成后用 superpowers:finishing-a-development-branch 推送 + 开 PR。

## 关键约束

- 不要修改 `MetBench_BLL`（WPF 依赖项），所有 Stage 3 业务代码放 `MetBench_BLL.Core/SystemMT/OpenMOC/` 或新建 `MetBench_BLL.OpenMOC/` 项目。
- OpenMOC adapter 的 Python 部分放 `SUT/openmoc/`，与 `SUT/projectile/`（如已合并）同级。
- 用 Stage 2 的 `MrTransformation` + `InputGenerator` 做 follow-up 输入生成；用 Stage 1 的 `PythonOutputAdapter` + 自定义 `parse-output` 实现解析 OpenMOC 的 HDF5/CSV 输出。
- 不要碰 WPF 项目（`MetBench_Client`）；云端没有 Windows runtime，`dotnet build MetBench.sln` 在 EnableWindowsTargeting=true 配置下能编译为 Windows 目标但不能运行 WPF。

## 已知前置 PR

- PR #4: Stage 2 input data generation —— 必须先合并到 main 才能开 Stage 3 plan
- PR #3: projectile demo SUT —— 可参考 `SUT/projectile/` 作为真实 SUT 接线模板
- PR #2: Docker Stage 3 base image —— 这个 PR 已经卡在 OpenMOC SWIG 路径调试 4 次；本仓库 `.claude/web-setup.sh` 已经修好同样的安装路径，PR #2 可以直接复用 setup.sh 的逻辑或废弃

## 报告

每个 task 完成后向我报告：
- Status（DONE / DONE_WITH_CONCERNS / BLOCKED / NEEDS_CONTEXT）
- 测试结果（数量）
- Commit SHA
- 任何 plan 偏离及原因

开始时先 git status 确认在 main 上 + 跑 setup.sh + 测试通过 + 等我下达 brainstorming 指令。
```

---

## When to update this prompt

- After Stage 3 spec changes (`docs/superpowers/specs/...stage3...`).
- After PR #4 / PR #3 / PR #2 status changes (update the "已知前置 PR" block).
- When the cloud VM image gets a new pre-installed tool that lets us
  drop a step from `web-setup.sh`.
