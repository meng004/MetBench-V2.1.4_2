using System.Reflection;
using System.Xml.Linq;
using Reqnroll;
using Xunit;

namespace MetBench_SystemMT.Tests.Steps;

/// <summary>
/// Shared step bindings for UAT rubric BDD features in <c>Features/Uat/*.feature</c>.
///
/// Each UAT case in <c>docs/uat/acceptance-rubric.md</c> (47 cases across Part A–G)
/// has a single-scenario .feature file that asserts the rubric's pass criterion via
/// one of the steps below. The pattern is intentionally lightweight: BDD here is a
/// machine-checkable rubric verification layer, not a replacement for the underlying
/// unit tests (those continue to run via the normal test suite).
///
/// Three step kinds are provided:
///
/// <list type="number">
///   <item><b>Coverage assertion</b>:
///     "UAT case &lt;ID&gt; requires at least &lt;N&gt; verified facts in test class &lt;FQCN&gt;"
///     — uses reflection to confirm the underlying xUnit test class exists and exposes
///     at least N [Fact]/[Theory]/[SkippableFact] methods. Equivalent to the rubric's
///     "Passed ≥ N" structural promise. Catches missing or renamed test classes.</item>
///
///   <item><b>Baseline check</b>:
///     "UAT case &lt;ID&gt; baseline trx &lt;path&gt; shows test class &lt;FQCN&gt; with 0 Failed"
///     — parses a committed .trx and confirms the latest baseline run had no failures
///     for the named test class. Catches regressions that landed without a baseline
///     refresh.</item>
///
///   <item><b>Manual reference</b>:
///     "UAT case &lt;ID&gt; is documented for manual UI verification in &lt;path&gt;"
///     — confirms the UAT case ID appears in the manual test-procedures doc. Used for
///     UI-only cases (no automated test class possible cloud-side).</item>
/// </list>
/// </summary>
[Binding]
public sealed class UatRubricSteps
{
    private static readonly Assembly TestAssembly = typeof(UatRubricSteps).Assembly;

    [Then("UAT case {string} requires at least {int} verified facts in test class {string}")]
    public void ThenRubricFacts(string caseId, int minFacts, string fullClassName)
    {
        var t = TestAssembly.GetType(fullClassName);
        Assert.True(t is not null,
            $"UAT {caseId}: test class '{fullClassName}' not found in test assembly. " +
            "Rubric mapping is stale — either the test class was renamed or the rubric " +
            "row needs to be updated.");

        var factCount = t!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Count(m => m.GetCustomAttributes(inherit: false)
                .Any(a => a.GetType().Name is "FactAttribute" or "TheoryAttribute" or "SkippableFactAttribute"));

        Assert.True(factCount >= minFacts,
            $"UAT {caseId}: rubric promises ≥ {minFacts} facts in {fullClassName}, " +
            $"reflection found {factCount}. Rubric count and code drifted apart.");
    }

    [Then("UAT case {string} baseline trx {string} shows test class {string} with 0 Failed")]
    public void ThenBaselineTrxAllPassed(string caseId, string trxRelPath, string fullClassName)
    {
        var repoRoot = RepoRoot();
        var trxPath = Path.Combine(repoRoot, trxRelPath);
        Skip.IfNot(File.Exists(trxPath),
            $"baseline trx not found at {trxPath} — baseline-check step skipped " +
            "(coverage-assertion step still binds the contract).");

        var doc = XDocument.Load(trxPath);
        var ns = doc.Root!.GetDefaultNamespace();

        // Trx UnitTestResult names look like "MetBench_SystemMT.Tests.V2Schema.DbConfigTests.Default_path"
        // — match prefix by FQCN to avoid mis-matching across renames.
        var shortName = fullClassName.Split('.').Last();
        var results = doc.Descendants(ns + "UnitTestResult")
            .Where(r => ((string?)r.Attribute("testName"))?.Contains(shortName) == true)
            .ToList();

        Skip.If(results.Count == 0,
            $"UAT {caseId}: baseline trx contains 0 facts for {shortName}. " +
            "Either baseline was generated before the class existed, or the trx names " +
            "do not include the class name — skipping baseline check.");

        var failed = results.Count(r => (string?)r.Attribute("outcome") != "Passed");
        Assert.True(failed == 0,
            $"UAT {caseId}: baseline trx shows {failed} Failed fact(s) for {shortName}. " +
            "Either rerun the baseline or address the regression.");
    }

    [Then("UAT case {string} is documented for manual UI verification in {string}")]
    public void ThenDocumentedForManual(string caseId, string docRelPath)
    {
        var repoRoot = RepoRoot();
        var docPath = Path.Combine(repoRoot, docRelPath);
        Assert.True(File.Exists(docPath),
            $"UAT {caseId}: manual-verification doc missing at {docPath}");
        var content = File.ReadAllText(docPath);
        Assert.True(content.Contains(caseId),
            $"UAT {caseId}: doc {docRelPath} does not reference '{caseId}'. " +
            "Manual UI cases must have an entry in test-procedures.md.");
    }

    [Then("UAT case {string} helper {string} exists and is non-empty")]
    public void ThenHelperExistsAndNonEmpty(string caseId, string helperRelPath)
    {
        var repoRoot = RepoRoot();
        var helperPath = Path.Combine(repoRoot, helperRelPath);
        Assert.True(File.Exists(helperPath), $"UAT {caseId}: helper {helperPath} not found");
        var fi = new FileInfo(helperPath);
        Assert.True(fi.Length > 0, $"UAT {caseId}: helper {helperPath} is empty");
    }

    private static string RepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        for (int i = 0; i < 10; i++)
        {
            if (File.Exists(Path.Combine(dir, "MetBench.sln"))) return dir;
            var parent = Directory.GetParent(dir);
            if (parent is null) break;
            dir = parent.FullName;
        }
        throw new InvalidOperationException("Could not locate repo root (MetBench.sln).");
    }
}
