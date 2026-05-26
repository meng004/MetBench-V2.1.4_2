using System.Text.Json;
using MetBench_BLL.SystemMT.Catalog;

namespace MetBench_BLL.SystemMT.Catalog.Editing;

public sealed class SystemMtManifestCatalogEditor : ISystemMtManifestCatalogEditor
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private readonly string _sutRoot;

    public SystemMtManifestCatalogEditor(string sutRoot)
    {
        if (string.IsNullOrWhiteSpace(sutRoot))
            throw new ArgumentException("SUT root is required", nameof(sutRoot));

        _sutRoot = Path.GetFullPath(sutRoot);
    }

    public IReadOnlyList<SystemMtManifestDescriptor> ListManifests()
    {
        if (!Directory.Exists(_sutRoot))
            return Array.Empty<SystemMtManifestDescriptor>();

        return Directory.GetDirectories(_sutRoot)
            .Where(IsSafeSutDirectory)
            .Select(dir => new
            {
                SutId = Path.GetFileName(dir),
                ManifestPath = Path.GetFullPath(Path.Combine(dir, "catalog.json")),
            })
            .Where(x => File.Exists(x.ManifestPath))
            .OrderBy(x => x.SutId, StringComparer.Ordinal)
            .Select(x => new SystemMtManifestDescriptor(x.SutId, x.ManifestPath, x.SutId))
            .ToList();
    }

    public SystemMtCatalogDocument Load(string sutId)
    {
        var path = ResolveManifestPath(sutId);
        var json = File.ReadAllText(path);
        var doc = JsonSerializer.Deserialize<SystemMtCatalogDocument>(json, JsonOptions)
            ?? throw new CatalogValidationException($"Manifest '{path}' deserialized to null");
        doc.Validate();
        return doc;
    }

    public SystemMtManifestEditResult ValidateDraft(string sutId, SystemMtMrBindingDraft draft)
    {
        try
        {
            BuildValidatedDocument(sutId, draft);
            return SystemMtManifestEditResult.Ok();
        }
        catch (Exception ex) when (ex is CatalogValidationException or ArgumentException or InvalidOperationException)
        {
            return SystemMtManifestEditResult.Fail(ex.Message);
        }
    }

    public SystemMtManifestEditResult SaveDraft(string sutId, SystemMtMrBindingDraft draft)
    {
        SystemMtCatalogDocument document;
        try
        {
            document = BuildValidatedDocument(sutId, draft);
        }
        catch (Exception ex) when (ex is CatalogValidationException or ArgumentException or InvalidOperationException)
        {
            return SystemMtManifestEditResult.Fail(ex.Message);
        }

        var path = ResolveManifestPath(sutId);
        var tmpPath = path + ".tmp";
        File.WriteAllText(tmpPath, JsonSerializer.Serialize(document, JsonOptions));

        if (File.Exists(path))
            File.Replace(tmpPath, path, null);
        else
            File.Move(tmpPath, path);

        return SystemMtManifestEditResult.Ok();
    }

    private SystemMtCatalogDocument BuildValidatedDocument(string sutId, SystemMtMrBindingDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);

        var existing = Load(sutId);
        var binding = draft with { SutName = existing.SutName };
        var replacement = binding.ToBinding();
        replacement.Validate();

        var mrs = existing.Mrs
            .Where(mr => !string.Equals(mr.MrId, replacement.MrId, StringComparison.Ordinal))
            .ToList();
        mrs.Add(replacement);

        var document = new SystemMtCatalogDocument
        {
            SutName = existing.SutName,
            Program = existing.Program,
            Mrs = mrs,
        };
        document.Validate();
        return document;
    }

    private string ResolveManifestPath(string sutId)
    {
        if (string.IsNullOrWhiteSpace(sutId))
            throw new ArgumentException("SUT id is required", nameof(sutId));
        if (Path.IsPathRooted(sutId)
            || sutId.Contains("..", StringComparison.Ordinal)
            || sutId.Contains('/')
            || sutId.Contains('\\')
            || sutId.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            throw new ArgumentException($"Invalid SUT id '{sutId}'", nameof(sutId));

        var sutDir = Path.GetFullPath(Path.Combine(_sutRoot, sutId));
        EnsureUnderSutRoot(sutDir, sutId);
        if (Directory.Exists(sutDir) && IsReparsePoint(sutDir))
            throw new InvalidOperationException($"SUT directory '{sutId}' must not be a reparse point");

        var path = Path.GetFullPath(Path.Combine(sutDir, "catalog.json"));
        EnsureUnderSutRoot(path, sutId);
        if (!File.Exists(path))
            throw new FileNotFoundException($"System MT manifest not found for SUT '{sutId}'", path);
        return path;
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

    private static bool IsSafeSutDirectory(string path)
    {
        try
        {
            return !IsReparsePoint(path);
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static bool IsReparsePoint(string path) =>
        new DirectoryInfo(path).Attributes.HasFlag(FileAttributes.ReparsePoint);
}
