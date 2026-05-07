namespace MetBench_BLL.SystemMT;

public sealed class SystemMtCase
{
    public SystemMtCase(
        string caseName,
        string inputPath,
        string workingDirectory,
        string outputPath,
        IReadOnlyDictionary<string, string>? environmentVariables = null)
    {
        CaseName = RequireText(caseName, nameof(CaseName));
        InputPath = RequireText(inputPath, nameof(InputPath));
        WorkingDirectory = RequireText(workingDirectory, nameof(WorkingDirectory));
        OutputPath = RequireText(outputPath, nameof(OutputPath));
        EnvironmentVariables = environmentVariables ?? new Dictionary<string, string>();
    }

    public string CaseName { get; }

    public string InputPath { get; }

    public string WorkingDirectory { get; }

    public string OutputPath { get; }

    public IReadOnlyDictionary<string, string> EnvironmentVariables { get; }

    private static string RequireText(string value, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{propertyName} cannot be empty", propertyName);
        }

        return value;
    }
}
