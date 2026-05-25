using MetBench_BLL.SystemMT.Catalog.Typed.Runtime;
using MetBench_BLL.SystemMT.Catalog.Typed.Specs;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Catalog.Typed;

public sealed class FieldModelTests
{
    [Fact]
    public void FieldNormTolerance_requires_norm_kind()
    {
        var tol = new FieldNormToleranceSpec("L2", 1e-6, 1e-4);

        Assert.Equal("L2", tol.Norm);
    }

    [Fact]
    public void Field2DValue_keeps_dimensions()
    {
        var field = new Field2DValue(new[,]
        {
            { 1.0, 2.0 },
            { 3.0, 4.0 }
        });

        Assert.Equal(2, field.RowCount);
        Assert.Equal(2, field.ColumnCount);
    }

    [Fact]
    public void FieldPairing_roundtrips_kind()
    {
        FieldPairing pairing = new IdentityFieldPairing();

        Assert.IsType<IdentityFieldPairing>(pairing);
    }
}
