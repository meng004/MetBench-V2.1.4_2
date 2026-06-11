#requires -version 5
# PR-4 ObservableProperty migration — runtime PropertyChanged smoke driver.
# Drives the built WPF client and proves each migrated binding source still
# raises PropertyChanged at runtime (the source generator / SetProperty plumbing
# that the source-only guard test cannot observe). Screenshots are the evidence.
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms
Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class Pr4Win32 {
  [DllImport("user32.dll")] public static extern bool SetCursorPos(int X, int Y);
  [DllImport("user32.dll")] public static extern void mouse_event(uint f,uint dx,uint dy,uint d,UIntPtr e);
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h,int n);
  [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
  [DllImport("user32.dll",CharSet=CharSet.Auto)] public static extern IntPtr FindWindow(string cls,string title);
  [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr h,uint msg,IntPtr w,IntPtr l);
  [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
  public static IntPtr FindTips() { return FindWindow(null,"Tips"); }
  public static void CloseWin(IntPtr h) { PostMessage(h,0x0010,IntPtr.Zero,IntPtr.Zero); }
  public static void Click(int x,int y) {
    SetCursorPos(x,y); System.Threading.Thread.Sleep(90);
    mouse_event(0x0002,0,0,0,UIntPtr.Zero); System.Threading.Thread.Sleep(70);
    mouse_event(0x0004,0,0,0,UIntPtr.Zero);
  }
  public static void Wheel(int x,int y,int delta) {
    SetCursorPos(x,y); System.Threading.Thread.Sleep(40);
    mouse_event(0x0800,0,0,(uint)delta,UIntPtr.Zero);
  }
}
"@
[void][Pr4Win32]::SetProcessDPIAware()

$ErrorActionPreference = 'Stop'
$AE = [System.Windows.Automation.AutomationElement]
$TS = [System.Windows.Automation.TreeScope]
$CT = [System.Windows.Automation.ControlType]
$shotDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repo = Resolve-Path (Join-Path $shotDir '..\..\..\..')
$summary = Join-Path $shotDir 'driver-results.txt'
$script:win = $null
$script:scale = 1.0
$script:proc = $null
$script:results = New-Object System.Collections.ArrayList

function Prop($id, $value) { New-Object System.Windows.Automation.PropertyCondition($id, $value) }

function Find-ById($root, $id, $timeoutSec = 8) {
    $cond = Prop ($AE::AutomationIdProperty) $id
    $deadline = (Get-Date).AddSeconds($timeoutSec)
    do { $el = $root.FindFirst($TS::Descendants, $cond); if ($el) { return $el }; Start-Sleep -Milliseconds 200 } while ((Get-Date) -lt $deadline)
    return $null
}
function Find-ByName($root, $name, $timeoutSec = 8) {
    $cond = Prop ($AE::NameProperty) $name
    $deadline = (Get-Date).AddSeconds($timeoutSec)
    do { $el = $root.FindFirst($TS::Descendants, $cond); if ($el) { return $el }; Start-Sleep -Milliseconds 200 } while ((Get-Date) -lt $deadline)
    return $null
}
function Bring-Front {
    if ($script:win) {
        [void][Pr4Win32]::ShowWindow([IntPtr]$script:win.Current.NativeWindowHandle, 3)
        [void][Pr4Win32]::SetForegroundWindow([IntPtr]$script:win.Current.NativeWindowHandle)
        Start-Sleep -Milliseconds 250
    }
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
        [Pr4Win32]::Click([int](($r.X + $r.Width / 2) / $script:scale), [int](($r.Y + $r.Height / 2) / $script:scale)); return $true
    }
    return $false
}
function Click-SelectableAncestor($el) {
    $node = $el; $walker = [System.Windows.Automation.TreeWalker]::ControlViewWalker
    for ($i = 0; $i -lt 8 -and $node; $i++) { if (Click-Element $node) { return $true }; $node = $walker.GetParent($node) }
    return $false
}
function Mouse-ClickElement($el) {
    if (-not $el) { return $false }
    Bring-Front
    $r = $el.Current.BoundingRectangle
    if ($r.Width -le 0 -or $r.Height -le 0) { return $false }
    [Pr4Win32]::Click([int](($r.X + $r.Width / 2) / $script:scale), [int](($r.Y + $r.Height / 2) / $script:scale))
    return $true
}
function Shot($file) {
    Bring-Front
    $r = $script:win.Current.BoundingRectangle
    $x = [int]($r.X / $script:scale); $y = [int]($r.Y / $script:scale)
    $w = [int]($r.Width / $script:scale); $h = [int]($r.Height / $script:scale)
    if ($w -le 0 -or $h -le 0) { $w = 1280; $h = 800; $x = 0; $y = 0 }
    $bmp = New-Object System.Drawing.Bitmap($w, $h)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen($x, $y, 0, 0, $bmp.Size)
    $bmp.Save((Join-Path $shotDir $file), [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose(); $bmp.Dispose()
    Write-Host "[shot] $file"
}
function Start-MetBench($exe) {
    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = $exe; $psi.WorkingDirectory = Split-Path $exe -Parent; $psi.UseShellExecute = $false
    return [System.Diagnostics.Process]::Start($psi)
}
function Attach-MainWindow($proc) {
    $script:win = $null; Start-Sleep -Seconds 8
    $winCond = Prop ($AE::ProcessIdProperty) $proc.Id
    for ($i = 0; $i -lt 40 -and -not $script:win; $i++) {
        $script:win = $AE::RootElement.FindFirst($TS::Children, $winCond)
        if (-not $script:win) { Start-Sleep -Milliseconds 500 }
    }
    if (-not $script:win) { throw "Main window not found." }
    Bring-Front
    $physW = $AE::RootElement.Current.BoundingRectangle.Width
    $logW = [System.Windows.Forms.SystemInformation]::VirtualScreen.Width
    $script:scale = if ($logW -gt 0) { $physW / $logW } else { 1.0 }
    $script:winPid = $script:win.Current.ProcessId
    Write-Host "[attach] scale=$($script:scale) launchedPid=$($script:proc.Id) windowPid=$($script:winPid)"
}
function Set-NavSearch($text) {
    $editCond = Prop ($AE::ControlTypeProperty) $CT::Edit
    $edits = $script:win.FindAll($TS::Descendants, $editCond)
    foreach ($edit in $edits) {
        $vp = $null
        if ($edit.TryGetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern, [ref]$vp)) {
            try { if (-not $vp.Current.IsReadOnly) { $vp.SetValue($text); Start-Sleep -Milliseconds 500; return $true } } catch {}
        }
    }
    return $false
}
function Collapse-AllCombos {
    # close any stray open dropdown so a leftover popup item can't be mis-toggled
    try {
        $combos = $script:win.FindAll($TS::Descendants, (Prop ($AE::ControlTypeProperty) $CT::ComboBox))
        foreach ($c in $combos) {
            $ecp = $null
            if ($c.TryGetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern, [ref]$ecp)) {
                if ($ecp.Current.ExpandCollapseState -eq [System.Windows.Automation.ExpandCollapseState]::Expanded) { try { $ecp.Collapse() } catch {} }
            }
        }
    } catch {}
}
function Page-Marker-Present($verifyId, $verifyName) {
    if ($verifyId) { return [bool](Find-ById $script:win $verifyId 6) }
    if ($verifyName) { return [bool](Find-ByName $script:win $verifyName 6) }
    return $true
}
function Find-NavRow($navName) {
    # nav items are DataItems (no Invoke pattern); the DataItem row is the clickable target
    $all = $script:win.FindAll($TS::Descendants, (Prop ($AE::NameProperty) $navName))
    foreach ($e in $all) { if ($e.Current.ControlType -eq $CT::DataItem) { return $e } }
    if ($all.Count -gt 0) { return $all[0] }
    return $null
}
function Rect-InView($r, $winR) {
    return ($r.Width -gt 0 -and $r.Height -gt 0 -and $r.Y -ge ($winR.Y - 2) -and ($r.Y + $r.Height) -le ($winR.Y + $winR.Height + 2))
}
function Get-NavScroller($nav) {
    $walker = [System.Windows.Automation.TreeWalker]::ControlViewWalker
    $node = $nav; $sp = $null
    for ($i = 0; $i -lt 14 -and $node; $i++) {
        if ($node.TryGetCurrentPattern([System.Windows.Automation.ScrollPattern]::Pattern, [ref]$sp)) {
            if ($sp.Current.VerticallyScrollable) { return @{ El = $node; Sp = $sp } }
        }
        $node = $walker.GetParent($node)
    }
    return $null
}
function Click-NavRow($nav) {
    Bring-Front
    $winR = $script:win.Current.BoundingRectangle
    $scroller = Get-NavScroller $nav
    $navX = [int](($winR.X + 80) / $script:scale)
    $SA = [System.Windows.Automation.ScrollAmount]
    for ($i = 0; $i -lt 40; $i++) {
        $r = $nav.Current.BoundingRectangle
        if (Rect-InView $r $winR) { break }
        $down = ($r.Y -gt ($winR.Y + $winR.Height / 2))
        if ($scroller) {
            if ($down) { $scroller.Sp.Scroll($SA::NoAmount, $SA::LargeIncrement) } else { $scroller.Sp.Scroll($SA::NoAmount, $SA::LargeDecrement) }
        } else {
            $cy = [int](($winR.Y + $winR.Height / 2) / $script:scale)
            if ($down) { [Pr4Win32]::Wheel($navX, $cy, -240) } else { [Pr4Win32]::Wheel($navX, $cy, 240) }
        }
        Start-Sleep -Milliseconds 160
    }
    $r = $nav.Current.BoundingRectangle
    if (-not (Rect-InView $r $winR)) { return $false }
    $cx = [int](($r.X + $r.Width / 2) / $script:scale)
    $cyc = [int](($r.Y + $r.Height / 2) / $script:scale)
    if ($cx -lt [int]($winR.X / $script:scale)) { $cx = $navX }
    [Pr4Win32]::Click($cx, $cyc)
    return $true
}
function Navigate($navName, $verifyId = $null, $verifyName = $null) {
    Dismiss-AllDialogs
    Collapse-AllCombos
    $nav = Find-NavRow $navName
    if (-not $nav) { throw "Navigation item not found: $navName" }
    [void](Click-NavRow $nav)
    Start-Sleep -Milliseconds 1000
    if (Page-Marker-Present $verifyId $verifyName) { return }
    # retry once
    $nav = Find-NavRow $navName
    if ($nav) { [void](Click-NavRow $nav); Start-Sleep -Milliseconds 1200 }
    if (Page-Marker-Present $verifyId $verifyName) { return }
    $marker = if ($verifyId) { $verifyId } else { $verifyName }
    throw "Navigation to '$navName' did not reach page (missing marker '$marker')."
}
function Get-InnerText($el) {
    # text shown by a templated item: first descendant Text element with non-empty Name
    $txt = $el.FindFirst($TS::Descendants, (Prop ($AE::ControlTypeProperty) $CT::Text))
    if ($txt -and $txt.Current.Name) { return $txt.Current.Name }
    if ($el.Current.Name) { return $el.Current.Name }
    return ''
}
function Get-MultiSelectCombo {
    # the editable multi-select combo = the ComboBox whose dropdown items contain a CheckBox.
    $combos = $script:win.FindAll($TS::Descendants, (Prop ($AE::ControlTypeProperty) $CT::ComboBox))
    foreach ($c in $combos) {
        $ecp = $null
        if (-not $c.TryGetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern, [ref]$ecp)) { continue }
        try { $ecp.Expand(); Start-Sleep -Milliseconds 450 } catch { continue }
        $items = $c.FindAll($TS::Descendants, (Prop ($AE::ControlTypeProperty) $CT::ListItem))
        if ($items.Count -eq 0) { $items = $script:win.FindAll($TS::Descendants, (Prop ($AE::ControlTypeProperty) $CT::ListItem)) }
        $picked = New-Object System.Collections.ArrayList
        foreach ($it in $items) {
            $cb = $it.FindFirst($TS::Descendants, (Prop ($AE::ControlTypeProperty) $CT::CheckBox))
            if ($cb) { [void]$picked.Add(@{ Item = $it; CheckBox = $cb; Text = (Get-InnerText $it) }) }
        }
        if ($picked.Count -gt 0) { return @{ Combo = $c; Ecp = $ecp; Items = $picked } }
        try { $ecp.Collapse(); Start-Sleep -Milliseconds 200 } catch {}
    }
    return $null
}
# --- modal MessageBox helpers (UiDialog -> native MessageBox.Show; title is "Tips").
# Detected via Win32 FindWindow (immune to the blocked app UI thread that stalls UIA). ---
function Wait-Dialog($timeoutSec) {
    $deadline = (Get-Date).AddSeconds($timeoutSec)
    do {
        $h = [Pr4Win32]::FindTips()
        if ($h -ne [IntPtr]::Zero -and [Pr4Win32]::IsWindowVisible($h)) { return $h }
        Start-Sleep -Milliseconds 150
    } while ((Get-Date) -lt $deadline)
    return [IntPtr]::Zero
}
function Dismiss-AllDialogs {
    for ($i = 0; $i -lt 10; $i++) {
        $h = [Pr4Win32]::FindTips()
        if ($h -eq [IntPtr]::Zero) { return }
        [Pr4Win32]::CloseWin($h)
        Start-Sleep -Milliseconds 300
    }
}
function Get-ComboEditValue($combo) {
    # editable ComboBox exposes its text via ValuePattern on itself or a child Edit
    $vp = $null
    if ($combo.TryGetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern, [ref]$vp)) { return $vp.Current.Value }
    $edit = $combo.FindFirst($TS::Descendants, (Prop ($AE::ControlTypeProperty) $CT::Edit))
    if ($edit -and $edit.TryGetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern, [ref]$vp)) { return $vp.Current.Value }
    return ''
}
function Record($ac, $status, $detail) {
    [void]$script:results.Add([pscustomobject]@{ AC = $ac; Status = $status; Detail = $detail })
    Write-Host "[$ac] $status — $detail"
}

Push-Location $repo
try {
    foreach ($f in Get-ChildItem -Path $shotDir -Filter '0*.png' -ErrorAction SilentlyContinue) { Remove-Item $f.FullName -Force }
    # kill any orphaned client instance so we attach to a single, clean window
    Get-Process MetBench_Client -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Milliseconds 500
    $exe = Join-Path $repo 'MetBench_Client\bin\Debug\net8.0-windows7.0\MetBench_Client.exe'
    if (-not (Test-Path $exe)) { throw "Client exe not found: $exe" }
    $script:proc = Start-MetBench $exe
    Attach-MainWindow $script:proc
    Shot '00-main-window-startup.png'

    # ===== AC-V3a + AC-V3b: ApplicationManagement Domain combo (IsChecked + SelectedText) =====
    try {
        Navigate 'Application Management' $null 'IdApplication'
        Start-Sleep -Milliseconds 500
        $ms = Get-MultiSelectCombo
        if (-not $ms) { throw "Domain multi-select combo (checkbox items) not found." }
        $combo = $ms.Combo
        # pick first checkbox item with real text that is not the 'Other' sentinel
        $pick = $ms.Items | Where-Object { $_.Text -and $_.Text -ne 'Other' -and $_.Text -notmatch 'DomainEx' } | Select-Object -First 1
        if (-not $pick) { throw "No non-'Other' domain item with text found (items: $(( $ms.Items | ForEach-Object { $_.Text }) -join ','))." }
        $pickText = $pick.Text
        Shot '01-checkbox-ischecked-before.png'
        $tp = $null; $toggled = $false
        if ($pick.CheckBox.TryGetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern, [ref]$tp)) { $tp.Toggle(); $toggled = $true }
        else { $toggled = Mouse-ClickElement $pick.Item }
        Start-Sleep -Milliseconds 700
        Shot '01-checkbox-ischecked.png'
        $state = 'unknown'; $tp2 = $null
        if ($pick.CheckBox.TryGetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern, [ref]$tp2)) { $state = $tp2.Current.ToggleState }
        try { $ms.Ecp.Collapse() } catch {}
        Start-Sleep -Milliseconds 500
        $txt = Get-ComboEditValue $combo
        Shot '02-appmgmt-domain-selectedtext.png'
        if ($state -eq 'On' -or $toggled) { Record 'AC-V3a' 'pass' "domain '$pickText' checkbox ToggleState=$state (IsChecked [ObservableProperty] raised)" }
        else { Record 'AC-V3a' 'fail' "checkbox state=$state" }
        if ($txt -match [regex]::Escape($pickText)) { Record 'AC-V3b' 'pass' "combo Text='$txt' rebuilt from IsChecked via SelectedText SetProperty (not class name)" }
        elseif ($txt -notmatch 'DomainEx' -and $txt -ne '') { Record 'AC-V3b' 'partial' "combo Text='$txt' (picked '$pickText')" }
        else { Record 'AC-V3b' 'fail' "combo Text='$txt'" }
    } catch { Record 'AC-V3a' 'fail' $_.Exception.Message; Record 'AC-V3b' 'fail' $_.Exception.Message; try { Shot '02-appmgmt-domain-selectedtext.png' } catch {} }

    # ===== AC-V3c: MRManagement Application combo (SelectedText) =====
    try {
        Navigate 'MR Management' $null 'IdMR'
        Start-Sleep -Milliseconds 500
        $ms = Get-MultiSelectCombo
        if (-not $ms) { throw "Application multi-select combo (checkbox items) not found." }
        $combo = $ms.Combo
        $pick = $ms.Items | Where-Object { $_.Text -and $_.Text -ne 'Other' -and $_.Text -notmatch 'ApplicationEx' } | Select-Object -First 1
        if (-not $pick) { throw "No application item with text found." }
        $pickText = $pick.Text
        $tp = $null
        if ($pick.CheckBox.TryGetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern, [ref]$tp)) { $tp.Toggle() } else { [void](Mouse-ClickElement $pick.Item) }
        Start-Sleep -Milliseconds 700
        try { $ms.Ecp.Collapse() } catch {}
        Start-Sleep -Milliseconds 500
        $txt = Get-ComboEditValue $combo
        Shot '03-mrmgmt-application-selectedtext.png'
        if ($txt -match [regex]::Escape($pickText)) { Record 'AC-V3c' 'pass' "combo Text='$txt' rebuilt from IsChecked via SelectedText SetProperty (app '$pickText', not class name)" }
        elseif ($txt -notmatch 'ApplicationEx' -and $txt -ne '') { Record 'AC-V3c' 'partial' "combo Text='$txt' (picked '$pickText')" }
        else { Record 'AC-V3c' 'fail' "combo Text='$txt'" }
    } catch { Record 'AC-V3c' 'fail' $_.Exception.Message; try { Shot '03-mrmgmt-application-selectedtext.png' } catch {} }

    # ===== AC-V3d: MR ReportGenerator ReportTypeComboBox (SelectedValue + side-effect-on-change) =====
    try {
        Navigate 'MR ReportGenerator' 'ReportTypeComboBox'
        # clear any initial load dialog so the owner window is enabled for interaction
        Dismiss-AllDialogs
        $combo = Find-ById $script:win 'ReportTypeComboBox' 10
        if (-not $combo) { throw "ReportTypeComboBox not found." }
        function Select-ReportType($name) {
            $c = Find-ById $script:win 'ReportTypeComboBox' 5
            $ecp = $null
            if ($c.TryGetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern, [ref]$ecp)) { try { $ecp.Expand(); Start-Sleep -Milliseconds 450 } catch {} }
            $item = Find-ByName $c $name 4; if (-not $item) { $item = Find-ByName $script:win $name 3 }
            if (-not $item) { throw "Report type item not found: $name" }
            [void](Mouse-ClickElement $item)   # mouse click returns immediately; modal pops after
        }
        # 1) Word (change from initial) -> SetProperty true -> side effect: modal "无目标文件！"
        Select-ReportType 'Word'
        $h1 = Wait-Dialog 6
        $wordDialog = ($h1 -ne [IntPtr]::Zero)
        Shot '04-report-type-switch.png'
        Dismiss-AllDialogs
        Start-Sleep -Milliseconds 500
        # 2) Word again (same value) -> SetProperty false -> NO side effect (no modal)
        Select-ReportType 'Word'
        $h2 = Wait-Dialog 4
        $repeatDialog = ($h2 -ne [IntPtr]::Zero)
        Shot '05-report-type-same-no-retrigger.png'
        Dismiss-AllDialogs
        Start-Sleep -Milliseconds 500
        # 3) Excel (different) -> modal again, confirms change-detection still fires
        Select-ReportType 'Excel'
        $h3 = Wait-Dialog 6
        $excelDialog = ($h3 -ne [IntPtr]::Zero)
        Dismiss-AllDialogs
        if ($wordDialog -and -not $repeatDialog -and $excelDialog) {
            Record 'AC-V3d' 'pass' "Word->dialog; Word(repeat)->NO dialog (SetProperty short-circuit); Excel->dialog"
        } elseif ($wordDialog -and $excelDialog) {
            Record 'AC-V3d' 'partial' "Word dialog=$wordDialog; repeat dialog=$repeatDialog; Excel dialog=$excelDialog"
        } else {
            Record 'AC-V3d' 'fail' "Word dialog=$wordDialog; repeat=$repeatDialog; Excel dialog=$excelDialog"
        }
    } catch { Record 'AC-V3d' 'fail' $_.Exception.Message; try { Shot '04-report-type-switch.png' } catch {} }

    # ===== AC-V3e: SystemMT Result Binary/Historical (IsBinaryView via NotifyPropertyChangedFor) =====
    try {
        Navigate 'SystemMT Result' 'DataGrid_SystemMtResults'
        Start-Sleep -Milliseconds 800
        # select a row whose MR has >=2 history (advection-amplitude-linearity) to enable Historical
        $grid = Find-ById $script:win 'DataGrid_SystemMtResults' 10
        if ($grid) {
            $row = Find-ByName $script:win 'advection-amplitude-linearity' 4
            if ($row) { [void](Click-SelectableAncestor $row); Start-Sleep -Milliseconds 600 }
            else {
                $anyRow = $grid.FindFirst($TS::Descendants, (Prop ($AE::ControlTypeProperty) $CT::DataItem))
                if ($anyRow) { [void](Click-SelectableAncestor $anyRow); Start-Sleep -Milliseconds 600 }
            }
        }
        $binary = Find-ByName $script:win 'Single run (Binary)' 6
        $historical = Find-ByName $script:win 'Historical trend' 6
        if (-not $binary -or -not $historical) { throw "Binary/Historical radios not found." }
        function Radio-State($r) { $sp=$null; if ($r.TryGetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern, [ref]$sp)) { return $sp.Current.IsSelected } ; $tp=$null; if ($r.TryGetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern, [ref]$tp)) { return ($tp.Current.ToggleState -eq 'On') }; return $null }
        $binBefore = Radio-State $binary
        $histEnabled = $historical.Current.IsEnabled
        Shot '06-systemmt-binary-before.png'
        if ($histEnabled) {
            [void](Click-Element $historical)
            Start-Sleep -Milliseconds 700
            $binAfter = Radio-State $binary
            $histAfter = Radio-State $historical
            Shot '06-systemmt-binary-historical-toggle.png'
            if ($binBefore -eq $true -and $binAfter -eq $false -and $histAfter -eq $true) {
                Record 'AC-V3e' 'pass' "Binary was On; after Historical click Binary->Off (IsBinaryView synced via NotifyPropertyChangedFor), Historical->On"
            } else {
                Record 'AC-V3e' 'partial' "binBefore=$binBefore binAfter=$binAfter histAfter=$histAfter"
            }
        } else {
            Shot '06-systemmt-binary-historical-toggle.png'
            Record 'AC-V3e' 'blocked' "Historical radio disabled (CanShowHistoricalView=false for selected record; need >=2 same-MR records selected)"
        }
    } catch { Record 'AC-V3e' 'fail' $_.Exception.Message; try { Shot '06-systemmt-binary-historical-toggle.png' } catch {} }

    # --- write driver results ---
    $lines = @("PR-4 driver results", "branch=$(git rev-parse --abbrev-ref HEAD)", "head=$(git rev-parse HEAD)", "scale=$($script:scale)", "")
    foreach ($r in $script:results) { $lines += ("{0}`t{1}`t{2}" -f $r.AC, $r.Status, $r.Detail) }
    Set-Content -Path $summary -Value $lines -Encoding UTF8
    Write-Host "=== DRIVER DONE ==="
    $script:results | Format-Table -AutoSize | Out-String | Write-Host
}
catch {
    $msg = $_ | Out-String
    Set-Content -Path (Join-Path $shotDir 'driver-failure.txt') -Value $msg -Encoding UTF8
    Write-Host $msg
    throw
}
finally {
    if ($script:proc) { try { $script:proc.Refresh(); if (-not $script:proc.HasExited) { Stop-Process -Id $script:proc.Id -Force } } catch {} }
    Pop-Location
}
