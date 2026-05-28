using System.Text.Json;

namespace MetBench_BLL.SystemMT.Catalog.Editing;

public sealed class SystemMtSutEditor : ISystemMtSutEditor
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private readonly string _sutRoot;

    public SystemMtSutEditor(string sutRoot)
    {
        if (string.IsNullOrWhiteSpace(sutRoot))
            throw new ArgumentException("SUT root is required", nameof(sutRoot));

        _sutRoot = Path.GetFullPath(sutRoot);
    }

    public IReadOnlyList<SystemMtSutDescriptor> ListSuts()
    {
        if (!Directory.Exists(_sutRoot))
            return Array.Empty<SystemMtSutDescriptor>();

        var results = new List<SystemMtSutDescriptor>();
        foreach (var dir in Directory.GetDirectories(_sutRoot))
        {
            if (!IsSafeSutDirectory(dir)) continue;
            var manifestPath = Path.GetFullPath(Path.Combine(dir, "catalog.json"));
            if (!File.Exists(manifestPath)) continue;

            var sutId = Path.GetFileName(dir);
            var (programName, equation) = TryReadProgramSummary(manifestPath);
            results.Add(new SystemMtSutDescriptor(sutId, manifestPath, programName, equation));
        }

        return results
            .OrderBy(s => s.SutId, StringComparer.Ordinal)
            .ToList();
    }

    public SystemMtSutProgramDraft Load(string sutId)
    {
        var path = ResolveExistingManifestPath(sutId);
        var json = File.ReadAllText(path);
        var doc = JsonSerializer.Deserialize<SystemMtCatalogDocument>(json, JsonOptions)
            ?? throw new CatalogValidationException($"Manifest '{path}' deserialized to null");

        if (doc.Program is null)
            throw new CatalogValidationException($"Manifest '{path}' has no program section");

        return SystemMtSutProgramDraft.FromProgram(doc.SutName, doc.Program);
    }

    public SystemMtManifestEditResult ValidateDraft(string sutId, SystemMtSutProgramDraft draft)
    {
        try
        {
            BuildValidatedProgram(sutId, draft);
            return SystemMtManifestEditResult.Ok();
        }
        catch (Exception ex) when (ex is CatalogValidationException or ArgumentException or InvalidOperationException or FileNotFoundException)
        {
            return SystemMtManifestEditResult.Fail(ex.Message);
        }
    }

    public SystemMtManifestEditResult SaveDraft(string sutId, SystemMtSutProgramDraft draft)
    {
        ProgramDefinition program;
        try
        {
            program = BuildValidatedProgram(sutId, draft);
        }
        catch (Exception ex) when (ex is CatalogValidationException or ArgumentException or InvalidOperationException or FileNotFoundException)
        {
            return SystemMtManifestEditResult.Fail(ex.Message);
        }

        var sutDir = ResolveSutDirectory(sutId);
        var manifestPath = Path.GetFullPath(Path.Combine(sutDir, "catalog.json"));

        SystemMtCatalogDocument document;
        if (File.Exists(manifestPath))
        {
            // Preserve existing MR bindings verbatim; only the program section is editable here.
            var existingJson = File.ReadAllText(manifestPath);
            var existing = JsonSerializer.Deserialize<SystemMtCatalogDocument>(existingJson, JsonOptions)
                ?? throw new InvalidOperationException($"Manifest '{manifestPath}' deserialized to null");
            document = new SystemMtCatalogDocument
            {
                SutName = sutId,
                Program = program,
                Mrs = existing.Mrs,
            };
            // Full-document validate is safe here: existing Mrs are non-empty by the document
            // schema invariant (Mrs.Count >= 1).
            document.Validate();
        }
        else
        {
            // Bootstrap path: create the SUT directory and write a fresh manifest with an empty
            // Mrs array. Caller follows up via ISystemMtManifestCatalogEditor to add MR bindings.
            // The empty catalog will NOT parse through the launcher until at least one MR is
            // added — that is intentional fail-closed behaviour matching SystemMtCatalogDocument.Validate.
            Directory.CreateDirectory(sutDir);
            document = new SystemMtCatalogDocument
            {
                SutName = sutId,
                Program = program,
                Mrs = new List<MrBindingDefinition>(),
            };
            // Program-only validate; skip full document validate (would reject Mrs.Count == 0).
        }

        var tmpPath = manifestPath + ".tmp";
        File.WriteAllText(tmpPath, JsonSerializer.Serialize(document, JsonOptions));
        if (File.Exists(manifestPath))
            File.Replace(tmpPath, manifestPath, null);
        else
            File.Move(tmpPath, manifestPath);

        return SystemMtManifestEditResult.Ok();
    }

    private ProgramDefinition BuildValidatedProgram(string sutId, SystemMtSutProgramDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);

        if (!string.Equals(draft.SutName, sutId, StringComparison.Ordinal))
            throw new CatalogValidationException(
                $"Draft SutName '{draft.SutName}' does not match target SUT id '{sutId}'");

        var sutDir = ResolveSutDirectory(sutId);
        var program = draft.ToProgram();
        program.Validate();

        // Script existence check — non-blank script paths must resolve to actual files
        // under the SUT directory. Empty adapter / parser paths are allowed (they default
        // to no-op at runtime); the runner script is required and already non-empty by
        // ProgramDefinition.Validate.
        ValidateScriptExists(sutDir, program.RunnerScriptRelativePath, nameof(program.RunnerScriptRelativePath));
        ValidateScriptExistsIfSet(sutDir, program.InputParserScriptRelativePath, nameof(program.InputParserScriptRelativePath));
        ValidateScriptExistsIfSet(sutDir, program.OutputParserScriptRelativePath, nameof(program.OutputParserScriptRelativePath));
        ValidateScriptExistsIfSet(sutDir, program.InputAdapterScriptRelativePath, nameof(program.InputAdapterScriptRelativePath));
        ValidateScriptExistsIfSet(sutDir, program.OutputAdapterScriptRelativePath, nameof(program.OutputAdapterScriptRelativePath));

        return program;
    }

    private static void ValidateScriptExists(string sutDir, string relativePath, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            throw new CatalogValidationException($"{fieldName} is required");
        var abs = Path.GetFullPath(Path.Combine(sutDir, relativePath));
        if (!File.Exists(abs))
            throw new FileNotFoundException(
                $"{fieldName} script not found: {relativePath} (resolved to {abs})", abs);
    }

    private static void ValidateScriptExistsIfSet(string sutDir, string relativePath, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return;
        var abs = Path.GetFullPath(Path.Combine(sutDir, relativePath));
        if (!File.Exists(abs))
            throw new FileNotFoundException(
                $"{fieldName} script not found: {relativePath} (resolved to {abs})", abs);
    }

    private string ResolveSutDirectory(string sutId)
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
        return sutDir;
    }

    private string ResolveExistingManifestPath(string sutId)
    {
        var sutDir = ResolveSutDirectory(sutId);
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

    private static (string ProgramName, string Equation) TryReadProgramSummary(string manifestPath)
    {
        try
        {
            var doc = JsonSerializer.Deserialize<SystemMtCatalogDocument>(
                File.ReadAllText(manifestPath), JsonOptions);
            if (doc?.Program is null)
                return (string.Empty, string.Empty);
            return (doc.Program.ProgramName, doc.Program.Equation);
        }
        catch (JsonException)
        {
            return (string.Empty, string.Empty);
        }
    }

    private static bool IsSafeSutDirectory(string path)
    {
        try
        {
            return !IsReparsePoint(path);
        }
        catch (IOException) { return false; }
        catch (UnauthorizedAccessException) { return false; }
    }

    private static bool IsReparsePoint(string path) =>
        new DirectoryInfo(path).Attributes.HasFlag(FileAttributes.ReparsePoint);
}
