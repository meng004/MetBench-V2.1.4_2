# UC-A2 driver: validate excludeSelf rename fix in ApplicationService.UpdateService
. "$PSScriptRoot\_uat_helpers.ps1"

Write-Host ""
Write-Host "===== UC-A2: excludeSelf rename fix =====" -ForegroundColor Cyan

# Step 0: initial state
$p = Focus-MetBench
Save-Shot "UC-A2-step0-initial.png" | Out-Null

# Navigate to Application Management
Navigate-To "Application Management"
Start-Sleep -Milliseconds 800
Save-Shot "UC-A2-step1-app-mgmt-landing.png" | Out-Null

$app = Get-AppRoot $p.Id

function Find-EditByLabel([string]$labelText) {
    $a = Get-AppRoot $p.Id
    return (Find-FieldByLabel $a $labelText)
}

function Set-FormField([string]$labelText, [string]$value) {
    $ed = Find-EditByLabel $labelText
    if (-not $ed) { throw "field for label '$labelText' not found" }
    Set-EditValue $ed $value
    Start-Sleep -Milliseconds 200
}

function Read-FormField([string]$labelText) {
    $ed = Find-EditByLabel $labelText
    if (-not $ed) { return $null }
    $vp = $null
    if ($ed.TryGetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern, [ref]$vp)) {
        return $vp.Current.Value
    }
    return $null
}

function Get-AppGridRows {
    $a = Get-AppRoot $p.Id
    $grid = $a.FindFirst([System.Windows.Automation.TreeScope]::Descendants,
        (New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::AutomationIdProperty, "DataGrid_Application")))
    if (-not $grid) { return @() }
    return $grid.FindAll([System.Windows.Automation.TreeScope]::Descendants,
        (New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
            [System.Windows.Automation.ControlType]::DataItem)))
}

# Probe initial rows
$rows = Get-AppGridRows
Write-Host "  initial rows: $($rows.Count)"
if ($rows.Count -lt 1) { throw "need at least 1 row to drive UC-A2" }
foreach ($r in $rows) { Write-Host "    Name='$($r.Current.Name)'" }
Save-Shot "UC-A2-step2-grid-probe.png" | Out-Null

# Select row 0 — this populates the form
$row0 = $rows[0]
$originalName = $row0.Current.Name
Write-Host "  selecting row 0 (Name=$originalName)"
DoubleClick-Element $row0
Start-Sleep -Milliseconds 600
Save-Shot "UC-A2-step3-row-selected.png" | Out-Null

# Confirm form populated
$nameInForm = Read-FormField "Name"
$descInForm = Read-FormField "Description"
Write-Host "  form Name='$nameInForm'  Description='$descInForm'"

# ─── Test (a): description-only update (Name unchanged) ───
Write-Host ""
Write-Host "  --- Test (a): change Description only, keep Name ---"
$newDesc = "UAT round-2 verify $(Get-Date -Format yyyy-MM-dd-HHmmss)"
Set-FormField "Description" $newDesc
Start-Sleep -Milliseconds 200
Save-Shot "UC-A2-test-a-form-edited-desc-only.png" | Out-Null

# Click Edit button → confirm Yes
$editBtn = Find-ButtonByName (Get-AppRoot $p.Id) "Edit"
if (-not $editBtn) { throw "Edit button not found" }
Invoke-OrClick $editBtn
Write-Host "  clicked Edit"

# Wait for the Yes/No confirmation dialog (likely "提示" / message box from ui:UiMessageBox)
$dlg = Wait-Modal -pattern ".*" -timeoutMs 4000
if ($dlg) {
    Write-Host "  modal appeared: '$($dlg.Current.Name)'"
    Save-Shot "UC-A2-test-a-confirm-modal.png" | Out-Null
    Click-DialogBtn $dlg "Yes" | Out-Null
    Start-Sleep -Milliseconds 1500
}
# Wait for the result-tip modal ("修改记录 成功！" or "该应用程序已存在！")
$tip = Wait-Modal -pattern "Tips|提示" -timeoutMs 4000
if (-not $tip) {
    $tip = Wait-Modal -pattern ".*" -timeoutMs 2000
}
$tipText = if ($tip) { $tip.Current.Name } else { "(no tip modal)" }
Write-Host "  result tip Name='$tipText'"
# Find the text inside the modal
if ($tip) {
    $msgs = $tip.FindAll([System.Windows.Automation.TreeScope]::Descendants,
        (New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
            [System.Windows.Automation.ControlType]::Text)))
    foreach ($m in $msgs) {
        $mn = $m.Current.Name
        if ($mn -and $mn.Length -gt 1) { Write-Host "    text: '$mn'" }
    }
    Save-Shot "UC-A2-test-a-result-tip.png" | Out-Null
    Click-DialogBtn $tip "OK" | Out-Null
    Start-Sleep -Milliseconds 800
}

# ─── Test (b): rename Name (excludeSelf=true; new name is unique) ───
Write-Host ""
Write-Host "  --- Test (b): rename Name to unique new value ---"
# Re-select the row by name (description-only change kept name the same)
$rows = Get-AppGridRows
$row0 = $rows | Where-Object { $_.Current.Name -eq $originalName } | Select-Object -First 1
if (-not $row0) { throw "row '$originalName' missing after Test (a)" }
DoubleClick-Element $row0
Start-Sleep -Milliseconds 500

$renamedName = "$originalName-r2-$(Get-Date -Format HHmmss)"
Set-FormField "Name" $renamedName
Start-Sleep -Milliseconds 200
Save-Shot "UC-A2-test-b-form-renamed.png" | Out-Null

$editBtn = Find-ButtonByName (Get-AppRoot $p.Id) "Edit"
Invoke-OrClick $editBtn
$dlg = Wait-Modal -pattern ".*" -timeoutMs 4000
if ($dlg) {
    Save-Shot "UC-A2-test-b-confirm-modal.png" | Out-Null
    Click-DialogBtn $dlg "Yes" | Out-Null
    Start-Sleep -Milliseconds 1500
}
$tip = Wait-Modal -pattern "Tips|提示" -timeoutMs 4000
if (-not $tip) { $tip = Wait-Modal -pattern ".*" -timeoutMs 2000 }
if ($tip) {
    $msgs = $tip.FindAll([System.Windows.Automation.TreeScope]::Descendants,
        (New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
            [System.Windows.Automation.ControlType]::Text)))
    foreach ($m in $msgs) {
        $mn = $m.Current.Name
        if ($mn -and $mn.Length -gt 1) { Write-Host "    text: '$mn'" }
    }
    Save-Shot "UC-A2-test-b-result-tip.png" | Out-Null
    Click-DialogBtn $tip "OK" | Out-Null
    Start-Sleep -Milliseconds 800
}

# Final grid state
Start-Sleep -Milliseconds 600
Save-Shot "UC-A2-step-final-grid.png" | Out-Null
$rowsFinal = Get-AppGridRows
Write-Host ""
Write-Host "  final rows: $($rowsFinal.Count)"
foreach ($r in $rowsFinal) { Write-Host "    Name='$($r.Current.Name)'" }
Write-Host ""
Write-Host "===== UC-A2 driver done =====" -ForegroundColor Cyan
