# UAT Baseline — 2026-05-16

> 这是开发侧给测试员的 **参考基线** —— 测试员的 dry-run 结果应大体与这里一致；偏离过大说明环境异常。

| 项 | 值 |
|----|----|
| 仓库 commit | `97863eace894495e702a044ab967a428cca4286e` (main) |
| 平台 | Linux Ubuntu 24.04 cloud + OpenMOC venv (`/opt/openmoc-venv`) |
| .NET | 8.0.x |
| OpenMOC | installed, importable |
| LLM API | not exercised in baseline (UC-C4 走 fake gateway) |

## 整体结果

| 测试范围 | Pass | Skip | Fail | Cumulative wall |
|---------|------|------|------|----------------|
| BDD smoke (OpenMOC + CrossProgram + Heat + Projectile) | **22** | 0 | 0 | ~6 s |
| 全套 cross-platform suite | **458** | 2 | 0 | **22.35 s** |
| 性能预算 | n/a | n/a | n/a | < 120 s ✅ |
| > 2000ms 慢测试 | 0 个（OpenMOC scenario 这次都 < 2s） | | | |

## 文件清单

| 文件 | 用途 |
|------|------|
| `baseline-bdd.trx` | OpenMOC + 三 SUT BDD scenarios 单独跑 |
| `baseline-full.trx` | 全套 458 测试 trx |
| `bdd-console.log` | BDD 跑动时控制台输出 |
| `perf-baseline.log` | `tools/ci_perf_baseline.py` 输出 |

## 测试员如何对比

打开你自己 dry-run 的 trx，对比：

1. **Pass 数** ≥ 我们的 baseline（环境可能更全 → 跑得更多）
2. **Fail 数** = 0 （任一 ≥ 1 都是阻断）
3. **Cumulative 时间** ≤ 2× baseline（即 ≤ ~45 s）
4. OpenMOC 用例不应该全 [SKIP] — 全 SKIP 说明 venv 不通

如出现差异，先按 [setup-guide.md#6-故障排查](../../setup-guide.md#6-故障排查) 排查；仍不行报 issue（label `uat-env`）。
