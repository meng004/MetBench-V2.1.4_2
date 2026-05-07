namespace MetBench_BLL
{
    public class CsvDataRecord : BaseDataRecord
    {
        
        public  override Dictionary<string, object> Values { get; } = new Dictionary<string, object>();

        public CsvDataRecord(Dictionary<string, object> values)
        {
            Values = values;
        }

        public override bool Validate(IEnumerable<ColumnDefinition> columnDefinitions)
        {
            foreach (var colDef in columnDefinitions.Where(c => c.IsRequired))
            {
                if (!Values.ContainsKey(colDef.Name) || !ValidateField(Values[colDef.Name], colDef.DataType))
                    return false;
            }
            return true;
        }
    }
}
