namespace MetBench_BLL
{
    public interface IDataRecord
    {
        bool Validate(IEnumerable<ColumnDefinition> columnDefinitions);
        Dictionary<string, object> Values { get; }  
    }
}
