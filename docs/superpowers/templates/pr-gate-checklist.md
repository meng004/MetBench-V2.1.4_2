# PR Gate Checklist

> Use this checklist in every MetBench PR description or Layer-2 review note.
> The checklist keeps project state, tests, and Windows evidence from drifting apart.

## Scope

- [ ] This PR has one primary purpose.
- [ ] This PR does not mix feature implementation, governance changes, and unrelated cleanup.
- [ ] The affected requirement or plan is named.
- [ ] The PR states whether it changes the current status ledger.

## Facts

- [ ] Current `origin/main` head was checked before starting.
- [ ] If this PR changes current status, `docs/status/current.md` is updated.
- [ ] If this PR changes requirements, structure, or roadmap projections, the relevant projection document is updated.
- [ ] Historical plans were not used as the current execution source unless listed in the active plan index.

## Tests

- [ ] Focused tests were run, or the PR explains why none apply.
- [ ] Full cloud test baseline was run when code behavior changed, or the PR explains why not.
- [ ] Documentation-only PRs do not claim a new code-test baseline.

## Windows Classification

Choose exactly one highest required level. If multiple categories apply, choose the strongest evidence requirement in this order:

`UI-visible validation` > `run-and-log` > `build` > `no Windows evidence`.

- [ ] `No Windows evidence required`: no WPF, `App.xaml.cs`, Windows path, UI, or config-binding surface changed.
- [ ] `Windows build required`: DI, config binding, `App.xaml.cs`, or Windows project wiring changed.
- [ ] `Windows run-and-log required`: startup, file path, report generation, or runtime integration changed.
- [ ] `Windows UI-visible validation required`: WPF page, view model, navigation, or interaction changed.

## Review

- [ ] Layer 1 local review completed.
- [ ] Layer 2 PR review note completed.
- [ ] Review explicitly checked for status drift and stale baseline claims.

## Merge

- [ ] Required checks are green.
- [ ] Merge method is appropriate for the branch policy.
- [ ] After merge, local `main` should be synchronized before monitoring reads the workspace.

## AI Review (advisory, automated)

Every PR opened against `main` automatically triggers `pr-soft-review.yml` (per
[`docs/superpowers/specs/2026-05-26-pr-soft-review-via-claude-code-action.md`](../specs/2026-05-26-pr-soft-review-via-claude-code-action.md))
which now runs two advisory reviewers:

- `openai/codex-action@v1` as **Codex Governance Review** for scope, status,
  requirements/plan traceability, Windows classification, and Method MT /
  System MT boundary drift.
- `anthropics/claude-code-action@v1` as **Claude Semantic Review** for C# logic,
  exception paths, runtime boundaries, test adequacy, and WPF semantic risk.

- [ ] Codex Governance Review comment present on the PR (workflow ran, did not silently skip).
- [ ] Claude Semantic Review comment present on the PR (workflow ran, did not silently skip).
- [ ] Each FAIL / P0 / P1 in either AI review comment is either resolved or has a one-line
      human reply explaining why it does not apply.
- [ ] AI review jobs are **never** added to GitHub branch protection's required
      checks list — their job is to surface findings, not to block merge.

If the workflow itself errors (OpenAI / Anthropic API unavailable, quota
exhausted, secret missing) the PR can still be merged; record the absence and
rely on manual Layer 1 + Layer 2 review.
