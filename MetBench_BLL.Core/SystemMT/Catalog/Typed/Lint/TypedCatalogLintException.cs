using System;

namespace MetBench_BLL.SystemMT.Catalog.Typed.Lint;

public sealed class TypedCatalogLintException : Exception
{
    public TypedCatalogLintException(string message) : base(message)
    {
    }
}
