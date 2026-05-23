namespace MetBench_BLL.SystemMT;

/// <summary>
/// One input variable of a System-MT run, paired across the source and
/// follow-up cases: the value the SUT was fed in the source run and the value
/// it was fed in the (transformed) follow-up run. Captured so a run record
/// holds the "source input → transformed input" half of the regression triple
/// (the output half lives in the record's metric dictionaries).
/// </summary>
public sealed class InputSamplePoint
{
    /// <summary>Dotted/indexed path of the input leaf, e.g. <c>initial.N_A</c>.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Value fed to the SUT in the source run.</summary>
    public double SourceValue { get; set; }

    /// <summary>Value fed to the SUT in the follow-up (transformed) run.</summary>
    public double FollowUpValue { get; set; }
}
