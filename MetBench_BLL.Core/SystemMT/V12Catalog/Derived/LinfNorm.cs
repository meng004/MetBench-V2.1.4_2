using System;
using MetBench_BLL.SystemMT.V12Catalog.Runtime;

namespace MetBench_BLL.SystemMT.V12Catalog.Derived;

public static class LinfNorm
{
    public static double Compute(Field2DValue field)
    {
        var max = 0.0;
        for (var row = 0; row < field.RowCount; row++)
        {
            for (var column = 0; column < field.ColumnCount; column++)
            {
                max = Math.Max(max, Math.Abs(field.Values[row, column]));
            }
        }

        return max;
    }
}
