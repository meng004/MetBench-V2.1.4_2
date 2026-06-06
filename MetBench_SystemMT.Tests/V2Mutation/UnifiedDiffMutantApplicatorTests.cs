using MetBench_BLL.Mutation;
using MetBench_Domain;
using Xunit;

namespace MetBench_SystemMT.Tests.V2Mutation;

/// <summary>
/// Pins the IMutantApplicator contract spelled out in
/// MetBench_BLL.Core/Mutation/IMutantApplicator.cs. T6 is currently a Prototype layer
/// (orchestration shell + StubCellRunner per the maturity assessment); these tests
/// guard the applicator infrastructure piece that future cellRunner integration depends
/// on, so when launcher-backed cellRunner work lands the diff side is already trusted.
/// </summary>
public sealed class UnifiedDiffMutantApplicatorTests : IDisposable
{
    private readonly string _root;

    public UnifiedDiffMutantApplicatorTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "MetBenchMutantApplicatorTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            try { Directory.Delete(_root, recursive: true); } catch { /* best-effort cleanup */ }
        }
    }

    [Fact]
    public async Task ApplyAsync_modifies_an_existing_file_per_hunk_at_the_expected_line()
    {
        var baseSut = MakeSutWith(("solver.py", "import math\nx = 1\ny = 2\nprint(x + y)\n"));
        var mutant = new Mutant
        {
            IdMutant = 42,
            AppliedDiff = "--- a/solver.py\n+++ b/solver.py\n@@ -2,1 +2,1 @@\n-x = 1\n+x = 99\n",
        };
        var applicator = new UnifiedDiffMutantApplicator();

        var patched = await applicator.ApplyAsync(mutant, baseSut, _root);

        var patchedFile = Path.Combine(patched, "solver.py");
        Assert.True(File.Exists(patchedFile));
        var lines = File.ReadAllLines(patchedFile);
        Assert.Equal("import math", lines[0]);
        Assert.Equal("x = 99", lines[1]);
        Assert.Equal("y = 2", lines[2]);
        Assert.Equal("print(x + y)", lines[3]);
        // Patched tree is a fresh copy; base SUT must remain untouched.
        Assert.Equal("x = 1", File.ReadAllLines(Path.Combine(baseSut, "solver.py"))[1]);
    }

    [Fact]
    public async Task ApplyAsync_supports_multiple_hunks_within_one_file()
    {
        var baseSut = MakeSutWith(("model.py", "a\nb\nc\nd\ne\nf\n"));
        var mutant = new Mutant
        {
            IdMutant = 7,
            AppliedDiff =
                "--- a/model.py\n" +
                "+++ b/model.py\n" +
                "@@ -2,1 +2,1 @@\n" +
                "-b\n" +
                "+B\n" +
                "@@ -5,1 +5,1 @@\n" +
                "-e\n" +
                "+E\n",
        };
        var applicator = new UnifiedDiffMutantApplicator();

        var patched = await applicator.ApplyAsync(mutant, baseSut, _root);

        var lines = File.ReadAllLines(Path.Combine(patched, "model.py"));
        Assert.Equal("a", lines[0]);
        Assert.Equal("B", lines[1]);
        Assert.Equal("c", lines[2]);
        Assert.Equal("d", lines[3]);
        Assert.Equal("E", lines[4]);
        Assert.Equal("f", lines[5]);
    }

    [Fact]
    public async Task ApplyAsync_throws_on_empty_diff_not_silently_returns_unmutated_tree()
    {
        var baseSut = MakeSutWith(("a.py", "x = 1\n"));
        var mutant = new Mutant { IdMutant = 1, AppliedDiff = string.Empty };
        var applicator = new UnifiedDiffMutantApplicator();

        var ex = await Assert.ThrowsAsync<MutationApplicationException>(() =>
            applicator.ApplyAsync(mutant, baseSut, _root));

        // Critical: an empty diff MUST throw, otherwise the campaign would record
        // every cell as 'missed' against an unmutated tree and poison kill-rate stats.
        Assert.Contains("empty AppliedDiff", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ApplyAsync_throws_when_context_does_not_match_base_tree()
    {
        var baseSut = MakeSutWith(("solver.py", "import math\nx = 1\ny = 2\n"));
        // Hunk claims to delete 'x = 100' but the base file has 'x = 1' — must fail-loud.
        var mutant = new Mutant
        {
            IdMutant = 99,
            AppliedDiff = "--- a/solver.py\n+++ b/solver.py\n@@ -2,1 +2,1 @@\n-x = 100\n+x = 99\n",
        };
        var applicator = new UnifiedDiffMutantApplicator();

        var ex = await Assert.ThrowsAsync<MutationApplicationException>(() =>
            applicator.ApplyAsync(mutant, baseSut, _root));

        Assert.Contains("delete mismatch", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ApplyAsync_throws_when_target_file_is_missing()
    {
        var baseSut = MakeSutWith(("solver.py", "x = 1\n"));
        var mutant = new Mutant
        {
            IdMutant = 5,
            AppliedDiff = "--- a/no-such.py\n+++ b/no-such.py\n@@ -1,1 +1,1 @@\n-x\n+y\n",
        };
        var applicator = new UnifiedDiffMutantApplicator();

        var ex = await Assert.ThrowsAsync<MutationApplicationException>(() =>
            applicator.ApplyAsync(mutant, baseSut, _root));

        Assert.Contains("not found", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ApplyAsync_throws_on_diff_without_any_recognized_file_sections()
    {
        var baseSut = MakeSutWith(("a.py", "x\n"));
        var mutant = new Mutant
        {
            IdMutant = 11,
            // Looks like a diff preamble but has no --- / +++ pair.
            AppliedDiff = "diff --git a/a.py b/a.py\nindex 0000000..0000000 100644\n",
        };
        var applicator = new UnifiedDiffMutantApplicator();

        var ex = await Assert.ThrowsAsync<MutationApplicationException>(() =>
            applicator.ApplyAsync(mutant, baseSut, _root));

        Assert.Contains("no recognized file sections", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ApplyAsync_strips_git_a_b_path_prefixes()
    {
        var baseSut = MakeSutWith(("nested/dir/x.py", "v = 1\n"));
        var mutant = new Mutant
        {
            IdMutant = 12,
            AppliedDiff = "--- a/nested/dir/x.py\n+++ b/nested/dir/x.py\n@@ -1,1 +1,1 @@\n-v = 1\n+v = 2\n",
        };
        var applicator = new UnifiedDiffMutantApplicator();

        var patched = await applicator.ApplyAsync(mutant, baseSut, _root);

        Assert.Equal("v = 2", File.ReadAllLines(Path.Combine(patched, "nested", "dir", "x.py"))[0]);
    }

    private string MakeSutWith(params (string RelativePath, string Content)[] files)
    {
        var sutRoot = Path.Combine(_root, "base-sut-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(sutRoot);
        foreach (var (rel, content) in files)
        {
            var path = Path.Combine(sutRoot, rel.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, content);
        }
        return sutRoot;
    }
}
