# Mutation Testing Pilot

> Phase 5 of [`docs/superpowers/plans/2026-05-27-ci-governance-cat-b-hardening-plan.md`](../../docs/superpowers/plans/2026-05-27-ci-governance-cat-b-hardening-plan.md).
> Catches Cat B negative-space test gaps — assertions that exist but don't actually pin behaviour.

## Why

The T2/T3 post-merge review's Cat B finding T2 was a classic example: `IExcelSystemMtResultReportRenderer` XML doc claimed `ReportContext` support, the implementation silently discarded `context` via `_ = context ?? new ReportContext();`, and **no fact asserted Title appeared in the output**. The "missing test" was structurally invisible to AI review.

Mutation testing kills this class. Stryker.NET mutates production code (e.g. replaces `_ = context ?? new ReportContext();` with `_ = null;`), runs the test suite, and flags **survived** mutations — code changes that no test caught. Surviving mutations are concrete test-coverage gaps.

## Scope (pilot)

This pilot targets only `MetBench_BLL.Core/SystemMT/Catalog/Typed/`, the highest-leverage namespace (Cat B M5 originated there). Once the pilot's baseline is observed and stable, a follow-up PR can widen scope and propose a hard threshold.

Configured in [`stryker-config.json`](stryker-config.json):

- **Mutate**: `MetBench_BLL.Core/SystemMT/Catalog/Typed/**/*.cs`
- **Test project**: `MetBench_SystemMT.Tests/`
- **Reporters**: HTML + JSON + cleartext + progress
- **Thresholds**: `high: 85, low: 70, break: 0` — *informational only*; pipeline does NOT fail on low kill rate at MVP.
- **Concurrency**: 4 parallel runners.

## Running locally

```bash
dotnet tool install --global dotnet-stryker      # one-time
dotnet stryker --config-file tools/mutation-testing/stryker-config.json
```

Output:
- `StrykerOutput/<timestamp>/reports/mutation-report.html` — interactive HTML report.
- `StrykerOutput/<timestamp>/reports/mutation-report.json` — machine-readable.

Expected wall-clock: 15–30 min for the typed-catalog namespace on a modern laptop. CI weekly cron budget is 45 min.

## Running in CI

[`.github/workflows/mutation-testing.yml`](../../.github/workflows/mutation-testing.yml) provides three triggers:

1. **Weekly cron** — Monday 08:00 UTC. Establishes baseline drift over time.
2. **Manual dispatch** — `Actions → mutation-testing → Run workflow`.
3. **PR label `mutation-testing`** — opt-in per-PR run for risky reporting / catalog changes.

The workflow uploads `StrykerOutput/**` as a build artifact named `stryker-report`. Download from the workflow run summary.

## Interpreting output

Stryker classifies each mutation:

| Status | Meaning | Action |
|---|---|---|
| **Killed** | A test failed when the mutation was applied. ✅ Coverage works. | None. |
| **Survived** | No test failed. ❌ Coverage gap. | Add an assertion that distinguishes original from mutant. |
| **No coverage** | No test exercises this line. | Add ANY test that touches it. |
| **Timeout** | Mutation caused infinite loop; treat as killed. | None. |
| **Compile error** | Mutation didn't compile; ignored. | None (Stryker bug or syntactic mutator on incompatible site). |

**Survived + No coverage are the actionable categories.** A typical pilot result has 10-20 survived mutations on first run, each pointing at a real "fact would have been valuable" gap.

## Promotion path

After 2–3 weekly cron runs land:

1. Establish baseline kill rate range observed (e.g. 78 ± 3%).
2. Pick a hard threshold N points below the lower bound (e.g. baseline 75% → threshold 65%).
3. Open a follow-up PR that sets `"break": N` and adds the workflow to `dotnet-test.yml` as a non-blocking secondary check or as a required check.
4. Each subsequent PR that drops kill rate below threshold fails CI loudly, forcing the author to either add a fact or document the regression.

## Phase 5 acceptance status

- [x] Config file shipped (`tools/mutation-testing/stryker-config.json`).
- [x] CI workflow shipped (`.github/workflows/mutation-testing.yml`).
- [x] Local-run documentation (this README).
- [ ] First weekly cron run completed — pending Monday 2026-06-01 08:00 UTC.
- [ ] Baseline kill rate documented — pending first run.
- [ ] First actionable test-gap finding closed by follow-up PR — pending baseline.

The last three items deliberately spill past this PR's merge; they require real-time observation per plan §Phase 5.
