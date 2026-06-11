#requires -version 5
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms
Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class BatchADWin32 {
  [DllImport("user32.dll")] public static extern bool SetCursorPos(int X, int Y);
  [DllImport("user32.dll")] public static extern void mouse_event(uint f,uint dx,uint dy,uint d,UIntPtr e);
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h,int n);
  [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
  public static void Click(int x,int y) {
    SetCursorPos(x,y); System.Threading.Thread.Sleep(80);
    mouse_event(0x0002,0,0,0,UIntPtr.Zero); System.Threading.Thread.Sleep(60);
    mouse_event(0x0004,0,0,0,UIntPtr.Zero);
  }
}
"@
[void][BatchADWin32]::SetProcessDPIAware()

$ErrorActionPreference = 'Stop'
$AE = [System.Windows.Automation.AutomationElement]
$TS = [System.Windows.Automation.TreeScope]
$CT = [System.Windows.Automation.ControlType]
$shotDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repo = Resolve-Path (Join-Path $shotDir '..\..\..\..')
$buildLog = Join-Path $shotDir 'build-output.txt'
$generatorLog = Join-Path $shotDir 'package-generator-output.txt'
$summary = Join-Path $shotDir 'vm-summary.md'
$script:win = $null
$script:scale = 1.0
$script:proc = $null
$terminalStates = @('Succeeded', 'Failed', 'TimedOut', 'ArtifactMissing', 'Cancelled')

function Prop($id, $value) {
    New-Object System.Windows.Automation.PropertyCondition($id, $value)
}

function Find-ById($root, $id, $timeoutSec = 8) {
    $cond = Prop ([System.Windows.Automation.AutomationElement]::AutomationIdProperty) $id
    $deadline = (Get-Date).AddSeconds($timeoutSec)
    do {
        $el = $root.FindFirst($TS::Descendants, $cond)
        if ($el) { return $el }
        Start-Sleep -Milliseconds 200
    } while ((Get-Date) -lt $deadline)
    return $null
}

function Find-ByName($root, $name, $timeoutSec = 8) {
    $cond = Prop ([System.Windows.Automation.AutomationElement]::NameProperty) $name
    $deadline = (Get-Date).AddSeconds($timeoutSec)
    do {
        $el = $root.FindFirst($TS::Descendants, $cond)
        if ($el) { return $el }
        Start-Sleep -Milliseconds 200
    } while ((Get-Date) -lt $deadline)
    return $null
}

function Bring-Front {
    if ($script:win) {
        [void][BatchADWin32]::ShowWindow([IntPtr]$script:win.Current.NativeWindowHandle, 3)
        [void][BatchADWin32]::SetForegroundWindow([IntPtr]$script:win.Current.NativeWindowHandle)
        Start-Sleep -Milliseconds 250
    }
}

function Click-Element($el) {
    if (-not $el) { return $false }
    $ip = $null
    if ($el.TryGetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern, [ref]$ip)) {
        $ip.Invoke()
        return $true
    }
    $sp = $null
    if ($el.TryGetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern, [ref]$sp)) {
        $sp.Select()
        return $true
    }
    Bring-Front
    $r = $el.Current.BoundingRectangle
    if ($r.Width -gt 0 -and $r.Height -gt 0) {
        [BatchADWin32]::Click([int](($r.X + $r.Width / 2) / $script:scale), [int](($r.Y + $r.Height / 2) / $script:scale))
        return $true
    }
    return $false
}

function Click-SelectableAncestor($el) {
    $node = $el
    $walker = [System.Windows.Automation.TreeWalker]::ControlViewWalker
    for ($i = 0; $i -lt 8 -and $node; $i++) {
        if (Click-Element $node) { return $true }
        $node = $walker.GetParent($node)
    }
    return $false
}

function Set-NavSearch($text) {
    $editCond = Prop ([System.Windows.Automation.AutomationElement]::ControlTypeProperty) $CT::Edit
    $edits = $script:win.FindAll($TS::Descendants, $editCond)
    foreach ($edit in $edits) {
        $vp = $null
        if ($edit.TryGetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern, [ref]$vp)) {
            try {
                if (-not $vp.Current.IsReadOnly) {
                    $vp.SetValue($text)
                    Start-Sleep -Milliseconds 500
                    return $true
                }
            } catch {}
        }
    }
    return $false
}

function Set-TextById($id, $value) {
    $el = Find-ById $script:win $id 8
    if (-not $el) { throw "Element not found: $id" }
    $vp = $null
    if (-not $el.TryGetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern, [ref]$vp)) {
        throw "Element has no ValuePattern: $id"
    }
    $vp.SetValue($value)
    Start-Sleep -Milliseconds 150
}

function Get-TextById($id) {
    $el = Find-ById $script:win $id 2
    if (-not $el) { return '' }
    $vp = $null
    if ($el.TryGetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern, [ref]$vp)) {
        return $vp.Current.Value
    }
    return $el.Current.Name
}

function Shot($file) {
    Bring-Front
    $r = $script:win.Current.BoundingRectangle
    $x = [int]($r.X / $script:scale)
    $y = [int]($r.Y / $script:scale)
    $w = [int]($r.Width / $script:scale)
    $h = [int]($r.Height / $script:scale)
    $bmp = New-Object System.Drawing.Bitmap($w, $h)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen($x, $y, 0, 0, $bmp.Size)
    $bmp.Save((Join-Path $shotDir $file), [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose()
    $bmp.Dispose()
    Write-Host "[shot] $file"
}

function Select-ComboItem($comboId, $name) {
    $combo = Find-ById $script:win $comboId 10
    if (-not $combo) { throw "Combo not found: $comboId" }
    Bring-Front
    $ecp = $null
    if ($combo.TryGetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern, [ref]$ecp)) {
        $ecp.Expand()
        Start-Sleep -Milliseconds 450
    }
    $item = Find-ByName $script:win $name 10
    if (-not $item) { throw "Combo item not found: $name" }
    if (-not (Click-SelectableAncestor $item)) { throw "Could not select combo item: $name" }
    try { if ($ecp) { $ecp.Collapse() } } catch {}
    Start-Sleep -Milliseconds 350
}

function Start-MetBench($exe) {
    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = $exe
    $psi.WorkingDirectory = Split-Path $exe -Parent
    $psi.UseShellExecute = $false
    return [System.Diagnostics.Process]::Start($psi)
}

function Stop-MetBench {
    if ($script:proc) {
        try {
            $script:proc.Refresh()
            if (-not $script:proc.HasExited) {
                Stop-Process -Id $script:proc.Id -Force
            }
        } catch {}
        $script:proc = $null
        $script:win = $null
        Start-Sleep -Seconds 2
    }
}

function Attach-MainWindow($proc) {
    $script:win = $null
    Start-Sleep -Seconds 7
    $winCond = Prop ([System.Windows.Automation.AutomationElement]::ProcessIdProperty) $proc.Id
    for ($i = 0; $i -lt 30 -and -not $script:win; $i++) {
        $script:win = $AE::RootElement.FindFirst($TS::Children, $winCond)
        if (-not $script:win) { Start-Sleep -Milliseconds 500 }
    }
    if (-not $script:win) { throw "Main window not found." }
    Bring-Front
    $physW = $AE::RootElement.Current.BoundingRectangle.Width
    $logW = [System.Windows.Forms.SystemInformation]::VirtualScreen.Width
    $script:scale = if ($logW -gt 0) { $physW / $logW } else { 1.0 }
}

function Open-AsyncPage {
    [void](Set-NavSearch 'Async')
    $nav = Find-ById $script:win 'Nav_SystemMtAsyncExecution' 12
    if (-not $nav) { throw "Async navigation item not found." }
    if (-not (Click-SelectableAncestor $nav)) { throw "Async navigation item could not be clicked." }
    if (-not (Find-ById $script:win 'AsyncOperationCombo' 12)) {
        Shot 'diagnostic-after-async-nav.png'
        throw "Async page not reached."
    }
}

function Open-NavPage($search, $name, $expectedAutomationId, $shot) {
    [void](Set-NavSearch $search)
    $nav = Find-ByName $script:win $name 12
    if (-not $nav) { throw "Navigation item not found: $name" }
    if (-not (Click-SelectableAncestor $nav)) { throw "Navigation item could not be clicked: $name" }
    if ($expectedAutomationId -and -not (Find-ById $script:win $expectedAutomationId 12)) {
        Shot ("diagnostic-after-" + $shot)
        throw "Expected page element not reached: $expectedAutomationId"
    }
    Shot $shot
}

function Open-NavPageById($search, $navAutomationId, $expectedAutomationId, $shot) {
    [void](Set-NavSearch $search)
    $nav = Find-ById $script:win $navAutomationId 12
    if (-not $nav) { throw "Navigation item not found: $navAutomationId" }
    if (-not (Click-SelectableAncestor $nav)) { throw "Navigation item could not be clicked: $navAutomationId" }
    if ($expectedAutomationId -and -not (Find-ById $script:win $expectedAutomationId 12)) {
        Shot ("diagnostic-after-" + $shot)
        throw "Expected page element not reached: $expectedAutomationId"
    }
    Shot $shot
}

function Wait-NewJob($oldJob, $timeoutSec = 8) {
    $deadline = (Get-Date).AddSeconds($timeoutSec)
    do {
        $job = Get-TextById 'AsyncJobId'
        if ($job -and $job -ne '-' -and $job -ne $oldJob) { return $job }
        Start-Sleep -Milliseconds 200
    } while ((Get-Date) -lt $deadline)
    throw "Timed out waiting for a new async job id."
}

function Wait-Terminal($timeoutSec = 120) {
    $deadline = (Get-Date).AddSeconds($timeoutSec)
    do {
        $state = Get-TextById 'AsyncState'
        if ($terminalStates -contains $state) { return $state }
        Start-Sleep -Milliseconds 500
    } while ((Get-Date) -lt $deadline)
    throw "Timed out waiting for terminal async job state."
}

function Submit-And-Wait($operation, $terminalShot, $timeoutSec = 120) {
    $oldJob = Get-TextById 'AsyncJobId'
    [void](Click-Element (Find-ById $script:win 'AsyncSubmitButton' 5))
    $job = Wait-NewJob $oldJob
    Start-Sleep -Milliseconds 500
    $state = Wait-Terminal $timeoutSec
    Shot $terminalShot
    $artifact = Get-TextById 'AsyncArtifactPath'
    $result = Get-TextById 'AsyncResultSummary'
    return [pscustomobject]@{
        Operation = $operation
        JobId = $job
        State = $state
        ArtifactPath = $artifact
        Result = $result
    }
}

function Assert-PathExists($path, $label) {
    if (-not (Test-Path -LiteralPath $path)) {
        throw "$label does not exist: $path"
    }
}

function Write-BatchPackages($packageRoot) {
    $generatorRoot = Join-Path $shotDir 'package-generator'
    New-Item -ItemType Directory -Force -Path $generatorRoot | Out-Null
    $csproj = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="$repo\MetBench_BLL.Core\MetBench_BLL.Core.csproj" />
    <ProjectReference Include="$repo\MetBench_DAL\MetBench_DAL.csproj" />
  </ItemGroup>
</Project>
"@
    Set-Content -Path (Join-Path $generatorRoot 'BatchPackageGenerator.csproj') -Value $csproj -Encoding UTF8
    $program = @'
using MetBench_BLL.Core.SystemMT.ImportExport.Put;
using MetBench_DAL;

switch (args[0])
{
    case "packages":
        var root = args[1];
        Directory.CreateDirectory(root);
        SutImportPackageExporter.Export(ExternalMrAcceptancePutFixtures.CreateBatchAToyClassic(), Path.Combine(root, "toy"));
        SutImportPackageExporter.Export(ExternalMrAcceptancePutFixtures.CreateBatchAP1Heat(), Path.Combine(root, "p1"));
        SutImportPackageExporter.Export(ExternalMrAcceptancePutFixtures.CreateBatchDScimlDomainValidity(), Path.Combine(root, "sciml"));
        Console.WriteLine(root);
        break;
    case "latest-minmr-result":
        using (var repo = new LiteDbSystemMtResultRepository($"Filename={args[1]}"))
        {
            var rows = await repo.ListRecentAsync(50);
            var row = rows.FirstOrDefault(r => r.MrName.StartsWith("minmr-", StringComparison.Ordinal));
            if (row is null)
            {
                var names = rows.Select(r => $"{r.Id}:{r.MrName}").ToArray();
                throw new InvalidOperationException("No minmr-* System MT result record found. Recent rows: " + string.Join("; ", names));
            }
            Console.WriteLine(row.Id);
        }
        break;
    default:
        throw new ArgumentOutOfRangeException(nameof(args), args[0], "Unknown generator command.");
}
'@
    Set-Content -Path (Join-Path $generatorRoot 'Program.cs') -Value $program -Encoding UTF8
    dotnet run --project (Join-Path $generatorRoot 'BatchPackageGenerator.csproj') -- packages $packageRoot *> $generatorLog
    if ($LASTEXITCODE -ne 0) { throw "Package generator failed. See $generatorLog" }
}

function Get-LatestBatchExecutionId($resultDbPath) {
    $generatorRoot = Join-Path $shotDir 'package-generator'
    $queryLog = Join-Path $shotDir 'latest-result-query-output.txt'
    dotnet run --project (Join-Path $generatorRoot 'BatchPackageGenerator.csproj') -- latest-minmr-result $resultDbPath *> $queryLog
    if ($LASTEXITCODE -ne 0) { throw "Latest result query failed. See $queryLog" }
    return (Get-Content $queryLog | Select-Object -Last 1).Trim()
}

function Run-Import($packageRoot, $stagingRoot, $shot) {
    Select-ComboItem 'AsyncOperationCombo' 'ImportAssets'
    Set-TextById 'AsyncPackageRootBox' $packageRoot
    Set-TextById 'AsyncStagingRootBox' $stagingRoot
    $result = Submit-And-Wait 'ImportAssets' $shot 90
    if ($result.State -ne 'Succeeded') { throw "ImportAssets terminal state was $($result.State)." }
    Assert-PathExists $result.ArtifactPath 'Import artifact'
    Assert-PathExists (Join-Path (Split-Path $result.ArtifactPath -Parent) 'sut-import-unit.json') 'Staged sut-import-unit.json'
    return $result
}

function Run-Export($packageRoot, $exportRoot, $shot) {
    Select-ComboItem 'AsyncOperationCombo' 'ExportAssets'
    Set-TextById 'AsyncPackageRootBox' $packageRoot
    Set-TextById 'AsyncExportRootBox' $exportRoot
    $result = Submit-And-Wait 'ExportAssets' $shot 90
    if ($result.State -ne 'Succeeded') { throw "ExportAssets terminal state was $($result.State)." }
    Assert-PathExists $result.ArtifactPath 'Export artifact'
    return $result
}

Push-Location $repo
try {
    $stale = @(
        '01-async-page-ready.png',
        '02-import-batch-a-toy-succeeded.png',
        '03-import-batch-a-p1-succeeded.png',
        '04-import-batch-d-sciml-succeeded.png',
        '05-runbatch-batch-a-four-mrs-succeeded.png',
        '06-export-batch-a-toy-roundtrip-succeeded.png',
        '07-export-batch-a-p1-roundtrip-succeeded.png',
        '08-export-batch-d-sciml-roundtrip-succeeded.png',
        '09-result-dashboard-visible.png',
        '10-coverage-dashboard-visible.png',
        '11-anomaly-page-visible.png',
        '12-export-report-succeeded.png',
        'diagnostic-after-09-result-dashboard-visible.png',
        'build-output.txt',
        'package-generator-output.txt',
        'latest-result-query-output.txt',
        'vm-summary.md',
        'failure.txt')
    foreach ($name in $stale) {
        $path = Join-Path $shotDir $name
        if (Test-Path $path) { Remove-Item $path -Force }
    }

    dotnet build MetBench_Client\MetBench_Client.csproj --no-restore -v:minimal *> $buildLog
    $buildExit = $LASTEXITCODE
    $buildErrors = (Select-String -Path $buildLog -Pattern ': error ').Count
    if ($buildExit -ne 0 -or $buildErrors -gt 0) { throw "Build failed. See $buildLog" }

    $clientOut = Join-Path $repo 'MetBench_Client\bin\Debug\net8.0-windows7.0'
    $extraManifest = Join-Path $clientOut 'SUT\external_acceptance_minmr\acceptance-catalog.json'
    Assert-PathExists $extraManifest 'Batch A acceptance catalog'
    $env:METBENCH_EXTRA_MR_MANIFESTS = $extraManifest

    $workRoot = Join-Path $shotDir 'operation-artifacts'
    if (Test-Path $workRoot) { Remove-Item -Recurse -Force $workRoot }
    $packageRoot = Join-Path $workRoot 'packages'
    $stagingRoot = Join-Path $workRoot 'staging'
    $exportRoot = Join-Path $workRoot 'export'
    $reportExportRoot = Join-Path $workRoot 'report-export'
    New-Item -ItemType Directory -Force -Path $workRoot | Out-Null
    Write-BatchPackages $packageRoot

    $exe = Join-Path $clientOut 'MetBench_Client.exe'
    $script:proc = Start-MetBench $exe
    Attach-MainWindow $script:proc
    Open-AsyncPage
    Shot '01-async-page-ready.png'

    $importToy = Run-Import (Join-Path $packageRoot 'toy') (Join-Path $stagingRoot 'toy') '02-import-batch-a-toy-succeeded.png'
    $importP1 = Run-Import (Join-Path $packageRoot 'p1') (Join-Path $stagingRoot 'p1') '03-import-batch-a-p1-succeeded.png'
    $importSciml = Run-Import (Join-Path $packageRoot 'sciml') (Join-Path $stagingRoot 'sciml') '04-import-batch-d-sciml-succeeded.png'

    Select-ComboItem 'AsyncOperationCombo' 'RunBatch'
    Set-TextById 'AsyncBatchMrIdsBox' 'minmr-toy-sort-permutation, minmr-p1-heat-alpha-monotonic, minmr-p1-heat-timestep-convergence, minmr-p1-heat-mesh-convergence'
    $runBatch = Submit-And-Wait 'RunBatch' '05-runbatch-batch-a-four-mrs-succeeded.png' 180
    if ($runBatch.State -ne 'Succeeded') { throw "RunBatch terminal state was $($runBatch.State)." }
    if ($runBatch.Result -notmatch 'total=4; passed=4; failed=0') {
        throw "RunBatch result did not show 4/4 pass: $($runBatch.Result)"
    }

    $stagedToy = Split-Path $importToy.ArtifactPath -Parent
    $stagedP1 = Split-Path $importP1.ArtifactPath -Parent
    $stagedSciml = Split-Path $importSciml.ArtifactPath -Parent
    $exportToy = Run-Export $stagedToy (Join-Path $exportRoot 'toy') '06-export-batch-a-toy-roundtrip-succeeded.png'
    $exportP1 = Run-Export $stagedP1 (Join-Path $exportRoot 'p1') '07-export-batch-a-p1-roundtrip-succeeded.png'
    $exportSciml = Run-Export $stagedSciml (Join-Path $exportRoot 'sciml') '08-export-batch-d-sciml-roundtrip-succeeded.png'

    Stop-MetBench
    $executionId = Get-LatestBatchExecutionId (Join-Path $clientOut 'SystemMT.Litedb')
    $script:proc = Start-MetBench $exe
    Attach-MainWindow $script:proc
    Open-AsyncPage
    Select-ComboItem 'AsyncOperationCombo' 'ExportReport'
    Set-TextById 'AsyncExecutionIdBox' $executionId
    Set-TextById 'AsyncExportRootBox' $reportExportRoot
    $exportReport = Submit-And-Wait 'ExportReport' '12-export-report-succeeded.png' 90
    if ($exportReport.State -ne 'Succeeded') { throw "ExportReport terminal state was $($exportReport.State)." }
    Assert-PathExists (Join-Path $reportExportRoot 'report.html') 'Report export HTML'

    Open-NavPageById 'Result' 'Nav_SystemMtResult' $null '09-result-dashboard-visible.png'
    Open-NavPageById 'Coverage' 'Nav_Coverage' $null '10-coverage-dashboard-visible.png'
    Open-NavPageById 'Anomalies' 'Nav_Anomalies' $null '11-anomaly-page-visible.png'

    $shots = @(
        '01-async-page-ready.png',
        '02-import-batch-a-toy-succeeded.png',
        '03-import-batch-a-p1-succeeded.png',
        '04-import-batch-d-sciml-succeeded.png',
        '05-runbatch-batch-a-four-mrs-succeeded.png',
        '06-export-batch-a-toy-roundtrip-succeeded.png',
        '07-export-batch-a-p1-roundtrip-succeeded.png',
        '08-export-batch-d-sciml-roundtrip-succeeded.png',
        '09-result-dashboard-visible.png',
        '10-coverage-dashboard-visible.png',
        '11-anomaly-page-visible.png',
        '12-export-report-succeeded.png')

    $lines = @(
        '# Batch A/D External MR Assets WPF VM Summary',
        '',
        "branch=$(git rev-parse --abbrev-ref HEAD)",
        "head=$(git rev-parse HEAD)",
        "origin_main=$(git rev-parse origin/main)",
        '',
        '## Commands',
        '',
        '- `dotnet build MetBench_Client\MetBench_Client.csproj --no-restore -v:minimal`: exit 0; errors 0',
        '- `dotnet run --project package-generator\BatchPackageGenerator.csproj -- <packageRoot>`: exit 0',
        '- `dotnet run --project package-generator\BatchPackageGenerator.csproj -- latest-minmr-result <db>`: exit 0',
        '',
        '## Environment',
        '',
        ('- `METBENCH_EXTRA_MR_MANIFESTS={0}`' -f $extraManifest),
        '',
        '## WPF Jobs',
        '',
        '| Operation | Scope | JobId | State | ArtifactPath |',
        '|---|---|---|---|---|',
        ('| ImportAssets | Batch A toy | {0} | {1} | {2} |' -f $importToy.JobId, $importToy.State, $importToy.ArtifactPath),
        ('| ImportAssets | Batch A P1 heat | {0} | {1} | {2} |' -f $importP1.JobId, $importP1.State, $importP1.ArtifactPath),
        ('| ImportAssets | Batch D SciML | {0} | {1} | {2} |' -f $importSciml.JobId, $importSciml.State, $importSciml.ArtifactPath),
        ('| RunBatch | Batch A 4 MRs | {0} | {1} | {2} |' -f $runBatch.JobId, $runBatch.State, $runBatch.ArtifactPath),
        ('| ExportAssets | Batch A toy staged package | {0} | {1} | {2} |' -f $exportToy.JobId, $exportToy.State, $exportToy.ArtifactPath),
        ('| ExportAssets | Batch A P1 staged package | {0} | {1} | {2} |' -f $exportP1.JobId, $exportP1.State, $exportP1.ArtifactPath),
        ('| ExportAssets | Batch D SciML staged package | {0} | {1} | {2} |' -f $exportSciml.JobId, $exportSciml.State, $exportSciml.ArtifactPath),
        ('| ExportReport | Batch A latest minmr execution | {0} | {1} | {2} |' -f $exportReport.JobId, $exportReport.State, $exportReport.ArtifactPath),
        '',
        "report_execution_id=$executionId",
        "report_export_root=$reportExportRoot",
        "report_html=$(Join-Path $reportExportRoot 'report.html')",
        '',
        '## RunBatch Result',
        '',
        '```text',
        $runBatch.Result,
        '```',
        '',
        '## Screenshots',
        '')
    foreach ($shot in $shots) {
        $lines += '- `' + $shot + '`'
    }
    $lines += ''
    $lines += '## Notes'
    $lines += ''
    $lines += '- Result, Coverage, and Anomalies pages were opened and captured after Batch A/D async jobs.'
    $lines += '- ExportReport generated `report.html` for the latest Batch A minmr execution.'
    $lines += '- Batch D remains imported-only by design; the VM check verifies import/export visibility and artifact preservation, not live MGN replay.'
    Set-Content -Path $summary -Value $lines -Encoding UTF8
}
catch {
    $message = $_ | Out-String
    Set-Content -Path (Join-Path $shotDir 'failure.txt') -Value $message -Encoding UTF8
    throw
}
finally {
    Stop-MetBench
    Pop-Location
}
