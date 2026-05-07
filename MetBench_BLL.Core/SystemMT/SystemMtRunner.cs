namespace MetBench_BLL.SystemMT;

public sealed class SystemMtRunner
{
    private readonly CliProgramRunner _cliRunner;
    private readonly PythonOutputAdapter _outputAdapter;
    private readonly GreaterThanAssertion _greaterThanAssertion;

    public SystemMtRunner(
        CliProgramRunner cliRunner,
        PythonOutputAdapter outputAdapter,
        GreaterThanAssertion greaterThanAssertion)
    {
        _cliRunner = cliRunner;
        _outputAdapter = outputAdapter;
        _greaterThanAssertion = greaterThanAssertion;
    }

    public async Task<SystemMtResult> RunAsync(
        SystemMtTask task,
        string valueName,
        CancellationToken cancellationToken)
    {
        var sourceRun = await _cliRunner.RunAsync(task.Program, task.SourceCase, task.Timeout, cancellationToken);
        if (!sourceRun.Succeeded)
        {
            return FailedBeforeParsing(sourceRun, sourceRun, valueName, sourceRun.FailureReason);
        }

        var followUpRun = await _cliRunner.RunAsync(task.Program, task.FollowUpCase, task.Timeout, cancellationToken);
        if (!followUpRun.Succeeded)
        {
            return FailedBeforeParsing(sourceRun, followUpRun, valueName, followUpRun.FailureReason);
        }

        var sourceOutput = await _outputAdapter.ParseAsync(task.Program.OutputAdapterPath, sourceRun.OutputPath, cancellationToken);
        var followUpOutput = await _outputAdapter.ParseAsync(task.Program.OutputAdapterPath, followUpRun.OutputPath, cancellationToken);
        var assertion = task.AssertionName switch
        {
            "GreaterThan" => _greaterThanAssertion.Evaluate(valueName, sourceOutput, followUpOutput),
            _ => new SystemMtAssertionResult(
                task.AssertionName,
                valueName,
                double.NaN,
                double.NaN,
                false,
                $"Configuration failure: unsupported assertion '{task.AssertionName}'")
        };

        return new SystemMtResult(
            sourceRun,
            followUpRun,
            sourceOutput,
            followUpOutput,
            assertion,
            assertion.Passed,
            assertion.FailureReason);
    }

    private static SystemMtResult FailedBeforeParsing(
        CliRunResult sourceRun,
        CliRunResult followUpRun,
        string valueName,
        string failureReason)
    {
        var emptyOutput = new ParsedOutput(new Dictionary<string, double>(), new Dictionary<string, string>());
        var assertion = new SystemMtAssertionResult(
            "GreaterThan",
            valueName,
            double.NaN,
            double.NaN,
            false,
            failureReason);

        return new SystemMtResult(
            sourceRun,
            followUpRun,
            emptyOutput,
            emptyOutput,
            assertion,
            false,
            failureReason);
    }
}
