# T0-T2 Gap-Fill A1/A3 WPF Wiring VM Prompt

切换到分支 `claude/upbeat-fermi-CrlXj`（或合并后的 `main`），读取本文件，执行任务。

## 背景

云端已合并 A1（异步导出补 Word/Excel/PDF，PR #308）与 A3（`ExportReport` 异步 handler，PR #310）。`ExecutionArtifactExporter` 与 `ExportReportJobOperationHandler` 已就绪，但 WPF 的作业 worker 组合根尚未注入新渲染器 / 注册新 handler，因此运行中的 WPF 异步作业暂时只输出 HTML、且 `ExportReport` 提交后会在分派处失败。本任务做这两处 WPF 接线并截图验证。**仅改 WPF 组合根，不改 BLL.Core。**

## Preconditions

- `git status --short --branch` 显示目标分支、无无关脏文件。
- `dotnet --info` 成功。

## 改动点（唯一文件：`MetBench_Client/Hosting/SystemMtJobWorkerHostedService.cs`）

当前（约 63-72 行）：

```csharp
var operationDispatcher = new SystemMtJobOperationDispatcher(new ISystemMtJobOperationHandler[]
{
    new RunBatchJobOperationHandler(launcher, evidenceRepository),
    new ImportAssetsJobOperationHandler(new SutImportStagingService()),
    new ExportAssetsJobOperationHandler(),
    new ExportExecutionArtifactsJobOperationHandler(new ExecutionArtifactExporter(
        resultRepository,
        evidenceRepository,
        reportRenderer)),
});
```

改为（注入 Word/Excel/PDF 渲染器 + 注册 ExportReport handler）：

```csharp
// using MetBench_BLL.Reporting.SystemMt; + MetBench_BLL.Reporting.SystemMt.Charts.Rendering;
var chartRenderer = new SkiaChartRenderer();
var wordRenderer = new WordSystemMtResultReportRenderer(chartRenderer);
var excelRenderer = new ExcelSystemMtResultReportRenderer(chartRenderer);
var pdfRenderer = new PdfSystemMtResultReportRenderer(chartRenderer);

var artifactExporter = new ExecutionArtifactExporter(
    resultRepository,
    evidenceRepository,
    reportRenderer,
    markdown: null,
    word: wordRenderer,
    excel: excelRenderer,
    pdf: pdfRenderer);

var operationDispatcher = new SystemMtJobOperationDispatcher(new ISystemMtJobOperationHandler[]
{
    new RunBatchJobOperationHandler(launcher, evidenceRepository),
    new ImportAssetsJobOperationHandler(new SutImportStagingService()),
    new ExportAssetsJobOperationHandler(),
    new ExportExecutionArtifactsJobOperationHandler(artifactExporter),
    new ExportReportJobOperationHandler(artifactExporter),
});
```

注意：
- `SkiaChartRenderer` 在 `MetBench_BLL.Reporting.SystemMt.Charts.Rendering`；三个 report 渲染器在 `MetBench_BLL.Reporting.SystemMt`；`ExecutionArtifactExporter` 在 `MetBench_BLL.Core.SystemMT.ImportExport.ExecutionArtifacts`；`ExportReportJobOperationHandler` 在 `MetBench_BLL.SystemMT.Jobs`。补齐 `using`。
- 若 `WpfAsyncJobCancellationWiringTests` 的 source-scan 断言需要同步更新（它断言 hosted service 含特定 handler 注册），按其断言文本调整或扩断言以包含 `ExportReportJobOperationHandler`。

## Core steps

1. `dotnet build MetBench.sln --no-restore -v:minimal` —— 期望 0 errors。
2. 打开 WPF，导航到 System-MT 异步作业页。
3. 用一个纯 stdlib MR（如 `advection-amplitude-linearity`）跑一次异步执行，记下 execution id。
4. 提交 `ExportExecutionArtifacts` 作业，导出根设 `%TEMP%\metbench-gapfill\exec-export\`；**验证产物含 `report.html` + `report.docx` + `report.xlsx` + `report.pdf` + `manifest.json` + `execution-result.json`**（A1）。
5. 提交 `ExportReport` 作业，导出根设 `%TEMP%\metbench-gapfill\report-only\`；**验证终态 Succeeded、产物含 `report.html`（+ docx/xlsx/pdf）与 `manifest.json`，但无 `execution-result.json` / `execution-evidence.json`**（A3 report-only）。
6. 截图：四端导出产物列表、ExportReport report-only 产物列表、各作业终态 Succeeded + artifact 路径。存到 `docs/superpowers/specs/2026-06-06-t0-t2-gap-fill-vm-evidence/`。
7. 跑 WPF/守卫聚焦测试：`dotnet test MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~ExportReport|FullyQualifiedName~ExecutionArtifact|FullyQualifiedName~WpfAsync"`。
8. 写 `vm-summary.md`：精确命令、退出码、测试计数、截图文件名、job id、artifact 路径、阻塞项（如有）。

## Acceptance

- WPF build 0 errors。
- `ExportExecutionArtifacts` 异步作业终态 Succeeded，产物含 docx/xlsx/pdf（A1 在运行应用中生效）。
- `ExportReport` 异步作业终态 Succeeded，report-only 产物无 result/evidence json（A3 在运行应用中生效）。
- UI 不阻塞。
- 任一前置缺失则停下报告阻塞项，不得谎报 pass。
