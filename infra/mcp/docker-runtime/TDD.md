# Task 1 TDD Evidence

## Initial Task 1 Cycle

RED command:

```bash
rtk python3 -m unittest discover infra/mcp/docker-runtime/tests
```

RED observed summary:

- `FAILED (errors=6)`
- All six tests errored because `infra/mcp/docker-runtime/server.py` was missing.

GREEN command:

```bash
rtk python3 -m unittest discover infra/mcp/docker-runtime/tests
```

GREEN observed summary:

- `Ran 6 tests in 0.322s`
- `OK`

## Spec Review Fix Cycle

RED command:

```bash
rtk python3 -m unittest discover infra/mcp/docker-runtime/tests
```

RED observed summary:

- `Ran 8 tests in 0.008s`
- `FAILED (errors=5)`
- Failures showed the old model shape: `config.example.json` lacked `bind_port`, `load_config` still read `port`, and `ImageConfig` rejected `dockerfile` / `context`.

GREEN command:

```bash
rtk python3 -m unittest discover infra/mcp/docker-runtime/tests
```

GREEN observed summary:

- `Ran 8 tests in 0.008s`
- `OK`

## Code Quality Fix Cycle

RED command:

```bash
rtk python3 -m unittest discover infra/mcp/docker-runtime/tests
```

RED observed summary:

- `Ran 13 tests in 0.019s`
- `FAILED (failures=27, errors=3)`
- Failures showed missing validation for null or blank required strings, malformed
  `allowed_images`, malformed `allowed_mount_roots`, invalid numeric ranges, and
  invalid `argv` shapes.

GREEN command:

```bash
rtk python3 -m unittest discover infra/mcp/docker-runtime/tests
```

GREEN observed summary:

- `Ran 13 tests in 0.017s`
- `OK`

# Task 2 TDD Evidence

## Auth, Docker Command, and Dispatch Flow Cycle

RED command:

```bash
rtk python3 -m unittest discover infra/mcp/docker-runtime/tests
```

RED observed summary:

- `Ran 17 tests in 0.103s`
- `FAILED (errors=4)`
- Errors showed Task 2 API was missing: `authorize`,
  `build_docker_run_command`, and `dispatch_tool`.

GREEN command:

```bash
rtk python3 -m unittest discover infra/mcp/docker-runtime/tests
```

GREEN observed summary:

- `Ran 17 tests in 4.211s`
- `OK`

## Code Review Fix Cycle

RED command:

```bash
rtk python3 -m unittest discover infra/mcp/docker-runtime/tests
```

RED observed summary:

- `Ran 22 tests in 0.028s`
- `FAILED (errors=4)`
- Failures showed `dispatch_tool` did not accept an injected `id_factory`, and
  the HTTP handler factory for malformed body testing was missing.
- After avoiding sandbox-blocked socket binding in the HTTP tests, the targeted
  RED was `Ran 22 tests in 0.046s`, `FAILED (errors=3)` for missing
  `handle_http_tool_request`.

GREEN command:

```bash
rtk python3 -m unittest discover infra/mcp/docker-runtime/tests
```

GREEN observed summary:

- `Ran 22 tests in 0.046s`
- `OK`
