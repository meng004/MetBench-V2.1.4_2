# Historical bug supplement — would MetBench's MRs have caught these?

> Qualitative companion to the mutation-detection matrix. Three real fix
> commits from `openmc-dev/openmc` and `mit-crpg/OpenMOC` are inspected;
> for each we walk through whether MetBench's current MR suite would have
> caught the regression on our 2-group pin-cell test case.
>
> This is not a statistical sample. Three commits cannot generalise to
> "MetBench detects N% of real bugs". The purpose is to anchor the
> mutation-matrix numbers in real upstream faults and to surface
> coverage gaps the mutation catalogue did not exercise.

## Methodology

For each bug:

1. Identify the fix commit upstream (sha + title + author + date).
2. Read the diff and explain the bug in one paragraph.
3. Walk through what would happen on our cross-program test case
   (`SUT/openmoc/sample/pincell.json`, 2-group, factor=1.5):
   - Does the bug live on the code path our SUT exercises?
   - If yes, would the (k_source, k_followup) pair shift in a way that
     violates one of the four MR assertions?
4. Verdict: **detected**, **missed**, or **out-of-coverage** (= bug exists
   in a code path our MR does not touch).

This is read-only inspection; we do not check out the pre-fix tree and
build it. That is the next-phase Docker-reproducibility task.

---

## Case 1 — OpenMOC `Keff = fiss/(abs+leak)` accumulation bug

| field | value |
|-------|-------|
| repo | `mit-crpg/OpenMOC` |
| commit | `28008901bb36a68f116b934596a71c9678c14832` |
| title | "Fix bug in Keff=fiss/(abs+leak)" |
| author | Guillaume Giudicelli (MIT) |
| date | 2018-09-12 |
| files | `src/CPUSolver.cpp` (1 line) |

### The bug

```cpp
// Pre-fix (buggy):
_k_eff *= rates[0] / (rates[1] + rates[2]);

// Post-fix:
_k_eff = rates[0] / (rates[1] + rates[2]);
```

`_k_eff *= ...` instead of `_k_eff = ...`. The pre-fix version multiplies
the previous iteration's `_k_eff` by `fission / (absorption + leakage)`
on every iteration, instead of overwriting it. Over many iterations the
accumulated product diverges (or collapses, depending on initial
conditions).

### Would MetBench's MR catch it?

This is on the **k_eff computation path** our SUT exercises directly:
`SUT/openmoc/openmoc_runner.py` calls `solver.computeEigenvalue(max_iters)`
which internally invokes `CPUSolver::computeKeff`. Our case converges in
~500-600 iterations (`pincell.json` baseline iter = 553).

Effect on a single run:

- Source case: `_k_eff` accumulates a wrong product over 553 iterations.
  The reported `k_eff` is `(rates[0] / (rates[1] + rates[2]))^N` for some
  effective `N` depending on the convergence-rate ramp. For our case
  baseline ≈ 1.13 ≈ `(1.13)^1 = 1.13`, but with accumulation it could
  diverge to ∞ or collapse toward 0.

Effect on the MR (`k_followup > k_source` for ScaleNuSigmaF, factor=1.5):

- If both source and follow-up diverge to `inf`: `inf > inf` is **false**
  → MR reports `detected`. The mutation row most analogous to this in
  our catalogue is `Mut02-openmoc-runner-sigt-from-siga` (reported
  `k_src=inf k_flw=inf`, both MRs `DETECT`).
- If source and follow-up both converge to finite-but-wrong values, the
  ratio `k_followup / k_source` may still be roughly the correct 1.5×
  scaling (because the accumulation factor cancels in the ratio). In
  that case the strict `>` assertion still passes → **missed**.

### Verdict

**Detected (probabilistically)**. The MR assertion is qualitative
(direction only). The bug typically causes `_k_eff` to diverge — in our
catalogue Mut02 and Mut04 ("sigt-from-siga", "drop nu_sigma_f") which
produce `inf`/`nan` k_eff both show DETECT in the matrix. The same
mechanism would catch this 2018 bug.

If the buggy code somehow converged to a finite k_eff (depends on the
seed `_k_eff` value), the proportional MR ratio could still pass, and
the MR would miss. Without checking out the 2018 tree we can't measure
which branch fires; both are physically plausible.

---

## Case 2 — OpenMC `XSdata.add_temperature` returns None

| field | value |
|-------|-------|
| repo | `openmc-dev/openmc` |
| PR | #3712 |
| commit | `dfc80c70694c238ee64206106843879042e12d6a` |
| title | "fixing temperatures setting for mgxs" |
| author | Jonathan Shimwell |
| date | 2026-01-07 |
| files | `openmc/mgxs_library.py` |

### The bug

```python
# Pre-fix:
temp_store = self.temperatures.tolist().append(temperature)
# Post-fix:
temp_store = self.temperatures.tolist()
temp_store.append(temperature)
```

`list.append()` returns `None`; the pre-fix one-liner stores `None` into
`temp_store` and then into `self.temperatures`, breaking every
downstream method that expects a list-like `temperatures` array.

### Would MetBench's MR catch it?

**Out of coverage.** `SUT/openmc/openmc_runner.py` creates each
`XSdata(name, library.energy_groups)` once with a single (default)
temperature; we never call `add_temperature()`. The bug lives in
`mgxs_library.py:XSdata.add_temperature`, a code path our SUT does not
exercise.

If a user with a multi-temperature workflow had picked up this OpenMC
build, their k_eff calculation would have failed loudly (downstream
`self.temperatures.append` on `None`). It is a real bug, but not on the
path our MR exercises.

### Verdict

**Out of coverage**. To detect this class of bug, MetBench would need an
MR family that varies temperature (e.g.,
`MaterialTemperatureScaling`). That is a Phase 2 candidate.

---

## Case 3 — OpenMC distribcell group-name collapse

| field | value |
|-------|-------|
| repo | `openmc-dev/openmc` |
| PR | #3708 |
| commit | `10f2b7534c44104324b2baeabc89330fc39bd9fb` |
| title | "Fixing group names in MGXS HDF5 file" |
| author | Jonathan Shimwell |
| date | 2026-01-06 |
| files | `openmc/mgxs/mgxs.py` |

### The bug

```python
# Pre-fix:
if self.domain_type == 'distribcell':
    group_name = ''.zfill(num_digits)   # always "0000…0"
# Post-fix:
    group_name = str(subdomain).zfill(num_digits)  # unique per subdomain
```

For `distribcell` MGXS outputs, every subdomain wrote to the same HDF5
group name (`'0000…0'`), silently overwriting earlier subdomains so
that only the **last** one survived in the output file. Downstream
analyses would see consistent-looking data that was wrong because it
represented only one of N intended cells.

### Would MetBench's MR catch it?

**Out of coverage.** Our SUT does not use `distribcell` tallies; it
reports a single `k_eff` value via `openmc.StatePoint`'s `keff`
attribute. The buggy code path is the per-subdomain tally HDF5 export,
which is independent of the eigenvalue calculation.

### Verdict

**Out of coverage**. Detection would require an MR family that
exercises distribcell tallies (e.g., flux distribution invariants
under symmetry transformations) — also a Phase 2 candidate.

---

## Summary

| Case | Bug | On k_eff path? | MR verdict |
|------|-----|----------------|------------|
| OpenMOC 28008901 | `_k_eff *= ...` accumulation | yes | detected (probabilistically; depends on divergence behaviour) |
| OpenMC #3712 | `add_temperature` returns None | no | out-of-coverage |
| OpenMC #3708 | distribcell group name collapse | no | out-of-coverage |

**Reading**: of three real upstream fix commits we sampled, only the
OpenMOC `_k_eff` accumulation bug lives on the code path our 2-group
pin-cell + ScaleNuSigmaF/ScaleFuelSigmaA MR exercises. That bug would
likely be caught (matrix Mut02 / Mut04 show the same `inf`/`nan` →
detected pattern). The other two are real, severe bugs in OpenMC's
multi-group plumbing, but they affect tally outputs and temperature
handling — not eigenvalue computation under a single-temperature
input. Our MR cannot see them.

This is the **honest scope** of MetBench's current MR coverage. Two
out of three real bugs in the upstream multi-group code path go
unseen by us — not because the MR is weak, but because the MR
observes only k_eff and we picked SUT inputs that exercise a narrow
slice of the solver. Phase 2 should add:

1. **MaterialTemperatureScaling** MR family — would cover Case 2.
2. **Tally-symmetry** MR family (rotation / reflection on distribcell
   outputs) — would cover Case 3.

Both fit MetBench's `MrTransformation(name, parameters)` taxonomy and
would land entirely in `SUT/*` + new input adapters; no changes to the
launcher facade or C# layer are required.
