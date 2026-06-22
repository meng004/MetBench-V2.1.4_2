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

## Public Vocabulary

Business MCP exposes `job` as the durable public resource. A job is the queued,
running, or terminal business record addressed by `job_id`.

`submit_run` is a command that creates a `RunMr` job and returns `job_id`; it is
not a separate `run` resource. Business MCP does not expose `workflow` as a
resource. In this project, workflow describes the internal MetBench orchestration
path, not an API/MCP object.

## Control Semantics

Business MCP is a business control-plane adapter. Its stop semantic is
`cancel_job`:

- `cancel_job` targets a MetBench job id.
- `cancel_job` forwards to the REST API cancel action, not a DELETE resource
  operation.
- `cancel_job` requests the durable job state to move to `Cancelled` when the
  job is queued or running.
- `cancel_job` is not a Runtime MCP process control tool and must not accept
  runtime `run_id`, process id, container id, host path, or raw command fields.

The runtime execution-plane stop semantic is `kill`, not `cancel`. `kill`
targets a runtime execution handle and belongs to Runtime MCP only. Business MCP
does not expose `kill_run`; if a job cancellation can be propagated to a
killable backend, that propagation is handled below the control-plane service
and recorded as runtime evidence.
