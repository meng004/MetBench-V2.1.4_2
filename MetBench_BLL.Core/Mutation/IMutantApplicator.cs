using MetBench_Domain;

namespace MetBench_BLL.Mutation;

/// <summary>
/// Applies a stored <see cref="Mutant.AppliedDiff"/> to a working copy of a SUT so the
/// mutated source can be executed by <c>ISystemMtLauncher</c> against the unmutated MR
/// suite. This is the missing infrastructure piece between
/// <see cref="MutationCampaignService"/>'s orchestration shell and a real (non-stub)
/// <see cref="MutationCellRunner"/>.
///
/// <para>
/// Today, T6 (mutation) ships as a Prototype: the campaign engine
/// (<see cref="MutationCampaignService"/>) is real and unit-tested, but no production
/// applicator exists, and WPF wires <c>StubCellRunner</c> — a hash-based simulator that
/// never touches a real SUT. See
/// <c>docs/superpowers/specs/2026-06-06-metbench-maturity-assessment.md</c> Top risk #4
/// and the maturity remediation plan §5 for the deferred work that turns T6 into a
/// Functional layer (real applicator implementation, launcher integration with a
/// per-run SUT-root override, and a real cellRunner that calls
/// <c>ISystemMtLauncher.RunAsync</c> against the materialized patched tree).
/// </para>
/// </summary>
/// <remarks>
/// Contract:
/// <list type="bullet">
///   <item>
///     <description>
///     <see cref="ApplyAsync"/> copies the SUT at <paramref name="baseSutRoot"/> to a
///     fresh working tree under <paramref name="workspaceRoot"/>, then applies
///     <paramref name="mutant"/>'s <see cref="Mutant.AppliedDiff"/> to it, and returns
///     the absolute path to the patched working tree. The caller owns cleanup of the
///     returned directory.
///     </description>
///   </item>
///   <item>
///     <description>
///     Failure to parse or apply the diff must throw <see cref="MutationApplicationException"/>
///     with a precise message — the caller surfaces it as a <c>"error"</c> cell outcome
///     (CLAUDE.md §6 explicit error). Returning silently with an unmutated tree is
///     forbidden because it would produce a false "missed" outcome and poison kill-rate
///     statistics.
///     </description>
///   </item>
///   <item>
///     <description>
///     An empty <see cref="Mutant.AppliedDiff"/> is a configuration bug, not a no-op:
///     implementations must throw <see cref="MutationApplicationException"/>.
///     </description>
///   </item>
/// </list>
/// </remarks>
public interface IMutantApplicator
{
    /// <summary>
    /// Materialize a patched copy of <paramref name="baseSutRoot"/> under
    /// <paramref name="workspaceRoot"/> with <paramref name="mutant"/>'s diff applied,
    /// and return the absolute path to the patched tree.
    /// </summary>
    Task<string> ApplyAsync(
        Mutant mutant,
        string baseSutRoot,
        string workspaceRoot,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Thrown when an <see cref="IMutantApplicator"/> cannot produce a patched working tree:
/// empty diff, malformed diff, target file missing, hunk context mismatch, IO error.
/// </summary>
public sealed class MutationApplicationException : Exception
{
    public MutationApplicationException(string message) : base(message) { }
    public MutationApplicationException(string message, Exception innerException)
        : base(message, innerException) { }
}
