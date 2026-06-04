#requires -version 5
# PR-5 UIA verification: drive the WPF client to the System MT catalog + execution-history
# pages, select rows, capture the explanation/profile/pair-quality surfaces, and toggle
# language. Screenshots land in this verification directory.
# Includes HiDPI / coordinate-scale handling for the Windows VM.
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms
Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class Win32Ui {
  [DllImport("user32.dll")] public static extern bool SetCursorPos(int X, int Y);
  [DllImport("user32.dll")] public static extern void mouse_event(uint f,uint dx,uint dy,uint d,UIntPtr e);
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h,int n);
  [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
  [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
  [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
  [DllImport("user32.dll")] public static extern bool AttachThreadInput(uint a, uint b, bool f);
  [DllImport("user32.dll")] public static extern bool MoveWindow(IntPtr h,int x,int y,int w,int ht,bool repaint);
  [DllImport("kernel32.dll")] public static extern uint GetCurrentThreadId();
  public static void Click(int x,int y){
    SetCursorPos(x,y); System.Threading.Thread.Sleep(80);
    mouse_event(0x0002,0,0,0,UIntPtr.Zero); System.Threading.Thread.Sleep(60);
    mouse_event(0x0004,0,0,0,UIntPtr.Zero);
  }
  public static void Wheel(int x,int y,int notches){
    SetCursorPos(x,y); System.Threading.Thread.Sleep(60);
    for (int i=0;i<System.Math.Abs(notches);i++){
      uint delta = notches < 0 ? unchecked((uint)(-120)) : (uint)120;
      mouse_event(0x0800,0,0,delta,UIntPtr.Zero); System.Threading.Thread.Sleep(40);
    }
  }
  public static void Front(IntPtr h){
    uint pid; uint fg = GetWindowThreadProcessId(GetForegroundWindow(), out pid);
    uint cur = GetCurrentThreadId();
    AttachThreadInput(cur, fg, true);
    ShowWindow(h,3); SetForegroundWindow(h);
    AttachThreadInput(cur, fg, false);
  }
}
"@
[void][Win32Ui]::SetProcessDPIAware()

$ErrorActionPreference = 'Stop'
$AE = [System.Windows.Automation.AutomationElement]
$TS = [System.Windows.Automation.TreeScope]
$shotDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$script:win = $null
$script:scale = 1.0

function Prop($id, $val) {
    New-Object System.Windows.Automation.PropertyCondition($id, $val)
}
function Find-ByName($root, $name, $timeoutSec = 10) {
    $cond = Prop ([System.Windows.Automation.AutomationElement]::NameProperty) $name
    $deadline = (Get-Date).AddSeconds($timeoutSec)
    do {
        $el = $root.FindFirst($TS::Descendants, $cond)
        if ($el) { return $el }
        Start-Sleep -Milliseconds 250
    } while ((Get-Date) -lt $deadline)
    return $null
}
function Find-ById($root, $id, $timeoutSec = 10) {
    $cond = Prop ([System.Windows.Automation.AutomationElement]::AutomationIdProperty) $id
    $deadline = (Get-Date).AddSeconds($timeoutSec)
    do {
        $el = $root.FindFirst($TS::Descendants, $cond)
        if ($el) { return $el }
        Start-Sleep -Milliseconds 250
    } while ((Get-Date) -lt $deadline)
    return $null
}
function Bring-Front {
    if ($script:win) { [Win32Ui]::Front([IntPtr]$script:win.Current.NativeWindowHandle); Start-Sleep -Milliseconds 250 }
}
function Scroll-NavDown {
    $btn = $script:win.FindFirst($TS::Descendants, (Prop ([System.Windows.Automation.AutomationElement]::AutomationIdProperty) 'PART_ButtonScrollDown'))
    if ($btn) {
        $ip = $null
        if ($btn.TryGetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern, [ref]$ip)) {
            for ($k = 0; $k -lt 14; $k++) { $ip.Invoke(); Start-Sleep -Milliseconds 100 }
        }
    }
    Start-Sleep -Milliseconds 250
}
function Set-NavSearch($text) {
    $editCond = Prop ([System.Windows.Automation.AutomationElement]::ControlTypeProperty) ([System.Windows.Automation.ControlType]::Edit)
    $edits = $script:win.FindAll($TS::Descendants, $editCond)
    foreach ($edit in $edits) {
        $vp = $null
        if ($edit.TryGetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern, [ref]$vp)) {
            try {
                if (-not $vp.Current.IsReadOnly) {
                    $vp.SetValue($text)
                    Start-Sleep -Milliseconds 350
                    return $true
                }
            } catch {}
        }
    }
    return $false
}
function Set-ValueById($id, $text) {
    $el = Find-ById $script:win $id 8
    if (-not $el) { return $false }
    $vp = $null
    if ($el.TryGetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern, [ref]$vp)) {
        try {
            if (-not $vp.Current.IsReadOnly) {
                $vp.SetValue($text)
                Start-Sleep -Milliseconds 350
                return $true
            }
        } catch {}
    }
    return $false
}
function Click-Element($el) {
    if (-not $el) { return $false }
    $ip = $null
    if ($el.TryGetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern, [ref]$ip)) { $ip.Invoke(); return $true }
    $sp = $null
    if ($el.TryGetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern, [ref]$sp)) { $sp.Select(); return $true }
    Bring-Front
    $r = $el.Current.BoundingRectangle
    if ($r.Width -gt 0 -and $r.Height -gt 0) {
        [Win32Ui]::Click([int](($r.X + $r.Width/2) / $script:scale), [int](($r.Y + $r.Height/2) / $script:scale))
        return $true
    }
    return $false
}
function Click-ElementMouse($el) {
    if (-not $el) { return $false }
    Bring-Front
    $r = $el.Current.BoundingRectangle
    if ($r.Width -gt 0 -and $r.Height -gt 0) {
        [Win32Ui]::Click([int](($r.X + $r.Width/2) / $script:scale), [int](($r.Y + $r.Height/2) / $script:scale))
        return $true
    }
    return $false
}
function Select-FirstRow($grid, $preferName = $null) {
    if (-not $grid) { return $false }
    # Prefer a row whose cell text matches $preferName, else the first DataItem.
    if ($preferName) {
        $hit = Find-ByName $grid $preferName 3
        if ($hit -and (Click-ElementMouse $hit)) { return $true }
    }
    $rows = $grid.FindAll($TS::Descendants, (Prop ([System.Windows.Automation.AutomationElement]::ControlTypeProperty) ([System.Windows.Automation.ControlType]::DataItem)))
    if ($rows.Count -gt 0) {
        return (Click-ElementMouse $rows[0])
    }
    return $false
}
function Scroll-CardIntoView($anchorId) {
    # The explanation/profile card is a <Border> (no UIA peer), so anchor the
    # scroll on a real control inside the same ScrollViewer (a button) and walk
    # up to the nearest vertically-scrollable ScrollViewer, scrolling it to 100%.
    $card = Find-ById $script:win $anchorId 6
    if (-not $card) { Write-Host ("    [scroll] anchor not found: " + $anchorId); return $false }
    $walker = [System.Windows.Automation.TreeWalker]::ControlViewWalker
    $node = $card
    for ($i = 0; $i -lt 14 -and $node; $i++) {
        $scp = $null
        $has = $node.TryGetCurrentPattern([System.Windows.Automation.ScrollPattern]::Pattern, [ref]$scp)
        $ct = $node.Current.ControlType.ProgrammaticName
        if ($has) {
            $vs = $scp.Current.VerticallyScrollable
            Write-Host ("    [scroll] anc[$i] $ct scrollable=$vs")
            try {
                if ($vs) {
                    $scp.SetScrollPercent([System.Windows.Automation.ScrollPattern]::NoScroll, 100)
                    Start-Sleep -Milliseconds 400
                    Write-Host ("    [scroll] set vPct=" + [int]$scp.Current.VerticalScrollPercent)
                    return $true
                }
            } catch { Write-Host ("    [scroll] err " + $_) }
        } else {
            Write-Host ("    [scroll] anc[$i] $ct (no scroll)")
        }
        $node = $walker.GetParent($node)
    }
    # Fallback: mouse wheel over the right-hand detail panel.
    Bring-Front
    $r = $script:win.Current.BoundingRectangle
    [Win32Ui]::Wheel([int](($r.X + $r.Width * 0.80) / $script:scale), [int](($r.Y + $r.Height * 0.55) / $script:scale), -12)
    Start-Sleep -Milliseconds 400
    return $false
}
function Select-ComboItem($comboId, $itemName) {
    $cb = Find-ById $script:win $comboId 6
    if (-not $cb) { return $false }
    $ecp = $null
    if ($cb.TryGetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern, [ref]$ecp)) {
        $ecp.Expand(); Start-Sleep -Milliseconds 400
    }
    $it = $script:win.FindFirst($TS::Descendants, (Prop ([System.Windows.Automation.AutomationElement]::NameProperty) $itemName))
    $ok = $false
    if ($it) {
        $sip = $null
        if ($it.TryGetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern, [ref]$sip)) { $sip.Select(); $ok = $true }
        else { $ok = Click-Element $it }
    }
    try { if ($ecp) { $ecp.Collapse() } } catch {}
    Start-Sleep -Milliseconds 500
    return $ok
}
function Nav($name) {
    [void](Set-NavSearch $name)
    $item = Find-ByName $script:win $name 8
    if (-not $item) {
        [void](Set-NavSearch '')
        $item = Find-ByName $script:win $name 8
    }
    if (-not $item) { Write-Host ("  [warn] nav item not found: " + $name); return $false }
    $ok = Click-ElementMouse $item
    Start-Sleep -Seconds 2
    [void](Set-NavSearch '')
    return $ok
}
function Wait-Page($automationId, $timeoutSec = 8) {
    $page = Find-ById $script:win $automationId $timeoutSec
    if (-not $page) { Write-Host ("  [warn] page not reached: " + $automationId); return $false }
    return $true
}
function Shot($file) {
    try {
        $r = $script:win.Current.BoundingRectangle
        if ($r.Width -lt 1) { return }
        $s = $script:scale
        $x = [int]($r.X / $s); $y = [int]($r.Y / $s)
        $w = [int]($r.Width / $s); $h = [int]($r.Height / $s)
        $bmp = New-Object System.Drawing.Bitmap($w, $h)
        $g = [System.Drawing.Graphics]::FromImage($bmp)
        $g.CopyFromScreen($x, $y, 0, 0, $bmp.Size)
        $bmp.Save((Join-Path $shotDir $file), [System.Drawing.Imaging.ImageFormat]::Png)
        $g.Dispose(); $bmp.Dispose()
        Write-Host ("  [shot] " + $file)
    } catch { Write-Host ("  [shot-fail] " + $file + " : " + $_) }
}
function Set-Culture($itemName, $confirmNav) {
    Scroll-NavDown
    if (-not (Click-ElementMouse (Find-ByName $script:win 'Settings' 4))) {
        [void](Click-ElementMouse (Find-ByName $script:win '设置' 4))
    }
    Start-Sleep -Seconds 2
    $comboCond = Prop ([System.Windows.Automation.AutomationElement]::ControlTypeProperty) ([System.Windows.Automation.ControlType]::ComboBox)
    $nameCond  = Prop ([System.Windows.Automation.AutomationElement]::NameProperty) $itemName
    $deadline = (Get-Date).AddSeconds(10); $done = $false
    do {
        foreach ($cb in $script:win.FindAll($TS::Descendants, $comboCond)) {
            $ecp = $null
            if (-not $cb.TryGetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern, [ref]$ecp)) { continue }
            $ecp.Expand(); Start-Sleep -Milliseconds 400
            $it = $cb.FindFirst($TS::Descendants, $nameCond)
            if (-not $it) { $it = $script:win.FindFirst($TS::Descendants, $nameCond) }
            if ($it) {
                $sip = $null
                if ($it.TryGetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern, [ref]$sip)) { $sip.Select() } else { [void](Click-Element $it) }
                Start-Sleep -Milliseconds 250; try { $ecp.Collapse() } catch {}
                $done = $true; break
            }
            try { $ecp.Collapse() } catch {}
        }
        if (-not $done) { Start-Sleep -Milliseconds 400 }
    } while (-not $done -and (Get-Date) -lt $deadline)
    if (-not $done) { Write-Host ("  [warn] culture item not found: " + $itemName); return $false }
    $apply = Find-ByName $script:win 'Apply' 3
    if (-not $apply) { $apply = Find-ByName $script:win '应用' 3 }
    [void](Click-Element $apply)
    Start-Sleep -Seconds 1
    return $true
}

# --- launch ---
$exe = "C:\MetBench-V2.1.4_2\MetBench_Client\bin\Debug\net8.0-windows7.0\MetBench_Client.exe"
$proc = Start-Process $exe -PassThru
Start-Sleep -Seconds 7
$winCond = Prop ([System.Windows.Automation.AutomationElement]::ProcessIdProperty) $proc.Id
for ($i=0; $i -lt 20 -and -not $script:win; $i++) {
    $script:win = $AE::RootElement.FindFirst($TS::Children, $winCond)
    if (-not $script:win) { Start-Sleep -Milliseconds 500 }
}
if (-not $script:win) { throw "Main window not found." }
Bring-Front
# Maximize via WindowPattern + force full-screen size so the detail panel + card fit.
$wp = $null
if ($script:win.TryGetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern, [ref]$wp)) {
    try { $wp.SetWindowVisualState([System.Windows.Automation.WindowVisualState]::Maximized) } catch {}
    Start-Sleep -Milliseconds 400
}
# Physical desktop dims (UIA root) used for full-size nav vs. short-size capture.
$script:deskW = [int]$AE::RootElement.Current.BoundingRectangle.Width
$script:deskH = [int]$AE::RootElement.Current.BoundingRectangle.Height
function Resize-Window($h) {
    [void][Win32Ui]::MoveWindow([IntPtr]$script:win.Current.NativeWindowHandle, 0, 0, [Math]::Max(1600, $script:deskW - 40), $h, $true)
    Start-Sleep -Milliseconds 700
}
# Full-size so all nav items are reachable and pages lay out normally.
Resize-Window ([Math]::Max(900, $script:deskH - 60))
$physW = $AE::RootElement.Current.BoundingRectangle.Width
$logW  = [System.Windows.Forms.SystemInformation]::VirtualScreen.Width
$script:scale = if ($logW -gt 0) { $physW / $logW } else { 1.0 }
Write-Host ("[ok] window '" + $script:win.Current.Name + "' scale=" + [math]::Round($script:scale,3))

# --- 0) Produce one real pure-stdlib System MT execution with PR-3 pair-quality evidence ---
if ((Nav 'System MT') -and (Wait-Page 'Button_RunSystemMt')) {
    Start-Sleep -Seconds 2
    $run = Find-ById $script:win 'Button_RunSystemMt' 8
    if ($run -and (Click-Element $run)) {
        Write-Host "  [run] started default pure-stdlib System MT scenario"
        Start-Sleep -Seconds 12
    } else {
        Write-Host "  [warn] run button not found; history may not contain fresh pair-quality evidence"
    }
}

# --- 1) Equation Catalog (en) ---
if ((Nav 'System MT Equation Catalog') -and (Wait-Page 'DataGrid_SystemMtEquations')) {
    [void](Select-FirstRow (Find-ById $script:win 'DataGrid_SystemMtEquations' 8) 'bateman')
    Start-Sleep -Seconds 1
    if (Find-ById $script:win 'Border_EquationExplanationCard' 5) { Write-Host "  [ok] equation explanation card present" }
    Shot '01-equation-explanation-card.png'
}

# --- 2) SUT Catalog (en) ---
if ((Nav 'System MT SUT Catalog') -and (Wait-Page 'DataGrid_SystemMtSuts')) {
    [void](Select-FirstRow (Find-ById $script:win 'DataGrid_SystemMtSuts' 8) 'heat_equation')
    Start-Sleep -Seconds 1
    Shot '02-sut-profile-card.png'
}

# --- 3) MR Catalog (en) — manifest + first MR auto-select on load ---
if ((Nav 'System MT MR Catalog') -and (Wait-Page 'ComboBox_SystemMtManifest')) {
    Start-Sleep -Seconds 1
    # Prefer a physics MR manifest over the synthetic _test_csv default.
    [void](Select-ComboItem 'ComboBox_SystemMtManifest' 'heat_equation')
    Start-Sleep -Seconds 1
    [void](Select-FirstRow (Find-ById $script:win 'DataGrid_SystemMtMrBindings' 8))
    Start-Sleep -Seconds 1
    Shot '03-mr-explanation-card.png'
}

# --- 4/5) Execution History (en) — probe rows for pair-quality vs empty ---
if ((Nav 'System MT Execution History') -and (Wait-Page 'DataGrid_SystemMtExecutionHistory')) {
    Start-Sleep -Seconds 1
    $grid = Find-ById $script:win 'DataGrid_SystemMtExecutionHistory' 8
    $rows = @()
    if ($grid) { $rows = $grid.FindAll($TS::Descendants, (Prop ([System.Windows.Automation.AutomationElement]::ControlTypeProperty) ([System.Windows.Automation.ControlType]::DataItem))) }
    Write-Host ("  [info] execution-history rows: " + $rows.Count)
    $pairShot = $false
    for ($i = 0; $i -lt [Math]::Min($rows.Count, 12); $i++) {
        $sp = $null
        if ($rows[$i].TryGetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern, [ref]$sp)) { $sp.Select() } else { [void](Click-Element $rows[$i]) }
        Start-Sleep -Milliseconds 700
        $tb = Find-ById $script:win 'TextBox_EvidenceDetails' 4
        $txt = ''
        if ($tb) {
            $vp = $null
            if ($tb.TryGetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern, [ref]$vp)) { $txt = $vp.Current.Value }
        }
        if (-not $pairShot -and ($txt -match 'Pair quality')) {
            Write-Host ("  [ok] row " + $i + " has pair-quality evidence")
            Shot '04-execution-history-non-empty-pair-quality.png'; $pairShot = $true
        }
        if ($pairShot) { break }
    }
    [void](Set-ValueById 'TextBox_MrNameFilter' '__no_evidence_or_empty_pair_quality__')
    [void](Click-Element (Find-ById $script:win 'Button_RefreshSystemMtExecutionHistory' 4))
    Start-Sleep -Seconds 1
    Shot '05-execution-history-no-evidence-or-empty-pair-quality.png'
    if (-not $pairShot) { Write-Host "  [BLOCKER] no execution-history row exposed non-empty pair-quality evidence" }
}

# --- 6) zh-CN equation surface ---
if (Set-Culture '中文') {
    if ((Nav '系统级方程目录') -and (Wait-Page 'DataGrid_SystemMtEquations')) {
        [void](Select-FirstRow (Find-ById $script:win 'DataGrid_SystemMtEquations' 8) 'bateman')
        Start-Sleep -Seconds 1
        Shot '06-zh-cn-equation-or-history.png'
    }
}

# --- 7) back to en-US equation surface ---
if (Set-Culture 'English') {
    if ((Nav 'System MT Equation Catalog') -and (Wait-Page 'DataGrid_SystemMtEquations')) {
        [void](Select-FirstRow (Find-ById $script:win 'DataGrid_SystemMtEquations' 8) 'bateman')
        Start-Sleep -Seconds 1
        Shot '07-en-us-equation-or-history.png'
    }
}

Write-Host "[done] PR-5 verification drive complete."
try { $proc | Stop-Process -Force } catch {}
