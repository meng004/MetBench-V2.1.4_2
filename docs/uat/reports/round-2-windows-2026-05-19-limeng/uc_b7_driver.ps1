# UC-B7 driver: failed System MT run -> auto Anomaly record (PR #75)
. "$PSScriptRoot\_uat_helpers.ps1"

Write-Host ""
Write-Host "===== UC-B7: failed System MT run -> Anomaly =====" -ForegroundColor Cyan

$p = Focus-MetBench

# Probe: pre-state anomaly count
Navigate-To "Anomalies"
Start-Sleep -Milliseconds 1000
$app = Get-AppRoot $p.Id
$grids = $app.FindAll([System.Windows.Automation.TreeScope]::Descendants,
    (New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
        [System.Windows.Automation.ControlType]::DataGrid)))
$anomGrid = $grids | Select-Object -First 1
$preRows = @()
if ($anomGrid) {
    $preRows = $anomGrid.FindAll([System.Windows.Automation.TreeScope]::Descendants,
        (New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
            [System.Windows.Automation.ControlType]::DataItem)))
}
Write-Host "  pre-test anomaly rows: $($preRows.Count)"
Save-Shot "UC-B7-step0-anomaly-pre.png" | Out-Null

# Navigate to System MT
Navigate-To "System MT"
Start-Sleep -Milliseconds 1200
Save-Shot "UC-B7-step1-system-mt-landing.png" | Out-Null

$app = Get-AppRoot $p.Id

# Pick scenario combo (first ComboBox on page bound to AvailableMrs)
$combos = $app.FindAll([System.Windows.Automation.TreeScope]::Descendants,
    (New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
        [System.Windows.Automation.ControlType]::ComboBox)))
Write-Host "  System MT combos: $($combos.Count)"
$sceneCombo = $combos | Select-Object -First 1

# Expand scenario combo and inspect items
$ecp = $null
if ($sceneCombo.TryGetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern, [ref]$ecp)) {
    $ecp.Expand()
    Start-Sleep -Milliseconds 800
    Save-Shot "UC-B7-step2-scenario-combo-expanded.png" | Out-Null

    $items = $sceneCombo.FindAll([System.Windows.Automation.TreeScope]::Descendants,
        (New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
            [System.Windows.Automation.ControlType]::ListItem)))
    Write-Host "  scenarios:"
    foreach ($it in $items) { Write-Host "    '$($it.Current.Name)'" }

    # Prefer heat_equation ScaleAmplitude (no OpenMOC needed)
    $target = $items | Where-Object { $_.Current.Name -match "heat" -and $_.Current.Name -match "ScaleAmplitude" } | Select-Object -First 1
    if (-not $target) { $target = $items | Where-Object { $_.Current.Name -match "ScaleAmplitude" } | Select-Object -First 1 }
    if (-not $target) { $target = $items | Select-Object -First 1 }
    if (-not $target) { throw "no scenario items" }
    Write-Host "  selecting: '$($target.Current.Name)'"

    $sip = $null
    if ($target.TryGetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern, [ref]$sip)) {
        $sip.Select()
    } else {
        Click-Element $target
    }
    Start-Sleep -Milliseconds 600
    Save-Shot "UC-B7-step3-scenario-selected.png" | Out-Null
}

# Locate the Factor parameter Edit (immediately below scenario combo, narrow textbox)
$edits = $app.FindAll([System.Windows.Automation.TreeScope]::Descendants,
    (New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
        [System.Windows.Automation.ControlType]::Edit)))
Write-Host "  Edit count on page: $($edits.Count)"
# Find label "Factor parameter" then nearest Edit below it
$factorLabel = $app.FindFirst([System.Windows.Automation.TreeScope]::Descendants,
    (New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::NameProperty, "Factor parameter")))
if (-not $factorLabel) { throw "Factor parameter label not found" }
$flr = $factorLabel.Current.BoundingRectangle
Write-Host "  Factor label at ($($flr.Left),$($flr.Top))"

$factorEdit = $null; $bestDy = 9999
foreach ($e in $edits) {
    $er = $e.Current.BoundingRectangle
    if ($er.Width -eq 0) { continue }
    $dy = $er.Top - $flr.Bottom
    if ($dy -lt 0 -or $dy -gt 80) { continue }
    $dxLeftMatch = [Math]::Abs($er.Left - $flr.Left)
    if ($dxLeftMatch -gt 200) { continue }
    if ($dy -lt $bestDy) { $bestDy = $dy; $factorEdit = $e }
}
if (-not $factorEdit) { throw "Factor edit not found" }
Set-EditValue $factorEdit "0.5"
Start-Sleep -Milliseconds 400
Save-Shot "UC-B7-step4-factor-0.5.png" | Out-Null
Write-Host "  set factor=0.5"

# Click "Run scenario"
$runBtn = Find-ButtonByName (Get-AppRoot $p.Id) "Run scenario"
if (-not $runBtn) { throw "Run scenario button not found" }
Invoke-OrClick $runBtn
Write-Host "  clicked Run scenario; waiting for completion..."

# Wait for status to indicate completion (poll up to 30s)
$completed = $false
for ($attempt = 0; $attempt -lt 60; $attempt++) {
    Start-Sleep -Milliseconds 500
    $app2 = Get-AppRoot $p.Id
    $allText = $app2.FindAll([System.Windows.Automation.TreeScope]::Descendants,
        (New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
            [System.Windows.Automation.ControlType]::Text)))
    $statusFound = $false
    foreach ($t in $allText) {
        $tn = $t.Current.Name
        if ($tn -match "Completed|completed|finished|Failed|failed|done|error|Error") {
            Write-Host "    [$attempt] status text: '$tn'"
            $completed = $true
            $statusFound = $true
            break
        }
    }
    if ($completed) { break }
}
Save-Shot "UC-B7-step5-after-run.png" | Out-Null

# Locate the Recent runs DataGrid and check the new row
$app3 = Get-AppRoot $p.Id
$grid2 = $app3.FindAll([System.Windows.Automation.TreeScope]::Descendants,
    (New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
        [System.Windows.Automation.ControlType]::DataGrid))) | Select-Object -First 1
if ($grid2) {
    $newRuns = $grid2.FindAll([System.Windows.Automation.TreeScope]::Descendants,
        (New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
            [System.Windows.Automation.ControlType]::DataItem)))
    Write-Host "  SystemMT RecentRuns rows: $($newRuns.Count)"
    foreach ($r in $newRuns | Select-Object -First 3) {
        $kids = $r.FindAll([System.Windows.Automation.TreeScope]::Descendants,
            [System.Windows.Automation.Condition]::TrueCondition)
        $cells = @()
        foreach ($k in $kids) {
            $kn = $k.Current.Name
            if ($kn -and $kn.Length -gt 0 -and $kn.Length -lt 60) { $cells += $kn }
        }
        Write-Host "    row: $($cells -join ' | ')"
    }
}

# Navigate to Anomalies, refresh
Navigate-To "Anomalies"
Start-Sleep -Milliseconds 1500
Save-Shot "UC-B7-step6-anomaly-after-run.png" | Out-Null

# Anomaly page doesn't have an explicit Refresh button visible in XAML — navigation triggers reload via INavigationAware
$app4 = Get-AppRoot $p.Id
$anomGrid2 = $app4.FindAll([System.Windows.Automation.TreeScope]::Descendants,
    (New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
        [System.Windows.Automation.ControlType]::DataGrid))) | Select-Object -First 1
$postRows = @()
if ($anomGrid2) {
    $postRows = $anomGrid2.FindAll([System.Windows.Automation.TreeScope]::Descendants,
        (New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
            [System.Windows.Automation.ControlType]::DataItem)))
}
Write-Host ""
Write-Host "  post-test anomaly rows: $($postRows.Count) (was $($preRows.Count) before run)"

if ($postRows.Count -gt $preRows.Count) {
    Write-Host "  VERDICT: PASS — anomaly count increased by $($postRows.Count - $preRows.Count)" -ForegroundColor Green
    # Dump newest row details
    foreach ($r in $postRows | Select-Object -First 3) {
        $kids = $r.FindAll([System.Windows.Automation.TreeScope]::Descendants,
            [System.Windows.Automation.Condition]::TrueCondition)
        $cells = @()
        foreach ($k in $kids) {
            $kn = $k.Current.Name
            if ($kn -and $kn.Length -gt 0 -and $kn.Length -lt 80) { $cells += $kn }
        }
        Write-Host "    anomaly row: $($cells -join ' | ')"
    }
} else {
    Write-Host "  VERDICT: FAIL — anomaly count did not increase" -ForegroundColor Red
}

# Final pages screenshot
Save-Shot "UC-B7-step7-anomaly-final.png" | Out-Null

Write-Host ""
Write-Host "===== UC-B7 driver done =====" -ForegroundColor Cyan
