namespace MetBench_BLL.Core.SystemMT.ImportExport.Put;

public sealed class SutImportValidationException : Exception
{
    public SutImportValidationException(string message)
        : base(message)
    {
    }
}
