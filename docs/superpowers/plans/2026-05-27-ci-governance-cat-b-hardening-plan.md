# CI Governance — Category B Hardening Plan

> **Date**: 2026-05-27
> **Status**: Active — sequenced execution plan
> **Source review**: [`docs/superpowers/specs/2026-05-27-t2-t3-chain-post-merge-review.md`](../specs/2026-05-27-t2-t3-chain-post-merge-review.md) §3 Category split (5 A + 6 B)
> **Driver**: PR #206 ship grep-governance covered ~80% of the prior AI-review Cat A coverage. The remaining ~20% of Cat A (silent-discard / pinned-count / parity-hint) plus all 6 Cat B findings still rely on process discipline + post-merge review. This plan mechanizes them.

---

## §1 Goal

Move CI Cat B coverage from **~5%** (current — relies on chain-end review + manual ParityTests authoring) to **~50%** (target — mechanical guard tests + Roslyn analyzer + mutation testing). Cat A coverage moves from ~75% (current grep) to **~90%** (after the missing 3 grep checks are added).

**Non-goals**:
- Restoring auto-trigger AI review (kept on `workflow_dispatch`-only per PR #206).
- 100% Cat B coverage — D1 / D2 retrospective drift class is partially structural and requires ongoing chain-end review ritual.

---

## §2 Phases

Six PRs total. Each is independently shippable; later phases supersede earlier when they overlap (Phase 4 supersedes Phase 2's grep list once Roslyn analyzer is stable).

### Phase 1 — PR #207 · 3 missing grep checks + B-1 ledger guard

**Scope**: `dotnet-test.yml` governance step extension.

**Adds 4 checks**:

1. **G6 silent-discard pattern** (Cat A — was B1 / Excel ReportContext)
   ```bash
   if git diff "$PR_BASE_SHA"..HEAD -- '**/*.cs' | grep -qE '^\+\s*_ = .* \?\?'; then
     warn "G6: silent-discard pattern '_ = … ?? …' detected. Each occurrence is a candidate B1-class bug."
   fi
   ```
   Cost: 3 lines bash. Cited as the textbook B1 instance in the T2/T3 review.

2. **G7 pinned-count discipline** (Cat A — was the precedent that PR-N2 / PR-Bol-2B / PR-T3-8 had to teach)
   ```bash
   if git diff "$PR_BASE_SHA"..HEAD -- 'MetBench_BLL.Core/SystemMT/Launcher/LegacyCatalogFactory.cs' \
        | grep -qE '^\+ *yield return new MrBlueprint\('; then
     bumped=$(git diff "$PR_BASE_SHA"..HEAD -- 'MetBench_SystemMT.Tests/**/*.cs' \
                | grep -cE '^[+-]\s+Assert\.Equal\([0-9]+,' || true)
     if [ "${bumped:-0}" -lt 1 ]; then
       err "G7: new MrBlueprint added but no pinned 'Assert.Equal(N, …)' bumped. Per CLAUDE.md §12.5, 6 pinned sites must be updated."
       fail=1
     fi
   fi
   ```
   Cost: 8 lines bash. Hard fail (error, not warn) because pinned-count omissions caused multiple hard-test reds historically.

3. **G8 § 12.4 R1 parity-test hint** (Cat A / Cat B border — was L1 / FromBlueprint MetaPattern)
   ```bash
   added_fields=$(git diff "$PR_BASE_SHA"..HEAD -- 'MetBench_BLL.Core/**/*.cs' \
                    | grep -cE '^\+\s+public (string|int|long|double|bool|TimeSpan|IReadOnlyList|IReadOnlyDictionary|Guid)[^=(]* \{ (get|init)' || true)
   if [ "${added_fields:-0}" -gt 0 ] && ! echo "$diff_files" | grep -qE 'ParityTests\.cs$'; then
     warn "G8: public field(s) added in MetBench_BLL.Core but no *ParityTests.cs touched. If the type has multi-projection paths, parity test required (§12.4 R1)."
   fi
   ```
   Cost: 5 lines bash. Warn (not error) because false-positive rate is non-trivial; Phase 4 Roslyn analyzer will replace this with a precise version.

4. **G9 Stage-8 ledger Controlled-without-review-doc** (Cat B — was D1/D2 retrospective drift class; was discussed as B-1)
   ```bash
   new_stage8=$(git diff "$PR_BASE_SHA"..HEAD -- 'docs/status/current.md' \
                  | grep -cE '^\+\|.*Controlled.*— [0-9]+-phase' || true)
   if [ "${new_stage8:-0}" -gt 0 ]; then
     if ! git diff "$PR_BASE_SHA"..HEAD -- 'docs/status/current.md' \
            | grep -qE '^\+.*docs/superpowers/specs/.*post-merge-review'; then
       err "G9: new 'Controlled — N-phase' Stage-8 row added but no post-merge-review.md cross-link in the row. CLAUDE.md §12.4 R2."
       fail=1
     fi
   fi
   ```
   Cost: 6 lines bash. Hard fail — this is the structural Cat B guard that would have caught the T2/T3 chain marking Controlled before review was written.

**Acceptance criteria**:
- `dotnet-test.yml` `governance` job adds 4 new checks (G6 / G7 / G8 / G9).
- All checks fire on synthetic test diffs (developer creates a throwaway branch + verifies each check triggers / doesn't trigger as designed).
- Full suite green; no test regression.
- PR body shows manual verification of each new check on a synthetic input.

**Estimated effort**: 2h. **Branch**: `claude/phase1-grep-checks-extension`.

---

### Phase 2 — PR #208 · B-2 known-multi-projection list enforcement

**Scope**: introduce an explicit list of types that have ≥ 2 projection paths and need ParityTests coverage.

**Implementation**: new file `.github/governance/multi-projection-types.txt` (line-delimited type names) consumed by the grep step.

```bash
# Phase 2 G10 — multi-projection record discipline
while read -r type_name; do
  [ -z "$type_name" ] || [[ "$type_name" == \#* ]] && continue
  if git diff "$PR_BASE_SHA"..HEAD -- "**/*.cs" \
       | grep -qE "^[+-]\s+public (string|int|long|...).* \{ (get|init).*$type_name"; then
    if ! echo "$diff_files" | grep -qE "${type_name}ParityTests\.cs$"; then
      err "G10: $type_name field changed but ${type_name}ParityTests.cs not touched (§12.4 R1)"
      fail=1
    fi
  fi
done < .github/governance/multi-projection-types.txt
```

**Initial list** (verified by walking current codebase):
- `MrCatalogEntry` — `FromBlueprint` + `ManifestMrCatalogProvider.MapToEntry`
- `SystemMtResultRecord` — HTML / Markdown / PDF / Word / Excel renderers
- `MrSummary` — read by both launcher.ListAvailableAsync and importer
- `ExecutionEvidence` — recorder + renderer projection

**Acceptance criteria**:
- `multi-projection-types.txt` lists ≥ 4 entries.
- For each entry, a corresponding `<TypeName>ParityTests.cs` exists (or is added in same PR).
- New grep check (G10) fires on synthetic field-add diff lacking parity-test change.

**Estimated effort**: 3h. **Branch**: `claude/phase2-multi-projection-enforcement`.

---

### Phase 3 — PR #209 · B-3 spec-doc freshness cron

**Scope**: new workflow `.github/workflows/spec-freshness-monitor.yml`, weekly cron.

**Mechanism**:
1. Cron `0 6 * * 1` (Monday 06:00 UTC).
2. Scans `docs/superpowers/specs/*.md` for measurable claim patterns:
   - `top-1 candidate.*=\s*\\?[\`]([\w-]+)`
   - `next gap-fill.*=\s*\\?[\`]([\w-]+)`
   - `recommended.*MR.*[\`]([\w-]+)`
3. For each match, greps `MetBench_BLL.Core/SystemMT/Launcher/LegacyCatalogFactory.cs` + `SUT/*/catalog.json` for the MR id.
4. If MR id absent and spec mtime > 14 days old → open / update issue tagged `governance:stale-spec` with details.
5. Issue body: spec file + line, claimed MR id, spec age, suggested action (re-touch spec or implement MR).

**Acceptance criteria**:
- Workflow file syntactically valid (`yaml.safe_load`).
- Cron schedule documented in `CLAUDE.md §13 Roadmap pointers`.
- Dry-run mode (PR-time test): manually trigger via `workflow_dispatch`, confirm it can identify the historical stale claim (Phase 4 spec doc's pre-PR-199 `burgers-timestep-convergence` top-1 wording).
- Issue creation respects deduplication: re-running should not create duplicate issues for same `(spec_file, mr_id)` pair.

**Estimated effort**: 3h. **Branch**: `claude/phase3-spec-freshness-cron`.

---

### Phase 4 — PR #210 · B-4 Roslyn analyzer for multi-projection records

**Scope**: new `MetBench_Analyzers/` C# project (`.csproj` type Roslyn analyzer) shipping one analyzer.

**Analyzer rule**: `METBENCH001 — multi-projection record requires ParityTests`.

**Detection logic**:
1. Identify `public sealed record` types in `MetBench_BLL.Core/`.
2. For each, count distinct methods that construct that record via `new <Type>(...)` or `with { ... }` in production code outside the record's own file.
3. If construction sites ≥ 2 → flag as multi-projection.
4. Require existence of `<TypeName>ParityTests.cs` in `MetBench_SystemMT.Tests/`.
5. Require each public field on the record have at least one `Assert.Equal(h.<Field>, m.<Field>)` line inside that ParityTests file.

**Replaces Phase 2**: once this lands and is verified for ≥ 2 weeks, Phase 2's grep list can be removed (or kept as a fallback warning layer).

**Acceptance criteria**:
- `MetBench_Analyzers/MetBench_Analyzers.csproj` builds clean.
- Analyzer reports `METBENCH001` for each known multi-projection type in the current main if its ParityTests is removed (synthetic regression test).
- `.editorconfig` + `Directory.Build.props` route `METBENCH001` to `error` severity.
- CI `dotnet build` fails on a synthetic diff that adds a field without parity assertion.

**Estimated effort**: 1.5 days (~12h). **Branch**: `claude/phase4-multi-projection-analyzer`.

---

### Phase 5 — PR #211 · B-5 mutation testing pilot

**Scope**: introduce mutation testing on the highest-leverage namespace via Stryker.NET.

**Pilot target**: `MetBench_BLL.Core/SystemMT/Catalog/Typed/`.

**Setup**:
1. Add `tools/mutation-testing/stryker-config.json` configured for the typed catalog directory.
2. Add CI workflow `.github/workflows/mutation-testing.yml` triggered on label `mutation-testing` or weekly cron.
3. Initial run target: identify baseline survived mutations on current main.
4. **No hard gate at first** — informational only. After 1 week of baseline data, propose a threshold (likely ≥ 80% kill rate) for a future PR to harden.

**Acceptance criteria**:
- `dotnet stryker` runs successfully against `MetBench_BLL.Core/SystemMT/Catalog/Typed/`.
- Baseline mutation-kill-rate captured and logged in PR body.
- Survived mutations enumerated; ≥ 1 actionable test-gap finding documented.
- Workflow runs in < 30 min wall-clock; if slower, scope down to one sub-folder.

**Estimated effort**: 1 week (8h setup + several days observing pilot results). **Branch**: `claude/phase5-mutation-testing-pilot`.

---

### Phase 6 — PR #212 · PR-LEDGER chain-end review + status refresh

**Scope**: apply CLAUDE.md §12.4 R2 ritual to this 5-phase chain.

**Deliverables**:
- Fresh-session post-merge holistic review of phases 1–5.
- New doc `docs/superpowers/specs/2026-05-NN-ci-cat-b-hardening-chain-post-merge-review.md` following the chain-end-review-checklist.md template.
- `docs/status/current.md` Stage-8 row "CI Cat B hardening — Controlled" with cross-link to the review doc.
- Active plan index row updated.

**Acceptance criteria**:
- Per G9 grep check: Stage-8 row must include post-merge-review cross-link (this is itself the dogfood case).
- All 5 prior phase SHAs cited.

**Estimated effort**: 2h. **Branch**: `claude/phase6-cat-b-chain-ledger`.

---

## §3 Dependencies & sequencing

```
Phase 1 ──────────┐
                  ├── independent; can ship in any order
Phase 2 ──────────┤
                  │
Phase 3 ──────────┘

Phase 4 (Roslyn analyzer) ─── supersedes Phase 2 list-based check
                                     (kept as fallback for ~2 weeks)

Phase 5 (mutation pilot) ─── independent infrastructure track

Phase 6 (PR-LEDGER) ───── requires all 5 prior phases merged
```

No hard dependencies between Phases 1 / 2 / 3 / 5. Phase 4 conceptually supersedes Phase 2 but they coexist for a probation window. Phase 6 is the strict last step.

---

## §4 Total cost

| Phase | Effort | Cumulative |
|---|---|---|
| 1 | 2h | 2h |
| 2 | 3h | 5h |
| 3 | 3h | 8h |
| 4 | 12h | 20h |
| 5 | 8h setup + week observation | 28h |
| 6 | 2h | 30h |

**Total**: ~30h focused work + 1 week observation window for Phase 5. Spread over ~2 weeks calendar.

---

## §5 Acceptance criteria for the whole chain

After Phase 6 merges:

- [ ] Cat A coverage: ~75% (current) → ~90% (G6/G7/G8 added).
- [ ] Cat B coverage: ~5% (current — chain-end-review only) → ~50% (G9 ledger guard + G10 multi-projection enforcement + Phase 4 analyzer + Phase 5 mutation testing pilot).
- [ ] Average runner-minute per PR: unchanged (~1.5 min for `test` + ~3s for governance + Phase 4 analyzer adds ~5s at build).
- [ ] One new auto-issue category (`governance:stale-spec`) catches Phase-N spec retrospective drift weekly.
- [ ] Mutation testing baseline established and ≥ 1 test-coverage gap identified + closed by a follow-up PR.
- [ ] `CLAUDE.md §12` updated to reference Phases 1–5 mechanisms.

---

## §6 Verification strategy

Each phase has its own per-PR verification (acceptance criteria above). The chain-end review (Phase 6) verifies cross-phase coherence:

- [ ] No regression of the 5 grep checks across the chain.
- [ ] Phase 4 Roslyn analyzer reports same set of multi-projection types as Phase 2 grep list (sanity).
- [ ] Phase 5 mutation testing pilot results were used to write at least 1 follow-up test PR (worked-example evidence).
- [ ] G9 Stage-8 row cross-link works on this very chain's Phase 6 PR (dogfood case).
- [ ] All cleanup PRs (if any from the chain-end review) merged before Phase 6 declares Controlled.

---

## §7 Open questions parked for execution time

- **Q1 (Phase 1)**: should G7 pinned-count check be `err` (hard fail) or `warn`? Plan says `err` because pinned-count omissions caused hard-test red multiple times; reviewer at exec time can downgrade if false-positive rate emerges.
- **Q2 (Phase 2)**: should `multi-projection-types.txt` start with 4 entries (verified) or 8+ entries (predicted)? Plan says 4 verified — expand reactively as new multi-projection types appear.
- **Q3 (Phase 4)**: should the Roslyn analyzer ship at `warn` severity for 2 weeks then `error`? Plan says ship at `error` immediately — current main already passes (verified by `dotnet build` clean), so no false-positive risk.
- **Q4 (Phase 5)**: target Stryker.NET threshold = 80% / 85% / 90%? Plan says decide after baseline is observed.
- **Q5 (Phase 5)**: scope to just `Catalog/Typed/` or wider? Plan says `Catalog/Typed/` first because it's the highest-leverage namespace (Cat B M5 came from there).

---

## §8 References

- `docs/superpowers/specs/2026-05-27-t2-t3-chain-post-merge-review.md` — source of the 11 findings + Cat A/B split + 10 rules-of-thumb
- `CLAUDE.md §12.4 / §12.5` — third + fourth layer rules this plan operationalizes
- `docs/superpowers/templates/chain-end-review-checklist.md` — Phase 6 procedure template
- `.github/workflows/dotnet-test.yml` — current grep governance, target of Phases 1 / 2 extensions
