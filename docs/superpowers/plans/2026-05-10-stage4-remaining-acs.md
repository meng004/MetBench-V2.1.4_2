# Stage 4 Remaining ACs — Cloud + Windows VM Workflow Plan

> **For agentic + human collaborators:** Two environments now in play —
> Linux cloud (this Claude Code Web session) for BLL.Core / DAL / tests, and
> Windows 11 + VS 2022 (Parallels VM) for WPF. Each AC is tagged with its
> environment, dependencies, and parallelizability.

**Goal:** Close out the remaining Stage 4 acceptance criteria from `AGENTS.md`
without violating the "spec-only" boundaries the earlier plans established.

**Date:** 2026-05-10
**Status:** Active
**Predecessor PRs (already merged):** #10 (CI), #11 (.gitignore), #12 (Stage 4
AC #2 partial — persistence layer), #13 (Stage 4 AC #5 — second SUT).

---

## Stage 4 AC scoreboard

| AC | Description | Status | Owner |
|----|-------------|--------|-------|
| #1 | Users launch system-level MT tasks from WPF | ❌ | VM (after Cloud lands #1-A facade) |
| #2 | Each run result persisted **and reviewable** | 🟡 persisted only | Cloud done; review surface = VM |
| #3 | ≥1 report format generated | ❌ | Cloud (this PR onwards) |
| #4 | Batch execution of features/scenarios | ❌ | VM |
| #5 | ≥ design/prototype for second program adapter | ✅ heat-equation | — |
| #6 | Same MR on different programs via IR | ❌ | Cloud (architectural; defer until VM stable) |

---

## Environment split

- **Cloud (Linux, Claude Code Web)**
  - `MetBench_BLL.Core/`, `MetBench_DAL/`, `MetBench_SystemMT.Tests/`
  - Reporting (#3), launcher facade (#1-A), IR refactor (#6)
  - GHA `test` job is the gatekeeper. Every PR must be green.
- **VM (Windows 11 + VS 2022)**
  - `MetBench_BLL/` (WPF), `MetBench_Client/`, `App.config`, XAML resources
  - All UI work: launch dialog (#1-B), review window (#2 surface), batch button (#4)
  - VM contributes via PRs into the same repo; CI still runs the Linux subset.

**Hard rules:**
- Cloud must NOT touch `MetBench_BLL/*.xaml*`, `MetBench_Client/*.xaml*`, or
  any `App.config` — these are WPF concerns, untestable on Linux.
- VM must NOT modify `MetBench_BLL.Core/SystemMT/*` public types without first
  proposing the change in a Cloud PR (so CI catches breakage in 49+ tests).
- Coupling between Cloud and VM is mediated via a stable **launcher facade**
  (introduced in PR for AC #1-A); see "Coupling contract" below.

---

## Phases and parallelism

### Phase 1 — independent kickoff

| Track | Where | Work | Output |
|-------|-------|------|--------|
| 1A | Cloud | **AC #3 HTML report** (this PR) | `IReportRenderer` + `HtmlSystemMtResultReportRenderer` + tests |
| 1B | VM | Set up dev environment | VS 2022 installed, baseline `dotnet build MetBench.sln` succeeds, branch protection set on `main` |

These tracks are fully parallel. Track 1B is one-time setup; track 1A produces
a mergeable PR.

### Phase 2 — facade contract

| Track | Where | Work | Output |
|-------|-------|------|--------|
| 2A | Cloud | **AC #1-A launcher facade** | `ISystemMtScenarioLauncher` + DTOs + impl + tests |
| 2B | VM | Familiarize with WPF state | Read `MetBench_BLL/`, `MetBench_Client/App.xaml.cs`; confirm DI composition root |

Track 2A produces the API surface VM consumes in Phase 3. Track 2B is reading-only.

### Phase 3 — parallel execution

| Track | Where | Work |
|-------|-------|------|
| 3A | Cloud | **AC #6 IR refactor** (or report format extensions) |
| 3B | VM | **AC #1-B WPF launch UI** — consumes Phase 2 facade |

Both tracks ship independent PRs. Conflict surface is small if facade contract
holds.

### Phase 4 — VM-side completion

| Order | Where | Work |
|-------|-------|------|
| 4.1 | VM | **AC #2 review window** |
| 4.2 | VM | **AC #4 batch execution** |

Sequential within VM (single developer). Cloud may extend reports
(Word/PDF/Excel) in parallel if useful.

---

## Critical path

VM-side work is the bottleneck:

```
Phase 2A (#1-A facade, ~1 PR) → Phase 3B (#1-B WPF UI, 1-2 days)
   → Phase 4.1 (#2 review, ~½ day) → Phase 4.2 (#4 batch, hours)
```

Cloud can complete AC #3 + #6 in parallel without slowing VM down.

Estimated total wall-clock to all 6 Stage 4 ACs done: **3-4 working days**
assuming Phase 2A facade lands within 24h of Phase 1A.

---

## Coupling contract (VM ↔ Cloud)

VM consumes BLL.Core only through these stable interfaces:

- `ISystemMtScenarioLauncher` — launches one scenario by id, returns result.
- `ISystemMtResultReportRenderer` — renders persisted records to a string.
- `ISystemMtResultRepository` — already merged in #12.

**Type-leakage rule:** Facade methods accept/return DTO types (strings,
primitives, `Dictionary<string,string>`, `SystemMtResultRecord`). They must
NOT expose internal BLL.Core types like `MrTransformation`, `SystemMtTask`,
`SystemMtRunner`. This insulates the VM-side WPF code from Cloud-side
refactors (notably the AC #6 IR work).

If a Cloud PR breaks any of the three interfaces above, label it
`breaking-facade` — VM must rebuild and re-test before merge.

---

## Risks

| Risk | Mitigation |
|------|------------|
| AC #6 IR refactor breaks AC #1-B WPF code | Facade DTO insulation (rule above); land IR after WPF stabilizes if conflicts emerge |
| Cloud cannot test WPF changes | VM owner is sole reviewer for `*.xaml*` / `*.xaml.cs` PRs |
| GHA OpenMOC build remains broken | Out of scope here; tracked as PR #10 follow-up |
| `BsonMapper.Global` hidden coupling resurfaces | Already audited (no observed bug); future LiteDB consumers must use isolated mapper per PR #12 precedent |
| PAT shared in chat is still live | User must revoke (see "User-side residual actions") |

---

## Local VM operation steps

For the human operator on Mac running Parallels Desktop.

### One-time environment setup

1. **Create Windows 11 ARM VM** (Parallels Desktop):
   - Allocate ≥ 8 GB RAM, ≥ 60 GB disk, 4 vCPUs.
   - Enable nested virtualization off (not needed); Coherence mode off
     (XAML Designer dislikes it).
   - Install Parallels Tools.

2. **Install Visual Studio 2022 Community** (free):
   - Workloads: ✅ *.NET Desktop Development*, ✅ *.NET Multi-platform App UI
     development* (optional, for future Avalonia experiments).
   - Individual components: ✅ *.NET 8 SDK*, ✅ *Git for Windows*, ✅ *GitHub
     CLI*.

3. **Authenticate GitHub** (do NOT paste a PAT):
   ```powershell
   gh auth login
   # Choose: GitHub.com → HTTPS → Login with web browser
   ```

4. **Clone the repo into the VM** (avoid Parallels shared folders for active dev):
   ```powershell
   cd $HOME
   git clone https://github.com/meng004/MetBench-V2.1.4_2.git
   cd MetBench-V2.1.4_2
   ```

5. **Verify baseline build succeeds**:
   ```powershell
   dotnet build MetBench.sln
   ```
   Expected: 0 errors. If WPF projects fail to load, install the
   *.NET Desktop Development* workload (step 2).

6. **Verify the WPF entry runs** (smoke test):
   ```powershell
   dotnet run --project MetBench_Client
   ```
   Expected: WPF main window appears. Close it.

### After Phase 2A facade PR lands

7. **Pull and rebuild** before starting AC #1-B work:
   ```powershell
   git fetch origin
   git checkout main
   git pull --ff-only
   git checkout -b feature/stage4-wpf-launch-ui
   dotnet build MetBench.sln
   ```

8. **Reference the facade contract** (Cloud-supplied):
   - Public surface: `MetBench_BLL.Core/SystemMT/ISystemMtScenarioLauncher.cs`
   - DTOs: `MetBench_BLL.Core/SystemMT/Launcher/*.cs`
   - Existing repository: `MetBench_BLL.Core/SystemMT/Persistence/ISystemMtResultRepository.cs`

9. **Develop in VS 2022**:
   - Open `MetBench.sln`.
   - Add new XAML files under `MetBench_Client/Views/`.
   - Wire DI in `MetBench_Client/App.xaml.cs`.
   - Run F5 to debug.

10. **Push and open PR** (CI will run the Linux subset; WPF code itself
    is reviewed by the VM operator):
    ```powershell
    git push -u origin feature/stage4-wpf-launch-ui
    gh pr create --base main --title "feat(stage4): WPF launch UI for system-level MT (#1-B)" --body-file pr-body.md
    ```

---

## User-side residual actions (independent of dev environment)

| Action | Where | Why |
|--------|-------|-----|
| **Revoke PAT** | https://github.com/settings/tokens | A PAT was shared in chat history; treat as compromised |
| **Set branch protection on `main`** | repo Settings → Branches → Add ruleset | Make the GHA `test` job a required check; prevents red CI from being merged |

These two actions block nothing technically but are non-negotiable for
operational safety.

---

## Stage 4 AC #6 — IR philosophy (closed by PR #22)

The acceptance criterion reads:

> *"Gradually support running the same MR on different programs through an
> intermediate representation and adapter pattern."*

**Realised design**: the intermediate representation is `MrTransformation(name,
parameters)` — already defined in BLL.Core since Stage 2. No new class
hierarchy was required. Two solvers are considered to implement the same MR
when:

1. They register scenarios with the same `TransformationName` and same
   `ParameterOverrides` shape (e.g. `ScaleNuSigmaF` with `{factor: <positive>}`).
2. They use a common input JSON schema (the `SUT/openmc/sample/pincell.json`
   is byte-for-byte the OpenMOC `pincell.json`).
3. Their assertion direction agrees (both `GreaterThan` for `NuSigmaF`, both
   `LessThan` for `SigmaA`).
4. Their `ScenarioDescriptor.MrFamily` tags match (e.g.
   `NeutronTransport.Scaling.NuSigmaF`). This is the **taxonomy slug** that
   makes the cross-program relationship explicit at the API layer; the
   launcher's `ListAvailableAsync` groups by it.

**No new abstraction class** beyond `MrFamily : string = ""` on
`ScenarioDescriptor`. The cross-program demonstration is one `.feature` file
(`CrossProgramNeutronTransportMrs.feature`) with two Scenario Outlines, each
parameterised by `<solver>` ∈ {`openmoc`, `openmc`}. Step bindings dispatch
to the per-solver adapter scripts and python interpreter; the
`MrTransformation` flowing through is **identical** across both runs.

**Demonstration evidence**:

- 4 BDD scenario instances (2 MRs × 2 solvers).
- OpenMOC scenarios pass end-to-end in environments with OpenMOC importable
  (cloud sandbox, dev workstations with `METBENCH_OPENMOC_PYTHON` set).
- OpenMC scenarios will pass end-to-end on machines with `METBENCH_OPENMC_PYTHON`
  pointing at an OpenMC venv. They skip cleanly elsewhere via
  `OpenMcTestPaths.OpenMcImportable()`.
- The launcher's `ListAvailableAsync_groups_cross_program_scenarios_by_MrFamily`
  test asserts that `NeutronTransport.Scaling.NuSigmaF` is shared by
  `openmoc-pincell-nu-sigma-f` and `openmc-pincell-nu-sigma-f`, and similarly
  for SigmaA. Same MR, two solver implementations, single taxonomy slug.

**Adding a third solver** under the same MR family becomes:

1. Drop new Python scripts under `SUT/<solver>/` with `<solver>_runner.py`,
   `<solver>_input_adapter.py`, `<solver>_input_adapter_sigma_a.py`,
   `<solver>_output_adapter.py`.
2. Register the scenarios in `SystemMtScenarioLauncher.BuildScenarios` with
   `MrFamily: "NeutronTransport.Scaling.NuSigmaF"`.
3. Add `<solver>` to the `Examples` table of
   `CrossProgramNeutronTransportMrs.feature`.
4. Add the new asset-copy `<None Include>` blocks to the test csproj +
   `MetBench_Client.csproj`.

No BLL.Core changes required. No new interfaces. The IR is the
`MrTransformation` record; the adapter pattern is the per-solver
`*_input_adapter*.py` scripts; the family taxonomy is the
`ScenarioDescriptor.MrFamily` string.

---

## Stage 4 AC scoreboard (final)

| AC | Status | Where |
|----|--------|-------|
| #1 WPF launch UI | ✅ | PR #17 (SystemMtExecutionPage) |
| #2 review surface | ✅ | PR #12 (persistence) + PR #17 (UI) |
| #3 report | ✅ | PR #14 (HTML) |
| #4 batch execution | ✅ | PR #20 (RunBatchAsync) |
| #5 second SUT | ✅ | PR #13 (heat-equation) + PR #21 (OpenMC, bonus) |
| #6 cross-program IR | ✅ | PR #22 (MrFamily + cross-program BDD) |

**Stage 4 closed.** Roadmap continues per `AGENTS.md`.

**Stage 5 Phase 1 hand-off** (mutation-based empirical validation): plan in
[`2026-05-12-stage5-mutation-detection-study.md`](2026-05-12-stage5-mutation-detection-study.md);
results in [`../../experiments/`](../../experiments/).

---

## Out of scope

- OpenMOC build in CI (PR #10 follow-up).
- Avalonia UI migration (tracked as a possible future direction, not committed).
- DbConfig.cs `BsonMapper.Global` migration (audited; no observed bug; YAGNI).
- Method/unit-level MT changes (Stage 1-3 closed; do not regress).
