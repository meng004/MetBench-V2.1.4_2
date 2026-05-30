# Claude Code VM Prompt: T0-T5 Release Readiness Smoke

You are Claude Code running inside the Windows Parallels VM. Your task is to
execute the Windows side of the MetBench T0-T5 release-readiness confirmation
for the target head below and return evidence through GitHub.

## Target

| Field | Value |
|---|---|
| Repository | `limeng40/MetBench-V2.1.4` |
| Coordinator branch | `codex/t0-t5-release-readiness` |
| VM branch | `claude/vm-t0-t5-release-readiness` |
| Target production base | `b9e917c15683c37466f23e2c4927aecc6cdff8b2` |
| Run id | `t0-t5-2026-05-30` |

## Hard Rules

- Do not change production code.
- Do not bypass tests or hooks.
- Do not mark a check `pass` without command output or screenshot evidence.
- Push progress through GitHub using the VM branch.
- If the branch contains production-code changes relative to the target
  production base, stop and push a `blocked` status event.

## Setup

```powershell
git fetch origin
git checkout -B claude/vm-t0-t5-release-readiness origin/codex/t0-t5-release-readiness
git rev-parse HEAD
git diff --name-only b9e917c15683c37466f23e2c4927aecc6cdff8b2..HEAD -- . ":(exclude)docs/superpowers/plans/2026-05-30-t0-t5-minimal-release-readiness-confirmation-plan.md" ":(exclude)docs/superpowers/specs/2026-05-30-t0-t5-github-exchange-protocol.md" ":(exclude)docs/superpowers/specs/2026-05-30-t0-t5-vm-release-smoke/**" ":(exclude)docs/superpowers/vm-prompts/2026-05-30-t0-t5-release-readiness-vm-prompt.md" ":(exclude)docs/superpowers/vm-prompts/2026-05-30-t0-t5-vm-monitor-hook.md" ":(exclude)tools/release-readiness/vm_status_hook.ps1"
```

If the `git diff --name-only ...` command prints any path, record those paths in
`vm-summary.md`, run the hook with `blocked`, push, and stop.

Read these files before executing:

```text
docs/superpowers/plans/2026-05-30-t0-t5-minimal-release-readiness-confirmation-plan.md
docs/superpowers/specs/2026-05-30-t0-t5-github-exchange-protocol.md
docs/superpowers/vm-prompts/2026-05-30-t0-t5-vm-monitor-hook.md
tools/release-readiness/vm_status_hook.ps1
docs/superpowers/specs/2026-05-30-t0-t5-vm-release-smoke/README.md
```

## Evidence Directory

Use:

```text
docs\superpowers\specs\2026-05-30-t0-t5-vm-release-smoke
```

Create or update:

```text
vm-status.jsonl
vm-summary.md
01-build-output.png
02-system-mt-run-or-catalog.png
03-mr-catalog.png
04-sut-catalog.png
05-equation-catalog.png
06-samplecase-catalog.png
07-execution-history.png
08-reporting-or-export.png
09-anomaly-list.png
10-anomaly-status-action.png
```

## Hook Usage

Use the hook at every checkpoint:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\release-readiness\vm_status_hook.ps1 `
  -RunId "t0-t5-2026-05-30" `
  -Step "setup" `
  -Status "running" `
  -Message "starting checkout and target head validation" `
  -EvidenceDir "docs\superpowers\specs\2026-05-30-t0-t5-vm-release-smoke" `
  -Push
```

## Required Commands

Run the Windows build/test checks needed for the same T0-T5 core functions:

```powershell
dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~SystemMtLauncherTests.RunAsync_heat_equation_with_default_factor_passes_and_persists_execution_result" --logger "console;verbosity=minimal"
dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~LauncherEndToEndAdvectionTests.RunAsync_advection_mesh_conservation_passes_end_to_end" --logger "console;verbosity=minimal"
dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~LauncherEndToEndWaveTests.RunAsync_wave_mesh_energy_convergence_passes_end_to_end" --logger "console;verbosity=minimal"
dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~SystemMtManifestCatalogEditorTests" --logger "console;verbosity=minimal"
dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~SystemMtSutEditorTests" --logger "console;verbosity=minimal"
dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~SystemMtEquationEditorTests" --logger "console;verbosity=minimal"
dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~SystemMtSampleCaseEditorTests" --logger "console;verbosity=minimal"
dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~ExecutionHistoryEditorTests" --logger "console;verbosity=minimal"
dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~SystemMtLauncherTests.RunAsync_persists_failure_when_assertion_fails" --logger "console;verbosity=minimal"
dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~V2Anomaly.AnomalyStatusTests" --logger "console;verbosity=minimal"
dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~AnomalyStatusPersistenceTests" --logger "console;verbosity=minimal"
dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~AnomalyOrphanSweeperTests" --logger "console;verbosity=minimal"
dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~V2Pipeline.ReplayServiceTests" --logger "console;verbosity=minimal"
dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~V2Pipeline.ReplayContextBuilderTests" --logger "console;verbosity=minimal"
dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~SystemMtReportServiceTests" --logger "console;verbosity=minimal"
dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~HtmlSystemMtResultReportRendererTests" --logger "console;verbosity=minimal"
dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~PdfSystemMtResultReportRendererTests" --logger "console;verbosity=minimal"
dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~WordSystemMtResultReportRendererTests" --logger "console;verbosity=minimal"
dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~ExcelSystemMtResultReportRendererTests" --logger "console;verbosity=minimal"
dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~SystemMT.Reporting.Charts" --logger "console;verbosity=minimal"
dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~DiscoveredMrCatalogBinderTests" --logger "console;verbosity=minimal"
dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~Catalog_MR_id_set_equals_governance_whitelist" --logger "console;verbosity=minimal"
```

Record every command result in `vm-summary.md`.

## Screenshot Evidence

Use UIA, Playwright-for-WPF tooling, manual screenshot, or the existing VM
smokeshot harness if available. The screenshot matrix in the README must have
one row for every T0-T5 core check and must be updated from `PENDING` to
`PASS`, `FAIL`, or `BLOCKED`.

Minimum screenshot contents:

| File | Required content |
|---|---|
| `01-build-output.png` | Windows build/test output |
| `02-system-mt-run-or-catalog.png` | selected MR or run context |
| `03-mr-catalog.png` | MR catalog/editor |
| `04-sut-catalog.png` | SUT catalog/editor |
| `05-equation-catalog.png` | equation metadata/editor |
| `06-samplecase-catalog.png` | sample case/editor |
| `07-execution-history.png` | execution/result history |
| `08-reporting-or-export.png` | report/export/chart surface |
| `09-anomaly-list.png` | anomaly list from deliberate failure |
| `10-anomaly-status-action.png` | anomaly status action or persisted status |

## Final Summary

Write `vm-summary.md` with:

```markdown
# T0-T5 VM Release Smoke Summary

| Field | Value |
|---|---|
| Run id | t0-t5-2026-05-30 |
| Branch | claude/vm-t0-t5-release-readiness |
| HEAD | actual SHA from git rev-parse HEAD |
| Target production base | b9e917c15683c37466f23e2c4927aecc6cdff8b2 |
| Production-code delta from target base | YES/NO |
| Command checks | pass count / total command count |
| Screenshot matrix | complete/incomplete |
| Final VM decision | PASS/FAIL/BLOCKED |

## Command Evidence

| Command | Result | Notes |
|---|---|---|

## Screenshot Evidence

| File | Result | Notes |
|---|---|---|

## Blockers

None, or exact blocker plus smallest unblock action.
```

Run the hook one final time with `pass`, `fail`, or `blocked`, then push.
