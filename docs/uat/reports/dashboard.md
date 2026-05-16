# UAT Dashboard — 历次轮次趋势

> 每轮 UAT merge 后，下发人在本表追加一行；趋势用于决策 release / regression / improvement priority。

## 总览

| 轮次 | 日期 | commit | 测试员 | 平台 | Pass% | Blocker | Major | Minor | 总评 | 报告 |
|------|------|--------|--------|------|-------|---------|-------|-------|------|------|
| baseline | 2026-05-16 | `97863ea` | dev | Linux | 100% | 0 | 0 | 0 | reference | [`baseline-2026-05-16/`](baseline-2026-05-16/) |
| _round-1_ | _待排_ | | | | | | | | | |

## 趋势分析约定

下发人在每轮 merge 后 5 分钟更新本表 + 在下方写 2-3 句 commentary：

- **regression**：相邻轮次某类用例从 ✅ 变 ❌ → 标 🔴
- **new coverage**：本轮新增 UC → 标 🟢
- **flakiness**：同一 UC 多轮内偶尔 ❌ → 标 🟡

## Commentary（追加式，倒序）

### 2026-05-16 baseline
开发侧自跑作为 reference：458 facts pass / 0 fail / 22.35s cumulative。OpenMOC venv OK。 全部 7 类的 CLI 用例都跑通。

### _待写_

---

## Release 决策矩阵

依据本表 + 评价表 acceptance-rubric 的"Release 通过准则"：

| 条件 | 决策 |
|------|------|
| 连续 2 轮 PASS + 0 Blocker | **可发版** |
| 1 轮 CONDITIONAL PASS + 全部 Major 已有 fix PR | 待 fix merge 后再验 1 轮 |
| 任意轮 FAIL | 修复回归至少 1 轮 ALL-PASS 才能发版 |

## Tag 约定

每次发版 / 重大里程碑 → 给仓库打 tag：

```
uat-v2.1.0-round-1     # 第 1 轮 UAT 跑通的 commit
uat-v2.1.0-round-2
release-v2.1.0         # 发版基线（dashboard 决策"可发版"后打）
```

```bash
# 下发人在 round merge 后立刻打
git tag -a uat-v2.1.0-round-N -m "UAT round N: <PASS/CONDITIONAL/FAIL>"
git push origin uat-v2.1.0-round-N
```
