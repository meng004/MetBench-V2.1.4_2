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

`run_sut_command` mounts `<repo_root>:<repo_root>` and `/tmp:/tmp`, sets the
container working directory to `<repo_root>`, and rejects non-allowlisted
images. The generated `docker run` command does not use privileged mode or host
networking.

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
