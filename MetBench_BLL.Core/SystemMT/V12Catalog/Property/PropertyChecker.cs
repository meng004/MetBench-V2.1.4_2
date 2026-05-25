using System;
using System.Linq;
using MetBench_BLL.SystemMT.V12Catalog.Specs;

namespace MetBench_BLL.SystemMT.V12Catalog.Property;

public sealed class PropertyChecker : IPropertyChecker
{
    private readonly BoundPropertyChecker _bound = new();
    private readonly ShapePropertyChecker _shape = new();

    public PropertyResult Check(PropertySpec spec, PropertyVerificationContext context)
    {
        if (spec.Assertions.All(a => a is BoundPropertyPredicate))
        {
            return _bound.Check(spec, context);
        }

        if (spec.Assertions.All(a => a is ShapePropertyPredicate))
        {
            return _shape.Check(spec, context);
        }

        return new PropertyResult(spec.PropertyId, PropertyStatus.InvalidSpec, Array.Empty<PropertyPredicateResult>(), "Mixed property predicate kinds are not supported yet.");
    }
}
