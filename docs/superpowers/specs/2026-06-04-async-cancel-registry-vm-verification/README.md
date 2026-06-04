# Async Cancel True-Interrupt VM Evidence

Date: 2026-06-04

Branch: `codex/async-cancel-true-interrupt-repair-plan`

Validated repair commit: `4b0628617e50fe2b46d51d06229dba23268576e2`

Result: PASS on the repair branch.

## Probe

The VM probe used the WPF `System MT Async Execution` page and selected `advection-amplitude-linearity`.

This MR was accepted as a nonzero probe because focused launcher coverage passes for `LauncherEndToEndAdvectionTests`, and a VM preflight client run showed:

- value: `peak_amplitude`
- source: `0.75585863737664949`
- follow-up: `1.511717274753299`

The cancellation probe modified only the build-output copy:

`MetBench_Client/bin/Debug/net8.0-windows7.0/SUT/advection_1d/advection_1d.py`

The production `SUT/advection_1d/advection_1d.py` file was not edited.

## Evidence

- `test-summary.txt`: `result=PASS`, `ui_cancelled=True`, `before_pid_gone=True`, `after_pid_alive=False`.
- `process-before-cancel.txt`: PID `5860`, `process_name=python`, `alive=True`.
- `process-after-cancel.txt`: same PID `5860`, `alive=False`.
- `ui-state-after-cancel.txt`: `state=Cancelled`, `phase=cancelled`, `failure=cancellation requested`, empty `result`.
- `01-before-submit.png`: selected MR before submit.
- `04-running-before-cancel.png`: job running before Cancel.
- `05-cancelled-after-cancel.png`: UI cancelled state.
- `06-no-success-result-after-cancel.png`: no successful result after Cancel.

## Notes

Earlier failed probe attempts exposed two script issues, not product pass evidence:

- UIA selection did not switch from the default advection MR to heat-equation.
- The first advection PID probe inserted code before `from __future__ import annotations`, causing Python `SyntaxError` and a misleading failed result with source/follow-up `0`.

The final passing run used a corrected probe insertion after the `from __future__` line and removed stale `failure.txt` before execution.
