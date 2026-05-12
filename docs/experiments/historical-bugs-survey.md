# Historical-bug survey — mining OpenMOC + OpenMC fix commits

> Companion to [`historical-bugs.md`](historical-bugs.md) (walkthrough
> analysis of selected cases) and
> [`real-bugs-live-report.md`](real-bugs-live-report.md) (live
> reproduction status).
>
> This file documents the **screening process** we used to pick which
> upstream bugs to investigate, and lists fix commits we evaluated
> with reasons for include/exclude. It is a research-style scan, not
> an exhaustive bug census.

## Sources

* `mit-crpg/OpenMOC` master branch (cloned `--depth=2000` to /tmp).
  ~4 248 commits; ~913 (21%) match `fix|bug|incorrect|wrong|broken|regression|leak|crash` case-insensitive on the subject line.
* `openmc-dev/openmc` develop branch (same depth). ~15 935 commits;
  ~2 296 (14%) match the same fix-pattern.

## Filter criteria

A commit is a **MetBench-relevant fix candidate** iff:

1. It touches code on a path our SUT exercises (eigenvalue, geometry,
   cross-section setup, tally export, source iteration).
2. It is not (a) GUI/plotting, (b) build/CI, (c) documentation only,
   (d) dependency bump, (e) test fixture only.
3. We can predict which MR family **would** catch it if exercised.

Commits that meet 1+3 but not 2 are recorded with reason "out of MR
scope" rather than promoted to walkthrough.

## OpenMOC candidates (top 12 by date, after filtering)

| SHA | Date | Title | Files | Verdict |
|-----|------|-------|-------|---------|
| `28008901` | 2018-09-12 | Fix bug in Keff=fiss/(abs+leak) | `src/CPUSolver.cpp` (1 line) | **Case 1** in walkthrough + live-attempt |
| `8788 2ef5` | 2020-01-01 | Fix loading of nu-transport. Apply SPH factors only to SPH regions. | `src/Material.cpp`, `src/Geometry.cpp` | candidate — MR-family: monotonicity (SPH affects k_eff). Defer to PR-N |
| `e1931 7c7` | 2019-10-04 | Fix rotations for cells with quadratic surfaces, where phi in [0, 2pi] matters | geometry | candidate — MR-family: MR01 rotation. **Strong fit** for our MR01 |
| `83684 aac` | 2019-09-23 | Fix CMFD for small flux regions, add negative flux reset | CMFD solver | out of scope — CMFD not active in our SUT (no `setCmfd()` call) |
| `4f4baf` | 2017-04-18 | Fix bug for OpenMOC v0.1.4 release | release-mgmt | out of scope — release packaging only |
| `01a3e3` | 2017-02-22 | Fix bug in computeFSRSources | solver | candidate — affects k_eff. Walkthrough TBD if scope permits |
| `326cdb` | 2019-10-08 | computefsrfissionratesondevice was wrong | GPU solver | out of scope — GPU path not built |
| `3a695858` | 2019-10-22 | Reduce log volume for low-discr LS cases | LS solver tuning | low-impact — likely not MR-detectable |
| `a413 692b` | 2020-06-15 | Tighten plot similarity criterion | plotter | out of MR scope |
| `5c75 af0b` | 2020-08-16 | Fix array alignment for macosx | macOS build | out of MR scope |
| `e3387570` | 2019-11-26 | Fix cache alignments, optimize 2D LS solver | LS solver perf | low-impact |
| `dcddb 643` | 2020-05-17 | Fix seg fault at clean-up when redefining Ngroups | Material cleanup | out of scope — destructor path |

**Top OpenMOC walkthrough candidates** (in priority order):

1. `28008901` ← already in historical-bugs.md as **Case 1**
2. `e19317c7` (rotation fix for quadratic surfaces) — strong fit for MR01
3. `01a3e3xx` (computeFSRSources) — k_eff path
4. `87882ef5` (nu-transport / SPH factors) — k_eff path

## OpenMC candidates (top 18 by date, after filtering)

| SHA | PR# | Date | Title | Files | Verdict |
|-----|-----|------|-------|-------|---------|
| `dfc80c70` | #3712 | 2026-01-07 | fixing temperatures setting for mgxs | `openmc/mgxs_library.py` | **Case 2** in walkthrough + **live triggered** |
| `10f2b753` | #3708 | 2026-01-06 | Fixing group names in MGXS HDF5 file | `openmc/mgxs/mgxs.py` | **Case 3** in walkthrough (path not currently exercised) |
| `c7d7fa46` | #3692 | 2026-01-06 | Fix a bug in rotational periodic boundary conditions | C++ boundary | candidate — MR-family: MR01 rotation / MR02-MR03 mirror. **High value**, needs C++ build |
| `ef22558f` | #3662 | 2025-11-29 | borated_water temperature assignment | `openmc/model/funcs.py` | candidate — MR-family: MR-T temperature, **pure Python**, easy to live-trigger |
| `bd76fc05` | #3619 | 2025-11-03 | normalization of tally results with no_reduce | C++ tally | candidate — MR-family: MR02/MR03-tally. Needs C++ build |
| `6cd39073` | #3895 | 2026-03-23 | Fix surface tally when crossing lattice | C++ lattice + tally | out of scope — needs lattice geometry (we have single pin) |
| `0ab46dfa` | #3848 | 2026-03-04 | Fix cell data parsing | `openmc/cell.py` | candidate — pure Python, parsing path |
| `e130701f` | #3817 | 2026-02-24 | MeshFilter.get_pandas_dataframe handle all mesh types | filters | out of MR scope — pandas dataframe only, not on k_eff or flux path |
| `53ce1910` | #3825 | 2026-02-19 | S2 Random Ray Casting Issue | random ray | out of scope — we use eigenvalue + tally, not random ray |
| `19c0aafd` | #3802 | 2026-02-13 | None values in cross section data | `openmc/model/model.py` | candidate — pure Python plumbing, similar pattern to Case 2 |
| `a3426cf8` | #3798 | 2026-02-12 | weight windows regression test | weight windows | out of scope — variance-reduction, not on our path |
| `3b619d69` | #3773 | 2026-02-04 | length multiplier in several LibMesh methods | unstructured mesh | out of scope — needs libmesh |
| `7b4617af` | #3748 | 2026-01-30 | plotting model with multi-group cross sections | plotting | out of MR scope |
| `5c4121ef` | #3676 | 2025-12-12 | hdf5 source_bank struct size | source bank C++ | out of scope — source bank serialization not on k_eff/flux path |
| `f2813925` | #3668 | 2025-12-05 | plotting issue scaling source locations | plotting | out of MR scope |
| `afd9d060` | #3525 | 2025-09-12 | combining TimeFilter, MeshFilter, tracklength estimator | C++ tally | candidate — MR-family: tally; needs TimeFilter setup we don't have |
| `eaed4009` | #3558 | 2025-09-03 | plotting cross sections with S(a,b) data | plotting | out of MR scope |
| `767db7e6` | #3580 | 2025-09-25 | IFP implementation | IFP kinetics | out of scope |

**Top OpenMC walkthrough/live-trigger candidates** (in priority order):

1. `dfc80c70` (#3712) ← **Case 2** ✓ live-triggered today
2. `10f2b753` (#3708) ← **Case 3** (deferred to Phase-3 PR-1 2×2 pin)
3. `ef22558f` (#3662 borated_water temperature) — pure Python, easy
4. `c7d7fa46` (#3692 rotational periodic BC) — strong MR01/MR02 fit but C++
5. `bd76fc05` (#3619 tally no_reduce normalization) — strong MR02/MR03-tally fit but C++
6. `0ab46dfa` (#3848 cell data parsing) — pure Python
7. `19c0aafd` (#3802 None values in cross section data) — pure Python, similar pattern to Case 2

## Scoring rubric (used informally above)

For each candidate fix:

* **path-on-SUT?**  Does our pincell.json runner code path hit the
  buggy file? (k_eff / flux / cross-section / geometry)
* **MR-family prediction**: which existing MR (MR01-MR14 / MR-T /
  MR02-tally / MR03-tally) is hypothesized to catch the bug?
* **Reproducibility cost**:
  - **trivial** = pure-Python, no rebuild (Case 2, #3662, #3802, #3848)
  - **medium** = C++ patch + rebuild, no new SUT setup (Case 1, #3619, #3692)
  - **heavy** = needs new SUT geometry / inputs (Case 3 wants 2×2 pin)
* **Coverage-class novelty**: does this case test an MR family we
  haven't yet validated against any real bug? Phase-2 had only 1
  real-bug fit (Case 1 → MR-NuSigmaF); Phase-3 PR-1 adds the tally
  family (waiting on Case 3 / #3619 to validate it on a real bug);
  MR-T (added this round) has Case 2 as its first real-bug fit.

## Recommended hand-off

Next PR (Phase-3 PR-2 candidate) should pick:

1. **`#3662` (borated_water temperature)** — pure Python, exercises
   MR-T directly. Easy live trigger; closes Family B.1 with TWO real
   bugs (#3712 + #3662).
2. **`#3692` (rotational periodic BC)** — strong MR01/MR02 fit, but
   needs the OpenMC C++ rebuild infrastructure we don't have today.
   Defer until either (a) we build OpenMC from source in the cloud
   image or (b) we set up an OpenMC-specific Docker reproducer.
3. **`e19317c7`** in OpenMOC (rotation for quadratic surfaces) — fits
   MR01 nicely; needs OpenMOC C++ build (the same path that blocked
   Case 1's live attempt today). Bundle with the Case 1 retry.
