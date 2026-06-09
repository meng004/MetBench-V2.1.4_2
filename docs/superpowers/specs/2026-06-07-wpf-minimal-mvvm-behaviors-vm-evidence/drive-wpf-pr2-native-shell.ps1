#requires -version 5
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms
Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class WpfPr2NativeShellWin32 {
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern void mouse_event(uint f,uint dx,uint dy,uint data,UIntPtr extra);
  [DllImport("user32.dll")] public static extern bool SetCursorPos(int x,int y);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h,int n);
  [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
  public static void Click(int x,int y) {
    SetCursorPos(x,y); System.Threading.Thread.Sleep(80);
    mouse_event(0x0002,0,0,0,UIntPtr.Zero); System.Threading.Thread.Sleep(60);
    mouse_event(0x0004,0,0,0,UIntPtr.Zero);
  }
}
"@
[void][WpfPr2NativeShellWin32]::SetProcessDPIAware()

$ErrorActionPreference = 'Stop'
$AE = [System.Windows.Automation.AutomationElement]
$TS = [System.Windows.Automation.TreeScope]
$shotDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repo = Resolve-Path (Join-Path $shotDir '..\..\..\..')
$script:win = $null
$script:proc = $null
$script:scale = 1.0

function Prop($id, $value) {
    New-Object System.Windows.Automation.PropertyCondition($id, $value)
}

function Bring-Front {
    [void][WpfPr2NativeShellWin32]::ShowWindow([IntPtr]$script:win.Current.NativeWindowHandle, 3)
    [void][WpfPr2NativeShellWin32]::SetForegroundWindow([IntPtr]$script:win.Current.NativeWindowHandle)
    Start-Sleep -Milliseconds 250
}

function Click-Element($el) {
    $ip = $null
    if ($el.TryGetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern, [ref]$ip)) {
        $ip.Invoke()
        return
    }

    $sp = $null
    if ($el.TryGetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern, [ref]$sp)) {
        $sp.Select()
        return
    }

    Bring-Front
    $r = $el.Current.BoundingRectangle
    if ($r.Width -le 0 -or $r.Height -le 0) {
        throw "Element has no clickable bounds: $($el.Current.Name)"
    }

    [WpfPr2NativeShellWin32]::Click(
        [int](($r.X + $r.Width / 2) / $script:scale),
        [int](($r.Y + $r.Height / 2) / $script:scale))
}

function Click-Nav($name) {
    $cond = Prop ([System.Windows.Automation.AutomationElement]::NameProperty) $name
    $text = $script:win.FindFirst($TS::Descendants, $cond)
    if (-not $text) {
        throw "Navigation text not found: $name"
    }

    $node = $text
    $walker = [System.Windows.Automation.TreeWalker]::ControlViewWalker
    for ($i = 0; $i -lt 8 -and $node; $i++) {
        try {
            Click-Element $node
            Start-Sleep -Milliseconds 900
            return
        } catch {
            $node = $walker.GetParent($node)
        }
    }

    throw "Navigation item not clickable: $name"
}

function Shot-Window($file) {
    Bring-Front
    $r = $script:win.Current.BoundingRectangle
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

Push-Location $repo
try {
    foreach ($name in @(
        '01-main-window-startup.png',
        '02-mt-execution-native-page.png',
        '03-system-mt-equation-catalog-native-page.png',
        '04-settings-native-page.png',
        'failure-pr2-native-shell.txt')) {
        $path = Join-Path $shotDir $name
        if (Test-Path -LiteralPath $path) {
            Remove-Item -LiteralPath $path -Force
        }
    }

    $exe = Join-Path $repo 'MetBench_Client\bin\Debug\net8.0-windows7.0\MetBench_Client.exe'
    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = $exe
    $psi.WorkingDirectory = Split-Path $exe -Parent
    $psi.UseShellExecute = $false
    $script:proc = [System.Diagnostics.Process]::Start($psi)

    Start-Sleep -Seconds 7
    $winCond = Prop ([System.Windows.Automation.AutomationElement]::ProcessIdProperty) $script:proc.Id
    for ($i = 0; $i -lt 30 -and -not $script:win; $i++) {
        $script:win = $AE::RootElement.FindFirst($TS::Children, $winCond)
        if (-not $script:win) { Start-Sleep -Milliseconds 500 }
    }
    if (-not $script:win) { throw "Main window not found." }

    $physW = $AE::RootElement.Current.BoundingRectangle.Width
    $logW = [System.Windows.Forms.SystemInformation]::VirtualScreen.Width
    $script:scale = if ($logW -gt 0) { $physW / $logW } else { 1.0 }

    Shot-Window '01-main-window-startup.png'
    Click-Nav 'MT Execution'
    Shot-Window '02-mt-execution-native-page.png'
    Click-Nav 'System MT Equation Catalog'
    Shot-Window '03-system-mt-equation-catalog-native-page.png'
    Click-Nav 'Settings'
    Shot-Window '04-settings-native-page.png'
}
catch {
    Set-Content -Path (Join-Path $shotDir 'failure-pr2-native-shell.txt') -Value ($_ | Out-String) -Encoding UTF8
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
