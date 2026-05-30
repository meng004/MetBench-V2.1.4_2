# T0-T5 VM Monitor Hook Operating Notes

## Purpose

Use `tools/release-readiness/vm_status_hook.ps1` inside the Windows VM to create
an auditable heartbeat while Claude Code runs the T0-T5 release smoke.

## Required Invocation Pattern

Run this command at the start and end of every major checkpoint:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\release-readiness\vm_status_hook.ps1 `
  -RunId "t0-t5-2026-05-30" `
  -Step "setup" `
  -Status "running" `
  -Message "starting checkout and head validation" `
  -EvidenceDir "docs\superpowers\specs\2026-05-30-t0-t5-vm-release-smoke" `
  -Push
```

Allowed statuses:

```text
queued, running, pass, fail, skip, blocked, info
```

## Cadence

Emit and push a status event after:

1. Checkout and target-head validation.
2. Restore/build.
3. Selected T0/T1/T3 smoke commands.
4. T1 CRUD/editor command checks.
5. T2 reporting command checks.
6. T4 binder command checks.
7. T5 anomaly workflow command checks.
8. Screenshot capture.
9. Final summary.

If a step runs longer than 15 minutes, emit an `info` event before continuing.

## Evidence Rules

- Do not mark a step `pass` unless the matching command output or screenshot has
  already been written under the evidence directory.
- Use `blocked` only for an environmental issue that prevents meaningful
  execution, such as the wrong target head or a missing Windows runtime.
- Use `fail` for product or test failures.
- The VM branch must not include production code edits.

## Coordinator Polling

The macOS coordinator watches the VM branch with:

```bash
rtk git fetch origin claude/vm-t0-t5-release-readiness
rtk git show origin/claude/vm-t0-t5-release-readiness:docs/superpowers/specs/2026-05-30-t0-t5-vm-release-smoke/vm-status.jsonl
```

If no status stream appears, the coordinator must record Windows evidence as
`NOT RUN`.
