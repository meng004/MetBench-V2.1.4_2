#pragma warning disable CS0618 // injection test exercises obsolete HardcodedMrCatalogProvider for parity
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MetBench_BLL.SystemMT.Catalog;
using MetBench_BLL.SystemMT.Launcher;
using MetBench_BLL.SystemMT.Pipeline;
using MetBench_SystemMT.Tests.V2Anomaly;
using MetBench_SystemMT.Tests.V2Pipeline;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Launcher;

/// <summary>
/// Acceptance: an injected <see cref="IMrCatalogProvider"/> drives the launcher's
/// runnable MR set, and launcher construction rejects a missing provider.
/// </summary>
public sealed class SystemMtLauncherProviderInjectionTests
{
    private static LauncherOptions Opts() => new("/tmp", "python3", "python3");

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

    private sealed class FakeMrCatalogProvider : IMrCatalogProvider
    {
        private readonly IReadOnlyList<MrCatalogEntry> _entries;
        public FakeMrCatalogProvider(IReadOnlyList<MrCatalogEntry> entries) => _entries = entries;
        public string SourceDescription => "Fake";
        public IReadOnlyList<MrCatalogEntry> Load() => _entries;
    }

    [Fact]
    public async Task ListAvailableAsync_uses_injected_catalog_provider()
    {
        var fakeProvider = new FakeMrCatalogProvider(new[]
        {
            FakeEntry("provider-only-mr-a", "test-sut"),
            FakeEntry("provider-only-mr-b", "test-sut"),
        });

        var launcher = new SystemMtLauncher(
            options: Opts(),
            pipeline: new SystemMtPipeline(),
            recorder: new SystemMtExecutionRecorder(new FakeExecRepo(), new FakeResultRepo()),
            anomalyService: new RecordingAnomalyService(),
            catalogProvider: fakeProvider);

        var items = await launcher.ListAvailableAsync();

        Assert.Equal(2, items.Count);
        Assert.Contains(items, m => m.Id == "provider-only-mr-a");
        Assert.Contains(items, m => m.Id == "provider-only-mr-b");
        // Critically: the launcher does NOT also surface the 21 hardcoded MRs alongside.
        Assert.DoesNotContain(items, m => m.Id == "openmoc-pincell-nu-sigma-f");
    }

    [Fact]
    public void Constructor_without_catalog_provider_throws()
    {
        Assert.Throws<ArgumentNullException>(() => new SystemMtLauncher(
            options: Opts(),
            pipeline: new SystemMtPipeline(),
            recorder: new SystemMtExecutionRecorder(new FakeExecRepo(), new FakeResultRepo()),
            anomalyService: new RecordingAnomalyService(),
            catalogProvider: null!));
    }

    [Fact]
    public async Task Manifest_provider_yields_runnable_catalog_without_hardcoded_fallback()
    {
        var opts = new LauncherOptions(
            SutRoot: TestAssetPaths.AssetRoot(),
            SystemPython: "python3",
            OpenMocPython: "python3");

        var launcher = new SystemMtLauncher(
            options: opts,
            pipeline: new SystemMtPipeline(),
            recorder: new SystemMtExecutionRecorder(new FakeExecRepo(), new FakeResultRepo()),
            anomalyService: new RecordingAnomalyService(),
            catalogProvider: new ManifestMrCatalogProvider(opts));

        var items = await launcher.ListAvailableAsync();

        Assert.Equal(33, items.Count);
        Assert.Contains(items, m => m.Id == "openmoc-pincell-nu-sigma-f");
    }

    [Fact]
    public async Task Manifest_provider_yields_same_catalog_to_launcher_as_hardcoded()
    {
        var opts = new LauncherOptions(
            SutRoot: TestAssetPaths.AssetRoot(),
            SystemPython: "python3",
            OpenMocPython: "python3");

        var hardcodedLauncher = new SystemMtLauncher(
            options: opts,
            pipeline: new SystemMtPipeline(),
            recorder: new SystemMtExecutionRecorder(new FakeExecRepo(), new FakeResultRepo()),
            anomalyService: new RecordingAnomalyService(),
            catalogProvider: new HardcodedMrCatalogProvider(opts));

        var manifestLauncher = new SystemMtLauncher(
            options: opts,
            pipeline: new SystemMtPipeline(),
            recorder: new SystemMtExecutionRecorder(new FakeExecRepo(), new FakeResultRepo()),
            anomalyService: new RecordingAnomalyService(),
            catalogProvider: new ManifestMrCatalogProvider(opts));

        var hardcodedIds = (await hardcodedLauncher.ListAvailableAsync()).Select(m => m.Id).OrderBy(s => s).ToList();
        var manifestIds = (await manifestLauncher.ListAvailableAsync()).Select(m => m.Id).OrderBy(s => s).ToList();

        Assert.Equal(hardcodedIds, manifestIds);
    }
}
