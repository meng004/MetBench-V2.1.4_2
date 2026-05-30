using MetBench_BLL.SystemMT.Transformations;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Transformations;

public sealed class RefineRayTracksTests
{
    [Fact]
    public void Apply_scales_num_azim_and_inverse_scales_spacing_atomically()
    {
        var source = new Dictionary<string, object?>
        {
            ["tracking"] = new Dictionary<string, object?>
            {
                ["num_azim"] = 16,
                ["azim_spacing_cm"] = 0.05,
                ["z_coord_cm"] = 0.0,
            },
        };

        var result = new RefineRayTracks().Apply(
            source,
            "/tracking",
            new Dictionary<string, string> { ["factor"] = "3" });

        var tracking = Assert.IsAssignableFrom<IReadOnlyDictionary<string, object?>>(result["tracking"]);
        Assert.Equal(48, tracking["num_azim"]);
        Assert.Equal(0.05 / 3.0, Assert.IsType<double>(tracking["azim_spacing_cm"]), 12);
        Assert.Equal(0.0, Assert.IsType<double>(tracking["z_coord_cm"]));
    }

    [Fact]
    public void Registry_resolves_RefineRayTracks()
    {
        Assert.IsType<RefineRayTracks>(TransformationRegistry.Get("RefineRayTracks"));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("not-a-number")]
    public void Apply_rejects_invalid_factor(string factor)
    {
        var source = new Dictionary<string, object?>
        {
            ["tracking"] = new Dictionary<string, object?>
            {
                ["num_azim"] = 16,
                ["azim_spacing_cm"] = 0.05,
            },
        };

        Assert.Throws<ArgumentException>(() => new RefineRayTracks().Apply(
            source,
            "/tracking",
            new Dictionary<string, string> { ["factor"] = factor }));
    }
}
