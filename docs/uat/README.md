# MetBench v2.1 人工验收测试 (UAT) 包

> 📋 **下发给测试员**：先读 [`任务书.md`](任务书.md)（5 min）—— 从哪里开始 / 做什么 / 验收标准。本 README 是技术总览。
>
> 目标：让一名不熟悉 MetBench 源码的 **测试工程师 / 项目验收人员** 能照本手册
> 在 Windows 11 + Linux (Ubuntu 24.04) 双端独立完成全部核心功能的功能性验收，
> 并按照统一标准给出 **通过 / 不通过** 判定。

---

## 1. 验收范围

本次 UAT 覆盖 **v2.1 主线功能** + **论文支持型功能**，共 **5 大类 / 21 项核心用例**：

| 类别 | 涉及功能号 | 验收方式 |
|------|-----------|---------|
| A. **System-MT pipeline 主链路** | F1+pipeline+adapter | OpenMOC / Heat-Equation / Projectile 三 SUT BDD 跑通 |
| B. **数据持久化 & schema** | F18 / F19 / F7 (MetaPattern) | DB round-trip + 异常隔离 + 索引校验 |
| C. **MR 发现 & 验证** | F8 / F12 (Multi-LLM) / F14 (Pairing) | 真实 python sidecar + LLM consensus 输出 |
| D. **R-Case 自动复现** (论文核心) | F9 | 已知 bug `R-Case-4` 单次跑通端到端 |
| E. **运营层** | F10 (keyset) / F16 (CI perf) / F5-F6 (soft delete) | 性能基线 + 软删 + 趋势 |
| F. **WPF UI** (VM 仅) | client pages | Windows 11 + Parallels 手动 click-through |

---

## 2. 受众与环境

| 角色 | 环境 | 用本包的哪部分 |
|------|------|----------------|
| 后端 / API 验收员 | Ubuntu 24.04 cloud (或本地 Linux) | A + B + C + D + E |
| WPF UI 验收员 | Windows 11 + VS 2022 + Parallels | A 子集 + F |
| 集成验收审计员 | 任意 | acceptance-rubric.md 评分 |

**两端都需**：.NET 8 SDK · Python 3.12 · Git · 仓库 clone。
**仅 Linux 需**：OpenMOC venv（一次 `.claude/web-setup.sh` 自动装好）。
**仅 Windows 需**：WPF SDK（VS 2022 自带）+ OpenMOC（可选；不装则 OpenMOC 用例自动 skip）。

---

## 3. 文档结构

| 文件 | 用途 | 谁读 |
|------|------|------|
| [README.md](README.md) (本文件) | 总览 · 用例索引 | 所有人 |
| [setup-guide.md](setup-guide.md) | 一步步装环境 / SUT / LLM API 配置 | 验收员开工前必读 |
| [test-procedures.md](test-procedures.md) | 21 个用例的逐步执行步骤 + 预期输出 | 验收员主操作手册 |
| [acceptance-rubric.md](acceptance-rubric.md) | 评价表（通过 / 不通过判定） | 验收员 + 审计员 |
| [sample-data/](sample-data/) | 验收用预置 spec / 配置 / 输入 | 跟着 test-procedures 引用 |

---

## 4. 准入门槛（不通过则停验）

```bash
# Linux：以下三条 SUT 全绿才算可验收
dotnet build MetBench.sln          # 实际：Linux 跑 BLL.Core/DAL/Tests；WPF 在 Windows 跑
dotnet test  MetBench_SystemMT.Tests --filter "FullyQualifiedName~ColdStartIntegrationTests"

# 期望：Passed > 0 / Failed = 0
```

若准入失败，**停止 UAT**，回到 [setup-guide.md](setup-guide.md) 排查。

---

## 5. 时间预估

| 阶段 | 预估时长 |
|------|---------|
| 环境 setup（首次） | Linux 15-30 min（含 OpenMOC 编译） · Windows 10 min |
| 准入门槛跑通 | 5 min |
| A. 三 SUT BDD | 10 min |
| B. 持久化 & schema | 10 min |
| C. 发现 & 验证（含 LLM） | 15 min（含真实 API 等待） |
| D. R-Case 复现 | 10 min |
| E. 运营层 | 10 min |
| F. WPF click-through（Windows） | 30-45 min |
| 撰写 acceptance-rubric 评分 | 15 min |
| **合计** | **2-3 小时**（首次）/ **1-1.5 小时**（熟练后） |

---

## 6. 验收交付物

完成后请提交：

1. 填写完毕的 `acceptance-rubric.md` 副本（每行打 ✓ / ✗ 并附证据 / 截图链接）
2. 失败用例的 **重现日志 / 截图** 打包成 zip
3. 一份不超过 200 字的 **总评**：哪些通过、哪些不通过、阻断 vs 非阻断

---

## 7. 反馈渠道

- 验收期间发现严重 bug → GitHub Issue（label `uat-blocker`）
- 操作手册歧义 / 不清晰 → 在本目录提 PR
- 紧急阻断 → 联系开发负责人
