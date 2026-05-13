# MetBench v2 系统级 MT 设计文档

> 8 周开发的基线规格。任何与本目录文档相左的实现需走 RFC PR 修改本目录。

## 入口

**先读 [`v2-system-mt-architecture.md`](v2-system-mt-architecture.md)** — 整体架构总览（约 400 行），其余文档是子规格。

## 文档清单

| 文档 | 用途 | 谁应该读 |
|------|------|--------|
| [`v2-system-mt-architecture.md`](v2-system-mt-architecture.md) | 整体架构、模块清单、pipeline 数据流、实施路线 | 所有人 |
| [`glossary.md`](glossary.md) | 术语表（4 级 MR 语义 + 各子系统术语） | 写代码、写文档、写 commit 之前必读 |
| [`entity-model.md`](entity-model.md) | LiteDB schema 完整规格（23 collection） + ER 图 | 后端 / DB 开发者 |
| [`assertion-extensions.md`](assertion-extensions.md) | FluentAssertions 扩展方法 API + AssertionEvaluator | 断言相关代码开发者 |
| [`migration-plan.md`](migration-plan.md) | 8 周路线 + 迁移脚本草稿 + 归档策略 | 全流程负责人、各阶段 owner |

## 核心设计决策摘要

| 决策 | 选择 |
|------|------|
| MT 执行编排 | **C#** (`MetBench_BLL.Core`) |
| Adapter 实现语言 | **Python**（仅做文件 IO 解析） |
| MR 输入变换位置 | **C# Pipeline**（不在 Python adapter） |
| 持久化 | **LiteDB**（23 collection，3NF） |
| MR 描述层级 | **4 级**：MetaPattern / MRSchema / MRBinding / MRInstance |
| MR 数据库实体 | **扩展既有** `MetamorphicRelation` / `Application` + 新增 collection |
| BDD `.feature` 角色 | **MR 视图**，与 LiteDB 双向同步 |
| 断言系统 | **FluentAssertions 扩展方法**（`BeLessThanWithNoiseFloor` 等） |
| Discovery 子系统 | **`IMRDiscoverer` 接口** + MetaPattern + LLM-Native + Validator |
| Mutation 子系统 | **4 个新实体**承担测试评估 + 跨 SUT 差分 |
| 不做 | SQL Server / 微服务 / Python-as-core / MR DSL / Plugin DLL |

## 与既有 v1 / Stage 4 / Stage 5 的关系

- **v1 `MetBench_BLL/` 方法级 MT**：保持原样，**不动**
- **v1 `MR.litedb` 数据**：保持原样
- **Stage 4 系统级 MT 框架**：保留思路，扩展实现（C# 编排 + Python adapter）
- **Stage 5 Python 矩阵研究**：数据迁入 LiteDB；脚本降级为辅助工具

详见 [`migration-plan.md`](migration-plan.md) §1 迁移范围表。

## 修改本目录文档的流程

1. 任何架构 / schema / 术语 / 命名变更，先改本目录对应文档
2. 提 PR 标 `design-change` label
3. ≥ 1 人 review
4. 合并后再改实现代码
5. 实现 PR 在描述中引用对应设计 PR

**严禁**：实现代码先于设计文档变更；设计文档"事后补"。
