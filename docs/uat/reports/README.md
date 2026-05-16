# UAT 测试报告归档

每一次 UAT 验收的报告 + 证据都归档到 `docs/uat/reports/round-N-YYYYMMDD-tester/` 目录。

## 目录结构

```
docs/uat/reports/
├── README.md                              -- 本文件（索引 + 总览）
├── dashboard.md                           -- 历次轮次 PASS / FAIL 趋势
├── round-template/                        -- 单轮模板（测试员复制后填写）
│   ├── README.md                          -- 本轮元信息（commit / 平台 / 测试员）
│   ├── acceptance-rubric-filled.md        -- 填好的评价表副本
│   ├── evidence/                          -- 截图 / trx / 错误日志
│   └── findings.md                        -- 本轮发现的 issue 列表（链 GitHub Issues）
├── baseline-YYYYMMDD/                     -- 开发侧每次发版前的参考基线
│   ├── README.md
│   ├── baseline-bdd.trx
│   ├── baseline-full.trx
│   └── perf-baseline.log
└── round-1-20260520-zhangsan/             -- 第 1 轮，2026-05-20，测试员 zhangsan
    ├── README.md
    ├── acceptance-rubric-filled.md
    ├── evidence/
    └── findings.md
```

## 历史轮次

| 轮次 | 日期 | commit | 测试员 | 结果 | 报告 |
|------|------|--------|--------|------|------|
| baseline | 2026-05-16 | `97863ea` | dev-cloud (Claude) | reference | [`baseline-2026-05-16/`](baseline-2026-05-16/) |
| round-1 | TBD | TBD | TBD | TBD | TBD |

## 如何归档你的轮次（测试员视角）

### 1. 复制模板

```bash
cp -r docs/uat/reports/round-template docs/uat/reports/round-N-YYYYMMDD-<你的名字>
```

### 2. 填写

- `README.md`：本轮元信息（接手 commit、平台、LLM 是否真实跑、跑通了哪些类别）
- `acceptance-rubric-filled.md`：复制 `docs/uat/acceptance-rubric.md` 改名并填打分
- `evidence/`：截图、`*.trx`、错误日志 zip
- `findings.md`：每个 ❌ 一行，链向你新建的 GitHub Issue

### 3. 提交 PR

```bash
git checkout -b uat-round-N-<你的名字>
git add docs/uat/reports/round-N-YYYYMMDD-<你的名字>/
git commit -m "uat: round-N report by <你的名字>"
git push -u origin uat-round-N-<你的名字>
```

打开 PR，标题 `uat: round-N report — <PASS/CONDITIONAL/FAIL>`。

下发人审阅后 merge → 报告自动归档进仓库。

## 哪些去 GitHub Issues 哪些进报告

| 内容 | 去向 |
|------|------|
| 整体打分 / 类别通过率 / 总评 | 报告 PR 进 `reports/` 目录 |
| 单条阻断 bug | **GitHub Issue**，label `uat-blocker` / `uat-major` / `uat-minor` |
| 文档错误 / 描述不清 | **GitHub Issue**，label `uat-doc` |
| 新建议 / 新覆盖点 | **GitHub Issue**，label `uat-enhancement`，或直接发 PR 加新 UC |
| 环境装不通 | **GitHub Issue**，label `uat-env` |

`findings.md` 引用 issue 编号即可，不复制内容。
