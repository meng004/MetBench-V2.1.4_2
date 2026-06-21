# MetBench Business MCP

Agent-facing System MT control-plane MCP adapter.

This server exposes business job operations and forwards them to the REST API:

- `submit_run`
- `get_job`
- `get_result`
- `get_evidence`
- `cancel_job`
- `business_health`

It must not call Docker Runtime MCP, runtime executors, or host filesystem paths directly.
