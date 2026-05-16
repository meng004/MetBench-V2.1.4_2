# UAT 样本数据

| 文件 | 用例 | 说明 |
|------|------|------|
| `uat-rcase-spec.json` | UC-D1 R-Case 自动复现 | RCaseReproductionSpec 输入结构样例 |
| `uat-llm-providers.example.json` | UC-C4 Multi-LLM Consensus | 3 家 provider 配置样例（API key 走 env，**不**入仓库） |
| `uat-consensus-prompt.txt` | UC-C4 Multi-LLM Consensus | 发给 LLM 的 plausibility 判断 prompt 样例 |
| `uat-mr-spec.json` | UC-A5 新建 method-level MR | MR 表单字段样例 |

## 其他验收用 SUT 样本（仓库已自带）

| 路径 | 用途 |
|------|------|
| `SUT/openmoc/sample/pincell.json` | OpenMOC 主验收 source 输入 |
| `SUT/openmoc/sample/pincell-asymmetric.json` | 镜像 MR followup |
| `SUT/openmoc/sample/pincell-offcentre.json` | 边界 MR |
| `SUT/heat_equation/sample/gaussian.json` | Heat-Equation amplitude MR |

如需更多 R-Case 样本，参考 [docs/experiments/](../../experiments/)。
