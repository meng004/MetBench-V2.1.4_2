using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using MetBench_BLL.SystemMT.Assertions;
using MetBench_BLL.SystemMT.Persistence;
using MetBench_BLL.SystemMT.Pipeline;
using MetBench_Domain.V2;
using MetBench_Domain.V2.Enums;
using MetBench_IDAL;
using MetBench_SystemMT.Tests.V2Pipeline;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Pipeline;

/// <summary>
/// Task 6 step 2 acceptance: when SystemMtExecutionRecorder is constructed with an
/// IExecutionEvidenceRepository + IMetamorphicRelationV3Repository, a successful
/// pipeline outcome writes an ExecutionEvidence row alongside the existing Execution
/// + Result rows, with V3MrIdRef and 5D tags resolved from the V3 repo.
/// </summary>
public sealed class ExecutionEvidenceWriteThroughTests
{
    private static PipelineContext CtxFor(string mrCode, string sutName = "heat-equation") =>
        new(
            MrCode: mrCode,
            TransformationName: "ScaleField",
            AssertionTypeCode: "greater",
            ValueName: "max_u",
            TargetFieldPath: "/initial/amplitude",
            PathSyntax: "json-pointer",
            Parameters: new Dictionary<string, string> { ["factor"] = "2" },
            Tolerance: new AssertionTolerance(),
            ExtraAssertionValues: null,
            SutName: sutName,
            SourceCasePath: "/tmp/case.json",
            WorkingDirectory: "/tmp",
            InputParserCommand: "",
            OutputParserCommand: "",
            RunnerCommand: "",
            TimeoutSeconds: 30,
            CatalogVersionSha: "abc123",
            SutVersionSnapshot: "v1",
            MetbenchVersion: "v2.2-dev",
            TriggeredBy: "test");

    private static PipelineOutcome OkOutcome() => new(
        FinalStatus: "ok",
        ErrorMessage: null,
        StartedAt: DateTime.UtcNow,
        FinishedAt: DateTime.UtcNow,
        ArtifactsDirectory: "/tmp",
        SourceInputPath: "",
        FollowupInputPath: "",
        SourceOutputPath: "",
        FollowupOutputPath: "",
        SourceMetrics: null,
        FollowupMetrics: null,
        AssertionResult: new SystemMtAssertionResultV2(
            AssertionTypeCode: "greater",
            Passed: true,
            SourceValue: 1.0,
            FollowupValue: 2.0,
            ObservedDelta: 1.0,
            ExpectedThreshold: 0.0,
            Expression: "follow > src",
            FailureReason: null),
        SourceElapsed: TimeSpan.FromSeconds(1),
        FollowupElapsed: TimeSpan.FromSeconds(1),
        SourceExitCode: 0,
        FollowupExitCode: 0);

    [Fact]
    public async Task Record_writes_evidence_when_evidence_and_V3_repos_are_injected()
    {
        var execRepo = new FakeExecRepo();
        var resRepo = new FakeResultRepo();
        var evRepo = new InMemoryEvidenceRepo();
        var v3Id = Guid.NewGuid();
        var v3Repo = new InMemoryV3Repo();
        v3Repo.AddV3(new MetamorphicRelationV3
        {
            IdV3 = v3Id,
            MrCode = "heat-equation-amplitude",
            Equation = EquationKind.Fourier,
            ProgramType = ProgramKind.Num,
            MetaPattern = MetaPatternKind.Mono,
            SourceLevel = SourceLevelKind.Manual,
            FailureCorrelation = FailureCorrelationKind.None,
        });

        var recorder = new SystemMtExecutionRecorder(execRepo, resRepo, evRepo, v3Repo);
        var recorded = recorder.Record(CtxFor("heat-equation-amplitude"), OkOutcome(), mrInstanceId: 1);

        var evidence = await evRepo.GetByExecutionAsync(recorded.ExecutionId);
        Assert.NotNull(evidence);
        Assert.Equal(v3Id, evidence!.Metadata.V3MrIdRef);
        Assert.Equal("heat-equation-amplitude", evidence.Metadata.MrId);
        Assert.Equal("heat-equation", evidence.Metadata.SutName);
        Assert.Equal("Fourier", evidence.Metadata.Equation);
        Assert.Equal("Num", evidence.Metadata.ProgramType);
        Assert.Equal("Mono", evidence.Metadata.MetaPattern);
        Assert.Equal("Manual", evidence.Metadata.SourceLevel);
        Assert.Equal("None", evidence.Metadata.FailureCorrelation);
        Assert.Equal("v2.2-dev", evidence.Metadata.MetbenchVersion);
        Assert.Equal("2", evidence.TransformationParameters["factor"]);
    }

    [Fact]
    public async Task Record_writes_evidence_with_empty_V3_ref_when_V3_lookup_misses()
    {
        var execRepo = new FakeExecRepo();
        var resRepo = new FakeResultRepo();
        var evRepo = new InMemoryEvidenceRepo();
        var v3Repo = new InMemoryV3Repo(); // empty

        var recorder = new SystemMtExecutionRecorder(execRepo, resRepo, evRepo, v3Repo);
        var recorded = recorder.Record(CtxFor("unknown-mr"), OkOutcome(), mrInstanceId: 1);

        var evidence = await evRepo.GetByExecutionAsync(recorded.ExecutionId);
        Assert.NotNull(evidence);
        Assert.Equal(Guid.Empty, evidence!.Metadata.V3MrIdRef);
        Assert.Equal("unknown-mr", evidence.Metadata.MrId);
        Assert.Equal(string.Empty, evidence.Metadata.Equation);
    }

    [Fact]
    public void Record_without_evidence_repo_preserves_pre_Task6_behavior()
    {
        // Backward-compat: existing 9 ctor sites in tests do not pass evidence/V3 repos;
        // Recorder must still write Execution + Result and skip evidence cleanly.
        var execRepo = new FakeExecRepo();
        var resRepo = new FakeResultRepo();
        var recorder = new SystemMtExecutionRecorder(execRepo, resRepo);

        var recorded = recorder.Record(CtxFor("heat-equation-amplitude"), OkOutcome(), mrInstanceId: 1);

        Assert.NotEqual(Guid.Empty, recorded.ExecutionId);
        Assert.NotNull(recorded.ResultId);
        Assert.Single(execRepo.Data);
        Assert.Single(resRepo.Data);
    }

    [Fact]
    public async Task Record_does_not_write_evidence_when_outcome_has_no_AssertionResult()
    {
        // For error/timeout/cancelled outcomes (AssertionResult==null) the existing path
        // skips Result; evidence write also skips so the failure scope stays consistent.
        var execRepo = new FakeExecRepo();
        var resRepo = new FakeResultRepo();
        var evRepo = new InMemoryEvidenceRepo();
        var v3Repo = new InMemoryV3Repo();

        var recorder = new SystemMtExecutionRecorder(execRepo, resRepo, evRepo, v3Repo);
        var errored = OkOutcome() with { FinalStatus = "error", AssertionResult = null };
        var recorded = recorder.Record(CtxFor("heat-equation-amplitude"), errored, mrInstanceId: 1);

        Assert.Null(recorded.ResultId);
        var evidence = await evRepo.GetByExecutionAsync(recorded.ExecutionId);
        Assert.Null(evidence);
    }

    [Fact]
    public async Task Record_evidence_ExecutionId_matches_Execution_row()
    {
        var execRepo = new FakeExecRepo();
        var resRepo = new FakeResultRepo();
        var evRepo = new InMemoryEvidenceRepo();
        var v3Repo = new InMemoryV3Repo();

        var recorder = new SystemMtExecutionRecorder(execRepo, resRepo, evRepo, v3Repo);
        var recorded = recorder.Record(CtxFor("heat-equation-amplitude"), OkOutcome(), mrInstanceId: 1);

        var evidence = await evRepo.GetByExecutionAsync(recorded.ExecutionId);
        Assert.NotNull(evidence);
        Assert.Equal(recorded.ExecutionId, evidence!.ExecutionId);
        Assert.Equal(execRepo.Data[0].IdExecution, evidence.ExecutionId);
    }

    [Fact]
    public async Task Record_evidence_writes_sample_trace_for_target_field()
    {
        var execRepo = new FakeExecRepo();
        var resRepo = new FakeResultRepo();
        var evRepo = new InMemoryEvidenceRepo();
        var v3Repo = new InMemoryV3Repo();
        var tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "metbench-evidence-tests", Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(tempDir);
        var sourcePath = System.IO.Path.Combine(tempDir, "source.json");
        var followupPath = System.IO.Path.Combine(tempDir, "followup.json");
        await System.IO.File.WriteAllTextAsync(sourcePath, """{"initial":{"amplitude":1.0}}""");
        await System.IO.File.WriteAllTextAsync(followupPath, """{"initial":{"amplitude":2.0}}""");

        try
        {
            var recorder = new SystemMtExecutionRecorder(execRepo, resRepo, evRepo, v3Repo);
            var recorded = recorder.Record(
                CtxFor("heat-equation-amplitude") with { SourceCasePath = sourcePath },
                OkOutcome() with
                {
                    FollowupInputPath = followupPath,
                    FollowupMetrics = new Dictionary<string, double> { ["max_u"] = 3.5 },
                },
                mrInstanceId: 1);

            var evidence = await evRepo.GetByExecutionAsync(recorded.ExecutionId);
            Assert.NotNull(evidence);
            var trace = Assert.Single(evidence!.SampleTraces);
            Assert.Equal("max_u", trace.VariableName);
            Assert.Equal("/initial/amplitude", trace.Path);
            Assert.Equal("1.0", trace.SourceValueJson);
            Assert.Equal("2.0", trace.TransformedValueJson);
            Assert.Equal("3.5", trace.OutputValueJson);
        }
        finally
        {
            try { System.IO.Directory.Delete(tempDir, recursive: true); } catch { }
        }
    }

    private sealed class InMemoryEvidenceRepo : IExecutionEvidenceRepository
    {
        private readonly List<ExecutionEvidence> _store = new();

        public Task SaveAsync(ExecutionEvidence evidence, System.Threading.CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _store.RemoveAll(e => e.ExecutionId == evidence.ExecutionId);
            _store.Add(evidence);
            return Task.CompletedTask;
        }

        public Task<ExecutionEvidence?> GetByExecutionAsync(Guid executionId, System.Threading.CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<ExecutionEvidence?>(_store.FirstOrDefault(e => e.ExecutionId == executionId));
        }
    }

    private sealed class InMemoryV3Repo : IMetamorphicRelationV3Repository
    {
        private readonly Dictionary<string, MetamorphicRelationV3> _byCode = new(StringComparer.Ordinal);

        public void AddV3(MetamorphicRelationV3 v3) => _byCode[v3.MrCode] = v3;

        public MetamorphicRelationV3? GetByCode(string mrCode) =>
            _byCode.TryGetValue(mrCode, out var v) ? v : null;

        public ObservableCollection<MetamorphicRelationV3> GetByEquation(EquationKind equation) =>
            new(_byCode.Values.Where(v => v.Equation == equation));
        public ObservableCollection<MetamorphicRelationV3> GetByMetaPattern(MetaPatternKind pattern) =>
            new(_byCode.Values.Where(v => v.MetaPattern == pattern));
        public ObservableCollection<MetamorphicRelationV3> GetByEquationAndPattern(EquationKind equation, MetaPatternKind pattern) =>
            new(_byCode.Values.Where(v => v.Equation == equation && v.MetaPattern == pattern));

        // IGuidRepository<MetamorphicRelationV3>
        public MetamorphicRelationV3? Get(Guid id) => _byCode.Values.FirstOrDefault(v => v.IdV3 == id);
        public ObservableCollection<MetamorphicRelationV3> Get(MetamorphicRelationV3 template) => new(_byCode.Values.Where(v => v.MrCode == template.MrCode));
        public ObservableCollection<MetamorphicRelationV3> GetAll() => new(_byCode.Values);
        public bool Add(MetamorphicRelationV3 entity)
        {
            if (entity.IdV3 == Guid.Empty) entity.IdV3 = Guid.NewGuid();
            _byCode[entity.MrCode] = entity;
            return true;
        }
        public bool Modify(MetamorphicRelationV3 entity) { _byCode[entity.MrCode] = entity; return true; }
        public bool Remove(MetamorphicRelationV3 entity) => _byCode.Remove(entity.MrCode);
        public ObservableCollection<MetamorphicRelationV3> GetPage(int pageIndex, int pageSize) =>
            new(_byCode.Values.Skip(pageIndex * pageSize).Take(pageSize));
        public int Count() => _byCode.Count;
    }
}
