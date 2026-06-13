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

自动化验收（`McpThreeCaseAcceptanceTests`，经局域网 IP 非 loopback）：各 **3 passed / 0 failed / 0 skipped**。
- 用例 1：endpoint `http://192.168.50.111:8764`（host 局域网 IP），MR `p3-trajectory-sensitivity`
- 用例 2（**正规拓扑，2026-06-13 重验**）：endpoint `http://192.168.50.111:8765`（**host 局域网 IP**），
  server 在 **Windows host** 上经 **Docker Desktop 29.5.3** 跑 `backend=docker`，`repo_root=D:\Codes\MetBench-V2.1.4_2`，
  MR `openmc-pincell-nu-sigma-f`，证据 `mcp-acceptance-case2-host.trx` + `case2-host-*.png`（Job f88868ef，
  Succeeded，SUT=openmc）+ `case2-host-server.log`。
- 用例 2（WSL 旧证据，保留）：endpoint `http://172.24.17.83:8765`（WSL 私网 IP），server 在 WSL 用 docker-ce。
- 用例 3：endpoint `http://172.24.17.83:8766`，MR `openmc-pincell-nu-sigma-f`，WSL venv `/opt/openmc-venv`

**用例 2 正规拓扑下 G5（Windows→`/mnt` 挂载目标翻译）现场打通**（此前仅单测覆盖）：手工
`run_sut_command` 冒烟的生成命令为
`docker run --rm -v D:\Codes\MetBench-V2.1.4_2:/mnt/d/Codes/MetBench-V2.1.4_2 -v C:\Users\lemon\AppData\Local\Temp:/mnt/c/Users/lemon/AppData/Local/Temp -w /mnt/d/Codes/MetBench-V2.1.4_2 metbench-sut:latest /opt/openmc-venv/bin/python ... --output /mnt/c/.../Temp/g5-smoke-out.json`，
容器内 OpenMC 算出 k_eff=1.1245 并写回 Windows 临时目录、host 直接读到——Windows 盘符源路径
挂到容器 `/mnt/<盘>/...` 目标、与 client 侧 `pathStyle=wsl` argv 翻译前缀一致的完整链路得到端到端验证。

## 2. WPF UI 证据（FlaUI 自动化，`tools/uia-acceptance`）

每用例 4 张截图（启动 → 异步执行页 → 提交后 → Succeeded 终态）：
`case<N>-01-startup.png` / `case<N>-02-asyncpage.png` / `case<N>-03-submitted.png` / `case<N>-04-succeeded.png`。
操作为模拟用户路径：启动 WPF → 导航 System MT Async Execution 页 → ComboBox 选 MR → Submit → 轮询至 Succeeded。
WPF 运行时配置经 `appsettings.local.json` 的 `LauncherOptions:RuntimePythons` 注入（#361 接线），
token 经 `METBENCH_DOCKER_MCP_TOKEN` 环境变量传递。

## 3. 环境

- WSL2 Ubuntu-24.04：docker-ce 29.1.3（本次安装）、`/opt/openmc-venv`（OpenMC master 源码构建，
  dev 版本号 0.0.0，`import openmc` 与 `openmc` 二进制均验证通过）。
- **Windows host：Docker Desktop 29.5.3（2026-06-13 经 winget 安装）**，用例 2 正规拓扑使用；
  SUT 镜像经 `docker save`（WSL docker-ce）→ `docker load`（Docker Desktop 引擎）导入，
  容器内 `import openmc` 冒烟通过，免去 host 端重新编译。
- `metbench-sut:latest` 镜像：`docker/Dockerfile` 全量构建（OpenMOC + OpenMC），容器内
  `import openmc` 冒烟通过。
- 截面数据库未安装（按用户决定；验收 MR 全部为多群模式、运行时自生成截面库）。
- MCP server 均经 CLI `python(.exe)/python3 server.py <config>` 启动；绑定地址来自
  `auto-private-ipv4` 启动日志行。

## 4. 偏离与已知限制（如实记录）

1. **用例 2 已在正规拓扑（Windows host + Docker Desktop）重验通过**（2026-06-13，偏离已消除）：
   首轮因本机当时无 Docker Desktop，曾改在 WSL 内用 docker-ce 跑 `backend=docker` server
   （旧证据 `case2-*` 保留）；随后按用户要求安装 Docker Desktop 29.5.3，在 **Windows host** 上以
   `repo_root=D:\Codes\MetBench-V2.1.4_2` 重跑 server，自动化 3/3 + WPF UI 4 截图均通过
   （`mcp-acceptance-case2-host.trx` / `case2-host-*`）。此拓扑**首次现场触发 G5**（§1 已记录生成命令与
   k_eff 结果），Windows 盘符源路径→容器 `/mnt/<盘>/...` 目标的挂载翻译端到端验证，不再仅靠单测。
   两轮 server 均经宿主代理拉取镜像/依赖。
2. **RunBatch UI 演示未执行**：FlaUI 工具当前仅支持单 MR 提交；异步批量路径由主测试套件既有
   测试覆盖（非本次三用例判据项）。
3. **截图为锁屏会话捕获**：输入注入被会话策略拦截，工具改用 UIA Pattern（Invoke/SelectionItem/
   ExpandCollapse）+ 应用自身消息队列（PostMessage 空格激活导航），截图经
   `PrintWindow(PW_RENDERFULLCONTENT)` 渲染真实窗口内容。
4. **case2 首轮 04 截图竞态已重取证**：作业历史列表中的历史 Succeeded 行曾被工具误匹配导致
   截图早于当前作业完成（首轮 04 图显示 RunningSource）。清空 `SystemMtJobs.Litedb`
   历史后重跑取证，现 `case2-04-succeeded.png` 为当前作业（Job 13f858de）的真实终态。
   工具的终态匹配按当前作业状态块改进为后续可选项。
5. run_id 计数说明：case1 8 条 =自动化验收 4 + FlaUI 两轮 UI 运行各 2；case2（WSL 旧证据）8 条 =
   自动化 4 + UI 两轮 4（首轮截图作废但运行真实完成）；case3 6 条 = 自动化 4 + UI 1 轮 2；
   case2-host 7 条 = G5 手工冒烟 1 + 自动化 4 + UI 1 轮 2。

## 5. 工具

- `tools/uia-acceptance/`（FlaUI.UIA3，net8.0-windows，独立于 MetBench.sln）：
  `UiaAcceptance --exe <MetBench_Client.exe> --mr <id> --case <n> --evidence <dir> [--timeout-seconds N]`，
  另有 `--dump` 模式输出自动化树。
