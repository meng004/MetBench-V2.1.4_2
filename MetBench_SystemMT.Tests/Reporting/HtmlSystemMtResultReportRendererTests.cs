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
            Id = Guid.Parse("507f1f77-bcf8-6cd7-9943-9011deadbeef"),
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

    // ---- PR-126: ExecutionEvidence.TypedVerification projection ----

    private static readonly Guid TestRecordId = Guid.Parse("507f1f77-bcf8-6cd7-9943-9011deadbeef");

    private static ExecutionEvidence MakeEvidence(TypedVerificationEvidence? typed)
        => new()
        {
            IdEvidence = Guid.NewGuid(),
            ExecutionId = TestRecordId,
            TypedVerification = typed,
        };

    [Fact]
    public void Render_without_evidence_dictionary_matches_legacy_overload_byte_identical()
    {
        var renderer = new HtmlSystemMtResultReportRenderer();
        var record = MakeRecord();
        // Pin GeneratedAt so the two Render calls share a single timestamp;
        // otherwise DateTimeOffset.UtcNow differs by microseconds between calls.
        var ctx = new ReportContext(
            Title: "MetBench System-Level MT Run Report",
            GeneratedAt: new DateTimeOffset(2026, 6, 1, 8, 0, 0, TimeSpan.Zero));
        var legacy = renderer.Render(new[] { record }, ctx);
        var evidenceAware = renderer.Render(new[] { record }, evidenceByExecutionId: null, ctx);

        Assert.Equal(legacy, evidenceAware);
    }

    [Fact]
    public void Render_with_empty_evidence_dictionary_omits_typed_section()
    {
        var renderer = new HtmlSystemMtResultReportRenderer();
        var record = MakeRecord();
        var html = renderer.Render(
            new[] { record },
            evidenceByExecutionId: new Dictionary<Guid, ExecutionEvidence>());

        Assert.DoesNotContain("Typed status", html);
        Assert.DoesNotContain("Spec ID", html);
    }

    [Fact]
    public void Render_record_with_matching_typed_mr_evidence_surfaces_diagnostic()
    {
        var renderer = new HtmlSystemMtResultReportRenderer();
        var record = MakeRecord();
        var evidence = MakeEvidence(new TypedVerificationEvidence
        {
            SpecId = "heat-equation-amplitude",
            SpecKind = "MrSpec",
            PredicateId = "amplitude-greater",
            PredicateKind = "BinaryComparison",
            Status = "Passed",
            Passed = true,
            Diagnostic = new TypedDiagnosticEvidence
            {
                Expected = 1.13,
                Actual = 1.51,
                Residual = 0.38,
                Tolerance = 1e-6,
            },
        });

        var html = renderer.Render(
            new[] { record },
            new Dictionary<Guid, ExecutionEvidence> { [TestRecordId] = evidence });

        Assert.Contains("Spec ID", html);
        Assert.Contains("heat-equation-amplitude", html);
        Assert.Contains("Spec kind", html);
        Assert.Contains("MrSpec", html);
        Assert.Contains("Predicate", html);
        Assert.Contains("amplitude-greater", html);
        Assert.Contains("BinaryComparison", html);
        Assert.Contains("Typed status", html);
        // Status "Passed" appears in the typed section; the existing PASS badge already exists.
        Assert.Contains(">Passed<", html);
        Assert.Contains("Expected", html);
        Assert.Contains("Actual", html);
        Assert.Contains("Residual", html);
        Assert.Contains("Tolerance", html);
        Assert.Contains("1.13", html);
        Assert.Contains("1.51", html);
        Assert.Contains("0.38", html);
        Assert.Contains("1E-06", html); // invariant-culture "G" of 1e-6
    }

    [Fact]
    public void Render_record_with_skipped_evidence_shows_reason_and_omits_diagnostic()
    {
        var renderer = new HtmlSystemMtResultReportRenderer();
        var record = MakeRecord();
        var evidence = MakeEvidence(new TypedVerificationEvidence
        {
            SpecId = "heat-equation-amplitude",
            SpecKind = "MrSpec",
            PredicateId = "amplitude-greater",
            PredicateKind = "BinaryComparison",
            Status = "SkippedMissingObservable",
            Passed = null,
            Diagnostic = null,
            SkipOrInvalidReason = "Required observable is missing from role outputs.",
        });

        var html = renderer.Render(
            new[] { record },
            new Dictionary<Guid, ExecutionEvidence> { [TestRecordId] = evidence });

        Assert.Contains("Typed status", html);
        Assert.Contains("SkippedMissingObservable", html);
        Assert.Contains("Skip reason", html);
        Assert.Contains("Required observable is missing", html);
        Assert.DoesNotContain("<dt>Expected</dt>", html);
        Assert.DoesNotContain("<dt>Actual</dt>", html);
        Assert.DoesNotContain("<dt>Residual</dt>", html);
        Assert.DoesNotContain("<dt>Tolerance</dt>", html);
    }

    [Fact]
    public void Render_record_with_property_spec_evidence_lists_predicates_in_order()
    {
        var renderer = new HtmlSystemMtResultReportRenderer();
        var record = MakeRecord();
        var evidence = MakeEvidence(new TypedVerificationEvidence
        {
            SpecId = "neutron-flux-positivity",
            SpecKind = "PropertySpec",
            Status = "Held",
            Passed = true,
            PropertyPredicates = new List<TypedPropertyPredicateEvidence>
            {
                new()
                {
                    PredicateId = "phi-nonneg",
                    PredicateKind = "Bound",
                    Status = "Held",
                    Residual = 0.0,
                    Tolerance = 1e-12,
                    ExpectedJson = "{\"lower\":0}",
                    ActualJson = "0.5",
                },
                new()
                {
                    PredicateId = "phi-shape-monotone",
                    PredicateKind = "Shape",
                    Status = "Violated",
                    Residual = 0.03,
                    Tolerance = 0.01,
                    Reason = "Decrease at index 4",
                    ExpectedJson = "\"NonIncreasing\"",
                    ActualJson = "[1.0,0.9,0.7,0.8]",
                },
            },
        });

        var html = renderer.Render(
            new[] { record },
            new Dictionary<Guid, ExecutionEvidence> { [TestRecordId] = evidence });

        Assert.Contains("PropertySpec", html);
        Assert.Contains("neutron-flux-positivity", html);
        Assert.Contains("Property predicates", html);
        Assert.Contains("phi-nonneg", html);
        Assert.Contains("phi-shape-monotone", html);
        // Order: nonneg must precede shape-monotone in the rendered HTML.
        var nonnegIdx = html.IndexOf("phi-nonneg", StringComparison.Ordinal);
        var shapeIdx = html.IndexOf("phi-shape-monotone", StringComparison.Ordinal);
        Assert.True(nonnegIdx > 0 && shapeIdx > nonnegIdx,
            $"Property predicates must render in source order; got nonnegIdx={nonnegIdx}, shapeIdx={shapeIdx}.");
        Assert.Contains("Decrease at index 4", html);
        // MR-only fields must not leak into a PropertySpec block.
        Assert.DoesNotContain("amplitude-greater", html);
    }

    [Fact]
    public void Render_unmatched_record_omits_typed_section_when_only_other_records_have_evidence()
    {
        var renderer = new HtmlSystemMtResultReportRenderer();
        var record = MakeRecord();
        // Evidence keyed by a DIFFERENT executionId -- record has no match.
        var unrelatedId = Guid.NewGuid();
        var evidence = MakeEvidence(new TypedVerificationEvidence
        {
            SpecId = "unrelated",
            SpecKind = "MrSpec",
            Status = "Passed",
            Passed = true,
        });
        evidence.ExecutionId = unrelatedId;

        var html = renderer.Render(
            new[] { record },
            new Dictionary<Guid, ExecutionEvidence> { [unrelatedId] = evidence });

        Assert.DoesNotContain("Typed status", html);
        Assert.DoesNotContain("unrelated", html);
    }

    [Fact]
    public void Render_evidence_aware_escapes_user_supplied_strings()
    {
        var renderer = new HtmlSystemMtResultReportRenderer();
        var record = MakeRecord();
        var evidence = MakeEvidence(new TypedVerificationEvidence
        {
            SpecId = "<script>alert('xss')</script>",
            SpecKind = "MrSpec",
            PredicateId = "x & y",
            PredicateKind = "BinaryComparison",
            Status = "InvalidSpec",
            Passed = null,
            SkipOrInvalidReason = "<em>broken</em> & malformed",
        });

        var html = renderer.Render(
            new[] { record },
            new Dictionary<Guid, ExecutionEvidence> { [TestRecordId] = evidence });

        Assert.DoesNotContain("<script>alert('xss')</script>", html);
        Assert.Contains("&lt;script&gt;alert(&#39;xss&#39;)&lt;/script&gt;", html);
        Assert.Contains("x &amp; y", html);
        Assert.Contains("&lt;em&gt;broken&lt;/em&gt; &amp; malformed", html);
    }
}
