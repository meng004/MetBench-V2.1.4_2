# Minimum-MR-SubSet T3 归类评估计划

> Date: 2026-05-31
> Task: 读取 GitHub `meng004/minimum-mr-subset`，评估其实验对象是否可以放入 MetBench T3。
> Method: superpowers `writing-plans` + `subagent-driven-development`；一个主评估者 + 一个 gpt-5.4 subagent 独立复核。

## 1. 目标

判断 `meng004/minimum-mr-subset` 的实验对象是否应纳入 MetBench T3（覆盖层），并说明：

- 仓库的主身份：SUT、MR 集、MR 子集选择算法、评估工具，或混合工件。
- 与 MetBench T3 定义的匹配度。
- 若不能直接纳入 T3，应归入哪个 T 层或作为何种候选资产保留。
- 若未来允许纳入 T3，需要满足哪些最小准入条件。

## 2. 前置条件

- MetBench 本地仓库已同步到 `main...origin/main`。
- 以 `docs/status/current.md`、live git、活跃计划索引、`CLAUDE.md` T0-T6 定义为本仓库真相源。
- 外部仓库只读获取，不修改外部仓库，不修改 MetBench 生产代码。

## 3. 执行步骤

1. 同步并核对 MetBench 当前状态源。
2. 读取 GitHub 仓库元信息和文件结构，克隆到 `/private/tmp/minimum-mr-subset` 作只读分析。
3. 审阅外部仓库的状态文件、研究设计、PUT 适配器、LLM/MR pipeline、oracle/filter、已有数据与测试。
4. 派发 gpt-5.4 subagent 独立评估，要求它不改文件，只给出结构、核心算法、T3 适配性、证据路径和纳入建议。
5. 对照 MetBench T3/T6 定义形成测评结论。
6. 运行最小本地验证命令；若因环境缺依赖无法运行，记录为验证限制而非通过证据。

## 4. 验收标准

- 明确给出 `放入 T3 / 不放入 T3 / 条件性候选` 的判定。
- 判定必须引用具体仓库证据：文件、状态、测试、数据或 subagent 复核结论。
- 明确区分“外部仓库已有内容”和“进入 MetBench T3 还缺什么”。
- 明确指出更合适的 T 层归属或后续吸收路径。

