using System.Linq;
using MetBench_BLL.SystemMT.Catalog.Typed.Validation;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Catalog.Typed;

public sealed class V12ValidationContractTests
{
    [Fact]
    public void ValidationResult_invalid_contains_errors()
    {
        var result = ValidationResult.Invalid(new ValidationError("roles.baseline", "Missing role"));

        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Equal("roles.baseline", result.Errors.Single().Path);
    }

    [Fact]
    public void ValidationResult_valid_is_empty()
    {
        var result = ValidationResult.Valid;

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }
}
