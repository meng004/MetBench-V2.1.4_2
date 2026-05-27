using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using iTextSharp.text;
using iTextSharp.text.pdf;
using MetBench_BLL.SystemMT.Persistence;
using MetBench_BLL.SystemMT.Reporting;
using MetBench_BLL.SystemMT.Reporting.Charts;

namespace MetBench_BLL.Reporting.SystemMt;

/// <summary>
/// iTextSharp-based PDF renderer for System MT runs. Section layout follows
/// the HTML/Markdown precedents (PR #126 / PR #128):
///
///   Title page                                   (ReportContext.Title + GeneratedAt)
///   Per record:
///     - MR identity + pass/fail headline
///     - Source / Follow-up / ValueName / Elapsed
///     - Embedded chart PNG (BinaryRunPointProjector → SkiaChartRenderer)
///     - TypedVerification block (if evidence row matched by Id)
///
/// Numeric formatting uses <see cref="CultureInfo.InvariantCulture"/> for
/// byte-stability across host locales — matches PR #128 markdown precedent.
/// </summary>
public sealed class PdfSystemMtResultReportRenderer : IPdfSystemMtResultReportRenderer
{
    private readonly ISystemMtChartRenderer _chartRenderer;
    private readonly ChartRenderOptions _chartOptions;

    public PdfSystemMtResultReportRenderer(
        ISystemMtChartRenderer chartRenderer,
        ChartRenderOptions? chartOptions = null)
    {
        _chartRenderer = chartRenderer ?? throw new ArgumentNullException(nameof(chartRenderer));
        _chartOptions = chartOptions ?? new ChartRenderOptions();
    }

    public byte[] Render(
        IEnumerable<SystemMtResultRecord> records,
        IReadOnlyDictionary<Guid, ExecutionEvidence>? evidenceByExecutionId = null,
        ReportContext? context = null)
    {
        if (records is null) throw new ArgumentNullException(nameof(records));
        var ctx = context ?? new ReportContext();
        var generatedAt = ctx.GeneratedAt ?? DateTimeOffset.UtcNow;
        var list = records.ToList();

        using var ms = new MemoryStream();
        using (var document = new Document(PageSize.A4, marginLeft: 40, marginRight: 40, marginTop: 50, marginBottom: 40))
        {
            // PdfWriter must remain open until Document is closed; the using on Document handles ordering.
            var writer = PdfWriter.GetInstance(document, ms);
            writer.CloseStream = false;
            document.Open();

            WriteTitlePage(document, ctx.Title, generatedAt, list.Count);

            if (list.Count == 0)
            {
                document.Add(new Paragraph("No records to display.", BodyFont));
            }
            else
            {
                for (var i = 0; i < list.Count; i++)
                {
                    if (i > 0) document.NewPage();
                    var record = list[i];
                    ExecutionEvidence? evidence = null;
                    evidenceByExecutionId?.TryGetValue(record.Id, out evidence);
                    WriteRecordBlock(document, record, evidence);
                }
            }

            document.Close();
        }

        return ms.ToArray();
    }

    // ---- sections ----------------------------------------------------------

    private static void WriteTitlePage(Document document, string title, DateTimeOffset generatedAt, int recordCount)
    {
        var titleParagraph = new Paragraph(title, TitleFont)
        {
            Alignment = Element.ALIGN_LEFT,
            SpacingAfter = 14f,
        };
        document.Add(titleParagraph);

        document.Add(new Paragraph(
            $"Generated: {generatedAt.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture)}",
            MetaFont)
        {
            SpacingAfter = 4f,
        });
        document.Add(new Paragraph(
            $"Records: {recordCount.ToString(CultureInfo.InvariantCulture)}",
            MetaFont)
        {
            SpacingAfter = 16f,
        });
    }

    private void WriteRecordBlock(Document document, SystemMtResultRecord record, ExecutionEvidence? evidence)
    {
        var passText = record.Passed ? "PASS" : "FAIL";
        document.Add(new Paragraph(
            $"{record.MrName}  [{passText}]",
            record.Passed ? PassFont : FailFont)
        {
            SpacingAfter = 6f,
        });

        document.Add(new Paragraph($"Assertion: {Safe(record.AssertionName)}", BodyFont));
        document.Add(new Paragraph(
            $"Value: {Safe(record.ValueName)}  |  Source: {Fmt(record.SourceValue)}  |  Follow-up: {Fmt(record.FollowUpValue)}",
            BodyFont));
        document.Add(new Paragraph(
            $"Elapsed: source {Fmt(record.SourceElapsed.TotalSeconds)} s, follow-up {Fmt(record.FollowUpElapsed.TotalSeconds)} s",
            BodyFont)
        {
            SpacingAfter = 6f,
        });

        if (!record.Passed && !string.IsNullOrEmpty(record.FailureReason))
        {
            document.Add(new Paragraph($"Failure: {record.FailureReason}", FailureFont)
            {
                SpacingAfter = 6f,
            });
        }

        // Embedded chart: 2-point scatter; future Phase covers PhaseLine embedding for multi-phase MRs.
        var figure = BinaryRunPointProjector.Project(record);
        var png = _chartRenderer.RenderPng(figure, _chartOptions);
        var image = Image.GetInstance(png);
        // Scale to fit the page width (A4 portrait usable width ≈ 515 pt with 40 pt margins).
        image.ScaleToFit(515f, 320f);
        image.SpacingBefore = 4f;
        image.SpacingAfter = 8f;
        document.Add(image);

        if (evidence?.TypedVerification is { } typed)
        {
            WriteTypedVerificationBlock(document, typed);
        }
    }

    private static void WriteTypedVerificationBlock(Document document, TypedVerificationEvidence typed)
    {
        document.Add(new Paragraph("Typed verification", SectionFont)
        {
            SpacingAfter = 4f,
        });

        document.Add(new Paragraph($"Spec: {Safe(typed.SpecKind)} ({Safe(typed.SpecId)})", BodyFont));
        document.Add(new Paragraph($"Predicate: {Safe(typed.PredicateKind)} ({Safe(typed.PredicateId)})", BodyFont));
        document.Add(new Paragraph($"Status: {Safe(typed.Status)}", BodyFont));

        if (!string.IsNullOrEmpty(typed.SkipOrInvalidReason))
        {
            document.Add(new Paragraph($"Skip / invalid reason: {typed.SkipOrInvalidReason}", BodyFont));
        }

        if (typed.Diagnostic is { } diag)
        {
            document.Add(new Paragraph(
                $"Diagnostic: expected={Fmt(diag.Expected)}, actual={Fmt(diag.Actual)}, residual={Fmt(diag.Residual)}, tolerance={Fmt(diag.Tolerance)}",
                BodyFont));
        }

        if (typed.PropertyPredicates.Count > 0)
        {
            document.Add(new Paragraph("Property predicates:", BodyFont)
            {
                SpacingBefore = 4f,
            });
            for (var i = 0; i < typed.PropertyPredicates.Count; i++)
            {
                var p = typed.PropertyPredicates[i];
                document.Add(new Paragraph(
                    $"  [{i.ToString(CultureInfo.InvariantCulture)}] {Safe(p.PredicateKind)} ({Safe(p.PredicateId)}) → {Safe(p.Status)}",
                    BodyFont));
            }
        }
    }

    // ---- fonts -------------------------------------------------------------

    private static Font TitleFont => FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18, BaseColor.Black);
    private static Font SectionFont => FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, BaseColor.Black);
    private static Font MetaFont => FontFactory.GetFont(FontFactory.HELVETICA, 10, BaseColor.DarkGray);
    private static Font BodyFont => FontFactory.GetFont(FontFactory.HELVETICA, 10, BaseColor.Black);
    private static Font PassFont => FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 13, new BaseColor(0x38, 0x8E, 0x3C));
    private static Font FailFont => FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 13, new BaseColor(0xD3, 0x2F, 0x2F));
    private static Font FailureFont => FontFactory.GetFont(FontFactory.HELVETICA, 10, new BaseColor(0xD3, 0x2F, 0x2F));

    // ---- formatting helpers ------------------------------------------------

    private static string Fmt(double value)
        => double.IsFinite(value)
            ? value.ToString("G6", CultureInfo.InvariantCulture)
            : value.ToString(CultureInfo.InvariantCulture);

    private static string Safe(string? value) => string.IsNullOrWhiteSpace(value) ? "—" : value;
}
