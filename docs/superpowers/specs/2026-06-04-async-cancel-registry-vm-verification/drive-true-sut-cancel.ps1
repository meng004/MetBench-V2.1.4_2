#requires -version 5
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms
Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class TrueSutCancelWin32 {
  [DllImport("user32.dll")] public static extern bool SetCursorPos(int X, int Y);
  [DllImport("user32.dll")] public static extern void mouse_event(uint f,uint dx,uint dy,UIntPtr e);
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h,int n);
  [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
  public static void Click(int x,int y) {
    SetCursorPos(x,y); System.Threading.Thread.Sleep(80);
    mouse_event(0x0002,0,0,UIntPtr.Zero); System.Threading.Thread.Sleep(60);
    mouse_event(0x0004,0,0,UIntPtr.Zero);
  }
}
"@
[void][TrueSutCancelWin32]::SetProcessDPIAware()

$ErrorActionPreference = 'Stop'
$AE = [System.Windows.Automation.AutomationElement]
$TS = [System.Windows.Automation.TreeScope]
$CT = [System.Windows.Automation.ControlType]
$shotDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repo = Resolve-Path (Join-Path $shotDir '..\..\..\..')
$buildLog = Join-Path $shotDir 'build-output.txt'
$summaryLog = Join-Path $shotDir 'test-summary.txt'
$stateLog = Join-Path $shotDir 'ui-state-after-cancel.txt'
$sutPidFile = Join-Path $shotDir 'sut-pid-before-cancel.txt'
$script:win = $null
$script:scale = 1.0
$script:proc = $null
$script:sutScript = $null
$script:sutBackup = $null

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
        [void][TrueSutCancelWin32]::ShowWindow([IntPtr]$script:win.Current.NativeWindowHandle, 3)
        [void][TrueSutCancelWin32]::SetForegroundWindow([IntPtr]$script:win.Current.NativeWindowHandle)
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
        [TrueSutCancelWin32]::Click([int](($r.X + $r.Width / 2) / $script:scale), [int](($r.Y + $r.Height / 2) / $script:scale))
        return $true
    }
    return $false
}

function Force-Click-ById($id) {
    $el = Find-ById $script:win $id 5
    if (-not $el) { return $false }
    Bring-Front
    $r = $el.Current.BoundingRectangle
    if ($r.Width -le 0 -or $r.Height -le 0) { return $false }
    [TrueSutCancelWin32]::Click([int](($r.X + $r.Width / 2) / $script:scale), [int](($r.Y + $r.Height / 2) / $script:scale))
    return $true
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

function Get-TextById($id) {
    $el = Find-ById $script:win $id 2
    if (-not $el) { return '' }
    return $el.Current.Name
}

function Get-ComboSelection($combo) {
    $sel = $null
    if ($combo.TryGetCurrentPattern([System.Windows.Automation.SelectionPattern]::Pattern, [ref]$sel)) {
        $items = $sel.Current.GetSelection()
        if ($items.Count -gt 0) { return $items[0].Current.Name }
    }
    return $combo.Current.Name
}

function Dump-UiState($file) {
    $lines = @(
        "job=$(Get-TextById 'AsyncJobId')",
        "state=$(Get-TextById 'AsyncState')",
        "sut=$(Get-TextById 'AsyncSut')",
        "phase=$(Get-TextById 'AsyncPhase')",
        "progress=$(Get-TextById 'AsyncProgress')",
        "failure=$(Get-TextById 'AsyncFailureReason')",
        "result=$(Get-TextById 'AsyncResultSummary')"
    )
    Set-Content -Path (Join-Path $shotDir $file) -Value $lines -Encoding UTF8
    return $lines
}

function Select-Mr($mrId) {
    $combo = Find-ById $script:win 'AsyncMrCombo' 10
    if (-not $combo) { throw "AsyncMrCombo not found." }
    Bring-Front
    $ecp = $null
    if ($combo.TryGetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern, [ref]$ecp)) {
        $ecp.Expand()
        Start-Sleep -Milliseconds 500
    }
    $item = Find-ByName $script:win $mrId 10
    if (-not $item) { throw "MR item not found: $mrId" }
    if (-not (Click-SelectableAncestor $item)) { throw "Could not select MR: $mrId" }
    try { if ($ecp) { $ecp.Collapse() } } catch {}
    Start-Sleep -Milliseconds 300
    $selected = Get-ComboSelection (Find-ById $script:win 'AsyncMrCombo' 3)
    if ($selected -ne $mrId) {
        throw "MR selection did not stick: expected '$mrId', actual '$selected'."
    }
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
    if (-not (Find-ById $script:win 'AsyncSubmitButton' 12)) {
        Shot 'diagnostic-after-async-nav.png'
        throw "Async page not reached."
    }
}

function Wait-SutPid($timeoutSec = 12) {
    $deadline = (Get-Date).AddSeconds($timeoutSec)
    do {
        if (Test-Path $sutPidFile) {
            $text = (Get-Content $sutPidFile -Raw).Trim()
            $candidatePid = 0
            if ([int]::TryParse($text, [ref]$candidatePid)) {
                return $candidatePid
            }
        }
        Start-Sleep -Milliseconds 250
    } while ((Get-Date) -lt $deadline)
    return 0
}

function Wait-State($target, $timeoutSec = 12) {
    $deadline = (Get-Date).AddSeconds($timeoutSec)
    do {
        $state = Get-TextById 'AsyncState'
        if ($state -eq $target) { return $true }
        Start-Sleep -Milliseconds 250
    } while ((Get-Date) -lt $deadline)
    return $false
}

function Test-ProcessAlive($processId) {
    try {
        $p = [System.Diagnostics.Process]::GetProcessById([int]$processId)
        try { return -not $p.HasExited } finally { $p.Dispose() }
    } catch {
        return $false
    }
}

function Save-SutProcessState($path, $processId, $label) {
    $alive = Test-ProcessAlive $processId
    $lines = @(
        "label=$label",
        "pid=$processId",
        "alive=$alive",
        "sut_script=$script:sutScript"
    )
    try {
        $p = [System.Diagnostics.Process]::GetProcessById([int]$processId)
        $lines += "process_name=$($p.ProcessName)"
        $lines += "start_time=$($p.StartTime.ToString('o'))"
        $p.Dispose()
    } catch {
        $lines += "process_name="
        $lines += "start_time="
    }
    Set-Content -Path $path -Value $lines -Encoding UTF8
}

function Wait-PidGone($processId, $timeoutSec = 8) {
    $deadline = (Get-Date).AddSeconds($timeoutSec)
    do {
        if (-not (Test-ProcessAlive $processId)) { return $true }
        Start-Sleep -Milliseconds 250
    } while ((Get-Date) -lt $deadline)
    return $false
}

Push-Location $repo
try {
    foreach ($stale in @(
        'test-summary.txt',
        'failure.txt',
        'process-before-cancel.txt',
        'process-after-cancel.txt',
        'sut-pid-before-cancel.txt',
        'ui-state-after-cancel.txt')) {
        $stalePath = Join-Path $shotDir $stale
        if (Test-Path $stalePath) { Remove-Item $stalePath -Force }
    }
    "branch=$(git rev-parse --abbrev-ref HEAD)" | Add-Content $summaryLog
    "head=$(git rev-parse HEAD)" | Add-Content $summaryLog
    "selected_mr=advection-amplitude-linearity" | Add-Content $summaryLog
    "selected_value=peak_amplitude" | Add-Content $summaryLog
    "selected_relation=launcher asserts followup>source for factor=2; VM preflight screenshot showed source=0.75585863737664949, followup=1.511717274753299" | Add-Content $summaryLog

    Select-String -Path MetBench_Client\App.xaml.cs -Pattern 'AddSingleton<IJobCancellationRegistry, JobCancellationRegistry>' | Out-File (Join-Path $shotDir 'wiring-checks.txt') -Encoding UTF8
    Select-String -Path MetBench_Client\Hosting\SystemMtJobWorkerHostedService.cs -Pattern 'new SystemMtJobWorker\(_store, pipeline, _cancellation\)' | Out-File (Join-Path $shotDir 'wiring-checks.txt') -Append -Encoding UTF8

    dotnet build MetBench_Client\MetBench_Client.csproj -c Debug *> $buildLog
    $buildExit = $LASTEXITCODE
    $buildWarnings = (Select-String -Path $buildLog -Pattern ': warning ').Count
    $buildErrors = (Select-String -Path $buildLog -Pattern ': error ').Count
    "build_exit=$buildExit" | Add-Content $summaryLog
    "build_warnings=$buildWarnings" | Add-Content $summaryLog
    "build_errors=$buildErrors" | Add-Content $summaryLog
    if ($buildExit -ne 0 -or $buildErrors -gt 0) { throw "Build failed. See $buildLog" }

    $clientOut = Join-Path $repo 'MetBench_Client\bin\Debug\net8.0-windows7.0'
    $script:sutScript = Join-Path $clientOut 'SUT\advection_1d\advection_1d.py'
    if (!(Test-Path $script:sutScript)) { throw "Missing build-output SUT script: $script:sutScript" }
    $script:sutBackup = "$script:sutScript.true-cancel-backup"
    Copy-Item $script:sutScript $script:sutBackup -Force
    $original = Get-Content $script:sutScript -Raw
    $pidLiteral = $sutPidFile.Replace('\', '\\')
    $pidProbe = "import os`r`nwith open(r'$pidLiteral', 'w', encoding='utf-8') as handle:`r`n    handle.write(str(os.getpid()))`r`n    handle.flush()`r`nimport time`r`ntime.sleep(30)`r`n"
    if ($original -match '(?m)^from __future__ import annotations\r?\n') {
        $probe = $original -replace '(?m)^from __future__ import annotations\r?\n', "from __future__ import annotations`r`n$pidProbe"
    } else {
        $probe = "$pidProbe$original"
    }
    Set-Content $script:sutScript -Value $probe -Encoding UTF8
    "probe_modified_build_output=$script:sutScript" | Add-Content $summaryLog

    $exe = Join-Path $clientOut 'MetBench_Client.exe'
    $script:proc = Start-MetBench $exe
    Attach-MainWindow $script:proc
    Open-AsyncPage
    Select-Mr 'advection-amplitude-linearity'
    Shot '01-before-submit.png'
    [void](Click-Element (Find-ById $script:win 'AsyncSubmitButton' 5))
    Start-Sleep -Seconds 1
    Dump-UiState 'ui-state-after-submit.txt' | Out-Null
    Shot '02-after-submit.png'
    if ((Get-TextById 'AsyncJobId') -eq '-') {
        [void](Force-Click-ById 'AsyncSubmitButton')
        Start-Sleep -Seconds 1
        Dump-UiState 'ui-state-after-submit-retry.txt' | Out-Null
        Shot '03-after-submit-retry.png'
    }

    $sutPid = Wait-SutPid 15
    if ($sutPid -le 0) { throw "BLOCKED: no SUT PID file was written before Cancel." }
    Save-SutProcessState (Join-Path $shotDir 'process-before-cancel.txt') $sutPid 'before-cancel'
    if (-not (Test-ProcessAlive $sutPid)) { throw "BLOCKED: SUT PID $sutPid was not alive before Cancel." }
    "before_pid=$sutPid" | Add-Content $summaryLog
    Shot '04-running-before-cancel.png'

    [void](Click-Element (Find-ById $script:win 'AsyncCancelButton' 5))
    $cancelled = Wait-State 'Cancelled' 12
    Start-Sleep -Seconds 3
    $pidGone = Wait-PidGone $sutPid 8
    Save-SutProcessState (Join-Path $shotDir 'process-after-cancel.txt') $sutPid 'after-cancel'
    $uiLines = Dump-UiState 'ui-state-after-cancel.txt'
    Shot '05-cancelled-after-cancel.png'
    Shot '06-no-success-result-after-cancel.png'

    "after_pid_alive=$(Test-ProcessAlive $sutPid)" | Add-Content $summaryLog
    "ui_cancelled=$cancelled" | Add-Content $summaryLog
    "before_pid_gone=$pidGone" | Add-Content $summaryLog
    $resultLine = $uiLines | Where-Object { $_ -like 'result=*' }
    "result_line=$resultLine" | Add-Content $summaryLog

    if (-not $cancelled) { throw "FAIL: UI did not reach Cancelled." }
    if (-not $pidGone) { throw "FAIL: before-cancel SUT PID $sutPid remained alive." }
    if ($resultLine -ne 'result=') { throw "FAIL: cancelled job displayed a non-empty result summary: $resultLine" }
    "result=PASS" | Add-Content $summaryLog
}
catch {
    $message = $_ | Out-String
    Set-Content -Path (Join-Path $shotDir 'failure.txt') -Value $message -Encoding UTF8
    "result=FAIL" | Add-Content $summaryLog
    "failure=$($_.Exception.Message)" | Add-Content $summaryLog
    throw
}
finally {
    if ($script:sutBackup -and (Test-Path $script:sutBackup)) {
        Copy-Item $script:sutBackup $script:sutScript -Force
        Remove-Item $script:sutBackup -Force
    }
    git status -sb | Out-File (Join-Path $shotDir 'git-status-after-restore.txt') -Encoding UTF8
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
