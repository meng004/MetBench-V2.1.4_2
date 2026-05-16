# run_full_smoke.ps1 — orchestrate the 10-step + MetaPatterns smoke flow on Windows.
#
# Per docs/superpowers/plans/2026-05-15-v2.1-followup-pipeline.md §PR-VM-5:
# launches MetBench_Client.exe, drives it via smokeshot UIA, writes screenshots
# under -OutDir, and emits a Markdown summary listing what passed / failed / skipped.
#
# Usage:
#   .\tools\smokeshot\run_full_smoke.ps1
#   .\tools\smokeshot\run_full_smoke.ps1 -OutDir C:\smoke-out
#   .\tools\smokeshot\run_full_smoke.ps1 -KeepRunning   # don't kill the app when done

[CmdletBinding()]
param(
    [string]$OutDir = "C:\Users\limeng\MetBench-V2.1.4_2\docs\screenshots\v2-ship-2026-05-14",
    [string]$ClientExe = "C:\Users\limeng\MetBench-V2.1.4_2\MetBench_Client\bin\Debug\net8.0-windows7.0\MetBench_Client.exe",
    [string]$Smokeshot = "C:\Users\limeng\MetBench-V2.1.4_2\tools\smokeshot\bin\Debug\net8.0-windows\smokeshot.exe",
    [switch]$KeepRunning,
    [int]$LaunchWaitSec = 8
)

$ErrorActionPreference = 'Continue'  # individual flow failure shouldn't abort the rest

if (-not (Test-Path $ClientExe))   { Write-Error "Client exe not found: $ClientExe — run 'dotnet build MetBench_Client.csproj' first."; exit 3 }
if (-not (Test-Path $Smokeshot))   { Write-Error "smokeshot.exe not found: $Smokeshot — run 'dotnet build tools/smokeshot' first."; exit 3 }
if (-not (Test-Path $OutDir))      { New-Item -ItemType Directory -Path $OutDir -Force | Out-Null }

$results = New-Object System.Collections.ArrayList
$sw      = [System.Diagnostics.Stopwatch]::StartNew()

function Run-Flow {
    param([string]$Label, [string[]]$FlowArgs)
    Write-Host ""
    Write-Host "=== $Label ===" -ForegroundColor Cyan
    $flowSw = [System.Diagnostics.Stopwatch]::StartNew()
    & $Smokeshot @FlowArgs
    $exit = $LASTEXITCODE
    $flowSw.Stop()
    $status = switch ($exit) {
        0 { '[OK]' }
        1 { '[FAIL]' }
        2 { '[SKIP]' }
        3 { '[APP-NOT-RUNNING]' }
        default { "[EXIT=$exit]" }
    }
    [void]$results.Add([pscustomobject]@{
        Label    = $Label
        FlowArgs = ($FlowArgs -join ' ')
        ExitCode = $exit
        Status   = $status
        Seconds  = [math]::Round($flowSw.Elapsed.TotalSeconds, 1)
    })
    Write-Host "  $status ($($flowSw.Elapsed.TotalSeconds)s)" -ForegroundColor $(if ($exit -eq 0) { 'Green' } elseif ($exit -eq 2) { 'Yellow' } else { 'Red' })
    return $exit
}

# ---------- 1. Launch app (if not already running) ----------
$existing = Get-Process -Name MetBench_Client -ErrorAction SilentlyContinue
$launched = $false
if ($existing) {
    Write-Host "MetBench_Client already running (PID $($existing[0].Id)); reusing." -ForegroundColor DarkGray
} else {
    Write-Host "Launching $ClientExe ..." -ForegroundColor DarkGray
    Start-Process -FilePath $ClientExe | Out-Null
    Start-Sleep -Seconds $LaunchWaitSec
    $launched = $true
}

# ---------- 2. Drive each flow ----------
# Existing PR #29 nav flow (steps 4-10 partial)
Run-Flow -Label 'nav-all (PR #29 steps 4-10 nav)' -FlowArgs @('nav-all','--out',$OutDir)

# Step 1-2 (form-fill is currently nav+capture; full add awaits AutomationProperties.Name on XAML)
Run-Flow -Label 'Step 1: Application Management add' -FlowArgs @('app-add','openmoc-smoke','--out',$OutDir)
Run-Flow -Label 'Step 2: MR Management add'          -FlowArgs @('mr-add','MR-smoke-test','--out',$OutDir)

# Step 3 — gated on OpenMOC venv
Run-Flow -Label 'Step 3: MT Execution (OpenMOC gated)' -FlowArgs @('mt-exec','--out',$OutDir)

# MetaPatterns CRUD (covers PR-VM-3 manual-smoke acceptance)
Run-Flow -Label 'MetaPatterns CRUD: list / page2 / toggle' -FlowArgs @('metapatterns','--out',$OutDir)

$sw.Stop()

# ---------- 3. Write Markdown summary ----------
$summaryPath = Join-Path $OutDir 'smoke-summary.md'
$lines = @()
$lines += '# Smoke Summary'
$lines += ''
$lines += "_Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss zzz')_"
$lines += "_Total: $([math]::Round($sw.Elapsed.TotalSeconds, 1))s_"
$lines += ''
$lines += '| # | Flow | Args | Status | Seconds |'
$lines += '|---|------|------|--------|---------|'
for ($i = 0; $i -lt $results.Count; $i++) {
    $r = $results[$i]
    $lines += "| $($i+1) | $($r.Label) | ``$($r.FlowArgs)`` | $($r.Status) | $($r.Seconds) |"
}
$lines += ''
$lines += '## Screenshots captured'
$lines += ''
Get-ChildItem $OutDir -Filter 'smoke-*.png' | Sort-Object Name | ForEach-Object {
    $lines += "- ``$($_.Name)`` — $([math]::Round($_.Length / 1024)) KB"
}
$lines += ''
$lines += '## Exit code legend'
$lines += '- `[OK]` 0   — flow completed all steps'
$lines += '- `[FAIL]` 1 — flow ran but a step failed or element not found'
$lines += '- `[SKIP]` 2 — flow intentionally skipped (e.g., OpenMOC venv missing for Step 3)'
$lines += '- `[APP-NOT-RUNNING]` 3 — MetBench_Client.exe not running'

Set-Content -Path $summaryPath -Value $lines -Encoding UTF8
Write-Host ""
Write-Host "Wrote summary: $summaryPath" -ForegroundColor Green

# ---------- 4. Stop the app we launched ----------
if ($launched -and -not $KeepRunning) {
    $proc = Get-Process -Name MetBench_Client -ErrorAction SilentlyContinue
    if ($proc) {
        Write-Host "Stopping MetBench_Client (we launched it)..." -ForegroundColor DarkGray
        Stop-Process -Id $proc[0].Id -Force
    }
}

$failures = ($results | Where-Object { $_.ExitCode -eq 1 -or $_.ExitCode -eq 3 }).Count
exit $failures
