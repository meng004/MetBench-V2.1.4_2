# MCP 三用例验收证据汇总（vm-summary）

日期：2026-06-13
机器：Windows 11 IoT Enterprise LTSC 2024（单机拓扑：Windows host + WSL2 Ubuntu-24.04）
分支：`mcp-dual-backend-acceptance`
依据：spec `docs/superpowers/specs/2026-06-12-mcp-three-case-acceptance-design.md` §8/§9；
runbook `docs/uat/mcp-three-case-acceptance-runbook.md`

## 1. 判据矩阵（spec §9）

| 判据 | 用例 1（Windows local-python :8764） | 用例 2（docker 后端 :8765） | 用例 3（WSL 模拟远程 :8766） |
|---|---|---|---|
| P1 client→server 正常（preflight `runtime_health` pass + server 端 run_id 记录） | ✅ `mcp-acceptance-case1.trx`（3/3）+ `case1-server.log` 8 条 `run_sut_command ... status=completed` | ✅ `mcp-acceptance-case2.trx`（3/3）+ `case2-server.log` 8 条 | ✅ `mcp-acceptance-case3.trx`（3/3）+ `case3-server.log` 6 条 |
| P2 异步 MT 正常（async job 终态 `Succeeded`、`MrRunResult` 持久化） | ✅ trx `Acceptance_3` + WPF 截图 `case1-04-succeeded.png` | ✅ trx `Acceptance_3` + `case2-04-succeeded.png`（Job 13f858de，State=Succeeded，MR assertion passed） | ✅ trx `Acceptance_3` + `case3-04-succeeded.png`（Job 4087517c，State=Succeeded，SUT=openmc） |

自动化验收（`McpThreeCaseAcceptanceTests`，经局域网 IP 非 loopback）：三用例各 **3 passed / 0 failed / 0 skipped**。
- 用例 1：endpoint `http://192.168.50.111:8764`（host 局域网 IP），MR `p3-trajectory-sensitivity`
- 用例 2：endpoint `http://172.24.17.83:8765`（WSL 私网 IP），MR `openmc-pincell-nu-sigma-f`，容器镜像 `metbench-sut:latest`
- 用例 3：endpoint `http://172.24.17.83:8766`，MR `openmc-pincell-nu-sigma-f`，WSL venv `/opt/openmc-venv`

## 2. WPF UI 证据（FlaUI 自动化，`tools/uia-acceptance`）

每用例 4 张截图（启动 → 异步执行页 → 提交后 → Succeeded 终态）：
`case<N>-01-startup.png` / `case<N>-02-asyncpage.png` / `case<N>-03-submitted.png` / `case<N>-04-succeeded.png`。
操作为模拟用户路径：启动 WPF → 导航 System MT Async Execution 页 → ComboBox 选 MR → Submit → 轮询至 Succeeded。
WPF 运行时配置经 `appsettings.local.json` 的 `LauncherOptions:RuntimePythons` 注入（#361 接线），
token 经 `METBENCH_DOCKER_MCP_TOKEN` 环境变量传递。

## 3. 环境

- WSL2 Ubuntu-24.04：docker-ce 29.1.3（本次安装）、`/opt/openmc-venv`（OpenMC master 源码构建，
  dev 版本号 0.0.0，`import openmc` 与 `openmc` 二进制均验证通过）。
- `metbench-sut:latest` 镜像：`docker/Dockerfile` 全量构建（OpenMOC + OpenMC），容器内
  `import openmc` 冒烟通过。
- 截面数据库未安装（按用户决定；验收 MR 全部为多群模式、运行时自生成截面库）。
- MCP server 均经 CLI `python(.exe)/python3 server.py <config>` 启动；绑定地址来自
  `auto-private-ipv4` 启动日志行。

## 4. 偏离与已知限制（如实记录）

1. **用例 2 server 宿主从 Windows host 移到 WSL**：本机未安装 Docker Desktop（也无任何 docker），
   改在 WSL 内安装 docker-ce 并在 WSL 内跑 `backend=docker` 的 server。docker 后端语义
   （容器内 OpenMC、挂载根、argv 经 `pathStyle=wsl` 翻译、LAN 访问）全部如实验证；
   Windows 源路径挂载目标翻译（G5）未被现场路径触发（配置根已是 `/mnt/*` 形态），该行为由
   `test_server.py` 单元测试覆盖。dockerd/构建经宿主代理（`172.24.16.1:7897`）拉取镜像与依赖。
2. **RunBatch UI 演示未执行**：FlaUI 工具当前仅支持单 MR 提交；异步批量路径由主测试套件既有
   测试覆盖（非本次三用例判据项）。
3. **截图为锁屏会话捕获**：输入注入被会话策略拦截，工具改用 UIA Pattern（Invoke/SelectionItem/
   ExpandCollapse）+ 应用自身消息队列（PostMessage 空格激活导航），截图经
   `PrintWindow(PW_RENDERFULLCONTENT)` 渲染真实窗口内容。
4. **case2 首轮 04 截图竞态已重取证**：作业历史列表中的历史 Succeeded 行曾被工具误匹配导致
   截图早于当前作业完成（首轮 04 图显示 RunningSource）。清空 `SystemMtJobs.Litedb`
   历史后重跑取证，现 `case2-04-succeeded.png` 为当前作业（Job 13f858de）的真实终态。
   工具的终态匹配按当前作业状态块改进为后续可选项。
5. run_id 计数说明：case1 8 条 =自动化验收 4 + FlaUI 两轮 UI 运行各 2；case2 8 条 =
   自动化 4 + UI 两轮 4（首轮截图作废但运行真实完成）；case3 6 条 = 自动化 4 + UI 1 轮 2。

## 5. 工具

- `tools/uia-acceptance/`（FlaUI.UIA3，net8.0-windows，独立于 MetBench.sln）：
  `UiaAcceptance --exe <MetBench_Client.exe> --mr <id> --case <n> --evidence <dir> [--timeout-seconds N]`，
  另有 `--dump` 模式输出自动化树。
