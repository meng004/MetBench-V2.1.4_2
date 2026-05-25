using System;
using MetBench_BLL.SystemMT.V12Catalog.Runtime;

namespace MetBench_BLL.SystemMT.V12Catalog.Derived;

public static class L2Norm
{
    public static double Compute(Field2DValue field)
    {
        var sumSquares = 0.0;
        for (var row = 0; row < field.RowCount; row++)
        {
            for (var column = 0; column < field.ColumnCount; column++)
            {
                var value = field.Values[row, column];
                sumSquares += value * value;
            }
        }

        return Math.Sqrt(sumSquares);
    }
}
