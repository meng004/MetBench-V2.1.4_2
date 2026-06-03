using MetBench_BLL.Core.SystemMT.ImportExport.Put;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.ImportExport;

public sealed class AGroupPutImportExportTests
{
    [Theory]
    [InlineData("P5", new[] { "t", "power", "precursor", "power_extrema" })]
    [InlineData("P4", new[] { "q", "p", "energy" })]
    [InlineData("P9", new[] { "k_eff", "sigma_k", "reaction_balance" })]
    public void A_group_fixtures_validate_as_single_sut_import_units(string sutId, string[] observables)
    {
        var unit = AGroupPutFixtures.Create(sutId);

        var validation = SutImportValidator.Validate(unit);

        Assert.True(validation.IsValid, string.Join(Environment.NewLine, validation.Errors));
        Assert.Equal(sutId, unit.Sut.SutId);
        Assert.Equal(observables, unit.Sut.Observables.Select(o => o.Name));
        Assert.All(unit.Mrs, mr => Assert.Equal(sutId, mr.SutId));
        Assert.All(unit.IoGroups, io => Assert.Equal(sutId, io.SutId));
        Assert.All(unit.Mutations, mutation => Assert.Equal(sutId, mutation.SutId));
        Assert.All(unit.Detections, detection => Assert.Equal(EvidenceKind.ImportedResearchEvidence, detection.EvidenceKind));
    }

    [Fact]
    public void P9_fixture_is_classified_as_surrogate()
    {
        var unit = AGroupPutFixtures.Create("P9");

        Assert.Equal(ProgramKind.Surrogate, unit.Sut.ProgramKind);
        Assert.Contains("surrogate", unit.Sut.Adapter.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void P9_without_surrogate_classification_is_rejected()
    {
        var unit = AGroupPutFixtures.Create("P9") with
        {
            Sut = AGroupPutFixtures.Create("P9").Sut with { ProgramKind = ProgramKind.MonteCarlo }
        };

        var validation = SutImportValidator.Validate(unit);

        Assert.False(validation.IsValid);
        Assert.Contains(validation.Errors, e => e.Contains("P9", StringComparison.OrdinalIgnoreCase)
            && e.Contains("surrogate", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validator_rejects_cross_sut_mr_reference()
    {
        var unit = AGroupPutFixtures.Create("P5");
        var badMr = unit.Mrs[0] with { SutId = "P4" };
        unit = unit with { Mrs = ReplaceFirst(unit.Mrs, badMr) };

        var validation = SutImportValidator.Validate(unit);

        Assert.False(validation.IsValid);
        Assert.Contains(validation.Errors, e => e.Contains("single-SUT closure", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validator_rejects_unknown_observable_reference()
    {
        var unit = AGroupPutFixtures.Create("P4");
        var badMr = unit.Mrs[0] with { ObservableRefs = unit.Mrs[0].ObservableRefs.Append("unknown_energy").ToArray() };
        unit = unit with { Mrs = ReplaceFirst(unit.Mrs, badMr) };

        var validation = SutImportValidator.Validate(unit);

        Assert.False(validation.IsValid);
        Assert.Contains(validation.Errors, e => e.Contains("unknown observable", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("missing-mr", null, null)]
    [InlineData(null, "missing-mutation", null)]
    [InlineData(null, null, "missing-io")]
    public void Validator_rejects_unknown_detection_references(string? mrId, string? mutationId, string? ioGroupId)
    {
        var unit = AGroupPutFixtures.Create("P5");
        var detection = unit.Detections[0] with
        {
            MrId = mrId ?? unit.Detections[0].MrId,
            MutationId = mutationId ?? unit.Detections[0].MutationId,
            IoGroupId = ioGroupId ?? unit.Detections[0].IoGroupId
        };
        unit = unit with { Detections = ReplaceFirst(unit.Detections, detection) };

        var validation = SutImportValidator.Validate(unit);

        Assert.False(validation.IsValid);
        Assert.Contains(validation.Errors, e => e.Contains("detection", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validator_rejects_missing_provenance()
    {
        var unit = AGroupPutFixtures.Create("P4") with { Provenance = Provenance.Empty };

        var validation = SutImportValidator.Validate(unit);

        Assert.False(validation.IsValid);
        Assert.Contains(validation.Errors, e => e.Contains("provenance", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validator_rejects_duplicate_mr_ids()
    {
        var unit = AGroupPutFixtures.Create("P5");
        unit = unit with { Mrs = unit.Mrs.Concat(new[] { unit.Mrs[0] }).ToArray() };

        var validation = SutImportValidator.Validate(unit);

        Assert.False(validation.IsValid);
        Assert.Contains(validation.Errors, e => e.Contains("duplicate", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("../escape.json")]
    [InlineData("/tmp/escape.json")]
    [InlineData("subdir/../../escape.json")]
    public void Package_path_validator_rejects_unsafe_paths(string relativePath)
    {
        var root = Path.Combine(Path.GetTempPath(), "MetBenchPutImportPathTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        var ex = Assert.Throws<SutImportValidationException>(
            () => SutImportPackageExporter.ResolvePackageFile(root, relativePath));

        Assert.Contains("path traversal", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("P5")]
    [InlineData("P4")]
    [InlineData("P9")]
    public void Export_round_trips_A_group_units(string sutId)
    {
        var unit = AGroupPutFixtures.Create(sutId);
        var root = Path.Combine(Path.GetTempPath(), "MetBenchPutImportRoundTrip", Guid.NewGuid().ToString("N"));

        SutImportPackageExporter.Export(unit, root);
        var imported = SutImportPackageExporter.Import(root);

        Assert.Equal(unit.Sut.SutId, imported.Sut.SutId);
        Assert.Equal(unit.Provenance.Commit, imported.Provenance.Commit);
        Assert.Equal(unit.Sut.Observables.Select(o => o.Name), imported.Sut.Observables.Select(o => o.Name));
        Assert.Equal(unit.Mrs.Select(m => m.MrId), imported.Mrs.Select(m => m.MrId));
        Assert.Equal(unit.Mutations.Select(m => m.MutationId), imported.Mutations.Select(m => m.MutationId));
        Assert.Equal(unit.Detections.Select(d => d.DetectionId), imported.Detections.Select(d => d.DetectionId));
        Assert.True(SutImportValidator.Validate(imported).IsValid);
    }

    [Fact]
    public void Compatibility_defaults_to_imported_only_when_bindings_are_missing()
    {
        var unit = AGroupPutFixtures.Create("P5");

        var profile = CompatibilityProfileBuilder.Build(unit);

        Assert.Equal(RuntimeReadiness.ImportedOnly, profile.OverallReadiness);
        Assert.False(profile.CanPromoteToRuntimeCandidate());
        Assert.Contains(profile.Findings, f => f.Contains("transform", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(profile.Findings, f => f.Contains("assertion", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Missing_transform_binding_prevents_runtime_candidate()
    {
        var unit = AGroupPutFixtures.Create("P4");
        var mr = unit.Mrs[0] with
        {
            AssertionBinding = new AssertionBinding(
                CompatibilityStatus.NativeSupported,
                "EnergyInvariantPredicate",
                Metric: "energy",
                Tolerance: ToleranceSpec.Relative(1e-8))
        };
        unit = unit with { Mrs = ReplaceFirst(unit.Mrs, mr) };

        var profile = CompatibilityProfileBuilder.Build(unit);

        Assert.Equal(RuntimeReadiness.ImportedOnly, profile.OverallReadiness);
        Assert.Contains(profile.Findings, f => f.Contains("transform", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Missing_assertion_binding_prevents_runtime_candidate()
    {
        var unit = AGroupPutFixtures.Create("P4");
        var mr = unit.Mrs[0] with
        {
            TransformBinding = new TransformBinding(
                CompatibilityStatus.NativeSupported,
                "IdentityTimeStepTransform",
                Shape: ShapeSpec.ScalarSeries())
        };
        unit = unit with { Mrs = ReplaceFirst(unit.Mrs, mr) };

        var profile = CompatibilityProfileBuilder.Build(unit);

        Assert.Equal(RuntimeReadiness.ImportedOnly, profile.OverallReadiness);
        Assert.Contains(profile.Findings, f => f.Contains("assertion", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Explicit_P4_energy_invariant_can_be_runtime_candidate()
    {
        var unit = AGroupPutFixtures.Create("P4");
        var mr = unit.Mrs[0] with
        {
            TransformBinding = new TransformBinding(
                CompatibilityStatus.NativeSupported,
                "IdentityTimeStepTransform",
                Shape: ShapeSpec.ScalarSeries()),
            AssertionBinding = new AssertionBinding(
                CompatibilityStatus.NativeSupported,
                "EnergyInvariantPredicate",
                Metric: "energy",
                Tolerance: ToleranceSpec.Relative(1e-8))
        };
        unit = unit with { Mrs = ReplaceFirst(unit.Mrs, mr) };

        var profile = CompatibilityProfileBuilder.Build(unit);

        Assert.Equal(RuntimeReadiness.RuntimeCandidate, profile.OverallReadiness);
        Assert.True(profile.CanPromoteToRuntimeCandidate());
    }

    [Fact]
    public void P9_runtime_candidate_requires_sigma_k_and_noise_semantics()
    {
        var unit = AGroupPutFixtures.Create("P9");
        var mr = unit.Mrs[0] with
        {
            TransformBinding = new TransformBinding(
                CompatibilityStatus.NativeSupported,
                "ParticleCountScaleTransform",
                Shape: ShapeSpec.Scalar()),
            AssertionBinding = new AssertionBinding(
                CompatibilityStatus.NativeSupported,
                "BinaryComparisonPredicate.Equal",
                Metric: "k_eff",
                Tolerance: ToleranceSpec.Absolute(1e-3))
        };
        unit = unit with { Mrs = ReplaceFirst(unit.Mrs, mr) };

        var profile = CompatibilityProfileBuilder.Build(unit);

        Assert.Equal(RuntimeReadiness.ImportedOnly, profile.OverallReadiness);
        Assert.Contains(profile.Findings, f => f.Contains("sigma_k", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void P9_with_sigma_k_and_noise_semantics_can_be_runtime_candidate()
    {
        var unit = AGroupPutFixtures.Create("P9");
        var mr = unit.Mrs[0] with
        {
            TransformBinding = new TransformBinding(
                CompatibilityStatus.NativeSupported,
                "ParticleCountScaleTransform",
                Shape: ShapeSpec.Scalar()),
            AssertionBinding = new AssertionBinding(
                CompatibilityStatus.NativeSupported,
                "NoiseAwareBinaryComparisonPredicate",
                Metric: "k_eff",
                Tolerance: ToleranceSpec.Absolute(1e-3),
                Notes: "uses sigma_k for two-sigma compatibility")
        };
        unit = unit with { Mrs = ReplaceFirst(unit.Mrs, mr) };

        var profile = CompatibilityProfileBuilder.Build(unit);

        Assert.Equal(RuntimeReadiness.RuntimeCandidate, profile.OverallReadiness);
    }

    [Fact]
    public void Export_does_not_write_live_runtime_paths()
    {
        var unit = AGroupPutFixtures.Create("P5");
        var root = Path.Combine(Path.GetTempPath(), "MetBenchPutImportNoLiveWrites", Guid.NewGuid().ToString("N"));

        SutImportPackageExporter.Export(unit, root);

        Assert.False(Directory.Exists(Path.Combine(root, "SUT")));
        Assert.False(File.Exists(Path.Combine(root, "catalog.json")));
        Assert.False(Directory.Exists(Path.Combine(root, "MetBench_DataBase")));
        Assert.False(Directory.Exists(Path.Combine(root, "ExecutionEvidence")));
    }

    private static IReadOnlyList<T> ReplaceFirst<T>(IReadOnlyList<T> source, T replacement)
    {
        return source.Select((item, index) => index == 0 ? replacement : item).ToArray();
    }
}
