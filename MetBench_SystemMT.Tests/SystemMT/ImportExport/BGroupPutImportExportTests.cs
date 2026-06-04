using MetBench_BLL.Core.SystemMT.ImportExport.Put;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.ImportExport;

public sealed class BGroupPutImportExportTests
{
    [Theory]
    [InlineData("P3", new[] { "t", "trajectory", "centroid" })]
    [InlineData("P8", new[] { "x", "probability_density", "norm" })]
    public void B_group_fixtures_validate_as_single_sut_import_units(string sutId, string[] observables)
    {
        var unit = BGroupPutFixtures.Create(sutId);

        var validation = SutImportValidator.Validate(unit);

        Assert.True(validation.IsValid, string.Join(Environment.NewLine, validation.Errors));
        Assert.Equal(sutId, unit.Sut.SutId);
        Assert.Equal(observables, unit.Sut.Observables.Select(o => o.Name));
        Assert.All(unit.Mrs, mr => Assert.Equal(sutId, mr.SutId));
        Assert.All(unit.IoGroups, io => Assert.Equal(sutId, io.SutId));
        Assert.All(unit.Mutations, mutation => Assert.Equal(sutId, mutation.SutId));
        Assert.All(unit.Detections, detection =>
        {
            Assert.Equal(EvidenceKind.ImportedResearchEvidence, detection.EvidenceKind);
            Assert.Equal(DetectionResult.Inconclusive, detection.Result);
        });
    }

    [Theory]
    [InlineData("P3", new[] { ObservableKind.Series, ObservableKind.Vector, ObservableKind.Vector })]
    [InlineData("P8", new[] { ObservableKind.Vector, ObservableKind.Vector, ObservableKind.Series })]
    public void B_group_observable_shapes_match_external_put_contract(string sutId, ObservableKind[] observableKinds)
    {
        var unit = BGroupPutFixtures.Create(sutId);

        Assert.Equal(observableKinds, unit.Sut.Observables.Select(o => o.Kind));
    }

    [Theory]
    [InlineData("P3", "p3-trajectory-sensitivity", "initial-condition-perturbation")]
    [InlineData("P8", "p8-norm-conservation", "time-step-perturbation")]
    public void B_group_mutations_are_operator_class_only(string sutId, string mrId, string operatorClass)
    {
        var unit = BGroupPutFixtures.Create(sutId);

        Assert.Contains(unit.Mrs, mr => mr.MrId == mrId);
        var mutation = Assert.Single(unit.Mutations);
        Assert.Equal(MutationRepresentationKind.OperatorClassOnly, mutation.RepresentationKind);
        Assert.Equal(operatorClass, mutation.OperatorClass);
    }

    [Theory]
    [InlineData("P3")]
    [InlineData("P8")]
    public void Compatibility_defaults_to_imported_only_for_B_group(string sutId)
    {
        var unit = BGroupPutFixtures.Create(sutId);

        var profile = CompatibilityProfileBuilder.Build(unit);

        Assert.Equal(RuntimeReadiness.ImportedOnly, profile.OverallReadiness);
        Assert.False(profile.CanPromoteToRuntimeCandidate());
        Assert.Contains(profile.Findings, f => f.Contains("transform", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(profile.Findings, f => f.Contains("assertion", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("P3")]
    [InlineData("P8")]
    public void Export_round_trips_B_group_units(string sutId)
    {
        var unit = BGroupPutFixtures.Create(sutId);
        var root = Path.Combine(Path.GetTempPath(), "MetBenchBGroupPutImportRoundTrip", Guid.NewGuid().ToString("N"));

        SutImportPackageExporter.Export(unit, root);
        var imported = SutImportPackageExporter.Import(root);

        Assert.Equal(unit.Sut.SutId, imported.Sut.SutId);
        Assert.Equal(unit.Provenance.Commit, imported.Provenance.Commit);
        Assert.Equal(unit.Sut.Observables.Select(o => o.Name), imported.Sut.Observables.Select(o => o.Name));
        Assert.Equal(unit.Mrs.Select(m => m.MrId), imported.Mrs.Select(m => m.MrId));
        Assert.Equal(unit.Mutations.Select(m => m.MutationId), imported.Mutations.Select(m => m.MutationId));
        Assert.Equal(unit.Detections.Select(d => d.DetectionId), imported.Detections.Select(d => d.DetectionId));
        Assert.All(imported.Detections, detection => Assert.Equal(DetectionResult.Inconclusive, detection.Result));
        Assert.True(SutImportValidator.Validate(imported).IsValid);
    }

    [Theory]
    [InlineData("P3")]
    [InlineData("P8")]
    public void Round_tripped_B_group_units_remain_imported_only(string sutId)
    {
        var unit = BGroupPutFixtures.Create(sutId);
        var root = Path.Combine(Path.GetTempPath(), "MetBenchBGroupPutImportCompatibility", Guid.NewGuid().ToString("N"));

        SutImportPackageExporter.Export(unit, root);
        var imported = SutImportPackageExporter.Import(root);

        var profile = CompatibilityProfileBuilder.Build(imported);

        Assert.Equal(RuntimeReadiness.ImportedOnly, profile.OverallReadiness);
        Assert.False(profile.CanPromoteToRuntimeCandidate());
    }
}
