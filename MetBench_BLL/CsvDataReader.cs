namespace MetBench_BLL
{
    public class CsvDataReader : IMTDataInterface<CsvDataRecord>
    {
        public IEnumerable<ColumnDefinition> ColumnDefinitions { get; }
        public List<CsvDataRecord> Data { get; private set; }
        private readonly string _filePath;
        public Dictionary<string, object> Values { get; }  // 确保有这个属性
        public CsvDataReader(string filePath, IEnumerable<ColumnDefinition> columnDefinitions)
        {
            _filePath = filePath;
            ColumnDefinitions = columnDefinitions.ToList();
            Data = new List<CsvDataRecord>();
        }
        public void LoadData()
        {
            Data.Clear();

            if (!File.Exists(_filePath))
                throw new FileNotFoundException($"CSV文件未找到: {_filePath}");

            var lines = File.ReadAllLines(_filePath);
            if (lines.Length < 1) return;

            // ​​处理CSV文件的第一行（表头行）
            var headers = lines[0].Split(',').Select(h => h.Trim()).ToArray();

            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;

                var values = lines[i].Split(',');
                var recordData = new Dictionary<string, object>();

                for (int col = 0; col < headers.Length && col < values.Length; col++)
                {
                    var header = headers[col];
                    var value = values[col].Trim();

                    // 根据列定义转换类型
                    var colDef = ColumnDefinitions.FirstOrDefault(c => c.Name == header);
                    if (colDef != null)
                    {
                        try
                        {
                            recordData[header] = Convert.ChangeType(value, colDef.DataType);
                        }
                        catch
                        {
                            if (colDef.IsRequired)
                                throw new FormatException($"字段 '{header}' 的值 '{value}' 无法转换为 {colDef.DataType.Name}");

                            recordData[header] = GetDefaultValue(colDef.DataType);
                        }
                    }
                    else
                    {
                        // 未定义的列保持为字符串
                        recordData[header] = value;
                    }
                }

                var record = new CsvDataRecord(recordData);
                if (record.Validate(ColumnDefinitions))
                {
                    Data.Add(record);
                }
            }
            var data = Data;
        }

        private object GetDefaultValue(Type type)
        {
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }
    }
}
