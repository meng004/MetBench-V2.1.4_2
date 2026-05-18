# Plan — 反应堆物理 5 大方程 SUT 覆盖

> **Stage 8 / Goal 2 — Writing-plan 阶段**
> **日期**: 2026-05-18
> **状态**: 正式实施计划，approved-to-execute pending Goal 1 land + user OK
> **关联**: [brainstorming](2026-05-18-reactor-physics-five-equations-brainstorming.md) · [Goal 1 plan](2026-05-18-meta-prompt-mr-discovery-plan.md) · [AGENTS.md Stage 8 Goal 2](../../../AGENTS.md#goal-2-反应堆物理-5-大方程-sut-覆盖)
> **总工时**: ~28h（5 phase 平均 5.5h，含 paper writeup）

---

## 1. 目标 & 验收标准

**目标**：MetBench 覆盖反应堆物理 5 大方程的可执行 SUT。每方程至少 2-3 个 MR 跑通 + 入 BDD + 入 baseline。

**验收标准**（Stage 8 ship 条件）：

| # | criterion | 验法 |
|---|---|---|
| AC1 | 5 方程各有 ≥ 1 个可执行 SUT in `SUT/<sut>/` | `ls SUT/` |
| AC2 | 5 方程各有 ≥ 2 个 MR（覆盖 ≥ 2 NOETHER MetaPattern） | Launcher MR 注册 ≥ 5 × 2 = 10 new |
| AC3 | 5 方程各有 ≥ 1 个 `.feature` BDD scenario | `MetBench_SystemMT.Tests/Features/*.feature` 新增 5 |
| AC4 | Goal 1 meta-prompt 引擎对每个新 SUT 跑过 → 自动产 candidate ≥ 3 | `docs/experiments/2026-XX-yy-stage8/<sut>-candidates.json` |
| AC5 | baseline-2026-XX-YY 全套 ≥ 546 + 20 new = **566 facts** pass | `dotnet test` |
| AC6 | UAT acceptance-rubric Part C 加 4 新 UC（UC-C12 是 Goal 1，UC-C13~C16 是 Goal 2 各方程） | grep `^### UC-C1[3-6]` test-procedures.md |
| AC7 | `docs/experiments/2026-XX-yy-five-equations/` 含 paper writeup 5 方程 coverage 实证 | `ls` |

---

## 2. 阶段总览

```
Phase 8.2.0 (prep)        Goal 1 meta-prompt 引擎 land (~14h, 已在 Goal 1 plan)
              ↓
Phase 8.2.1 (#5 point-kinetics)   home-grown RK4 + 2 MR + BDD          ~3h
              ↓
Phase 8.2.2 (#2 burnup)           home-grown Bateman 3 核素 + 3 MR     ~5h
              ↓
Phase 8.2.3 (#4 thermal-hydraulics) home-grown 1D sub-channel + 2 MR   ~5h
              ↓
Phase 8.2.4 (#3 heat upgrade)     fuel_pin_conduction 1D 径向 + 2 MR   ~4h
              ↓
Phase 8.2.5 (real-program follow-up, optional) OpenMC depletion 对比   ~5h
              ↓
Phase 8.2.6 (paper writeup + UAT)  5 方程 coverage 实证 + UC-C13~C16   ~6h
```

总工时：~28h（含 Goal 2 全部 5 phase + writeup；不含 Goal 1 14h prep）。

---

## 3. Phase 8.2.1 — Point-kinetics（首发，#5，~3h）

**为什么先做**：纯 Python ODE，最简单；IAEA benchmark 数据公开，标准化。

### 3.1 物理

```
dn/dt = (ρ - β)/Λ · n + Σ_i λ_i C_i + S
dC_i/dt = β_i / Λ · n - λ_i C_i      (i = 1..6 delayed neutron groups)
```

参数：
- ρ (reactivity, dimensionless)
- β (total delayed neutron fraction, ~0.0065 for U-235)
- Λ (prompt neutron generation time, ~1e-5 s)
- λ_i (decay constants of 6 delayed groups, s^-1)
- β_i (fractional yields of 6 groups, sum = β)

### 3.2 SUT 结构

```
SUT/point_kinetics/
├── point_kinetics.py            # CLI runner: --input <json> --output <json>
├── point_kinetics_input_adapter.py        # generic transformer
├── point_kinetics_output_adapter.py       # extract n(t_final), peak n, integral
├── sample/
│   ├── prompt_critical.json     # ρ = β step
│   └── ramp_insertion.json      # ρ(t) linear ramp
├── scg.json                      # 因果图
└── equation.md                   # 含 LaTeX governing eq + 守恒
```

`point_kinetics.py` 简化版：python stdlib + numpy + scipy.integrate.solve_ivp（RK4 或 Radau implicit for stiff）。~80 行。

### 3.3 2 MR 设计（用 Goal 1 引擎产 candidate + 人工 review 选）

1. **m_inv: time scaling invariance** — `ρ(αt) → n(t/α)` 时间缩放对应 trajectory 缩放
2. **m_mono: reactivity monotonicity** — `ρ ↑ → peak n ↑`

### 3.4 测试

- `PointKineticsRunnerSmokeTests.cs` — 1 fact，跑 sample, 验 n(t_final) 在合理范围
- `PointKineticsInputAdapterTests.cs` — 2 fact, MR transformation 正确
- `PointKineticsOutputAdapterTests.cs` — 2 fact, 输出 metrics 提取
- `PointKineticsRampReactivity.feature` — 1 BDD scenario
- Launcher 注册 2 MR id (`point-kinetics-ramp-monotonicity` + `point-kinetics-time-scaling`)

### 3.5 工时分解

- 物理代码（point_kinetics.py 80 行 + adapters）：1.5h
- SUT 配套文件（sample / scg / equation.md）：0.5h
- 测试（5 fact + 1 BDD）：1h

---

## 4. Phase 8.2.2 — Burnup（#2，~5h）

### 4.1 物理

```
dN_i/dt = λ_{i-1} N_{i-1} - (λ_i + σ_a,i φ) N_i + Σ_j σ_{j→i} φ N_j  (Bateman)
```

简化版 3 核素链：U-235 → U-236 → Np-237 + Xe-135（裂变毒物）。

### 4.2 SUT 结构

```
SUT/burnup_bateman/
├── burnup_bateman.py            # scipy.integrate.odeint with sparse Jacobian
├── burnup_bateman_input_adapter.py
├── burnup_bateman_output_adapter.py     # 输出 N_i(t_final) + 总反应性
├── sample/
│   ├── pwr_cycle.json           # 典型 PWR 燃料 18 月循环
│   └── high_burnup.json
├── scg.json
└── equation.md
```

### 4.3 3 MR 设计

1. **m_inv: nuclide conservation** — 总核子数守恒（Σ N_i 不变，除 fission 损失）
2. **m_mono: flux monotonicity** — φ ↑ → N_{burnable} 衰减更快
3. **m_conv: time step refinement** — Δt → 0 时 N_i 收敛到 reference

### 4.4 测试

- `BurnupBatemanRunnerSmokeTests.cs` — 1 fact
- `BurnupBatemanInputAdapterTests.cs` — 3 fact
- `BurnupBatemanOutputAdapterTests.cs` — 2 fact
- `BurnupBatemanConservation.feature` — 1 BDD
- Launcher 注册 3 MR

### 4.5 工时

- 物理代码（含 Jacobian 简化）：2.5h
- SUT 配套：0.5h
- 测试（6 fact + 1 BDD）：1.5h
- equation.md（含 Bateman LaTeX 详细）：0.5h

---

## 5. Phase 8.2.3 — Thermal-hydraulics（#4，~5h）

### 5.1 物理（简化 1D sub-channel，单相）

```
∂ρ/∂t + ∂(ρ u)/∂x = 0
ρ ∂u/∂t = -∂p/∂x + 摩擦 + 热膨胀
ρ c_p ∂T/∂t = -ρ c_p u ∂T/∂x + q'''(x)    (energy)
```

简化：steady-state, single-phase, prescribed heat flux profile q'''(x)。input：channel geometry + inlet conditions + heat profile；output：outlet T, pressure drop, peak clad temp。

### 5.2 SUT 结构

```
SUT/sub_channel/
├── sub_channel.py               # 1D segregated solver, Newton iterations
├── sub_channel_input_adapter.py
├── sub_channel_output_adapter.py
├── sample/
│   ├── single_phase.json
│   └── boiling_onset.json
├── scg.json
└── equation.md
```

### 5.3 2 MR 设计

1. **m_inv: mass conservation** — ρ_in u_in A_in = ρ_out u_out A_out（注：A 常量则 ρu 守恒）
2. **m_mono: power monotonicity** — q''' ↑ → T_out ↑

### 5.4 测试

- `SubChannelRunnerSmokeTests.cs` — 1 fact
- `SubChannelInputAdapterTests.cs` — 2 fact
- `SubChannelOutputAdapterTests.cs` — 2 fact
- `SubChannelEnergyBalance.feature` — 1 BDD
- Launcher 注册 2 MR

### 5.5 工时

- 物理代码：3h（含 Newton 迭代收敛）
- SUT 配套：0.5h
- 测试（5 fact + 1 BDD）：1h
- equation.md：0.5h

---

## 6. Phase 8.2.4 — Heat conduction upgrade（#3，~4h）

### 6.1 物理（升级到 2D 径向燃料棒）

```
ρ c_p ∂T/∂t = (1/r) ∂/∂r (r k(T) ∂T/∂r) + q'''(r)
boundaries: T(R_fuel) given (clad-coolant interface); ∂T/∂r|_{r=0} = 0 (symmetry)
```

### 6.2 SUT 结构

```
SUT/fuel_pin_conduction/
├── fuel_pin_conduction.py       # 1D radial finite-difference + temp-dependent k(T)
├── fuel_pin_conduction_input_adapter.py
├── fuel_pin_conduction_output_adapter.py
├── sample/
│   └── uo2_pin.json
├── scg.json
└── equation.md
```

**保留旧 `SUT/heat_equation/`** —— 旧 1D Cartesian demo 不动；新 fuel_pin 是工程级。

### 6.3 2 MR 设计

1. **m_inv: rotational symmetry** — angular invariance of 1D radial model
2. **m_mono: power → centerline T** — q''' ↑ → T_centerline ↑

### 6.4 测试

- `FuelPinConductionRunnerSmokeTests.cs` — 1 fact
- 2 adapter tests
- `FuelPinConductionCenterlineMonotonicity.feature` — 1 BDD
- Launcher 注册 2 MR

### 6.5 工时

- 物理代码（径向 FDM + k(T) 非线性迭代）：2.5h
- SUT 配套：0.5h
- 测试 + BDD：1h

---

## 7. Phase 8.2.5 — Real-program follow-up（可选，~5h）

只在 reviewer 反馈"需要工业级证据"时启动。

候选：**OpenMC depletion** —— OpenMC 已装，depletion 是 built-in module，需 ENDF/B-VIII（5 GB）。

| Deliverable | 内容 |
|---|---|
| `.claude/web-setup.sh` 加可选 ENDF 下载段（标 `SKIP_ENDF=1` 默认） | 1h |
| `SUT/openmc_depletion/` 接入 OpenMC depletion as alternate burnup SUT | 2h |
| Cross-program BDD: `BurnupBatemanVsOpenmc.feature`（home-grown vs OpenMC 一致性） | 1h |
| 论文段落 "controlled comparison: home-grown vs industrial code" | 1h |

---

## 8. Phase 8.2.6 — paper writeup + UAT（~6h）

### 8.1 论文段落 `docs/experiments/2026-XX-yy-five-equations/README.md`

- 5 方程 × MR 数 × NOETHER MetaPattern coverage 矩阵
- 每方程实证数据：MR pass/fail 在 baseline 上
- meta-prompt 引擎对每方程的 candidate 召回率
- "framework universality" 论述（5 方程覆盖反应堆物理工程实践完整栈）
- limitations 段：home-grown 简化 / 缺工业 reviewer / `m_adj` 仍 out-of-scope

工时：3h

### 8.2 UAT BDD wrapper

每方程加 1 个 UC-C13~C16 BDD wrapper（4 个）：

- UC-C13: Point-kinetics SUT smoke + 2 MR via Launcher
- UC-C14: Burnup Bateman SUT smoke + 3 MR
- UC-C15: Sub-channel SUT smoke + 2 MR
- UC-C16: Fuel pin conduction SUT smoke + 2 MR

`acceptance-rubric.md` Part C 行数 11 → 15。
`test-procedures.md` 三段式 4 个新 UC。

工时：2h

### 8.3 baseline 刷新

跑 baseline-2026-XX-YY → 全套 ≥ 566 facts pass / 0 skip / 0 fail。
更新 dashboard.md baseline-3 行。

工时：1h（含 trx 写入 / perf-baseline.txt / README）

---

## 9. 工时汇总

| Phase | 方程 | SUT | 工时 |
|---|---|---|---|
| 8.2.1 | #5 point-kinetics | home-grown | 3h |
| 8.2.2 | #2 burnup | home-grown Bateman | 5h |
| 8.2.3 | #4 thermal-hydraulics | home-grown 1D sub-channel | 5h |
| 8.2.4 | #3 heat upgrade | fuel_pin_conduction | 4h |
| 8.2.5 | follow-up OpenMC depletion | (可选) | 5h |
| 8.2.6 | paper writeup + UAT | — | 6h |
| **合计（不含可选 8.2.5）** | | | **23h** |
| **合计（含 8.2.5）** | | | **28h** |

---

## 10. PR 切片

| PR | Phase | 文件改动 |
|---|---|---|
| #Y1 | 8.2.1 | `SUT/point_kinetics/` + 5 test fact + 1 BDD + Launcher 2 行 |
| #Y2 | 8.2.2 | `SUT/burnup_bateman/` + 6 test fact + 1 BDD + Launcher 3 行 |
| #Y3 | 8.2.3 | `SUT/sub_channel/` + 5 test fact + 1 BDD + Launcher 2 行 |
| #Y4 | 8.2.4 | `SUT/fuel_pin_conduction/` + 5 test fact + 1 BDD + Launcher 2 行 |
| #Y5 | 8.2.5 | （可选）OpenMC depletion 接入 |
| #Y6 | 8.2.6 | paper writeup + UAT BDD 4 新 + acceptance-rubric / test-procedures sync + baseline 刷新 |

每 PR 独立。

---

## 11. 依赖

| 依赖 | 状态 | 备注 |
|---|---|---|
| **Goal 1 meta-prompt 引擎** | ❌ 未 land | Goal 2 用其自动产 candidate；Phase 8.2.0 prep |
| `SystemMtMrLauncher.BuildMrCatalog` 接 5 SUT × 2-3 MR | ✅ 接入模式已稳定 | 每 phase 加 yield blueprint |
| Python 3.12 + numpy + scipy on cloud | ✅ web-setup.sh 已装 | — |
| BDD step bindings 通用 `CliProgramRunner` | ✅ 已稳定 | 复用 |

---

## 12. 风险

| 风险 | 缓解 |
|---|---|
| home-grown solver 物理过简 | 论文明写 "minimal canonical solver for MR scaffolding"；Phase 8.2.5 可启动作 controlled-comparison |
| 5 方程各 1 PR 总 5+ PR 维护成本 | 每 PR ≤ 5h；连续 4 周 W14-17 持续推进 |
| Stiff ODE (burnup) 收敛慢 | 用 scipy `Radau` 或 `BDF` implicit solver |
| sub-channel 单相 → 两相 reviewer 质疑 | 论文 limitation 段诚实标 "single-phase only, two-phase future work" |
| 测试 wall 增加 | 每方程 SUT 简化版 smoke test < 2s；cumulative < 120s budget 仍守得住 |

---

## 13. 不交付（scope 外，明确）

- **不**接 RELAP / Serpent / MOOSE / OpenFOAM 等重量级工业程序（除非 Phase 8.2.5 启动）
- **不**做多通道 / 两相 / 凝汽器 / 安壳等更高级 thermal-hydraulics 模型
- **不**做空间动力学（仅 point-kinetics，不做 space-time kinetics）
- **不**做 fuel performance 全栈（仅热传导，不做包壳变形 / 间隙模型 / fission gas release）
- **不**做核数据库管理（用现有 ENDF 假设，或 home-grown 用近似常数）

---

## 14. 完成时的 main 状态（Stage 8 ship）

| 指标 | 目标 |
|---|---|
| SUT 总数 | 4 → **9**（含 amax demo + 4 新方程 SUT） |
| Launcher MR 数 | 5 → **5 + 9 = 14**（5 原有 + 9 新方程） |
| BDD scenarios | 30 → **30 + 5 = 35** |
| UAT BDD | 49 (含 Goal 1 UC-C12) → **49 + 4 = 53**（UC-C13~C16） |
| 全套 facts | 546 (Goal 1 后) → **546 + 21 = 567** |
| `equation.md` 入仓 | 5 → **9** |
| 论文 writeup | `docs/experiments/2026-XX-yy-five-equations/README.md` 落成 |

---

## 15. 时间盒提醒

- Goal 1（meta-prompt 引擎）必须先 land —— Goal 2 用其产 candidate
- Goal 2 各 phase 顺序灵活，但建议 **W14: point-kinetics + burnup** / **W15: sub-channel + fuel-pin** / **W16: paper writeup + UAT + baseline 刷新** / **W17（可选）: OpenMC depletion follow-up**
- 跟 v2.1.0 发版**互不阻塞** —— v2.1 走 Windows UAT round-1 PASS → tag；Stage 8 是 v2.2 主线
