namespace MetBench_BLL.SystemMT.Persistence;

/// <summary>
/// Per-variable evidence triple captured during a System-MT pipeline run:
/// (source value, transformed value, output value) at a specific JSON-pointer path.
/// Values are serialized to JSON so the schema accommodates arbitrary shapes
/// (scalars, arrays, nested objects).
/// </summary>
public sealed class ExecutionSampleTrace
{
    /// <summary>Logical variable name (e.g. "amplitude", "k_eff", "phi_max").</summary>
    public string VariableName { get; set; } = string.Empty;

    /// <summary>JSON Pointer path into the SUT input / output structure (e.g. "/initial/amplitude").</summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>JSON-serialized source value (before the MR transformation).</summary>
    public string SourceValueJson { get; set; } = string.Empty;

    /// <summary>JSON-serialized transformed value (after the MR transformation; what the follow-up SUT actually received).</summary>
    public string TransformedValueJson { get; set; } = string.Empty;

    /// <summary>JSON-serialized output value extracted from the follow-up SUT run.</summary>
    public string OutputValueJson { get; set; } = string.Empty;
}
