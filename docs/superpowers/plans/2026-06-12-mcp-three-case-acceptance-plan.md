# MCP Server 三用例验收测试 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**状态**: 完成（Task 1-12 全部交付；三用例现场验收 2026-06-13 执行通过，证据见 vm-evidence/vm-summary.md）
**Spec**: `docs/superpowers/specs/2026-06-12-mcp-three-case-acceptance-design.md`
**分支**: `mcp-dual-backend-acceptance`（已存在，spec 已提交）

**Goal:** 关闭 G1-G6 六个集成缺口，使 Windows 上的 MetBench 能经 LAN 访问三种形态的 MCP server（Windows local-python / Windows docker / WSL 模拟远程）完成异步 MT，并交付环境门控验收测试 + 验收操作手册。

**Architecture:** server.py 增加 `backend: docker|local` 双后端与挂载目标翻译；.NET 侧 `docker-mcp://` URI 增加可选 `localPython`（parser 本地 python）与 `pathStyle=wsl`（argv 路径翻译）参数；WPF `App.xaml.cs` 把环境变量喂进 `RuntimePythons`。验收层为环境门控 xUnit 测试 + WPF 手动 runbook。

**Tech Stack:** Python 3 stdlib（unittest）、.NET 8 xUnit（含 SkippableFact）、WPF（仅 DI 一处）。

**执行约定（每个 Task 都适用）：**
- TDD：先写测试 → 跑出失败 → 最小实现 → 跑过 → 提交。
- Python 测试命令（仓库根目录）：`python -m unittest discover -s infra/mcp/docker-runtime/tests -v`
- .NET 测试命令模板：`dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --no-restore --filter "<FILTER>"`
- 提交信息末尾加：`Co-Authored-By: Claude <noreply@anthropic.com>`（仓库作者已配置为 meng004）。
- **§0.5 最小修改**：只改各 Task 列出的文件与位置，不顺手重构。

---

## File Structure（全量改动地图）

| 文件 | 动作 | 职责 |
|---|---|---|
| `infra/mcp/docker-runtime/server.py` | Modify | backend 配置、路径翻译、挂载生成、local 执行 |
| `infra/mcp/docker-runtime/tests/test_server.py` | Modify | 上述全部的 TDD 测试 |
| `infra/mcp/docker-runtime/config.local-win.example.json` | Create | 用例 1 配置示例 |
| `infra/mcp/docker-runtime/config.docker-win.example.json` | Create | 用例 2 配置示例 |
| `infra/mcp/docker-runtime/config.local-wsl.example.json` | Create | 用例 3 配置示例 |
| `infra/mcp/docker-runtime/README.md` | Modify | backend 说明 + 三用例启动命令 |
| `MetBench_BLL.Core/SystemMT/Runtime/RuntimeModels.cs` | Modify | `DockerMcpRuntimeOptions` 加 2 字段 + `DockerMcpPathStyle` enum |
| `MetBench_BLL.Core/SystemMT/Runtime/LauncherOptionsRuntimeProfileProvider.cs` | Modify | 解析 `localPython` / `pathStyle` |
| `MetBench_BLL.Core/SystemMT/Runtime/DockerMcpProcessExecutor.cs` | Modify | argv WSL 路径翻译 |
| `MetBench_BLL.Core/SystemMT/Launcher/SystemMtLauncher.cs` | Modify | parser 命令用 LocalPythonExecutable |
| `MetBench_SystemMT.Tests/SystemMT/Runtime/DockerMcpRuntimeProfileTests.cs` | Modify | URI 新参数解析矩阵 |
| `MetBench_SystemMT.Tests/SystemMT/Runtime/DockerMcpProcessExecutorTests.cs` | Modify | 路径翻译表驱动 + RunAsync 翻译 |
| `MetBench_SystemMT.Tests/SystemMT/Launcher/LauncherDockerMcpLocalParserTests.cs` | Create | G1 fact：parser 本地跑、仅 runner 走 MCP（loopback 假 server，CI 可跑） |
| `MetBench_SystemMT.Tests/SystemMT/Jobs/WpfRuntimePythonsWiringTests.cs` | Create | G6 守护：App.xaml.cs 接线存在性 |
| `MetBench_Client/App.xaml.cs` | Modify | `RuntimePythons` 环境变量接线（G6） |
| `MetBench_SystemMT.Tests/SystemMT/Acceptance/McpThreeCaseAcceptanceTests.cs` | Create | 环境门控验收测试（3 条） |
| `docs/uat/mcp-three-case-acceptance-runbook.md` | Create | 验收操作手册 |
| `docs/superpowers/specs/2026-06-12-mcp-three-case-acceptance-design.md` | Modify | §4.2 localPython 缺省语义 retro-touch（见 Task 8） |
| `docs/status/current.md`、`docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md` | Modify | 状态投影 |

---

## Phase A — Python MCP server 双后端

### Task 1: config `backend` 字段（fail-closed）

**Files:**
- Modify: `infra/mcp/docker-runtime/server.py`
- Test: `infra/mcp/docker-runtime/tests/test_server.py`

- [ ] **Step 1: 写失败测试** — 在 `DockerRuntimeServerTests` 类内追加：

```python
    def test_load_config_defaults_backend_to_docker(self):
        config = self.write_config_and_load(self.valid_config_payload())

        self.assertEqual("docker", config.backend)

    def test_load_config_accepts_local_backend(self):
        payload = self.valid_config_payload()
        payload["backend"] = "local"

        config = self.write_config_and_load(payload)

        self.assertEqual("local", config.backend)

    def test_load_config_rejects_unknown_backend(self):
        payload = self.valid_config_payload()
        payload["backend"] = "kubernetes"

        with self.assertRaisesRegex(ValueError, "backend"):
            self.write_config_and_load(payload)

    def test_load_config_local_backend_allows_image_without_dockerfile(self):
        payload = self.valid_config_payload()
        payload["backend"] = "local"
        payload["allowed_images"] = {"wsl-openmc": {}}

        config = self.write_config_and_load(payload)

        self.assertEqual("", config.allowed_images["wsl-openmc"].dockerfile)
        self.assertEqual("", config.allowed_images["wsl-openmc"].context)

    def test_load_config_docker_backend_still_requires_dockerfile(self):
        payload = self.valid_config_payload()
        payload["allowed_images"] = {"img": {}}

        with self.assertRaisesRegex(ValueError, "dockerfile"):
            self.write_config_and_load(payload)
```

同时把既有 `test_runtime_config_and_image_config_dataclasses_exist` 中 RuntimeConfig 字段名断言列表末尾追加 `"backend"`（dataclass 带默认值字段必须排最后）。

- [ ] **Step 2: 跑测试确认失败**

Run: `python -m unittest discover -s infra/mcp/docker-runtime/tests -v`
Expected: 新增 5 个测试 FAIL/ERROR（`backend` 属性不存在 / dockerfile 必填报错）。

- [ ] **Step 3: 最小实现** — `server.py`：

`RuntimeConfig` dataclass 末尾追加字段：

```python
@dataclass
class RuntimeConfig:
    bind_host: str
    bind_port: int
    auth_token: str
    repo_root: str
    allowed_images: dict[str, ImageConfig]
    allowed_mount_roots: list[str]
    default_timeout_seconds: int
    max_output_bytes: int
    backend: str = "docker"
```

新增常量与加载函数（放在 `_load_allowed_images` 之前）：

```python
VALID_BACKENDS = ("docker", "local")


def _load_backend(payload: dict[str, Any]) -> str:
    backend = payload.get("backend", "docker")
    if backend not in VALID_BACKENDS:
        raise ValueError(f"backend must be one of {VALID_BACKENDS}")
    return backend
```

`_load_allowed_images` 加 `backend` 参数（local 时 dockerfile/context 可缺省为 `""`）：

```python
def _load_allowed_images(payload: dict[str, Any], backend: str = "docker") -> dict[str, ImageConfig]:
    allowed_images = payload.get("allowed_images")
    if not isinstance(allowed_images, dict) or not allowed_images:
        raise ValueError("allowed_images must be a non-empty object")

    result: dict[str, ImageConfig] = {}
    for name, image in allowed_images.items():
        if not isinstance(name, str) or not name.strip():
            raise ValueError("allowed_images keys must be non-blank strings")
        if not isinstance(image, dict):
            raise ValueError("allowed_images entries must be objects")

        if backend == "local":
            dockerfile = image.get("dockerfile") or ""
            context = image.get("context") or ""
            if not isinstance(dockerfile, str) or not isinstance(context, str):
                raise ValueError("allowed_images dockerfile/context must be strings when present")
        else:
            dockerfile = _required_string(image, "dockerfile")
            context = _required_string(image, "context")
        result[name] = ImageConfig(dockerfile=dockerfile, context=context)

    return result
```

`load_config` 中：先 `backend = _load_backend(payload)`，把 `allowed_images=_load_allowed_images(payload, backend)`，并在 `RuntimeConfig(...)` 末尾传 `backend=backend`。

- [ ] **Step 4: 跑测试确认通过**

Run: `python -m unittest discover -s infra/mcp/docker-runtime/tests -v`
Expected: 全部 PASS（含既有测试，0 回归）。

- [ ] **Step 5: 提交**

```bash
git add infra/mcp/docker-runtime/server.py infra/mcp/docker-runtime/tests/test_server.py
git commit -m "feat(mcp): add fail-closed backend config field (docker|local)"
```

### Task 2: Windows 路径 → `/mnt` 翻译 helper

**Files:**
- Modify: `infra/mcp/docker-runtime/server.py`
- Test: `infra/mcp/docker-runtime/tests/test_server.py`

- [ ] **Step 1: 写失败测试**

```python
    def test_translate_mount_target_converts_windows_paths(self):
        cases = [
            ("D:\\Codes\\MetBench", "/mnt/d/Codes/MetBench"),
            ("c:/Users/lemon/AppData/Local/Temp", "/mnt/c/Users/lemon/AppData/Local/Temp"),
            ("/opt/openmc-data", "/opt/openmc-data"),
            ("relative/path", "relative/path"),
        ]

        for source, expected in cases:
            with self.subTest(source=source):
                self.assertEqual(expected, self.server.translate_mount_target(source))
```

- [ ] **Step 2: 跑测试确认失败** — Expected: AttributeError `translate_mount_target`。

- [ ] **Step 3: 最小实现** — `server.py` 顶部 `import re`，并在 `authorize` 之后新增：

```python
WINDOWS_PATH_PATTERN = re.compile(r"^([A-Za-z]):[\\/](.*)$")


def translate_mount_target(path: str) -> str:
    match = WINDOWS_PATH_PATTERN.match(path)
    if match is None:
        return path
    drive = match.group(1).lower()
    rest = match.group(2).replace("\\", "/")
    return f"/mnt/{drive}/{rest}"
```

- [ ] **Step 4: 跑测试确认通过**
- [ ] **Step 5: 提交** — `git commit -m "feat(mcp): add windows-to-/mnt mount target translation"`

### Task 3: docker 后端挂载 `allowed_mount_roots` + 目标翻译（关 G3/G5）

**Files:**
- Modify: `infra/mcp/docker-runtime/server.py`（`build_docker_run_command`）
- Test: `infra/mcp/docker-runtime/tests/test_server.py`

- [ ] **Step 1: 写失败测试**

```python
    def test_build_docker_run_command_mounts_allowed_roots_with_translated_targets(self):
        config = self.valid_runtime_config()
        config.repo_root = "D:\\Codes\\MetBench"
        config.allowed_mount_roots = [
            "D:\\Codes\\MetBench",
            "C:\\Users\\lemon\\AppData\\Local\\Temp",
        ]

        command = self.server.build_docker_run_command(
            config,
            "metbench-sut:latest",
            ["python", "sut.py"],
            timeout_seconds=30,
        )

        self.assertIn("D:\\Codes\\MetBench:/mnt/d/Codes/MetBench", command)
        self.assertIn(
            "C:\\Users\\lemon\\AppData\\Local\\Temp:/mnt/c/Users/lemon/AppData/Local/Temp",
            command,
        )
        self.assertNotIn("/tmp:/tmp", command)
        w_index = command.index("-w")
        self.assertEqual("/mnt/d/Codes/MetBench", command[w_index + 1])
        self.assertEqual(["metbench-sut:latest", "python", "sut.py"], command[-3:])

    def test_build_docker_run_command_mounts_extra_linux_roots(self):
        config = self.valid_runtime_config()
        config.allowed_mount_roots = ["/tmp", "/opt/openmc-data"]

        command = self.server.build_docker_run_command(
            config,
            "metbench-sut:latest",
            ["python", "sut.py"],
            timeout_seconds=30,
        )

        self.assertIn(f"{REPO_ROOT}:{REPO_ROOT}", command)
        self.assertIn("/tmp:/tmp", command)
        self.assertIn("/opt/openmc-data:/opt/openmc-data", command)
```

- [ ] **Step 2: 跑测试确认失败** — Expected: 新增 2 个测试 FAIL（现实现只挂 repo_root 与 /tmp）。

- [ ] **Step 3: 最小实现** — 重写 `build_docker_run_command` 的挂载段：

```python
def _is_windows_path(path: str) -> bool:
    return WINDOWS_PATH_PATTERN.match(path) is not None


def build_docker_run_command(
    config: RuntimeConfig,
    image: str,
    argv: list[str],
    timeout_seconds: int | None = None,
) -> list[str]:
    validate_run_request(config, {"image": image, "argv": argv})

    effective_timeout = config.default_timeout_seconds if timeout_seconds is None else timeout_seconds
    if (
        not isinstance(effective_timeout, int)
        or isinstance(effective_timeout, bool)
        or effective_timeout <= 0
    ):
        raise ValueError("timeout_seconds must be positive")

    roots: list[str] = []
    for root in [config.repo_root, *config.allowed_mount_roots]:
        if root not in roots:
            roots.append(root)
    # Legacy compatibility: Linux hosts always mounted /tmp; Windows hosts must not.
    if not _is_windows_path(config.repo_root) and "/tmp" not in roots:
        roots.append("/tmp")

    command = ["docker", "run", "--rm"]
    for root in roots:
        command += ["-v", f"{root}:{translate_mount_target(root)}"]
    command += ["-w", translate_mount_target(config.repo_root), image, *argv]
    return command
```

注意：原实现的 `repo_root = str(Path(config.repo_root))` 归一化被移除（对既有 Linux 路径无行为差异，且会在 Windows 宿主上破坏 Linux 路径）；既有测试 `test_build_docker_run_command_uses_repo_and_tmp_mounts_without_privileged_networking`（`allowed_mount_roots=["/tmp"]`）必须保持通过。

- [ ] **Step 4: 跑测试确认通过**（全部，0 回归）
- [ ] **Step 5: 提交** — `git commit -m "feat(mcp): mount allowed_mount_roots with translated container targets"`

### Task 4: local 执行后端（关 G4）

**Files:**
- Modify: `infra/mcp/docker-runtime/server.py`（`run_sut_command` / `build_runtime_image` / 新 `build_local_run_command`）
- Test: `infra/mcp/docker-runtime/tests/test_server.py`

- [ ] **Step 1: 写失败测试** — 测试类内先加构造 helper，再加 3 个测试：

```python
    def local_runtime_config(self):
        return self.server.RuntimeConfig(
            bind_host="auto-private-ipv4",
            bind_port=8766,
            auth_token="secret",
            repo_root="/home/mt",
            allowed_images={
                "wsl-openmc": self.server.ImageConfig(dockerfile="", context=""),
            },
            allowed_mount_roots=["/tmp"],
            default_timeout_seconds=60,
            max_output_bytes=1024,
            backend="local",
        )

    def test_local_backend_runs_argv_directly_without_docker(self):
        config = self.local_runtime_config()
        calls = []

        def fake_runner(command, timeout_seconds):
            calls.append((command, timeout_seconds))
            return self.server.CommandResult(returncode=0, stdout="ok", stderr="")

        response = self.server.dispatch_tool(
            config,
            "Bearer secret",
            {
                "tool": "run_sut_command",
                "arguments": {
                    "image": "wsl-openmc",
                    "argv": ["python", "sut.py", "--input", "in.json"],
                    "timeout_seconds": 9,
                },
            },
            runner=fake_runner,
            id_factory=lambda: "local-run-1",
        )

        self.assertEqual("completed", response["status"])
        self.assertEqual(1, len(calls))
        self.assertEqual(["python", "sut.py", "--input", "in.json"], calls[0][0])
        self.assertEqual(9, calls[0][1])
        self.assertEqual(["python", "sut.py", "--input", "in.json"], response["command"])
        self.assertNotIn("docker", response["command"])

    def test_local_backend_still_rejects_non_allowlisted_image(self):
        config = self.local_runtime_config()

        with self.assertRaisesRegex(ValueError, "not allowlisted"):
            self.server.dispatch_tool(
                config,
                "Bearer secret",
                {
                    "tool": "run_sut_command",
                    "arguments": {"image": "other", "argv": ["python", "x.py"]},
                },
                runner=lambda command, timeout_seconds: self.server.CommandResult(0, "", ""),
            )

    def test_local_backend_rejects_build_runtime_image(self):
        config = self.local_runtime_config()

        with self.assertRaisesRegex(ValueError, "local"):
            self.server.dispatch_tool(
                config,
                "Bearer secret",
                {
                    "tool": "build_runtime_image",
                    "arguments": {"image": "wsl-openmc"},
                },
                runner=lambda command, timeout_seconds: self.server.CommandResult(0, "", ""),
            )
```

- [ ] **Step 2: 跑测试确认失败** — Expected: 第 1 个测试 FAIL（命令带 docker 前缀），第 3 个 FAIL（local 下 build 未被拒绝）。

- [ ] **Step 3: 最小实现**：

```python
def build_local_run_command(
    config: RuntimeConfig,
    image: str,
    argv: list[str],
    timeout_seconds: int | None = None,
) -> list[str]:
    validate_run_request(config, {"image": image, "argv": argv})

    effective_timeout = config.default_timeout_seconds if timeout_seconds is None else timeout_seconds
    if (
        not isinstance(effective_timeout, int)
        or isinstance(effective_timeout, bool)
        or effective_timeout <= 0
    ):
        raise ValueError("timeout_seconds must be positive")

    return list(argv)
```

`run_sut_command` 中把 `command = build_docker_run_command(...)` 改为：

```python
    if config.backend == "local":
        command = build_local_run_command(config, image, argv, timeout_seconds)
    else:
        command = build_docker_run_command(config, image, argv, timeout_seconds)
```

`build_runtime_image` 函数体开头插入：

```python
    if config.backend == "local":
        raise ValueError("build_runtime_image is not supported when backend is 'local'")
```

- [ ] **Step 4: 跑测试确认通过**
- [ ] **Step 5: 提交** — `git commit -m "feat(mcp): add local execution backend for run_sut_command"`

### Task 5: 三份 config 示例 + README

**Files:**
- Create: `infra/mcp/docker-runtime/config.local-win.example.json`、`config.docker-win.example.json`、`config.local-wsl.example.json`
- Modify: `infra/mcp/docker-runtime/README.md`
- Test: `infra/mcp/docker-runtime/tests/test_server.py`

- [ ] **Step 1: 写失败测试**

```python
    def test_acceptance_config_examples_load(self):
        base = Path(__file__).resolve().parents[1]
        cases = [
            ("config.local-win.example.json", "local", 8764),
            ("config.docker-win.example.json", "docker", 8765),
            ("config.local-wsl.example.json", "local", 8766),
        ]

        for name, backend, port in cases:
            with self.subTest(name=name):
                config = self.server.load_config(base / name)

                self.assertEqual(backend, config.backend)
                self.assertEqual(port, config.bind_port)
                self.assertEqual("change-me", config.auth_token)
```

- [ ] **Step 2: 跑测试确认失败** — Expected: FileNotFoundError。

- [ ] **Step 3: 创建三份文件**：

`config.local-win.example.json`（用例 1）：

```json
{
  "backend": "local",
  "bind_host": "auto-private-ipv4",
  "bind_port": 8764,
  "auth_token": "change-me",
  "repo_root": "D:\\Codes\\MetBench-V2.1.4_2",
  "allowed_images": {
    "windows-local": {}
  },
  "allowed_mount_roots": ["D:\\Codes\\MetBench-V2.1.4_2"],
  "default_timeout_seconds": 600,
  "max_output_bytes": 1048576
}
```

`config.docker-win.example.json`（用例 2）：

```json
{
  "backend": "docker",
  "bind_host": "auto-private-ipv4",
  "bind_port": 8765,
  "auth_token": "change-me",
  "repo_root": "D:\\Codes\\MetBench-V2.1.4_2",
  "allowed_images": {
    "metbench-sut:latest": {
      "dockerfile": "docker/Dockerfile",
      "context": "docker"
    }
  },
  "allowed_mount_roots": [
    "D:\\Codes\\MetBench-V2.1.4_2",
    "C:\\Users\\lemon\\AppData\\Local\\Temp"
  ],
  "default_timeout_seconds": 600,
  "max_output_bytes": 1048576
}
```

`config.local-wsl.example.json`（用例 3）：

```json
{
  "backend": "local",
  "bind_host": "auto-private-ipv4",
  "bind_port": 8766,
  "auth_token": "change-me",
  "repo_root": "/mnt/d/Codes/MetBench-V2.1.4_2",
  "allowed_images": {
    "wsl-openmc": {}
  },
  "allowed_mount_roots": ["/mnt/d/Codes/MetBench-V2.1.4_2", "/tmp"],
  "default_timeout_seconds": 600,
  "max_output_bytes": 1048576
}
```

README.md 末尾追加「Backends」一节：说明 `backend` 字段语义（docker 缺省 / local 直接执行、local 下 `build_runtime_image` 显式报错、`image` 仍为 allowlist key）与三用例启动命令：

```
python infra/mcp/docker-runtime/server.py infra/mcp/docker-runtime/config.local-win.example.json
python infra/mcp/docker-runtime/server.py infra/mcp/docker-runtime/config.docker-win.example.json
python3 infra/mcp/docker-runtime/server.py infra/mcp/docker-runtime/config.local-wsl.example.json   # WSL 内
```

- [ ] **Step 4: 跑测试确认通过**
- [ ] **Step 5: 提交** — `git commit -m "docs(mcp): add three-case acceptance config examples"`

---

## Phase B — .NET client 侧

### Task 6: `DockerMcpRuntimeOptions` 扩展 + URI 解析（fail-closed）

**Files:**
- Modify: `MetBench_BLL.Core/SystemMT/Runtime/RuntimeModels.cs:68-72`
- Modify: `MetBench_BLL.Core/SystemMT/Runtime/LauncherOptionsRuntimeProfileProvider.cs:61-70`
- Test: `MetBench_SystemMT.Tests/SystemMT/Runtime/DockerMcpRuntimeProfileTests.cs`

- [ ] **Step 1: 写失败测试** — 在 `DockerMcpRuntimeProfileTests` 追加（复用文件内已有的 `Options(...)` helper）：

```csharp
    [Fact]
    public void Provider_parses_optional_local_python_and_wsl_path_style()
    {
        var options = Options(new Dictionary<string, string>
        {
            ["openmc"] =
                "docker-mcp://openmc?image=metbench-sut:latest&python=/opt/openmc-venv/bin/python&endpoint=http%3A%2F%2F192.168.1.20%3A8765&authTokenEnv=METBENCH_DOCKER_MCP_TOKEN&localPython=python&pathStyle=wsl",
        });
        IRuntimeProfileProvider provider = new LauncherOptionsRuntimeProfileProvider(options);

        var profile = provider.GetProfile("openmc");

        Assert.NotNull(profile.DockerMcp);
        Assert.Equal("python", profile.DockerMcp!.LocalPythonExecutable);
        Assert.Equal(DockerMcpPathStyle.Wsl, profile.DockerMcp.PathStyle);
    }

    [Fact]
    public void Provider_defaults_local_python_and_path_style_when_absent()
    {
        var options = Options(new Dictionary<string, string>
        {
            ["openmc"] =
                "docker-mcp://openmc?image=metbench-sut:latest&python=/opt/openmc-venv/bin/python&endpoint=http%3A%2F%2F192.168.1.20%3A8765",
        });
        IRuntimeProfileProvider provider = new LauncherOptionsRuntimeProfileProvider(options);

        var profile = provider.GetProfile("openmc");

        Assert.Null(profile.DockerMcp!.LocalPythonExecutable);
        Assert.Equal(DockerMcpPathStyle.None, profile.DockerMcp.PathStyle);
    }

    [Theory]
    [InlineData("docker-mcp://openmc?image=i&python=p&endpoint=http%3A%2F%2F127.0.0.1%3A8765&pathStyle=windows")]
    [InlineData("docker-mcp://openmc?image=i&python=p&endpoint=http%3A%2F%2F127.0.0.1%3A8765&pathStyle=")]
    public void Provider_fails_closed_on_invalid_path_style(string value)
    {
        var options = Options(new Dictionary<string, string> { ["openmc"] = value });
        IRuntimeProfileProvider provider = new LauncherOptionsRuntimeProfileProvider(options);

        var ex = Assert.Throws<RuntimeEnvironmentResolutionException>(() => provider.GetProfile("openmc"));

        Assert.Contains("pathStyle", ex.Message);
    }
```

- [ ] **Step 2: 跑测试确认失败**

Run: `dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~DockerMcpRuntimeProfileTests"`
Expected: 编译错误（`LocalPythonExecutable` / `DockerMcpPathStyle` 不存在）→ 即「失败」证据。

- [ ] **Step 3: 最小实现**：

`RuntimeModels.cs` —— 在 `DockerMcpRuntimeOptions` 前加 enum，record 加两个带默认值的参数（不破坏既有调用方）：

```csharp
public enum DockerMcpPathStyle
{
    None = 0,
    Wsl = 1,
}

public sealed record DockerMcpRuntimeOptions(
    string Endpoint,
    string Image,
    string PythonExecutable,
    string? AuthTokenEnvironmentVariable = null,
    string? LocalPythonExecutable = null,
    DockerMcpPathStyle PathStyle = DockerMcpPathStyle.None);
```

`LauncherOptionsRuntimeProfileProvider.CreateDockerProfile` —— 在 `authTokenEnv` 处理之后、`return` 之前插入：

```csharp
        query.TryGetValue("localPython", out var localPython);
        localPython = string.IsNullOrWhiteSpace(localPython) ? null : localPython;

        var pathStyle = DockerMcpPathStyle.None;
        if (query.TryGetValue("pathStyle", out var pathStyleRaw))
        {
            if (!string.Equals(pathStyleRaw, "wsl", StringComparison.OrdinalIgnoreCase))
                throw InvalidDockerRuntime(runtimeKey, "pathStyle");
            pathStyle = DockerMcpPathStyle.Wsl;
        }
```

并把 `return` 中的构造改为：

```csharp
            dockerMcp: new DockerMcpRuntimeOptions(
                endpoint, image, python, authTokenEnv, localPython, pathStyle));
```

- [ ] **Step 4: 跑测试确认通过**（同 filter，全绿；另跑 `--filter "FullyQualifiedName~RuntimeProfileProviderTests|FullyQualifiedName~RuntimePreflight"` 确认 0 回归）。
- [ ] **Step 5: 提交** — `git commit -m "feat(runtime): parse optional localPython and pathStyle docker-mcp params"`

### Task 7: SplitCommand 反斜杠修复（新缺口 G7）+ executor argv WSL 路径翻译（关 G2）

**Files:**
- Modify: `MetBench_BLL.Core/SystemMT/Runtime/DockerMcpProcessExecutor.cs`（`SplitCommand` L54-92 + `RunAsync` L20-42）
- Modify: `docs/superpowers/specs/2026-06-12-mcp-three-case-acceptance-design.md`（§2 缺口表补 G7 行）
- Test: `MetBench_SystemMT.Tests/SystemMT/Runtime/DockerMcpProcessExecutorTests.cs`

**新缺口 G7（计划期核实）**：`SplitCommand` 把 `\` 一律当转义符消费（`DockerMcpProcessExecutor.cs:63-67`），`"D:\repo\runner.py"` 被拆成 `D:repounner.py`——用例 1 需要原样保留的 Windows 路径、用例 2/3 翻译前的 token 全部被破坏。**冲突挑明**：修复方案是把转义收窄为仅 `\"`（保留引号转义能力），其余 `\` 字面保留；若既有 SplitCommand 测试钉死了旧转义语义，更新这些测试并在 PR 描述记录语义变更（launcher 生成的命令从不包含 `\"`，生产路径无受影响调用方）。

- [ ] **Step 0a: 写 G7 失败测试**（先读 `DockerMcpProcessExecutorTests.cs` 看是否已有 SplitCommand 测试钉旧语义）：

```csharp
    [Fact]
    public void SplitCommand_preserves_backslashes_in_quoted_windows_paths()
    {
        var argv = DockerMcpProcessExecutor.SplitCommand(
            "\"python\" \"D:\\repo\\runner.py\" --input \"C:\\Temp\\in.json\"");

        Assert.Equal(
            new[] { "python", @"D:\repo\runner.py", "--input", @"C:\Temp\in.json" },
            argv);
    }
```

Run（确认失败）: `dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~DockerMcpProcessExecutorTests"`
Expected: FAIL —— 实际 argv 为 `D:repounner.py` 形态（把实际输出贴进 PR 描述作 G7 证据）。

- [ ] **Step 0b: 修 SplitCommand** —— 把转义分支（L56-67）改为「`\` 仅在后随 `"` 时转义，否则字面保留」：

```csharp
            if (escaping)
            {
                if (ch != '"')
                {
                    current.Append('\\');
                }
                current.Append(ch);
                escaping = false;
                continue;
            }
```

（`if (ch == '\\') { escaping = true; continue; }` 与收尾的 `if (escaping) current.Append('\\');` 保持不变。）若有旧测试断言 `\x` 转义为 `x`，按新语义更新并注明。

Run（确认通过 + 0 回归）: 同 filter 全绿。

- [ ] **Step 0c: spec §2 缺口表补 G7 行**：「G7 | `SplitCommand` 把 `\` 一律当转义符，Windows 路径 token 被破坏 | `DockerMcpProcessExecutor.cs:63-67` | 1、2、3」。

- [ ] **Step 1: 写翻译失败测试** — 追加（fake client 若文件内已有可复用，否则按下方定义；`DockerMcpRunResult` 构造参数顺序以 `RuntimeModels.cs` 实际定义为准，先读后写）：

```csharp
    [Theory]
    [InlineData(@"D:\Codes\MetBench\SUT\openmc\openmc_runner.py", "/mnt/d/Codes/MetBench/SUT/openmc/openmc_runner.py")]
    [InlineData("c:/Users/lemon/AppData/Local/Temp/x.json", "/mnt/c/Users/lemon/AppData/Local/Temp/x.json")]
    [InlineData("--input", "--input")]
    [InlineData("/opt/openmc-venv/bin/python", "/opt/openmc-venv/bin/python")]
    [InlineData("5000", "5000")]
    public void TranslateWindowsPathToWsl_translates_only_windows_absolute_paths(
        string token, string expected)
    {
        Assert.Equal(expected, DockerMcpProcessExecutor.TranslateWindowsPathToWsl(token));
    }

    [Fact]
    public async Task RunAsync_translates_argv_when_path_style_is_wsl()
    {
        var client = new RecordingClient();
        var executor = new DockerMcpProcessExecutor(client);
        var options = new DockerMcpRuntimeOptions(
            "http://127.0.0.1:8765", "img", "/opt/venv/bin/python",
            PathStyle: DockerMcpPathStyle.Wsl);

        await executor.RunAsync(
            options,
            "\"/opt/venv/bin/python\" \"D:\\repo\\SUT\\runner.py\" --input \"C:\\Temp\\in.json\"",
            30,
            CancellationToken.None);

        Assert.Equal(
            new[]
            {
                "/opt/venv/bin/python",
                "/mnt/d/repo/SUT/runner.py",
                "--input",
                "/mnt/c/Temp/in.json",
            },
            client.LastArgv);
    }

    [Fact]
    public async Task RunAsync_keeps_argv_untranslated_when_path_style_is_none()
    {
        var client = new RecordingClient();
        var executor = new DockerMcpProcessExecutor(client);
        var options = new DockerMcpRuntimeOptions(
            "http://127.0.0.1:8765", "img", "python");

        await executor.RunAsync(options, "python \"D:\\repo\\runner.py\"", 30, CancellationToken.None);

        Assert.Equal(new[] { "python", @"D:\repo\runner.py" }, client.LastArgv);
    }
```

RecordingClient（若文件内无可复用 fake）：

```csharp
    private sealed class RecordingClient : IDockerMcpRuntimeClient
    {
        public IReadOnlyList<string>? LastArgv;

        public Task<DockerMcpHealthResult> HealthAsync(
            DockerMcpRuntimeOptions options, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<DockerMcpRunResult> RunSutCommandAsync(
            DockerMcpRuntimeOptions options,
            IReadOnlyList<string> argv,
            int timeoutSeconds,
            CancellationToken cancellationToken = default)
        {
            LastArgv = argv;
            return Task.FromResult(new DockerMcpRunResult(0, string.Empty, string.Empty, false));
        }
    }
```

（`RunAsync_keeps_argv_untranslated_when_path_style_is_none` 的期望值依赖 Step 0b 已修复的 SplitCommand 语义——Windows 路径 token 原样保留。）

- [ ] **Step 2: 跑测试确认失败**

Run: `dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~DockerMcpProcessExecutorTests"`
Expected: 编译错误（`TranslateWindowsPathToWsl` 不存在）。

- [ ] **Step 3: 最小实现** — `DockerMcpProcessExecutor.cs`：

```csharp
    internal static string TranslateWindowsPathToWsl(string token)
    {
        if (token.Length < 3
            || !char.IsAsciiLetter(token[0])
            || token[1] != ':'
            || (token[2] != '\\' && token[2] != '/'))
        {
            return token;
        }

        var drive = char.ToLowerInvariant(token[0]);
        var rest = token[3..].Replace('\\', '/');
        return $"/mnt/{drive}/{rest}";
    }
```

`RunAsync` 中 `var argv = SplitCommand(command);` 之后插入：

```csharp
        if (options.PathStyle == DockerMcpPathStyle.Wsl)
        {
            argv = argv.Select(TranslateWindowsPathToWsl).ToList();
        }
```

（需要 `using System.Linq;`。翻译发生在 Step 0b 修复后的 SplitCommand 之后，token 内反斜杠已原样保留。）

- [ ] **Step 4: 跑测试确认通过**（同 filter 全绿）。
- [ ] **Step 5: 提交**

```bash
git add MetBench_BLL.Core/SystemMT/Runtime/DockerMcpProcessExecutor.cs MetBench_SystemMT.Tests/SystemMT/Runtime/DockerMcpProcessExecutorTests.cs docs/superpowers/specs/2026-06-12-mcp-three-case-acceptance-design.md
git commit -m "fix(runtime): preserve backslashes in SplitCommand and translate wsl argv paths"
```

### Task 8: launcher parser 命令用 `LocalPythonExecutable`（关 G1）+ loopback 集成 fact 测试

**Files:**
- Modify: `MetBench_BLL.Core/SystemMT/Launcher/SystemMtLauncher.cs:185-215`
- Create: `MetBench_SystemMT.Tests/SystemMT/Launcher/LauncherDockerMcpLocalParserTests.cs`
- Modify: `docs/superpowers/specs/2026-06-12-mcp-three-case-acceptance-design.md`（§4.2 retro-touch）

**语义决策（冲突挑明）**：spec §4.2 原文「localPython 缺省回退 `LauncherOptions.SystemPython`」与同节「无新参数时行为不变」矛盾——缺省换成 SystemPython 本身就是行为变化。本 Task 取后者：**缺省沿用 profile 的 `python`**（与 main 现行为逐字节一致），显式设 `localPython` 才覆盖。Step 6 同 PR 修 spec 措辞（R3）。

- [ ] **Step 1: 写失败测试** — 新文件 `LauncherDockerMcpLocalParserTests.cs`。测试用进程内 `HttpListener` 充当假 MCP server（loopback，CI 可跑）：`runtime_health` 返回 ok；`run_sut_command` 把 argv[0]（故意配置的假容器 python `/nonexistent/container-python`）替换为真实本地 python 后用 `System.Diagnostics.Process` 执行并返回 stdout/exit code。MR 用纯 stdlib 的 B 组 `p3-trajectory-sensitivity`（**先读** `MetBench_SystemMT.Tests/SystemMT/Jobs/MinimumMrSubsetBGroupAsyncJobTests.cs`，复用其 MR id 常量与 catalog provider 构造——若该测试用额外 manifest 路径构造 `ManifestMrCatalogProvider`，照抄）。

测试主体（fact 断言）：

```csharp
// options: RuntimePythons["system"] =
//   $"docker-mcp://system?image=test-image&python=/nonexistent/container-python"
//   + $"&endpoint={Uri.EscapeDataString(fakeServer.Endpoint)}"
//   + $"&localPython={Uri.EscapeDataString(TestAssetPaths.PythonExecutable())}"
// （system key 的 docker-mcp URI 会让该 key 全部 MR 走 MCP——测试自包含，无全局影响）

var result = await launcher.RunAsync(MrId);

Assert.True(result.Passed, "FailureReason: " + result.FailureReason);
// G1 fact：parser/output-parser 在本地跑（若走了假容器 python /nonexistent/... 会失败）
// 路由 fact：恰好 2 次 run_sut_command（source + followup），且每次 argv[0] 是假容器 python
Assert.Equal(2, fakeServer.RunSutCommandCalls.Count);
Assert.All(fakeServer.RunSutCommandCalls, argv =>
    Assert.Equal("/nonexistent/container-python", argv[0]));
```

假 server 处理器骨架（放同文件内 `private sealed class FakeMcpServer : IDisposable`）：

```csharp
// HttpListener on http://127.0.0.1:{free-port}/ ；POST /tool：
//   tool == "runtime_health"   → {"status":"ok","bind_host":"127.0.0.1","bind_port":port,"repo_root":"/"}
//   tool == "run_sut_command"  → 记录 argv；用 TestAssetPaths.PythonExecutable() 替换 argv[0] 执行；
//                                返回 {"run_id":"t","status":"completed","returncode":exit,
//                                      "stdout":stdout,"stderr":stderr}
// 响应字段名以 DockerMcpRuntimeClient.RunSutCommandAsync 的实际解析为准——先读
// MetBench_BLL.Core/SystemMT/Runtime/DockerMcpRuntimeClient.cs 再写假响应。
```

- [ ] **Step 2: 跑测试确认失败**

Run: `dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~LauncherDockerMcpLocalParserTests"`
Expected: FAIL —— 现行 launcher 把 parser 命令也建在 `/nonexistent/container-python` 上，parser 本地执行报错（即 G1 的可复现证据）。把失败输出存档进 PR 描述。

- [ ] **Step 3: 最小实现** — `SystemMtLauncher.cs`：

L187 起改为（新增 `parserPythonExecutable` 变量）：

```csharp
        var pythonExecutable = blueprint.PythonExecutable;
        var parserPythonExecutable = blueprint.PythonExecutable;
        try
        {
            resolvedRuntimeProfile = CreateRuntimeProfile(blueprint);
            pythonExecutable = resolvedRuntimeProfile.DockerMcp?.PythonExecutable
                ?? resolvedRuntimeProfile.ExecutablePath
                ?? blueprint.PythonExecutable;
            parserPythonExecutable = resolvedRuntimeProfile.DockerMcp?.LocalPythonExecutable
                ?? pythonExecutable;
        }
        catch (RuntimeEnvironmentResolutionException ex)
        {
            runtimeProfileResolutionError = ex.Message;
        }
```

L213-214 改为：

```csharp
            InputParserCommand: $"\"{parserPythonExecutable}\" \"{blueprint.InputParserScriptPath}\"",
            OutputParserCommand: $"\"{parserPythonExecutable}\" \"{blueprint.OutputParserScriptPath}\"",
```

`RunnerCommand`（L215）保持用 `pythonExecutable`，不动。

- [ ] **Step 4: 跑测试确认通过** — 同 filter 全绿；再跑回归：

```
dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~LauncherEndToEnd|FullyQualifiedName~RuntimePreflightLauncherTests"
```

Expected: 全绿（OpenMOC/OpenMC 相关按环境 skip）。

- [ ] **Step 5: spec retro-touch** — 编辑 spec §4.2 第一个列表项，把「缺省回退 `LauncherOptions.SystemPython`」改为「缺省沿用 profile 的 `python`（与既有行为逐字节一致）；显式设 `localPython` 才覆盖 parser/output-parser 的解释器」。

- [ ] **Step 6: 提交**

```bash
git add MetBench_BLL.Core/SystemMT/Launcher/SystemMtLauncher.cs MetBench_SystemMT.Tests/SystemMT/Launcher/LauncherDockerMcpLocalParserTests.cs docs/superpowers/specs/2026-06-12-mcp-three-case-acceptance-design.md
git commit -m "feat(launcher): run parser commands with localPython under docker-mcp profiles"
```

### Task 9: WPF `RuntimePythons` 接线（关 G6）

**Files:**
- Modify: `MetBench_Client/App.xaml.cs:137-143`
- Create: `MetBench_SystemMT.Tests/SystemMT/Jobs/WpfRuntimePythonsWiringTests.cs`

- [ ] **Step 1: 写失败测试** — 仿 `WpfAsyncJobCancellationWiringTests` 的 ReadRepoFile 模式（先读该文件抄其 `ReadRepoFile` helper）：

```csharp
public sealed class WpfRuntimePythonsWiringTests
{
    [Fact]
    public void App_feeds_runtime_pythons_from_environment_variables()
    {
        var app = ReadRepoFile("MetBench_Client", "App.xaml.cs");

        Assert.Contains("RuntimePythons", app);
        Assert.Contains("METBENCH_SYSTEM_PYTHON", app);
        Assert.Contains("METBENCH_OPENMC_PYTHON", app);
        Assert.Contains("StringComparer.OrdinalIgnoreCase", app);
    }
}
```

- [ ] **Step 2: 跑测试确认失败**

Run: `dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~WpfRuntimePythonsWiringTests"`
Expected: FAIL（App.xaml.cs 无 RuntimePythons）。

- [ ] **Step 3: 最小实现** — `App.xaml.cs:137-143` 的 `LauncherOptions` 构造追加 `RuntimePythons:` 命名参数（**先读** `LauncherOptions.cs` 确认 `RuntimePythons` 是构造参数还是 init 属性，按实际写法传入；CLAUDE.md §6 的 DI 示例即此形态）：

```csharp
                services.AddSingleton(provider => new LauncherOptions(
                    SutRoot: Path.Combine(
                        Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!,
                        "SUT"),
                    SystemPython: OperatingSystem.IsWindows() ? "python" : "python3",
                    OpenMocPython: Environment.GetEnvironmentVariable("METBENCH_OPENMOC_PYTHON")
                        ?? (OperatingSystem.IsWindows() ? "python" : "python3"),
                    RuntimePythons: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        // 空值由 ResolvePythonExecutable 的非空白检查跳过：env 未设时行为与现状一致。
                        ["system"] = Environment.GetEnvironmentVariable("METBENCH_SYSTEM_PYTHON") ?? "",
                        ["openmc"] = Environment.GetEnvironmentVariable("METBENCH_OPENMC_PYTHON") ?? "",
                    }));
```

- [ ] **Step 4: 验证** — 守护测试过 + WPF 本机编译：

```
dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~WpfRuntimePythonsWiringTests"
dotnet build MetBench_Client/MetBench_Client.csproj --no-restore
```

Expected: 测试 PASS；WPF 0 编译错误。

- [ ] **Step 5: 提交** — `git commit -m "feat(wpf): feed RuntimePythons from METBENCH_SYSTEM/OPENMC_PYTHON env vars"`

---

## Phase C — 验收资产

### Task 10: 环境门控验收测试（3 条）

**Files:**
- Create: `MetBench_SystemMT.Tests/SystemMT/Acceptance/McpThreeCaseAcceptanceTests.cs`

环境契约（未设全则 3 条全 skip，CI 永远 skip）：
- `METBENCH_MCP_ACCEPTANCE_URI` —— 完整 `docker-mcp://` URI（指向被测 server）
- `METBENCH_MCP_ACCEPTANCE_RUNTIME_KEY` —— 与 URI host 一致的 runtime key（`system` / `openmc`）
- `METBENCH_MCP_ACCEPTANCE_MR` —— 要跑的 MR id
- `METBENCH_MCP_ACCEPTANCE_SUTROOT`（可选）—— 缺省 `TestAssetPaths.AssetRoot()`
- URI 的 `authTokenEnv` 指到的变量（如 `METBENCH_DOCKER_MCP_TOKEN`）

- [ ] **Step 1: 写测试**（验收测试无"先失败"环节——门控 skip 即 CI 行为；本 Task 的 TDD 体现在先验证 skip 路径再验证真实路径）：

```csharp
using MetBench_BLL.SystemMT.Catalog;
using MetBench_BLL.SystemMT.Launcher;
using MetBench_BLL.SystemMT.Pipeline;
using MetBench_BLL.SystemMT.Runtime;
using MetBench_SystemMT.Tests.SystemMT;
using MetBench_SystemMT.Tests.V2Anomaly;
using MetBench_SystemMT.Tests.V2Pipeline;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Acceptance;

/// <summary>
/// LAN acceptance for the three MCP server deployments (spec
/// 2026-06-12-mcp-three-case-acceptance-design.md §8). Skips unless the
/// METBENCH_MCP_ACCEPTANCE_* environment variables point at a live server.
/// </summary>
public sealed class McpThreeCaseAcceptanceTests
{
    private const string SkipReason =
        "MCP acceptance env is not configured. Set METBENCH_MCP_ACCEPTANCE_URI, "
        + "METBENCH_MCP_ACCEPTANCE_RUNTIME_KEY and METBENCH_MCP_ACCEPTANCE_MR.";

    private static string? Env(string name) => Environment.GetEnvironmentVariable(name);

    private static bool Configured =>
        !string.IsNullOrWhiteSpace(Env("METBENCH_MCP_ACCEPTANCE_URI"))
        && !string.IsNullOrWhiteSpace(Env("METBENCH_MCP_ACCEPTANCE_RUNTIME_KEY"))
        && !string.IsNullOrWhiteSpace(Env("METBENCH_MCP_ACCEPTANCE_MR"));

    private static LauncherOptions AcceptanceOptions()
    {
        var key = Env("METBENCH_MCP_ACCEPTANCE_RUNTIME_KEY")!;
        return new LauncherOptions(
            SutRoot: Env("METBENCH_MCP_ACCEPTANCE_SUTROOT") ?? TestAssetPaths.AssetRoot(),
            SystemPython: TestAssetPaths.PythonExecutable(),
            OpenMocPython: TestAssetPaths.PythonExecutable(),
            RuntimePythons: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [key] = Env("METBENCH_MCP_ACCEPTANCE_URI")!,
            });
    }

    [SkippableFact]
    public async Task Acceptance_1_preflight_health_reaches_live_server()
    {
        Skip.IfNot(Configured, SkipReason);

        var key = Env("METBENCH_MCP_ACCEPTANCE_RUNTIME_KEY")!;
        var profile = new LauncherOptionsRuntimeProfileProvider(AcceptanceOptions()).GetProfile(key);

        Assert.NotNull(profile.DockerMcp);
        var health = await new DockerMcpRuntimeClient().HealthAsync(profile.DockerMcp!);
        Assert.True(health.Available, "runtime_health failed: " + health.Detail);
        Assert.Equal("ok", health.Status);
    }

    [SkippableFact]
    public async Task Acceptance_2_launcher_runs_mr_end_to_end_through_mcp()
    {
        Skip.IfNot(Configured, SkipReason);

        var options = AcceptanceOptions();
        var execs = new FakeExecRepo();
        var results = new FakeResultRepo();
        var launcher = new SystemMtLauncher(
            options,
            new SystemMtPipeline(),
            new SystemMtExecutionRecorder(execs, results),
            new RecordingAnomalyService(),
            new ManifestMrCatalogProvider(options));

        var result = await launcher.RunAsync(Env("METBENCH_MCP_ACCEPTANCE_MR")!);

        Assert.True(result.Passed, "FailureReason: " + result.FailureReason);
        Assert.Single(results.Data);
    }

    [SkippableFact]
    public async Task Acceptance_3_async_job_reaches_succeeded_through_mcp()
    {
        Skip.IfNot(Configured, SkipReason);
        // 接线照抄 MinimumMrSubsetBGroupAsyncJobTests（先读该文件）：
        // SystemMtJobService(store, queue) → SubmitAsync(RunMr, MR) →
        // SystemMtJobWorker(store, new SystemMtAsyncPipeline(launcher, evidenceRepo)).RunJobAsync →
        // Assert: store.GetAsync(id).State == SystemMtJobState.Succeeded
        //         store.GetResultAsync(id) != null
    }
}
```

（`DockerMcpHealthResult` 属性名 `Available`/`Status`/`Detail`、`SystemMtJobService` 构造与 `SubmitAsync` 形参——**先读** `DockerMcpRuntimeClient.cs`、`SystemMtJobService.cs` 与 `MinimumMrSubsetBGroupAsyncJobTests.cs` 再落笔，按实际签名微调。）

- [ ] **Step 2: 验证 skip 路径（CI 行为）**

Run: `dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~McpThreeCaseAcceptanceTests"`
Expected: 3 skipped, 0 failed（本机未起 server 时）。

- [ ] **Step 3: 提交** — `git commit -m "test(acceptance): add env-gated three-case mcp acceptance suite"`

### Task 11: 验收操作手册（runbook）

**Files:**
- Create: `docs/uat/mcp-three-case-acceptance-runbook.md`

- [ ] **Step 1: 写手册**，必须含以下章节（逐字命令，操作者可粘贴）：

1. **前置条件**：Windows python ≥3.10；Docker Desktop（用例 2）+ WSL2 Ubuntu 24.04（用例 3）；token 约定（`$env:METBENCH_DOCKER_MCP_TOKEN`，与 server config `auth_token` 一致，不入库）。
2. **环境构建**：
   - 用例 2 镜像：`docker build -t metbench-sut:latest docker/`（多 GB 源码构建，一次性）。
   - 用例 3 WSL venv：照 `docker/Dockerfile` builder 段的 OpenMC 步骤在 WSL 内执行（apt 依赖 → cmake 构建 → `/home/<user>/openmc-venv` + binary symlink）；验证 `~/openmc-venv/bin/python -c "import openmc"`。**不下载截面数据库**。
3. **server 启动（CLI，三条）**：复制三份 example config 去掉 `.example`、填入真实 `repo_root` / `auth_token`，然后：
   - 用例 1（Windows host）：`python infra/mcp/docker-runtime/server.py infra/mcp/docker-runtime/config.local-win.json`
   - 用例 2（Windows host）：`python infra/mcp/docker-runtime/server.py infra/mcp/docker-runtime/config.docker-win.json`
   - 用例 3（WSL 内）：`python3 infra/mcp/docker-runtime/server.py infra/mcp/docker-runtime/config.local-wsl.json`
   - 记录各自 bind IP（启动日志/`runtime_health`），写进证据。
4. **验收测试（每用例一组 env + 一次 dotnet test）**：spec §5 的三组 URI 逐字给出（含 PowerShell `$env:` 设置与 `Uri` 转义说明），命令模板：

   ```powershell
   dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --no-restore `
     --filter "FullyQualifiedName~McpThreeCaseAcceptanceTests" `
     --logger "trx;LogFileName=mcp-acceptance-case<N>.trx"
   ```

   通过判据：3 passed / 0 failed / 0 skipped；`.trx` 归档路径。
5. **WPF 手动验收**：设同组 env → `dotnet run --project MetBench_Client` → 异步作业页提交 RunMr（用例 1：`p3-trajectory-sensitivity`；用例 2/3：`openmc-pincell-nu-sigma-f` + RunBatch 3 个 openmc MR）→ 截图清单（job Succeeded、结果页、RuntimeEvidence）；证据放 `docs/superpowers/specs/2026-06-12-mcp-three-case-acceptance-vm-evidence/`（照 2026-06-05 vm-evidence 先例：`vm-summary.md` + 截图）。
6. **判据对照表**：spec §9 两条判据 × 三用例的勾选矩阵。

- [ ] **Step 2: 提交** — `git commit -m "docs(uat): add three-case mcp acceptance runbook"`

### Task 12: 状态投影（§11.4 回写）

**Files:**
- Modify: `docs/status/current.md`（表中追加一行：MCP 三用例验收 —— 实施完成/验收待执行 状态，引 spec/plan/runbook）
- Modify: `docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md`（登记本 plan 为活跃）
- Modify: 本 plan 文件「状态」字段

- [ ] **Step 1: 三处更新**（一行级别，指针互引，不复制结论）。
- [ ] **Step 2: 提交** — `git commit -m "docs(status): project mcp three-case acceptance plan status"`

---

## 最终验证（PR 前必跑全集）

```bash
python -m unittest discover -s infra/mcp/docker-runtime/tests -v        # 全 PASS
dotnet build MetBench_BLL.Core/MetBench_BLL.Core.csproj --no-restore     # 0 error
dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --no-restore   # 全套 0 fail（env-gated 项 skip）
dotnet build MetBench_Client/MetBench_Client.csproj --no-restore         # 0 error（Windows）
git diff --check                                                          # pass
```

## PR Gate Classification

- Scope：单一目的——MCP 三用例验收能力（双后端 + client 参数 + 接线 + 验收资产），1 个 PR。
- Windows classification：**需要 Windows 证据**（App.xaml.cs 改动 + WPF 编译在本机完成；WPF 手动验收按 runbook 另行执行并归档 vm-evidence）。
- 模块 E ritual：单 PR，不触发 ≥3-PR chain 条件。
- PR body 按 `docs/superpowers/templates/pr-gate-checklist.md` 填 7 节；Task 8 的 G1 失败输出粘贴进 Tests 节作 fact 证据。
