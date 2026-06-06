namespace MetBench_BLL.Core.SystemMT.ImportExport.ExecutionArtifacts;

public sealed record ExecutionArtifactExportRequest(
    Guid ExecutionId,
    string ExportRoot,
    bool IncludeEvidence = true,
    bool IncludeHtml = true,
    bool IncludeMarkdown = true,
    bool IncludeWord = false,
    bool IncludeExcel = false,
    bool IncludePdf = false);
