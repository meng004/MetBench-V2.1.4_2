namespace MetBench_BLL.SystemMT.Differential;

/// <summary>
/// Outcome of a differential run pair. Total enum: every input the runner can see maps to
/// exactly one of these three values.
/// </summary>
public enum DifferentialAgreementStatus
{
    /// <summary>The two results satisfied the configured criterion.</summary>
    Agree = 0,

    /// <summary>The two results did not satisfy the configured criterion.</summary>
    Disagree = 1,

    /// <summary>
    /// The runner could not evaluate the criterion (e.g. metric name mismatch, NaN / ±∞
    /// values, zero source under a ratio criterion, negative tolerance). The reason field
    /// names which precondition failed.
    /// </summary>
    Inconclusive = 2,
}
