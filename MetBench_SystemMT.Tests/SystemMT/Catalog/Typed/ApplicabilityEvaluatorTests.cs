using System.Collections.Generic;
using MetBench_BLL.SystemMT.Catalog.Typed.Specs;
using MetBench_BLL.SystemMT.Catalog.Typed.Validation;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Catalog.Typed;

public sealed class ApplicabilityEvaluatorTests
{
    [Fact]
    public void False_applicability_skips_before_run()
    {
        var decision = new ApplicabilityEvaluator().Evaluate(
            new ApplicabilitySpec(
                new ConditionExpr[]
                {
                    new ComparisonConditionExpr(
                        new InputRefExpr("boron_ppm"),
                        "LessOrEqual",
                        new ConstantRefExpr(1000.0))
                }),
            new Dictionary<string, double> { ["boron_ppm"] = 1600.0 });

        Assert.False(decision.ShouldRun);
        Assert.Contains("boron_ppm", decision.Reason);
    }
}
