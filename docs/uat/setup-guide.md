# UAT 环境准备手册

> 按照本文档从零搭好 UAT 环境。首次执行约 30 分钟（含 OpenMOC 编译）。
> 所有命令均假定你已 `cd` 到仓库根 `MetBench-V2.1.4_2/`。

---

## 1. Linux (Ubuntu 24.04) 端 — 后端 / 服务 / 论文核心

### 1.1 基础工具

```bash
sudo apt-get update
sudo apt-get install -y git curl build-essential
```

### 1.2 .NET 8 SDK

```bash
# 使用 Microsoft 官方 apt 源
wget https://packages.microsoft.com/config/ubuntu/24.04/packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt-get update && sudo apt-get install -y dotnet-sdk-8.0
dotnet --version          # 期望: 8.0.x
```

### 1.3 Clone 仓库

```bash
git clone https://github.com/meng004/MetBench-V2.1.4_2.git
cd MetBench-V2.1.4_2
git checkout main
git pull
```

### 1.4 验证 cross-platform 编译

```bash
dotnet build MetBench_BLL.Core/MetBench_BLL.Core.csproj
dotnet build MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj
# 期望: 0 Error(s)
```

### 1.5 OpenMOC venv（用 setup 脚本，**一键自动**）

```bash
bash .claude/web-setup.sh
# 首次约 10-15 min。完成后输出: "[setup] OpenMOC import OK"
# 安装到 /opt/openmoc-venv，python 路径 /opt/openmoc-venv/bin/python
```

确认环境变量：

```bash
echo "export METBENCH_OPENMOC_PYTHON=/opt/openmoc-venv/bin/python" >> ~/.bashrc
source ~/.bashrc
```

### 1.6 (可选) Heat-Equation & Projectile

无需额外依赖 —— 这两个 SUT 用 Python 3 标准库 + numpy / matplotlib（已随 OpenMOC venv 装好）。

---

## 2. Windows 11 端 — WPF UI

### 2.1 前置

| 工具 | 版本 | 安装 |
|------|------|------|
| Visual Studio 2022 | 17.10+ | 工作负载勾选 ".NET 桌面开发" |
| .NET 8 SDK | 8.0.x | VS 自带，或独立 `winget install Microsoft.DotNet.SDK.8` |
| Git for Windows | 2.40+ | `winget install Git.Git` |
| OpenMOC | 可选 | 不装则 OpenMOC 用例自动 skip（标 [SKIP]） |

### 2.2 clone + build

```powershell
cd C:\src
git clone https://github.com/meng004/MetBench-V2.1.4_2.git
cd MetBench-V2.1.4_2
dotnet build MetBench.sln
# 期望: 0 Error(s)（Warning 可忽略）
```

### 2.3 启动 WPF UI

```powershell
dotnet run --project MetBench_Client
# 主窗口弹出，左侧 NavigationView 显示 8-10 个页面入口
```

---

## 3. LLM API 配置（验证 F12 Multi-LLM 必需）

UAT 用 LLM provider 兼容 OpenAI Anthropic-Compatible API。配置文件 `.env` **必须 gitignored**，
切勿提交到仓库。

### 3.1 创建 `.env`

在仓库根目录新建 `.env`（已在 `.gitignore`）：

```bash
# === Provider 1: DeepSeek (anthropic-compat) ===
ANTHROPIC_BASE_URL=https://api.deepseek.com/anthropic
ANTHROPIC_API_KEY=sk-YOUR-DEEPSEEK-KEY
ANTHROPIC_MODEL=deepseek-chat

# === Provider 2 (可选 —— 走 multi-LLM consensus 需要 ≥ 2 家) ===
DEEPSEEK_API_KEY=sk-YOUR-DEEPSEEK-KEY
OPENAI_API_KEY=sk-YOUR-OPENAI-KEY
ANTHROPIC_NATIVE_KEY=sk-ant-YOUR-CLAUDE-KEY
```

### 3.2 验证 key 可用

```bash
# DeepSeek
curl -s https://api.deepseek.com/anthropic/v1/messages \
  -H "x-api-key: $ANTHROPIC_API_KEY" \
  -H "anthropic-version: 2023-06-01" \
  -H "content-type: application/json" \
  -d '{"model":"deepseek-chat","max_tokens":32,"messages":[{"role":"user","content":"reply ok"}]}'
# 期望返回 JSON 含 "content":[{"type":"text","text":"ok"...
```

### 3.3 LLM provider 列表样例

见 [sample-data/uat-llm-providers.example.json](sample-data/uat-llm-providers.example.json)。

> 仅做功能验收：单家 LLM (DeepSeek 一家) 已足够跑通 F12 一半用例。
> 若要验 consensus / Cohen's κ，至少 ≥ 2 家。

---

## 4. SUT 配置

仓库内 `SUT/` 目录已包含 3 个 SUT 的 adapter + sample 输入：

| SUT | 位置 | 启动 python | 依赖 |
|-----|------|-------------|------|
| OpenMOC | `SUT/openmoc/` | `$METBENCH_OPENMOC_PYTHON` (`/opt/openmoc-venv/bin/python`) | OpenMOC 已 import OK |
| Heat-Equation | `SUT/heat_equation/` | `python3` | Python 3 stdlib + numpy（venv 已含） |
| Projectile | `SUT/projectile/` | `python3` | Python 3 stdlib |

**自检**（Linux）：

```bash
$METBENCH_OPENMOC_PYTHON -c "import openmoc; print(openmoc.__file__)"
# 期望: /opt/openmoc-venv/lib/python3.12/site-packages/openmoc/openmoc.py

python3 SUT/heat_equation/heat_equation.py SUT/heat_equation/sample/gaussian.json /tmp/he.out
cat /tmp/he.out | head -3   # 期望 JSON 输出含 "temperature":[...]

python3 SUT/projectile/projectile.py 45 100 9.81
# 期望: range_meters: ~1019.4
```

---

## 5. 数据库初始化

LiteDB 是文件 DB，**首次运行时自动创建**。无需手工建库。

| 文件 | 用途 | 默认位置 |
|------|------|----------|
| `MR.Litedb` | v1 + v2 主库（24 collections） | 二进制运行目录 |
| `SystemMT.Litedb` | system-MT 结果库 | 二进制运行目录 |

### 5.1 自定义路径（验收时推荐）

```bash
# Linux
export METBENCH_DB_PATH=/tmp/uat-mr.db
# Windows PowerShell
$env:METBENCH_DB_PATH = "C:\Temp\uat-mr.db"
```

### 5.2 验证 DB 可读

```bash
dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~DbConfigTests"
# 期望: Passed > 0, Failed = 0
```

---

## 6. 故障排查

| 症状 | 原因 | 处置 |
|------|------|------|
| `dotnet build` 报 MSB4019 (Linux) | 想编 MetBench_Client（WPF） | Linux 不能编 WPF —— 这是预期。Linux 只跑 `*.Core` / `*.DAL` / `*.Tests` |
| OpenMOC 测试 [SKIP] | venv 不在或 `METBENCH_OPENMOC_PYTHON` 未导出 | 重跑 1.5 / 1.6 |
| F12 测试报 401 | LLM key 失效 / 余额不足 | 重新核对 `.env` |
| Test runner 卡住 > 5min | 卡 OpenMOC 进程未退出 | `pkill -f openmoc; pkill -f python3` 后重跑 |
| LiteDB lock 报错 | 上次进程未释放 | `rm /tmp/uat-mr.db*` 或换文件名 |

完成后切换到 [test-procedures.md](test-procedures.md) 开始用例执行。
