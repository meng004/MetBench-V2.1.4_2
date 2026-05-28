# Decision Record Template

> Date: YYYY-MM-DD
> Status: Active scoped — decision-of-record for new module `<name>`
> Cross-link: PR-`<id>` creating the module

> Copy this template to `docs/superpowers/specs/YYYY-MM-DD-<topic>-decision.md`
> whenever a PR adds a NEW file under any of the four G11 watched path families
> (per `.github/workflows/dotnet-test.yml` Check 11):
>
> - `MetBench_BLL.Core/SystemMT/Reporting/.*Renderer\.cs`
> - `MetBench_BLL.Core/SystemMT/Catalog/Typed/Runtime/.*Kernel\.cs`
> - `MetBench_BLL.Core/Discovery/.*Discoverer\.cs`
> - `MetBench_BLL.Core/SystemMT/Anomaly/.*\.cs`
>
> Then cite the resulting decision record in the PR body with the literal
> substring `decision-record: <path-or-N/A-reason>` to silence G11.

---

## §1 Motivation

What user-visible behaviour, governance gap, or roadmap obligation forces a new
module to land? Name the active plan / spec / status-ledger row driving the work.

_Fill in: 3–6 lines._

## §2 Alternatives Considered

List at least two alternatives explicitly rejected:

1. **Do nothing / defer.** Why is the status quo insufficient?
2. **Extend an existing module.** Which one? Why does it not absorb the new responsibility cleanly (boundary, naming, public surface, coupling)?
3. **Optional: a third option** (parameterised hook, configuration-only, etc.).

_Fill in: one paragraph per alternative._

## §3 Chosen Approach

- **New module name**: `<TypeName>`
- **Path**: `<full-path>.cs`
- **Public surface**: one-line summary of the public types / methods introduced.
- **Single-sentence reason** it dominates §2 alternatives.

## §4 Risks

At least one concrete failure mode the new module could introduce, plus its
mitigation. Examples: schema break, perf regression, projection-parity drift,
test-only coverage, undocumented invariant.

- **Risk:** …
- **Mitigation:** …

## §5 Rollback Plan

How is this module retired if the chosen approach proves wrong?

- Single PR revert?
- Behind a feature flag / catalog-row toggle?
- Test bands that must be re-baselined?
- Downstream consumers that must be migrated first?

## §6 Success Criteria

Observable post-merge checks that pin the decision. Examples:

- A specific fact assertion in `MetBench_SystemMT.Tests/`.
- A specific parity test (CLAUDE.md §12.4 R1).
- A specific status-ledger row movement (Open → Controlled).
- A specific MR catalog row count or evidence-projection check.

Each criterion should be greppable / runnable, not narrative.

---

> Cross-references:
> - v2 charter §6 P7 — [`docs/superpowers/specs/2026-05-28-code-governance-v2-charter.md`](../specs/2026-05-28-code-governance-v2-charter.md)
> - P7 implementation plan — [`docs/superpowers/plans/2026-05-28-p7-g11-decision-record-plan.md`](../plans/2026-05-28-p7-g11-decision-record-plan.md)
> - CLAUDE.md §12.2 module B row (G11 entry)
