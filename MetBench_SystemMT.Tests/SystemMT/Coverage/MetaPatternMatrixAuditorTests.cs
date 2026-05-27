using MetBench_BLL.SystemMT.Assertions;
using MetBench_BLL.SystemMT.Catalog;
using MetBench_BLL.SystemMT.Coverage;
using MetBench_BLL.SystemMT.Launcher;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Coverage;

/// <summary>
/// Pins the structural contract of <see cref="MetaPatternMatrixAuditor"/> using
/// both a hand-crafted in-memory provider and the live manifest catalog at
/// <c>TestAssets/</c> (the same path the launcher reads in CI).
/// </summary>
public sealed class MetaPatternMatrixAuditorTests
{
    private sealed class InMemoryProvider : IMrCatalogProvider
    {
        private readonly IReadOnlyList<MrCatalogEntry> _entries;
        public InMemoryProvider(IReadOnlyList<MrCatalogEntry> entries) => _entries = entries;
        public string SourceDescription => "in-memory";
        public IReadOnlyList<MrCatalogEntry> Load() => _entries;
    }

    private static MrCatalogEntry Entry(string mrId, string equationKey, string metaPattern) =>
        new(
            Mr: new MrSummary(
                Id: mrId,
                DisplayName: mrId,
                SutName: "sut-" + mrId,
                TransformationName: "ScaleField",
                AssertionName: "GreaterThan",
                ValueName: "y",
                DefaultParameters: new Dictionary<string, string>(),
                Description: "test",
                MrFamily: "test"),
            SampleCaseRelativePath: "sample.json",
            RunnerScriptPath: "runner.py",
            InputAdapterScriptPath: "in.py",
            OutputAdapterScriptPath: "out.py",
            PythonExecutable: "python3",
            WorkRootName: "wr",
            Timeout: TimeSpan.FromSeconds(30),
            InputParserScriptPath: "in_parser.py",
            OutputParserScriptPath: "out_parser.py",
            TransformSteps: new[] { new MrCatalogTransformStep("ScaleField", "/x") },
            AssertionTypeCode: AssertionTypeCodes.Greater,
            EquationKey: equationKey,
            Tolerance: null)
        {
            MetaPattern = metaPattern,
        };

    private static IMrCatalogProvider Live()
        => new ManifestMrCatalogProvider(new LauncherOptions(
            SutRoot: TestAssetPaths.AssetRoot(),
            SystemPython: TestAssetPaths.PythonExecutable(),
            OpenMocPython: TestAssetPaths.PythonExecutable()));

    [Fact]
    public void Audit_throws_when_provider_null()
    {
        Assert.Throws<ArgumentNullException>(() => MetaPatternMatrixAuditor.Audit(null!));
    }

    [Fact]
    public void Audit_empty_catalog_yields_zero_cells_zero_gaps()
    {
        var matrix = MetaPatternMatrixAuditor.Audit(new InMemoryProvider(Array.Empty<MrCatalogEntry>()));

        Assert.Empty(matrix.Cells);
        Assert.Empty(matrix.Gaps);
        Assert.Empty(matrix.Equations);
        Assert.Equal(new[] { "Mono", "Inv", "Conv" }, matrix.MetaPatterns);
    }

    [Fact]
    public void Audit_single_mr_yields_one_cell_and_two_gaps()
    {
        var provider = new InMemoryProvider(new[]
        {
            Entry("mr-a", equationKey: "Boltzmann", metaPattern: "Mono"),
        });

        var matrix = MetaPatternMatrixAuditor.Audit(provider);

        Assert.Single(matrix.Cells);
        Assert.Equal("Boltzmann", matrix.Cells[0].EquationKey);
        Assert.Equal("Mono", matrix.Cells[0].MetaPattern);
        Assert.Equal(new[] { "mr-a" }, matrix.Cells[0].MrIds);
        Assert.Equal(
            new[] { ("Boltzmann", "Conv"), ("Boltzmann", "Inv") },
            matrix.Gaps.OrderBy(g => g.MetaPattern, StringComparer.Ordinal).ToArray());
    }

    [Fact]
    public void Audit_groups_multiple_mrs_into_single_cell_sorted()
    {
        var provider = new InMemoryProvider(new[]
        {
            Entry("mr-b", "Boltzmann", "Mono"),
            Entry("mr-a", "Boltzmann", "Mono"),
            Entry("mr-c", "Boltzmann", "Mono"),
        });

        var matrix = MetaPatternMatrixAuditor.Audit(provider);

        var cell = Assert.Single(matrix.Cells);
        Assert.Equal(new[] { "mr-a", "mr-b", "mr-c" }, cell.MrIds);
    }

    [Fact]
    public void Audit_distinct_equations_produce_independent_cells_and_gap_axes()
    {
        var provider = new InMemoryProvider(new[]
        {
            Entry("a", "Boltzmann", "Mono"),
            Entry("b", "Boltzmann", "Inv"),
            Entry("c", "Poisson", "Conv"),
        });

        var matrix = MetaPatternMatrixAuditor.Audit(provider);

        // Cells: ordered by (equation, meta-pattern)
        Assert.Equal(3, matrix.Cells.Count);
        // 2 equations × 3 patterns = 6 expected slots; 3 filled → 3 gaps
        Assert.Equal(3, matrix.Gaps.Count);
        Assert.Contains(("Boltzmann", "Conv"), matrix.Gaps);
        Assert.Contains(("Poisson", "Mono"), matrix.Gaps);
        Assert.Contains(("Poisson", "Inv"), matrix.Gaps);
        Assert.Equal(new[] { "Boltzmann", "Poisson" }, matrix.Equations);
    }

    [Fact]
    public void Audit_unclassified_mr_does_not_count_as_filled_canonical_cell()
    {
        // An MR whose manifest meta_pattern is empty is grouped under the
        // "Unclassified" bucket, NOT one of {Mono, Inv, Conv}. So the canonical
        // Cartesian product still treats every (equation, canonical-pattern)
        // pair as a gap. (Goal: surface migration debt, not hide it.)
        var provider = new InMemoryProvider(new[]
        {
            Entry("mr-unc", "Boltzmann", metaPattern: string.Empty),
        });

        var matrix = MetaPatternMatrixAuditor.Audit(provider);

        var cell = Assert.Single(matrix.Cells);
        Assert.Equal("Unclassified", cell.MetaPattern);
        Assert.Equal(3, matrix.Gaps.Count);
        Assert.Equal(
            new[] { ("Boltzmann", "Conv"), ("Boltzmann", "Inv"), ("Boltzmann", "Mono") },
            matrix.Gaps.OrderBy(g => g.MetaPattern, StringComparer.Ordinal).ToArray());
    }

    [Fact]
    public void Audit_gaps_set_disjoint_from_filled_cells()
    {
        var provider = new InMemoryProvider(new[]
        {
            Entry("a", "Boltzmann", "Mono"),
            Entry("b", "Boltzmann", "Conv"),
            Entry("c", "Diffusion", "Mono"),
            Entry("d", "Diffusion", "Inv"),
        });

        var matrix = MetaPatternMatrixAuditor.Audit(provider);

        var filledKeys = matrix.Cells.Select(c => (c.EquationKey, c.MetaPattern)).ToHashSet();
        foreach (var gap in matrix.Gaps)
        {
            Assert.DoesNotContain(gap, filledKeys);
        }
    }

    [Fact]
    public void Audit_each_mr_appears_in_exactly_one_cell()
    {
        var provider = new InMemoryProvider(new[]
        {
            Entry("a", "Boltzmann", "Mono"),
            Entry("b", "Boltzmann", "Inv"),
            Entry("c", "Diffusion", "Mono"),
            Entry("d", "Diffusion", "Inv"),
            Entry("e", "Diffusion", "Conv"),
        });

        var matrix = MetaPatternMatrixAuditor.Audit(provider);

        var mrIdsInCells = matrix.Cells.SelectMany(c => c.MrIds).ToList();
        Assert.Equal(5, mrIdsInCells.Count);
        Assert.Equal(5, mrIdsInCells.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void Audit_live_manifest_catalog_classifies_every_mr_into_some_cell()
    {
        // Live system-test: the auditor must succeed on the production manifest
        // catalog under TestAssets/ without throwing, and the sum of cell sizes
        // must equal the launcher MR count (no MR silently dropped).
        var provider = Live();
        var totalMrCount = provider.Load().Count;

        var matrix = MetaPatternMatrixAuditor.Audit(provider);

        var sumOverCells = matrix.Cells.Sum(c => c.MrIds.Count);
        Assert.Equal(totalMrCount, sumOverCells);
    }

    [Fact]
    public void Audit_live_manifest_catalog_has_no_unclassified_mrs()
    {
        // Regression guard: every MR in the production catalog carries a
        // manifest meta_pattern. If this fact ever fails, the manifest grew an
        // MR without setting meta_pattern — fix the manifest, do not loosen
        // this assertion.
        var matrix = MetaPatternMatrixAuditor.Audit(Live());

        var unclassified = matrix.Cells
            .Where(c => c.MetaPattern == MetaPatternMatrixAuditor.UnclassifiedMetaPattern)
            .SelectMany(c => c.MrIds)
            .ToList();
        Assert.Empty(unclassified);
    }

    [Fact]
    public void Audit_hardcoded_and_manifest_providers_produce_identical_matrices()
    {
        // PR-T2-T3 review-fix: the auditor's contract is to produce the same
        // matrix regardless of which IMrCatalogProvider implementation it reads
        // from. Before the fix, HardcodedMrCatalogProvider routed entries
        // through MrCatalogEntry.FromBlueprint which did not preserve MetaPattern,
        // so every row landed in the Unclassified bucket and the gap set was
        // wrong. After the fix, FromBlueprint derives MetaPattern from the
        // MrFamily convention; this fact pins parity end-to-end.
        var options = new LauncherOptions(
            SutRoot: TestAssetPaths.AssetRoot(),
            SystemPython: TestAssetPaths.PythonExecutable(),
            OpenMocPython: TestAssetPaths.PythonExecutable());
#pragma warning disable CS0618 // HardcodedMrCatalogProvider is [Obsolete] — intentional parity test target until removal.
        var hardcodedMatrix = MetaPatternMatrixAuditor.Audit(new HardcodedMrCatalogProvider(options));
#pragma warning restore CS0618
        var manifestMatrix = MetaPatternMatrixAuditor.Audit(new ManifestMrCatalogProvider(options));

        Assert.Equal(manifestMatrix.Cells.Count, hardcodedMatrix.Cells.Count);
        Assert.Equal(manifestMatrix.Gaps.Count, hardcodedMatrix.Gaps.Count);
        for (int i = 0; i < manifestMatrix.Cells.Count; i++)
        {
            var h = hardcodedMatrix.Cells[i];
            var m = manifestMatrix.Cells[i];
            Assert.Equal(m.EquationKey, h.EquationKey);
            Assert.Equal(m.MetaPattern, h.MetaPattern);
            Assert.Equal(m.MrIds, h.MrIds);
        }
    }
}
