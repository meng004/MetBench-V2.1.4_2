# T0-T5 Release Readiness GitHub Exchange Protocol

## Purpose

This protocol coordinates the macOS release-readiness assessment branch with the
Windows VM Claude Code run. It exists because full release readiness requires
both cloud-safe command evidence and Windows WPF screenshot evidence for the same
target repository head.

## Branches

| Role | Branch |
|---|---|
| Coordinator package | `codex/t0-t5-release-readiness` |
| VM evidence return | `claude/vm-t0-t5-release-readiness` |

Remote repository:

```text
https://github.com/meng004/MetBench-V2.1.4_2.git
```

The VM worker must base its branch on the coordinator package branch after the
package is pushed. The VM branch must not contain production code changes.

## Target Production Base

| Field | Value |
|---|---|
| Target production base | `b9e917c15683c37466f23e2c4927aecc6cdff8b2` |
| Run id | `t0-t5-2026-05-30` |

The coordinator branch may contain documentation, prompt, and evidence-hook
commits on top of this base. The VM must confirm that the branch has no
production-code delta from the target production base before running tests.

## Evidence Paths

| Path | Owner | Purpose |
|---|---|---|
| `docs/superpowers/specs/2026-05-30-t0-t5-vm-release-smoke/README.md` | VM | Evidence manifest and screenshot matrix |
| `docs/superpowers/specs/2026-05-30-t0-t5-vm-release-smoke/vm-status.jsonl` | VM hook | Append-only status stream |
| `docs/superpowers/specs/2026-05-30-t0-t5-vm-release-smoke/vm-summary.md` | VM | Final command and UI receipt |
| `docs/superpowers/specs/2026-05-30-t0-t5-vm-release-smoke/*.png` | VM | Screenshot evidence |
| `docs/superpowers/specs/2026-05-30-t0-t5-vm-release-smoke/claude-vm-run.log` | VM bootstrap | Claude Code CLI execution log |

## Status Event Schema

Each `vm-status.jsonl` line is compact JSON with these fields:

| Field | Required | Meaning |
|---|---|---|
| `timestamp_utc` | yes | UTC timestamp in ISO-8601 format |
| `run_id` | yes | `t0-t5-2026-05-30` |
| `branch` | yes | Current VM git branch |
| `head` | yes | `git rev-parse HEAD` result |
| `step` | yes | Logical step, for example `setup`, `build`, `T1-5`, `final` |
| `status` | yes | One of `queued`, `running`, `pass`, `fail`, `skip`, `blocked`, `info` |
| `message` | yes | One short evidence statement |
| `evidence_dir` | yes | Evidence directory path |
| `screenshots` | yes | Screenshot file names observed in the evidence directory |
| `log_path` | no | Command log or summary path |

## VM Push Cadence

The VM worker must commit and push after these checkpoints:

1. Setup/head validation.
2. Windows build result.
3. T0/T1/T3 selected smoke result.
4. T1/T2/T4/T5 focused command evidence.
5. Screenshot matrix completion.
6. Final summary.

Push commit messages should use:

```text
docs(vm): record t0-t5 release smoke setup pass
```

## Coordinator Polling

Run from the macOS coordinator worktree:

```bash
rtk git fetch origin claude/vm-t0-t5-release-readiness
rtk git show origin/claude/vm-t0-t5-release-readiness:docs/superpowers/specs/2026-05-30-t0-t5-vm-release-smoke/vm-status.jsonl
rtk git show origin/claude/vm-t0-t5-release-readiness:docs/superpowers/specs/2026-05-30-t0-t5-vm-release-smoke/vm-summary.md
```

If either file is missing, Windows VM evidence is not available for the target
head and the final decision cannot be `RELEASE-READY`.

## VM Bootstrap

The coordinator can start the VM-side Claude Code CLI through Parallels Tools:

```bash
rtk prlctl exec "Windows 11" --current-user powershell -NoProfile -ExecutionPolicy Bypass -Command "if (!(Test-Path C:\Users\codex\metbench-t0-t5-release-readiness\.git)) { git clone https://github.com/meng004/MetBench-V2.1.4_2.git C:\Users\codex\metbench-t0-t5-release-readiness }; Set-Location C:\Users\codex\metbench-t0-t5-release-readiness; git fetch origin; git checkout -B claude/vm-t0-t5-release-readiness origin/codex/t0-t5-release-readiness; powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\release-readiness\start_vm_claude_t0_t5.ps1 -Background"
```

The bootstrap writes `claude-vm-run.log` under the evidence directory and uses
the same VM evidence branch.

## Required Screenshot Set

| File | Required content |
|---|---|
| `01-build-output.png` | Windows build/test command result |
| `02-system-mt-run-or-catalog.png` | System-MT MR run or catalog view showing selected MR context |
| `03-mr-catalog.png` | MR catalog/editor surface |
| `04-sut-catalog.png` | SUT catalog/editor surface |
| `05-equation-catalog.png` | Equation metadata/editor surface |
| `06-samplecase-catalog.png` | Sample case/editor surface |
| `07-execution-history.png` | persisted execution/result history |
| `08-reporting-or-export.png` | report/export/chart surface |
| `09-anomaly-list.png` | anomaly list with deliberate failure context |
| `10-anomaly-status-action.png` | anomaly status action or status evidence |

## Failure Handling

If a command fails, the VM worker must:

1. Capture the command output in `vm-summary.md`.
2. Capture a screenshot when the failure is UI-visible.
3. Append a `fail` or `blocked` event with the smallest known unblock action.
4. Commit and push the evidence branch.

The coordinator must report the failure as evidence, not reinterpret it as a
pass from older screenshots.
