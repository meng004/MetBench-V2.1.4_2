using System.Collections.Generic;
using MetBench_BLL.SystemMT.V12Catalog.Runtime;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.V12Catalog;

public sealed class V12RuntimeContractTests
{
    [Fact]
    public void VerificationContext_requires_validated_spec()
    {
        Assert.Throws<ArgumentException>(() => new VerificationContext(null!, new Dictionary<string, RoleOutput>()));
    }
}
