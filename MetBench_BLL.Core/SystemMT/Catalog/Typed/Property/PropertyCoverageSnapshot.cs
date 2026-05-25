namespace MetBench_BLL.SystemMT.Catalog.Typed.Property;

public sealed record PropertyCoverageSnapshot(int MrCount, int PropertyCount)
{
    public static PropertyCoverageSnapshot Build(int mrCount, int propertyCount) => new(mrCount, propertyCount);
}
