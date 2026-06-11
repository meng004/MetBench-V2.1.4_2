using MetBench_BLL.Core.SystemMT.ImportExport.Put;
using MetBench_SystemMT.Tests.SystemMT;
using System.Text.Json;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.ImportExport;

public sealed class ExternalMrAcceptanceBatchImportTests
{
    [Fact]
    public void Batch_A_toy_classic_package_preserves_all_classic_mrs()
    {
        var unit = ExternalMrAcceptancePutFixtures.CreateBatchAToyClassic();

        var validation = SutImportValidator.Validate(unit);

        Assert.True(validation.IsValid, string.Join(Environment.NewLine, validation.Errors));
        Assert.Equal("minimum-mr-subset-toy-classic", unit.Sut.SutId);
        Assert.Equal(7, unit.Mrs.Count);
        Assert.Equal(3, unit.IoGroups.Count);
        Assert.Equal(new[] { "sort_MR1_permute", "sort_MR2_concat_dup", "sort_MR3_remove_max" },
            unit.Mrs.Where(m => m.Metadata["program"] == "sorting").Select(m => m.MrId));
        Assert.Equal(new[] { "matmul_MR1_transpose", "matmul_MR2_scalar" },
            unit.Mrs.Where(m => m.Metadata["program"] == "matmul").Select(m => m.MrId));
        Assert.Equal(new[] { "quad_MR1_swap_roots", "quad_MR2_scale" },
            unit.Mrs.Where(m => m.Metadata["program"] == "quadratic").Select(m => m.MrId));
        Assert.All(unit.Mrs, mr => Assert.Equal(CompatibilityStatus.ImportedOnly, mr.TransformBinding.Status));
    }

    [Fact]
    public void Batch_A_p1_heat_package_captures_ten_mrs_five_mutation_classes_and_full_detection_matrix()
    {
        var unit = ExternalMrAcceptancePutFixtures.CreateBatchAP1Heat();

        var validation = SutImportValidator.Validate(unit);
        var profile = CompatibilityProfileBuilder.Build(unit);

        Assert.True(validation.IsValid, string.Join(Environment.NewLine, validation.Errors));
        Assert.Equal("minimum-mr-subset-p1-heat", unit.Sut.SutId);
        Assert.Equal("8944822054e18a1d80ec8c90f56a1214a8fd1665", unit.Provenance.Commit);
        Assert.Equal(10, unit.Mrs.Count);
        Assert.Equal(new[] { "mut_C", "mut_F", "mut_G", "mut_M", "mut_T" }, unit.Mutations.Select(m => m.OperatorClass).Order());
        Assert.Equal(50, unit.Detections.Count);
        Assert.Equal(10, unit.Detections.Count(d => d.Result == DetectionResult.Detected));
        Assert.Equal(40, unit.Detections.Count(d => d.Result == DetectionResult.Survived));
        Assert.Equal(RuntimeReadiness.ImportedOnly, profile.OverallReadiness);
        Assert.All(unit.Mrs, mr => Assert.Equal(CompatibilityStatus.RequiresAdapter, mr.TransformBinding.Status));
        Assert.Contains(profile.Findings, f => f.Contains("transform", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Batch_D_sciml_domain_validity_package_preserves_mr_cards_and_seeded_fault_metrics()
    {
        var unit = ExternalMrAcceptancePutFixtures.CreateBatchDScimlDomainValidity();

        var validation = SutImportValidator.Validate(unit);

        Assert.True(validation.IsValid, string.Join(Environment.NewLine, validation.Errors));
        Assert.Equal("sciml-domain-validity-mgn", unit.Sut.SutId);
        Assert.Equal(ProgramKind.Surrogate, unit.Sut.ProgramKind);
        Assert.Equal("c2956e8eb42b81b216a4cf31720c98e1e035f2e8", unit.Provenance.Commit);
        Assert.Equal(3, unit.Mrs.Count);
        Assert.Equal(10, unit.Mutations.Count);
        Assert.Equal(30, unit.Detections.Count);
        Assert.Equal(5, unit.Detections.Count(d => d.Result == DetectionResult.Detected));
        Assert.Contains(unit.Mrs, m => m.MrId == "mgn-node-permutation-equivariance"
            && m.AssertionBinding.Metric == "permutation_relative_l2_error"
            && m.AssertionBinding.Tolerance == ToleranceSpec.Relative(1e-6));
    }

    [Fact]
    public void Batch_D_discrete_divergence_remains_deferred_imported_only()
    {
        var unit = ExternalMrAcceptancePutFixtures.CreateBatchDScimlDomainValidity();

        var mr = Assert.Single(unit.Mrs, m => m.MrId == "mgn-discrete-divergence-boundedness");
        var profile = CompatibilityProfileBuilder.Build(unit);

        Assert.Equal(CompatibilityStatus.ImportedOnly, mr.TransformBinding.Status);
        Assert.Equal(CompatibilityStatus.ImportedOnly, mr.AssertionBinding.Status);
        Assert.Equal("design-time-deferred", mr.Metadata["status"]);
        Assert.Contains("skip", mr.Metadata["allowed_verdicts"], StringComparison.OrdinalIgnoreCase);
        Assert.Equal(RuntimeReadiness.ImportedOnly, profile.OverallReadiness);
    }

    [Fact]
    public void Batch_B_existing_runtime_reconcile_package_preserves_stable_runtime_sut_ids()
    {
        var unit = ExternalMrAcceptancePutFixtures.CreateBatchBExistingRuntimeReconcile();

        var validation = SutImportValidator.Validate(unit);
        var profile = CompatibilityProfileBuilder.Build(unit);

        Assert.True(validation.IsValid, string.Join(Environment.NewLine, validation.Errors));
        Assert.Equal("minimum-mr-subset-existing-runtime-reconcile", unit.Sut.SutId);
        Assert.Equal("metbench-import-minmr-existing-runtime-reconcile-v1", unit.Sut.Metadata["package_id"]);
        Assert.Equal(new[]
        {
            "minimum-mr-subset-p3",
            "minimum-mr-subset-p4",
            "minimum-mr-subset-p5",
            "minimum-mr-subset-p8",
            "minimum-mr-subset-p9-surrogate"
        }, unit.Mrs.Select(m => m.Metadata["existing_sut_id"]).Order(StringComparer.Ordinal));
        Assert.Contains(unit.Mrs, m => m.MrId == "p8-norm-conservation"
            && m.Metadata["compatibility_risk"].Contains("np.trapz", StringComparison.Ordinal));
        Assert.Equal(RuntimeReadiness.RuntimeCandidate, profile.OverallReadiness);
        Assert.All(unit.Detections, d => Assert.Equal(EvidenceKind.ImportedResearchEvidence, d.EvidenceKind));
    }

    [Fact]
    public void Batch_B_existing_runtime_reconcile_bindings_match_existing_catalog_assets()
    {
        var unit = ExternalMrAcceptancePutFixtures.CreateBatchBExistingRuntimeReconcile();

        foreach (var mr in unit.Mrs)
        {
            var io = Assert.Single(unit.IoGroups, g => g.Metadata["existing_sut_id"] == mr.Metadata["existing_sut_id"]);
            var sutDir = io.Metadata["existing_sut_directory"];
            var catalogPath = Path.Combine(TestAssetPaths.AssetRoot(), sutDir, "catalog.json");
            var sourcePath = Assert.Single(io.InputRefs);
            var sourceUnderAssets = sourcePath.StartsWith("SUT/", StringComparison.Ordinal)
                ? sourcePath["SUT/".Length..]
                : sourcePath;

            Assert.True(File.Exists(catalogPath), $"Missing existing catalog for {mr.MrId}: {catalogPath}");
            Assert.True(
                File.Exists(Path.Combine(TestAssetPaths.AssetRoot(), sourceUnderAssets.Replace('/', Path.DirectorySeparatorChar))),
                $"Missing existing sample for {mr.MrId}: {sourcePath}");

            using var doc = JsonDocument.Parse(File.ReadAllText(catalogPath));
            var existingMr = doc.RootElement
                .GetProperty("mrs")
                .EnumerateArray()
                .Single(e => e.GetProperty("mr_id").GetString() == mr.MrId);

            Assert.Equal(existingMr.GetProperty("value_name").GetString(), mr.AssertionBinding.Metric);
            Assert.Equal(existingMr.GetProperty("assertion_type_code").GetString(), mr.AssertionBinding.PredicateKind);
            Assert.Equal(existingMr.GetProperty("transformation_name").GetString(), mr.TransformBinding.TransformKind);
        }
    }

    [Fact]
    public void Batch_C_local_remaining_package_captures_four_puts_with_runtime_candidate_bindings()
    {
        var unit = ExternalMrAcceptancePutFixtures.CreateBatchCLocalRemaining();

        var validation = SutImportValidator.Validate(unit);
        var profile = CompatibilityProfileBuilder.Build(unit);

        Assert.True(validation.IsValid, string.Join(Environment.NewLine, validation.Errors));
        Assert.Equal("minimum-mr-subset-local-remaining", unit.Sut.SutId);
        Assert.Equal("metbench-import-minmr-local-remaining-v1", unit.Sut.Metadata["package_id"]);
        Assert.Equal(new[] { "P10", "P2", "P6", "P7" }, unit.Mrs.Select(m => m.Metadata["external_family"]).Order(StringComparer.Ordinal));
        Assert.All(unit.Mrs, m => Assert.Equal(CompatibilityStatus.MappedSupported, m.TransformBinding.Status));
        Assert.All(unit.Mrs, m => Assert.Equal(CompatibilityStatus.MappedSupported, m.AssertionBinding.Status));
        Assert.All(unit.IoGroups, g => Assert.True(g.Metadata.ContainsKey("sample_case_relative_path")));
        Assert.Equal(RuntimeReadiness.RuntimeCandidate, profile.OverallReadiness);
    }

    [Theory]
    [InlineData("toy")]
    [InlineData("p1")]
    [InlineData("sciml")]
    [InlineData("batch-b")]
    [InlineData("batch-c")]
    public void Batch_external_acceptance_packages_export_and_import_without_losing_evidence(string package)
    {
        var unit = package switch
        {
            "toy" => ExternalMrAcceptancePutFixtures.CreateBatchAToyClassic(),
            "p1" => ExternalMrAcceptancePutFixtures.CreateBatchAP1Heat(),
            "sciml" => ExternalMrAcceptancePutFixtures.CreateBatchDScimlDomainValidity(),
            "batch-b" => ExternalMrAcceptancePutFixtures.CreateBatchBExistingRuntimeReconcile(),
            "batch-c" => ExternalMrAcceptancePutFixtures.CreateBatchCLocalRemaining(),
            _ => throw new ArgumentOutOfRangeException(nameof(package), package, null)
        };
        var root = Path.Combine(Path.GetTempPath(), "MetBenchExternalMrBatchRoundTrip", Guid.NewGuid().ToString("N"));

        SutImportPackageExporter.Export(unit, root);
        var imported = SutImportPackageExporter.Import(root);

        Assert.Equal(unit.Sut.SutId, imported.Sut.SutId);
        Assert.Equal(unit.Provenance.Commit, imported.Provenance.Commit);
        Assert.Equal(unit.Mrs.Select(m => m.MrId), imported.Mrs.Select(m => m.MrId));
        Assert.Equal(unit.Mutations.Select(m => m.MutationId), imported.Mutations.Select(m => m.MutationId));
        Assert.Equal(unit.Detections.Select(d => d.DetectionId), imported.Detections.Select(d => d.DetectionId));
        Assert.Equal(unit.Detections.Select(d => d.Result), imported.Detections.Select(d => d.Result));
        Assert.True(SutImportValidator.Validate(imported).IsValid);
    }
}
