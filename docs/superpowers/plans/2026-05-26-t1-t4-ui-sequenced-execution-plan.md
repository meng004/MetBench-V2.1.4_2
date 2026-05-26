# T1 T4 UI Sequenced Execution Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Execute the approved control sequence: push and merge the docs-only gate PR, then execute T1 manifest-driven runtime environments, then execute T4-to-T0 binder, while keeping UI MR CRUD isolated as a Windows/VM track.

**Architecture:** This is an orchestration plan, not a feature implementation plan. It controls PR order, preconditions, stop conditions, review layers, and environment boundaries so cloud-side scalability work and Windows-only UI work do not contaminate each other.

**Tech Stack:** GitHub PR workflow, .NET 8, xUnit, MetBench System MT docs/control ledger, `MetBench_BLL.Core`, `MetBench_SystemMT.Tests`, Windows SSH/RDP for future WPF work.

---

## Scope And Non-Goals

This plan governs four tracks:

1. PR-0 docs-only control gate: push branch `codex/t1-cloud-plans-and-ui-gate`, open PR, pass review/checks, merge.
2. PR-1 T1 cloud-side multi-env implementation: execute `docs/superpowers/plans/2026-05-26-t1-manifest-driven-runtime-environments-plan.md`.
3. PR-2 T4-to-T0 binder implementation: execute `docs/superpowers/plans/2026-05-26-t4-to-t0-mr-discovery-binder-plan.md` only after PR-1 merges or is explicitly waived in the ledger.
4. PR-UI Windows/VM MR CRUD: keep separate, plan only after cloud-side tracks are not blocked by it.

This plan must not implement product code directly. It must not merge UI MR CRUD into PR-0, PR-1, or PR-2. It must not treat CLI CRUD, JSON editing, or catalog tests as a substitute for WPF MR CRUD.

## Global Execution Order

The execution order is mandatory:

1. PR-0 docs-only gate.
2. PR-1 T1 manifest-driven runtime environments.
3. PR-2 T4-to-T0 binder.
4. PR-UI Windows/VM MR CRUD.

No later PR may start coding until the earlier PR is merged, except PR-2 may start only if `docs/status/current.md` explicitly waives the T1 dependency for binder work.

## Global Preconditions

- [ ] Repository is `meng004/MetBench-V2.1.4_2`.
- [ ] Local branch for PR-0 is `codex/t1-cloud-plans-and-ui-gate`.
- [ ] PR-0 branch contains commit `ad72754 docs(plan): gate T1 multi-env and T4 binder work` or a newer commit that includes the same approved control docs.
- [ ] `origin/main` resolves successfully from git before each PR starts.
- [ ] `docs/status/current.md` remains the current status ledger.
- [ ] `docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md` remains the active plan registry.
- [ ] All shell commands in this repository use the `rtk` prefix.

## Files Controlled By This Plan

- Read and verify: `docs/status/current.md`
- Read and verify: `docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md`
- Read and verify: `docs/superpowers/plans/2026-05-26-t1-manifest-driven-runtime-environments-plan.md`
- Read and verify: `docs/superpowers/plans/2026-05-26-t4-to-t0-mr-discovery-binder-plan.md`
- Create later: a dedicated Windows/VM UI MR CRUD plan under `docs/superpowers/plans/` before touching `MetBench_Client`

## Task 1: PR-0 Docs-Only Control Gate

**Files:**
- Modify already committed: `docs/status/current.md`
- Modify already committed: `docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md`
- Modify already committed: `docs/requirements.md`
- Modify already committed: `docs/PROJECT-STRUCTURE.md`
- Create already committed: `docs/superpowers/plans/2026-05-26-t1-manifest-driven-runtime-environments-plan.md`
- Create already committed: `docs/superpowers/plans/2026-05-26-t4-to-t0-mr-discovery-binder-plan.md`
- Create: this orchestration plan `docs/superpowers/plans/2026-05-26-t1-t4-ui-sequenced-execution-plan.md`

- [ ] **Step 1: Verify PR-0 branch state**

Run:

```bash
rtk git status --short --branch
```

Expected:

```text
## codex/t1-cloud-plans-and-ui-gate...origin/main [ahead 1]
```

After this orchestration plan is committed, expected state becomes:

```text
## codex/t1-cloud-plans-and-ui-gate...origin/main [ahead 2]
```

- [ ] **Step 2: Verify docs-only diff**

Run:

```bash
rtk git diff --name-only origin/main...HEAD
```

Expected file set includes only docs/control files:

```text
docs/PROJECT-STRUCTURE.md
docs/requirements.md
docs/status/current.md
docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md
docs/superpowers/plans/2026-05-26-t1-manifest-driven-runtime-environments-plan.md
docs/superpowers/plans/2026-05-26-t4-to-t0-mr-discovery-binder-plan.md
docs/superpowers/plans/2026-05-26-t1-t4-ui-sequenced-execution-plan.md
```

If any production `.cs`, `.xaml`, `SUT/`, or test source file appears, stop and remove it from PR-0.

- [ ] **Step 3: Run static doc sanity checks**

Run:

```bash
rtk git diff --check origin/main...HEAD
```

Expected: no output and exit code 0.

Run:

```bash
rtk perl -ne 'BEGIN{@p=("TB"."D","TO"."DO","implement "."later","fill in "."details","Similar "."to Task","appropriate "."error handling","Write tests "."for the above")} for $p (@p) { print "$ARGV:$.:$p\n" if index($_,$p) >= 0 }' docs/superpowers/plans/2026-05-26-t1-manifest-driven-runtime-environments-plan.md docs/superpowers/plans/2026-05-26-t4-to-t0-mr-discovery-binder-plan.md docs/superpowers/plans/2026-05-26-t1-t4-ui-sequenced-execution-plan.md
```

Expected: no output.

- [ ] **Step 4: Push PR-0 branch**

Run:

```bash
rtk git push -u origin codex/t1-cloud-plans-and-ui-gate
```

Expected: branch is pushed to GitHub.

- [ ] **Step 5: Open PR-0**

Run:

```bash
rtk gh pr create --base main --head codex/t1-cloud-plans-and-ui-gate --title "docs(plan): gate T1 multi-env and T4 binder work" --body "## Summary
- Corrects the T1 status: runner/adapter/catalog additivity is demonstrated, but T1 multi-env management and UI MR CRUD remain open.
- Registers the cloud-side T1 manifest-driven runtime environment plan.
- Registers the cloud-side T4-to-T0 binder plan behind the T1 multi-env gate.
- Keeps UI MR CRUD as a separate Windows/VM-scoped track.

## Tests
- rtk git diff --check
- placeholder scan over the new writing-plans documents

## Scope
- Docs/control only.
- No production code.
- No tests changed.
- No WPF changes.
- No Method MT changes."
```

Expected: GitHub returns a PR URL.

- [ ] **Step 6: Monitor PR-0 checks**

Run:

```bash
rtk gh pr checks --watch
```

Expected: required checks are green or no code checks are required for docs-only PR. If any check fails, inspect with:

```bash
rtk gh pr view --json statusCheckRollup,reviewDecision,mergeStateStatus
```

- [ ] **Step 7: Two-layer PR-0 review**

Layer 1 self-review:

- PR-0 is docs-only.
- T1 is no longer marked complete.
- T1 multi-env precedes T4 binder.
- UI MR CRUD is gated to Windows/VM.
- Active plan index and status ledger agree.

Layer 2 maintainer review:

- Could monitoring still misreport T1 as complete? Expected answer: no.
- Could a cloud agent accidentally start UI work from this PR? Expected answer: no.
- Could T4 binder start before T1 without an explicit waiver? Expected answer: no.

- [ ] **Step 8: Merge PR-0**

Run:

```bash
rtk gh pr merge --squash --delete-branch
```

Expected: PR-0 is merged into `main`.

- [ ] **Step 9: Sync local main**

Run:

```bash
rtk git switch main
rtk git pull --ff-only origin main
```

Expected: local `main` is at the PR-0 merge commit.

## Task 2: PR-1 T1 Manifest-Driven Runtime Environments

**Files:**
- Execute plan: `docs/superpowers/plans/2026-05-26-t1-manifest-driven-runtime-environments-plan.md`
- Modify expected: `MetBench_BLL.Core/SystemMT/Launcher/LauncherOptions.cs`
- Create expected: `MetBench_BLL.Core/SystemMT/Launcher/RuntimeEnvironmentResolutionException.cs`
- Modify expected: `MetBench_BLL.Core/SystemMT/Catalog/ManifestMrCatalogProvider.cs`
- Modify expected: `MetBench_BLL.Core/SystemMT/Catalog/PythonExecutableKinds.cs`
- Test expected: `MetBench_SystemMT.Tests/SystemMT/Launcher/RuntimeEnvironmentResolverTests.cs`
- Test expected: `MetBench_SystemMT.Tests/SystemMT/Catalog/ManifestMrCatalogProviderTests.cs`
- Docs expected: `docs/status/current.md`
- Docs expected: `docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md`

- [ ] **Step 1: Verify PR-1 preconditions**

Run:

```bash
rtk git switch main
rtk git pull --ff-only origin main
rtk rg -n "T1 multi-env management|2026-05-26-t1-manifest-driven-runtime-environments-plan|T4-to-T0 binder|UI MR CRUD" docs/status/current.md docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md
```

Expected:

- T1 multi-env is open.
- T1 plan is active.
- T4 binder is queued after T1.
- UI MR CRUD is Windows/VM gated.

- [ ] **Step 2: Create PR-1 branch**

Run:

```bash
rtk git switch -c codex/t1-manifest-driven-runtime-envs
```

Expected: new branch from latest `main`.

- [ ] **Step 3: Execute the T1 plan task-by-task**

Use `superpowers:subagent-driven-development` or `superpowers:executing-plans` with:

```text
Execute docs/superpowers/plans/2026-05-26-t1-manifest-driven-runtime-environments-plan.md exactly.
Respect all preconditions and stop conditions.
Use TDD.
Do not touch Method MT.
Do not touch WPF unless constructor compatibility fails and the plan's stop condition requires splitting a Windows PR.
```

- [ ] **Step 4: Required focused red/green evidence**

Run before implementation where the plan asks for red:

```bash
rtk dotnet test MetBench_SystemMT.Tests --no-restore --filter "FullyQualifiedName~RuntimeEnvironmentResolverTests|FullyQualifiedName~ManifestMrCatalogProviderTests"
```

Expected before implementation: fail because resolver behavior is not present.

Run after implementation:

```bash
rtk dotnet test MetBench_SystemMT.Tests --no-restore --filter "FullyQualifiedName~RuntimeEnvironmentResolverTests|FullyQualifiedName~ManifestMrCatalogProviderTests"
```

Expected after implementation: pass.

- [ ] **Step 5: Required full verification**

Run:

```bash
rtk dotnet test MetBench_SystemMT.Tests --no-restore
```

Expected:

- 0 failures.
- Existing OpenMOC/OpenMC/SciPy tests may skip only with explicit environment reasons.

- [ ] **Step 6: PR-1 review**

Layer 1 self-review:

- Runtime keys are generic and manifest-driven.
- Existing `system`, `openmoc`, `openmc`, and `scipy` behavior is preserved.
- Unknown non-system keys fail closed with a clear message.
- No new SUT is added.
- No Method MT change.
- No WPF change unless split was required.

Layer 2 maintainer review:

- Would future `fenics` or `fipy` require a new `LauncherOptions.<sut>Python` field? Expected answer: no.
- Can monitoring still claim T1 multi-env open after merge? Expected answer: no, ledger must move to controlled.
- Are external-runtime skips still honest? Expected answer: yes.

- [ ] **Step 7: Commit PR-1**

Run:

```bash
rtk git status --short
rtk git add MetBench_BLL.Core/SystemMT/Launcher/LauncherOptions.cs MetBench_BLL.Core/SystemMT/Launcher/RuntimeEnvironmentResolutionException.cs MetBench_BLL.Core/SystemMT/Catalog/ManifestMrCatalogProvider.cs MetBench_BLL.Core/SystemMT/Catalog/PythonExecutableKinds.cs MetBench_SystemMT.Tests/SystemMT/Launcher/RuntimeEnvironmentResolverTests.cs MetBench_SystemMT.Tests/SystemMT/Catalog/ManifestMrCatalogProviderTests.cs docs/status/current.md docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md
rtk git commit -m "feat(t1): resolve SUT runtime environments from manifest keys"
```

Expected: one PR-1 feature commit.

- [ ] **Step 8: Push and merge PR-1**

Run:

```bash
rtk git push -u origin codex/t1-manifest-driven-runtime-envs
rtk gh pr create --base main --head codex/t1-manifest-driven-runtime-envs --title "feat(t1): resolve SUT runtime environments from manifest keys" --body "## Summary
- Adds manifest-driven runtime-key resolution for System MT SUTs.
- Preserves existing system/openmoc/openmc/scipy behavior.
- Fails closed for unconfigured future runtime keys.
- Updates the status ledger so T1 multi-env management becomes controlled.

## Tests
- rtk dotnet test MetBench_SystemMT.Tests --no-restore --filter \"FullyQualifiedName~RuntimeEnvironmentResolverTests|FullyQualifiedName~ManifestMrCatalogProviderTests\"
- rtk dotnet test MetBench_SystemMT.Tests --no-restore

## Scope
- Cloud-side only.
- No Method MT changes.
- No WPF changes unless explicitly noted."
rtk gh pr checks --watch
rtk gh pr merge --squash --delete-branch
rtk git switch main
rtk git pull --ff-only origin main
```

Expected: PR-1 merged, local `main` updated.

## Task 3: PR-2 T4-To-T0 Binder

**Files:**
- Execute plan: `docs/superpowers/plans/2026-05-26-t4-to-t0-mr-discovery-binder-plan.md`
- Create expected: `MetBench_BLL.Core/SystemMT/Catalog/Binding/DiscoveredMrCatalogBinder.cs`
- Create expected: `MetBench_BLL.Core/SystemMT/Catalog/Binding/DiscoveredMrBindingDraft.cs`
- Create expected: `MetBench_BLL.Core/SystemMT/Catalog/Binding/DiscoveredMrBindingError.cs`
- Create expected: `MetBench_BLL.Core/SystemMT/Catalog/Binding/DiscoveredMrBindingResult.cs`
- Create expected: `MetBench_BLL.Core/SystemMT/Catalog/Binding/IDiscoveredMrCatalogBinder.cs`
- Test expected: `MetBench_SystemMT.Tests/SystemMT/Catalog/Binding/DiscoveredMrCatalogBinderTests.cs`
- Docs expected: `docs/status/current.md`
- Docs expected: `docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md`

- [ ] **Step 1: Verify PR-2 gate**

Run:

```bash
rtk git switch main
rtk git pull --ff-only origin main
rtk rg -n "T1 multi-env management|T4-to-T0 binder|2026-05-26-t4-to-t0-mr-discovery-binder-plan" docs/status/current.md docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md
```

Expected:

- T1 multi-env is controlled, or the ledger explicitly waives T1 for binder work.
- T4 binder plan is still queued or active.

Stop if T1 is still open and no waiver exists.

- [ ] **Step 2: Create PR-2 branch**

Run:

```bash
rtk git switch -c codex/t4-to-t0-discovery-binder
```

Expected: new branch from latest `main`.

- [ ] **Step 3: Execute the T4 binder plan task-by-task**

Use `superpowers:subagent-driven-development` or `superpowers:executing-plans` with:

```text
Execute docs/superpowers/plans/2026-05-26-t4-to-t0-mr-discovery-binder-plan.md exactly.
Respect all preconditions and stop conditions.
Use TDD.
Do not call LLM APIs.
Do not mutate active SUT/<sut>/catalog.json files automatically.
Do not touch Method MT.
Do not touch WPF.
```

- [ ] **Step 4: Required focused red/green evidence**

Run before implementation where the plan asks for red:

```bash
rtk dotnet test MetBench_SystemMT.Tests --no-restore --filter FullyQualifiedName~DiscoveredMrCatalogBinderTests
```

Expected before implementation: fail because binder types do not exist.

Run after implementation:

```bash
rtk dotnet test MetBench_SystemMT.Tests --no-restore --filter FullyQualifiedName~DiscoveredMrCatalogBinderTests
```

Expected after implementation: pass.

- [ ] **Step 5: Required full verification**

Run:

```bash
rtk dotnet test MetBench_SystemMT.Tests --no-restore
```

Expected: 0 failures. Existing external-runtime skips remain skip-safe.

- [ ] **Step 6: PR-2 review**

Layer 1 self-review:

- Binder does not mutate active catalog files.
- Binder output passes existing catalog validation.
- Invalid candidates fail closed.
- Discovery provenance is retained.
- No LLM calls.
- No Method MT changes.
- No WPF changes.

Layer 2 maintainer review:

- Is "discovered" clearly distinct from "catalog-bound" and "executable"? Expected answer: yes.
- Could unreviewed candidates enter T0 execution automatically? Expected answer: no.
- Can monitoring trace candidate provenance? Expected answer: yes.

- [ ] **Step 7: Commit PR-2**

Run:

```bash
rtk git status --short
rtk git add MetBench_BLL.Core/SystemMT/Catalog/Binding MetBench_SystemMT.Tests/SystemMT/Catalog/Binding docs/status/current.md docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md
rtk git commit -m "feat(t4): bind discovery candidates to draft System MT catalog assets"
```

Expected: one PR-2 feature commit.

- [ ] **Step 8: Push and merge PR-2**

Run:

```bash
rtk git push -u origin codex/t4-to-t0-discovery-binder
rtk gh pr create --base main --head codex/t4-to-t0-discovery-binder --title "feat(t4): bind discovery candidates to draft System MT catalog assets" --body "## Summary
- Adds a fail-closed binder from validated T4 discovery drafts to draft System MT catalog assets.
- Records discovery provenance.
- Keeps active SUT catalogs immutable unless a later approval step applies the draft.

## Tests
- rtk dotnet test MetBench_SystemMT.Tests --no-restore --filter FullyQualifiedName~DiscoveredMrCatalogBinderTests
- rtk dotnet test MetBench_SystemMT.Tests --no-restore

## Scope
- Cloud-side only.
- No LLM API calls.
- No Method MT changes.
- No WPF changes.
- No automatic mutation of SUT/<sut>/catalog.json."
rtk gh pr checks --watch
rtk gh pr merge --squash --delete-branch
rtk git switch main
rtk git pull --ff-only origin main
```

Expected: PR-2 merged, local `main` updated.

## Task 4: PR-UI Windows/VM MR CRUD Planning Gate

**Files:**
- Create later: `docs/superpowers/plans/2026-05-26-t1-ui-mr-crud-windows-vm-plan.md`
- Modify later: `docs/status/current.md`
- Modify later: `docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md`

- [ ] **Step 1: Verify UI work is still gated**

Run:

```bash
rtk rg -n "T1 UI MR CRUD|Windows/VM|MetBench_Client" docs/status/current.md docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md
```

Expected: UI MR CRUD is open and Windows/VM scoped.

- [ ] **Step 2: Write a separate Windows/VM plan before touching UI**

The UI plan must include:

- WPF page/viewmodel/file list.
- MR catalog read/list/create/update/validate/save workflow.
- SSH command path for Windows build and log collection.
- RDP/FlaUI verification path for visible interaction.
- Explicit no-cloud-build caveat for `MetBench_Client`.
- Acceptance criteria that a non-author can add, inspect, edit, validate, and save MR assets without hand-editing JSON.

- [ ] **Step 3: Stop before implementation**

Do not edit `MetBench_Client` until the UI plan is reviewed and approved.

## Global Review Checklist

Run this checklist after each PR:

- The PR is derived from the active plan index.
- The status ledger and active plan index agree.
- The PR does not mix cloud-side BLL work with Windows-only UI work.
- The PR has red/green TDD evidence when it changes code.
- The PR has full `MetBench_SystemMT.Tests` evidence when it changes code.
- The PR body states external dependency limitations honestly.
- The PR body states Windows validation classification honestly.
- Two-layer review is recorded before merge.

## Global Stop Conditions

Stop and report instead of coding if:

- `origin/main` cannot be fetched.
- Working tree is dirty with unrelated changes.
- A required prior PR is not merged.
- The status ledger contradicts the active plan index.
- T4 binder is about to start while T1 multi-env is still open and not explicitly waived.
- UI work is requested inside PR-0, PR-1, or PR-2.
- Any implementation requires reintroducing Method MT into System MT.
- Any implementation requires bypassing typed catalog validation or fail-closed behavior.

## Acceptance Criteria

- PR-0 merged: control docs truthfully mark T1 multi-env and UI MR CRUD as open; T1/T4/UI execution order is explicit.
- PR-1 merged: new SUT runtime families no longer require adding a new `LauncherOptions.<sut>Python` field.
- PR-2 merged: T4 discovery candidates can be transformed into draft T0 catalog assets with provenance and fail-closed validation.
- PR-UI not started until a Windows/VM plan is separately approved.
- Monitoring can no longer misreport runner additivity as full T1 completion.

## Execution Handoff

Recommended execution mode after this plan merges:

1. Subagent-Driven for PR-1 and PR-2.
2. Inline execution is acceptable for PR-0 push/merge monitoring.
3. Windows/VM UI work requires a separate plan and visible verification path.
