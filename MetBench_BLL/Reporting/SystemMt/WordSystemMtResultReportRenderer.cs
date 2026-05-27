using System.Globalization;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using MetBench_BLL.SystemMT.Persistence;
using MetBench_BLL.SystemMT.Reporting;
using MetBench_BLL.SystemMT.Reporting.Charts;
using A = DocumentFormat.OpenXml.Drawing;
using Pic = DocumentFormat.OpenXml.Drawing.Pictures;
using Wp = DocumentFormat.OpenXml.Drawing.Wordprocessing;

namespace MetBench_BLL.Reporting.SystemMt;

/// <summary>
/// Word (<c>.docx</c>) projection of a System-level MT run-report.
/// DocumentFormat.OpenXml 3.3.0 backend; embeds a Phase-2 PNG chart per record
/// via <see cref="ISystemMtChartRenderer"/>. Output is deterministic when
/// <see cref="ReportContext.GeneratedAt"/> is supplied (no
/// <see cref="CoreFilePropertiesPart"/> is added, so OpenXml does not inject
/// a wall-clock created/modified timestamp into the package).
/// </summary>
public sealed class WordSystemMtResultReportRenderer : IWordSystemMtResultReportRenderer
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    // 5 inch chart inline; 1 inch = 914400 EMU. Aspect 3:2 derived from the default
    // 720x480 Phase-2 PNG so the rendered chart fills the column without distortion.
    private const long ImageWidthEmu = 4_572_000;   // 5 inch
    private const long ImageHeightEmu = 3_048_000;  // 5 inch × 480/720

    private readonly ISystemMtChartRenderer _chartRenderer;
    private readonly ChartRenderOptions _chartOptions;

    public WordSystemMtResultReportRenderer(ISystemMtChartRenderer chartRenderer, ChartRenderOptions? chartOptions = null)
    {
        _chartRenderer = chartRenderer ?? throw new ArgumentNullException(nameof(chartRenderer));
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
        using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = mainPart.Document.AppendChild(new Body());

            AppendParagraph(body, ctx.Title, fontSizeHalfPoints: 36, bold: true);
            AppendParagraph(body, $"Generated at {generatedAt.ToString("u", Inv)}", fontSizeHalfPoints: 20, italic: true);

            var passed = list.Count(r => r.Passed);
            var failed = list.Count - passed;
            AppendParagraph(body,
                $"Total: {list.Count.ToString(Inv)}   Passed: {passed.ToString(Inv)}   Failed: {failed.ToString(Inv)}",
                fontSizeHalfPoints: 22, bold: true);

            if (list.Count == 0)
            {
                AppendParagraph(body, "No run results to display.", fontSizeHalfPoints: 22);
            }
            else
            {
                uint imageId = 1;
                foreach (var record in list)
                {
                    TypedVerificationEvidence? typed = null;
                    if (evidenceByExecutionId is not null
                        && evidenceByExecutionId.TryGetValue(record.Id, out var ev))
                    {
                        typed = ev?.TypedVerification;
                    }
                    AppendRecord(mainPart, body, record, typed, imageId);
                    imageId++;
                }
            }

            mainPart.Document.Save();
        }
        return stream.ToArray();
    }

    private void AppendRecord(MainDocumentPart mainPart, Body body, SystemMtResultRecord record, TypedVerificationEvidence? typed, uint imageId)
    {
        AppendParagraph(body, record.MrName, fontSizeHalfPoints: 28, bold: true);
        AppendParagraph(body, record.Passed ? "PASS" : "FAIL", fontSizeHalfPoints: 22, bold: true);
        AppendParagraph(body, $"Assertion: {record.AssertionName}  on  {record.ValueName}", fontSizeHalfPoints: 20);
        AppendParagraph(body, $"Source value: {record.SourceValue.ToString("G", Inv)}", fontSizeHalfPoints: 20);
        AppendParagraph(body, $"Follow-up value: {record.FollowUpValue.ToString("G", Inv)}", fontSizeHalfPoints: 20);
        AppendParagraph(body, $"Run at: {record.RunAt.ToString("u", Inv)}", fontSizeHalfPoints: 20);

        if (!record.Passed && !string.IsNullOrEmpty(record.FailureReason))
        {
            AppendParagraph(body, $"Failure reason: {record.FailureReason}", fontSizeHalfPoints: 20);
        }

        if (typed is not null)
        {
            AppendTypedVerification(body, typed);
        }

        EmbedChart(mainPart, body, record, imageId);
    }

    private static void AppendTypedVerification(Body body, TypedVerificationEvidence typed)
    {
        AppendParagraph(body, "Typed verification", fontSizeHalfPoints: 22, bold: true);

        if (!string.IsNullOrEmpty(typed.SpecId))
            AppendParagraph(body, $"Spec ID: {typed.SpecId}", fontSizeHalfPoints: 20);
        if (!string.IsNullOrEmpty(typed.SpecKind))
            AppendParagraph(body, $"Spec kind: {typed.SpecKind}", fontSizeHalfPoints: 20);
        if (!string.IsNullOrEmpty(typed.PredicateId))
            AppendParagraph(body, $"Predicate: {typed.PredicateId} ({typed.PredicateKind})", fontSizeHalfPoints: 20);
        if (!string.IsNullOrEmpty(typed.Status))
            AppendParagraph(body, $"Status: {typed.Status}", fontSizeHalfPoints: 20);
        if (typed.Diagnostic is { } d)
        {
            AppendParagraph(body,
                $"Expected: {d.Expected.ToString("G", Inv)}   Actual: {d.Actual.ToString("G", Inv)}   " +
                $"Residual: {d.Residual.ToString("G", Inv)}   Tolerance: {d.Tolerance.ToString("G", Inv)}",
                fontSizeHalfPoints: 20);
        }
        if (!string.IsNullOrEmpty(typed.SkipOrInvalidReason))
            AppendParagraph(body, $"Skip reason: {typed.SkipOrInvalidReason}", fontSizeHalfPoints: 20);

        if (typed.PropertyPredicates is { Count: > 0 })
        {
            AppendParagraph(body, "Property predicates:", fontSizeHalfPoints: 20, bold: true);
            foreach (var p in typed.PropertyPredicates)
            {
                var line = $"- {p.PredicateId} ({p.PredicateKind})  status={p.Status}";
                if (p.Residual.HasValue) line += $"  residual={p.Residual.Value.ToString("G", Inv)}";
                if (p.Tolerance.HasValue) line += $"  tolerance={p.Tolerance.Value.ToString("G", Inv)}";
                AppendParagraph(body, line, fontSizeHalfPoints: 20);
            }
        }
    }

    private void EmbedChart(MainDocumentPart mainPart, Body body, SystemMtResultRecord record, uint imageId)
    {
        var figure = BinaryRunPointProjector.Project(record);
        var png = _chartRenderer.RenderPng(figure, _chartOptions);

        var imagePart = mainPart.AddImagePart(ImagePartType.Png);
        using (var imgStream = new MemoryStream(png))
            imagePart.FeedData(imgStream);
        var relId = mainPart.GetIdOfPart(imagePart);

        var drawing = BuildInlineDrawing(relId, imageId, $"chart-{imageId}", ImageWidthEmu, ImageHeightEmu);
        var run = new Run(drawing);
        var paragraph = new Paragraph(run);
        body.AppendChild(paragraph);
    }

    private static Drawing BuildInlineDrawing(string relId, uint imageId, string imageName, long cx, long cy)
    {
        // Hand-built wp:Inline → a:Graphic → pic:Picture wrapper. The shape of this
        // tree is the canonical "embed PNG inline at fixed size" pattern from the
        // OpenXml SDK samples; keep field order stable for byte-determinism.
        return new Drawing(
            new Wp.Inline(
                new Wp.Extent { Cx = cx, Cy = cy },
                new Wp.EffectExtent { LeftEdge = 0L, TopEdge = 0L, RightEdge = 0L, BottomEdge = 0L },
                new Wp.DocProperties { Id = imageId, Name = imageName },
                new Wp.NonVisualGraphicFrameDrawingProperties(
                    new A.GraphicFrameLocks { NoChangeAspect = true }),
                new A.Graphic(
                    new A.GraphicData(
                        new Pic.Picture(
                            new Pic.NonVisualPictureProperties(
                                new Pic.NonVisualDrawingProperties { Id = 0U, Name = imageName },
                                new Pic.NonVisualPictureDrawingProperties()),
                            new Pic.BlipFill(
                                new A.Blip { Embed = relId },
                                new A.Stretch(new A.FillRectangle())),
                            new Pic.ShapeProperties(
                                new A.Transform2D(
                                    new A.Offset { X = 0L, Y = 0L },
                                    new A.Extents { Cx = cx, Cy = cy }),
                                new A.PresetGeometry(new A.AdjustValueList()) { Preset = A.ShapeTypeValues.Rectangle })))
                    { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" }))
            {
                DistanceFromTop = 0U,
                DistanceFromBottom = 0U,
                DistanceFromLeft = 0U,
                DistanceFromRight = 0U,
            });
    }

    private static void AppendParagraph(Body body, string text, int fontSizeHalfPoints, bool bold = false, bool italic = false)
    {
        var paragraph = body.AppendChild(new Paragraph());
        var run = paragraph.AppendChild(new Run());
        var runProperties = new RunProperties(new FontSize { Val = fontSizeHalfPoints.ToString(Inv) });
        if (bold) runProperties.Append(new Bold());
        if (italic) runProperties.Append(new Italic());
        run.RunProperties = runProperties;
        run.AppendChild(new Text(text) { Space = SpaceProcessingModeValues.Preserve });
    }
}
