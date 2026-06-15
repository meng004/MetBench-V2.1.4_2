# SP4 — Per-SUT/MR WPF Async-Page UI Evidence

> **Date**: 2026-06-16
> **Branch**: `sp4-async-ui-evidence`
> **Goal**: for every runtime-catalog MR, capture WPF **async-execution-page** UI evidence (select MR → Submit → poll `AsyncState` to terminal → 4 screenshots), proving the async page runs each imported SUT/MR end-to-end.

## Method

`tools/sp4_run_all.ps1` loops all **38 MR ids** (enumerated live from the async page `AsyncMrCombo`) through `tools/uia-acceptance`'s `--mr` mode (the PR #280-proven async-page driver), with pre/post `MetBench_Client` kill and `METBENCH_SYSTEM_PYTHON`=codex python. Per-MR terminal state + exit code in `sp4-results.csv`; 4 screenshots per MR (`case<mr>-0{1..4}-*.png`).

## Result — 33 job-Succeeded / 5 job-Failed (of 38)

**Important semantics** (per the T0-T2 async closure design §7): a job **State** of `Succeeded` means the async job ran to completion — it does **NOT** mean the MR passed. An MR-assertion violation is recorded as an **anomaly** while the job stays `Succeeded` (per-item for T5). A job `Failed` is an **infrastructure** failure (runtime preflight / process / parse), not an MR violation.

| Bucket | Count | MRs |
|---|---|---|
| **Succeeded — MR held** | 30 | all host pure-stdlib + scipy MRs (advection/bateman/burgers/damped-oscillator/decay-chain/diffusion/fourier/heat/lotka-volterra/p3-p9/poisson/subchannel/wave + scipy-bvp/ivp) |
| **Succeeded — MR assertion FAILED (anomaly)** | 3 | `openmc-pincell-nu-sigma-f`, `openmc-pincell-particle-count-convergence`, `openmc-pincell-sigma-a` — async page ran them end-to-end (job Succeeded) but the MR was violated → anomaly (UI shows "MR assertion failed"). openmc ran on the host (codex python has openmc importable). |
| **Failed — runtime preflight (container-only)** | 3 | `openmoc-pincell-{nu-sigma-f,ray-track-convergence,sigma-a}` — no openmoc runtime on the host; T1 preflight fail-closed to `Failed`. Expected; SP1 container xUnit already runs real openmoc. |
| **Failed — async-path JSON parse (finding)** | 2 | `csv-roundtrip-identity` ("'k' is an invalid start of a value" — its CSV output is JSON-parsed); `projectile-scale-v0` ("'4' is invalid after a single JSON value. Expected end of data"). |

Authoritative per-MR results: `sp4-results.csv` (38 rows). Screenshots: `case<mr>-04-*.png` is the terminal-state panel per MR.

## SP4 UI-evidence conclusion

The WPF **async-execution page** drives **33/38** MRs end-to-end to a terminal job state with UI evidence (submit → poll → terminal + 4 screenshots), across all SUT/equation families. This is the SP4 deliverable for those 33.

## Findings (for the team)

1. **`csv-roundtrip-identity` and `projectile-scale-v0` fail via the async WPF page with `System.Text.Json` parse errors** — the async-page RunMr result-handling parses SUT output as a single JSON value, which breaks for the CSV-output test SUT and for projectile's non-single-JSON stdout. These SUTs exercise via the launcher/xUnit path (SP1); the async-page path needs to tolerate the same output shapes. Reconcile the async result parsing with the launcher's output handling.
2. **openmc MRs report `MR assertion failed` (anomaly) on the host** — the async page ran them (job Succeeded) but the metamorphic relation was violated. This is the expected anomaly-detection behavior (job stays Succeeded; anomaly recorded for T5), consistent with the known OpenMOC×OpenMC cross-program disagreement (T5). Whether the host-openmc result is itself trustworthy (vs the container openmc) is a separate question.
3. **openmoc MRs need their runtime** — `Failed` at preflight on the host (no openmoc in codex python). Real openmoc is covered by SP1 container xUnit; the WPF GUI can't run inside the container, so per-MR openmoc *UI* evidence is container-infeasible and is recorded as such.

## Scope note

SP4 captures the async-page UI evidence on the host. openmoc per-MR UI evidence is container-infeasible (WPF GUI ∉ container); SP1 already proves openmoc/openmc run real in the container via xUnit. CI unchanged.
