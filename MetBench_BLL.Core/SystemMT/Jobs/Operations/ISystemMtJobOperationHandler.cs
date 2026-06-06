namespace MetBench_BLL.SystemMT.Jobs;

public interface ISystemMtJobOperationHandler : ISystemMtJobOperationDispatcher
{
    SystemMtJobKind Kind { get; }
}
