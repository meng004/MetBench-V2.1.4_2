#requires -version 5
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms
Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class WpfMinimalBehaviorsWin32 {
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
[void][WpfMinimalBehaviorsWin32]::SetProcessDPIAware()

$ErrorActionPreference = 'Stop'
$AE = [System.Windows.Automation.AutomationElement]
$TS = [System.Windows.Automation.TreeScope]
$CT = [System.Windows.Automation.ControlType]
$shotDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repo = Resolve-Path (Join-Path $shotDir '..\..\..\..')
$script:win = $null
$script:proc = $null
$script:scale = 1.0

function Prop($id, $value) {
    New-Object System.Windows.Automation.PropertyCondition($id, $value)
}

function And-Cond($left, $right) {
    New-Object System.Windows.Automation.AndCondition($left, $right)
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
        [void][WpfMinimalBehaviorsWin32]::ShowWindow([IntPtr]$script:win.Current.NativeWindowHandle, 3)
        [void][WpfMinimalBehaviorsWin32]::SetForegroundWindow([IntPtr]$script:win.Current.NativeWindowHandle)
        Start-Sleep -Milliseconds 250
    }
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
        [WpfMinimalBehaviorsWin32]::Click([int](($r.X + $r.Width / 2) / $script:scale), [int](($r.Y + $r.Height / 2) / $script:scale))
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
        if (-not $edit.Current.IsOffscreen) {
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
    }
    return $false
}

function Scroll-NavDown {
    Bring-Front
    $r = $script:win.Current.BoundingRectangle
    $x = [int](($r.X + 160) / $script:scale)
    $y = [int](($r.Y + [Math]::Min(700, $r.Height - 100)) / $script:scale)
    [WpfMinimalBehaviorsWin32]::Wheel($x, $y, -720)
    Start-Sleep -Milliseconds 450
}

function Open-Page([string[]]$searchTerms, [string[]]$navNames) {
    foreach ($name in $navNames) {
        $nav = Find-VisibleByNameAny $script:win @($name) 2
        if ($nav -and (Click-SelectableAncestor $nav)) {
            Start-Sleep -Seconds 1
            return
        }
    }
    for ($i = 0; $i -lt 8; $i++) {
        Scroll-NavDown
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
}

function Start-MetBench($exe) {
    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = $exe
    $psi.WorkingDirectory = Split-Path $exe -Parent
    $psi.UseShellExecute = $false
    return [System.Diagnostics.Process]::Start($psi)
}

function Attach-MainWindow($proc) {
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

Push-Location $repo
try {
    foreach ($name in @(
        '01-main-window-startup.png',
        '02-mt-report-generator-behavior-page.png',
        '03-export-command-empty-file-dialog.png',
        'failure.txt')) {
        $path = Join-Path $shotDir $name
        if (Test-Path -LiteralPath $path) { Remove-Item -LiteralPath $path -Force }
    }

    $exe = Join-Path $repo 'MetBench_Client\bin\Debug\net8.0-windows7.0\MetBench_Client.exe'
    $script:proc = Start-MetBench $exe
    Attach-MainWindow $script:proc
    Shot-Element $script:win '01-main-window-startup.png'

    Open-Page @('MT') @('MT Execution')
    $reportButton = Find-ButtonByNameAny $script:win @('MTReport', 'MT Report', 'MT 报告') 8
    if (-not $reportButton) { throw "MTReport button not found." }
    [void](Click-Element $reportButton)
    $entryDialog = Find-Dialog 10
    Close-Dialog $entryDialog
    Shot-Element $script:win '02-mt-report-generator-behavior-page.png'

    $button = Find-ButtonByNameAny $script:win @('Export', '导出') 8
    if (-not $button) { throw "Export button not found." }
    [void](Click-Element $button)
    $dialog = Find-Dialog 10
    Shot-Element $dialog '03-export-command-empty-file-dialog.png'

    foreach ($shot in @(
        '01-main-window-startup.png',
        '02-mt-report-generator-behavior-page.png',
        '03-export-command-empty-file-dialog.png')) {
        if (-not (Test-Path -LiteralPath (Join-Path $shotDir $shot))) { throw "Missing screenshot: $shot" }
    }
}
catch {
    Set-Content -Path (Join-Path $shotDir 'failure.txt') -Value ($_ | Out-String) -Encoding UTF8
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
