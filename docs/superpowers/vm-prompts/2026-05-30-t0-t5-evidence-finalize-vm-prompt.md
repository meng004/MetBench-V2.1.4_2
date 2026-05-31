# Claude Code VM Prompt: Finalize T0-T5 Evidence Matrix

You are Claude Code running inside the Windows Parallels VM. Continue the
existing run in:

```text
C:\Users\codex\metbench-t0-t5-release-readiness
```

Do not rerun the full suite unless a log is missing. Your job is to finish the
evidence package that already exists under:

```text
docs\superpowers\specs\2026-05-30-t0-t5-vm-release-smoke
```

## Current Known Evidence

- `vm-status.jsonl` already contains:
  - setup `pass`
  - build-and-T0-T1-T3-smoke `pass`
  - command-checks `pass`
- `command-evidence.log` exists.
- `full-suite.log` exists.
- UIA smokeshot images exist.
- Required mapped images `02-system-mt-run-or-catalog.png` through
  `09-anomaly-list.png` may already exist.

## Required Finish

1. Confirm these files exist:
   - `command-evidence.log`
   - `full-suite.log`
   - `02-system-mt-run-or-catalog.png`
   - `03-mr-catalog.png`
   - `04-sut-catalog.png`
   - `05-equation-catalog.png`
   - `06-samplecase-catalog.png`
   - `07-execution-history.png`
   - `08-reporting-or-export.png`
   - `09-anomaly-list.png`

2. Create missing required screenshots:
   - `01-build-output.png`: if no better command-window screenshot can be
     captured, create it by showing `command-evidence.log` or `full-suite.log`
     on screen and taking a screenshot. Do not use a blank placeholder.
   - `10-anomaly-status-action.png`: if the anomaly status action cannot be
     captured separately, use the best anomaly/status UI evidence available and
     document the limitation in `vm-summary.md`.

3. Update `README.md` Screenshot Evidence Matrix:
   - Set each of the 21 rows to `PASS` only when its artifact exists and is not
     empty.
   - Use `BLOCKED` only for a row that still lacks screenshot evidence, and
     explain exactly why in `vm-summary.md`.

4. Rewrite `vm-summary.md` so it reflects the latest run, not the earlier setup
   blocker. Include:
   - command-check result: 22/22 filtered commands passed; 255 filtered tests;
     full suite 1558 passed / 0 failed / 12 env-gated skips.
   - screenshot matrix completeness.
   - final VM decision: `PASS` only if all 21 rows are `PASS`; otherwise
     `BLOCKED`.

5. Append a final hook event:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\release-readiness\vm_status_hook.ps1 `
  -RunId "t0-t5-2026-05-30" `
  -Step "final" `
  -Status "pass" `
  -Message "short final status" `
  -EvidenceDir "docs\superpowers\specs\2026-05-30-t0-t5-vm-release-smoke" `
  -LogPath "docs\superpowers\specs\2026-05-30-t0-t5-vm-release-smoke\vm-summary.md" `
  -Push
```

Use `-Status "pass"` when the 21-row matrix is complete. Change it to
`-Status "blocked"` if any screenshot row remains blocked. The hook will commit
and push evidence. If the hook commit
finds nothing to commit, commit/push the evidence manually with:

```powershell
git add docs\superpowers\specs\2026-05-30-t0-t5-vm-release-smoke
git commit -m "docs(vm): finalize t0-t5 release smoke evidence"
git push -u origin claude/vm-t0-t5-release-readiness
```

Do not modify production code.
