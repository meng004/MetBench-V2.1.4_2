#requires -version 5
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms
Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class P4DeadlockWin32 {
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
  public static void Wheel(int x,int y,int delta) {
    SetCursorPos(x,y); System.Threading.Thread.Sleep(80);
    mouse_event(0x0800,0,0,unchecked((uint)delta),UIntPtr.Zero);
  }
}
"@
[void][P4DeadlockWin32]::SetProcessDPIAware()

$ErrorActionPreference = 'Stop'
$AE = [System.Windows.Automation.AutomationElement]
$TS = [System.Windows.Automation.TreeScope]
$CT = [System.Windows.Automation.ControlType]
$shotDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repo = Resolve-Path (Join-Path $shotDir '..\..\..\..')
$buildLog = Join-Path $shotDir 'build-output.txt'
$summary = Join-Path $shotDir 'vm-summary.md'
$script:win = $null
$script:proc = $null
$script:scale = 1.0

function Prop($id, $value) {
    New-Object System.Windows.Automation.PropertyCondition($id, $value)
}

function And-Cond($left, $right) {
    New-Object System.Windows.Automation.AndCondition($left, $right)
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

function Find-ByNameAny($root, [string[]]$names, $timeoutSec = 8) {
    $deadline = (Get-Date).AddSeconds($timeoutSec)
    do {
        foreach ($name in $names) {
            $cond = Prop ([System.Windows.Automation.AutomationElement]::NameProperty) $name
            $el = $root.FindFirst($TS::Descendants, $cond)
            if ($el) { return $el }
        }
        Start-Sleep -Milliseconds 200
    } while ((Get-Date) -lt $deadline)
    return $null
}

function Find-ByNameContainsAny($root, [string[]]$names, $timeoutSec = 8) {
    $deadline = (Get-Date).AddSeconds($timeoutSec)
    do {
        $all = $root.FindAll($TS::Descendants, [System.Windows.Automation.Condition]::TrueCondition)
        foreach ($el in $all) {
            foreach ($name in $names) {
                if ($el.Current.Name -and $el.Current.Name.Contains($name)) {
                    return $el
                }
            }
        }
        Start-Sleep -Milliseconds 200
    } while ((Get-Date) -lt $deadline)
    return $null
}

function Find-VisibleByNameAny($root, [string[]]$names, $timeoutSec = 8) {
    $deadline = (Get-Date).AddSeconds($timeoutSec)
    do {
        foreach ($name in $names) {
            $cond = Prop ([System.Windows.Automation.AutomationElement]::NameProperty) $name
            $all = $root.FindAll($TS::Descendants, $cond)
            foreach ($el in $all) {
                if (-not $el.Current.IsOffscreen) { return $el }
            }
        }
        Start-Sleep -Milliseconds 200
    } while ((Get-Date) -lt $deadline)
    return $null
}

function Find-ButtonByNameAny($root, [string[]]$names, $timeoutSec = 8) {
    $buttonCond = Prop ([System.Windows.Automation.AutomationElement]::ControlTypeProperty) $CT::Button
    $deadline = (Get-Date).AddSeconds($timeoutSec)
    do {
        foreach ($name in $names) {
            $nameCond = Prop ([System.Windows.Automation.AutomationElement]::NameProperty) $name
            $el = $root.FindFirst($TS::Descendants, (And-Cond $buttonCond $nameCond))
            if ($el -and -not $el.Current.IsOffscreen) { return $el }
        }
        Start-Sleep -Milliseconds 200
    } while ((Get-Date) -lt $deadline)
    return $null
}

function Bring-Front {
    if ($script:win) {
        [void][P4DeadlockWin32]::ShowWindow([IntPtr]$script:win.Current.NativeWindowHandle, 3)
        [void][P4DeadlockWin32]::SetForegroundWindow([IntPtr]$script:win.Current.NativeWindowHandle)
        Start-Sleep -Milliseconds 250
    }
}

function Scroll-NavDown {
    Bring-Front
    $r = $script:win.Current.BoundingRectangle
    $x = [int](($r.X + 160) / $script:scale)
    $y = [int](($r.Y + [Math]::Min(700, $r.Height - 100)) / $script:scale)
    [P4DeadlockWin32]::Wheel($x, $y, -720)
    Start-Sleep -Milliseconds 450
}

function Click-Element($el) {
    if (-not $el) { return $false }
    if ($el.Current.IsOffscreen) {
        $sip = $null
        if ($el.TryGetCurrentPattern([System.Windows.Automation.ScrollItemPattern]::Pattern, [ref]$sip)) {
            $sip.ScrollIntoView()
            Start-Sleep -Milliseconds 500
        }
    }
    if ($el.Current.IsOffscreen) { return $false }
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
        [P4DeadlockWin32]::Click([int](($r.X + $r.Width / 2) / $script:scale), [int](($r.Y + $r.Height / 2) / $script:scale))
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
    $visibleEdits = @()
    foreach ($edit in $edits) {
        if (-not $edit.Current.IsOffscreen) {
            $visibleEdits += $edit
        }
    }
    foreach ($edit in ($visibleEdits | Sort-Object { $_.Current.BoundingRectangle.X })) {
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

function Shot-Element($el, $file) {
    $r = $el.Current.BoundingRectangle
    $x = [int]($r.X / $script:scale)
    $y = [int]($r.Y / $script:scale)
    $w = [Math]::Max(1, [int]($r.Width / $script:scale))
    $h = [Math]::Max(1, [int]($r.Height / $script:scale))
    $bmp = New-Object System.Drawing.Bitmap($w, $h)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen($x, $y, 0, 0, $bmp.Size)
    $bmp.Save((Join-Path $shotDir $file), [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose()
    $bmp.Dispose()
    Write-Host "[shot] $file"
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

function Open-Page([string[]]$searchTerms, [string[]]$navNames, [string[]]$navIds = @()) {
    foreach ($id in $navIds) {
        $nav = Find-ById $script:win $id 2
        if ($nav -and (Click-SelectableAncestor $nav)) {
            Start-Sleep -Seconds 1
            return
        }
    }
    foreach ($name in $navNames) {
        $nav = Find-VisibleByNameAny $script:win @($name) 2
        if ($nav -and (Click-SelectableAncestor $nav)) {
            Start-Sleep -Seconds 1
            return
        }
    }
    for ($i = 0; $i -lt 8; $i++) {
        Scroll-NavDown
        foreach ($id in $navIds) {
            $nav = Find-ById $script:win $id 1
            if ($nav -and (Click-SelectableAncestor $nav)) {
                Start-Sleep -Seconds 1
                return
            }
        }
        foreach ($name in $navNames) {
            $nav = Find-VisibleByNameAny $script:win @($name) 1
            if ($nav -and (Click-SelectableAncestor $nav)) {
                Start-Sleep -Seconds 1
                return
            }
        }
    }
    foreach ($term in $searchTerms) {
        [void](Set-NavSearch $term)
        $nav = Find-ByNameAny $script:win $navNames 4
        if ($nav -and (Click-SelectableAncestor $nav)) {
            Start-Sleep -Seconds 1
            return
        }
    }
    throw "Navigation item not found: $($navNames -join ', ')"
}

function Find-Dialog($timeoutSec = 10) {
    $deadline = (Get-Date).AddSeconds($timeoutSec)
    $procCond = Prop ([System.Windows.Automation.AutomationElement]::ProcessIdProperty) $script:proc.Id
    do {
        $windows = $AE::RootElement.FindAll($TS::Children, $procCond)
        foreach ($window in $windows) {
            if ($window.Current.NativeWindowHandle -ne $script:win.Current.NativeWindowHandle -and -not $window.Current.IsOffscreen) {
                return $window
            }
        }
        Start-Sleep -Milliseconds 200
    } while ((Get-Date) -lt $deadline)
    throw "Dialog window not found."
}

function Close-Dialog($dialog) {
    $button = Find-ButtonByNameAny $dialog @('OK', 'Ok', 'Yes') 4
    if (-not $button) { throw "Dialog OK button not found." }
    [void](Click-Element $button)
    Start-Sleep -Milliseconds 600
    Bring-Front
    if (-not (Find-ByNameAny $script:win @('MR Management', 'Application Management', 'MR ReportGenerator') 2)) {
        throw "Main window did not remain responsive after closing dialog."
    }
}

function Trigger-ButtonDialog([string]$shot, [string[]]$buttonNames) {
    $button = Find-ButtonByNameAny $script:win $buttonNames 8
    if (-not $button) { throw "Button not found: $($buttonNames -join ', ')" }
    [void](Click-Element $button)
    $dialog = Find-Dialog 10
    Shot-Element $dialog $shot
    Close-Dialog $dialog
}

function Select-ComboItem([string[]]$names) {
    $comboCond = Prop ([System.Windows.Automation.AutomationElement]::ControlTypeProperty) $CT::ComboBox
    $combos = $script:win.FindAll($TS::Descendants, $comboCond)
    $visibleCombos = @()
    foreach ($candidate in $combos) {
        if (-not $candidate.Current.IsOffscreen) {
            $visibleCombos += $candidate
        }
    }
    $combo = Find-ById $script:win 'ReportTypeComboBox' 2
    if (-not $combo) {
        $combo = $visibleCombos |
        Sort-Object { $_.Current.BoundingRectangle.X } -Descending |
        Select-Object -First 1
    }
    if (-not $combo) { throw "ComboBox not found." }
    $ecp = $null
    if ($combo.TryGetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern, [ref]$ecp)) {
        $ecp.Expand()
        Start-Sleep -Milliseconds 400
    }
    $item = Find-ByNameAny $script:win $names 2
    if (-not $item) {
        $item = Find-ByNameContainsAny $script:win $names 2
    }
    if (-not $item) {
        $comboDescriptions = $visibleCombos | ForEach-Object {
            $r = $_.Current.BoundingRectangle
            "name='$($_.Current.Name)' x=$($r.X) y=$($r.Y) w=$($r.Width) h=$($r.Height)"
        }
        Write-Host "[diagnostic] visible combos: $($comboDescriptions -join '; ')"
        $item = Find-ByNameAny $AE::RootElement $names 8
        if (-not $item) {
            $item = Find-ByNameContainsAny $AE::RootElement $names 8
        }
    }
    if (-not $item) {
        Bring-Front
        try { $combo.SetFocus() } catch {}
        Start-Sleep -Milliseconds 200
        [System.Windows.Forms.SendKeys]::SendWait("{F4}")
        Start-Sleep -Milliseconds 300
        [System.Windows.Forms.SendKeys]::SendWait("{DOWN}")
        Start-Sleep -Milliseconds 200
        [System.Windows.Forms.SendKeys]::SendWait("{ENTER}")
        Start-Sleep -Milliseconds 800
        return
    }
    if (-not (Click-SelectableAncestor $item)) { throw "Could not select combo item." }
    Start-Sleep -Milliseconds 500
}

Push-Location $repo
try {
    foreach ($name in @(
        '01-mr-management-save-dialog.png',
        '02-application-management-save-dialog.png',
        '03-mt-report-generator-selection-change.png',
        'build-output.txt',
        'vm-summary.md',
        'failure.txt')) {
        $path = Join-Path $shotDir $name
        if (Test-Path -LiteralPath $path) { Remove-Item -LiteralPath $path -Force }
    }

    dotnet build MetBench.sln --no-restore -v:minimal 2>&1 | Out-File -FilePath $buildLog -Encoding UTF8
    $buildExit = $LASTEXITCODE
    $buildErrors = (Select-String -Path $buildLog -Pattern ': error ').Count
    if ($buildExit -ne 0 -or $buildErrors -gt 0) { throw "Build failed. See $buildLog" }

    $clientOut = Join-Path $repo 'MetBench_Client\bin\Debug\net8.0-windows7.0'
    $exe = Join-Path $clientOut 'MetBench_Client.exe'
    $script:proc = Start-MetBench $exe
    Attach-MainWindow $script:proc

    Open-Page @('MR') @('MR Management')
    Trigger-ButtonDialog '01-mr-management-save-dialog.png' @('Add')

    Open-Page @('Application') @('Application Management')
    Trigger-ButtonDialog '02-application-management-save-dialog.png' @('Add')

    Open-Page @('MT') @('MT Execution')
    Trigger-ButtonDialog '03-mt-report-generator-selection-change.png' @('MTReport', 'MT Report')

    $dialogShots = @(
        '01-mr-management-save-dialog.png',
        '02-application-management-save-dialog.png',
        '03-mt-report-generator-selection-change.png')
    foreach ($shot in $dialogShots) {
        if (-not (Test-Path -LiteralPath (Join-Path $shotDir $shot))) { throw "Missing screenshot: $shot" }
    }

    $showDialogGuard = & rg -n "\.ShowDialogAsync\(\)\.(Result|GetAwaiter\(\)\.GetResult\(\))" MetBench_Client\ViewModels -g "*.cs"
    $showDialogExit = $LASTEXITCODE
    $asyncVoidAll = & rg -n "async void" MetBench_Client\ViewModels -g "*.cs"
    $asyncVoidUnexpected = $asyncVoidAll | Where-Object { $_ -notmatch 'OnNavigatedTo' }
    if ($showDialogExit -eq 0) { throw "ShowDialog sync wait guard was not empty." }
    if ($asyncVoidUnexpected.Count -gt 0) { throw "Unexpected async void remains: $($asyncVoidUnexpected -join '; ')" }

    $lines = @(
        '# P4 WPF Deadlock Surface VM Summary',
        '',
        "branch=$(git rev-parse --abbrev-ref HEAD)",
        "base_head_at_run=$(git rev-parse HEAD)",
        "origin_main=$(git rev-parse origin/main)",
        'worktree_at_run=dirty with P4 WPF deadlock changes under validation',
        '',
        '## Commands',
        '',
        '- `dotnet build MetBench.sln --no-restore -v:minimal`: exit 0; errors 0',
        '- `rg -n "\.ShowDialogAsync\(\)\.(Result|GetAwaiter\(\)\.GetResult\(\))" MetBench_Client\ViewModels -g "*.cs"`: no matches',
        '- `rg -n "async void" MetBench_Client\ViewModels -g "*.cs" | non-OnNavigatedTo filter`: no matches',
        '- UIA driver: exit 0',
        '',
        '## Screenshots',
        '')
    foreach ($shot in $dialogShots) {
        $lines += '- `' + $shot + '`'
    }
    $lines += ''
    $lines += '## Blockers'
    $lines += ''
    $lines += 'None.'
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
