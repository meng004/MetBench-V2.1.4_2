using MetBench_BLL.Equations;
using MetBench_BLL.Equations.Bateman;
using MetBench_BLL.MethodMT;
using Xunit;

namespace MetBench_SystemMT.Tests.MethodMT;

/// <summary>
/// G-06 参数化 AAA：用 BatemanAnalyticSolution（L2 解析解）作为方法级 SUT，
/// 验证若干 Bateman MR 在样本输入上的关系成立。
///
/// 这是 catalog-driven validator 的雏形：每行 InlineData 对应一条 MR；
/// 未来 G-12 升级 PBT 时此处替换为 FsCheck property，AAA 测试保留为基线。
/// </summary>
public sealed class MethodMtBatemanTests
{
    private readonly MethodMtPipeline _pipeline;
    private readonly EquationFunctionRegistry _equationRegistry;

    public MethodMtBatemanTests()
    {
        _equationRegistry = new EquationFunctionRegistry();

        // 注册 bateman.ScaleInitial L1 Recipe（与 launcher 同一 Recipe）
        const string scaleInitialRecipe = """
            {"compose":[
              {"op":"ScaleField","path":"/initial/N_A","params":{"factor":"{factor}"}},
              {"op":"ScaleField","path":"/initial/N_B","params":{"factor":"{factor}"}},
              {"op":"ScaleField","path":"/initial/N_C","params":{"factor":"{factor}"}}
            ]}
            """;
        _equationRegistry.Register(new RecipeBasedEquationFunction(
            "bateman", "ScaleInitial", "transformation", scaleInitialRecipe));
        _equationRegistry.Register(new BatemanAnalyticSolution());

        _pipeline = new MethodMtPipeline(_equationRegistry);
    }

    /// <summary>SUT：包装 BatemanAnalyticSolution 为 dict → dict 委托。</summary>
    private Func<IReadOnlyDictionary<string, object?>, IReadOnlyDictionary<string, object?>>
        BatemanSut() => input =>
    {
        var solution = new BatemanAnalyticSolution();
        // BatemanAnalyticSolution.Execute 返回平铺 {"N_A":..., "N_B":..., "N_C":...}
        return solution.Execute(input, new Dictionary<string, string>());
    };

    private static IReadOnlyDictionary<string, object?> BatemanSourceInput() =>
        new Dictionary<string, object?>
        {
            ["initial"] = new Dictionary<string, object?>
            {
                ["N_A"] = 100.0, ["N_B"] = 0.0, ["N_C"] = 0.0,
            },
            ["params"] = new Dictionary<string, object?>
            {
                ["lambda_A"] = 0.1, ["lambda_B"] = 0.05, ["t_final"] = 10.0,
            },
        };

    public static IEnumerable<object[]> BatemanMrCases() => new[]
    {
        // (MrCode, TransformationName, TargetFieldPath, FactorParam, AssertionCode, ValueName, ExpectedPassed)
        new object[] { "bateman-scale-initial-x2",      "ScaleInitial",  "",                    "2", "greater", "N_C", true },
        new object[] { "bateman-scale-initial-x3",      "ScaleInitial",  "",                    "3", "greater", "N_C", true },
        new object[] { "bateman-scale-N_A-x2-greater",  "ScaleField",    "/initial/N_A",        "2", "greater", "N_C", true },
        new object[] { "bateman-scale-N_A-x2-less-fails","ScaleField",   "/initial/N_A",        "2", "less",    "N_C", false },
    };

    [Theory]
    [MemberData(nameof(BatemanMrCases))]
    public async Task Bateman_method_mr_outcome(
        string mrCode, string txName, string path, string factor,
        string assertionCode, string valueName, bool expectedPassed)
    {
        // Arrange
        var parameters = string.IsNullOrEmpty(factor)
            ? new Dictionary<string, string>()
            : new Dictionary<string, string> { ["factor"] = factor };
        var request = new MethodMtRunRequest(
            MrCode: mrCode,
            EquationKey: "bateman",
            TransformationName: txName,
            TargetFieldPath: path,
            Parameters: parameters,
            AssertionTypeCode: assertionCode,
            ValueName: valueName,
            SourceInput: BatemanSourceInput(),
            SutFunction: BatemanSut());

        // Act
        var outcome = await _pipeline.ExecuteAsync(request);

        // Assert
        Assert.Equal(expectedPassed, outcome.Passed);
        Assert.Equal(mrCode, outcome.MrCode);
    }

}
