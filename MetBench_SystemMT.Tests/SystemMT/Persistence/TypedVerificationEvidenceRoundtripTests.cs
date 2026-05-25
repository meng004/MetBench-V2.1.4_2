using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MetBench_BLL.SystemMT.Persistence;
using MetBench_DAL;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Persistence;

/// <summary>
/// ExecutionEvidence v2 round-trip contract. Locks the additive
/// <see cref="ExecutionEvidence.TypedVerification"/> block: legacy v1 rows must
/// continue to round-trip with <c>TypedVerification == null</c>, and new v2
/// rows must preserve typed status, diagnostic, and per-predicate property
/// fields exactly as written.
/// </summary>
public sealed class TypedVerificationEvidenceRoundtripTests : IDisposable
{
    private readonly string _tempFile;
    private readonly string _connectionString;

    public TypedVerificationEvidenceRoundtripTests()
    {
        _tempFile = Path.Combine(Path.GetTempPath(), $"metbench-evidence-v2-{Guid.NewGuid():N}.litedb");
        _connectionString = $"Filename={_tempFile}";
    }

    public void Dispose()
    {
        try { File.Delete(_tempFile); } catch { /* swallow cleanup errors */ }
    }

    private static ExecutionEvidence MakeBase(Guid executionId) => new()
    {
        IdEvidence = Guid.NewGuid(),
        ExecutionId = executionId,
        Metadata = new ExecutionMetadataSnapshot
        {
            MrId = "heat-equation-amplitude",
            V3MrIdRef = Guid.NewGuid(),
            SutName = "heat-equation",
            Equation = "Fourier",
            ProgramType = "Num",
            MetaPattern = "Mono",
            SourceLevel = "Manual",
            FailureCorrelation = "None",
            CatalogManifestPath = "SUT/heat_equation/catalog.json",
            MetbenchVersion = "v2.2-dev",
        },
        SampleTraces =
        {
            new ExecutionSampleTrace
            {
                VariableName = "amplitude",
                Path = "/initial/amplitude",
                SourceValueJson = "1.0",
                TransformedValueJson = "2.0",
                OutputValueJson = "2.0",
            },
        },
        TransformationParameters = new Dictionary<string, string> { ["factor"] = "2" },
    };

    [Fact]
    public async Task TypedVerification_round_trips_for_MrSpec_with_diagnostic()
    {
        using var repo = new LiteDbExecutionEvidenceRepository(_connectionString);
        var executionId = Guid.NewGuid();
        var evidence = MakeBase(executionId);
        evidence.TypedVerification = new TypedVerificationEvidence
        {
            SpecId = "heat-equation-amplitude",
            SpecKind = "MrSpec",
            PredicateId = "amplitude-equal",
            PredicateKind = "BinaryComparison",
            Status = "Passed",
            Passed = true,
            Diagnostic = new TypedDiagnosticEvidence
            {
                Expected = 2.0,
                Actual = 2.0,
                Residual = 0.0,
                Tolerance = 1e-9,
            },
            SkipOrInvalidReason = null,
            PropertyPredicates = new List<TypedPropertyPredicateEvidence>(),
        };

        await repo.SaveAsync(evidence);
        var loaded = await repo.GetByExecutionAsync(executionId);

        Assert.NotNull(loaded);
        Assert.NotNull(loaded!.TypedVerification);
        var t = loaded.TypedVerification!;
        Assert.Equal("heat-equation-amplitude", t.SpecId);
        Assert.Equal("MrSpec", t.SpecKind);
        Assert.Equal("amplitude-equal", t.PredicateId);
        Assert.Equal("BinaryComparison", t.PredicateKind);
        Assert.Equal("Passed", t.Status);
        Assert.True(t.Passed);
        Assert.NotNull(t.Diagnostic);
        Assert.Equal(2.0, t.Diagnostic!.Expected);
        Assert.Equal(2.0, t.Diagnostic.Actual);
        Assert.Equal(0.0, t.Diagnostic.Residual);
        Assert.Equal(1e-9, t.Diagnostic.Tolerance);
        Assert.Null(t.SkipOrInvalidReason);
        Assert.Empty(t.PropertyPredicates);
    }

    [Fact]
    public async Task TypedVerification_round_trips_for_PropertySpec_with_predicate_results()
    {
        using var repo = new LiteDbExecutionEvidenceRepository(_connectionString);
        var executionId = Guid.NewGuid();
        var evidence = MakeBase(executionId);
        evidence.TypedVerification = new TypedVerificationEvidence
        {
            SpecId = "neutron-flux-positivity",
            SpecKind = "PropertySpec",
            PredicateId = string.Empty,
            PredicateKind = string.Empty,
            Status = "Held",
            Passed = true,
            Diagnostic = null,
            SkipOrInvalidReason = null,
            PropertyPredicates = new List<TypedPropertyPredicateEvidence>
            {
                new()
                {
                    PredicateId = "phi-nonneg",
                    PredicateKind = "Bound",
                    Status = "Held",
                    Residual = 0.0,
                    Tolerance = 1e-12,
                    Reason = null,
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
                    Reason = "Decrease detected at index 4",
                    ExpectedJson = "\"NonIncreasing\"",
                    ActualJson = "[1.0,0.9,0.7,0.8]",
                },
            },
        };

        await repo.SaveAsync(evidence);
        var loaded = await repo.GetByExecutionAsync(executionId);

        Assert.NotNull(loaded);
        var t = loaded!.TypedVerification!;
        Assert.Equal("PropertySpec", t.SpecKind);
        Assert.Equal("Held", t.Status);
        Assert.Equal(2, t.PropertyPredicates.Count);

        var first = t.PropertyPredicates[0];
        Assert.Equal("phi-nonneg", first.PredicateId);
        Assert.Equal("Bound", first.PredicateKind);
        Assert.Equal("Held", first.Status);
        Assert.Equal(0.0, first.Residual);
        Assert.Equal(1e-12, first.Tolerance);
        Assert.Equal("{\"lower\":0}", first.ExpectedJson);
        Assert.Equal("0.5", first.ActualJson);

        var second = t.PropertyPredicates[1];
        Assert.Equal("phi-shape-monotone", second.PredicateId);
        Assert.Equal("Violated", second.Status);
        Assert.Equal(0.03, second.Residual);
        Assert.Equal("Decrease detected at index 4", second.Reason);
    }

    [Fact]
    public async Task Legacy_row_without_typed_verification_loads_with_null_typed_verification()
    {
        using var repo = new LiteDbExecutionEvidenceRepository(_connectionString);
        var executionId = Guid.NewGuid();
        var evidence = MakeBase(executionId);
        // Intentionally leave TypedVerification null — this is the v1 shape.

        await repo.SaveAsync(evidence);
        var loaded = await repo.GetByExecutionAsync(executionId);

        Assert.NotNull(loaded);
        Assert.Null(loaded!.TypedVerification);
        // v1 fields still round-trip:
        Assert.Single(loaded.SampleTraces);
        Assert.Equal("amplitude", loaded.SampleTraces[0].VariableName);
        Assert.Equal("2", loaded.TransformationParameters["factor"]);
    }
}
