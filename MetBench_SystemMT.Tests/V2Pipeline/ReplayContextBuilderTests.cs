using System.Collections.ObjectModel;
using MetBench_BLL.SystemMT.Pipeline;
using MetBench_Domain;
using MetBench_IDAL;
using Xunit;

namespace MetBench_SystemMT.Tests.V2Pipeline;

/// <summary>
/// TDD for T-E ReplayContextBuilder —— 把 Anomaly 沿 Result→Execution→MRInstance→MRBinding
/// → MR + Application 这条链路 join 回 PipelineContext。
/// </summary>
public sealed class ReplayContextBuilderTests
{
    [Fact]
    public void Build_full_join_path_succeeds_and_returns_all_fields()
    {
        var fakes = SetupFullChain();
        var sut = new ReplayContextBuilder(
            fakes.AnomalyRepo, fakes.ResultRepo, fakes.ExecRepo,
            fakes.MrInstanceRepo, fakes.BindingRepo, fakes.MrRepo, fakes.AppRepo);

        var built = sut.Build(fakes.AnomalyId, workingDirectory: "/tmp/replay");

        Assert.Equal("MR-T", built.Context.MrCode);
        Assert.Equal("m_mono", built.Context.TransformationName);
        Assert.Equal("openmoc", built.Context.SutName);
        Assert.Equal("less", built.Context.AssertionTypeCode);
        Assert.Equal("k_eff", built.Context.ValueName);
        Assert.Equal("materials.fuel.temperature_kelvin", built.Context.TargetFieldPath);
        Assert.Equal("json-pointer", built.Context.PathSyntax);
        Assert.Equal("1.25", built.Context.Parameters["factor"]);
        Assert.Equal("/tmp/replay", built.Context.WorkingDirectory);
        // Version snapshot 从 Execution 复制
        Assert.Equal("abc123", built.Context.CatalogVersionSha);
        Assert.Equal("openmoc@2026-05", built.Context.SutVersionSnapshot);
        Assert.Equal("v2.1", built.Context.MetbenchVersion);
        Assert.StartsWith("replay-of-", built.Context.TriggeredBy);
        // Tolerance 透传
        Assert.True(built.Context.Tolerance.NoiseAware);
        Assert.Equal(0.001, built.Context.Tolerance.ToleranceRel);
    }

    [Fact]
    public void Build_missing_anomaly_throws_with_id()
    {
        var fakes = SetupFullChain();
        var sut = NewBuilder(fakes);
        var ex = Assert.Throws<InvalidOperationException>(() =>
            sut.Build(Guid.NewGuid(), "/tmp"));
        Assert.Contains("Anomaly", ex.Message);
        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public void Build_missing_result_throws()
    {
        var fakes = SetupFullChain();
        fakes.ResultRepo.Data.Clear();
        var sut = NewBuilder(fakes);
        var ex = Assert.Throws<InvalidOperationException>(() => sut.Build(fakes.AnomalyId, "/tmp"));
        Assert.Contains("Result", ex.Message);
    }

    [Fact]
    public void Build_missing_execution_throws()
    {
        var fakes = SetupFullChain();
        fakes.ExecRepo.Data.Clear();
        var sut = NewBuilder(fakes);
        Assert.Throws<InvalidOperationException>(() => sut.Build(fakes.AnomalyId, "/tmp"));
    }

    [Fact]
    public void Build_mrinstance_parameter_overrides_propagate_into_context()
    {
        var fakes = SetupFullChain();
        fakes.MrInstanceRepo.Data.Single().ParameterOverrides = new()
        {
            ["factor"] = "2.0",
            ["seed"] = "42",
        };
        var sut = NewBuilder(fakes);
        var built = sut.Build(fakes.AnomalyId, "/tmp");
        Assert.Equal("2.0", built.Context.Parameters["factor"]);
        Assert.Equal("42", built.Context.Parameters["seed"]);
    }

    [Fact]
    public void Build_sample_path_override_in_instance_wins()
    {
        var fakes = SetupFullChain();
        fakes.MrInstanceRepo.Data.Single().SampleCaseOverridePath = "SUT/openmoc/sample/regression.json";
        var built = NewBuilder(fakes).Build(fakes.AnomalyId, "/tmp");
        Assert.Equal("SUT/openmoc/sample/regression.json", built.Context.SourceCasePath);
    }

    [Fact]
    public void Build_falls_back_to_binding_default_sample_when_no_override()
    {
        var fakes = SetupFullChain();
        var built = NewBuilder(fakes).Build(fakes.AnomalyId, "/tmp");
        Assert.Equal("SUT/openmoc/sample/pincell.json", built.Context.SourceCasePath);
    }

    [Fact]
    public void Build_triggeredby_uniquely_identifies_replay()
    {
        var fakes = SetupFullChain();
        var built = NewBuilder(fakes).Build(fakes.AnomalyId, "/tmp");
        Assert.Equal($"replay-of-{fakes.AnomalyId}", built.Context.TriggeredBy);
    }

    // ===== ExtractValueName helper =====

    [Fact]
    public void ExtractValueName_falls_back_to_k_eff_when_operator_empty()
    {
        var mr = new MetamorphicRelation { Operator = "" };
        Assert.Equal("k_eff", ReplayContextBuilder.ExtractValueName(mr));
    }

    [Fact]
    public void ExtractValueName_uses_operator_field_when_set()
    {
        var mr = new MetamorphicRelation { Operator = "flux_thermal" };
        Assert.Equal("flux_thermal", ReplayContextBuilder.ExtractValueName(mr));
    }

    // ===== Fixtures =====

    private static ReplayContextBuilder NewBuilder(JoinFakes f) =>
        new(f.AnomalyRepo, f.ResultRepo, f.ExecRepo, f.MrInstanceRepo, f.BindingRepo, f.MrRepo, f.AppRepo);

    private static JoinFakes SetupFullChain()
    {
        var resultId = Guid.NewGuid();
        var executionId = Guid.NewGuid();
        var anomalyId = Guid.NewGuid();
        var fakes = new JoinFakes { AnomalyId = anomalyId };

        fakes.AppRepo.Data.Add(new Application
        {
            IdApplication = 1, Name = "openmoc", CodeName = "openmoc",
            SourceTestCaseName = "pincell", DomainName = "neutron-transport",
        });

        fakes.MrRepo.Data.Add(new MetamorphicRelation
        {
            IdMR = 5, Code = "MR-T", Description = "RaiseFuelTemperature",
            MetaPatternCode = "m_mono", AssertionTypeCode = "less",
            Context = "", Constraint = "", OrderOfMR = "",
            InputPattern = "", OutputPattern = "",
            InputPatterntosympy = "", OutputPatterntosympy = "",
            DimensionOfInputPattern = "", DimensionOfOutputPattern = "",
            Granularity = "", Hierarchy = "", Operator = "k_eff", Expression = "",
            InputPatternImageData = Array.Empty<byte>(),
            OutputPatternImageData = Array.Empty<byte>(),
        });

        fakes.BindingRepo.Data.Add(new MRBinding
        {
            IdMRBinding = 10, MRId = 5, ApplicationId = 1,
            DefaultSampleCasePath = "SUT/openmoc/sample/pincell.json",
            DefaultTolerance = new ToleranceConfig
            {
                NoiseAware = true,
                ToleranceRel = 0.001,
                ToleranceAbs = 0.0,
                NoiseMultiplier = 3.0,
            },
            ParameterMappings = new()
            {
                new ParameterMapping
                {
                    AbstractParamName = "physics.fuel.temperature",
                    ConcreteFieldPath = "materials.fuel.temperature_kelvin",
                    PathSyntax = "json-pointer",
                },
            },
            IsActive = true,
        });

        fakes.MrInstanceRepo.Data.Add(new MRInstance
        {
            IdInstance = 100, MRBindingId = 10,
            ParameterOverrides = new() { ["factor"] = "1.25" },
        });

        fakes.ExecRepo.Data.Add(new Execution
        {
            IdExecution = executionId, MRInstanceId = 100, Status = "anomaly",
            CatalogVersionSha = "abc123",
            SutVersionSnapshot = "openmoc@2026-05",
            MetbenchVersion = "v2.1",
            TriggeredBy = "cli-user",
        });

        fakes.ResultRepo.Data.Add(new Result
        {
            IdResult = resultId, ExecutionId = executionId,
            SourceValue = 1.13, FollowupValue = 0.51,
            AssertionPassed = false,
        });

        fakes.AnomalyRepo.Data.Add(new MetBench_Domain.Anomaly
        {
            IdAnomaly = anomalyId, ResultId = resultId,
            Severity = "major", Status = "new", Category = "basin",
        });

        return fakes;
    }
}

internal sealed class JoinFakes
{
    public Guid AnomalyId { get; set; }
    public FakeRcbAnomalyRepo AnomalyRepo { get; } = new();
    public FakeRcbResultRepo ResultRepo { get; } = new();
    public FakeRcbExecRepo ExecRepo { get; } = new();
    public FakeRcbMrInstanceRepo MrInstanceRepo { get; } = new();
    public FakeRcbBindingRepo BindingRepo { get; } = new();
    public FakeRcbMrRepo MrRepo { get; } = new();
    public FakeRcbAppRepo AppRepo { get; } = new();
}

internal sealed class FakeRcbAnomalyRepo : IAnomalyRepository
{
    public List<MetBench_Domain.Anomaly> Data { get; } = new();
    public ObservableCollection<MetBench_Domain.Anomaly> GetAll() => new(Data);
    public MetBench_Domain.Anomaly? Get(Guid id) => Data.FirstOrDefault(a => a.IdAnomaly == id);
    public ObservableCollection<MetBench_Domain.Anomaly> Get(MetBench_Domain.Anomaly e) => new(Data);
    public bool Add(MetBench_Domain.Anomaly e) { Data.Add(e); return true; }
    public bool Modify(MetBench_Domain.Anomaly e) => true;
    public bool Remove(MetBench_Domain.Anomaly e) => Data.Remove(e);
    public ObservableCollection<MetBench_Domain.Anomaly> GetPage(int p, int s) => new(Data.Skip(p * s).Take(s).ToList());
    public int Count() => Data.Count;
    public MetBench_Domain.Anomaly? GetByResult(Guid id) => Data.FirstOrDefault(a => a.ResultId == id);
    public ObservableCollection<MetBench_Domain.Anomaly> GetByStatus(string s) => new();
    public ObservableCollection<MetBench_Domain.Anomaly> GetByLinkedBug(int id) => new();
}

internal sealed class FakeRcbResultRepo : IResultRepository
{
    public List<Result> Data { get; } = new();
    public ObservableCollection<Result> GetAll() => new(Data);
    public Result? Get(Guid id) => Data.FirstOrDefault(r => r.IdResult == id);
    public ObservableCollection<Result> Get(Result e) => new(Data);
    public bool Add(Result e) { Data.Add(e); return true; }
    public bool Modify(Result e) => true;
    public bool Remove(Result e) => Data.Remove(e);
    public ObservableCollection<Result> GetPage(int p, int s) => new(Data.Skip(p * s).Take(s).ToList());
    public int Count() => Data.Count;
    public Result? GetByExecution(Guid id) => Data.FirstOrDefault(r => r.ExecutionId == id);
}

internal sealed class FakeRcbExecRepo : IExecutionRepository
{
    public List<Execution> Data { get; } = new();
    public ObservableCollection<Execution> GetAll() => new(Data);
    public Execution? Get(Guid id) => Data.FirstOrDefault(e => e.IdExecution == id);
    public ObservableCollection<Execution> Get(Execution e) => new(Data);
    public bool Add(Execution e) { Data.Add(e); return true; }
    public bool Modify(Execution e) => true;
    public bool Remove(Execution e) => Data.Remove(e);
    public ObservableCollection<Execution> GetPage(int p, int s) => new(Data.Skip(p * s).Take(s).ToList());
    public int Count() => Data.Count;
    public ObservableCollection<Execution> GetByMRInstance(int id) => new();
    public ObservableCollection<Execution> GetByBatch(Guid id) => new();
    public ObservableCollection<Execution> GetByStatus(string s, int p, int sz) => new();
    public ObservableCollection<Execution> GetByDateRange(DateTime f, DateTime t) => new();
}

internal sealed class FakeRcbMrInstanceRepo : IMRInstanceRepository
{
    public List<MRInstance> Data { get; } = new();
    public ObservableCollection<MRInstance> GetAll() => new(Data);
    public MRInstance Get(int id) => Data.First(m => m.IdInstance == id);
    public ObservableCollection<MRInstance> Get(MRInstance e) => new(Data);
    public bool Add(MRInstance e) { Data.Add(e); return true; }
    public bool Modify(MRInstance e) => true;
    public bool Remove(MRInstance e) => Data.Remove(e);
    public ObservableCollection<MRInstance> GetByBinding(int id) => new();
    public ObservableCollection<MRInstance> GetReusable() => new();
    public MRInstance? GetByName(string name) => Data.FirstOrDefault(m => m.Name == name);
}

internal sealed class FakeRcbBindingRepo : IMRBindingRepository
{
    public List<MRBinding> Data { get; } = new();
    public ObservableCollection<MRBinding> GetAll() => new(Data);
    public MRBinding Get(int id) => Data.First(b => b.IdMRBinding == id);
    public ObservableCollection<MRBinding> Get(MRBinding e) => new(Data);
    public bool Add(MRBinding e) { Data.Add(e); return true; }
    public bool Modify(MRBinding e) => true;
    public bool Remove(MRBinding e) => Data.Remove(e);
    public ObservableCollection<MRBinding> GetByMR(int id) => new(Data.Where(b => b.MRId == id).ToList());
    public ObservableCollection<MRBinding> GetByApplication(int id) => new();
    public ObservableCollection<MRBinding> GetActive() => new();
}

internal sealed class FakeRcbMrRepo : IMetamorphicRelationRepository
{
    public List<MetamorphicRelation> Data { get; } = new();
    public DatatoImage datatoImage { get; set; } = null!;
    public ObservableCollection<MetamorphicRelation> GetAll() => new(Data);
    public MetamorphicRelation Get(int id) => Data.First(m => m.IdMR == id);
    public ObservableCollection<MetamorphicRelation> Get(MetamorphicRelation e) => new(Data);
    public bool Add(MetamorphicRelation e) { Data.Add(e); return true; }
    public bool Modify(MetamorphicRelation e) => true;
    public bool Remove(MetamorphicRelation e) => Data.Remove(e);
    public ObservableCollection<MetamorphicRelations_QueryResultData> GetAll_MIX() => new();
    public ObservableCollection<MetamorphicRelations_QueryResultData> Get(MetamorphicRelation m, string d) => new();
    public ObservableCollection<MetamorphicRelations_QueryResultData> GetAll_MIXTwoTable() => new();
    public bool IsDuplicate(MetamorphicRelation m, bool excludeSelf = false) => false;
}

internal sealed class FakeRcbAppRepo : IApplicationRepository
{
    public List<Application> Data { get; } = new();
    public ObservableCollection<Application> GetAll() => new(Data);
    public Application Get(int id) => Data.First(a => a.IdApplication == id);
    public ObservableCollection<Application> Get(Application e) => new(Data);
    public bool Add(Application e) { Data.Add(e); return true; }
    public bool Modify(Application e) => true;
    public bool Remove(Application e) => Data.Remove(e);
    public int Get(string name) => Data.FirstOrDefault(a => a.Name == name)?.IdApplication ?? -1;
    public ObservableCollection<Applications_QueryResultData> GetAll_MIX() => new();
    public ObservableCollection<Applications_QueryResultData> GetByName(string name) => new();
    public bool IsDuplicate(Application app, bool excludeSelf) => false;
}
