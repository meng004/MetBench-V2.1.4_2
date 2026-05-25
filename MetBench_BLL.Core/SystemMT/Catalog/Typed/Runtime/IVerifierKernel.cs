namespace MetBench_BLL.SystemMT.Catalog.Typed.Runtime;

public interface IVerifierKernel<in TPredicate>
{
    VerificationResult Evaluate(TPredicate predicate, VerificationContext context);
}
