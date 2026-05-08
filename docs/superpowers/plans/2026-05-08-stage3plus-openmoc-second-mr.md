# Stage 3+: Second OpenMOC MR (LessThan + ScaleFuelSigmaA)

> **For agentic workers:** Implement task-by-task. Each task is TDD: failing test first, then implementation, then a non-regressing dotnet test run. Two-phase review (spec compliance + code quality) at the end.

**Goal:** Add a second OpenMOC metamorphic relation that complements the Stage 3 GreaterThan/ScaleNuSigmaF MR with the opposite direction: scaling the fuel material's absorption cross-section makes k_eff strictly decrease, so the assertion is `LessThan`. This validates that the system-MT abstraction is reusable across MRs and finally satisfies the spec's "expose an interface so later stages can add ... assertions without changing Reqnroll steps" rule by introducing `IMrAssertion`.

**MR scenario:** 2D pin-cell with reflective boundaries and 2-energy-group cross sections (same sample case as Stage 3). The transformation `ScaleFuelSigmaA` multiplies the fuel material's per-group absorption cross section by a configured `factor > 1`. Implementation detail: OpenMOC takes `sigma_t` and `sigma_s` (not `sigma_a` directly); the adapter therefore computes `delta_t_g = (factor - 1) * sigma_a_g` per group from the documented `sigma_a` field, adds `delta_t_g` to `sigma_t_g`, and updates the `sigma_a` field in the JSON for documentation consistency. Scattering is left untouched. The new effective absorption is `factor * old_absorption`, while production (`nu*sigma_f`) and scattering (`sigma_s`) are unchanged.

**Math:** With production fixed and absorption scaled by c > 1, the dominant eigenvalue of `(1/k)F·phi = (T - S)·phi` strictly decreases (T grows by `(c-1) * sigma_a` per group while S is unchanged, so the LHS over RHS ratio grows for any non-trivial flux distribution → k must shrink to compensate). Empirically verified during planning: with the Stage 3 sample case and factor=1.5, k_eff drops from 1.133 to 0.809 (ratio 0.71).

**Architecture goal:** Land the spec rule "[the assertion layer should] expose an interface so later stages can add ... assertions without changing Reqnroll steps." Introduce `IMrAssertion`, make `GreaterThanAssertion` implement it, refactor `SystemMtRunner` to accept `IEnumerable<IMrAssertion>`, update all call sites, and confirm DI in `MetBench_Client/App.xaml.cs` resolves correctly.

**Tech stack:** .NET 8 (`MetBench_BLL.Core`, light DI changes in `MetBench_Client/App.xaml.cs` only), xUnit 2.9, Reqnroll.xUnit 3.3, Python stdlib (no openmoc import in the input adapter).

---

## Scope guard

This plan adds:

- One new C# interface (`IMrAssertion`).
- One new C# concrete assertion (`LessThanAssertion`) plus its unit tests.
- A SystemMtRunner refactor (constructor signature change: 3rd positional arg becomes `IEnumerable<IMrAssertion>`; assertion dispatch goes through a name-keyed dictionary built from that collection).
- One new Python input adapter (`openmoc_input_adapter_sigma_a.py`).
- One new BDD scenario (`OpenMocPinCellSigmaA.feature`) and its step bindings.
- Whatever is required at SystemMtRunner call sites to compile (every existing test must still pass; the breaking-change is internal to the .NET solution).

This plan does NOT:

- Add a new C# project (`MetBench_BLL.OpenMOC` etc.).
- Touch the WPF `MetBench_BLL` project's source files (`MetBench_Client/App.xaml.cs` is allowed because that's the DI composition root).
- Add OpenMOC-specific code to `MetBench_BLL.Core/SystemMT/*.cs`.
- Modify the existing `openmoc_runner.py`, `openmoc_input_adapter.py`, or `openmoc_output_adapter.py` (if any change is needed there, this plan is wrong).
- Modify the existing `OpenMocPinCellNuSigmaF.feature` or its step bindings.
- Touch persistence, WPF UI, or report generation (Stage 4).

## File structure

Create:

- `MetBench_BLL.Core/SystemMT/IMrAssertion.cs`
- `MetBench_BLL.Core/SystemMT/LessThanAssertion.cs`
- `MetBench_SystemMT.Tests/SystemMT/LessThanAssertionTests.cs`
- `SUT/openmoc/openmoc_input_adapter_sigma_a.py`
- `MetBench_SystemMT.Tests/SystemMT/OpenMocSigmaAInputAdapterTests.cs`
- `MetBench_SystemMT.Tests/Features/OpenMocPinCellSigmaA.feature`
- `MetBench_SystemMT.Tests/Steps/OpenMocPinCellSigmaASteps.cs`

Modify:

- `MetBench_BLL.Core/SystemMT/GreaterThanAssertion.cs` — implement `IMrAssertion` (add `Name` property, no behavior change).
- `MetBench_BLL.Core/SystemMT/SystemMtRunner.cs` — replace the single `GreaterThanAssertion` field with a `Dictionary<string, IMrAssertion>` built from a constructor-injected `IEnumerable<IMrAssertion>`. Update the assertion dispatch.
- All `SystemMtRunner` call sites (12 places across 7 test files):
  - `MetBench_SystemMT.Tests/Steps/SystemLevelCliMtSteps.cs`
  - `MetBench_SystemMT.Tests/Steps/SystemLevelGeneratedFollowupSteps.cs`
  - `MetBench_SystemMT.Tests/Steps/ProjectileRangeSteps.cs`
  - `MetBench_SystemMT.Tests/Steps/OpenMocPinCellNuSigmaFSteps.cs`
  - `MetBench_SystemMT.Tests/SystemMT/SystemMtRunnerTests.cs`
  - `MetBench_SystemMT.Tests/SystemMT/SystemMtRunnerGeneratedFollowupTests.cs`
- `MetBench_Client/App.xaml.cs` — change `services.AddScoped<GreaterThanAssertion>();` to two `IMrAssertion` registrations.

## Open question deliberately resolved up-front

`MetBench_Client` (WPF) cannot run on this Linux cloud sandbox. The DI registration change must compile (`dotnet build MetBench.sln -p:EnableWindowsTargeting=true` from Linux), but cannot be smoke-tested at runtime here. The change is mechanical (two AddScoped lines instead of one) and directly mirrors the type signature of the new constructor, so a build pass is sufficient.

## Sample case

This plan reuses `SUT/openmoc/sample/pincell.json` as-is. The fuel's `sigma_a` documentation values are slightly inconsistent with `sigma_t - sum(sigma_s)` (off by 0.0003 per group; an artefact of the source dataset). The new ScaleFuelSigmaA adapter trusts the documented `sigma_a` field as the absolute baseline rather than re-deriving it; this keeps the transformation deterministic and decoupled from sigma_s details. The runner does not consume sigma_a, so the cross-check inconsistency is harmless.

---

## Task 1: Introduce `IMrAssertion` and make `GreaterThanAssertion` implement it

**Files:**
- Create: `MetBench_BLL.Core/SystemMT/IMrAssertion.cs`
- Modify: `MetBench_BLL.Core/SystemMT/GreaterThanAssertion.cs`

- [ ] **Step 1**: Create the interface:

```csharp
namespace MetBench_BLL.SystemMT;

public interface IMrAssertion
{
    string Name { get; }
    SystemMtAssertionResult Evaluate(string valueName, ParsedOutput source, ParsedOutput followUp);
}
```

- [ ] **Step 2**: Make `GreaterThanAssertion : IMrAssertion`. Add `public string Name => "GreaterThan";`. Existing `Evaluate(...)` already matches the interface.

- [ ] **Step 3**: `dotnet build MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj` must still compile. No tests change yet.

## Task 2: Refactor `SystemMtRunner` to dispatch via `IMrAssertion` registry

**Files:**
- Modify: `MetBench_BLL.Core/SystemMT/SystemMtRunner.cs`

- [ ] **Step 1**: Change the constructor signature to:

```csharp
public SystemMtRunner(
    CliProgramRunner cliRunner,
    PythonOutputAdapter outputAdapter,
    IEnumerable<IMrAssertion> assertions,
    InputGenerator? inputGenerator = null)
```

Build an internal `IReadOnlyDictionary<string, IMrAssertion>` keyed by `Name` (case-insensitive). Reject duplicate names with `ArgumentException`. Reject empty assertion sets with `ArgumentException`.

- [ ] **Step 2**: Replace the assertion `switch` in `RunAsync` with a dictionary lookup; if `task.AssertionName` is not registered, return the existing `Configuration failure: unsupported assertion '<name>'` shape. The `FailedBeforeRun` and `FailedAfterRun` helpers continue to fabricate a placeholder `SystemMtAssertionResult` with a fixed name; switch their hard-coded "GreaterThan" to `task.AssertionName ?? "Unknown"` so failures attribute to the requested MR.

Wait — `FailedBeforeRun` and `FailedAfterRun` are static helpers that don't see `task`. Either thread `task.AssertionName` through them or accept the cosmetic mismatch (the assertion name in those failure paths reflects "the runner never got to evaluate, here's a placeholder"). Decision: thread it through; failures should attribute correctly.

- [ ] **Step 3**: `dotnet test` must fail at compile time across every call site that passes `new GreaterThanAssertion()` as the third positional arg (it's now `IEnumerable<IMrAssertion>`). This is intentional — the failing build is the to-do list for Task 3.

## Task 3: Update SystemMtRunner call sites + DI

**Files (modify each):**
- `MetBench_SystemMT.Tests/Steps/SystemLevelCliMtSteps.cs`
- `MetBench_SystemMT.Tests/Steps/SystemLevelGeneratedFollowupSteps.cs`
- `MetBench_SystemMT.Tests/Steps/ProjectileRangeSteps.cs`
- `MetBench_SystemMT.Tests/Steps/OpenMocPinCellNuSigmaFSteps.cs`
- `MetBench_SystemMT.Tests/SystemMT/SystemMtRunnerTests.cs`
- `MetBench_SystemMT.Tests/SystemMT/SystemMtRunnerGeneratedFollowupTests.cs`
- `MetBench_Client/App.xaml.cs`

- [ ] **Step 1**: At every test call site, change `new GreaterThanAssertion()` → `new IMrAssertion[] { new GreaterThanAssertion() }`. Preserve the `inputGenerator` 4th argument where present.

- [ ] **Step 2**: In `App.xaml.cs`, replace `services.AddScoped<GreaterThanAssertion>();` with the pair:

```csharp
services.AddScoped<IMrAssertion, GreaterThanAssertion>();
services.AddScoped<IMrAssertion, LessThanAssertion>();  // pre-emptive; LessThanAssertion lands in Task 4
```

(If LessThanAssertion is not yet committed, registering it here will not compile; therefore commit App.xaml.cs in Task 4 alongside LessThanAssertion. In Task 3 only the GreaterThan registration is updated.)

Revised Task 3 Step 2: in `App.xaml.cs`, change the single `AddScoped<GreaterThanAssertion>()` line to `AddScoped<IMrAssertion, GreaterThanAssertion>()`. The MS DI container resolves `IEnumerable<IMrAssertion>` automatically.

- [ ] **Step 3**: `dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj` returns to **35 passed** with `METBENCH_OPENMOC_PYTHON=/opt/openmoc-venv/bin/python`. Compatibility unchanged.

- [ ] **Step 4**: `dotnet build MetBench.sln -p:EnableWindowsTargeting=true` succeeds (Linux cross-compile of the WPF project) — confirms `App.xaml.cs` still compiles after the DI change.

## Task 4: `LessThanAssertion` + unit tests

**Files:**
- Create: `MetBench_BLL.Core/SystemMT/LessThanAssertion.cs`
- Create: `MetBench_SystemMT.Tests/SystemMT/LessThanAssertionTests.cs`
- Modify: `MetBench_Client/App.xaml.cs` — add the second `IMrAssertion` registration.

- [ ] **Step 1**: Write `LessThanAssertionTests.cs` mirroring `GreaterThanAssertionTests.cs`: three tests covering pass (follow-up < source), fail (follow-up >= source), missing-value-in-source, missing-value-in-follow-up. Expect failure (red).

- [ ] **Step 2**: Implement `LessThanAssertion` as a structural mirror of `GreaterThanAssertion`. Comparison is `followUpValue < sourceValue`; failure message reverses the direction text.

- [ ] **Step 3**: Add `services.AddScoped<IMrAssertion, LessThanAssertion>();` in `App.xaml.cs` (sibling line to the GreaterThan registration).

- [ ] **Step 4**: `dotnet test` reports **35 + 4 = 39 passed**.

## Task 5: ScaleFuelSigmaA Python input adapter + unit tests

**Files:**
- Create: `SUT/openmoc/openmoc_input_adapter_sigma_a.py`
- Create: `MetBench_SystemMT.Tests/SystemMT/OpenMocSigmaAInputAdapterTests.cs`

- [ ] **Step 1**: `OpenMocSigmaAInputAdapterTests.cs` covers three cases like `OpenMocInputAdapterTests`: happy path with factor 1.5 (verifies `sigma_a` per-group scales as expected, `sigma_t` per-group increases by `(factor - 1) * old_sigma_a` per group, `sigma_s` and `nu_sigma_f` unchanged); rejection of factor <= 0; rejection of missing factor. Expect 3 failures (file not present yet).

- [ ] **Step 2**: Implement the adapter. The transformation:

```python
factor = float(params["factor"])
fuel = case["materials"]["fuel"]
old_sigma_a = list(fuel["sigma_a"])
old_sigma_t = list(fuel["sigma_t"])
delta_t = [(factor - 1.0) * a for a in old_sigma_a]
fuel["sigma_t"] = [t + d for t, d in zip(old_sigma_t, delta_t)]
fuel["sigma_a"] = [a * factor for a in old_sigma_a]
```

The other arrays are unchanged. The log line reports both old/new sigma_a and the resulting sigma_t adjustment, so the BDD log gives a complete trail.

The script's CLI surface is identical to `openmoc_input_adapter.py` (subparser `transform-input`, args `--source-file`, `--output-file`, `--params`, identical stdout JSON shape with `transformation: "ScaleFuelSigmaA"`).

- [ ] **Step 3**: `MetBench_SystemMT.Tests.csproj` already links `..\SUT\openmoc\*.py` to `TestAssets/openmoc/`, so the new file is picked up automatically. No csproj change needed.

- [ ] **Step 4**: `dotnet test` reports **39 + 3 = 42 passed**.

## Task 6: BDD scenario

**Files:**
- Create: `MetBench_SystemMT.Tests/Features/OpenMocPinCellSigmaA.feature`
- Create: `MetBench_SystemMT.Tests/Steps/OpenMocPinCellSigmaASteps.cs`

- [ ] **Step 1**: The feature:

```gherkin
Feature: OpenMOC pin-cell MR - scaling fuel sigma_a decreases k_eff

  Background:
    The dominant eigenvalue (k_eff) strictly decreases when fuel
    absorption is uniformly scaled up while production (nu*sigma_f) and
    scattering (sigma_s) are held constant. This is the directional
    counterpart to the Stage 3 ScaleNuSigmaF MR and validates the
    LessThan assertion.

  Scenario: Follow-up k_eff is less than source k_eff after scaling fuel sigma_a
    Given an OpenMOC pin-cell source case for sigma_a from "openmoc/sample/pincell.json"
    And an OpenMOC MR transformation "ScaleFuelSigmaA" with parameter "factor" set to "1.5"
    When I run source and the generated follow-up through OpenMOC for sigma_a
    Then the OpenMOC parsed value "k_eff" of the generated follow-up should be less than the source
    And the OpenMOC follow-up k_eff should be at most 0.85 times the source k_eff
```

The step text uses an "for sigma_a" suffix on the Given/When that overlap with the Stage 3 scenario, to avoid Reqnroll's ambiguous-binding error. The `And` step `an OpenMOC MR transformation ... with parameter ...` is reused verbatim from `OpenMocPinCellNuSigmaFSteps.cs` — but since each step binding lives in its own class with its own state, the state from the existing class wouldn't be visible here. Solution: the Stage 3+ step file binds its own copy of `an OpenMOC MR transformation ...`. Reqnroll will then error on duplicate. Solution-revised: use a unique step text in the new feature, e.g., `And the OpenMOC sigma_a transformation "ScaleFuelSigmaA" with parameter "factor" set to "1.5"`.

- [ ] **Step 2**: Step bindings construct `SystemProgram` with `openmoc_runner.py` (unchanged) and `openmoc_output_adapter.py` (unchanged); construct `InputGenerator` with the new `openmoc_input_adapter_sigma_a.py`; build `SystemMtTask.WithGeneratedFollowUp(... assertionName: "LessThan", ...)`; construct `SystemMtRunner` with both `GreaterThanAssertion` and `LessThanAssertion` in the assertions array; run; assert `_result.Passed`.

- [ ] **Step 3**: `dotnet test` reports **42 + 1 = 43 passed** with venv; **41 + 2 skipped** without.

## Task 7: Full regression + dual review

- [ ] **Step 1**: Run the full test suite with venv and without; record results.
- [ ] **Step 2**: Run `git diff main..HEAD --stat -- 'MetBench_BLL.Core/SystemMT/*.cs'` and confirm only the four expected files: IMrAssertion.cs (+), LessThanAssertion.cs (+), GreaterThanAssertion.cs (M), SystemMtRunner.cs (M).
- [ ] **Step 3**: Confirm `grep -i openmoc MetBench_BLL.Core/ --include='*.cs'` returns zero (the runner refactor must not leak OpenMOC-isms into Core).
- [ ] **Step 4**: Spawn an independent spec-compliance reviewer. Brief:
  - Stage 3+'s acceptance: a second OpenMOC MR runs end-to-end through the existing pipeline AND the spec rule "expose an interface so later stages can add assertions without changing Reqnroll steps" is now satisfied for the GreaterThan/LessThan pair.
  - Test the rule: pretend a third assertion (`EqualWithTolerance`) is being added. What would change? Answer should be: only a new C# class implementing `IMrAssertion`, plus its DI registration; SystemMtRunner is untouched, the two existing BDD step bindings are untouched.
- [ ] **Step 5**: Spawn an independent code-quality reviewer. Apply blocker fixes.

## Task 8: Push and open PR

- [ ] `git push -u origin feature/stage3plus-openmoc-second-mr`
- [ ] Open PR with body summarising: the new MR, the IMrAssertion refactor, the test deltas (35 → 43 with venv), zero changes outside the planned scope.
- [ ] Subscribe to PR activity.

---

## Done when

- `dotnet test ...` reports **43 passed, 0 skipped, 0 failed** with the OpenMOC venv set; **41 passed, 2 skipped** without.
- `grep -i openmoc MetBench_BLL.Core/SystemMT/*.cs` returns zero.
- Adding a third assertion no longer requires SystemMtRunner or step-binding changes (verified by spec-compliance reviewer's thought experiment).
- The Stage 3 MR (PR #7) keeps passing.
