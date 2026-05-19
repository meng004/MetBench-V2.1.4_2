# MetBench SUT runtime — Linux-native OpenMOC + OpenMC in a single image.
#
# Mirrors `.claude/web-setup.sh` (Ubuntu 24.04 + Python 3.12 + system-site-
# packages venvs). Two venvs ship side-by-side so one image serves both SUTs:
#
#     /opt/openmoc-venv/bin/python   →  import openmoc
#     /opt/openmc-venv/bin/python    →  import openmc       (+ openmc binary)
#
# Two build targets:
#
#     # SUT-only image (the python venvs, no .NET):
#     docker build -t metbench-sut:latest docker/
#
#     # SUT + .NET 8 SDK so dotnet test can run *inside* the container with
#     # no host prerequisites (no WSL2, no host dotnet install):
#     docker build -t metbench-runtime:latest --target sdk docker/
#
# Wire the SUT image into a host-side .NET launcher (Windows/macOS/Linux):
#
#     METBENCH_OPENMOC_PYTHON="<repo>/docker/wrappers/openmoc-docker.sh"
#     METBENCH_OPENMC_PYTHON="<repo>/docker/wrappers/openmc-docker.sh"
#
# Wire the runtime image for fully-containerised execution (preferred):
#
#     docker run --rm -v "${PWD}:/work" -w /work \
#         -e METBENCH_OPENMOC_PYTHON=/opt/openmoc-venv/bin/python \
#         -e METBENCH_OPENMC_PYTHON=/opt/openmc-venv/bin/python \
#         metbench-runtime:latest dotnet test ...
#
# Build args:
#     OPENMOC_REF  mit-crpg/OpenMOC branch       (default: 3D-MOC)
#     OPENMC_REF   openmc-dev/openmc branch      (default: master)
# =============================================================================

ARG OPENMOC_REF=3D-MOC
ARG OPENMC_REF=master


# -----------------------------------------------------------------------------
# Stage 1 — builder: compiles OpenMOC SWIG extension + OpenMC C++ binary.
# Throws away build-essential / cmake / swig / *-dev / git afterwards by not
# being part of the final image.
# -----------------------------------------------------------------------------
FROM ubuntu:24.04 AS builder

ARG OPENMOC_REF
ARG OPENMC_REF

ENV DEBIAN_FRONTEND=noninteractive \
    OPENMOC_VENV=/opt/openmoc-venv \
    OPENMC_VENV=/opt/openmc-venv \
    OPENMC_PREFIX=/opt/openmc

RUN apt-get update -qq \
 && apt-get install -y --no-install-recommends \
        build-essential cmake pkg-config git ca-certificates curl \
        swig \
        libhdf5-dev libhdf5-serial-dev libgomp1 \
        libpng-dev libxml2-dev libeigen3-dev \
        python3.12 python3.12-dev python3.12-venv \
        python3-numpy python3-h5py python3-matplotlib python3-pandas python3-scipy \
 && rm -rf /var/lib/apt/lists/*

# --- OpenMOC ----------------------------------------------------------------
# Python 3.12 venv with system-site-packages so apt numpy/h5py/matplotlib are
# reused without rebuilding. setup.py build_ext --inplace generates the SWIG
# extension; pip install . only ships pure-Python, so _openmoc.so + openmoc.py
# are copied into site-packages explicitly (same workaround as web-setup.sh).
RUN python3.12 -m venv --system-site-packages "$OPENMOC_VENV" \
 && "$OPENMOC_VENV/bin/pip" install --no-cache-dir --upgrade pip setuptools wheel

RUN git clone --depth=1 --branch "$OPENMOC_REF" \
        https://github.com/mit-crpg/OpenMOC.git /tmp/OpenMOC \
 && for f in /tmp/OpenMOC/openmoc/checkvalue.py \
             /tmp/OpenMOC/openmoc/plotter.py \
             /tmp/OpenMOC/openmoc/process.py; do \
        [ -f "$f" ] && sed -i \
            's/from collections import Iterable/from collections.abc import Iterable/g' \
            "$f"; \
    done \
 && cd /tmp/OpenMOC \
 && "$OPENMOC_VENV/bin/python" setup.py build_ext --inplace \
 && "$OPENMOC_VENV/bin/pip" install --no-cache-dir --no-build-isolation . \
 && cp /tmp/OpenMOC/build/lib/_openmoc.cpython-312-x86_64-linux-gnu.so \
       "$OPENMOC_VENV/lib/python3.12/site-packages/openmoc/" \
 && cp /tmp/OpenMOC/openmoc/openmoc.py \
       "$OPENMOC_VENV/lib/python3.12/site-packages/openmoc/openmoc.py" \
 && cd / \
 && "$OPENMOC_VENV/bin/python" -c "import openmoc; print('openmoc OK')" \
 && rm -rf /tmp/OpenMOC

# --- OpenMC -----------------------------------------------------------------
# Separate venv so the two scientific stacks do not collide. C++ binary lands
# at /opt/openmc, Python bindings into the venv; openmc binary is symlinked
# into the venv bin/ for openmc_runner.py's locate-by-sys.executable logic.
RUN python3.12 -m venv --system-site-packages "$OPENMC_VENV" \
 && "$OPENMC_VENV/bin/pip" install --no-cache-dir --upgrade pip setuptools wheel

RUN git clone --depth=1 --branch "$OPENMC_REF" --recurse-submodules \
        https://github.com/openmc-dev/openmc.git /tmp/openmc-src \
 && mkdir -p /tmp/openmc-src/build \
 && cd /tmp/openmc-src/build \
 && cmake -DCMAKE_INSTALL_PREFIX="$OPENMC_PREFIX" \
          -DCMAKE_BUILD_TYPE=Release \
          -DOPENMC_USE_MPI=OFF \
          -DOPENMC_USE_OPENMP=ON \
          -DOPENMC_USE_DAGMC=OFF \
          -DOPENMC_USE_LIBMESH=OFF \
          .. \
 && make -j"$(nproc)" \
 && make install \
 && cd /tmp/openmc-src \
 && "$OPENMC_VENV/bin/pip" install --no-cache-dir --no-build-isolation . \
 && ln -sf "$OPENMC_PREFIX/bin/openmc" "$OPENMC_VENV/bin/openmc" \
 && cd / \
 && "$OPENMC_VENV/bin/python" -c "import openmc; print('openmc OK', openmc.__version__)" \
 && "$OPENMC_VENV/bin/openmc" --version \
 && rm -rf /tmp/openmc-src


# -----------------------------------------------------------------------------
# Stage 2 — runtime: minimal Ubuntu 24.04 with only the shared libraries the
# venvs' compiled extensions link against, plus Python 3.12 + apt scientific
# packages (numpy/h5py/etc. — system-site-packages of both venvs depends on
# these existing here, not in the builder).
# -----------------------------------------------------------------------------
FROM ubuntu:24.04 AS runtime

ENV DEBIAN_FRONTEND=noninteractive

RUN apt-get update -qq \
 && apt-get install -y --no-install-recommends \
        libhdf5-103-1t64 libhdf5-cpp-103-1t64 \
        libhdf5-hl-100t64 libhdf5-hl-cpp-100t64 \
        libgomp1 libpng16-16t64 libxml2 \
        python3.12 python3.12-venv \
        python3-numpy python3-h5py python3-matplotlib python3-pandas python3-scipy \
        ca-certificates \
 && rm -rf /var/lib/apt/lists/* \
 && useradd -ms /bin/bash mt

COPY --from=builder /opt/openmoc-venv /opt/openmoc-venv
COPY --from=builder /opt/openmc-venv  /opt/openmc-venv
COPY --from=builder /opt/openmc       /opt/openmc

# COPY collapses dangling symlinks; re-create the binary link explicitly.
RUN ln -sf /opt/openmc/bin/openmc /opt/openmc-venv/bin/openmc

# Smoke-test both stacks at build time so a broken image fails the build, not
# the first user invocation.
RUN /opt/openmoc-venv/bin/python -c "import openmoc; print('runtime openmoc OK')" \
 && /opt/openmc-venv/bin/python  -c "import openmc;  print('runtime openmc OK', openmc.__version__)" \
 && /opt/openmc-venv/bin/openmc --version

USER mt
WORKDIR /work

# No ENTRYPOINT: the caller passes the full python+script command line, e.g.
#     docker run --rm -v $PWD/SUT:/work/SUT metbench-sut:latest \
#         /opt/openmoc-venv/bin/python /work/SUT/openmoc/openmoc_runner.py \
#         --input /work/SUT/openmoc/sample/pincell.json --output /tmp/out.json
#
# For an all-in-one image that *also* contains the .NET 8 SDK (so dotnet
# test can run inside the container with no host prerequisites), build the
# sibling `docker/Dockerfile.runtime` which extends this image.
