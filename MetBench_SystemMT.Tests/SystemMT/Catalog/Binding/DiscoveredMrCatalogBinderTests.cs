using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MetBench_BLL.SystemMT.Catalog;
using MetBench_BLL.SystemMT.Catalog.Binding;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Catalog.Binding;

/// <summary>
/// PR-2 T4-to-T0 MR Discovery Binder — pins the controlled boundary between
/// validated T4 discovery candidates and T0 System MT catalog assets.
///
/// The binder is a gate, not a shortcut:
///   * pure / deterministic / no IO,
///   * accepts <see cref="DiscoveredMrBindingDraft"/>, returns a
///     manifest-compatible <see cref="MrBindingDefinition"/> embedded in a
///     <see cref="DiscoveredMrBindingResult"/> + provenance,
///   * fail-closed for blank, unsupported, out-of-range, traversal-unsafe inputs,
///   * never writes to or reads from <c>SUT/&lt;sut&gt;/catalog.json</c>.
/// </summary>
public sealed class DiscoveredMrCatalogBinderTests
{
    private static DiscoveredMrBindingDraft ValidDraft(Action<DiscoveredMrBindingDraftBuilder>? mutate = null)
    {
        var b = new DiscoveredMrBindingDraftBuilder();
        mutate?.Invoke(b);
        return b.Build();
    }

    [Fact]
    public void Bind_valid_discovery_draft_returns_manifest_binding_with_provenance()
    {
        var binder = new DiscoveredMrCatalogBinder();
        var draft = ValidDraft();

        var result = binder.Bind(draft);

        Assert.True(result.Success, string.Join("; ", result.Errors.Select(e => e.Message)));
        Assert.NotNull(result.Binding);
        Assert.NotNull(result.Provenance);
        Assert.Empty(result.Errors);

        Assert.Equal(draft.MrId, result.Binding!.MrId);
        Assert.Equal(draft.SutId, result.Binding.SutName);
        Assert.Equal(draft.AssertionTypeCode, result.Binding.AssertionTypeCode);
        Assert.Equal(draft.ValueName, result.Binding.ValueName);
        Assert.Equal(draft.TransformationName, result.Binding.TransformationName);

        Assert.Equal(draft.DiscoveryMethod, result.Provenance!.DiscoveryMethod);
        Assert.Equal(draft.DiscoveryRunId, result.Provenance.DiscoveryRunId);
        Assert.Equal(draft.Confidence, result.Provenance.Confidence);
        Assert.Equal(draft.SutId, result.Provenance.SutId);
        Assert.Equal(draft.MrId, result.Provenance.MrId);
    }

    [Fact]
    public void Bind_valid_draft_emits_binding_that_passes_existing_MrBindingDefinition_Validate()
    {
        var result = new DiscoveredMrCatalogBinder().Bind(ValidDraft());

        Assert.True(result.Success);
        // Binder output must pass the existing schema check without further patching.
        var ex = Record.Exception(() => result.Binding!.Validate());
        Assert.Null(ex);
    }

    [Fact]
    public void Bind_output_embeds_into_SystemMtCatalogDocument_and_passes_Document_Validate()
    {
        var result = new DiscoveredMrCatalogBinder().Bind(ValidDraft());
        Assert.True(result.Success);

        var doc = new SystemMtCatalogDocument
        {
            SutName = result.Binding!.SutName,
            Program = new ProgramDefinition
            {
                ProgramName = result.Binding.SutName,
                RunnerScriptRelativePath = "runner.py",
                InputParserScriptRelativePath = "in_parser.py",
                OutputParserScriptRelativePath = "out_parser.py",
                InputAdapterScriptRelativePath = "in_adapter.py",
                OutputAdapterScriptRelativePath = "out_adapter.py",
                PythonExecutableKind = "system",
            },
            Mrs = new List<MrBindingDefinition> { result.Binding },
        };

        var ex = Record.Exception(() => doc.Validate());
        Assert.Null(ex);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Bind_rejects_blank_SutId(string blank)
    {
        var result = new DiscoveredMrCatalogBinder().Bind(ValidDraft(b => b.SutId = blank));

        Assert.False(result.Success);
        Assert.Null(result.Binding);
        Assert.Null(result.Provenance);
        Assert.Contains(result.Errors, e => string.Equals(e.Field, nameof(DiscoveredMrBindingDraft.SutId), StringComparison.Ordinal));
    }

    [Fact]
    public void Bind_rejects_blank_MrId()
    {
        var result = new DiscoveredMrCatalogBinder().Bind(ValidDraft(b => b.MrId = ""));

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Field == nameof(DiscoveredMrBindingDraft.MrId));
    }

    [Theory]
    [InlineData("")]
    [InlineData("approximately-equal")]
    [InlineData("strictly-greater")]
    public void Bind_rejects_unknown_or_blank_AssertionTypeCode(string bad)
    {
        var result = new DiscoveredMrCatalogBinder().Bind(ValidDraft(b => b.AssertionTypeCode = bad));

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Field == nameof(DiscoveredMrBindingDraft.AssertionTypeCode));
        if (!string.IsNullOrWhiteSpace(bad))
        {
            Assert.Contains(result.Errors, e => e.Message.Contains(bad, StringComparison.Ordinal));
        }
    }

    [Theory]
    [InlineData("less-noise-aware")]
    [InlineData("greater-noise-aware")]
    public void Bind_rejects_noise_aware_assertion_codes_per_LegacyAssertionPredicateMapper_fail_closed_list(string code)
    {
        var result = new DiscoveredMrCatalogBinder().Bind(ValidDraft(b => b.AssertionTypeCode = code));

        Assert.False(result.Success);
        var error = Assert.Single(result.Errors.Where(e => e.Field == nameof(DiscoveredMrBindingDraft.AssertionTypeCode)));
        Assert.Contains(code, error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Bind_rejects_blank_ValueName()
    {
        var result = new DiscoveredMrCatalogBinder().Bind(ValidDraft(b => b.ValueName = ""));

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Field == nameof(DiscoveredMrBindingDraft.ValueName));
    }

    [Fact]
    public void Bind_rejects_blank_DiscoveryMethod()
    {
        var result = new DiscoveredMrCatalogBinder().Bind(ValidDraft(b => b.DiscoveryMethod = ""));

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Field == nameof(DiscoveredMrBindingDraft.DiscoveryMethod));
    }

    [Fact]
    public void Bind_rejects_blank_DiscoveryRunId()
    {
        var result = new DiscoveredMrCatalogBinder().Bind(ValidDraft(b => b.DiscoveryRunId = ""));

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Field == nameof(DiscoveredMrBindingDraft.DiscoveryRunId));
    }

    [Theory]
    [InlineData(-0.0001)]
    [InlineData(1.0001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Bind_rejects_Confidence_outside_unit_interval_or_nonfinite(double confidence)
    {
        var result = new DiscoveredMrCatalogBinder().Bind(ValidDraft(b => b.Confidence = confidence));

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Field == nameof(DiscoveredMrBindingDraft.Confidence));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    [InlineData(0.5)]
    public void Bind_accepts_Confidence_at_unit_interval_bounds(double confidence)
    {
        var result = new DiscoveredMrCatalogBinder().Bind(ValidDraft(b => b.Confidence = confidence));

        Assert.True(result.Success, string.Join("; ", result.Errors.Select(e => e.Message)));
        Assert.Equal(confidence, result.Provenance!.Confidence);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Bind_rejects_TimeoutSeconds_not_positive(int timeout)
    {
        var result = new DiscoveredMrCatalogBinder().Bind(ValidDraft(b => b.TimeoutSeconds = timeout));

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Field == nameof(DiscoveredMrBindingDraft.TimeoutSeconds));
    }

    [Fact]
    public void Bind_rejects_blank_WorkRootName()
    {
        var result = new DiscoveredMrCatalogBinder().Bind(ValidDraft(b => b.WorkRootName = ""));

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Field == nameof(DiscoveredMrBindingDraft.WorkRootName));
    }

    [Fact]
    public void Bind_rejects_blank_TransformationName()
    {
        var result = new DiscoveredMrCatalogBinder().Bind(ValidDraft(b => b.TransformationName = ""));

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Field == nameof(DiscoveredMrBindingDraft.TransformationName));
    }

    [Fact]
    public void Bind_rejects_DefaultParameters_with_blank_key()
    {
        var result = new DiscoveredMrCatalogBinder().Bind(ValidDraft(b =>
            b.DefaultParameters = new Dictionary<string, string> { ["factor"] = "2", ["   "] = "x" }));

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Field == nameof(DiscoveredMrBindingDraft.DefaultParameters));
    }

    [Theory]
    [InlineData("../../../etc/passwd")]
    [InlineData("sample/../../../escape.json")]
    [InlineData("/abs/path/case.json")]
    public void Bind_rejects_unsafe_SampleCaseRelativePath(string unsafePath)
    {
        var result = new DiscoveredMrCatalogBinder().Bind(ValidDraft(b => b.SampleCaseRelativePath = unsafePath));

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Field == nameof(DiscoveredMrBindingDraft.SampleCaseRelativePath));
    }

    [Fact]
    public void Bind_rejects_approx_assertion_without_any_tolerance_source()
    {
        // approx requires either ToleranceRel/Abs > 0 or NoiseAware=true; the draft does not
        // carry tolerance fields, so the binder must default the binding's tolerance to zero,
        // which makes MrBindingDefinition.Validate() reject the approx-class binding. The
        // binder must surface that schema error as a binding error rather than emitting an
        // unvalidated binding.
        var result = new DiscoveredMrCatalogBinder().Bind(ValidDraft(b => b.AssertionTypeCode = "approx"));

        Assert.False(result.Success);
        Assert.NotEmpty(result.Errors);
        Assert.Null(result.Binding);
    }

    [Fact]
    public void Bind_does_not_touch_filesystem_under_a_sentinel_SUT_root()
    {
        // Create a sentinel directory that mimics a "SUT/" tree under temp. The binder must
        // not read or write anywhere under it. After binding several distinct drafts, the
        // sentinel directory must contain ONLY the files we placed.
        var sentinelRoot = Path.Combine(Path.GetTempPath(), "metbench-binder-sentinel-" + Guid.NewGuid().ToString("N"));
        var sentinelSutDir = Path.Combine(sentinelRoot, "fake-sut");
        Directory.CreateDirectory(sentinelSutDir);
        var sentinelCatalog = Path.Combine(sentinelSutDir, "catalog.json");
        File.WriteAllText(sentinelCatalog, "{\"sut_name\": \"fake-sut\"}");
        var initialContent = File.ReadAllText(sentinelCatalog);

        try
        {
            var binder = new DiscoveredMrCatalogBinder();
            for (var i = 0; i < 5; i++)
            {
                var r = binder.Bind(ValidDraft(b => b.MrId = $"draft-{i}-mr"));
                Assert.True(r.Success, $"draft {i}: {string.Join("; ", r.Errors.Select(e => e.Message))}");
            }

            // Sentinel directory must be untouched.
            var files = Directory.GetFiles(sentinelRoot, "*", SearchOption.AllDirectories);
            Assert.Single(files);
            Assert.Equal(sentinelCatalog, files[0]);
            Assert.Equal(initialContent, File.ReadAllText(sentinelCatalog));
        }
        finally
        {
            try { Directory.Delete(sentinelRoot, recursive: true); } catch { /* swallow */ }
        }
    }

    [Fact]
    public void Bind_provenance_BoundAtUtc_is_recent_and_in_UTC()
    {
        var before = DateTimeOffset.UtcNow.AddSeconds(-1);
        var result = new DiscoveredMrCatalogBinder().Bind(ValidDraft());
        var after = DateTimeOffset.UtcNow.AddSeconds(1);

        Assert.True(result.Success);
        Assert.NotNull(result.Provenance);
        Assert.InRange(result.Provenance!.BoundAtUtc, before, after);
        Assert.Equal(TimeSpan.Zero, result.Provenance.BoundAtUtc.Offset);
    }

    [Fact]
    public void Bind_collects_all_independent_field_errors_in_one_call()
    {
        // Several independent fields are bad. The binder collects all errors so the caller
        // sees the full list in one pass instead of getting one error at a time.
        var result = new DiscoveredMrCatalogBinder().Bind(ValidDraft(b =>
        {
            b.SutId = "";
            b.MrId = "";
            b.AssertionTypeCode = "unknown-code";
            b.Confidence = 2.0;
        }));

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Field == nameof(DiscoveredMrBindingDraft.SutId));
        Assert.Contains(result.Errors, e => e.Field == nameof(DiscoveredMrBindingDraft.MrId));
        Assert.Contains(result.Errors, e => e.Field == nameof(DiscoveredMrBindingDraft.AssertionTypeCode));
        Assert.Contains(result.Errors, e => e.Field == nameof(DiscoveredMrBindingDraft.Confidence));
    }

    /// <summary>
    /// Fluent builder for tests so each fact can mutate one field on a known-good draft.
    /// </summary>
    private sealed class DiscoveredMrBindingDraftBuilder
    {
        public string SutId { get; set; } = "heat-equation";
        public string MrId { get; set; } = "heat-equation-amplitude-discovered";
        public string DisplayName { get; set; } = "Heat equation amplitude (discovered)";
        public string EquationKey { get; set; } = "fourier";
        public string MetaPattern { get; set; } = "Mono";
        public string TransformationName { get; set; } = "ScaleField";
        public string AssertionTypeCode { get; set; } = "greater";
        public string ValueName { get; set; } = "max_temperature";
        public string SampleCaseRelativePath { get; set; } = "sample/base.json";
        public string WorkRootName { get; set; } = "MetBenchHeatEquationDiscovered";
        public int TimeoutSeconds { get; set; } = 30;
        public IReadOnlyDictionary<string, string> DefaultParameters { get; set; } =
            new Dictionary<string, string> { ["factor"] = "2" };
        public IReadOnlyList<DiscoveredMrBindingTransformStep> TransformSteps { get; set; } =
            new[] { new DiscoveredMrBindingTransformStep("ScaleField", "/source/amplitude") };
        public string DiscoveryMethod { get; set; } = "MetaPattern-Structural";
        public string DiscoveryRunId { get; set; } = "run-001";
        public double Confidence { get; set; } = 0.82;

        public DiscoveredMrBindingDraft Build() => new(
            SutId: SutId,
            MrId: MrId,
            DisplayName: DisplayName,
            EquationKey: EquationKey,
            MetaPattern: MetaPattern,
            TransformationName: TransformationName,
            AssertionTypeCode: AssertionTypeCode,
            ValueName: ValueName,
            SampleCaseRelativePath: SampleCaseRelativePath,
            WorkRootName: WorkRootName,
            TimeoutSeconds: TimeoutSeconds,
            DefaultParameters: DefaultParameters,
            TransformSteps: TransformSteps,
            DiscoveryMethod: DiscoveryMethod,
            DiscoveryRunId: DiscoveryRunId,
            Confidence: Confidence);
    }
}
