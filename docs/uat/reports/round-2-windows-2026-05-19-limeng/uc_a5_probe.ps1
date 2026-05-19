# UC-A5 combo probe — dump all combos with bounding rects
. "$PSScriptRoot\_uat_helpers.ps1"
$p = Focus-MetBench
Navigate-To "MR Management"
Start-Sleep -Milliseconds 800
$app = Get-AppRoot $p.Id
$combos = $app.FindAll([System.Windows.Automation.TreeScope]::Descendants,
    (New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
        [System.Windows.Automation.ControlType]::ComboBox)))
Write-Host "found $($combos.Count) combos"
$i = 0
foreach ($c in $combos) {
    $r = $c.Current.BoundingRectangle
    $name = $c.Current.Name
    $aid = $c.Current.AutomationId
    $isOff = $c.Current.IsOffscreen
    Write-Host "  [$i] off=$isOff name='$name' aid='$aid' rect=($($r.Left),$($r.Top),$($r.Width),$($r.Height))"
    $i++
}
