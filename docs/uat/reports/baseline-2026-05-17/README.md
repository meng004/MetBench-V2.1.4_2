# UAT Baseline — 2026-05-17

> 开发侧给测试员的 **参考基线** —— 测试员的 dry-run 结果应大体与这里一致；偏离过大说明环境异常。
> 替代 [baseline-2026-05-16](../baseline-2026-05-16/README.md)。本次 baseline 体现 W11-W12 一系列改动 land 后的 main 状态。

| 项 | 值 |
|---|---|
| 仓库 commit | `45a145f2396ef247110849a80eae1ad1eec56497` (main, post W11/W12 全部 PR 合并) |
| 平台 | Linux Ubuntu 24.04 cloud + OpenMOC venv (`/opt/openmoc-venv`) + OpenMC venv (`/opt/openmc-venv`) |
| .NET | 8.0.126 |
| OpenMOC | installed (`3D-MOC` branch), importable |
| OpenMC | installed (`master`), importable + binary on PATH (0.15.3) |
| LLM API | not exercised in baseline (UC-C4 走 fake gateway；如要跑 real experiment 见 [W11.2 实验包](../../../experiments/2026-05-w11-llm-consensus/)) |

## 自上次 baseline 后的关键变化（2026-05-16 → 2026-05-17）

| 改动 | PR | 测试影响 |
|---|---|---|
| W11.2 Multi-LLM consensus 真实跑通 | #57 | +1 LLM 实验 fact（env-gated，CI 不跑） |
| W12 F13 OpenMC 接入 | #57 | +1 smoke + 4 cross-program BDD（OpenMOC × OpenMC × 2 MR） |
| scenario→MR launcher 改名 | #58 | 测试方法名跟随改 |
| UAT BDD 21 用例 | #59 | +20 scenarios（UC-F/G/C） + 1 @ignore |
| UC-C11 unignore | #60 | UC-C11 active |
| LiteDB schema migration | #62 | +2 migration facts |
| UAT 三段式重写 | #63 | docs only |
| W12 F11 路径 A monitor | #61 | docs + workflow，不影响测试 |
| **DbConfig.Instance flake 修复**（本 baseline 同 PR） | 本 PR | `[Collection("DbConfigGlobal")]` 加到 6 个 class，根治 5 个 KeysetPagination flake |

## 整体结果

| 测试范围 | Pass | Skip | Fail | Cumulative wall |
|---|---|---|---|---|
| **BDD smoke** (Features filter — OpenMOC + OpenMC + Cross-program + Heat + Projectile + SystemLevel + UAT 21) | **30** | 1 | 0 | ~27 s |
| **全套 cross-platform suite** | **521** | **0** | **0** | **35 s** (wall) / **73.02 s** (cumulative) |
| 性能预算 | n/a | n/a | n/a | **< 120 s ✅** (cumulative 73.02s) |
| > 2000ms 慢测试 | **6 个** — 全部是 OpenMOC/OpenMC 真实物理跑（合理） | | | |

**100% Pass / 0 Skip / 0 Failed** — 历史首次完全清空 skip 列表（UC-C10 由本 baseline 自身解锁）。

## 文件清单

| 文件 | 大小 | 用途 |
|---|---|---|
| `baseline-bdd.trx` | ~50 KB | BDD smoke 30 scenarios + 1 skip |
| `baseline-full.trx` | ~742 KB | 全套 520 测试 trx |
| `perf-baseline.txt` | ~1 KB | `tools/ci_perf_baseline.py` 输出 (Pass / 6 slow / 71.58s) |
| (本 README) | | 验收员对比指南 |

## 测试员如何对比

打开你自己 dry-run 的 trx，对比：

1. **Pass 数** ≥ 520（环境若更全可能更多）
2. **Fail 数** = 0（任一 ≥ 1 都是阻断；含 KeysetPagination flake → 环境异常或缺 `[Collection]` 同步修复）
3. **Cumulative 时间** ≤ 2× baseline（即 ≤ ~150 s 内）
4. OpenMOC / OpenMC scenarios 不应该全 [SKIP] — 全 SKIP 说明 venv 不通；按 [setup-guide.md](../../setup-guide.md) 安装

如出现差异：

- 优先按 [setup-guide.md#6-故障排查](../../setup-guide.md#6-故障排查) 排查
- 仍不行报 issue（label `uat-env`）

## 6 个 > 2s 慢测试（参考）

| 测试 | 耗时 | 原因 |
|---|---|---|
| `ScaleNuSigmaF × openmc` (BDD) | 17.6 s | OpenMC MC 跑 source + followup (60 batches × 5000 particles 各 1 次) |
| `ScaleFuelSigmaA × openmc` (BDD) | 12.6 s | 同上 |
| `OpenMcRunnerSmokeTests` | 12.0 s | OpenMC MC 单次跑 |
| `ScaleNuSigmaF × openmoc` (BDD) | 3.3 s | OpenMOC deterministic |
| `Follow-up k_eff exceeds source (NuSigmaF)` | 2.6 s | OpenMOC source + followup |
| `Follow-up k_eff < source (SigmaA)` | 2.4 s | 同上 |

所有 6 个都是物理仿真跑动，已经在 < 2s 是不现实的；预算上限 120 s 也远未触及。
