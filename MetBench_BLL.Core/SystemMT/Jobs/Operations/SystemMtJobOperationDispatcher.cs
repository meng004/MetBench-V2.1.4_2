namespace MetBench_BLL.SystemMT.Jobs;

public sealed class SystemMtJobOperationDispatcher : ISystemMtJobOperationDispatcher
{
    private readonly IReadOnlyDictionary<SystemMtJobKind, ISystemMtJobOperationHandler> _handlers;

    public SystemMtJobOperationDispatcher(IEnumerable<ISystemMtJobOperationHandler> handlers)
    {
        if (handlers is null)
            throw new ArgumentNullException(nameof(handlers));

        _handlers = handlers.ToDictionary(handler => handler.Kind);
    }

    public Task<JobExecutionOutcome> ExecuteAsync(
        Guid jobId,
        SystemMtJobRecord record,
        IProgress<SystemMtJobProgress>? progress,
        CancellationToken cancellationToken)
    {
        if (!_handlers.TryGetValue(record.Kind, out var handler))
        {
            return Task.FromResult(new JobExecutionOutcome(
                SystemMtJobState.Failed,
                record.SutName,
                Result: null,
                FailureReason: $"No operation handler is registered for job kind {record.Kind}."));
        }

        return handler.ExecuteAsync(jobId, record, progress, cancellationToken);
    }
}
