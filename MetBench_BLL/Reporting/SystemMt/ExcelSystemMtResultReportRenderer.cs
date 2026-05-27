using System.Globalization;
using ClosedXML.Excel;
using ClosedXML.Excel.Drawings;
using MetBench_BLL.SystemMT.Persistence;
using MetBench_BLL.SystemMT.Reporting;
using MetBench_BLL.SystemMT.Reporting.Charts;

namespace MetBench_BLL.Reporting.SystemMt;

/// <summary>
/// Excel (<c>.xlsx</c>) projection of a System-level MT run-report.
/// ClosedXML 0.104.2 backend; embeds a Phase-2 PNG chart per record via
/// <see cref="ISystemMtChartRenderer"/>. Numeric source / follow-up values
/// are written as real number cells (not strings) so downstream users can
/// sort, filter, and pivot without manual coercion.
/// </summary>
public sealed class ExcelSystemMtResultReportRenderer : IExcelSystemMtResultReportRenderer
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public const string SummarySheetName = "Summary";
    public const string TypedVerificationSheetName = "TypedVerification";
    public const string ChartsSheetName = "Charts";

    private readonly ISystemMtChartRenderer _chartRenderer;
    private readonly ChartRenderOptions _chartOptions;

    public ExcelSystemMtResultReportRenderer(ISystemMtChartRenderer chartRenderer, ChartRenderOptions? chartOptions = null)
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
        _ = context ?? new ReportContext();
        var list = records.ToList();

        using var workbook = new XLWorkbook();
        WriteSummarySheet(workbook, list);
        WriteTypedVerificationSheetIfPresent(workbook, list, evidenceByExecutionId);
        WriteChartsSheet(workbook, list);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void WriteSummarySheet(XLWorkbook workbook, IReadOnlyList<SystemMtResultRecord> records)
    {
        var sheet = workbook.Worksheets.Add(SummarySheetName);

        sheet.Cell(1, 1).Value = "MrName";
        sheet.Cell(1, 2).Value = "AssertionName";
        sheet.Cell(1, 3).Value = "ValueName";
        sheet.Cell(1, 4).Value = "SourceValue";
        sheet.Cell(1, 5).Value = "FollowUpValue";
        sheet.Cell(1, 6).Value = "Passed";
        sheet.Cell(1, 7).Value = "FailureReason";
        sheet.Cell(1, 8).Value = "RunAt";
        sheet.Row(1).Style.Font.Bold = true;

        for (int i = 0; i < records.Count; i++)
        {
            var r = records[i];
            int row = i + 2;
            sheet.Cell(row, 1).Value = r.MrName;
            sheet.Cell(row, 2).Value = r.AssertionName;
            sheet.Cell(row, 3).Value = r.ValueName;
            sheet.Cell(row, 4).Value = r.SourceValue;       // numeric
            sheet.Cell(row, 5).Value = r.FollowUpValue;     // numeric
            sheet.Cell(row, 6).Value = r.Passed;            // boolean
            sheet.Cell(row, 7).Value = r.FailureReason;
            sheet.Cell(row, 8).Value = r.RunAt.ToString("u", Inv);
        }
    }

    private static void WriteTypedVerificationSheetIfPresent(
        XLWorkbook workbook,
        IReadOnlyList<SystemMtResultRecord> records,
        IReadOnlyDictionary<Guid, ExecutionEvidence>? evidenceByExecutionId)
    {
        if (evidenceByExecutionId is null) return;

        var typedRows = new List<(SystemMtResultRecord rec, TypedVerificationEvidence typed)>();
        foreach (var r in records)
        {
            if (evidenceByExecutionId.TryGetValue(r.Id, out var ev) && ev?.TypedVerification is { } typed)
            {
                typedRows.Add((r, typed));
            }
        }
        if (typedRows.Count == 0) return;

        var sheet = workbook.Worksheets.Add(TypedVerificationSheetName);
        sheet.Cell(1, 1).Value = "MrName";
        sheet.Cell(1, 2).Value = "SpecId";
        sheet.Cell(1, 3).Value = "SpecKind";
        sheet.Cell(1, 4).Value = "PredicateId";
        sheet.Cell(1, 5).Value = "PredicateKind";
        sheet.Cell(1, 6).Value = "Status";
        sheet.Cell(1, 7).Value = "Expected";
        sheet.Cell(1, 8).Value = "Actual";
        sheet.Cell(1, 9).Value = "Residual";
        sheet.Cell(1, 10).Value = "Tolerance";
        sheet.Cell(1, 11).Value = "SkipOrInvalidReason";
        sheet.Row(1).Style.Font.Bold = true;

        for (int i = 0; i < typedRows.Count; i++)
        {
            var (rec, t) = typedRows[i];
            int row = i + 2;
            sheet.Cell(row, 1).Value = rec.MrName;
            sheet.Cell(row, 2).Value = t.SpecId ?? string.Empty;
            sheet.Cell(row, 3).Value = t.SpecKind ?? string.Empty;
            sheet.Cell(row, 4).Value = t.PredicateId ?? string.Empty;
            sheet.Cell(row, 5).Value = t.PredicateKind ?? string.Empty;
            sheet.Cell(row, 6).Value = t.Status ?? string.Empty;
            if (t.Diagnostic is { } d)
            {
                sheet.Cell(row, 7).Value = d.Expected;
                sheet.Cell(row, 8).Value = d.Actual;
                sheet.Cell(row, 9).Value = d.Residual;
                sheet.Cell(row, 10).Value = d.Tolerance;
            }
            sheet.Cell(row, 11).Value = t.SkipOrInvalidReason ?? string.Empty;
        }
    }

    private void WriteChartsSheet(XLWorkbook workbook, IReadOnlyList<SystemMtResultRecord> records)
    {
        var sheet = workbook.Worksheets.Add(ChartsSheetName);
        if (records.Count == 0) return;

        // Anchor each chart 25 rows below the previous; label in column A.
        const int rowsPerChart = 25;
        for (int i = 0; i < records.Count; i++)
        {
            var rec = records[i];
            int anchorRow = 1 + i * rowsPerChart;

            sheet.Cell(anchorRow, 1).Value = rec.MrName;
            sheet.Cell(anchorRow, 1).Style.Font.Bold = true;

            var figure = BinaryRunPointProjector.Project(rec);
            var png = _chartRenderer.RenderPng(figure, _chartOptions);
            using var imgStream = new MemoryStream(png);
            var picture = sheet.AddPicture(imgStream, XLPictureFormat.Png, $"chart-{i}");
            picture.MoveTo(sheet.Cell(anchorRow + 1, 1));
            // Scale to ~50% so the inline chart fits inside the row stride.
            picture.Scale(0.5);
        }
    }
}
