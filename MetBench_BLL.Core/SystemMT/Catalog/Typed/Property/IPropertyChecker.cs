using MetBench_BLL.SystemMT.Catalog.Typed.Specs;

namespace MetBench_BLL.SystemMT.Catalog.Typed.Property;

public interface IPropertyChecker
{
    PropertyResult Check(PropertySpec spec, PropertyVerificationContext context);
}
