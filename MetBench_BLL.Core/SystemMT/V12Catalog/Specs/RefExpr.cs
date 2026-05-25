namespace MetBench_BLL.SystemMT.V12Catalog.Specs;

public abstract record RefExpr;

public sealed record InputRefExpr(string Name) : RefExpr;

public sealed record ConstantRefExpr(double Value) : RefExpr;
