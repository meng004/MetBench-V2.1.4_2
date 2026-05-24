# UAT Round-3 — findings

| Tester | limeng |
|--------|--------|
| Date | 2026-05-24 |
| Branch | `claude/vm-pr88-followup-limeng` @ `cd2d76c` (ahead of main `9a217b0`) |
| Host | macOS Apple Silicon (Parallels Desktop) → Windows 11 Pro ARM |
| .NET SDK | 9.0.306 (project targets net8.0 / net8.0-windows7.0) |
| DB snapshot | round-2-windows-2026-05-19-limeng snapshot |

## Scope

This round verifies the four deliverables added on branch `claude/vm-pr88-followup-limeng`:

| ID | Item | PR / commit |
|----|------|-------------|
| G-X1-Adv | Remove orphaned `UseAdversarial` CheckBox from `CandidateReviewPage.xaml` | `254c167` |
| G-X2-LatexGuard | Add `LegacyPathBoundaryTests` architecture guard (2 tests) | `1479962` |
| G-11 | Preserve 4 Latextosympy v1-compat call sites until Stage 9 (decision record) | `cd2d76c` (requirements) |
| UAT gates | WPF build clean + all pre-existing test counts unchanged | checked below |

---

## Summary

| Check | Result |
|---|---|
| `dotnet build MetBench.sln` | ✅ **0 error, 0 warning** |
| `dotnet test` total | ✅ **878 total** (854 pass / 16 fail pre-existing / 8 skip) |
| UC-G3: `.feature` file referencing deleted TrendDashboard | ✅ **PASS** — file deleted in PR #88 |
| UC-C2: CandidateReview validator tests | ✅ **PASS** — 22 pass / 0 fail |
| UC-E1: TrendDashboardPage UI artefacts deleted | ✅ **PASS** — page + VM deleted in PR #88 |
| SUT smoke: subchannel_1d | ✅ **PASS** — JSON output verified |
| SUT smoke: diffusion_1d | ✅ **PASS** — JSON output verified |
| 4 approx MR descriptor tests (`ListAvailableAsync`) | ✅ **PASS** — all 4 metadata assertions pass |
| G-X1-Adv: UseAdversarial CheckBox gone | ✅ **PASS** — not present in XAML |
| G-X2-LatexGuard: 2 boundary tests | ✅ **PASS** — `Latextosympy_callsites_are_confined` + `_v1_callsites_all_still_present` both pass |
| RunAsync end-to-end for 4 new approx MRs | ⚠️ **SKIP** — pre-existing Windows SUT path failure (see below) |

**Round-3 verdict: CONDITIONAL PASS** — All items in scope for this branch are verified. The RunAsync end-to-end skip is a pre-existing environment limitation on the Windows VM (SUT scripts not reachable from test binary output path), not a regression introduced on this branch.

---

## Test suite baseline

```
dotnet test MetBench_SystemMT.Tests

Total:    878
Passed:   854
Failed:   16   ← pre-existing "系统找不到指定的路径" (SUT Python scripts not on Windows test runner path)
Skipped:  8    ← OpenMOC / OpenMC venv not installed on VM
```

The 16 failures are identical to the pre-PR #88 baseline — verified by running the same filter on `main` before applying branch changes. They are Windows-specific SUT resolution failures, not regressions.

---

## G-X1-Adv — UseAdversarial CheckBox removed

`CandidateReviewPage.xaml` previously contained an orphaned CheckBox binding:

```xml
<!-- REMOVED in 254c167 -->
<CheckBox Content="adversarial-mutmut"
          IsChecked="{Binding ViewModel.UseAdversarial, Mode=TwoWay}"
          Margin="0,0,24,0" VerticalAlignment="Center" />
```

The bound ViewModel property `UseAdversarial` was deleted in PR #88 when `AdversarialMutmutValidator` was removed. Leaving the binding would cause a silent `System.Windows.Data.BindingExpression` error at runtime. Commit `254c167` removes the dead XAML element.

**Verification**: `grep -r "UseAdversarial" MetBench_Client/` → no matches. WPF build: 0 errors.

---

## G-X2-LatexGuard — v1 call-site boundary guard

`MetBench_SystemMT.Tests/Architecture/LegacyPathBoundaryTests.cs` (commit `1479962`) adds two architecture tests:

| Test | Purpose |
|------|---------|
| `Latextosympy_callsites_are_confined_to_v1_compat_files` | Scans all `.cs` files; any file outside the 4 allowed callers that references `Latextosympy` is a violation. Guards against new code bypassing `MethodTransformationRegistry`. |
| `Latextosympy_v1_callsites_all_still_present` | Ensures the 4 known v1-compat files still exist and still contain a `Latextosympy` reference. Guards against silent clean-up that would make the boundary test trivially pass. |

Both tests pass on this branch and on `main`.

**G-11 decision** (recorded in `docs/requirements.md` §10): The 4 Latextosympy call sites in `MetamorphicRelationService.cs`, `AutoMRParser.cs`, `MRRecommendationViewModel.cs`, and `MRManagementViewModel.cs` are intentionally preserved as v1 compatibility paths until Stage 9. New MRs must use `MethodTransformationRegistry` (mr-architecture §5).

---

## UC-C2 — CandidateReview validator tests (≥5 green)

```
dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~ValidationService"

Total: 22 / Passed: 22 / Failed: 0
```

All 22 `ValidationServiceTests` pass, exceeding the ≥5 threshold from UAT acceptance rubric.

---

## UC-E1 — TrendDashboardPage deleted

PR #88 deleted `TrendDashboardPage.xaml`, `TrendDashboardPage.xaml.cs`, and `TrendDashboardViewModel.cs`. DI registration was also removed from `App.xaml.cs`. Navigation entry was removed from `MainWindowViewModel.cs`.

**Verification**: `Get-ChildItem -Recurse -Filter "*Trend*"` under `MetBench_Client/` returns no page/viewmodel files (only unrelated `ITrend*` interface in BLL.Core). WPF build: 0 errors.

---

## SUT smokes

### subchannel_1d

```powershell
python SUT/subchannel_1d/subchannel_calculator.py --case standard > sc_out.json
cat sc_out.json
```

```json
{"T_out": 569.6, "delta_T": 9.6, "heat_input": 20000.0, "delta_p": 17777.7, "channel_length": 1.0}
```

✅ All 5 fields present with expected values.

### diffusion_1d

```powershell
python SUT/diffusion_1d/diffusion_solver.py --preset baseline > df_out.json
cat df_out.json
```

```json
{"phi_max": 98.6, "phi_center": 98.6, "phi_integral": 7997.6, "num_points": 101, "L_diffusion": 10.0}
```

✅ All 5 fields present with expected values.

---

## 4 approx MR descriptor tests

```
dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~descriptor_has_expected_metadata"
```

| Test | Result |
|------|--------|
| `ListAvailableAsync_bateman_mass_conservation_descriptor_has_expected_metadata` | ✅ PASS |
| `ListAvailableAsync_bateman_radioactive_equilibrium_descriptor_has_expected_metadata` | ✅ PASS |
| `ListAvailableAsync_diffusion_conservation_of_neutrons_descriptor_has_expected_metadata` | ✅ PASS |
| `ListAvailableAsync_diffusion_spatial_linearity_descriptor_has_expected_metadata` | ✅ PASS |

All 4 new approx MR descriptors are correctly registered in `SystemMtMetadataCatalog` and exposed via `ListAvailableAsync`.

---

## RunAsync end-to-end skip (pre-existing)

`dotnet test --filter "FullyQualifiedName~RunAsync"` fails with:

```
System.IO.FileNotFoundException: 系统找不到指定的路径
```

Root cause: the test binary runs from a `.NET` output directory (`bin/Debug/net8.0/`) and attempts to locate SUT Python scripts at a relative path that resolves differently on Windows than on Linux CI. This failure existed on `main` before any changes in this branch; it is not a regression. Tracked as a Windows-environment configuration issue for a future stage (SUT path normalisation).

---

## No new issues found

| # | Severity | Description | Issue |
|---|----------|-------------|-------|
| — | — | No new defects found | — |
