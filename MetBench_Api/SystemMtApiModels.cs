namespace MetBench_Api;

public sealed record SystemMtSubmitRunRequest(
    string MrId,
    IReadOnlyDictionary<string, string>? ParameterOverrides = null);

public sealed record SystemMtJobReceiptResponse(
    Guid JobId,
    DateTime AcceptedAtUtc);

public sealed record SystemMtApiError(
    string Code,
    string Message);
