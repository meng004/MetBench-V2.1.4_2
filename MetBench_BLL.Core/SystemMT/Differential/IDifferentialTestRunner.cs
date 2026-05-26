using System.Threading;
using System.Threading.Tasks;

namespace MetBench_BLL.SystemMT.Differential;

/// <summary>
/// Sanctioned API for the T1 §2.1 element 3 "same-equation cross-method differential" semantic:
/// run two MRs through <c>ISystemMtLauncher</c> and apply a deterministic agreement criterion
/// over the two <c>MrRunResult</c> objects.
/// </summary>
/// <remarks>
/// Not to be confused with the typed catalog's
/// <c>MetBench_BLL.SystemMT.Catalog.Typed.Runtime.CrossMethodComparisonKernel</c>: that
/// kernel operates on a single MR spec with two intra-MR roles (left/right method bindings
/// inside one predicate), which is a different shape from the cross-MR pair this runner
/// orchestrates. Both can coexist; they answer different questions.
/// </remarks>
public interface IDifferentialTestRunner
{
    Task<DifferentialRunResult> RunPairAsync(
        DifferentialRunRequest request,
        CancellationToken cancellationToken = default);
}
