namespace MetBench_SystemMT.Tests.SystemMT;

internal static class TestAssetPaths
{
    public static string TypedCatalogRoot() => Path.Combine(AssetRoot(), "SystemMT", "Catalog", "Typed");
    public static string TypedMrSample => Path.Combine(TypedCatalogRoot(), "samples", "mr-sample.yaml");
    public static string TypedPropertySample => Path.Combine(TypedCatalogRoot(), "samples", "property-sample.yaml");

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
