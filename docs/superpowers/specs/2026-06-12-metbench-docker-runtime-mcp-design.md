# MetBench Docker Runtime MCP Design

Date: 2026-06-12

## Scope

This design adds a LAN-accessible Docker runtime MCP bridge and wires MetBench
System MT to a Docker runtime backend without changing WPF UI surfaces.

## Phase A: MCP Server Infrastructure

- Server path: `infra/mcp/docker-runtime/server.py`.
- Transport: authenticated JSON-over-HTTP `POST /tool`.
- Default bind host: `auto-private-ipv4`, resolved at server startup to a
  private non-loopback IPv4 address.
- Tools:
  - `runtime_health`
  - `list_runtime_images`
  - `build_runtime_image`
  - `run_sut_command`
  - `get_run_result`
- Security boundary:
  - exact `Authorization: Bearer <token>` required;
  - only allowlisted images can be built or run;
  - run IDs are service-generated;
  - generated `docker run` uses no privileged mode and no host networking;
  - SUT arguments such as `--input` and `--output` are allowed because they are
    appended after the image and are not Docker daemon flags.

## Phase B: MetBench Docker Runtime Backend

MetBench activates the backend through `LauncherOptions.RuntimePythons`:

```csharp
RuntimePythons = new Dictionary<string, string>
{
    ["openmoc-docker"] =
        "docker-mcp://openmoc-docker"
        + "?image=metbench-sut:latest"
        + "&python=/opt/openmoc-venv/bin/python"
        + "&endpoint=http%3A%2F%2F192.168.1.20%3A8765"
        + "&authTokenEnv=METBENCH_DOCKER_MCP_TOKEN",
};
```

The URI host must match the runtime key. The profile parser fails closed for
missing `image`, missing `python`, missing or invalid `endpoint`, non-http(s)
endpoints, malformed percent-encoding, and runtime-key mismatch.

Runtime behavior:

- `RuntimeKind.Docker` stores `DockerMcpRuntimeOptions`.
- Preflight calls Docker MCP `runtime_health`; it does not probe the container
  Python executable path on the host.
- Launcher uses `DockerMcpRuntimeOptions.PythonExecutable` when building parser,
  output-parser, and runner commands.
- Pipeline routes only SUT runner commands through Docker MCP `run_sut_command`;
  parser and adapter commands stay local.
- Runtime evidence is still recorded through the existing runtime preflight
  evidence path.

## Windows Classification

No Windows evidence is required for this PR. The implementation touches
cloud-safe core, tests, docs, and Python infrastructure only. It does not change
WPF XAML, `App.xaml.cs`, Windows config binding, UI navigation, or VM runtime
surfaces.
