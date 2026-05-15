using MetBench_BLL.SystemMT.ParameterMapping;
using Xunit;

namespace MetBench_SystemMT.Tests.V2Transformations;

/// <summary>
/// TDD 验证 P3.2 — 三个 PathSyntax resolver 的 get/set/exists 行为
/// + Factory 分派 + 错误路径。
/// </summary>
public sealed class FieldPathResolverTests
{
    // =============== JsonPointerResolver ===============

    [Fact]
    public void JsonPointer_get_with_slash_path()
    {
        var data = new Dictionary<string, object?>
        {
            ["materials"] = new Dictionary<string, object?>
            {
                ["fuel"] = new Dictionary<string, object?>
                {
                    ["temperature_kelvin"] = 600.0
                }
            }
        };
        var r = new JsonPointerResolver();
        Assert.Equal(600.0, r.Get(data, "/materials/fuel/temperature_kelvin"));
    }

    [Fact]
    public void JsonPointer_get_with_dot_path()
    {
        var data = new Dictionary<string, object?>
        {
            ["materials"] = new Dictionary<string, object?>
            {
                ["fuel"] = new Dictionary<string, object?>
                {
                    ["temperature_kelvin"] = 600.0
                }
            }
        };
        var r = new JsonPointerResolver();
        Assert.Equal(600.0, r.Get(data, "materials.fuel.temperature_kelvin"));
    }

    [Fact]
    public void JsonPointer_get_array_element()
    {
        var data = new Dictionary<string, object?>
        {
            ["materials"] = new Dictionary<string, object?>
            {
                ["fuel"] = new Dictionary<string, object?>
                {
                    ["sigma_t"] = new List<object?> { 0.222, 0.833 }
                }
            }
        };
        var r = new JsonPointerResolver();
        Assert.Equal(0.222, r.Get(data, "materials.fuel.sigma_t[0]"));
        Assert.Equal(0.833, r.Get(data, "materials/fuel/sigma_t/1"));
    }

    [Fact]
    public void JsonPointer_set_copy_on_write_does_not_mutate_source()
    {
        var data = new Dictionary<string, object?>
        {
            ["materials"] = new Dictionary<string, object?>
            {
                ["fuel"] = new Dictionary<string, object?>
                {
                    ["temperature_kelvin"] = 600.0
                }
            }
        };
        var r = new JsonPointerResolver();
        var updated = r.Set(data, "materials.fuel.temperature_kelvin", 900.0);
        Assert.Equal(900.0, r.Get(updated, "materials.fuel.temperature_kelvin"));
        // 源不变
        Assert.Equal(600.0, r.Get(data, "materials.fuel.temperature_kelvin"));
    }

    [Fact]
    public void JsonPointer_get_missing_path_throws()
    {
        var data = new Dictionary<string, object?>
        {
            ["materials"] = new Dictionary<string, object?>()
        };
        var r = new JsonPointerResolver();
        Assert.Throws<FieldPathNotFoundException>(() =>
            r.Get(data, "materials.fuel.temperature_kelvin"));
    }

    [Fact]
    public void JsonPointer_Exists_reports_correctly()
    {
        var data = new Dictionary<string, object?>
        {
            ["x"] = new Dictionary<string, object?> { ["y"] = 42 }
        };
        var r = new JsonPointerResolver();
        Assert.True(r.Exists(data, "x.y"));
        Assert.False(r.Exists(data, "x.z"));
        Assert.False(r.Exists(data, "missing"));
    }

    // =============== McnpCardResolver ===============

    [Fact]
    public void Mcnp_path_translation()
    {
        Assert.Equal("/cards/m1/tmp",
            McnpCardResolver.ToJsonPointer("card:m1::tmp"));
    }

    [Fact]
    public void Mcnp_get_set_through_jsonpointer_delegation()
    {
        var data = new Dictionary<string, object?>
        {
            ["cards"] = new Dictionary<string, object?>
            {
                ["m1"] = new Dictionary<string, object?>
                {
                    ["tmp"] = 600.0
                }
            }
        };
        var r = new McnpCardResolver();
        Assert.Equal(600.0, r.Get(data, "card:m1::tmp"));
        var updated = r.Set(data, "card:m1::tmp", 900.0);
        Assert.Equal(900.0, r.Get(updated, "card:m1::tmp"));
    }

    [Fact]
    public void Mcnp_invalid_format_throws()
    {
        var data = new Dictionary<string, object?>();
        var r = new McnpCardResolver();
        Assert.Throws<FormatException>(() => r.Get(data, "no-prefix"));
        Assert.Throws<FormatException>(() => r.Get(data, "card:no-double-colon"));
    }

    // =============== NamelistKeyResolver ===============

    [Fact]
    public void Namelist_path_translation()
    {
        Assert.Equal("/namelists/material/T",
            NamelistKeyResolver.ToJsonPointer("&material/T"));
    }

    [Fact]
    public void Namelist_get_set_through_jsonpointer_delegation()
    {
        var data = new Dictionary<string, object?>
        {
            ["namelists"] = new Dictionary<string, object?>
            {
                ["material"] = new Dictionary<string, object?>
                {
                    ["T"] = 600.0
                }
            }
        };
        var r = new NamelistKeyResolver();
        Assert.Equal(600.0, r.Get(data, "&material/T"));
        var updated = r.Set(data, "&material/T", 900.0);
        Assert.Equal(900.0, r.Get(updated, "&material/T"));
    }

    [Fact]
    public void Namelist_invalid_format_throws()
    {
        var data = new Dictionary<string, object?>();
        var r = new NamelistKeyResolver();
        Assert.Throws<FormatException>(() => r.Get(data, "no-ampersand"));
        Assert.Throws<FormatException>(() => r.Get(data, "&no-slash"));
    }

    // =============== Factory ===============

    [Fact]
    public void Factory_dispatches_by_path_syntax()
    {
        Assert.IsType<JsonPointerResolver>(FieldPathResolverFactory.For("json-pointer"));
        Assert.IsType<McnpCardResolver>(FieldPathResolverFactory.For("mcnp-card"));
        Assert.IsType<NamelistKeyResolver>(FieldPathResolverFactory.For("namelist-key"));
    }

    [Fact]
    public void Factory_rejects_unknown_syntax()
    {
        Assert.Throws<NotSupportedException>(() =>
            FieldPathResolverFactory.For("unknown-syntax"));
    }

    [Fact]
    public void Factory_lists_three_supported_syntaxes()
    {
        Assert.Equal(3, FieldPathResolverFactory.SupportedSyntaxes.Count);
        Assert.Contains("json-pointer", FieldPathResolverFactory.SupportedSyntaxes);
        Assert.Contains("mcnp-card", FieldPathResolverFactory.SupportedSyntaxes);
        Assert.Contains("namelist-key", FieldPathResolverFactory.SupportedSyntaxes);
    }
}
