# MCP Server 三用例验收测试设计（local-python / docker / WSL 模拟远程）

日期：2026-06-12

关联设计：`docs/superpowers/specs/2026-06-12-metbench-docker-runtime-mcp-design.md`（已合入 main，PR #358）

## 1. 范围与目标

对已合入 main 的 Docker runtime MCP backend 做**真实拓扑验收**：MCP server 经 CLI
启动（`python server.py CONFIG_PATH`，`server.py:390-395` 现成入口，每用例一份
config），Windows 主机启动 MetBench WPF，经局域网 IP（非 loopback）连接 MCP
server，提交异步 MT 作业，验证两条判据：

1. MCP client 访问 server 正常（preflight `runtime_health` 连通性证据）；
2. MetBench 异步执行 MT 正常（async job 终态 `Succeeded`，`MrRunResult` 持久化）。

三个测试用例覆盖「server 宿主 × 执行后端」矩阵：

| 用例 | server 宿主 / 端口 | 执行后端 | SUT 与运行环境 | 验收 MR |
|---|---|---|---|---|
| 1. 本地 python | Windows host，:8764 | `backend=local`，直接 `subprocess(argv)` | 纯 stdlib SUT，Windows 自带 python | `p3-trajectory-sensitivity`（runtime key `system`） |
| 2. docker | Windows host，:8765 | `backend=docker`，经 Docker Desktop 跑 `metbench-sut:latest` | OpenMC 在容器内 `/opt/openmc-venv` | 3 个 `openmc-pincell-*` |
| 3. 远程服务器（WSL 模拟） | WSL2 内，:8766 | `backend=local`，WSL venv 直接执行 | OpenMC 在 WSL venv（与 Dockerfile 同款源码构建） | 3 个 `openmc-pincell-*` |

**决策记录**：

- 用例 1 不用 OpenMC——OpenMC 官方不支持原生 Windows，用纯 stdlib SUT 把验收焦点
  保持在 MCP 链路本身（用户已确认）。OpenMC 由用例 2/3 覆盖。
- **不下载截面数据库**（用户已确认）：MetBench 现有 OpenMC MR 全部运行在多群模式，
  截面库 `mg_cross_sections.h5` 由 `SUT/openmc/openmc_runner.py` 运行时自生成
  （`openmc_runner.py:224-226`），验收路径不消费外部截面数据。两个 OpenMC 环境只装
  OpenMC 及其依赖库。

## 2. 集成缺口（验收必须先关闭，均已对 main 核实）

合入的 Phase B 仅用 mock 验证过，从未在「Windows 主机 + Linux 执行环境」真实拓扑下运行：

| # | 缺口 | 证据 | 影响用例 |
|---|---|---|---|
| G1 | parser/output-parser 命令用 profile 的容器 python 路径构建，却在 Windows 本地执行 | `SystemMtLauncher.cs:191-215`：同一 `pythonExecutable` 喂给 parser/runner 三条命令 | 2、3（用例 1 server 与 client 同宿主同 python，不受影响） |
| G2 | runner argv 含 Windows 绝对路径（SUT 脚本、`--input`/`--output` 临时文件），Linux 执行环境内不可解析 | `SystemMtLauncher.cs:170` workRoot 用 `Path.GetTempPath()`；`DockerMcpProcessExecutor.cs:29` argv 原样转发 | 2、3 |
| G3 | server `docker run` 只挂载 `repo_root` + `/tmp`，Windows 临时目录容器内不可见 | `server.py:190-202` | 2 |
| G4 | server 无 local 执行后端 | `server.py` 仅 `build_docker_run_command` 一条执行路径 | 1、3 |
| G5 | server 在 Windows host 上生成 `-v {root}:{root}` 同路径挂载，Windows 源路径不能作为 Linux 容器内目标路径 | `server.py:190-202` | 2 |
| G6 | WPF 未把任何环境变量喂进 `RuntimePythons`，docker-mcp URI 无法进入 profile 解析 | `App.xaml.cs:137-143` 实际只注册 `SystemPython` + `OpenMocPython`（与 CLAUDE.md §6 文档示例漂移） | 全部 |
| G7 | `SplitCommand` 把 `\` 一律当转义符消费，Windows 路径 token 被拆坏（实施期核实） | `DockerMcpProcessExecutor.cs`（修复前 L63-67） | 全部 |

方案取舍（用户已确认）：client 侧 argv 路径翻译 + server 侧双后端 + server 侧挂载
目标翻译。备选「server 侧 argv 翻译」被否（G1 无论如何只能 client 侧解决）；备选
「MetBench 进 WSL 跑」被否（违背 Windows WPF 经局域网验收的意图）。

## 3. 架构

```
Windows host                              │
                                          │
MetBench WPF (SystemMtAsyncJobPage)       │
  → SystemMtJobService 入队               │
  → SystemMtAsyncPipeline                 │
  → SystemMtLauncher                      │
     ├─ parser/output-parser：本地         │
     │  Windows python 执行（新 localPython）
     └─ SUT runner：HTTP POST /tool + Bearer token
        ├──→ :8764 server.py backend=local   （用例1，同宿主，无路径翻译）
        ├──→ :8765 server.py backend=docker  （用例2，argv 经 pathStyle=wsl 翻译；
        │         server 挂载目标翻译 D:\x → /mnt/d/x，经 Docker Desktop 跑容器）
        │                                 │ WSL2 (Ubuntu 24.04)
        └──→ :8766 server.py backend=local ──（用例3，模拟远程 Linux 服务器；
                  argv 经 pathStyle=wsl 翻译，/mnt/c、/mnt/d 天然可见）
```

- 用例 1/2 的 server 绑定 Windows host 私有 IPv4，用例 3 绑定 WSL 私有 IPv4
  （均为现有 `auto-private-ipv4` 逻辑）；实际 IP 记入验收证据。
- 三个 server 不同端口可同时驻留；用例切换只改对应环境变量里的 URI，重启 WPF 生效。
- 用例 2/3 的文件交换依赖统一翻译规则 `X:\path` → `/mnt/<盘符小写>/path`：
  用例 3 靠 WSL drvfs 天然成立；用例 2 靠 server 按同一规则生成挂载目标。

## 4. 新代码组件（全部 CI 可测）

### 4.1 server.py（`infra/mcp/docker-runtime/`）

- config 新增 `backend: "docker" | "local"`，缺省 `docker`，未知值 fail-closed。
- `local` 后端：`run_sut_command` 直接 `subprocess.run(argv)`，不经 docker；`image`
  参数仍必须命中 `allowed_images` key（协议、审计、allowlist 语义不变；local 后端下
  条目的 `dockerfile` 字段可缺省）；`build_runtime_image` 返回显式错误，不做假 build。
- `docker` 后端：`build_docker_run_command` 把 `allowed_mount_roots` 逐一挂进容器；
  挂载目标按统一规则翻译——源 root 匹配 `^[A-Za-z]:[\\/]` 时目标为
  `/mnt/<盘符小写>/<其余路径正斜杠化>`（关 G5），Linux 风格 root 保持同路径映射；
  工作目录 `-w` 同规则翻译；`repo_root` + `/tmp` 挂载在 Linux 宿主上保持向后兼容，
  Windows 宿主上 `/tmp` 不挂载。
- CLI 入口维持 `python server.py CONFIG_PATH`（已存在，满足"通过 CLI 启动"）。

### 4.2 .NET BLL.Core

- `docker-mcp://` URI 新增两个可选 fail-closed 参数：
  - `localPython` —— Docker profile 下 parser/output-parser 命令改用它（缺省沿用 profile 的 `python`，与既有行为逐字节一致；显式设置才覆盖）；runner 仍用 profile 的 `python`（关 G1）；
  - `pathStyle=wsl` —— `DockerMcpProcessExecutor` 把 argv 中匹配
    `^[A-Za-z]:[\\/]` 的 token 翻译为 `/mnt/<盘符小写>/...`，反斜杠转正斜杠，
    其余 token 原样（关 G2）；`pathStyle` 出现但值非 `wsl` →
    `RuntimeEnvironmentResolutionException`。
- `DockerMcpRuntimeOptions` 扩展 `LocalPythonExecutable?` / `PathStyle`；
  不改 `ISystemMtLauncher` 签名，遵守 facade 类型泄漏规则。
- 用例 1 不需要这两个参数（同宿主同 python，URI 只含 image/python/endpoint/
  authTokenEnv）——参数可选性本身就是用例 1 的回归断言之一。

### 4.3 WPF（最小必改，关 G6）

按 CLAUDE.md §6 已文档化的 PR-1 T1 约定，把环境变量喂进 `RuntimePythons`：

```csharp
RuntimePythons: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
{
    ["system"] = Environment.GetEnvironmentVariable("METBENCH_SYSTEM_PYTHON") ?? "",
    ["openmc"] = Environment.GetEnvironmentVariable("METBENCH_OPENMC_PYTHON") ?? "",
}
```

空值由 `ResolvePythonExecutable` 的非空白检查自动跳过（`LauncherOptions.cs:99-109`），
环境变量未设时行为与现状逐字节一致。URI 进入后由
`LauncherOptionsRuntimeProfileProvider.cs:19-21` 建出 Docker profile。
注意：设 `METBENCH_SYSTEM_PYTHON` 会把该 WPF 会话内**所有** `system` key 的 MR 路由
经 MCP——验收用专用会话执行并在操作手册中明示。

## 5. 环境与部署资产

- **Windows host**：自带 python（用例 1 的 SUT 与所有用例的 parser 是纯 stdlib）；
  Docker Desktop（用例 2 构建并运行 `metbench-sut:latest`，已有
  `docker/Dockerfile`）。
- **WSL**：Ubuntu 24.04；OpenMC venv 复用 `docker/Dockerfile` 同款源码构建步骤
  （cmake + pip + binary symlink）。不下载截面数据库。
- **server 配置**：`config.local-win.json`（:8764）、`config.docker-win.json`
  （:8765）、`config.local-wsl.json`（:8766）三份示例入库；token 用占位符，实际
  token 不入库。
- **MetBench 侧环境变量**（每用例一组，操作手册给出逐字命令）：
  - 用例 1：`METBENCH_SYSTEM_PYTHON=docker-mcp://system?image=windows-local&tool=python&local=python&python=python&endpoint=http://<hostIP>:8764&authTokenEnv=METBENCH_DOCKER_MCP_TOKEN`
  - 用例 2：`METBENCH_OPENMC_PYTHON=docker-mcp://openmc?image=metbench-sut:latest&tool=openmc-runner&local=openmc-runner&python=/opt/openmc-venv/bin/python&endpoint=http://<hostIP>:8765&authTokenEnv=METBENCH_DOCKER_MCP_TOKEN&localPython=python&pathStyle=wsl`
  - 用例 3：`METBENCH_OPENMC_PYTHON=docker-mcp://openmc?image=wsl-openmc&tool=openmc-runner&local=openmc-runner&python=/home/<wsl_user>/openmc-venv/bin/python&endpoint=http://<wslIP>:8766&authTokenEnv=METBENCH_DOCKER_MCP_TOKEN&localPython=python&pathStyle=wsl`
  - （URI 内 `&`/`:` 等在实际设置时按 PowerShell 转义规则处理，手册给出可粘贴形式。）

## 6. 数据流（用例 2 一次 RunMr，差异点对照）

1. WPF 异步页提交 `RunMr(openmc-pincell-nu-sigma-f)` → 入队 → Worker →
   `ISystemMtLauncher.RunAsync`；
2. profile 解析（URI）→ `RuntimeKind.Docker` → preflight 调 `runtime_health` →
   `RuntimeEvidence` 记录连通性（验收判据 1）；
3. parser 本地（`localPython`=Windows python）跑：读 sample、生成 follow-up 输入，
   写 `C:\Users\...\Temp\<workroot>`；
4. runner 命令经 `pathStyle=wsl` 翻译后发 `run_sut_command` → server（Windows host）
   生成 `docker run`，把 `D:\`/`C:\` 挂载根翻译为 `/mnt/d`/`/mnt/c` 目标 → 容器内
   OpenMC 计算，输出写 `/mnt/c/...Temp/...`（即 Windows 临时目录本体）；
5. output parser 本地读输出 → MR 断言 → `MrRunResult` 持久化 → job `Succeeded`
   （验收判据 2）。

差异：用例 3 第 4 步 server 在 WSL 内直接 `subprocess(argv)`（`/mnt/*` 天然可见，
无挂载）；用例 1 第 3-5 步全部原生 Windows 路径与本地 python，无翻译。

## 7. 错误处理

沿用既有 fail-closed 模式：URI 参数非法 → `RuntimeEnvironmentResolutionException`；
server 不可达 / 401 / 未知 backend → preflight Blocked → job 终态 `Failed` +
RuntimeEvidence。基础设施失败不混入 MR 断言异常（与既有批量终态语义一致）。

## 8. 测试策略（TDD 三层 + WPF 手动）

| 层 | 内容 | 门控 |
|---|---|---|
| Python 单元 | backend 配置 fail-closed、local 后端执行（fake subprocess）、docker 挂载含 mount roots、Windows 源 root 挂载目标翻译、local+build 显式报错 | 无条件跑 |
| .NET 单元 | 新 URI 参数解析矩阵（合法/非法/缺省）、launcher 的 parser 命令用 localPython 而 runner 用 profile python（防 G1 回归 fact 断言）、路径翻译表驱动用例、无新参数时行为不变（用例 1 回归） | 无条件跑（CI） |
| .NET 验收 | 环境门控（仿 `OpenMocImportable` 模式，`METBENCH_MCP_ACCEPTANCE_URI` 未设则 skip）：真实 preflight 连通 + `RunAsync` 端到端 + async job 到 `Succeeded`；三用例各跑一遍，留 `.trx` | 本机验收时跑 |
| WPF 手动 | 启动 MetBench，按用例设环境变量：用例 1 提交 RunMr(p3)；用例 2/3 各提交 RunMr + RunBatch（3 个 openmc MR）；截图 job `Succeeded` 与结果页，写 vm-evidence 文档（照 `docs/superpowers/specs/2026-06-05-*-vm-evidence/` 先例） | 验收终局 |

## 9. 验收判据

1. **client→server 正常**：三用例的 preflight `RuntimeEvidence` 均记录 docker-mcp
   健康检查 pass；server 端有对应 run_id 记录。
2. **异步 MT 正常**：三用例的 async job 均到终态 `Succeeded`，`MrRunResult`
   持久化进 `SystemMT.Litedb`，WPF 页面截图为证。

## 10. 不交付（Out of scope）

- 不新增 `RuntimeKind`、不新增 `wsl-mcp://` scheme（local 后端对 client 透明）；
- 不改 WPF UI/XAML/导航（WPF 仅 §4.3 的 `App.xaml.cs` DI 字典改动）；
- 不下载截面数据库、不新增消费连续能量截面库的 MR；
- 不在 Windows 原生安装 OpenMC（conda 非官方路径，风险大于收益）；
- 不做双机拓扑、不要求 WSL mirrored networking；
- 不做 result/evidence 导入（信任模型未建立，沿用既有边界）。

## 11. Windows Classification

需要 Windows 证据：本验收的核心就是 Windows 主机上的 WPF 真实运行（WPF 手动层 +
环境门控验收测试在 Windows 执行）。新增生产代码位于 cloud-safe
`MetBench_BLL.Core`、Python `infra/` 与 `App.xaml.cs` 一处 DI 字典（Windows 本机
编译验证）。
