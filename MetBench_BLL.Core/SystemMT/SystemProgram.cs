namespace MetBench_BLL.SystemMT;

public sealed class SystemProgram : TargetProgram
{
    public SystemProgram(
        ProgramLanguage language,
        string profileName,
        string executablePath,
        string argumentTemplate,
        string outputAdapterPath,
        IReadOnlySet<int>? acceptableExitCodes = null)
        : base(language)
    {
        ProfileName = RequireText(profileName, nameof(ProfileName));
        ExecutablePath = RequireText(executablePath, nameof(ExecutablePath));
        ArgumentTemplate = RequireText(argumentTemplate, nameof(ArgumentTemplate));
        OutputAdapterPath = RequireText(outputAdapterPath, nameof(OutputAdapterPath));
        AcceptableExitCodes = acceptableExitCodes ?? new HashSet<int> { 0 };
    }

    public override string ProgramType => "System";

    public string ProfileName { get; }

    public string ExecutablePath { get; }

    public string ArgumentTemplate { get; }

    public string OutputAdapterPath { get; }

    public IReadOnlySet<int> AcceptableExitCodes { get; }

    public override object GetProgramData()
    {
        return new
        {
            Language = GetLanguageName(),
            ProfileName,
            ExecutablePath,
            ArgumentTemplate,
            OutputAdapterPath,
            AcceptableExitCodes
        };
    }

    private static string RequireText(string value, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{propertyName} cannot be empty", propertyName);
        }

        return value;
    }
}
