using System;

namespace MetBench_BLL.SystemMT.Catalog.Typed.Schema;

public sealed class TypedCatalogSchemaException : Exception
{
    public TypedCatalogSchemaException(string message) : base(message)
    {
    }
}
