using MetBench_BLL.SystemMT;
using MetBench_BLL.SystemMT.Persistence;
using MetBench_DAL;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Persistence;

/// <summary>
/// R1 parity guard for the two <see cref="LiteDbSystemMtResultRepository.SaveAsync"/>
/// write paths. CLAUDE.md §12.5 tracks this as a planned guard ("LegacyResultRecordParityTests");
/// this is its implementation.
///
/// <para>Path A — legacy mirror: <c>SaveAsync(string mrName, SystemMtResult result)</c>
/// projects via <see cref="SystemMtResultRecord.FromResult"/> then <c>Insert</c>s.</para>
/// <para>Path B — direct record: <c>SaveAsync(SystemMtResultRecord record)</c>
/// <c>Upsert</c>s a pre-built record (e.g. from the recorder's evidence-write mirror).</para>
///
/// <para>Invariant: given the same logical input (a <see cref="SystemMtResult"/> + mrName),
/// both write paths must produce records whose persisted fields are field-by-field equal,
/// excluding the two auto-set fields (<see cref="SystemMtResultRecord.Id"/> minted by autoId
/// when callers leave it <c>Guid.Empty</c>; <see cref="SystemMtResultRecord.RunAt"/> set
/// to UtcNow when default). Any future change to one path that breaks this invariant
/// silently corrupts the WPF Execution History page's reads — this test fails it loudly.</para>
/// </summary>
public sealed class LegacyResultRecordParityTests : IDisposable
{
    private readonly List<string> _dbPaths = new();

    public void Dispose()
    {
        foreach (var path in _dbPaths)
        {
            if (File.Exists(path)) File.Delete(path);
            var log = path + "-log";
            if (File.Exists(log)) File.Delete(log);
        }
    }

    private string NewDbPath()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            "MetBenchLegacyParityTests",
            Guid.NewGuid().ToString("N") + ".db");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        _dbPaths.Add(path);
        return path;
    }

    [Fact]
    public async Task SaveAsync_string_result_and_SaveAsync_record_produce_field_equal_records()
    {
        const string mrName = "openmoc-pincell-nu-sigma-f";
        var input = MakeResult(
            inputGeneration: MakeInputGeneration(),
            inputSamples: SampleInputs());

        // Path A: legacy mirror
        var aPath = NewDbPath();
        using (var repo = new LiteDbSystemMtResultRepository($"Filename={aPath}"))
        {
            var idA = await repo.SaveAsync(mrName, input);
            var recordA = await repo.GetAsync(idA);
            Assert.NotNull(recordA);

            // Path B: pre-built record (the recorder's mirror path uses FromResult itself).
            var bPath = NewDbPath();
            using var repoB = new LiteDbSystemMtResultRepository($"Filename={bPath}");
            var built = SystemMtResultRecord.FromResult(mrName, input);
            // Leave Id empty + RunAt default so both paths exercise their auto-fill behavior;
            // we exclude those two from the parity assertion.
            var idB = await repoB.SaveAsync(built);
            var recordB = await repoB.GetAsync(idB);
            Assert.NotNull(recordB);

            AssertRecordsFieldEqual(recordA!, recordB!);
        }
    }

    [Fact]
    public async Task Parity_holds_with_input_generation_absent_and_no_samples()
    {
        const string mrName = "heat-equation-amplitude";
        var input = MakeResult(inputGeneration: null, inputSamples: null);

        var aPath = NewDbPath();
        using var repo = new LiteDbSystemMtResultRepository($"Filename={aPath}");
        var idA = await repo.SaveAsync(mrName, input);
        var recordA = await repo.GetAsync(idA);

        var bPath = NewDbPath();
        using var repoB = new LiteDbSystemMtResultRepository($"Filename={bPath}");
        var idB = await repoB.SaveAsync(SystemMtResultRecord.FromResult(mrName, input));
        var recordB = await repoB.GetAsync(idB);

        AssertRecordsFieldEqual(recordA!, recordB!);
    }

    [Fact]
    public async Task Parity_holds_when_assertion_fails_and_failure_reason_is_propagated()
    {
        const string mrName = "diffusion-mesh-richardson";
        var input = MakeResult(passed: false);

        var aPath = NewDbPath();
        using var repo = new LiteDbSystemMtResultRepository($"Filename={aPath}");
        var idA = await repo.SaveAsync(mrName, input);
        var recordA = await repo.GetAsync(idA);

        var bPath = NewDbPath();
        using var repoB = new LiteDbSystemMtResultRepository($"Filename={bPath}");
        var idB = await repoB.SaveAsync(SystemMtResultRecord.FromResult(mrName, input));
        var recordB = await repoB.GetAsync(idB);

        AssertRecordsFieldEqual(recordA!, recordB!);
        Assert.False(recordA!.Passed);
        Assert.Equal("MR violated", recordA.FailureReason);
    }

    private static void AssertRecordsFieldEqual(SystemMtResultRecord a, SystemMtResultRecord b)
    {
        // Excluded: Id (autoId-generated per repo) and RunAt (UtcNow-stamped at save time);
        // both are documented auto-set fields and would differ across two separate saves.
        Assert.Equal(a.MrName, b.MrName);
        Assert.Equal(a.AssertionName, b.AssertionName);
        Assert.Equal(a.ValueName, b.ValueName);
        Assert.Equal(a.SourceValue, b.SourceValue);
        Assert.Equal(a.FollowUpValue, b.FollowUpValue);
        Assert.Equal(a.Passed, b.Passed);
        Assert.Equal(a.FailureReason, b.FailureReason);
        Assert.Equal(a.SourceCaseName, b.SourceCaseName);
        Assert.Equal(a.FollowUpCaseName, b.FollowUpCaseName);
        Assert.Equal(a.SourceElapsed, b.SourceElapsed);
        Assert.Equal(a.FollowUpElapsed, b.FollowUpElapsed);
        Assert.Equal(a.SourceExitCode, b.SourceExitCode);
        Assert.Equal(a.FollowUpExitCode, b.FollowUpExitCode);
        Assert.Equal(a.SourceMetrics, b.SourceMetrics);
        Assert.Equal(a.FollowUpMetrics, b.FollowUpMetrics);
        Assert.Equal(a.TransformationName, b.TransformationName);
        Assert.Equal(a.TransformationParameters, b.TransformationParameters);
        Assert.Equal(a.InputGenerationSucceeded, b.InputGenerationSucceeded);
        Assert.Equal(a.InputGenerationLog, b.InputGenerationLog);
        Assert.Equal(a.InputSamples.Count, b.InputSamples.Count);
        for (var i = 0; i < a.InputSamples.Count; i++)
        {
            Assert.Equal(a.InputSamples[i].Name, b.InputSamples[i].Name);
            Assert.Equal(a.InputSamples[i].SourceValue, b.InputSamples[i].SourceValue);
            Assert.Equal(a.InputSamples[i].FollowUpValue, b.InputSamples[i].FollowUpValue);
        }
    }

    private static SystemMtResult MakeResult(
        bool passed = true,
        InputGenerationResult? inputGeneration = null,
        IReadOnlyList<InputSamplePoint>? inputSamples = null)
    {
        var sourceRun = new CliRunResult(
            "source", 0, "stdout-source", string.Empty,
            TimeSpan.FromSeconds(2.5), "/tmp/source/output.json", true, string.Empty);
        var followUpRun = new CliRunResult(
            "follow-up", 0, "stdout-followup", string.Empty,
            TimeSpan.FromSeconds(2.7), "/tmp/followup/output.json", true, string.Empty);
        var sourceOutput = new ParsedOutput(
            new Dictionary<string, double> { ["k_eff"] = 1.13, ["iterations"] = 553 },
            new Dictionary<string, string> { ["program"] = "openmoc" });
        var followUpOutput = new ParsedOutput(
            new Dictionary<string, double> { ["k_eff"] = 1.51, ["iterations"] = 464 },
            new Dictionary<string, string> { ["program"] = "openmoc" });
        var assertion = new SystemMtAssertionResult(
            "GreaterThan", "k_eff", 1.13, 1.51, passed,
            passed ? string.Empty : "MR violated");
        return new SystemMtResult(
            sourceRun, followUpRun, sourceOutput, followUpOutput, assertion,
            passed, passed ? string.Empty : "MR violated", inputGeneration, inputSamples);
    }

    private static InputGenerationResult MakeInputGeneration()
    {
        var transformation = new MrTransformation(
            "scale-nu-sigma-f",
            new Dictionary<string, string> { ["factor"] = "1.1" });
        return new InputGenerationResult(
            "/tmp/source/input.json",
            "/tmp/followup/input.json",
            transformation,
            true,
            "ok",
            string.Empty);
    }

    private static IReadOnlyList<InputSamplePoint> SampleInputs() => new List<InputSamplePoint>
    {
        new() { Name = "initial.x0", SourceValue = 1.0, FollowUpValue = 2.0 },
        new() { Name = "initial.v0", SourceValue = 0.5, FollowUpValue = 1.0 },
    };
}
