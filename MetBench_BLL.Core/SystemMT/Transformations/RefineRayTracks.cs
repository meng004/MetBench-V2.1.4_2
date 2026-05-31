using System.Globalization;
using MetBench_BLL.SystemMT.ParameterMapping;

namespace MetBench_BLL.SystemMT.Transformations;

/// <summary>
/// OpenMOC ray-track refinement: <c>num_azim *= factor</c> and
/// <c>azim_spacing_cm /= factor</c> as one atomic tracking transform.
/// </summary>
public sealed class RefineRayTracks : IMRTransformation
{
    private readonly IFieldPathResolver _resolver;

    public RefineRayTracks() : this(new JsonPointerResolver()) { }

    public RefineRayTracks(IFieldPathResolver resolver)
    {
        _resolver = resolver;
    }

    public string Name => "RefineRayTracks";

    public string ParametersSchema => """
        {
          "type": "object",
          "required": ["factor"],
          "properties": { "factor": { "type": "string", "pattern": "^[+]?[0-9]*\\.?[0-9]+([eE][-+]?[0-9]+)?$" } }
        }
        """;

    public Dictionary<string, object?> Apply(
        IReadOnlyDictionary<string, object?> sourceData,
        string targetFieldPath,
        IReadOnlyDictionary<string, string> parameters)
    {
        if (!parameters.TryGetValue("factor", out var factorText))
            throw new ArgumentException("RefineRayTracks requires 'factor' parameter");
        if (!double.TryParse(factorText, NumberStyles.Float, CultureInfo.InvariantCulture, out var factor))
            throw new ArgumentException($"RefineRayTracks 'factor' must be a number; got '{factorText}'");
        if (factor <= 0)
            throw new ArgumentException($"RefineRayTracks 'factor' must be > 0; got '{factorText}'");

        var trackingPath = string.IsNullOrWhiteSpace(targetFieldPath) ? "/tracking" : targetFieldPath;
        var numAzimPath = AppendPath(trackingPath, "num_azim");
        var spacingPath = AppendPath(trackingPath, "azim_spacing_cm");

        var oldNumAzim = Convert.ToDouble(_resolver.Get(sourceData, numAzimPath), CultureInfo.InvariantCulture);
        var oldSpacing = Convert.ToDouble(_resolver.Get(sourceData, spacingPath), CultureInfo.InvariantCulture);

        var newNumAzim = System.Math.Max(1, (int)System.Math.Round(oldNumAzim * factor, MidpointRounding.AwayFromZero));
        var newSpacing = oldSpacing / factor;

        var refined = _resolver.Set(sourceData, numAzimPath, newNumAzim);
        return _resolver.Set(refined, spacingPath, newSpacing);
    }

    private static string AppendPath(string prefix, string leaf)
    {
        var trimmed = prefix.TrimEnd('/');
        return string.IsNullOrEmpty(trimmed) ? "/" + leaf : trimmed + "/" + leaf;
    }
}
