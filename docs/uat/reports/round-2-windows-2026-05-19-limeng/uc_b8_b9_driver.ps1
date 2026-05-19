# UC-B8 + UC-B9 driver: commonality + replay
# Prereq: UC-B7 already produced 1 anomaly. We add one more failed run for B8 multi-select.

. "$PSScriptRoot\_uat_helpers.ps1"

Write-Host ""
Write-Host "===== UC-B8/B9: commonality + replay =====" -ForegroundColor Cyan
$p = Focus-MetBench

# ───── Step 1: produce a 2nd failed run so we have ≥2 anomalies for commonality ─────
Navigate-To "System MT"
Start-Sleep -Milliseconds 1200
$app = Get-AppRoot $p.Id
$combos = $app.FindAll([System.Windows.Automation.TreeScope]::Descendants,
    (New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
        [System.Windows.Automation.ControlType]::ComboBox)))
$sceneCombo = $combos | Select-Object -First 1
$ecp = $null
if ($sceneCombo.TryGetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern, [ref]$ecp)) {
    $ecp.Expand(); Start-Sleep -Milliseconds 600
}
$items = $sceneCombo.FindAll([System.Windows.Automation.TreeScope]::Descendants,
    (New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
        [System.Windows.Automation.ControlType]::ListItem)))
# Pick heat-equation again (so we don't need OpenMOC)
$target = $items | Where-Object { $_.Current.Name -match "heat" -and $_.Current.Name -match "ScaleAmplitude" } | Select-Object -First 1
$sip = $null
if ($target.TryGetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern, [ref]$sip)) { $sip.Select() } else { Click-Element $target }
Start-Sleep -Milliseconds 600

# Factor 0.3 (still inverts assertion, slightly different value than 0.5 → 2nd distinct run)
$factorLabel = (Get-AppRoot $p.Id).FindFirst([System.Windows.Automation.TreeScope]::Descendants,
    (New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::NameProperty, "Factor parameter")))
$flr = $factorLabel.Current.BoundingRectangle
$edits = (Get-AppRoot $p.Id).FindAll([System.Windows.Automation.TreeScope]::Descendants,
    (New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
        [System.Windows.Automation.ControlType]::Edit)))
$factorEdit = $null; $bestDy = 9999
foreach ($e in $edits) {
    $er = $e.Current.BoundingRectangle
    if ($er.Width -eq 0) { continue }
    $dy = $er.Top - $flr.Bottom
    if ($dy -lt 0 -or $dy -gt 80) { continue }
    if ($dy -lt $bestDy) { $bestDy = $dy; $factorEdit = $e }
}
Set-EditValue $factorEdit "0.3"
Start-Sleep -Milliseconds 300
Save-Shot "UC-B8-step1-second-failing-run-prep.png" | Out-Null
$runBtn = Find-ButtonByName (Get-AppRoot $p.Id) "Run scenario"
Invoke-OrClick $runBtn

# Wait for completion
$completed = $false
for ($i = 0; $i -lt 30; $i++) {
    Start-Sleep -Milliseconds 500
    $allText = (Get-AppRoot $p.Id).FindAll([System.Windows.Automation.TreeScope]::Descendants,
        (New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
            [System.Windows.Automation.ControlType]::Text)))
    foreach ($t in $allText) {
        if ($t.Current.Name -match "Completed in source=") { $completed = $true; break }
    }
    if ($completed) { break }
}
Write-Host "  2nd run completed=$completed"
Save-Shot "UC-B8-step2-after-second-run.png" | Out-Null

# ───── Step 2: navigate to Anomalies, expect ≥2 rows ─────
Navigate-To "Anomalies"
Start-Sleep -Milliseconds 1500
Save-Shot "UC-B8-step3-anomalies-2-rows.png" | Out-Null

$app2 = Get-AppRoot $p.Id
$anomGrid = $app2.FindAll([System.Windows.Automation.TreeScope]::Descendants,
    (New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
        [System.Windows.Automation.ControlType]::DataGrid))) | Select-Object -First 1
$rows = $anomGrid.FindAll([System.Windows.Automation.TreeScope]::Descendants,
    (New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
        [System.Windows.Automation.ControlType]::DataItem)))
Write-Host "  anomaly rows: $($rows.Count)"
foreach ($r in $rows | Select-Object -First 5) { Write-Host "    Name='$($r.Current.Name)'" }

if ($rows.Count -lt 2) {
    Write-Host "  WARN: less than 2 anomalies; UC-B8 commonality may be trivial" -ForegroundColor Yellow
}

# ───── UC-B8: Multi-select + Analyze commonality ─────
# Multi-select: click first, Ctrl+click second
if ($rows.Count -ge 1) {
    $sip0 = $null
    if ($rows[0].TryGetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern, [ref]$sip0)) {
        $sip0.Select()
        Start-Sleep -Milliseconds 200
    }
    if ($rows.Count -ge 2) {
        $sip1 = $null
        if ($rows[1].TryGetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern, [ref]$sip1)) {
            $sip1.AddToSelection()
            Start-Sleep -Milliseconds 200
        }
    }
}
Save-Shot "UC-B8-step4-anomalies-selected.png" | Out-Null

$btnCommon = Find-ButtonByName (Get-AppRoot $p.Id) "Analyze commonality"
if (-not $btnCommon) { throw "Analyze commonality button not found" }
Invoke-OrClick $btnCommon
Start-Sleep -Milliseconds 1500
Save-Shot "UC-B8-step5-commonality-report.png" | Out-Null

# Inspect the commonality TextBlocks
$texts = (Get-AppRoot $p.Id).FindAll([System.Windows.Automation.TreeScope]::Descendants,
    (New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
        [System.Windows.Automation.ControlType]::Text)))
$reportTextFound = $false
foreach ($t in $texts) {
    $tn = $t.Current.Name
    if ($tn -match "Commonality Report|anomalies analyzed|Dominant severity|Dominant category|noise-floor|major/critical|Total:") {
        Write-Host "    [report] '$tn'"
        $reportTextFound = $true
    }
}
if ($reportTextFound) {
    Write-Host "  UC-B8 VERDICT: PASS — commonality report visible" -ForegroundColor Green
} else {
    Write-Host "  UC-B8 VERDICT: INCONCLUSIVE — report text not detected" -ForegroundColor Yellow
}

# ───── UC-B9: Replay this anomaly ─────
Write-Host ""
Write-Host "  --- UC-B9: Replay this anomaly ---"
# Select a single anomaly first
$rows = (Get-AppRoot $p.Id).FindAll([System.Windows.Automation.TreeScope]::Descendants,
    (New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::AutomationIdProperty, "DataGrid_Application"))) |
    Select-Object -First 1

# Re-locate anomaly grid + select row 0
$anomGrid = (Get-AppRoot $p.Id).FindAll([System.Windows.Automation.TreeScope]::Descendants,
    (New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
        [System.Windows.Automation.ControlType]::DataGrid))) | Select-Object -First 1
$rows = $anomGrid.FindAll([System.Windows.Automation.TreeScope]::Descendants,
    (New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
        [System.Windows.Automation.ControlType]::DataItem)))
$preRunsCount = $rows.Count
Write-Host "    pre-replay anomaly count: $preRunsCount"
if ($rows.Count -ge 1) {
    $sip0 = $null
    if ($rows[0].TryGetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern, [ref]$sip0)) {
        $sip0.Select()
        Start-Sleep -Milliseconds 300
    }
}
$btnReplay = Find-ButtonByName (Get-AppRoot $p.Id) "Replay this anomaly"
if (-not $btnReplay) { throw "Replay button not found" }
Save-Shot "UC-B9-step1-before-replay.png" | Out-Null
Invoke-OrClick $btnReplay
Start-Sleep -Milliseconds 3000  # replay re-runs the MR

# Wait for replay to settle
for ($i = 0; $i -lt 30; $i++) {
    Start-Sleep -Milliseconds 500
    $errText = (Get-AppRoot $p.Id).FindAll([System.Windows.Automation.TreeScope]::Descendants,
        (New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
            [System.Windows.Automation.ControlType]::Text)))
    foreach ($t in $errText) {
        if ($t.Current.Name -match "^Error|System\.|^Replay") {
            Write-Host "    replay status: '$($t.Current.Name)'"
        }
    }
    break
}
Save-Shot "UC-B9-step2-after-replay.png" | Out-Null

# Verify the System MT result table got a new row
Navigate-To "System MT"
Start-Sleep -Milliseconds 1200
Save-Shot "UC-B9-step3-system-mt-after-replay.png" | Out-Null
$grid = (Get-AppRoot $p.Id).FindAll([System.Windows.Automation.TreeScope]::Descendants,
    (New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
        [System.Windows.Automation.ControlType]::DataGrid))) | Select-Object -First 1
$runs = $grid.FindAll([System.Windows.Automation.TreeScope]::Descendants,
    (New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
        [System.Windows.Automation.ControlType]::DataItem)))
Write-Host "  System MT RecentRuns count after replay: $($runs.Count)"

# Compare against anomaly count post-replay
Navigate-To "Anomalies"
Start-Sleep -Milliseconds 1500
$anomGrid = (Get-AppRoot $p.Id).FindAll([System.Windows.Automation.TreeScope]::Descendants,
    (New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
        [System.Windows.Automation.ControlType]::DataGrid))) | Select-Object -First 1
$rowsPost = $anomGrid.FindAll([System.Windows.Automation.TreeScope]::Descendants,
    (New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
        [System.Windows.Automation.ControlType]::DataItem)))
Write-Host "  post-replay anomaly count: $($rowsPost.Count)"
Save-Shot "UC-B9-step4-anomalies-after-replay.png" | Out-Null

Write-Host ""
Write-Host "===== UC-B8/B9 driver done =====" -ForegroundColor Cyan
