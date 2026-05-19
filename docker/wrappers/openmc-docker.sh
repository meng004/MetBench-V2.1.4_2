#!/usr/bin/env bash
# Single-executable wrapper that forwards `python <args>` into the OpenMC
# venv inside the metbench-sut container. See openmoc-docker.sh for the
# rationale (importability gate cannot parse multi-word env var values).
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
    "$IMAGE" /opt/openmc-venv/bin/python "$@"
