#requires -version 5
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms
Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class AsyncVmWin32 {
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
[void][AsyncVmWin32]::SetProcessDPIAware()

$ErrorActionPreference = 'Stop'
$AE = [System.Windows.Automation.AutomationElement]
$TS = [System.Windows.Automation.TreeScope]
$CT = [System.Windows.Automation.ControlType]
$shotDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repo = Resolve-Path (Join-Path $shotDir '..\..\..\..')
$buildLog = Join-Path $shotDir 'build-output.txt'
$statusLog = Join-Path $shotDir 'observed-status-sequence.txt'
$script:win = $null
$script:scale = 1.0

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
        [void][AsyncVmWin32]::ShowWindow([IntPtr]$script:win.Current.NativeWindowHandle, 3)
        [void][AsyncVmWin32]::SetForegroundWindow([IntPtr]$script:win.Current.NativeWindowHandle)
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
        [AsyncVmWin32]::Click([int](($r.X + $r.Width / 2) / $script:scale), [int](($r.Y + $r.Height / 2) / $script:scale))
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

function Text-Shot($file, $text) {
    $font = New-Object System.Drawing.Font('Consolas', 13)
    $bmp = New-Object System.Drawing.Bitmap(1500, 900)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.Clear([System.Drawing.Color]::White)
    $brush = [System.Drawing.Brushes]::Black
    $rect = New-Object System.Drawing.RectangleF(20, 20, 1460, 860)
    $g.DrawString($text, $font, $brush, $rect)
    $bmp.Save((Join-Path $shotDir $file), [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose()
    $bmp.Dispose()
}

function Select-Mr($mrId) {
    $combo = Find-ById $script:win 'AsyncMrCombo' 10
    if (-not $combo) { throw "AsyncMrCombo not found." }
    $ecp = $null
    if ($combo.TryGetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern, [ref]$ecp)) {
        $ecp.Expand()
        Start-Sleep -Milliseconds 500
    }
    $item = Find-ByName $script:win $mrId 8
    if (-not $item) { throw "MR item not found: $mrId" }
    if (-not (Click-Element $item)) { throw "Could not select MR: $mrId" }
    try { if ($ecp) { $ecp.Collapse() } } catch {}
    Start-Sleep -Milliseconds 300
}

function Get-TextById($id) {
    $el = Find-ById $script:win $id 2
    if (-not $el) { return '' }
    return $el.Current.Name
}

function Dump-State($label) {
    $line = "$(Get-Date -Format o) [$label] state=$(Get-TextById 'AsyncState') phase=$(Get-TextById 'AsyncPhase') progress=$(Get-TextById 'AsyncProgress') job=$(Get-TextById 'AsyncJobId')"
    Add-Content -Path $statusLog -Value $line
    Write-Host $line
}

function Wait-Terminal($timeoutSec = 45) {
    $deadline = (Get-Date).AddSeconds($timeoutSec)
    do {
        Dump-State 'poll'
        $state = Get-TextById 'AsyncState'
        if ($state -in @('Succeeded', 'Failed', 'TimedOut', 'ArtifactMissing', 'Cancelled')) { return $state }
        Start-Sleep -Milliseconds 500
    } while ((Get-Date) -lt $deadline)
    throw "Timed out waiting for terminal async job state."
}

Push-Location $repo
try {
    dotnet build MetBench_Client\MetBench_Client.csproj -v:quiet *> $buildLog
    if ($LASTEXITCODE -ne 0) { throw "dotnet build failed with exit $LASTEXITCODE" }
    $tail = (Get-Content $buildLog -Tail 45) -join [Environment]::NewLine
    Text-Shot '01-build-output.png' $tail

    if (Test-Path $statusLog) { Remove-Item $statusLog -Force }
    $exe = Join-Path $repo 'MetBench_Client\bin\Debug\net8.0-windows7.0\MetBench_Client.exe'
    $proc = Start-Process $exe -WorkingDirectory (Split-Path $exe -Parent) -PassThru
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

    [void](Set-NavSearch 'Async')
    $nav = Find-ByName $script:win 'System MT Async Execution' 12
    if (-not $nav) { throw "Async navigation item not found." }
    if (-not (Click-SelectableAncestor $nav)) { throw "Async navigation item could not be clicked." }
    if (-not (Find-ById $script:win 'AsyncSubmitButton' 12)) {
        Shot 'diagnostic-after-async-nav.png'
        throw "Async page not reached."
    }
    Shot '02-async-page-loaded.png'

    Select-Mr 'advection-amplitude-linearity'
    [void](Click-Element (Find-ById $script:win 'AsyncSubmitButton' 5))
    Start-Sleep -Milliseconds 150
    Dump-State 'submit-immediate'
    Shot '03-submit-immediate.png'
    Start-Sleep -Seconds 1
    Dump-State 'auto-poll'
    Shot '04-polling-progress.png'
    [void](Click-Element (Find-ById $script:win 'AsyncRefreshButton' 5))
    Start-Sleep -Milliseconds 300
    Dump-State 'manual-refresh'
    Shot '05-manual-refresh.png'
    $successState = Wait-Terminal 45
    if ($successState -ne 'Succeeded') { throw "Expected success MR to Succeed, got $successState." }
    Shot '06-succeeded-result.png'

    $failureCandidates = @(
        'openmc-pincell-particle-count-convergence',
        'openmc-pincell-nu-sigma-f',
        'scipy-bvp-poisson-seed-mesh-insensitivity',
        'scipy-ivp-lv-step-convergence',
        'openmoc-pincell-ray-track-convergence'
    )
    $capturedFailure = $false
    foreach ($candidate in $failureCandidates) {
        try {
            Select-Mr $candidate
            [void](Click-Element (Find-ById $script:win 'AsyncSubmitButton' 5))
            Start-Sleep -Seconds 1
            if (-not $capturedFailure) { Shot '07-failure-running.png' }
            $failedState = Wait-Terminal 60
            if ($failedState -ne 'Succeeded') {
                Add-Content -Path $statusLog -Value "$(Get-Date -Format o) [failure-candidate] mr=$candidate terminal=$failedState"
                Shot '08-failed-result.png'
                $capturedFailure = $true
                break
            }
            Add-Content -Path $statusLog -Value "$(Get-Date -Format o) [failure-candidate-succeeded] mr=$candidate terminal=$failedState"
        } catch {
            Add-Content -Path $statusLog -Value "$(Get-Date -Format o) [failure-candidate-exception] mr=$candidate error=$($_.Exception.Message)"
            Shot '08-failed-result.png'
            $capturedFailure = $true
            break
        }
    }
    if (-not $capturedFailure) {
        $blocker = "AC-V5 blocker: all attempted dependency-sensitive failure candidates reached Succeeded on this VM: $($failureCandidates -join ', ')"
        Set-Content -Path (Join-Path $shotDir 'failure-path-blocked.txt') -Value $blocker -Encoding UTF8
        Write-Host $blocker
    }

    Select-Mr 'scipy-bvp-poisson-seed-mesh-insensitivity'
    [void](Click-Element (Find-ById $script:win 'AsyncSubmitButton' 5))
    Start-Sleep -Milliseconds 30
    Shot '09-cancel-before.png'
    [void](Click-Element (Find-ById $script:win 'AsyncCancelButton' 5))
    Start-Sleep -Milliseconds 300
    Dump-State 'cancel-clicked'
    Shot '10-cancelled.png'
}
finally {
    try { if ($proc -and -not $proc.HasExited) { $proc | Stop-Process -Force } } catch {}
    Pop-Location
}
