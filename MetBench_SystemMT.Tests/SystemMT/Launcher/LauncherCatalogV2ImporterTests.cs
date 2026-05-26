using System.Collections.ObjectModel;
using MetBench_BLL.SystemMT.Catalog;
using MetBench_BLL.SystemMT.Launcher;
using MetBench_BLL.SystemMT.Pipeline;
using MetBench_Domain;
using MetBench_IDAL;
using MetBench_SystemMT.Tests.V2Anomaly;
using MetBench_SystemMT.Tests.V2Pipeline;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Launcher;

/// <summary>
/// 方向 2 — <see cref="LauncherCatalogV2Importer"/>:把 launcher 内部 17 MR 目录投影
/// 到 v2 实体表(Application + MetamorphicRelation + MRBinding),验证内容正确、
/// 幂等、写审计、且不破坏 launcher 的运行时行为(对 MT 流程的再次验证)。
/// </summary>
public sealed class LauncherCatalogV2ImporterTests
{
    private readonly FakeImporterAppRepo _apps = new();
    private readonly FakeImporterMrRepo _mrs = new();
    private readonly FakeImporterBindingRepo _bindings = new();
    private readonly FakeImporterAuditRepo _audit = new();
    private readonly FakeExecRepo _execs = new();
    private readonly FakeResultRepo _results = new();
    private readonly SystemMtLauncher _launcher;
    private readonly LauncherCatalogV2Importer _importer;

    private static MrCatalogEntry FakeEntry(string mrId, string sutName) =>
        new(
            Mr: new MrSummary(
                Id: mrId,
                DisplayName: "fake",
                SutName: sutName,
                TransformationName: "ScaleField",
                AssertionName: "GreaterThan",
                ValueName: "y",
                DefaultParameters: new Dictionary<string, string>(),
                Description: "test",
                MrFamily: "test"),
            SampleCaseRelativePath: "fake/sample.json",
            RunnerScriptPath: "/tmp/runner.py",
            InputAdapterScriptPath: "/tmp/in_adapter.py",
            OutputAdapterScriptPath: "/tmp/out_adapter.py",
            PythonExecutable: "python3",
            WorkRootName: "MetBenchFake",
            Timeout: TimeSpan.FromSeconds(30),
            InputParserScriptPath: "/tmp/in_parser.py",
            OutputParserScriptPath: "/tmp/out_parser.py",
            TransformSteps: new[] { new MrCatalogTransformStep("ScaleField", "/x") },
            AssertionTypeCode: "greater",
            EquationKey: "",
            Tolerance: null);

    private sealed class FakeCatalogReader : ISystemMtCatalogReader
    {
        private readonly IReadOnlyList<MrCatalogEntry> _entries;
        public FakeCatalogReader(IReadOnlyList<MrCatalogEntry> entries) => _entries = entries;
        public IReadOnlyList<MrCatalogEntry> GetCatalogEntries() => _entries;
    }

    public LauncherCatalogV2ImporterTests()
    {
        var launcherOptions = new LauncherOptions(
            SutRoot: TestAssetPaths.AssetRoot(),
            SystemPython: TestAssetPaths.PythonExecutable(),
            OpenMocPython: TestAssetPaths.PythonExecutable());
        _launcher = new SystemMtLauncher(
            launcherOptions,
            new SystemMtPipeline(),
            new SystemMtExecutionRecorder(_execs, _results),
            new RecordingAnomalyService(),
            new ManifestMrCatalogProvider(launcherOptions));
        _importer = new LauncherCatalogV2Importer(_launcher, _apps, _mrs, _bindings, _audit);
    }

    [Fact]
    public void Import_creates_one_Application_per_unique_SUT_with_system_level_kind()
    {
        var summary = _importer.Import();

        Assert.Equal(13, summary.ApplicationsCreated);
        Assert.Equal(13, _apps.Data.Count);
        var sutNames = _apps.Data.Select(a => a.Name).OrderBy(n => n).ToArray();
        Assert.Equal(
            new[] { "advection-1d", "burgers-1d", "damped-oscillator", "decay-chain", "diffusion-1d", "heat-equation", "lotka-volterra", "openmc", "openmoc", "poisson-1d", "projectile", "subchannel-1d", "wave-1d" },
            sutNames);
        Assert.All(_apps.Data, a => Assert.Equal("system-level", a.Kind));
        Assert.All(_apps.Data, a => Assert.Equal("Python", a.ProgrammingLanguage));
    }

    [Fact]
    public void Import_creates_one_MetamorphicRelation_per_launcher_MR_with_v2_fields()
    {
        var summary = _importer.Import();

        Assert.Equal(25, summary.MrsCreated);
        Assert.Equal(25, _mrs.Data.Count);
        Assert.All(_mrs.Data, m => Assert.Equal("system-level", m.Kind));
        Assert.All(_mrs.Data, m => Assert.Equal("manual", m.DiscoveryMethod));
        // S8-P1 后元模式扩展：Scaling → m_mono / Invariance → m_inv / Convergence → m_conv
        var allowedMps = new HashSet<string> { "m_mono", "m_inv", "m_conv" };
        Assert.All(_mrs.Data, m => Assert.Contains(m.MetaPatternCode, allowedMps));
        // S8-P1 加 2 个 MR：bateman-mass-conservation (m_inv) + bateman-timestep-cauchy (m_conv)
        Assert.Equal("m_inv", _mrs.Data.Single(m => m.Code == "bateman-mass-conservation").MetaPatternCode);
        Assert.Equal("m_conv", _mrs.Data.Single(m => m.Code == "bateman-timestep-cauchy").MetaPatternCode);
    }

    [Fact]
    public void Import_creates_MRBindings_with_valid_FK_to_MR_and_Application()
    {
        _importer.Import();

        Assert.Equal(25, _bindings.Data.Count);
        foreach (var b in _bindings.Data)
        {
            Assert.NotNull(_mrs.Data.FirstOrDefault(m => m.IdMR == b.MRId));
            Assert.NotNull(_apps.Data.FirstOrDefault(a => a.IdApplication == b.ApplicationId));
            Assert.Equal("active", b.Status);
            Assert.False(string.IsNullOrEmpty(b.DefaultSampleCasePath));
        }
    }

    [Fact]
    public void Import_Application_carries_runner_and_parser_paths()
    {
        _importer.Import();

        var openmoc = _apps.Data.First(a => a.Name == "openmoc");
        Assert.Contains("openmoc_runner.py", openmoc.RunnerEntryPath!);
        Assert.Contains("openmoc_input_parser.py", openmoc.InputParserPath!);
        Assert.Contains("openmoc_output_parser.py", openmoc.OutputParserPath!);

        // 注:ODE SUT 的 runner 用 <sut>.py 命名(decay_chain.py / damped_oscillator.py /
        // lotka_volterra.py);openmoc/openmc 用 <sut>_runner.py。
        var lotka = _apps.Data.First(a => a.Name == "lotka-volterra");
        Assert.Contains("lotka_volterra.py", lotka.RunnerEntryPath!);
        Assert.Contains("lotka_volterra_input_parser.py", lotka.InputParserPath!);
    }

    [Fact]
    public void Import_MR_carries_transformation_assertion_value_name()
    {
        _importer.Import();

        var nsf = _mrs.Data.First(m => m.Code == "openmoc-pincell-nu-sigma-f");
        Assert.Equal("ScaleField", nsf.TransformationName);
        Assert.Equal("greater", nsf.AssertionTypeCode);
        Assert.Equal("k_eff", nsf.ValueName);

        var sigmaA = _mrs.Data.First(m => m.Code == "openmoc-pincell-sigma-a");
        Assert.Equal("ScaleFuelAbsorption", sigmaA.TransformationName);
        Assert.Equal("less", sigmaA.AssertionTypeCode);

        var lotka = _mrs.Data.First(m => m.Code == "lotka-volterra-scale-gamma");
        Assert.Equal("ScaleField", lotka.TransformationName);
        Assert.Equal("mean_prey", lotka.ValueName);
    }

    [Fact]
    public void Import_MR_carries_EquationKey_from_blueprint()
    {
        // S8-P5 review fix: 之前 importer 漏写 EquationKey → V3 migration 全 collapse 到 Other
        _importer.Import();

        Assert.Equal("bateman", _mrs.Data.First(m => m.Code == "bateman-mass-conservation").EquationKey);
        Assert.Equal("bateman", _mrs.Data.First(m => m.Code == "decay-chain-scale-initial").EquationKey);
        Assert.Equal("heat-equation-1d", _mrs.Data.First(m => m.Code == "fourier-alpha-monotonic").EquationKey);
        Assert.Equal("diffusion", _mrs.Data.First(m => m.Code == "diffusion-source-linearity").EquationKey);
        Assert.Equal("navier-stokes", _mrs.Data.First(m => m.Code == "subchannel-flow-temperature-monotone").EquationKey);
    }

    [Fact]
    public void Import_writes_one_audit_log_entry_with_counts()
    {
        _importer.Import();

        var log = Assert.Single(_audit.Data);
        Assert.Equal("launcher.catalog.import", log.Action);
        Assert.Equal("launcher-import", log.Actor);
        Assert.Contains("applicationsCreated", log.DetailsJson);
        Assert.Contains("\"mrsCreated\":25", log.DetailsJson);
        Assert.Contains("\"bindingsCreated\":25", log.DetailsJson);
    }

    [Fact]
    public void Import_is_idempotent_on_second_run_no_duplicate_rows()
    {
        var first = _importer.Import();
        var second = _importer.Import();

        // 第 1 次:全部新建
        Assert.Equal(13, first.ApplicationsCreated);
        Assert.Equal(25, first.MrsCreated);
        Assert.Equal(25, first.BindingsCreated);

        // 第 2 次:全部已存在
        Assert.Equal(0, second.ApplicationsCreated);
        Assert.Equal(13, second.ApplicationsExisting);
        Assert.Equal(0, second.MrsCreated);
        Assert.Equal(25, second.MrsExisting);
        Assert.Equal(0, second.BindingsCreated);
        Assert.Equal(25, second.BindingsExisting);

        // 行数总和未变
        Assert.Equal(13, _apps.Data.Count);
        Assert.Equal(25, _mrs.Data.Count);
        Assert.Equal(25, _bindings.Data.Count);
        // 两次都写一条审计
        Assert.Equal(2, _audit.Data.Count);
    }

    [Fact]
    public void Import_accepts_custom_actor_in_audit_log()
    {
        _importer.Import(actor: "test-actor");

        var log = Assert.Single(_audit.Data);
        Assert.Equal("test-actor", log.Actor);
    }

    [Fact]
    public void Constructor_rejects_null_arguments()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new LauncherCatalogV2Importer(null!, _apps, _mrs, _bindings, _audit));
        Assert.Throws<ArgumentNullException>(() =>
            new LauncherCatalogV2Importer(_launcher, null!, _mrs, _bindings, _audit));
        Assert.Throws<ArgumentNullException>(() =>
            new LauncherCatalogV2Importer(_launcher, _apps, null!, _bindings, _audit));
        Assert.Throws<ArgumentNullException>(() =>
            new LauncherCatalogV2Importer(_launcher, _apps, _mrs, null!, _audit));
        Assert.Throws<ArgumentNullException>(() =>
            new LauncherCatalogV2Importer(_launcher, _apps, _mrs, _bindings, null!));
    }

    [Fact]
    public void Import_reads_entries_from_catalog_reader_abstraction()
    {
        var importer = new LauncherCatalogV2Importer(
            new FakeCatalogReader(new[]
            {
                FakeEntry("reader-only-mr", "reader-sut"),
            }),
            _apps,
            _mrs,
            _bindings,
            _audit);

        var summary = importer.Import();

        Assert.Equal(1, summary.ApplicationsCreated);
        Assert.Equal(1, summary.MrsCreated);
        Assert.Equal(1, summary.BindingsCreated);
        Assert.Equal("reader-sut", Assert.Single(_apps.Data).Name);
        Assert.Equal("reader-only-mr", Assert.Single(_mrs.Data).Code);
    }

    [Fact]
    public async Task Import_then_RunAsync_heat_equation_still_produces_execution_result()
    {
        // 「对 MT 流程的再次验证」:导入数据后,launcher 仍能跑通端到端 MR
        _importer.Import();

        var result = await _launcher.RunAsync("heat-equation-amplitude");

        Assert.True(result.Passed, result.FailureReason);
        var exec = Assert.Single(_execs.Data);
        var res = Assert.Single(_results.Data);
        Assert.Equal("ok", exec.Status);
        Assert.True(res.AssertionPassed);
        Assert.Equal(exec.IdExecution, res.ExecutionId);
    }

    // ====================== fakes (auto-id) ======================

    internal sealed class FakeImporterAppRepo : IApplicationRepository
    {
        public List<Application> Data { get; } = new();
        private int _nextId = 1;
        public ObservableCollection<Application> GetAll() => new(Data);
        public Application Get(int id) => Data.First(a => a.IdApplication == id);
        public ObservableCollection<Application> Get(Application e) => new(Data);
        public bool Add(Application e) { e.IdApplication = _nextId++; Data.Add(e); return true; }
        public bool Modify(Application e) => true;
        public bool Remove(Application e) => Data.Remove(e);
        public int Get(string name) => Data.FirstOrDefault(a => a.Name == name)?.IdApplication ?? -1;
        public ObservableCollection<Applications_QueryResultData> GetAll_MIX() => new();
        public ObservableCollection<Applications_QueryResultData> GetByName(string name) => new();
        public bool IsDuplicate(Application app, bool excludeSelf) => false;
    }

    internal sealed class FakeImporterMrRepo : IMetamorphicRelationRepository
    {
        public List<MetamorphicRelation> Data { get; } = new();
        private int _nextId = 1;
        public DatatoImage datatoImage { get; set; } = null!;
        public ObservableCollection<MetamorphicRelation> GetAll() => new(Data);
        public MetamorphicRelation Get(int id) => Data.First(m => m.IdMR == id);
        public ObservableCollection<MetamorphicRelation> Get(MetamorphicRelation e) => new(Data);
        public bool Add(MetamorphicRelation e) { e.IdMR = _nextId++; Data.Add(e); return true; }
        public bool Modify(MetamorphicRelation e) => true;
        public bool Remove(MetamorphicRelation e) => Data.Remove(e);
        public ObservableCollection<MetamorphicRelations_QueryResultData> GetAll_MIX() => new();
        public ObservableCollection<MetamorphicRelations_QueryResultData> Get(MetamorphicRelation m, string d) => new();
        public ObservableCollection<MetamorphicRelations_QueryResultData> GetAll_MIXTwoTable() => new();
        public bool IsDuplicate(MetamorphicRelation m, bool excludeSelf = false) => false;
    }

    internal sealed class FakeImporterBindingRepo : IMRBindingRepository
    {
        public List<MRBinding> Data { get; } = new();
        private int _nextId = 1;
        public ObservableCollection<MRBinding> GetAll() => new(Data);
        public MRBinding Get(int id) => Data.First(b => b.IdMRBinding == id);
        public ObservableCollection<MRBinding> Get(MRBinding e) => new(Data);
        public bool Add(MRBinding e) { e.IdMRBinding = _nextId++; Data.Add(e); return true; }
        public bool Modify(MRBinding e) => true;
        public bool Remove(MRBinding e) => Data.Remove(e);
        public ObservableCollection<MRBinding> GetByMR(int mrId) => new(Data.Where(b => b.MRId == mrId).ToList());
        public ObservableCollection<MRBinding> GetByApplication(int appId) => new(Data.Where(b => b.ApplicationId == appId).ToList());
        public ObservableCollection<MRBinding> GetActive() => new(Data.Where(b => b.Status == "active").ToList());
        public ObservableCollection<MRBinding> GetByStatus(string s) => new(Data.Where(b => b.Status == s).ToList());
    }

    internal sealed class FakeImporterAuditRepo : IAuditLogRepository
    {
        public List<AuditLog> Data { get; } = new();
        public ObservableCollection<AuditLog> GetAll() => new(Data);
        public AuditLog? Get(Guid id) => Data.FirstOrDefault(l => l.IdLog == id);
        public ObservableCollection<AuditLog> Get(AuditLog e) => new(Data);
        public bool Add(AuditLog e) { Data.Add(e); return true; }
        public bool Modify(AuditLog e) => true;
        public bool Remove(AuditLog e) => Data.Remove(e);
        public ObservableCollection<AuditLog> GetPage(int p, int s) => new(Data.Skip(p * s).Take(s).ToList());
        public int Count() => Data.Count;
        public ObservableCollection<AuditLog> GetByDateRange(DateTime f, DateTime t) => new();
        public ObservableCollection<AuditLog> GetByAction(string a) => new(Data.Where(l => l.Action == a).ToList());
        public ObservableCollection<AuditLog> GetByTargetEntity(string et, string eid) => new();
    }
}
