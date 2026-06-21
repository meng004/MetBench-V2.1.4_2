using System.Text.Json;
using MetBench_BLL.Equations;
using MetBench_BLL.MT;
using MetBench_BLL.SystemMT.Assertions;
using MetBench_BLL.SystemMT.Catalog.Typed.Migration;
using MetBench_BLL.SystemMT.Catalog.Typed.Runtime;
using MetBench_BLL.SystemMT.Catalog.Typed.Specs;
using MetBench_BLL.SystemMT.ParameterMapping;
using MetBench_BLL.SystemMT.Runtime;
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
    private readonly IRuntimeProcessExecutor _runtimeProcessExecutor;
    private readonly IPredicateDispatcher _predicateDispatcher;

    public SystemMtPipeline(
        IProcessExecutor? processExecutor = null,
        IRuntimeProcessExecutor? runtimeProcessExecutor = null,
        IPredicateDispatcher? predicateDispatcher = null)
    {
        _processExecutor = processExecutor ?? new DefaultProcessExecutor();
        _runtimeProcessExecutor = runtimeProcessExecutor
            ?? new RuntimeProcessExecutorRegistry(
                new LocalRuntimeProcessExecutor(_processExecutor));
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
        string sourceRuntimeRunId = string.Empty;
        string followupRuntimeRunId = string.Empty;

        try
        {
            // 1. ParsingSource — 调 Python input_parser 读 source.json → dict
            progress?.Report(PipelineStatus.ParsingSource);
            var parseSourceInvocation =
                ctx.InputParserInvocation.WithArguments("parse", "--input", ctx.SourceCasePath);
            var psResult = await _processExecutor.RunAsync(
                parseSourceInvocation, artifactsDir, ctx.TimeoutSeconds, cancellationToken)
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
            var writeInvocation = ctx.InputParserInvocation.WithArguments(
                "write", "--dict-file", dictTempPath, "--output", followupInputPath);
            var wResult = await _processExecutor.RunAsync(
                writeInvocation, artifactsDir, ctx.TimeoutSeconds, cancellationToken)
                .ConfigureAwait(false);
            if (wResult.ExitCode != 0)
                return Fail(PipelineStatus.Error, "WritingFollowup failed: " + wResult.Stderr);

            // 4-5. RunningSource & RunningFollowup — 调 SUT runner
            progress?.Report(PipelineStatus.RunningSource);
            var runSourceInvocation = ctx.RunnerInvocation.WithArguments(
                "--input", ctx.SourceCasePath, "--output", sourceOutputPath);
            var rsResult = await RunSutCommandAsync(
                ctx, runSourceInvocation, artifactsDir, cancellationToken)
                .ConfigureAwait(false);
            srcExitCode = rsResult.ExitCode;
            srcElapsed = rsResult.Elapsed;
            sourceRuntimeRunId = rsResult.RuntimeRunId;
            if (rsResult.TimedOut) return Fail(PipelineStatus.Timeout, "Source SUT timed out");
            if (rsResult.ExitCode != 0) return Fail(PipelineStatus.Error, "Source SUT failed: " + rsResult.Stderr);

            progress?.Report(PipelineStatus.RunningFollowup);
            var runFollowupInvocation = ctx.RunnerInvocation.WithArguments(
                "--input", followupInputPath, "--output", followupOutputPath);
            var rfResult = await RunSutCommandAsync(
                ctx, runFollowupInvocation, artifactsDir, cancellationToken)
                .ConfigureAwait(false);
            flwExitCode = rfResult.ExitCode;
            flwElapsed = rfResult.Elapsed;
            followupRuntimeRunId = rfResult.RuntimeRunId;
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
                SourceRuntimeRunId = sourceRuntimeRunId,
                FollowupRuntimeRunId = followupRuntimeRunId,
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
            FollowupExitCode: flwExitCode)
        {
            SourceRuntimeRunId = sourceRuntimeRunId,
            FollowupRuntimeRunId = followupRuntimeRunId,
        };
    }

    private async Task<Dictionary<string, object?>> ParseOutputDict(
        PipelineContext ctx, string outputPath, string workDir, CancellationToken ct)
    {
        var invocation = ctx.OutputParserInvocation.WithArguments("parse", "--output-file", outputPath);
        var result = await _processExecutor.RunAsync(invocation, workDir, ctx.TimeoutSeconds, ct)
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

    /// <summary>
    /// PR-Bol-2A: 多相 reference-convergence 管线。串行执行 <c>mp.Phases</c>:
    /// 每相位用 phase.Parameters 跑一次 <c>transform → write → run → parse</c>，
    /// 累积 <c>phaseMetrics[role] = metrics</c>。最后用 launcher 预构建的 typed spec
    /// + predicate 跑一次 <see cref="IPredicateDispatcher.Dispatch"/>。
    ///
    /// <para>设计契约（PR-Bol-2A）：</para>
    /// <list type="bullet">
    ///   <item><c>mp.Base.TypedSpec</c> 必须非空（launcher 必须预构建）；否则 fail-closed。</item>
    ///   <item>每相位有独立的输入 / 输出文件 (<c>phase.in.{role}.json</c> / <c>phase.out.{role}.json</c>)。</item>
    ///   <item>进度回调 emit <c>"running-phase:{role}"</c>；不进 <see cref="PipelineStatus.All"/>。</item>
    ///   <item>显示兼容：第一相位 → <c>SourceMetrics</c>，最后相位 → <c>FollowupMetrics</c>。</item>
    /// </list>
    /// </summary>
    public async Task<PipelineOutcome> ExecuteMultiPhaseAsync(
        MultiPhaseExecutionContext mp,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (mp is null) throw new ArgumentNullException(nameof(mp));
        if (mp.Phases is null || mp.Phases.Count == 0)
            throw new ArgumentException("MultiPhaseExecutionContext requires at least one phase.", nameof(mp));

        var ctx = mp.Base;
        var startedAt = DateTime.UtcNow;
        var artifactsDir = ctx.WorkingDirectory;
        Directory.CreateDirectory(artifactsDir);

        // Display-compat fields filled at the end from first / last phase.
        TimeSpan firstElapsed = TimeSpan.Zero;
        TimeSpan lastElapsed = TimeSpan.Zero;
        int firstExitCode = 0;
        int lastExitCode = 0;
        string firstRuntimeRunId = string.Empty;
        string lastRuntimeRunId = string.Empty;
        string firstInputPath = "";
        string lastInputPath = "";
        string firstOutputPath = "";
        string lastOutputPath = "";

        try
        {
            if (ctx.TypedSpec is not { } typedSpec || ctx.TypedPredicate is not { } typedPredicate)
            {
                return Fail(PipelineStatus.Error,
                    "ExecuteMultiPhaseAsync requires PipelineContext.TypedSpec + TypedPredicate "
                    + "to be pre-built by the launcher (no string-code dispatch in the multi-phase path).");
            }

            // 1. ParsingSource — 一次性解析 source case
            progress?.Report(PipelineStatus.ParsingSource);
            var parseSourceInvocation =
                ctx.InputParserInvocation.WithArguments("parse", "--input", ctx.SourceCasePath);
            var psResult = await _processExecutor.RunAsync(
                parseSourceInvocation, artifactsDir, ctx.TimeoutSeconds, cancellationToken)
                .ConfigureAwait(false);
            if (psResult.ExitCode != 0)
                return Fail(PipelineStatus.Error, "ParsingSource failed: " + psResult.Stderr);

            var sourceDict = (Dictionary<string, object?>)ConvertJsonValue(
                JsonDocument.Parse(psResult.Stdout).RootElement)!
                ?? throw new InvalidOperationException("Empty parse result");

            // 解析变换一次（每相位共享，仅 phase.Parameters 不同）
            var transformation = ctx.EquationFunctionRegistry is { } eqReg
                ? new TransformationResolver(eqReg).Resolve(ctx.TransformationName, ctx.EquationKey)
                : TransformationRegistry.Get(ctx.TransformationName);

            var phaseMetrics = new Dictionary<string, IReadOnlyDictionary<string, double>>(
                StringComparer.Ordinal);
            var phaseMetricsForContext = new Dictionary<string, IReadOnlyDictionary<string, double>>(
                StringComparer.Ordinal);

            // 2-N. 每相位独立 transform → write → run → parse
            for (var i = 0; i < mp.Phases.Count; i++)
            {
                var phase = mp.Phases[i];
                progress?.Report($"{PipelineStatus.RunningPhase}:{phase.Role}");

                var phaseInputPath = Path.Combine(artifactsDir, $"phase.in.{phase.Role}.json");
                var phaseOutputPath = Path.Combine(artifactsDir, $"phase.out.{phase.Role}.json");
                if (i == 0) { firstInputPath = phaseInputPath; firstOutputPath = phaseOutputPath; }
                if (i == mp.Phases.Count - 1) { lastInputPath = phaseInputPath; lastOutputPath = phaseOutputPath; }

                // Merge blueprint defaults (ctx.Parameters) with per-phase overrides; phase wins on conflict.
                // This lets common parameters (e.g. mesh size baseline) live once on the blueprint and only
                // the per-phase delta (e.g. factor) appear in refinement_phases.
                var mergedParameters = new Dictionary<string, string>(ctx.Parameters, StringComparer.Ordinal);
                foreach (var (k, v) in phase.Parameters)
                {
                    mergedParameters[k] = v;
                }

                // Apply transformation with merged parameters
                var phaseDict = transformation.Apply(sourceDict, ctx.TargetFieldPath, mergedParameters);

                // Write phase input via Python input parser
                var dictTempPath = Path.Combine(artifactsDir, $"phase.dict.{phase.Role}.json");
                File.WriteAllText(dictTempPath, JsonSerializer.Serialize(phaseDict));
                var writeInvocation = ctx.InputParserInvocation.WithArguments(
                    "write", "--dict-file", dictTempPath, "--output", phaseInputPath);
                var wResult = await _processExecutor.RunAsync(
                    writeInvocation, artifactsDir, ctx.TimeoutSeconds, cancellationToken)
                    .ConfigureAwait(false);
                if (wResult.ExitCode != 0)
                    return Fail(PipelineStatus.Error,
                        $"WritingPhase '{phase.Role}' failed: {wResult.Stderr}");

                // Run SUT
                var runInvocation = ctx.RunnerInvocation.WithArguments(
                    "--input", phaseInputPath, "--output", phaseOutputPath);
                var rResult = await RunSutCommandAsync(
                    ctx, runInvocation, artifactsDir, cancellationToken)
                    .ConfigureAwait(false);
                if (i == 0)
                {
                    firstElapsed = rResult.Elapsed;
                    firstExitCode = rResult.ExitCode;
                    firstRuntimeRunId = rResult.RuntimeRunId;
                }
                if (i == mp.Phases.Count - 1)
                {
                    lastElapsed = rResult.Elapsed;
                    lastExitCode = rResult.ExitCode;
                    lastRuntimeRunId = rResult.RuntimeRunId;
                }
                if (rResult.TimedOut)
                    return Fail(PipelineStatus.Timeout, $"Phase '{phase.Role}' SUT timed out");
                if (rResult.ExitCode != 0)
                    return Fail(PipelineStatus.Error,
                        $"Phase '{phase.Role}' SUT failed: {rResult.Stderr}");

                // Parse phase output → metrics
                var phaseOutDict = await ParseOutputDict(ctx, phaseOutputPath, artifactsDir, cancellationToken)
                    .ConfigureAwait(false);
                var metrics = ExtractMetrics(phaseOutDict);
                phaseMetrics[phase.Role] = metrics;
                phaseMetricsForContext[phase.Role] = metrics;
            }

            // Asserting — typed dispatch via FromPhaseOutputs
            progress?.Report(PipelineStatus.Asserting);
            SystemMtAssertionResultV2 assertionResult;
            VerificationResult? verification = null;
            try
            {
                var verificationContext = TypedVerificationContextFactory.FromPhaseOutputs(
                    typedSpec, phaseMetricsForContext, ctx.Parameters);
                verification = _predicateDispatcher.Dispatch(typedPredicate, verificationContext);
                assertionResult = verification.Assertion ?? new SystemMtAssertionResultV2(
                    AssertionTypeCode: ctx.AssertionTypeCode,
                    Passed: false,
                    SourceValue: null,
                    FollowupValue: null,
                    ObservedDelta: null,
                    ExpectedThreshold: null,
                    Expression: $"{verification.Status} on '{ctx.ValueName}'",
                    FailureReason: verification.Context?.Reason ?? verification.Status.ToString());
            }
            catch (InvalidOperationException ex)
            {
                assertionResult = SystemMtAssertionResultV2.UnknownType(ctx.AssertionTypeCode) with
                {
                    FailureReason = ex.Message,
                };
            }

            var finalStatus = assertionResult.Passed ? PipelineStatus.Ok : PipelineStatus.Anomaly;
            progress?.Report(finalStatus);

            // Display-compat: first phase metrics → SourceMetrics, last phase metrics → FollowupMetrics
            var sourceMetricsDisplay = phaseMetrics[mp.Phases[0].Role];
            var followupMetricsDisplay = phaseMetrics[mp.Phases[mp.Phases.Count - 1].Role];

            return new PipelineOutcome(
                FinalStatus: finalStatus,
                ErrorMessage: null,
                StartedAt: startedAt,
                FinishedAt: DateTime.UtcNow,
                ArtifactsDirectory: artifactsDir,
                SourceInputPath: firstInputPath,
                FollowupInputPath: lastInputPath,
                SourceOutputPath: firstOutputPath,
                FollowupOutputPath: lastOutputPath,
                SourceMetrics: sourceMetricsDisplay,
                FollowupMetrics: followupMetricsDisplay,
                AssertionResult: assertionResult,
                SourceElapsed: firstElapsed,
                FollowupElapsed: lastElapsed,
                SourceExitCode: firstExitCode,
                FollowupExitCode: lastExitCode)
            {
                TypedSpec = typedSpec,
                TypedPredicate = typedPredicate,
                TypedVerification = verification,
                PhaseMetrics = phaseMetrics,
                SourceRuntimeRunId = firstRuntimeRunId,
                FollowupRuntimeRunId = lastRuntimeRunId,
            };
        }
        catch (OperationCanceledException)
        {
            return Fail(PipelineStatus.Cancelled, "Multi-phase pipeline cancelled by user");
        }
        catch (Exception ex)
        {
            return Fail(PipelineStatus.Error, $"{ex.GetType().Name}: {ex.Message}");
        }

        PipelineOutcome Fail(string status, string err) => new(
            FinalStatus: status,
            ErrorMessage: err,
            StartedAt: startedAt,
            FinishedAt: DateTime.UtcNow,
            ArtifactsDirectory: artifactsDir,
            SourceInputPath: firstInputPath,
            FollowupInputPath: lastInputPath,
            SourceOutputPath: firstOutputPath,
            FollowupOutputPath: lastOutputPath,
            SourceMetrics: null,
            FollowupMetrics: null,
            AssertionResult: null,
            SourceElapsed: firstElapsed,
            FollowupElapsed: lastElapsed,
            SourceExitCode: firstExitCode,
            FollowupExitCode: lastExitCode)
        {
            SourceRuntimeRunId = firstRuntimeRunId,
            FollowupRuntimeRunId = lastRuntimeRunId,
        };
    }

    private Task<ProcessResult> RunSutCommandAsync(
        PipelineContext ctx,
        ProcessInvocation invocation,
        string workingDirectory,
        CancellationToken cancellationToken) =>
        _runtimeProcessExecutor.RunAsync(
            ctx.RuntimeProfile,
            invocation,
            workingDirectory,
            ctx.TimeoutSeconds,
            cancellationToken);
}
