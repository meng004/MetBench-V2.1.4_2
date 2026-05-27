using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;
using MetBench_BLL.SystemMT.Persistence;
using MetBench_BLL.SystemMT.Reporting;
using MetBench_BLL.SystemMT.Reporting.Charts;

namespace MetBench_BLL.Reporting.SystemMt;

/// <summary>
/// OpenXml (DocumentFormat.OpenXml) implementation of the SystemMT Word report.
/// Section layout mirrors the HTML / PDF / Markdown precedents.
///
/// Image embedding is the only non-trivial OOXML detail: a chart PNG is added
/// as an <see cref="ImagePart"/> on the <see cref="MainDocumentPart"/>, then
/// referenced from an <see cref="A.Blip"/> inside a <see cref="W.Run"/> via
/// the standard Inline DrawingML graph (per ECMA-376 §17.3.3.9).
/// </summary>
public sealed class WordSystemMtResultReportRenderer : IWordSystemMtResultReportRenderer
{
    // EMU = English Metric Unit. 914400 per inch; 9525 per pixel at 96 dpi.
    // We hard-pin to 96 dpi so the embedded chart's physical size in the doc
    // stays predictable across Phase 2 ChartRenderOptions.Dpi variations.
    private const long EmuPerPixelAt96Dpi = 9525L;

    private readonly ISystemMtChartRenderer _chartRenderer;
    private readonly ChartRenderOptions _chartOptions;

    public WordSystemMtResultReportRenderer(
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
        using (var doc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document))
        {
            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = mainPart.Document.AppendChild(new Body());

            WriteTitleBlock(body, ctx.Title, generatedAt, list.Count);

            if (list.Count == 0)
            {
                body.AppendChild(MakeParagraph("No records to display.", bold: false, fontHalfPoints: 22));
            }
            else
            {
                for (var i = 0; i < list.Count; i++)
                {
                    var record = list[i];
                    ExecutionEvidence? evidence = null;
                    evidenceByExecutionId?.TryGetValue(record.Id, out evidence);
                    WriteRecordBlock(mainPart, body, record, evidence);
                }
            }

            mainPart.Document.Save();
        }
        return ms.ToArray();
    }

    // ---- sections ----------------------------------------------------------

    private static void WriteTitleBlock(Body body, string title, DateTimeOffset generatedAt, int recordCount)
    {
        body.AppendChild(MakeParagraph(title, bold: true, fontHalfPoints: 36));
        body.AppendChild(MakeParagraph(
            $"Generated: {generatedAt.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture)}",
            bold: false, fontHalfPoints: 20));
        body.AppendChild(MakeParagraph(
            $"Records: {recordCount.ToString(CultureInfo.InvariantCulture)}",
            bold: false, fontHalfPoints: 20));
    }

    private void WriteRecordBlock(MainDocumentPart mainPart, Body body, SystemMtResultRecord record, ExecutionEvidence? evidence)
    {
        var passText = record.Passed ? "PASS" : "FAIL";
        body.AppendChild(MakeParagraph(
            $"{record.MrName}  [{passText}]",
            bold: true, fontHalfPoints: 28));
        body.AppendChild(MakeParagraph($"Assertion: {Safe(record.AssertionName)}", bold: false, fontHalfPoints: 20));
        body.AppendChild(MakeParagraph(
            $"Value: {Safe(record.ValueName)}  |  Source: {Fmt(record.SourceValue)}  |  Follow-up: {Fmt(record.FollowUpValue)}",
            bold: false, fontHalfPoints: 20));
        body.AppendChild(MakeParagraph(
            $"Elapsed: source {Fmt(record.SourceElapsed.TotalSeconds)} s, follow-up {Fmt(record.FollowUpElapsed.TotalSeconds)} s",
            bold: false, fontHalfPoints: 20));

        if (!record.Passed && !string.IsNullOrEmpty(record.FailureReason))
        {
            body.AppendChild(MakeParagraph($"Failure: {record.FailureReason}", bold: false, fontHalfPoints: 20));
        }

        // Embedded chart image.
        var figure = BinaryRunPointProjector.Project(record);
        var png = _chartRenderer.RenderPng(figure, _chartOptions);
        body.AppendChild(MakeImageParagraph(mainPart, png, _chartOptions.Width, _chartOptions.Height));

        if (evidence?.TypedVerification is { } typed)
        {
            WriteTypedVerificationBlock(body, typed);
        }
    }

    private static void WriteTypedVerificationBlock(Body body, TypedVerificationEvidence typed)
    {
        body.AppendChild(MakeParagraph("Typed verification", bold: true, fontHalfPoints: 22));
        body.AppendChild(MakeParagraph($"Spec: {Safe(typed.SpecKind)} ({Safe(typed.SpecId)})", bold: false, fontHalfPoints: 20));
        body.AppendChild(MakeParagraph($"Predicate: {Safe(typed.PredicateKind)} ({Safe(typed.PredicateId)})", bold: false, fontHalfPoints: 20));
        body.AppendChild(MakeParagraph($"Status: {Safe(typed.Status)}", bold: false, fontHalfPoints: 20));

        if (!string.IsNullOrEmpty(typed.SkipOrInvalidReason))
        {
            body.AppendChild(MakeParagraph($"Skip / invalid reason: {typed.SkipOrInvalidReason}", bold: false, fontHalfPoints: 20));
        }

        if (typed.Diagnostic is { } diag)
        {
            body.AppendChild(MakeParagraph(
                $"Diagnostic: expected={Fmt(diag.Expected)}, actual={Fmt(diag.Actual)}, residual={Fmt(diag.Residual)}, tolerance={Fmt(diag.Tolerance)}",
                bold: false, fontHalfPoints: 20));
        }

        if (typed.PropertyPredicates.Count > 0)
        {
            body.AppendChild(MakeParagraph("Property predicates:", bold: false, fontHalfPoints: 20));
            for (var i = 0; i < typed.PropertyPredicates.Count; i++)
            {
                var p = typed.PropertyPredicates[i];
                body.AppendChild(MakeParagraph(
                    $"  [{i.ToString(CultureInfo.InvariantCulture)}] {Safe(p.PredicateKind)} ({Safe(p.PredicateId)}) → {Safe(p.Status)}",
                    bold: false, fontHalfPoints: 20));
            }
        }
    }

    // ---- OOXML helpers -----------------------------------------------------

    private static Paragraph MakeParagraph(string text, bool bold, int fontHalfPoints)
    {
        var run = new Run();
        var runProps = new RunProperties();
        if (bold) runProps.Append(new Bold());
        runProps.Append(new FontSize { Val = fontHalfPoints.ToString(CultureInfo.InvariantCulture) });
        run.RunProperties = runProps;
        run.AppendChild(new Text(text) { Space = SpaceProcessingModeValues.Preserve });
        var paragraph = new Paragraph();
        paragraph.AppendChild(run);
        return paragraph;
    }

    private static Paragraph MakeImageParagraph(MainDocumentPart mainPart, byte[] png, int pixelWidth, int pixelHeight)
    {
        var imagePart = mainPart.AddImagePart(ImagePartType.Png);
        using (var stream = new MemoryStream(png))
        {
            imagePart.FeedData(stream);
        }
        var relId = mainPart.GetIdOfPart(imagePart);

        var widthEmu = pixelWidth * EmuPerPixelAt96Dpi;
        var heightEmu = pixelHeight * EmuPerPixelAt96Dpi;

        var drawing = new Drawing(
            new DW.Inline(
                new DW.Extent { Cx = widthEmu, Cy = heightEmu },
                new DW.EffectExtent { LeftEdge = 0L, TopEdge = 0L, RightEdge = 0L, BottomEdge = 0L },
                new DW.DocProperties { Id = (UInt32Value)1U, Name = "Chart" },
                new DW.NonVisualGraphicFrameDrawingProperties(
                    new A.GraphicFrameLocks { NoChangeAspect = true }),
                new A.Graphic(
                    new A.GraphicData(
                        new PIC.Picture(
                            new PIC.NonVisualPictureProperties(
                                new PIC.NonVisualDrawingProperties { Id = (UInt32Value)0U, Name = "Chart.png" },
                                new PIC.NonVisualPictureDrawingProperties()),
                            new PIC.BlipFill(
                                new A.Blip { Embed = relId, CompressionState = A.BlipCompressionValues.Print },
                                new A.Stretch(new A.FillRectangle())),
                            new PIC.ShapeProperties(
                                new A.Transform2D(
                                    new A.Offset { X = 0L, Y = 0L },
                                    new A.Extents { Cx = widthEmu, Cy = heightEmu }),
                                new A.PresetGeometry(new A.AdjustValueList()) { Preset = A.ShapeTypeValues.Rectangle })))
                    { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" }))
            {
                DistanceFromTop = 0U,
                DistanceFromBottom = 0U,
                DistanceFromLeft = 0U,
                DistanceFromRight = 0U,
                EditId = "50D07946",
            });

        var run = new Run();
        run.AppendChild(drawing);
        var paragraph = new Paragraph();
        paragraph.AppendChild(run);
        return paragraph;
    }

    // ---- formatting helpers ------------------------------------------------

    private static string Fmt(double value)
        => double.IsFinite(value)
            ? value.ToString("G6", CultureInfo.InvariantCulture)
            : value.ToString(CultureInfo.InvariantCulture);

    private static string Safe(string? value) => string.IsNullOrWhiteSpace(value) ? "—" : value;
}
