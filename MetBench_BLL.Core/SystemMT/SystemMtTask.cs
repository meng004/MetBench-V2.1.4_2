namespace MetBench_BLL.SystemMT;

public sealed class SystemMtTask
{
    private SystemMtTask(
        SystemProgram program,
        SystemMtCase sourceCase,
        SystemMtCase? followUpCase,
        string? generatedFollowUpCaseName,
        string? generatedFollowUpInputPath,
        string? generatedFollowUpWorkingDirectory,
        string? generatedFollowUpOutputPath,
        MrTransformation? inputTransformation,
        string assertionName,
        TimeSpan timeout)
    {
        Program = program ?? throw new ArgumentNullException(nameof(program));
        SourceCase = sourceCase ?? throw new ArgumentNullException(nameof(sourceCase));
        FollowUpCase = followUpCase;
        GeneratedFollowUpCaseName = generatedFollowUpCaseName;
        GeneratedFollowUpInputPath = generatedFollowUpInputPath;
        GeneratedFollowUpWorkingDirectory = generatedFollowUpWorkingDirectory;
        GeneratedFollowUpOutputPath = generatedFollowUpOutputPath;
        InputTransformation = inputTransformation;
        AssertionName = string.IsNullOrWhiteSpace(assertionName)
            ? throw new ArgumentException("AssertionName cannot be empty", nameof(assertionName))
            : assertionName;
        Timeout = timeout > TimeSpan.Zero
            ? timeout
            : throw new ArgumentException("Timeout must be greater than zero", nameof(timeout));
    }

    public SystemProgram Program { get; }
    public SystemMtCase SourceCase { get; }
    public SystemMtCase? FollowUpCase { get; }
    public string? GeneratedFollowUpCaseName { get; }
    public string? GeneratedFollowUpInputPath { get; }
    public string? GeneratedFollowUpWorkingDirectory { get; }
    public string? GeneratedFollowUpOutputPath { get; }
    public MrTransformation? InputTransformation { get; }
    public string AssertionName { get; }
    public TimeSpan Timeout { get; }

    public static SystemMtTask WithFollowUpCase(
        SystemProgram program,
        SystemMtCase sourceCase,
        SystemMtCase followUpCase,
        string assertionName,
        TimeSpan timeout)
    {
        if (followUpCase is null)
        {
            throw new ArgumentNullException(nameof(followUpCase));
        }

        if (sourceCase is not null
            && sourceCase.CaseName.Equals(followUpCase.CaseName, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Source and follow-up case names must be different");
        }

        return new SystemMtTask(
            program, sourceCase!, followUpCase,
            generatedFollowUpCaseName: null,
            generatedFollowUpInputPath: null,
            generatedFollowUpWorkingDirectory: null,
            generatedFollowUpOutputPath: null,
            inputTransformation: null,
            assertionName,
            timeout);
    }

    public static SystemMtTask WithGeneratedFollowUp(
        SystemProgram program,
        SystemMtCase sourceCase,
        string followUpCaseName,
        string followUpInputPath,
        string followUpWorkingDirectory,
        string followUpOutputPath,
        MrTransformation transformation,
        string assertionName,
        TimeSpan timeout)
    {
        if (transformation is null)
        {
            throw new ArgumentNullException(nameof(transformation));
        }

        if (string.IsNullOrWhiteSpace(followUpCaseName))
        {
            throw new ArgumentException("followUpCaseName cannot be empty", nameof(followUpCaseName));
        }

        if (sourceCase is not null
            && sourceCase.CaseName.Equals(followUpCaseName, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Source and follow-up case names must be different");
        }

        return new SystemMtTask(
            program, sourceCase!,
            followUpCase: null,
            generatedFollowUpCaseName: followUpCaseName,
            generatedFollowUpInputPath: followUpInputPath,
            generatedFollowUpWorkingDirectory: followUpWorkingDirectory,
            generatedFollowUpOutputPath: followUpOutputPath,
            inputTransformation: transformation,
            assertionName,
            timeout);
    }
}
