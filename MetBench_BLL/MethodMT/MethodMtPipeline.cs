using MetBench_BLL.Equations;
using MetBench_BLL.MethodMT.Assertions;
using MetBench_BLL.MethodMT.Transformations;
using MetBench_BLL.MT;

namespace MetBench_BLL.MethodMT;

/// <summary>
/// 方法级 MT 管道 — 在内存 dict 上跑变换、调 C# SUT 函数两次、用 MethodAssertionEvaluator 断言。
/// </summary>
/// <remarks>
/// 与 <c>SystemMtPipeline</c>（BLL.Core）共享 <see cref="IMtPipeline{TReq,TOut}"/> 抽象，
/// 但执行步骤完全独立（不走进程 / 文件 / parser）。设计见 mr-architecture.md §6（method 侧执行栈）。
/// </remarks>
public sealed class MethodMtPipeline : IMtPipeline<MethodMtRunRequest, MethodMtRunOutcome>
{
    private readonly MethodTransformationRegistry _transformations;
    private readonly MethodAssertionEvaluator _assertions;

    public MethodMtPipeline(EquationFunctionRegistry equationRegistry)
    {
        ArgumentNullException.ThrowIfNull(equationRegistry);
        _transformations = new MethodTransformationRegistry(equationRegistry);
        _assertions = new MethodAssertionEvaluator();
    }

    public Task<MethodMtRunOutcome> ExecuteAsync(
        MethodMtRunRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        // 1) 解析变换
        var transform = _transformations.Resolve(request.TransformationName, request.EquationKey);

        // 2) 生成衍生输入
        Dictionary<string, object?> followupInput;
        try
        {
            followupInput = transform.Apply(request.SourceInput, request.TargetFieldPath, request.Parameters);
        }
        catch (Exception ex)
        {
            return Task.FromResult(Failure(request,
                $"Transformation '{request.TransformationName}' failed: {ex.Message}",
                followup: new Dictionary<string, object?>()));
        }

        // 3) 调 SUT 两次
        IReadOnlyDictionary<string, object?> sourceOutput;
        IReadOnlyDictionary<string, object?> followupOutput;
        try
        {
            sourceOutput = request.SutFunction(request.SourceInput);
        }
        catch (Exception ex)
        {
            return Task.FromResult(Failure(request,
                $"SUT function failed on source input: {ex.Message}",
                followup: followupInput));
        }

        try
        {
            followupOutput = request.SutFunction(followupInput);
        }
        catch (Exception ex)
        {
            return Task.FromResult(Failure(request,
                $"SUT function failed on follow-up input: {ex.Message}",
                followup: followupInput));
        }

        // 4) 抽 ValueName 标量
        if (!TryGetScalar(sourceOutput, request.ValueName, out var srcVal))
        {
            return Task.FromResult(Failure(request,
                $"Source output missing scalar key '{request.ValueName}'",
                followup: followupInput));
        }
        if (!TryGetScalar(followupOutput, request.ValueName, out var flwVal))
        {
            return Task.FromResult(Failure(request,
                $"Follow-up output missing scalar key '{request.ValueName}'",
                followup: followupInput));
        }

        // 5) 评估断言
        MethodMtAssertionResult assertion;
        try
        {
            assertion = _assertions.Evaluate(request.AssertionTypeCode, srcVal, flwVal, request.Tolerance);
        }
        catch (ArgumentException ex)
        {
            return Task.FromResult(new MethodMtRunOutcome(
                Passed: false,
                MrCode: request.MrCode,
                AssertionTypeCode: request.AssertionTypeCode,
                SourceValue: srcVal,
                FollowupValue: flwVal,
                SourceInput: request.SourceInput,
                FollowupInput: followupInput,
                ErrorMessage: ex.Message));
        }

        return Task.FromResult(new MethodMtRunOutcome(
            Passed: assertion.Passed,
            MrCode: request.MrCode,
            AssertionTypeCode: request.AssertionTypeCode,
            SourceValue: srcVal,
            FollowupValue: flwVal,
            SourceInput: request.SourceInput,
            FollowupInput: followupInput,
            ErrorMessage: assertion.Passed ? null :
                $"Assertion '{request.AssertionTypeCode}' failed: source={srcVal}, follow-up={flwVal}"));
    }

    private static MethodMtRunOutcome Failure(
        MethodMtRunRequest request, string error,
        IReadOnlyDictionary<string, object?> followup) =>
        new(Passed: false,
            MrCode: request.MrCode,
            AssertionTypeCode: request.AssertionTypeCode,
            SourceValue: double.NaN,
            FollowupValue: double.NaN,
            SourceInput: request.SourceInput,
            FollowupInput: followup,
            ErrorMessage: error);

    private static bool TryGetScalar(
        IReadOnlyDictionary<string, object?> dict, string key, out double value)
    {
        value = double.NaN;
        if (!dict.TryGetValue(key, out var raw) || raw is null) return false;
        if (raw is double d) { value = d; return true; }
        if (raw is float f)  { value = f; return true; }
        if (raw is int i)    { value = i; return true; }
        if (raw is long l)   { value = l; return true; }
        if (raw is decimal m) { value = (double)m; return true; }
        if (raw is string s && double.TryParse(s, System.Globalization.CultureInfo.InvariantCulture, out var parsed))
        {
            value = parsed; return true;
        }
        return false;
    }
}
