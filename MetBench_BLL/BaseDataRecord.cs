namespace MetBench_BLL
{
    public abstract class BaseDataRecord : IDataRecord
    {
        public abstract bool Validate(IEnumerable<ColumnDefinition> columnDefinitions);

        public virtual Dictionary<string, object> Values { get; }  // 确保有这个属性
        protected bool ValidateField(object value, Type expectedType)
        {
            if (value == null)
                return !expectedType.IsValueType || Nullable.GetUnderlyingType(expectedType) != null;

            return expectedType.IsAssignableFrom(value.GetType());
        }
    }
}
