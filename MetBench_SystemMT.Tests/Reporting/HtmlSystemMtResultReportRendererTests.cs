using MetBench_BLL.SystemMT.Persistence;
using MetBench_BLL.SystemMT.Reporting;
using Xunit;

namespace MetBench_SystemMT.Tests.Reporting;

public sealed class HtmlSystemMtResultReportRendererTests
{
    private static SystemMtResultRecord MakeRecord(
        string scenario = "OpenMocPinCellNuSigmaF",
        bool passed = true,
        string assertionName = "GreaterThan",
        string valueName = "k_eff",
        double sourceValue = 1.13,
        double followUpValue = 1.51,
        string failureReason = "",
        string? transformationName = null,
        Dictionary<string, string>? transformationParams = null,
        bool? inputGenerationSucceeded = null,
        string? inputGenerationLog = null,
        Dictionary<string, double>? sourceMetrics = null,
        Dictionary<string, double>? followUpMetrics = null)
    {
        return new SystemMtResultRecord
        {
            Id = "507f1f77bcf86cd799439011",
            MrName = scenario,
            RunAt = new DateTimeOffset(2026, 5, 9, 12, 34, 56, TimeSpan.Zero),
            AssertionName = assertionName,
            ValueName = valueName,
            SourceValue = sourceValue,
            FollowUpValue = followUpValue,
            Passed = passed,
            FailureReason = failureReason,
            SourceCaseName = "source",
            FollowUpCaseName = "follow-up",
            SourceElapsed = TimeSpan.FromSeconds(2.5),
            FollowUpElapsed = TimeSpan.FromSeconds(2.7),
            SourceExitCode = 0,
            FollowUpExitCode = 0,
            SourceMetrics = sourceMetrics ?? new Dictionary<string, double>(),
            FollowUpMetrics = followUpMetrics ?? new Dictionary<string, double>(),
            TransformationName = transformationName,
            TransformationParameters = transformationParams,
            InputGenerationSucceeded = inputGenerationSucceeded,
            InputGenerationLog = inputGenerationLog,
        };
    }

    [Fact]
    public void Render_null_records_throws_argument_null()
    {
        var renderer = new HtmlSystemMtResultReportRenderer();
        Assert.Throws<ArgumentNullException>(() => renderer.Render(null!));
    }

    [Fact]
    public void Render_empty_collection_produces_valid_empty_state()
    {
        var renderer = new HtmlSystemMtResultReportRenderer();
        var html = renderer.Render(Array.Empty<SystemMtResultRecord>());

        Assert.StartsWith("<!DOCTYPE html>", html);
        Assert.Contains("<html lang=\"en\">", html);
        Assert.Contains("Total: 0", html);
        Assert.Contains("Passed: 0", html);
        Assert.Contains("Failed: 0", html);
        Assert.Contains("No run results to display.", html);
        Assert.Contains("</html>", html);
    }

    [Fact]
    public void Render_single_passing_record_includes_scenario_assertion_and_values()
    {
        var renderer = new HtmlSystemMtResultReportRenderer();
        var html = renderer.Render(new[]
        {
            MakeRecord(scenario: "OpenMocPinCellNuSigmaF", sourceValue: 1.13, followUpValue: 1.51),
        });

        Assert.Contains("OpenMocPinCellNuSigmaF", html);
        Assert.Contains("GreaterThan on", html);
        Assert.Contains("k_eff", html);
        Assert.Contains("1.13", html);
        Assert.Contains("1.51", html);
        Assert.Contains("badge-pass", html);
        Assert.Contains(">PASS<", html);
        Assert.DoesNotContain(">FAIL<", html);
    }

    [Fact]
    public void Render_failing_record_marked_distinctly_with_failure_reason()
    {
        var renderer = new HtmlSystemMtResultReportRenderer();
        var html = renderer.Render(new[]
        {
            MakeRecord(passed: false, failureReason: "follow-up not greater than source"),
        });

        Assert.Contains("badge-fail", html);
        Assert.Contains(">FAIL<", html);
        Assert.Contains("row-fail", html);
        Assert.Contains("follow-up not greater than source", html);
    }

    [Fact]
    public void Render_records_with_input_generation_show_transformation_and_params()
    {
        var renderer = new HtmlSystemMtResultReportRenderer();
        var html = renderer.Render(new[]
        {
            MakeRecord(
                transformationName: "ScaleFuelSigmaA",
                transformationParams: new Dictionary<string, string> { ["factor"] = "1.5" },
                inputGenerationSucceeded: true,
                inputGenerationLog: "Scaled by 1.5"),
        });

        Assert.Contains("ScaleFuelSigmaA", html);
        Assert.Contains("factor", html);
        Assert.Contains("1.5", html);
        Assert.Contains("succeeded", html);
        Assert.Contains("Scaled by 1.5", html);
    }

    [Fact]
    public void Render_omits_transformation_section_when_absent()
    {
        var renderer = new HtmlSystemMtResultReportRenderer();
        var html = renderer.Render(new[] { MakeRecord(transformationName: null) });

        Assert.DoesNotContain("Transformation parameters", html);
        Assert.DoesNotContain("Input generation log", html);
    }

    [Fact]
    public void Render_escapes_html_in_scenario_name_and_failure_reason()
    {
        var renderer = new HtmlSystemMtResultReportRenderer();
        var html = renderer.Render(new[]
        {
            MakeRecord(
                scenario: "<script>alert('xss')</script>",
                passed: false,
                failureReason: "value <em>was</em> wrong & off"),
        });

        Assert.DoesNotContain("<script>alert('xss')</script>", html);
        Assert.Contains("&lt;script&gt;alert(&#39;xss&#39;)&lt;/script&gt;", html);
        Assert.Contains("&lt;em&gt;was&lt;/em&gt;", html);
        Assert.Contains("wrong &amp; off", html);
    }

    [Fact]
    public void Render_includes_pass_fail_summary_counts_for_mixed_records()
    {
        var renderer = new HtmlSystemMtResultReportRenderer();
        var html = renderer.Render(new[]
        {
            MakeRecord(passed: true),
            MakeRecord(passed: true),
            MakeRecord(passed: false, failureReason: "x"),
        });

        Assert.Contains("Total: 3", html);
        Assert.Contains("Passed: 2", html);
        Assert.Contains("Failed: 1", html);
    }

    [Fact]
    public void Render_uses_provided_context_title_and_generated_at()
    {
        var renderer = new HtmlSystemMtResultReportRenderer();
        var generatedAt = new DateTimeOffset(2026, 6, 1, 8, 0, 0, TimeSpan.Zero);
        var html = renderer.Render(
            Array.Empty<SystemMtResultRecord>(),
            new ReportContext("My Custom Report", generatedAt));

        Assert.Contains("<title>My Custom Report</title>", html);
        Assert.Contains("<h1>My Custom Report</h1>", html);
        Assert.Contains("2026-06-01T08:00:00.0000000+00:00", html);
    }

    [Fact]
    public void Render_renders_doubles_with_invariant_culture()
    {
        var prevCulture = Thread.CurrentThread.CurrentCulture;
        try
        {
            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("de-DE");
            var renderer = new HtmlSystemMtResultReportRenderer();
            var html = renderer.Render(new[]
            {
                MakeRecord(sourceValue: 1234.5, followUpValue: 6789.01),
            });

            Assert.Contains("1234.5", html);
            Assert.Contains("6789.01", html);
            Assert.DoesNotContain("1234,5", html);
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = prevCulture;
        }
    }

    [Fact]
    public void Render_includes_metrics_when_present()
    {
        var renderer = new HtmlSystemMtResultReportRenderer();
        var html = renderer.Render(new[]
        {
            MakeRecord(
                sourceMetrics: new Dictionary<string, double> { ["k_eff"] = 1.13, ["iterations"] = 553 },
                followUpMetrics: new Dictionary<string, double> { ["k_eff"] = 1.51, ["iterations"] = 464 }),
        });

        Assert.Contains("Source metrics", html);
        Assert.Contains("Follow-up metrics", html);
        Assert.Contains("iterations", html);
        Assert.Contains("553", html);
        Assert.Contains("464", html);
    }
}
