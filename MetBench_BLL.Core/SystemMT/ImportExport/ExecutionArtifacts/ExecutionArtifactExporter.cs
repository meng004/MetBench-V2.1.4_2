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
    private readonly Func<DateTime> _utcNow;

    public ExecutionArtifactExporter(
        ISystemMtResultRepository results,
        IExecutionEvidenceRepository? evidence,
        ISystemMtResultReportRenderer html,
        SystemMtReportService? markdown = null,
        Func<DateTime>? utcNow = null)
    {
        _results = results ?? throw new ArgumentNullException(nameof(results));
        _evidence = evidence;
        _html = html ?? throw new ArgumentNullException(nameof(html));
        _markdown = markdown;
        _utcNow = utcNow ?? (() => DateTime.UtcNow);
    }

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

        var record = await _results.GetAsync(request.ExecutionId.ToString(), cancellationToken)
            ?? throw new InvalidOperationException($"Execution result '{request.ExecutionId}' was not found.");

        Directory.CreateDirectory(request.ExportRoot);
        var files = new List<string>();

        await WriteJsonAsync(
            Path.Combine(request.ExportRoot, "execution-result.json"),
            record,
            files,
            cancellationToken);

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
            var evidenceMap = evidence is null
                ? null
                : new Dictionary<Guid, ExecutionEvidence> { [request.ExecutionId] = evidence };
            var htmlFile = Path.Combine(request.ExportRoot, "report.html");
            await File.WriteAllTextAsync(
                htmlFile,
                _html.Render(new[] { record }, evidenceMap),
                cancellationToken);
            files.Add("report.html");
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
}
