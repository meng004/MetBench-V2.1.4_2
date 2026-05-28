# P7 — G11 Decision-Record-or-Die Grep + Template Plan

> Date: 2026-05-28
> Status: Active scoped — single PR
> Driver: v2 charter §6 P7 (`docs/superpowers/specs/2026-05-28-code-governance-v2-charter.md`)
> Module: B (mechanical pattern guards) — adds the eleventh grep G11 + a paired template.
> Branch: `claude/p7-g11-decision-record`

---

## §1 Goal & Acceptance

Prevent the Cat B "new module landed without a written §5.x-style decision record" pattern (charter §5 row "Cat B — 未经 spec 直接产新 module") by adding (a) a path-pattern grep that warns when a PR creates a new file inside any of the four "new-module" path families and the PR body carries no `decision-record:` substring, and (b) a standardized template that authors copy when a real new module must land. Acceptance: G11 emits `::warning::` on a synthetic test diff and is silenced when the PR body adds `decision-record: ...`; template registered in `docs/superpowers/templates/` and indexed.

---

## §2 Inventory (measured 2026-05-28)

Path inventory measured against working tree at branch `claude/p7-g11-decision-record` (= `origin/main` head):

| # | Path pattern targeted by G11 | Files matching today | Notes |
|---|---|---|---|
| 1 | `MetBench_BLL.Core/SystemMT/Reporting/.*Renderer\.cs` | 6 files: `HtmlSystemMtResultReportRenderer.cs`, `IExcelSystemMtResultReportRenderer.cs`, `IPdfSystemMtResultReportRenderer.cs`, `ISystemMtResultReportRenderer.cs`, `IWordSystemMtResultReportRenderer.cs`, `Charts/ISystemMtChartRenderer.cs` | The path in the charter task description (`Reporting/SystemMt/.*Renderer\.cs`) is incorrect — Core-side renderers live directly under `Reporting/` and `Reporting/Charts/`. Windows-side renderer impls live under `MetBench_BLL/Reporting/SystemMt/*Renderer.cs` (4 files); G11 deliberately scopes to `MetBench_BLL.Core/` only (Method MT path excluded per task constraint). |
| 2 | `MetBench_BLL.Core/SystemMT/Catalog/Typed/Runtime/.*Kernel\.cs` | 13 files (`BinaryComparisonKernel.cs`, `CrossMethodComparisonKernel.cs`, `DerivedInvariantKernel.cs`, `ErrorMonotonicKernel.cs`, `FieldEqualityKernel.cs`, `FieldProportionalityKernel.cs`, `IVerifierKernel.cs`, `NoiseAwareBinaryComparisonKernel.cs`, `OrderedSequenceShapeKernel.cs`, `ScaledEqualityKernel.cs`, `SequenceShapeKernel.cs`, `SubadditiveKernel.cs`, `VarianceRatioKernel.cs`) | The charter task suggested `Typed/Specs/.*Predicate\.cs` — measured tree shows `Specs/` only carries two base spec types (`PredicateSpec.cs`, `PropertyPredicateSpec.cs`); the actual "new predicate module" unit consistently shipped (e.g. PR-N1 `NoiseAwareBinaryComparisonKernel`, PR-VR `VarianceRatioKernel` per `2026-05-26-typed-noise-aware-scalar-predicate-plan.md` / `2026-05-26-variance-ratio-launcher-pipeline-wiring-plan.md`) is a `*Kernel.cs` file in `Runtime/`. Plan therefore targets the *kernel* family, not `Specs/*Predicate.cs`. |
| 3 | `MetBench_BLL.Core/Discovery/.*Discoverer\.cs` | 4 files: `IMRDiscoverer.cs`, `LlmNativeDiscoverer.cs`, `MetaPatternDiscoverer.cs`, `ScgHeuristicDiscoverer.cs` | Path matches charter intent exactly. |
| 4 | `MetBench_BLL.Core/SystemMT/Anomaly/.*\.cs` (flat, but exclude `IAnomalyService.cs` and existing files via the "new file" filter) | 6 files: `AnomalyClassifier.cs`, `AnomalyFilter.cs`, `AnomalyService.cs`, `AnomalySeverityThresholds.cs`, `CommonalityReport.cs`, `IAnomalyService.cs` | No sub-directory split exists. Use the flat `Anomaly/*.cs` pattern. |

Prior PRs grepped — no `decision-record:` substring convention has been used yet (the §5.x form lives in spec-doc text only). G11 introduces the convention; no retro-active sweep needed.

---

## §3 Design

### 3.1 G11 grep — placement, inputs, outputs

Mirror the G10 implementation style in `.github/workflows/dotnet-test.yml` (lines 180–229). Place G11 immediately after G10's `done < "$registry"` block, before the final `Governance grep result:` echo. Use the same `warn` helper, same `git diff "$PR_BASE_SHA"..HEAD` data source already in scope, same `gh pr view --json body --jq .body` body content already in scope as `$body`.

Pseudocode (final wording goes in the file edit PR, not in this plan):

```bash
# Check 11 (G11) — decision-record-or-die for new-module paths (Cat B prevention):
# A PR that creates a NEW file under any registered "new-module" path family
# must declare a decision record in the PR body via the `decision-record:`
# substring (per docs/superpowers/templates/decision-record-template.md).
# Mirrors the §5.x decision-record discipline established by
# docs/superpowers/specs/2026-05-26-t3-coverage-assessment-and-next-sut-decision.md.
new_module_files=$(git diff --name-only --diff-filter=A "$PR_BASE_SHA"..HEAD \
                    | grep -E '^(MetBench_BLL\.Core/SystemMT/Reporting/.*Renderer\.cs|MetBench_BLL\.Core/SystemMT/Catalog/Typed/Runtime/.*Kernel\.cs|MetBench_BLL\.Core/Discovery/.*Discoverer\.cs|MetBench_BLL\.Core/SystemMT/Anomaly/.*\.cs)$' \
                    || true)
if [ -n "$new_module_files" ]; then
  if ! echo "$body" | grep -qE 'decision-record:'; then
    warn "G11 decision-record-or-die: new module file(s) added without a 'decision-record:' citation in PR body. Files: $(echo "$new_module_files" | tr '\n' ' '). Add 'decision-record: docs/superpowers/specs/YYYY-MM-DD-<topic>-decision.md' (or 'decision-record: N/A (internal-helper / refactor / file-split)') per docs/superpowers/templates/decision-record-template.md (v2 charter §6 P7)."
  fi
fi
```

Key properties (matching existing G6/G8/G9/G10 conventions):
- **Advisory only**: emits `::warning::`; `exit 0` at end of step (unchanged).
- **Diff filter `--diff-filter=A`**: only NEW files trigger; modifications, deletions, renames are silent. This makes the check safe against the existing 6 + 13 + 4 + 6 = 29 already-shipped files.
- **OR-anchored regex** (`^(pat1|pat2|pat3|pat4)$`): exact path match, no partial matches.
- **Single substring body check**: literally `grep -qE 'decision-record:'` — any line in PR body containing the substring silences the warning. Same fail-open semantics as G6/G8.

### 3.2 Path-pattern rationale (one line each)

- Pattern 1 — `Reporting/.*Renderer\.cs`: every new render target (HTML / PDF / Word / Excel / chart family) historically corresponded to a phased plan (T2 chain `2026-05-27-t2-systemmt-visualization-and-t3-gap-fill-plan.md`). A new renderer landing without a recorded scope decision is the exact pattern G11 prevents.
- Pattern 2 — `Typed/Runtime/.*Kernel\.cs`: each kernel is a new typed verification semantic (PR-N1 added `NoiseAwareBinaryComparisonKernel`, PR-VR enriched `VarianceRatioKernel`). Per §5(iii) of `2026-05-26-t3-coverage-assessment-and-next-sut-decision.md` and CLAUDE.md §12.4 R1, any new verification semantic should ship in its own PR with a recorded reason. The `*Kernel.cs` filename is the canonical new-semantic indicator.
- Pattern 3 — `Discovery/.*Discoverer\.cs`: every new `IMRDiscoverer` impl (LLM / meta-pattern / SCG-heuristic) was the centerpiece of a phased plan. Hitting this pattern without a decision record is the exact Cat B regression risk in T4.
- Pattern 4 — `Anomaly/*.cs`: T5 anomaly investigation analytics are still small in number (6 files); each addition is structurally a new analytic. Plan deliberately treats `Anomaly/` as a flat watch zone rather than picking a sub-pattern, since no sub-directory split has emerged yet. If a future PR adds e.g. `Anomaly/Analytics/`, the pattern naturally still matches.

### 3.3 `decision-record-template.md` structure

Located at `docs/superpowers/templates/decision-record-template.md` to live alongside `pr-gate-checklist.md` and `chain-end-review-checklist.md`. ≤ 100 lines. Mirrors the structural cadence of `2026-05-26-t3-coverage-assessment-and-next-sut-decision.md`'s §1–§5.3.

Sections (each ≤ 15 lines guidance + a fill-in stub):
1. **§1 Motivation** — what user-visible or governance gap forces a new module?
2. **§2 Alternatives Considered** — at least two: doing nothing, extending existing module, third option.
3. **§3 Chosen Approach** — the new module name, path, public surface, single sentence reason it dominates the alternatives.
4. **§4 Risks** — at least one concrete failure mode and its mitigation.
5. **§5 Rollback Plan** — single PR revert? Behind a feature flag? Catalog-row-only? Required test bands to re-baseline?
6. **§6 Success Criteria** — observable post-merge checks; cross-link to facts that pin the criteria.

Header preamble line standardised at the top:
```
> Date: YYYY-MM-DD
> Status: Active scoped — decision-of-record for new module <name>
> Cross-link: PR-<id> creating the module
```

Cross-reference appended to template's footer pointing to v2 charter §6 P7 + this plan + CLAUDE.md §12.2 module B row.

---

## §4 Trigger model, false-positive handling, silence escape hatch

G11 fires only when **all** of these hold:
1. PR has at least one file with `git diff --name-only --diff-filter=A` (i.e. a new file landing on this branch vs base SHA).
2. That file's full path matches one of the four `^(...)$` alternatives.
3. PR body contains no `decision-record:` substring.

False-positive scenarios documented in this plan (so reviewers and authors share intuition):
- **Internal helper extracted from an existing renderer/kernel/discoverer/analytic** — e.g. splitting `HtmlSystemMtResultReportRenderer.cs` into a renderer + a private formatting helper. If the helper itself matches `*Renderer.cs` by name, G11 fires; author silences with `decision-record: N/A (file-split)`.
- **Interface added next to an existing impl** — same handling: `decision-record: N/A (interface-extraction)`.
- **Refactor moving an existing renderer's location** — `git diff --diff-filter=R` would catch rename, but a rename that materialises through diff as A+D pair on some git configs could still trigger. Same handling: `decision-record: N/A (refactor / file move)`.

Silence escape hatch policy: the grep deliberately uses substring match, not URL/path match, so `decision-record: N/A (...)` works as a valid silence. CLAUDE.md §12.2 already encodes that authors are accountable for the silence reason; chain-end review (module E) checks for unjustified silences.

---

## §5 Test strategy

This PR ships workflow YAML + a markdown template. No code under `MetBench_BLL.Core/`, no fact change.

- **No new xUnit fact** — `MetBench_SystemMT.Tests` build/run unaffected.
- **Manual smoke** during PR-time:
  1. Self-test positive trigger: this PR itself adds `docs/superpowers/templates/decision-record-template.md` — that path does NOT match any of the four G11 patterns (templates dir, not Core), so this PR alone does not fire G11. To demonstrate G11 fires, the PR description includes a "test the grep" appendix instructing the reviewer to push a throwaway commit adding e.g. `MetBench_BLL.Core/SystemMT/Anomaly/GhostAnalytic.cs` (zero-byte) to a side branch and observe `::warning::` on the resulting PR. Drop the commit before merging this PR. No real new module is shipped here.
  2. Self-test silence: this PR's body itself contains `decision-record: docs/superpowers/plans/2026-05-28-p7-g11-decision-record-plan.md` — proves the silence substring path works against the grep, even though no patterned file is touched.
- **Cloud `test` gate**: must pass unchanged (workflow YAML edit + new markdown file do not affect `dotnet test`).
- **Governance job smoke**: must succeed (G6–G11 all execute on this PR's diff); no warning should be emitted on this PR's own diff because (a) no patterned-path file is added and (b) PR body cites `decision-record:` anyway.

---

## §6 Risks & Stop conditions

- **Risk A — pattern over-fires on `*Renderer.cs` interface declarations.** The 6 existing renderer files include 5 interfaces. A new `ISomeRenderer.cs` would fire G11. Mitigation: G11 is advisory; `decision-record: N/A (interface-extraction)` silences. Acceptable cost.
- **Risk B — patterns under-fire because real new-module additions land outside the four families.** The four families cover the four "spec-grade" module surfaces enumerated in v2 charter §6 P7; future families (e.g. T6 mutation analytics) would require a follow-up PR to extend the regex. Documented as expected evolution, not a defect of this PR.
- **Risk C — the chosen kernel path (`Typed/Runtime/*Kernel.cs`) was substituted for the charter task's suggested `Typed/Specs/*Predicate.cs`.** Evidence: `Specs/` contains only base spec types (2 files), whereas the actual additive new-semantic shipping unit is a `*Kernel.cs` in `Runtime/` (13 files). Trade-off: kernel match is the right contract surface; if a future spec adds a new `Specs/*Predicate.cs` (e.g. for a non-runtime predicate model), the regex will not match. This plan accepts that and documents it.
- **Stop** — if the implementer discovers that `MetBench_BLL.Core/SystemMT/Anomaly/` is being restructured (e.g. a parallel PR introduces an `Anomaly/Analytics/` sub-directory) such that the flat pattern over-matches, pause and reconfirm the pattern. If the reviewer requests `Specs/*Predicate.cs` instead of `Runtime/*Kernel.cs`, pause and reconfirm against the kernel-vs-spec evidence in §2 row 2 / §3.2 pattern 2.

---

## §7 Execution steps (≤ 9)

1. Re-confirm the inventory in §2 (counts shouldn't change between writing-plan and writing-code phases).
2. Read `.github/workflows/dotnet-test.yml` lines 168–229 (G9/G10 block) to confirm the warn-helper variable, the `$body`/`$diff_files` capture context, and the closing `exit 0` location are still as documented in §3.1.
3. Edit `.github/workflows/dotnet-test.yml`: insert G11 block (per §3.1 pseudocode) between G10's `done < "$registry"` and `echo "Governance grep result: $fail warning(s). ..."`.
4. Create `docs/superpowers/templates/decision-record-template.md` with the §3.3 structure.
5. Register this plan + the template in `docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md` §1: add a row pointing to this plan; add a row in §2 (templates) for `decision-record-template.md`.
6. Update CLAUDE.md §12.2 module B row to cross-link the now-existing template path (text already declares G11; only path resolution needs the template file to land).
7. PR body: include `decision-record: docs/superpowers/plans/2026-05-28-p7-g11-decision-record-plan.md` (self-silence) + the §5 manual-smoke appendix.
8. Commit with conventional message `ci(governance): add G11 decision-record-or-die grep + template (v2 charter P7)`. **Do not amend** prior commits.
9. Push; rely on the `governance` job to self-validate the grep ran without firing on this PR.

**Out of scope** (per task constraint): no ledger refresh PR (`docs/status/current.md` untouched in this PR — handled by a later P7-LEDGER PR if the chain warrants it); no chain-end review; no MetBench_Client / Method MT / SemanticCatalogBoundaryTests changes.

---

## §8 Acceptance checklist

- [ ] `.github/workflows/dotnet-test.yml` `governance` job contains a `Check 11 (G11) — decision-record-or-die` block placed after G10 and before the closing `echo` summary line.
- [ ] G11 regex literally matches the four `^(...)$` alternatives in §3.1 (no broader patterns; no path under `MetBench_Client/`; no path under `MetBench_BLL/` Windows-side).
- [ ] G11 uses `--diff-filter=A` so modifications of existing renderers/kernels/discoverers/anomaly files do NOT fire.
- [ ] `docs/superpowers/templates/decision-record-template.md` exists with §1–§6 structure, ≤ 100 lines.
- [ ] `docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md` §1 has a new row for this plan; §2 has a row for the template.
- [ ] CLAUDE.md §12.2 module B row cross-references the new template path.
- [ ] PR body contains `decision-record:` substring (self-silence) and a manual-smoke appendix per §5.
- [ ] `dotnet test` Required gate still green; `governance` job ran end-to-end and emitted no unexpected `::warning::` for this PR's own diff.
