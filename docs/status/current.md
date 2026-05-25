# MetBench Current Status Ledger

> **Status date**: 2026-05-25
> **Ledger role**: the single current-status source for monitoring, planning, and handoff.
> **Rule**: other documents may project this status for their own purpose, but they do not redefine it.

---

## 1. Current Main

| Field | Value |
|---|---|
| Repository | `meng004/MetBench-V2.1.4_2` |
| Main branch | `origin/main` |
| Current main head | Resolve live from git; do not copy a static commit from this file |
| Last audited governance baseline | `6218106`, PR #112, `docs(governance): add control rules and next-stage audit` |
| Root worktree status at ledger introduction | `main...origin/main`, clean |

## 2. Latest Auditable Code Baseline

| Field | Value |
|---|---|
| Latest code-test baseline commit | `e839214` |
| Baseline source | PR #110, `fix(v12-pr10): harden review follow-up coverage semantics` |
| Test command | `dotnet test MetBench_SystemMT.Tests --no-restore` |
| Result | `1043 pass / 0 fail / 0 skip` |
| Reason current head differs | PR #111 and PR #112 are documentation/governance-only changes and do not refresh the code-test baseline |

## 3. Stage 8 Completion Snapshot

| Area | Status | Evidence |
|---|---|---|
| Catalog convergence / metadata / evidence | Complete for current mainline, with evidence granularity still eligible for expansion | PR #91-#95 |
| MR verification v1.2 | Complete for current roadmap | PR #97-#110 |
| v1.2 inventory denominator | `44 MR + 4 Property` | PR #109 / PR #110 migration and coverage gates |
| Documentation truth-source alignment | Complete through PR #111 | PR #111 |
| Governance baseline | Complete through PR #112 | PR #112 |

## 4. Active Control Documents

| Document | Role |
|---|---|
| `docs/superpowers/specs/2026-05-25-metbench-project-control-rules.md` | Control charter: source hierarchy, gates, review, status refresh |
| `docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md` | Active plan registry |
| `docs/superpowers/plans/2026-05-25-metbench-governed-next-stage-plan.md` | Current governed next-stage master plan |
| `docs/superpowers/plans/2026-05-25-verification-semantics-convergence-implementation-plan.md` | Scoped implementation plan for verification semantics convergence PR-A through PR-D |
| `docs/superpowers/specs/2026-05-25-metbench-macro-assessment-and-risk-audit.md` | Macro map and risk audit |
| `docs/superpowers/specs/2026-05-25-verification-semantics-convergence-design.md` | Verification semantics convergence design: System MT typed semantics, Method MT isolation, and legacy assertion migration/removal |
| `docs/superpowers/templates/pr-gate-checklist.md` | Required PR checklist template |

## 5. Projection Documents

The following documents are projections of the ledger and their own domain. They must not introduce a competing current-status truth:

| Document | Projection responsibility |
|---|---|
| `docs/requirements.md` | Requirement -> implementation -> test traceability |
| `AGENTS.md` | Roadmap and staged delivery projection |
| `CLAUDE.md` | Agent collaboration and engineering constraints |
| `docs/PROJECT-STRUCTURE.md` | Repository structure and test matrix projection |

## 6. Current Open Risks

| Risk | Status | Required next step |
|---|---|---|
| Verification semantics convergence | Design locked and planned | Use `docs/superpowers/specs/2026-05-25-verification-semantics-convergence-design.md` and execute `docs/superpowers/plans/2026-05-25-verification-semantics-convergence-implementation-plan.md`; PR-C remains gated by the ExecutionEvidence v2 decision state unless it proves evidence schema is unchanged |
| ExecutionEvidence final shape | Open | Write Evidence v2 design after semantic-convergence decisions are stable |
| Windows verification policy | Partially controlled | Use PR gate classification now; write the dedicated Windows policy before the next Windows-touching PR |
| Active vs historical plan drift | Controlled but must be maintained | Keep the active plan index current whenever a phase changes |

## 7. Current Execution Order

The next stage must proceed in this order:

1. Maintain this ledger as the current status source.
2. Use the active plan index to select work.
3. Execute verification semantics convergence PR-A and PR-B from `docs/superpowers/plans/2026-05-25-verification-semantics-convergence-implementation-plan.md`; these are docs/naming work and must preserve runtime behavior.
4. Design Windows verification policy before the next Windows-touching PR.
5. Design ExecutionEvidence v2.
6. Execute verification semantics convergence PR-C and PR-D only after the ledger allows assertion-runtime implementation under the current ExecutionEvidence v2 decision state.
7. Only then proceed to implementation PRs for evidence, configuration, or UI-facing work.

## 8. Update Triggers

Update this ledger when any of the following changes:

- latest auditable code-test baseline
- active plan
- inventory denominator
- Windows verification status
- open risk state
- Stage completion judgment

Do not update this ledger just to copy the latest merge commit. Monitoring should resolve the live `origin/main` head directly from git and then use this ledger for status interpretation.
