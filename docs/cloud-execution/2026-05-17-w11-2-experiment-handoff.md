# 新 Session 接手提示词 — W11.2 真实 Multi-LLM 实验复跑

> **目的**：当前 Claude Code Web session 无法刷新网络白名单（白名单改动仅对新 session 生效）。
> 用户已在新 cloud environment 加入 `api.bltcy.ai`，需要在**新 session** 里继续 W11.2 实验。
> 把下面这段（"开场提示词"以下）粘贴到新 session 的第一条消息。

---

## 当前项目状态（截至 2026-05-17 03:30 UTC）

| 项 | 值 |
|----|----|
| Repo | `meng004/MetBench-V2.1.4_2` |
| 主分支 | `main` |
| 最新 commit | `04f987477c02e3637f793b2337a1c9a830a783fb` (含 W11.1 + W11.2 全部代码) |
| 已发布 tag | `v2.1.0-rc1`（已 push 到 origin） |
| 测试态 | 495 pass / 2 skip / 0 fail（不含 env-gated 实验） |

## 你已经 ship 的 W11 工作

| PR | 内容 | 状态 |
|----|------|------|
| #45 / #46 / #47 / #48 / #49 / #50 / #51 / #52 / #53 / #55 | v2.1.0-rc1 主体 + W11.1 + W11.2 代码 | ✅ merged |
| **#54** | W11.3 F13 + F11 解锁 RFC（doc-only） | 🟡 **CI flaky 失败，待 rebase 重跑** |

## 待办

### A. **首要** —— 重跑 W11.2 真实 Multi-LLM 实验

上一 session 在沙箱白名单未生效时跑过一次，结果：
- DeepSeek 20/20 ✅，OpenAI / Claude 各 0/20 ❌（403 Host not in allowlist）
- 1 家结果存在 `docs/experiments/2026-05-w11-llm-consensus/results.json`，README 已说明限制
- 你的任务：3 家全通的情况下重跑，覆写 results.json，更新 README 分析

### B. **次要** —— 处理 PR #54 flaky CI

doc-only PR，估计是 OpenMOC 集成测试 CI 冷启动 flaky。merge 进 main 后 rebase 即可。

### C. **可选** —— W12 F13 OpenMC 接入（待用户显式启动）

按 `docs/superpowers/plans/2026-05-17-f13-third-sut-rfc.md` 末尾 checklist 走。

## 开场提示词（粘贴到新 session）

```
继续 MetBench v2.1 的 W11.2 实验任务。上一 session 完整 handoff 见
docs/cloud-execution/2026-05-17-w11-2-experiment-handoff.md。

要点：
1. main commit 04f987477c02e3637f793b2337a1c9a830a783fb 含全部 W11 代码
2. W11.2 真实 Multi-LLM 实验上次只 DeepSeek 跑通，OpenAI + Claude 因沙箱
   白名单未生效 403。新 session 白名单已加 api.bltcy.ai，需要重跑。
3. 我现在给你 LLM keys，请：
   a) 验证 api.bltcy.ai 在本 session 可达（curl -X POST /v1/chat/completions）
   b) 把 keys 写入 .env（.env 已 gitignored，不要 commit）
   c) METBENCH_LLM_EXPERIMENT=1 dotnet test
      MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj
      --filter "FullyQualifiedName~MultiLlmRealExperiment"
   d) 跑通后 results.json 覆写；更新 docs/experiments/2026-05-w11-llm-consensus/README.md
      把"沙箱限制" 段改成 3 家都跑通的真实结果
   e) 开 PR "W11.2 续 真实 Multi-LLM 实验数据"
4. PR #54 doc-only 的 RFC，CI flaky 挂了。等 main 移动后帮我 rebase + 重跑 CI + merge
5. 不要做 W12 F13 OpenMC 接入除非我明确说启动

下面是 LLM keys，仅写入 .env 用，不要 echo / log / commit：

# DeepSeek
DEEPSEEK_BASE_URL=https://api.deepseek.com
DEEPSEEK_API_KEY=<同前一 session>
DEEPSEEK_MODEL=deepseek-v4-pro

# OpenAI via bltcy.ai
OPENAI_BASE_URL=https://api.bltcy.ai
OPENAI_API_KEY=<同前一 session>
OPENAI_MODEL=gpt-5.5

# Claude via bltcy.ai (same key)
CLAUDE_BASE_URL=https://api.bltcy.ai
CLAUDE_API_KEY=<同前一 session>
CLAUDE_MODEL=claude-opus-4-7

跟我说连通性 OK 后再跑实验，不要直接消耗 LLM 配额。
```

## 新 Session 怎么 verify 自己接得对了

```bash
# 1. 在仓库根（自动）
pwd  # 应该是 /home/user/MetBench-V2.1.4_2 或类似

# 2. main commit
git log --oneline -3
# 期望 top 3 commit 含 W11.2 / W11.1 续 / W11.1 骨架 字样

# 3. 实验脚本存在
ls MetBench_SystemMT.Tests/Experiments/MultiLlmRealExperiment.cs
ls docs/experiments/2026-05-w11-llm-consensus/

# 4. 当前实验状态
cat docs/experiments/2026-05-w11-llm-consensus/README.md | head -50

# 5. 已知 PR
gh pr list  # 或 mcp__github__list_pull_requests
# 期望: #54 RFC open + 状态 conflicts/needs-rebase
```

## 验证 bltcy.ai 白名单生效（**新 session 第一件事**）

```bash
curl -sI https://api.bltcy.ai/ -o /dev/null -w "%{http_code}\n"
# 期望: 不是 403 (有 x-deny-reason: host_not_allowed 就是没生效)
# 200 / 401 / 404 / 405 都 OK — 任何不是 403 + host_not_allowed 都说明通了

# 真正测：
curl -s -X POST https://api.bltcy.ai/v1/chat/completions \
  -H "Authorization: Bearer $OPENAI_API_KEY" \
  -H "Content-Type: application/json" \
  -d '{"model":"gpt-5.5","max_tokens":16,"messages":[{"role":"user","content":"reply ok"}]}' \
  | head -3
# 期望: 不出现 "Host not in allowlist"；出现 JSON 含 choices[0].message.content 即通
```

## 复跑实验后的产物

| 文件 | 应包含 |
|------|--------|
| `docs/experiments/2026-05-w11-llm-consensus/results.json` | 60 次调用全成功（DeepSeek 20 + OpenAI 20 + Claude 20） |
| 同上 README.md | 重写"沙箱限制"段，改成真实 3 家 consensus 统计 |
| 论文措辞段 | 给出真实数字（如 unanimous=N/20, mean κ=X, accuracy=Y%） |

## 安全

- `.env` 永远 gitignored —— 已 verified `git check-ignore .env` 返回 `.env`
- LLM keys 不要 echo / log / commit
- 在新 PR description / commit message 中可以提"使用 .env keys"但不放实际 key

## 已合并到 main 的 v2.1.0-rc1 主体

| 范围 | 内容 |
|------|------|
| 论文核心 | F9 R-Case 自动复现 + F12 Multi-LLM Consensus + Cohen's κ |
| 论文加分 | W11.1 SCG-Heuristic Discoverer（第 3 类 MR 识别） |
| 主线 | F5/F6/F7/F10/F14/F15/F16/F18/F19 |
| UAT | 46 用例 + 评价表 + 任务书 + governance + baseline |
| 数据 | W11.2 实验 infrastructure + 20 prompt 集 + DeepSeek-only 初步数据 |

## UAT 是否启动了？

目前**未启动**测试员 round-1。release notes / 任务书 / 评价表 / baseline 全部就绪，等用户指令下发给测试员。新 session 不要主动联系测试员，**除非用户明确说**。
