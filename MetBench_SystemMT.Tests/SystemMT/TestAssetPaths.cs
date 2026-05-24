namespace MetBench_SystemMT.Tests.SystemMT;

internal static class TestAssetPaths
{
    public static string V12CatalogRoot() => Path.Combine(AssetRoot(), "V12Catalog");
    public static string V12MrSample => Path.Combine(V12CatalogRoot(), "samples", "mr-sample.yaml");
    public static string V12PropertySample => Path.Combine(V12CatalogRoot(), "samples", "property-sample.yaml");

    public static string AssetRoot()
    {
        return Path.Combine(AppContext.BaseDirectory, "TestAssets");
    }

    public static string PythonExecutable()
    {
        var configured = Environment.GetEnvironmentVariable("METBENCH_TEST_PYTHON");
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured;
        }

        return OperatingSystem.IsWindows() ? "python" : "python3";
    }
}
