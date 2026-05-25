using MetBench_BLL.SystemMT.Catalog.Typed.Specs;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Catalog.Typed;

public sealed class ReferenceRoleModelTests
{
    [Fact]
    public void ErrorMonotonic_requires_ordered_roles_and_reference()
    {
        var predicate = new ErrorMonotonicPredicate(
            "p1",
            new[] { "coarse", "fine" },
            "reference",
            "k_eff",
            NormKind.Absolute);

        Assert.Equal("reference", predicate.ReferenceRole);
        Assert.Equal(new[] { "coarse", "fine" }, predicate.OrderedRoles);
    }
}
