using System.Text.Json;
using System.Text.Json.Serialization;
using MetBench_BLL.Reporting;
using MetBench_BLL.SystemMT.Persistence;
using MetBench_BLL.SystemMT.Reporting;

namespace MetBench_BLL.Core.SystemMT.ImportExport.ExecutionArtifacts;

public sealed class ExecutionArtifactExporter
{
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly ISystemMtResultRepository _results;
    private readonly IExecutionEvidenceRepository? _evidence;
    private readonly ISystemMtResultReportRenderer _html;
    private readonly SystemMtReportService? _markdown;
    private readonly IWordSystemMtResultReportRenderer? _word;
    private readonly IExcelSystemMtResultReportRenderer? _excel;
    private readonly IPdfSystemMtResultReportRenderer? _pdf;
    private readonly Func<DateTime> _utcNow;

    public ExecutionArtifactExporter(
        ISystemMtResultRepository results,
        IExecutionEvidenceRepository? evidence,
        ISystemMtResultReportRenderer html,
        SystemMtReportService? markdown = null,
        IWordSystemMtResultReportRenderer? word = null,
        IExcelSystemMtResultReportRenderer? excel = null,
        IPdfSystemMtResultReportRenderer? pdf = null,
        Func<DateTime>? utcNow = null)
    {
        _results = results ?? throw new ArgumentNullException(nameof(results));
        _evidence = evidence;
        _html = html ?? throw new ArgumentNullException(nameof(html));
        _markdown = markdown;
        _word = word;
        _excel = excel;
        _pdf = pdf;
        _utcNow = utcNow ?? (() => DateTime.UtcNow);
    }

    /// <summary>True when a Word (<c>.docx</c>) renderer is available for export.</summary>
    public bool HasWordRenderer => _word is not null;

    /// <summary>True when an Excel (<c>.xlsx</c>) renderer is available for export.</summary>
    public bool HasExcelRenderer => _excel is not null;

    /// <summary>True when a PDF renderer is available for export.</summary>
    public bool HasPdfRenderer => _pdf is not null;

    public async Task<string> ExportAsync(
        ExecutionArtifactExportRequest request,
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));
        if (request.ExecutionId == Guid.Empty)
            throw new ArgumentException("ExecutionId must be a non-empty Guid.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.ExportRoot))
            throw new ArgumentException("ExportRoot must be non-blank.", nameof(request));
        if (request.IncludeMarkdown && _markdown is null)
            throw new InvalidOperationException("Markdown execution report export requires SystemMtReportService.");
        if (request.IncludeWord && _word is null)
            throw new InvalidOperationException("Word execution report export requires IWordSystemMtResultReportRenderer.");
        if (request.IncludeExcel && _excel is null)
            throw new InvalidOperationException("Excel execution report export requires IExcelSystemMtResultReportRenderer.");
        if (request.IncludePdf && _pdf is null)
            throw new InvalidOperationException("PDF execution report export requires IPdfSystemMtResultReportRenderer.");

        var record = await _results.GetAsync(request.ExecutionId.ToString(), cancellationToken)
            ?? throw new InvalidOperationException($"Execution result '{request.ExecutionId}' was not found.");

        Directory.CreateDirectory(request.ExportRoot);
        var files = new List<string>();

        if (request.IncludeResultJson)
        {
            await WriteJsonAsync(
                Path.Combine(request.ExportRoot, "execution-result.json"),
                record,
                files,
                cancellationToken);
        }

        ExecutionEvidence? evidence = null;
        if (request.IncludeEvidence && _evidence is not null)
        {
            evidence = await _evidence.GetByExecutionAsync(request.ExecutionId, cancellationToken);
            if (evidence is not null)
            {
                await WriteJsonAsync(
                    Path.Combine(request.ExportRoot, "execution-evidence.json"),
                    evidence,
                    files,
                    cancellationToken);
            }
        }

        if (request.IncludeHtml)
        {
            var htmlFile = Path.Combine(request.ExportRoot, "report.html");
            await File.WriteAllTextAsync(
                htmlFile,
                _html.Render(new[] { record }, EvidenceMap(request.ExecutionId, evidence)),
                cancellationToken);
            files.Add("report.html");
        }

        if (request.IncludeWord)
        {
            await WriteBytesAsync(
                Path.Combine(request.ExportRoot, "report.docx"),
                _word!.Render(new[] { record }, EvidenceMap(request.ExecutionId, evidence)),
                files,
                cancellationToken);
        }

        if (request.IncludeExcel)
        {
            await WriteBytesAsync(
                Path.Combine(request.ExportRoot, "report.xlsx"),
                _excel!.Render(new[] { record }, EvidenceMap(request.ExecutionId, evidence)),
                files,
                cancellationToken);
        }

        if (request.IncludePdf)
        {
            await WriteBytesAsync(
                Path.Combine(request.ExportRoot, "report.pdf"),
                _pdf!.Render(new[] { record }, EvidenceMap(request.ExecutionId, evidence)),
                files,
                cancellationToken);
        }

        if (request.IncludeMarkdown)
        {
            var markdownFile = Path.Combine(request.ExportRoot, "report.md");
            _markdown!.GenerateExecution(request.ExecutionId, markdownFile);
            files.Add("report.md");
        }

        var manifest = new ExecutionArtifactExportManifest(
            request.ExecutionId,
            jobId,
            _utcNow(),
            files.ToArray());
        var manifestFile = Path.Combine(request.ExportRoot, ExecutionArtifactExportManifest.FileName);
        await File.WriteAllTextAsync(
            manifestFile,
            JsonSerializer.Serialize(manifest, JsonOptions),
            cancellationToken);
        return manifestFile;
    }

    private static async Task WriteJsonAsync<T>(
        string path,
        T value,
        ICollection<string> files,
        CancellationToken cancellationToken)
    {
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(value, JsonOptions), cancellationToken);
        files.Add(Path.GetFileName(path));
    }

    private static async Task WriteBytesAsync(
        string path,
        byte[] bytes,
        ICollection<string> files,
        CancellationToken cancellationToken)
    {
        await File.WriteAllBytesAsync(path, bytes, cancellationToken);
        files.Add(Path.GetFileName(path));
    }

    private static IReadOnlyDictionary<Guid, ExecutionEvidence>? EvidenceMap(
        Guid executionId,
        ExecutionEvidence? evidence) =>
        evidence is null
            ? null
            : new Dictionary<Guid, ExecutionEvidence> { [executionId] = evidence };
}
