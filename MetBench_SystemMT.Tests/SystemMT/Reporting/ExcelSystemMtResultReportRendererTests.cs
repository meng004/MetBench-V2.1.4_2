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
    public void Render_empty_input_returns_workbook_with_title_and_header_no_data_rows()
    {
        var xlsx = NewRenderer().Render(Array.Empty<SystemMtResultRecord>(), FixedContext());

        using var wb = Open(xlsx);
        var summary = wb.Worksheet(ExcelSystemMtResultReportRenderer.SummarySheetName);
        Assert.NotNull(summary);
        Assert.Equal("MrName", summary.Cell(ExcelSystemMtResultReportRenderer.SummaryHeaderRow, 1).GetString());
        Assert.True(
            summary.Cell(ExcelSystemMtResultReportRenderer.SummaryFirstDataRow, 1).IsEmpty(),
            "Summary sheet must contain no data rows for empty input");
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
    public void Render_single_record_first_data_row_contains_mr_name()
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
    public void Render_multi_record_summary_has_one_data_row_per_record()
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
        // Verify by reading each expected data row position; the title / generated-at /
        // totals / header rows above are out of scope for this fact.
        for (int i = 0; i < records.Length; i++)
        {
            var row = ExcelSystemMtResultReportRenderer.SummaryFirstDataRow + i;
            Assert.Equal(records[i].MrName, summary.Cell(row, 1).GetString());
        }
        // No 4th data row.
        Assert.True(summary.Cell(
            ExcelSystemMtResultReportRenderer.SummaryFirstDataRow + records.Length, 1).IsEmpty());
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
    public void Render_summary_sheet_contains_report_context_title_at_top()
    {
        // PR-T2-T3 review-fix B1: ReportContext.Title / GeneratedAt / totals now
        // appear in the Summary sheet header rows (previously discarded). Parity
        // with PDF and Word title sections.
        var ctx = new ReportContext(Title: "Custom Boltzmann Excel Report", GeneratedAt: FixedGenAt);
        var xlsx = NewRenderer().Render(new[] { SampleRecord("mr-x", passed: true) }, ctx);

        using var wb = Open(xlsx);
        var summary = wb.Worksheet(ExcelSystemMtResultReportRenderer.SummarySheetName);
        Assert.Equal("Custom Boltzmann Excel Report",
            summary.Cell(ExcelSystemMtResultReportRenderer.SummaryTitleRow, 1).GetString());
        Assert.Contains("Generated at",
            summary.Cell(ExcelSystemMtResultReportRenderer.SummaryGeneratedAtRow, 1).GetString());
        Assert.Contains("Total: 1",
            summary.Cell(ExcelSystemMtResultReportRenderer.SummaryTotalsRow, 1).GetString());
        Assert.Contains("Passed: 1",
            summary.Cell(ExcelSystemMtResultReportRenderer.SummaryTotalsRow, 1).GetString());
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
            wb1.Worksheet(ExcelSystemMtResultReportRenderer.SummarySheetName)
               .Cell(ExcelSystemMtResultReportRenderer.SummaryFirstDataRow, 1).GetString(),
            wb2.Worksheet(ExcelSystemMtResultReportRenderer.SummarySheetName)
               .Cell(ExcelSystemMtResultReportRenderer.SummaryFirstDataRow, 1).GetString());
        Assert.Equal(
            wb1.Worksheet(ExcelSystemMtResultReportRenderer.ChartsSheetName).Pictures.Count,
            wb2.Worksheet(ExcelSystemMtResultReportRenderer.ChartsSheetName).Pictures.Count);
    }
}
