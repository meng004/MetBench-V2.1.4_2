using MetBench_BLL.SystemMT;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT;

public sealed class MrTransformationTests
{
    [Fact]
    public void Constructor_rejects_empty_name()
    {
        var error = Assert.Throws<ArgumentException>(() =>
            new MrTransformation("", new Dictionary<string, string>()));

        Assert.Contains("Name", error.Message);
    }

    [Fact]
    public void Constructor_copies_parameters_defensively()
    {
        var source = new Dictionary<string, string> { ["multiplier"] = "2" };
        var transformation = new MrTransformation("ScalarMultiply", source);
        source["multiplier"] = "9";

        Assert.Equal("2", transformation.Parameters["multiplier"]);
    }

    [Fact]
    public void Parameters_are_read_only()
    {
        var transformation = new MrTransformation(
            "ScalarMultiply",
            new Dictionary<string, string> { ["multiplier"] = "2" });

        Assert.IsAssignableFrom<IReadOnlyDictionary<string, string>>(transformation.Parameters);
    }
}
