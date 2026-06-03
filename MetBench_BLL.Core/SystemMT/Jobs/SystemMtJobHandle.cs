namespace MetBench_BLL.SystemMT.Jobs;

/// <summary>SubmitAsync 的返回值：受理凭据，后续 polling 用 <see cref="JobId"/>。</summary>
public sealed record SystemMtJobHandle(Guid JobId, DateTime AcceptedAtUtc);
