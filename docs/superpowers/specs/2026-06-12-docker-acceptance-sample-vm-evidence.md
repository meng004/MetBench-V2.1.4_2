# Docker Acceptance Sample Evidence - 2026-06-12

Branch: `codex/external-mr-batch-e-runtime`

Head: `2413fed` plus local sample test change.

## Sample

The Docker acceptance sample is `docker-acceptance-python-stdlib`.

It uses a dependency-light image and command so that future executor work can
validate the Docker path without pulling the MeshGraphNets / Torch runtime:

- backend key: `docker-acceptance-python-stdlib`
- image: `python:3.12-slim`
- command template:
  `python /workspace/run_identity.py --input /workspace/in/source.json --output /workspace/out/followup.json`
- work directory: `/workspace`
- input mount: `docker-acceptance/in` -> `/workspace/in`
- output mount: `docker-acceptance/out` -> `/workspace/out`
- secret reference: `METBENCH_DOCKER_SAMPLE_TOKEN` -> `configured-by-operator`
- timeout: 5 minutes

## MetBench-Side Acceptance

The sample is represented by
`MetBench_SystemMT.Tests/SystemMT/Runtime/DockerAcceptanceSampleTests.cs`.

Accepted behavior for the current build:

- the typed Docker backend configuration validates required fields;
- `SystemMtJobService` resolves the backend key before queueing;
- the queued record stores only safe backend display fields:
  `BackendKind=docker`, `BackendExternalId=docker-acceptance-python-stdlib`;
- sanitized diagnostics expose the image and secret reference name, not raw
  secret values;
- `SystemMtAsyncPipeline` fails closed with `MiddlewareUnavailable` before
  invoking the launcher because no production Docker executor exists yet.

## Execution

Command:

```powershell
dotnet test MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj --no-restore --filter FullyQualifiedName~DockerAcceptanceSampleTests --logger "console;verbosity=minimal"
```

Result:

- `1/1` passed
- no skips
- no failures

Docker CLI check:

```powershell
docker --version
```

Result:

- blocked by VM environment: `docker` is not recognized as a command.

## Conclusion

This is a Docker acceptance sample for MetBench's configuration and async job
boundary. It is not evidence of real Docker SUT execution. Real Docker
acceptance remains blocked until this VM has Docker installed and MetBench has a
production Docker executor with artifact staging, output retrieval, cancellation,
status polling, and runtime evidence.
