# Stage 3: OpenMOC Single-program Application Implementation Plan

> **For agentic workers:** Implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. Each task ships with a failing test first (TDD), implementation, then a green run. After every task, run the full SystemMT test suite and confirm a non-decreasing pass count.

**Goal:** Apply the Stage 1 (CLI execution + output parsing + GreaterThan assertion) and Stage 2 (input transformation via Python adapter) mechanisms to OpenMOC as the first real scientific computing program. Demonstrate one OpenMOC metamorphic relation end-to-end and return a pass/fail.

**MR Scenario:** A 2D pin-cell with reflective boundaries and 2-energy-group cross sections. The MR transformation `ScaleNuSigmaF` multiplies the fuel material's `nu*sigma_f` (per-group) by a configured factor `c > 1`. The assertion `GreaterThan` requires `k_eff(follow-up) > k_eff(source)`. Mathematical justification: `k_eff` is the dominant eigenvalue of `(1/k)·F·phi = (T - S)·phi`. Scaling `F` (the production operator that contains `nu*sigma_f`) by `c > 1` strictly increases the dominant eigenvalue when absorption (`sigma_a`) and scattering (`sigma_s`) are unchanged. The MR is therefore valid by construction, not empirical.

**Architecture:** No new C# business classes. All OpenMOC-specific logic lives in three Python scripts in `SUT/openmoc/`:

- `openmoc_runner.py` — the SUT. Reads a JSON case description, constructs an OpenMOC `Geometry` + `CPUSolver`, computes `k_eff`, writes a JSON output file.
- `openmoc_input_adapter.py` — implements the `transform-input` subcommand. Multiplies the fuel material's `nu_sigma_f` array element-wise by the `factor` parameter. Writes a follow-up JSON case description.
- `openmoc_output_adapter.py` — implements the `parse-output` subcommand. Reads the runner's output JSON and emits the normalized `{values, metadata}` payload that `PythonOutputAdapter` parses.

C# composes these via the existing `SystemProgram` + `SystemMtTask.WithGeneratedFollowUp` + `SystemMtRunner` pipeline. The transformation name on the C# side is a label only; the input adapter is single-purpose (one adapter ↔ one transformation, matching the Stage 2 `example_output_adapter.py` precedent).

**OpenMOC Python Resolution:** OpenMOC is not importable from system `python3` because the cloud environment installs it into a dedicated venv at `/opt/openmoc-venv/bin/python` (driven by `.claude/web-setup.sh`). Add a small `OpenMocTestPaths` helper in the tests that resolves the OpenMOC-capable Python in this order:

1. `METBENCH_OPENMOC_PYTHON` environment variable, if set;
2. `/opt/openmoc-venv/bin/python`, if it exists;
3. `python3` (final fallback so unit tests of the JSON-only adapters still run on stock systems).

The Reqnroll scenario probes this Python at scenario start; if `import openmoc` fails, the scenario is skipped with a clear message rather than failed.

**Tech Stack:** .NET 8 (`MetBench_BLL.Core`, no changes), xUnit 2.9, Reqnroll.xUnit 3.3, Python 3.12 (system + venv), OpenMOC branch `3D-MOC` (commit pinned by `.claude/web-setup.sh`), HDF5 1.10, NumPy 1.26, h5py 3.10.

---

## Scope Guard

This plan implements Stage 3 from `docs/superpowers/specs/2026-05-07-system-level-mt-bdd-design.md`.

Do NOT implement here:

- a second OpenMOC MR (Stage 4);
- HDF5 cross-section authoring (we keep XS inline in the case JSON for Stage 3);
- WPF UI for OpenMOC tasks (Stage 4);
- LiteDB persistence of OpenMOC `SystemMtResult` (Stage 4);
- a transformation registry or DSL — `ScaleNuSigmaF` is single-purpose;
- new C# assertions — `GreaterThan` already covers the chosen MR;
- a new C# project (`MetBench_BLL.OpenMOC/`); the spec's "C# unaware of OpenMOC" rule is best satisfied by adding **zero** C# code;
- modifying the WPF `MetBench_BLL` project;
- replacing the projectile demo (the projectile MR remains green);
- changing `MetBench_BLL.Core/SystemMT/*.cs` — they are reused as-is.

## File Structure

Create:

- `SUT/openmoc/openmoc_runner.py`
- `SUT/openmoc/openmoc_input_adapter.py`
- `SUT/openmoc/openmoc_output_adapter.py`
- `SUT/openmoc/sample/pincell.json` — small reference source case used by tests and humans.
- `MetBench_SystemMT.Tests/SystemMT/OpenMocTestPaths.cs` — Python resolver helper.
- `MetBench_SystemMT.Tests/SystemMT/OpenMocOutputAdapterTests.cs` — adapter contract tests (pure JSON, no OpenMOC import).
- `MetBench_SystemMT.Tests/SystemMT/OpenMocInputAdapterTests.cs` — adapter contract tests (pure JSON).
- `MetBench_SystemMT.Tests/SystemMT/OpenMocRunnerSmokeTests.cs` — end-to-end runner test (skipped if OpenMOC python unavailable).
- `MetBench_SystemMT.Tests/Features/OpenMocPinCellNuSigmaF.feature`
- `MetBench_SystemMT.Tests/Steps/OpenMocPinCellNuSigmaFSteps.cs`

Modify:

- `MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj` — link `SUT/openmoc/*.py` and `SUT/openmoc/sample/*.json` into `TestAssets/openmoc/` exactly as the projectile assets are linked.

## OpenMOC Case JSON Format

Source and follow-up case files share the same schema:

```json
{
  "geometry": {
    "x_extent_cm": 1.26,
    "y_extent_cm": 1.26,
    "z_extent_cm": 1.0,
    "fuel_radius_cm": 0.4
  },
  "tracking": {
    "num_azim": 4,
    "azim_spacing_cm": 0.1,
    "z_coord_cm": 0.0
  },
  "solver": {
    "convergence_threshold": 1.0e-3,
    "max_iters": 50,
    "num_threads": 1
  },
  "materials": {
    "fuel": {
      "num_groups": 2,
      "sigma_t":    [0.222222, 0.833333],
      "sigma_a":    [0.010120, 0.080032],
      "sigma_s":    [0.192423, 0.020000, 0.000000, 0.753300],
      "nu_sigma_f": [0.006400, 0.156500],
      "sigma_f":    [0.002500, 0.066600],
      "chi":        [1.000000, 0.000000]
    },
    "moderator": {
      "num_groups": 2,
      "sigma_t":    [0.230000, 1.530000],
      "sigma_a":    [0.000400, 0.020000],
      "sigma_s":    [0.219000, 0.010600, 0.000000, 1.510000],
      "nu_sigma_f": [0.0,      0.0],
      "sigma_f":    [0.0,      0.0],
      "chi":        [0.0,      0.0]
    }
  }
}
```

The runner expects exactly two named materials (`fuel`, `moderator`). `sigma_s` is row-major with shape `[num_groups * num_groups]`. The two materials' cross sections are placeholders chosen to give a stable, near-critical k_eff for the source case — the exact magnitude of k_eff is not asserted; only the directional inequality is.

## Runner Output JSON

```json
{
  "k_eff": 1.18234,
  "iterations": 17,
  "converged": true,
  "metadata": {
    "runner": "openmoc",
    "openmoc_version": "..."
  }
}
```

## Adapter `parse-output` Output

Per the spec contract (`{values, metadata}`):

```json
{
  "values": { "k_eff": 1.18234, "iterations": 17.0, "converged": 1.0 },
  "metadata": { "adapter": "openmoc", "outputFile": "<absolute path>" }
}
```

`iterations` and `converged` are emitted as numbers so `PythonOutputAdapter` (which expects `Dictionary<string, double>`) accepts them.

## Adapter `transform-input` Output

```json
{
  "transformation": "ScaleNuSigmaF",
  "source": "<abs source path>",
  "output": "<abs output path>",
  "params": { "factor": 1.5 },
  "log": "Scaled fuel.nu_sigma_f by 1.5: [0.00640, 0.15650] -> [0.00960, 0.23475]"
}
```

This matches the schema produced by `example_output_adapter.py` so `PythonInputAdapter.ParseLog` can pull `log` without changes.

---

## Task 1: Add the OpenMOC Output Adapter (parse-output) and Its Unit Tests

**Files:**

- Create: `SUT/openmoc/openmoc_output_adapter.py`
- Create: `MetBench_SystemMT.Tests/SystemMT/OpenMocOutputAdapterTests.cs`
- Modify: `MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj` (link `SUT/openmoc/*.py` to `TestAssets/openmoc/`).

- [ ] **Step 1: Write the failing test**

`OpenMocOutputAdapterTests.cs` exercises the script via the standard `PythonOutputAdapter` since the adapter is JSON-only and runs on system `python3`. Cover three cases:

1. **Happy path:** create a temp file with `{"k_eff": 1.234, "iterations": 7, "converged": true, "metadata": {...}}` and assert `values["k_eff"] ≈ 1.234`, `values["iterations"] == 7.0`, `values["converged"] == 1.0`, `metadata["adapter"] == "openmoc"`.
2. **Missing file:** delete the path and assert `PythonOutputAdapter.ParseAsync` throws `InvalidOperationException` mentioning "Adapter failure".
3. **Malformed JSON:** write garbage and assert the same exception class.

Test file imports `MetBench_BLL.SystemMT` and uses `TestAssetPaths.AssetRoot()` joined with `openmoc/openmoc_output_adapter.py`. Run: `dotnet test --filter FullyQualifiedName~OpenMocOutputAdapter`. Expect 3 failures (asset missing).

- [ ] **Step 2: Implement**

Write `SUT/openmoc/openmoc_output_adapter.py` with the same shape as `projectile_output_adapter.py`:

```python
def parse_output(output_file: str) -> dict:
    payload = json.loads(Path(output_file).read_text(encoding="utf-8"))
    values = {
        "k_eff": float(payload["k_eff"]),
        "iterations": float(payload["iterations"]),
        "converged": 1.0 if bool(payload["converged"]) else 0.0,
    }
    return {"values": values, "metadata": {"adapter": "openmoc", "outputFile": str(Path(output_file).resolve())}}
```

`main()` parses the `parse-output --output-file` argv shape and prints the JSON.

Update the `.csproj`:

```xml
<None Include="..\SUT\openmoc\*.py">
  <Link>TestAssets\openmoc\%(Filename)%(Extension)</Link>
  <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
</None>
<None Include="..\SUT\openmoc\sample\*.json">
  <Link>TestAssets\openmoc\sample\%(Filename)%(Extension)</Link>
  <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
</None>
```

- [ ] **Step 3: Run all tests; verify 26 (baseline) + 3 = 29 passed**

- [ ] **Step 4: Spec-compliance review (subagent)** — confirm the adapter respects the "Python adapters must not decide whether the MR passed / control execution / write database / read feature files" rule from the spec.

- [ ] **Step 5: Code-quality review (subagent)** — confirm the adapter has no dead branches, no print-debugging, and matches the projectile adapter style.

- [ ] **Step 6: Commit** with message `feat(stage3): openmoc output adapter (parse-output) + tests`.

---

## Task 2: Add the OpenMOC Input Adapter (transform-input) and Its Unit Tests

**Files:**

- Create: `SUT/openmoc/openmoc_input_adapter.py`
- Create: `MetBench_SystemMT.Tests/SystemMT/OpenMocInputAdapterTests.cs`

- [ ] **Step 1: Write the failing test**

Three cases in `OpenMocInputAdapterTests.cs`:

1. **Happy path:** write a small source JSON with `fuel.nu_sigma_f = [0.006, 0.156]`, call `PythonInputAdapter.TransformAsync` with `factor=1.5`, assert the resulting follow-up JSON has `fuel.nu_sigma_f == [0.009, 0.234]` (within 1e-9), and that all other arrays are byte-for-byte identical.
2. **Factor must be > 0:** `params={"factor":"-1"}` produces an `InvalidOperationException` from C# because the adapter exits non-zero.
3. **Missing factor parameter:** `params={}` produces an `InvalidOperationException`.

- [ ] **Step 2: Implement** `SUT/openmoc/openmoc_input_adapter.py`:

```python
def transform_input(source_file, output_file, params_json):
    params = json.loads(params_json)
    factor = float(params["factor"])
    if factor <= 0:
        raise ValueError(f"factor must be > 0 (got {factor})")
    case = json.loads(Path(source_file).read_text(encoding="utf-8"))
    before = list(case["materials"]["fuel"]["nu_sigma_f"])
    case["materials"]["fuel"]["nu_sigma_f"] = [v * factor for v in before]
    after = case["materials"]["fuel"]["nu_sigma_f"]
    Path(output_file).parent.mkdir(parents=True, exist_ok=True)
    Path(output_file).write_text(json.dumps(case, indent=2) + "\n", encoding="utf-8")
    return {
        "transformation": "ScaleNuSigmaF",
        "source": str(Path(source_file).resolve()),
        "output": str(Path(output_file).resolve()),
        "params": {"factor": factor},
        "log": f"Scaled fuel.nu_sigma_f by {factor}: {before} -> {after}",
    }
```

`main()` parses `transform-input --source-file --output-file --params` and prints the JSON. Errors (KeyError, ValueError) propagate to a non-zero exit with the message on stderr.

- [ ] **Step 3: Run all tests; verify 29 + 3 = 32 passed**

- [ ] **Step 4: Spec-compliance review (subagent)** — confirm the script does not import openmoc (only stdlib) and does not write into MetBench's database / control flow.

- [ ] **Step 5: Code-quality review (subagent)**

- [ ] **Step 6: Commit** with message `feat(stage3): openmoc input adapter (transform-input ScaleNuSigmaF) + tests`.

---

## Task 3: Add the OpenMOC Sample Pin-cell Case JSON

**Files:**

- Create: `SUT/openmoc/sample/pincell.json` (the JSON in "OpenMOC Case JSON Format" above).

- [ ] **Step 1: Write the case file** with the cross sections from the plan above.

- [ ] **Step 2: Add a quick C# unit test** `OpenMocSampleCaseTests.cs` that loads the JSON via `JsonDocument` and asserts the schema contract: presence of `geometry`, `materials.fuel.num_groups == 2`, `len(materials.fuel.nu_sigma_f) == 2`, `len(materials.fuel.sigma_s) == 4`. This guards the adapter contract.

- [ ] **Step 3: Run all tests; verify 32 + 1 = 33 passed**

- [ ] **Step 4: Commit** with message `feat(stage3): sample pin-cell case JSON for openmoc MR`.

---

## Task 4: Add the OpenMOC Runner Script and an End-to-End Smoke Test

**Files:**

- Create: `SUT/openmoc/openmoc_runner.py`
- Create: `MetBench_SystemMT.Tests/SystemMT/OpenMocTestPaths.cs`
- Create: `MetBench_SystemMT.Tests/SystemMT/OpenMocRunnerSmokeTests.cs`

- [ ] **Step 1: Write `OpenMocTestPaths`** (test helper, not BLL):

```csharp
internal static class OpenMocTestPaths
{
    public static string OpenMocPython()
    {
        var configured = Environment.GetEnvironmentVariable("METBENCH_OPENMOC_PYTHON");
        if (!string.IsNullOrWhiteSpace(configured)) return configured;
        if (File.Exists("/opt/openmoc-venv/bin/python")) return "/opt/openmoc-venv/bin/python";
        return TestAssetPaths.PythonExecutable();
    }

    public static bool OpenMocImportable()
    {
        try
        {
            var p = Process.Start(new ProcessStartInfo(OpenMocPython(), "-c \"import openmoc\"")
            { UseShellExecute = false, RedirectStandardError = true, RedirectStandardOutput = true });
            p!.WaitForExit(10_000);
            return p.ExitCode == 0;
        }
        catch { return false; }
    }
}
```

- [ ] **Step 2: Write the failing smoke test** (`OpenMocRunnerSmokeTests.cs`):

```csharp
[SkippableFact]
public async Task Runner_solves_pincell_and_writes_keff()
{
    Skip.IfNot(OpenMocTestPaths.OpenMocImportable(), "OpenMOC not importable; install via .claude/web-setup.sh");

    var inputPath  = Path.Combine(TestAssetPaths.AssetRoot(), "openmoc", "sample", "pincell.json");
    var workDir    = Path.Combine(Path.GetTempPath(), "MetBench-Stage3-Smoke", Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(workDir);
    var outputPath = Path.Combine(workDir, "output.json");

    var psi = new ProcessStartInfo(OpenMocTestPaths.OpenMocPython(),
        $"\"{Path.Combine(TestAssetPaths.AssetRoot(), "openmoc", "openmoc_runner.py")}\" --input \"{inputPath}\" --output \"{outputPath}\"")
    { UseShellExecute = false, RedirectStandardError = true, RedirectStandardOutput = true };
    var p = Process.Start(psi)!;
    await p.WaitForExitAsync();
    Assert.True(p.ExitCode == 0, p.StandardError.ReadToEnd());

    using var doc = JsonDocument.Parse(await File.ReadAllTextAsync(outputPath));
    Assert.True(doc.RootElement.TryGetProperty("k_eff", out var k));
    Assert.True(k.GetDouble() > 0);
    Assert.True(doc.RootElement.GetProperty("converged").GetBoolean());
}
```

Add `Xunit.SkippableFact.MSBuild` package or use a manual `Skip` pattern (xUnit 2.9 has `Assert.Skip`). Document in the test file.

- [ ] **Step 3: Implement `openmoc_runner.py`** — minimal pin-cell builder:

```python
import argparse, json, sys
from pathlib import Path

def build_material(name, mat_dict):
    import openmoc
    m = openmoc.Material(name=name)
    m.setNumEnergyGroups(int(mat_dict["num_groups"]))
    m.setSigmaT(mat_dict["sigma_t"])
    m.setSigmaA(mat_dict["sigma_a"])
    m.setSigmaS(mat_dict["sigma_s"])
    m.setNuSigmaF(mat_dict["nu_sigma_f"])
    m.setSigmaF(mat_dict["sigma_f"])
    m.setChi(mat_dict["chi"])
    return m

def solve(case):
    import openmoc
    import openmoc.log as log
    log.set_log_level("ERROR")

    fuel_mat = build_material("fuel", case["materials"]["fuel"])
    mod_mat  = build_material("moderator", case["materials"]["moderator"])
    g = case["geometry"]; t = case["tracking"]; sv = case["solver"]
    half_x, half_y, half_z = g["x_extent_cm"]/2, g["y_extent_cm"]/2, g["z_extent_cm"]/2

    xmin = openmoc.XPlane(x=-half_x); xmax = openmoc.XPlane(x= half_x)
    ymin = openmoc.YPlane(y=-half_y); ymax = openmoc.YPlane(y= half_y)
    zmin = openmoc.ZPlane(z=-half_z); zmax = openmoc.ZPlane(z= half_z)
    for s in (xmin, xmax, ymin, ymax, zmin, zmax):
        s.setBoundaryType(openmoc.REFLECTIVE)
    fuel_cyl = openmoc.ZCylinder(x=0.0, y=0.0, radius=g["fuel_radius_cm"])

    fuel_cell = openmoc.Cell(name="fuel"); fuel_cell.setFill(fuel_mat); fuel_cell.addSurface(-1, fuel_cyl)
    mod_cell  = openmoc.Cell(name="moderator"); mod_cell.setFill(mod_mat); mod_cell.addSurface(+1, fuel_cyl)
    for cell in (fuel_cell, mod_cell):
        cell.addSurface(+1, xmin); cell.addSurface(-1, xmax)
        cell.addSurface(+1, ymin); cell.addSurface(-1, ymax)
        cell.addSurface(+1, zmin); cell.addSurface(-1, zmax)

    root = openmoc.Universe(name="root")
    root.addCell(fuel_cell); root.addCell(mod_cell)
    geom = openmoc.Geometry(); geom.setRootUniverse(root); geom.initializeFlatSourceRegions()

    tg = openmoc.TrackGenerator(geom, num_azim=t["num_azim"], azim_spacing=t["azim_spacing_cm"])
    tg.setZCoord(t["z_coord_cm"]); tg.generateTracks()

    solver = openmoc.CPUSolver(tg)
    solver.setNumThreads(int(sv["num_threads"]))
    solver.setConvergenceThreshold(float(sv["convergence_threshold"]))
    solver.computeEigenvalue(int(sv["max_iters"]))
    return float(solver.getKeff()), int(solver.getNumIterations()), True

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--input", required=True); ap.add_argument("--output", required=True)
    a = ap.parse_args()
    case = json.loads(Path(a.input).read_text(encoding="utf-8"))
    k, n_iter, converged = solve(case)
    Path(a.output).parent.mkdir(parents=True, exist_ok=True)
    Path(a.output).write_text(json.dumps({
        "k_eff": k, "iterations": n_iter, "converged": converged,
        "metadata": {"runner": "openmoc"},
    }, indent=2) + "\n", encoding="utf-8")
    return 0

if __name__ == "__main__":
    sys.exit(main())
```

- [ ] **Step 4: Run smoke test against the venv python**

```bash
METBENCH_OPENMOC_PYTHON=/opt/openmoc-venv/bin/python \
  dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj \
    --filter FullyQualifiedName~OpenMocRunnerSmoke
```

Expect 1 passed, 0 skipped. If openmoc isn't importable, expect 0 passed, 1 skipped. Either way the existing 33 tests stay green.

- [ ] **Step 5: Spec-compliance review (subagent)** — confirm the runner only computes; it does not parse the MR, doesn't compare source vs follow-up, doesn't write the assertion result.

- [ ] **Step 6: Code-quality review (subagent)** — confirm `getNumIterations` actually exists on `CPUSolver` (verify in `/opt/openmoc-venv/bin/python -c 'import openmoc; print([x for x in dir(openmoc.CPUSolver) if "Iter" in x])'`); if not, fall back to a constant `0` for `iterations` so the JSON shape is still stable.

- [ ] **Step 7: Commit** with message `feat(stage3): openmoc runner script + smoke test`.

---

## Task 5: Wire the BDD Scenario

**Files:**

- Create: `MetBench_SystemMT.Tests/Features/OpenMocPinCellNuSigmaF.feature`
- Create: `MetBench_SystemMT.Tests/Steps/OpenMocPinCellNuSigmaFSteps.cs`

- [ ] **Step 1: Write the feature file**

```gherkin
Feature: OpenMOC pin-cell MR - scaling fuel nu*sigma_f increases k_eff

  Background:
    The dominant eigenvalue (k_eff) of a fixed transport problem with reflective
    boundaries strictly increases when the production operator (nu*sigma_f) is
    uniformly scaled by a factor greater than 1, with absorption and scattering
    held constant. Stage 3 validates this MR end-to-end through OpenMOC.

  Scenario: Follow-up k_eff exceeds source k_eff after scaling nu*sigma_f
    Given an OpenMOC pin-cell source case from "openmoc/sample/pincell.json"
    And the MR transformation "ScaleNuSigmaF" with parameter "factor" set to "1.5"
    When I run source and the generated follow-up through OpenMOC
    Then the parsed value "k_eff" of the generated follow-up should be greater than the source
```

- [ ] **Step 2: Write the failing step definitions** (`OpenMocPinCellNuSigmaFSteps.cs`)

The step bindings construct `SystemProgram(Python, "openmoc-pincell", openmocPython, "openmoc_runner.py --input {input} --output {output}", openmoc_output_adapter.py)`, build a `SystemMtTask.WithGeneratedFollowUp` with `MrTransformation("ScaleNuSigmaF", {"factor": "1.5"})`, run it through `SystemMtRunner`, and assert `_result.Passed` for the `GreaterThan` MR on `k_eff`.

The `When` step probes `OpenMocTestPaths.OpenMocImportable()` and calls `Assert.Skip` (or `throw new SkipException(...)`) if false, so this scenario is skip — not fail — when openmoc is not installed.

- [ ] **Step 3: Run the feature**

```bash
METBENCH_OPENMOC_PYTHON=/opt/openmoc-venv/bin/python \
  dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj
```

Expect 33 (prior) + 1 (smoke) + 1 (BDD) = 35 passed; or 34 passed + 1 skipped if openmoc isn't installed. **All other tests stay green** (this is a hard regression gate).

- [ ] **Step 4: Spec-compliance review (subagent)**

Verify against `docs/superpowers/specs/2026-05-07-system-level-mt-bdd-design.md`:

- [ ] MetBench/Reqnroll starts OpenMOC for a source case;
- [ ] The system generates a follow-up case and starts the second run;
- [ ] OpenMOC output files are parsed for MR-relevant values;
- [ ] At least one OpenMOC MR executes end-to-end and returns pass/fail;
- [ ] OpenMOC-specific logic remains isolated in a Python adapter (no `OpenMOC` token appears in `MetBench_BLL.Core/`).

Block on any unmet item.

- [ ] **Step 5: Code-quality review (subagent)**

- [ ] **Step 6: Commit** with message `feat(stage3): openmoc pin-cell MR BDD scenario`.

---

## Task 6: Re-verify Compatibility

- [ ] Run the full SystemMT suite without `METBENCH_OPENMOC_PYTHON`. The Stage 3 BDD scenario and smoke test should be **skipped** (not failed) if `/opt/openmoc-venv/bin/python` does not exist on the executing machine. Confirm baseline `26/26 + 3 (output) + 3 (input) + 1 (sample) = 33` pass, plus 2 skipped.
- [ ] Run with `METBENCH_OPENMOC_PYTHON` set and confirm `35 passed, 0 skipped`.
- [ ] Confirm `git grep -i openmoc -- 'MetBench_BLL.Core/**'` returns **zero matches** — proves the C# core is OpenMOC-agnostic.
- [ ] Confirm the projectile MR scenario still passes.

## Task 7: Push and Open PR

- [ ] `git push -u origin feature/stage3-openmoc`.
- [ ] Open PR titled `feat(stage3): OpenMOC single-program MR (k_eff > source after ScaleNuSigmaF)` against `main`. Body summarizes the MR, the architectural promise (zero new C# code), and the env requirements (`.claude/web-setup.sh`).

## Out-of-band Follow-ups (not part of this PR)

- `web-setup.sh` requires three workarounds in this cloud sandbox: (1) `dot.net` and Microsoft CDN URLs return 403; install .NET via `packages.microsoft.com` apt repo. (2) The apt repo only ships `dotnet-sdk-8.0` and `dotnet-sdk-10.0`; the codebase already targets `net8.0`, so 8.0 is sufficient and the script's `--channel 9.0` requirement is incorrect. (3) `cli.github.com` is blocked; `gh` ships in Ubuntu universe. (4) Default `python3` on this image is 3.11 but apt-installed `python3-numpy/h5py/matplotlib` are built for 3.12; OpenMOC must be installed into a `python3.12` venv with `--system-site-packages`. (5) OpenMOC's `setup.py install` does not embed `_openmoc.so` into the wheel; manually copy `build/lib/_openmoc*.so` and `openmoc/openmoc.py` after `pip install`. (6) OpenMOC's Python sources still use `from collections import Iterable`; patch to `collections.abc`. These belong to a separate `chore(setup): cloud-env workarounds` PR — track them outside Stage 3 acceptance.

---

## Done When

- [ ] All Stage 3 acceptance criteria from the spec are demonstrably met (one OpenMOC MR end-to-end pass).
- [ ] No new C# files are added to `MetBench_BLL.Core/`.
- [ ] No file under `MetBench_BLL/` (the WPF project) is modified.
- [ ] `dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj` is green with `METBENCH_OPENMOC_PYTHON` set.
- [ ] Without `METBENCH_OPENMOC_PYTHON`, the suite still passes (skipped scenarios documented).
