#!/usr/bin/env bash
# Single-executable wrapper that forwards `python <args>` into the OpenMOC
# venv inside the metbench-sut container. Set METBENCH_OPENMOC_PYTHON to the
# absolute path of this script so .NET's ProcessStartInfo (UseShellExecute=
# false) can exec it directly — multi-word `docker run …` does not work as
# an env var value because the test importability gate
# (MetBench_SystemMT.Tests/SystemMT/OpenMocTestPaths.cs) does not pass it
# through a shell.
#
# Env overrides:
#   METBENCH_SUT_IMAGE   container image, default metbench-sut:latest
#   METBENCH_HOST_REPO   host path mounted at the same path inside the
#                        container, default = repo root (two dirs up).
set -euo pipefail
IMAGE="${METBENCH_SUT_IMAGE:-metbench-sut:latest}"
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO="${METBENCH_HOST_REPO:-$(cd "$HERE/../.." && pwd)}"
exec docker run --rm --user "$(id -u):$(id -g)" \
    -v /tmp:/tmp -v "$REPO:$REPO" \
    "$IMAGE" /opt/openmoc-venv/bin/python "$@"
