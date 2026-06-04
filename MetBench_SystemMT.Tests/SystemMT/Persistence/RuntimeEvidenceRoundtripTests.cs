using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MetBench_BLL.SystemMT.Persistence;
using MetBench_BLL.SystemMT.Runtime;
using MetBench_DAL;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Persistence;

public sealed class RuntimeEvidenceRoundtripTests : IDisposable
{
    private readonly string _tempFile;
    private readonly string _connectionString;

    public RuntimeEvidenceRoundtripTests()
    {
        _tempFile = Path.Combine(Path.GetTempPath(), $"metbench-runtime-evidence-{Guid.NewGuid():N}.litedb");
        _connectionString = $"Filename={_tempFile}";
    }

    public void Dispose()
    {
        try { File.Delete(_tempFile); } catch { }
    }

    [Fact]
    public async Task RuntimeEvidence_round_trips_when_present()
    {
        using var repo = new LiteDbExecutionEvidenceRepository(_connectionString);
        var executionId = Guid.NewGuid();
        var evidence = MakeEvidence(executionId);
        evidence.RuntimeEvidence = new RuntimeEvidence
        {
            RuntimeKey = "system",
            RuntimeProfileDisplayName = "System Python",
            RuntimeKind = RuntimeKind.LocalPython.ToString(),
            ResolvedExecutablePath = "python3",
            Passed = true,
            FailureKind = RuntimeFailureKind.None.ToString(),
            FailureDetail = string.Empty,
            Diagnostics =
            {
                new RuntimeCheckEvidence
                {
                    CheckKind = "startup",
                    Name = "System Python",
                    Passed = true,
                    FailureKind = RuntimeFailureKind.None.ToString(),
                    Detail = "startup passed",
                    ExitCode = 0,
                    TimedOut = false,
                    Stdout = "Python 3",
                    Stderr = string.Empty,
                },
            },
        };

        await repo.SaveAsync(evidence);
        var loaded = await repo.GetByExecutionAsync(executionId);

        Assert.NotNull(loaded);
        Assert.NotNull(loaded!.RuntimeEvidence);
        Assert.Equal("system", loaded.RuntimeEvidence!.RuntimeKey);
        Assert.Equal("System Python", loaded.RuntimeEvidence.RuntimeProfileDisplayName);
        Assert.Equal(RuntimeKind.LocalPython.ToString(), loaded.RuntimeEvidence.RuntimeKind);
        Assert.Equal("python3", loaded.RuntimeEvidence.ResolvedExecutablePath);
        Assert.True(loaded.RuntimeEvidence.Passed);
        Assert.Equal(RuntimeFailureKind.None.ToString(), loaded.RuntimeEvidence.FailureKind);
        var diagnostic = Assert.Single(loaded.RuntimeEvidence.Diagnostics);
        Assert.Equal("startup", diagnostic.CheckKind);
        Assert.True(diagnostic.Passed);
        Assert.Equal(0, diagnostic.ExitCode);
        Assert.Equal("Python 3", diagnostic.Stdout);
    }

    [Fact]
    public async Task Legacy_row_without_runtime_evidence_loads_with_null_runtime_evidence()
    {
        using var repo = new LiteDbExecutionEvidenceRepository(_connectionString);
        var executionId = Guid.NewGuid();
        var evidence = MakeEvidence(executionId);

        await repo.SaveAsync(evidence);
        var loaded = await repo.GetByExecutionAsync(executionId);

        Assert.NotNull(loaded);
        Assert.Null(loaded!.RuntimeEvidence);
        Assert.Equal("heat-equation-amplitude", loaded.Metadata.MrId);
    }

    private static ExecutionEvidence MakeEvidence(Guid executionId) => new()
    {
        IdEvidence = Guid.NewGuid(),
        ExecutionId = executionId,
        Metadata = new ExecutionMetadataSnapshot
        {
            MrId = "heat-equation-amplitude",
            SutName = "heat-equation",
            Equation = "Fourier",
            ProgramType = "Num",
            MetaPattern = "Mono",
            SourceLevel = "Manual",
            FailureCorrelation = "None",
            MetbenchVersion = "v2.2-dev",
        },
        SampleTraces =
        {
            new ExecutionSampleTrace
            {
                VariableName = "max_u",
                Path = "/initial/amplitude",
                SourceValueJson = "1.0",
                TransformedValueJson = "2.0",
                OutputValueJson = "2.0",
            },
        },
        TransformationParameters = new Dictionary<string, string> { ["factor"] = "2" },
    };
}
