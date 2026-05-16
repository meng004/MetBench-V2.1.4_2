# UAT Round-N — YYYY-MM-DD — <你的名字>

> 复制本目录到 `round-N-YYYYMMDD-<你的名字>/` 后填写。

## 元信息

| 项 | 值 |
|----|----|
| 轮次 | round-N |
| 日期 | YYYY-MM-DD |
| 测试员 | <你的名字> |
| 接手 commit | `<git rev-parse HEAD 输出>` |
| 平台 — Linux | Ubuntu __.__ / .NET 8._._  / OpenMOC ✅ or ❌ |
| 平台 — Windows | Windows 11 __H__ / VS 2022 __ / OpenMOC ✅ or ❌ |
| LLM 实跑 | ✅ DeepSeek / ✅ Anthropic / ✅ OpenAI / ❌ 全 fake |
| 总工时 | __ h |
| 完成时间 | YYYY-MM-DD HH:MM |

## 结果摘要

| 类别 | 用例数 | ✅ | ⚠️ | ❌ | Pass% |
|------|--------|---|----|----|-------|
| A. 管理 CRUD | 8 | | | | |
| B. MR 主流程 | 9 | | | | |
| C. MR 发现 & 验证 | 9 | | | | |
| D. R-Case 复现 | 2 | | | | |
| E. 可视化 & 报表 | 7 | | | | |
| F. 持久化 & schema | 5 | | | | |
| G. 运营 & 性能 | 5 | | | | |
| **合计** | **45** | | | | |

## 总评

- [ ] **PASS**
- [ ] **CONDITIONAL PASS**（理由：________）
- [ ] **FAIL**（阻断 bug：________）

## 与 baseline 对比

参考 [`../baseline-YYYYMMDD/README.md`](../baseline-2026-05-16/README.md)。

- Pass 差异：__
- Fail 差异：__
- 性能差异：__

## 文件

- [`acceptance-rubric-filled.md`](acceptance-rubric-filled.md) — 逐行打分（45 行）
- [`evidence/`](evidence/) — 截图 + trx + 错误日志
- [`findings.md`](findings.md) — 本轮发现的 issue 列表（每行链 GitHub Issue 编号）
