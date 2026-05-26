using System.Text.Json;
using MetBench_BLL.Equations;
using MetBench_BLL.MT;
using MetBench_BLL.SystemMT.Assertions;
using MetBench_BLL.SystemMT.Catalog.Typed.Migration;
using MetBench_BLL.SystemMT.Catalog.Typed.Runtime;
using MetBench_BLL.SystemMT.Catalog.Typed.Specs;
using MetBench_BLL.SystemMT.ParameterMapping;
using MetBench_BLL.SystemMT.Transformations;

namespace MetBench_BLL.SystemMT.Pipeline;

/// <summary>
/// v2 MT Pipeline 实现 — 按 PipelineStatus 9 状态机串行执行。
/// </summary>
/// <remarks>
/// 状态转移：
///   queued → parsing-source → transforming → writing-followup →
///   running-source → running-followup → parsing-outputs → asserting →
///   ok / anomaly / error / timeout
///
/// 任意阶段失败：状态置 error/timeout，ErrorMessage 填充，跳过后续阶段，返回 PipelineOutcome。
///
/// 同时实现共享抽象 <see cref="IMtPipeline{TReq,TOut}"/>（mr-architecture.md §2 协议层共享），
/// 与方法级 <see cref="MetBench_BLL.MethodMT.MethodMtPipeline"/> 对称。
/// </remarks>
public sealed class SystemMtPipeline : ISystemMtPipeline, IMtPipeline<PipelineContext, PipelineOutcome>
{
    private readonly IProcessExecutor _processExecutor;
    private readonly IPredicateDispatcher _predicateDispatcher;

    public SystemMtPipeline(
        IProcessExecutor? processExecutor = null,
        IPredicateDispatcher? predicateDispatcher = null)
    {
        _processExecutor = processExecutor ?? new DefaultProcessExecutor();
        _predicateDispatcher = predicateDispatcher ?? new PredicateDispatcher();
    }

    Task<PipelineOutcome> IMtPipeline<PipelineContext, PipelineOutcome>.ExecuteAsync(
        PipelineContext request, CancellationToken cancellationToken)
        => ExecuteAsync(request, progress: null, cancellationToken);

    public async Task<PipelineOutcome> ExecuteAsync(
        PipelineContext ctx,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var startedAt = DateTime.UtcNow;
        var artifactsDir = ctx.WorkingDirectory;
        Directory.CreateDirectory(artifactsDir);

        var followupInputPath = Path.Combine(artifactsDir, "followup.in.json");
        var sourceOutputPath = Path.Combine(artifactsDir, "source.out.json");
        var followupOutputPath = Path.Combine(artifactsDir, "followup.out.json");

        TimeSpan srcElapsed = TimeSpan.Zero;
        TimeSpan flwElapsed = TimeSpan.Zero;
        int srcExitCode = 0;
        int flwExitCode = 0;

        try
        {
            // 1. ParsingSource — 调 Python input_parser 读 source.json → dict
            progress?.Report(PipelineStatus.ParsingSource);
            var parseSourceCmd =
                $"{ctx.InputParserCommand} parse --input \"{ctx.SourceCasePath}\"";
            var psResult = await _processExecutor.RunAsync(
                parseSourceCmd, artifactsDir, ctx.TimeoutSeconds, cancellationToken)
                .ConfigureAwait(false);
            if (psResult.ExitCode != 0)
                return Fail(PipelineStatus.Error, "ParsingSource failed: " + psResult.Stderr);

            // JsonSerializer 把嵌套值反序列化为 JsonElement；转成原生 Dictionary/List/double/string
            // 让 IMRTransformation / IFieldPathResolver 能直接操作
            var sourceDict = (Dictionary<string, object?>)ConvertJsonValue(
                JsonDocument.Parse(psResult.Stdout).RootElement)!
                ?? throw new InvalidOperationException("Empty parse result");

            // 2. Transforming — C# IMRTransformation 在内存 dict 上应用变换
            progress?.Report(PipelineStatus.Transforming);
            var resolver = FieldPathResolverFactory.For(ctx.PathSyntax);
            var transformation = ctx.EquationFunctionRegistry is { } eqReg
                ? new TransformationResolver(eqReg).Resolve(ctx.TransformationName, ctx.EquationKey)
                : TransformationRegistry.Get(ctx.TransformationName);
            var followupDict = transformation.Apply(sourceDict, ctx.TargetFieldPath, ctx.Parameters);

            // 3. WritingFollowup — dict → JSON 临时文件 → 调 Python input_parser write
            progress?.Report(PipelineStatus.WritingFollowup);
            var dictTempPath = Path.Combine(artifactsDir, "followup.dict.json");
            File.WriteAllText(dictTempPath, JsonSerializer.Serialize(followupDict));
            var writeCmd =
                $"{ctx.InputParserCommand} write --dict-file \"{dictTempPath}\" --output \"{followupInputPath}\"";
            var wResult = await _processExecutor.RunAsync(
                writeCmd, artifactsDir, ctx.TimeoutSeconds, cancellationToken)
                .ConfigureAwait(false);
            if (wResult.ExitCode != 0)
                return Fail(PipelineStatus.Error, "WritingFollowup failed: " + wResult.Stderr);

            // 4-5. RunningSource & RunningFollowup — 调 SUT runner
            progress?.Report(PipelineStatus.RunningSource);
            var runSourceCmd =
                $"{ctx.RunnerCommand} --input \"{ctx.SourceCasePath}\" --output \"{sourceOutputPath}\"";
            var rsResult = await _processExecutor.RunAsync(
                runSourceCmd, artifactsDir, ctx.TimeoutSeconds, cancellationToken)
                .ConfigureAwait(false);
            srcExitCode = rsResult.ExitCode;
            srcElapsed = rsResult.Elapsed;
            if (rsResult.TimedOut) return Fail(PipelineStatus.Timeout, "Source SUT timed out");
            if (rsResult.ExitCode != 0) return Fail(PipelineStatus.Error, "Source SUT failed: " + rsResult.Stderr);

            progress?.Report(PipelineStatus.RunningFollowup);
            var runFollowupCmd =
                $"{ctx.RunnerCommand} --input \"{followupInputPath}\" --output \"{followupOutputPath}\"";
            var rfResult = await _processExecutor.RunAsync(
                runFollowupCmd, artifactsDir, ctx.TimeoutSeconds, cancellationToken)
                .ConfigureAwait(false);
            flwExitCode = rfResult.ExitCode;
            flwElapsed = rfResult.Elapsed;
            if (rfResult.TimedOut) return Fail(PipelineStatus.Timeout, "Followup SUT timed out");
            if (rfResult.ExitCode != 0) return Fail(PipelineStatus.Error, "Followup SUT failed: " + rfResult.Stderr);

            // 6. ParsingOutputs — 调 Python output_parser × 2
            progress?.Report(PipelineStatus.ParsingOutputs);
            var sourceOutDict = await ParseOutputDict(ctx, sourceOutputPath, artifactsDir, cancellationToken)
                .ConfigureAwait(false);
            var followupOutDict = await ParseOutputDict(ctx, followupOutputPath, artifactsDir, cancellationToken)
                .ConfigureAwait(false);

            var sourceMetrics = ExtractMetrics(sourceOutDict);
            var followupMetrics = ExtractMetrics(followupOutDict);

            // 7. Asserting — typed predicate dispatch only; legacy string codes are
            // mapped fail-closed via LegacyAssertionPredicateMapper (PR-C).
            progress?.Report(PipelineStatus.Asserting);
            var (assertionResult, typedSpec, typedPredicate, typedVerification) =
                EvaluateAssertion(ctx, sourceMetrics, followupMetrics);

            // 8. Final
            var finalStatus = assertionResult.Passed ? PipelineStatus.Ok : PipelineStatus.Anomaly;
            progress?.Report(finalStatus);

            return new PipelineOutcome(
                FinalStatus: finalStatus,
                ErrorMessage: null,
                StartedAt: startedAt,
                FinishedAt: DateTime.UtcNow,
                ArtifactsDirectory: artifactsDir,
                SourceInputPath: ctx.SourceCasePath,
                FollowupInputPath: followupInputPath,
                SourceOutputPath: sourceOutputPath,
                FollowupOutputPath: followupOutputPath,
                SourceMetrics: sourceMetrics,
                FollowupMetrics: followupMetrics,
                AssertionResult: assertionResult,
                SourceElapsed: srcElapsed,
                FollowupElapsed: flwElapsed,
                SourceExitCode: srcExitCode,
                FollowupExitCode: flwExitCode)
            {
                TypedSpec = typedSpec,
                TypedPredicate = typedPredicate,
                TypedVerification = typedVerification,
            };
        }
        catch (OperationCanceledException)
        {
            return Fail(PipelineStatus.Cancelled, "Pipeline cancelled by user");
        }
        catch (Exception ex)
        {
            return Fail(PipelineStatus.Error, $"{ex.GetType().Name}: {ex.Message}");
        }

        // 局部函数：构造失败的 PipelineOutcome
        PipelineOutcome Fail(string status, string err) => new(
            FinalStatus: status,
            ErrorMessage: err,
            StartedAt: startedAt,
            FinishedAt: DateTime.UtcNow,
            ArtifactsDirectory: artifactsDir,
            SourceInputPath: ctx.SourceCasePath,
            FollowupInputPath: followupInputPath,
            SourceOutputPath: sourceOutputPath,
            FollowupOutputPath: followupOutputPath,
            SourceMetrics: null,
            FollowupMetrics: null,
            AssertionResult: null,
            SourceElapsed: srcElapsed,
            FollowupElapsed: flwElapsed,
            SourceExitCode: srcExitCode,
            FollowupExitCode: flwExitCode);
    }

    private async Task<Dictionary<string, object?>> ParseOutputDict(
        PipelineContext ctx, string outputPath, string workDir, CancellationToken ct)
    {
        var cmd = $"{ctx.OutputParserCommand} parse --output-file \"{outputPath}\"";
        var result = await _processExecutor.RunAsync(cmd, workDir, ctx.TimeoutSeconds, ct)
            .ConfigureAwait(false);
        if (result.ExitCode != 0)
            throw new InvalidOperationException(
                $"Output parser failed (exit {result.ExitCode}): {result.Stderr}");
        return JsonSerializer.Deserialize<Dictionary<string, object?>>(result.Stdout)
            ?? throw new InvalidOperationException("Empty output parse result");
    }

    /// <summary>从 output_parser 输出的 {values, metadata} 字典里抽出 values。</summary>
    private static IReadOnlyDictionary<string, double> ExtractMetrics(Dictionary<string, object?> parsed)
    {
        if (!parsed.TryGetValue("values", out var valuesObj) || valuesObj is null)
            return new Dictionary<string, double>();
        if (valuesObj is not JsonElement el)
            return new Dictionary<string, double>();
        var result = new Dictionary<string, double>();
        foreach (var prop in el.EnumerateObject())
        {
            if (prop.Value.TryGetDouble(out var d))
                result[prop.Name] = d;
        }
        return result;
    }

    /// <summary>
    /// 递归把 JsonElement 转成原生 C# 类型：
    /// Object → Dictionary&lt;string, object?&gt;；Array → List&lt;object?&gt;；
    /// Number → double；String → string；Bool → bool；Null → null。
    /// IMRTransformation / IFieldPathResolver 需要操作原生类型而非 JsonElement。
    /// </summary>
    private static object? ConvertJsonValue(JsonElement el) => el.ValueKind switch
    {
        JsonValueKind.Object =>
            el.EnumerateObject().ToDictionary(p => p.Name, p => ConvertJsonValue(p.Value)),
        JsonValueKind.Array =>
            el.EnumerateArray().Select(ConvertJsonValue).ToList(),
        JsonValueKind.Number => el.GetDouble(),
        JsonValueKind.String => el.GetString(),
        JsonValueKind.True   => true,
        JsonValueKind.False  => false,
        JsonValueKind.Null   => (object?)null,
        _ => null,
    };

    /// <summary>
    /// 经典 less/greater/approx 和 scaling 走 LegacyAssertionPredicateMapper → 类型化谓词；
    /// 类型化路径直接执行 PredicateDispatcher，避免再次走旧 AssertionEvaluator。
    /// 未识别的旧 AssertionTypeCode 直接 fail-closed，返回 UnknownType 结果。
    /// 返回 4 元组：传统断言结果 + 类型化 (spec, predicate, verification) 三元组，
    /// 供 PipelineOutcome 把类型化证据带回给 SystemMtExecutionRecorder（PR-123）。
    /// </summary>
    private (SystemMtAssertionResultV2 Assertion, MrSpec? TypedSpec, PredicateSpec? TypedPredicate, VerificationResult? TypedVerification)
        EvaluateAssertion(
            PipelineContext ctx,
            IReadOnlyDictionary<string, double> sourceMetrics,
            IReadOnlyDictionary<string, double> followupMetrics)
    {
        MrSpec spec;
        PredicateSpec predicate;
        try
        {
            if (ctx.TypedSpec is { } providedSpec && ctx.TypedPredicate is { } providedPredicate)
            {
                spec = providedSpec;
                predicate = providedPredicate;
            }
            else
            {
                // String-code dispatch is confined to TypedSpecFactory.ForLegacyAssertion
                // inside the Catalog/Typed/Migration/ namespace per the
                // SemanticCatalogBoundaryTests guard — the pipeline only sees the
                // resulting typed MrSpec + first predicate.
                spec = TypedSpecFactory.ForLegacyAssertion(
                    mrCode: ctx.MrCode,
                    assertionTypeCode: ctx.AssertionTypeCode,
                    valueName: ctx.ValueName,
                    parameters: ctx.Parameters,
                    toleranceAbs: ctx.Tolerance.ToleranceAbs,
                    toleranceRel: ctx.Tolerance.ToleranceRel);
                predicate = spec.Predicates![0];
            }
        }
        catch (ArgumentException ex)
        {
            var unknown = SystemMtAssertionResultV2.UnknownType(ctx.AssertionTypeCode) with
            {
                FailureReason = ex.Message,
            };
            return (unknown, null, null, null);
        }

        VerificationContext verificationContext;
        try
        {
            verificationContext = TypedVerificationContextFactory.FromScalarOutputs(
                spec,
                sourceMetrics,
                followupMetrics,
                ctx.Parameters);
        }
        catch (InvalidOperationException ex)
        {
            var unknown = SystemMtAssertionResultV2.UnknownType(ctx.AssertionTypeCode) with
            {
                FailureReason = ex.Message,
            };
            return (unknown, spec, predicate, null);
        }

        var verification = _predicateDispatcher.Dispatch(predicate, verificationContext);
        if (verification.Assertion is { } typedAssertion)
        {
            return (typedAssertion, spec, predicate, verification);
        }

        var diagnosticReason = verification.Context?.Reason ?? verification.Status.ToString();
        var fallback = new SystemMtAssertionResultV2(
            AssertionTypeCode: ctx.AssertionTypeCode,
            Passed: false,
            SourceValue: sourceMetrics.TryGetValue(ctx.ValueName, out var sv) ? sv : (double?)null,
            FollowupValue: followupMetrics.TryGetValue(ctx.ValueName, out var fv) ? fv : (double?)null,
            ObservedDelta: null,
            ExpectedThreshold: null,
            Expression: $"{verification.Status} on '{ctx.ValueName}'",
            FailureReason: diagnosticReason);
        return (fallback, spec, predicate, verification);
    }
}
