namespace MetBench_BLL.SystemMT.Catalog.Typed.Specs;

public abstract record ConditionExpr;

public sealed record ComparisonConditionExpr(
    RefExpr Left,
    string Operator,
    RefExpr Right) : ConditionExpr;
