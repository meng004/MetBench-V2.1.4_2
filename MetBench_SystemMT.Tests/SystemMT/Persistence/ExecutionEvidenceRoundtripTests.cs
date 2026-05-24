using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MetBench_BLL.SystemMT.Persistence;
using MetBench_DAL;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Persistence;

public sealed class ExecutionEvidenceRoundtripTests : IDisposable
{
    private readonly string _tempFile;
    private readonly string _connectionString;

    public ExecutionEvidenceRoundtripTests()
    {
        _tempFile = Path.Combine(Path.GetTempPath(), $"metbench-evidence-{Guid.NewGuid():N}.litedb");
        _connectionString = $"Filename={_tempFile}";
    }

    public void Dispose()
    {
        try { File.Delete(_tempFile); } catch { /* swallow cleanup errors */ }
    }

    private static ExecutionEvidence MakeEvidence(Guid executionId, string mrId = "heat-equation-amplitude") =>
        new()
        {
            IdEvidence = Guid.NewGuid(),
            ExecutionId = executionId,
            Metadata = new ExecutionMetadataSnapshot
            {
                MrId = mrId,
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
    public async Task SaveAsync_then_GetByExecution_returns_full_evidence()
    {
        using var repo = new LiteDbExecutionEvidenceRepository(_connectionString);
        var executionId = Guid.NewGuid();
        var evidence = MakeEvidence(executionId);

        await repo.SaveAsync(evidence);
        var loaded = await repo.GetByExecutionAsync(executionId);

        Assert.NotNull(loaded);
        Assert.Equal(executionId, loaded!.ExecutionId);
        Assert.Equal("heat-equation-amplitude", loaded.Metadata.MrId);
        Assert.Equal("Fourier", loaded.Metadata.Equation);
        Assert.Single(loaded.SampleTraces);
        Assert.Equal("amplitude", loaded.SampleTraces[0].VariableName);
        Assert.Equal("/initial/amplitude", loaded.SampleTraces[0].Path);
        Assert.Equal("2", loaded.TransformationParameters["factor"]);
    }

    [Fact]
    public async Task GetByExecution_returns_null_for_unknown_id()
    {
        using var repo = new LiteDbExecutionEvidenceRepository(_connectionString);
        var loaded = await repo.GetByExecutionAsync(Guid.NewGuid());
        Assert.Null(loaded);
    }

    [Fact]
    public async Task SaveAsync_is_idempotent_via_upsert_keyed_on_IdEvidence()
    {
        using var repo = new LiteDbExecutionEvidenceRepository(_connectionString);
        var executionId = Guid.NewGuid();
        var evidence = MakeEvidence(executionId);

        await repo.SaveAsync(evidence);
        evidence.SampleTraces.Add(new ExecutionSampleTrace
        {
            VariableName = "t_final",
            Path = "/params/t_final",
            SourceValueJson = "1.0",
            TransformedValueJson = "1.0",
            OutputValueJson = "1.0",
        });
        await repo.SaveAsync(evidence);

        var loaded = await repo.GetByExecutionAsync(executionId);
        Assert.NotNull(loaded);
        Assert.Equal(2, loaded!.SampleTraces.Count);
    }

    [Fact]
    public async Task ExecutionId_unique_index_rejects_two_records_for_same_execution()
    {
        using var repo = new LiteDbExecutionEvidenceRepository(_connectionString);
        var executionId = Guid.NewGuid();

        await repo.SaveAsync(MakeEvidence(executionId));
        var secondWithSameExecution = MakeEvidence(executionId, mrId: "different-mr");

        var ex = await Assert.ThrowsAsync<LiteDB.LiteException>(
            () => repo.SaveAsync(secondWithSameExecution));
        Assert.Contains("unique", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SaveAsync_throws_on_cancelled_token()
    {
        using var repo = new LiteDbExecutionEvidenceRepository(_connectionString);
        var cts = new System.Threading.CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => repo.SaveAsync(MakeEvidence(Guid.NewGuid()), cts.Token));
    }

    [Fact]
    public async Task SaveAsync_throws_on_null_evidence()
    {
        using var repo = new LiteDbExecutionEvidenceRepository(_connectionString);
        await Assert.ThrowsAsync<ArgumentNullException>(() => repo.SaveAsync(null!));
    }

    [Fact]
    public async Task Metadata_5d_tags_and_V3_ref_round_trip_unchanged()
    {
        using var repo = new LiteDbExecutionEvidenceRepository(_connectionString);
        var executionId = Guid.NewGuid();
        var v3Id = Guid.NewGuid();
        var evidence = MakeEvidence(executionId);
        evidence.Metadata.V3MrIdRef = v3Id;
        evidence.Metadata.MetaPattern = "Inv";
        evidence.Metadata.SourceLevel = "MetaPrompt";
        evidence.Metadata.FailureCorrelation = "RealBug";

        await repo.SaveAsync(evidence);
        var loaded = await repo.GetByExecutionAsync(executionId);

        Assert.NotNull(loaded);
        Assert.Equal(v3Id, loaded!.Metadata.V3MrIdRef);
        Assert.Equal("Inv", loaded.Metadata.MetaPattern);
        Assert.Equal("MetaPrompt", loaded.Metadata.SourceLevel);
        Assert.Equal("RealBug", loaded.Metadata.FailureCorrelation);
    }
}
