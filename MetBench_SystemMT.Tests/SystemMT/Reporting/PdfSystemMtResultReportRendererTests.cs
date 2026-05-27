using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using iTextSharp.text.pdf;
using MetBench_BLL.Reporting.SystemMt;
using MetBench_BLL.Reporting.SystemMt.Charts.Rendering;
using MetBench_BLL.SystemMT.Persistence;
using MetBench_BLL.SystemMT.Reporting;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Reporting;

public sealed class PdfSystemMtResultReportRendererTests
{
    private static readonly byte[] PdfMagic = { 0x25, 0x50, 0x44, 0x46, 0x2D }; // "%PDF-"

    private static SystemMtResultRecord MakeRecord(
        Guid? id = null,
        string mrName = "openmoc-pincell-nu-sigma-f",
        string assertion = "GreaterThan",
        string valueName = "k_eff",
        double source = 1.0,
        double followUp = 1.5,
        bool passed = true,
        string failureReason = "")
        => new()
        {
            Id = id ?? Guid.NewGuid(),
            MrName = mrName,
            AssertionName = assertion,
            ValueName = valueName,
            SourceValue = source,
            FollowUpValue = followUp,
            Passed = passed,
            FailureReason = failureReason,
            RunAt = new DateTimeOffset(2026, 5, 27, 12, 0, 0, TimeSpan.Zero),
            SourceElapsed = TimeSpan.FromSeconds(1.25),
            FollowUpElapsed = TimeSpan.FromSeconds(1.5),
        };

    private static PdfSystemMtResultReportRenderer NewRenderer()
        => new(new SkiaChartRenderer());

    /// <summary>
    /// iTextSharp's LGPLv2 fork does NOT ship <c>PdfTextExtractor</c>, so we
    /// reverse-validate via two complementary channels:
    /// 1. <see cref="PdfReader"/> for structural facts (page count, image count
    ///    by walking each page's /Resources /XObject dictionary).
    /// 2. A raw-byte ASCII search for content assertions ("MR name appears",
    ///    "PASS appears"). Helvetica with ASCII glyphs serialises bytes-as-is
    ///    inside the content stream's `(text) Tj` operator.
    /// </summary>
    private static (int Pages, int Images) InspectStructure(byte[] pdfBytes)
    {
        using var reader = new PdfReader(pdfBytes);
        var pages = reader.NumberOfPages;
        var imageCount = 0;
        for (var p = 1; p <= pages; p++)
        {
            var page = reader.GetPageN(p);
            var resources = page?.GetAsDict(PdfName.Resources);
            if (resources is null) continue;
            var xobjects = resources.GetAsDict(PdfName.Xobject);
            if (xobjects is null) continue;
            foreach (var name in xobjects.Keys)
            {
                var stream = xobjects.GetAsStream(name);
                if (stream is null) continue;
                if (PdfName.Image.Equals(stream.Get(PdfName.Subtype)))
                {
                    imageCount++;
                }
            }
        }
        return (pages, imageCount);
    }

    /// <summary>
    /// Decompresses each page's content stream and concatenates them, then
    /// searches for an ASCII needle. iTextSharp Flate-compresses content
    /// streams by default, so a raw byte search on the PDF file would miss
    /// every text token.
    /// </summary>
    private static bool DecompressedContentContainsAscii(byte[] pdfBytes, string needle)
    {
        using var reader = new PdfReader(pdfBytes);
        var needleBytes = Encoding.ASCII.GetBytes(needle);
        for (var p = 1; p <= reader.NumberOfPages; p++)
        {
            var content = reader.GetPageContent(p);
            for (var i = 0; i + needleBytes.Length <= content.Length; i++)
            {
                var ok = true;
                for (var j = 0; j < needleBytes.Length; j++)
                {
                    if (content[i + j] != needleBytes[j]) { ok = false; break; }
                }
                if (ok) return true;
            }
        }
        return false;
    }

    [Fact]
    public void Render_single_record_returns_valid_pdf_magic()
    {
        var bytes = NewRenderer().Render(new[] { MakeRecord() });

        Assert.True(bytes.Length >= 5);
        for (var i = 0; i < PdfMagic.Length; i++)
        {
            Assert.Equal(PdfMagic[i], bytes[i]);
        }
    }

    [Fact]
    public void Render_single_record_byte_length_in_sane_range()
    {
        var bytes = NewRenderer().Render(new[] { MakeRecord() });
        Assert.InRange(bytes.Length, 10_000, 5_000_000);
    }

    [Fact]
    public void Render_single_record_embeds_one_chart_image()
    {
        var bytes = NewRenderer().Render(new[] { MakeRecord() });

        var (pages, images) = InspectStructure(bytes);
        Assert.True(pages >= 1);
        Assert.Equal(1, images);
    }

    [Fact]
    public void Render_multi_record_embeds_one_chart_per_record_and_one_page_per_record()
    {
        var records = new[]
        {
            MakeRecord(mrName: "mr-1"),
            MakeRecord(mrName: "mr-2"),
            MakeRecord(mrName: "mr-3"),
        };

        var bytes = NewRenderer().Render(records);

        var (pages, images) = InspectStructure(bytes);
        Assert.Equal(3, images);
        Assert.Equal(3, pages);
        Assert.True(DecompressedContentContainsAscii(bytes, "mr-1"));
        Assert.True(DecompressedContentContainsAscii(bytes, "mr-2"));
        Assert.True(DecompressedContentContainsAscii(bytes, "mr-3"));
    }

    [Fact]
    public void Render_includes_mr_name_in_content_stream()
    {
        var bytes = NewRenderer().Render(new[] { MakeRecord(mrName: "openmoc-unique-id") });
        Assert.True(DecompressedContentContainsAscii(bytes, "openmoc-unique-id"));
    }

    [Fact]
    public void Render_shows_PASS_token_for_passed_record()
    {
        var bytes = NewRenderer().Render(new[] { MakeRecord(passed: true) });
        Assert.True(DecompressedContentContainsAscii(bytes, "PASS"));
    }

    [Fact]
    public void Render_shows_FAIL_token_and_failure_reason_text_for_failed_record()
    {
        var bytes = NewRenderer().Render(new[]
        {
            MakeRecord(passed: false, failureReason: "tolerance-exceeded-marker"),
        });
        Assert.True(DecompressedContentContainsAscii(bytes, "FAIL"));
        Assert.True(DecompressedContentContainsAscii(bytes, "tolerance-exceeded-marker"));
    }

    [Fact]
    public void Render_empty_input_returns_one_page_no_records_pdf()
    {
        var bytes = NewRenderer().Render(Array.Empty<SystemMtResultRecord>());

        var (pages, images) = InspectStructure(bytes);
        Assert.Equal(1, pages);
        Assert.Equal(0, images);
        Assert.True(DecompressedContentContainsAscii(bytes, "No records"));
    }

    [Fact]
    public void Render_surfaces_typed_verification_when_evidence_present()
    {
        var record = MakeRecord();
        var evidence = new ExecutionEvidence
        {
            IdEvidence = Guid.NewGuid(),
            ExecutionId = record.Id,
            TypedVerification = new TypedVerificationEvidence
            {
                SpecId = "spec-marker-123",
                SpecKind = "Variance",
                PredicateId = "pred-marker-abc",
                PredicateKind = "VarianceRatioPredicate",
                Status = "Passed",
                Diagnostic = new TypedDiagnosticEvidence
                {
                    Expected = 0.5, Actual = 0.49, Residual = 0.01, Tolerance = 0.05,
                },
            },
        };
        var bytes = NewRenderer().Render(
            new[] { record },
            new Dictionary<Guid, ExecutionEvidence> { [record.Id] = evidence });

        Assert.True(DecompressedContentContainsAscii(bytes, "Typed verification"));
        Assert.True(DecompressedContentContainsAscii(bytes, "spec-marker-123"));
        Assert.True(DecompressedContentContainsAscii(bytes, "VarianceRatioPredicate"));
    }

    [Fact]
    public void Render_without_evidence_dictionary_omits_typed_verification_block()
    {
        var bytes = NewRenderer().Render(new[] { MakeRecord() });
        Assert.False(DecompressedContentContainsAscii(bytes, "Typed verification"));
    }

    [Fact]
    public void Render_passes_evidence_mismatch_through_without_surfacing()
    {
        var record = MakeRecord();
        var evidence = new ExecutionEvidence
        {
            IdEvidence = Guid.NewGuid(),
            ExecutionId = Guid.NewGuid(), // different from record.Id
            TypedVerification = new TypedVerificationEvidence
            {
                SpecKind = "ShouldNotAppear",
                Status = "Passed",
            },
        };
        var bytes = NewRenderer().Render(
            new[] { record },
            new Dictionary<Guid, ExecutionEvidence> { [evidence.ExecutionId] = evidence });

        Assert.False(DecompressedContentContainsAscii(bytes, "ShouldNotAppear"));
    }

    [Fact]
    public void Render_throws_on_null_records()
    {
        Assert.Throws<ArgumentNullException>(() => NewRenderer().Render(null!));
    }

    [Fact]
    public void Constructor_throws_on_null_chart_renderer()
    {
        Assert.Throws<ArgumentNullException>(() => new PdfSystemMtResultReportRenderer(null!));
    }
}
