using System;
using System.Threading;
using System.Threading.Tasks;

namespace MetBench_BLL.SystemMT.Persistence;

/// <summary>
/// Persistence contract for <see cref="ExecutionEvidence"/> records.
/// Implementations live in <c>MetBench_DAL</c> (LiteDB) and in tests (in-memory fakes).
/// </summary>
public interface IExecutionEvidenceRepository
{
    Task SaveAsync(ExecutionEvidence evidence, CancellationToken cancellationToken = default);

    Task<ExecutionEvidence?> GetByExecutionAsync(Guid executionId, CancellationToken cancellationToken = default);
}
