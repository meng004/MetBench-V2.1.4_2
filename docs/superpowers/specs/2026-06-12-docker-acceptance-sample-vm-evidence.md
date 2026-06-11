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

## Docker Desktop Install Attempt

Command:

```powershell
winget install --id Docker.DockerDesktop --exact --accept-package-agreements --accept-source-agreements --silent
```

Result:

- installed Docker Desktop `4.77.0`;
- install location: `C:\Program Files\Docker\Docker`;
- Docker CLI binary is present at
  `C:\Program Files\Docker\Docker\resources\bin\docker.exe`;
- direct client version check returns `Docker version 29.5.3, build d1c06ef`.

Post-install daemon checks:

```powershell
Get-Service com.docker.service
docker info --format '{{json .ServerVersion}}'
```

Result:

- `com.docker.service` remains `Stopped`;
- `docker info` / `docker version` with server access timed out;
- current user `CCF8\limeng` is listed in local group `docker-users`, but the
  current login token from `whoami /groups` does not yet contain that group;
- `BUILTIN\Administrators` is present as `deny only`, so this shell cannot start
  `com.docker.service`;
- `wsl --status` still reports WSL is not installed; `wsl --install` did not
  complete in this session.

Current blocker:

- Docker Desktop is installed, but real container execution is blocked until the
  VM session is restarted or the user logs out/in so `docker-users` membership
  is applied, and Docker Desktop/WSL can initialize the Linux engine.

## Conclusion

This is a Docker acceptance sample for MetBench's configuration and async job
boundary. It is not evidence of real Docker SUT execution. Real Docker
acceptance remains blocked until this VM has a running Docker daemon and
MetBench has a production Docker executor with artifact staging, output
retrieval, cancellation, status polling, and runtime evidence.
