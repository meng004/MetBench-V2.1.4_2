# Minimum-MR-SubSet T3 归类评估

> Date: 2026-05-31
> External repo: `https://github.com/meng004/Minimum-MR-SubSet`
> Local read-only clone: `/private/tmp/minimum-mr-subset`
> MetBench live local head during assessment: `0f5f9f4` (`d85b74e fix(client): make PagingBar localized Run.Text bindings OneWay`)
> Result: **不建议整体放入 T3；主归类应为 T6 mutation / adequacy 研究资产，T3 只可按单个 PUT 条件性拆分候选。**

## 1. 证据摘要

本次评估读取了外部仓库元信息、状态文件、研究设计、PUT 适配器、LLM pipeline、filter/oracle、已有数据与测试，并派发 gpt-5.4 subagent 做独立只读复核。

关键证据：

- `STATUS.md` 明确该项目是 T2 / Minimum Complete MR Subset 研究工件，当前仍有 R2-3 inter-rater kappa blocker 与 9 个 confirmatory PUT 后续任务。
- `phase1_rq.md` 的主问题是给定故障模型 `M` 和候选 MR 集 `R`，是否存在最小完备子集 `S`，并研究 Set Cover / greedy / ILP / 退化映射。
- `phase4_design.md` 定义 10 个 PUT 选型，但用途是支撑最小完备 MR 子集实证，而非 MetBench catalog-ready SUT onboarding。
- `experiments/puts/` 有 10 个 `run_canonical()` 适配器；`tests/puts/test_smoke.py` 只要求每个 PUT 在 60 秒内返回有限 observables。
- `data/raw/p1_heat/` 已有 P1 heat 的 `mrs.json`、`layer2_ratings.json`、`layer3_arbitration.json`、`detection_matrix.csv`，但未看到 10 个 PUT 全部达到同等数据闭环。
- `scripts/llm/multi_llm_pipeline.py`、`scripts/filters/layer1_mechanical.py`、`scripts/oracles/subcase_*.py` 表明核心能力是 MR 生成 / 筛选 / rater agreement / 退化 oracle，而不是 MetBench T3 SUT catalog 运行链路。

## 2. T 层映射

| 对象 | 外部仓库证据 | 与 MetBench T3 的关系 | 推荐归类 |
|---|---|---|---|
| 10 个 PUT 适配器 | `experiments/puts/p1_heat.py` 到 `p10_pinn_hnn.py`，`run_canonical()` smoke 接口 | 概念上覆盖 Heat/Wave/Lorenz/Pendulum/PKE/Poisson/Burgers/Schrodinger/OpenMC/PINN，但不是 MetBench `SUT/<sut>/catalog.json` 形态 | T3 候选素材，不是可直接纳入资产 |
| P1 heat 数据 | `data/raw/p1_heat/*` | 有 MR / rating / detection matrix 样例，可做 T6 adequacy 数据样板 | T6 优先 |
| MR subset selection | `phase1_rq.md`、`phase4_design.md` | 关注 MR 集合选择与故障模型相对完备，不是代表性方程覆盖 | T6 主归类 |
| Multi-LLM MR pipeline | `scripts/llm/*` | 与 MetBench T4 MR 识别相关，但外部项目用于 T2 论文实证 | T4 参考，非 T3 |
| Filters / oracles | `scripts/filters/*`、`scripts/oracles/*` | 可为 MR 质量治理或 T6 子集选择提供参考 | T6 / 治理参考 |

## 3. 判定

**不建议把 `minimum-mr-subset` 整体放入 MetBench T3。**

理由：

1. MetBench T3 的当前定义是“按 ODE / PDE 选代表性方程，每个方程至少 1 个可执行 MT 的 SUT”，核心是可执行 SUT 覆盖；该仓库主问题是最小完备 MR 子集选择，核心是 mutation / adequacy / set cover。
2. 该仓库虽然有 10 个 PUT，但接口层是 `run_canonical()` smoke 和论文实验适配，不具备 MetBench T3 所需的 manifest catalog、typed predicate、sample case、launcher end-to-end、skip policy 和 evidence recorder 链路。
3. MetBench 当前 T3 已覆盖 Heat / Wave / Poisson / Burgers / OpenMC 等相邻对象；直接引入外部仓库的同名 PUT 不会自动增加新的 T3 覆盖，除非它以新的方程、方法族或真实外部求解器身份通过 next-SUT gate。
4. 外部仓库状态显示主实验仍在推进，P1 heat 之外的 confirmatory PUT 数据闭环仍待完成，不适合作为 release-quality T3 资产。
5. subagent 独立复核结论一致：该仓库更像 T6 mutation / adequacy 研究资产，而不是当前 MetBench T3 的可接入 SUT 候选。

## 4. 条件性 T3 候选

可以把其中个别 PUT 作为未来 T3 候选素材，但必须逐个立 scoped plan，而不是整体导入。

| PUT | T3 价值 | 当前判断 |
|---|---|---|
| `p5_pke` | 反应堆点堆动力学 ODE，可能补充 reactor-anchor 深化 | 候选优先级较高，但需验证真实模型、依赖、输入输出、MR 语义与可重复性 |
| `p3_lorenz` / `p4_pendulum` | 非线性 ODE / 动力系统，可能扩展 ODE 代表性 | 可作为 T3 后续候选，但需证明不是 toy-only smoke |
| `p8_schrodinger` | 复值 PDE，可能补充现有 PDE 谱系 | 有候选价值，但需补真实 solver 和 typed MR |
| `p10_pinn_hnn` | ML/PINN/data-driven SUT driver | 符合 T3 next-SUT driver 之一，但当前更像 smoke/surrogate，不能直接纳入 |
| `p1_heat` / `p2_wave` / `p6_poisson` / `p7_burgers` | 与已纳入 MetBench T3 的方程高度重叠 | 仅可作对照实现或交叉验证，不构成新覆盖 |
| `p9_openmc` | 与既有 OpenMC/Boltzmann 路径重叠 | 若是 surrogate，不可替代现有 OpenMC；若变成真实配置，可作为 Boltzmann deepening plan |

## 5. 未来准入条件

若未来要从该仓库吸收单个对象进入 T3，最小准入条件为：

1. 新建 candidate-specific T3 plan，并注册到 active plan index。
2. 提供 MetBench `SUT/<sut>/` 形态：sample input、runner/adapter、manifest catalog、MR metadata、typed predicate 或明确 fail-closed mapping。
3. 至少一个 MR 通过 `ISystemMtLauncher` end-to-end，生成 source/follow-up 输出和 assertion result。
4. 明确运行时依赖、skip policy、CI / VM 验证边界。
5. 明确该 PUT 增加了哪类 T3 覆盖：新方程、新程序类型、新求解方法、reactor anchor deepening、ML/PINN driver，或缺失 meta-pattern。
6. 不依赖尚未完成的 LLM rater pipeline 作为 T3 运行时前置。

## 6. 验证记录

执行过的验证：

- GitHub 元信息读取：`gh repo view meng004/minimum-mr-subset`，确认默认分支 `main`，仓库描述为 `Minimum MR subset research workspace`。
- 只读克隆：`gh repo clone meng004/minimum-mr-subset /private/tmp/minimum-mr-subset`。
- 文件结构检查：确认 `experiments/puts/`、`scripts/llm/`、`scripts/filters/`、`scripts/oracles/`、`data/raw/p1_heat/`、`tests/` 存在。
- 本地测试尝试：`python3 -m pytest tests/puts/test_smoke.py tests/toy_put/test_toy_correctness.py tests/filters/test_layer1.py -q`。

验证限制：

- 本机 Python 环境缺 `pytest`、`numpy`、`scipy`、`z3`，因此未取得外部仓库测试通过证据；这不能记作 green，只能记作环境未满足。
- `experiments/env/requirements.txt` 列出 `pytest`、`z3-solver`、LLM 客户端等依赖，但未列 `numpy/scipy`；实际 PUT smoke 测试会导入 `numpy`，部分 PUT 还需要 `scipy`。

## 7. 结论

`minimum-mr-subset` 的整体价值很高，但它当前应作为 **T6 mutation / adequacy / minimum MR subset** 的研究资产和未来治理参考，不应整体放入 **T3 coverage**。

T3 可吸收的不是这个仓库本身，而是其中经过单独准入的 PUT 候选；当前最稳妥路线是先记录为 T6 参考，再为 `p5_pke`、`p8_schrodinger` 或 `p10_pinn_hnn` 这类真正能新增覆盖的对象另开 scoped T3 candidate plan。

## 8. 后续基础设施记录

后续应建立一个独立的导入基础设施，而不是临时复制外部仓库内容。目标是让 MetBench 未来可以从 `minimum-mr-subset` 一类研究仓库中受控导入：

- SUT 候选：把外部 PUT 描述转成 MetBench `SUT/<sut>/` runner / sample / manifest / skip-policy 结构。
- MR 候选：把外部 MR、operator class、non-trivial rating、derivation chain 转成 T4 discovery draft 或 T0 manifest-compatible binding。
- Mutation / adequacy 成果：把 detection matrix、mutant/operator classes、minimum MR subset 结果导入 T6 adequacy analytics。
- Provenance：保留外部仓库 URL、commit、数据文件路径、生成模型/评审者、筛选规则和导入时间，避免研究证据与产品 catalog 混淆。

该基础设施应先以 docs/spec + small adapter prototype 进入新 scoped plan；在没有完成导入 schema、provenance 和 fail-closed 校验前，不应把外部 PUT/MR 直接注册进运行时 catalog。
