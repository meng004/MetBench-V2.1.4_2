# SP3b UI acceptance runner.
# Wraps tools/uia-acceptance (FlaUI) with mandatory pre/post process cleanup so a
# lingering MetBench_Client.exe never holds the LiteDB files (MR/SystemMT/SystemMtJobs)
# and crashes the next launch. UIA patterns only inside the tool; no input injection.
#
# Usage:
#   powershell -File tools/sp3b_run.ps1 -Label caseA7 -Steps "nav:Nav_MetaPatterns;sleep:2500;dumppage:tree;shot:01-page;assertname:MetaPatterns"
param(
  [Parameter(Mandatory = $true)][string]$Label,
  [Parameter(Mandatory = $true)][string]$Steps,
  [int]$TimeoutSeconds = 600
)
$ErrorActionPreference = "Continue"
$root = "D:\Codes\MetBench-V2.1.4_2"
$tool = Join-Path $root "tools\uia-acceptance\bin\Release\net8.0-windows\UiaAcceptance.exe"
$exe  = Join-Path $root "MetBench_Client\bin\Release\net8.0-windows7.0\MetBench_Client.exe"
$ev   = Join-Path $root "docs\superpowers\specs\2026-06-15-sp3b-uat-wpf-ui-evidence"
New-Item -ItemType Directory -Force -Path $ev | Out-Null

function Stop-Client {
  Get-Process MetBench_Client -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
  Start-Sleep -Milliseconds 700
}

Stop-Client
& $tool --exe $exe --evidence $ev --label $Label --steps $Steps --timeout-seconds $TimeoutSeconds 2>&1 | ForEach-Object { "$_" }
$code = $LASTEXITCODE
Stop-Client
Write-Output "=== $Label EXIT: $code ==="
exit $code
