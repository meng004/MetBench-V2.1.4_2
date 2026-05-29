---
topic: debt #5 — Anomaly 状态机 string → AnomalyStatus enum
status: cloud-complete（跨平台已实现+测试绿；WPF 待 VM 编译验证）
branch: followup/debts-2026-05-29
base: origin/main (84ae500)
date: 2026-05-29
---

# debt #5 — Anomaly 状态机 string → `AnomalyStatus` enum

## 目标 & 验收标准

把 `Anomaly.Status`（自由 string）改为强类型 `AnomalyStatus` enum，并把目前**只写在
doc 注释里、未被代码强制**的状态机（`new → investigating → {known | confirmed-bug |
false-positive | fixed-upstream}`）落实为 BLL 层的确定性校验（CLAUDE.md §1.3 确定性逻辑交给代码）。
LiteDB 持久化由 string 改为 int，并对既有 string 数据做一次性迁移。

**验收**：
- BLL.Core / Domain / IDAL / DAL / Tests 在 Linux `dotnet test` 全绿。
- enum：`Unspecified=0, New=1, Investigating=2, Known=3, ConfirmedBug=4, FalsePositive=5, FixedUpstream=6`；LiteDB 存 int。
- kebab：`new / investigating / known / confirmed-bug / false-positive / fixed-upstream`（`Unspecified` ↔ `unspecified`，仅防御）。
- 合法转移：`new → investigating`；`investigating → {known, confirmed-bug, false-positive, fixed-upstream}`；其余抛 `InvalidAnomalyStatusTransitionException`。
- 旧 string 数据读得回（lazy 反序列化兼容）+ 一次性迁移把 on-disk 值改成 int。
- WPF（`MetBench_Client`）由 VM 编译验证（云端无法编译）。

## enum 设计权衡（§5 冲突挑明）

选定路径：**全 enum 契约** —— `Anomaly.Status`、`AnomalyFilter.Status`、
`IAnomalyService.TransitionStatus`、`IAnomalyRepository.GetByStatus` 全部改成 `AnomalyStatus`。
被否决的替代：在 service 边界保留 string（只改实体字段 + 内部校验）—— 会在服务边界留下
stringly-typed 残留，违背 debt 的本意。代价：WPF 2 处调用点要在边界做 kebab→enum 转换，
交由 VM track 修（CLAUDE.md §9）。

转移非法时**抛异常**而非返回 false：`TransitionStatus` 现有 `bool` 返回保留为
"anomaly 不存在 = false"；非法转移属契约违反 → 抛 `InvalidAnomalyStatusTransitionException`
（§6 显式报错；WPF 既有 try/catch 会显示消息）。

## 改动清单

| 项目 | 文件 | 改动 |
|---|---|---|
| Domain | `V2/AnomalyStatus.cs`（新） | enum + kebab 双向 + 转移表 + `RegisterBsonMapping` |
| Domain | `V2/Anomaly.cs` | `Status` string → `AnomalyStatus`（默认 `New`） |
| BLL.Core | `Anomaly/InvalidAnomalyStatusTransitionException.cs`（新） | 非法转移异常 |
| BLL.Core | `Anomaly/AnomalyService.cs` | List 过滤 / Commonality 分组(→kebab key) / TransitionStatus 校验 / RecordAnomalyAsync 起手 New |
| BLL.Core | `Anomaly/AnomalyFilter.cs` | `Status` → `AnomalyStatus?` |
| BLL.Core | `Anomaly/IAnomalyService.cs` | `TransitionStatus(..., AnomalyStatus, ...)` |
| IDAL | `V2/IAnomalyRepository.cs` | `GetByStatus(AnomalyStatus)` |
| DAL | `V2/LiteDbAnomalyRepository.cs` | `GetByStatus(AnomalyStatus)` |
| DAL | `DbConfig.cs` | ctor 注册 int 序列化器 + 一次性 string→int 迁移（无条件、幂等） |
| tools | `SeedCrossProgramAnomalies/Program.cs` | `Status = AnomalyStatus.Investigating` + 调用 `RegisterBsonMapping`（该进程不经 DbConfig） |
| Tests | 见下 | 更新既有 4 文件 + 新增 enum/转移/持久化/迁移测试 |
| WPF（**VM 修**） | `MetBench_Client/ViewModels/AnomalyListViewModel.cs` | `AnomalyFilter(Status:)` + `TransitionStatus(...)` 2 处 kebab→enum |

CommonalityReport.ByStatus 维持 `Dictionary<string,int>`（kebab key），不波及报表渲染器。
审计 detailsJson 维持 kebab `{"from":"...","to":"..."}`，不波及既有日志消费方。

## CI 不可见、由 VM 兜底的点

- WPF `AnomalyListViewModel` 2 处调用点编译失败 = 设计内（VM 修）。
- on-disk int 迁移 + GetByStatus 真实 LiteDB 行为由新增 DAL 集成测试覆盖（Linux 可跑）。
