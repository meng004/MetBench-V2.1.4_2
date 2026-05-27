using System.Globalization;
using iTextSharp.text;
using iTextSharp.text.pdf;
using MetBench_BLL.SystemMT.Persistence;
using MetBench_BLL.SystemMT.Reporting;
using MetBench_BLL.SystemMT.Reporting.Charts;

namespace MetBench_BLL.Reporting.SystemMt;

/// <summary>
/// PDF projection of a System-level MT run-report. iTextSharp / LGPLv2 backend;
/// embeds a Phase-2 PNG chart per record via <see cref="ISystemMtChartRenderer"/>.
///
/// Note: iTextSharp injects PDF metadata (/CreationDate, /ModDate, /ID) on
/// every render, so the raw byte buffer is NOT byte-deterministic. Callers
/// that need equality must compare via <see cref="PdfReader"/> on extracted
/// structural content (page count, embedded image count, text). See
/// <c>PdfSystemMtResultReportRendererTests</c> for the documented contract.
/// </summary>
public sealed class PdfSystemMtResultReportRenderer : IPdfSystemMtResultReportRenderer
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    private static readonly BaseColor TextColor = new BaseColor(31, 35, 40);
    private static readonly BaseColor MutedColor = new BaseColor(87, 96, 106);
    private static readonly BaseColor PassColor = new BaseColor(26, 127, 55);
    private static readonly BaseColor FailColor = new BaseColor(207, 34, 46);

    private readonly ISystemMtChartRenderer _chartRenderer;
    private readonly ChartRenderOptions _chartOptions;

    public PdfSystemMtResultReportRenderer(ISystemMtChartRenderer chartRenderer, ChartRenderOptions? chartOptions = null)
    {
        _chartRenderer = chartRenderer ?? throw new ArgumentNullException(nameof(chartRenderer));
        // Smaller default size for inline PDF embedding (keeps per-record block to a few hundred KB).
        _chartOptions = chartOptions ?? new ChartRenderOptions(Width: 720, Height: 480, Dpi: 150, Theme: ChartTheme.Light);
    }

    public byte[] Render(IEnumerable<SystemMtResultRecord> records, ReportContext? context = null)
        => Render(records, evidenceByExecutionId: null, context);

    public byte[] Render(
        IEnumerable<SystemMtResultRecord> records,
        IReadOnlyDictionary<Guid, ExecutionEvidence>? evidenceByExecutionId,
        ReportContext? context = null)
    {
        if (records is null) throw new ArgumentNullException(nameof(records));
        var ctx = context ?? new ReportContext();
        var generatedAt = ctx.GeneratedAt ?? DateTimeOffset.UtcNow;
        var list = records.ToList();

        using var stream = new MemoryStream();
        var document = new Document(PageSize.A4, 36f, 36f, 48f, 48f);
        var writer = PdfWriter.GetInstance(document, stream);
        writer.CloseStream = false;

        document.Open();

        WriteTitle(document, ctx, generatedAt, list);

        if (list.Count == 0)
        {
            WriteEmptyNotice(document);
        }
        else
        {
            foreach (var record in list)
            {
                TypedVerificationEvidence? typed = null;
                if (evidenceByExecutionId is not null
                    && evidenceByExecutionId.TryGetValue(record.Id, out var ev))
                {
                    typed = ev?.TypedVerification;
                }
                WriteRecord(document, record, typed);
            }
        }

        document.Close();
        return stream.ToArray();
    }

    private static void WriteTitle(Document document, ReportContext ctx, DateTimeOffset generatedAt, IReadOnlyList<SystemMtResultRecord> list)
    {
        var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18f, TextColor);
        var metaFont = FontFactory.GetFont(FontFactory.HELVETICA, 10f, MutedColor);
        var countsFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11f, TextColor);

        var title = new Paragraph(ctx.Title, titleFont) { SpacingAfter = 8f };
        document.Add(title);

        var meta = new Paragraph($"Generated at {generatedAt.ToString("u", Inv)}", metaFont) { SpacingAfter = 6f };
        document.Add(meta);

        var passed = list.Count(r => r.Passed);
        var failed = list.Count - passed;
        var counts = new Paragraph(
            $"Total: {list.Count.ToString(Inv)}   Passed: {passed.ToString(Inv)}   Failed: {failed.ToString(Inv)}",
            countsFont)
        {
            SpacingAfter = 18f,
        };
        document.Add(counts);
    }

    private static void WriteEmptyNotice(Document document)
    {
        var font = FontFactory.GetFont(FontFactory.HELVETICA, 11f, MutedColor);
        var p = new Paragraph("No run results to display.", font);
        document.Add(p);
    }

    private void WriteRecord(Document document, SystemMtResultRecord record, TypedVerificationEvidence? typed)
    {
        var headingFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14f, TextColor);
        var bodyFont = FontFactory.GetFont(FontFactory.HELVETICA, 10f, TextColor);
        var labelFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10f, MutedColor);
        var badgeFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10f, BaseColor.White);

        var heading = new Paragraph(record.MrName, headingFont) { SpacingBefore = 10f, SpacingAfter = 4f };
        document.Add(heading);

        // PASS / FAIL badge as a colored single-cell table (tiny, inline).
        var badgeTable = new PdfPTable(1) { WidthPercentage = 14f, HorizontalAlignment = Element.ALIGN_LEFT };
        var badgeCell = new PdfPCell(new Phrase(record.Passed ? "PASS" : "FAIL", badgeFont))
        {
            BackgroundColor = record.Passed ? PassColor : FailColor,
            HorizontalAlignment = Element.ALIGN_CENTER,
            Padding = 4f,
            Border = Rectangle.NO_BORDER,
        };
        badgeTable.AddCell(badgeCell);
        badgeTable.SpacingAfter = 6f;
        document.Add(badgeTable);

        document.Add(new Paragraph(
            $"Assertion: {record.AssertionName}  on  {record.ValueName}",
            bodyFont));
        document.Add(new Paragraph(
            $"Source value: {record.SourceValue.ToString("G", Inv)}", bodyFont));
        document.Add(new Paragraph(
            $"Follow-up value: {record.FollowUpValue.ToString("G", Inv)}", bodyFont));
        document.Add(new Paragraph(
            $"Run at: {record.RunAt.ToString("u", Inv)}", bodyFont));

        if (!record.Passed && !string.IsNullOrEmpty(record.FailureReason))
        {
            var failFont = FontFactory.GetFont(FontFactory.HELVETICA, 10f, FailColor);
            document.Add(new Paragraph($"Failure reason: {record.FailureReason}", failFont) { SpacingBefore = 4f });
        }

        if (typed is not null)
        {
            WriteTypedVerification(document, typed, labelFont, bodyFont);
        }

        EmbedChart(document, record);
    }

    private static void WriteTypedVerification(Document document, TypedVerificationEvidence typed, Font labelFont, Font bodyFont)
    {
        document.Add(new Paragraph("Typed verification", labelFont) { SpacingBefore = 6f, SpacingAfter = 2f });

        if (!string.IsNullOrEmpty(typed.SpecId))
            document.Add(new Paragraph($"Spec ID: {typed.SpecId}", bodyFont));
        if (!string.IsNullOrEmpty(typed.SpecKind))
            document.Add(new Paragraph($"Spec kind: {typed.SpecKind}", bodyFont));
        if (!string.IsNullOrEmpty(typed.PredicateId))
            document.Add(new Paragraph($"Predicate: {typed.PredicateId} ({typed.PredicateKind})", bodyFont));
        if (!string.IsNullOrEmpty(typed.Status))
            document.Add(new Paragraph($"Status: {typed.Status}", bodyFont));
        if (typed.Diagnostic is { } d)
        {
            document.Add(new Paragraph(
                $"Expected: {d.Expected.ToString("G", Inv)}   Actual: {d.Actual.ToString("G", Inv)}   " +
                $"Residual: {d.Residual.ToString("G", Inv)}   Tolerance: {d.Tolerance.ToString("G", Inv)}",
                bodyFont));
        }
        if (!string.IsNullOrEmpty(typed.SkipOrInvalidReason))
            document.Add(new Paragraph($"Skip reason: {typed.SkipOrInvalidReason}", bodyFont));

        if (typed.PropertyPredicates is { Count: > 0 })
        {
            document.Add(new Paragraph("Property predicates:", labelFont) { SpacingBefore = 2f });
            foreach (var p in typed.PropertyPredicates)
            {
                var line = $"- {p.PredicateId} ({p.PredicateKind})  status={p.Status}";
                if (p.Residual.HasValue)
                    line += $"  residual={p.Residual.Value.ToString("G", Inv)}";
                if (p.Tolerance.HasValue)
                    line += $"  tolerance={p.Tolerance.Value.ToString("G", Inv)}";
                document.Add(new Paragraph(line, bodyFont));
            }
        }
    }

    private void EmbedChart(Document document, SystemMtResultRecord record)
    {
        var figure = BinaryRunPointProjector.Project(record);
        var png = _chartRenderer.RenderPng(figure, _chartOptions);
        var image = Image.GetInstance(png);

        // Scale the chart down to fit the page width (A4 minus margins ≈ 523pt).
        const float maxWidthPt = 480f;
        if (image.Width > maxWidthPt)
        {
            var scale = maxWidthPt / image.Width;
            image.ScalePercent(scale * 100f);
        }
        image.SpacingBefore = 8f;
        image.SpacingAfter = 14f;
        image.Alignment = Element.ALIGN_LEFT;
        document.Add(image);
    }
}
