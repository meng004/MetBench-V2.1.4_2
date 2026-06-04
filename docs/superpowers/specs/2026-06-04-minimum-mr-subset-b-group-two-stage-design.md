# Minimum-MR-SubSet B-Group Two-Stage Import/Runtime Design

> Status: proposed scoped design for the second controlled `minimum-mr-subset` import batch.
> Scope: B group only: P8 Schrodinger and P3 Lorenz.
> Required sequence: Stage 1 import/export staging only; Stage 2 live runtime promotion with current async execution pipeline validation.

## 1. Evidence Basis

MetBench baseline:

- Local branch at design start: `main...origin/main`, clean.
- Live `origin/main`: `6293455df3fcbe7692032d46ea05b97fdfb6035f`.
- Existing A-group staging model: `MetBench_BLL.Core/SystemMT/ImportExport/Put/`.
- Existing async path: `SystemMtJobService` submits `SystemMtJobRequest`; `SystemMtJobWorker` executes through `ISystemMtAsyncPipeline`; `SystemMtAsyncPipeline` delegates to `ISystemMtLauncher.RunAsync`; cancellation uses `IJobCancellationRegistry`.

External `minimum-mr-subset` evidence:

- Local read-only source tree: `/private/tmp/minimum-mr-subset`.
- External commit: `b931b5f74d0f3f3cb704cd6fedbcb4f523cccd7f`.
- P3 source path: `experiments/puts/p3_lorenz.py`.
  - `put_id`: `P3`.
  - family: `chaotic ODE / RK`.
  - observables: `t`, `trajectory`, `centroid`.
  - imports: `numpy`, `scipy.integrate.solve_ivp`.
- P8 source path: `experiments/puts/p8_schrodinger.py`.
  - `put_id`: `P8`.
  - family: `complex PDE / spectral`.
  - observables: `x`, `probability_density`, `norm`.
  - imports: `numpy`.
- Shared smoke contract: `tests/puts/test_smoke.py` requires each PUT `run_canonical()` to return finite non-empty observables in under 60 seconds.
- Data limitation: the external tree currently exposes complete `mrs.json` and `detection_matrix.csv` only under `data/raw/p1_heat/`; no equivalent P3/P8 data raw directory was observed.
- Local execution limitation: direct P3/P8 smoke attempts failed with `ModuleNotFoundError: No module named 'numpy'`; P3 would also require SciPy.

Therefore this design may claim source-level import evidence for P3/P8, but it must not claim local external PUT execution or P3/P8 detection-matrix completeness until those are separately verified.

## 2. Design Decision

The second batch is split into two mandatory stages:

1. **Stage 1: Import only**.
   - Add P3/P8 to the existing `SutImportUnit` staging model.
   - Preserve SUT, MR draft, IO group, mutation/operator-class, detection placeholder, compatibility, and provenance.
   - Default all P3/P8 relations to `RuntimeReadiness.ImportedOnly`.
   - Do not write `SUT/`, live manifests, LiteDB, runtime catalog counts, or execution evidence.
2. **Stage 2: Live runtime promotion + async validation**.
   - Promote only explicitly compatible P3/P8 MR slices into live System-MT catalog assets.
   - Add MetBench-owned runnable assets under `SUT/`.
   - Validate both synchronous launcher execution and current async job pipeline execution.
   - Keep runtime promotion separate from imported research evidence.

This preserves the A-group rule: import success means the package is structurally valid, not that the MR is executable. Runtime status changes only in Stage 2.

## 3. B-Group Scope

| PUT | Role in this batch | Why now | Runtime caution |
|---|---|---|---|
| P8 Schrodinger | Complex PDE / spectral stress case | Adds field-like `probability_density`, norm preservation, and complex-state provenance pressure | External source uses NumPy FFT. A MetBench live runner must be explicitly described as a compatible MetBench runtime slice, not proof that the external NumPy adapter ran locally. |
| P3 Lorenz | Chaotic ODE / trajectory sensitivity stress case | Adds vector trajectory and centroid observables; useful for trajectory/shape compatibility | External source uses SciPy `solve_ivp`. Any cloud-safe live runner must define its own deterministic integration policy and tolerance. |

Excluded from this batch: P1, P2, P6, P7, P10, and real OpenMC/P9 work.

## 4. Stage 1 Import Model

Stage 1 reuses the existing import unit:

```text
SutImportUnit U = <S, R, G, Mu, D, Pi, K>

S  = one SUT asset
R  = imported MR asset list for S
G  = imported IO groups for S
Mu = mutation/operator-class assets for S
D  = detection relation over R x Mu x G
Pi = provenance
K  = compatibility profile
```

For P3/P8, `Mu` may contain only `MutationRepresentationKind.OperatorClassOnly` entries unless concrete mutants are observed. `D` must use `DetectionResult.Inconclusive` when no real detection matrix row exists for P3/P8.

Required Stage 1 rules:

- P3/P8 source paths and external commit are mandatory provenance fields.
- P3 observables are `t`, `trajectory`, `centroid`.
- P8 observables are `x`, `probability_density`, `norm`.
- P3/P8 imports are valid only as single-SUT packages.
- P3/P8 imports remain `ImportedOnly` unless both transform and assertion bindings are explicit.
- No live runtime files or catalog counts change.

## 5. Stage 2 Runtime Promotion Model

Stage 2 promotes a deliberately small runtime slice:

| PUT | First promoted MR candidate | Expected transform | Expected assertion | Async requirement |
|---|---|---|---|---|
| P8 | `p8-norm-conservation` | timestep/count or propagation-step transform | approximate invariant on `norm_drift` or final norm | Submit via `SystemMtJobService`, worker executes through `SystemMtAsyncPipeline`, final status `Succeeded`, result saved. |
| P3 | `p3-trajectory-sensitivity` or `p3-centroid-shift` | initial-condition or time-horizon perturbation | monotone/separation or bounded centroid relation with explicit tolerance | Same async success path; missing runtime must fail or skip cleanly, not hang. |

The promoted assets may be MetBench-contained runners rather than direct execution of the external NumPy/SciPy modules. If so, the manifest and docs must state that they are MetBench runtime slices derived from the imported PUT semantics. They must not claim the external repository adapter executed unless the external dependency environment is actually run and recorded.

## 6. Async Execution Integration

The current async pipeline is already launcher-backed:

```text
SystemMtJobService.SubmitAsync(SystemMtJobRequest)
  -> IJobStore.CreateAsync(Queued)
  -> IJobQueue.EnqueueAsync(jobId)
  -> SystemMtJobWorker.RunJobAsync(jobId)
  -> ISystemMtAsyncPipeline.ExecuteJobAsync(jobId, request, progress, token)
  -> SystemMtAsyncPipeline
  -> ISystemMtLauncher.RunAsync(mrId, parameterOverrides, token)
```

Stage 2 must test the promoted P3/P8 MR IDs through this exact chain. It is not enough to call `ISystemMtLauncher.RunAsync` directly.

Minimum async acceptance:

- Submit P3/P8 promoted MR IDs as `SystemMtJobRequest`.
- Execute a worker against an in-memory or LiteDB job store.
- Observe state progression to a terminal status.
- For success cases, verify `GetResultAsync(jobId)` returns a non-null `MrRunResult`.
- For dependency-missing cases, verify the job reaches `Failed` or the corresponding launcher test cleanly skips with an explicit reason.

Cancellation acceptance is optional for this batch because PR #288 already covers process-tree cancellation. If Stage 2 adds a deliberately long-running P3/P8 path, it must also add cancellation evidence; otherwise it should reuse existing cancellation coverage and avoid fragile timing tests.

## 7. Storage And Runtime Boundaries

- Stage 1 writes only BLL.Core import/export models, fixtures, validator tests, docs, and package round-trip tests.
- Stage 1 does not change `.github/governance/expected-catalog-counts.txt`.
- Stage 2 may change:
  - `SUT/minimum_mr_subset_p3*/`
  - `SUT/minimum_mr_subset_p8*/`
  - manifest catalog assets
  - metadata catalog rows
  - runtime count whitelist
  - launcher and async job tests
- Stage 2 must not change WPF UI unless a separate Windows VM plan is approved.

## 8. Risks And Responses

| Risk | Response |
|---|---|
| P3/P8 external smoke cannot run locally due to missing NumPy/SciPy. | Record as environment limitation. Stage 1 stays source/provenance import only. Stage 2 either uses MetBench-contained runners or adds explicit runtime skip policy. |
| P8 complex wavefunction is hidden by real-only observables. | Keep `probability_density` and `norm` as real observables; preserve complex/spectral provenance in metadata. Do not invent a full complex typed predicate in this batch. |
| P3 chaotic sensitivity produces flaky assertions. | Use deterministic small-step runner and conservative, explicitly documented tolerance; prefer centroid or separation metrics over pointwise full-trajectory equality. |
| No P3/P8 detection matrix exists. | Mutation assets remain operator-class only; detection records are `Inconclusive` unless real rows are imported. |
| Async tests duplicate synchronous launcher tests. | Async tests must drive `SystemMtJobService` + `SystemMtJobWorker` and assert persisted job status/result, not only direct launcher success. |

## 9. Completion Definition

Stage 1 is complete when P3/P8 staged packages validate and round-trip without live catalog mutation.

Stage 2 is complete when P3/P8 promoted runtime MRs are listed by the launcher, pass focused launcher execution tests, and pass focused async job execution tests through the current polling/job pipeline.
