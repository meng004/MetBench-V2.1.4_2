using System;

namespace MetBench_BLL.SystemMT.V12Catalog.Lint;

public sealed class V12CatalogLintException : Exception
{
    public V12CatalogLintException(string message) : base(message)
    {
    }
}
