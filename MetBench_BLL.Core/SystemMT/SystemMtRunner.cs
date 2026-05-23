namespace MetBench_BLL.SystemMT;

/// <summary>
/// W1 系统级 MT runner — 已被 W2 <see cref="MetBench_BLL.SystemMT.Pipeline.SystemMtPipeline"/>
/// 取代。本类标 <see cref="ObsoleteAttribute"/> 仅为保留 v1 兼容路径与既有测试覆盖；
/// 新代码不得使用，将在 Stage 9 重构中彻底移除（mr-architecture §2 协议层 / 执行层分离）。
/// </summary>
[Obsolete("W1 system-MT execution path. New code must use ISystemMtPipeline (mr-architecture §2). " +
          "Slated for Stage 9 removal. See G-07 in docs/requirements.md.")]
public sealed class SystemMtRunner
{
    private readonly CliProgramRunner _cliRunner;
    private readonly PythonOutputAdapter _outputAdapter;
    private readonly IReadOnlyDictionary<string, IMrAssertion> _assertions;
    private readonly InputGenerator? _inputGenerator;

    public SystemMtRunner(
        CliProgramRunner cliRunner,
        PythonOutputAdapter outputAdapter,
        IEnumerable<IMrAssertion> assertions,
        InputGenerator? inputGenerator = null)
    {
        _cliRunner = cliRunner ?? throw new ArgumentNullException(nameof(cliRunner));
        _outputAdapter = outputAdapter ?? throw new ArgumentNullException(nameof(outputAdapter));
        if (assertions is null)
        {
            throw new ArgumentNullException(nameof(assertions));
        }

        var byName = new Dictionary<string, IMrAssertion>(StringComparer.OrdinalIgnoreCase);
        foreach (var assertion in assertions)
        {
            if (assertion is null)
            {
                throw new ArgumentException("Assertions collection contains a null entry", nameof(assertions));
            }

            if (string.IsNullOrWhiteSpace(assertion.Name))
            {
                throw new ArgumentException(
                    $"Assertion {assertion.GetType().Name} has empty Name", nameof(assertions));
            }

            if (!byName.TryAdd(assertion.Name, assertion))
            {
                throw new ArgumentException(
                    $"Duplicate assertion registered for name '{assertion.Name}'", nameof(assertions));
            }
        }

        if (byName.Count == 0)
        {
            throw new ArgumentException("At least one assertion must be registered", nameof(assertions));
        }

        _assertions = byName;
        _inputGenerator = inputGenerator;
    }

    public async Task<SystemMtResult> RunAsync(
        SystemMtTask task,
        string valueName,
        CancellationToken cancellationToken)
    {
        InputGenerationResult? generation = null;
        SystemMtCase followUpCase;

        if (task.FollowUpCase is not null)
        {
            followUpCase = task.FollowUpCase;
        }
        else if (task.InputTransformation is not null)
        {
            if (_inputGenerator is null)
            {
                return FailedBeforeRun(task.AssertionName, valueName,
                    "Configuration failure: task requires input generation but no InputGenerator is registered.");
            }

            generation = await _inputGenerator.GenerateAsync(
                task.SourceCase.InputPath,
                task.GeneratedFollowUpInputPath!,
                task.InputTransformation,
                cancellationToken);

            if (!generation.Succeeded)
            {
                return FailedBeforeRun(task.AssertionName, valueName, generation.FailureReason, generation);
            }

            followUpCase = new SystemMtCase(
                task.GeneratedFollowUpCaseName!,
                generation.FollowUpInputPath,
                task.GeneratedFollowUpWorkingDirectory!,
                task.GeneratedFollowUpOutputPath!);
        }
        else
        {
            return FailedBeforeRun(task.AssertionName, valueName,
                "Configuration failure: task has neither a follow-up case nor an input transformation.");
        }

        var sourceRun = await _cliRunner.RunAsync(task.Program, task.SourceCase, task.Timeout, cancellationToken);
        if (!sourceRun.Succeeded)
        {
            return FailedAfterRun(sourceRun, sourceRun, task.AssertionName, valueName, sourceRun.FailureReason, generation);
        }

        var followUpRun = await _cliRunner.RunAsync(task.Program, followUpCase, task.Timeout, cancellationToken);
        if (!followUpRun.Succeeded)
        {
            return FailedAfterRun(sourceRun, followUpRun, task.AssertionName, valueName, followUpRun.FailureReason, generation);
        }

        var sourceOutput = await _outputAdapter.ParseAsync(task.Program.OutputAdapterPath, sourceRun.OutputPath, cancellationToken);
        var followUpOutput = await _outputAdapter.ParseAsync(task.Program.OutputAdapterPath, followUpRun.OutputPath, cancellationToken);
        var assertion = _assertions.TryGetValue(task.AssertionName, out var mrAssertion)
            ? mrAssertion.Evaluate(valueName, sourceOutput, followUpOutput)
            : new SystemMtAssertionResult(
                task.AssertionName, valueName, double.NaN, double.NaN, false,
                $"Configuration failure: unsupported assertion '{task.AssertionName}'. " +
                $"Registered assertions: [{string.Join(", ", _assertions.Keys)}]");

        var inputSamples = InputCaseReader.ReadSamples(
            task.SourceCase.InputPath, followUpCase.InputPath);

        return new SystemMtResult(
            sourceRun, followUpRun, sourceOutput, followUpOutput, assertion,
            assertion.Passed, assertion.FailureReason, generation, inputSamples);
    }

    private static SystemMtResult FailedBeforeRun(
        string assertionName, string valueName, string reason, InputGenerationResult? generation = null)
    {
        var emptyRun = new CliRunResult(string.Empty, -1, string.Empty, string.Empty, TimeSpan.Zero, string.Empty, false, reason);
        var emptyOutput = new ParsedOutput(new Dictionary<string, double>(), new Dictionary<string, string>());
        var assertion = new SystemMtAssertionResult(assertionName, valueName, double.NaN, double.NaN, false, reason);
        return new SystemMtResult(emptyRun, emptyRun, emptyOutput, emptyOutput, assertion, false, reason, generation);
    }

    private static SystemMtResult FailedAfterRun(
        CliRunResult sourceRun, CliRunResult followUpRun,
        string assertionName, string valueName, string reason, InputGenerationResult? generation)
    {
        var emptyOutput = new ParsedOutput(new Dictionary<string, double>(), new Dictionary<string, string>());
        var assertion = new SystemMtAssertionResult(assertionName, valueName, double.NaN, double.NaN, false, reason);
        return new SystemMtResult(sourceRun, followUpRun, emptyOutput, emptyOutput, assertion, false, reason, generation);
    }
}
