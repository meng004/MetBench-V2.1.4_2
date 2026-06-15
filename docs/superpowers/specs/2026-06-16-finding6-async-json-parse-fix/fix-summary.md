# Fix — SP4 finding #6: async-page RunMr fails non-JSON-sample SUTs (JSON parse)

> **Date**: 2026-06-16 · **Branch**: `fix-async-json-parse` · follows SP4 #368 / SP5 #369.

## Symptom (SP4)

Via the WPF async-execution page, `csv-roundtrip-identity` and `projectile-scale-v0`
RunMr jobs reached terminal **Failed** with `System.Text.Json` errors:
`'k' is an invalid start of a value` (CSV) / `'4' is invalid after a single JSON value` (text).
The other 36 MRs (JSON sample cases) were unaffected.

## Root cause (pinpointed via full stack)

These two SUTs are the **non-JSON I/O** fixtures (`_test-csv` → `sample/standard.csv`,
`projectile` → `sample/standard.txt`). `SystemMtExecutionRecorder.BuildSampleTraces`
called `JsonDocument.Parse(File.ReadAllText(sampleCasePath))` **unconditionally** to
build per-field sample-trace evidence, throwing a fatal `JsonException` on a non-JSON
sample. The exception propagated up `WriteEvidenceAsync → RecordAsync →
SystemMtLauncher.RunAsync → SystemMtAsyncPipeline.ExecuteJobAsync`, and the worker
marked the whole job **Failed**.

Why it surfaced only via the WPF path: the recorder's evidence write runs only when an
`IExecutionEvidenceRepository` is injected. The launcher end-to-end test
(`LauncherEndToEndTestCsvTests`, **passing** in SP1 on both container and host) wires no
evidence repo, so `BuildSampleTraces` never ran there; the WPF app wires a real LiteDB
evidence repo, so it did. **The launcher core was never at fault.**

## Fix

`BuildSampleTraces` now wraps the JSON parse + trace-building in a `try/catch (JsonException
or IOException or UnauthorizedAccessException)` that degrades to **no traces** — sample
traces are run-record *enrichment*, never a run gate. This mirrors `InputCaseReader`'s
documented best-effort contract. One file: `MetBench_BLL.Core/SystemMT/Pipeline/SystemMtExecutionRecorder.cs`.

## Verification

- **Unit (CI-safe)**: new regression `ExecutionEvidenceWriteThroughTests.Record_with_non_json_sample_case_degrades_to_no_traces_and_does_not_fail_run` (CSV source/followup → no throw, evidence written, `SampleTraces` empty). Full recorder+evidence suite **20/20 pass** (was failing the new test before the fix).
- **End-to-end (WPF async page)**: after rebuild, both `csv-roundtrip-identity` and
  `projectile-scale-v0` now reach terminal **Succeeded** (exit 0, 4 screenshots) — see
  `csv-roundtrip-after-fix-succeeded.png` / `projectile-after-fix-succeeded.png`.

SP4 per-MR async coverage improves **33/38 → 35/38** job-Succeeded (openmoc×3 remain
container-only). No CI gate change. Note (out of scope): `SystemMtJobWorker`'s catch
keeps only `ex.Message` (no stack) — a diagnostic gap that made this harder to pinpoint;
candidate for a future small improvement.
