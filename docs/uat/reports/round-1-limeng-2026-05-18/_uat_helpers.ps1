# Reusable helpers for Round-1 UAT automation.
# Source: . .\docs\uat\reports\round-1-limeng-2026-05-18\_uat_helpers.ps1

Add-Type -AssemblyName UIAutomationClient -ErrorAction SilentlyContinue
Add-Type -AssemblyName UIAutomationTypes -ErrorAction SilentlyContinue
Add-Type -AssemblyName System.Windows.Forms -ErrorAction SilentlyContinue
Add-Type -AssemblyName System.Drawing -ErrorAction SilentlyContinue

if (-not ('UatNative' -as [type])) {
    Add-Type @'
using System;
using System.Runtime.InteropServices;
public class UatNative {
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int X, int Y);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
    [DllImport("kernel32.dll")] static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")] static extern bool AttachThreadInput(uint a, uint b, bool f);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int n);
    [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr h, IntPtr i, int x, int y, int cx, int cy, uint f);
    [StructLayout(LayoutKind.Sequential)] public struct INPUT { public uint type; public MOUSEINPUT mi; }
    [StructLayout(LayoutKind.Sequential)] public struct MOUSEINPUT {
        public int dx, dy; public uint mouseData; public uint dwFlags; public uint time; public IntPtr dwExtraInfo;
    }
    [DllImport("user32.dll")] static extern uint SendInput(uint n, INPUT[] inp, int sz);
    public static readonly IntPtr HWND_TOP = IntPtr.Zero;
    public static void Focus(IntPtr h) {
        uint pid; uint tid = GetWindowThreadProcessId(h, out pid);
        uint cur = GetCurrentThreadId();
        ShowWindow(h, 3);
        AttachThreadInput(cur, tid, true);
        SetForegroundWindow(h);
        System.Threading.Thread.Sleep(300);
        AttachThreadInput(cur, tid, false);
    }
    public static void ClickAbs(int px, int py, int sw, int sh) {
        int nx = px * 65535 / sw, ny = py * 65535 / sh;
        INPUT[] d = new INPUT[2];
        d[0].type = 0; d[0].mi.dx = nx; d[0].mi.dy = ny; d[0].mi.dwFlags = 0x8001;
        d[1].type = 0; d[1].mi.dx = nx; d[1].mi.dy = ny; d[1].mi.dwFlags = 0x8002 | 0x8001;
        SendInput(2, d, System.Runtime.InteropServices.Marshal.SizeOf(typeof(INPUT)));
        System.Threading.Thread.Sleep(60);
        INPUT[] u = new INPUT[1];
        u[0].type = 0; u[0].mi.dx = nx; u[0].mi.dy = ny; u[0].mi.dwFlags = 0x8004 | 0x8001;
        SendInput(1, u, System.Runtime.InteropServices.Marshal.SizeOf(typeof(INPUT)));
    }
    public static void DoubleClickAbs(int px, int py, int sw, int sh) {
        ClickAbs(px, py, sw, sh);
        System.Threading.Thread.Sleep(50);
        ClickAbs(px, py, sw, sh);
    }
}
'@
}

$Script:EvidenceDir = "C:\Users\limeng\MetBench-V2.1.4_2\docs\uat\reports\round-1-limeng-2026-05-18"
$Script:ShotDir = Join-Path $EvidenceDir "screenshots"

function Get-MetBench {
    $p = Get-Process MetBench_Client -ErrorAction SilentlyContinue |
        Where-Object { $_.MainWindowHandle -ne 0 } |
        Select-Object -First 1
    if (-not $p) { throw "MetBench not running" }
    return $p
}

function Focus-MetBench {
    $p = Get-MetBench
    [UatNative]::Focus($p.MainWindowHandle)
    Start-Sleep -Milliseconds 400
    return $p
}

function Get-AppRoot([int]$ProcId) {
    $root = [System.Windows.Automation.AutomationElement]::RootElement
    $cond = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ProcessIdProperty, $ProcId)
    return $root.FindFirst([System.Windows.Automation.TreeScope]::Children, $cond)
}

function Find-ByName($parent, [string]$name, [string]$controlType = $null) {
    $nameCond = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::NameProperty, $name)
    if ($controlType) {
        $ctMap = @{
            'DataItem' = [System.Windows.Automation.ControlType]::DataItem
            'Button' = [System.Windows.Automation.ControlType]::Button
            'Edit' = [System.Windows.Automation.ControlType]::Edit
            'Text' = [System.Windows.Automation.ControlType]::Text
            'ComboBox' = [System.Windows.Automation.ControlType]::ComboBox
            'CheckBox' = [System.Windows.Automation.ControlType]::CheckBox
            'HeaderItem' = [System.Windows.Automation.ControlType]::HeaderItem
        }
        $ctCond = New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::ControlTypeProperty, $ctMap[$controlType])
        $cond = New-Object System.Windows.Automation.AndCondition(@($nameCond, $ctCond))
    } else {
        $cond = $nameCond
    }
    return $parent.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $cond)
}

function Click-Element($el) {
    if (-not $el) { throw "Click-Element: null" }
    $r = $el.Current.BoundingRectangle
    $px = [int]($r.Left + $r.Width / 2)
    $py = [int]($r.Top + $r.Height / 2)
    $root = [System.Windows.Automation.AutomationElement]::RootElement
    $sw = [int]$root.Current.BoundingRectangle.Width
    $sh = [int]$root.Current.BoundingRectangle.Height
    [UatNative]::ClickAbs($px, $py, $sw, $sh)
}

function DoubleClick-Element($el) {
    if (-not $el) { throw "DoubleClick-Element: null" }
    $r = $el.Current.BoundingRectangle
    $px = [int]($r.Left + $r.Width / 2)
    $py = [int]($r.Top + $r.Height / 2)
    $root = [System.Windows.Automation.AutomationElement]::RootElement
    $sw = [int]$root.Current.BoundingRectangle.Width
    $sh = [int]$root.Current.BoundingRectangle.Height
    [UatNative]::DoubleClickAbs($px, $py, $sw, $sh)
}

function Navigate-To([string]$navName) {
    $p = Focus-MetBench
    $app = Get-AppRoot $p.Id
    $el = Find-ByName $app $navName 'DataItem'
    if (-not $el) { throw "Navigate-To: nav item '$navName' not found" }
    Click-Element $el
    Start-Sleep -Milliseconds 1500
}

function Save-Shot([string]$name) {
    Start-Sleep -Milliseconds 400
    $scr = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
    $bmp = New-Object System.Drawing.Bitmap($scr.Width, $scr.Height)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen($scr.Location, [System.Drawing.Point]::Empty, $scr.Size)
    $g.Dispose()
    $out = Join-Path $Script:ShotDir $name
    $bmp.Save($out, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    Write-Host "  shot -> $name"
    return $out
}

function Set-EditValue($el, [string]$value) {
    if (-not $el) { throw "Set-EditValue: null" }
    $vp = $null
    if ($el.TryGetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern, [ref]$vp)) {
        $vp.SetValue($value)
        return
    }
    Click-Element $el
    Start-Sleep -Milliseconds 200
    [System.Windows.Forms.SendKeys]::SendWait("^a")
    Start-Sleep -Milliseconds 100
    [System.Windows.Forms.SendKeys]::SendWait($value)
}

function Find-FieldByLabel($app, [string]$labelText) {
    # Find Edit textbox to the right of a TextBlock label.
    $labelCond = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::NameProperty, $labelText)
    $labels = $app.FindAll([System.Windows.Automation.TreeScope]::Descendants, $labelCond)
    foreach ($lab in $labels) {
        if ($lab.Current.ControlType -ne [System.Windows.Automation.ControlType]::Text) { continue }
        $lr = $lab.Current.BoundingRectangle
        if ($lr.Width -eq 0) { continue }
        # find all Edit elements within +/- 25px Y and to the right of label
        $editCond = New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
            [System.Windows.Automation.ControlType]::Edit)
        $edits = $app.FindAll([System.Windows.Automation.TreeScope]::Descendants, $editCond)
        $candidate = $null; $bestDx = 9999
        foreach ($e in $edits) {
            $er = $e.Current.BoundingRectangle
            if ($er.Width -eq 0) { continue }
            $dy = [Math]::Abs(($er.Top + $er.Height / 2) - ($lr.Top + $lr.Height / 2))
            if ($dy -gt 25) { continue }
            $dx = $er.Left - $lr.Right
            if ($dx -lt 0 -or $dx -gt 200) { continue }
            if ($dx -lt $bestDx) { $bestDx = $dx; $candidate = $e }
        }
        if ($candidate) { return $candidate }
    }
    return $null
}

function Find-ButtonByName($app, [string]$name) {
    $cond = New-Object System.Windows.Automation.AndCondition(@(
        (New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::NameProperty, $name)),
        (New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
            [System.Windows.Automation.ControlType]::Button))
    ))
    return $app.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $cond)
}

function Wait-Modal([string]$pattern, [int]$timeoutMs = 5000) {
    $root = [System.Windows.Automation.AutomationElement]::RootElement
    $p = Get-MetBench
    $deadline = (Get-Date).AddMilliseconds($timeoutMs)
    while ((Get-Date) -lt $deadline) {
        $wins = $root.FindAll([System.Windows.Automation.TreeScope]::Children,
            [System.Windows.Automation.Condition]::TrueCondition)
        $modal = $wins | Where-Object { $_.Current.ProcessId -eq $p.Id -and $_.Current.Name -match $pattern -and $_.Current.Name -ne "MetBench" } | Select-Object -First 1
        if ($modal) { return $modal }
        Start-Sleep -Milliseconds 200
    }
    return $null
}

function Invoke-OrClick($el) {
    if (-not $el) { throw "Invoke-OrClick: null" }
    $ip = $null
    if ($el.TryGetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern, [ref]$ip)) {
        $ip.Invoke()
    } else {
        Click-Element $el
    }
}

function Click-DialogBtn($dlg, [string]$btnName) {
    $btn = $dlg.FindFirst([System.Windows.Automation.TreeScope]::Descendants,
        (New-Object System.Windows.Automation.AndCondition(@(
            (New-Object System.Windows.Automation.PropertyCondition(
                [System.Windows.Automation.AutomationElement]::NameProperty, $btnName)),
            (New-Object System.Windows.Automation.PropertyCondition(
                [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
                [System.Windows.Automation.ControlType]::Button))
        ))))
    if ($btn) { Invoke-OrClick $btn; return $true }
    return $false
}

function Dismiss-AllTips([int]$maxLoops = 5) {
    $root = [System.Windows.Automation.AutomationElement]::RootElement
    $p = Get-MetBench
    for ($i = 0; $i -lt $maxLoops; $i++) {
        $wins = $root.FindAll([System.Windows.Automation.TreeScope]::Children,
            [System.Windows.Automation.Condition]::TrueCondition)
        $modals = $wins | Where-Object { $_.Current.ProcessId -eq $p.Id -and $_.Current.Name -ne "MetBench" -and $_.Current.Name }
        if (-not $modals) { return }
        foreach ($m in $modals) {
            $mname = $m.Current.Name
            $clicked = $false
            foreach ($btn in @("Yes", "OK")) {
                if (Click-DialogBtn $m $btn) {
                    Write-Host ("    dismissed " + $mname + " with " + $btn)
                    $clicked = $true
                    Start-Sleep -Milliseconds 800
                    break
                }
            }
            if (-not $clicked) {
                Write-Host ("    could not dismiss " + $mname)
            }
        }
    }
}

function Dump-Tree([int]$ProcId, [int]$maxItems = 100) {
    $app = Get-AppRoot $ProcId
    $all = $app.FindAll([System.Windows.Automation.TreeScope]::Descendants,
        [System.Windows.Automation.Condition]::TrueCondition)
    $n = [Math]::Min($all.Count, $maxItems)
    for ($i = 0; $i -lt $n; $i++) {
        $e = $all[$i]
        $name = $e.Current.Name
        if ($name.Length -gt 50) { $name = $name.Substring(0, 50) }
        $ct = $e.Current.ControlType.ProgrammaticName -replace 'ControlType\.', ''
        $cls = $e.Current.ClassName
        "  [{0,3}] Name='{1,-40}' CT={2,-15} Class={3}" -f $i, $name, $ct, $cls
    }
}

Write-Host "UAT helpers loaded. Evidence dir: $Script:EvidenceDir"
