namespace MetBench_BLL.SystemMT.V12Catalog.Specs;

public abstract record ConditionExpr;

public sealed record ComparisonConditionExpr(
    RefExpr Left,
    string Operator,
    RefExpr Right) : ConditionExpr;
