# MR Verification v1.2 Two-Layer Review Policy

## Goal

将 v1.2 主线的 review 升级为两层，避免“测试绿了就直接 merge”的单闸门流程。

## Policy

从 `PR-3` 起，所有 MR Verification v1.2 相关 PR 都必须完成以下两层 review，缺一不可：

1. `Layer 1: local implementation review`
   - 由当前执行代理完成
   - 必须在聚焦测试、范围测试、全量 `MetBench_SystemMT.Tests` 通过之后进行
   - 必须检查：
     - 范围是否越界
     - 是否回退到 stringly typed / legacy dictionary predicate
     - 是否绕过 `Validate()` 装载 gate
     - Property 与 MR 路径是否混用
     - diagnostics / status / validator 行为是否与计划一致
   - 必须留下可回指的验证证据

2. `Layer 2: independent review record`
   - 必须形成独立于实现步骤的 review 留痕
   - 优先顺序：
     - GitHub PR review / PR comment 留痕
     - 仓库内 retrospective review 记录
   - 必须明确：
     - review 范围
     - 关注风险
     - 发现的问题或“未发现阻断问题”
     - merge decision

## Minimum Merge Gate

对 v1.2 相关 PR，只有同时满足以下条件才允许 merge：

- TDD 红绿循环已完成
- 聚焦测试已通过
- 相关切片测试已通过
- 全量 `MetBench_SystemMT.Tests` 已通过
- Layer 1 review 已完成
- Layer 2 review 已留痕
- GitHub required checks 为 green

## Retrospective Scope

本策略生效前已合并但未具备 Layer 2 review 留痕的 PR，需要补做 retrospective review。

当前需要补做的范围：

- `PR #97` `feat(v12): add PR-0 typed catalog foundation`
- `PR #98` `docs(v12): add PR-1 to PR-10 implementation plans`
- `PR #99` `feat(v12-pr1): add typed model and fail-closed validators`
- `PR #100` `feat(v12-pr2): add scalar verification runtime`
