# UC-A5 driver v2: target the form-side narrow ApplicationName combo directly
. "$PSScriptRoot\_uat_helpers.ps1"

Write-Host ""
Write-Host "===== UC-A5: ApplicationEx ComboBox display =====" -ForegroundColor Cyan

$p = Focus-MetBench
Navigate-To "MR Management"
Start-Sleep -Milliseconds 800
Save-Shot "UC-A5-step1-mr-mgmt-landing.png" | Out-Null

$app = Get-AppRoot $p.Id
$combos = $app.FindAll([System.Windows.Automation.TreeScope]::Descendants,
    (New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
        [System.Windows.Automation.ControlType]::ComboBox)))

# ApplicationName combo: form-side, MaxWidth=140 in XAML, narrowest combo on the page
$appCombo = $null
$minW = 9999
foreach ($c in $combos) {
    $r = $c.Current.BoundingRectangle
    if ($r.Width -gt 0 -and $r.Width -lt $minW) {
        $minW = $r.Width
        $appCombo = $c
    }
}
if (-not $appCombo) { throw "no combo found" }
$ar = $appCombo.Current.BoundingRectangle
Write-Host "  picked narrowest combo at ($($ar.Left),$($ar.Top)) size $($ar.Width)x$($ar.Height)"

# Expand via pattern (most reliable)
$ecp = $null
if ($appCombo.TryGetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern, [ref]$ecp)) {
    $ecp.Expand()
    Start-Sleep -Milliseconds 700
    Save-Shot "UC-A5-step2-combo-expanded.png" | Out-Null
    Write-Host "  combo expanded"
} else {
    throw "combo has no ExpandCollapsePattern"
}

# Enumerate list items
$items = $appCombo.FindAll([System.Windows.Automation.TreeScope]::Descendants,
    (New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
        [System.Windows.Automation.ControlType]::ListItem)))
Write-Host "  combo contains $($items.Count) ListItem"

$itemNames = @()
foreach ($it in $items) {
    $nm = $it.Current.Name
    $itemNames += $nm
    Write-Host "    item: '$nm'"
    # Inspect children
    $kids = $it.FindAll([System.Windows.Automation.TreeScope]::Descendants,
        [System.Windows.Automation.Condition]::TrueCondition)
    foreach ($k in $kids) {
        $kn = $k.Current.Name
        $kct = $k.Current.ControlType.ProgrammaticName -replace 'ControlType\.', ''
        if ($kn) { Write-Host "      child[$kct] = '$kn'" }
    }
}

# Verdict
$bad = $itemNames | Where-Object { $_ -match 'MetBench_(Domain|Client\.Models)\.Application' }
if ($bad) {
    Write-Host "  VERDICT: FAIL — class-FQN items: $($bad -join ', ')" -ForegroundColor Red
} elseif ($items.Count -eq 0) {
    Write-Host "  VERDICT: INCONCLUSIVE — combo empty" -ForegroundColor Yellow
} else {
    Write-Host "  VERDICT: PASS — items show business name, no class FQN" -ForegroundColor Green
}

# Try selecting first item and check the post-select Text in the combo's edit
if ($items.Count -ge 1) {
    Write-Host ""
    Write-Host "  --- selecting item [0] ---"
    $first = $items[0]
    $sip = $null
    if ($first.TryGetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern, [ref]$sip)) {
        $sip.Select()
    } else {
        Click-Element $first
    }
    Start-Sleep -Milliseconds 600
    Save-Shot "UC-A5-step3-item-selected.png" | Out-Null

    # The editable combo's text comes through Value pattern
    $vp = $null
    if ($appCombo.TryGetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern, [ref]$vp)) {
        $editText = $vp.Current.Value
        Write-Host "  combo text after select = '$editText'"
        if ($editText -match 'MetBench_(Domain|Client\.Models)\.Application') {
            Write-Host "  POST-SELECT VERDICT: FAIL — class FQN" -ForegroundColor Red
        } else {
            Write-Host "  POST-SELECT VERDICT: PASS — business name" -ForegroundColor Green
        }
    } else {
        # Fallback: look at Edit descendant
        $edit = $appCombo.FindFirst([System.Windows.Automation.TreeScope]::Descendants,
            (New-Object System.Windows.Automation.PropertyCondition(
                [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
                [System.Windows.Automation.ControlType]::Edit)))
        if ($edit) {
            $vp2 = $null
            if ($edit.TryGetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern, [ref]$vp2)) {
                $editText = $vp2.Current.Value
                Write-Host "  combo edit text = '$editText'"
                if ($editText -match 'MetBench_(Domain|Client\.Models)\.Application') {
                    Write-Host "  POST-SELECT VERDICT: FAIL — class FQN" -ForegroundColor Red
                } else {
                    Write-Host "  POST-SELECT VERDICT: PASS — business name" -ForegroundColor Green
                }
            }
        }
    }
}

# Sibling check: Discovery page Target SUT combo
Write-Host ""
Write-Host "  --- Sibling: Discovery page Target SUT combo ---"
Navigate-To "Discovery"
Start-Sleep -Milliseconds 800
Save-Shot "UC-A5-step4-discovery-landing.png" | Out-Null
$appD = Get-AppRoot $p.Id
$combosD = $appD.FindAll([System.Windows.Automation.TreeScope]::Descendants,
    (New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
        [System.Windows.Automation.ControlType]::ComboBox)))
Write-Host "    Discovery has $($combosD.Count) ComboBoxes"
foreach ($c in $combosD) {
    try {
        $ecp2 = $null
        if (-not $c.TryGetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern, [ref]$ecp2)) { continue }
        $ecp2.Expand()
        Start-Sleep -Milliseconds 500
        $items2 = $c.FindAll([System.Windows.Automation.TreeScope]::Descendants,
            (New-Object System.Windows.Automation.PropertyCondition(
                [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
                [System.Windows.Automation.ControlType]::ListItem)))
        if ($items2.Count -gt 0) {
            Write-Host "    combo with $($items2.Count) items:"
            foreach ($it in $items2) { Write-Host "      '$($it.Current.Name)'" }
            $badD = $items2 | Where-Object { $_.Current.Name -match 'MetBench_(Domain|Client\.Models)\.' }
            if ($badD) { Write-Host "      ⚠ BAD" -ForegroundColor Red }
        }
        $ecp2.Collapse()
    } catch { }
}
Save-Shot "UC-A5-step5-discovery-final.png" | Out-Null

Write-Host ""
Write-Host "===== UC-A5 driver done =====" -ForegroundColor Cyan
