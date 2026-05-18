# UAT 用例执行手册

> 每个用例编号 **UC-XX**，对应 [acceptance-rubric.md](acceptance-rubric.md) 同号评分行。
> 命令默认 `cd MetBench-V2.1.4_2/` 后执行；WPF 步骤默认在 Windows 11 + VS 2022。
>
> 每个用例采用统一三段式：**初始条件 / 操作步骤 / 断言**。验收员逐项核对断言即可。

## 验收用例索引

| 类别 | 范围 | 用例数 | 平台 |
|------|------|-------|------|
| A. **管理 CRUD**（应用 / 域 / MR / MetaPattern） | F、UI | 8 | Win |
| B. **MR 蜕变测试主流程**（选 → 生 → 跑 → 看 → 重跑） | F、UI + Linux | 9 | Win + Linux |
| C. **MR 发现 & 验证**（含 LLM / mutmut / 多家投票 / SCG / OpenMC） | F | 11 | Linux + Win |
| D. **R-Case 自动复现**（论文核心） | F | 2 | Linux |
| E. **可视化 & 报表**（趋势 / coverage / Word/Excel/PDF/HTML） | F、UI | 7 | Win |
| F. **持久化 & schema** | F | 5 | Linux |
| G. **运营 & 性能** | F | 5 | Linux |
| **合计** | | **47** | |

---

## §0 公共环境（所有用例的默认初始条件）

每个用例的"初始条件"段引用本节作为基线，**只列差异**（如某用例需要额外种子数据或前置用例的产物）。

| 项 | 期望状态 | 验证命令 |
|---|---|---|
| OS | Linux Ubuntu 24.04+ 或 Windows 11 | `lsb_release -a` / Windows 设置 |
| .NET SDK | 8.0.x | `dotnet --version` |
| Python (system) | 3.11+ | `python3 --version` |
| Python (OpenMOC venv) | 3.12 + openmoc importable | `/opt/openmoc-venv/bin/python -c "import openmoc"` |
| Python (OpenMC venv) | 3.12 + openmc importable + binary on PATH | `/opt/openmc-venv/bin/python -c "import openmc"` + `/opt/openmc-venv/bin/openmc --version` |
| 仓库 commit | `main` HEAD（验收时 record commit hash） | `git rev-parse HEAD` |
| `MR.Litedb` 数据库文件 | 首次启动后自动创建于 `MetBench_Client/bin/Debug/net8.0-windows7.0/` 或 `app dir` | LiteDB Studio 打开 |
| `SystemMT.Litedb` 数据库文件 | 同上（系统级 MT 结果独立 DB，自 v2.1 起） | 同上 |
| LLM `.env`（若做 C4） | 含 `DEEPSEEK_API_KEY`、`OPENAI_API_KEY`、`CLAUDE_API_KEY` 三组 base/key/model；权限 0600；**已 gitignore** | `ls -la .env` |
| 工具 | LiteDB Studio (Win)、Process Monitor、Office 365 (Word/Excel/PDF 验证) | — |

### 启动 WPF 应用（Win 用例）

```powershell
cd MetBench-V2.1.4_2\
dotnet run --project MetBench_Client
```

冷启动期望 < 5 s。每个用例的"操作步骤"默认从主窗口已打开开始；若需要重启会在该用例特别注明。

### 启动 Linux 测试集（F、G、部分 C 用例）

```bash
cd MetBench-V2.1.4_2/
METBENCH_OPENMOC_PYTHON=/opt/openmoc-venv/bin/python \
METBENCH_OPENMC_PYTHON=/opt/openmc-venv/bin/python \
dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj \
  --logger "trx;LogFileName=uat-results.trx"
```

trx 文件落在 `MetBench_SystemMT.Tests/TestResults/`。所有 F/G 类用例的"断言"段都引用同一份 trx 的不同 facts。

---

## 类别 A — 管理 CRUD

### UC-A1 Application 管理 — 新建

**初始条件**:
- §0 公共环境就绪
- 当前 `Applications` 集合无名为 `UAT-App-1` 的行

**操作步骤**:
1. 启动 WPF 应用（见 §0）
2. 左侧导航点 **Application Management** 页
3. 点 "**+ New Application**" 按钮，弹出对话框
4. 填：`Name=UAT-App-1` `Code=uat-app-1` `Description=UAT smoke`
5. 点 "Save"

**断言**:
1. ✅ 列表多一行 `UAT-App-1`，显示在最上方（按创建时间倒序）
2. ✅ 用 LiteDB Studio 打开 `MR.Litedb`，`Applications` 集合可查到该行；`Code=uat-app-1`
3. ✅ 操作 < 2 s 完成（点 Save 到列表刷新结束）

---

### UC-A2 Application 管理 — 编辑

**初始条件**:
- §0 公共环境就绪
- **UC-A1 已执行成功**，`UAT-App-1` 在列表中

**操作步骤**:
1. 在 Application Management 页双击 `UAT-App-1` 行
2. 在弹出对话框中改 `Description=UAT smoke v2`
3. 点 "Save"

**断言**:
1. ✅ 列表中 `UAT-App-1` 的 Description 列已更新为 `UAT smoke v2`
2. ✅ LiteDB 中同一 `IdApplication` 的 `Description` 字段已更新
3. ✅ 其它字段（Name / Code / 时间戳之外的字段）保持不变

---

### UC-A3 Application 管理 — 删除

**初始条件**:
- §0 公共环境就绪
- **UC-A2 已执行成功**

**操作步骤**:
1. 选中 `UAT-App-1` 行
2. 点 "Delete" 按钮
3. 弹出确认对话框中点 "Yes"

**断言**:
1. ✅ 列表中 `UAT-App-1` 行消失
2. ✅ LiteDB `Applications` 集合**不再含**该行（硬删模式）**或** 含该行但 `Status=deleted`（V2 软删模式 — 取决于 schema 配置）
3. ✅ 若软删：在 "Show deleted only" 过滤下能重现该行

---

### UC-A4 Domain 管理 — 新建 + 绑定

**初始条件**:
- §0 公共环境就绪
- 当前无 `Neutronics` Domain，且至少有一个 Application（可先重新跑 UC-A1）

**操作步骤**:
1. 进 **Domain Management** 页
2. 点 "**+ New Domain**"，填 `Name=Neutronics` `Code=neutronics`
3. 在 "Bound Applications" 多选框中勾上 `UAT-App-1`
4. Save

**断言**:
1. ✅ Domain 列表新增 `Neutronics` 行
2. ✅ LiteDB `ApplicationDomains` junction 表新增一行 `(UAT-App-1, Neutronics)`
3. ✅ 回到 Application Management 页，`UAT-App-1` 的 Domain 列含 `Neutronics`

---

### UC-A5 MR 管理 — 新建 method-level MR

**初始条件**:
- §0 公共环境就绪
- 至少有一个 Application（如 `UAT-App-1`）

**操作步骤**:
1. 进 **MR Management** 页
2. 点 "+ New"，弹出表单
3. 填：
   - `Name=UAT-Identity-MR`
   - `Type=invariance`
   - `Granularity=method`
   - `Constraint=output == input`
4. 点 Save

**断言**:
1. ✅ MR 列表多一行 `UAT-Identity-MR`，含 Type/Granularity/Constraint 三列
2. ✅ 点中该行后 "MR Display" 详情页显示完整字段
3. ✅ LiteDB `MetamorphicRelations` 集合可查到该行

---

### UC-A6 MR 管理 — 列表筛选 / 搜索

**初始条件**:
- §0 公共环境就绪
- MR 列表至少 ≥ 5 行（含 UC-A5 的 `UAT-Identity-MR`）

**操作步骤**:
1. 在 MR Management 页搜索框输入 `Identity`
2. 观察列表
3. 清空搜索框

**断言**:
1. ✅ 输入 `Identity` 后列表只剩匹配行（至少含 `UAT-Identity-MR`）
2. ✅ 响应时间 < 500 ms（用秒表 / Process Monitor）
3. ✅ 清空后列表恢复完整（≥ 5 行）

---

### UC-A7 MetaPattern 列表 — 显示 8 个 NOETHER

**初始条件**:
- §0 公共环境就绪
- 首次启动后 MetaPattern seed 已自动执行（无需手工种子）

**操作步骤**:
1. 进 **MetaPatterns** 页
2. 观察列表
3. 切换 "Show out-of-scope only" 过滤器

**断言**:
1. ✅ 默认状态：列表**恰好 8 行**：
   - `m_inv`、`m_mono`、`m_conv`、`m_cmp` 状态 `active`
   - `m_adj`、`m_rev`、`m_dyn`、`m_rel` 状态 `out-of-scope`
2. ✅ 任选一行点开能看 `HypothesisTemplate` / `DefaultAssertionTypeCode` 字段
3. ✅ "Show out-of-scope only" 过滤下只剩 4 行

---

### UC-A8 CRUD 端到端（无 UI 路径）

**初始条件**:
- §0 公共环境就绪（Linux 跑测试集）

**操作步骤**:
1. 在仓根跑：
   ```bash
   dotnet test MetBench_SystemMT.Tests \
     --filter "FullyQualifiedName~V1CompatibilityTests|FullyQualifiedName~V2EntityRoundtripTests|FullyQualifiedName~MetaPatternEntityTests|FullyQualifiedName~MRBindingStatusTests"
   ```

**断言**:
1. ✅ 测试结果 `Passed > 0, Failed = 0`
2. ✅ 至少 4 类实体覆盖：Application v1 形 / V2 schema round-trip / MetaPattern / MRBinding
3. ✅ trx 文件归档

---

## 类别 B — MR 蜕变测试主流程

### UC-B1 Discovery 页选 MR

**初始条件**:
- §0 公共环境就绪
- `SUT/amax.py` 存在（仓库自带）

**操作步骤**:
1. 进 **Discovery** 页（method-level MR discovery）
2. 选 SUT = `amax.py`（从下拉框）
3. 点 "Run Discovery"

**断言**:
1. ✅ 列表 ≥ 1 行候选 MR
2. ✅ 每行含 `Confidence` 列（0-1 之间数值）
3. ✅ 每行含 `Sample-Pass-Rate` 列

---

### UC-B2 System-MT 选 MR + input

**初始条件**:
- §0 公共环境就绪
- OpenMOC venv 可用（`/opt/openmoc-venv/bin/python -c "import openmoc"` 成功）
- `SUT/openmoc/sample/pincell.json` 存在

**操作步骤**:
1. 进 **MT Execution** 页
2. 在 "Available MRs" 选 `OpenMOC pin-cell — ScaleNuSigmaF`
3. 在 "Input sample" 文件选择器选 `SUT/openmoc/sample/pincell.json`

**断言**:
1. ✅ 上方 "Selected MR" 区域显示完整 MR 描述（transformation/assertion/expected k_eff direction）
2. ✅ 中间 "Source Input Preview" 显示 `pincell.json` 文本（至少前 30 行）

---

### UC-B3 Followup 自动生成

**初始条件**:
- **UC-B2 已执行成功**（MR + 输入都已选）

**操作步骤**:
1. 点 "**Generate Follow-up**" 按钮（不立即跑 SUT）
2. 观察右侧 "Follow-up Input" 区域

**断言**:
1. ✅ followup JSON 显示在右侧
2. ✅ 与 source 仅在 `materials.fuel.nu_sigma_f` 字段有 1.5× 缩放差异，其它字段相同
3. ✅ followup 文件落在系统 temp 目录如 `temp/openmoc_followup_*.json`，验收员可手工 `cat` 验证
4. ✅ 生成耗时 < 1 s

---

### UC-B4 测试执行 — 点 Run

**初始条件**:
- **UC-B3 已执行成功**
- 进度条空闲（Status=`idle`）

**操作步骤**:
1. 点 "**Run**" 按钮
2. 观察底部状态栏 + 进度条
3. 等待执行结束

**断言**:
1. ✅ 进度条按 `source → followup → assertion` 三阶段推进
2. ✅ 单次 OpenMOC 跑总时长 ~20-60 s（含 source + followup + assertion）
3. ✅ 结束后 Status 显示为 `ok`（绿底）或 `anomaly`（红底）

---

### UC-B5 结果展示 — Result 面板

**初始条件**:
- **UC-B4 刚完成**

**操作步骤**:
1. 查看 "Result" 面板

**断言** — 面板含以下全部字段：
1. ✅ `Source k_eff`（数值，> 0）
2. ✅ `Follow-up k_eff`（数值，> 0）
3. ✅ `Assertion Passed` 布尔值（true/false）
4. ✅ `Observed Δ`（数值，绝对差或相对差）
5. ✅ `Expected Threshold`
6. ✅ 若 `Assertion Passed=false`：`Failure Reason` 文本可见

---

### UC-B6 结果可视化 — chart

**初始条件**:
- **UC-B5 刚完成**

**操作步骤**:
1. 滚到 MT Execution 页底部图表区
2. 鼠标 hover 节点

**断言**:
1. ✅ 数值类 metric（如 k_eff）→ CartesianChart 显示 source vs followup 两条线
2. ✅ 类别类 metric → PieChart 显示对比
3. ✅ hover 节点显示 tooltip（含数值 + 时间戳）

---

### UC-B7 Anomaly List 浏览

**初始条件**:
- §0 公共环境就绪
- **至少有过 1 次 `anomaly` 状态的运行**（重跑 UC-B4 + factor=0.5 强制 anomaly）

**操作步骤**:
1. 进 **Anomaly List** 页
2. 观察列表
3. 点任一行展开

**断言**:
1. ✅ 列表按时间倒序排列，最近的 anomaly 在最上
2. ✅ 每行含 `Severity` `Category` `LinkedKnownBug` 三列
3. ✅ 点行后右侧详情显示 source/followup 原始输入

---

### UC-B8 异常点 commonality 分析

**初始条件**:
- §0 公共环境就绪
- Anomaly 列表 ≥ 2 行

**操作步骤**:
1. 在 Anomaly List 多选 ≥ 2 个 anomaly 行（Ctrl + click）
2. 点 "Analyze Commonality" 按钮

**断言**:
1. ✅ 弹出 / 右侧面板显示 commonality 报告
2. ✅ 报告含 `Shared MR` `Shared SUT` `Shared parameter range` 等共同维度
3. ✅ 若所选 anomaly 无共同点：显示 "No commonality"（不报错）

---

### UC-B9 Anomaly Replay 重跑

**初始条件**:
- §0 公共环境就绪
- Anomaly 列表 ≥ 1 行

**操作步骤**:
1. 在 Anomaly List 选一行
2. 点 "**Replay**" 按钮 → 跳转 **Replay Result** 页
3. 点 "**Run Real Replay**"

**断言**:
1. ✅ 上方区域显示原始 anomaly 的 source/follow-up values
2. ✅ 下方区域显示 replay 跑出的 source/follow-up values
3. ✅ 中间显示 `Reproduced=true/false` + 数值偏差百分比
4. ✅ 若 `Reproduced=true`：右侧 `KnownBug` 字段已自动 link 到 anomaly 记录

---

## 类别 C — MR 发现 & 验证

### UC-C1 真实 python sidecar 发现

**初始条件**:
- §0 Linux 测试集就绪
- `SUT/amax.py` 存在

**操作步骤**:
```bash
dotnet test MetBench_SystemMT.Tests \
  --filter "FullyQualifiedName~MetaPatternDiscovererIntegrationTests" \
  --logger "trx;LogFileName=uc-c1.trx"
```

**断言**:
1. ✅ `Passed ≥ 4, Failed = 0`
2. ✅ trx 文件归档

---

### UC-C2 Empirical + LLM Validator

**初始条件**:
- §0 Linux 测试集就绪

**操作步骤**:
```bash
dotnet test MetBench_SystemMT.Tests \
  --filter "FullyQualifiedName~ValidatorTests" \
  --logger "trx;LogFileName=uc-c2.trx"
```

**断言**:
1. ✅ `Passed ≥ 8, Failed = 0`
2. ✅ trx 包含至少一个 `EmpiricalValidator_*` 和一个 `TheoreticalLlm*` 命名的 fact

---

### UC-C3 MRPairing m_cmp partner

**初始条件**:
- §0 Linux 测试集就绪

**操作步骤**:
```bash
dotnet test MetBench_SystemMT.Tests \
  --filter "FullyQualifiedName~MRPairingServiceTests" \
  --logger "trx;LogFileName=uc-c3.trx"
```

**断言**:
1. ✅ `Passed ≥ 11, Failed = 0`

---

### UC-C4 Multi-LLM Consensus + Cohen's κ

**初始条件**:
- §0 Linux 测试集就绪
- `.env` 含 3 家 provider 的 base/key/model（**仅在跑真实端到端实验时需要**；单测用 fake gateway 不需要）

**操作步骤**:

单元测试路径（不消耗 LLM 配额）：
```bash
dotnet test MetBench_SystemMT.Tests \
  --filter "FullyQualifiedName~MultiLlmConsensusValidatorTests" \
  --logger "trx;LogFileName=uc-c4-unit.trx"
```

可选真实实验（消耗配额，约 4-5 分钟）：
```bash
METBENCH_LLM_EXPERIMENT=1 dotnet test MetBench_SystemMT.Tests \
  --filter "FullyQualifiedName~MultiLlmRealExperiment"
```

**断言**:
1. ✅ 单测路径 `Passed ≥ 15, Failed = 0`
2. ✅ 覆盖：strict majority / tie → null / 解析失败剔除 / 异常隔离 / κ unanimous=1
3. ✅ （可选）真实实验跑通后 `docs/experiments/2026-05-w11-llm-consensus/results.json` 含 60 次成功调用，consensus accuracy ≥ 95%, mean κ ≥ 0.8

---

### UC-C5 Validation Service E2E

**初始条件**:
- §0 Linux 测试集就绪

**操作步骤**:
```bash
dotnet test MetBench_SystemMT.Tests \
  --filter "FullyQualifiedName~ValidationServiceTests" \
  --logger "trx;LogFileName=uc-c5.trx"
```

**断言**:
1. ✅ `Passed > 0, Failed = 0`

---

### UC-C6 Candidate Review 页 — UI（Manual UI）

**初始条件**:
- §0 公共环境就绪
- 至少跑过一次 Discovery（UC-B1）产出 candidate

**操作步骤**:
1. 进 **Candidate Review** 页
2. 观察 candidate 列表
3. 选一行点 "Validate"
4. 验证后点 "Promote"

**断言**:
1. ✅ candidate 列表非空（至少 1 行）
2. ✅ 点 Validate 后右侧显示 `EmpiricalSample` 通过率
3. ✅ 点 Promote 后该 candidate 出现在正式 `MR Management` 列表
4. ✅ 截图归档 `MetBench-UAT-Screenshots/UC-C6-*.png`

---

### UC-C7 MR Recommendation 页（Manual UI）

**初始条件**:
- §0 公共环境就绪
- 至少有一个 Application + Domain + ≥ 3 个 MR

**操作步骤**:
1. 进 **MR Recommendation** 页
2. 选 Application = `UAT-App-1`、Domain = `Neutronics`
3. 点 "Recommend"

**断言**:
1. ✅ 列表显示 top-K（通常 K=10）推荐 MR
2. ✅ 按 `Confidence` 列降序排列
3. ✅ 截图归档

---

### UC-C8 AutoDetectMR 页（Manual UI）

**初始条件**:
- §0 公共环境就绪
- `SUT/amax.py` 存在

**操作步骤**:
1. 进 **Auto Detect MR** 页
2. 选 SUT = `amax.py`，identifier mode = "Random + Heuristic"
3. 设 sample size = 50
4. 点 "**Detect**"

**断言**:
1. ✅ 进度条推进 < 2 min 完成
2. ✅ 完成后列表显示候选 MR，每行含 `Confidence` `Type` `Hypothesis` 字段
3. ✅ 候选可以勾选 "Save to Candidate" 入 candidate 库
4. ✅ 截图归档

---

### UC-C9 Mutation Campaign 页（Manual UI）

**初始条件**:
- §0 公共环境就绪
- MR 列表含 `ScaleNuSigmaF`

**操作步骤**:
1. 进 **Mutation Campaign** 页
2. 选 MR = `ScaleNuSigmaF`、operators = `mutmut.all`
3. 点 "Start Campaign"
4. 等待结束（可能数分钟到数十分钟）

**断言**:
1. ✅ 矩阵显示 `MR × Mutant × Pass/Kill` 网格
2. ✅ "Kill Rate" 数值 ≥ 0（≥ 50% 说明 MR 有抗变异能力）
3. ✅ 失败 mutant 行可点开看 mutation diff
4. ✅ 截图归档

---

### UC-C10 SCG-Heuristic Discoverer

**初始条件**:
- §0 Linux 测试集就绪
- `SUT/openmoc/scg.json` 存在 + JSON 合法
- `SUT/openmc/scg.json` 存在（PR #57 添加）

**操作步骤**:
```bash
# 1. 验证 SCG JSON 合法
test -f SUT/openmoc/scg.json && python3 -c "import json; print(len(json.load(open('SUT/openmoc/scg.json'))['nodes']),'nodes')"
# 期望: 10 nodes

# 2. 跑测试集
dotnet test MetBench_SystemMT.Tests \
  --filter "FullyQualifiedName~ScgHeuristicDiscovererTests|FullyQualifiedName~JsonFileScgGraphBuilderTests|FullyQualifiedName~DiscoveryMethodSeedTests" \
  --logger "trx;LogFileName=uc-c10.trx"
```

**断言**:
1. ✅ JSON 校验输出 `10 nodes`
2. ✅ 测试 `Passed ≥ 29, Failed = 0`
3. ✅ trx 含 3 类 do-calculus 模式（direct-cause / mediator / confounder）的 candidate 产出
4. ✅ trx 含 `DiscoveryMethodSeed_*` fact 验证 SCG-Heuristic 入库

---

### UC-C11 OpenMC 第 3-SUT BDD smoke

**初始条件**:
- §0 公共环境就绪
- OpenMC binary + Python bindings 可用（`/opt/openmc-venv/bin/openmc --version` 输出版本号）
- `SUT/openmc/sample/pincell.json` 存在

**操作步骤**:
```bash
METBENCH_OPENMC_PYTHON=/opt/openmc-venv/bin/python \
dotnet test MetBench_SystemMT.Tests \
  --filter "FullyQualifiedName~OpenMcRunnerSmokeTests|FullyQualifiedName~CrossProgramNeutronTransport" \
  --logger "trx;LogFileName=uc-c11.trx"
```

**断言**:
1. ✅ `OpenMcRunnerSmokeTests` Passed = 1, Failed = 0
2. ✅ output JSON 含 `k_eff ∈ [0.5, 2.0]` + `metadata.runner = "openmc"`
3. ✅ Cross-program BDD 4 个 scenarios（ScaleNuSigmaF × {openmoc, openmc} + ScaleFuelSigmaA × {openmoc, openmc}）全 Pass
4. ✅ trx 归档

---

## 类别 D — R-Case 自动复现（论文核心）

### UC-D1 R-Case service 跑通

**初始条件**:
- §0 Linux 测试集就绪
- 至少 1 条 anomaly 入 LiteDB（可由 UC-B7 前置或 fake 注入）

**操作步骤**:
```bash
dotnet test MetBench_SystemMT.Tests \
  --filter "FullyQualifiedName~RCaseReproductionServiceTests" \
  --logger "trx;LogFileName=uc-d1.trx"
```

参考 spec: [sample-data/uat-rcase-spec.json](sample-data/uat-rcase-spec.json)

**断言**:
1. ✅ `Passed ≥ 9, Failed = 0`
2. ✅ 覆盖 R-Case 复现 / classification / 失败处理路径

---

### UC-D2 R-Case audit log 落库

**初始条件**:
- UC-D1 执行后

**操作步骤**:
```bash
grep "WriteAudit_records_r_case_reproduced" MetBench_SystemMT.Tests/TestResults/uc-d1.trx
```

**断言**:
1. ✅ trx 含 fact `WriteAudit_records_r_case_reproduced` 且 outcome = Passed
2. ✅ （可选）打开 `MR.Litedb` 看 `AuditLog` 集合含 `r-case.reproduced` 类型的行

---

## 类别 E — 可视化 & 报表

### UC-E1 Trend Dashboard — 时间序列（Manual UI）

**初始条件**:
- §0 公共环境就绪
- LiteDB 含 ≥ 1 周的历史 anomaly / pipeline 数据

**操作步骤**:
1. 进 **Trend Dashboard** 页
2. 选 metric = `Anomaly Count`，时间窗口 = "最近 4 周"
3. 点 "Refresh"
4. 鼠标 hover 任一节点

**断言**:
1. ✅ CartesianChart 显示 4 周折线
2. ✅ hover 显示每点 `(date, count)` tooltip
3. ✅ 若有 WoW 变化 ≥ 20% 或 burst 期：节点高亮标注
4. ✅ 刷新 < 3 s 完成
5. ✅ 截图归档

---

### UC-E2 Coverage Dashboard — 4 维饼图（Manual UI）

**初始条件**:
- §0 公共环境就绪
- LiteDB 含至少 ≥ 5 条 pipeline 执行记录

**操作步骤**:
1. 进 **Coverage Dashboard** 页
2. 点 "Refresh"

**断言**:
1. ✅ 显示 4 个 PieChart：`By Application` / `By MR` / `By Domain` / `By MetaPattern`
2. ✅ 每图至少 2 个扇区（避免单一类别一统天下）
3. ✅ legend 显示各扇区百分比
4. ✅ 刷新 < 3 s
5. ✅ 截图归档

---

### UC-E3 MT Report Generator — 4 端导出

**初始条件**:
- §0 公共环境就绪
- LiteDB 含至少 ≥ 3 条 pipeline 记录 + ≥ 1 条 anomaly

**操作步骤**:
1. 进 **MT Report Generator** 页
2. 选 scope = `By MR`，时间窗口 = `All`
3. 点 "Generate All"
4. 等待结束

**断言**:
1. ✅ 在 `Documents/MetBench_MTReport/` 生成 4 个文件：
   - `MTTestReport_Word.docx`
   - `MTTestReport_Excel.xlsx`
   - `MTTestReport_Pdf.pdf`
   - `MTTestReport_Html.html`
2. ✅ 每个文件可用对应工具打开
3. ✅ 内容包含：报告头 / 测试摘要 / MR 列表 / 结果统计 / 异常列表
4. ✅ 4 个文件总生成时间 < 30 s

---

### UC-E4 HTML 报告 WebView2 嵌入（Manual UI）

**初始条件**:
- UC-E3 已执行（4 个报告已生成）

**操作步骤**:
1. 在 MT Report Generator 页内点 "View HTML in App"

**断言**:
1. ✅ WebView2 在页内渲染 HTML 报告
2. ✅ CSS 正确（无错位 / 颜色丢失）
3. ✅ 表格正确（无截断 / 列乱）
4. ✅ 截图归档

---

### UC-E5 Dashboard 主页 cards（Manual UI）

**初始条件**:
- §0 公共环境就绪
- LiteDB 已有运营数据

**操作步骤**:
1. 进 **Dashboard**（主页）
2. 观察顶部 cards

**断言**:
1. ✅ 4-6 个 card 显示
2. ✅ 包含至少：`Total MRs` / `Total Executions Today` / `Anomalies This Week` / `Validation Pass Rate`
3. ✅ 每个 card 数值有意义（非 0 或 N/A，除非确实是空 DB）
4. ✅ 截图归档

---

### UC-E6 SystemMtReport service CLI

**初始条件**:
- §0 Linux 测试集就绪

**操作步骤**:
```bash
dotnet test MetBench_SystemMT.Tests \
  --filter "FullyQualifiedName~SystemMtReportServiceTests" \
  --logger "trx;LogFileName=uc-e6.trx"
```

**断言**:
1. ✅ `Passed ≥ 6, Failed = 0`

---

### UC-E7 HtmlSystemMtResultReportRenderer 单测

**初始条件**:
- §0 Linux 测试集就绪

**操作步骤**:
```bash
dotnet test MetBench_SystemMT.Tests \
  --filter "FullyQualifiedName~HtmlSystemMtResultReport" \
  --logger "trx;LogFileName=uc-e7.trx"
```

**断言**:
1. ✅ `Passed > 0, Failed = 0`
2. ✅ 覆盖 HTML 渲染 / 转义 / 表格生成

---

## 类别 F — 持久化 & schema

### UC-F1 DbConfig 三级 override

**初始条件**:
- §0 Linux 测试集就绪

**操作步骤**:
```bash
dotnet test MetBench_SystemMT.Tests \
  --filter "FullyQualifiedName~DbConfigTests" \
  --logger "trx;LogFileName=uc-f1.trx"
```

**断言**:
1. ✅ `Passed ≥ 5, Failed = 0`
2. ✅ 覆盖：默认路径 / env var override / CLI flag override / 优先级 CLI > env > config

---

### UC-F2 MetaPattern Seed 8 个

**初始条件**:
- §0 Linux 测试集就绪

**操作步骤**:
```bash
dotnet test MetBench_SystemMT.Tests \
  --filter "FullyQualifiedName~MetaPatternEntityTests" \
  --logger "trx;LogFileName=uc-f2.trx"
```

**断言**:
1. ✅ `Passed ≥ 11, Failed = 0`
2. ✅ 覆盖：实体 round-trip / Seed 8 NOETHER 行 / Status 字段 / HypothesisTemplate

---

### UC-F3 MRBinding.Status 软删 + 索引

**初始条件**:
- §0 Linux 测试集就绪

**操作步骤**:
```bash
dotnet test MetBench_SystemMT.Tests \
  --filter "FullyQualifiedName~MRBindingStatusTests" \
  --logger "trx;LogFileName=uc-f3.trx"
```

**断言**:
1. ✅ `Passed ≥ 7, Failed = 0`
2. ✅ 覆盖：`Status ∈ {active, deprecated, deleted}` 三态转移 / 软删过滤 / 索引存在

---

### UC-F4 V2 schema migration

**初始条件**:
- §0 Linux 测试集就绪

**操作步骤**:
```bash
dotnet test MetBench_SystemMT.Tests \
  --filter "FullyQualifiedName~V2SoftDeleteAndMigrationTests" \
  --logger "trx;LogFileName=uc-f4.trx"
```

**断言**:
1. ✅ `Passed ≥ 9, Failed = 0`
2. ✅ 覆盖 V1→V2 schema 升级 / 软删字段加入 / 索引重建

---

### UC-F5 V2 DI 完整性

**初始条件**:
- §0 Linux 测试集就绪

**操作步骤**:
```bash
dotnet test MetBench_SystemMT.Tests \
  --filter "FullyQualifiedName~V2RepositoryDIBindingTests" \
  --logger "trx;LogFileName=uc-f5.trx"
```

**断言**:
1. ✅ 所有 V2 `IXxxRepository` 接口都能从 `AddSystemMtRepositories()` 容器解析
2. ✅ `Passed ≥ 5, Failed = 0`

---

## 类别 G — 运营 & 性能

### UC-G1 LiteDB Keyset 分页

**初始条件**:
- §0 Linux 测试集就绪

**操作步骤**:
```bash
dotnet test MetBench_SystemMT.Tests \
  --filter "FullyQualifiedName~KeysetPaginationTests" \
  --logger "trx;LogFileName=uc-g1.trx"
```

**断言**:
1. ✅ `Passed ≥ 10, Failed = 0`
2. ✅ 覆盖：page 边界 / tie-breaker / 空集

---

### UC-G2 CI 性能基线

**初始条件**:
- §0 Linux 测试集就绪
- `tools/ci_perf_baseline.py` 存在 + 可执行

**操作步骤**:
```bash
dotnet test MetBench_SystemMT.Tests \
  --logger "trx;LogFileName=uat-results.trx"
python3 tools/ci_perf_baseline.py --trx MetBench_SystemMT.Tests/TestResults/uat-results.trx
```

**断言**（参考 dry-run 实测）：

```
===== CI perf baseline =====
  Trx file:           ...uat-results.trx
  Total cumulative:   ~40-60s   (budget: 120s)
  Slow tests (>2000ms): 3-10    # OpenMOC + OpenMC 集成预期慢
✓ PASS: under 120s budget.
```

1. ✅ `ci_perf_baseline.py` exit 0
2. ✅ cumulative < 120 s
3. ✅ 慢测试个数 ≤ 20（含 OpenMOC + OpenMC 实跑）

---

### UC-G3 多维 burst 检测

**初始条件**:
- §0 Linux 测试集就绪

**操作步骤**:
```bash
dotnet test MetBench_SystemMT.Tests \
  --filter "FullyQualifiedName~MultiDimBurstDetectionTests" \
  --logger "trx;LogFileName=uc-g3.trx"
```

**断言**:
1. ✅ `Passed ≥ 4, Failed = 0`

---

### UC-G4 Coverage service 单测

**初始条件**:
- §0 Linux 测试集就绪

**操作步骤**:
```bash
dotnet test MetBench_SystemMT.Tests \
  --filter "FullyQualifiedName~CoverageServiceTests" \
  --logger "trx;LogFileName=uc-g4.trx"
```

**断言**:
1. ✅ `Passed ≥ 5, Failed = 0`
2. ✅ 覆盖 4 维 coverage 计算 / 边界

---

### UC-G5 Anomaly service + commonality

**初始条件**:
- §0 Linux 测试集就绪

**操作步骤**:
```bash
dotnet test MetBench_SystemMT.Tests \
  --filter "FullyQualifiedName~AnomalyServiceTests" \
  --logger "trx;LogFileName=uc-g5.trx"
```

**断言**:
1. ✅ `Passed ≥ 8, Failed = 0`
2. ✅ 覆盖：List / Filter / Commonality / TransitionStatus / LinkToKnownBug

---

## 通用：测试执行后的产物归档

每场验收（dry-run 或正式）后，验收员把所有产物打包归档：

```bash
# Linux 端
tar czf uat-evidence-$(date +%Y%m%d).tgz \
  MetBench_SystemMT.Tests/TestResults/*.trx \
  docs/uat/acceptance-rubric.md
```

Windows 端额外打包：

```
C:\Users\<you>\Documents\MetBench_MTReport\         # 4 端报告
%TEMP%\MetBench-UAT-Screenshots\                    # 验收员截图
```

把 tarball + Windows 包合并存到 `docs/uat/reports/<验收日期>/`。

---

## 用例与代码 / PR 对应矩阵

| 用例 | 来源 PR / 代码位置 |
|------|--------------------|
| A1-A8 | 现有 `ApplicationRepository` / `MRManagementViewModel` / MetaPattern PR #34 |
| B1-B9 | 现有 BLL + MTExecutionPage + 新 ReplayResultViewModel |
| C1-C4 | PR #34 (F7) · PR #38 (F14 Pairing) · PR #45 (F12 LLM) |
| C5 | 现有 ValidationService |
| C6-C9 | WPF UI 页 |
| C10 | PR #52 / #53 (W11.1 SCG-Heuristic) |
| C11 | PR #57 (W12 F13 OpenMC 接入) |
| D1-D2 | PR #43 (F9 R-Case 复现) |
| E1-E7 | 现有 Trend/Coverage/MTReportGenerator + PR #34 (多维 burst) |
| F1-F5 | PR #37 (F18 DbConfig) · PR #34 (MetaPattern) · PR #35 (F19 Status) |
| G1-G5 | PR #46 (F10 keyset) · PR #38 (F16 CI perf) |
