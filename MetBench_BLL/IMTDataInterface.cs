namespace MetBench_BLL
{
    public interface IMTDataInterface<T> where T : IDataRecord
    {        
        IEnumerable<ColumnDefinition> ColumnDefinitions { get; }
        List<T> Data { get; }
        void LoadData();
    }
}
