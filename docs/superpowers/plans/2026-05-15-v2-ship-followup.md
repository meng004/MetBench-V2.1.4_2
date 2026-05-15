# v2 Ship Follow-up Tasks — 2026-05-15

> 起点：PR #27 合并到 `main` 之后的延续任务。
> 来源：综合 §6.2 (status doc) + R3 (review feedback) + 4 项小缺口 (PR #27 review) +
> known issues §7b (vm-handoff doc) + MR 矩阵 25% gap (m_conv)。

---

## 优先级 P1：合并后立即处理（W9，1 周内）

### F1 · 真实 Validator sampler 替换 stub
**目标**: `EmpiricalValidator` / `AdversarialMutmutValidator` DI 注入真实采样器，不再 hardcoded demo。

**位置**: `MetBench_Client/App.xaml.cs` line ~175

**实施**:
- `EmpiricalValidator` sampler 从 LiteDB `Results` collection 拉历史 (source, followup) 对，按 `candidate.SuggestedAssertionTypeCode` 判定
- `AdversarialMutmutValidator` sampler 调真实 `MutationCampaignService` 跑 5×5 矩阵
- TDD：扩 `ValidatorTests` 用真实 fake repo 做端到端

**验收**:
- DiscoveryPage → ValidatePage → 不再固定 10/10 pass
- ValidationRun 表记录真实 baseline pass rate

### F2 · ReplayResultViewModel 接真实 ReplayService
**目标**: 替换 `_demoClassification` 硬编码 enum 切换为真实 `ReplayService.ReplayAsync(ctx, original)`。

**位置**: `MetBench_Client/ViewModels/ReplayResultViewModel.cs`

**实施**:
- `OnNavigatedTo` 时从 `ReplayInbox.PendingAnomaly` → 重建 `PipelineContext` + `original PipelineOutcome`
- await `ReplayService.ReplayAsync(...)` → 拿真实 `ReplayClassification`
- 把 6 demo classification 切换器改为只读显示

**验收**:
- 从 AnomalyListPage 选 anomaly → Replay → 真实跑出 6 个分类之一
- 端到端验证：originalAnomaly + replayPass → `FixedOrFlaky`

### F3 · R3 Serive → Service 重命名（独立 PR）
**目标**: 修拼写错误 `Serive` → `Service` 跨 BLL + Client。

**影响文件**:
- `MetBench_BLL/ApplicationSerive.cs` → `ApplicationService.cs`
- `MetBench_BLL/DomainSerive.cs` → `DomainService.cs`
- `MetBench_BLL/MetamorphicRelationSerive.cs` → `MetamorphicRelationService.cs`
- `MetBench_BLL/MTVisualizationSerive.cs` → `MTVisualizationService.cs`
- `MetBench_BLL/MRRecommendationSerive.cs` → `MRRecommendationService.cs`
- ~10 个 ViewModel + App.xaml.cs 引用点

**策略**: 
- 1 commit 改类名 + 文件名 + 引用
- 加 `[Obsolete] type alias` 保持 6 个月向后兼容
- 必须在 Windows 上 build 通过

**验收**: dotnet build MetBench.sln 通过 + 所有 ViewModel 命令仍工作

---

## 优先级 P2：合并后中期（W9-W10，2 周内）

### F4 · m_conv 矩阵补齐（3 个缺失 .feature）
**目标**: m_conv 覆盖率从 25%（1/4）→ 100%（4/4）。

**新增 .feature**:
1. `metbench/catalog/features/m_conv/MR10-conv-num-azim-refine.feature`
   - OpenMOC `num_azim` × 2/4/8 refine → expect k_eff 收敛到极限
2. `metbench/catalog/features/m_conv/MR11-conv-azim-spacing-refine.feature`
   - OpenMOC `azim_spacing` ÷ 2/4 refine → expect k_eff 单调收敛
3. `metbench/catalog/features/m_conv/MR13-conv-batches-refine.feature`
   - OpenMC `batches` × 5/10/20 refine → expect σ_MC 单调减半

**验收**:
- 跑 `python tools/validate_feature_sync.py` 通过
- 3 个新 .feature 都能被 Reqnroll 拾起
- MR 矩阵报告 m_conv 100%

### F5 · LiteDB 索引唯一性 / 软删除 / 迁移测试
**目标**: 补 P1.9 schema test 的空白（review G3）。

**测试场景**:
- Application.IdApplication 重复插入应 throw
- MRBindings 引用不存在的 MR 应 reject
- 软删除（Status='deleted'）后 GetByStatus 应过滤
- DbConfig 版本迁移（v1 schema → v2）round-trip

**位置**: `MetBench_SystemMT.Tests/V2Schema/V2IndexConstraintTests.cs` 扩

**验收**: 新增 10+ test，全过；覆盖率 round-trip → constraint-violation

### F6 · TrendAnalysisService 加 sut_id / mr_code 维度
**目标**: burst detection 现仅按 Category 分组，扩至 SUT 和 MR 维度。

**位置**: `MetBench_BLL.Core/Trend/TrendAnalysisService.cs:79-110`

**实施**:
- 加 `DetectBurstsByDimension(string dimension)` 接受 "category" / "sut" / "mr-code" / "metapattern"
- 用 Anomaly + Execution + MRInstance join 拿到对应字段
- 加 dimension 参数到 `ComputeWeekly`

**验收**: 新增 4 个 test 验证不同维度的 burst 检出；headline 包含具体维度信息

### F7 · DefaultProcessExecutor / MetaPatternDiscoverer 端到端集成 smoke
**目标**: 覆盖 review G1。

**实施**:
- `DefaultProcessExecutorSmokeTests` 已有 4 个基础 smoke，加 1 个真实跑 `python noether_candidates.py` 案例
- 新 test 文件 `MetaPatternDiscovererIntegrationTests.cs` 验证 stdout JSON 完整解析链路

**验收**: 不依赖 fake，真正 spawn python sidecar 拿 JSON 解析成 ≥3 proposal

### F8 · DiscoveryPage smoke 自动化（步骤 1-3）
**目标**: PR #27 known limitation —— SUT 上传 / MR 录入 / MT 执行需手动操作。

**实施**:
- 扩 `tools/smokeshot/Program.cs` 加 UIA 处理 OpenFileDialog（File picker）
- DataGrid 自动输入（CellPattern + ValuePattern）
- 完整 10 步无人值守

**验收**: 一行命令跑完 10 步，生成 10 张截图 + 1 个 markdown summary

---

## 优先级 P3：合并后中长期（W11+）

### F9 · R-Case 自动化复现 pipeline
**目标**: 把 R-Case-4 / R-Case-6（OpenMOC narrow basin）的 sweep + 检测自动化。

**实施**:
- `tools/auto_repro_rcase.py`：mr_parameter_sweep → MutationCampaign → 自动归类 anomaly → 写 KnownBugs 表
- WPF DashboardPage 加 "Reproduce R-Case-X" 按钮

**验收**: 一键复现 R-Case-4 + 自动 link Anomaly→KnownBug

### F10 · LiteDB keyset pagination（review R2）
**目标**: 关闭深翻页 O(n) 性能问题。

**位置**: `MetBench_DAL/V2/LiteDbGuidPkRepositoryBase.cs:77`

**实施**:
- 加 `GetPageAfter(Guid lastSeenId, int pageSize)` 用索引扫描
- VM 端 `PagingViewModel<T>` 提供两种模式选择（offset / keyset）
- 数据量超 10k 时 UI 自动切换 keyset

**验收**: 大表 100k 行 + 翻第 1000 页 < 100ms

### F11 · m_adj MR 族解锁（如接入 adjoint solver）
**目标**: NOETHER 8 MetaPattern 中 m_adj 现 out-of-scope —— 待 SUT 支持 adjoint flux 后开放。

**前置条件**: OpenMOC 或 OpenMC 暴露 adjoint k_eff API

**实施**:
- 新增 `MR16-adj-self-adjoint.feature`
- assertion: `k_adjoint == k_forward within ε`

### F12 · 多 LLM provider 矩阵互验
**目标**: TheoreticalLlmValidator 跑 DeepSeek + Anthropic + OpenAI，比较 plausibility 一致度。

**实施**:
- `MetBench_BLL/Discovery/AnthropicLlmGateway.cs`
- `MetBench_BLL/Discovery/OpenAiLlmGateway.cs`
- DI 多 ILlmGateway 注入，按 candidate Round-robin

**验收**: ValidationRuns 表能查到 3 个 provider 对同一 candidate 的判断 + Cohen's κ 计算

### F13 · 第 3 个 SUT 接入（如 Serpent / MCNP）
**目标**: 提升 m_cmp 普适性。

**实施**: 按 P3 模板写 input/output adapter + 注册到 Applications 表 + 新增 cross-program scenario

### F14 · CI 性能基线
**目标**: 防止测试套件慢慢膨胀回归。

**实施**: 
- GitHub Action 加 step：`dotnet test --logger trx`
- 总时长 > 30s 报警
- 单 test > 500ms 报警

---

## 任务依赖关系

```
F1 (validator real sampler)
F2 (replay real service)        都 ship-ready，立即可做
F3 (Serive→Service rename)      独立，无依赖

F4 (m_conv 3 .feature)          独立
F5 (LiteDB constraint test)     独立
F6 (Trend dimensions)           独立
F7 (MetaPattern smoke)          独立

F8 (smoke 步骤 1-3)              依赖 F1+F2（替换 stub 后再测端到端）
F9 (R-Case 自动复现)             依赖 F1+F2
F10 (keyset pagination)         独立
F11 (m_adj)                     依赖 SUT 升级 (out of metbench scope)
F12 (multi-LLM)                 独立
F13 (3rd SUT)                   独立
F14 (CI perf)                   独立
```

---

## 建议执行顺序

**W9 (cloud + VM 协作)**：
- 周一: F3 Serive→Service rename 单独 PR （VM 完成）
- 周二: F1 + F2 一起做（cloud 写真实 sampler，VM 接 ReplayService）
- 周三-四: F4 m_conv 三个 .feature（cloud）
- 周五: F5 LiteDB constraint test（cloud）

**W10**:
- F6 Trend dimensions（cloud）
- F7 + F8（cloud + VM）

**W11+** 按需求和资源安排 F9-F14。

---

## 验收 v2.1 完整 ship 条件

- [ ] F1-F8 全部完成
- [ ] MR 矩阵覆盖率 100%（16/15 + m_conv 3 个）
- [ ] PR #27 已合并到 main（本次操作）
- [ ] CI 全绿连续 1 周
- [ ] 端到端 smoke 10 步无人值守 < 5 分钟
- [ ] 论文复现包 + 录屏 demo 提交 advisor 审阅
