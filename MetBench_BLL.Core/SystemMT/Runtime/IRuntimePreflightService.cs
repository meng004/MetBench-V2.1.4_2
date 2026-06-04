using System.Threading;
using System.Threading.Tasks;

namespace MetBench_BLL.SystemMT.Runtime;

public interface IRuntimePreflightService
{
    Task<RuntimePreflightResult> CheckAsync(
        RuntimeProfile profile,
        CancellationToken cancellationToken = default);
}
