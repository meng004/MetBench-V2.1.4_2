using MetBench_BLL.SystemMT.Catalog.Typed.Runtime;

namespace MetBench_BLL.SystemMT.Catalog.Typed.Derived;

public static class FieldRegionMean
{
    public static double Compute(Field2DValue field)
    {
        var sum = 0.0;
        var count = 0;

        for (var row = 0; row < field.RowCount; row++)
        {
            for (var column = 0; column < field.ColumnCount; column++)
            {
                sum += field.Values[row, column];
                count++;
            }
        }

        return count == 0 ? 0.0 : sum / count;
    }
}
