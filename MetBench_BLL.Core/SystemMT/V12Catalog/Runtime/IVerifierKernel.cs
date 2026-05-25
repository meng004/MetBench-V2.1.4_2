namespace MetBench_BLL.SystemMT.V12Catalog.Runtime;

public interface IVerifierKernel<in TPredicate>
{
    VerificationResult Evaluate(TPredicate predicate, VerificationContext context);
}
