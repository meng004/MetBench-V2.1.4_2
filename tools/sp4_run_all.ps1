# SP4 batch runner: per-MR WPF async-execution-page UI evidence.
# For each MR id, runs tools/uia-acceptance in --mr mode (launch → nav async page →
# select MR → Submit → poll AsyncState to terminal → 4 screenshots), with mandatory
# pre/post MetBench_Client kill so LiteDB is never locked. Records terminal state +
# exit code per MR to sp4-results.csv.
#
# Usage: powershell -File tools/sp4_run_all.ps1 [-TimeoutSeconds 150] [-OnlyHost]
param([int]$TimeoutSeconds = 150, [switch]$OnlyHost)
$ErrorActionPreference = "Continue"
$root = "D:\Codes\MetBench-V2.1.4_2"
$tool = Join-Path $root "tools\uia-acceptance\bin\Release\net8.0-windows\UiaAcceptance.exe"
$exe  = Join-Path $root "MetBench_Client\bin\Release\net8.0-windows7.0\MetBench_Client.exe"
$ev   = Join-Path $root "docs\superpowers\specs\2026-06-16-sp4-async-ui-evidence"
New-Item -ItemType Directory -Force -Path $ev | Out-Null
$env:METBENCH_SYSTEM_PYTHON = "C:\Users\lemon\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe"

# 6 openmoc/openmc MRs need their venvs (container only); host preflight fails them.
$container = @('openmc-pincell-nu-sigma-f','openmc-pincell-particle-count-convergence','openmc-pincell-sigma-a',
               'openmoc-pincell-nu-sigma-f','openmoc-pincell-ray-track-convergence','openmoc-pincell-sigma-a')
$hostMrs = @(
 'advection-amplitude-linearity','advection-mesh-conservation','bateman-mass-conservation','bateman-timestep-cauchy',
 'burgers-amplitude-peak-monotone','burgers-mesh-conservation','csv-roundtrip-identity','damped-oscillator-scale-state',
 'decay-chain-scale-initial','diffusion-mesh-richardson','diffusion-source-linearity','fourier-alpha-monotonic',
 'fourier-timestep-convergence','heat-equation-amplitude','lotka-volterra-scale-gamma',
 'p3-trajectory-sensitivity','p4-energy-invariant','p5-power-response','p8-norm-conservation','p9-k-eff-noise-aware',
 'poisson-mesh-richardson','poisson-source-superposition','projectile-scale-v0',
 'scipy-bvp-poisson-seed-mesh-insensitivity','scipy-bvp-poisson-source-superposition',
 'scipy-ivp-lv-prey-growth-monotone','scipy-ivp-lv-step-convergence',
 'subchannel-flow-temperature-monotone','subchannel-friction-invariance','subchannel-heat-flux-linearity',
 'wave-amplitude-linearity','wave-mesh-energy-convergence')
$mrs = if ($OnlyHost) { $hostMrs } else { $hostMrs + $container }

$csv = Join-Path $ev "sp4-results.csv"
"mr,exitcode,terminal,class" | Out-File -FilePath $csv -Encoding utf8
function Stop-Client { Get-Process MetBench_Client -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue; Start-Sleep -Milliseconds 700 }

$i = 0
foreach ($mr in $mrs) {
  $i++
  $cls = if ($container -contains $mr) { "container-only" } else { "host" }
  Write-Output "[$i/$($mrs.Count)] $mr ($cls)"
  Stop-Client
  $out = & $tool --exe $exe --mr $mr --case $mr --evidence $ev --timeout-seconds $TimeoutSeconds 2>&1
  $code = $LASTEXITCODE
  $line = $out | Select-String "Terminal state reached:|final state=" | Select-Object -Last 1
  $term = "unknown"
  if ($line) { if ($line.Line -match '(Succeeded|Failed|Cancelled)') { $term = $Matches[1] } }
  "$mr,$code,$term,$cls" | Out-File -FilePath $csv -Append -Encoding utf8
  Stop-Client
}
Write-Output "SP4 batch done -> $csv"
