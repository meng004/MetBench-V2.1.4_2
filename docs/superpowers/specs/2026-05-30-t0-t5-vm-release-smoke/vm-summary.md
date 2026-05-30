# T0-T5 VM Release Smoke Summary

| Field | Value |
|---|---|
| Run id | t0-t5-2026-05-30 |
| Branch | claude/vm-t0-t5-release-readiness |
| HEAD | 30cbbfebcacbe4c6b07229077e258ca76b213bc6 |
| Target production base | b9e917c15683c37466f23e2c4927aecc6cdff8b2 |
| Production-code delta from target base | YES (see Blockers — disputed: harness script, not product code) |
| Command checks | 0 / 22 (gate stopped run before command phase) |
| Screenshot matrix | incomplete (all rows PENDING) |
| Final VM decision | BLOCKED |

## Command Evidence

| Command | Result | Notes |
|---|---|---|
| (none) | not run | Setup gate fired before the command phase; see Blockers. |

## Screenshot Evidence

| File | Result | Notes |
|---|---|---|
| (none) | not captured | Setup gate fired before the screenshot phase; see Blockers. |

## Blockers

The Setup production-code delta check printed one path, so per the Hard Rule
("If the branch contains production-code changes relative to the target
production base, stop and push a `blocked` status event") and the Setup
instruction ("If the `git diff --name-only ...` command prints any path,
record those paths in `vm-summary.md`, run the hook with `blocked`, push, and
stop"), the run was stopped.

Path flagged by the delta command:

```
tools/release-readiness/start_vm_claude_t0_t5.ps1
```

### Assessment of the flagged path

- Added in coordinator commit `e43fc0e` ("docs: add T0-T5 VM Claude
  bootstrap"); diff stat `1 file changed, 71 insertions(+)` — a brand-new file,
  not a modification of existing product code.
- It is the **VM bootstrap orchestration script** referenced by the exchange
  protocol (`docs/superpowers/specs/2026-05-30-t0-t5-github-exchange-protocol.md`
  §VM Bootstrap). Its body only clones the repo, checks out the coordinator /
  VM branches, writes `run-claude-vm.ps1`, and starts the Claude Code CLI. It
  contains **no MetBench product code** (no `MetBench_BLL.Core`,
  `MetBench_Client`, `MetBench_DAL`, `MetBench_Domain`, etc.).
- It lives in the same `tools/release-readiness/` harness family as
  `tools/release-readiness/vm_status_hook.ps1`, which the Setup delta command
  **does** explicitly exclude.

### Conflict (recorded per CLAUDE.md §5)

The Hard Rule's intent ("production-code changes") does not cover this file —
it is coordinator harness, not product code. But the Setup delta command's
exclude list omits `tools/release-readiness/start_vm_claude_t0_t5.ps1` (the
bootstrap was added in `e43fc0e`, apparently after the exclude list was
authored), so the deterministic gate fired on it.

Path chosen: honor the explicit deterministic gate and report `BLOCKED` rather
than override the gate with a judgment call that this file "does not count."
Marked-for-cleanup alternative: the coordinator updates the exclude list.

### Smallest unblock action

Add the bootstrap script to the Setup delta command's exclude list (mirroring
the existing `vm_status_hook.ps1` exclusion) and re-launch the VM run:

```
":(exclude)tools/release-readiness/start_vm_claude_t0_t5.ps1"
```

Equivalently, the coordinator confirms this harness path is intended to be
present on the VM branch, after which the run can proceed to the command and
screenshot phases. No production-code change was found on the branch.
