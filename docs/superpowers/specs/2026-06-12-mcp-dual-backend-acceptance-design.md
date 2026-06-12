# MCP Server 双后端验收测试设计（docker / WSL-local）

日期：2026-06-12

关联设计：`docs/superpowers/specs/2026-06-12-metbench-docker-runtime-mcp-design.md`（已合入 main，PR #358）

## 1. 范围与目标

对已合入 main 的 Docker runtime MCP backend 做**真实拓扑验收**：Windows 主机启动
MetBench WPF，经局域网 IP（非 loopback）访问运行于 WSL2 内的 MCP server，提交异步
MT 作业，验证两条判据：

1. MCP client 访问 server 正常（preflight `runtime_health` 连通性证据）；
2. MetBench 异步执行 MT 正常（async job 终态 `Succeeded`，`MrRunResult` 持久化）。

两个测试用例，唯一变量是 server 执行后端：

| 用例 | server 位置 | 执行后端 | OpenMC 所在 |
|---|---|---|---|
| 1. docker mcp server | WSL2 内，:8765 | `backend=docker`，经 Docker Desktop WSL integration 跑 `metbench-sut:latest` 容器 | 容器内 `/opt/openmc-venv` |
| 2. wsl mcp server | WSL2 内，:8766 | `backend=local`，直接在 WSL 环境执行 argv | WSL venv（与 Dockerfile 同款源码构建） |

两个执行环境均安装 OpenMC、依赖库及官方 ENDF/B-VII.1 截面数据库。

**已确认事实（冲突挑明）**：MetBench 现有 3 个 OpenMC MR 全部运行在多群模式，截面库
`mg_cross_sections.h5` 由 `SUT/openmc/openmc_runner.py` 运行时自生成（`openmc_runner.py:224-226`），
验收路径不消费外部截面数据库。安装 ENDF/B-VII.1 是用户要求的环境完备性项，
以 `OPENMC_CROSS_SECTIONS` 指向有效库文件作为证据，不影响验收判据。

## 2. 集成缺口（验收必须先关闭，均已对 main 核实）

合入的 Phase B 仅用 mock 验证过，从未在「Windows 主机 + Linux 执行环境」真实拓扑下运行：

| # | 缺口 | 证据 |
|---|---|---|
| G1 | parser/output-parser 命令用容器 python 路径构建，却在 Windows 本地执行 | `SystemMtLauncher.cs:191-215`：同一 `pythonExecutable` 喂给 parser/runner 三条命令 |
| G2 | runner argv 含 Windows 绝对路径（SUT 脚本、`--input`/`--output` 临时文件），WSL/容器内不可解析 | `SystemMtLauncher.cs:170` workRoot 用 `Path.GetTempPath()`；`DockerMcpProcessExecutor.cs:29` argv 原样转发 |
| G3 | server `docker run` 只挂载 `repo_root` + `/tmp`，翻译后的 `/mnt/c/...` 临时路径容器内不可见 | `server.py:190-202` |
| G4 | server 无 local 执行后端 | `server.py` 仅 `build_docker_run_command` 一条执行路径 |

方案取舍：client 侧路径翻译 + server 侧双后端（选定）；备选「server 侧翻译」被否
（G1 无论如何只能 client 侧解决，且 Windows 主机知识不应泄漏进 OS 无关的 server）；
备选「MetBench 进 WSL 跑」被否（违背 Windows WPF 经局域网验收的意图）。

## 3. 架构

```
Windows host                          │ WSL2 (Ubuntu 24.04)
                                      │
MetBench WPF (SystemMtAsyncJobPage)   │
  → SystemMtJobService 入队           │
  → SystemMtAsyncPipeline             │
  → SystemMtLauncher                  │
     ├─ parser/output-parser：本地     │
     │  Windows python 执行（新）      │
     └─ SUT runner：路径翻译后（新）    │
        HTTP POST /tool ──────────────┼→ server.py :8765  backend=docker（用例1）
        Bearer token    ──────────────┼→ server.py :8766  backend=local （用例2）
                                      │   ├─ 用例1: docker run metbench-sut(+xs)
                                      │   │  （挂 /mnt/d、/mnt/c/...Temp、截面库）
                                      │   └─ 用例2: WSL venv 直接执行 openmc_runner.py
```

- server 绑定 WSL 私有 IPv4（现有 `auto-private-ipv4` 逻辑），Windows 经该非回环
  LAN IP 访问；实际 IP 记入验收证据。
- 两个 server 不同端口同时驻留；用例切换只改 `METBENCH_OPENMC_PYTHON` 的 URI
  （`endpoint` 与 `python` 参数不同），重启 WPF 生效。
- 文件交换依赖 WSL `/mnt/c`、`/mnt/d` drvfs：Windows 临时目录与 SUT 脚本对
  WSL/容器天然可见，是路径翻译方案成立的前提。

## 4. 新代码组件（全部 CI 可测）

### 4.1 server.py（`infra/mcp/docker-runtime/`）

- config 新增 `backend: "docker" | "local"`，缺省 `docker`，未知值 fail-closed。
- `local` 后端：`run_sut_command` 直接 `subprocess.run(argv)`，不经 docker；`image`
  参数仍必须命中 `allowed_images` key（协议、审计、allowlist 语义不变；local 后端下
  条目的 `dockerfile` 字段可缺省）；`build_runtime_image` 返回显式错误，不做假 build。
- `docker` 后端：`build_docker_run_command` 把 `allowed_mount_roots` 逐一以
  `-v root:root` 挂进容器；`repo_root` + `/tmp` 挂载保持向后兼容。

### 4.2 .NET BLL.Core

- `docker-mcp://` URI 新增两个可选 fail-closed 参数：
  - `localPython` —— Docker profile 下 parser/output-parser 命令改用它（缺省回退
    `LauncherOptions.SystemPython`）；runner 仍用容器 `python`（关 G1）；
  - `pathStyle=wsl` —— `DockerMcpProcessExecutor` 把 argv 中匹配
    `^[A-Za-z]:[\\/]` 的 token 翻译为 `/mnt/<盘符小写>/...`，反斜杠转正斜杠，
    其余 token 原样（关 G2）；`pathStyle` 出现但值非 `wsl` →
    `RuntimeEnvironmentResolutionException`。
- `DockerMcpRuntimeOptions` 扩展 `LocalPythonExecutable?` / `PathStyle`；
  不改 `ISystemMtLauncher` 签名，遵守 facade 类型泄漏规则。

### 4.3 WPF（一处最小必改）

**已核实**：`App.xaml.cs:137-143` 实际只注册 `SystemPython` + `OpenMocPython`，
未读 `METBENCH_OPENMC_PYTHON`、未设 `RuntimePythons`（与 CLAUDE.md §6 文档示例
存在漂移）。`ResolvePythonExecutable("openmc")` 因此落到 `SystemPython`，
docker-mcp URI 无法进入。必改一行：

```csharp
OpenMcPython: Environment.GetEnvironmentVariable("METBENCH_OPENMC_PYTHON")
    ?? (OperatingSystem.IsWindows() ? "python" : "python3")
```

URI 经兼容字段进入后，`LauncherOptionsRuntimeProfileProvider.cs:19-21`
（先 `ResolvePythonExecutable` 再判断 `docker-mcp://` 前缀）即可建出 Docker
profile。该改动同时消除上述文档漂移；Windows 本机可编译验证。

## 5. 环境与部署资产

- **WSL**：Ubuntu 24.04；OpenMC venv 复用 `docker/Dockerfile` 同款源码构建步骤
  （cmake + pip + binary symlink）；Docker Desktop WSL integration 提供 `docker` CLI。
- **截面数据库**：ENDF/B-VII.1（endfb71_hdf5，约 2GB）下载至 WSL
  `/opt/openmc-data/endfb71_hdf5/`。用例 2 经 server 进程环境继承
  `OPENMC_CROSS_SECTIONS`；用例 1 把 `/opt/openmc-data` 加入 `allowed_mount_roots`
  挂载 + 薄层镜像（`FROM metbench-sut:latest` + `ENV OPENMC_CROSS_SECTIONS=...`）。
- **server 配置**：`config.docker.json`（:8765）与 `config.local.json`（:8766）
  两份示例入库；token 用占位符，实际 token 不入库。

## 6. 数据流（用例 1 一次 RunMr）

1. WPF 异步页提交 `RunMr(openmc-pincell-nu-sigma-f)` → 入队 → Worker →
   `ISystemMtLauncher.RunAsync`；
2. profile 解析（URI）→ `RuntimeKind.Docker` → preflight 调 `runtime_health` →
   `RuntimeEvidence` 记录连通性（验收判据 1）；
3. parser 本地（Windows python）跑：读 sample、生成 follow-up 输入，写
   `C:\Users\...\Temp\<workroot>`；
4. runner 命令翻译路径后经 `run_sut_command` 发往 server → 容器内 OpenMC 计算
   （输入输出均在 `/mnt/c/...Temp/...`，即 Windows 临时目录本体）；
5. output parser 本地读输出 → MR 断言 → `MrRunResult` 持久化 → job `Succeeded`
   （验收判据 2）。

用例 2 仅第 4 步变为 WSL venv 直接执行。

## 7. 错误处理

沿用既有 fail-closed 模式：URI 参数非法 → `RuntimeEnvironmentResolutionException`；
server 不可达 / 401 / 未知 backend → preflight Blocked → job 终态 `Failed` +
RuntimeEvidence。基础设施失败不混入 MR 断言异常（与既有批量终态语义一致）。

## 8. 测试策略（TDD 三层 + WPF 手动）

| 层 | 内容 | 门控 |
|---|---|---|
| Python 单元 | backend 配置 fail-closed、local 后端执行（fake subprocess）、docker 挂载含 mount roots、local+build 显式报错 | 无条件跑 |
| .NET 单元 | 新 URI 参数解析矩阵（合法/非法/缺省）、launcher 的 parser 命令用 localPython 而 runner 用容器 python（防 G1 回归 fact 断言）、路径翻译表驱动用例 | 无条件跑（CI） |
| .NET 验收 | 环境门控（仿 `OpenMocImportable` 模式，`METBENCH_MCP_ACCEPTANCE_URI` 未设则 skip）：真实 preflight 连通 + `RunAsync` 端到端真实 OpenMC + async job 到 `Succeeded`；两后端各跑一遍，留 `.trx` | 本机验收时跑 |
| WPF 手动 | 启动 MetBench，对两后端各提交 RunMr + RunBatch（3 个 openmc MR），截图 job `Succeeded` 与结果页，写 vm-evidence 文档（照 `docs/superpowers/specs/2026-06-05-*-vm-evidence/` 先例） | 验收终局 |

## 9. 验收判据

1. **client→server 正常**：两后端的 preflight `RuntimeEvidence` 均记录 docker-mcp
   健康检查 pass；server 端有对应 run_id 记录。
2. **异步 MT 正常**：两后端的 async job 均到终态 `Succeeded`，`MrRunResult`
   持久化进 `SystemMT.Litedb`，WPF 页面截图为证。

## 10. 不交付（Out of scope）

- 不新增 `RuntimeKind`、不新增 `wsl-mcp://` scheme（local 后端对 client 透明）；
- 不改 WPF UI/XAML/导航（WPF 仅 §4.3 的 `App.xaml.cs` 一行 DI 改动）；
- 不新增消费连续能量截面库的 MR；
- 不做双机拓扑、不要求 WSL mirrored networking；
- 不做 result/evidence 导入（信任模型未建立，沿用既有边界）。

## 11. Windows Classification

需要 Windows 证据：本验收的核心就是 Windows 主机上的 WPF 真实运行（WPF 手动层 +
环境门控验收测试在 Windows 执行）。新增生产代码本身全部位于 cloud-safe
`MetBench_BLL.Core` 与 Python `infra/`，CI 可编译可测。
