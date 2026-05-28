using System.Text.Json;
using System.Text.RegularExpressions;

namespace MetBench_BLL.SystemMT.Catalog.Editing;

public sealed class SystemMtSampleCaseEditor : ISystemMtSampleCaseEditor
{
    private static readonly Regex FileNamePattern = new(
        @"^[a-z0-9][a-z0-9_-]*\.json$",
        RegexOptions.Compiled);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private readonly string _sutRoot;

    public SystemMtSampleCaseEditor(string sutRoot)
    {
        if (string.IsNullOrWhiteSpace(sutRoot))
            throw new ArgumentException("SUT root is required", nameof(sutRoot));
        _sutRoot = Path.GetFullPath(sutRoot);
    }

    public IReadOnlyList<SystemMtSampleCaseDescriptor> ListSamples(string sutId)
    {
        var sampleDir = ResolveSampleDirectory(sutId);
        if (!Directory.Exists(sampleDir))
            return Array.Empty<SystemMtSampleCaseDescriptor>();

        return Directory.GetFiles(sampleDir, "*.json")
            .Select(p => new SystemMtSampleCaseDescriptor(sutId, Path.GetFileName(p), Path.GetFullPath(p)))
            .OrderBy(d => d.FileName, StringComparer.Ordinal)
            .ToList();
    }

    public string LoadSample(string sutId, string fileName)
    {
        var path = ResolveExistingSamplePath(sutId, fileName);
        return File.ReadAllText(path);
    }

    public SystemMtManifestEditResult ValidateDraft(string sutId, string fileName, string jsonText)
    {
        try
        {
            EnsureValidFileName(fileName);
            EnsureSutIdValid(sutId);
            EnsureJsonParseable(jsonText);
            return SystemMtManifestEditResult.Ok();
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or JsonException)
        {
            return SystemMtManifestEditResult.Fail(ex.Message);
        }
    }

    public SystemMtManifestEditResult SaveDraft(string sutId, string fileName, string jsonText)
    {
        var validation = ValidateDraft(sutId, fileName, jsonText);
        if (!validation.Success) return validation;

        var sampleDir = ResolveSampleDirectory(sutId);
        Directory.CreateDirectory(sampleDir);
        var samplePath = Path.GetFullPath(Path.Combine(sampleDir, fileName));
        EnsureUnderSampleDir(samplePath, sampleDir, sutId, fileName);

        var tmpPath = samplePath + ".tmp";
        File.WriteAllText(tmpPath, jsonText);
        if (File.Exists(samplePath))
            File.Replace(tmpPath, samplePath, null);
        else
            File.Move(tmpPath, samplePath);

        return SystemMtManifestEditResult.Ok();
    }

    public SystemMtManifestEditResult Delete(string sutId, string fileName)
    {
        try
        {
            EnsureValidFileName(fileName);
            var samplePath = ResolveExistingSamplePath(sutId, fileName);

            var references = FindReferencingMrIds(sutId, fileName);
            if (references.Count > 0)
                return SystemMtManifestEditResult.Fail(
                    $"sample/{fileName} is referenced by {references.Count} MR binding(s): "
                    + string.Join(", ", references));

            File.Delete(samplePath);
            return SystemMtManifestEditResult.Ok();
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or FileNotFoundException)
        {
            return SystemMtManifestEditResult.Fail(ex.Message);
        }
    }

    private IReadOnlyList<string> FindReferencingMrIds(string sutId, string fileName)
    {
        // sample_case_relative_path is per-SUT — we only need to scan this SUT's
        // catalog.json, not the whole SUT root.
        var manifestPath = ResolveExistingManifestPath(sutId);
        var references = new List<string>();
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(manifestPath));
            if (!doc.RootElement.TryGetProperty("mrs", out var mrs) || mrs.ValueKind != JsonValueKind.Array)
                return references;

            var expected = $"sample/{fileName}";
            foreach (var mr in mrs.EnumerateArray())
            {
                if (!mr.TryGetProperty("sample_case_relative_path", out var sampleProp))
                    continue;
                var path = sampleProp.GetString();
                if (string.Equals(path, expected, StringComparison.Ordinal))
                {
                    var id = mr.TryGetProperty("mr_id", out var idProp)
                        ? idProp.GetString() ?? "(unnamed)"
                        : "(unnamed)";
                    references.Add(id);
                }
            }
        }
        catch (JsonException)
        {
            // Malformed manifest cannot be guarded by us — defer to launcher's existing
            // catalog validation. Return no references so delete is not silently blocked
            // by an unparseable file.
        }
        return references;
    }

    private static void EnsureValidFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name is required", nameof(fileName));
        if (!FileNamePattern.IsMatch(fileName))
            throw new ArgumentException(
                $"Invalid sample file name '{fileName}'; must match ^[a-z0-9][a-z0-9_-]*\\.json$",
                nameof(fileName));
    }

    private static void EnsureSutIdValid(string sutId)
    {
        if (string.IsNullOrWhiteSpace(sutId))
            throw new ArgumentException("SUT id is required", nameof(sutId));
        if (Path.IsPathRooted(sutId)
            || sutId.Contains("..", StringComparison.Ordinal)
            || sutId.Contains('/')
            || sutId.Contains('\\')
            || sutId.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            throw new ArgumentException($"Invalid SUT id '{sutId}'", nameof(sutId));
    }

    private static void EnsureJsonParseable(string jsonText)
    {
        if (string.IsNullOrWhiteSpace(jsonText))
            throw new ArgumentException("Sample JSON body is required", nameof(jsonText));
        // Throws JsonException on malformed input; caller wraps as Fail.
        using var _ = JsonDocument.Parse(jsonText, new JsonDocumentOptions
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip,
        });
    }

    private string ResolveSampleDirectory(string sutId)
    {
        EnsureSutIdValid(sutId);
        var sutDir = Path.GetFullPath(Path.Combine(_sutRoot, sutId));
        EnsureUnderSutRoot(sutDir, sutId);
        return Path.Combine(sutDir, "sample");
    }

    private string ResolveExistingManifestPath(string sutId)
    {
        EnsureSutIdValid(sutId);
        var sutDir = Path.GetFullPath(Path.Combine(_sutRoot, sutId));
        EnsureUnderSutRoot(sutDir, sutId);
        var path = Path.GetFullPath(Path.Combine(sutDir, "catalog.json"));
        if (!File.Exists(path))
            throw new FileNotFoundException($"System MT manifest not found for SUT '{sutId}'", path);
        return path;
    }

    private string ResolveExistingSamplePath(string sutId, string fileName)
    {
        EnsureValidFileName(fileName);
        var sampleDir = ResolveSampleDirectory(sutId);
        var samplePath = Path.GetFullPath(Path.Combine(sampleDir, fileName));
        EnsureUnderSampleDir(samplePath, sampleDir, sutId, fileName);
        if (!File.Exists(samplePath))
            throw new FileNotFoundException($"Sample '{fileName}' not found under '{sampleDir}'", samplePath);
        return samplePath;
    }

    private void EnsureUnderSutRoot(string path, string sutId)
    {
        var root = Path.EndsInDirectorySeparator(_sutRoot)
            ? _sutRoot
            : _sutRoot + Path.DirectorySeparatorChar;
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        if (!path.StartsWith(root, comparison))
            throw new ArgumentException($"Invalid SUT id '{sutId}'", nameof(sutId));
    }

    private static void EnsureUnderSampleDir(string path, string sampleDir, string sutId, string fileName)
    {
        var root = Path.EndsInDirectorySeparator(sampleDir)
            ? sampleDir
            : sampleDir + Path.DirectorySeparatorChar;
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        if (!path.StartsWith(root, comparison))
            throw new ArgumentException(
                $"Sample path escapes SUT '{sutId}' sample directory: {fileName}",
                nameof(fileName));
    }
}
