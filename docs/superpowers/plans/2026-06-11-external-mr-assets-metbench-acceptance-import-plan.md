# External MR Assets MetBench Acceptance Import Plan (2026-06-11)

> **Status:** Batch A/D cloud acceptance path implemented; Windows/WPF UI
> acceptance evidence completed in VM on 2026-06-11.
> Cloud evidence:
> `docs/superpowers/specs/2026-06-11-external-mr-assets-batch-a-d-cloud-evidence.md`.
> VM evidence:
> `docs/superpowers/specs/2026-06-11-external-mr-assets-batch-a-d-vm-evidence/vm-summary.md`.
> **Scope:** Classify and batch-import experiment assets from
> `meng004/Minimum-MR-SubSet` and
> `meng004/Domain-Validity-Gated-MR-for-SciML` into MetBench acceptance testing
> packages.
> **Goal:** Execute MetBench acceptance tests around the MT main flow, asset
> import/export, visualization, anomaly, and report modules.
> **Truth boundary:** This plan is based on repository reads on 2026-06-11.
> `Minimum-MR-SubSet` remote `main` was
> `8944822054e18a1d80ec8c90f56a1214a8fd1665`;
> `Domain-Validity-Gated-MR-for-SciML` remote `main` was
> `c2956e8eb42b81b216a4cf31720c98e1e035f2e8`.

## 1. Objective

Create a staged MetBench acceptance-test asset stream from the two external
repositories. The final deliverables are:

- acceptance test plan;
- executable or imported-only test cases;
- test data inventory;
- explicit test conclusions and evidence boundaries;
- SUT import packages that can be staged through MetBench `ImportAssets` and
  round-tripped through `ExportAssets`.

The acceptance focus is MetBench as a platform, not broad scientific claims
about the imported programs. Claims about imported research results must retain
their original scope limits.

## 0. Batch A/D Execution Status (2026-06-11)

Cloud-side Batch A and D import-package fixtures are implemented in
`MetBench_BLL.Core/SystemMT/ImportExport/Put/ExternalMrAcceptancePutFixtures.cs`
with focused TDD coverage in
`MetBench_SystemMT.Tests/SystemMT/ImportExport/ExternalMrAcceptanceBatchImportTests.cs`.

Implemented packages:

- `metbench-import-minmr-toy-classic-v1`: 7 classic toy MRs over sorting,
  matrix multiplication, and quadratic roots. Runtime status remains
  imported-only until toy adapters and typed predicates are bound.
- `metbench-import-minmr-p1-heat-v1`: 10 P1 heat MRs, 5 mutation operator
  classes, and the full 50-row detection matrix. The fixture stays imported-only
  because Batch A has not yet bound the external P1 solver through the MetBench
  launcher/runtime adapter path.
- `metbench-import-sciml-domain-validity-fixture-v1`: 3 SciML domain-validity
  MR cards, 10 seeded-fault mutants, and a 30-row seeded-fault evidence matrix.
  Discrete divergence stays imported-only/deferred and may only appear as
  diagnostic evidence until an upstream calibrated threshold exists.

Evidence collected:

- Red phase: `dotnet test MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj
  --no-restore --filter FullyQualifiedName~ExternalMrAcceptanceBatchImportTests`
  failed with `CS0103` because `ExternalMrAcceptancePutFixtures` did not exist.
- Green phase: same command passed `7/7`.
- Regression: `dotnet test MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj
  --no-restore --filter FullyQualifiedName~SystemMT.ImportExport` passed
  `52/52`, skipped `3` external-source prerequisite-gated tests.

## 0.1 Batch A/D Cloud Completion Status (2026-06-11)

Additional cloud-side acceptance coverage now closes the non-WPF parts of
Batch A and D:

- Batch A import/export job path: `ImportAssetsJobOperationHandler` stages the
  toy and P1 packages and writes both `staging-manifest.json` and
  `sut-import-unit.json`; `ExportAssetsJobOperationHandler` round-trips the
  staged package back to a valid `sut-import-unit.json`.
- Batch A launcher path: the explicit acceptance catalog
  `SUT/external_acceptance_minmr/acceptance-catalog.json` binds one toy sorting MR and
  three P1 heat MRs to pure-stdlib local Python runners. These MRs execute
  through `SystemMtLauncher -> SystemMtPipeline -> SystemMtExecutionRecorder`
  without changing the global catalog whitelist.
- Batch A anomaly/evidence path: `ExternalMrAcceptanceEvidenceProjector`
  projects the P1 50-row detection matrix to 10 anomaly candidates while
  preserving the imported-research-evidence limitation.
- Batch D evidence/report path: the same projector renders the SciML
  30-row seeded-fault matrix, the 5 detected records, the
  one-SUT / one-checkpoint limitation, and the deferred/diagnostic status of
  `mgn-discrete-divergence-boundedness`.
- Batch A report/visualization path: the four local acceptance executions render
  through HTML, Word, Excel, and PDF report renderers, and every execution
  projects to a `BinaryRunPointProjector` chart DTO.

Implemented files:

- `SUT/external_acceptance_minmr/acceptance-catalog.json`
- `SUT/external_acceptance_minmr/external_acceptance_minmr.py`
- `SUT/external_acceptance_minmr/external_acceptance_minmr_input_parser.py`
- `SUT/external_acceptance_minmr/external_acceptance_minmr_output_parser.py`
- `SUT/external_acceptance_minmr/sample/toy_sort.json`
- `SUT/external_acceptance_minmr/sample/p1_heat.json`
- `MetBench_BLL.Core/SystemMT/ImportExport/Put/ExternalMrAcceptanceEvidenceProjector.cs`
- `MetBench_SystemMT.Tests/SystemMT/ImportExport/ExternalMrAcceptanceCompletionTests.cs`

Cloud verification:

- `dotnet test MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj
  --no-restore --filter FullyQualifiedName~ExternalMrAcceptanceCompletionTests`
  passed `7/7`.

Windows/WPF UI acceptance remains separate evidence, not cloud evidence. The
VM task prompt is registered at
`docs/superpowers/vm-prompts/2026-06-11-batch-a-d-external-mr-assets-ui-acceptance-vm-prompt.md`.
The cloud evidence report is registered at
`docs/superpowers/specs/2026-06-11-external-mr-assets-batch-a-d-cloud-evidence.md`.
The expected VM回执 file shape is registered at
`docs/superpowers/specs/2026-06-11-external-mr-assets-batch-a-d-vm-evidence-template.md`.

## 0.2 Batch A/D Windows/WPF VM Completion Status (2026-06-11)

VM execution completed on branch `codex/external-mr-asset-acceptance-plan` at
head `39ebb4efd1ad824fa02e23e4e817cf467bb4c47c` before the VM evidence commit.

VM verification:

- `dotnet build MetBench_Client\MetBench_Client.csproj --no-restore
  -v:minimal`: exit `0`, build errors `0`.
- `drive-batch-a-d-vm.ps1`: exit `0`.
- WPF `ImportAssets` succeeded for Batch A toy, Batch A P1 heat, and Batch D
  SciML packages; each staged artifact contains `staging-manifest.json` and
  `sut-import-unit.json`.
- WPF `RunBatch` succeeded for 4 Batch A runtime MRs:
  `minmr-toy-sort-permutation`, `minmr-p1-heat-alpha-monotonic`,
  `minmr-p1-heat-timestep-convergence`, and
  `minmr-p1-heat-mesh-convergence`; result summary was
  `total=4; passed=4; failed=0; cancelled=0; pending=0`.
- WPF `ExportAssets` round-tripped the staged Batch A toy, Batch A P1, and
  Batch D SciML packages to valid `sut-import-unit.json` files.
- WPF `ExportReport` generated `report.html` for the latest Batch A `minmr-*`
  execution.
- WPF result, coverage, and anomaly pages were opened and captured after the
  Batch A/D jobs.

The VM evidence directory is
`docs/superpowers/specs/2026-06-11-external-mr-assets-batch-a-d-vm-evidence/`.
It contains the reusable driver script, `vm-summary.md`, screenshots, and
import/export JSON packages needed to audit the run. Batch D remains
imported-only by design: the VM check verifies UI import/export visibility and
evidence preservation, not live MGN replay.

## 0.3 Batch E Docker Pilot Status (2026-06-12)

Batch E has a Docker pilot entry point and explicit test gate, but real Docker
execution is not yet green on this workstation.

Implemented:

- `RuntimeKind.DockerContainer` and `RuntimeProfile.DockerContainer(...)` model a
  Docker runtime as executable for preflight.
- Docker preflight probes run:
  `docker --version`,
  `docker image inspect <image>`, and
  `docker run --rm <image> <probe-command>`.
- `RuntimeDockerPilotTests.Configured_docker_runtime_image_runs_real_container_probe`
  executes a real container only when `METBENCH_DOCKER_RUNTIME_IMAGE` is set.
  Without that variable it skips with an explicit reason.
- `CreateBatchEScimlMgnRuntime()` creates the
  `metbench-import-sciml-mgn-runtime-v1` package shape for the MGN Batch E
  assets. It records Docker runtime metadata, checkpoint/dataset artifact
  references, three runtime MR cards, and seeded-fault evidence. It remains
  `ImportedOnly` because graph/tensor adapters, mounted artifact roots, and
  calibrated diagnostic thresholds are not bound yet.

Local evidence collected on 2026-06-12:

- `docker --version`: Docker CLI exists, `Docker version 29.5.3, build d1c06ef`.
- Initial sandboxed `docker images metbench-runtime ...`: failed because Docker
  daemon pipe was absent and Docker config read was denied.
- Escalated `docker images metbench-runtime ...`: failed with
  `npipe:////./pipe/dockerDesktopLinuxEngine ... The system cannot find the file specified`.
- After attempting to start Docker Desktop, Docker processes were present, but:
  - `docker info --format ...`: returned empty fields plus a Docker API `500`.
  - `docker version`: client `29.5.3`, API `1.54`, context `desktop-linux`, then
    Docker API `500` on `/v1.54/version`.
  - `docker ps` on both `desktop-linux` and `default`: failed or timed out with
    Docker API `500` on `/containers/json`.

Conclusion:

- Batch E Docker executor/preflight code is ready for a real image probe.
- This workstation has not produced a passing real Docker container run yet.
- Do not claim Batch E real SUT execution until
  `METBENCH_DOCKER_RUNTIME_IMAGE=<image>` plus the Docker pilot test passes and
  the command output is recorded.

## 2. Source Repositories

### 2.1 Minimum-MR-SubSet

Observed assets:

- `experiments/toy_put/classic_mt_catalog.json`: 7 classic MRs over sorting,
  matrix multiplication, and quadratic roots.
- `experiments/puts/p1..p10_*.py`: ten PUT adapters:
  P1 heat, P2 wave, P3 Lorenz, P4 pendulum, P5 point kinetics, P6 Poisson,
  P7 Burgers, P8 Schrodinger, P9 OpenMC surrogate, P10 PINN/HNN smoke.
- `data/raw/p1_heat/mrs.json`: 10 P1 heat MRs.
- `data/raw/p1_heat/detection_matrix.csv`: P1 detection matrix over
  `mut_C`, `mut_M`, `mut_G`, `mut_T`, and `mut_F`.
- `experiments/env/Dockerfile.mutator` and `docker-compose.yml`: mutation
  environment using cosmic-ray plus an ad-hoc mutator stub.

Existing MetBench overlap:

- `SUT/minimum_mr_subset_p3`
- `SUT/minimum_mr_subset_p4`
- `SUT/minimum_mr_subset_p5`
- `SUT/minimum_mr_subset_p8`
- `SUT/minimum_mr_subset_p9_surrogate`
- `MetBench_BLL.Core/SystemMT/ImportExport/Put/` already contains the staged
  PUT import/export model and A/B-group fixtures.

### 2.2 Domain-Validity-Gated-MR-for-SciML

Observed assets:

- `research_assets/mr_cards/node_permutation_equivariance.json`
- `research_assets/mr_cards/mirror_y_equivariance.json`
- `research_assets/mr_cards/discrete_divergence_boundedness.json`
- `research_assets/fixtures/node_permutation_case.json`
- `research_assets/experiments/claim-ledger.yml`
- `research_assets/runs/*/manifest.yml`
- `research_assets/runs/seeded-fault-detection/raw/metric_ledger.json`
- `tools/run_*`: real-SUT and evidence runners for MeshGraphNets-family
  cylinder-flow pilots.

Evidence boundary:

- Current runtime claims are one-SUT / one-checkpoint bounded claims.
- Node permutation has a real single-MR pilot with relative L2 0.0 under
  tolerance `1e-6`.
- Mirror-y has both OOD-stress and exact synthetic symmetric-mesh evidence.
- Discrete divergence remains a deferred or diagnostic MR, not an absolute
  mass-conservation verdict.
- Seeded-fault detection reports 10 mutants in five classes, with 5/10 detected
  by at least one MR in that bounded catalogue.

## 3. Runtime Environment Classification

| Source | Asset class | Runtime category | MetBench readiness |
|---|---|---|---|
| Minimum toy PUTs | sorting / matmul / quadratic | Local Python | RuntimeCandidate |
| Minimum P1 heat | NumPy FDM + 10 MRs + detection matrix | Local Python | RuntimeCandidate |
| Minimum P2/P6/P7/P10 | NumPy smoke PUTs | Local Python | RuntimeCandidate after parser/catalog work |
| Minimum P3 Lorenz | SciPy `solve_ivp` | Local Python with SciPy or Docker | Already represented in MetBench; reconcile provenance |
| Minimum P8 Schrodinger | NumPy FFT, uses `np.trapz` in external source | Local Python with NumPy pin or Docker | Already represented in MetBench; keep NumPy compatibility gate |
| Minimum P9 surrogate | deterministic OpenMC surrogate | Local Python | Already represented in MetBench |
| Minimum mutator stack | cosmic-ray / ad-hoc mutators | Docker | ImportedOnly until Docker executor exists |
| Domain node-permutation fixture | self-contained fixture | Local Python | RuntimeCandidate |
| Domain real MeshGraphNets pilots | torch checkpoint + dataset roots | SSH or Docker | ImportedOnly until runtime executor and artifacts are configured |
| Domain seeded-fault matrix | committed metric ledger | Import-only evidence | ImportedOnly; report/anomaly projection candidate |

Runtime policy:

- **Local**: allowed for acceptance baselines and CI-friendly smoke tests.
- **Docker**: use for dependency-heavy or pinned environments. Until MetBench has
  a production Docker executor, Docker assets remain `ImportedOnly` or are run
  outside MetBench with clearly separated evidence.
- **SSH remote**: use for large checkpoints, GPU/CPU server paths, and
  repository-local data roots. Until MetBench has an SSH executor, SSH assets are
  import packages plus external evidence, not MetBench end-to-end executions.

## 4. Batch Import Plan

### Batch A - Local Acceptance Baseline

Import:

- toy `sorting`, `matmul`, `quadratic`;
- P1 heat, including 10 MRs and the detection matrix.

Purpose:

- Provide a small, deterministic acceptance baseline for MT main flow,
  `ImportAssets`, `ExportAssets`, report generation, visualization, and anomaly
  surfaces.

Expected packages:

- `metbench-import-minmr-toy-classic-v1`
- `metbench-import-minmr-p1-heat-v1`

Acceptance:

- **Cloud complete:** `ImportAssets` stages the packages and writes
  `staging-manifest.json` plus `sut-import-unit.json`.
- **Cloud complete:** one toy MR and three P1 heat MRs execute through the
  launcher using the explicit acceptance catalog.
- **Cloud complete:** P1 mutant/MR detection records project to anomaly
  candidates with imported-evidence limitations.
- **Cloud complete:** Batch A execution records render through HTML / Word /
  Excel / PDF reports and binary run-point chart DTOs.
- **VM complete:** WPF ImportAssets / RunBatch / ExportAssets completed in the
  VM; result, coverage, and anomaly views were opened and captured in
  `docs/superpowers/specs/2026-06-11-external-mr-assets-batch-a-d-vm-evidence/`.

### Batch B - Existing Minimum-MR Runtime Reconciliation

Import/reconcile:

- P3 Lorenz;
- P4 pendulum;
- P5 point kinetics;
- P8 Schrodinger;
- P9 OpenMC surrogate.

Purpose:

- Avoid duplicate SUT creation.
- Attach external provenance, import-package compatibility, and detection
  records to existing MetBench runtime slices.

Expected package:

- `metbench-import-minmr-existing-runtime-reconcile-v1`

Acceptance:

- Existing MetBench SUT IDs remain stable.
- Import/export round-trip preserves provenance and compatibility profile.
- P3/P8 dependency gates remain explicit. P8 external `np.trapz` risk is
  recorded as an environment compatibility issue unless patched upstream or
  pinned.

### Batch C - Remaining Local Numerical PUTs

Import:

- P2 wave;
- P6 Poisson;
- P7 Burgers;
- P10 PINN/HNN smoke.

Purpose:

- Expand local acceptance coverage without requiring Docker or remote servers.

Expected package:

- `metbench-import-minmr-local-remaining-v1`

Acceptance:

- Each SUT has sample input, runner, output parser, catalog binding, and at
  least one smoke MR or imported MR card.
- MRs without explicit MetBench predicate support stay `ImportedOnly` until a
  typed assertion binding is implemented.

### Batch D - SciML Domain-Validity Fixture and Evidence Import

Import:

- three MR cards;
- domain-validity rubric;
- node-permutation fixture;
- claim ledger;
- seeded-fault metric ledger.

Purpose:

- Exercise MetBench evidence import, report rendering, visualization, and anomaly
  review without over-claiming real-SUT execution.

Expected package:

- `metbench-import-sciml-domain-validity-fixture-v1`

Acceptance:

- **Cloud complete:** node-permutation is staged as fixture evidence, not a
  fresh MGN runtime verdict.
- **Cloud complete:** seeded-fault detection matrix is projected to report text
  and anomaly candidates with the original one-SUT / one-checkpoint limitation.
- **Cloud complete:** discrete divergence is displayed as deferred/diagnostic,
  not as a pass/fail absolute conservation MR.
- **VM complete:** WPF ImportAssets / ExportAssets preserved the SciML package,
  and result / coverage / anomaly views were opened and captured in the VM
  evidence directory. Live MGN replay remains outside Batch D scope.

### Batch E - Real SUT Runtime Extension

Import/run:

- MeshGraphNets cylinder-flow checkpoint and dataset;
- mirror-y real and synthetic symmetric-mesh pilots;
- conservation diagnostic;
- multicheckpoint and seeded-fault runs.

Runtime:

- Docker for reproducible torch/cpu and pinned dependency runs.
- SSH remote server for large artifact paths and long-running executions.

Expected package:

- `metbench-import-sciml-mgn-runtime-v1`

Acceptance:

- Docker preflight/pilot support exists for an already-built local image, but
  this is only a runtime probe, not MGN replay.
- Requires graph/tensor adapters, artifact mount policy, and Docker or SSH
  execution wiring before claiming end-to-end MetBench MGN execution.
- Before those exist, this batch is `ImportedOnly` with external evidence and
  blocked runtime readiness.

## 5. Acceptance Test Cases

| ID | Module | Test case | Expected result |
|---|---|---|---|
| AT-01 | Import | Import Batch A package through `ImportAssets` | Job succeeds; staged manifest and import unit are written |
| AT-02 | MT main flow | Run toy sorting permutation MR | Source/follow-up execute; verdict pass |
| AT-03 | MT main flow | Run P1 heat alpha-invariance MR | Source/follow-up execute; verdict pass |
| AT-04 | MT main flow | Run P1 heat time-scaling MR | Source/follow-up execute; verdict pass |
| AT-05 | MT main flow | Run P1 heat mesh-convergence MR | Multi-run evidence records convergence predicate |
| AT-06 | Anomaly | Load P1 detected mutant evidence | Detected records appear as anomaly candidates |
| AT-07 | Export | Export Batch A package through `ExportAssets` | Round-trip import unit remains valid |
| AT-08 | Report | Export report for Batch A execution set | Word/Excel/PDF/HTML artifacts exist and include SUT/MR/verdict evidence |
| AT-09 | Visualization | Open dashboard after Batch A runs | Pass/fail/anomaly counts and MR coverage are visible |
| AT-10 | Runtime | Run local preflight for Batch A | Required Python dependencies pass; missing dependency fails closed |
| AT-11 | Evidence | Import SciML seeded-fault ledger | 10 mutants and 5/10 union detections are displayed with limitations |
| AT-12 | Docker | Submit Docker runtime pilot | Runs a real `docker run --rm <image> python --version` probe only when `METBENCH_DOCKER_RUNTIME_IMAGE` is set; otherwise explicitly skipped. This does not claim MGN replay. |
| AT-13 | SSH | Submit SSH real-SUT pilot | Blocked until SSH executor exists; not counted as a failed MetBench test |

## 6. Test Data Inventory

Minimum-MR-SubSet:

- `experiments/toy_put/classic_mt_catalog.json`
- `experiments/puts/p1_heat.py`
- `experiments/puts/p2_wave.py`
- `experiments/puts/p3_lorenz.py`
- `experiments/puts/p4_pendulum.py`
- `experiments/puts/p5_pke.py`
- `experiments/puts/p6_poisson.py`
- `experiments/puts/p7_burgers.py`
- `experiments/puts/p8_schrodinger.py`
- `experiments/puts/p9_openmc.py`
- `experiments/puts/p10_pinn_hnn.py`
- `data/raw/p1_heat/mrs.json`
- `data/raw/p1_heat/detection_matrix.csv`
- `experiments/env/Dockerfile.mutator`

Domain-Validity-Gated-MR-for-SciML:

- `research_assets/mr_cards/node_permutation_equivariance.json`
- `research_assets/mr_cards/mirror_y_equivariance.json`
- `research_assets/mr_cards/discrete_divergence_boundedness.json`
- `research_assets/rubric/domain_validity_rubric.json`
- `research_assets/fixtures/node_permutation_case.json`
- `research_assets/experiments/claim-ledger.yml`
- `research_assets/runs/real-sut-node-permutation-pilot/manifest.yml`
- `research_assets/runs/mirror-y-rate-upgrade/manifest.yml`
- `research_assets/runs/mirror-y-symmetric-mesh/manifest.yml`
- `research_assets/runs/conservation-diagnostic-pilot/manifest.yml`
- `research_assets/runs/seeded-fault-detection/manifest.yml`
- `research_assets/runs/seeded-fault-detection/raw/metric_ledger.json`

## 7. Test Conclusion Template

Each batch must report:

- imported package ID and source commit;
- runtime environment used: local, Docker, or SSH;
- MetBench operation coverage: MT main flow, import, export, visualization,
  anomaly, report;
- executed MR count, pass/fail/skip count, and anomaly count;
- dependency/preflight status;
- exported artifact paths;
- explicit limitations.

Allowed conclusion examples:

- Batch A local acceptance passed when all required local jobs complete and
  reports/export artifacts exist.
- Batch D evidence import passed when imported ledgers are visible and retain
  their one-SUT / one-checkpoint limitations.
- Batch E remains blocked, not failed, if Docker/SSH executor support is absent.

Forbidden conclusion examples:

- Do not claim general SUT reliability.
- Do not claim cross-SUT or geometry-independent rates from single-SUT evidence.
- Do not claim MetBench executed Docker/SSH real SUTs until the corresponding
  executor evidence exists.

## 8. Implementation Steps

Batch A/D status: steps 1-6, 8, and 9 are complete across cloud and VM
evidence. Step 10 is a future Batch E prerequisite and is not required for
Batch A/D completion.

1. Add package builders for Batch A using `SutImportUnit` and
   `SutImportPackageExporter`.
2. Add focused tests for Batch A import/export round-trip and validator failures.
3. Add local SUT folders or generated package assets for toy and P1 heat.
4. Add launcher tests for selected Batch A MRs.
5. Add anomaly projection tests from P1 detection records.
6. Add report/export acceptance tests for Batch A executions.
7. Reconcile existing P3/P4/P5/P8/P9 metadata with Minimum-MR-SubSet provenance.
8. Add Batch D import-only package builder for MR cards and seeded-fault ledgers.
9. Add dashboard/report tests that display imported evidence limitations.
10. Add Docker runtime preflight/pilot gate for already-built local images.
11. Design Docker/SSH runtime executor contracts before Batch E is promoted from
    `ImportedOnly` to executable.

## 9. Verification Commands

Minimum cloud-side verification for implementation PRs:

```powershell
dotnet test MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~ImportExport|FullyQualifiedName~SystemMtJob|FullyQualifiedName~ExecutionArtifact|FullyQualifiedName~SystemMtReport"
dotnet test MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~Launcher|FullyQualifiedName~Anomaly|FullyQualifiedName~Coverage"
```

Windows/WPF verification is required for visualization and report UI acceptance:

```powershell
dotnet build MetBench.sln --no-restore
```

Collect UIA or screenshot evidence for:

- ImportAssets;
- RunMr or RunBatch;
- ExportAssets;
- ExportExecutionArtifacts;
- ExportReport;
- dashboard visualization;
- anomaly view.

## 10. Deliverables

- Plan file: this document.
- Import packages:
  - `metbench-import-minmr-toy-classic-v1`
  - `metbench-import-minmr-p1-heat-v1`
  - `metbench-import-minmr-existing-runtime-reconcile-v1`
  - `metbench-import-minmr-local-remaining-v1`
  - `metbench-import-sciml-domain-validity-fixture-v1`
  - `metbench-import-sciml-mgn-runtime-v1`
- Acceptance test cases and fixtures in `MetBench_SystemMT.Tests`.
- Test data manifests with provenance and commit IDs.
- Batch-level test conclusion reports.
- Cloud evidence report:
  `docs/superpowers/specs/2026-06-11-external-mr-assets-batch-a-d-cloud-evidence.md`.
- Windows/WPF VM回执 target:
  `docs/superpowers/specs/2026-06-11-external-mr-assets-batch-a-d-vm-evidence-template.md`.
