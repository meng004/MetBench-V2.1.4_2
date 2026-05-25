using MetBench_BLL.SystemMT.V12Catalog.Specs;

namespace MetBench_BLL.SystemMT.V12Catalog.Property;

public interface IPropertyChecker
{
    PropertyResult Check(PropertySpec spec, PropertyVerificationContext context);
}
