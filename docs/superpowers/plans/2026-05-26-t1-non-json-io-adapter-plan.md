# T1 Non-JSON I/O File Format Adapter — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Generalise the System MT input / output adapter contract (T1 §2.1 element 2 in `CLAUDE.md`) so future SUTs whose native I/O is CSV, YAML, Fortran namelist with non-numeric scalars, plain text, or a binary fixture can be wired in **without changing the launcher**, the pipeline, or `ManifestMrCatalogProvider`. Today every adapter under `SUT/*/` reads / writes JSON via stdlib `json.loads` / `json.dumps`; non-JSON SUTs cannot be onboarded under the current adapter contract without bespoke Python plumbing per SUT.

**Architecture:** Keep the existing `IInputAdapter` / `IOutputAdapter` contract (Python sub-process I/O, byte-stream) intact. Introduce a single new abstraction at the **Python adapter side**: a small `metbench_io_format` Python helper package shipped under `SUT/_shared/metbench_io/` that provides `read_input(path, fmt) -> dict` and `write_input(data, path, fmt) -> None`, with `fmt` in `{"json", "csv-row", "yaml", "namelist", "plain-text"}`. SUT-author wiring is then one line per parser script (`from metbench_io import read_input as _read; data = _read(path, fmt=PROGRAM_INPUT_FORMAT)`). The C# adapter layer stays unchanged — it remains protocol-agnostic about the wire format, which means **no `MetBench_BLL.Core` changes** are required and the new code lives entirely under `SUT/_shared/` plus a small fixture-driven SUT under `SUT/_test_csv/` that exercises the new adapter via the existing launcher.

**Tech Stack:** Python stdlib (`csv`, `tomllib` for namelist subset, `xml.etree.ElementTree` for namelist value extraction), xUnit (only for the SUT smoke tests), no new C# dependencies, no new NuGet packages.

---

## Scope and Non-Goals

This is a cloud-side T1 plan. It is suitable for Linux/cloud execution because the new helper is Python-only and the test SUT runs on the existing pure-stdlib `system` runtime.

This plan must **not**:

- Modify `MetBench_BLL.Core`, `MetBench_BLL`, `MetBench_DAL`, or `MetBench_Domain` source.
- Change `ISystemMtLauncher` / `MrRunResult` / `LauncherOptions` / `ManifestMrCatalogProvider`.
- Add a new SUT runtime key (would require revisiting PR-1 contract).
- Touch Method MT.
- Touch WPF / `MetBench_Client` / `App.xaml.cs`.
- Change any existing `SUT/*/` directory (no edits to existing JSON parsers).
- Add a new MR id to any existing SUT.

It must:

- Ship one new helper package at `SUT/_shared/metbench_io/` with **pure Python stdlib** (no scipy, no numpy, no pyyaml).
- Ship one minimal test SUT at `SUT/_test_csv/` (catalog.json + runner + I/O parser scripts + one sample input CSV + the new helper imported) that proves CSV input / CSV output round-trips through the launcher.
- The new test SUT registers one MR (`csv-roundtrip-identity`) under a synthetic equation `_test_csv` that just echoes the input row through the runner. The MR's assertion is `approx` over the echoed scalar.
- Pinned-count test files bump 29 → 30 MR / 15 → 16 SUT (only if the new test SUT is registered; if we keep the helper unregistered, no pinned-count edit is needed — see §"Registration mode" below).

## Registration Mode (single design decision the plan locks)

There are two ways to ship A:

**Mode 1 — helper only.** Add `SUT/_shared/metbench_io/` and tests under `MetBench_SystemMT.Tests/SystemMT/Shared/` that drive the helper directly via `PythonScriptRunner.Run`. No new SUT, no new MR, no pinned-count edits. **Risk**: the helper is technically dead code at end-of-PR; the next SUT that wants CSV brings its own parser and may not discover the helper exists.

**Mode 2 — helper + test SUT.** Same helper + a `SUT/_test_csv/` minimal SUT that uses the helper through the launcher. This raises pinned counts to 30 / 16 across six test files (same files PR-2 touched). **Risk**: the test SUT lives in the catalog forever as a "synthetic example" SUT — slightly noisy.

**Lock: Mode 2.** Reasoning: the helper *must* round-trip through the launcher and `ManifestMrCatalogProvider` end-to-end to prove the wire format is opaque to the launcher; mode 1 leaves that integration unverified. The "_test_csv" prefix + leading underscore makes the synthetic nature obvious in directory listings.

## Files

- Create: `SUT/_shared/metbench_io/__init__.py`
- Create: `SUT/_shared/metbench_io/_csv.py`
- Create: `SUT/_shared/metbench_io/_plain_text.py`
- Create: `SUT/_test_csv/catalog.json`
- Create: `SUT/_test_csv/_test_csv_runner.py`
- Create: `SUT/_test_csv/_test_csv_input_parser.py`
- Create: `SUT/_test_csv/_test_csv_output_parser.py`
- Create: `SUT/_test_csv/sample/standard.csv`
- Create: `MetBench_SystemMT.Tests/SystemMT/Shared/MetBenchIoHelperTests.cs`
- Create: `MetBench_SystemMT.Tests/SystemMT/Launcher/LauncherEndToEndTestCsvTests.cs`
- Modify: `MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj` (csproj entries for new SUT assets)
- Modify: `MetBench_BLL.Core/SystemMT/Launcher/LegacyCatalogFactory.cs` (one new `MrBlueprint` for `csv-roundtrip-identity`)
- Modify: `MetBench_BLL.Core/SystemMT/Metadata/SystemMtMetadataCatalog.cs` (one new `EquationMetadata` row `_test_csv` + one `MrMetadata` row)
- Modify: pinned-count tests (29 → 30, 15 → 16) — six files identical to PR-2's pattern.
- Modify: `docs/status/current.md`
- Modify: `docs/requirements.md`
- Modify: `docs/PROJECT-STRUCTURE.md`
- Modify: `docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md`

## Task 1: Helper Contract + Failing Tests

- [ ] **Step 1:** Add `MetBenchIoHelperTests.cs` driving the helper directly via `PythonScriptRunner.Run`. Cover:
  - CSV: header-row read, body-row write, quoted strings preserved, numeric coercion preserved as strings (we do not type-coerce — leave that to the SUT runner).
  - Plain text: round-trip preserves line endings, no trailing newline added or stripped.
  - Unknown format → helper exits non-zero with a clear diagnostic.
- [ ] **Step 2:** Run focused tests → red (helper does not exist).

## Task 2: Helper + Test SUT + Failing End-to-End Test

- [ ] **Step 1:** Implement `SUT/_shared/metbench_io/{_csv,_plain_text,__init__}.py`.
- [ ] **Step 2:** Implement `SUT/_test_csv/` (catalog.json declares `python_executable_kind: "system"`, runner echoes the CSV row, parsers use the helper).
- [ ] **Step 3:** Add `LauncherEndToEndTestCsvTests` (mirror existing `LauncherEndToEndPoissonTests` shape) running `csv-roundtrip-identity` end to end → red until catalog wiring is added.
- [ ] **Step 4:** Run focused tests → red.

## Task 3: Catalog Wiring + Pinned-Count Bumps

- [ ] **Step 1:** Add `_test_csv` `EquationMetadata` + `csv-roundtrip-identity` `MrMetadata` to `SystemMtMetadataCatalog.cs`.
- [ ] **Step 2:** Add `csv-roundtrip-identity` `MrBlueprint` to `LegacyCatalogFactory.cs` (under `PythonExecutable: options.SystemPython`).
- [ ] **Step 3:** Bump pinned-count tests 29 → 30 / 15 → 16 across the six files PR-2 last touched:
  - `SystemMtLauncherTests` (order list + count)
  - `CatalogParityTests`
  - `HardcodedMrCatalogProviderTests`
  - `SystemMtBootstrapTests`
  - `LauncherCatalogV2ImporterTests`
  - `SystemMtLauncherProviderInjectionTests`
- [ ] **Step 4:** csproj `<None Include>` entries for `SUT/_test_csv/*.py`, `sample/*.csv`, plus `SUT/_shared/metbench_io/*.py`.
- [ ] **Step 5:** Run full suite → green.

## Task 4: Docs

- [ ] **Step 1:** `docs/status/current.md` row `T1 non-JSON I/O adapter` Open → Controlled with the helper contract + test surface.
- [ ] **Step 2:** `docs/requirements.md` F-T1-02 row extended with CSV support reference.
- [ ] **Step 3:** `docs/PROJECT-STRUCTURE.md` §2 SUT inventory bumped to 16 SUT / 13 equations / 30 MR.
- [ ] **Step 4:** Retire this plan to §3 of the active plan index.

## Task 5: Two-Layer Review and PR

- [ ] **Layer 1 self-review:**
  - No `MetBench_BLL.Core` source edits beyond the two catalog rows and the pinned-count tests.
  - No Method MT, no WPF.
  - Helper is pure stdlib.
- [ ] **Layer 2 maintainer review:**
  - Could the `_test_csv` SUT be confused with a real SUT in catalog reports? Leading underscore + plan note should make it visually distinct.
  - Could the helper be misused to read arbitrary files? It's a thin wrapper over `open(path)`; SUT runners already have arbitrary filesystem access by construction, so no new risk.
- [ ] Commit, push, open PR titled `feat(t1): add cross-format SUT I/O helper + CSV roundtrip test SUT`.

## Acceptance Criteria

- The `metbench_io` Python helper supports CSV and plain text round-trip with deterministic encoding.
- `csv-roundtrip-identity` MR runs end-to-end through the unchanged launcher.
- `_test_csv` test SUT exists and is the only new SUT.
- Inventory 30 MR / 16 SUT / 13 equations across all pinned-count files.
- Full `MetBench_SystemMT.Tests` is green.

## Stop Conditions

Stop and report without coding if:

- `origin/main` is unreachable.
- PR-B (cross-method differential runner) is not yet merged when this PR opens (the sequenced gate requires B first).
- The helper would need a non-stdlib dependency (e.g. real YAML parsing) — defer that format until a real SUT needs it.
- The new SUT's parser shape forces an `IInputAdapter` / `IOutputAdapter` contract change in C# — escalate before continuing.
