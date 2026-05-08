#!/usr/bin/env bash
# Claude Code on web setup for MetBench Stage 3 (OpenMOC + .NET 8 + Python 3.12).
#
# This script bootstraps a cloud Linux session into a state where the
# MetBench .NET tests can run and OpenMOC is importable from a dedicated
# Python venv. It is the same toolchain a Stage 3 cloud session needs to
# reproduce the OpenMOC pin-cell MR end-to-end.
#
# Idempotent: re-running is a no-op for already-installed components.
# Approximate cost: 5-10 min on first run, < 30 s on subsequent runs.
#
# Cloud-sandbox notes:
#
#   - dot.net / dotnetcli.azureedge.net / aka.ms return 403 in the current
#     Claude Code on web sandbox. The script installs .NET via the
#     packages.microsoft.com apt repo instead. That repo only ships
#     dotnet-sdk-8.0 and 10.0 (no 9.0), and the codebase targets `net8.0`,
#     so 8.0 is sufficient. DOTNET_CHANNEL is honoured but only "8.0" and
#     "10.0" are reachable from the sandbox.
#   - cli.github.com returns 403 too; gh ships in the Ubuntu universe
#     repo (apt install gh) and that's used here.
#   - Default `python3` on the image is Python 3.11, but the apt
#     scientific stack (python3-numpy, python3-h5py, python3-matplotlib)
#     is built for Python 3.12. The OpenMOC venv is therefore created on
#     /usr/bin/python3.12 with --system-site-packages so it picks up the
#     matching numpy/h5py/matplotlib without rebuilding them from source.
#   - OpenMOC's setup.py does not embed the SWIG `_openmoc.so` extension
#     into a `pip install .` wheel; the script copies _openmoc*.so and
#     openmoc/openmoc.py from the build tree into the venv site-packages
#     after install. This is the same workaround the upstream Dockerfile
#     achieves by running `setup.py install` twice.
#   - OpenMOC's Python sources still use `from collections import
#     Iterable`, removed in Python 3.10. The script patches the three
#     affected files (checkvalue.py, plotter.py, process.py) before the
#     install step.
#
# Environment variables this script honors (all optional):
#   DOTNET_CHANNEL        default 8.0  (apt repo only ships 8.0 and 10.0)
#   OPENMOC_REF           default 3D-MOC (mit-crpg/OpenMOC branch)
#   OPENMOC_VENV          default /opt/openmoc-venv
#   SKIP_OPENMOC          set to 1 to skip the heavy OpenMOC compile

set -euo pipefail

DOTNET_CHANNEL="${DOTNET_CHANNEL:-8.0}"
OPENMOC_REF="${OPENMOC_REF:-3D-MOC}"
OPENMOC_VENV="${OPENMOC_VENV:-/opt/openmoc-venv}"

log() { printf '\n[setup] %s\n' "$*"; }

# ---------------------------------------------------------------------------
# 1. Apt-installed scientific stack and SWIG (matches the upstream OpenMOC
#    Dockerfile's choices and avoids pip/apt numpy ABI mixing).
#
#    python3.12 is requested explicitly (separate from the system python3)
#    because the Ubuntu 24.04 scientific Python packages target 3.12.
# ---------------------------------------------------------------------------
log "Installing apt packages (build tools, swig, hdf5, python3.12 + scientific stack)"
sudo apt-get update -qq
sudo DEBIAN_FRONTEND=noninteractive apt-get install -y --no-install-recommends \
    build-essential cmake pkg-config ca-certificates curl gnupg \
    swig \
    libhdf5-dev libhdf5-serial-dev libgomp1 \
    python3.12 python3.12-dev python3.12-venv \
    python3 python3-dev python3-pip python3-setuptools \
    python3-numpy python3-h5py python3-matplotlib

# ---------------------------------------------------------------------------
# 2. .NET SDK via the Microsoft apt repo. The dot.net install script
#    redirects to a CDN that is blocked from this sandbox; the apt repo
#    is reachable.
# ---------------------------------------------------------------------------
DOTNET_PKG="dotnet-sdk-${DOTNET_CHANNEL}"
if ! command -v dotnet >/dev/null \
   || [ "$(dotnet --version 2>/dev/null | cut -d. -f1)" != "${DOTNET_CHANNEL%%.*}" ]; then
    log "Installing ${DOTNET_PKG} via packages.microsoft.com"
    if [ ! -f /etc/apt/sources.list.d/microsoft-prod.list ] \
       && [ ! -f /etc/apt/sources.list.d/microsoft-prod.sources ]; then
        TMPDEB="$(mktemp --suffix=.deb)"
        curl -fsSL -o "$TMPDEB" \
            "https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/packages-microsoft-prod.deb"
        sudo dpkg -i "$TMPDEB"
        rm -f "$TMPDEB"
        sudo apt-get update -qq
    fi
    sudo DEBIAN_FRONTEND=noninteractive apt-get install -y --no-install-recommends "${DOTNET_PKG}"
else
    log ".NET SDK already installed: $(dotnet --version)"
fi

# ---------------------------------------------------------------------------
# 3. gh CLI from the Ubuntu universe repo (cli.github.com is blocked).
# ---------------------------------------------------------------------------
if ! command -v gh >/dev/null; then
    log "Installing gh CLI from Ubuntu apt"
    sudo DEBIAN_FRONTEND=noninteractive apt-get install -y --no-install-recommends gh
fi

# ---------------------------------------------------------------------------
# 4. OpenMOC (heavy step; ~5 min). Installed into a dedicated venv so the
#    Debian-patched system setuptools (which the OpenMOC setup.py is not
#    compatible with for `setup.py install`) is not used.
# ---------------------------------------------------------------------------
if [ "${SKIP_OPENMOC:-0}" = "1" ]; then
    log "SKIP_OPENMOC=1 → skipping OpenMOC install"
elif "${OPENMOC_VENV}/bin/python" -c "import openmoc" >/dev/null 2>&1; then
    log "OpenMOC already importable from ${OPENMOC_VENV}; skipping rebuild"
else
    log "Creating Python 3.12 venv at ${OPENMOC_VENV} (with --system-site-packages)"
    sudo install -d -m 0755 -o "$(id -u)" -g "$(id -g)" "${OPENMOC_VENV%/*}" 2>/dev/null \
        || sudo install -d -m 0777 "${OPENMOC_VENV%/*}"
    if [ ! -x "${OPENMOC_VENV}/bin/python" ]; then
        sudo /usr/bin/python3.12 -m venv --system-site-packages "${OPENMOC_VENV}"
        sudo chown -R "$(id -u):$(id -g)" "${OPENMOC_VENV}"
    fi
    "${OPENMOC_VENV}/bin/pip" install --upgrade pip setuptools wheel >/dev/null

    log "Cloning OpenMOC (branch ${OPENMOC_REF})"
    rm -rf /tmp/OpenMOC
    git clone --depth=1 --branch "${OPENMOC_REF}" https://github.com/mit-crpg/OpenMOC.git /tmp/OpenMOC

    log "Patching 'from collections import Iterable' (removed in Python 3.10)"
    for f in /tmp/OpenMOC/openmoc/checkvalue.py \
             /tmp/OpenMOC/openmoc/plotter.py \
             /tmp/OpenMOC/openmoc/process.py; do
        if [ -f "$f" ]; then
            sed -i 's/from collections import Iterable/from collections.abc import Iterable/g' "$f"
        fi
    done

    log "Building _openmoc.so in-place and installing the Python package"
    (
        cd /tmp/OpenMOC
        # build_ext --inplace generates _openmoc.so + openmoc/openmoc.py
        # in the source tree.
        "${OPENMOC_VENV}/bin/python" setup.py build_ext --inplace >/dev/null
        # `pip install .` produces a pure-Python wheel that does NOT
        # include the C extension. We install it for the metadata + pure-
        # Python files, then copy _openmoc.so + openmoc.py manually.
        "${OPENMOC_VENV}/bin/pip" install --no-build-isolation . >/dev/null
    )

    SP="${OPENMOC_VENV}/lib/python3.12/site-packages/openmoc"
    log "Copying _openmoc.so and SWIG-generated openmoc.py into ${SP}"
    cp /tmp/OpenMOC/build/lib/_openmoc.cpython-312-x86_64-linux-gnu.so "${SP}/"
    cp /tmp/OpenMOC/openmoc/openmoc.py "${SP}/openmoc.py"

    rm -rf /tmp/OpenMOC

    log "Smoke-testing 'import openmoc' from ${OPENMOC_VENV}/bin/python"
    (cd /tmp && "${OPENMOC_VENV}/bin/python" -c "import openmoc; print('openmoc import OK')")
fi

# ---------------------------------------------------------------------------
# 5. Restore .NET dependencies for the SystemMT test project so subsequent
#    `dotnet test` calls do not pay the cold-start NuGet restore each time.
# ---------------------------------------------------------------------------
if [ -f MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj ]; then
    log "Pre-warming dotnet restore for MetBench_SystemMT.Tests"
    dotnet restore MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj >/dev/null
fi

log "Setup complete."
log "  dotnet --version : $(dotnet --version 2>/dev/null || echo 'not installed')"
log "  python3 --version: $(python3 --version)"
log "  python3.12       : $(/usr/bin/python3.12 --version 2>/dev/null || echo 'not installed')"
if [ -x "${OPENMOC_VENV}/bin/python" ] \
   && "${OPENMOC_VENV}/bin/python" -c "import openmoc" >/dev/null 2>&1; then
    log "  openmoc          : importable from ${OPENMOC_VENV}/bin/python"
else
    log "  openmoc          : not importable (run with SKIP_OPENMOC unset to build)"
fi
log "  gh               : $(gh --version 2>/dev/null | head -n1 || echo 'not installed')"
log ""
log "  To run Stage 3 tests against this venv:"
log "    METBENCH_OPENMOC_PYTHON=${OPENMOC_VENV}/bin/python \\"
log "        dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj"
