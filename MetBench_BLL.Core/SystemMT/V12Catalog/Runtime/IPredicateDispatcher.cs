using MetBench_BLL.SystemMT.V12Catalog.Specs;

namespace MetBench_BLL.SystemMT.V12Catalog.Runtime;

public interface IPredicateDispatcher
{
    VerificationResult Dispatch(PredicateSpec predicate, VerificationContext context);
}
