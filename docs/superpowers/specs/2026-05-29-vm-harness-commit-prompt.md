# VM Prompt — Decide & execute: commit the FlaUI/UIA screenshot harness to `tools/`?

> Copy the section below to the Windows VM (Claude Code / operator). Self-contained — the VM session has no
> cloud context. This is a **decision-first** prompt: the VM evaluates whether the harness is worth keeping,
> then acts accordingly. The harness currently lives **outside the repo** at `C:\Users\codex\debt5uia\`.

---

你在 Windows VM（有 FlaUI/UIA3 环境）。debt #5 验证时你建了一个 FlaUI/UIA3 自动化 harness，在仓库外 `C:\Users\codex\debt5uia\`——它驱动 `MetBench_Client`、对 seeded 隔离 LiteDB 做交互断言并截图。现在评估它是否值得作为**可复用的 VM 截图 harness** commit 到仓库 `tools/`，并据评估执行。无本会话历史，以下自包含。

## 背景
- 仓库 MetBench-V2（.NET 8 + WPF）：cloud(Linux) 跑 BLL.Core/DAL/Tests，WPF 只能在 Windows VM 编译/运行/验证（CLAUDE.md §8/§9）。
- harness 的潜在价值：自动化 WPF GUI 截图 + UIA 程序化断言（比人眼截图强），未来任意 WPF 交互验证可复用，降低 VM 手动成本。

## 第一步：评估（你判断，你最了解 harness 实际内容）— 逐项给结论
1. **通用性**：是 debt#5 专用（硬编码 AnomalyList/AnomalyStatus），还是参数化可复用（任意页/控件/断言）？专用 → 复用价值低。
2. **依赖/可维护性**：FlaUI/UIA3 NuGet、Windows-only、需 seeded LiteDB——依赖是否清晰可声明？
3. **硬编码**：是否含 `C:\Users\codex\...` 绝对路径或机器特定配置？
4. **质量**：scaffolding-grade 一次性脚本，还是工程化（有结构、可维护）？
5. **CI 关系**：它进不了 cloud CI（WPF 不编译）——会不会被误以为有 CI 覆盖？

## 第二步：决策
- **判定值得 commit** → 整理后放 `tools/vm-screenshot-harness/`：
  - 去掉所有硬编码绝对路径，改参数/env（`--db <path>` / `--out <dir>` / 控件用 AutomationId）。
  - 加 `README.md`：Windows-only + FlaUI/UIA3 依赖 + 跑法 + **明确不进 CI、是 VM 侧半自动验证工具**。
  - 尽量参数化到"任意 WPF 页+控件+断言"的最小可复用形态，而非 AnomalyList 专用。
  - 走 §12 PR：新分支 → commit → PR targeting main → 填 `docs/superpowers/templates/pr-gate-checklist.md` 7 节（Windows Classification 注明 VM-only tool、不进 required check）。
- **判定不值得**（太专用/scaffolding/维护负担 > 复用收益）→ 保留在 repo 外，在 issue 或 PR 评论记一句"harness 留 VM 本地，原因 …"，不污染 `tools/`。

## 约束
- CLAUDE.md §0.5 最小修改、§9 cross-env（VM 拥有工具侧，勿动 `MetBench_BLL.Core` 契约）、§12 PR 门禁。
- **诚实**：若它本质是一次性 scaffolding，直说不值得 commit，别为"可复用"硬包装。

## 回报
评估结论（commit / 不 commit + 理由）；若 commit → PR URL + 整理要点（去了哪些硬编码、README 要点）。
