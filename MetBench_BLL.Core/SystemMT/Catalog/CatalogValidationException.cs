namespace MetBench_BLL.SystemMT.Catalog;

public sealed class CatalogValidationException : System.Exception
{
    public CatalogValidationException(string message) : base(message) { }
    public CatalogValidationException(string message, System.Exception inner) : base(message, inner) { }
}
