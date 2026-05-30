param(
    [Parameter(Mandatory = $true)]
    [string]$RunId,

    [Parameter(Mandatory = $true)]
    [string]$Step,

    [Parameter(Mandatory = $true)]
    [ValidateSet("queued", "running", "pass", "fail", "skip", "blocked", "info")]
    [string]$Status,

    [Parameter(Mandatory = $true)]
    [string]$Message,

    [string]$EvidenceDir = "docs\superpowers\specs\2026-05-30-t0-t5-vm-release-smoke",

    [string]$LogPath = "",

    [switch]$Push
)

$ErrorActionPreference = "Stop"

function Get-GitValue {
    param([string[]]$GitArgs)
    try {
        $value = & git @GitArgs 2>$null
        if ($LASTEXITCODE -eq 0) {
            return ($value -join "`n").Trim()
        }
    }
    catch {
        return ""
    }
    return ""
}

$root = Get-GitValue -GitArgs @("rev-parse", "--show-toplevel")
if ([string]::IsNullOrWhiteSpace($root)) {
    throw "vm_status_hook.ps1 must run inside a git worktree."
}

$evidencePath = Join-Path $root $EvidenceDir
New-Item -ItemType Directory -Force -Path $evidencePath | Out-Null

$statusPath = Join-Path $evidencePath "vm-status.jsonl"
$branch = Get-GitValue -GitArgs @("branch", "--show-current")
$head = Get-GitValue -GitArgs @("rev-parse", "HEAD")

$screenshots = @()
if (Test-Path $evidencePath) {
    $screenshots = Get-ChildItem -Path $evidencePath -Filter "*.png" -File |
        Sort-Object Name |
        ForEach-Object { $_.Name }
}

$event = [ordered]@{
    timestamp_utc = (Get-Date).ToUniversalTime().ToString("o")
    run_id = $RunId
    branch = $branch
    head = $head
    step = $Step
    status = $Status
    message = $Message
    evidence_dir = $EvidenceDir
    screenshots = $screenshots
}

if (-not [string]::IsNullOrWhiteSpace($LogPath)) {
    $event.log_path = $LogPath
}

($event | ConvertTo-Json -Compress) | Add-Content -Path $statusPath -Encoding utf8

if ($Push) {
    git add $EvidenceDir | Out-Null
    $safeStep = ($Step -replace "[^A-Za-z0-9._-]", "-").Trim("-")
    if ([string]::IsNullOrWhiteSpace($safeStep)) {
        $safeStep = "step"
    }
    git commit -m "docs(vm): record $RunId $safeStep $Status" | Out-Null
    git push -u origin $branch | Out-Null
}
