using System;

namespace MetBench_BLL.SystemMT.V12Catalog.Schema;

public sealed class V12CatalogSchemaException : Exception
{
    public V12CatalogSchemaException(string message) : base(message)
    {
    }
}
