namespace MetBench_BLL.SystemMT;

public sealed class SystemMtTask
{
    public SystemMtTask(
        SystemProgram program,
        SystemMtCase sourceCase,
        SystemMtCase followUpCase,
        string assertionName,
        TimeSpan timeout)
    {
        Program = program ?? throw new ArgumentNullException(nameof(program));
        SourceCase = sourceCase ?? throw new ArgumentNullException(nameof(sourceCase));
        FollowUpCase = followUpCase ?? throw new ArgumentNullException(nameof(followUpCase));
        AssertionName = RequireText(assertionName, nameof(AssertionName));
        Timeout = timeout;

        if (SourceCase.CaseName.Equals(FollowUpCase.CaseName, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Source and follow-up case names must be different");
        }

        if (timeout <= TimeSpan.Zero)
        {
            throw new ArgumentException("Timeout must be greater than zero", nameof(timeout));
        }
    }

    public SystemProgram Program { get; }

    public SystemMtCase SourceCase { get; }

    public SystemMtCase FollowUpCase { get; }

    public string AssertionName { get; }

    public TimeSpan Timeout { get; }

    private static string RequireText(string value, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{propertyName} cannot be empty", propertyName);
        }

        return value;
    }
}
