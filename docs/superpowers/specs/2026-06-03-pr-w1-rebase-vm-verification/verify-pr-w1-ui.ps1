#requires -version 5
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms
Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class PrW1Win32 {
  [DllImport("user32.dll")] public static extern bool SetCursorPos(int X, int Y);
  [DllImport("user32.dll")] public static extern void mouse_event(uint f,uint dx,uint dy,uint d,UIntPtr e);
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h,int n);
  [DllImport("user32.dll")] public static extern bool MoveWindow(IntPtr h,int x,int y,int w,int ht,bool repaint);
  public static void Click(int x,int y){
    SetCursorPos(x,y); System.Threading.Thread.Sleep(80);
    mouse_event(0x0002,0,0,0,UIntPtr.Zero); System.Threading.Thread.Sleep(60);
    mouse_event(0x0004,0,0,0,UIntPtr.Zero);
  }
}
"@

$ErrorActionPreference = "Stop"
$AE = [System.Windows.Automation.AutomationElement]
$TS = [System.Windows.Automation.TreeScope]
$rootDir = (Resolve-Path (Join-Path $PSScriptRoot "..\..\..\..")).Path
$exe = Join-Path $rootDir "MetBench_Client\bin\Debug\net8.0-windows7.0\MetBench_Client.exe"
$script:win = $null
$script:scale = 1.0

function Prop($id, $value) {
    New-Object System.Windows.Automation.PropertyCondition($id, $value)
}

function Find-ByName($root, [string]$name, [int]$timeoutSec = 8) {
    $cond = Prop ([System.Windows.Automation.AutomationElement]::NameProperty) $name
    $deadline = (Get-Date).AddSeconds($timeoutSec)
    do {
        $el = $root.FindFirst($TS::Descendants, $cond)
        if ($el) { return $el }
        Start-Sleep -Milliseconds 250
    } while ((Get-Date) -lt $deadline)
    return $null
}

function Find-ById($root, [string]$id, [int]$timeoutSec = 8) {
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
    [PrW1Win32]::ShowWindow([IntPtr]$script:win.Current.NativeWindowHandle, 3) | Out-Null
    [PrW1Win32]::SetForegroundWindow([IntPtr]$script:win.Current.NativeWindowHandle) | Out-Null
    Start-Sleep -Milliseconds 250
}

function Click-Element($el) {
    if (-not $el) { return $false }
    $ip = $null
    if ($el.TryGetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern, [ref]$ip)) {
        $ip.Invoke()
        Start-Sleep -Milliseconds 300
        return $true
    }
    $sip = $null
    if ($el.TryGetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern, [ref]$sip)) {
        $sip.Select()
        Start-Sleep -Milliseconds 300
        return $true
    }
    Bring-Front
    $r = $el.Current.BoundingRectangle
    if ($r.Width -gt 0 -and $r.Height -gt 0) {
        [PrW1Win32]::Click([int](($r.X + $r.Width / 2) / $script:scale), [int](($r.Y + $r.Height / 2) / $script:scale))
        Start-Sleep -Milliseconds 500
        return $true
    }
    return $false
}

function Set-NavSearch([string]$text) {
    $editCond = Prop ([System.Windows.Automation.AutomationElement]::ControlTypeProperty) ([System.Windows.Automation.ControlType]::Edit)
    foreach ($edit in $script:win.FindAll($TS::Descendants, $editCond)) {
        $vp = $null
        if ($edit.TryGetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern, [ref]$vp)) {
            try {
                if (-not $vp.Current.IsReadOnly) {
                    $vp.SetValue($text)
                    Start-Sleep -Milliseconds 400
                    return $true
                }
            } catch {}
        }
    }
    return $false
}

function Scroll-NavDown {
    $btn = Find-ById $script:win "PART_ButtonScrollDown" 2
    if ($btn) {
        for ($i = 0; $i -lt 18; $i++) { [void](Click-Element $btn) }
    }
}

function Nav([string[]]$names) {
    foreach ($name in $names) {
        [void](Set-NavSearch $name)
        $item = Find-ByName $script:win $name 5
        if (-not $item) {
            [void](Set-NavSearch "")
            Scroll-NavDown
            $item = Find-ByName $script:win $name 3
        }
        if ($item -and (Click-Element $item)) {
            [void](Set-NavSearch "")
            Start-Sleep -Seconds 2
            return $true
        }
    }
    Write-Host ("[warn] nav not found: " + ($names -join " | "))
    [void](Set-NavSearch "")
    return $false
}

function Shot([string]$file) {
    Bring-Front
    $r = $script:win.Current.BoundingRectangle
    $x = [int]($r.X / $script:scale)
    $y = [int]($r.Y / $script:scale)
    $w = [int]($r.Width / $script:scale)
    $h = [int]($r.Height / $script:scale)
    $bmp = New-Object System.Drawing.Bitmap($w, $h)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen($x, $y, 0, 0, $bmp.Size)
    $path = Join-Path $PSScriptRoot $file
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose()
    $bmp.Dispose()
    Write-Host "[shot] $file"
}

function Click-WindowFraction([double]$fx, [double]$fy) {
    Bring-Front
    $r = $script:win.Current.BoundingRectangle
    [PrW1Win32]::Click([int](($r.X + ($r.Width * $fx)) / $script:scale), [int](($r.Y + ($r.Height * $fy)) / $script:scale))
    Start-Sleep -Seconds 2
}

function Select-SecondCultureAndApply {
    Scroll-NavDown
    $settings = Find-ByName $script:win "Settings" 4
    if (-not $settings) {
        [void](Nav @("Settings"))
    } else {
        [void](Click-Element $settings)
        Start-Sleep -Seconds 2
    }
    $comboCond = Prop ([System.Windows.Automation.AutomationElement]::ControlTypeProperty) ([System.Windows.Automation.ControlType]::ComboBox)
    $combos = $script:win.FindAll($TS::Descendants, $comboCond)
    if ($combos.Count -eq 0) { Write-Host "[warn] settings culture combobox not found"; return $false }
    $combo = $combos[$combos.Count - 1]
    $ecp = $null
    if ($combo.TryGetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern, [ref]$ecp)) {
        $ecp.Expand()
        Start-Sleep -Milliseconds 400
        $items = $combo.FindAll($TS::Descendants, (Prop ([System.Windows.Automation.AutomationElement]::ControlTypeProperty) ([System.Windows.Automation.ControlType]::ListItem)))
        if ($items.Count -lt 2) {
            $items = $script:win.FindAll($TS::Descendants, (Prop ([System.Windows.Automation.AutomationElement]::ControlTypeProperty) ([System.Windows.Automation.ControlType]::ListItem)))
        }
        if ($items.Count -ge 2) { [void](Click-Element $items[1]) }
        try { $ecp.Collapse() } catch {}
    }
    $apply = Find-ByName $script:win "Apply" 2
    if ($apply) { [void](Click-Element $apply); Start-Sleep -Seconds 1; return $true }
    Write-Host "[warn] apply button not found"
    return $false
}

if (-not (Test-Path $exe)) { throw "Client exe not found: $exe" }
$proc = Start-Process $exe -PassThru
Start-Sleep -Seconds 7
$winCond = Prop ([System.Windows.Automation.AutomationElement]::ProcessIdProperty) $proc.Id
for ($i = 0; $i -lt 30 -and -not $script:win; $i++) {
    $script:win = $AE::RootElement.FindFirst($TS::Children, $winCond)
    if (-not $script:win) { Start-Sleep -Milliseconds 500 }
}
if (-not $script:win) { throw "Main window not found for pid $($proc.Id)" }

$desktop = $AE::RootElement.Current.BoundingRectangle
$logical = [System.Windows.Forms.SystemInformation]::VirtualScreen
$script:scale = if ($logical.Width -gt 0) { $desktop.Width / $logical.Width } else { 1.0 }
[PrW1Win32]::MoveWindow([IntPtr]$script:win.Current.NativeWindowHandle, 0, 0, [Math]::Min(1700, [int]$desktop.Width), [Math]::Min(1000, [int]$desktop.Height), $true) | Out-Null
Bring-Front
Write-Host "[ok] launched $($script:win.Current.Name)"

if (Nav @("System MT")) {
    $run = Find-ById $script:win "Button_RunSystemMt" 8
    if ($run) {
        [void](Click-Element $run)
        Write-Host "[run] clicked Button_RunSystemMt"
        Start-Sleep -Seconds 15
    } else {
        Write-Host "[warn] Button_RunSystemMt not found"
    }
    Shot "03-system-mt-execution-page.png"
}

if (Select-SecondCultureAndApply) {
    Click-WindowFraction 0.07 0.475
    Shot "01-nav-systemmt-result-zh-cn.png"
}

Click-WindowFraction 0.07 0.475
if ($true) {
    [void](Click-Element (Find-ById $script:win "Button_RefreshSystemMtResults" 4))
    Start-Sleep -Seconds 3
    $grid = Find-ById $script:win "DataGrid_SystemMtResults" 4
    $rows = 0
    if ($grid) {
        $rowCond = Prop ([System.Windows.Automation.AutomationElement]::ControlTypeProperty) ([System.Windows.Automation.ControlType]::DataItem)
        $rows = $grid.FindAll($TS::Descendants, $rowCond).Count
    }
    $chart = Find-ById $script:win "Chart_SystemMtResult" 4
    $noData = Find-ById $script:win "TextBlock_SystemMtResultNoData" 1
    Write-Host "[result] rows=$rows chartFound=$([bool]$chart) noDataFound=$([bool]$noData)"
    Shot "02-systemmt-result-chart.png"
}

Click-WindowFraction 0.07 0.520
Shot "04-anomalies-page.png"
Click-WindowFraction 0.07 0.745
Shot "05-coverage-page.png"

try { Stop-Process -Id $proc.Id -Force } catch {}
Write-Host "[done] screenshots in $PSScriptRoot"
