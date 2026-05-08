namespace MetBench_BLL.SystemMT;

public sealed class SystemMtRunner
{
    private readonly CliProgramRunner _cliRunner;
    private readonly PythonOutputAdapter _outputAdapter;
    private readonly GreaterThanAssertion _greaterThanAssertion;
    private readonly InputGenerator? _inputGenerator;

    public SystemMtRunner(
        CliProgramRunner cliRunner,
        PythonOutputAdapter outputAdapter,
        GreaterThanAssertion greaterThanAssertion,
        InputGenerator? inputGenerator = null)
    {
        _cliRunner = cliRunner ?? throw new ArgumentNullException(nameof(cliRunner));
        _outputAdapter = outputAdapter ?? throw new ArgumentNullException(nameof(outputAdapter));
        _greaterThanAssertion = greaterThanAssertion ?? throw new ArgumentNullException(nameof(greaterThanAssertion));
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
                return FailedBeforeRun(valueName,
                    "Configuration failure: task requires input generation but no InputGenerator is registered.");
            }

            generation = await _inputGenerator.GenerateAsync(
                task.SourceCase.InputPath,
                task.GeneratedFollowUpInputPath!,
                task.InputTransformation,
                cancellationToken);

            if (!generation.Succeeded)
            {
                return FailedBeforeRun(valueName, generation.FailureReason, generation);
            }

            followUpCase = new SystemMtCase(
                task.GeneratedFollowUpCaseName!,
                generation.FollowUpInputPath,
                task.GeneratedFollowUpWorkingDirectory!,
                task.GeneratedFollowUpOutputPath!);
        }
        else
        {
            return FailedBeforeRun(valueName,
                "Configuration failure: task has neither a follow-up case nor an input transformation.");
        }

        var sourceRun = await _cliRunner.RunAsync(task.Program, task.SourceCase, task.Timeout, cancellationToken);
        if (!sourceRun.Succeeded)
        {
            return FailedAfterRun(sourceRun, sourceRun, valueName, sourceRun.FailureReason, generation);
        }

        var followUpRun = await _cliRunner.RunAsync(task.Program, followUpCase, task.Timeout, cancellationToken);
        if (!followUpRun.Succeeded)
        {
            return FailedAfterRun(sourceRun, followUpRun, valueName, followUpRun.FailureReason, generation);
        }

        var sourceOutput = await _outputAdapter.ParseAsync(task.Program.OutputAdapterPath, sourceRun.OutputPath, cancellationToken);
        var followUpOutput = await _outputAdapter.ParseAsync(task.Program.OutputAdapterPath, followUpRun.OutputPath, cancellationToken);
        var assertion = task.AssertionName switch
        {
            "GreaterThan" => _greaterThanAssertion.Evaluate(valueName, sourceOutput, followUpOutput),
            _ => new SystemMtAssertionResult(
                task.AssertionName, valueName, double.NaN, double.NaN, false,
                $"Configuration failure: unsupported assertion '{task.AssertionName}'")
        };

        return new SystemMtResult(
            sourceRun, followUpRun, sourceOutput, followUpOutput, assertion,
            assertion.Passed, assertion.FailureReason, generation);
    }

    private static SystemMtResult FailedBeforeRun(string valueName, string reason, InputGenerationResult? generation = null)
    {
        var emptyRun = new CliRunResult(string.Empty, -1, string.Empty, string.Empty, TimeSpan.Zero, string.Empty, false, reason);
        var emptyOutput = new ParsedOutput(new Dictionary<string, double>(), new Dictionary<string, string>());
        var assertion = new SystemMtAssertionResult("GreaterThan", valueName, double.NaN, double.NaN, false, reason);
        return new SystemMtResult(emptyRun, emptyRun, emptyOutput, emptyOutput, assertion, false, reason, generation);
    }

    private static SystemMtResult FailedAfterRun(
        CliRunResult sourceRun, CliRunResult followUpRun, string valueName, string reason, InputGenerationResult? generation)
    {
        var emptyOutput = new ParsedOutput(new Dictionary<string, double>(), new Dictionary<string, string>());
        var assertion = new SystemMtAssertionResult("GreaterThan", valueName, double.NaN, double.NaN, false, reason);
        return new SystemMtResult(sourceRun, followUpRun, emptyOutput, emptyOutput, assertion, false, reason, generation);
    }
}
