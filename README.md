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
| v2 system-MT services (cross-platform) | Anomaly · Discovery · Validation · Mutation · Coverage · Reporting | `MetBench_BLL.Core/` (sub-namespaces) |
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
| **OpenMOC** (2D pin-cell neutron transport, deterministic MOC) | Production-grade case | `ScaleNuSigmaF` (k_eff increases), `ScaleFuelSigmaA` (k_eff decreases) | Python |
| **OpenMC** (2D pin-cell neutron transport, Monte Carlo) | Cross-implementation `m_cmp` partner of OpenMOC | Same 2 MR transformations as OpenMOC (cross-program scenario instances) | Python |
| **1D heat equation** (finite-difference solver) | Validates abstraction transfers beyond neutron transport | `ScaleAmplitude` (linearity) | Python (stdlib only) |
| **Projectile** | Closed-loop demo SUT | trivial range MR | Python |

详见 [`docs/PROJECT-STRUCTURE.md`](docs/PROJECT-STRUCTURE.md) §2 §3（含当前 SUT inventory、系统级 BDD 测试矩阵与 Launcher MR 注册映射）。

## Build and run tests

Requires **.NET 8 SDK**.

```bash
dotnet build MetBench.sln
dotnet test MetBench_SystemMT.Tests
```

The OpenMOC and OpenMC BDD scenarios + smoke tests require working
Python venvs with the respective package importable. They detect availability
via `OpenMocTestPaths` / `OpenMcTestPaths` and **skip cleanly** when the SUT
is not available. All other tests run on a plain .NET 8 install with no extra
setup.

To exercise the OpenMOC + OpenMC tests locally, see `.claude/web-setup.sh`
for the Linux install path that has been verified end-to-end (cmake +
source build for OpenMC; SWIG build for OpenMOC).

## Continuous integration

Every push to `main` and every pull request runs
`dotnet test MetBench_SystemMT.Tests` on `ubuntu-24.04` via
`.github/workflows/dotnet-test.yml`. OpenMOC, OpenMC, SciPy, and other
environment-sensitive paths skip cleanly when the matching runtime is not
configured. The current pass / skip baseline is maintained in the
[current status ledger](docs/status/current.md), while CI also enforces a
120-second performance budget through `tools/ci_perf_baseline.py`.

A second workflow `.github/workflows/f11-monthly-monitor.yml` runs on a
monthly cron (`17 3 1 * *` UTC) to poll OpenMOC upstream for adjoint-flux
export commits (F11 m_adj path A; auto-files a tracking issue when hit).

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

- 📘 [`docs/PROJECT-STRUCTURE.md`](docs/PROJECT-STRUCTURE.md) — 项目结构 / SUT 测试矩阵 / MetBench 框架测试覆盖 一目了然
- 🧭 [`AGENTS.md`](AGENTS.md) — 分阶段 roadmap
- 🤖 [`CLAUDE.md`](CLAUDE.md) — AI agent / 协作者非显然约定
- 🗒 [`docs/superpowers/plans/`](docs/superpowers/plans/) — per-stage 实现计划 + RFC

Current state at the time of this README:

- **Stage 1** (BDD-driven system-level MT closed loop): landed
- **Stage 2** (input data generation and follow-up derivation): landed
- **Stage 3** (OpenMOC single-program application, with two MRs in opposite directions): landed
- **Stage 4** (platform features, persistence, reporting, second SUT): landed
- **Stage 5 Phase 1** (mutation-based empirical validation of the MR suite): landed
- **Stage 6** (v2 development P1-P8 cloud-side): landed
- **Stage 7** (W11-W12: Multi-LLM consensus 真实跑通 / OpenMC 第 3 SUT 接入 / UAT 47 用例 markdown + 21 用例 BDD / scenario→MR 命名统一 / LiteDB schema migration / F11 月度监控): landed 2026-05-17，baseline-2026-05-17 作为 release-v2.1.0 historical reference；当前绿基线见 [`docs/status/current.md`](docs/status/current.md)

剩余前置：Windows 端跑过 1 轮 UAT round-1（**21 个 WPF UI 用例** A1-A7 + B1-B9 + E1-E5；其余 5 个 CLI 用例 A8/D1/D2/E6/E7 已由 cloud baseline 覆盖，参 [windows-uat-round-1.md](docs/uat/runbooks/windows-uat-round-1.md)）→ tag `release-v2.1.0`。

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
