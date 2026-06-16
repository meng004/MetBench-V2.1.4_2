# MCP Server 三用例验收操作手册

关联设计：`docs/superpowers/specs/2026-06-12-mcp-three-case-acceptance-design.md`  
测试代码：`MetBench_SystemMT.Tests/SystemMT/Acceptance/McpThreeCaseAcceptanceTests.cs`  
证据目录：`docs/superpowers/specs/2026-06-12-mcp-three-case-acceptance-vm-evidence/`

---

## 用例矩阵

| 用例 | server 宿主 / 端口 | 执行后端 | 验收 MR |
|---|---|---|---|
| 1. local-python | Windows host :8764 | `backend=local`，直接 subprocess | `p3-trajectory-sensitivity`（key `system`） |
| 2. docker | Windows host :8765 | `backend=docker`，Docker Desktop | `openmc-pincell-nu-sigma-f`（key `openmc`） |
| 3. wsl-remote | WSL2 内 :8766 | `backend=local`，WSL venv | `openmc-pincell-nu-sigma-f`（key `openmc`） |

---

## 1. 前置条件

### 1.1 Windows 本机 Python（所有用例均需）

验收测试的 parser/output-parser 在 **Windows host** 本地执行，需要真实 Python 解释器。

**注意**：Windows Store 安装的 `python.exe` 是存根，在非交互调用（`subprocess`、`dotnet test`）下可能重定向至 Store 或直接返回错误。请确认 PATH 中的 `python` 是真实解释器：

```powershell
python --version          # 应显示 Python 3.10+ 版本号，而非跳转 Store
(Get-Command python).Source  # 应指向实际 .exe，而非 WindowsApps\... 存根
```

若 PATH 中没有真实 `python`，可在运行测试前通过 `METBENCH_TEST_PYTHON` 指定完整路径：

```powershell
$env:METBENCH_TEST_PYTHON = "C:\Python312\python.exe"   # 按本机实际路径填写
```

`TestAssetPaths.PythonExecutable()` 优先读 `METBENCH_TEST_PYTHON`；未设时 Windows 上按 `python` → `python3` → `py` 探测 PATH，全部缺失时保留旧默认 `python` 以暴露环境缺口。

### 1.2 Docker Desktop（用例 2 需要）

- Docker Desktop 已安装并运行（`docker info` 无报错）
- Linux 容器模式（默认）
- 仓库根所在磁盘（通常 `D:\`）和 Windows 临时目录所在磁盘（通常 `C:\`）已在 Docker Desktop > Settings > Resources > File Sharing 中授权

### 1.3 WSL2 Ubuntu 24.04（用例 3 需要）

- WSL2 已启用，默认发行版为 Ubuntu 24.04
- 仓库通过 drvfs 在 WSL 内可访问（`/mnt/d/Codes/MetBench-V2.1.4_2`）
- 用例 3 server 在 WSL 内启动，WSL NAT 模式下 Windows host 可经 WSL 私有 IP 访问

### 1.4 token 约定

每个 server config 内的 `auth_token` 与 PowerShell 环境变量 `METBENCH_DOCKER_MCP_TOKEN` 必须完全一致。实际 token 不入库——config 示例中的占位符 `change-me` 在本地副本中替换为自选强密码字符串。

```powershell
$env:METBENCH_DOCKER_MCP_TOKEN = "your-strong-token-here"   # 替换为真实 token
```

---

## 2. 环境构建

### 2.1 用例 2：构建 Docker 镜像 `metbench-sut:latest`

镜像包含 OpenMOC venv（`/opt/openmoc-venv`）和 OpenMC venv（`/opt/openmc-venv`），从源码构建，体积数 GB，**一次性操作**。

在 Windows host 仓库根执行（PowerShell 或 CMD）：

```powershell
# 从仓库根运行；构建上下文为 docker/ 目录
docker build -t metbench-sut:latest docker/
```

构建过程：编译 OpenMOC SWIG 扩展 + OpenMC C++ 二进制，最终 stage 复制两个 venv 到 Ubuntu 24.04 runtime 层，构建末尾打印：

```
runtime openmoc OK
runtime openmc OK  <version>
<openmc version string>
```

若需要含 .NET 8 SDK 的完整运行时镜像（容器内跑 `dotnet test`，本次验收不需要）：

```powershell
docker build -t metbench-runtime:latest --target sdk docker/
```

### 2.2 用例 3：在 WSL 内构建 OpenMC venv

在 WSL2 Ubuntu 24.04 终端内逐步执行，目标 venv 为 `~/openmc-venv`（对应 config 内 `python=/home/<user>/openmc-venv/bin/python`）。

**说明**：不下载截面数据库。MetBench 现有 OpenMC MR 运行在多群模式，`openmc_runner.py` 在运行时自生成 `mg_cross_sections.h5`，验收路径不消费外部截面数据。

```bash
# 步骤 1：安装构建依赖
sudo apt-get update
sudo apt-get install -y \
    build-essential cmake pkg-config git ca-certificates curl \
    libhdf5-dev libhdf5-serial-dev libgomp1 \
    libpng-dev libxml2-dev libeigen3-dev \
    python3.12 python3.12-dev python3.12-venv \
    python3-numpy python3-h5py python3-matplotlib python3-pandas python3-scipy

# 步骤 2：创建 venv（system-site-packages 复用 apt 科学库）
python3.12 -m venv --system-site-packages ~/openmc-venv
~/openmc-venv/bin/pip install --no-cache-dir --upgrade pip setuptools wheel

# 步骤 3：克隆并编译 OpenMC（从源码；不含截面数据库）
git clone --depth=1 --branch master --recurse-submodules \
    https://github.com/openmc-dev/openmc.git /tmp/openmc-src
mkdir -p /tmp/openmc-src/build
cd /tmp/openmc-src/build
cmake -DCMAKE_INSTALL_PREFIX=$HOME/openmc-install \
      -DCMAKE_BUILD_TYPE=Release \
      -DOPENMC_USE_MPI=OFF \
      -DOPENMC_USE_OPENMP=ON \
      -DOPENMC_USE_DAGMC=OFF \
      -DOPENMC_USE_LIBMESH=OFF \
      ..
make -j$(nproc)
make install

# 步骤 4：安装 Python 绑定并将 openmc 可执行文件链接到 venv/bin
cd /tmp/openmc-src
~/openmc-venv/bin/pip install --no-cache-dir --no-build-isolation .
ln -sf $HOME/openmc-install/bin/openmc ~/openmc-venv/bin/openmc

# 步骤 5：验证
~/openmc-venv/bin/python -c "import openmc; print('openmc OK', openmc.__version__)"
~/openmc-venv/bin/openmc --version

# 清理源码（可选）
rm -rf /tmp/openmc-src
```

两条验证命令均应返回版本号，无 import 错误。

---

## 3. Server 启动（CLI × 3）

### 3.1 准备 config 文件

示例文件位于 `infra/mcp/docker-runtime/`，去掉 `.example` 后缀后在本地编辑——**不要修改入库的示例文件**。

```powershell
# Windows host（用例 1 + 2）
Copy-Item infra\mcp\docker-runtime\config.local-win.example.json `
          infra\mcp\docker-runtime\config.local-win.json
Copy-Item infra\mcp\docker-runtime\config.docker-win.example.json `
          infra\mcp\docker-runtime\config.docker-win.json
```

```bash
# WSL 内（用例 3）；路径映射到 /mnt/d/...
cp /mnt/d/Codes/MetBench-V2.1.4_2/infra/mcp/docker-runtime/config.local-wsl.example.json \
   /mnt/d/Codes/MetBench-V2.1.4_2/infra/mcp/docker-runtime/config.local-wsl.json
```

在每份 `*.json` 中替换以下两处（其余字段已是合理默认值）：

- `"repo_root"` → 本机实际仓库根路径
  - 用例 1/2（Windows）：如 `"D:\\Codes\\MetBench-V2.1.4_2"`
  - 用例 3（WSL）：如 `"/mnt/d/Codes/MetBench-V2.1.4_2"`
- `"auth_token"` → 与 `METBENCH_DOCKER_MCP_TOKEN` 完全一致的真实 token

用例 2 的 `config.docker-win.json` 中还需确认 `allowed_mount_roots` 含 Windows 临时目录盘符根，示例已含：

```json
"allowed_mount_roots": [
  "D:\\Codes\\MetBench-V2.1.4_2",
  "C:\\Users\\lemon\\AppData\\Local\\Temp"
]
```

将 `lemon` 替换为本机用户名（或直接改为 `C:\\Users\\<你的用户名>\\AppData\\Local\\Temp`）。

### 3.2 启动三个 server

**用例 1**（Windows host PowerShell，端口 8764）：

```powershell
python infra\mcp\docker-runtime\server.py infra\mcp\docker-runtime\config.local-win.json
```

**用例 2**（Windows host PowerShell，端口 8765）：

```powershell
python infra\mcp\docker-runtime\server.py infra\mcp\docker-runtime\config.docker-win.json
```

**用例 3**（WSL2 bash 终端，端口 8766）：

```bash
cd /mnt/d/Codes/MetBench-V2.1.4_2
python3 infra/mcp/docker-runtime/server.py infra/mcp/docker-runtime/config.local-wsl.json
```

每个 server 启动时会打印绑定地址，例如：

```
docker-runtime MCP server (local backend) listening on http://192.168.1.42:8764
```

- 用例 1/2：`bind_host=auto-private-ipv4` 解析为 Windows host 的私有 IPv4（局域网 IP），记为 `<hostIP>`；可用 `ipconfig` 交叉确认
- 用例 3：解析为 WSL 私有 IPv4，可另行确认：

```bash
wsl hostname -I        # 第一个 IP 即 WSL 私有 IP，记为 <wslIP>
```

### 3.3 确认 server 可达（每用例）

以用例 1 为例（替换 IP 和端口以验证其余用例）：

```powershell
$token = $env:METBENCH_DOCKER_MCP_TOKEN
Invoke-RestMethod -Uri "http://<hostIP>:8764/tool" `
    -Method Post `
    -Headers @{ Authorization = "Bearer $token"; "Content-Type" = "application/json" } `
    -Body '{"tool":"runtime_health","arguments":{}}'
```

预期返回：

```json
{"status": "ok", "bind_host": "192.168.1.42", "bind_port": 8764, "repo_root": "..."}
```

将实际返回截图或粘贴记入证据（`bind_host` 即 `<hostIP>`，`repo_root` 应与 config 一致）。

---

## 4. 自动化验收（每用例一组 env + dotnet test）

在 Windows PowerShell（仓库根）运行。先设置 `METBENCH_DOCKER_MCP_TOKEN`（见§1.4），再设置每用例专属的三个环境变量，最后运行 `dotnet test`。

**测试类过滤**：`McpThreeCaseAcceptanceTests`（共 3 个 SkippableFact，均需 pass）

**通过判据**：`3 passed / 0 failed / 0 skipped`；`.trx` 文件归档至证据目录。

---

### 4.1 用例 1：local-python backend（端口 8764，key=system，MR=p3）

将 `<hostIP>` 替换为§3.2 server 启动时打印的真实 IP，将 `<python_full_path>` 替换为本机 Python 完整路径（如 `C:\Python312\python.exe`）。

```powershell
# 构造 URI（&、: 等在 PowerShell 字符串内无需额外转义，但赋给 env var 需用引号包裹整个字符串）
$pythonEncoded = [Uri]::EscapeDataString("C:\Python312\python.exe")  # 按实际路径替换
$env:METBENCH_MCP_ACCEPTANCE_URI = `
    "docker-mcp://system?image=windows-local" + `
    "&python=$pythonEncoded" + `
    "&endpoint=http://<hostIP>:8764" + `
    "&authTokenEnv=METBENCH_DOCKER_MCP_TOKEN"
$env:METBENCH_MCP_ACCEPTANCE_RUNTIME_KEY = "system"
$env:METBENCH_MCP_ACCEPTANCE_MR          = "p3-trajectory-sensitivity"

dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --no-restore `
    --filter "FullyQualifiedName~McpThreeCaseAcceptanceTests" `
    --logger "trx;LogFileName=mcp-acceptance-case1.trx"
```

**说明**：用例 1 server 与 client 同宿主、同 python，URI 中**不需要** `localPython` 或 `pathStyle` 参数——参数可选性本身即为此用例的回归断言。

---

### 4.2 用例 2：docker backend（端口 8765，key=openmc，MR=openmc-pincell-nu-sigma-f）

将 `<hostIP>` 替换为真实 IP，`<windows_python_full_path>` 替换为本机 Python 完整路径（用于 `localPython`，即 parser 在 Windows host 本地执行所用解释器）。

```powershell
$localPythonEncoded = [Uri]::EscapeDataString("C:\Python312\python.exe")  # 按实际路径替换
$env:METBENCH_MCP_ACCEPTANCE_URI = `
    "docker-mcp://openmc?image=metbench-sut:latest" + `
    "&python=/opt/openmc-venv/bin/python" + `
    "&endpoint=http://<hostIP>:8765" + `
    "&authTokenEnv=METBENCH_DOCKER_MCP_TOKEN" + `
    "&localPython=$localPythonEncoded" + `
    "&pathStyle=wsl"
$env:METBENCH_MCP_ACCEPTANCE_RUNTIME_KEY = "openmc"
$env:METBENCH_MCP_ACCEPTANCE_MR          = "openmc-pincell-nu-sigma-f"

dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --no-restore `
    --filter "FullyQualifiedName~McpThreeCaseAcceptanceTests" `
    --logger "trx;LogFileName=mcp-acceptance-case2.trx"
```

**说明**：
- `python=/opt/openmc-venv/bin/python`：容器内 OpenMC venv python，SUT runner 命令由此执行。
- `localPython`：Windows host 上的 Python 全路径，parser/output-parser 在本地执行时使用（关闭缺口 G1）。
- `pathStyle=wsl`：launcher 将 argv 中匹配 `^[A-Za-z]:[\\/]` 的 token 翻译为 `/mnt/<盘符小写>/...`，容器内可解析（关闭缺口 G2）。
- server 端 `allowed_mount_roots` 须含仓库根和 Windows 临时目录（见§3.1）；`docker run` 挂载目标按 `X:\path` → `/mnt/x/path` 规则翻译（关闭缺口 G3/G5）。

---

### 4.3 用例 3：WSL local backend（端口 8766，key=openmc，MR=openmc-pincell-nu-sigma-f）

将 `<wslIP>` 替换为§3.2 WSL server 启动时打印的真实 IP（或 `wsl hostname -I` 第一项），将 `<wsl_user>` 替换为 WSL 用户名，`<windows_python_full_path>` 替换为本机 Python 完整路径。

```powershell
$localPythonEncoded = [Uri]::EscapeDataString("C:\Python312\python.exe")  # 按实际路径替换
$env:METBENCH_MCP_ACCEPTANCE_URI = `
    "docker-mcp://openmc?image=wsl-openmc" + `
    "&python=/home/<wsl_user>/openmc-venv/bin/python" + `
    "&endpoint=http://<wslIP>:8766" + `
    "&authTokenEnv=METBENCH_DOCKER_MCP_TOKEN" + `
    "&localPython=$localPythonEncoded" + `
    "&pathStyle=wsl"
$env:METBENCH_MCP_ACCEPTANCE_RUNTIME_KEY = "openmc"
$env:METBENCH_MCP_ACCEPTANCE_MR          = "openmc-pincell-nu-sigma-f"

dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --no-restore `
    --filter "FullyQualifiedName~McpThreeCaseAcceptanceTests" `
    --logger "trx;LogFileName=mcp-acceptance-case3.trx"
```

**说明**：
- `image=wsl-openmc`：config.local-wsl.json 中 `allowed_images` 的 key（local 后端下 `dockerfile` 字段可缺省）。
- WSL `backend=local` 下 server 直接 `subprocess(argv)`，`/mnt/c`/`/mnt/d` 经 drvfs 天然可见，无额外挂载。
- Windows NAT 模式下 `<wslIP>` 通常为 `172.x.x.x`；从 Windows host 访问该 IP 的 8766 端口默认可达，若防火墙拦截参见§7。

---

### 4.4 trx 归档

测试通过后将三份 trx 复制到证据目录：

```powershell
$evDir = "docs\superpowers\specs\2026-06-12-mcp-three-case-acceptance-vm-evidence"
New-Item -ItemType Directory -Force $evDir
Copy-Item TestResults\mcp-acceptance-case1.trx $evDir\
Copy-Item TestResults\mcp-acceptance-case2.trx $evDir\
Copy-Item TestResults\mcp-acceptance-case3.trx $evDir\
```

---

## 5. WPF 手动验收

### 5.1 配置 appsettings.local.json 并启动 WPF

WPF 启动时加载 MetBench_Client 输出目录旁的 `appsettings.local.json`（可选文件），并把
`LauncherOptions:RuntimePythons` 配置节绑定进 `LauncherOptions.RuntimePythons` 字典
（`App.xaml.cs`，#361 引入；原 `METBENCH_SYSTEM_PYTHON` / `METBENCH_OPENMC_PYTHON`
环境变量接线已撤除）。**修改 appsettings.local.json 后需重启 WPF** 才能生效。

也可以不手写 JSON，直接在 WPF 新增的 **SystemMtRuntimeEnvironmentPage 运行时环境页面**
里填 runtime key / endpoint / image / python / auth token env 并保存——保存即写入同一份
`appsettings.local.json`（同样需重启 WPF 生效）。

**注意**：配置 `system` key 后，当前 WPF 会话内**所有** `system` key 的 MR 均经 MCP
路由——请使用专用验收会话，不要与日常使用会话混用。

URI 可用 #361 新增的 `profile-uri` 子命令生成：

```powershell
python infra/mcp/docker-runtime/server.py profile-uri `
    --runtime-key system `
    --endpoint http://<hostIP>:8764 `
    --image windows-local `
    --python C:\Python312\python.exe `
    --auth-token-env METBENCH_DOCKER_MCP_TOKEN
```

**注意**：`profile-uri` 命令不支持 `localPython` / `pathStyle` 两个参数。用例 2/3 需在
生成的 URI 末尾手动追加 `&localPython=<URL编码的Windows python完整路径>&pathStyle=wsl`，
或直接手写完整 URI（运行时环境页面同样不含这两个字段，保存后需手动编辑 JSON 补全）。

**用例 1**：在 MetBench_Client 输出目录（`MetBench_Client\bin\Debug\net8.0-windows7.0\`
或实际输出目录）写 `appsettings.local.json`，`python=` 取 URL 编码后的本机 Python 完整路径：

```json
{
  "LauncherOptions": {
    "RuntimePythons": {
      "system": "docker-mcp://system?image=windows-local&python=C%3A%5CPython312%5Cpython.exe&endpoint=http%3A%2F%2F<hostIP>%3A8764&authTokenEnv=METBENCH_DOCKER_MCP_TOKEN"
    }
  }
}
```

**用例 2**（`openmc` key；末尾手动追加 `localPython` / `pathStyle`）：

```json
{
  "LauncherOptions": {
    "RuntimePythons": {
      "openmc": "docker-mcp://openmc?image=metbench-sut%3Alatest&python=%2Fopt%2Fopenmc-venv%2Fbin%2Fpython&endpoint=http%3A%2F%2F<hostIP>%3A8765&authTokenEnv=METBENCH_DOCKER_MCP_TOKEN&localPython=C%3A%5CPython312%5Cpython.exe&pathStyle=wsl"
    }
  }
}
```

**用例 3**（`openmc` key，WSL endpoint 与 WSL venv python）：

```json
{
  "LauncherOptions": {
    "RuntimePythons": {
      "openmc": "docker-mcp://openmc?image=wsl-openmc&python=%2Fhome%2F<wsl_user>%2Fopenmc-venv%2Fbin%2Fpython&endpoint=http%3A%2F%2F<wslIP>%3A8766&authTokenEnv=METBENCH_DOCKER_MCP_TOKEN&localPython=C%3A%5CPython312%5Cpython.exe&pathStyle=wsl"
    }
  }
}
```

写好配置后，在同一 PowerShell 会话设置 token 环境变量并启动 WPF：

```powershell
$env:METBENCH_DOCKER_MCP_TOKEN = "your-strong-token-here"

dotnet run --project MetBench_Client
```

### 5.2 WPF 操作步骤

进入 **SystemMT 异步作业页**：

**用例 1**：
1. 提交 RunMr，选择 MR = `p3-trajectory-sensitivity`
2. 等待作业终态 `Succeeded`

**用例 2 / 用例 3**：
1. 提交 RunMr，选择 MR = `openmc-pincell-nu-sigma-f`
2. 提交 RunBatch，选择 3 个 `openmc-pincell-*` MR
3. 等待所有作业终态 `Succeeded`

### 5.3 截图清单

每用例截图并命名存入证据目录 `docs/superpowers/specs/2026-06-12-mcp-three-case-acceptance-vm-evidence/`：

| 文件名 | 内容 |
|---|---|
| `case1-job-succeeded.png` | 用例 1 RunMr 作业终态 `Succeeded` |
| `case1-result-page.png` | 用例 1 结果详情页（含 MrRunResult 显示） |
| `case1-runtime-evidence.png` | 用例 1 RuntimeEvidence（preflight health pass 记录） |
| `case2-job-succeeded.png` | 用例 2 RunMr 作业终态 `Succeeded` |
| `case2-batch-succeeded.png` | 用例 2 RunBatch 3 个 MR 全部 `Succeeded` |
| `case2-runtime-evidence.png` | 用例 2 RuntimeEvidence |
| `case3-job-succeeded.png` | 用例 3 RunMr 作业终态 `Succeeded` |
| `case3-batch-succeeded.png` | 用例 3 RunBatch 3 个 MR 全部 `Succeeded` |
| `case3-runtime-evidence.png` | 用例 3 RuntimeEvidence |

参照先例 `docs/superpowers/specs/2026-06-05-t0-t2-async-import-export-vm-evidence/vm-summary.md`，在证据目录下同步创建 `vm-summary.md`，记录 branch、验收提交 hash、测试通过输出摘要、截图清单对应说明。

---

## 6. 判据对照表

基于 spec §9 两条判据：

| 判据 | 描述 | 用例 1 | 用例 2 | 用例 3 |
|---|---|---|---|---|
| **P1** client→server 正常 | preflight `RuntimeEvidence` 记录 docker-mcp 健康检查 pass；run_id 证据来自 server 控制台的 `run_sut_command run_id=... status=...` 日志行（每次 MR 运行产生 2 行：source + follow-up），操作员在验收测试/WPF 提交后截取这些行归档 | `[ ]` | `[ ]` | `[ ]` |
| **P2** 异步 MT 正常 | async job 到终态 `Succeeded`，`MrRunResult` 持久化进 `SystemMT.Litedb`，WPF 页面截图为证 | `[ ]` | `[ ]` | `[ ]` |

自动化验收（§4）覆盖 P1 + P2（测试 `Acceptance_1_*` 验 P1，`Acceptance_2_*` / `Acceptance_3_*` 验 P2）；WPF 手动验收（§5）提供 P2 的 UI 截图证据。

全部 6 个 checkbox 打勾后，在 `docs/superpowers/specs/2026-06-12-mcp-three-case-acceptance-vm-evidence/vm-summary.md` 中标记验收完成，并在 `docs/status/current.md` 中将此 chain 标记为 Controlled。

---

## 7. 故障排查

### 7.1 HTTP 401

```
health.Available = false, Status = "error", Detail = "401 Unauthorized"
```

**原因**：`METBENCH_DOCKER_MCP_TOKEN` 与 config 中 `auth_token` 不一致。  
**处置**：逐字对比两处 token（注意末尾空格、换行），重新设置环境变量后重跑。

### 7.2 连不上 server（Connection refused / timeout）

**用例 1/2（Windows host）**：
- 确认 server 已在目标端口启动（`netstat -an | findstr "8764\|8765"`）
- Windows Defender 防火墙可能阻拦入站。临时放行：

```powershell
New-NetFirewallRule -DisplayName "MetBench MCP 8764" -Direction Inbound `
    -Protocol TCP -LocalPort 8764 -Action Allow
New-NetFirewallRule -DisplayName "MetBench MCP 8765" -Direction Inbound `
    -Protocol TCP -LocalPort 8765 -Action Allow
```

- 确认 URI 中 `<hostIP>` 使用 server 启动时打印的实际 IP，而非 `127.0.0.1`（loopback 在跨进程场景下可能不通）

**用例 3（WSL）**：
- WSL NAT 模式下 Windows 访问 WSL IP 通常无需额外配置；若不通，在 WSL 内确认 `wsl hostname -I` 返回的 IP 与 URI 一致
- 若使用 WSL mirrored 网络模式，WSL IP 可能与 NAT 模式不同；本验收假设默认 NAT 模式
- 确认 WSL 内 server 进程仍在运行（`ps aux | grep server.py`）

### 7.3 `RuntimeEnvironmentResolutionException`

错误信息通常包含 "missing key" 或 "pathStyle" 字样。

**原因 A**：URI 参数缺失或拼写错误（`authTokenEnv`、`image`、`python`、`endpoint` 任一缺失）。  
**原因 B**：`pathStyle` 出现但值不是 `wsl`（目前只支持 `wsl`，其他值 fail-closed）。  
**处置**：检查 `METBENCH_MCP_ACCEPTANCE_URI` 内容，确认所有必填参数存在且拼写正确。

### 7.4 Parser 失败（`FileNotFoundException` / Python 调用出错）

**原因**：`localPython` 未设置（用例 2/3），或设置的路径不是真实 Python 解释器（Store 存根），导致 parser 在 Windows 本地执行失败。  
**处置**：
- 用例 1 不需要 `localPython`（同宿主）
- 用例 2/3 的 URI 中必须包含 `&localPython=<URL编码的Windows python完整路径>`
- 验证：`& "C:\Python312\python.exe" --version` 应打印版本号而非跳转 Store

### 7.5 Docker mount 失败（容器内找不到输入/输出文件）

```
FileNotFoundError: /mnt/c/Users/.../Temp/.../input.json
```

**原因 A**：`config.docker-win.json` 的 `allowed_mount_roots` 未包含 Windows 临时目录所在路径根（通常 `C:\Users\<user>\AppData\Local\Temp`）。  
**处置**：在 `allowed_mount_roots` 中添加完整临时目录路径，重启 server。

**原因 B**：`pathStyle=wsl` 未设置，argv 中的 Windows 路径未翻译，容器内无法解析。  
**处置**：确认 URI 包含 `&pathStyle=wsl`。

**原因 C**：Docker Desktop File Sharing 未授权目标磁盘。  
**处置**：Docker Desktop > Settings > Resources > File Sharing，添加 `C:\` 和 `D:\`（或仓库/临时目录所在盘）。
