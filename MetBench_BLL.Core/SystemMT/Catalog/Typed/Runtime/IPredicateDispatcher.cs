using MetBench_BLL.SystemMT.Catalog.Typed.Specs;

namespace MetBench_BLL.SystemMT.Catalog.Typed.Runtime;

public interface IPredicateDispatcher
{
    VerificationResult Dispatch(PredicateSpec predicate, VerificationContext context);
}
