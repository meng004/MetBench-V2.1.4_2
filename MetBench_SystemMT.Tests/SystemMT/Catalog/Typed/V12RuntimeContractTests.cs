using System.Collections.Generic;
using MetBench_BLL.SystemMT.Catalog.Typed.Runtime;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Catalog.Typed;

public sealed class V12RuntimeContractTests
{
    [Fact]
    public void VerificationContext_requires_validated_spec()
    {
        Assert.Throws<ArgumentException>(() => new VerificationContext(null!, new Dictionary<string, RoleOutput>()));
    }
}
