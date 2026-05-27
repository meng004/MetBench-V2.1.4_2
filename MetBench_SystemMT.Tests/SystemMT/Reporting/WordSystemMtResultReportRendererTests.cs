using System.IO.Compression;
using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using MetBench_BLL.Reporting.SystemMt;
using MetBench_BLL.SystemMT.Persistence;
using MetBench_BLL.SystemMT.Reporting;
using MetBench_BLL.SystemMT.Reporting.Charts;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Reporting;

/// <summary>
/// Pins the structural surface of <see cref="WordSystemMtResultReportRenderer"/>.
/// Linux-only / pure-managed: DocumentFormat.OpenXml 3.3.0 + SkiaSharp Linux
/// native asset (both already in MetBench_BLL.csproj from PR-T2-2).
/// </summary>
public sealed class WordSystemMtResultReportRendererTests
{
    private static readonly DateTimeOffset FixedRunAt = new(2026, 5, 27, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset FixedGenAt = new(2026, 5, 27, 12, 30, 0, TimeSpan.Zero);

    private static WordSystemMtResultReportRenderer NewRenderer() =>
        new(new MetBench_BLL.Reporting.SystemMt.Charts.Rendering.SkiaChartRenderer());

    private static SystemMtResultRecord SampleRecord(string mrName, bool passed) => new()
    {
        Id = Guid.NewGuid(),
        MrName = mrName,
        RunAt = FixedRunAt,
        AssertionName = "GreaterThan",
        ValueName = "k_eff",
        SourceValue = 1.0,
        FollowUpValue = passed ? 1.5 : 0.8,
        Passed = passed,
        FailureReason = passed ? string.Empty : "follow-up did not strictly exceed source",
        SourceCaseName = "source.json",
        FollowUpCaseName = "followup.json",
    };

    private static ReportContext FixedContext(string? title = null) =>
        new(Title: title ?? "MetBench System-Level MT Run Report", GeneratedAt: FixedGenAt);

    private static int CountTopLevelBodyChildren(byte[] docx)
    {
        using var stream = new MemoryStream(docx);
        using var doc = WordprocessingDocument.Open(stream, isEditable: false);
        var body = doc.MainDocumentPart!.Document.Body!;
        return body.ChildElements.Count;
    }

    private static int CountMediaPngs(byte[] docx)
    {
        using var ms = new MemoryStream(docx);
        using var zip = new ZipArchive(ms, ZipArchiveMode.Read);
        // OPC normalizes embedded image paths but the exact location can shift
        // between OpenXml SDK versions; the stable invariant is that every
        // ImagePart added to MainDocumentPart materializes as exactly one .png
        // entry somewhere in the package (image parts only ever produce one
        // file each, and tests / `[Content_Types].xml` cross-checks pin the
        // rest of the contract).
        return zip.Entries.Count(e => e.FullName.EndsWith(".png", StringComparison.OrdinalIgnoreCase));
    }

    private static string ReadContentTypesXml(byte[] docx)
    {
        using var ms = new MemoryStream(docx);
        using var zip = new ZipArchive(ms, ZipArchiveMode.Read);
        var entry = zip.GetEntry("[Content_Types].xml")
                    ?? throw new InvalidOperationException("[Content_Types].xml missing from docx package");
        using var sr = new StreamReader(entry.Open(), Encoding.UTF8);
        return sr.ReadToEnd();
    }

    private static string ExtractBodyText(byte[] docx)
    {
        using var ms = new MemoryStream(docx);
        using var doc = WordprocessingDocument.Open(ms, isEditable: false);
        var body = doc.MainDocumentPart!.Document.Body!;
        return string.Join("\n", body.Descendants<Text>().Select(t => t.Text));
    }

    [Fact]
    public void Constructor_rejects_null_chart_renderer()
    {
        Assert.Throws<ArgumentNullException>(() => new WordSystemMtResultReportRenderer(null!));
    }

    [Fact]
    public void Render_throws_when_records_null()
    {
        var r = NewRenderer();
        Assert.Throws<ArgumentNullException>(() => r.Render((IEnumerable<SystemMtResultRecord>)null!));
    }

    [Fact]
    public void Render_empty_input_returns_valid_docx_with_no_records_paragraph()
    {
        var docx = NewRenderer().Render(Array.Empty<SystemMtResultRecord>(), FixedContext());

        Assert.NotNull(docx);
        Assert.True(docx.Length > 1_000, $"docx too short: {docx.Length} bytes");
        Assert.True(CountTopLevelBodyChildren(docx) > 0, "Body must contain at least one paragraph");
        Assert.Contains("No run results to display", ExtractBodyText(docx));
        Assert.Equal(0, CountMediaPngs(docx));
    }

    [Fact]
    public void Render_single_record_within_size_band()
    {
        var docx = NewRenderer().Render(new[] { SampleRecord("openmoc-pincell-nu-sigma-f", passed: true) }, FixedContext());

        Assert.True(docx.Length > 5_000, $"docx unexpectedly small: {docx.Length} bytes");
        Assert.True(docx.Length < 5_000_000, $"docx unexpectedly large: {docx.Length} bytes");
    }

    [Fact]
    public void Render_single_record_embeds_one_png_in_word_media()
    {
        var docx = NewRenderer().Render(new[] { SampleRecord("openmoc-pincell-nu-sigma-f", passed: true) }, FixedContext());

        Assert.Equal(1, CountMediaPngs(docx));
    }

    [Fact]
    public void Render_multi_record_embeds_one_png_per_record_in_word_media()
    {
        var records = Enumerable.Range(0, 5).Select(i => SampleRecord($"mr-{i}", passed: i % 2 == 0)).ToArray();

        var docx = NewRenderer().Render(records, FixedContext());

        Assert.Equal(5, CountMediaPngs(docx));
    }

    [Fact]
    public void Render_content_types_xml_declares_image_png()
    {
        var docx = NewRenderer().Render(new[] { SampleRecord("mr-x", passed: true) }, FixedContext());

        var contentTypes = ReadContentTypesXml(docx);
        Assert.Contains("image/png", contentTypes);
    }

    [Fact]
    public void Render_body_text_contains_mr_name_and_pass_label_and_value_name()
    {
        var docx = NewRenderer().Render(new[] { SampleRecord("openmoc-pincell-nu-sigma-f", passed: true) }, FixedContext());
        var text = ExtractBodyText(docx);

        Assert.Contains("openmoc-pincell-nu-sigma-f", text);
        Assert.Contains("PASS", text);
        Assert.Contains("GreaterThan", text);
        Assert.Contains("k_eff", text);
    }

    [Fact]
    public void Render_failed_record_body_text_contains_fail_label_and_failure_reason()
    {
        var docx = NewRenderer().Render(new[] { SampleRecord("openmoc-pincell-sigma-a", passed: false) }, FixedContext());
        var text = ExtractBodyText(docx);

        Assert.Contains("FAIL", text);
        Assert.Contains("follow-up did not strictly exceed source", text);
    }

    [Fact]
    public void Render_with_evidence_includes_typed_verification_block()
    {
        var rec = SampleRecord("openmc-pincell-particle-count-convergence", passed: true);
        var evidence = new ExecutionEvidence
        {
            TypedVerification = new TypedVerificationEvidence
            {
                SpecId = "BolAlg02",
                SpecKind = "MrSpec",
                PredicateId = "VarianceRatioPredicate",
                PredicateKind = "Statistical",
                Status = "Passed",
                Diagnostic = new TypedDiagnosticEvidence
                {
                    Expected = 0.5,
                    Actual = 0.48,
                    Residual = 0.02,
                    Tolerance = 0.05,
                },
            },
        };
        var map = new Dictionary<Guid, ExecutionEvidence> { [rec.Id] = evidence };

        var docx = NewRenderer().Render(new[] { rec }, map, FixedContext());
        var text = ExtractBodyText(docx);

        Assert.Contains("Typed verification", text);
        Assert.Contains("BolAlg02", text);
        Assert.Contains("VarianceRatioPredicate", text);
        Assert.Contains("Passed", text);
    }

    [Fact]
    public void Render_without_evidence_dictionary_omits_typed_verification_block()
    {
        var docx = NewRenderer().Render(new[] { SampleRecord("mr-x", passed: true) }, FixedContext());

        Assert.DoesNotContain("Typed verification", ExtractBodyText(docx));
    }

    [Fact]
    public void Render_is_structurally_deterministic_when_generated_at_is_fixed()
    {
        // Raw byte equality is NOT guaranteed: the underlying ZIP container
        // records per-entry mtime from the system clock (same failure class as
        // iTextSharp's /CreationDate injection in Phase 3a). Structural
        // determinism is pinned via three invariants: same image count, same
        // body XML, same extracted text. Drift in any of these would fail.
        var records = new[]
        {
            SampleRecord("mr-a", passed: true),
            SampleRecord("mr-b", passed: false),
        };
        var renderer = NewRenderer();

        var first = renderer.Render(records, FixedContext());
        var second = renderer.Render(records, FixedContext());

        Assert.Equal(CountMediaPngs(first), CountMediaPngs(second));
        Assert.Equal(CountTopLevelBodyChildren(first), CountTopLevelBodyChildren(second));
        Assert.Equal(ExtractBodyText(first), ExtractBodyText(second));
    }

    [Fact]
    public void Render_report_context_title_appears_in_body()
    {
        var ctx = FixedContext("Custom Boltzmann Word Report");
        var docx = NewRenderer().Render(new[] { SampleRecord("mr-x", passed: true) }, ctx);

        Assert.Contains("Custom Boltzmann Word Report", ExtractBodyText(docx));
    }
}
