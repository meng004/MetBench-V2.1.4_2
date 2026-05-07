namespace MetBench_BLL
{
    public class ColumnDefinition
    {
        
        //列名
        public string Name { get;  }
        //列值的类型
        public Type DataType { get; }
        //内容是否必须存在
        public bool IsRequired { get; }

        public ColumnDefinition(string name, Type dataType, bool isRequired = true)
        {
            Name = name;
            DataType = dataType;
            IsRequired = isRequired;
        }
    }
}