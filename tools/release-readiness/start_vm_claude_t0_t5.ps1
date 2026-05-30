param(
    [string]$RepoUrl = "https://github.com/meng004/MetBench-V2.1.4_2.git",
    [string]$Worktree = "C:\Users\codex\metbench-t0-t5-release-readiness",
    [string]$CoordinatorBranch = "codex/t0-t5-release-readiness",
    [string]$VmBranch = "claude/vm-t0-t5-release-readiness",
    [string]$RunId = "t0-t5-2026-05-30",
    [switch]$Background
)

$ErrorActionPreference = "Stop"

function Invoke-Step {
    param(
        [string]$Description,
        [scriptblock]$Script
    )

    Write-Host "==> $Description"
    & $Script
}

Invoke-Step "Prepare worktree" {
    $parent = Split-Path -Parent $Worktree
    New-Item -ItemType Directory -Force -Path $parent | Out-Null

    if (-not (Test-Path (Join-Path $Worktree ".git"))) {
        git clone $RepoUrl $Worktree
    }
}

Set-Location $Worktree

Invoke-Step "Checkout coordinator package" {
    git fetch origin
    git checkout -B $VmBranch "origin/$CoordinatorBranch"
    git config user.name "Codex VM"
    git config user.email "codex-vm@example.invalid"
}

$evidenceDir = Join-Path $Worktree "docs\superpowers\specs\2026-05-30-t0-t5-vm-release-smoke"
New-Item -ItemType Directory -Force -Path $evidenceDir | Out-Null

$launcher = Join-Path $evidenceDir "run-claude-vm.ps1"
$logPath = Join-Path $evidenceDir "claude-vm-run.log"
$promptPath = "docs\superpowers\vm-prompts\2026-05-30-t0-t5-release-readiness-vm-prompt.md"

Set-Content -Path $launcher -Encoding utf8 -Value @"
`$ErrorActionPreference = "Stop"
Set-Location "$Worktree"
Get-Content "$promptPath" -Raw | claude -p --permission-mode auto --output-format text *> "$logPath"
"@

Invoke-Step "Record VM bootstrap status" {
    powershell -ExecutionPolicy Bypass -File .\tools\release-readiness\vm_status_hook.ps1 `
        -RunId $RunId `
        -Step "vm-claude-bootstrap" `
        -Status "running" `
        -Message "starting Claude Code CLI from VM bootstrap script" `
        -EvidenceDir "docs\superpowers\specs\2026-05-30-t0-t5-vm-release-smoke" `
        -Push
}

if ($Background) {
    Start-Process powershell -ArgumentList @(
        "-NoProfile",
        "-ExecutionPolicy", "Bypass",
        "-File", $launcher
    ) -WorkingDirectory $Worktree
    Write-Host "Started Claude Code CLI in background. Log: $logPath"
}
else {
    powershell -NoProfile -ExecutionPolicy Bypass -File $launcher
}
