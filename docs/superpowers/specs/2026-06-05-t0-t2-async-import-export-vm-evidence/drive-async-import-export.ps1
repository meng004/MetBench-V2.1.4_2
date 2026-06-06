#requires -version 5
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms
Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class AsyncImportExportWin32 {
  [DllImport("user32.dll")] public static extern bool SetCursorPos(int X, int Y);
  [DllImport("user32.dll")] public static extern void mouse_event(uint f,uint dx,uint dy,uint d,UIntPtr e);
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h,int n);
  [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
  public static void Click(int x,int y) {
    SetCursorPos(x,y); System.Threading.Thread.Sleep(80);
    mouse_event(0x0002,0,0,0,UIntPtr.Zero); System.Threading.Thread.Sleep(60);
    mouse_event(0x0004,0,0,0,UIntPtr.Zero);
  }
}
"@
[void][AsyncImportExportWin32]::SetProcessDPIAware()

$ErrorActionPreference = 'Stop'
$AE = [System.Windows.Automation.AutomationElement]
$TS = [System.Windows.Automation.TreeScope]
$CT = [System.Windows.Automation.ControlType]
$shotDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repo = Resolve-Path (Join-Path $shotDir '..\..\..\..')
$buildLog = Join-Path $shotDir 'build-output.txt'
$summary = Join-Path $shotDir 'vm-summary.md'
$script:win = $null
$script:scale = 1.0
$script:proc = $null
$terminalStates = @('Succeeded', 'Failed', 'TimedOut', 'ArtifactMissing', 'Cancelled')

function Prop($id, $value) {
    New-Object System.Windows.Automation.PropertyCondition($id, $value)
}

function Find-ById($root, $id, $timeoutSec = 8) {
    $cond = Prop ([System.Windows.Automation.AutomationElement]::AutomationIdProperty) $id
    $deadline = (Get-Date).AddSeconds($timeoutSec)
    do {
        $el = $root.FindFirst($TS::Descendants, $cond)
        if ($el) { return $el }
        Start-Sleep -Milliseconds 200
    } while ((Get-Date) -lt $deadline)
    return $null
}

function Find-ByName($root, $name, $timeoutSec = 8) {
    $cond = Prop ([System.Windows.Automation.AutomationElement]::NameProperty) $name
    $deadline = (Get-Date).AddSeconds($timeoutSec)
    do {
        $el = $root.FindFirst($TS::Descendants, $cond)
        if ($el) { return $el }
        Start-Sleep -Milliseconds 200
    } while ((Get-Date) -lt $deadline)
    return $null
}

function Bring-Front {
    if ($script:win) {
        [void][AsyncImportExportWin32]::ShowWindow([IntPtr]$script:win.Current.NativeWindowHandle, 3)
        [void][AsyncImportExportWin32]::SetForegroundWindow([IntPtr]$script:win.Current.NativeWindowHandle)
        Start-Sleep -Milliseconds 250
    }
}

function Click-Element($el) {
    if (-not $el) { return $false }
    $ip = $null
    if ($el.TryGetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern, [ref]$ip)) {
        $ip.Invoke()
        return $true
    }
    $sp = $null
    if ($el.TryGetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern, [ref]$sp)) {
        $sp.Select()
        return $true
    }
    Bring-Front
    $r = $el.Current.BoundingRectangle
    if ($r.Width -gt 0 -and $r.Height -gt 0) {
        [AsyncImportExportWin32]::Click([int](($r.X + $r.Width / 2) / $script:scale), [int](($r.Y + $r.Height / 2) / $script:scale))
        return $true
    }
    return $false
}

function Click-SelectableAncestor($el) {
    $node = $el
    $walker = [System.Windows.Automation.TreeWalker]::ControlViewWalker
    for ($i = 0; $i -lt 8 -and $node; $i++) {
        if (Click-Element $node) { return $true }
        $node = $walker.GetParent($node)
    }
    return $false
}

function Set-NavSearch($text) {
    $editCond = Prop ([System.Windows.Automation.AutomationElement]::ControlTypeProperty) $CT::Edit
    $edits = $script:win.FindAll($TS::Descendants, $editCond)
    foreach ($edit in $edits) {
        $vp = $null
        if ($edit.TryGetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern, [ref]$vp)) {
            try {
                if (-not $vp.Current.IsReadOnly) {
                    $vp.SetValue($text)
                    Start-Sleep -Milliseconds 500
                    return $true
                }
            } catch {}
        }
    }
    return $false
}

function Set-TextById($id, $value) {
    $el = Find-ById $script:win $id 8
    if (-not $el) { throw "Element not found: $id" }
    $vp = $null
    if (-not $el.TryGetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern, [ref]$vp)) {
        throw "Element has no ValuePattern: $id"
    }
    $vp.SetValue($value)
    Start-Sleep -Milliseconds 150
}

function Get-TextById($id) {
    $el = Find-ById $script:win $id 2
    if (-not $el) { return '' }
    $vp = $null
    if ($el.TryGetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern, [ref]$vp)) {
        return $vp.Current.Value
    }
    return $el.Current.Name
}

function Shot($file) {
    Bring-Front
    $r = $script:win.Current.BoundingRectangle
    $x = [int]($r.X / $script:scale)
    $y = [int]($r.Y / $script:scale)
    $w = [int]($r.Width / $script:scale)
    $h = [int]($r.Height / $script:scale)
    $bmp = New-Object System.Drawing.Bitmap($w, $h)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen($x, $y, 0, 0, $bmp.Size)
    $bmp.Save((Join-Path $shotDir $file), [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose()
    $bmp.Dispose()
    Write-Host "[shot] $file"
}

function Select-ComboItem($comboId, $name) {
    $combo = Find-ById $script:win $comboId 10
    if (-not $combo) { throw "Combo not found: $comboId" }
    Bring-Front
    $ecp = $null
    if ($combo.TryGetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern, [ref]$ecp)) {
        $ecp.Expand()
        Start-Sleep -Milliseconds 450
    }
    $item = Find-ByName $script:win $name 10
    if (-not $item) { throw "Combo item not found: $name" }
    if (-not (Click-SelectableAncestor $item)) { throw "Could not select combo item: $name" }
    try { if ($ecp) { $ecp.Collapse() } } catch {}
    Start-Sleep -Milliseconds 350
}

function Start-MetBench($exe) {
    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = $exe
    $psi.WorkingDirectory = Split-Path $exe -Parent
    $psi.UseShellExecute = $false
    return [System.Diagnostics.Process]::Start($psi)
}

function Attach-MainWindow($proc) {
    $script:win = $null
    Start-Sleep -Seconds 7
    $winCond = Prop ([System.Windows.Automation.AutomationElement]::ProcessIdProperty) $proc.Id
    for ($i = 0; $i -lt 30 -and -not $script:win; $i++) {
        $script:win = $AE::RootElement.FindFirst($TS::Children, $winCond)
        if (-not $script:win) { Start-Sleep -Milliseconds 500 }
    }
    if (-not $script:win) { throw "Main window not found." }
    Bring-Front
    $physW = $AE::RootElement.Current.BoundingRectangle.Width
    $logW = [System.Windows.Forms.SystemInformation]::VirtualScreen.Width
    $script:scale = if ($logW -gt 0) { $physW / $logW } else { 1.0 }
}

function Open-AsyncPage {
    [void](Set-NavSearch 'Async')
    $nav = Find-ByName $script:win 'System MT Async Execution' 12
    if (-not $nav) { throw "Async navigation item not found." }
    if (-not (Click-SelectableAncestor $nav)) { throw "Async navigation item could not be clicked." }
    if (-not (Find-ById $script:win 'AsyncOperationCombo' 12)) {
        Shot 'diagnostic-after-async-nav.png'
        throw "Async page not reached."
    }
}

function Assert-VisibleById($id) {
    $el = Find-ById $script:win $id 2
    if (-not $el) { throw "Expected visible element not found: $id" }
    if ($el.Current.IsOffscreen) { throw "Expected visible element was offscreen: $id" }
}

function Assert-HiddenById($id) {
    $el = Find-ById $script:win $id 1
    if ($el -and -not $el.Current.IsOffscreen) {
        throw "Expected hidden element was visible: $id"
    }
}

function Capture-OperationVisibility($operation, $shot, $visibleIds, $hiddenIds) {
    Select-ComboItem 'AsyncOperationCombo' $operation
    foreach ($id in $visibleIds) {
        Assert-VisibleById $id
    }
    foreach ($id in $hiddenIds) {
        Assert-HiddenById $id
    }
    Shot $shot
}

function Wait-NewJob($oldJob, $timeoutSec = 8) {
    $deadline = (Get-Date).AddSeconds($timeoutSec)
    do {
        $job = Get-TextById 'AsyncJobId'
        if ($job -and $job -ne '-' -and $job -ne $oldJob) { return $job }
        Start-Sleep -Milliseconds 200
    } while ((Get-Date) -lt $deadline)
    throw "Timed out waiting for a new async job id."
}

function Wait-Terminal($timeoutSec = 90) {
    $deadline = (Get-Date).AddSeconds($timeoutSec)
    do {
        $state = Get-TextById 'AsyncState'
        if ($terminalStates -contains $state) { return $state }
        Start-Sleep -Milliseconds 500
    } while ((Get-Date) -lt $deadline)
    throw "Timed out waiting for terminal async job state."
}

function Submit-And-Wait($operation, $terminalShot, $timeoutSec = 90) {
    $oldJob = Get-TextById 'AsyncJobId'
    [void](Click-Element (Find-ById $script:win 'AsyncSubmitButton' 5))
    $job = Wait-NewJob $oldJob
    Start-Sleep -Milliseconds 500
    $state = Wait-Terminal $timeoutSec
    Shot $terminalShot
    $artifact = Get-TextById 'AsyncArtifactPath'
    $result = Get-TextById 'AsyncResultSummary'
    return [pscustomobject]@{
        Operation = $operation
        JobId = $job
        State = $state
        ArtifactPath = $artifact
        Result = $result
    }
}

function Write-P4ImportPackage($packageRoot) {
    New-Item -ItemType Directory -Force -Path $packageRoot | Out-Null
    $json = @'
{
  "SchemaVersion": "put-import.v1",
  "Sut": {
    "SutId": "P4",
    "Name": "Hamiltonian pendulum",
    "EquationFamily": "Hamiltonian ODE / symplectic integration",
    "ProgramKind": "NumericalSolver",
    "Adapter": {
      "AdapterId": "minimum-mr-subset-p4",
      "Description": "Imported staging descriptor for p4_pendulum.py Hamiltonian pendulum fixture.",
      "SourcePath": "experiments/puts/p4_pendulum.py",
      "EntryPoints": [ "run_canonical" ]
    },
    "Observables": [
      { "Name": "q", "Kind": "Series", "Unit": "rad", "Description": "Pendulum generalized coordinate" },
      { "Name": "p", "Kind": "Series", "Unit": "rad/s", "Description": "Pendulum generalized momentum" },
      { "Name": "energy", "Kind": "Series", "Unit": "relative", "Description": "Hamiltonian energy trajectory" }
    ],
    "Metadata": { "priority": "A" }
  },
  "Mrs": [
    {
      "MrId": "p4-energy-invariant",
      "SutId": "P4",
      "Name": "Hamiltonian energy-invariant imported MR",
      "ObservableRefs": [ "q", "p", "energy" ],
      "TransformBinding": {
        "Status": "ImportedOnly",
        "TransformKind": "ImportedOnly",
        "Shape": null,
        "Notes": "minimum-mr-subset fixture has no MetBench runtime transform binding yet."
      },
      "AssertionBinding": {
        "Status": "ImportedOnly",
        "PredicateKind": "ImportedOnly",
        "Metric": "",
        "Tolerance": null,
        "Notes": "minimum-mr-subset fixture has no MetBench typed assertion binding yet."
      },
      "Metadata": { "source": "tests/puts/test_smoke.py" }
    }
  ],
  "IoGroups": [
    {
      "IoGroupId": "p4-canonical",
      "SutId": "P4",
      "Name": "P4 canonical imported smoke IO",
      "InputRefs": [ "canonical-input" ],
      "OutputRefs": [ "canonical-output" ],
      "Metadata": { "source": "run_canonical" }
    }
  ],
  "Mutations": [
    {
      "MutationId": "p4-mut-integrator-drift",
      "SutId": "P4",
      "Name": "P4 integrator drift perturbation class",
      "OperatorClass": "integrator-drift",
      "RepresentationKind": "OperatorClassOnly",
      "Metadata": { "representation": "research-imported" }
    }
  ],
  "Detections": [
    {
      "DetectionId": "p4-detection-001",
      "MrId": "p4-energy-invariant",
      "MutationId": "p4-mut-integrator-drift",
      "IoGroupId": "p4-canonical",
      "Result": "Detected",
      "EvidenceKind": "ImportedResearchEvidence",
      "Notes": "Detection row imported from minimum-mr-subset research fixture metadata; not re-executed by MetBench runtime."
    }
  ],
  "Provenance": {
    "RepositoryUrl": "https://github.com/meng004/Minimum-MR-SubSet.git",
    "Commit": "b931b5f74d0f3f3cb704cd6fedbcb4f523cccd7f",
    "SourcePaths": [ "experiments/puts/p4_pendulum.py", "tests/puts/test_smoke.py" ],
    "CapturedBy": "codex-a-group-put-import",
    "CapturedAtUtc": "2026-06-02T00:00:00+00:00"
  },
  "Compatibility": {
    "OverallReadiness": "ImportedOnly",
    "Findings": [ "Imported fixture is staged only until transform and assertion bindings are reviewed." ]
  }
}
'@
    Set-Content -Path (Join-Path $packageRoot 'sut-import-unit.json') -Value $json -Encoding UTF8
}

function Extract-ExecutionId($resultSummary) {
    $match = [regex]::Match($resultSummary, 'Record:\s*([0-9a-fA-F-]{36})')
    if (-not $match.Success) { throw "Could not parse execution id from result summary: $resultSummary" }
    return $match.Groups[1].Value
}

function Assert-PathExists($path, $label) {
    if (-not (Test-Path -LiteralPath $path)) {
        throw "$label does not exist: $path"
    }
}

Push-Location $repo
try {
    $stale = @(
        '01-page-operation-selector-visible.png',
        '02-runmr-queued-or-running.png',
        '03-runmr-succeeded.png',
        '04-runbatch-terminal.png',
        '05-import-assets-terminal-artifact.png',
        '06-export-assets-terminal-artifact.png',
        '07-export-execution-artifacts-terminal-artifact.png',
        '08-failure-display.png',
        '09-visibility-runmr.png',
        '10-visibility-runbatch.png',
        '11-visibility-import-assets.png',
        '12-visibility-export-assets.png',
        '13-visibility-export-execution-artifacts.png',
        'build-output.txt',
        'vm-summary.md')
    foreach ($name in $stale) {
        $path = Join-Path $shotDir $name
        if (Test-Path $path) { Remove-Item $path -Force }
    }

    dotnet build MetBench_Client\MetBench_Client.csproj --no-restore -v:minimal *> $buildLog
    $buildExit = $LASTEXITCODE
    $buildErrors = (Select-String -Path $buildLog -Pattern ': error ').Count
    if ($buildExit -ne 0 -or $buildErrors -gt 0) { throw "Build failed. See $buildLog" }

    $clientOut = Join-Path $repo 'MetBench_Client\bin\Debug\net8.0-windows7.0'
    $exe = Join-Path $clientOut 'MetBench_Client.exe'
    $script:proc = Start-MetBench $exe
    Attach-MainWindow $script:proc
    Open-AsyncPage
    Shot '01-page-operation-selector-visible.png'

    Capture-OperationVisibility `
        'RunMr' `
        '09-visibility-runmr.png' `
        @('AsyncMrCombo') `
        @('AsyncBatchMrIdsBox', 'AsyncPackageRootBox', 'AsyncStagingRootBox', 'AsyncExportRootBox', 'AsyncExecutionIdBox')

    Capture-OperationVisibility `
        'RunBatch' `
        '10-visibility-runbatch.png' `
        @('AsyncBatchMrIdsBox') `
        @('AsyncMrCombo', 'AsyncPackageRootBox', 'AsyncStagingRootBox', 'AsyncExportRootBox', 'AsyncExecutionIdBox')

    Capture-OperationVisibility `
        'ImportAssets' `
        '11-visibility-import-assets.png' `
        @('AsyncPackageRootBox', 'AsyncStagingRootBox') `
        @('AsyncMrCombo', 'AsyncBatchMrIdsBox', 'AsyncExportRootBox', 'AsyncExecutionIdBox')

    Capture-OperationVisibility `
        'ExportAssets' `
        '12-visibility-export-assets.png' `
        @('AsyncPackageRootBox', 'AsyncExportRootBox') `
        @('AsyncMrCombo', 'AsyncBatchMrIdsBox', 'AsyncStagingRootBox', 'AsyncExecutionIdBox')

    Capture-OperationVisibility `
        'ExportExecutionArtifacts' `
        '13-visibility-export-execution-artifacts.png' `
        @('AsyncExportRootBox', 'AsyncExecutionIdBox') `
        @('AsyncMrCombo', 'AsyncBatchMrIdsBox', 'AsyncPackageRootBox', 'AsyncStagingRootBox')

    Select-ComboItem 'AsyncOperationCombo' 'RunMr'
    Select-ComboItem 'AsyncMrCombo' 'advection-amplitude-linearity'
    $oldJob = Get-TextById 'AsyncJobId'
    [void](Click-Element (Find-ById $script:win 'AsyncSubmitButton' 5))
    $runMrJob = Wait-NewJob $oldJob
    Start-Sleep -Milliseconds 500
    Shot '02-runmr-queued-or-running.png'
    $runMrState = Wait-Terminal 90
    Shot '03-runmr-succeeded.png'
    $runMrResult = Get-TextById 'AsyncResultSummary'
    if ($runMrState -ne 'Succeeded') { throw "RunMr terminal state was $runMrState." }
    $executionId = Extract-ExecutionId $runMrResult
    $runMr = [pscustomobject]@{
        Operation = 'RunMr'
        JobId = $runMrJob
        State = $runMrState
        ArtifactPath = Get-TextById 'AsyncArtifactPath'
        Result = $runMrResult
    }

    Select-ComboItem 'AsyncOperationCombo' 'RunBatch'
    Set-TextById 'AsyncBatchMrIdsBox' 'advection-amplitude-linearity, heat-equation-amplitude'
    $runBatch = Submit-And-Wait 'RunBatch' '04-runbatch-terminal.png' 120
    if ($runBatch.State -ne 'Succeeded') { throw "RunBatch terminal state was $($runBatch.State)." }

    $workRoot = Join-Path $shotDir 'operation-artifacts'
    $packageRoot = Join-Path $workRoot 'p4-package'
    $stagingRoot = Join-Path $workRoot 'p4-staging'
    $assetExportRoot = Join-Path $workRoot 'p4-export'
    $executionExportRoot = Join-Path $workRoot 'execution-export'
    $missingExecutionRoot = Join-Path $workRoot 'missing-execution-export'
    Write-P4ImportPackage $packageRoot

    Select-ComboItem 'AsyncOperationCombo' 'ImportAssets'
    Set-TextById 'AsyncPackageRootBox' $packageRoot
    Set-TextById 'AsyncStagingRootBox' $stagingRoot
    $importAssets = Submit-And-Wait 'ImportAssets' '05-import-assets-terminal-artifact.png' 60
    if ($importAssets.State -ne 'Succeeded') { throw "ImportAssets terminal state was $($importAssets.State)." }
    Assert-PathExists $importAssets.ArtifactPath 'Import artifact'

    Select-ComboItem 'AsyncOperationCombo' 'ExportAssets'
    Set-TextById 'AsyncPackageRootBox' $packageRoot
    Set-TextById 'AsyncExportRootBox' $assetExportRoot
    $exportAssets = Submit-And-Wait 'ExportAssets' '06-export-assets-terminal-artifact.png' 60
    if ($exportAssets.State -ne 'Succeeded') { throw "ExportAssets terminal state was $($exportAssets.State)." }
    Assert-PathExists $exportAssets.ArtifactPath 'Asset export artifact'

    Select-ComboItem 'AsyncOperationCombo' 'ExportExecutionArtifacts'
    Set-TextById 'AsyncExecutionIdBox' $executionId
    Set-TextById 'AsyncExportRootBox' $executionExportRoot
    $exportExecution = Submit-And-Wait 'ExportExecutionArtifacts' '07-export-execution-artifacts-terminal-artifact.png' 60
    if ($exportExecution.State -ne 'Succeeded') { throw "ExportExecutionArtifacts terminal state was $($exportExecution.State)." }
    Assert-PathExists (Join-Path $executionExportRoot 'manifest.json') 'Execution export manifest'
    Assert-PathExists (Join-Path $executionExportRoot 'execution-result.json') 'Execution export result'
    Assert-PathExists (Join-Path $executionExportRoot 'report.html') 'Execution export html report'

    Select-ComboItem 'AsyncOperationCombo' 'ExportExecutionArtifacts'
    Set-TextById 'AsyncExecutionIdBox' '11111111-1111-1111-1111-111111111111'
    Set-TextById 'AsyncExportRootBox' $missingExecutionRoot
    $failure = Submit-And-Wait 'ExportExecutionArtifacts' '08-failure-display.png' 60
    if ($failure.State -ne 'Failed') { throw "Expected failure display job to fail, got $($failure.State)." }
    if ([string]::IsNullOrWhiteSpace((Get-TextById 'AsyncFailureReason'))) {
        throw "Failure display did not expose AsyncFailureReason text."
    }

    $shots = @(
        '01-page-operation-selector-visible.png',
        '02-runmr-queued-or-running.png',
        '03-runmr-succeeded.png',
        '04-runbatch-terminal.png',
        '05-import-assets-terminal-artifact.png',
        '06-export-assets-terminal-artifact.png',
        '07-export-execution-artifacts-terminal-artifact.png',
        '08-failure-display.png',
        '09-visibility-runmr.png',
        '10-visibility-runbatch.png',
        '11-visibility-import-assets.png',
        '12-visibility-export-assets.png',
        '13-visibility-export-execution-artifacts.png')

    $lines = @(
        '# T0-T2 Async Import/Export PR4 WPF VM Summary',
        '',
        "branch=$(git rev-parse --abbrev-ref HEAD)",
        "head=$(git rev-parse HEAD)",
        "origin_main=$(git rev-parse origin/main)",
        '',
        '## Commands',
        '',
        '- `dotnet build MetBench_Client\MetBench_Client.csproj --no-restore -v:minimal`: exit 0; errors 0',
        '',
        '## WPF Jobs',
        '',
        "| Operation | JobId | State | ArtifactPath |",
        "|---|---|---|---|",
        "| $($runMr.Operation) | $($runMr.JobId) | $($runMr.State) | $($runMr.ArtifactPath) |",
        "| $($runBatch.Operation) | $($runBatch.JobId) | $($runBatch.State) | $($runBatch.ArtifactPath) |",
        "| $($importAssets.Operation) | $($importAssets.JobId) | $($importAssets.State) | $($importAssets.ArtifactPath) |",
        "| $($exportAssets.Operation) | $($exportAssets.JobId) | $($exportAssets.State) | $($exportAssets.ArtifactPath) |",
        "| $($exportExecution.Operation) | $($exportExecution.JobId) | $($exportExecution.State) | $($exportExecution.ArtifactPath) |",
        "| $($failure.Operation)-missing-execution | $($failure.JobId) | $($failure.State) | $($failure.ArtifactPath) |",
        '',
        "execution_id=$executionId",
        "execution_export_root=$executionExportRoot",
        "execution_export_manifest=$(Join-Path $executionExportRoot 'manifest.json')",
        "execution_export_result=$(Join-Path $executionExportRoot 'execution-result.json')",
        "execution_export_html=$(Join-Path $executionExportRoot 'report.html')",
        '',
        '## Screenshots',
        '')
    foreach ($shot in $shots) {
        $lines += '- `' + $shot + '`'
    }
    $lines += ''
    $lines += '## Blockers'
    $lines += ''
    $lines += 'None for PR4 WPF operation exposure. Do not mark the T0-T2 release-closure chain Controlled from this PR alone.'
    Set-Content -Path $summary -Value $lines -Encoding UTF8
}
catch {
    $message = $_ | Out-String
    Set-Content -Path (Join-Path $shotDir 'failure.txt') -Value $message -Encoding UTF8
    throw
}
finally {
    if ($script:proc) {
        try {
            $script:proc.Refresh()
            if (-not $script:proc.HasExited) {
                Stop-Process -Id $script:proc.Id -Force
            }
        } catch {}
    }
    Pop-Location
}
