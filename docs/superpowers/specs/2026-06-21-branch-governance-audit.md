# Branch Governance Audit - 2026-06-21

## Scope

This audit records the branch governance pass started on 2026-06-21 after the
System MT API / MCP control-plane branch was pushed.

Truth sources used:

- `docs/status/current.md`
- live `origin/main`
- `docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md`
- `CLAUDE.md`
- live Git refs after `git fetch --all --prune`

Live `origin/main` at audit time:

```text
a3e2fa4c6743e229c2bc0fa0a44d5b7017f490d2
```

## Initial Inventory

Before cleanup:

- Local branches: 11
- Real remote branches, excluding `origin/HEAD`: 29
- Unique branch names across local + remote, excluding `origin/HEAD`: 35

Branches proven merged into `origin/main` by `git branch -r --merged origin/main`
were treated as cleanup-safe remote refs.

## Cleanup Completed

Deleted remote branches already merged into `origin/main`:

- `claude/maturity-assessment-and-plan`
- `claude/p0-quick-wins`
- `claude/p1-mr-runtime-coverage`
- `claude/p2-bll-explicit-errors`
- `claude/p3-bll-ratchet`
- `claude/p3-dal-ratchet`
- `claude/p3-domain-idal-ratchet`
- `claude/p4-source-guard-tests`
- `claude/p4-wpf-deadlock`
- `claude/p4-wpf-deadlock-vm-prompt`
- `claude/p5-t6-mutation-applicator`
- `claude/wpf-mvvm-convergence-plan`
- `work`

After cleanup and prune:

- Local branches: 11
- Real remote branches, excluding `origin/HEAD`: 16
- Unique branch names across local + remote, excluding `origin/HEAD`: 22
- Remote branches still merged into `origin/main`: none except `origin/main`

## Remaining Remote Branch Classification

### Clean Merge-Tree Candidates

These branches had no merge-tree conflict against `origin/main` in the dry run.
This means they are candidates for PR review, not automatic merge approval.

- `origin/codex/external-mr-batch-e-runtime`
- `origin/codex/systemmt-api-mcp-control-plane`
- `origin/codex/wpf-warning-ratchet`
- `origin/docs/claude-md-init-refresh`

Required before merge:

- refresh against current `origin/main`
- run the branch-specific test gate
- inspect status-ledger / active-plan-index changes for stale projections
- use PR gate and CI as final authority

### Conflict / Rebase Required

These branches conflict with `origin/main` and should not be merged directly:

- `origin/claude/upbeat-fermi-CrlXj`
- `origin/codex/external-mr-asset-acceptance-plan`
- `origin/codex/quality-follow-up-remediation`
- `origin/codex/quality-tdd-remediation`
- `origin/codex/wpf-minimal-mvvm-behaviors`
- `origin/codex/wpf-pr2-native-shell`
- `origin/codex/wpf-pr3-display-dependencies`
- `origin/docker-runtime-mcp-codex`
- `origin/docs/mcp-acceptance-post-merge-status`
- `origin/plan-minimum-mr-subset-p3-p8-dependency-tests`
- `origin/plan-systemmt-runtime-governance-v1`

Recommended handling:

- WPF branches should be consolidated into one Windows/VM validation line before
  code merge.
- Docker Runtime / MCP branches should be compared with
  `codex/systemmt-api-mcp-control-plane`; keep one surviving control-plane path.
- Plan/status-only branches should be rebased and checked for stale status
  claims before any PR.
- Quality remediation branches should be split into focused PRs if they still
  apply after the current control-plane changes.

## Local Branch Classification

Merged local branch with dirty worktree:

- `codex/systemmt-api-adapter` points at `origin/main`, but deletion was blocked
  because it is checked out at `.worktrees/systemmt-api-adapter`. That worktree
  has uncommitted API / control-plane files, so it must not be deleted until the
  dirty worktree is reviewed or intentionally superseded.

Local branches with pruned or missing remote tracking refs need owner review
before deletion because they may contain unpublished or worktree-owned changes:

- `codex/mutation-page-xamlparse-fix`
- `codex/systemmt-api-control-plane`
- `codex/systemmt-explainability-pair-quality-plan`
- `docker-runtime-cli-ui-codex`
- `docker-runtime-plan-docs-codex`

Local branches with live remote counterparts:

- `codex/systemmt-api-mcp-control-plane`
- `docker-runtime-mcp-codex`
- `plan-minimum-mr-subset-p3-p8-dependency-tests`
- `plan-systemmt-runtime-governance-v1`

## Governance Policy

Branch classes:

- `Active`: has current owner, plan, PR, or explicit task.
- `Candidate`: merge-tree clean and ready for PR gate.
- `Conflict`: requires rebase, split, or supersession decision.
- `Archive`: historically useful but should not remain as an active branch.
- `Delete`: merged into `origin/main` or explicitly superseded.

Rules:

1. Fetch/prune before any branch inventory or merge decision.
2. Delete remote branches merged into `origin/main` during weekly governance.
3. Do not delete unmerged remote branches without one of:
   - merged PR evidence,
   - supersession evidence,
   - explicit owner approval.
4. A branch older than 14 days without PR activity, plan reference, or owner
   update must be moved to `Archive` or revalidated.
5. Docs/status branches must be checked for stale projection against
   `docs/status/current.md` and live `origin/main` before merge.
6. WPF branches require Windows/VM evidence classification before being marked
   merge-ready.
7. API / MCP / runtime branches must preserve the launcher facade, typed
   catalog, evidence, persistence, and fail-closed boundaries.

## Next Actions

1. Review dirty worktree `.worktrees/systemmt-api-adapter`; either recover any
   unique files into the surviving control-plane branch or remove the worktree
   after explicit supersession confirmation.
2. Open or refresh PR for `codex/systemmt-api-mcp-control-plane`.
3. Triage clean candidates in this order:
   - `codex/wpf-warning-ratchet`
   - `docs/claude-md-init-refresh`
   - `codex/external-mr-batch-e-runtime`
4. Consolidate conflict clusters:
   - WPF cluster
   - Docker Runtime / MCP cluster
   - quality remediation cluster
   - plan/status stale-projection cluster
5. Repeat inventory after each merge or deletion.
