# MetBench

A research and teaching tool for **system-level metamorphic testing (MT)** of
scientific computing programs.

MetBench extends classical method/unit-level metamorphic testing to
system/acceptance level: the System Under Test (SUT) is an external CLI
program (e.g. a neutron-transport solver), inputs and outputs are files, and
metamorphic relations (MRs) are expressed as Reqnroll/Gherkin BDD scenarios.
Source and follow-up cases are generated, executed end-to-end, and compared
through a typed assertion layer.

## Architecture

| Layer | Responsibility | Project / directory |
|------|----------------|---------------------|
| UI (Windows) | Configure, launch, and monitor MT tasks | `MetBench_Client/` (WPF) |
| Business orchestration (WPF) | Original method-level MT logic and DI composition | `MetBench_BLL/` |
| Business orchestration (cross-platform) | System-level MT runner, assertions, adapters, persistence contracts | `MetBench_BLL.Core/` |
| v2 system-MT services (cross-platform) | Anomaly · Discovery · Validation · Mutation · Coverage · Trend · Reporting | `MetBench_BLL.Core/` (sub-namespaces) |
| BDD execution | Reqnroll runs `.feature` files and dispatches to step bindings | `MetBench_SystemMT.Tests/` |
| Persistence (v1 + v2) | LiteDB-backed run-result + 23-collection v2 schema | `MetBench_DAL/` |
| Domain (v1 + v2) | Method-level entities + v2 4-level MR hierarchy entities | `MetBench_Domain/`, `MetBench_IDAL/` |
| Adapters | Per-program input/output file conversion (Python stdlib only) | `SUT/<program>/` |

Responsibility boundaries:

- WPF is the UI layer.
- C# BLL is the business orchestration layer.
- Reqnroll is the BDD execution layer.
- CLI runners invoke external programs under test.
- Python adapters handle program-specific input/output file conversion.
  Adapters do **not** own the test workflow — workflow control stays in C#
  and Reqnroll.

## Systems Under Test

| SUT | Role | MRs | Adapter language |
|-----|------|-----|------------------|
| **OpenMOC** (2D pin-cell neutron transport) | Production-grade case | `ScaleNuSigmaF` (k_eff increases), `ScaleFuelSigmaA` (k_eff decreases) | Python |
| **1D heat equation** (finite-difference solver) | Validates the abstraction transfers beyond OpenMOC | `ScaleAmplitude` (linearity) | Python (stdlib only) |
| **Projectile** | Closed-loop demo SUT | trivial range MR | Python |

## Build and run tests

Requires **.NET 8 SDK**.

```bash
dotnet build MetBench.sln
dotnet test MetBench_SystemMT.Tests
```

The OpenMOC BDD scenarios and the OpenMOC smoke test require a working
OpenMOC Python venv. They detect availability via `OpenMocTestPaths` and
**skip cleanly** when OpenMOC is not importable. All other tests run on a
plain .NET 8 install with no extra setup.

To exercise the OpenMOC tests locally, see `.claude/web-setup.sh` for the
Linux install path that has been verified end-to-end.

## Continuous integration

Every push to `main` and every pull request runs
`dotnet test MetBench_SystemMT.Tests` on `ubuntu-24.04` via
`.github/workflows/dotnet-test.yml`. OpenMOC is not built in CI; OpenMOC-
specific tests skip cleanly there. Cold runtime is around 25 seconds.

## Repository layout

```
MetBench_BLL.Core/       # cross-platform BLL: SystemMT runner, adapters, persistence
MetBench_BLL/            # WPF-side BLL (legacy method-level MT)
MetBench_Client/         # WPF UI
MetBench_DAL/            # LiteDB persistence
MetBench_Domain/         # legacy method-level domain entities
MetBench_IDAL/           # DAL contracts
MetBench_SystemMT.Tests/ # Reqnroll features, step bindings, unit + integration tests
SUT/                     # System-Under-Test programs and Python adapters
docs/                    # design specs and staged implementation plans
.github/workflows/       # GitHub Actions CI
```

## Roadmap and design docs

The staged plan is in [`AGENTS.md`](AGENTS.md). Current state at the time of
this README:

- **Stage 1** (BDD-driven system-level MT closed loop): landed.
- **Stage 2** (input data generation and follow-up derivation): landed.
- **Stage 3** (OpenMOC single-program application, with two MRs in opposite
  directions): landed.
- **Stage 4** (platform features, persistence, reporting, second SUT): landed
  (all six acceptance criteria closed via PRs #10–#23).
- **Stage 5 Phase 1** (mutation-based empirical validation of the MR suite):
  landed; see [`docs/experiments/`](docs/experiments/).

Per-stage implementation plans live under `docs/superpowers/plans/`.

## Experiments

Empirical results from the mutation-detection study live under
[`docs/experiments/`](docs/experiments/):

- [`mutation-catalogue.md`](docs/experiments/mutation-catalogue.md) — 28
  hand-built candidate mutations across OpenMOC + OpenMC runners and adapters.
- [`screening-results.md`](docs/experiments/screening-results.md) — baseline
  screening that filters equivalent mutants before the MR matrix is scored.
- [`mutation-detection-matrix.md`](docs/experiments/mutation-detection-matrix.md)
  — per-MR detection rate with Wilson 95% CI, cross-solver Cohen's κ, and
  threshold-sensitivity sweep.
- [`historical-bugs.md`](docs/experiments/historical-bugs.md) — three real
  upstream `openmc-dev/openmc` and `mit-crpg/OpenMOC` fix commits walked
  through against MetBench's current MR coverage.

The orchestrator lives in [`tools/mutation_study.py`](tools/mutation_study.py);
mutation patches in [`tools/mutations.py`](tools/mutations.py).

## Contributing

The development workflow is PR-driven:

1. Create a feature branch off `main`.
2. Commit changes with focused, well-described commits.
3. Open a pull request — the CI workflow gates merging.
4. Squash-merge on green CI.

Test-driven changes are preferred: add a failing test first, then make it
pass. Cross-platform code lives in `MetBench_BLL.Core/` and must compile on
Linux; WPF-only code lives in `MetBench_BLL/` and `MetBench_Client/` and is
edited from a Windows environment.

## License

[Apache License 2.0](LICENSE).
