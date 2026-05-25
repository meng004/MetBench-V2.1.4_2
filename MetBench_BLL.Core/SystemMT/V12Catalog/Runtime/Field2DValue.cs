namespace MetBench_BLL.SystemMT.V12Catalog.Runtime;

public sealed class Field2DValue
{
    public Field2DValue(double[,] values)
    {
        Values = values;
    }

    public double[,] Values { get; }
    public int RowCount => Values.GetLength(0);
    public int ColumnCount => Values.GetLength(1);
}
