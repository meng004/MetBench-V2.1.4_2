$ErrorActionPreference = "Stop"
Set-Location "C:\Users\codex\metbench-t0-t5-release-readiness"
Get-Content "docs\superpowers\vm-prompts\2026-05-30-t0-t5-release-readiness-vm-prompt.md" -Raw | claude -p --permission-mode auto --output-format text *> "C:\Users\codex\metbench-t0-t5-release-readiness\docs\superpowers\specs\2026-05-30-t0-t5-vm-release-smoke\claude-vm-run.log"
