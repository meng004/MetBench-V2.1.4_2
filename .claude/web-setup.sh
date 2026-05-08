#!/usr/bin/env bash
# Claude Code on web setup for MetBench Stage 3 (OpenMOC + .NET 9 + Python 3.12).
#
# Idempotent: re-running is a no-op for already-installed components.
# Approximate cost: 5-10 min on first run, < 30 s on subsequent runs.
#
# Environment variables this script honors (all optional):
#   DOTNET_CHANNEL        default 9.0
#   OPENMOC_REF           default 3D-MOC (mit-crpg/OpenMOC branch)
#   SKIP_OPENMOC          set to 1 to skip the heavy OpenMOC compile

set -euo pipefail

DOTNET_CHANNEL="${DOTNET_CHANNEL:-9.0}"
OPENMOC_REF="${OPENMOC_REF:-3D-MOC}"
DOTNET_INSTALL_DIR="${DOTNET_ROOT:-$HOME/.dotnet}"

log() { printf '\n[setup] %s\n' "$*"; }

# ---------------------------------------------------------------------------
# 1. Apt-installed scientific stack and SWIG (matches the upstream OpenMOC
#    Dockerfile's choices and avoids pip/apt numpy ABI mixing).
# ---------------------------------------------------------------------------
log "Installing apt packages (build tools, swig, hdf5, python3-numpy/h5py/matplotlib)"
sudo apt-get update -qq
sudo DEBIAN_FRONTEND=noninteractive apt-get install -y --no-install-recommends \
    build-essential cmake pkg-config ca-certificates curl gnupg \
    swig \
    libhdf5-dev libhdf5-serial-dev libgomp1 \
    python3 python3-dev python3-pip python3-venv python3-setuptools \
    python3-numpy python3-h5py python3-matplotlib

# ---------------------------------------------------------------------------
# 2. .NET SDK 9 via the official install script (apt feed lags behind).
# ---------------------------------------------------------------------------
if ! command -v dotnet >/dev/null || [ "$(dotnet --version 2>/dev/null | cut -d. -f1)" != "9" ]; then
    log "Installing .NET SDK channel ${DOTNET_CHANNEL} into ${DOTNET_INSTALL_DIR}"
    curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
    bash /tmp/dotnet-install.sh --channel "${DOTNET_CHANNEL}" --install-dir "${DOTNET_INSTALL_DIR}"
    rm /tmp/dotnet-install.sh
else
    log ".NET SDK already installed: $(dotnet --version)"
fi

# ---------------------------------------------------------------------------
# 3. OpenMOC (heavy step; ~5 min). Skip with SKIP_OPENMOC=1 if you only need
#    .NET work and will install OpenMOC in a separate session.
# ---------------------------------------------------------------------------
if [ "${SKIP_OPENMOC:-0}" != "1" ]; then
    if python3 -c "import openmoc" 2>/dev/null; then
        log "OpenMOC already importable; skipping rebuild"
    else
        log "Cloning and installing OpenMOC (branch ${OPENMOC_REF})"
        cd /tmp
        rm -rf OpenMOC
        git clone --depth=1 --branch "${OPENMOC_REF}" https://github.com/mit-crpg/OpenMOC.git
        cd OpenMOC
        # build_ext --inplace first so the SWIG _openmoc.so is staged in the
        # source tree, then setup.py install copies both .so and the openmoc/
        # Python package to the same site-packages directory in one consistent
        # layer. Single-pass `setup.py install` leaves _openmoc unbuilt; the
        # canonical upstream Dockerfile achieves the same effect by running
        # setup.py install twice. build_ext --inplace is the explicit form.
        python3 setup.py build_ext --inplace
        python3 setup.py install --user
        cd /
        rm -rf /tmp/OpenMOC
        # Smoke-test from a directory that cannot shadow the installed package.
        cd /tmp && python3 -c "import openmoc; print('openmoc import OK')"
    fi
else
    log "SKIP_OPENMOC=1 → skipping OpenMOC install"
fi

# ---------------------------------------------------------------------------
# 4. gh CLI (for opening PRs from the cloud session).
# ---------------------------------------------------------------------------
if ! command -v gh >/dev/null; then
    log "Installing gh CLI"
    curl -fsSL https://cli.github.com/packages/githubcli-archive-keyring.gpg \
        | sudo gpg --dearmor -o /usr/share/keyrings/githubcli-archive-keyring.gpg
    echo "deb [arch=$(dpkg --print-architecture) signed-by=/usr/share/keyrings/githubcli-archive-keyring.gpg] https://cli.github.com/packages stable main" \
        | sudo tee /etc/apt/sources.list.d/github-cli.list > /dev/null
    sudo apt-get update -qq
    sudo DEBIAN_FRONTEND=noninteractive apt-get install -y --no-install-recommends gh
fi

# ---------------------------------------------------------------------------
# 5. Restore .NET dependencies for the SystemMT test project so subsequent
#    `dotnet test` calls do not pay the cold-start NuGet restore each time.
# ---------------------------------------------------------------------------
if [ -f MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj ]; then
    log "Pre-warming dotnet restore for MetBench_SystemMT.Tests"
    dotnet restore MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj
fi

log "Setup complete."
log "  dotnet --version : $(dotnet --version)"
log "  python3 --version: $(python3 --version)"
log "  openmoc          : $(python3 -c 'import openmoc; print(\"importable\")' 2>/dev/null || echo 'not installed')"
log "  gh               : $(gh --version 2>/dev/null | head -n1 || echo 'not installed')"
