namespace MetBench_BLL.SystemMT.Catalog.Typed.Derived;

public static class MassNumberSum
{
    public static double Compute(double[,] values)
    {
        var sum = 0.0;
        for (var row = 0; row < values.GetLength(0); row++)
        {
            for (var column = 0; column < values.GetLength(1); column++)
            {
                sum += values[row, column];
            }
        }

        return sum;
    }
}
