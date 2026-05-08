# Running MetBench on Claude Code (Web)

This document describes how to run MetBench work — primarily Stage 3
(OpenMOC integration) — in a Claude Code on web cloud session.

The cloud environment is a managed Linux VM (Ubuntu 24.04, 4 vCPU,
16 GB RAM, 30 GB disk). It has Python, Node, Java, Go, Rust, Git, and
Docker pre-installed. **It does not have the .NET SDK or OpenMOC
pre-installed**; `.claude/web-setup.sh` installs both on first run.

It does **not** support Windows-only work (WPF UI). Stage 4 must be
done on a Windows host.

## Quick start

1. Open https://claude.ai/code, start a new session, pick the
   `meng004/MetBench-V2.1.4_2` repository.
2. In the first turn of the session, paste the Stage 3 starter prompt
   from [`stage3-starter-prompt.md`](./stage3-starter-prompt.md).
3. The session will execute `bash .claude/web-setup.sh`, then proceed
   with Stage 3 brainstorming + planning + subagent-driven execution.

## Required project Secrets (Claude Code on web → Settings → Secrets)

| Name | Purpose |
|---|---|
| `GITHUB_TOKEN` | fine-grained PAT scoped to `meng004/MetBench-V2.1.4_2`. Permissions: `Contents: read+write`, `Pull requests: read+write`, `Issues: read`. **Do NOT** grant Workflows / Actions / Webhooks. |

Optional:

| Name | Purpose |
|---|---|
| `GIT_AUTHOR_NAME` / `GIT_AUTHOR_EMAIL` | Override default committer info if you do not want the default Claude identity. |

## Required project Environment variables

These can also be set inside `web-setup.sh` if a Secrets-only flow is
preferred; the script honours overrides and supplies sensible defaults.

| Name | Default | Purpose |
|---|---|---|
| `DOTNET_CHANNEL` | `9.0` | .NET SDK channel installed by the official `dotnet-install.sh`. |
| `DOTNET_ROOT` | `$HOME/.dotnet` | Where the SDK is installed. Must be on `PATH`. |
| `DOTNET_NOLOGO` | `1` | Suppress the SDK first-run banner. |
| `DOTNET_CLI_TELEMETRY_OPTOUT` | `1` | Opt out of telemetry. |
| `DOTNET_ROLL_FORWARD` | `LatestMajor` | Allow `net8.0` test host to roll forward to a newer runtime if only that is installed. |
| `OPENMOC_REF` | `3D-MOC` | The mit-crpg/OpenMOC branch to clone and build. Pin to a tag for reproducibility. |
| `PYTHONPATH` | `$HOME/.local/lib/python3.12/site-packages` | Make the user-site OpenMOC install discoverable from non-login shells. |
| `PIP_BREAK_SYSTEM_PACKAGES` | `1` | Allow `pip install` against the apt-managed system Python (only if `web-setup.sh` ever needs `pip install`; it currently uses apt for numpy/h5py/matplotlib). |
| `METBENCH_TEST_PYTHON` | `python3` | Used by `MetBench_SystemMT.Tests/SystemMT/TestAssetPaths.cs` to launch the example/projectile/OpenMOC adapters. |
| `OMP_NUM_THREADS` | `4` | Hint for OpenMP-built OpenMOC. Tune to the cloud VM. |
| `SKIP_OPENMOC` | (unset) | Set to `1` to skip the heavy OpenMOC compile (~5 min) when only the .NET workflow needs validation. |

## Network egress whitelist

Default Claude Code on web egress should already cover everything
below. If your organization restricts egress, request these:

```
github.com
api.github.com
codeload.github.com
objects.githubusercontent.com
raw.githubusercontent.com
dot.net
dotnet.microsoft.com
download.visualstudio.microsoft.com
builds.dotnet.microsoft.com
aka.ms
files.pythonhosted.org
pypi.org
deb.debian.org
archive.ubuntu.com
security.ubuntu.com
cli.github.com
```

The whitelist intentionally does **not** include analytics or social
domains. `web-setup.sh` will fail closed on any other host.

## What the setup script does

`bash .claude/web-setup.sh` is idempotent and roughly does:

1. `apt-get install` build-essential, cmake, swig, hdf5 dev headers,
   python3-numpy / python3-h5py / python3-matplotlib (matches the
   upstream OpenMOC Dockerfile to avoid pip/apt ABI mixing).
2. Install .NET SDK 9 via `dot.net/v1/dotnet-install.sh` if absent.
3. Clone OpenMOC at `$OPENMOC_REF`, run `setup.py build_ext --inplace`
   then `setup.py install --user`, smoke-test `import openmoc`.
4. Install gh CLI from the official Debian repo.
5. Pre-warm `dotnet restore` for `MetBench_SystemMT.Tests` so the
   first `dotnet test` call does not pay a cold start.

First run takes 5-10 minutes (OpenMOC compile dominates). Subsequent
runs are < 30 s.

## Verifying after setup

```bash
dotnet --version                # 9.0.x
python3 -c 'import openmoc; print("ok")'
gh --version                    # 2.x
dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj
```

The test command should report 25/25 (after Stage 2 merges) or 13/13
(if started against current `main` before Stage 2 merges).

## What this does not do

- Build or run any WPF project (`MetBench_Client`). The Linux runtime
  has no WPF; `EnableWindowsTargeting=true` lets the projects
  cross-compile to Windows targets but they cannot execute.
- Run the OpenMC adapter (Stage 3 will only target OpenMOC; OpenMC
  reuse is reserved for later stages per the spec).
- Acquire any nuclear data libraries. Stage 3 will pull whatever
  cross-section data is needed at task time, isolated from this setup.
