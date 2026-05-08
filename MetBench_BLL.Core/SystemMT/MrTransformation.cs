namespace MetBench_BLL.SystemMT;

public sealed class MrTransformation
{
    public MrTransformation(string name, IReadOnlyDictionary<string, string> parameters)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name cannot be empty", nameof(name));
        }

        if (parameters is null)
        {
            throw new ArgumentNullException(nameof(parameters));
        }

        Name = name;
        Parameters = new Dictionary<string, string>(parameters);
    }

    public string Name { get; }

    public IReadOnlyDictionary<string, string> Parameters { get; }
}
