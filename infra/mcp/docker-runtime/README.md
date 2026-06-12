# MetBench Docker Runtime MCP

Minimal JSON-over-HTTP runtime bridge for LAN clients that need to build
allowlisted Docker images and run SUT commands inside those images.

## Start

Edit `config.example.json` or provide an equivalent config file. The default
`bind_host` value is `auto-private-ipv4`; the server resolves it at startup to
the first private, non-loopback IPv4 address it can see on the host.

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
