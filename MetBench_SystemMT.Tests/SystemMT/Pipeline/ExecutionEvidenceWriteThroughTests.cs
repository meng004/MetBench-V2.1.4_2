using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using MetBench_BLL.SystemMT.Assertions;
using MetBench_BLL.SystemMT.Persistence;
using MetBench_BLL.SystemMT.Pipeline;
using MetBench_BLL.SystemMT.Runtime;
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
        var recorded = await recorder.RecordAsync(CtxFor("heat-equation-amplitude"), OkOutcome(), mrInstanceId: 1);

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
        var recorded = await recorder.RecordAsync(CtxFor("unknown-mr"), OkOutcome(), mrInstanceId: 1);

        var evidence = await evRepo.GetByExecutionAsync(recorded.ExecutionId);
        Assert.NotNull(evidence);
        Assert.Equal(Guid.Empty, evidence!.Metadata.V3MrIdRef);
        Assert.Equal("unknown-mr", evidence.Metadata.MrId);
        Assert.Equal(string.Empty, evidence.Metadata.Equation);
    }

    [Fact]
    public async Task Record_without_evidence_repo_preserves_pre_Task6_behavior()
    {
        // Backward-compat: existing 9 ctor sites in tests do not pass evidence/V3 repos;
        // Recorder must still write Execution + Result and skip evidence cleanly.
        var execRepo = new FakeExecRepo();
        var resRepo = new FakeResultRepo();
        var recorder = new SystemMtExecutionRecorder(execRepo, resRepo);

        var recorded = await recorder.RecordAsync(CtxFor("heat-equation-amplitude"), OkOutcome(), mrInstanceId: 1);

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
        var recorded = await recorder.RecordAsync(CtxFor("heat-equation-amplitude"), errored, mrInstanceId: 1);

        Assert.Null(recorded.ResultId);
        var evidence = await evRepo.GetByExecutionAsync(recorded.ExecutionId);
        Assert.Null(evidence);
    }

    [Fact]
    public async Task Record_writes_runtime_evidence_when_outcome_has_no_AssertionResult()
    {
        var execRepo = new FakeExecRepo();
        var resRepo = new FakeResultRepo();
        var evRepo = new InMemoryEvidenceRepo();
        var v3Repo = new InMemoryV3Repo();

        var runtimeEvidence = new RuntimeEvidence
        {
            RuntimeKey = "system",
            RuntimeProfileDisplayName = "System Python",
            RuntimeKind = RuntimeKind.LocalPython.ToString(),
            ResolvedExecutablePath = "python",
            Passed = true,
            FailureKind = RuntimeFailureKind.None.ToString(),
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
                },
            },
        };

        var recorder = new SystemMtExecutionRecorder(execRepo, resRepo, evRepo, v3Repo);
        var errored = OkOutcome() with { FinalStatus = "error", AssertionResult = null };
        var recorded = await recorder.RecordAsync(
            CtxFor("heat-equation-amplitude"),
            errored,
            mrInstanceId: 1,
            runtimeEvidence: runtimeEvidence);

        Assert.Null(recorded.ResultId);
        Assert.Empty(resRepo.Data);
        var evidence = await evRepo.GetByExecutionAsync(recorded.ExecutionId);
        Assert.NotNull(evidence);
        Assert.NotNull(evidence!.RuntimeEvidence);
        Assert.Equal("system", evidence.RuntimeEvidence!.RuntimeKey);
        Assert.True(evidence.RuntimeEvidence.Passed);
    }

    [Fact]
    public async Task Record_evidence_ExecutionId_matches_Execution_row()
    {
        var execRepo = new FakeExecRepo();
        var resRepo = new FakeResultRepo();
        var evRepo = new InMemoryEvidenceRepo();
        var v3Repo = new InMemoryV3Repo();

        var recorder = new SystemMtExecutionRecorder(execRepo, resRepo, evRepo, v3Repo);
        var recorded = await recorder.RecordAsync(CtxFor("heat-equation-amplitude"), OkOutcome(), mrInstanceId: 1);

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
            var recorded = await recorder.RecordAsync(
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

    // ---- ExecutionEvidence v2 (PR-C0) additions ----

    [Fact]
    public async Task Record_writes_TypedVerification_when_typed_inputs_are_provided()
    {
        var execRepo = new FakeExecRepo();
        var resRepo = new FakeResultRepo();
        var evRepo = new InMemoryEvidenceRepo();
        var v3Repo = new InMemoryV3Repo();

        var predicate = (MetBench_BLL.SystemMT.Catalog.Typed.Specs.BinaryComparisonPredicate)
            MetBench_BLL.SystemMT.Catalog.Typed.Migration.LegacyAssertionPredicateMapper.MapScalar(
                "greater", "followup", "source", "max_u");
        var spec = MetBench_BLL.SystemMT.Catalog.Typed.Migration.TypedSpecFactory.ForLegacyScalar(
            mrCode: "heat-equation-amplitude",
            valueName: "max_u",
            predicate: predicate,
            toleranceAbs: 0.0,
            toleranceRel: 0.0);
        var typedAssertion = OkOutcome().AssertionResult!;
        var typedResult = MetBench_BLL.SystemMT.Catalog.Typed.Runtime.VerificationResult.FromAssertion(
            typedAssertion,
            new MetBench_BLL.SystemMT.Catalog.Typed.Runtime.VerificationDiagnostic(1.0, 2.0, 1.0, 0.0));

        var recorder = new SystemMtExecutionRecorder(execRepo, resRepo, evRepo, v3Repo);
        var recorded = await recorder.RecordAsync(
            CtxFor("heat-equation-amplitude"),
            OkOutcome(),
            mrInstanceId: 1,
            batchId: null,
            typedVerification: typedResult,
            typedProperty: null,
            typedSpec: spec,
            typedPredicate: predicate,
            typedPropertySpec: null);

        var evidence = await evRepo.GetByExecutionAsync(recorded.ExecutionId);
        Assert.NotNull(evidence);
        var typed = evidence!.TypedVerification;
        Assert.NotNull(typed);
        Assert.Equal("MrSpec", typed!.SpecKind);
        Assert.Equal("heat-equation-amplitude", typed.SpecId);
        Assert.Equal("BinaryComparison", typed.PredicateKind);
        Assert.Equal("Passed", typed.Status);
        Assert.True(typed.Passed);
        Assert.NotNull(typed.Diagnostic);
        Assert.Equal(1.0, typed.Diagnostic!.Expected);
        Assert.Equal(2.0, typed.Diagnostic.Actual);
    }

    [Fact]
    public async Task Record_writes_pair_quality_from_typed_verification_without_treating_skips_as_failed()
    {
        var execRepo = new FakeExecRepo();
        var resRepo = new FakeResultRepo();
        var evRepo = new InMemoryEvidenceRepo();
        var v3Repo = new InMemoryV3Repo();

        var predicate = (MetBench_BLL.SystemMT.Catalog.Typed.Specs.BinaryComparisonPredicate)
            MetBench_BLL.SystemMT.Catalog.Typed.Migration.LegacyAssertionPredicateMapper.MapScalar(
                "greater", "followup", "source", "max_u");
        var spec = MetBench_BLL.SystemMT.Catalog.Typed.Migration.TypedSpecFactory.ForLegacyScalar(
            mrCode: "heat-equation-amplitude",
            valueName: "max_u",
            predicate: predicate,
            toleranceAbs: 0.0,
            toleranceRel: 0.0);
        var skipped = MetBench_BLL.SystemMT.Catalog.Typed.Runtime.VerificationResult
            .SkippedMissingObservable("max_u missing from followup role");

        var recorder = new SystemMtExecutionRecorder(execRepo, resRepo, evRepo, v3Repo);
        var recorded = await recorder.RecordAsync(
            CtxFor("heat-equation-amplitude"),
            OkOutcome() with
            {
                SourceMetrics = new Dictionary<string, double>(),
                FollowupMetrics = new Dictionary<string, double>(),
            },
            mrInstanceId: 1,
            batchId: null,
            typedVerification: skipped,
            typedProperty: null,
            typedSpec: spec,
            typedPredicate: predicate,
            typedPropertySpec: null);

        var evidence = await evRepo.GetByExecutionAsync(recorded.ExecutionId);

        Assert.NotNull(evidence);
        Assert.Equal(1, evidence!.PairQuality.PlannedPairs);
        Assert.Equal(1, evidence.PairQuality.ExecutedPairs);
        Assert.Equal(0, evidence.PairQuality.ValidPairs);
        Assert.Equal(0, evidence.PairQuality.PassedPairs);
        Assert.Equal(0, evidence.PairQuality.FailedPairs);
        Assert.Equal(1, evidence.PairQuality.SkippedPairs);
        Assert.Equal(0, evidence.PairQuality.InvalidSpecPairs);
        Assert.Equal(0.0, evidence.PairQuality.PassRateValid);
        Assert.Equal(0.0, evidence.PairQuality.PassRateAll);
        var reason = Assert.Single(evidence.PairQuality.SkipReasons);
        Assert.Equal("SkippedMissingObservable", reason.Status);
        Assert.Equal("max_u missing from followup role", reason.Reason);
    }

    [Fact]
    public async Task Record_writes_evidence_with_null_TypedVerification_when_typed_inputs_are_absent()
    {
        var execRepo = new FakeExecRepo();
        var resRepo = new FakeResultRepo();
        var evRepo = new InMemoryEvidenceRepo();
        var v3Repo = new InMemoryV3Repo();

        var recorder = new SystemMtExecutionRecorder(execRepo, resRepo, evRepo, v3Repo);
        var recorded = await recorder.RecordAsync(CtxFor("heat-equation-amplitude"), OkOutcome(), mrInstanceId: 1);

        var evidence = await evRepo.GetByExecutionAsync(recorded.ExecutionId);
        Assert.NotNull(evidence);
        Assert.Null(evidence!.TypedVerification);
    }

    [Fact]
    public async Task Live_pipeline_outcome_carries_typed_triple_into_evidence_without_explicit_typed_args()
    {
        // Wires PR-C0's TypedVerification block all the way through the live
        // SystemMtPipeline → SystemMtExecutionRecorder loop, without the
        // caller having to hand the typed triple in explicitly. The pipeline
        // captures the typed (MrSpec, PredicateSpec, VerificationResult)
        // produced by the dispatcher, attaches them to PipelineOutcome via
        // init-only properties, and the recorder reads them when no override
        // is supplied.
        var tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
            "metbench-pipeline-typed-evidence", Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(tempDir);
        var sourcePath = System.IO.Path.Combine(tempDir, "source.in.json");
        await System.IO.File.WriteAllTextAsync(sourcePath,
            "{\"materials\":{\"fuel\":{\"temperature_kelvin\":600.0}}}");

        try
        {
            var sourceOut = new
            {
                values = new Dictionary<string, double> { ["k_eff"] = 1.13 },
                metadata = new Dictionary<string, string> { ["adapter"] = "test" },
            };
            var followupOut = new
            {
                values = new Dictionary<string, double> { ["k_eff"] = 0.51 },
                metadata = new Dictionary<string, string> { ["adapter"] = "test" },
            };

            var fake = new V2Pipeline.FakeProcessExecutor(cmd =>
            {
                if (cmd.Contains("input-parser parse"))
                {
                    var data = new Dictionary<string, object?>
                    {
                        ["materials"] = new Dictionary<string, object?>
                        {
                            ["fuel"] = new Dictionary<string, object?>
                            {
                                ["temperature_kelvin"] = 600.0,
                            },
                        },
                    };
                    return new ProcessResult(
                        0, System.Text.Json.JsonSerializer.Serialize(data), "", TimeSpan.FromMilliseconds(5), false);
                }
                if (cmd.Contains("input-parser write"))
                    return new ProcessResult(0, "{}", "", TimeSpan.FromMilliseconds(5), false);
                if (cmd.Contains("runner"))
                {
                    var outPath = ExtractOutputArg(cmd);
                    var which = cmd.Contains(sourcePath) ? (object)sourceOut : followupOut;
                    System.IO.File.WriteAllText(outPath, System.Text.Json.JsonSerializer.Serialize(which));
                    return new ProcessResult(0, "", "", TimeSpan.FromMilliseconds(10), false);
                }
                if (cmd.Contains("output-parser"))
                {
                    var outPath = ExtractOutputFileArg(cmd);
                    return new ProcessResult(0, System.IO.File.ReadAllText(outPath), "", TimeSpan.FromMilliseconds(5), false);
                }
                return new ProcessResult(1, "", "Unknown command", TimeSpan.Zero, false);
            });

            var ctx = new PipelineContext(
                MrCode: "heat-equation-amplitude",
                TransformationName: "ScaleField",
                AssertionTypeCode: "less",
                ValueName: "k_eff",
                TargetFieldPath: "materials.fuel.temperature_kelvin",
                PathSyntax: "json-pointer",
                Parameters: new Dictionary<string, string> { ["factor"] = "1.5" },
                Tolerance: new AssertionTolerance(),
                ExtraAssertionValues: null,
                SutName: "test-sut",
                SourceCasePath: sourcePath,
                WorkingDirectory: tempDir,
                InputParserCommand: "input-parser",
                OutputParserCommand: "output-parser",
                RunnerCommand: "runner",
                TimeoutSeconds: 30,
                CatalogVersionSha: "test-sha",
                SutVersionSnapshot: "test-sut-v1",
                MetbenchVersion: "v2.2-dev",
                TriggeredBy: "test");

            var pipeline = new SystemMtPipeline(fake);
            var outcome = await pipeline.ExecuteAsync(ctx);

            Assert.Equal(PipelineStatus.Ok, outcome.FinalStatus);
            Assert.NotNull(outcome.AssertionResult);

            var execRepo = new FakeExecRepo();
            var resRepo = new FakeResultRepo();
            var evRepo = new InMemoryEvidenceRepo();
            var v3Repo = new InMemoryV3Repo();

            // Production call site (SystemMtLauncher.RunAsync line 188) passes
            // no typed args; the recorder must pick them up from the outcome.
            var recorder = new SystemMtExecutionRecorder(execRepo, resRepo, evRepo, v3Repo);
            var recorded = await recorder.RecordAsync(ctx, outcome, mrInstanceId: -1);

            var evidence = await evRepo.GetByExecutionAsync(recorded.ExecutionId);
            Assert.NotNull(evidence);

            var typed = evidence!.TypedVerification;
            Assert.NotNull(typed);
            Assert.Equal("MrSpec", typed!.SpecKind);
            Assert.False(string.IsNullOrEmpty(typed.PredicateId));
            Assert.Equal("BinaryComparison", typed.PredicateKind);
            Assert.Equal("Passed", typed.Status);
            Assert.True(typed.Passed);
            Assert.NotNull(typed.Diagnostic);
            // Less: expected = source = 1.13, actual = followup = 0.51
            Assert.Equal(1.13, typed.Diagnostic!.Expected);
            Assert.Equal(0.51, typed.Diagnostic.Actual);
            Assert.True(typed.Diagnostic.Residual >= 0.0);

            // Existing legacy v1 evidence fields must still round-trip.
            Assert.Equal("heat-equation-amplitude", evidence.Metadata.MrId);
            Assert.Equal("v2.2-dev", evidence.Metadata.MetbenchVersion);
            Assert.Equal("1.5", evidence.TransformationParameters["factor"]);
        }
        finally
        {
            try { System.IO.Directory.Delete(tempDir, recursive: true); } catch { }
        }
    }

    private static string ExtractOutputArg(string cmd)
    {
        const string marker = "--output \"";
        var i = cmd.IndexOf(marker, StringComparison.Ordinal);
        if (i < 0) throw new InvalidOperationException("No --output arg in " + cmd);
        var start = i + marker.Length;
        var end = cmd.IndexOf('"', start);
        return cmd.Substring(start, end - start);
    }

    private static string ExtractOutputFileArg(string cmd)
    {
        const string marker = "--output-file \"";
        var i = cmd.IndexOf(marker, StringComparison.Ordinal);
        if (i < 0) throw new InvalidOperationException("No --output-file arg in " + cmd);
        var start = i + marker.Length;
        var end = cmd.IndexOf('"', start);
        return cmd.Substring(start, end - start);
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

        public Task<bool> DeleteByExecutionIdAsync(Guid executionId, System.Threading.CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_store.RemoveAll(e => e.ExecutionId == executionId) > 0);
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
