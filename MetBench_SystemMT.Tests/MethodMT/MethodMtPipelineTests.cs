using MetBench_BLL.Equations;
using MetBench_BLL.MethodMT;
using Xunit;

namespace MetBench_SystemMT.Tests.MethodMT;

/// <summary>
/// G-06 单测：MethodMtPipeline 的 7 个核心路径。
/// AAA 结构（参考 mr-architecture.md §6 method 侧执行栈）。
/// </summary>
public sealed class MethodMtPipelineTests
{
    private readonly MethodMtPipeline _pipeline;

    public MethodMtPipelineTests()
    {
        _pipeline = new MethodMtPipeline(new EquationFunctionRegistry());
    }

    [Fact]
    public async Task ScaleField_greater_passes_when_followup_exceeds_source()
    {
        // Arrange: SUT = identity（output["y"] = input["x"]）。ScaleField /x by 2 必使 followup > source。
        var request = new MethodMtRunRequest(
            MrCode: "test-scale-greater",
            EquationKey: "",
            TransformationName: "ScaleField",
            TargetFieldPath: "/x",
            Parameters: new Dictionary<string, string> { ["factor"] = "2" },
            AssertionTypeCode: "greater",
            ValueName: "y",
            SourceInput: new Dictionary<string, object?> { ["x"] = 10.0 },
            SutFunction: input => new Dictionary<string, object?> { ["y"] = (double)input["x"]! });

        // Act
        var outcome = await _pipeline.ExecuteAsync(request);

        // Assert
        Assert.True(outcome.Passed, outcome.ErrorMessage);
        Assert.Equal(10.0, outcome.SourceValue);
        Assert.Equal(20.0, outcome.FollowupValue);
        Assert.Null(outcome.ErrorMessage);
    }

    [Fact]
    public async Task ScaleField_less_fails_when_followup_exceeds_source()
    {
        // Arrange: SUT = identity，ScaleField factor 2 后 followup > source，断言 "less" 应 fail
        var request = new MethodMtRunRequest(
            MrCode: "test-scale-less-fail",
            EquationKey: "",
            TransformationName: "ScaleField",
            TargetFieldPath: "/x",
            Parameters: new Dictionary<string, string> { ["factor"] = "2" },
            AssertionTypeCode: "less",
            ValueName: "y",
            SourceInput: new Dictionary<string, object?> { ["x"] = 10.0 },
            SutFunction: input => new Dictionary<string, object?> { ["y"] = (double)input["x"]! });

        // Act
        var outcome = await _pipeline.ExecuteAsync(request);

        // Assert
        Assert.False(outcome.Passed);
        Assert.NotNull(outcome.ErrorMessage);
        Assert.Contains("less", outcome.ErrorMessage);
    }

    [Fact]
    public async Task Approx_within_tolerance_passes()
    {
        // Arrange: SUT 加固定漂移 0.0005；factor=1 实质 Identity；source/followup 相同
        var request = new MethodMtRunRequest(
            MrCode: "test-approx-pass",
            EquationKey: "",
            TransformationName: "Identity",
            TargetFieldPath: "/x",
            Parameters: new Dictionary<string, string>(),
            AssertionTypeCode: "approx",
            ValueName: "y",
            SourceInput: new Dictionary<string, object?> { ["x"] = 10.0 },
            SutFunction: input => new Dictionary<string, object?> { ["y"] = (double)input["x"]! + 0.0005 },
            Tolerance: 1e-2);

        // Act
        var outcome = await _pipeline.ExecuteAsync(request);

        // Assert: 输出 10.0005 == 10.0005，差为 0，在 1e-2 容差内
        Assert.True(outcome.Passed, outcome.ErrorMessage);
    }

    [Fact]
    public async Task Approx_outside_tolerance_fails()
    {
        // Arrange: source 跑 input.x=10 输出 10；followup ScaleField factor=2 → input.x=20 → 输出 20。差 10，容差 1e-9 内 fail。
        var request = new MethodMtRunRequest(
            MrCode: "test-approx-fail",
            EquationKey: "",
            TransformationName: "ScaleField",
            TargetFieldPath: "/x",
            Parameters: new Dictionary<string, string> { ["factor"] = "2" },
            AssertionTypeCode: "approx",
            ValueName: "y",
            SourceInput: new Dictionary<string, object?> { ["x"] = 10.0 },
            SutFunction: input => new Dictionary<string, object?> { ["y"] = (double)input["x"]! },
            Tolerance: 1e-9);

        // Act
        var outcome = await _pipeline.ExecuteAsync(request);

        // Assert
        Assert.False(outcome.Passed);
        Assert.NotNull(outcome.ErrorMessage);
    }

    [Fact]
    public async Task Unknown_transformation_throws_UnknownTransformationException()
    {
        // Arrange
        var request = new MethodMtRunRequest(
            MrCode: "test-unknown-transform",
            EquationKey: "",
            TransformationName: "NonExistentTransform",
            TargetFieldPath: "/x",
            Parameters: new Dictionary<string, string>(),
            AssertionTypeCode: "greater",
            ValueName: "y",
            SourceInput: new Dictionary<string, object?> { ["x"] = 10.0 },
            SutFunction: input => new Dictionary<string, object?> { ["y"] = 0.0 });

        // Act + Assert
        await Assert.ThrowsAsync<UnknownTransformationException>(
            () => _pipeline.ExecuteAsync(request));
    }

    [Fact]
    public async Task Unsupported_assertion_code_reports_error_via_outcome()
    {
        // Arrange: 噪声感知 assertion code 不在 method 侧 allowed 集
        var request = new MethodMtRunRequest(
            MrCode: "test-unsupported-assertion",
            EquationKey: "",
            TransformationName: "Identity",
            TargetFieldPath: "/x",
            Parameters: new Dictionary<string, string>(),
            AssertionTypeCode: "less-noise-aware",
            ValueName: "y",
            SourceInput: new Dictionary<string, object?> { ["x"] = 10.0 },
            SutFunction: input => new Dictionary<string, object?> { ["y"] = (double)input["x"]! });

        // Act
        var outcome = await _pipeline.ExecuteAsync(request);

        // Assert
        Assert.False(outcome.Passed);
        Assert.NotNull(outcome.ErrorMessage);
        Assert.Contains("less-noise-aware", outcome.ErrorMessage);
    }

    [Fact]
    public async Task Sut_function_exception_reports_error_via_outcome()
    {
        // Arrange: SUT 函数抛
        var request = new MethodMtRunRequest(
            MrCode: "test-sut-throws",
            EquationKey: "",
            TransformationName: "Identity",
            TargetFieldPath: "/x",
            Parameters: new Dictionary<string, string>(),
            AssertionTypeCode: "greater",
            ValueName: "y",
            SourceInput: new Dictionary<string, object?> { ["x"] = 10.0 },
            SutFunction: input => throw new InvalidOperationException("boom"));

        // Act
        var outcome = await _pipeline.ExecuteAsync(request);

        // Assert
        Assert.False(outcome.Passed);
        Assert.NotNull(outcome.ErrorMessage);
        Assert.Contains("SUT function failed", outcome.ErrorMessage);
        Assert.Contains("boom", outcome.ErrorMessage);
    }
}
