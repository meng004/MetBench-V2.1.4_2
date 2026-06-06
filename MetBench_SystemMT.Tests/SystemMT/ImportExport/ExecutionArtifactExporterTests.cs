using System.Text.Json;
using MetBench_BLL.Core.SystemMT.ImportExport.ExecutionArtifacts;
using MetBench_BLL.SystemMT;
using MetBench_BLL.SystemMT.Persistence;
using MetBench_BLL.SystemMT.Reporting;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.ImportExport;

public sealed class ExecutionArtifactExporterTests
{
    [Fact]
    public async Task ExportAsync_missing_execution_result_fails_with_clear_diagnostic()
    {
        using var temp = TempDirectory.Create();
        var executionId = Guid.NewGuid();
        var exporter = new ExecutionArtifactExporter(
            new FakeResultRepository(),
            new FakeEvidenceRepository(),
            new HtmlSystemMtResultReportRenderer());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            exporter.ExportAsync(
                new ExecutionArtifactExportRequest(executionId, temp.Root, IncludeMarkdown: false),
                Guid.NewGuid()));

        Assert.Contains($"Execution result '{executionId}' was not found", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExportAsync_writes_manifest_result_evidence_and_html_report()
    {
        using var temp = TempDirectory.Create();
        var executionId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var resultRepo = new FakeResultRepository(MakeRecord(executionId));
        var evidenceRepo = new FakeEvidenceRepository(MakeEvidence(executionId));
        var exporter = new ExecutionArtifactExporter(
            resultRepo,
            evidenceRepo,
            new HtmlSystemMtResultReportRenderer());

        var manifestPath = await exporter.ExportAsync(
            new ExecutionArtifactExportRequest(executionId, temp.Root, IncludeMarkdown: false),
            jobId);

        Assert.Equal(Path.Combine(temp.Root, "manifest.json"), manifestPath);
        Assert.True(File.Exists(Path.Combine(temp.Root, "execution-result.json")));
        Assert.True(File.Exists(Path.Combine(temp.Root, "execution-evidence.json")));
        Assert.True(File.Exists(Path.Combine(temp.Root, "report.html")));

        var manifest = JsonSerializer.Deserialize<ExecutionArtifactExportManifest>(
            File.ReadAllText(manifestPath),
            ExecutionArtifactExporter.JsonOptions);
        Assert.NotNull(manifest);
        Assert.Equal(executionId, manifest!.ExecutionId);
        Assert.Equal(jobId, manifest.JobId);
        Assert.Contains("execution-result.json", manifest.Files);
        Assert.Contains("execution-evidence.json", manifest.Files);
        Assert.Contains("report.html", manifest.Files);

        var html = File.ReadAllText(Path.Combine(temp.Root, "report.html"));
        Assert.Contains("p5-power-response", html);
    }

    [Fact]
    public async Task ExportAsync_omits_evidence_file_when_evidence_is_absent()
    {
        using var temp = TempDirectory.Create();
        var executionId = Guid.NewGuid();
        var exporter = new ExecutionArtifactExporter(
            new FakeResultRepository(MakeRecord(executionId)),
            new FakeEvidenceRepository(),
            new HtmlSystemMtResultReportRenderer());

        var manifestPath = await exporter.ExportAsync(
            new ExecutionArtifactExportRequest(executionId, temp.Root, IncludeMarkdown: false),
            Guid.NewGuid());

        Assert.False(File.Exists(Path.Combine(temp.Root, "execution-evidence.json")));
        var manifest = JsonSerializer.Deserialize<ExecutionArtifactExportManifest>(
            File.ReadAllText(manifestPath),
            ExecutionArtifactExporter.JsonOptions);
        Assert.DoesNotContain("execution-evidence.json", manifest!.Files);
    }

    [Fact]
    public async Task ExportAsync_markdown_requested_without_report_service_fails_closed()
    {
        using var temp = TempDirectory.Create();
        var executionId = Guid.NewGuid();
        var exporter = new ExecutionArtifactExporter(
            new FakeResultRepository(MakeRecord(executionId)),
            new FakeEvidenceRepository(),
            new HtmlSystemMtResultReportRenderer());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            exporter.ExportAsync(new ExecutionArtifactExportRequest(executionId, temp.Root), Guid.NewGuid()));

        Assert.Contains("Markdown execution report export requires SystemMtReportService", ex.Message);
        Assert.False(File.Exists(Path.Combine(temp.Root, "manifest.json")));
    }

    [Fact]
    public async Task ExportAsync_writes_word_excel_pdf_reports_when_renderers_present()
    {
        using var temp = TempDirectory.Create();
        var executionId = Guid.NewGuid();
        var renderer = new FakeBinaryReportRenderer();
        var exporter = new ExecutionArtifactExporter(
            new FakeResultRepository(MakeRecord(executionId)),
            new FakeEvidenceRepository(MakeEvidence(executionId)),
            new HtmlSystemMtResultReportRenderer(),
            markdown: null,
            word: renderer,
            excel: renderer,
            pdf: renderer);

        Assert.True(exporter.HasWordRenderer);
        Assert.True(exporter.HasExcelRenderer);
        Assert.True(exporter.HasPdfRenderer);

        var manifestPath = await exporter.ExportAsync(
            new ExecutionArtifactExportRequest(
                executionId,
                temp.Root,
                IncludeMarkdown: false,
                IncludeWord: true,
                IncludeExcel: true,
                IncludePdf: true),
            Guid.NewGuid());

        Assert.True(File.Exists(Path.Combine(temp.Root, "report.docx")));
        Assert.True(File.Exists(Path.Combine(temp.Root, "report.xlsx")));
        Assert.True(File.Exists(Path.Combine(temp.Root, "report.pdf")));

        var manifest = JsonSerializer.Deserialize<ExecutionArtifactExportManifest>(
            File.ReadAllText(manifestPath),
            ExecutionArtifactExporter.JsonOptions);
        Assert.Contains("report.docx", manifest!.Files);
        Assert.Contains("report.xlsx", manifest.Files);
        Assert.Contains("report.pdf", manifest.Files);
        Assert.Equal(new byte[] { 1, 2, 3, 4 }, File.ReadAllBytes(Path.Combine(temp.Root, "report.docx")));
    }

    [Theory]
    [InlineData("Word", true, false, false)]
    [InlineData("Excel", false, true, false)]
    [InlineData("PDF", false, false, true)]
    public async Task ExportAsync_format_requested_without_renderer_fails_closed(
        string label, bool word, bool excel, bool pdf)
    {
        using var temp = TempDirectory.Create();
        var executionId = Guid.NewGuid();
        var exporter = new ExecutionArtifactExporter(
            new FakeResultRepository(MakeRecord(executionId)),
            new FakeEvidenceRepository(),
            new HtmlSystemMtResultReportRenderer());

        Assert.False(exporter.HasWordRenderer);
        Assert.False(exporter.HasExcelRenderer);
        Assert.False(exporter.HasPdfRenderer);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            exporter.ExportAsync(
                new ExecutionArtifactExportRequest(
                    executionId,
                    temp.Root,
                    IncludeMarkdown: false,
                    IncludeWord: word,
                    IncludeExcel: excel,
                    IncludePdf: pdf),
                Guid.NewGuid()));

        Assert.Contains(label, ex.Message, StringComparison.Ordinal);
        Assert.False(File.Exists(Path.Combine(temp.Root, "manifest.json")));
    }

    private static SystemMtResultRecord MakeRecord(Guid executionId) => new()
    {
        Id = executionId,
        MrName = "p5-power-response",
        RunAt = new DateTimeOffset(2026, 6, 5, 0, 0, 0, TimeSpan.Zero),
        AssertionName = "greater",
        ValueName = "power_extrema",
        SourceValue = 1.0,
        FollowUpValue = 2.0,
        Passed = true,
        SourceCaseName = "source",
        FollowUpCaseName = "follow-up",
        SourceExitCode = 0,
        FollowUpExitCode = 0,
    };

    private static ExecutionEvidence MakeEvidence(Guid executionId) => new()
    {
        IdEvidence = Guid.NewGuid(),
        ExecutionId = executionId,
        RuntimeEvidence = new RuntimeEvidence
        {
            RuntimeKey = "python-stdlib",
            RuntimeKind = "Python",
            Passed = true,
        },
    };

    private sealed class FakeResultRepository : ISystemMtResultRepository
    {
        private readonly Dictionary<string, SystemMtResultRecord> _records = new(StringComparer.Ordinal);

        public FakeResultRepository(params SystemMtResultRecord[] records)
        {
            foreach (var record in records)
                _records[record.Id.ToString()] = record;
        }

        public Task<SystemMtResultRecord?> GetAsync(string id, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _records.TryGetValue(id, out var record);
            return Task.FromResult<SystemMtResultRecord?>(record);
        }

        public Task<string> SaveAsync(string mrName, SystemMtResult result, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<string> SaveAsync(SystemMtResultRecord record, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<IReadOnlyList<SystemMtResultRecord>> ListRecentAsync(int limit = 100, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<IReadOnlyList<SystemMtResultRecord>> ListByMrNameAsync(string mrName, int limit = 100, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<MetBench_BLL.Paging.PagedResult<SystemMtResultRecord>> ListPagedAsync(MetBench_BLL.Paging.PageRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<MetBench_BLL.Paging.PagedResult<SystemMtResultRecord>> ListPagedByMrNameAsync(string mrName, MetBench_BLL.Paging.PageRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<int> DeleteBatchAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class FakeEvidenceRepository : IExecutionEvidenceRepository
    {
        private readonly Dictionary<Guid, ExecutionEvidence> _records = new();

        public FakeEvidenceRepository(params ExecutionEvidence[] records)
        {
            foreach (var record in records)
                _records[record.ExecutionId] = record;
        }

        public Task<ExecutionEvidence?> GetByExecutionAsync(Guid executionId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _records.TryGetValue(executionId, out var evidence);
            return Task.FromResult<ExecutionEvidence?>(evidence);
        }

        public Task SaveAsync(ExecutionEvidence evidence, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> DeleteByExecutionIdAsync(Guid executionId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class FakeBinaryReportRenderer
        : IWordSystemMtResultReportRenderer, IExcelSystemMtResultReportRenderer, IPdfSystemMtResultReportRenderer
    {
        public byte[] Render(IEnumerable<SystemMtResultRecord> records, ReportContext? context = null)
            => new byte[] { 1, 2, 3, 4 };

        public byte[] Render(
            IEnumerable<SystemMtResultRecord> records,
            IReadOnlyDictionary<Guid, ExecutionEvidence>? evidenceByExecutionId,
            ReportContext? context = null)
            => new byte[] { 1, 2, 3, 4 };
    }

    private sealed class TempDirectory : IDisposable
    {
        public string Root { get; }

        private TempDirectory(string root) => Root = root;

        public static TempDirectory Create() =>
            new(Path.Combine(Path.GetTempPath(), "MetBenchExecutionArtifactExporterTests", Guid.NewGuid().ToString("N")));

        public void Dispose()
        {
            if (Directory.Exists(Root))
                Directory.Delete(Root, recursive: true);
        }
    }
}
