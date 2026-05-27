using ClosedXML.Excel;
using MetBench_BLL.Reporting.SystemMt;
using MetBench_BLL.SystemMT.Persistence;
using MetBench_BLL.SystemMT.Reporting;
using MetBench_BLL.SystemMT.Reporting.Charts;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Reporting;

/// <summary>
/// Pins the structural surface of <see cref="ExcelSystemMtResultReportRenderer"/>.
/// Linux-only / pure-managed: ClosedXML 0.104.2 + SkiaSharp Linux native
/// asset (both already in MetBench_BLL.csproj from PR-T2-2).
/// </summary>
public sealed class ExcelSystemMtResultReportRendererTests
{
    private static readonly DateTimeOffset FixedRunAt = new(2026, 5, 27, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset FixedGenAt = new(2026, 5, 27, 12, 30, 0, TimeSpan.Zero);

    private static ExcelSystemMtResultReportRenderer NewRenderer() =>
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
    };

    private static ReportContext FixedContext() => new(GeneratedAt: FixedGenAt);

    private static XLWorkbook Open(byte[] bytes)
    {
        var ms = new MemoryStream(bytes);
        return new XLWorkbook(ms);
    }

    [Fact]
    public void Constructor_rejects_null_chart_renderer()
    {
        Assert.Throws<ArgumentNullException>(() => new ExcelSystemMtResultReportRenderer(null!));
    }

    [Fact]
    public void Render_throws_when_records_null()
    {
        var r = NewRenderer();
        Assert.Throws<ArgumentNullException>(() => r.Render((IEnumerable<SystemMtResultRecord>)null!));
    }

    [Fact]
    public void Render_empty_input_returns_workbook_with_summary_header_only()
    {
        var xlsx = NewRenderer().Render(Array.Empty<SystemMtResultRecord>(), FixedContext());

        using var wb = Open(xlsx);
        var summary = wb.Worksheet(ExcelSystemMtResultReportRenderer.SummarySheetName);
        Assert.NotNull(summary);
        // M1 (PR-B): Summary sheet now carries a title block (row 1) + generated-at (row 2)
        // + counts (row 3) before the header row (row 5). Row 6+ is data.
        Assert.Equal("MrName", summary.Cell(ExcelSystemMtResultReportRenderer.SummaryHeaderRow, 1).GetString());
        Assert.True(
            summary.Cell(ExcelSystemMtResultReportRenderer.SummaryFirstDataRow, 1).IsEmpty(),
            "Summary first data row must be empty for empty input");
    }

    [Fact]
    public void Render_workbook_always_has_summary_and_charts_sheets()
    {
        var xlsx = NewRenderer().Render(new[] { SampleRecord("mr-x", passed: true) }, FixedContext());

        using var wb = Open(xlsx);
        var sheetNames = wb.Worksheets.Select(s => s.Name).ToHashSet();
        Assert.Contains(ExcelSystemMtResultReportRenderer.SummarySheetName, sheetNames);
        Assert.Contains(ExcelSystemMtResultReportRenderer.ChartsSheetName, sheetNames);
    }

    [Fact]
    public void Render_single_record_summary_a2_contains_mr_name()
    {
        var xlsx = NewRenderer().Render(new[] { SampleRecord("openmoc-pincell-nu-sigma-f", passed: true) }, FixedContext());

        using var wb = Open(xlsx);
        var summary = wb.Worksheet(ExcelSystemMtResultReportRenderer.SummarySheetName);
        Assert.Equal("openmoc-pincell-nu-sigma-f",
            summary.Cell(ExcelSystemMtResultReportRenderer.SummaryFirstDataRow, 1).GetString());
    }

    [Fact]
    public void Render_source_value_cell_is_numeric_not_text()
    {
        var rec = SampleRecord("mr-x", passed: true);
        var xlsx = NewRenderer().Render(new[] { rec }, FixedContext());

        using var wb = Open(xlsx);
        var summary = wb.Worksheet(ExcelSystemMtResultReportRenderer.SummarySheetName);
        var sourceCell = summary.Cell(ExcelSystemMtResultReportRenderer.SummaryFirstDataRow, 4);
        Assert.Equal(XLDataType.Number, sourceCell.DataType);
        Assert.Equal(rec.SourceValue, sourceCell.GetDouble());
    }

    [Fact]
    public void Render_followup_value_cell_is_numeric_not_text()
    {
        var rec = SampleRecord("mr-x", passed: true);
        var xlsx = NewRenderer().Render(new[] { rec }, FixedContext());

        using var wb = Open(xlsx);
        var summary = wb.Worksheet(ExcelSystemMtResultReportRenderer.SummarySheetName);
        var followCell = summary.Cell(ExcelSystemMtResultReportRenderer.SummaryFirstDataRow, 5);
        Assert.Equal(XLDataType.Number, followCell.DataType);
        Assert.Equal(rec.FollowUpValue, followCell.GetDouble());
    }

    [Fact]
    public void Render_charts_sheet_has_one_picture_per_record()
    {
        var records = Enumerable.Range(0, 5).Select(i => SampleRecord($"mr-{i}", passed: i % 2 == 0)).ToArray();

        var xlsx = NewRenderer().Render(records, FixedContext());

        using var wb = Open(xlsx);
        var charts = wb.Worksheet(ExcelSystemMtResultReportRenderer.ChartsSheetName);
        Assert.Equal(5, charts.Pictures.Count);
    }

    [Fact]
    public void Render_single_record_charts_sheet_has_exactly_one_picture()
    {
        var xlsx = NewRenderer().Render(new[] { SampleRecord("mr-x", passed: true) }, FixedContext());

        using var wb = Open(xlsx);
        Assert.Equal(1, wb.Worksheet(ExcelSystemMtResultReportRenderer.ChartsSheetName).Pictures.Count);
    }

    [Fact]
    public void Render_multi_record_summary_has_one_data_row_per_record_plus_header()
    {
        var records = new[]
        {
            SampleRecord("mr-a", passed: true),
            SampleRecord("mr-b", passed: false),
            SampleRecord("mr-c", passed: true),
        };

        var xlsx = NewRenderer().Render(records, FixedContext());

        using var wb = Open(xlsx);
        var summary = wb.Worksheet(ExcelSystemMtResultReportRenderer.SummarySheetName);
        // M1: rows 1-3 are title + generated-at + counts; row 4 is the blank separator;
        // row 5 is the header; rows 6+ are data. We count concrete data rows below the
        // header row to avoid the title block inflating the count.
        var dataRowCount = 0;
        for (var r = ExcelSystemMtResultReportRenderer.SummaryFirstDataRow; r <= 100; r++)
        {
            if (summary.Cell(r, 1).IsEmpty()) break;
            dataRowCount++;
        }
        Assert.Equal(3, dataRowCount);
    }

    [Fact]
    public void Render_passed_cell_is_boolean_type()
    {
        var rec = SampleRecord("mr-x", passed: false);
        var xlsx = NewRenderer().Render(new[] { rec }, FixedContext());

        using var wb = Open(xlsx);
        var summary = wb.Worksheet(ExcelSystemMtResultReportRenderer.SummarySheetName);
        var passedCell = summary.Cell(ExcelSystemMtResultReportRenderer.SummaryFirstDataRow, 6);
        Assert.Equal(XLDataType.Boolean, passedCell.DataType);
        Assert.False(passedCell.GetBoolean());
    }

    [Fact]
    public void Render_with_evidence_creates_typed_verification_sheet()
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
                Diagnostic = new TypedDiagnosticEvidence { Expected = 0.5, Actual = 0.48, Residual = 0.02, Tolerance = 0.05 },
            },
        };
        var map = new Dictionary<Guid, ExecutionEvidence> { [rec.Id] = evidence };

        var xlsx = NewRenderer().Render(new[] { rec }, map, FixedContext());

        using var wb = Open(xlsx);
        Assert.True(wb.Worksheets.Contains(ExcelSystemMtResultReportRenderer.TypedVerificationSheetName));
        var typed = wb.Worksheet(ExcelSystemMtResultReportRenderer.TypedVerificationSheetName);
        Assert.Equal("BolAlg02", typed.Cell(2, 2).GetString());
        Assert.Equal("VarianceRatioPredicate", typed.Cell(2, 4).GetString());
        Assert.Equal(0.5, typed.Cell(2, 7).GetDouble());
    }

    [Fact]
    public void Render_without_evidence_dictionary_omits_typed_verification_sheet()
    {
        var xlsx = NewRenderer().Render(new[] { SampleRecord("mr-x", passed: true) }, FixedContext());

        using var wb = Open(xlsx);
        Assert.False(wb.Worksheets.Contains(ExcelSystemMtResultReportRenderer.TypedVerificationSheetName));
    }

    [Fact]
    public void Render_two_calls_are_structurally_identical()
    {
        // ClosedXML / OPC zip records system-clock mtimes for entries; raw byte
        // equality is not guaranteed. Pin structural invariants instead.
        var records = new[]
        {
            SampleRecord("mr-a", passed: true),
            SampleRecord("mr-b", passed: false),
        };
        var renderer = NewRenderer();

        var first = renderer.Render(records, FixedContext());
        var second = renderer.Render(records, FixedContext());

        using var wb1 = Open(first);
        using var wb2 = Open(second);
        Assert.Equal(wb1.Worksheets.Count, wb2.Worksheets.Count);
        Assert.Equal(
            wb1.Worksheet(ExcelSystemMtResultReportRenderer.SummarySheetName).Cell(ExcelSystemMtResultReportRenderer.SummaryFirstDataRow, 1).GetString(),
            wb2.Worksheet(ExcelSystemMtResultReportRenderer.SummarySheetName).Cell(ExcelSystemMtResultReportRenderer.SummaryFirstDataRow, 1).GetString());
        Assert.Equal(
            wb1.Worksheet(ExcelSystemMtResultReportRenderer.ChartsSheetName).Pictures.Count,
            wb2.Worksheet(ExcelSystemMtResultReportRenderer.ChartsSheetName).Pictures.Count);
    }

    // ---- PR-B regression facts -----------------------------------------------

    [Fact]
    public void Render_summary_title_cell_carries_report_context_title()
    {
        // M1 (PR-B): Excel renderer previously discarded ReportContext entirely.
        var ctx = new ReportContext(Title: "MetBench Demo Report", GeneratedAt: FixedGenAt);
        var xlsx = NewRenderer().Render(new[] { SampleRecord("mr-x", passed: true) }, ctx);

        using var wb = Open(xlsx);
        var summary = wb.Worksheet(ExcelSystemMtResultReportRenderer.SummarySheetName);
        Assert.Equal("MetBench Demo Report", summary.Cell(1, 1).GetString());
        Assert.True(summary.Cell(1, 1).Style.Font.Bold);
    }

    [Fact]
    public void Render_summary_meta_row_carries_generated_at()
    {
        // M1 (PR-B): the previous renderer discarded GeneratedAt; pin it.
        var xlsx = NewRenderer().Render(new[] { SampleRecord("mr-x", passed: true) }, FixedContext());

        using var wb = Open(xlsx);
        var summary = wb.Worksheet(ExcelSystemMtResultReportRenderer.SummarySheetName);
        var metaText = summary.Cell(2, 1).GetString();
        Assert.Contains("2026-05-27 12:30:00Z", metaText);
    }

    [Fact]
    public void Render_summary_counts_row_reflects_pass_fail_totals()
    {
        // M1 (PR-B): summary block now includes counts; matches PDF/Word title block.
        var records = new[]
        {
            SampleRecord("mr-a", passed: true),
            SampleRecord("mr-b", passed: true),
            SampleRecord("mr-c", passed: false),
        };
        var xlsx = NewRenderer().Render(records, FixedContext());

        using var wb = Open(xlsx);
        var summary = wb.Worksheet(ExcelSystemMtResultReportRenderer.SummarySheetName);
        var countsText = summary.Cell(3, 1).GetString();
        Assert.Contains("Records: 3", countsText);
        Assert.Contains("Passed: 2", countsText);
        Assert.Contains("Failed: 1", countsText);
    }

    [Fact]
    public void Render_charts_sheet_anchor_spacing_grows_with_chart_height()
    {
        // M3 (PR-B): hard-coded rowsPerChart=25 → derived from ChartRenderOptions.Height
        // so taller charts no longer overlap their neighbours.
        var records = new[] { SampleRecord("mr-a", passed: true), SampleRecord("mr-b", passed: true) };
        var smallChart = new ChartRenderOptions(Width: 720, Height: 480);
        var tallChart = new ChartRenderOptions(Width: 720, Height: 2000);

        var xlsxSmall = new ExcelSystemMtResultReportRenderer(
            new MetBench_BLL.Reporting.SystemMt.Charts.Rendering.SkiaChartRenderer(), smallChart)
            .Render(records, FixedContext());
        var xlsxTall = new ExcelSystemMtResultReportRenderer(
            new MetBench_BLL.Reporting.SystemMt.Charts.Rendering.SkiaChartRenderer(), tallChart)
            .Render(records, FixedContext());

        using var wbSmall = Open(xlsxSmall);
        using var wbTall = Open(xlsxTall);
        // Charts sheet picture count matches record count regardless of height.
        Assert.Equal(2, wbSmall.Worksheet(ExcelSystemMtResultReportRenderer.ChartsSheetName).Pictures.Count);
        Assert.Equal(2, wbTall.Worksheet(ExcelSystemMtResultReportRenderer.ChartsSheetName).Pictures.Count);
        // Tall-chart second anchor row must be larger than small-chart second anchor row.
        var smallAnchors = wbSmall.Worksheet(ExcelSystemMtResultReportRenderer.ChartsSheetName)
            .CellsUsed(c => c.Style.Font.Bold && c.GetString().StartsWith("mr-")).Select(c => c.Address.RowNumber).OrderBy(r => r).ToList();
        var tallAnchors = wbTall.Worksheet(ExcelSystemMtResultReportRenderer.ChartsSheetName)
            .CellsUsed(c => c.Style.Font.Bold && c.GetString().StartsWith("mr-")).Select(c => c.Address.RowNumber).OrderBy(r => r).ToList();
        Assert.Equal(2, smallAnchors.Count);
        Assert.Equal(2, tallAnchors.Count);
        Assert.True(tallAnchors[1] > smallAnchors[1],
            $"Tall chart second anchor (row {tallAnchors[1]}) must exceed small chart second anchor (row {smallAnchors[1]})");
    }
}
