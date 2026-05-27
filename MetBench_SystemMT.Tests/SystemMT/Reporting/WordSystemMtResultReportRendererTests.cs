using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using MetBench_BLL.Reporting.SystemMt;
using MetBench_BLL.Reporting.SystemMt.Charts.Rendering;
using MetBench_BLL.SystemMT.Persistence;
using MetBench_BLL.SystemMT.Reporting;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Reporting;

public sealed class WordSystemMtResultReportRendererTests
{
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

    private static WordSystemMtResultReportRenderer NewRenderer()
        => new(new SkiaChartRenderer());

    /// <summary>
    /// Reverse-validates the .docx (a zip archive of XML + media parts) using
    /// the OpenXml SDK. Returns (paragraph count, body text concatenated,
    /// image part count, image content types).
    /// </summary>
    private static (int Paragraphs, string BodyText, int ImageParts, List<string> ImageContentTypes) Inspect(byte[] docxBytes)
    {
        using var ms = new MemoryStream(docxBytes, writable: false);
        using var doc = WordprocessingDocument.Open(ms, isEditable: false);
        var body = doc.MainDocumentPart!.Document.Body!;
        var paragraphs = body.Elements<Paragraph>().Count();
        var text = new StringBuilder();
        foreach (var p in body.Elements<Paragraph>())
        {
            foreach (var t in p.Descendants<Text>()) text.Append(t.Text);
            text.AppendLine();
        }
        var imageParts = doc.MainDocumentPart!.ImageParts.ToList();
        return (paragraphs, text.ToString(), imageParts.Count, imageParts.Select(ip => ip.ContentType).ToList());
    }

    /// <summary>
    /// .docx is a zip: assert top-level Content_Types declarative entry.
    /// </summary>
    private static bool ContentTypesXmlDeclaresPng(byte[] docxBytes)
    {
        using var ms = new MemoryStream(docxBytes, writable: false);
        using var zip = new ZipArchive(ms, ZipArchiveMode.Read);
        var ct = zip.GetEntry("[Content_Types].xml");
        if (ct is null) return false;
        using var stream = ct.Open();
        using var reader = new StreamReader(stream);
        var xml = reader.ReadToEnd();
        return xml.Contains("image/png", StringComparison.Ordinal);
    }

    [Fact]
    public void Render_single_record_returns_valid_docx_zip()
    {
        var bytes = NewRenderer().Render(new[] { MakeRecord() });
        // .docx is a zip; first two bytes are 'P' 'K'.
        Assert.True(bytes.Length > 1_000);
        Assert.Equal((byte)'P', bytes[0]);
        Assert.Equal((byte)'K', bytes[1]);
    }

    [Fact]
    public void Render_single_record_byte_length_in_sane_range()
    {
        var bytes = NewRenderer().Render(new[] { MakeRecord() });
        Assert.InRange(bytes.Length, 5_000, 5_000_000);
    }

    [Fact]
    public void Render_single_record_has_one_image_part()
    {
        var bytes = NewRenderer().Render(new[] { MakeRecord() });
        var (paragraphs, _, images, contentTypes) = Inspect(bytes);
        Assert.True(paragraphs >= 1);
        Assert.Equal(1, images);
        Assert.All(contentTypes, ct => Assert.Equal("image/png", ct));
    }

    [Fact]
    public void Render_multi_record_has_one_image_part_per_record()
    {
        var records = new[]
        {
            MakeRecord(mrName: "mr-1"),
            MakeRecord(mrName: "mr-2"),
            MakeRecord(mrName: "mr-3"),
        };

        var bytes = NewRenderer().Render(records);
        var (_, text, images, _) = Inspect(bytes);

        Assert.Equal(3, images);
        Assert.Contains("mr-1", text);
        Assert.Contains("mr-2", text);
        Assert.Contains("mr-3", text);
    }

    [Fact]
    public void Render_includes_mr_name_in_body_text()
    {
        var bytes = NewRenderer().Render(new[] { MakeRecord(mrName: "openmoc-unique-id") });
        var (_, text, _, _) = Inspect(bytes);
        Assert.Contains("openmoc-unique-id", text);
    }

    [Fact]
    public void Render_shows_PASS_token_for_passed_record()
    {
        var bytes = NewRenderer().Render(new[] { MakeRecord(passed: true) });
        var (_, text, _, _) = Inspect(bytes);
        Assert.Contains("PASS", text);
    }

    [Fact]
    public void Render_shows_FAIL_and_failure_reason_for_failed_record()
    {
        var bytes = NewRenderer().Render(new[]
        {
            MakeRecord(passed: false, failureReason: "tolerance-exceeded-marker"),
        });
        var (_, text, _, _) = Inspect(bytes);
        Assert.Contains("FAIL", text);
        Assert.Contains("tolerance-exceeded-marker", text);
    }

    [Fact]
    public void Render_empty_input_returns_doc_with_no_records_paragraph_and_zero_images()
    {
        var bytes = NewRenderer().Render(Array.Empty<SystemMtResultRecord>());
        var (_, text, images, _) = Inspect(bytes);
        Assert.Equal(0, images);
        Assert.Contains("No records", text);
    }

    [Fact]
    public void Render_content_types_xml_declares_image_png()
    {
        var bytes = NewRenderer().Render(new[] { MakeRecord() });
        Assert.True(ContentTypesXmlDeclaresPng(bytes));
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
        var (_, text, _, _) = Inspect(bytes);
        Assert.Contains("Typed verification", text);
        Assert.Contains("spec-marker-123", text);
        Assert.Contains("VarianceRatioPredicate", text);
    }

    [Fact]
    public void Render_without_evidence_dictionary_omits_typed_verification_block()
    {
        var bytes = NewRenderer().Render(new[] { MakeRecord() });
        var (_, text, _, _) = Inspect(bytes);
        Assert.DoesNotContain("Typed verification", text);
    }

    [Fact]
    public void Render_evidence_id_mismatch_does_not_leak_into_record_block()
    {
        var record = MakeRecord();
        var evidence = new ExecutionEvidence
        {
            IdEvidence = Guid.NewGuid(),
            ExecutionId = Guid.NewGuid(), // != record.Id
            TypedVerification = new TypedVerificationEvidence
            {
                SpecKind = "ShouldNotAppear",
                Status = "Passed",
            },
        };
        var bytes = NewRenderer().Render(
            new[] { record },
            new Dictionary<Guid, ExecutionEvidence> { [evidence.ExecutionId] = evidence });
        var (_, text, _, _) = Inspect(bytes);
        Assert.DoesNotContain("ShouldNotAppear", text);
    }

    [Fact]
    public void Render_throws_on_null_records()
    {
        Assert.Throws<ArgumentNullException>(() => NewRenderer().Render(null!));
    }

    [Fact]
    public void Constructor_throws_on_null_chart_renderer()
    {
        Assert.Throws<ArgumentNullException>(() => new WordSystemMtResultReportRenderer(null!));
    }
}
