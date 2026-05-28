using System.Text;
using iTextSharp.text.pdf;
using MetBench_BLL.Reporting.SystemMt;
using MetBench_BLL.SystemMT.Persistence;
using MetBench_BLL.SystemMT.Reporting;
using MetBench_BLL.SystemMT.Reporting.Charts;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Reporting;

/// <summary>
/// Pins the structural surface of <see cref="PdfSystemMtResultReportRenderer"/>.
/// Linux-only / pure-managed: iTextSharp.LGPLv2.Core + SkiaSharp NoDependencies
/// Linux native asset (both already in MetBench_BLL.csproj from PR-T2-2).
/// </summary>
public sealed class PdfSystemMtResultReportRendererTests
{
    private static readonly DateTimeOffset FixedRunAt = new(2026, 5, 27, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset FixedGenAt = new(2026, 5, 27, 12, 30, 0, TimeSpan.Zero);

    private static PdfSystemMtResultReportRenderer NewRenderer() =>
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

    private static int CountPages(byte[] pdf)
    {
        var reader = new PdfReader(pdf);
        try { return reader.NumberOfPages; }
        finally { reader.Close(); }
    }

    private static string ExtractText(byte[] pdf)
    {
        // iTextSharp.LGPLv2.Core 3.7.1 does not ship the high-level text-extraction
        // subsystem (iTextSharp.text.pdf.parser is absent). Read per-page content
        // streams (uncompressed by GetPageContent) and concatenate the latin1
        // bytes. PDF text-show operators emit string literals between parens
        // (e.g. "(openmoc-pincell-nu-sigma-f) Tj"), so a plain ASCII substring
        // search over the concatenated content streams is sufficient for the
        // structural assertions in this test class — we do NOT need accurate
        // re-flowed page text, only "did this label make it into the page".
        var reader = new PdfReader(pdf);
        try
        {
            var sb = new StringBuilder();
            for (int i = 1; i <= reader.NumberOfPages; i++)
            {
                var bytes = reader.GetPageContent(i);
                sb.Append(Encoding.Latin1.GetString(bytes));
                sb.Append('\n');
            }
            return sb.ToString();
        }
        finally { reader.Close(); }
    }

    private static int CountEmbeddedImages(byte[] pdf)
    {
        // iTextSharp re-encodes incoming PNG bytes into PDF Image XObjects
        // (FlateDecode + DCTDecode wrapped in a stream). When the PNG carries
        // an alpha channel (Phase 2's SkiaSharp output does), iTextSharp emits
        // TWO XObjects per chart: a primary RGB image plus an /SMask soft-mask
        // image that holds the alpha channel. Only the primary counts as a
        // "chart"; the smask is a child resource.
        //
        // We identify primary images by walking each page's /Resources/XObject
        // dict for entries with /Subtype /Image and excluding those that are
        // referenced as some other image's /SMask. (Equivalent to: count the
        // images whose own dictionary carries an /SMask key, when alphas are
        // present; if no alpha is present anywhere on the page, count all
        // /Image entries directly.)
        var reader = new PdfReader(pdf);
        try
        {
            int count = 0;
            for (int i = 1; i <= reader.NumberOfPages; i++)
            {
                var page = reader.GetPageN(i);
                var resources = page?.GetAsDict(PdfName.Resources);
                var xobjects = resources?.GetAsDict(PdfName.Xobject);
                if (xobjects is null) continue;

                // First pass: collect the indirect-object refs of every /SMask referenced.
                var smaskRefs = new HashSet<int>();
                foreach (var key in xobjects.Keys)
                {
                    var stream = xobjects.GetAsStream(key);
                    if (stream is null) continue;
                    if (!PdfName.Image.Equals(stream.GetAsName(PdfName.Subtype))) continue;
                    var smaskRef = stream.Get(PdfName.Smask) as PdfIndirectReference;
                    if (smaskRef is not null) smaskRefs.Add(smaskRef.Number);
                }

                // Second pass: count /Image entries whose own object number is NOT a smask target.
                foreach (var key in xobjects.Keys)
                {
                    var indirect = xobjects.Get(key) as PdfIndirectReference;
                    var stream = xobjects.GetAsStream(key);
                    if (stream is null) continue;
                    if (!PdfName.Image.Equals(stream.GetAsName(PdfName.Subtype))) continue;
                    if (indirect is not null && smaskRefs.Contains(indirect.Number)) continue;
                    count++;
                }
            }
            return count;
        }
        finally { reader.Close(); }
    }

    [Fact]
    public void Constructor_rejects_null_chart_renderer()
    {
        Assert.Throws<ArgumentNullException>(() => new PdfSystemMtResultReportRenderer(null!));
    }

    [Fact]
    public void Render_throws_when_records_null()
    {
        var r = NewRenderer();
        Assert.Throws<ArgumentNullException>(() => r.Render((IEnumerable<SystemMtResultRecord>)null!));
    }

    [Fact]
    public void Render_empty_input_returns_valid_one_page_pdf()
    {
        var pdf = NewRenderer().Render(Array.Empty<SystemMtResultRecord>(), FixedContext());

        Assert.NotNull(pdf);
        Assert.True(pdf.Length > 200, $"PDF too short: {pdf.Length} bytes");
        Assert.Equal(1, CountPages(pdf));
        Assert.Contains("No run results to display", ExtractText(pdf));
        Assert.Equal(0, CountEmbeddedImages(pdf));
    }

    [Fact]
    public void Render_single_record_returns_pdf_within_size_band()
    {
        var pdf = NewRenderer().Render(new[] { SampleRecord("openmoc-pincell-nu-sigma-f", passed: true) }, FixedContext());

        // Lower bound calibrated against Linux baseline (Cloud CI ~12kB). Windows
        // ARM64 with default SkiaSharp text rendering produces a smaller chart PNG
        // (font hinting / anti-aliasing differences), driving the single-record PDF
        // down to ~8kB. 7_500 retains the "PDF is not empty" guarantee while
        // accommodating cross-platform rasterizer variance.
        Assert.True(pdf.Length > 7_500, $"PDF unexpectedly small: {pdf.Length} bytes");
        Assert.True(pdf.Length < 5_000_000, $"PDF unexpectedly large: {pdf.Length} bytes");
    }

    [Fact]
    public void Render_single_record_embeds_exactly_one_chart_png()
    {
        var pdf = NewRenderer().Render(new[] { SampleRecord("openmoc-pincell-nu-sigma-f", passed: true) }, FixedContext());

        Assert.Equal(1, CountEmbeddedImages(pdf));
    }

    [Fact]
    public void Render_multi_record_embeds_one_png_per_record()
    {
        var records = Enumerable.Range(0, 5).Select(i => SampleRecord($"mr-{i}", passed: i % 2 == 0)).ToArray();

        var pdf = NewRenderer().Render(records, FixedContext());

        Assert.Equal(5, CountEmbeddedImages(pdf));
    }

    [Fact]
    public void Render_single_record_extracted_text_contains_mr_name_and_pass_badge()
    {
        var pdf = NewRenderer().Render(new[] { SampleRecord("openmoc-pincell-nu-sigma-f", passed: true) }, FixedContext());
        var text = ExtractText(pdf);

        Assert.Contains("openmoc-pincell-nu-sigma-f", text);
        Assert.Contains("PASS", text);
        Assert.Contains("GreaterThan", text);
        Assert.Contains("k_eff", text);
    }

    [Fact]
    public void Render_failed_record_text_contains_fail_badge_and_failure_reason()
    {
        var pdf = NewRenderer().Render(new[] { SampleRecord("openmoc-pincell-sigma-a", passed: false) }, FixedContext());
        var text = ExtractText(pdf);

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

        var pdf = NewRenderer().Render(new[] { rec }, map, FixedContext());
        var text = ExtractText(pdf);

        Assert.Contains("Typed verification", text);
        Assert.Contains("BolAlg02", text);
        Assert.Contains("VarianceRatioPredicate", text);
        Assert.Contains("Passed", text);
    }

    [Fact]
    public void Render_without_evidence_dictionary_omits_typed_verification_block()
    {
        var pdf = NewRenderer().Render(new[] { SampleRecord("mr-x", passed: true) }, FixedContext());

        Assert.DoesNotContain("Typed verification", ExtractText(pdf));
    }

    [Fact]
    public void Render_two_calls_produce_structurally_identical_pdfs()
    {
        // iTextSharp injects /CreationDate / /ModDate / /ID metadata each call,
        // so raw byte equality is not guaranteed. Pin structural equivalence:
        // page count, embedded image count, and full extracted page text match.
        var records = new[]
        {
            SampleRecord("mr-a", passed: true),
            SampleRecord("mr-b", passed: false),
        };

        var renderer = NewRenderer();
        var first = renderer.Render(records, FixedContext());
        var second = renderer.Render(records, FixedContext());

        Assert.Equal(CountPages(first), CountPages(second));
        Assert.Equal(CountEmbeddedImages(first), CountEmbeddedImages(second));
        Assert.Equal(ExtractText(first), ExtractText(second));
    }

    [Fact]
    public void Render_report_context_title_appears_on_first_page()
    {
        var ctx = FixedContext("Custom Boltzmann Report");
        var pdf = NewRenderer().Render(new[] { SampleRecord("mr-x", passed: true) }, ctx);

        Assert.Contains("Custom Boltzmann Report", ExtractText(pdf));
    }
}
