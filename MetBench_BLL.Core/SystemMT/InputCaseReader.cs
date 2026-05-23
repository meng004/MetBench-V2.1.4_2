using System.Globalization;
using System.Text.Json;

namespace MetBench_BLL.SystemMT;

/// <summary>
/// Reads a System-MT run's source and follow-up input case files and pairs
/// their numeric leaves into <see cref="InputSamplePoint"/>s. Best-effort: a
/// missing, unreadable, or non-JSON file yields an empty list rather than an
/// exception — capturing sample points is run-record enrichment and must never
/// fail the run itself.
/// </summary>
public static class InputCaseReader
{
    /// <summary>
    /// Pair every numeric input leaf of the source and follow-up case files.
    /// Non-numeric leaves (strings, booleans, null) are skipped. Arrays are
    /// flattened with positional indices (<c>sigma_t.0</c>, <c>sigma_t.1</c>).
    /// A leaf present in only one file gets <see cref="double.NaN"/> on the
    /// missing side. Returns an empty list if either file cannot be read.
    /// </summary>
    public static IReadOnlyList<InputSamplePoint> ReadSamples(string sourcePath, string followUpPath)
    {
        var source = TryFlatten(sourcePath);
        var followUp = TryFlatten(followUpPath);
        if (source is null || followUp is null)
        {
            return Array.Empty<InputSamplePoint>();
        }

        var names = new SortedSet<string>(source.Keys, StringComparer.Ordinal);
        names.UnionWith(followUp.Keys);

        var samples = new List<InputSamplePoint>(names.Count);
        foreach (var name in names)
        {
            samples.Add(new InputSamplePoint
            {
                Name = name,
                SourceValue = source.TryGetValue(name, out var s) ? s : double.NaN,
                FollowUpValue = followUp.TryGetValue(name, out var f) ? f : double.NaN,
            });
        }
        return samples;
    }

    private static Dictionary<string, double>? TryFlatten(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return null;
            }

            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var flat = new Dictionary<string, double>(StringComparer.Ordinal);
            Flatten(document.RootElement, prefix: string.Empty, flat);
            return flat;
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static void Flatten(JsonElement element, string prefix, Dictionary<string, double> sink)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    Flatten(property.Value, Join(prefix, property.Name), sink);
                }
                break;

            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    Flatten(item, Join(prefix, index.ToString(CultureInfo.InvariantCulture)), sink);
                    index++;
                }
                break;

            case JsonValueKind.Number:
                if (element.TryGetDouble(out var value))
                {
                    sink[prefix.Length == 0 ? "value" : prefix] = value;
                }
                break;

            // Strings, booleans and null are not numeric inputs — skipped.
        }
    }

    private static string Join(string prefix, string key) =>
        prefix.Length == 0 ? key : prefix + "." + key;
}
