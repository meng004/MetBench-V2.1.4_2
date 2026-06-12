# MetBench Docker Runtime MCP

Minimal JSON-over-HTTP runtime bridge for LAN clients that need to build
allowlisted Docker images and run SUT commands inside those images.

## Start

Edit `config.example.json` or provide an equivalent config file. The default
`bind_host` value is `auto-private-ipv4`; the server resolves it at startup to
the first private, non-loopback IPv4 address it can see on the host.

```bash
rtk python3 tools/metbench-docker-runtime-mcp serve --config infra/mcp/docker-runtime/config.example.json
```

The legacy entry point still works:

```bash
rtk python3 infra/mcp/docker-runtime/server.py infra/mcp/docker-runtime/config.example.json
```

The server accepts authenticated `POST /tool` requests:

```http
Authorization: Bearer change-me
Content-Type: application/json
```

## LAN Client Examples

Health:

```json
{
  "tool": "runtime_health",
  "arguments": {}
}
```

List allowlisted images:

```json
{
  "tool": "list_runtime_images",
  "arguments": {}
}
```

Build an allowlisted runtime image:

```json
{
  "tool": "build_runtime_image",
  "arguments": {
    "image": "metbench-sut:latest",
    "timeout_seconds": 120
  }
}
```

Run a SUT command:

```json
{
  "tool": "run_sut_command",
  "arguments": {
    "image": "metbench-sut:latest",
    "argv": ["python", "SUT/demo.py"],
    "timeout_seconds": 60
  }
}
```

The response contains a service-generated `run_id`.

Read a stored run result:

```json
{
  "tool": "get_run_result",
  "arguments": {
    "run_id": "<run_id returned by run_sut_command>"
  }
}
```

`run_sut_command` deduplicates `[repo_root, *allowed_mount_roots]` and mounts
each entry at its translated container target: Windows-style sources
(`X:\path\...`) become `/mnt/x/path/...` inside the container; POSIX paths are
passed through unchanged. `/tmp:/tmp` is only added on non-Windows hosts.  The
container working directory is set to the translated `repo_root`. The generated
`docker run` command does not use privileged mode or host networking.

## MetBench Runtime Backend

MetBench activates this backend through `LauncherOptions.RuntimePythons`.
Configure a manifest runtime key with a `docker-mcp://` URI:

```bash
rtk python3 tools/metbench-docker-runtime-mcp profile-uri \
  --runtime-key openmoc-docker \
  --endpoint http://192.168.1.20:8765 \
  --image metbench-sut:latest \
  --python /opt/openmoc-venv/bin/python \
  --auth-token-env METBENCH_DOCKER_MCP_TOKEN
```

```csharp
RuntimePythons = new Dictionary<string, string>
{
    ["openmoc-docker"] =
        "docker-mcp://openmoc-docker"
        + "?image=metbench-sut:latest"
        + "&python=/opt/openmoc-venv/bin/python"
        + "&endpoint=http%3A%2F%2F192.168.1.20%3A8765"
        + "&authTokenEnv=METBENCH_DOCKER_MCP_TOKEN",
}
```

The URI host must match the manifest runtime key. The endpoint must be
`http` or `https`; the auth token is read from the named environment variable.
When a matching MR catalog entry uses `RuntimeKey = "openmoc-docker"`, launcher
preflight calls `runtime_health`, and SUT runner commands are executed via
`run_sut_command`. Parser and adapter commands remain local MetBench processes.

## Backends

The `backend` config field selects how `run_sut_command` executes the requested
command:

- **`"docker"` (default)** — runs allowlisted images via `docker run`.  Each
  entry in `allowed_images` must supply `dockerfile` and `context` so
  `build_runtime_image` can build it.
- **`"local"`** — executes the `argv` directly in the server's own process
  environment (no Docker involved).  The `image` argument is still validated
  against `allowed_images`, but entries may omit `dockerfile` and `context`.
  `build_runtime_image` returns an explicit error when the backend is `"local"`.
  The local backend executes commands with the server process's own privileges on
  the host — any holder of the Bearer token can run arbitrary argv (the image key
  is an allowlist label, not a sandbox); deploy only on trusted LANs with a
  strong token.

`allowed_mount_roots` entries should use consistent path casing and separators;
deduplication against `repo_root` is case-sensitive.

### Acceptance deployment startup commands

Copy the relevant example, drop the `.example` suffix, and set a real
`repo_root` and `auth_token` before starting:

```
# Case 1 – local backend on Windows (port 8764)
python infra/mcp/docker-runtime/server.py infra/mcp/docker-runtime/config.local-win.json

# Case 2 – docker backend on Windows via Docker Desktop (port 8765)
python infra/mcp/docker-runtime/server.py infra/mcp/docker-runtime/config.docker-win.json

# Case 3 – local backend inside WSL simulating a remote Linux server (port 8766)
python3 infra/mcp/docker-runtime/server.py infra/mcp/docker-runtime/config.local-wsl.json
```

The equivalent `serve --config <path>` subcommand form works for each case as
well.

## MetBench UI

MetBench loads `appsettings.local.json` at startup and reads
`LauncherOptions:RuntimePythons`. The WPF client includes a System MT
`Runtime Environments` page for Docker MCP profiles. Fill:

- Runtime key: the manifest `python_executable_kind` / runtime key, such as
  `openmoc-docker` or `docker-linux`.
- Endpoint: `http://<LAN-IP>:8765`.
- Image: an image allowlisted by the MCP server config.
- Python executable: the interpreter path inside the container.
- Auth token env: optional environment variable name that stores the Bearer
  token on the MetBench client machine.

Saving writes the generated `docker-mcp://` value to:

```json
{
  "LauncherOptions": {
    "RuntimePythons": {
      "docker-linux": "docker-mcp://docker-linux?image=..."
    }
  }
}
```

Restart MetBench after saving so the launcher singleton reads the updated
configuration.
