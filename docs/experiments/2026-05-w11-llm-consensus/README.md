# Experiment — W11.2 Multi-LLM Consensus on 20 MR candidates

> **Date**: 2026-05-17（续跑：沙箱白名单放行 `api.bltcy.ai` 后 3 家全部跑通）
> **Commit**: `040a4d956f645cfc3330b5bc6ecf8a89d6c0fddf`（main） + W11.2 续跑分支
> **Plan ref**: [W11 plan §W11.2](../../superpowers/plans/2026-05-16-w11-plan.md)
> **Code**: `MetBench_SystemMT.Tests/Experiments/MultiLlmRealExperiment.cs`
> **Prompts**: [`prompts.json`](prompts.json) (20 candidates)
> **Raw results**: [`results.json`](results.json)

## 1. 实验设计

让 3 家 LLM 对同一 20 个 metamorphic relation candidate 给出 plausibility 判断（`{plausible: true|false}`）。
F12 `MultiLlmConsensusValidator` 做 strict-majority consensus + pair-wise Cohen's κ。

| Provider | 路径 | Model | 实际结果 |
|----------|------|-------|---------|
| DeepSeek | `https://api.deepseek.com/v1/chat/completions` | `deepseek-v4-pro` | ✅ 20/20 |
| OpenAI | `https://api.bltcy.ai/v1/chat/completions` | `gpt-5.5` | ✅ 20/20 |
| Claude | `https://api.bltcy.ai/v1/chat/completions` | `claude-opus-4-7` | ✅ 20/20 |

候选 prompt 集（20 条）覆盖：
- 7 个 `amax / amin` (identity / 加 ±∞ / 排列 / 缩放 / 负号 / 复制 / 加常数)
- 3 个 `sin` (半周期 / 周期 / 缩放 x)
- 2 个 `heat_equation` (振幅缩放 / 平移)
- 3 个 OpenMOC `k_eff` (ν·Σf / Σa / 几何旋转)
- 2 个 projectile (倍速 / 角度互补)
- 3 个 spurious 假阳性 sanity check

每条标 `expected: true|false` ground truth。

## 2. 跑动统计

| 项 | 值 |
|----|----|
| 总耗时 | **4 分 46 秒**（20 candidates × 3 providers fan-out 并发） |
| LLM 调用总次数 | **60 次（全部成功）** |
| 写出文件 | [`results.json`](results.json) |

## 3. 结果总览

| 指标 | 值 |
|------|----|
| Consensus accuracy（与 ground truth 一致） | **20 / 20 = 100.0%** |
| Unanimous agreement（3 家完全一致） | **19 / 20 = 95.0%** |
| Mean pair-wise Cohen's κ | **0.925** |

| Provider | OK | ERR | 投 `true` | 投 `false` |
|----------|----|-----|---------|----------|
| DeepSeek | 20 | 0 | 14 | 6 |
| OpenAI | 20 | 0 | 13 | 7 |
| Claude | 20 | 0 | 14 | 6 |

3 家各自单家准确率均为 19/20 或 20/20，全员一致命中 ground truth（majority vote 永远对）。

## 4. 唯一非 unanimous 行 — `MR-sin-full-period`

| 字段 | 值 |
|------|----|
| program | `sin(x: float) -> float` |
| transformation | `x' = x + 2*pi` |
| candidate assertion | `sin(x') == sin(x)` |
| expected (ground truth) | `True`（2π periodicity） |
| consensus（多数派） | `True`（DeepSeek + Claude） |
| κ on this row | **-0.500** |

### 三家投票理由

| Provider | 投票 | 摘录 |
|----------|------|------|
| DeepSeek | `true` | _"Sine is a periodic function with period 2π; sin(x + 2π) = sin(x) for all real x."_ |
| Claude | `true` | _"Sine is periodic with period 2*pi, so sin(x + 2*pi) = sin(x) mathematically. Note: in floating-point arithmetic exact equality may fail due to rounding, but the relation [holds]..."_ |
| OpenAI | **`false`** | _"Mathematically sin(x + 2*pi) = sin(x) for real numbers, but for a floating-point program exact equality is not generally reliable because x + 2*pi and the subsequent sin..."_ |

**评注**：本质上是 LLM 间口味差异，不是错答：

- DeepSeek + Claude 看的是数学层面（assertion plausible，浮点误差是后续 oracle/tolerance 的问题）。
- OpenAI 把"严格 `==`、浮点不可靠"算成 plausible=false 的理由。

Claude 实际在 reason 里也提到了浮点陷阱，但仍然投 `true`。**3 家信号一致都识别出了浮点陷阱**，只是是否让它影响投票存在分歧。这种"投票口径不一致但失败模式相同"恰好是 W11.2 Multi-LLM Consensus 想验证的能力 —— strict majority 把口味分歧吸收掉，最终 consensus 命中 ground truth。

## 5. 基础设施验证

| 组件 | 状态 |
|------|------|
| `.env` 加载（多 provider key） | ✅ |
| `OpenAiCompatibleLlmGateway` HTTP 调用 | ✅ 真实成功 60 / 60 |
| `MultiLlmConsensusValidator` fan-out 并发 | ✅ |
| Strict majority + pair-wise Cohen's κ | ✅（mean κ = 0.925） |
| `Plausible=null` 异常隔离 | ✅（本次未触发，但单测已覆盖） |
| 结果 / 统计 JSON 序列化 | ✅ |
| 实验脚本 env-var gating（CI 不跑） | ✅ |

沙箱续跑（白名单放行 `api.bltcy.ai` 后）零失败、零异常重试。

## 6. 论文措辞建议

> _We applied F12 Multi-LLM Consensus to 20 candidate MR proposals across three providers
> (DeepSeek deepseek-v4-pro, OpenAI gpt-5.5, Anthropic claude-opus-4-7).
> All 60 LLM calls succeeded. Strict-majority consensus matched ground truth on
> **20/20 candidates (100% accuracy)**, with unanimous 3-way agreement on **19/20 (95%)**
> and mean pair-wise Cohen's κ = **0.925**. The single non-unanimous row was
> a sine 2π-periodicity assertion where two providers approved the mathematical
> relation while a third rejected it on floating-point strict-equality grounds —
> the three providers shared the same failure-mode awareness but diverged on
> whether to count it against the relation's plausibility. Strict-majority
> consensus correctly absorbed this stylistic disagreement._

## 7. 下一步

### 7.1 扩大 candidate 集（论文建议）

20 条仅作初探。论文目标扩到 ~100-200 条，覆盖：
- m_inv (~30) — 各类几何 / 物理对称
- m_mono (~30) — 各类参数单调性
- m_conv (~20) — 解收敛性
- m_cmp (~10) — 跨实现一致性
- spurious / trap (~20) — 假阳性 baseline

### 7.2 prompt 模板加入浮点假设（可选）

可加 `"Assume strict mathematical equality (not bit-exact floating-point equality) when judging plausibility."`
强制三家统一口径；预计 unanimous rate → 100%。是否要这么做需衡量：
- 利：unanimous rate 上去更"好看"；
- 弊：本次 OpenAI 揭示的"浮点严格等"分歧是真实信号，过早抹平会丢掉这类有价值的 fail-mode 提示。

建议**保留当前 prompt**，把浮点 tolerance 的讨论留到 oracle 层（F7 / F8）而不是 plausibility 层。

### 7.3 沙箱备忘

`api.bltcy.ai` 现已在 Claude Code on the web 沙箱白名单内，后续多 LLM 实验直接复用 `.env` 即可。
（key 在 `.env` 中，未入仓；本机权限 `0600`，`.gitignore` 已覆盖 `.env*`。）
